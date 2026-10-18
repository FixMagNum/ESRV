using Engine;
using Engine.Graphics;

namespace Game;

public class SpeedFlybyParticleSystem : ParticleSystem<SpeedFlybyParticleSystem.Particle>
{
	public class Particle : Game.Particle
	{
		public float Time;

		public float TimeToLive;

		public float TotalLife;

		public Color BaseRgb;

		public float DepthAlpha;
	}

	public Vector3 AnchorPosition;

	public Vector3 MovementDir;

	public Vector3 SpawnAreaHalfSize;

	public float SpawnFrontOffset;

	public float SpawnRatePerSecond;

	public float ParticleSize = 0.32f;

	public float MaxVisibilityDistance = 120f;

	public Vector2 LifetimeMinMax = new Vector2(0.22f, 0.5f);

	public Random m_random = new Random();

	public float m_toGenerate;

	public float m_age;

	public bool m_visible;

	public int TextureRowMin;

	public int TextureRowMax;

	public int TextureColMin;

	public int TextureColMax;

	public Color Tint;

	public bool UseEnvironmentLighting;

	public float MinLightFactor;

	private SubsystemTerrain m_subsystemTerrain;

	private Texture2D m_textureAtlas;

	private Texture2D m_textureUnderwater;

	private SubsystemSky m_subsystemSky;

	private bool m_underwaterMode;

	public bool UnderwaterMode
	{
		get
		{
			return m_underwaterMode;
		}
		set
		{
			if (m_underwaterMode != value)
			{
				m_underwaterMode = value;
				ApplyModeTexture();
			}
		}
	}

	public override bool Simulate(float dt)
	{
		dt = MathUtils.Clamp(dt, 0f, 0.1f);
		m_age += dt;
		m_toGenerate += SpawnRatePerSecond * dt;
		Vector3 text = MovementDir;
		if (text.LengthSquared() < 1E-06f)
		{
			text = Vector3.UnitZ;
		}
		text = Vector3.Normalize(text);
		Vector3 color = Vector3.UnitY;
		Vector3 v = Vector3.Cross(text, color);
		if (v.LengthSquared() < 1E-06f)
		{
			v = Vector3.UnitX;
		}
		v = Vector3.Normalize(v);
		Vector3 vector = Vector3.Normalize(Vector3.Cross(v, text));
		Vector3 vector2 = AnchorPosition + text * SpawnFrontOffset;
		float num = (m_underwaterMode ? 0.75f : 1f);
		for (int i = 0; i < Particles.Length; i++)
		{
			Particle particle = Particles[i];
			if (particle.IsActive)
			{
				particle.Time += dt;
				particle.TimeToLive -= dt;
				if (particle.TimeToLive <= 0f)
				{
					particle.IsActive = false;
				}
				else
				{
					float num2 = MathUtils.Saturate(particle.TimeToLive / particle.TotalLife);
					float num3 = (m_underwaterMode ? particle.DepthAlpha : 1f);
					byte a = (byte)(255f * num2 * num3 * num);
					particle.Color = Color.PremultiplyAlpha(new Color(particle.BaseRgb, a));
				}
				Particles[i] = particle;
			}
			else
			{
				if (!(m_toGenerate >= 1f))
				{
					continue;
				}
				Vector3 position;
				if (m_underwaterMode)
				{
					float num4 = m_random.Float(-1f, 1f) * SpawnAreaHalfSize.X;
					float num5 = m_random.Float(-1f, 1f) * SpawnAreaHalfSize.Y;
					float num6 = m_random.Float(0f, 1f) * SpawnAreaHalfSize.Z;
					position = vector2 + v * num4 + vector * num5 + text * num6;
				}
				else
				{
					float num7 = m_random.Float(-1f, 1f) * SpawnAreaHalfSize.X;
					float num8 = m_random.Float(-1f, 1f) * SpawnAreaHalfSize.Y;
					position = vector2 + v * num7 + vector * num8;
				}
				float num9 = 1f;
				Color baseRgb;
				if (m_underwaterMode)
				{
					int x = Terrain.ToCell(position.X);
					int y = Terrain.ToCell(position.Y);
					int z = Terrain.ToCell(position.Z);
					Color color2 = BlockColorsMap.WaterColorsMap.Lookup(m_subsystemTerrain.Terrain, x, y, z);
					float num10 = 0.35f;
					byte r = (byte)MathUtils.Clamp((float)(int)color2.R + (float)(255 - color2.R) * num10, 0f, 255f);
					byte g = (byte)MathUtils.Clamp((float)(int)color2.G + (float)(255 - color2.G) * num10, 0f, 255f);
					byte b = (byte)MathUtils.Clamp((float)(int)color2.B + (float)(255 - color2.B) * num10, 0f, 255f);
					baseRgb = new Color(r, g, b);
					float num11 = MathUtils.Max(1f, m_subsystemSky.m_viewFogRange.Y);
					float num12 = MathUtils.Saturate(m_subsystemSky.ViewUnderWaterDepth / num11);
					num9 = 1f - num12;
				}
				else
				{
					float num13 = 1f;
					if (UseEnvironmentLighting)
					{
						int x2 = Terrain.ToCell(position.X);
						int y2 = Terrain.ToCell(position.Y);
						int z2 = Terrain.ToCell(position.Z);
						int cellLight = m_subsystemTerrain.Terrain.GetCellLight(x2, y2, z2);
						num13 = MathUtils.Lerp(MinLightFactor, 1f, (float)cellLight / 15f);
					}
					byte r2 = (byte)MathUtils.Clamp((float)(int)Tint.R * num13, 0f, 255f);
					byte g2 = (byte)MathUtils.Clamp((float)(int)Tint.G * num13, 0f, 255f);
					byte b2 = (byte)MathUtils.Clamp((float)(int)Tint.B * num13, 0f, 255f);
					baseRgb = new Color(r2, g2, b2);
				}
				particle.IsActive = true;
				particle.Position = position;
				float v2 = ParticleSize * m_random.Float(0.75f, 1.25f);
				particle.Size = new Vector2(v2);
				if (m_underwaterMode)
				{
					particle.TextureSlot = 0;
				}
				else
				{
					int num14 = m_random.Int(TextureRowMin, TextureRowMax);
					int num15 = m_random.Int(TextureColMin, TextureColMax);
					particle.TextureSlot = num14 * 4 + num15;
				}
				particle.FlipX = m_random.Int(0, 1) == 0;
				particle.FlipY = m_random.Int(0, 1) == 0;
				particle.TotalLife = m_random.Float(LifetimeMinMax.X, LifetimeMinMax.Y);
				particle.TimeToLive = particle.TotalLife;
				particle.Time = 0f;
				particle.BaseRgb = baseRgb;
				particle.DepthAlpha = num9;
				particle.Color = Color.PremultiplyAlpha(new Color(particle.BaseRgb, (byte)(255f * num * num9)));
				m_toGenerate--;
				Particles[i] = particle;
			}
		}
		m_toGenerate = MathUtils.Remainder(m_toGenerate, 1f);
		m_visible = false;
		return false;
	}

