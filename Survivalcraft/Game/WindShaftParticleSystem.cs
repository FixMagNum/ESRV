using System;
using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class WindShaftParticleSystem : ParticleSystemBase
{
	public class Particle
	{
		public bool IsActive;

		public byte TextureSlot;

		public Vector3 Position;

		public Vector3 PreviousPosition;

		public Vector3 Velocity;

		public Vector2 TexCoord1;

		public Vector2 TexCoord2;

		public Vector2 TexCoord3;

		public Vector2 TexCoord4;

		public float Speed;

		public float DeviationAngle;
	}

	public float SnowAccelerationPerLevel;

	private const int m_maxParticles = 10;

	private readonly List<Particle> m_particles;

	private readonly GameWidget m_gameWidget;

	private readonly SubsystemWeather m_subsystemWeather;

	private readonly Random m_random;

	private TexturedBatch3D m_batch;

	private Texture2D m_texture;

	private Vector2 m_size;

	private float m_intensity;

	private float m_toCreate;

	private float m_averageSpeed;

	private int m_activeParticlesCount;

	private float minSpeed;

	private float maxSpeed;

	private bool m_isEmpty;

	private readonly int[] m_allowedTextureSlots;

	public static readonly int[] s_defaultModerateSlots;

	public int MinParticlesPerShaft = 1;

	public int MaxParticlesPerShaft = 10;

	public float BreezeStrength = 2f;

	public float ModerateStrength = 6f;

	public float StrongStrength = 10f;

	public float StormStrength = 16f;

	public float SizeWindMultiplier = 2f;

	public Point2 Point { get; set; }

	public WindShaftParticleSystem(GameWidget gameWidget, SubsystemWeather subsystemWeather, Random random, Point2 point, int[] allowedTextureSlots)
	{
		SnowAccelerationPerLevel = 1f;
		m_gameWidget = gameWidget;
		m_subsystemWeather = subsystemWeather;
		m_random = random;
		Point = point;
		m_allowedTextureSlots = ((allowedTextureSlots != null && allowedTextureSlots.Length != 0) ? allowedTextureSlots : s_defaultModerateSlots);
		m_particles = new List<Particle>(MaxParticlesPerShaft);
		for (int i = 0; i < MaxParticlesPerShaft; i++)
		{
			m_particles.Add(new Particle());
		}
		m_activeParticlesCount = MinParticlesPerShaft;
		m_texture = ContentManager.Get<Texture2D>("Textures/Weather/WindParticle");
		m_size = new Vector2(0.12f, 0.12f);
		minSpeed = 0.5f;
		maxSpeed = 3f;
		m_averageSpeed = (minSpeed + maxSpeed) / 2f;
		m_toCreate = m_random.Float(0f, 0.9f);
	}

	public override bool Simulate(float dt)
	{
		if (SettingsManager.StagePrecipitation >= 1f)
		{
			return true;
		}
		Camera activeCamera = m_gameWidget.ActiveCamera;
		if (activeCamera == null)
		{
			return false;
		}
		dt = MathUtils.Clamp(dt, 0f, 0.1f);
		float currentWindStrength = m_subsystemWeather.CurrentWindStrength;
		if (m_subsystemWeather.m_lightningIntensity >= 0.8f || currentWindStrength <= 0.01f)
		{
			m_intensity = 0f;
			UpdateParticleCount(0f);
		}
		else
		{
			float f = UpdateParticleCount(currentWindStrength);
			m_intensity = MathUtils.Lerp(0.25f, 1f, f);
		}
		Vector2 vector = m_subsystemWeather.CurrentWindVector;
		if (vector.LengthSquared() < 1E-06f)
		{
			vector = Vector2.UnitY;
		}
		Vector3 vector2 = Vector3.Normalize(new Vector3(vector.X, 0f, vector.Y));
		float y = activeCamera.ViewPosition.Y;
		float num = y - 15f;
		float num2 = y + 15f;
		float num3 = (float)Terrain.ExtractTopHeight(m_subsystemWeather.SubsystemTerrain.Terrain.GetShaftValue(Point.X, Point.Y)) + 1f;
		if (num < num3)
		{
			num = num3;
		}
		if (num2 < num + 1f)
		{
			num2 = num + 1f;
		}
		float particleAngle = GetParticleAngle(currentWindStrength);
		float num4 = 1f;
		int num5 = (int)currentWindStrength - 12;
		if (num5 > 0)
		{
			num4 = 15f + SnowAccelerationPerLevel * (float)num5;
		}
		m_toCreate += (float)m_activeParticlesCount * m_intensity * dt;
		m_isEmpty = true;
		Vector3 vector3 = vector2;
		Vector3 unitY = Vector3.UnitY;
		for (int i = 0; i < m_activeParticlesCount; i++)
		{
			Particle particle = m_particles[i];
			if (particle.IsActive)
			{
				float num6 = particleAngle + particle.DeviationAngle;
				float num7 = (float)Math.Cos(num6);
				float num8 = (float)Math.Sin(num6);
				Vector3 v = vector3 * num7 + unitY * (0f - num8);
				v = ((!(v.LengthSquared() < 1E-06f)) ? Vector3.Normalize(v) : vector3);
				particle.Velocity = v * (particle.Speed * num4);
				particle.PreviousPosition = particle.Position;
				particle.Position += particle.Velocity * dt;
				if (particle.Position.Y < num || particle.Position.Y > num2)
				{
					particle.IsActive = false;
				}
				else
				{
					m_isEmpty = false;
				}
				m_particles[i] = particle;
			}
			else if (m_toCreate >= 1f && m_intensity > 0f)
			{
				particle.IsActive = true;
				particle.Speed = m_random.Float(minSpeed, maxSpeed);
				particle.DeviationAngle = MathUtils.DegToRad(m_random.Float(-10f, 10f));
				particle.Position.X = (float)Point.X + m_random.Float(0f, 1f);
				particle.Position.Y = m_random.Float(num, num2);
				particle.Position.Z = (float)Point.Y + m_random.Float(0f, 1f);
				particle.PreviousPosition = particle.Position;
				int num9 = ((m_allowedTextureSlots == null || m_allowedTextureSlots.Length == 0) ? m_random.Int(0, 15) : m_allowedTextureSlots[m_random.Int(0, m_allowedTextureSlots.Length - 1)]);
				particle.TextureSlot = (byte)num9;
				float num10 = 0.25f;
				int num11 = num9 % 4;
				int num12 = num9 / 4;
				Vector2 vector4 = (particle.TexCoord1 = new Vector2((float)num11 * num10, (float)num12 * num10));
				particle.TexCoord2 = vector4 + new Vector2(num10, 0f);
				particle.TexCoord3 = vector4 + new Vector2(num10, num10);
				particle.TexCoord4 = vector4 + new Vector2(0f, num10);
				m_toCreate--;
				m_isEmpty = false;
				m_particles[i] = particle;
			}
		}
		m_toCreate = MathUtils.Remainder(m_toCreate, 1f);
		return false;
	}

	public override void Draw(Camera camera)
	{
		if (m_isEmpty || camera.GameWidget != m_gameWidget)
		{
			return;
		}
		float num = 1f - SettingsManager.StagePrecipitation;
		if (num <= 0f)
		{
			return;
		}
		if (m_batch == null)
		{
			m_batch = SubsystemParticles.PrimitivesRenderer.TexturedBatch(m_texture, useAlphaTest: false, 0, DepthStencilState.DepthRead, null, BlendState.AlphaBlend, SamplerState.PointClamp);
		}
		float num2 = camera.ViewPosition.Y + 10f;
		Vector3 viewDirection = camera.ViewDirection;
		Vector3 vector = Vector3.Normalize(Vector3.Cross(viewDirection, Vector3.UnitY));
		Vector3 vector2 = Vector3.Normalize(Vector3.Cross(vector, viewDirection));
		float num3 = MathUtils.Lerp(f: MathUtils.Saturate((m_subsystemWeather.CurrentWindStrength - ModerateStrength) / MathUtils.Max(0.001f, StormStrength - ModerateStrength)), x1: 1f, x2: SizeWindMultiplier);
		Vector3 vector3 = vector * (m_size.X * num3);
		Vector3 vector4 = vector2 * (m_size.Y * num3);
		Color snowColor = m_subsystemWeather.SnowColor;
		for (int i = 0; i < m_activeParticlesCount; i++)
		{
			Particle particle = m_particles[i];
			if (particle.IsActive)
			{
				Vector3 vector5 = new Vector3(0f, -1f, 0f);
				Vector3 v = ((particle.Velocity.LengthSquared() > 1E-06f) ? Vector3.Normalize(particle.Velocity) : vector5);
				Vector3 v2 = Vector3.Cross(vector5, v);
				float angle = (float)Math.Acos(Vector3.Dot(vector5, v));
				if (v2.LengthSquared() < 1E-06f)
				{
					v2 = Vector3.UnitX;
					angle = ((!(Vector3.Dot(vector5, v) < 0f)) ? 0f : ((float)Math.PI));
				}
				else
				{
					v2 = Vector3.Normalize(v2);
				}
				Matrix m = Matrix.CreateFromAxisAngle(v2, angle);
				Vector3 vector6 = Vector3.Transform(-vector3 - vector4, m);
				Vector3 vector7 = Vector3.Transform(vector3 - vector4, m);
				Vector3 vector8 = Vector3.Transform(vector3 + vector4, m);
				Vector3 vector9 = Vector3.Transform(-vector3 + vector4, m);
				Vector3 p = particle.Position + vector6;
				Vector3 p2 = particle.Position + vector7;
				Vector3 p3 = particle.Position + vector8;
				Vector3 p4 = particle.Position + vector9;
				float num4 = MathUtils.Min(0.6f * (num2 - particle.Position.Y), 1f);
				Color color = snowColor * (num4 * num);
				m_batch.QueueQuad(p, p2, p3, p4, particle.TexCoord1, particle.TexCoord2, particle.TexCoord3, particle.TexCoord4, color);
			}
		}
	}

	private float GetParticleAngle(float windStrength)
	{
		float x = 5f;
		float f = MathUtils.Saturate(windStrength / 16f);
		return MathUtils.DegToRad(MathUtils.Lerp(25f, x, f));
	}

	static WindShaftParticleSystem()
	{
		s_defaultModerateSlots = new int[4] { 0, 1, 2, 3 };
	}

	private float UpdateParticleCount(float windStrength)
	{
		float x;
		if (windStrength <= BreezeStrength)
		{
			x = MathUtils.Lerp(0.1f, 0.25f, (BreezeStrength > 0f) ? (windStrength / BreezeStrength) : 0f);
		}
		else if (windStrength <= ModerateStrength)
		{
			x = MathUtils.Lerp(0.25f, 0.5f, (windStrength - BreezeStrength) / MathUtils.Max(0.001f, ModerateStrength - BreezeStrength));
		}
		else
		{
			x = ((!(windStrength <= StrongStrength)) ? MathUtils.Lerp(0.75f, 1f, MathUtils.Saturate((windStrength - StrongStrength) / MathUtils.Max(0.001f, StormStrength - StrongStrength))) : MathUtils.Lerp(0.5f, 0.75f, (windStrength - ModerateStrength) / MathUtils.Max(0.001f, StrongStrength - ModerateStrength)));
		}
		x = MathUtils.Saturate(x);
		int x2 = (int)MathUtils.Lerp(MinParticlesPerShaft, MaxParticlesPerShaft, x);
		x2 = MathUtils.Clamp(x2, MinParticlesPerShaft, MaxParticlesPerShaft);
		if (x2 != m_activeParticlesCount)
		{
			if (x2 < m_activeParticlesCount)
			{
				for (int i = x2; i < m_activeParticlesCount; i++)
				{
					m_particles[i].IsActive = false;
				}
			}
			m_activeParticlesCount = x2;
		}
		if (x2 != m_activeParticlesCount)
		{
			if (x2 < m_activeParticlesCount)
			{
				for (int j = x2; j < m_activeParticlesCount; j++)
				{
					m_particles[j].IsActive = false;
				}
			}
			m_activeParticlesCount = x2;
		}
		return x;
	}
}
