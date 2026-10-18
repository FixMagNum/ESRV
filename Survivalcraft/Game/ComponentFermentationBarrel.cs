using Engine;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentFermentationBarrel : ComponentInventoryBase, IUpdateable
{
	public enum FermentationState
	{
		Inactive,
		Active,
		Ready
	}

	public enum DrinkType
	{
		None,
		Beer,
		Kvass,
		PumpkinTincture,
		BlackBrew
	}

	private SubsystemTerrain m_subsystemTerrain;

	private SubsystemExplosions m_subsystemExplosions;

	private ComponentBlockEntity m_componentBlockEntity;

	private int m_waterLayers;

	private float m_progressSeconds;

	private bool m_ingredientsChanged;

	private bool isRun;

	public int WaterLayers => m_waterLayers;

	public float Progress01 => m_progressSeconds / FullTimeSeconds;

	public FermentationState State
	{
		get
		{
			if (m_progressSeconds >= FullTimeSeconds)
			{
				return FermentationState.Ready;
			}
			Point3 coordinates = m_componentBlockEntity.Coordinates;
			int cellValue = m_subsystemTerrain.Terrain.GetCellValue(coordinates.X, coordinates.Y, coordinates.Z);
			if (Terrain.ExtractContents(cellValue) == 428 && FermentationBarrelBlock.GetActive(Terrain.ExtractData(cellValue)))
			{
				return FermentationState.Active;
			}
			return FermentationState.Inactive;
		}
	}

	public DrinkType ResultDrink { get; private set; }

	public bool SafeStopped { get; private set; }

	public int MugSlotIndex => 4;

	public int WaterIntakeSlotIndex => 5;

	public float FullTimeSeconds => 300f;

	public float ProgressRollbackOnBucketSeconds => 1f / 3f * FullTimeSeconds;

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override int GetSlotCapacity(int slotIndex, int value)
	{
		if (slotIndex >= 0 && slotIndex <= 3)
		{
			if (GetAllowedIngredientValue(value))
			{
				return base.GetSlotCapacity(slotIndex, value);
			}
			return 0;
		}
		if (slotIndex == MugSlotIndex)
		{
			if (Terrain.ExtractContents(value) != 429)
			{
				return 0;
			}
			return base.GetSlotCapacity(slotIndex, value);
		}
		if (slotIndex != WaterIntakeSlotIndex)
		{
			return base.GetSlotCapacity(slotIndex, value);
		}
		switch (Terrain.ExtractContents(value))
		{
		case 91:
			if (State == FermentationState.Ready && m_waterLayers >= 1)
			{
				return 0;
			}
			return 1;
		case 90:
			return 1;
		default:
			return 0;
		}
	}

	public override void AddSlotItems(int slotIndex, int value, int count)
	{
		if (slotIndex == MugSlotIndex && Terrain.ExtractContents(value) == 429 && count > 0)
		{
			Slot slot = m_slots[slotIndex];
			int value2 = value;
			if (State == FermentationState.Ready && ResultDrink != DrinkType.None && m_waterLayers >= 1)
			{
				MugBlock.DrinkType drink = MugBlock.GetDrink(value);
				MugBlock.DrinkType drinkType = ToMugDrink(ResultDrink);
				if (drink != drinkType)
				{
					value2 = MugBlock.MakeValue(drinkType);
					AddWaterLayers(-1);
				}
			}
			slot.Value = value2;
			slot.Count = 1;
			OnSlotChange(slotIndex);
			return;
		}
		if (slotIndex == WaterIntakeSlotIndex && Terrain.ExtractContents(value) == 91)
		{
			if (State == FermentationState.Ready && m_waterLayers >= 1)
			{
				return;
			}
			if (count > 0 && m_waterLayers < 3)
			{
				AddWaterLayers(1);
				if (State == FermentationState.Active)
				{
					m_progressSeconds = MathUtils.Max(0f, m_progressSeconds - ProgressRollbackOnBucketSeconds);
				}
				OnSlotChange(slotIndex);
				Slot slot2 = m_slots[slotIndex];
				slot2.Value = Terrain.MakeBlockValue(90, 0, 0);
				slot2.Count = 1;
				OnSlotChange(slotIndex);
				return;
			}
		}
		if (State == FermentationState.Active && slotIndex >= 0 && slotIndex <= 3 && count > 0)
		{
			m_ingredientsChanged = true;
		}
		base.AddSlotItems(slotIndex, value, count);
	}

	public override int RemoveSlotItems(int slotIndex, int count)
	{
		if (State == FermentationState.Active && slotIndex >= 0 && slotIndex <= 3 && count > 0)
		{
			m_ingredientsChanged = true;
		}
		return base.RemoveSlotItems(slotIndex, count);
	}

	public override void SetSlotValue(int slotIndex, object obj)
	{
		int num = 0;
		if (slotIndex >= 0 && slotIndex < m_slots.Count)
		{
			num = m_slots[slotIndex].Value;
		}
		base.SetSlotValue(slotIndex, obj);
		if (slotIndex < 0 || slotIndex > 3)
		{
			return;
		}
		int value = m_slots[slotIndex].Value;
		if (State == FermentationState.Active)
		{
			if (IsOnlyDamageChangeSameItem(num, value))
			{
				m_slots[slotIndex].Value = num;
				OnSlotChange(slotIndex);
			}
			else
			{
				m_ingredientsChanged = true;
			}
		}
	}

	public void Update(float dt)
	{
		if (CommonLib.WorkType == WorkType.Client)
		{
			return;
		}
		m_waterLayers = MathUtils.Clamp(m_waterLayers, 0, 3);
		m_progressSeconds = MathUtils.Clamp(m_progressSeconds, 0f, FullTimeSeconds);
		if (m_waterLayers == 0 && m_progressSeconds >= FullTimeSeconds)
		{
			m_progressSeconds = 0f;
			ResultDrink = DrinkType.None;
			SetActive(active: false);
		}
		if (State == FermentationState.Inactive && m_waterLayers > 0 && MatchesAnyRecipe(out var _))
		{
			SetActive(active: true);
		}
		if (State == FermentationState.Active)
		{
			if (m_ingredientsChanged && !SafeStopped)
			{
				m_ingredientsChanged = false;
				TriggerAccident();
				return;
			}
			m_progressSeconds = MathUtils.Min(m_progressSeconds + dt, FullTimeSeconds);
			if (m_progressSeconds >= FullTimeSeconds)
			{
				FixDrinkTypeByIngredients();
				for (int node = 0; node < 4; node++)
				{
					m_slots[node].Value = 0;
					m_slots[node].Count = 0;
					OnSlotChange(node);
				}
				SetActive(active: false);
			}
		}
		if (State == FermentationState.Active && CommonLib.WorkType != WorkType.Client)
		{
			if (!isRun)
			{
				isRun = true;
				CommonLib.Net.QueuePackage(new ComponentFermentationBarrelPackage(Entity.EntityId, m_progressSeconds, m_waterLayers, (byte)ResultDrink));
			}
			if (Time.PeriodicEvent(0.1, 0.0))
			{
				CommonLib.Net.QueuePackage(new ComponentFermentationBarrelPackage(Entity.EntityId, m_progressSeconds, m_waterLayers, (byte)ResultDrink));
			}
		}
		else if (isRun && CommonLib.WorkType != WorkType.Client)
		{
			isRun = false;
			CommonLib.Net.QueuePackage(new ComponentFermentationBarrelPackage(Entity.EntityId, m_progressSeconds, m_waterLayers, (byte)ResultDrink));
		}
	}

	public void SafeStop()
	{
		if (IsAddedToProject && !SafeStopped)
		{
			SafeStopped = true;
			Point3 coordinates = m_componentBlockEntity.Coordinates;
			int value = Terrain.MakeBlockValue(18, 0, 0);
			if (Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(coordinates.X, coordinates.Y, coordinates.Z)) == 428)
			{
				m_subsystemTerrain.ChangeCell(coordinates.X, coordinates.Y, coordinates.Z, value);
			}
		}
	}

	public override void Load(ValuesDictionary vd, IdToEntityMap idToEntityMap)
	{
		base.Load(vd, idToEntityMap);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemExplosions = Project.FindSubsystem<SubsystemExplosions>(throwOnError: true);
		m_componentBlockEntity = Entity.FindComponent<ComponentBlockEntity>(throwOnError: true);
		m_waterLayers = MathUtils.Clamp(vd.GetValue("WaterLayers", 0), 0, 3);
		m_progressSeconds = MathUtils.Clamp(vd.GetValue("ProgressSeconds", 0f), 0f, FullTimeSeconds);
		vd.GetValue("State", 0);
		ResultDrink = (DrinkType)vd.GetValue("DrinkType", 0);
		SafeStopped = vd.GetValue("SafeStopped", defaultValue: false);
	}

	public override void Save(ValuesDictionary vd, EntityToIdMap idToEntityMap)
	{
		base.Save(vd, idToEntityMap);
		vd.SetValue("WaterLayers", m_waterLayers);
		vd.SetValue("ProgressSeconds", m_progressSeconds);
		vd.SetValue("State", (int)State);
		vd.SetValue("DrinkType", (int)ResultDrink);
		vd.SetValue("SafeStopped", SafeStopped);
	}

	private bool HasAnyIngredient()
	{
		for (int i = 0; i < 4; i++)
		{
			if (m_slots[i].Count > 0 && GetAllowedIngredient(Terrain.ExtractContents(m_slots[i].Value)))
			{
				return true;
			}
		}
		return false;
	}

	private bool GetAllowedIngredient(int contents)
	{
		if (contents != 1 && contents != 2)
		{
			return contents == 3;
		}
		return true;
	}

	private void AddWaterLayers(int delta)
	{
		int waterLayers = m_waterLayers;
		m_waterLayers = MathUtils.Clamp(m_waterLayers + delta, 0, 3);
		if (waterLayers == m_waterLayers)
		{
			return;
		}
		NotifyWaterChanged();
		if (m_waterLayers == 0)
		{
			if (m_progressSeconds >= FullTimeSeconds)
			{
				m_progressSeconds = 0f;
			}
			ResultDrink = DrinkType.None;
			SetActive(active: false);
		}
	}

	private void NotifyWaterChanged()
	{
	}

	private void FixDrinkTypeByIngredients()
	{
		if (MatchesAnyRecipe(out var drink))
		{
			ResultDrink = drink;
		}
		else
		{
			ResultDrink = DrinkType.None;
		}
	}

	private void TriggerAccident()
	{
		Point3 coordinates = m_componentBlockEntity.Coordinates;
		if (Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(coordinates.X, coordinates.Y, coordinates.Z)) == 428)
		{
			m_subsystemTerrain.ChangeCell(coordinates.X, coordinates.Y, coordinates.Z, 0);
			m_subsystemExplosions.AddExplosion(coordinates.X, coordinates.Y, coordinates.Z, 60f, isIncendiary: false, noExplosionSound: false, m_componentBlockEntity.OwnPlayerData, disableItemSpawn: false);
		}
	}

	private bool IsActive()
	{
		Point3 coordinates = m_componentBlockEntity.Coordinates;
		int cellValue = m_subsystemTerrain.Terrain.GetCellValue(coordinates.X, coordinates.Y, coordinates.Z);
		if (Terrain.ExtractContents(cellValue) == 428)
		{
			return FermentationBarrelBlock.GetActive(Terrain.ExtractData(cellValue));
		}
		return false;
	}

	private void SetActive(bool active)
	{
		Point3 coordinates = m_componentBlockEntity.Coordinates;
		int cellValue = m_subsystemTerrain.Terrain.GetCellValue(coordinates.X, coordinates.Y, coordinates.Z);
		if (Terrain.ExtractContents(cellValue) == 428)
		{
			int num = Terrain.ExtractData(cellValue);
			int num2 = FermentationBarrelBlock.SetActive(num, active);
			if (num2 != num)
			{
				m_subsystemTerrain.ChangeCell(coordinates.X, coordinates.Y, coordinates.Z, Terrain.MakeBlockValue(428, 0, num2));
			}
		}
	}

	private bool IsFlowerKvassRecipe()
	{
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		for (int i = 0; i < 3; i++)
		{
			if (m_slots[i].Count <= 0)
			{
				return false;
			}
			switch (Terrain.ExtractContents(m_slots[i].Value))
			{
			case 24:
				flag = true;
				break;
			case 25:
				flag2 = true;
				break;
			default:
				return false;
			case 20:
				flag3 = true;
				break;
			}
		}
		if ((flag & flag2 & flag3) && Terrain.ExtractContents(m_slots[3].Value) == 329)
		{
			return m_slots[3].Count >= 12;
		}
		return false;
	}

	private bool IsFlowerPumpkinTincture()
	{
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		for (int i = 0; i < 3; i++)
		{
			if (m_slots[i].Count <= 0)
			{
				return false;
			}
			switch (Terrain.ExtractContents(m_slots[i].Value))
			{
			case 24:
				flag = true;
				break;
			case 25:
				flag2 = true;
				break;
			default:
				return false;
			case 20:
				flag3 = true;
				break;
			}
		}
		if ((flag & flag2 & flag3) && m_slots[3].Count >= 10)
		{
			return Terrain.ExtractContents(m_slots[3].Value) == 131;
		}
		return false;
	}

	private bool IsRyeBeerRecipe()
	{
		for (int i = 0; i < 3; i++)
		{
			if (m_slots[i].Count < 100)
			{
				return false;
			}
			int value = m_slots[i].Value;
			if (Terrain.ExtractContents(value) != 173 || Terrain.ExtractData(value) != 5)
			{
				return false;
			}
		}
		int num = Terrain.ExtractContents(m_slots[3].Value);
		if (m_slots[3].Count > 0)
		{
			if (num != 343 && num != 342 && num != 338 && num != 339 && num != 340 && num != 341 && num != 242 && num != 247 && num != 313 && num != 315 && num != 316 && num != 176 && num != 337 && num != 335 && num != 334)
			{
				return num == 314;
			}
			return true;
		}
		return false;
	}

	private bool GetAllowedIngredientValue(int value)
	{
		int text = Terrain.ExtractContents(value);
		int cultureInfo = Terrain.ExtractData(value);
		if (text != 173 || cultureInfo != 5)
		{
			switch (text)
			{
			case 20:
			case 24:
			case 25:
			case 131:
			case 176:
			case 242:
			case 247:
			case 313:
			case 314:
			case 315:
			case 316:
			case 329:
			case 334:
			case 335:
			case 337:
			case 338:
			case 339:
			case 340:
			case 341:
			case 342:
			case 343:
				break;
			case 311:
			case 362:
			case 394:
				return true;
			case 203:
			{
				int clothingIndex = ClothingBlock.GetClothingIndex(cultureInfo);
				int clothingColor = ClothingBlock.GetClothingColor(cultureInfo);
				if (clothingIndex == 16)
				{
					return clothingColor == 15;
				}
				return false;
			}
			default:
				return false;
			}
		}
		return true;
	}

	private bool MatchesAnyRecipe(out DrinkType drink)
	{
		if (IsBlackBrewRecipe())
		{
			drink = DrinkType.BlackBrew;
			return true;
		}
		if (IsRyeBeerRecipe())
		{
			drink = DrinkType.Beer;
			return true;
		}
		if (IsFlowerPumpkinTincture())
		{
			drink = DrinkType.PumpkinTincture;
			return true;
		}
		if (IsFlowerKvassRecipe())
		{
			drink = DrinkType.Kvass;
			return true;
		}
		drink = DrinkType.None;
		return false;
	}

	private static MugBlock.DrinkType ToMugDrink(DrinkType d)
	{
		return d switch
		{
			DrinkType.Beer => MugBlock.DrinkType.Beer, 
			DrinkType.Kvass => MugBlock.DrinkType.Kvass, 
			DrinkType.PumpkinTincture => MugBlock.DrinkType.PumpkinTincture, 
			DrinkType.BlackBrew => MugBlock.DrinkType.BlackBrew, 
			_ => MugBlock.DrinkType.None, 
		};
	}

	private static bool IsOnlyDamageChangeSameItem(int oldValue, int newValue)
	{
		if (oldValue == newValue)
		{
			return false;
		}
		int num = Terrain.ExtractContents(oldValue);
		int num2 = Terrain.ExtractContents(newValue);
		if (num != num2)
		{
			return false;
		}
		Block obj = BlocksManager.Blocks[num];
		int num3 = obj.SetDamage(oldValue, 0);
		int num4 = obj.SetDamage(newValue, 0);
		return num3 == num4;
	}

	public void NetSync(float progressSeconds, int waterLayers, DrinkType resultDrink)
	{
		m_progressSeconds = MathUtils.Clamp(progressSeconds, 0f, FullTimeSeconds);
		m_waterLayers = MathUtils.Clamp(waterLayers, 0, 3);
		ResultDrink = resultDrink;
	}

	private bool IsBlackBrewRecipe()
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		bool flag = false;
		for (int i = 0; i < 4; i++)
		{
			int count = m_slots[i].Count;
			if (count <= 0)
			{
				continue;
			}
			int value = m_slots[i].Value;
			switch (Terrain.ExtractContents(value))
			{
			case 394:
				num += count;
				break;
			case 362:
				num2 += count;
				break;
			case 311:
				num3 += count;
				break;
			default:
				return false;
			case 203:
			{
				int data = Terrain.ExtractData(value);
				int clothingIndex = ClothingBlock.GetClothingIndex(data);
				int clothingColor = ClothingBlock.GetClothingColor(data);
				if (clothingIndex != 16 || clothingColor != 15 || count < 1)
				{
					return false;
				}
				flag = true;
				break;
			}
			}
		}
		return (num >= 100 && num2 >= 10 && num3 >= 100) & flag;
	}
}
