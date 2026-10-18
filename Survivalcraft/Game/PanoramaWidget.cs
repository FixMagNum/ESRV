using System;
using System.Collections.Generic;
using Engine;
using Engine.Audio;
using Engine.Graphics;

namespace Game;

public class PanoramaWidget : Widget
{
	private enum PanoramaBaseStyle
	{
		Rainy,
		Snowy,
		Desert,
		Oceanic,
		Cloudy,
		Embrace
	}

	private enum RainVariant
	{
		Drizzle,
		Normal,
		Thunder,
		Storm
	}

	private enum SnowVariant
	{
		Flurry,
		Normal,
		Thunder,
		Storm
	}

	private enum SandVariant
	{
		Drift,
		Normal,
		Thunder,
		Storm
	}

	private enum CloudVariant
	{
		Light,
		Medium,
		Storm
	}

	private enum Mode
	{
		Rain,
		Snow,
		Sand,
		Bubbles
	}

	private struct Particle2D
	{
		public Vector2 Pos;

		public Vector2 Vel;

		public float SizeX;

		public float SizeY;

		public float Rot;

		public float Life;

		public float MaxLife;

		public byte Tile;

		public float Alpha;

		public byte Layer;

		public byte TexKind;
	}

	private class CloudLayer
	{
		public float Speed;

		public float Scale;

		public Vector2 Offset;

		public Color Color;
	}

	private enum WindVariant
	{
		None,
		Left,
		Right
	}

	private enum BubbleVariant
	{
		Drizzle,
		Normal,
		Thunder
	}

	private enum FireflyMode
	{
		Fireflies,
		Flies,
		Both
	}

	private float timeOffset;

	private Vector2 position;

	private float movementRange;

	private Vector2 direction;

	private float changeDirectionTime;

	private float directionTimer;

	private Texture2D recipeTexture;

	private static bool s_initialized;

	private static bool s_isLightThemeFrozen;

	private static PanoramaBaseStyle s_currentStyle;

	private static double s_styleSince;

	private const double STYLE_DURATION = 180.0;

	private static string s_selectedTextureKey;

	private static Texture2D s_selectedTexture;

	private static Random s_rand;

	private static Texture2D s_texRainDrizzle;

	private static Texture2D s_texRain;

	private static Texture2D s_texRainStorm;

	private static Texture2D s_texSnow;

	private static Texture2D s_texSnowStorm;

	private static Texture2D s_texSand;

	private static Texture2D s_texSandStorm;

	private static Texture2D s_texBubbles;

	private static Texture2D s_texCloudTile;

	private static int s_styleGen;

	private static RainVariant s_rainV;

	private static SnowVariant s_snowV;

	private static SandVariant s_sandV;

	private static CloudVariant s_cloudV;

	private int m_appliedStyleGen = -1;

	private readonly List<Particle2D> m_particles = new List<Particle2D>(2048);

	private float m_spawnCarry;

	private readonly List<CloudLayer> m_cloudLayers = new List<CloudLayer>(3);

	private static readonly Color CLOUD_LIGHT;

	private static readonly Color CLOUD_MEDIUM;

	private static readonly Color CLOUD_STORM;

	private static Texture2D s_texFireflies;

	private static WindVariant s_wind;

	private int m_sandSide;

	private float m_noEffectTimer;

	private Screen m_prevScreen;

	private bool m_prevSpecial;

	private static Texture2D s_texFlies;

	private static BubbleVariant s_bubbleV;

	private static FireflyMode s_fireflyMode;

	private static float s_fliesSizeMul;

	private static bool s_audioInit;

	private static double s_nextThunderTime;

	private static readonly float s_ambienceVolume;

	private static readonly float s_fadeSpeed;

	private static readonly Color WATER_TINT;

	private static Sound s_loopRain;

	private static Sound s_loopStormRain;

	private static Sound s_loopWaterWind;

	private static Sound s_loopLightWarmWind;

	private static Sound s_loopAverageWarmWind;

	private static Sound s_loopStormWarmWind;

	private static Sound s_loopHurricaneWarmWind;

	private static Sound s_loopLightColdWind;

	private static Sound s_loopAverageColdWind;

	private static Sound s_loopStormColdWind;

	private static Sound s_loopHurricaneColdWind;

	private int m_cloudSide;

	private bool m_textureErrorLogged;

	private bool m_useSimplePanorama;

	private bool m_simpleIsLightTheme;

	private static PanoramaBaseStyle s_loadedStyle;

	private static bool s_resourcesLoaded;

	private static PanoramaBaseStyle s_lastLoadedStyle;

	private static string s_loadedVariantKey;

	private static Dictionary<string, List<string>> s_textureCache;

	public Texture2D Texture { get; set; }

	public PanoramaWidget()
	{
		m_useSimplePanorama = !SettingsManager.NewPanoramaEnabled;
		if (m_useSimplePanorama)
		{
			UnloadAllResources();
			LoadSimpleResources();
			GC.Collect();
		}
		else
		{
			EnsureSessionInitialized();
			EnsureResourcesForCurrentStyle();
			direction = Vector2.Normalize(new Vector2(s_rand.Float(-1f, 1f), s_rand.Float(-1f, 1f)));
			changeDirectionTime = s_rand.Float(5f, 10f);
			directionTimer = 0f;
			ApplyStyleGenIfNeeded(force: true);
		}
		timeOffset = s_rand.Float(0f, 1000f);
		position = Vector2.Zero;
		movementRange = 0.05f;
		m_prevScreen = ScreensManager.CurrentScreen;
		m_prevSpecial = IsSpecialScreenOpen();
	}

	private void DrawImage(DrawContext dc)
	{
		if (IsSpecialScreenOpen())
		{
			DrawRecipeImage(dc);
		}
		else
		{
			DrawStandardImage(dc);
		}
	}

	public override void MeasureOverride(Vector2 parentAvailableSize)
	{
		IsDrawRequired = true;
	}

	public override void Draw(DrawContext dc)
	{
		if (m_useSimplePanorama)
		{
			if (IsSpecialScreenOpen())
			{
				DrawRecipeImage(dc);
			}
			else
			{
				DrawSimpleImage(dc);
			}
			DrawSquares(dc);
			return;
		}
		if (IsSpecialScreenOpen())
		{
			DrawRecipeImage(dc);
		}
		else
		{
			DrawStandardImage(dc);
		}
		if (!IsSpecialScreenOpen())
		{
			DrawEffects(dc);
			FlatBatch2D flatBatch2D = dc.PrimitivesRenderer2D.FlatBatch(0, DepthStencilState.None, null, BlendState.AlphaBlend);
			int num = flatBatch2D.TriangleVertices.Count;
			flatBatch2D.QueueQuad(color: new Color(0f, 0f, 0f, s_isLightThemeFrozen ? 0.04f : 0.16f), corner1: Vector2.Zero, corner2: ActualSize, depth: 0f);
			flatBatch2D.TransformTriangles(GlobalTransform, num);
		}
		else
		{
			DrawSquares(dc);
		}
	}

	public override void Update()
	{
		bool terrain = !SettingsManager.NewPanoramaEnabled;
		if (terrain != m_useSimplePanorama)
		{
			SwitchPanoramaMode(terrain);
		}
		if (m_useSimplePanorama)
		{
			SimpleUpdate();
			return;
		}
		if (s_currentStyle != s_loadedStyle)
		{
			EnsureResourcesForCurrentStyle();
		}
		Screen random = ScreensManager.CurrentScreen;
		bool i = IsSpecialScreen(random);
		bool j = IsSpecialScreen(m_prevScreen);
		if (random != m_prevScreen || i != m_prevSpecial)
		{
			if (j | i)
			{
				ResetEffectsState(i);
			}
			else if (!i)
			{
				Texture = s_selectedTexture;
			}
			m_prevScreen = random;
			m_prevSpecial = i;
		}
		UpdateSessionRotationIfNeeded();
		ApplyStyleGenIfNeeded(force: false);
		if (IsSpecialScreenOpen())
		{
			UpdateAmbience(0f);
			return;
		}
		directionTimer += Time.FrameDuration;
		if (directionTimer >= changeDirectionTime)
		{
			direction = Vector2.Normalize(new Vector2(s_rand.Float(-1f, 1f), s_rand.Float(-1f, 1f)));
			changeDirectionTime = s_rand.Float(5f, 10f);
			directionTimer = 0f;
		}
		float num = 0.0025f * Time.FrameDuration;
		position += direction * num;
		position.X = MathUtils.Clamp(position.X, 0f - movementRange, movementRange);
		position.Y = MathUtils.Clamp(position.Y, 0f - movementRange, movementRange);
		UpdateEffects(Time.FrameDuration);
		GuardEffectsLiveness(Time.FrameDuration);
		UpdateAmbience(Time.FrameDuration);
	}

