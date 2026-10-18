using Engine;
using Engine.Graphics;
using Game.NetWork;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentHumanBones : ComponentInventoryBase, IDrawable, IUpdateable
{
	public PrimitivesRenderer3D m_primitivesRenderer = new PrimitivesRenderer3D();

	public SubsystemTerrain m_subsystemTerrain;

	public ComponentBlockEntity m_blockEntity;

	public LightSmoother m_lightSmoother = new LightSmoother();

	private Matrix m_skeletonMatrix;

	private bool m_isVisible;

	public int Pose { get; set; }

	public int[] DrawOrders => new int[1] { 10 };

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		base.Load(valuesDictionary, idToEntityMap);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_blockEntity = Entity.FindComponent<ComponentBlockEntity>(throwOnError: true);
		Pose = valuesDictionary.GetValue("Pose", 0);
		Point3 coordinates = m_blockEntity.Coordinates;
		m_skeletonMatrix = CalculateSkeletonRotation(coordinates.X, coordinates.Z, Pose);
	}

	public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
	{
		base.Save(valuesDictionary, entityToIdMap);
		valuesDictionary.SetValue("Pose", Pose);
	}

	public void Update(float dt)
	{
		if (CommonLib.WorkType != WorkType.Server && m_isVisible)
		{
			m_isVisible = false;
			Point3 num = m_blockEntity.Coordinates;
			Vector3 position = new Vector3((float)num.X + 0.5f, (float)num.Y + 0.5f, (float)num.Z + 0.5f);
			m_lightSmoother.Update(dt, position, m_subsystemTerrain, Project);
		}
	}

	public void Draw(Camera camera, int drawOrder)
	{
		Point3 coordinates = m_blockEntity.Coordinates;
		if (m_subsystemTerrain.Terrain.GetChunkAtCell(coordinates.X, coordinates.Z) == null)
		{
			return;
		}
		Vector3 vector = new Vector3((float)coordinates.X + 0.5f, (float)coordinates.Y + 0.5f, (float)coordinates.Z + 0.5f);
		if (Vector3.DistanceSquared(camera.ViewPosition, vector) > 3600f)
		{
			return;
		}
		m_isVisible = true;
		Vector3 v = Vector3.Normalize(vector - camera.ViewPosition);
		if (Vector3.Dot(camera.ViewDirection, v) < 0.3f)
		{
			return;
		}
		DrawBlockEnvironmentData drawBlockEnvironmentData = new DrawBlockEnvironmentData
		{
			SubsystemTerrain = m_subsystemTerrain,
			Light = m_lightSmoother.SmoothedLightInt,
			Humidity = m_subsystemTerrain.Terrain.GetSeasonalHumidity(coordinates.X, coordinates.Z),
			Temperature = m_subsystemTerrain.Terrain.GetSeasonalTemperature(coordinates.X, coordinates.Z)
		};
		Matrix skeletonMatrix = m_skeletonMatrix;
		Vector3 position = new Vector3((float)coordinates.X + 0.5f, (float)coordinates.Y + 0.1f, (float)coordinates.Z + 0.5f);
		for (int i = 0; i < MathUtils.Min(SlotsCount, 6); i++)
		{
			int slotValue = GetSlotValue(i);
			if (slotValue != 0 && GetSlotCount(i) != 0)
			{
				Block obj = BlocksManager.Blocks[Terrain.ExtractContents(slotValue)];
				Vector3 bodyPartOffset = GetBodyPartOffset(i);
				float num = 0.3f;
				if (!(obj is FlatBlock))
				{
					num = 0.5f;
				}
				Matrix matrix = (drawBlockEnvironmentData.InWorldMatrix = Matrix.CreateScale(num) * Matrix.CreateTranslation(bodyPartOffset) * skeletonMatrix * Matrix.CreateTranslation(position));
				obj.DrawBlock(m_primitivesRenderer, slotValue, Color.White, num, ref matrix, drawBlockEnvironmentData);
			}
		}
		m_primitivesRenderer.Flush(camera.ViewProjectionMatrix);
	}

	private Vector3 GetBodyPartOffset(int slotIndex)
	{
		return slotIndex switch
		{
			0 => new Vector3(0f, 0.7f, 0f), 
			1 => new Vector3(0f, 0.45f, 0f), 
			2 => new Vector3(0f, 0.25f, 0f), 
			3 => new Vector3(0f, 0.05f, 0f), 
			4 => new Vector3(-0.25f, 0.45f, 0.1f), 
			5 => new Vector3(0.25f, 0.45f, 0.1f), 
			_ => new Vector3(0f, 0.4f, 0f), 
		};
	}

	private Matrix CalculateSkeletonRotation(int x, int z, int poseIndex)
	{
		float yaw = 0f;
		float num = 0f;
		float num2 = 0f;
		switch (poseIndex)
		{
		case 0:
		{
			switch ((int)MathUtils.Floor(MathUtils.Saturate(SimplexNoise.Noise((float)x * 0.2f + 2100f, (float)z * 0.2f + 2100f)) * 4f))
			{
			case 0:
				num = MathUtils.DegToRad(90f);
				break;
			case 1:
				num = MathUtils.DegToRad(-90f);
				break;
			case 2:
				num2 = MathUtils.DegToRad(90f);
				break;
			case 3:
				num2 = MathUtils.DegToRad(-90f);
				break;
			}
			yaw = MathUtils.Lerp(0f - MathUtils.DegToRad(90f), MathUtils.DegToRad(90f), SimplexNoise.Noise((float)x * 0.2f + 2200f, (float)z * 0.2f + 2200f));
			float num3 = MathUtils.Lerp(0f - MathUtils.DegToRad(15f), MathUtils.DegToRad(15f), SimplexNoise.Noise((float)x * 0.2f + 2300f, (float)z * 0.2f + 2300f));
			num += num3;
			num2 += num3;
			break;
		}
		case 3:
			num = MathUtils.DegToRad(-90f);
			break;
		case 8:
			num = MathUtils.DegToRad(90f);
			break;
		case 4:
			num2 = MathUtils.DegToRad(180f);
			break;
		}
		return Matrix.CreateFromYawPitchRoll(yaw, num, num2);
	}
}
