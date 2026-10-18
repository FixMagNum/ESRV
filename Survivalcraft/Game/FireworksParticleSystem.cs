using System;
using Engine;
using Engine.Graphics;

namespace Game;

public class FireworksParticleSystem : ParticleSystem<FireworksParticleSystem.Particle>
{
	public class Particle : Game.Particle
	{
		public Vector3 Velocity;

		public float TimeToLive;

		public float FadeRate;

		public Color BaseColor;

		public float RotationSpeed;

		public float GenerationFrequency;

		public float GenerationAccumulator;

		public bool HighDamping;
	}

	public Random m_random = new Random();

	public Color m_color;

	public float m_flickering;

	public int m_nextParticle;

	public FireworksParticleSystem(Vector3 position, Color color, FireworksBlock.Shape shape, float flickering, float particleSize)
		: base(300)
	{
		Texture = ContentManager.Get<Texture2D>("Textures/FireworksParticle");
		m_color = color;
		m_flickering = flickering;
		TextureSlotsCount = 2;
		if (shape == FireworksBlock.Shape.SmallBurst || shape == FireworksBlock.Shape.LargeBurst)
		{
			int num = ((shape == FireworksBlock.Shape.SmallBurst) ? 100 : 200);
			while (m_nextParticle < num)
			{
				Particle num2 = Particles[m_nextParticle++];
				num2.IsActive = true;
				num2.Position = position;
				num2.Size = new Vector2(0.2f * particleSize);
				num2.TimeToLive = ((shape == FireworksBlock.Shape.SmallBurst) ? m_random.Float(0.5f, 2f) : m_random.Float(1f, 3f));
				num2.Velocity = m_random.Vector3(0.5f, 1f);
				num2.Velocity *= (float)((shape == FireworksBlock.Shape.SmallBurst) ? 16 : 26) * num2.Velocity.LengthSquared();
				num2.TextureSlot = m_random.Int(0, 3);
				num2.FadeRate = m_random.Float(1f, 3f);
				num2.BaseColor = m_color * m_random.Float(0.5f, 1f);
				num2.RotationSpeed = 0f;
			}
		}
		switch (shape)
		{
		case FireworksBlock.Shape.Circle:
		{
			float fillColor = m_random.Float(0f, (float)Math.PI * 2f);
			int num5 = 150;
			for (int j = 0; j < num5; j++)
			{
				float vector2 = (float)Math.PI * 2f * (float)j / (float)num5 + fillColor;
				Vector3 value = new Vector3(MathUtils.Sin(vector2) + 0.1f * m_random.Float(-1f, 1f), 0f, MathUtils.Cos(vector2) + 0.1f * m_random.Float(-1f, 1f));
				Particle obj2 = Particles[m_nextParticle++];
				obj2.IsActive = true;
				obj2.Position = position;
				obj2.Size = new Vector2(0.2f * particleSize);
				obj2.TimeToLive = m_random.Float(1f, 3f);
				obj2.Velocity = 20f * value;
				obj2.TextureSlot = m_random.Int(0, 3);
				obj2.FadeRate = m_random.Float(1f, 3f);
				obj2.BaseColor = m_color * m_random.Float(0.5f, 1f);
				obj2.RotationSpeed = 0f;
			}
			break;
		}
		case FireworksBlock.Shape.Disc:
		{
			float f = m_random.Float(0f, (float)Math.PI * 2f);
			int f2 = 13;
			for (int m = 0; m <= f2; m++)
			{
				float num10 = (float)m / (float)f2;
				int num11 = (int)MathUtils.Round(num10 * 2f * (float)f2);
				for (int n = 0; n < num11; n++)
				{
					float x4 = (float)Math.PI * 2f * (float)n / (float)num11 + f;
					Vector3 vector4 = new Vector3(num10 * MathUtils.Sin(x4) + 0.1f * m_random.Float(-1f, 1f), 0f, num10 * MathUtils.Cos(x4) + 0.1f * m_random.Float(-1f, 1f));
					Particle obj4 = Particles[m_nextParticle++];
					obj4.IsActive = true;
					obj4.Position = position;
					obj4.Size = new Vector2(0.2f * particleSize);
					obj4.TimeToLive = m_random.Float(1f, 3f);
					obj4.Velocity = 22f * vector4;
					obj4.TextureSlot = m_random.Int(0, 3);
					obj4.FadeRate = m_random.Float(1f, 3f);
					obj4.BaseColor = m_color * m_random.Float(0.5f, 1f);
					obj4.RotationSpeed = 0f;
				}
			}
			break;
		}
		case FireworksBlock.Shape.Ball:
		{
			float num12 = m_random.Float(0f, (float)Math.PI * 2f);
			int num13 = 12;
			Vector3 vector5 = default;
			for (int num14 = 0; num14 <= num13; num14++)
			{
				float x5 = (float)Math.PI * (float)num14 / (float)num13;
				vector5.Y = MathUtils.Cos(x5);
				float num15 = MathUtils.Sin(x5);
				int num16 = (int)MathUtils.Round(num15 * 2f * (float)num13);
				for (int num17 = 0; num17 < num16; num17++)
				{
					float x6 = (float)Math.PI * 2f * (float)num17 / (float)num16 + num12;
					vector5.X = num15 * MathUtils.Sin(x6);
					vector5.Z = num15 * MathUtils.Cos(x6);
					Particle obj5 = Particles[m_nextParticle++];
					obj5.IsActive = true;
					obj5.Position = position;
					obj5.Size = new Vector2(0.2f * particleSize);
					obj5.TimeToLive = m_random.Float(1f, 3f);
					obj5.Velocity = 20f * vector5;
					obj5.TextureSlot = m_random.Int(0, 3);
					obj5.FadeRate = m_random.Float(1f, 3f);
					obj5.BaseColor = m_color * m_random.Float(0.5f, 1f);
					obj5.RotationSpeed = 0f;
				}
			}
			break;
		}
		case FireworksBlock.Shape.ShortTrails:
		case FireworksBlock.Shape.LongTrails:
		{
			float num6 = m_random.Float(0f, (float)Math.PI * 2f);
			int num7 = 3;
			Vector3 vector3 = default;
			for (int k = 0; k <= num7; k++)
			{
				float x2 = (float)Math.PI * (float)k / (float)num7;
				float num8 = MathUtils.Sin(x2);
				int num9 = (int)MathUtils.Round(num8 * (float)((shape == FireworksBlock.Shape.ShortTrails) ? 3 : 2) * (float)num7);
				for (int l = 0; l < num9; l++)
				{
					float x3 = (float)Math.PI * 2f * (float)l / (float)num9 + num6;
					vector3.X = num8 * MathUtils.Sin(x3) + 0.3f * m_random.Float(-1f, 1f);
					vector3.Y = MathUtils.Cos(x2) + 0.3f * m_random.Float(-1f, 1f);
					vector3.Z = num8 * MathUtils.Cos(x3) + 0.3f * m_random.Float(-1f, 1f);
					Particle obj3 = Particles[m_nextParticle++];
					obj3.IsActive = true;
					obj3.Position = position;
					obj3.Size = new Vector2(0.25f);
					obj3.TimeToLive = m_random.Float(0.5f, 2.5f);
					obj3.Velocity = ((shape == FireworksBlock.Shape.ShortTrails) ? (25f * vector3) : (35f * vector3));
					obj3.TextureSlot = m_random.Int(0, 3);
					obj3.FadeRate = m_random.Float(1f, 3f);
					obj3.BaseColor = m_color * m_random.Float(0.5f, 1f);
					obj3.GenerationFrequency = ((shape == FireworksBlock.Shape.ShortTrails) ? 1.9f : 2.1f);
					obj3.RotationSpeed = m_random.Float(-40f, 40f);
				}
			}
			break;
		}
		case FireworksBlock.Shape.FlatTrails:
		{
			float num3 = m_random.Float(0f, (float)Math.PI * 2f);
			int num4 = 13;
			for (int i = 0; i < num4; i++)
			{
				float x = (float)Math.PI * 2f * (float)i / (float)num4 + num3;
				Vector3 vector = new Vector3(MathUtils.Sin(x) + 0.1f * m_random.Float(-1f, 1f), 0f, MathUtils.Cos(x) + 0.1f * m_random.Float(-1f, 1f));
				Particle obj = Particles[m_nextParticle++];
				obj.IsActive = true;
				obj.Position = position;
				obj.Size = new Vector2(0.25f);
				obj.TimeToLive = m_random.Float(0.5f, 2.5f);
				obj.Velocity = 25f * vector;
				obj.TextureSlot = m_random.Int(0, 3);
				obj.FadeRate = m_random.Float(1f, 3f);
				obj.BaseColor = m_color * m_random.Float(0.5f, 1f);
				obj.GenerationFrequency = 2.5f;
				obj.RotationSpeed = m_random.Float(-40f, 40f);
			}
			break;
		}
		}
	}