	private void DrawSquares(DrawContext dc)
	{
		FlatBatch2D x = dc.PrimitivesRenderer2D.FlatBatch(1, DepthStencilState.None, null, BlendState.AlphaBlend);
		int y = x.LineVertices.Count;
		int i = x.TriangleVertices.Count;
		float j = (float)MathUtils.Remainder(Time.FrameStartTime + (double)timeOffset, 10000.0);
		float random = ActualSize.X / 12f;
		float num = (float)(int)GlobalColorTransform.A / 255f;
		float num2 = 0.7f * j;
		float num3 = 0.5f * j;
		for (float num5 = 0f; num5 < ActualSize.X; num5 += random)
		{
			for (float num6 = 0f; num6 < ActualSize.Y; num6 += random)
			{
				float num8 = 0.35f * MathUtils.Pow(MathUtils.Saturate(SimplexNoise.OctavedNoise(num5 + 1000f, num6, num2, 0.5f, 1, 2f, 1f) - 0.1f), 1f) * num;
				if (num8 > 0.01f)
				{
					Vector2 num12 = new Vector2(num5, num6);
					Vector2 k = new Vector2(num5 + random, num6 + random);
					x.QueueRectangle(num12, k, 0f, new Color(0f, 0f, 0f, num8));
				}
				float num13 = 0.7f * MathUtils.Pow(SimplexNoise.OctavedNoise(num5, num6, num3, 0.5f, 1, 2f, 1f), 3f) * num;
				if (num13 > 0.01f)
				{
					Vector2 l = new Vector2(num5, num6);
					Vector2 x2 = new Vector2(num5 + random, num6 + random);
					x.QueueQuad(l, x2, 0f, new Color(0f, 0f, 0f, num13));
				}
			}
		}
		x.TransformLines(GlobalTransform, y);
		x.TransformTriangles(GlobalTransform, i);
	}

	private void DrawStandardImage(DrawContext dc)
	{
		if (Texture == null)
		{
			if (!m_textureErrorLogged)
			{
				LogPanoramaTextureError("DrawStandardImage", s_selectedTextureKey);
				m_textureErrorLogged = true;
			}
			return;
		}
		float i = 1.15f;
		Vector2 j = ActualSize * i;
		Vector2 num = (ActualSize - j) / 2f;
		Vector2 num2 = new Vector2(position.X, position.Y);
		Vector2 random = new Vector2(position.X + 1f, position.Y + 1f);
		TexturedBatch2D texturedBatch2D = dc.PrimitivesRenderer2D.TexturedBatch(Texture, useAlphaTest: false, 0, DepthStencilState.None, null, BlendState.AlphaBlend, SamplerState.LinearWrap);
		int num3 = texturedBatch2D.TriangleVertices.Count;
		texturedBatch2D.QueueQuad(num, num + j, 0f, num2, random, Color.White);
		texturedBatch2D.TransformTriangles(GlobalTransform, num3);
	}

	private void DrawRecipeImage(DrawContext dc)
	{
		if (recipeTexture == null)
		{
			string num3 = ((SettingsManager.ThemeMode == ThemeMode.Light || SettingsManager.ThemeMode == ThemeMode.LightTransparent) ? "Textures/Gui/LightRecepaediaPanorama" : "Textures/Gui/DarkRecepaediaPanorama");
			try
			{
				recipeTexture = ContentManager.Get<Texture2D>(num3);
			}
			catch (Exception)
			{
				if (!m_textureErrorLogged)
				{
					LogPanoramaTextureError("DrawRecipeImage", num3);
					m_textureErrorLogged = true;
				}
				return;
			}
		}
		float list = (float)MathUtils.Remainder(Time.FrameStartTime + (double)timeOffset, 10000.0);
		float x = 2f * SimplexNoise.OctavedNoise(list, 0.02f, 4, 2f, 0.5f) - 1f;
		float y = 2f * SimplexNoise.OctavedNoise(list + 100f, 0.02f, 4, 2f, 0.5f) - 1f;
		position += 0.03f * new Vector2(x, y) * MathUtils.Min(Time.FrameDuration, 0.1f);
		position.X = MathUtils.Remainder(position.X, 1f);
		position.Y = MathUtils.Remainder(position.Y, 1f);
		float i = 0.5f * MathUtils.PowSign(MathUtils.Sin(0.21f * list + 2f), 2f) + 0.5f;
		float j = MathUtils.Lerp(0.13f, 0.3f, i);
		float random = j / (float)recipeTexture.Height * (float)recipeTexture.Width / ActualSize.X * ActualSize.Y;
		Vector2 num4 = new Vector2(position.X - j, position.Y - random);
		Vector2 num5 = new Vector2(position.X + j, position.Y + random);
		TexturedBatch2D texturedBatch2D = dc.PrimitivesRenderer2D.TexturedBatch(recipeTexture, useAlphaTest: false, 0, DepthStencilState.DepthWrite, null, BlendState.AlphaBlend, SamplerState.LinearWrap);
		int probability = texturedBatch2D.TriangleVertices.Count;
		texturedBatch2D.QueueQuad(Vector2.Zero, ActualSize, 1f, num4, num5, GlobalColorTransform);
		texturedBatch2D.TransformTriangles(GlobalTransform, probability);
	}

	private bool IsSpecialScreenOpen()
	{
		if (!(ScreensManager.CurrentScreen is RecipaediaRecipesScreen) && !(ScreensManager.CurrentScreen is RecipaediaScreen) && !(ScreensManager.CurrentScreen is RecipaediaDescriptionScreen) && !(ScreensManager.CurrentScreen is BestiaryScreen) && !(ScreensManager.CurrentScreen is BestiaryDescriptionScreen) && !(ScreensManager.CurrentScreen is AchievementsScreen))
		{
			return ScreensManager.CurrentScreen is AchievementDescriptionScreen;
		}
		return true;
	}

	private static void EnsureSessionInitialized()
	{
		if (!s_initialized)
		{
			s_isLightThemeFrozen = SettingsManager.ThemeMode == ThemeMode.Light || SettingsManager.ThemeMode == ThemeMode.LightTransparent;
			s_currentStyle = (PanoramaBaseStyle)MathUtils.Floor(s_rand.Float(0f, 6f));
			s_styleSince = Time.FrameStartTime;
			PickNewStyleImage(assignSub: true);
			s_initialized = true;
		}
	}

	private static void UpdateSessionRotationIfNeeded()
	{
		if (s_initialized && Time.FrameStartTime - s_styleSince >= 180.0)
		{
			s_currentStyle = (PanoramaBaseStyle)MathUtils.Floor(s_rand.Float(0f, 6f));
			s_styleSince = Time.FrameStartTime;
			PickNewStyleImage(assignSub: true);
		}
	}

	private static string GetStyleFolderName(PanoramaBaseStyle style)
	{
		return style switch
		{
			PanoramaBaseStyle.Rainy => "RainyPanorama", 
			PanoramaBaseStyle.Snowy => "SnowyPanorama", 
			PanoramaBaseStyle.Desert => "DesertPanorama", 
			PanoramaBaseStyle.Oceanic => "OceanPanorama", 
			PanoramaBaseStyle.Cloudy => "CloudyPanorama", 
			PanoramaBaseStyle.Embrace => "EmbracePanorama", 
			_ => "OceanPanorama", 
		};
	}

	static PanoramaWidget()
	{
		s_textureCache = new Dictionary<string, List<string>>();
		s_lastLoadedStyle = (PanoramaBaseStyle)(-1);
		s_loadedStyle = (PanoramaBaseStyle)(-1);
		s_resourcesLoaded = false;
		s_ambienceVolume = 0.45f;
		s_fadeSpeed = 1.25f;
		WATER_TINT = new Color(0.05f, 0.3f, 0.4f, 1f);
		s_fliesSizeMul = 0.23f;
		s_rand = new Random();
		CLOUD_LIGHT = new Color(1f, 1f, 1f, 1f);
		CLOUD_MEDIUM = new Color(0.75f, 0.75f, 0.78f, 1f);
		CLOUD_STORM = new Color(0.35f, 0.38f, 0.4f, 1f);
		ScreensManager.OnEnterScreen += OnEnterScreen;
	}

	private void UpdateEffects(float dt)
	{
		switch (s_currentStyle)
		{
		case PanoramaBaseStyle.Rainy:
			UpdatePrecipitation(dt, Mode.Rain);
			break;
		case PanoramaBaseStyle.Snowy:
			UpdatePrecipitation(dt, Mode.Snow);
			break;
		case PanoramaBaseStyle.Desert:
			UpdatePrecipitation(dt, Mode.Sand);
			break;
		case PanoramaBaseStyle.Oceanic:
			UpdateBubbles(dt);
			break;
		case PanoramaBaseStyle.Cloudy:
			UpdateClouds(dt);
			break;
		case PanoramaBaseStyle.Embrace:
			UpdateFireflies(dt);
			break;
		}
	}

	private void DrawEffects(DrawContext dc)
	{
		if (s_resourcesLoaded)
		{
			switch (s_currentStyle)
			{
			case PanoramaBaseStyle.Rainy:
				DrawPrecipitation(dc, Mode.Rain);
				break;
			case PanoramaBaseStyle.Snowy:
				DrawPrecipitation(dc, Mode.Snow);
				break;
			case PanoramaBaseStyle.Desert:
				DrawPrecipitation(dc, Mode.Sand);
				break;
			case PanoramaBaseStyle.Oceanic:
				DrawBubbles(dc);
				break;
			case PanoramaBaseStyle.Cloudy:
				DrawClouds(dc);
				break;
			case PanoramaBaseStyle.Embrace:
				DrawFireflies(dc);
				break;
			}
		}
	}

