using Engine;
using Engine.Graphics;

namespace Game;

public class FireflyParticleSystem : ParticleSystem<FireflyParticleSystem.Particle>
{
	public class Particle : Game.Particle
	{
		public Vector3 Velocity;

		public float TimeToLive;

		public float Seed;

		public Vector3 TargetVelocity;

		public float TimeToNextTurn;

		public float TurnSpeed;
	}

	private float m_toGenerate;

	private Random m_random;

	private SubsystemTerrain m_subsystemTerrain;

	private float m_age;

	private bool m_visible;

	private Vector3 m_position;

	private float m_maxVisibilityDistance;

	private const float MaxSpeed = 0.8f;

	private const float MinSpeed = 0.4f;

	public Vector3 Position
	{
		get
		{
			return m_position;
		}
		set
		{
			m_position = value;
		}
	}

	public bool IsStopped { get; set; }

	public float PopulationMultiplier { get; set; } = 1f;

	public float VisibilityFactor { get; set; } = 1f;

	public FireflyParticleSystem(SubsystemTerrain terrain, Vector3 position, float maxVisibilityDistance)
		: base(30)
	{
		m_random = new Random();
		m_subsystemTerrain = terrain;
		m_position = position;
		m_maxVisibilityDistance = maxVisibilityDistance;
		Texture = ContentManager.Get<Texture2D>("Textures/FirefliesParticle");
		TextureSlotsCount = 3;
	}

	public override void Draw(Camera camera)
	{
		if (Vector3.DistanceSquared(Position, camera.ViewPosition) > m_maxVisibilityDistance * m_maxVisibilityDistance)
		{
			m_visible = false;
			return;
		}
		m_visible = true;
		base.Draw(camera);
	}

	public override bool Simulate(float dt)
	{
		if (IsStopped)
		{
			return true;
		}
		dt = MathUtils.Clamp(dt, 0f, 0.1f);
		float num = MathUtils.Pow(0.7f, dt);
		m_toGenerate += PopulationMultiplier * dt * 3f;
		int num2 = 0;
		for (int i = 0; i < Particles.Length; i++)
		{
			Particle particle = Particles[i];
			if (particle.IsActive)
			{
				num2++;
				particle.TimeToNextTurn -= dt;
				if (particle.TimeToNextTurn <= 0f)
				{
					particle.TargetVelocity = RandomVelocity();
					particle.TurnSpeed = m_random.Float(0.3f, 0.6f);
					particle.TimeToNextTurn = m_random.Float(10f, 60f);
				}
				particle.Velocity += (particle.TargetVelocity - particle.Velocity) * (particle.TurnSpeed * dt);
				Vector3 vector = particle.Position + particle.Velocity * dt;
				if (m_subsystemTerrain.Raycast(particle.Position, vector, useInteractionBoxes: false, skipAirBlocks: true, (int v, float d) => BlocksManager.Blocks[Terrain.ExtractContents(v)].IsCollidable).HasValue)
				{
					particle.Velocity = RandomVelocity();
					particle.TargetVelocity = particle.Velocity;
				}
				else
				{
					particle.Position = vector;
				}
				particle.Velocity += m_random.Vector3(0f, 0.5f) * dt;
				particle.Velocity *= num;
				float num3 = particle.Velocity.Length();
				if (num3 < 0.4f)
				{
					particle.Velocity *= 0.4f / ((num3 > 0.0001f) ? num3 : 1f);
				}
				else if (num3 > 0.8f)
				{
					particle.Velocity *= 0.8f / num3;
				}
				float num4 = 0.8f + 0.2f * MathUtils.Sin(6f * (particle.Seed + m_age));
				float visibilityFactor = VisibilityFactor;
				particle.Color = Color.PremultiplyAlpha(new Color(num4 * visibilityFactor, num4 * visibilityFactor, num4 * visibilityFactor, visibilityFactor));
			}
			else if (!IsStopped && m_toGenerate >= 1f && num2 < 3)
			{
				Vector3 vector2 = m_random.Vector3(0f, 1f) * 0.5f;
				particle.IsActive = true;
				particle.Position = Position + vector2;
				particle.Velocity = RandomVelocity();
				particle.TargetVelocity = particle.Velocity;
				particle.TimeToNextTurn = m_random.Float(10f, 60f);
				particle.TurnSpeed = m_random.Float(0.3f, 0.6f);
				particle.Size = new Vector2(0.08f);
				particle.TextureSlot = m_random.Int(0, 8);
				particle.Seed = m_random.Float(0f, 1f);
				particle.TimeToLive = float.PositiveInfinity;
				m_toGenerate--;
				num2++;
			}
		}
		m_toGenerate = MathUtils.Remainder(m_toGenerate, 1f);
		m_age += dt;
		if (IsStopped)
		{
			return num2 == 0;
		}
		return false;
	}

	private Vector3 RandomVelocity()
	{
		Vector3 y = m_random.Vector3(1f);
		y.Y = m_random.Float(-0.1f, 0.4f);
		y = Vector3.Normalize(y);
		return y * m_random.Float(0.4f, 0.8f);
	}
}