	public override void Draw(Camera camera)
	{
		float num = Vector3.Dot(AnchorPosition - camera.ViewPosition, camera.ViewDirection);
		if (num > -0.5f && num <= MaxVisibilityDistance && Vector3.DistanceSquared(AnchorPosition, camera.ViewPosition) <= MaxVisibilityDistance * MaxVisibilityDistance)
		{
			m_visible = true;
			base.Draw(camera);
		}
	}

	public void SetTextureRange(int rowMin, int rowMax, int colMin, int colMax)
	{
		if (m_underwaterMode)
		{
			TextureRowMin = (TextureRowMax = 0);
			TextureColMin = (TextureColMax = 0);
			return;
		}
		TextureRowMin = MathUtils.Clamp(rowMin, 0, 3);
		TextureRowMax = MathUtils.Clamp(rowMax, 0, 3);
		TextureColMin = MathUtils.Clamp(colMin, 0, 3);
		TextureColMax = MathUtils.Clamp(colMax, 0, 3);
		if (TextureRowMax < TextureRowMin)
		{
			TextureRowMax = TextureRowMin;
		}
		if (TextureColMax < TextureColMin)
		{
			TextureColMax = TextureColMin;
		}
	}

	public SpeedFlybyParticleSystem(SubsystemTerrain terrain)
		: base(192)
	{
		m_subsystemTerrain = terrain;
		m_subsystemSky = terrain.Project.FindSubsystem<SubsystemSky>(throwOnError: true);
		m_textureAtlas = ContentManager.Get<Texture2D>("Textures/SpeedFlyby");
		m_textureUnderwater = ContentManager.Get<Texture2D>("Textures/UnderwaterSpeedFlyby");
		TextureRowMin = 0;
		TextureRowMax = 3;
		TextureColMin = 0;
		TextureColMax = 3;
		UseEnvironmentLighting = true;
		MinLightFactor = 0.2f;
		Tint = Color.White;
		m_underwaterMode = false;
		LifetimeMinMax = new Vector2(0.6f, 1.2f);
		ApplyModeTexture();
	}

	private void ApplyModeTexture()
	{
		if (m_underwaterMode)
		{
			Texture = m_textureUnderwater;
			TextureSlotsCount = 1;
			TextureRowMin = (TextureRowMax = 0);
			TextureColMin = (TextureColMax = 0);
			return;
		}
		Texture = m_textureAtlas;
		TextureSlotsCount = 4;
		TextureRowMin = MathUtils.Clamp(TextureRowMin, 0, 3);
		TextureRowMax = MathUtils.Clamp(TextureRowMax, 0, 3);
		TextureColMin = MathUtils.Clamp(TextureColMin, 0, 3);
		TextureColMax = MathUtils.Clamp(TextureColMax, 0, 3);
		if (TextureRowMax < TextureRowMin)
		{
			TextureRowMax = TextureRowMin;
		}
		if (TextureColMax < TextureColMin)
		{
			TextureColMax = TextureColMin;
		}
	}
}
