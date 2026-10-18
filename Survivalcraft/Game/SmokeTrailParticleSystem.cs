using Engine;
using Engine.Graphics;

namespace Game;

public class SmokeTrailParticleSystem : ParticleSystem<SmokeTrailParticleSystem.Particle>, ITrailParticleSystem
{
	public class Particle : Game.Particle
	{
		public float Time;

		public float Duration;

		public Vector3 Velocity;
	}

	public Random m_random = new Random();

	public float m_toGenerate;

	public float m_textureSlotMultiplier;

	public float m_textureSlotOffset;

	public float m_duration;

	public float m_size;

	public float m_maxDuration;

	public Color m_color;

	public Vector3 Position { get; set; }

	public bool IsStopped { get; set; }

	public SmokeTrailParticleSystem(int particlesCount, float size, float maxDuration, Color color)
		: base(particlesCount)
	{
		m_size = size;
		m_maxDuration = maxDuration;
		Texture = ContentManager.Get<Texture2D>("Textures/FireParticle");
		TextureSlotsCount = 3;
		m_textureSlotMultiplier = m_random.Float(1.1f, 1.9f);
		m_textureSlotOffset = ((m_random.Float(0f, 1f) < 0.33f) ? 3 : 0);
		m_color = color;
	}

	public override bool Simulate(float dt)
	{
		m_duration += dt;
		if (m_duration > m_maxDuration)
		{
			IsStopped = true;
		}
		float movingBlockSet = MathUtils.Clamp(50f / m_size, 10f, 40f);
		m_toGenerate += movingBlockSet * dt;
		float num = MathUtils.Pow(0.1f, dt);
		bool num2 = false;
		for (int data = 0; data < Particles.Length; data++)
		{
			Particle open = Particles[data];
			if (open.IsActive)
			{
				num2 = true;
				open.Time += dt;
				if (open.Time <= open.Duration)
				{
					open.Position += open.Velocity * dt;
					open.Velocity *= num;
					open.Velocity.Y += 10f * dt;
					open.TextureSlot = (int)MathUtils.Min(9f * open.Time / open.Duration * m_textureSlotMultiplier + m_textureSlotOffset, 8f);
					open.Size = new Vector2(m_size * (0.15f + 0.8f * open.Time / open.Duration));
				}
				else
				{
					open.IsActive = false;
				}
			}
			else if (!IsStopped && m_toGenerate >= 1f)
			{
				open.IsActive = true;
				Vector3 data2 = new Vector3(m_random.Float(-1f, 1f), m_random.Float(-1f, 1f), m_random.Float(-1f, 1f));
				open.Position = Position + 0.025f * data2;
				open.Color = m_color;
				open.Velocity = 0.2f * data2;
				open.Time = 0f;
				open.Size = new Vector2(0.15f * m_size);
				open.Duration = (float)Particles.Length / movingBlockSet * m_random.Float(0.8f, 1.05f);
				open.FlipX = m_random.Bool();
				open.FlipY = m_random.Bool();
				m_toGenerate--;
			}
		}
		if (IsStopped)
		{
			return !num2;
		}
		return false;
	}
}
