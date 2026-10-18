using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class CycloneWindStreakParticleSystem : ParticleSystemBase
{
	public class Particle
	{
		public bool IsActive;

		public Vector3 Position;

		public Vector3 PreviousPosition;

		public Vector3 SpawnPosition;

		public Vector3 Velocity;

		public float Speed;

		public float Length;

		public float Width;

		public float Shade;

		public float Age;

		public float Duration;

		public bool IsCollided;

		public bool FlipTexture;
	}

	public Point2 Point;

	public int MinParticles = 2;

	public int MaxParticles = 12;

	public float MinLength = 6f;

	public float MaxLength = 14f;

	public float Width = 0.35f;

	private readonly GameWidget m_gameWidget;

	private readonly SubsystemWeather m_weather;

	private readonly Random m_random;

	private List<Particle> m_particles;

	private int m_activeCount;

	private float m_toCreate;

	private bool m_isEmpty;

	private float m_intensity;

	private Texture2D m_texture;

	private TexturedBatch3D m_batch;

	public CycloneWindStreakParticleSystem(GameWidget gameWidget, SubsystemWeather weather, Random random, Point2 cell)
	{
		m_gameWidget = gameWidget;
		m_weather = weather;
		m_random = random;
		Point = cell;
		m_texture = ContentManager.Get<Texture2D>("Textures/Weather/WindStreakLong");
		m_batch = null;
		m_particles = new List<Particle>(MaxParticles);
		for (int i = 0; i < MaxParticles; i++)
		{
			m_particles.Add(new Particle());
		}
		m_isEmpty = true;
		m_toCreate = m_random.Float(0f, 1f);
	}

	public override bool Simulate(float dt)
	{
		if (!SettingsManager.DisplayWindParticles)
		{
			return true;
		}
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
		float currentWindStrength = m_weather.CurrentWindStrength;
		float x = 0f;
		if (currentWindStrength > 0.1f)
		{
			if (currentWindStrength <= 8f)
			{
				x = MathUtils.Saturate(currentWindStrength / 8f) * 0.35f;
			}
			else
			{
				x = ((!(currentWindStrength <= 12f)) ? (0.7f + MathUtils.Saturate((currentWindStrength - 12f) / 4f) * 0.3f) : (0.35f + MathUtils.Saturate((currentWindStrength - 8f) / 4f) * 0.35f));
			}
		}
		x = MathUtils.Saturate(x);
		float num = x * x;
		float minLength = MinLength;
		float maxLength = MaxLength;
		float width = Width;
		float num2 = 20f;
		float num3 = 38f;
		float x2 = 0.5f;
		float x3 = 0.4f;
		float num4 = 4f;
		bool isCycloneActive = m_weather.IsCycloneActive;
		float num5 = 0f;
		if (isCycloneActive)
		{
			Vector2 v = new Vector2((float)Point.X + 0.5f, (float)Point.Y + 0.5f);
			Vector2 cycloneCenter = m_weather.CycloneCenter;
			float num6 = MathUtils.Max(1f, m_weather.CycloneRadius);
			if (Vector2.Distance(v, cycloneCenter) <= num6)
			{
				num5 = 1f;
			}
		}
		if (x <= 0f)
		{
			m_intensity = 0f;
		}
		else
		{
			float x4 = num;
			if (num5 > 0f)
			{
				x = 1f;
				x4 = 1f;
			}
			m_intensity = MathUtils.Saturate(x4);
		}
		Vector2 vector = m_weather.CurrentWindVector;
		if (vector.LengthSquared() < 1E-06f)
		{
			vector = Vector2.UnitY;
		}
		Vector3 vector2 = Vector3.Normalize(new Vector3(vector.X, 0f, vector.Y));
		float y = activeCamera.ViewPosition.Y;
		float num7 = y - 10f;
		float num8 = y + 10f;
		float num9 = (float)Terrain.ExtractTopHeight(m_weather.SubsystemTerrain.Terrain.GetShaftValue(Point.X, Point.Y)) + 1f;
		if (num7 < num9)
		{
			num7 = num9;
		}
		if (num8 < num7 + 1f)
		{
			num8 = num7 + 1f;
		}
		int num10 = 0;
		if (m_intensity > 0f)
		{
			float x5 = MinParticles;
			float x6 = MaxParticles;
			num10 = (int)MathUtils.Floor(MathUtils.Lerp(x5, x6, m_intensity) + 0.5f);
			num10 = MathUtils.Clamp(num10, MinParticles, MaxParticles);
		}
		if (num10 < m_activeCount)
		{
			for (int i = num10; i < m_activeCount; i++)
			{
				m_particles[i].IsActive = false;
			}
		}
		m_activeCount = num10;
		m_toCreate += (float)m_activeCount * m_intensity * dt;
		m_isEmpty = true;
		for (int j = 0; j < m_activeCount; j++)
		{
			Particle particle = m_particles[j];
			if (particle.IsActive)
			{
				particle.Age += dt;
				Vector3 position = particle.Position + particle.Velocity * dt;
				int x7 = Terrain.ToCell(position.X);
				int y2 = Terrain.ToCell(position.Y);
				int z = Terrain.ToCell(position.Z);
				int cellContents = m_weather.SubsystemTerrain.Terrain.GetCellContents(x7, y2, z);
				Block block = BlocksManager.Blocks[cellContents];
				if (block != null && block.IsCollidable)
				{
					if (!particle.IsCollided)
					{
						particle.IsCollided = true;
						particle.Duration = particle.Age + 1f;
						particle.Velocity = Vector3.Zero;
					}
				}
				else
				{
					particle.PreviousPosition = particle.Position;
					particle.Position = position;
					if ((particle.Position - particle.SpawnPosition).Length() > particle.Length * num4 || particle.Position.Y < num7 || particle.Position.Y > num8)
					{
						particle.IsActive = false;
					}
					else
					{
						m_isEmpty = false;
					}
				}
				m_particles[j] = particle;
			}
			else if (m_toCreate >= 1f && m_intensity > 0f)
			{
				particle.IsActive = true;
				particle.FlipTexture = m_random.Bool();
				float num11 = ((x > 0f) ? MathUtils.Lerp(x3, 1f, x) : 0f);
				float f = ((x > 0f) ? MathUtils.Lerp(x2, 1f, x) : 0f);
				float num12 = ((x > 0f) ? MathUtils.Lerp(0.11f, 1f, x) : 0f);
				particle.Shade = m_random.Float(0.4f, 1f);
				particle.Length = MathUtils.Lerp(minLength, maxLength, f) * m_random.Float(0.5f, 1.3f);
				particle.Width = width * num11 * m_random.Float(0.7f, 1.3f);
				particle.Speed = m_random.Float(num2 * num12 * 0.7f, num3 * num12 * 1.2f);
				Vector3 vector3 = Vector3.Normalize(vector2 + new Vector3(m_random.Float(-0.03f, 0.03f), m_random.Float(-0.03f, 0.03f), m_random.Float(-0.03f, 0.03f)));
				particle.Velocity = vector3 * particle.Speed;
				particle.Age = m_random.Float(0f, 0.3f);
				particle.Duration = m_random.Float(2f, 5f);
				particle.IsCollided = false;
				particle.Position.X = (float)Point.X + m_random.Float(-0.5f, 1.5f);
				particle.Position.Z = (float)Point.Y + m_random.Float(-0.5f, 1.5f);
				float num13 = m_random.Float(0f, 1f) + m_random.Float(0f, 1f) - 1f;
				float x8 = y + num13 * 20f;
				x8 = MathUtils.Clamp(x8, num7, num8);
				particle.Position.Y = x8;
				particle.SpawnPosition = particle.Position;
				m_toCreate--;
				m_isEmpty = false;
				m_particles[j] = particle;
			}
		}
		m_toCreate = MathUtils.Remainder(m_toCreate, 1f);
		return false;
	}

	public override void Draw(Camera camera)
	{
		if (m_isEmpty || camera.GameWidget != m_gameWidget || !SettingsManager.DisplayWindParticles)
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
			m_batch = SubsystemParticles.PrimitivesRenderer.TexturedBatch(m_texture, useAlphaTest: false, 0, DepthStencilState.DepthRead, null, BlendState.AlphaBlend, SamplerState.LinearClamp);
		}
		bool flag = !SettingsManager.RenderWeatherPrecipitationOutsideView;
		Vector3 viewPosition = camera.ViewPosition;
		Vector3 viewDirection = camera.ViewDirection;
		float num2 = MathUtils.Clamp(m_weather.SubsystemSky.SkyLightIntensity, 0.2f, 1f);
		float f = MathUtils.Saturate(m_weather.GlobalPrecipitationIntensity);
		float num3 = 5f;
		float num4 = 18f;
		Vector2 v = new Vector2(viewPosition.X, viewPosition.Z);
		Vector2 vector = m_weather.CurrentWindVector;
		if (vector.LengthSquared() < 1E-06f)
		{
			vector = Vector2.UnitY;
		}
		Vector3 vector2 = Vector3.Normalize(new Vector3(vector.X, 0f, vector.Y));
		Vector3 unitY = Vector3.UnitY;
		bool isCycloneActive = m_weather.IsCycloneActive;
		Vector2 cycloneCenter = m_weather.CycloneCenter;
		float cycloneRadius = m_weather.CycloneRadius;
		float num5 = cycloneRadius * cycloneRadius;
		for (int i = 0; i < m_activeCount; i++)
		{
			Particle particle = m_particles[i];
			if (!particle.IsActive)
			{
				continue;
			}
			if (flag)
			{
				Vector3 v2 = particle.Position - viewPosition;
				if (Vector3.Dot(viewDirection, v2) <= 0f)
				{
					continue;
				}
			}
			Vector2 v3 = new Vector2(particle.Position.X, particle.Position.Z);
			float num6 = Vector2.Distance(v, v3);
			float num7 = MathUtils.Saturate((num6 - num3) / (num4 - num3));
			float num8 = 1f - MathUtils.Saturate((num6 - (num4 - 2f)) / 2f);
			float num9 = num7 * num8;
			if (!(num9 > 0f))
			{
				continue;
			}
			bool flag2 = false;
			if (isCycloneActive)
			{
				float num10 = v3.X - cycloneCenter.X;
				float num11 = v3.Y - cycloneCenter.Y;
				flag2 = num10 * num10 + num11 * num11 <= num5;
			}
			float num12 = num * m_intensity * num9;
			if (flag2)
			{
				num12 = MathUtils.Max(num12, num * num9 * 0.85f);
			}
			if (num12 > 0f)
			{
				Vector3 vector3 = vector2 * (particle.Length * 0.5f);
				Vector3 vector4 = unitY * (particle.Width * 0.5f);
				Vector3 p = particle.Position - vector3 - vector4;
				Vector3 p2 = particle.Position + vector3 - vector4;
				Vector3 p3 = particle.Position + vector3 + vector4;
				Vector3 p4 = particle.Position - vector3 + vector4;
				float num13;
				if (SettingsManager.SkyLightUseParticles)
				{
					int x = Terrain.ToCell(particle.Position.X);
					int y = Terrain.ToCell(particle.Position.Y);
					int z = Terrain.ToCell(particle.Position.Z);
					int cellLight = m_weather.SubsystemTerrain.Terrain.GetCellLight(x, y, z);
					num13 = LightingManager.LightIntensityByLightValue[cellLight];
				}
				else
				{
					num13 = num2;
				}
				float num14 = particle.Shade * num13;
				num14 *= MathUtils.Lerp(1.1f, 0.7f, f);
				if (flag2)
				{
					num14 *= MathUtils.Lerp(0.9f, 0.6f, f);
				}
				num14 = MathUtils.Clamp(num14, 0f, 1f);
				float num15 = MathUtils.Saturate(particle.Age / 1.5f);
				float num16 = MathUtils.Saturate((particle.Duration - particle.Age) / 1.5f);
				float num17 = num15 * num16;
				byte a = (byte)MathUtils.Clamp((int)(num12 * 50f * num17), 0, 50);
				byte b = (byte)MathUtils.Clamp((int)(num14 * 255f), 0, 255);
				byte b2 = b;
				Color color = Color.PremultiplyAlpha(new Color(b, b2, b2, a));
				if (particle.FlipTexture)
				{
					m_batch.QueueQuad(p, p2, p3, p4, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(0f, 0f), color);
				}
				else
				{
					m_batch.QueueQuad(p, p2, p3, p4, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f), color);
				}
			}
		}
	}
}
