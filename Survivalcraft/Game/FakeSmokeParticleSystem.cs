using Engine;
using Engine.Graphics;

namespace Game;

public class FakeSmokeParticleSystem : ParticleSystem<FakeSmokeParticleSystem.Particle>
{
	public class Particle : Game.Particle
	{
		public Vector3 Velocity;

		public float TimeToLive;

		public float InitialTimeToLive;
	}

	public float m_toGenerate;

	private Random m_random;

	private SubsystemTerrain m_subsystemTerrain;

	private SubsystemWeather m_subsystemWeather;

	public bool m_visible;

	public Vector3 m_position;

	public float m_maxVisibilityDistance;

	public float m_age;

	public Vector3 Position { get; set; }

	public Vector3 Direction { get; set; } = new Vector3(0f, 1f, 0f);

	public float ParticleSize { get; set; } = 0.3f;

	public float SpawnRateMultiplier { get; set; } = 0.1f;

	public Color Color { get; set; }

	public bool IsStopped { get; set; }

	public float VerticalSpeed { get; set; } = 0.5f;

	public bool RandomOffsetEnabled { get; set; }

	public override bool Simulate(float dt)
	{
		if (IsStopped || (!m_visible && m_age >= 2f))
		{
			return true;
		}
		dt = MathUtils.Clamp(dt, 0f, 0.1f);
		float num = MathUtils.Pow(0.02f, dt);
		Color color;
		if (SettingsManager.SkyLightUseParticles)
		{
			int cellLight = m_subsystemTerrain.Terrain.GetCellLight(Terrain.ToCell(Position.X), Terrain.ToCell(Position.Y), Terrain.ToCell(Position.Z));
			float num2 = LightingManager.LightIntensityByLightValue[cellLight];
			color = new Color(num2, num2, num2) * Color.White;
		}
		else
		{
			float num3 = MathUtils.Clamp(m_subsystemWeather.SubsystemSky.SkyLightIntensity, 0f, 1f);
			color = new Color(num3, num3, num3) * Color.White;
		}
		Vector3 vector = new Vector3(m_subsystemWeather.CurrentWindVector.X, 0f, m_subsystemWeather.CurrentWindVector.Y);
		float currentWindStrength = m_subsystemWeather.CurrentWindStrength;
		m_toGenerate += SpawnRateMultiplier * dt * 3f;
		bool flag = false;
		for (int i = 0; i < Particles.Length; i++)
		{
			Particle particle = Particles[i];
			if (particle.IsActive)
			{
				flag = true;
				particle.TimeToLive -= dt;
				if (particle.TimeToLive > 0f)
				{
					Vector3 velocity = particle.Velocity;
					if (currentWindStrength > 0f)
					{
						float num4 = 1f;
						float num5;
						if (currentWindStrength <= 3f)
						{
							num5 = 10f;
						}
						else if (currentWindStrength <= 6f)
						{
							num5 = 30f;
							num4 = 1.2f;
						}
						else if (currentWindStrength <= 9f)
						{
							num5 = 45f;
							num4 = 1.5f;
						}
						else
						{
							num5 = 75f;
							num4 = 2f;
						}
						float num6 = MathUtils.Saturate(currentWindStrength / 10f);
						velocity += vector * currentWindStrength * num6 * num4 * dt;
						particle.Velocity = velocity * MathUtils.Lerp(1f, num6, num5 / 90f);
					}
					particle.Position += new Vector3(velocity.X, VerticalSpeed, velocity.Z) * dt;
					particle.Velocity *= num;
					float num7 = particle.TimeToLive / particle.InitialTimeToLive;
					float num8 = ((currentWindStrength > 10f) ? MathUtils.Lerp(1f, 4f, 1f - num7) : 1f);
					particle.Size = new Vector2(ParticleSize * (1f + (1f - num7)) * num8);
					particle.Color = Color.PremultiplyAlpha(new Color(color, (byte)(200f * num7)));
					particle.TextureSlot = (int)(4.5f * MathUtils.Saturate(1f - num7));
					if (particle.TimeToLive < 0.2f)
					{
						particle.TextureSlot = 8;
					}
				}
				else
				{
					particle.IsActive = false;
				}
			}
			else if (!IsStopped && m_toGenerate >= 1f)
			{
				Vector3 vector2 = (RandomOffsetEnabled ? new Vector3(m_random.Float(-0.3f, 0.3f), 0f, m_random.Float(-0.3f, 0.3f)) : Vector3.Zero);
				Vector3 vector3 = m_random.Vector3(0f, 1f);
				particle.IsActive = true;
				particle.Position = Position + vector3 * 0.05f - new Vector3(0f, 0.2f, 0f) + vector2;
				particle.Color = color;
				particle.Velocity = Direction + vector3 * 0.1f;
				particle.TimeToLive = 6f;
				particle.InitialTimeToLive = particle.TimeToLive;
				particle.Size = new Vector2(ParticleSize);
				particle.FlipX = m_random.Bool();
				particle.FlipY = m_random.Bool();
				m_toGenerate--;
			}
		}
		m_toGenerate = MathUtils.Remainder(m_toGenerate, 1f);
		m_visible = false;
		if (IsStopped)
		{
			return !flag;
		}
		return false;
	}

	public FakeSmokeParticleSystem(SubsystemTerrain terrain, SubsystemWeather subsystemWeather, Vector3 position, float maxVisibilityDistance)
		: base(100)
	{
		Color = Color.White;
		m_random = new Random();
		m_subsystemTerrain = terrain;
		m_subsystemWeather = subsystemWeather;
		m_position = position;
		m_maxVisibilityDistance = maxVisibilityDistance;
		Texture = ContentManager.Get<Texture2D>("Textures/NaturalSmokeParticle");
		TextureSlotsCount = 3;
	}

	public override void Draw(Camera camera)
	{
		float num = Vector3.Dot(m_position - camera.ViewPosition, camera.ViewDirection);
		if (num > -0.5f && num <= m_maxVisibilityDistance && Vector3.DistanceSquared(m_position, camera.ViewPosition) <= m_maxVisibilityDistance * m_maxVisibilityDistance)
		{
			m_visible = true;
			base.Draw(camera);
		}
		else
		{
			m_visible = false;
		}
	}
}
