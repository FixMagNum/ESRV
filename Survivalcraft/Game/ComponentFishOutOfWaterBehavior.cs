using Engine;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentFishOutOfWaterBehavior : ComponentBehavior, IUpdateable
{
	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemTime m_subsystemTime;

	public ComponentCreature m_componentCreature;

	public ComponentFishModel m_componentFishModel;

	public float m_importanceLevel;

	public float m_outOfWaterTime;

	public Random m_random = new Random();

	private bool m_isBend;

	public Vector3? m_targetWaterPos;

	public double m_nextFindWaterTime;

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override float ImportanceLevel => m_importanceLevel;

	public bool IsBend
	{
		get
		{
			return m_isBend;
		}
		set
		{
			if (m_isBend != value)
			{
				m_isBend = value;
				if (CommonLib.WorkType == WorkType.Server)
				{
					CommonLib.Net.QueuePackage(new ComponentBehaviorPackage(this, m_isBend));
				}
			}
		}
	}

	public void Update(float dt)
	{
		if (CommonLib.WorkType != WorkType.Client)
		{
			m_stateMachine.Update();
		}
		else if (IsBend)
		{
			m_componentFishModel.BendOrder = 2f * (2f * MathUtils.Saturate(SimplexNoise.OctavedNoise((float)MathUtils.Remainder(m_subsystemTime.GameTime, 1000.0), 1.2f * m_componentCreature.ComponentLocomotion.TurnSpeed, 1, 1f, 1f)) - 1f);
		}
		else
		{
			m_componentFishModel.BendOrder = null;
		}
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_componentCreature = Entity.FindComponent<ComponentCreature>(throwOnError: true);
		m_componentFishModel = Entity.FindComponent<ComponentFishModel>(throwOnError: true);
		m_stateMachine.AddState("Inactive", null, () =>
		{
			if (IsOutOfWater())
			{
				m_outOfWaterTime += m_subsystemTime.GameTimeDelta;
			}
			else
			{
				m_outOfWaterTime = 0f;
			}
			if (m_outOfWaterTime > 1f)
			{
				m_importanceLevel = 1000f;
			}
			if (IsActive)
			{
				m_stateMachine.TransitionTo("Jump");
			}
		}, null);
		m_stateMachine.AddState("Jump", () =>
		{
			IsBend = true;
			m_targetWaterPos = FindNearestWaterBlock(7);
			m_nextFindWaterTime = m_subsystemTime.GameTime + (double)m_random.Float(1.5f, 2.5f);
		}, () =>
		{
			m_componentFishModel.BendOrder = 2f * (2f * MathUtils.Saturate(SimplexNoise.OctavedNoise((float)MathUtils.Remainder(m_subsystemTime.GameTime, 1000.0), 1.2f * m_componentCreature.ComponentLocomotion.TurnSpeed, 1, 1f, 1f)) - 1f);
			if (!IsActive)
			{
				m_stateMachine.TransitionTo("Inactive");
			}
			if (!IsOutOfWater())
			{
				m_importanceLevel = 0f;
			}
			if (m_targetWaterPos.HasValue)
			{
				int x = Terrain.ToCell(m_targetWaterPos.Value.X);
				int y = Terrain.ToCell(m_targetWaterPos.Value.Y);
				int z = Terrain.ToCell(m_targetWaterPos.Value.Z);
				int num = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(x, y, z));
				if (num != 18 && !(BlocksManager.Blocks[num] is FocalorsWaterBlock))
				{
					m_targetWaterPos = null;
				}
			}
			if (m_subsystemTime.GameTime > m_nextFindWaterTime)
			{
				m_nextFindWaterTime = m_subsystemTime.GameTime + (double)m_random.Float(1.5f, 3f);
				m_targetWaterPos = FindNearestWaterBlock(7);
			}
			if (m_random.Float(0f, 1f) < 2.5f * m_subsystemTime.GameTimeDelta)
			{
				float jumpOrder = (m_targetWaterPos.HasValue ? 0.5f : 1f);
				m_componentCreature.ComponentLocomotion.JumpOrder = jumpOrder;
			}
			else
			{
				m_componentCreature.ComponentLocomotion.JumpOrder = 0f;
			}
			if (m_targetWaterPos.HasValue)
			{
				Vector3 v = m_targetWaterPos.Value - m_componentCreature.ComponentBody.Position;
				v.Y = 0f;
				if (v.LengthSquared() > 0.01f)
				{
					v = Vector3.Normalize(v);
					ComponentBody componentBody = m_componentCreature.ComponentBody;
					Vector3 v2 = new Vector3(componentBody.Matrix.Right.X, 0f, componentBody.Matrix.Right.Z).Normalized();
					Vector3 v3 = new Vector3(componentBody.Matrix.Forward.X, 0f, componentBody.Matrix.Forward.Z).Normalized();
					float x2 = Vector3.Dot(v, v2) * 2f;
					float x3 = Vector3.Dot(v, v3) * 2f;
					m_componentCreature.ComponentLocomotion.WalkOrder = new Vector2(MathUtils.Clamp(x2, -1f, 1f), MathUtils.Clamp(x3, -1f, 1f));
					float x4 = componentBody.Rotation.ToYawPitchRoll().X;
					float x5 = MathUtils.NormalizeAngle(MathUtils.Atan2(v.X, v.Z) - x4);
					m_componentCreature.ComponentLocomotion.TurnOrder = new Vector2(MathUtils.Clamp(x5, -1f, 1f), 0f);
				}
			}
			else
			{
				m_componentCreature.ComponentLocomotion.WalkOrder = new Vector2(m_random.Float(-1f, 1f), m_random.Float(-0.5f, 1f));
				m_componentCreature.ComponentLocomotion.TurnOrder = new Vector2(MathUtils.Sign(m_componentFishModel.BendOrder.Value), 0f);
			}
		}, () =>
		{
			IsBend = false;
			m_componentCreature.ComponentLocomotion.JumpOrder = 0f;
			m_componentCreature.ComponentLocomotion.WalkOrder = null;
			m_componentCreature.ComponentLocomotion.TurnOrder = Vector2.Zero;
			m_componentCreature.ComponentLocomotion.LookOrder = Vector2.Zero;
			m_targetWaterPos = null;
		});
		m_stateMachine.TransitionTo("Inactive");
	}

	public bool IsOutOfWater()
	{
		return m_componentCreature.ComponentBody.ImmersionFactor < 0.33f;
	}

	private Vector3? FindNearestWaterBlock(int radius)
	{
		Vector3 position = m_componentCreature.ComponentBody.Position;
		int num = Terrain.ToCell(position.X);
		int num2 = Terrain.ToCell(position.Y);
		int num3 = Terrain.ToCell(position.Z);
		for (int i = 1; i <= radius; i++)
		{
			for (int j = -i; j <= i; j++)
			{
				for (int k = -i; k <= i; k++)
				{
					if (MathUtils.Abs(j) != i && MathUtils.Abs(k) != i)
					{
						continue;
					}
					for (int l = -2; l <= 2; l++)
					{
						int num4 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(num + j, num2 + l, num3 + k));
						if (num4 == 18 || BlocksManager.Blocks[num4] is FocalorsWaterBlock)
						{
							return new Vector3((float)(num + j) + 0.5f, (float)(num2 + l) + 0.6f, (float)(num3 + k) + 0.5f);
						}
					}
				}
			}
		}
		return null;
	}
}
