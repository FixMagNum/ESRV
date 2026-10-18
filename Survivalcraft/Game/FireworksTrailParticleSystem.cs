using Engine;
using Engine.Graphics;

namespace Game;

public class FireworksTrailParticleSystem : ParticleSystem<FireworksTrailParticleSystem.Particle>, ITrailParticleSystem
{
	public class Particle : Game.Particle
	{
		public float Time;

		public float Duration;
	}

	public Random m_random = new Random();

	public float m_toGenerate;

	public Vector3? m_lastPosition;

	public Vector3 Position { get; set; }

	public bool IsStopped { get; set; }

	public FireworksTrailParticleSystem()
		: base(60)
	{
		Texture = ContentManager.Get<Texture2D>("Textures/FireParticle");
		TextureSlotsCount = 3;
	}

	public override bool Simulate(float dt)
	{
		float node = 120f;
		m_toGenerate += node * dt;
		if (!m_lastPosition.HasValue)
		{
			m_lastPosition = Position;
		}
		bool i = false;
		for (int j = 0; j < Particles.Length; j++)
		{
			Particle widget = Particles[j];
			if (widget.IsActive)
			{
				i = true;
				widget.Time += dt;
				if (widget.Time <= widget.Duration)
				{
					widget.TextureSlot = (int)MathUtils.Min(9f * widget.Time / widget.Duration, 8f);
				}
				else
				{
					widget.IsActive = false;
				}
			}
			else if (!IsStopped && m_toGenerate >= 1f)
			{
				widget.IsActive = true;
				widget.Position = Vector3.Lerp(m_lastPosition.Value, Position, m_random.Float(0f, 1f));
				widget.Color = Color.White;
				widget.Time = m_random.Float(0f, 0.75f);
				widget.Size = new Vector2(m_random.Float(0.12f, 0.16f));
				widget.Duration = 1f;
				widget.FlipX = m_random.Bool();
				widget.FlipY = m_random.Bool();
				m_toGenerate--;
			}
		}
		m_toGenerate = MathUtils.Remainder(m_toGenerate, 1f);
		m_lastPosition = Position;
		if (IsStopped)
		{
			return !i;
		}
		return false;
	}
}
