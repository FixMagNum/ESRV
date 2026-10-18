using System.Collections.Generic;
using System.Linq;
using Engine;
using Game.NetWork;
using TemplatesDatabase;

namespace Game;

public class SubsystemBowBlockBehavior : SubsystemBlockBehavior
{
	public SubsystemTime m_subsystemTime;

	public SubsystemProjectiles m_subsystemProjectiles;

	public SubsystemAudio m_subsystemAudio;

	public static string fName = "SubsystemBowBlockBehavior";

	public Random m_random = new Random();

	public Dictionary<ComponentMiner, double> m_aimStartTimes = new Dictionary<ComponentMiner, double>();

	public ArrowBlock.ArrowType[] m_supportedArrowTypes = new ArrowBlock.ArrowType[7]
	{
		ArrowBlock.ArrowType.WoodenArrow,
		ArrowBlock.ArrowType.StoneArrow,
		ArrowBlock.ArrowType.CopperArrow,
		ArrowBlock.ArrowType.IronArrow,
		ArrowBlock.ArrowType.DiamondArrow,
		ArrowBlock.ArrowType.FireArrow,
		ArrowBlock.ArrowType.TorchArrow
	};

	public override int[] HandledBlocks => new int[0];

	public override bool OnEditInventoryItem(IInventory inventory, int slotIndex, ComponentPlayer componentPlayer)
	{
		componentPlayer.ComponentGui.ModalPanelWidget = ((componentPlayer.ComponentGui.ModalPanelWidget == null) ? new BowWidget(inventory, slotIndex) : null);
		return true;
	}