	private void UpdatePrecipitation(float dt, Mode mode)
	{
		float x = ActualSize.X * ActualSize.Y / 5000000f;
		if (x < 0.2f)
		{
			x = 0.2f;
		}
		float y;
		float random;
		float i;
		float num;
		float num2;
		float num3;
		bool num4;
		switch (mode)
		{
		case Mode.Rain:
			if (s_rainV == RainVariant.Drizzle)
			{
				y = 300f;
				random = 780f;
				i = 120f;
				num = 0f;
				num2 = 4f;
				num3 = 48f;
				num4 = false;
			}
			else if (s_rainV == RainVariant.Normal)
			{
				y = 900f;
				random = 1200f;
				i = 160f;
				num = 0f;
				num2 = 4.4f;
				num3 = 56f;
				num4 = false;
			}
			else if (s_rainV == RainVariant.Thunder)
			{
				y = 1200f;
				random = 1350f;
				i = 170f;
				num = 0f;
				num2 = 4.6f;
				num3 = 60f;
				num4 = false;
			}
			else
			{
				y = 1600f;
				random = 1500f;
				i = 200f;
				num = 0f;
				num2 = 116f;
				num3 = 116f;
				num4 = false;
			}
			break;
		case Mode.Snow:
			if (s_snowV == SnowVariant.Flurry)
			{
				y = 250f;
				random = 70f;
				i = 25f;
				num = 10f;
				num2 = 8f;
				num3 = 8f;
				num4 = true;
			}
			else if (s_snowV == SnowVariant.Normal)
			{
				y = 550f;
				random = 95f;
				i = 35f;
				num = 15f;
				num2 = 9.6f;
				num3 = 9.6f;
				num4 = true;
			}
			else if (s_snowV == SnowVariant.Thunder)
			{
				y = 750f;
				random = 190f;
				i = 40f;
				num = 15f;
				num2 = 9.6f;
				num3 = 9.6f;
				num4 = true;
			}
			else
			{
				y = 1100f;
				random = 485f;
				i = 55f;
				num = 18f;
				num2 = 252f;
				num3 = 252f;
				num4 = false;
			}
			break;
		default:
			if (s_sandV == SandVariant.Drift)
			{
				y = 300f;
				random = 130f;
				i = 40f;
				num = 10f;
				num2 = 14f;
				num3 = 14f;
				num4 = true;
			}
			else if (s_sandV == SandVariant.Normal)
			{
				y = 700f;
				random = 180f;
				i = 60f;
				num = 12f;
				num2 = 16f;
				num3 = 16f;
				num4 = true;
			}
			else if (s_sandV == SandVariant.Thunder)
			{
				y = 950f;
				random = 360f;
				i = 70f;
				num = 12f;
				num2 = 16f;
				num3 = 16f;
				num4 = true;
			}
			else
			{
				y = 1300f;
				random = 740f;
				i = 90f;
				num = 15f;
				num2 = 162f;
				num3 = 162f;
				num4 = false;
			}
			break;
		}
		float flag = y * x * dt + m_spawnCarry;
		int num5 = (int)flag;
		m_spawnCarry = flag - (float)num5;
		float j = ActualSize.X;
		float x2 = ActualSize.Y;
		if (j <= 1f || x2 <= 1f)
		{
			num5 = 0;
		}
		Vector2 z = new Vector2(j * 0.5f, x2 * 0.5f);
		float num6 = MathUtils.Sqrt(j * j + x2 * x2);
		float num7 = 0f;
		if (s_wind == WindVariant.Left)
		{
			num7 = MathUtils.DegToRad(45f);
		}
		else if (s_wind == WindVariant.Right)
		{
			num7 = MathUtils.DegToRad(-45f);
		}
		int num8 = 0;
		if (mode == Mode.Sand)
		{
			if (s_wind == WindVariant.Left)
			{
				num8 = 1;
			}
			else if (s_wind == WindVariant.Right)
			{
				num8 = -1;
			}
			else
			{
				if (m_sandSide == 0)
				{
					m_sandSide = ((s_rand.Float(0f, 1f) < 0.5f) ? 1 : (-1));
				}
				num8 = m_sandSide;
			}
		}
		int x3 = num5;
		int k = num5;
		for (int l = 0; l < 2; l++)
		{
			bool flag2 = l == 1;
			int num9 = (flag2 ? k : x3);
			for (int m = 0; m < num9; m++)
			{
				float num10 = random + s_rand.Float(0f - i, i);
				if (num10 < 5f)
				{
					num10 = 5f;
				}
				Vector2 vector;
				switch (mode)
				{
				case Mode.Rain:
				{
					float x6 = num7;
					vector = new Vector2(MathUtils.Sin(x6), MathUtils.Cos(x6));
					break;
				}
				case Mode.Snow:
				{
					float num11 = MathUtils.DegToRad(s_rand.Float(0f - num, num));
					float x5 = num7 + num11;
					vector = new Vector2(MathUtils.Sin(x5), MathUtils.Cos(x5));
					break;
				}
				default:
				{
					float x4 = MathUtils.DegToRad(num);
					vector = new Vector2((float)num8 * MathUtils.Cos(x4), MathUtils.Sin(x4));
					break;
				}
				}
				float num12 = MathUtils.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
				if (num12 > 1E-06f)
				{
					vector *= 1f / num12;
				}
				else
				{
					vector = new Vector2(0f, 1f);
				}
				Vector2 vector2 = new Vector2(0f - vector.Y, vector.X);
				float num13 = MathUtils.Abs(vector2.X) * j + MathUtils.Abs(vector2.Y) * x2;
				float num14 = MathUtils.Max(num2, num3) + 24f;
				float num15 = 0.5f * num13 + num14;
				float num16 = 0.65f * num6;
				float num17 = 0.65f * num6;
				float num18 = s_rand.Float(0f - num15, num15);
				Particle2D item = new Particle2D
				{
					Pos = z - vector * num16 + vector2 * num18
				};
				float num19 = (flag2 ? 0.55f : 1f);
				float num20 = (flag2 ? 0.75f : 1f);
				float alpha = (flag2 ? 0.55f : 1f);
				float num21 = num2 * num19;
				float num22 = num3 * num19;
				if (!flag2 && s_rand.Float(0f, 1f) < 0.02f)
				{
					float num23 = s_rand.Float(1.3f, 1.8f);
					num21 *= num23;
					num22 *= num23;
				}
				item.Vel = vector * (num10 * num20);
				item.Rot = MathUtils.Atan2(item.Vel.Y, item.Vel.X);
				item.SizeX = num21;
				item.SizeY = num22;
				item.Alpha = alpha;
				item.Layer = (byte)(flag2 ? 1u : 0u);
				if (num4)
				{
					item.Tile = (byte)s_rand.Int(0, 15);
				}
				float num24 = num16 + num6 + num17;
				item.MaxLife = (item.Life = num24 / (num10 * num20));
				m_particles.Add(item);
			}
		}
		for (int num25 = m_particles.Count - 1; num25 >= 0; num25--)
		{
			Particle2D value = m_particles[num25];
			value.Pos += value.Vel * dt;
			if (mode == Mode.Snow && (s_snowV == SnowVariant.Thunder || s_snowV == SnowVariant.Storm))
			{
				value.Pos.X = value.Pos.X + MathUtils.Sin(7f * (float)Time.FrameStartTime + value.Pos.Y * 0.01f) * 8f * dt;
			}
			value.Life -= dt;
			bool flag3 = value.Pos.X < -1.5f * j || value.Pos.X > 2.5f * j || value.Pos.Y < -1.5f * x2 || value.Pos.Y > 2.5f * x2;
			if ((value.Life <= 0f) | flag3)
			{
				m_particles[num25] = m_particles[m_particles.Count - 1];
				m_particles.RemoveAt(m_particles.Count - 1);
			}
			else
			{
				m_particles[num25] = value;
			}
		}
	}

