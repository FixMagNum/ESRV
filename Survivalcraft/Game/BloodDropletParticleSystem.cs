using Engine;
using Engine.Graphics;

namespace Game;

public class BloodDropletParticleSystem : ParticleSystem<BloodDropletParticleSystem.Particle>
{
	public class Particle : Game.Particle
	{
		public Vector3 Velocity;

		public Vector3 PreviousPosition;

		public new byte TextureSlot;

		public float TimeToLive;

		public int HitFace;

		public bool InWater;

		public float WaterTimer;
	}

	private readonly SubsystemTerrain m_subsystemTerrain;

	private readonly Vector3 m_origin;

	private readonly Vector3 m_direction;

	private readonly int m_dropletsCount;

	private readonly float m_spreadRadians;

	private readonly float m_minSpeed;

	private readonly float m_maxSpeed;

	private readonly Random m_random;

	private TexturedBatch3D m_batch;

	private readonly float m_gravity;

	public override bool Simulate(float dt)
	{
		dt = MathUtils.Clamp(dt, 0f, 0.1f);
		bool flag = false;
		for (int i = 0; i < Particles.Length; i++)
		{
			Particle particle = Particles[i];
			if (!particle.IsActive)
			{
				continue;
			}
			float num = m_gravity;
			if (particle.InWater)
			{
				num *= 0.12f;
			}
			Particle particle2 = particle;
			particle2.Velocity.Y = particle2.Velocity.Y + num * dt;
			float num2 = MathUtils.Abs(particle.Velocity.Y) * 0.02f;
			if (!particle.InWater)
			{
				Particle particle3 = particle;
				particle3.Velocity.X = particle3.Velocity.X + m_random.Float(0f - num2, num2);
				Particle particle4 = particle;
				particle4.Velocity.Z = particle4.Velocity.Z + m_random.Float(0f - num2, num2);
			}
			else
			{
				Particle particle5 = particle;
				particle5.Velocity.X = particle5.Velocity.X * 0.96f;
				Particle particle6 = particle;
				particle6.Velocity.Z = particle6.Velocity.Z * 0.96f;
				particle.Velocity.Y = MathUtils.Lerp(particle.Velocity.Y, -0.2f, 0.02f);
			}
			particle.Position += particle.Velocity * dt;
			particle.TimeToLive -= dt;
			particle.Size *= 0.992f;
			particle.Color *= 0.995f;
			Block blockAt = GetBlockAt(particle.Position);
			if (blockAt is FocalorsMagmaBlock || blockAt is QuicksandBlock)
			{
				particle.IsActive = false;
				continue;
			}
			if (blockAt is FocalorsWaterBlock)
			{
				if (!particle.InWater)
				{
					particle.InWater = true;
					particle.WaterTimer = 0.5f;
					particle.Velocity *= 0.25f;
				}
				else
				{
					particle.WaterTimer -= dt;
					if (particle.WaterTimer <= 0f)
					{
						SubsystemParticles.AddParticleSystem(new BloodWaterPuddleParticleSystem(m_subsystemTerrain, particle.Position));
						particle.IsActive = false;
						continue;
					}
				}
			}
			else
			{
				particle.InWater = false;
				particle.WaterTimer = 0f;
			}
			if (!particle.InWater && Raycast(particle) && m_random.Float(0f, 1f) < 0.3f)
			{
				SubsystemParticles.AddParticleSystem(new BloodPuddleParticleSystem(m_subsystemTerrain, particle.Position, particle.HitFace));
			}
			if (particle.TimeToLive <= 0f)
			{
				particle.IsActive = false;
			}
			else
			{
				flag = true;
			}
		}
		return !flag;
	}

