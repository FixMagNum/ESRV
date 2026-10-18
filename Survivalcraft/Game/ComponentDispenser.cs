using Engine;
using Game.NetWork;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentDispenser : ComponentInventoryBase
{
	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemAudio m_subsystemAudio;

	public SubsystemPickables m_subsystemPickables;

	public SubsystemProjectiles m_subsystemProjectiles;

	public ComponentBlockEntity m_componentBlockEntity;

	public void Dispense()
	{
		Point3 mountingFace = m_componentBlockEntity.Coordinates;
		int data = Terrain.ExtractData(m_subsystemTerrain.Terrain.GetCellValue(mountingFace.X, mountingFace.Y, mountingFace.Z));
		int direction = DispenserBlock.GetDirection(data);
		DispenserBlock.Mode mode = DispenserBlock.GetMode(data);
		int num = 0;
		int slotValue;
		while (true)
		{
			if (num < SlotsCount)
			{
				slotValue = GetSlotValue(num);
				int slotCount = GetSlotCount(num);
				if (slotValue != 0 && slotCount > 0)
				{
					break;
				}
				num++;
				continue;
			}
			return;
		}
		if (CommonLib.WorkType == WorkType.Client)
		{
			int num2 = ((GetSlotCount(num) > 0) ? 1 : 0);
			for (int i = 0; i < num2; i++)
			{
				DispenseItem(mountingFace, direction, slotValue, mode);
			}
		}
		else
		{
			int num3 = RemoveSlotItems(num, 1);
			for (int j = 0; j < num3; j++)
			{
				DispenseItem(mountingFace, direction, slotValue, mode);
			}
		}
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		base.Load(valuesDictionary, idToEntityMap);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(throwOnError: true);
		m_subsystemProjectiles = Project.FindSubsystem<SubsystemProjectiles>(throwOnError: true);
		m_componentBlockEntity = Entity.FindComponent<ComponentBlockEntity>(throwOnError: true);
	}

	public void DispenseItem(Point3 point, int face, int value, DispenserBlock.Mode mode)
	{
		Vector3 color2 = CellFace.FaceToVector3(face);
		Vector3 color3 = new Vector3((float)point.X + 0.5f, (float)point.Y + 0.5f, (float)point.Z + 0.5f) + 0.6f * color2;
		if (mode == DispenserBlock.Mode.Dispense)
		{
			float num = 1.8f;
			m_subsystemPickables.AddPickable(value, 1, color3, num * (color2 + m_random.Vector3(0.2f)), null);
			m_subsystemAudio.PlaySound("Audio/DispenserDispense", 1f, 0f, new Vector3(color3.X, color3.Y, color3.Z), 3f, autoDelay: true);
			return;
		}
		float num2 = m_random.Float(39f, 41f);
		if (m_subsystemProjectiles.FireProjectile(value, color3, num2 * (color2 + m_random.Vector3(0.025f) + new Vector3(0f, 0.05f, 0f)), Vector3.Zero, (m_componentBlockEntity.OwnPlayerData == null) ? null : m_componentBlockEntity.OwnPlayerData.ComponentPlayer) != null)
		{
			m_subsystemAudio.PlaySound("Audio/DispenserShoot", 1f, 0f, new Vector3(color3.X, color3.Y, color3.Z), 4f, autoDelay: true);
		}
		else
		{
			DispenseItem(point, face, value, DispenserBlock.Mode.Dispense);
		}
	}
}
