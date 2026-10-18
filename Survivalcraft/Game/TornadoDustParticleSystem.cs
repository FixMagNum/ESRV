using Engine;
using Engine.Graphics;

namespace Game;

public class TornadoDustParticleSystem : ParticleSystem<TornadoDustParticleSystem.Particle>
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

	public Vector3 Direction { get; set; }

	public Vector3 SwirlCenter { get; set; }

	public Vector2 Swirl { get; set; }

	public float ParticleSize { get; set; }

	public float BaseAlpha { get; set; }

	public float Lifetime { get; set; }

	public float SpawnRateMultiplier { get; set; }

	public float VerticalSpeed { get; set; }

	public bool RandomOffsetEnabled { get; set; }

	public Color Color { get; set; }

	public bool IsStopped { get; set; }

	public float BaseY { get; set; } = 70f;

	public float TopY { get; set; } = 130f;

	public float SpawnRadius { get; set; } = 8f;

	public float TopRadius { get; set; } = 1.5f;

	public TornadoDustParticleSystem(SubsystemTerrain terrain, SubsystemWeather weather, Vector3 position, float maxVisibilityDistance)
		: base(200)
	{
		Direction = new Vector3(0f, 1f, 0f);
		Swirl = new Vector2(2f, 2f);
		ParticleSize = 0.9f;
		BaseAlpha = 210f;
		Lifetime = 6f;
		SpawnRateMultiplier = 0.25f;
		VerticalSpeed = 2f;
		RandomOffsetEnabled = true;
		Color = Color.White;
		Color = Color.White;
		m_random = new Random();
		m_subsystemTerrain = terrain;
		m_subsystemWeather = weather;
		m_position = position;
		m_maxVisibilityDistance = maxVisibilityDistance;
		Texture = ContentManager.Get<Texture2D>("Textures/TornadoDustParticle");
		TextureSlotsCount = 3;
	}

	public override bool Simulate(float dt)
	{
		if (IsStopped || (!m_visible && m_age >= 2f))
		{
			return true;
		}
		dt = MathUtils.Clamp(dt, 0f, 0.1f);
		float num = MathUtils.Pow(0.02f, dt);
		int x = Terrain.ToCell(Position.X);
		int y = Terrain.ToCell(Position.Y);
		int z = Terrain.ToCell(Position.Z);
		int cellLight = m_subsystemTerrain.Terrain.GetCellLight(x, y, z);
		float num2 = MathUtils.Max(LightingManager.LightIntensityByLightValue[cellLight], 0.5f);
		Color color = new Color(num2, num2, num2) * Color;
		m_toGenerate += SpawnRateMultiplier * dt * 4f;
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
					Vector2 vector = new Vector2(particle.Position.X, particle.Position.Z);
					Vector2 vector2 = new Vector2(SwirlCenter.X, SwirlCenter.Z);
					Vector2 vector3 = vector - vector2;
					float num3 = vector3.X * vector3.X + vector3.Y * vector3.Y;
					if (num3 > 1E-10f)
					{
						float num4 = MathUtils.Sqrt(num3);
						Vector2 vector4 = vector3 / num4;
						Vector2 vector5 = new Vector2(0f - vector4.Y, vector4.X);
						float num5 = 0f;
						if (TopY > BaseY)
						{
							num5 = MathUtils.Saturate((particle.Position.Y - BaseY) / (TopY - BaseY));
						}
						float num6 = MathUtils.Lerp(SpawnRadius, TopRadius, num5);
						float num7 = num4 - num6;
						float num8 = Swirl.X * (0.6f + 0.8f * num5);
						float num9 = Swirl.Y * (0.4f + 1.6f * num5);
						Vector2 vector6 = vector5 * (num8 * num4);
						Vector2 vector7 = -vector4 * (num9 * num7);
						velocity.X += (vector6.X + vector7.X) * dt;
						velocity.Z += (vector6.Y + vector7.Y) * dt;
					}
					particle.Position += new Vector3(velocity.X, VerticalSpeed, velocity.Z) * dt;
					particle.Velocity = velocity * num;
					float num10 = MathUtils.Saturate(particle.TimeToLive / particle.InitialTimeToLive);
					particle.Size = new Vector2(ParticleSize * (1f + (1f - num10)));
					int num11 = TextureSlotsCount * TextureSlotsCount;
					int textureSlot = (int)MathUtils.Clamp(MathUtils.Floor((1f - num10) * (float)(num11 - 1)), 0f, num11 - 1);
					particle.TextureSlot = textureSlot;
					if (particle.TimeToLive < 0.2f)
					{
						particle.TextureSlot = num11 - 1;
					}
					particle.Color = Color.PremultiplyAlpha(new Color(color, (byte)(BaseAlpha * num10)));
				}
				else
				{
					particle.IsActive = false;
				}
			}
			else
			{
				if (IsStopped || !(m_toGenerate >= 1f))
				{
					continue;
				}
				int num12 = m_random.Int(2, 3);
				int num13 = 0;
				while (num13 < num12 && m_toGenerate >= 1f)
				{
					float num14 = m_random.Float(0.4f * SpawnRadius, SpawnRadius);
					float x2 = m_random.Float(0f, 6.283185f);
					Vector3 vector8 = new Vector3(MathUtils.Cos(x2) * num14, 0f, MathUtils.Sin(x2) * num14);
					Vector3 vector9 = (RandomOffsetEnabled ? new Vector3(m_random.Float(-0.25f, 0.25f), 0f, m_random.Float(-0.25f, 0.25f)) : Vector3.Zero);
					Vector3 vector10 = m_random.Vector3(0f, 1f);
					particle.IsActive = true;
					particle.Position = new Vector3(Position.X, BaseY, Position.Z) + vector8 - new Vector3(0f, 0.22f, 0f) + vector9;
					particle.Color = color;
					particle.Velocity = Direction + vector10 * 0.05f;
					particle.TimeToLive = Lifetime;
					particle.InitialTimeToLive = particle.TimeToLive;
					particle.Size = new Vector2(ParticleSize);
					particle.FlipX = m_random.Bool();
					particle.FlipY = m_random.Bool();
					m_toGenerate--;
					num13++;
					if (num13 < num12 && i < Particles.Length - 1)
					{
						i++;
					}
				}
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
