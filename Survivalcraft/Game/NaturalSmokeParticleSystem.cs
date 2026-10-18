using Engine;
using Engine.Graphics;

namespace Game;

public class NaturalSmokeParticleSystem : ParticleSystem<NaturalSmokeParticleSystem.Particle>
{
	public class Particle : Game.Particle
	{
		public Vector3 Velocity;

		public float TimeToLive;

		public float InitialTimeToLive;

		public Vector3 HorizontalDirection;
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
		Color white = Color.White;
		Vector3 vector = new Vector3(m_subsystemWeather.CurrentWindVector.X, 0f, m_subsystemWeather.CurrentWindVector.Y);
		float currentWindStrength = m_subsystemWeather.CurrentWindStrength;
		bool flag = currentWindStrength > 10f;
		float windExposureFactor = m_subsystemWeather.GetWindExposureFactor(Position);
		m_toGenerate += SpawnRateMultiplier * dt * 3f;
		bool flag2 = false;
		int num2 = 0;
		for (int i = 0; i < Particles.Length; i++)
		{
			Particle particle = Particles[i];
			if (particle.IsActive)
			{
				flag2 = true;
				num2++;
				particle.TimeToLive -= dt;
				if (particle.TimeToLive > 0f)
				{
					Vector3 vector2 = particle.Position + particle.Velocity * dt;
					int x = Terrain.ToCell(vector2.X);
					int y = Terrain.ToCell(vector2.Y);
					int z = Terrain.ToCell(vector2.Z);
					int cellContents = m_subsystemTerrain.Terrain.GetCellContents(x, y, z);
					if (cellContents != 0 && BlocksManager.Blocks[cellContents].IsCollidable && BlocksManager.Blocks[cellContents].GetType() != typeof(CampfireBlock) && BlocksManager.Blocks[cellContents].GetType() != typeof(CoalCampfireBlock))
					{
						if (particle.Velocity.Y > 0f && particle.HorizontalDirection == Vector3.Zero)
						{
							Vector3 vector3 = ChooseBestDirection(particle.Position);
							particle.HorizontalDirection = vector3 * 2f;
						}
						particle.Velocity = new Vector3(particle.HorizontalDirection.X, 0f, particle.HorizontalDirection.Z);
						particle.Position += particle.Velocity * dt;
					}
					else
					{
						Vector3 velocity = particle.Velocity;
						if (currentWindStrength > 0f && windExposureFactor > 0f)
						{
							float num3 = 1f;
							float num4;
							if (currentWindStrength <= 3f)
							{
								num4 = 10f;
							}
							else if (currentWindStrength <= 6f)
							{
								num4 = 30f;
								num3 = 1.2f;
							}
							else if (currentWindStrength <= 9f)
							{
								num4 = 45f;
								num3 = 1.5f;
							}
							else
							{
								num4 = 75f;
								num3 = 2f;
							}
							float num5 = MathUtils.Saturate(currentWindStrength / 10f);
							velocity += vector * currentWindStrength * num5 * num3 * dt;
							particle.Velocity = velocity * MathUtils.Lerp(1f, num5, num4 / 90f);
						}
						particle.Position += new Vector3(velocity.X, VerticalSpeed, velocity.Z) * dt;
					}
					particle.Velocity *= num;
					float num6 = particle.TimeToLive / particle.InitialTimeToLive;
					float num7 = ((flag && windExposureFactor > 0f) ? MathUtils.Lerp(1f, 4f, 1f - num6) : 1f);
					particle.Size = new Vector2(ParticleSize * (1f + (1f - num6)) * num7);
					particle.Color = Color.PremultiplyAlpha(new Color(white, (byte)(200f * num6)));
					particle.TextureSlot = (int)(4.5f * MathUtils.Saturate(1f - num6));
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
				Vector3 vector4 = (RandomOffsetEnabled ? new Vector3(m_random.Float(-0.3f, 0.3f), 0f, m_random.Float(-0.3f, 0.3f)) : new Vector3(0f, 0f, 0f));
				Vector3 vector5 = m_random.Vector3(0f, 1f);
				particle.IsActive = true;
				particle.Position = Position + vector5 * 0.05f - new Vector3(0f, 0.2f, 0f) + vector4;
				particle.Color = white;
				particle.Velocity = Direction + vector5 * 0.1f;
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
			return !flag2;
		}
		return false;
	}

	public NaturalSmokeParticleSystem(SubsystemTerrain terrain, SubsystemWeather subsystemWeather, Vector3 position, float maxVisibilityDistance)
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

	private Vector3 ChooseBestDirection(Vector3 currentPosition)
	{
		int num = 10;
		Vector3[] array = new Vector3[8]
		{
			new Vector3(1f, 0f, 0f),
			new Vector3(-1f, 0f, 0f),
			new Vector3(0f, 0f, 1f),
			new Vector3(0f, 0f, -1f),
			new Vector3(1f, 0f, 1f),
			new Vector3(-1f, 0f, 1f),
			new Vector3(1f, 0f, -1f),
			new Vector3(-1f, 0f, -1f)
		};
		Vector3 vector = Vector3.Zero;
		int num2 = int.MaxValue;
		Vector3[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			Vector3 vector2 = array2[i];
			int num3 = 0;
			for (int j = 1; j <= num; j++)
			{
				int x = Terrain.ToCell(currentPosition.X + vector2.X * (float)j);
				int y = Terrain.ToCell(currentPosition.Y);
				int z = Terrain.ToCell(currentPosition.Z + vector2.Z * (float)j);
				int cellContents = m_subsystemTerrain.Terrain.GetCellContents(x, y, z);
				if (cellContents != 0 && BlocksManager.Blocks[cellContents].IsCollidable)
				{
					num3++;
				}
			}
			if (num3 < num2)
			{
				num2 = num3;
				vector = vector2;
				if (num3 == 0)
				{
					break;
				}
			}
		}
		if (vector == Vector3.Zero)
		{
			return array[m_random.Int(0, array.Length - 1)];
		}
		return Vector3.Normalize(vector);
	}
}
