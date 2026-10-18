using System.Collections.Generic;
using Engine;
using Engine.Graphics;
using Game.NetWork;
using Game.NetWork.Packages;
using TemplatesDatabase;

namespace Game;

public class SubsystemElectricBlockBehavior : SubsystemBlockBehavior, IUpdateable, IDrawable
{
	public class ElectricArc
	{
		public Vector3 Source;

		public ComponentBody Target;

		public float Lifetime;

		public float VoltageFactor;
	}

	public SubsystemElectricity m_subsystemElectricity;

	public SubsystemBlocksScanner m_subsystemBlocksScanner;

	public SubsystemBodies m_subsystemBodies;

	public SubsystemTime m_subsystemTime;

	public SubsystemAmbientSounds m_subsystemAmbientSounds;

	public SubsystemAudio m_subsystemAudio;

	public HashSet<Point3> m_lightbulbs;

	public Random m_random;

	public Dictionary<ComponentCreature, double> m_creatureShockTimers;

	public float m_electricHumVolume;

	public List<ElectricArc> m_activeArcs = new List<ElectricArc>();

	public PrimitivesRenderer3D m_primitivesRenderer3d = new PrimitivesRenderer3D();

	public override int[] HandledBlocks => new int[63]
	{
		133, 140, 137, 143, 156, 134, 371, 135, 145, 224,
		146, 157, 180, 181, 183, 138, 139, 141, 142, 184,
		187, 186, 188, 144, 151, 179, 152, 254, 253, 182,
		185, 56, 57, 58, 83, 84, 166, 194, 86, 63,
		97, 98, 210, 211, 105, 106, 107, 234, 235, 236,
		147, 153, 154, 223, 155, 243, 120, 121, 199, 216,
		227, 237, 366
	};

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public int[] DrawOrders => new int[1] { 110 };

