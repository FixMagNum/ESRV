using Engine;
using TemplatesDatabase;

namespace Game;

public class SubsystemCarpetBlockBehavior : SubsystemPollableBlockBehavior
{
	public SubsystemWeather m_subsystemWeather;

	public Random m_random = new Random();

	public override int[] HandledBlocks => new int[1] { 208 };

	public override void Load(ValuesDictionary valuesDictionary)
	{
		m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
		base.Load(valuesDictionary);
	}

	public override void OnPoll(int value, int x, int y, int z, int pollPass)
	{
		if (m_random.Float(0f, 1f) < 0.25f)
		{
			PrecipitationShaftInfo input = m_subsystemWeather.GetPrecipitationShaftInfo(x, z);
			if (input.Intensity > 0f && y >= input.YLimit - 1)
			{
				SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: true, noParticleSystem: false);
			}
		}
	}

	public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded)
	{
		if (isLoaded)
		{
			int componentMiner = Terrain.ExtractData(value);
			if (CarpetBlock.GetFaceMask(componentMiner) == 0)
			{
				int componentBody = CarpetBlock.SetFaceMask(componentMiner, 16);
				SubsystemTerrain.ChangeCell(x, y, z, Terrain.ReplaceData(value, componentBody), updateModificationCounter: false);
			}
		}
	}

	public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner)
	{
		int x = Terrain.ExtractContents(componentMiner.ActiveBlockValue);
		if (x == 159 || x == 207)
		{
			Point3 y = raycastResult.CellFace.Point;
			int z = SubsystemTerrain.Terrain.GetCellValue(y.X, y.Y, y.Z);
			if (Terrain.ExtractContents(z) == 208)
			{
				int x2 = Terrain.ExtractData(z);
				int material = CarpetBlock.GetMaterial(x2);
				int y2 = ((x == 207) ? 1 : 2);
				if (material != y2)
				{
					int z2 = CarpetBlock.SetMaterial(x2, y2);
					SubsystemTerrain.ChangeCell(y.X, y.Y, y.Z, Terrain.MakeBlockValue(208, 0, z2));
					componentMiner.RemoveActiveTool(1);
					Project.FindSubsystem<SubsystemAudio>(throwOnError: true)?.PlaySound("Audio/BlockPlaced", 1f, 0f, new Vector3(y.X, y.Y, y.Z), 3f, autoDelay: true);
					return true;
				}
			}
		}
		return false;
	}
}
