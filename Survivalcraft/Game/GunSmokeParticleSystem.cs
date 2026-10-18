using Engine;
using Engine.Graphics;

namespace Game;

public class GunSmokeParticleSystem : ParticleSystem<GunSmokeParticleSystem.Particle>
{
	public class Particle : Game.Particle
	{
		public Vector3 Velocity;

		public float Time;

		public float Duration;
	}

	public Random m_random = new Random();

	public float m_time;

	public float m_toGenerate;

	public Vector3 m_position;

	public Vector3 m_direction;

	public Color m_color;

	public float ParticleSize { get; set; } = 0.3f;

	public float SpawnRateMultiplier { get; set; } = 1f;

	public override bool Simulate(float dt)
	{
		m_time += dt;
		float num = MathUtils.Lerp(100f, 15f, MathUtils.Saturate(2f * m_time / 0.5f)) * SpawnRateMultiplier;
		float num2 = MathUtils.Pow(0.05f, dt);
		float num3 = MathUtils.Lerp(10f, 0f, MathUtils.Saturate(2f * m_time / 0.5f));
		Vector3 vector = new Vector3(0.5f, 1f, 0.5f);
		if (m_time < 0.5f)
		{
			m_toGenerate += num * dt;
		}
		else
		{
			m_toGenerate = 0f;
		}
		bool flag = false;
		for (int i = 0; i < Particles.Length; i++)
		{
			Particle particle = Particles[i];
			if (particle.IsActive)
			{
				flag = true;
				particle.Time += dt;
				if (particle.Time <= particle.Duration)
				{
					particle.Position += particle.Velocity * dt;
					particle.Velocity *= num2;
					particle.Velocity += vector * dt;
					particle.TextureSlot = (int)MathUtils.Min(9f * particle.Time / particle.Duration, 8f);
					particle.Size = new Vector2(ParticleSize);
				}
				else
				{
					particle.IsActive = false;
				}
			}
			else if (m_toGenerate >= 1f)
			{
				particle.IsActive = true;
				Vector3 vector2 = m_random.Vector3(-0.3f, 0.3f);
				particle.Position = m_position + 0.3f * vector2;
				particle.Color = m_color;
				particle.Velocity = num3 * (m_direction + m_random.Vector3(-0.1f, 0.1f)) + 0.8f * vector2;
				particle.Size = Vector2.Zero;
				particle.Time = 0f;
				particle.Duration = m_random.Float(0.7f, 1.5f);
				particle.FlipX = m_random.Bool();
				particle.FlipY = m_random.Bool();
				m_toGenerate--;
			}
		}
		m_toGenerate = MathUtils.Remainder(m_toGenerate, 1f);
		if (!flag)
		{
			return m_time >= 0.5f;
		}
		return false;
	}

	public GunSmokeParticleSystem(SubsystemTerrain terrain, Vector3 position, Vector3 direction)
		: base(50)
	{
		Texture = ContentManager.Get<Texture2D>("Textures/GunSmokeParticle");
		TextureSlotsCount = 3;
		m_position = position;
		m_direction = Vector3.Normalize(direction);
		int num = Terrain.ToCell(position.X);
		int num2 = Terrain.ToCell(position.Y);
		int num3 = Terrain.ToCell(position.Z);
		int x = 0;
		x = MathUtils.Max(x, terrain.Terrain.GetCellLight(num + 1, num2, num3));
		x = MathUtils.Max(x, terrain.Terrain.GetCellLight(num - 1, num2, num3));
		x = MathUtils.Max(x, terrain.Terrain.GetCellLight(num, num2 + 1, num3));
		x = MathUtils.Max(x, terrain.Terrain.GetCellLight(num, num2 - 1, num3));
		x = MathUtils.Max(x, terrain.Terrain.GetCellLight(num, num2, num3 + 1));
		x = MathUtils.Max(x, terrain.Terrain.GetCellLight(num, num2, num3 - 1));
		float num4 = LightingManager.LightIntensityByLightValue[x];
		m_color = new Color(num4, num4, num4);
	}
}