	public override void Draw(Camera camera)
	{
		if (Texture == null)
		{
			return;
		}
		if (m_batch == null)
		{
			m_batch = SubsystemParticles.PrimitivesRenderer.TexturedBatch(Texture, useAlphaTest: false, 0, DepthStencilState.DepthRead, null, BlendState.AlphaBlend, SamplerState.PointClamp);
		}
		Vector3 viewDirection = camera.ViewDirection;
		Vector3 vector = Vector3.Normalize(Vector3.Cross(viewDirection, Vector3.UnitY));
		Vector3 vector2 = Vector3.Normalize(Vector3.Cross(vector, viewDirection));
		for (int i = 0; i < Particles.Length; i++)
		{
			Particle particle = Particles[i];
			if (particle.IsActive)
			{
				Vector3 vector3 = vector * particle.Size.X * 0.5f;
				Vector3 vector4 = vector2 * particle.Size.Y * 0.5f;
				float num = (float)(particle.TextureSlot % 3) / 3f;
				float num2 = (float)(particle.TextureSlot / 3) / 3f;
				float num3 = 1f / 3f;
				m_batch.QueueQuad(particle.Position - vector3 - vector4, particle.Position + vector3 - vector4, particle.Position + vector3 + vector4, particle.Position - vector3 + vector4, new Vector2(num, num2 + num3), new Vector2(num + num3, num2 + num3), new Vector2(num + num3, num2), new Vector2(num, num2), particle.Color);
			}
		}
	}

	private void SpawnDroplets()
	{
		for (int i = 0; i < m_dropletsCount; i++)
		{
			Vector3 vector = Vector3.Transform(m_direction, Quaternion.CreateFromAxisAngle(Vector3.UnitY, m_random.Float(0f - m_spreadRadians, m_spreadRadians)));
			float num = m_random.Float(0.2f, 0.6f);
			Vector3 vector2 = Vector3.Normalize(vector * 0.8f + Vector3.UnitY * num);
			float num2 = m_random.Float(m_minSpeed, m_maxSpeed);
			for (int j = 0; j < 10; j++)
			{
				Particle obj = Particles[i * 10 + j];
				obj.IsActive = true;
				obj.Position = m_origin + vector2 * ((float)j * 0.1f);
				obj.Velocity = vector2 * num2 * m_random.Float(0.8f, 1.2f);
				obj.Size = new Vector2(0.2f);
				obj.Color = new Color(200, 0, 0);
				obj.TimeToLive = 3f;
				obj.TextureSlot = (byte)m_random.Int(0, 9);
			}
		}
	}

	private bool Raycast(Particle p)
	{
		if (p.PreviousPosition == Vector3.Zero)
		{
			p.PreviousPosition = p.Position;
		}
		TerrainRaycastResult? terrainRaycastResult = m_subsystemTerrain.Raycast(p.PreviousPosition, p.Position, useInteractionBoxes: false, skipAirBlocks: false, (int v, float dist) => BlocksManager.Blocks[Terrain.ExtractContents(v)].IsCollidable);
		p.PreviousPosition = p.Position;
		if (terrainRaycastResult.HasValue)
		{
			p.Position = terrainRaycastResult.Value.HitPoint(0.01f);
			p.IsActive = false;
			p.HitFace = terrainRaycastResult.Value.CellFace.Face;
			return true;
		}
		return false;
	}

	public BloodDropletParticleSystem(SubsystemTerrain subsystemTerrain, Vector3 origin, Vector3 direction, int dropletsCount, float spreadDegrees = 5f, float minSpeed = 0.05f, float maxSpeed = 0.4f, float gravity = -22.81f)
		: base(dropletsCount * 10)
	{
		if (SettingsManager.DisplayBloodParticles)
		{
			m_random = new Random();
			m_subsystemTerrain = subsystemTerrain;
			m_origin = origin;
			m_direction = Vector3.Normalize(direction);
			m_dropletsCount = dropletsCount;
			m_spreadRadians = MathUtils.DegToRad(spreadDegrees);
			m_minSpeed = minSpeed;
			m_maxSpeed = maxSpeed;
			m_gravity = gravity;
			Texture = ContentManager.Get<Texture2D>("Textures/BloodDropletParticle");
			TextureSlotsCount = 9;
			SpawnDroplets();
		}
	}

	private Block GetBlockAt(Vector3 p)
	{
		int x = Terrain.ToCell(p.X);
		int y = Terrain.ToCell(p.Y);
		int z = Terrain.ToCell(p.Z);
		int cellValue = m_subsystemTerrain.Terrain.GetCellValue(x, y, z);
		return BlocksManager.Blocks[Terrain.ExtractContents(cellValue)];
	}
}