	private void DrawPrecipitation(DrawContext dc, Mode mode)
	{
		bool random = false;
		Texture2D num;
		switch (mode)
		{
		case Mode.Rain:
			if (s_rainV != RainVariant.Drizzle)
			{
				num = ((s_rainV != RainVariant.Storm) ? s_texRain : s_texRainStorm);
			}
			else
			{
				num = s_texRainDrizzle;
			}
			break;
		case Mode.Snow:
			if (s_snowV == SnowVariant.Storm)
			{
				num = s_texSnowStorm;
				break;
			}
			num = s_texSnow;
			random = true;
			break;
		default:
			return;
		case Mode.Sand:
			if (s_sandV == SandVariant.Storm)
			{
				num = s_texSandStorm;
				break;
			}
			num = s_texSand;
			random = true;
			break;
		}
		if (num == null)
		{
			return;
		}
		SamplerState i = (((mode == Mode.Snow && s_snowV == SnowVariant.Storm) || (mode == Mode.Sand && s_sandV == SandVariant.Storm)) ? SamplerState.PointClamp : SamplerState.LinearClamp);
		TexturedBatch2D num2 = dc.PrimitivesRenderer2D.TexturedBatch(num, useAlphaTest: false, 0, DepthStencilState.None, null, BlendState.AlphaBlend, i);
		int num3 = num2.TriangleVertices.Count;
		bool flag = mode == Mode.Rain;
		bool temperatureFast = flag && s_rainV == RainVariant.Storm;
		bool num4 = flag && !temperatureFast;
		for (int j = 0; j < m_particles.Count; j++)
		{
			Particle2D num5 = m_particles[j];
			Vector2 num6 = new Vector2(num5.SizeX * 0.5f, num5.SizeY * 0.5f);
			float num7 = MathUtils.Cos(num5.Rot);
			float num8 = MathUtils.Sin(num5.Rot);
			Vector2 num9 = new Vector2(0f - num8, num7) * num6.X;
			Vector2 k = new Vector2(num7, num8) * num6.Y;
			Vector2 face = num5.Pos - num9 - k;
			Vector2 point = num5.Pos + num9 - k;
			Vector2 num10 = num5.Pos + num9 + k;
			Vector2 value = num5.Pos - num9 + k;
			Color color = Color.White * (s_isLightThemeFrozen ? 0.96f : 0.88f);
			color *= num5.Alpha;
			if (num4)
			{
				Vector2 p = (face + point) * 0.5f;
				num2.QueueTriangle(value, num10, p, 0f, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0f), color);
			}
			else if (random)
			{
				byte tile = num5.Tile;
				int num11 = tile % 4;
				int num12 = tile / 4;
				Vector2 texCoord = new Vector2((float)num11 * 0.25f, (float)num12 * 0.25f);
				Vector2 texCoord2 = new Vector2((float)num11 * 0.25f, (float)num12 * 0.25f + 0.25f);
				Vector2 texCoord3 = new Vector2((float)num11 * 0.25f + 0.25f, (float)num12 * 0.25f + 0.25f);
				Vector2 texCoord4 = new Vector2((float)num11 * 0.25f + 0.25f, (float)num12 * 0.25f);
				num2.QueueQuad(face, point, num10, value, 0f, texCoord, texCoord2, texCoord3, texCoord4, color);
			}
			else
			{
				num2.QueueQuad(face, point, num10, value, 0f, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f), color);
			}
		}
		num2.TransformTriangles(GlobalTransform, num3);
	}

	private void UpdateBubbles(float dt)
	{
		float x = ActualSize.X * ActualSize.Y / 1000000f;
		float y;
		float random;
		float num;
		float i;
		float num2;
		if (s_bubbleV == BubbleVariant.Drizzle)
		{
			y = 80f;
			random = 45f;
			num = 70f;
			i = 8f;
			num2 = 26f;
		}
		else if (s_bubbleV == BubbleVariant.Normal)
		{
			y = 160f;
			random = 50f;
			num = 80f;
			i = 10f;
			num2 = 34f;
		}
		else
		{
			y = 300f;
			random = 55f;
			num = 90f;
			i = 12f;
			num2 = 42f;
		}
		float num3 = y * x * dt + m_spawnCarry;
		int num4 = (int)num3;
		m_spawnCarry = num3 - (float)num4;
		int num5 = num4;
		int flag = num4;
		for (int j = 0; j < 2; j++)
		{
			bool num7 = j == 1;
			int j2 = (num7 ? flag : num5);
			for (int k = 0; k < j2; k++)
			{
				Particle2D num8 = new Particle2D
				{
					Pos = 
					{
						X = s_rand.Float(0f, ActualSize.X),
						Y = ActualSize.Y + 10f
					}
				};
				float num9 = s_rand.Float(random, num);
				float x2 = MathUtils.DegToRad(s_rand.Float(-15f, 15f));
				num8.Vel = new Vector2(num9 * MathUtils.Sin(x2) * 0.35f, 0f - num9);
				if (s_wind == WindVariant.Left)
				{
					num8.Vel.X = num8.Vel.X + num9 * 0.15f;
				}
				if (s_wind == WindVariant.Right)
				{
					num8.Vel.X = num8.Vel.X - num9 * 0.15f;
				}
				float num10 = s_rand.Float(i, num2);
				if (num7)
				{
					num10 *= 0.55f;
					num8.Vel *= 0.75f;
					num8.Alpha = 0.55f;
					num8.Layer = 1;
				}
				else
				{
					if (s_rand.Float(0f, 1f) < 0.02f)
					{
						num10 *= s_rand.Float(1.4f, 2f);
					}
					num8.Alpha = 1f;
					num8.Layer = 0;
				}
				num8.SizeX = num10;
				num8.SizeY = num10;
				num8.Rot = 0f;
				num8.MaxLife = (num8.Life = s_rand.Float(2f, 4.5f));
				num8.Tile = (byte)s_rand.Int(0, 8);
				m_particles.Add(num8);
			}
		}
		for (int num11 = m_particles.Count - 1; num11 >= 0; num11--)
		{
			Particle2D value = m_particles[num11];
			value.Pos += value.Vel * dt;
			value.Pos.X = value.Pos.X + MathUtils.Sin((float)Time.FrameStartTime * 1.5f + value.Pos.Y * 0.01f) * 20f * dt;
			value.Life -= dt;
			if (value.Pos.Y < -20f || value.Pos.X < -40f || value.Pos.X > ActualSize.X + 40f)
			{
				m_particles[num11] = m_particles[m_particles.Count - 1];
				m_particles.RemoveAt(m_particles.Count - 1);
			}
			else
			{
				m_particles[num11] = value;
			}
		}
	}

	private void DrawBubbles(DrawContext dc)
	{
		if (s_texBubbles != null)
		{
			TexturedBatch2D x = dc.PrimitivesRenderer2D.TexturedBatch(s_texBubbles, useAlphaTest: false, 0, DepthStencilState.None, null, BlendState.AlphaBlend, SamplerState.LinearClamp);
			int y = x.TriangleVertices.Count;
			for (int random = 0; random < m_particles.Count; random++)
			{
				Particle2D num = m_particles[random];
				Vector2 i = new Vector2(num.SizeX * 0.5f, num.SizeY * 0.5f);
				Vector2 num2 = num.Pos - i;
				Vector2 num3 = num.Pos + i;
				int num4 = MathUtils.Clamp(num.Tile, 0, 8);
				int num5 = num4 % 3;
				int flag = num4 / 3;
				Vector2 j = new Vector2((float)num5 * (1f / 3f), (float)flag * (1f / 3f));
				Vector2 num6 = j + new Vector2(1f / 3f, 1f / 3f);
				Color num7 = WATER_TINT;
				num7 *= (s_isLightThemeFrozen ? 0.96f : 0.88f);
				num7 *= num.Alpha;
				x.QueueQuad(num2, num3, 0f, j, num6, num7);
			}
			x.TransformTriangles(GlobalTransform, y);
		}
	}

	private void UpdateClouds(float dt)
	{
		float i = MathUtils.Max(64f, ActualSize.X);
		float j = MathUtils.Max(64f, ActualSize.Y);
		int num = m_cloudSide;
		for (int k = 0; k < m_cloudLayers.Count; k++)
		{
			CloudLayer num3 = m_cloudLayers[k];
			int cellValueFast;
			if (s_wind == WindVariant.Left)
			{
				cellValueFast = -1;
			}
			else if (s_wind == WindVariant.Right)
			{
				cellValueFast = 1;
			}
			else
			{
				if (num == 0)
				{
					num = ((!(s_rand.Float(0f, 1f) < 0.5f)) ? 1 : (-1));
				}
				cellValueFast = num;
			}
			num3.Offset.X = MathUtils.Remainder(num3.Offset.X + (float)cellValueFast * num3.Speed / i * dt, 1f);
			num3.Offset.Y = MathUtils.Remainder(num3.Offset.Y + 0.1f * num3.Speed / j * dt, 1f);
			m_cloudLayers[k] = num3;
		}
		m_cloudSide = num;
	}

	private void DrawClouds(DrawContext dc)
	{
		if (s_texCloudTile != null)
		{
			TexturedBatch2D i = dc.PrimitivesRenderer2D.TexturedBatch(s_texCloudTile, useAlphaTest: false, 0, DepthStencilState.None, null, BlendState.AlphaBlend, SamplerState.LinearWrap);
			int j = i.TriangleVertices.Count;
			for (int num = 0; num < m_cloudLayers.Count; num++)
			{
				CloudLayer num2 = m_cloudLayers[num];
				float num3 = MathUtils.Max(1f, num2.Scale * 0.5f);
				float num4 = num3 * (ActualSize.Y / MathUtils.Max(1f, ActualSize.X));
				Vector2 vector = new Vector2(num2.Offset.X, num2.Offset.Y);
				Vector2 texCoord = vector + new Vector2(num3, num4);
				Color color = num2.Color;
				color *= (s_isLightThemeFrozen ? 0.9f : 0.2f);
				i.QueueQuad(Vector2.Zero, ActualSize, 0f, vector, texCoord, color);
			}
			i.TransformTriangles(GlobalTransform, j);
		}
	}

	private static void PickNewStyleImage(bool assignSub)
	{
		string i = GetStyleFolderName(s_currentStyle);
		string j = (s_isLightThemeFrozen ? "Light" : "Dark");
		List<int> num = new List<int>();
		for (int k = 1; k <= 20; k++)
		{
			num.Add(k);
		}
		bool num2 = false;
		while (num.Count > 0)
		{
			int cellValueFast = s_rand.Int(0, num.Count - 1);
			int num4 = num[cellValueFast];
			num.RemoveAt(cellValueFast);
			string data = $"Textures/Gui/{i}/{j}/{num4}";
			try
			{
				Texture2D texture2D = ContentManager.Get<Texture2D>(data);
				if (texture2D != null)
				{
					s_selectedTextureKey = data;
					s_selectedTexture = texture2D;
					num2 = true;
					break;
				}
			}
			catch
			{
			}
		}
		if (!num2)
		{
			List<int> list = new List<int>();
			for (int l = 1; l <= 25; l++)
			{
				list.Add(l);
			}
			string text = (s_isLightThemeFrozen ? "Textures/Gui/LightPanorama" : "Textures/Gui/DarkPanorama");
			while (list.Count > 0)
			{
				int index = s_rand.Int(0, list.Count - 1);
				int num5 = list[index];
				list.RemoveAt(index);
				string name = text + num5;
				try
				{
					Texture2D texture2D2 = ContentManager.Get<Texture2D>(name);
					if (texture2D2 != null)
					{
						s_selectedTextureKey = name;
						s_selectedTexture = texture2D2;
						num2 = true;
						break;
					}
				}
				catch
				{
				}
			}
		}
		if (!num2)
		{
			Log.Warning("PanoramaWidget: no textures found.");
		}
		if (assignSub)
		{
			AssignSubVariantForCurrentStyle();
		}
		s_styleGen++;
	}

	private static void AssignSubVariantForCurrentStyle()
	{
		float random = s_rand.Float(0f, 1f);
		s_wind = ((random < 0.33f) ? WindVariant.Left : ((random < 0.66f) ? WindVariant.Right : WindVariant.None));
		switch (s_currentStyle)
		{
		case PanoramaBaseStyle.Rainy:
		{
			float array2 = s_rand.Float(0f, 1f);
			if (array2 < 0.35f)
			{
				s_rainV = RainVariant.Drizzle;
			}
			else if (array2 < 0.75f)
			{
				s_rainV = RainVariant.Normal;
			}
			else if (array2 < 0.9f)
			{
				s_rainV = RainVariant.Thunder;
			}
			else
			{
				s_rainV = RainVariant.Storm;
			}
			break;
		}
		case PanoramaBaseStyle.Snowy:
		{
			float i = s_rand.Float(0f, 1f);
			if (i < 0.35f)
			{
				s_snowV = SnowVariant.Flurry;
			}
			else if (i < 0.75f)
			{
				s_snowV = SnowVariant.Normal;
			}
			else if (i < 0.9f)
			{
				s_snowV = SnowVariant.Thunder;
			}
			else
			{
				s_snowV = SnowVariant.Storm;
			}
			break;
		}
		case PanoramaBaseStyle.Desert:
		{
			float terrainBrush = s_rand.Float(0f, 1f);
			if (terrainBrush < 0.35f)
			{
				s_sandV = SandVariant.Drift;
			}
			else if (terrainBrush < 0.75f)
			{
				s_sandV = SandVariant.Normal;
			}
			else if (terrainBrush < 0.9f)
			{
				s_sandV = SandVariant.Thunder;
			}
			else
			{
				s_sandV = SandVariant.Storm;
			}
			break;
		}
		case PanoramaBaseStyle.Oceanic:
		{
			float num = s_rand.Float(0f, 1f);
			if (num < 0.5f)
			{
				s_bubbleV = BubbleVariant.Drizzle;
			}
			else if (num < 0.85f)
			{
				s_bubbleV = BubbleVariant.Normal;
			}
			else
			{
				s_bubbleV = BubbleVariant.Thunder;
			}
			break;
		}
		case PanoramaBaseStyle.Cloudy:
		{
			float j = s_rand.Float(0f, 1f);
			if (j < 0.5f)
			{
				s_cloudV = CloudVariant.Light;
			}
			else if (j < 0.85f)
			{
				s_cloudV = CloudVariant.Medium;
			}
			else
			{
				s_cloudV = CloudVariant.Storm;
			}
			break;
		}
		case PanoramaBaseStyle.Embrace:
		{
			float vector = s_rand.Float(0f, 1f);
			if (vector < 0.34f)
			{
				s_fireflyMode = FireflyMode.Fireflies;
			}
			else if (vector < 0.67f)
			{
				s_fireflyMode = FireflyMode.Flies;
			}
			else
			{
				s_fireflyMode = FireflyMode.Both;
			}
			break;
		}
		}
	}

	private void ApplyStyleGenIfNeeded(bool force)
	{
		if (force || m_appliedStyleGen != s_styleGen)
		{
			if (!IsSpecialScreenOpen())
			{
				Texture = s_selectedTexture;
			}
			m_particles.Clear();
			m_spawnCarry = 0f;
			m_cloudLayers.Clear();
			m_sandSide = 0;
			m_cloudSide = 0;
			m_noEffectTimer = 0f;
			if (s_currentStyle == PanoramaBaseStyle.Cloudy)
			{
				InitCloudLayersForVariant();
			}
			m_appliedStyleGen = s_styleGen;
		}
	}

	private void InitCloudLayersForVariant()
	{
		if (s_cloudV == CloudVariant.Light)
		{
			m_cloudLayers.Add(new CloudLayer
			{
				Speed = 30f,
				Scale = 2.2f,
				Offset = new Vector2(s_rand.Float(0f, 1f), s_rand.Float(0f, 1f)),
				Color = CLOUD_LIGHT
			});
		}
		else if (s_cloudV == CloudVariant.Medium)
		{
			m_cloudLayers.Add(new CloudLayer
			{
				Speed = 60f,
				Scale = 2.4f,
				Offset = new Vector2(s_rand.Float(0f, 1f), s_rand.Float(0f, 1f)),
				Color = CLOUD_MEDIUM
			});
			m_cloudLayers.Add(new CloudLayer
			{
				Speed = 90f,
				Scale = 3f,
				Offset = new Vector2(s_rand.Float(0f, 1f) + 0.37f, s_rand.Float(0f, 1f) + 0.19f),
				Color = CLOUD_MEDIUM * 0.95f
			});
		}
		else
		{
			m_cloudLayers.Add(new CloudLayer
			{
				Speed = 60f,
				Scale = 2.6f,
				Offset = new Vector2(s_rand.Float(0f, 1f), s_rand.Float(0f, 1f)),
				Color = CLOUD_STORM
			});
			m_cloudLayers.Add(new CloudLayer
			{
				Speed = 95f,
				Scale = 3.2f,
				Offset = new Vector2(s_rand.Float(0f, 1f) + 0.31f, s_rand.Float(0f, 1f) + 0.27f),
				Color = CLOUD_STORM * 0.95f
			});
			m_cloudLayers.Add(new CloudLayer
			{
				Speed = 230f,
				Scale = 4.2f,
				Offset = new Vector2(s_rand.Float(0f, 1f) + 0.63f, s_rand.Float(0f, 1f) + 0.54f),
				Color = CLOUD_STORM * 0.9f
			});
		}
	}

	private void UpdateFireflies(float dt)
	{
		float terrain = ActualSize.X * ActualSize.Y / 5000000f;
		if (terrain < 0.2f)
		{
			terrain = 0.2f;
		}
		float x = 220f * terrain * dt + m_spawnCarry;
		int num = (int)x;
		m_spawnCarry = x - (float)num;
		float y = ActualSize.X;
		float num2 = ActualSize.Y;
		if (y <= 1f || num2 <= 1f)
		{
			num = 0;
		}
		Vector2 x2 = new Vector2(y * 0.5f, num2 * 0.5f);
		float y2 = MathUtils.Sqrt(y * y + num2 * num2);
		float i = 0f;
		if (s_wind == WindVariant.Left)
		{
			i = MathUtils.DegToRad(45f);
		}
		else if (s_wind == WindVariant.Right)
		{
			i = MathUtils.DegToRad(-45f);
		}
		int j = num;
		int random = num;
		for (int k = 0; k < 2; k++)
		{
			bool num5 = k == 1;
			int x3 = (num5 ? random : j);
			for (int l = 0; l < x3; l++)
			{
				float k2 = MathUtils.DegToRad(s_rand.Float(-18f, 18f));
				float num7 = (float)Math.PI - i + k2;
				Vector2 num8 = new Vector2(MathUtils.Sin(num7), MathUtils.Cos(num7));
				float num9 = MathUtils.Sqrt(num8.X * num8.X + num8.Y * num8.Y);
				if (num9 > 1E-06f)
				{
					num8 *= 1f / num9;
				}
				else
				{
					num8 = new Vector2(0f, -1f);
				}
				Vector2 num10 = new Vector2(0f - num8.Y, num8.X);
				float num11 = MathUtils.Abs(num10.X) * y + MathUtils.Abs(num10.Y) * num2;
				float num12 = 0.5f * num11 + 24f;
				float value = 0.65f * y2;
				float value2 = 0.65f * y2;
				float l2 = s_rand.Float(0f - num12, num12);
				Particle2D m = new Particle2D
				{
					Pos = x2 - num8 * value + num10 * l2
				};
				float n = s_rand.Float(60f, 110f);
				float num13 = s_rand.Float(6f, 12f) * 2.5f;
				byte num14 = 0;
				if (s_fireflyMode == FireflyMode.Flies)
				{
					num14 = 1;
				}
				else if (s_fireflyMode == FireflyMode.Both)
				{
					num14 = ((!(s_rand.Float(0f, 1f) < 0.5f)) ? ((byte)1) : ((byte)0));
				}
				if (num14 == 1)
				{
					num13 *= s_fliesSizeMul;
				}
				if (num5)
				{
					num13 *= 0.55f;
					n *= 0.75f;
					m.Alpha = 0.55f;
					m.Layer = 1;
				}
				else
				{
					if (num14 != 1 && s_rand.Float(0f, 1f) < 0.02f)
					{
						num13 *= s_rand.Float(1.3f, 1.8f);
					}
					m.Alpha = 1f;
					m.Layer = 0;
				}
				m.Vel = num8 * n;
				m.SizeX = num13;
				m.SizeY = num13;
				m.Rot = 0f;
				m.Tile = (byte)s_rand.Int(0, 8);
				m.TexKind = num14;
				float num15 = value + y2 + value2;
				m.MaxLife = (m.Life = num15 / n);
				m_particles.Add(m);
			}
		}
		for (int num16 = m_particles.Count - 1; num16 >= 0; num16--)
		{
			Particle2D num17 = m_particles[num16];
			num17.Pos += num17.Vel * dt;
			num17.Life -= dt;
			bool num18 = num17.Pos.X < -1.5f * y || num17.Pos.Y < -1.5f * num2 || num17.Pos.X > y * 2.5f || num17.Pos.Y > num2 * 2.5f;
			if ((num17.Life <= 0f) | num18)
			{
				m_particles[num16] = m_particles[m_particles.Count - 1];
				m_particles.RemoveAt(m_particles.Count - 1);
			}
			else
			{
				m_particles[num16] = num17;
			}
		}
	}

	private void DrawFireflies(DrawContext dc)
	{
		if (s_texFireflies == null || s_texFlies == null)
		{
			return;
		}
		TexturedBatch2D terrain = dc.PrimitivesRenderer2D.TexturedBatch(s_texFireflies, useAlphaTest: false, 0, DepthStencilState.None, null, BlendState.AlphaBlend, SamplerState.LinearClamp);
		TexturedBatch2D x = dc.PrimitivesRenderer2D.TexturedBatch(s_texFlies, useAlphaTest: false, 0, DepthStencilState.None, null, BlendState.AlphaBlend, SamplerState.LinearClamp);
		int num = terrain.TriangleVertices.Count;
		int y = x.TriangleVertices.Count;
		for (int i = 0; i < m_particles.Count; i++)
		{
			Particle2D x2 = m_particles[i];
			Vector2 y2 = new Vector2(x2.SizeX * 0.5f, x2.SizeY * 0.5f);
			Vector2 i2 = x2.Pos - y2;
			Vector2 j = x2.Pos + y2;
			Color random = Color.White * (s_isLightThemeFrozen ? 0.96f : 0.88f);
			random *= x2.Alpha;
			if (x2.TexKind == 1)
			{
				x.QueueQuad(i2, j, 0f, new Vector2(0f, 0f), new Vector2(1f, 1f), random);
				continue;
			}
			int num2 = MathUtils.Clamp(x2.Tile, 0, 8);
			int probability = num2 % 3;
			int num3 = num2 / 3;
			Vector2 num4 = new Vector2((float)probability / 3f, (float)num3 / 3f);
			Vector2 num5 = num4 + new Vector2(1f / 3f, 1f / 3f);
			terrain.QueueQuad(i2, j, 0f, num4, num5, random);
		}
		terrain.TransformTriangles(GlobalTransform, num);
		x.TransformTriangles(GlobalTransform, y);
	}

	private void GuardEffectsLiveness(float dt)
	{
		if ((s_currentStyle == PanoramaBaseStyle.Cloudy && m_cloudLayers.Count > 0) || m_particles.Count > 0)
		{
			m_noEffectTimer = 0f;
			return;
		}
		m_noEffectTimer += dt;
		if (m_noEffectTimer >= 0.5f)
		{
			AssignSubVariantForCurrentStyle();
			m_spawnCarry = 0f;
			float i = 0.35f;
			UpdateEffects(i);
			if (s_currentStyle == PanoramaBaseStyle.Cloudy && m_cloudLayers.Count == 0)
			{
				InitCloudLayersForVariant();
			}
			m_noEffectTimer = 0f;
		}
	}

	private void UpdateAmbience(float dt)
	{
		if (!SettingsManager.NewPanoramaEnabled)
		{
			PauseAmbience();
		}
		else
		{
			if (!s_resourcesLoaded)
			{
				return;
			}
			float num = 0f;
			float num2 = 0f;
			float num3 = 0f;
			float num4 = 0f;
			float num5 = 0f;
			float num6 = 0f;
			float num7 = 0f;
			float num8 = 0f;
			float num9 = 0f;
			float num10 = 0f;
			float num11 = 0f;
			bool flag = false;
			double frameStartTime = Time.FrameStartTime;
			switch (s_currentStyle)
			{
			case PanoramaBaseStyle.Rainy:
				if (s_rainV == RainVariant.Storm)
				{
					num2 = 0.5f * s_ambienceVolume;
					flag = true;
					break;
				}
				num = ((s_rainV == RainVariant.Drizzle) ? 0.35f : ((s_rainV == RainVariant.Normal) ? 0.6f : 0.75f)) * s_ambienceVolume;
				if (s_rainV == RainVariant.Thunder)
				{
					flag = true;
				}
				break;
			case PanoramaBaseStyle.Snowy:
				if (s_snowV == SnowVariant.Flurry)
				{
					num8 = 0.35f * s_ambienceVolume;
				}
				else if (s_snowV == SnowVariant.Normal)
				{
					num9 = 0.6f * s_ambienceVolume;
				}
				else if (s_snowV == SnowVariant.Thunder)
				{
					num10 = 0.8f * s_ambienceVolume;
				}
				else
				{
					num11 = 1f * s_ambienceVolume;
				}
				break;
			case PanoramaBaseStyle.Desert:
				if (s_sandV == SandVariant.Drift)
				{
					num4 = 0.35f * s_ambienceVolume;
				}
				else if (s_sandV == SandVariant.Normal)
				{
					num5 = 0.6f * s_ambienceVolume;
				}
				else if (s_sandV == SandVariant.Thunder)
				{
					num6 = 0.8f * s_ambienceVolume;
				}
				else
				{
					num7 = 1f * s_ambienceVolume;
				}
				break;
			case PanoramaBaseStyle.Oceanic:
				num3 = 0.65f * s_ambienceVolume;
				break;
			case PanoramaBaseStyle.Cloudy:
				if (s_cloudV == CloudVariant.Light)
				{
					num4 = 0.35f * s_ambienceVolume;
				}
				else if (s_cloudV == CloudVariant.Medium)
				{
					num5 = 0.55f * s_ambienceVolume;
				}
				else
				{
					num6 = 0.8f * s_ambienceVolume;
				}
				break;
			}
			float num12 = MathUtils.Saturate(SettingsManager.SoundsVolume);
			FadeTo(s_loopRain, num * num12, dt);
			FadeTo(s_loopStormRain, num2 * num12, dt);
			FadeTo(s_loopWaterWind, num3 * num12, dt);
			FadeTo(s_loopLightWarmWind, num4 * num12, dt);
			FadeTo(s_loopAverageWarmWind, num5 * num12, dt);
			FadeTo(s_loopStormWarmWind, num6 * num12, dt);
			FadeTo(s_loopHurricaneWarmWind, num7 * num12, dt);
			FadeTo(s_loopLightColdWind, num8 * num12, dt);
			FadeTo(s_loopAverageColdWind, num9 * num12, dt);
			FadeTo(s_loopStormColdWind, num10 * num12, dt);
			FadeTo(s_loopHurricaneColdWind, num11 * num12, dt);
			if (flag)
			{
				double num13 = ((s_rainV == RainVariant.Storm) ? 5.0 : 10.0);
				if (s_nextThunderTime <= 0.0)
				{
					s_nextThunderTime = frameStartTime + (double)s_rand.Float(0.5f, 1.5f);
				}
				if (frameStartTime >= s_nextThunderTime)
				{
					SpawnThunder();
					s_nextThunderTime = frameStartTime + num13 + (double)s_rand.Float(-1f, 1f);
				}
			}
			else
			{
				s_nextThunderTime = 0.0;
			}
		}
	}

	private static void SpawnThunder()
	{
		if (!s_resourcesLoaded)
		{
			return;
		}
		try
		{
			new Sound(ContentManager.Get<SoundBuffer>((s_rand.Float(0f, 1f) < 0.5f) ? "Audio/ThunderFar" : "Audio/ThunderNear"), s_ambienceVolume, 1f, 0f, isLooped: false, disposeOnStop: true).Play();
		}
		catch (Exception ex)
		{
			Log.Warning("Thunder sound failed: {0}", ex.Message);
		}
	}

	private static void FadeTo(BaseSound s, float target, float dt)
	{
		if (s != null)
		{
			float subsystemGameInfo = s.Volume;
			float random = s_fadeSpeed * dt;
			if (subsystemGameInfo < target)
			{
				subsystemGameInfo = MathUtils.Min(subsystemGameInfo + random, target);
			}
			else if (subsystemGameInfo > target)
			{
				subsystemGameInfo = MathUtils.Max(subsystemGameInfo - random, target);
			}
			s.Volume = MathUtils.Saturate(subsystemGameInfo);
		}
	}

	private static void OnEnterScreen()
	{
		if (!SettingsManager.NewPanoramaEnabled)
		{
			PauseAmbience();
			if (s_resourcesLoaded)
			{
				UnloadAllResources();
			}
			return;
		}
		Screen currentScreen = ScreensManager.CurrentScreen;
		bool vector = HasPanoramaWidget(currentScreen);
		bool num = IsSpecialScreen(currentScreen);
		if ((!vector | num) || SettingsManager.SoundsVolume <= 0f)
		{
			PauseAmbience();
		}
		else
		{
			ResumeAmbience();
		}
	}

	private static bool HasPanoramaWidget(Widget root)
	{
		if (root == null)
		{
			return false;
		}
		if (root is PanoramaWidget)
		{
			return true;
		}
		if (root is ContainerWidget containerWidget)
		{
			foreach (Widget child in containerWidget.Children)
			{
				if (HasPanoramaWidget(child))
				{
					return true;
				}
			}
			return false;
		}
		return false;
	}

	private static bool IsSpecialScreen(Screen s)
	{
		if (!(s is RecipaediaRecipesScreen) && !(s is RecipaediaScreen) && !(s is RecipaediaDescriptionScreen) && !(s is BestiaryScreen) && !(s is BestiaryDescriptionScreen) && !(s is AchievementsScreen))
		{
			return s is AchievementDescriptionScreen;
		}
		return true;
	}

	private static void PauseAmbience()
	{
		s_nextThunderTime = 0.0;
		foreach (Sound item in AllLoops())
		{
			if (item != null)
			{
				item.Volume = 0f;
				item.Pause();
			}
		}
	}

	private static void ResumeAmbience()
	{
		if (SettingsManager.SoundsVolume <= 0f || !SettingsManager.NewPanoramaEnabled)
		{
			return;
		}
		EnsureResourcesForCurrentStyle();
		foreach (Sound item in AllLoops())
		{
			item?.Play();
		}
	}

	private static IEnumerable<Sound> AllLoops()
	{
		yield return s_loopRain;
		yield return s_loopStormRain;
		yield return s_loopWaterWind;
		yield return s_loopLightWarmWind;
		yield return s_loopAverageWarmWind;
		yield return s_loopStormWarmWind;
		yield return s_loopHurricaneWarmWind;
		yield return s_loopLightColdWind;
		yield return s_loopAverageColdWind;
		yield return s_loopStormColdWind;
		yield return s_loopHurricaneColdWind;
	}

	private static Sound MkLoop(string path)
	{
		try
		{
			Sound sound = new Sound(ContentManager.Get<SoundBuffer>(path), 0f, 1f, 0f, isLooped: true);
			sound.Volume = 0f;
			sound.Play();
			return sound;
		}
		catch (Exception ex)
		{
			Log.Warning("Panorama ambience load failed \"{0}\": {1}", path, ex.Message);
			return null;
		}
	}

	public static string GetCurrentLogoKey()
	{
		return s_currentStyle switch
		{
			PanoramaBaseStyle.Rainy => "LogoRain", 
			PanoramaBaseStyle.Snowy => "LogoSnow", 
			PanoramaBaseStyle.Desert => "LogoDesert", 
			PanoramaBaseStyle.Oceanic => "LogoOcean", 
			PanoramaBaseStyle.Cloudy => "LogoClouds", 
			PanoramaBaseStyle.Embrace => "LogoEmbrace", 
			_ => "LogoDefault", 
		};
	}

	private static string GetCurrentVariantName()
	{
		return s_currentStyle switch
		{
			PanoramaBaseStyle.Rainy => s_rainV.ToString(), 
			PanoramaBaseStyle.Snowy => s_snowV.ToString(), 
			PanoramaBaseStyle.Desert => s_sandV.ToString(), 
			PanoramaBaseStyle.Oceanic => s_bubbleV.ToString(), 
			PanoramaBaseStyle.Cloudy => s_cloudV.ToString(), 
			PanoramaBaseStyle.Embrace => s_fireflyMode.ToString(), 
			_ => "Unknown", 
		};
	}

	private static int ExtractImageIndexFromKey(string key)
	{
		if (string.IsNullOrEmpty(key))
		{
			return -1;
		}
		int num = key.Length - 1;
		while (num >= 0 && char.IsDigit(key[num]))
		{
			num--;
		}
		if (num == key.Length - 1)
		{
			return -1;
		}
		if (int.TryParse(key.Substring(num + 1), out var result))
		{
			return result;
		}
		return -1;
	}

	private static void LogPanoramaTextureError(string where, string customKey = null)
	{
		try
		{
			string text = (s_isLightThemeFrozen ? "Light" : "Dark");
			string styleFolderName = GetStyleFolderName(s_currentStyle);
			string text2 = s_currentStyle.ToString();
			string currentVariantName = GetCurrentVariantName();
			string text3 = customKey ?? s_selectedTextureKey ?? "<null>";
			int num = ExtractImageIndexFromKey(text3);
			Log.Warning("Panorama texture is null at {0}. Theme={1}, Style={2} ({3}, folder={4}), Variant={5}, Key=\"{6}\", Index={7}", where, text, text2, (int)s_currentStyle, styleFolderName, currentVariantName, text3, num);
		}
		catch (Exception ex)
		{
			Log.Warning("Panorama texture error logging failed at {0}: {1}", where, ex.Message);
		}
	}

	private void DrawSimpleImage(DrawContext dc)
	{
		if (Texture != null)
		{
			float num = (float)MathUtils.Remainder(Time.FrameStartTime + (double)timeOffset, 10000.0);
			float x = 2f * SimplexNoise.OctavedNoise(num, 0.02f, 4, 2f, 0.5f) - 1f;
			float y = 2f * SimplexNoise.OctavedNoise(num + 100f, 0.02f, 4, 2f, 0.5f) - 1f;
			position += 0.03f * new Vector2(x, y) * MathUtils.Min(Time.FrameDuration, 0.1f);
			position.X = MathUtils.Remainder(position.X, 1f);
			position.Y = MathUtils.Remainder(position.Y, 1f);
			float f = 0.5f * MathUtils.PowSign(MathUtils.Sin(0.21f * num + 2f), 2f) + 0.5f;
			float num2 = MathUtils.Lerp(0.13f, 0.3f, f);
			float num3 = num2 / (float)Texture.Height * (float)Texture.Width / ActualSize.X * ActualSize.Y;
			Vector2 texCoord = new Vector2(position.X - num2, position.Y - num3);
			Vector2 texCoord2 = new Vector2(position.X + num2, position.Y + num3);
			TexturedBatch2D texturedBatch2D = dc.PrimitivesRenderer2D.TexturedBatch(Texture, useAlphaTest: false, 0, DepthStencilState.DepthWrite, null, BlendState.AlphaBlend, SamplerState.LinearWrap);
			int count = texturedBatch2D.TriangleVertices.Count;
			texturedBatch2D.QueueQuad(Vector2.Zero, ActualSize, 1f, texCoord, texCoord2, GlobalColorTransform);
			texturedBatch2D.TransformTriangles(GlobalTransform, count);
		}
	}

	private void SimpleUpdate()
	{
		bool flag = SettingsManager.ThemeMode == ThemeMode.Light || SettingsManager.ThemeMode == ThemeMode.LightTransparent;
		if (flag != m_simpleIsLightTheme)
		{
			m_simpleIsLightTheme = flag;
			Texture = ContentManager.Get<Texture2D>(flag ? "Textures/Gui/LightPanorama" : "Textures/Gui/DarkPanorama");
			recipeTexture = ContentManager.Get<Texture2D>(flag ? "Textures/Gui/LightRecepaediaPanorama" : "Textures/Gui/DarkRecepaediaPanorama");
		}
	}

	private void SwitchPanoramaMode(bool useSimple)
	{
		m_useSimplePanorama = useSimple;
		UnloadAllResources();
		m_particles.Clear();
		m_cloudLayers.Clear();
		if (useSimple)
		{
			LoadSimpleResources();
			PauseAmbience();
			position = Vector2.Zero;
			GC.Collect();
			return;
		}
		EnsureSessionInitialized();
		EnsureResourcesForCurrentStyle();
		Texture = s_selectedTexture;
		ApplyStyleGenIfNeeded(force: true);
		if (SettingsManager.SoundsVolume > 0f && !IsSpecialScreen(ScreensManager.CurrentScreen))
		{
			ResumeAmbience();
		}
	}

	private static void EnsureResourcesForCurrentStyle()
	{
		if (!SettingsManager.NewPanoramaEnabled)
		{
			return;
		}
		string i = s_currentStyle.ToString() + "_" + GetCurrentVariantName();
		if (s_resourcesLoaded && s_loadedVariantKey == i && s_selectedTexture != null)
		{
			return;
		}
		UnloadAllResources(s_selectedTexture != null);
		if (s_selectedTexture == null)
		{
			if (!string.IsNullOrEmpty(s_selectedTextureKey))
			{
				try
				{
					s_selectedTexture = ContentManager.Get<Texture2D>(s_selectedTextureKey);
				}
				catch
				{
					PickNewStyleImage(assignSub: false);
				}
			}
			else
			{
				PickNewStyleImage(assignSub: false);
			}
		}
		try
		{
			bool j = SettingsManager.SoundsVolume > 0f;
			switch (s_currentStyle)
			{
			case PanoramaBaseStyle.Rainy:
				if (s_rainV == RainVariant.Drizzle)
				{
					s_texRainDrizzle = ContentManager.Get<Texture2D>("Textures/Weather/DrizzleRainParticle");
					if (j)
					{
						s_loopRain = MkLoop("Audio/Rain/Rain");
					}
				}
				else if (s_rainV == RainVariant.Storm)
				{
					s_texRainStorm = ContentManager.Get<Texture2D>("Textures/Weather/StormRainParticle");
					if (j)
					{
						s_loopStormRain = MkLoop("Audio/Rain/StormRain");
					}
				}
				else
				{
					s_texRain = ContentManager.Get<Texture2D>("Textures/Weather/RainParticle");
					if (j)
					{
						s_loopRain = MkLoop("Audio/Rain/Rain");
					}
				}
				break;
			case PanoramaBaseStyle.Snowy:
				if (s_snowV == SnowVariant.Storm)
				{
					s_texSnowStorm = ContentManager.Get<Texture2D>("Textures/Weather/StormSnowParticle");
					if (j)
					{
						s_loopHurricaneColdWind = MkLoop("Audio/Wind/HurricaneColdWind");
					}
					break;
				}
				s_texSnow = ContentManager.Get<Texture2D>("Textures/Weather/SnowParticle");
				if (j)
				{
					if (s_snowV == SnowVariant.Flurry)
					{
						s_loopLightColdWind = MkLoop("Audio/Wind/LightColdWind");
					}
					else if (s_snowV == SnowVariant.Normal)
					{
						s_loopAverageColdWind = MkLoop("Audio/Wind/AverageColdWind");
					}
					else
					{
						s_loopStormColdWind = MkLoop("Audio/Wind/StormColdWind");
					}
				}
				break;
			case PanoramaBaseStyle.Desert:
				if (s_sandV == SandVariant.Storm)
				{
					s_texSandStorm = ContentManager.Get<Texture2D>("Textures/Weather/StormSandParticle");
					if (j)
					{
						s_loopHurricaneWarmWind = MkLoop("Audio/Wind/HurricaneWarmWind");
					}
					break;
				}
				s_texSand = ContentManager.Get<Texture2D>("Textures/Weather/SandParticle");
				if (j)
				{
					if (s_sandV == SandVariant.Drift)
					{
						s_loopLightWarmWind = MkLoop("Audio/Wind/LightWarmWind");
					}
					else if (s_sandV == SandVariant.Normal)
					{
						s_loopAverageWarmWind = MkLoop("Audio/Wind/AverageWarmWind");
					}
					else
					{
						s_loopStormWarmWind = MkLoop("Audio/Wind/StormWarmWind");
					}
				}
				break;
			case PanoramaBaseStyle.Oceanic:
				s_texBubbles = ContentManager.Get<Texture2D>("Textures/UnderwaterBubbles");
				if (j)
				{
					s_loopWaterWind = MkLoop("Audio/Wind/WaterWind");
				}
				break;
			case PanoramaBaseStyle.Cloudy:
				s_texCloudTile = ContentManager.Get<Texture2D>("Textures/Clouds1");
				if (j)
				{
					s_loopLightWarmWind = MkLoop("Audio/Wind/LightWarmWind");
				}
				break;
			case PanoramaBaseStyle.Embrace:
				s_texFireflies = ContentManager.Get<Texture2D>("Textures/FirefliesParticle");
				s_texFlies = ContentManager.Get<Texture2D>("Textures/Fly");
				break;
			}
			s_audioInit = true;
			s_nextThunderTime = 0.0;
			s_loadedStyle = s_currentStyle;
			s_loadedVariantKey = i;
			s_resourcesLoaded = true;
		}
		catch (Exception ex)
		{
			Log.Error("Failed to load panorama resources: " + ex.Message);
			s_resourcesLoaded = false;
		}
	}

	private void LoadSimpleResources()
	{
		m_simpleIsLightTheme = SettingsManager.ThemeMode == ThemeMode.Light || SettingsManager.ThemeMode == ThemeMode.LightTransparent;
		Texture = ContentManager.Get<Texture2D>(m_simpleIsLightTheme ? "Textures/Gui/LightPanorama" : "Textures/Gui/DarkPanorama");
		recipeTexture = ContentManager.Get<Texture2D>(m_simpleIsLightTheme ? "Textures/Gui/LightRecepaediaPanorama" : "Textures/Gui/DarkRecepaediaPanorama");
	}

	private static void SafeDispose(Sound s)
	{
		if (s != null)
		{
			s.Stop();
			s.Dispose();
		}
	}

	private void ResetEffectsState(bool isSpecial)
	{
		m_particles.Clear();
		m_spawnCarry = 0f;
		m_cloudLayers.Clear();
		m_sandSide = 0;
		m_cloudSide = 0;
		m_noEffectTimer = 0f;
		position = Vector2.Zero;
		direction = Vector2.Normalize(new Vector2(s_rand.Float(-1f, 1f), s_rand.Float(-1f, 1f)));
		changeDirectionTime = s_rand.Float(5f, 10f);
		directionTimer = 0f;
		timeOffset = s_rand.Float(0f, 1000f);
		AssignSubVariantForCurrentStyle();
		if (s_currentStyle == PanoramaBaseStyle.Cloudy)
		{
			InitCloudLayersForVariant();
		}
		if (!isSpecial)
		{
			Texture = s_selectedTexture;
		}
		UpdateEffects(0.35f);
		s_nextThunderTime = 0.0;
	}

	private static void DisposeTexture(ref Texture2D texture)
	{
		if (texture != null)
		{
			texture.Dispose();
		}
		texture = null;
	}

	private static void UnloadAllResources(bool keepTexture = false)
	{
		SafeDispose(s_loopRain);
		SafeDispose(s_loopStormRain);
		SafeDispose(s_loopWaterWind);
		SafeDispose(s_loopLightWarmWind);
		SafeDispose(s_loopAverageWarmWind);
		SafeDispose(s_loopStormWarmWind);
		SafeDispose(s_loopHurricaneWarmWind);
		SafeDispose(s_loopLightColdWind);
		SafeDispose(s_loopAverageColdWind);
		SafeDispose(s_loopStormColdWind);
		SafeDispose(s_loopHurricaneColdWind);
		s_loopRain = null;
		s_loopStormRain = null;
		s_loopWaterWind = null;
		s_loopLightWarmWind = null;
		s_loopAverageWarmWind = null;
		s_loopStormWarmWind = null;
		s_loopHurricaneWarmWind = null;
		s_loopLightColdWind = null;
		s_loopAverageColdWind = null;
		s_loopStormColdWind = null;
		s_loopHurricaneColdWind = null;
		if (!keepTexture)
		{
			if (!string.IsNullOrEmpty(s_selectedTextureKey))
			{
				ContentManager.Dispose(s_selectedTextureKey);
			}
			else
			{
				DisposeTexture(ref s_selectedTexture);
			}
			s_selectedTextureKey = null;
		}
		DisposeTexture(ref s_texRainDrizzle);
		DisposeTexture(ref s_texRain);
		DisposeTexture(ref s_texRainStorm);
		DisposeTexture(ref s_texSnow);
		DisposeTexture(ref s_texSnowStorm);
		DisposeTexture(ref s_texSand);
		DisposeTexture(ref s_texSandStorm);
		DisposeTexture(ref s_texBubbles);
		DisposeTexture(ref s_texCloudTile);
		DisposeTexture(ref s_texFireflies);
		DisposeTexture(ref s_texFlies);
		s_resourcesLoaded = false;
		s_loadedStyle = (PanoramaBaseStyle)(-1);
		s_loadedVariantKey = null;
	}
}