	public override bool Simulate(float dt)
	{
		dt = MathUtils.Clamp(dt, 0f, 0.1f);
		float num = MathUtils.Pow(0.01f, dt);
		float num2 = MathUtils.Pow(0.1f, dt);
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
				Vector3 position = (particle.Position += particle.Velocity * dt);
				particle.Velocity.Y += -9.81f * dt;
				particle.Velocity *= (particle.HighDamping ? num : num2);
				particle.Color = particle.BaseColor * MathUtils.Min(particle.FadeRate * particle.TimeToLive, 1f);
				particle.Rotation += particle.RotationSpeed * dt;
				if (!particle.HighDamping && m_random.Float(0f, 1f) < m_flickering)
				{
					particle.Color = Color.Transparent;
				}
				if (m_random.Float(0f, 1f) < 20f * dt)
				{
					particle.TextureSlot = m_random.Int(0, 3);
				}
				if (particle.GenerationFrequency > 0f)
				{
					float num3 = particle.Velocity.Length();
					particle.GenerationAccumulator += particle.GenerationFrequency * num3 * dt;
					if (particle.GenerationAccumulator > 1f && m_nextParticle < Particles.Length)
					{
						particle.GenerationAccumulator--;
						Particle obj = Particles[m_nextParticle++];
						obj.IsActive = true;
						obj.Position = position;
						obj.Size = new Vector2(0.2f);
						obj.TimeToLive = 1f;
						obj.TextureSlot = m_random.Int(0, 3);
						obj.FadeRate = 1f;
						obj.BaseColor = particle.BaseColor;
						obj.HighDamping = true;
						obj.RotationSpeed = 0f;
					}
				}
			}
			else
			{
				particle.IsActive = false;
			}
		}
		return !flag;
	}
}
