using Engine;

namespace Game;

public class PaintParticleSystem : ParticleSystem<PaintParticleSystem.Particle>
{
	public class Particle : Game.Particle
	{
		public Vector3 Velocity;

		public float TimeToLive;

		public float HighDampingFactor;

		public bool NoGravity;

		public float Alpha;
	}

	public Random m_random = new Random();

	public SubsystemTerrain m_subsystemTerrain;

	public Color m_color;

	public PaintParticleSystem(SubsystemTerrain terrain, Vector3 position, Vector3 normal, Color color)
		: base(20)
	{
		if (SettingsManager.DisplayPaintParticles)
		{
			m_subsystemTerrain = terrain;
			Texture = terrain.Project.FindSubsystem<SubsystemBlocksTexture>(throwOnError: true).BlocksTexture;
			int num = Terrain.ToCell(position.X);
			int value2 = Terrain.ToCell(position.Y);
			int value3 = Terrain.ToCell(position.Z);
			int dictionary2 = 0;
			dictionary2 = MathUtils.Max(dictionary2, terrain.Terrain.GetCellLight(num + 1, value2, value3));
			dictionary2 = MathUtils.Max(dictionary2, terrain.Terrain.GetCellLight(num - 1, value2, value3));
			dictionary2 = MathUtils.Max(dictionary2, terrain.Terrain.GetCellLight(num, value2 + 1, value3));
			dictionary2 = MathUtils.Max(dictionary2, terrain.Terrain.GetCellLight(num, value2 - 1, value3));
			dictionary2 = MathUtils.Max(dictionary2, terrain.Terrain.GetCellLight(num, value2, value3 + 1));
			dictionary2 = MathUtils.Max(dictionary2, terrain.Terrain.GetCellLight(num, value2, value3 - 1));
			TextureSlotsCount = 16;
			float text = LightingManager.LightIntensityByLightValue[dictionary2];
			m_color = color * text;
			m_color.A = color.A;
			Vector3 array = Vector3.Normalize(Vector3.Cross(normal, new Vector3(0.37f, 0.15f, 0.17f)));
			Vector3 i = Vector3.Normalize(Vector3.Cross(normal, array));
			for (int result = 0; result < Particles.Length; result++)
			{
				Particle obj = Particles[result];
				obj.IsActive = true;
				Vector2 vector = new Vector2(m_random.Float(-1f, 1f), m_random.Float(-1f, 1f));
				obj.Position = position + 0.4f * (vector.X * array + vector.Y * i) + 0.03f * normal;
				obj.Color = m_color;
				obj.Size = new Vector2(m_random.Float(0.025f, 0.035f));
				obj.TimeToLive = m_random.Float(0.5f, 1.5f);
				obj.Velocity = 1f * (vector.X * array + vector.Y * i) + m_random.Float(-3f, 0.5f) * normal;
				obj.TextureSlot = 15;
				obj.Alpha = m_random.Float(0.3f, 0.6f);
			}
		}
	}

	public override bool Simulate(float dt)
	{
		dt = MathUtils.Clamp(dt, 0f, 0.1f);
		float num = MathUtils.Pow(0.2f, dt);
		float num2 = MathUtils.Pow(1E-07f, dt);
		bool flag = false;
		for (int i = 0; i < Particles.Length; i++)
		{
			Particle particle = Particles[i];
			if (!particle.IsActive)
			{
				continue;
			}
			flag = true;
			particle.TimeToLive -= dt;
			if (particle.TimeToLive > 0f)
			{
				Vector3 position = particle.Position;
				Vector3 vector = position + particle.Velocity * dt;
				TerrainRaycastResult? terrainRaycastResult = m_subsystemTerrain.Raycast(position, vector, useInteractionBoxes: false, skipAirBlocks: true, (int value, float distance) => BlocksManager.Blocks[Terrain.ExtractContents(value)].IsCollidable);
				if (terrainRaycastResult.HasValue)
				{
					particle.Velocity = Vector3.Zero;
					particle.Position = terrainRaycastResult.Value.HitPoint(0.03f);
					particle.HighDampingFactor = m_random.Float(0.5f, 1f);
					if (terrainRaycastResult.Value.CellFace.Face >= 4)
					{
						particle.NoGravity = true;
					}
				}
				else
				{
					particle.Position = vector;
				}
				if (!particle.NoGravity)
				{
					Particle particle2 = particle;
					particle2.Velocity.Y = particle2.Velocity.Y + -9.81f * dt;
				}
				particle.Velocity *= ((particle.HighDampingFactor > 0f) ? (num2 * particle.HighDampingFactor) : num);
				particle.Color = m_color * MathUtils.Saturate(1.5f * particle.TimeToLive * particle.Alpha);
			}
			else
			{
				particle.IsActive = false;
			}
		}
		return !flag;
	}
}