	public override bool OnAim(Ray3 aim, ComponentMiner componentMiner, AimState state)
	{
		IInventory gameTimeDelta = componentMiner.Inventory;
		if (gameTimeDelta != null)
		{
			int flag = gameTimeDelta.ActiveSlotIndex;
			if (flag >= 0)
			{
				int insulation = gameTimeDelta.GetSlotValue(flag);
				int arg = gameTimeDelta.GetSlotCount(flag);
				int flag2 = Terrain.ExtractContents(insulation);
				int flag3 = Terrain.ExtractData(insulation);
				if (flag2 == 191 && arg > 0)
				{
					if (!m_aimStartTimes.TryGetValue(componentMiner, out var flag4))
					{
						flag4 = m_subsystemTime.GameTime;
						m_aimStartTimes[componentMiner] = flag4;
					}
					float num7 = (float)(m_subsystemTime.GameTime - flag4);
					float num8 = (float)MathUtils.Remainder(m_subsystemTime.GameTime, 1000.0);
					Vector3 num9 = ((componentMiner.ComponentCreature.ComponentBody.IsSneaking ? 0.02f : 0.04f) + 0.25f * MathUtils.Saturate((num7 - 2.1f) / 5f)) * new Vector3
					{
						X = SimplexNoise.OctavedNoise(num8, 2f, 3, 2f, 0.5f),
						Y = SimplexNoise.OctavedNoise(num8 + 100f, 2f, 3, 2f, 0.5f),
						Z = SimplexNoise.OctavedNoise(num8 + 200f, 2f, 3, 2f, 0.5f)
					};
					aim.Direction = Vector3.Normalize(aim.Direction + num9);
					switch (state)
					{
					case AimState.InProgress:
					{
						if (num7 >= 9f)
						{
							componentMiner.ComponentCreature.ComponentCreatureSounds.PlayMoanSound();
							return true;
						}
						ComponentFirstPersonModel num14 = componentMiner.Entity.FindComponent<ComponentFirstPersonModel>();
						if (num14 != null)
						{
							componentMiner.ComponentPlayer?.ComponentAimingSights.ShowAimingSights(aim.Position, aim.Direction);
							num14.ItemOffsetOrder = new Vector3(-0.1f, 0.15f, 0f);
							num14.ItemRotationOrder = new Vector3(0f, -0.7f, 0f);
						}
						componentMiner.ComponentCreature.ComponentCreatureModel.AimHandAngleOrder = 1.2f;
						componentMiner.ComponentCreature.ComponentCreatureModel.InHandItemOffsetOrder = new Vector3(0f, 0f, 0f);
						componentMiner.ComponentCreature.ComponentCreatureModel.InHandItemRotationOrder = new Vector3(0f, -0.2f, 0f);
						if (m_subsystemTime.PeriodicGameTimeEvent(0.1, 0.0) && CommonLib.WorkType != WorkType.Client)
						{
							int draw2 = MathUtils.Min(BowBlock.GetDraw(flag3) + 1, 15);
							gameTimeDelta.RemoveSlotItems(flag, 1);
							gameTimeDelta.AddSlotItems(flag, Terrain.MakeBlockValue(flag2, 0, BowBlock.SetDraw(flag3, draw2)), 1);
						}
						break;
					}
					case AimState.Cancelled:
						if (CommonLib.WorkType != WorkType.Client)
						{
							gameTimeDelta.RemoveSlotItems(flag, 1);
							gameTimeDelta.AddSlotItems(flag, Terrain.MakeBlockValue(flag2, 0, BowBlock.SetDraw(flag3, 0)), 1);
						}
						m_aimStartTimes.Remove(componentMiner);
						break;
					case AimState.Completed:
					{
						int draw = BowBlock.GetDraw(flag3);
						ArrowBlock.ArrowType? x = BowBlock.GetArrowType(flag3);
						if (x.HasValue)
						{
							Vector3 num10 = componentMiner.ComponentCreature.ComponentCreatureModel.EyePosition + componentMiner.ComponentCreature.ComponentBody.Matrix.Right * 0.3f - componentMiner.ComponentCreature.ComponentBody.Matrix.Up * 0.2f;
							Vector3 z = Vector3.Normalize(num10 + aim.Direction * 10f - num10);
							float num11 = MathUtils.Lerp(0f, 28f, MathUtils.Pow((float)draw / 15f, 0.75f));
							if (componentMiner.ComponentPlayer != null)
							{
								num11 *= 0.5f * (componentMiner.ComponentPlayer.ComponentLevel.StrengthFactor - 1f) + 1f;
							}
							Vector3 block = Vector3.Zero;
							if (x == ArrowBlock.ArrowType.WoodenArrow)
							{
								block = new Vector3(0.025f, 0.025f, 0.025f);
							}
							if (x == ArrowBlock.ArrowType.StoneArrow)
							{
								block = new Vector3(0.01f, 0.01f, 0.01f);
							}
							int num12 = Terrain.MakeBlockValue(192, 0, ArrowBlock.SetArrowType(0, x.Value));
							Vector3 num13 = Vector3.Normalize(Vector3.Cross(z, Vector3.UnitY));
							Vector3 eyePosition = Vector3.Normalize(Vector3.Cross(z, num13));
							Vector3 forwardVector = m_random.Float(0f - block.X, block.X) * num13 + m_random.Float(0f - block.Y, block.Y) * eyePosition + m_random.Float(0f - block.Z, block.Z) * z;
							if (m_subsystemProjectiles.FireProjectile(num12, num10, (z + forwardVector) * num11, Vector3.Zero, componentMiner.ComponentCreature) != null)
							{
								flag3 = BowBlock.SetArrowType(flag3, null);
								m_subsystemAudio.PlaySound("Audio/Bow", 1f, m_random.Float(-0.1f, 0.1f), num10, 3f, autoDelay: true);
							}
						}
						else
						{
							componentMiner.ComponentPlayer?.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 0), Color.White, blinking: true, playNotificationSound: false);
						}
						if (CommonLib.WorkType != WorkType.Client)
						{
							gameTimeDelta.RemoveSlotItems(flag, 1);
							int value = Terrain.MakeBlockValue(flag2, 0, BowBlock.SetDraw(flag3, 0));
							gameTimeDelta.AddSlotItems(flag, value, 1);
							int damageCount = 0;
							if (draw >= 15)
							{
								damageCount = 2;
							}
							else if (draw >= 4)
							{
								damageCount = 1;
							}
							componentMiner.DamageActiveTool(damageCount);
						}
						m_aimStartTimes.Remove(componentMiner);
						return true;
					}
					}
				}
			}
		}
		return false;
	}

	public override int GetProcessInventoryItemCapacity(IInventory inventory, int slotIndex, int value)
	{
		int num = Terrain.ExtractContents(value);
		ArrowBlock.ArrowType gameTimeDelta = ArrowBlock.GetArrowType(Terrain.ExtractData(value));
		if (num != 192 || !m_supportedArrowTypes.Contains(gameTimeDelta))
		{
			return 0;
		}
		if (!BowBlock.GetArrowType(Terrain.ExtractData(inventory.GetSlotValue(slotIndex))).HasValue)
		{
			return 1;
		}
		return 0;
	}

	public override void ProcessInventoryItem(IInventory inventory, int slotIndex, int value, int count, int processCount, out int processedValue, out int processedCount)
	{
		if (processCount == 1)
		{
			ArrowBlock.ArrowType num = ArrowBlock.GetArrowType(Terrain.ExtractData(value));
			int data = Terrain.ExtractData(inventory.GetSlotValue(slotIndex));
			processedValue = 0;
			processedCount = 0;
			inventory.RemoveSlotItems(slotIndex, 1);
			inventory.AddSlotItems(slotIndex, Terrain.MakeBlockValue(191, 0, BowBlock.SetArrowType(data, num)), 1);
		}
		else
		{
			processedValue = value;
			processedCount = count;
		}
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemProjectiles = Project.FindSubsystem<SubsystemProjectiles>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		base.Load(valuesDictionary);
	}
}