	public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded)
	{
		m_subsystemElectricity.OnElectricElementBlockGenerated(x, y, z);
		int num = Terrain.ExtractContents(value);
		if (num == 139 || num == 371)
		{
			m_lightbulbs.Add(new Point3(x, y, z));
		}
	}

	public override void OnBlockAdded(int value, int oldValue, int x, int y, int z)
	{
		base.OnBlockAdded(value, oldValue, x, y, z);
		m_subsystemElectricity.OnElectricElementBlockAdded(x, y, z);
		int model = Terrain.ExtractContents(value);
		if (model == 139 || model == 371)
		{
			m_lightbulbs.Add(new Point3(x, y, z));
		}
	}

	public override void OnBlockRemoved(int value, int newValue, int x, int y, int z)
	{
		base.OnBlockRemoved(value, newValue, x, y, z);
		m_subsystemElectricity.OnElectricElementBlockRemoved(x, y, z);
		int num = Terrain.ExtractContents(value);
		if (num == 139 || num == 371)
		{
			m_lightbulbs.Remove(new Point3(x, y, z));
		}
	}

	public override void OnBlockModified(int value, int oldValue, int x, int y, int z)
	{
		m_subsystemElectricity.OnElectricElementBlockModified(x, y, z);
	}

	public override void OnChunkDiscarding(TerrainChunk chunk)
	{
		m_subsystemElectricity.OnChunkDiscarding(chunk);
	}

	public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
	{
		for (int num = 0; num < 6; num++)
		{
			m_subsystemElectricity.GetElectricElement(x, y, z, num)?.OnNeighborBlockChanged(new CellFace(x, y, z, num), neighborX, neighborY, neighborZ);
		}
	}

	public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner)
	{
		int data = raycastResult.CellFace.X;
		int data2 = raycastResult.CellFace.Y;
		int z = raycastResult.CellFace.Z;
		for (int i = 0; i < 6; i++)
		{
			ElectricElement electricElement = m_subsystemElectricity.GetElectricElement(data, data2, z, i);
			if (electricElement != null)
			{
				return electricElement.OnInteract(raycastResult, componentMiner);
			}
		}
		return false;
	}

	public override void OnCollide(CellFace cellFace, float velocity, ComponentBody componentBody)
	{
		int num = cellFace.X;
		int y = cellFace.Y;
		int z = cellFace.Z;
		for (int i = 0; i < 6; i++)
		{
			ElectricElement electricElement = m_subsystemElectricity.GetElectricElement(num, y, z, i);
			if (electricElement != null)
			{
				electricElement.OnCollide(cellFace, velocity, componentBody);
				break;
			}
		}
	}

	public override void OnHitByProjectile(CellFace cellFace, WorldItem worldItem)
	{
		int num = cellFace.X;
		int y = cellFace.Y;
		int z = cellFace.Z;
		for (int i = 0; i < 6; i++)
		{
			ElectricElement electricElement = m_subsystemElectricity.GetElectricElement(num, y, z, i);
			if (electricElement != null)
			{
				electricElement.OnHitByProjectile(cellFace, worldItem);
				break;
			}
		}
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemElectricity = Project.FindSubsystem<SubsystemElectricity>(throwOnError: true);
		m_subsystemBodies = Project.FindSubsystem<SubsystemBodies>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemAmbientSounds = Project.FindSubsystem<SubsystemAmbientSounds>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemBlocksScanner = Project.FindSubsystem<SubsystemBlocksScanner>(throwOnError: true);
		m_subsystemBlocksScanner.ScanningChunkCompleted += OnScanningChunkCompleted;
	}

	public SubsystemElectricBlockBehavior()
	{
		m_creatureShockTimers = new Dictionary<ComponentCreature, double>();
		m_lightbulbs = new HashSet<Point3>();
		m_random = new Random();
	}

	public void Update(float dt)
	{
		double gameTime = m_subsystemTime.GameTime;
		if (m_subsystemTime.PeriodicGameTimeEvent(0.5, 0.0))
		{
			float num = 0f;
			List<Point3> list = new List<Point3>();
			foreach (Point3 lightbulb in m_lightbulbs)
			{
				if (SubsystemTerrain.Terrain.GetChunkAtCell(lightbulb.X, lightbulb.Z) == null)
				{
					continue;
				}
				int cellValue = SubsystemTerrain.Terrain.GetCellValue(lightbulb.X, lightbulb.Y, lightbulb.Z);
				int num2 = Terrain.ExtractContents(cellValue);
				if (num2 != 139 && num2 != 371)
				{
					list.Add(lightbulb);
				}
				else
				{
					if (LightbulbBlock.GetLightIntensity(Terrain.ExtractData(cellValue)) <= 0)
					{
						continue;
					}
					float num3 = m_subsystemAmbientSounds.SubsystemAudio.CalculateListenerDistanceSquared(new Vector3(lightbulb.X, lightbulb.Y, lightbulb.Z));
					if (num3 < 100f)
					{
						float num4 = 1f - MathUtils.Sqrt(num3) / 10f;
						if (num4 > num)
						{
							num = num4;
						}
					}
				}
			}
			foreach (Point3 item in list)
			{
				m_lightbulbs.Remove(item);
			}
			m_electricHumVolume = num;
		}
		if (m_subsystemAmbientSounds != null)
		{
			m_subsystemAmbientSounds.ElectricHumSoundVolume = m_electricHumVolume;
		}
		if (m_subsystemTime.PeriodicGameTimeEvent(1.0, 0.0))
		{
			ProcessElectricDamage(gameTime);
		}
	}

	private void OnScanningChunkCompleted(TerrainChunk chunk)
	{
		for (int node = 0; node < 16; node++)
		{
			for (int num = 0; num < 16; num++)
			{
				int i = chunk.GetTopHeightFast(node, num);
				for (int j = 0; j <= i; j++)
				{
					int widget = Terrain.ExtractContents(chunk.GetCellValueFast(node, j, num));
					if (widget == 139 || widget == 371)
					{
						int x = chunk.Origin.X + node;
						int z = chunk.Origin.Y + num;
						m_lightbulbs.Add(new Point3(x, j, z));
					}
				}
			}
		}
	}

	private void ProcessElectricDamage(double currentTime)
	{
		foreach (ComponentBody value in m_subsystemBodies.Bodies)
		{
			ComponentCreature componentCreature = value.Entity.FindComponent<ComponentCreature>();
			if (componentCreature == null || !(componentCreature.ComponentHealth.Health > 0f) || !(value.ImmersionDepth > 0.01f))
			{
				continue;
			}
			Point3 point = Terrain.ToCell(value.Position);
			float num = 0f;
			Vector3 source = Vector3.Zero;
			for (int i = point.X - 1; i <= point.X + 1; i++)
			{
				for (int j = point.Y - 1; j <= point.Y + 1; j++)
				{
					for (int k = point.Z - 1; k <= point.Z + 1; k++)
					{
						for (int l = 0; l < 6; l++)
						{
							ElectricElement electricElement = m_subsystemElectricity.GetElectricElement(i, j, k, l);
							if (electricElement != null)
							{
								float outputVoltage = electricElement.GetOutputVoltage(l);
								if (outputVoltage > num)
								{
									num = outputVoltage;
									source = new Vector3((float)i + 0.5f, (float)j + 0.5f, (float)k + 0.5f);
								}
							}
						}
					}
				}
			}
			if (!(num >= 0.7f))
			{
				continue;
			}
			float num2 = MathUtils.Clamp((num - 0.7f) / 0.8f, 0f, 1f);
			m_activeArcs.Add(new ElectricArc
			{
				Source = source,
				Target = value,
				Lifetime = 0.45f,
				VoltageFactor = num2
			});
			float amount = MathUtils.Lerp(0.1f, 0.7f, num2);
			float num3 = MathUtils.Lerp(0.8f, 1.2f, num2);
			if (!m_creatureShockTimers.TryGetValue(componentCreature, out var value2))
			{
				value2 = 0.0;
			}
			if (currentTime >= value2)
			{
				string text = LanguageControl.Get("ComponentHealth", "killed_by_electricity");
				if (CommonLib.WorkType != WorkType.Client)
				{
					componentCreature.ComponentHealth.Injure(amount, null, ignoreInvulnerability: false, text);
				}
				else
				{
					CommonLib.Net.QueuePackage(new ComponentHealthPackage(componentCreature.ComponentHealth, null, amount, text, ignoreInvulnerability: false, isRequest: true));
				}
				m_subsystemAudio.PlayRandomSound("Audio/Footsteps/IsExplodes", 0.6f, m_random.Float(-0.3f, 0.3f), value.Position, 4f, autoDelay: true);
				m_creatureShockTimers[componentCreature] = currentTime + (double)num3;
			}
		}
		if (!m_subsystemTime.PeriodicGameTimeEvent(10.0, 0.0))
		{
			return;
		}
		List<ComponentCreature> list = new List<ComponentCreature>();
		foreach (KeyValuePair<ComponentCreature, double> creatureShockTimer in m_creatureShockTimers)
		{
			if (creatureShockTimer.Key.ComponentHealth.Health <= 0f || creatureShockTimer.Key.Entity.Project == null)
			{
				list.Add(creatureShockTimer.Key);
			}
		}
		foreach (ComponentCreature item in list)
		{
			m_creatureShockTimers.Remove(item);
		}
	}

	public void Draw(Camera camera, int drawOrder)
	{
		if (m_activeArcs.Count == 0)
		{
			return;
		}
		FlatBatch3D fb = m_primitivesRenderer3d.FlatBatch(0, DepthStencilState.DepthRead, RasterizerState.CullNone, BlendState.Additive);
		float gameTimeDelta = m_subsystemTime.GameTimeDelta;
		for (int i = 0; i < m_activeArcs.Count; i++)
		{
			ElectricArc electricArc = m_activeArcs[i];
			electricArc.Lifetime -= gameTimeDelta;
			if (electricArc.Lifetime > 0f && electricArc.Target.Entity.Project != null)
			{
				int seed = i + (int)(electricArc.Lifetime / 0.15f);
				float thickness = 0.08f * MathUtils.Lerp(0.3f, 1f, electricArc.VoltageFactor);
				float jaggedness = 0.3f * MathUtils.Lerp(0.2f, 1f, electricArc.VoltageFactor);
				LightningRenderer.Config.ArcIntensity = 0.5f * electricArc.VoltageFactor;
				Color color = new Color(150, 200, 255) * (electricArc.Lifetime / 0.45f);
				LightningRenderer.DrawLightning(fb, electricArc.Source, electricArc.Target.Position + new Vector3(0f, electricArc.Target.BoxSize.Y * 0.5f, 0f), camera, color, thickness, jaggedness, 8, seed);
			}
			else
			{
				m_activeArcs.RemoveAt(i--);
			}
		}
		m_primitivesRenderer3d.Flush(camera.ViewProjectionMatrix);
	}
}
