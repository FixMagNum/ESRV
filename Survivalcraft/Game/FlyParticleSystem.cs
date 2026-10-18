using System;
using Engine;
using Engine.Graphics;

namespace Game;

public class FlyParticleSystem : ParticleSystem<FlyParticleSystem.Particle>
{
	public class Particle : Game.Particle
	{
		public Vector3 Velocity;

		public Vector3 Home;

		public Vector3 Axis;

		public Vector3 U;

		public Vector3 V;

		public float Radius;

		public float Speed;

		public float Phase;

		public float BobAmp;

		public float BobPhase;

		public float Follow;

		public float TimeToLive;

		public float Seed;
	}

	private Random m_random;

	private SubsystemTerrain m_subsystemTerrain;

	private float m_toGenerate;

	private float m_age;

	private bool m_visible;

	private Vector3 m_position;

	private float m_maxVisibilityDistance;

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

	public float PopulationMultiplier { get; set; }

	public float VisibilityFactor { get; set; } = 1f;

	public FlyParticleSystem(SubsystemTerrain terrain, Vector3 position, float maxVisibilityDistance)
		: base(30)
	{
		PopulationMultiplier = 1f;
		m_random = new Random();
		m_subsystemTerrain = terrain;
		m_position = position;
		m_maxVisibilityDistance = maxVisibilityDistance;
		Texture = ContentManager.Get<Texture2D>("Textures/Fly");
		TextureSlotsCount = 1;
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
		m_toGenerate += PopulationMultiplier * dt * 3f;
		int num = 0;
		for (int i = 0; i < Particles.Length; i++)
		{
			Particle particle = Particles[i];
			if (particle.IsActive)
			{
				num++;
				particle.Phase += particle.Speed * dt;
				float num2 = MathUtils.Sin(particle.Phase);
				float num3 = MathUtils.Cos(particle.Phase);
				Vector3 vector = particle.Radius * (particle.U * num2 + particle.V * (num2 * num3));
				vector += particle.Axis * (particle.BobAmp * MathUtils.Sin(2f * particle.Phase + particle.BobPhase));
				Vector3 vector2 = (particle.Home + vector - particle.Position) * particle.Follow;
				particle.Velocity += (vector2 - particle.Velocity) * (8f * dt);
				particle.Velocity += m_random.Vector3(0f, 0.05f) * dt;
				float num4 = particle.Velocity.Length();
				if (num4 > 0.5f)
				{
					particle.Velocity *= 0.5f / num4;
				}
				Vector3 vector3 = particle.Position + particle.Velocity * dt;
				if (m_subsystemTerrain.Raycast(particle.Position, vector3, useInteractionBoxes: false, skipAirBlocks: true, (int value, float d) => BlocksManager.Blocks[Terrain.ExtractContents(value)].IsCollidable).HasValue)
				{
					particle.Phase += m_random.Float(1.2f, 2.4f);
					particle.Velocity = m_random.Vector3(0.05f);
				}
				else
				{
					particle.Position = vector3;
				}
				float a = (0.85f + 0.15f * MathUtils.Sin(11f * (particle.Seed + m_age))) * VisibilityFactor;
				particle.Color = Color.PremultiplyAlpha(new Color(0.06f, 0.06f, 0.06f, a));
				Particles[i] = particle;
			}
			else if (!IsStopped && m_toGenerate >= 1f && num < 6)
			{
				Vector3 vector4 = m_random.Vector3(0.4f, 0.35f);
				particle.IsActive = true;
				particle.Home = Position + vector4;
				particle.Position = particle.Home;
				particle.Axis = Vector3.Normalize(Vector3.UnitY + m_random.Vector3(0.15f));
				BuildBasis(particle.Axis, out particle.U, out particle.V);
				float x = m_random.Float(0f, (float)Math.PI * 2f);
				float num5 = MathUtils.Cos(x);
				float num6 = MathUtils.Sin(x);
				Vector3 u = particle.U;
				Vector3 v = particle.V;
				particle.U = num5 * u + num6 * v;
				particle.V = (0f - num6) * u + num5 * v;
				particle.Radius = m_random.Float(0.65f, 1.5f);
				particle.Speed = m_random.Float(1.7f, 3f);
				particle.Phase = m_random.Float(0f, (float)Math.PI * 2f);
				particle.BobAmp = m_random.Float(0.01f, 0.04f);
				particle.BobPhase = m_random.Float(0f, (float)Math.PI * 2f);
				particle.Follow = m_random.Float(3f, 6f);
				particle.Velocity = Vector3.Zero;
				particle.Size = new Vector2(m_random.Float(0.006f, 0.01f));
				particle.TextureSlot = 0;
				particle.Seed = m_random.Float(0f, 1f);
				particle.TimeToLive = float.PositiveInfinity;
				m_toGenerate--;
				num++;
				Particles[i] = particle;
			}
		}
		m_toGenerate = MathUtils.Remainder(m_toGenerate, 1f);
		m_age += dt;
		if (IsStopped)
		{
			return num == 0;
		}
		return false;
	}

	private static void BuildBasis(Vector3 axis, out Vector3 u, out Vector3 v)
	{
		Vector3 vector = Vector3.Normalize((axis.LengthSquared() > 1E-06f) ? axis : Vector3.UnitY);
		Vector3 v2 = ((Math.Abs(vector.Y) < 0.9f) ? Vector3.UnitY : Vector3.UnitX);
		u = Vector3.Normalize(Vector3.Cross(v2, vector));
		v = Vector3.Cross(vector, u);
	}

	public void SetCenter(Vector3 center)
	{
		Vector3 vector = center - m_position;
		if (vector.LengthSquared() <= 0f)
		{
			return;
		}
		m_position = center;
		for (int i = 0; i < Particles.Length; i++)
		{
			Particle particle = Particles[i];
			if (particle.IsActive)
			{
				particle.Home += vector;
				particle.Position += vector;
				Particles[i] = particle;
			}
		}
	}
}
