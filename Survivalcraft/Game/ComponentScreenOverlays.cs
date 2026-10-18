using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.Graphics;
using Engine.Media;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentScreenOverlays : Component, IDrawable, IUpdateable
{
	private struct Snowflake
	{
		public Vector2 Position;

		public int TextureSlot;

		public float TimeToLive;

		public float TotalTime;

		public float Rotation;
	}

	private struct ScreenWound
	{
		public Vector2 Position;

		public float BaseSize;

		public float Rotation;

		public float RemainingTime;

		public float TotalTime;

		public int TextureSlot;

		public float PulseT;

		public float PulsePhase;

		public float ShakePhase;

		public bool Edge;
	}

	public SubsystemTime m_subsystemTime;

	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemSky m_subsystemSky;

	public ComponentGui m_componentGui;

	public ComponentPlayer m_componentPlayer;

	public PrimitivesRenderer2D m_primitivesRenderer2D;

	public PrimitivesRenderer3D m_primitivesRenderer3D;

	public Random m_random;

	public Vector2[] m_iceVertices;

	public Point2 m_cellsCount;

	public float? m_light;

	public double? m_waterSurfaceCrossTime;

	public bool m_isUnderWater;

	public static int[] m_drawOrders;

	private Texture2D m_snowflakeTexture;

	private int m_textureSlotsCount;

	private ComponentVitalStats m_componentVitalStats;

	private List<Snowflake> m_snowflakes;

	private Texture2D[] m_snowflakeTextures;

	private SubsystemWeather m_subsystemWeather;

	private Color m_overlayColor;

	private SubsystemPalette m_subsystemPalette;

	public SubsystemGameInfo m_subsystemGameInfo;

	private double m_achievementDisplayTime;

	private const double AchievementDuration = 5.0;

	private const double FadeDuration = 1.0;

	private const double SlideInTime = 0.5;

	private const double HoldTime = 10.0;

	private const double SlideOutTime = 0.5;

	private PlayerAchievements.AchievementDisplay m_currentAchievement;

	private Texture2D m_achievementTextureCommon;

	private Texture2D m_achievementTextureMaster;

	private Texture2D m_achievementTextureEvil;

	private Texture2D m_quicksandOverlayTexture;

	private Texture2D m_achievementTextureExCommon;

	private Texture2D m_achievementTextureExMaster;

	private Texture2D m_achievementTextureExEvil;

	private List<ScreenWound> m_wounds;

	private Texture2D m_damageTexture;

	private int m_damageSlotsCount;

	private ComponentHealth m_componentHealth;

	private bool m_prevHealthInitialized;

	private float m_prevHealth;

	private Texture2D m_bloodPuddleTexture;

	private Texture2D m_mudOverlayTexture;

	public float BlackoutFactor { get; set; }

	public float RedoutFactor { get; set; }

	public float GreenoutFactor { get; set; }

	public string FloatingMessage { get; set; }

	public float FloatingMessageFactor { get; set; }

	public string Message { get; set; }

	public float MessageFactor { get; set; }

	public float IceFactor { get; set; }

	public UpdateOrder UpdateOrder => UpdateOrder.Reset;

	public int[] DrawOrders => m_drawOrders;

	public float ShadeFactor { get; set; }

	public void Update(float dt)
	{
		bool flag = m_subsystemSky.ViewUnderWaterDepth > 0f;
		if (flag != m_isUnderWater)
		{
			m_isUnderWater = flag;
			m_waterSurfaceCrossTime = m_subsystemTime.GameTime;
		}
		BlackoutFactor = 0f;
		RedoutFactor = 0f;
		GreenoutFactor = 0f;
		IceFactor = 0f;
		FloatingMessage = null;
		FloatingMessageFactor = 0f;
		Message = null;
		ShadeFactor = 0f;
		MessageFactor = 0f;
		UpdateSnowflakes(dt);
		UpdateAchievementDisplay(dt);
		UpdateWounds(dt);
		if (m_componentHealth != null)
		{
			if (!m_prevHealthInitialized)
			{
				m_prevHealth = m_componentHealth.Health;
				m_prevHealthInitialized = true;
			}
			float num = m_componentHealth.Health - m_prevHealth;
			if (num < -1E-06f)
			{
				bool diedNow = m_componentHealth.Health <= 0f;
				OnDamage(0f - num, diedNow);
			}
			else if (num > 1E-06f)
			{
				OnHealed(num);
			}
			m_prevHealth = m_componentHealth.Health;
		}
		ComponentClothing componentClothing = m_componentPlayer.Entity.FindComponent<ComponentClothing>();
		if (componentClothing == null)
		{
			return;
		}
		float num2 = 0f;
		Color? color = null;
		foreach (ClothingSlot value in Enum.GetValues(typeof(ClothingSlot)))
		{
			foreach (int item in componentClothing.GetClothes(value))
			{
				ClothingData clothingData = BlocksManager.Blocks[Terrain.ExtractContents(item)].GetClothingData(Terrain.ExtractData(item));
				if (clothingData != null && clothingData.Shade > 0f)
				{
					num2 += clothingData.Shade;
					int clothingColor = ClothingBlock.GetClothingColor(Terrain.ExtractData(item));
					if (clothingColor != 0)
					{
						color = m_subsystemPalette.GetFabricColor(clothingColor);
					}
				}
			}
		}
		ShadeFactor = MathUtils.Saturate(num2);
		m_overlayColor = color ?? Color.Black;
	}

	public void Draw(Camera camera, int drawOrder)
	{
		if (m_currentAchievement != null)
		{
			DrawAchievementOverlay(camera);
		}
		if (m_componentPlayer.GameWidget != camera.GameWidget)
		{
			return;
		}
		if (m_waterSurfaceCrossTime.HasValue)
		{
			float playerData = (float)(m_subsystemTime.GameTime - m_waterSurfaceCrossTime.Value);
			float num = 0.66f * MathUtils.Sqr(MathUtils.Saturate(1f - 0.75f * playerData));
			if (num > 0.01f)
			{
				Matrix matrix = default;
				matrix.Translation = Vector3.Zero;
				matrix.Forward = camera.ViewDirection;
				matrix.Right = Vector3.Normalize(Vector3.Cross(camera.ViewUp, matrix.Forward));
				matrix.Up = Vector3.Normalize(Vector3.Cross(matrix.Right, matrix.Forward));
				Vector3 vector = matrix.ToYawPitchRoll();
				Vector2 zero = Vector2.Zero;
				zero.X -= 2f * vector.X / (float)Math.PI + 0.05f * MathUtils.Sin(5f * playerData);
				zero.Y += 2f * vector.Y / (float)Math.PI + (m_isUnderWater ? (0.75f * playerData) : (-0.75f * playerData));
				Texture2D texture = ContentManager.Get<Texture2D>("Textures/SplashOverlay");
				DrawTexturedOverlay(camera, texture, new Color(156, 206, 210), num, num, zero);
			}
		}
		DrawSnowflakes(camera);
		if (m_subsystemSky.ViewUnderQuicksandDepth > 0f)
		{
			float num2 = MathUtils.Saturate(m_subsystemSky.ViewUnderQuicksandDepth);
			int x = Terrain.ToCell(camera.ViewPosition.X);
			int num3 = Terrain.ToCell(camera.ViewPosition.Y);
			int z = Terrain.ToCell(camera.ViewPosition.Z);
			int cellValue = m_subsystemTerrain.Terrain.GetCellValue(x, num3, z);
			int data = Terrain.ExtractData(cellValue);
			if (Terrain.ExtractContents(cellValue) == 0)
			{
				data = Terrain.ExtractData(m_subsystemTerrain.Terrain.GetCellValue(x, num3 - 1, z));
			}
			float num4 = 0.5f;
			Texture2D texture2;
			Color c;
			if (QuicksandBlock.GetKind(data) == QuicksandBlock.QuicksandKind.Mud)
			{
				texture2 = m_mudOverlayTexture;
				c = new Color(60, 40, 20);
				num4 = 0.95f;
			}
			else
			{
				texture2 = m_quicksandOverlayTexture;
				c = BlockColorsMap.SandColorsMap.LookupSand(m_subsystemTerrain.Terrain, x, num3, z);
			}
			int cellLight = m_subsystemTerrain.Terrain.GetCellLight(x, num3, z);
			float s = MathUtils.Lerp(0.15f, 0.8f, (float)cellLight / 15f);
			c = Color.MultiplyColorOnly(c, s);
			Vector2 vector2 = new Vector2(0.05f * (float)MathUtils.Sin(0.7 * Time.FrameStartTime), 0.04f * (float)MathUtils.Cos(0.9 * Time.FrameStartTime));
			Vector2 viewportSize = camera.ViewportSize;
			float num5 = viewportSize.X / viewportSize.Y;
			Vector2 vector3 = new Vector2(0f, 0f);
			Vector2 vector4 = new Vector2(viewportSize.X, 0f);
			Vector2 vector5 = new Vector2(viewportSize.X, viewportSize.Y);
			Vector2 vector6 = new Vector2(0f, viewportSize.Y);
			Vector2 p = viewportSize * 0.5f;
			vector2.X = MathUtils.Remainder(vector2.X, 1f);
			vector2.Y = MathUtils.Remainder(vector2.Y, 1f);
			Vector2 vector7 = new Vector2(0f, 0f) + vector2;
			Vector2 vector8 = new Vector2(num5, 0f) + vector2;
			Vector2 vector9 = new Vector2(num5, 1f) + vector2;
			Vector2 vector10 = new Vector2(0f, 1f) + vector2;
			Vector2 texCoord = new Vector2(num5 * 0.5f, 0.5f) + vector2;
			Color color = c * num4;
			color.A = (byte)((float)(int)c.A * num4 * num2);
			Color color2 = c * num4;
			color2.A = color.A;
			TexturedBatch2D texturedBatch2D = m_primitivesRenderer2D.TexturedBatch(texture2, useAlphaTest: false, 0, DepthStencilState.None, null, BlendState.AlphaBlend, SamplerState.PointWrap);
			int count = texturedBatch2D.TriangleVertices.Count;
			texturedBatch2D.QueueTriangle(vector3, vector4, p, 0f, vector7, vector8, texCoord, color, color, color2);
			texturedBatch2D.QueueTriangle(vector4, vector5, p, 0f, vector8, vector9, texCoord, color, color, color2);
			texturedBatch2D.QueueTriangle(vector5, vector6, p, 0f, vector9, vector10, texCoord, color, color, color2);
			texturedBatch2D.QueueTriangle(vector6, vector3, p, 0f, vector10, vector7, texCoord, color, color, color2);
			texturedBatch2D.TransformTriangles(camera.ViewportMatrix, count);
			texturedBatch2D.Flush();
		}
		if (IceFactor > 0f)
		{
			DrawIceOverlay(camera, IceFactor);
		}
		if (m_componentVitalStats != null && m_componentVitalStats.Temperature == 0f && m_componentHealth != null && m_componentHealth.Health > 0f)
		{
			Vector2 vector11 = camera.ViewportSize / 2f;
			float globalScale = camera.GameWidget.GlobalScale;
			Subtexture subtexture = ContentManager.Get<Subtexture>("Textures/Atlas/SkullAndBones");
			if (subtexture != null)
			{
				TexturedBatch2D texturedBatch2D2 = m_primitivesRenderer2D.TexturedBatch(subtexture.Texture, useAlphaTest: false, 0, DepthStencilState.None, null, BlendState.AlphaBlend, SamplerState.PointClamp);
				Vector2 vector12 = new Vector2(70f, 70f) * globalScale;
				Vector2 vector13 = vector11 - vector12 / 2f - new Vector2(0f, 30f * globalScale);
				Vector2 corner = vector13 + vector12;
				texturedBatch2D2.QueueQuad(vector13, corner, 0f, subtexture.TopLeft, subtexture.BottomRight, Color.White);
				texturedBatch2D2.TransformTriangles(start: texturedBatch2D2.TriangleVertices.Count, matrix: camera.ViewportMatrix);
				texturedBatch2D2.Flush();
			}
			BitmapFont font = ContentManager.Get<BitmapFont>("Fonts/Pericles");
			FontBatch2D fontBatch2D = m_primitivesRenderer2D.FontBatch(font, 1, DepthStencilState.None, null, BlendState.AlphaBlend);
			int count2 = fontBatch2D.TriangleVertices.Count;
			string text = LanguageControl.Get("ComponentScreenOverlays", "FrozenMessage");
			Vector2 position = vector11 + new Vector2(0f, 40f * globalScale);
			fontBatch2D.QueueText(text, position, 0f, new Color(0, 0, 0), TextAnchor.HorizontalCenter, Vector2.One * globalScale * 1.2f, Vector2.Zero);
			fontBatch2D.TransformTriangles(camera.ViewportMatrix, count2);
			fontBatch2D.Flush();
		}
		if (ShadeFactor > 0f)
		{
			DrawBlackOverlay(camera, ShadeFactor);
		}
		if (BlackoutFactor > 0.01f)
		{
			float num6 = BlackoutFactor;
			if (num6 > 0.4f)
			{
				num6 = 0.4f;
			}
			DrawOverlay(camera, Color.Black, num6, num6);
		}
		if (RedoutFactor > 0.01f)
		{
			float num7 = RedoutFactor;
			if (m_componentHealth != null && m_componentHealth.Health > 0f && num7 > 0.75f)
			{
				num7 = 0.75f;
			}
			DrawOverlay(camera, new Color(255, 64, 0), MathUtils.Saturate(2f * (num7 - 0.5f)), num7);
		}
		if (m_subsystemTime.GameTimeFactor == 0f)
		{
			DrawOverlay(camera, Color.Black, 0.6f, 0.6f);
			Vector2 vector14 = camera.ViewportSize / 2f;
			float globalScale2 = camera.GameWidget.GlobalScale;
			FlatBatch2D flatBatch2D = m_primitivesRenderer2D.FlatBatch(1, DepthStencilState.None, null, BlendState.AlphaBlend);
			int count3 = flatBatch2D.TriangleVertices.Count;
			float num8 = 10f * globalScale2;
			float num9 = 36f * globalScale2;
			float num10 = 12f * globalScale2;
			Vector2 vector15 = vector14 + new Vector2(0f - num8 - num10 / 2f, (0f - num9) / 2f) - new Vector2(0f, 30f * globalScale2);
			Vector2 p2 = vector15 + new Vector2(num8, 0f);
			Vector2 vector16 = vector15 + new Vector2(num8, num9);
			Vector2 p3 = vector15 + new Vector2(0f, num9);
			flatBatch2D.QueueTriangle(vector15, p2, vector16, 0f, Color.White, Color.White, Color.White);
			flatBatch2D.QueueTriangle(vector15, vector16, p3, 0f, Color.White, Color.White, Color.White);
			Vector2 vector17 = vector14 + new Vector2(num10 / 2f, (0f - num9) / 2f) - new Vector2(0f, 30f * globalScale2);
			Vector2 p4 = vector17 + new Vector2(num8, 0f);
			Vector2 vector18 = vector17 + new Vector2(num8, num9);
			Vector2 p5 = vector17 + new Vector2(0f, num9);
			flatBatch2D.QueueTriangle(vector17, p4, vector18, 0f, Color.White, Color.White, Color.White);
			flatBatch2D.QueueTriangle(vector17, vector18, p5, 0f, Color.White, Color.White, Color.White);
			flatBatch2D.TransformTriangles(camera.ViewportMatrix, count3);
			flatBatch2D.Flush();
			BitmapFont font2 = ContentManager.Get<BitmapFont>("Fonts/Pericles");
			FontBatch2D fontBatch2D2 = m_primitivesRenderer2D.FontBatch(font2, 1, DepthStencilState.None, null, BlendState.AlphaBlend);
			int count4 = fontBatch2D2.TriangleVertices.Count;
			string text2 = LanguageControl.Get("ComponentScreenOverlays", "ServerPaused");
			fontBatch2D2.QueueText(text2, vector14 + new Vector2(0f, 25f * globalScale2), 0f, Color.White, TextAnchor.HorizontalCenter | TextAnchor.VerticalCenter, Vector2.One * globalScale2 * 0.9f, Vector2.Zero);
			fontBatch2D2.TransformTriangles(camera.ViewportMatrix, count4);
			fontBatch2D2.Flush();
		}
		if (GreenoutFactor > 0.01f)
		{
			DrawOverlay(camera, new Color(166, 175, 103), GreenoutFactor, MathUtils.Saturate(2f * GreenoutFactor));
		}
		DrawScreenWounds(camera);
		if (!string.IsNullOrEmpty(FloatingMessage) && FloatingMessageFactor > 0.01f)
		{
			DrawFloatingMessage(camera, FloatingMessage, FloatingMessageFactor);
		}
		if (!string.IsNullOrEmpty(Message) && MessageFactor > 0.01f)
		{
			DrawMessage(camera, Message, MessageFactor);
		}
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		base.Load(valuesDictionary, idToEntityMap);
		m_subsystemPalette = Project.FindSubsystem<SubsystemPalette>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemSky = Project.FindSubsystem<SubsystemSky>(throwOnError: true);
		m_componentGui = Entity.FindComponent<ComponentGui>(throwOnError: true);
		m_componentVitalStats = Entity.FindComponent<ComponentVitalStats>(throwOnError: true);
		m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
		m_componentPlayer = Entity.FindComponent<ComponentPlayer>(throwOnError: true);
		m_snowflakeTexture = ContentManager.Get<Texture2D>("Textures/Weather/SnowParticle");
		m_achievementTextureCommon = ContentManager.Get<Texture2D>("Textures/Achievements/Achievement");
		m_achievementTextureMaster = ContentManager.Get<Texture2D>("Textures/Achievements/Achievement_Master");
		m_achievementTextureEvil = ContentManager.Get<Texture2D>("Textures/Achievements/Achievement_Killer");
		m_achievementTextureExCommon = ContentManager.Get<Texture2D>("Textures/Achievements/AchievementEx");
		m_achievementTextureExMaster = ContentManager.Get<Texture2D>("Textures/Achievements/Achievement_MasterEx");
		m_achievementTextureExEvil = ContentManager.Get<Texture2D>("Textures/Achievements/Achievement_KillerEx");
		m_quicksandOverlayTexture = ContentManager.Get<Texture2D>("Textures/QuicksandOverlay");
		m_mudOverlayTexture = ContentManager.Get<Texture2D>("Textures/MudOverlay");
		m_textureSlotsCount = 4;
		m_componentHealth = Entity.FindComponent<ComponentHealth>(throwOnError: false);
		m_damageTexture = ContentManager.Get<Texture2D>("Textures/Damage");
		m_bloodPuddleTexture = ContentManager.Get<Texture2D>("Textures/BloodPuddleParticle");
		m_damageSlotsCount = 2;
		m_wounds = new List<ScreenWound>();
		m_prevHealthInitialized = false;
	}

	public void DrawOverlay(Camera camera, Color color, float innerFactor, float outerFactor)
	{
		Vector2 center = camera.ViewportSize;
		Vector2 vector = new Vector2(0f, 0f);
		Vector2 vector2 = new Vector2(center.X, 0f);
		Vector2 vector3 = new Vector2(center.X, center.Y);
		Vector2 vector4 = new Vector2(0f, center.Y);
		Vector2 p = new Vector2(center.X / 2f, center.Y / 2f);
		Color color2 = color * outerFactor;
		Color color3 = color * innerFactor;
		FlatBatch2D flatBatch2D = m_primitivesRenderer2D.FlatBatch(0, DepthStencilState.None, null, BlendState.AlphaBlend);
		int count = flatBatch2D.TriangleVertices.Count;
		flatBatch2D.QueueTriangle(vector, vector2, p, 0f, color2, color2, color3);
		flatBatch2D.QueueTriangle(vector2, vector3, p, 0f, color2, color2, color3);
		flatBatch2D.QueueTriangle(vector3, vector4, p, 0f, color2, color2, color3);
		flatBatch2D.QueueTriangle(vector4, vector, p, 0f, color2, color2, color3);
		flatBatch2D.TransformTriangles(camera.ViewportMatrix, count);
		flatBatch2D.Flush();
	}

	public void DrawTexturedOverlay(Camera camera, Texture2D texture, Color color, float innerFactor, float outerFactor, Vector2 offset)
	{
		Vector2 updateProgress = camera.ViewportSize;
		float num = updateProgress.X / updateProgress.Y;
		Vector2 elapsed = new Vector2(0f, 0f);
		Vector2 timedOut = new Vector2(updateProgress.X, 0f);
		Vector2 vector = new Vector2(updateProgress.X, updateProgress.Y);
		Vector2 vector2 = new Vector2(0f, updateProgress.Y);
		Vector2 p = new Vector2(updateProgress.X / 2f, updateProgress.Y / 2f);
		offset.X = MathUtils.Remainder(offset.X, 1f);
		offset.Y = MathUtils.Remainder(offset.Y, 1f);
		Vector2 vector3 = new Vector2(0f, 0f) + offset;
		Vector2 vector4 = new Vector2(num, 0f) + offset;
		Vector2 vector5 = new Vector2(num, 1f) + offset;
		Vector2 vector6 = new Vector2(0f, 1f) + offset;
		Vector2 texCoord = new Vector2(num / 2f, 0.5f) + offset;
		Color color2 = color * outerFactor;
		Color color3 = color * innerFactor;
		TexturedBatch2D texturedBatch2D = m_primitivesRenderer2D.TexturedBatch(texture, useAlphaTest: false, 0, DepthStencilState.None, null, BlendState.Additive, SamplerState.PointWrap);
		int count = texturedBatch2D.TriangleVertices.Count;
		texturedBatch2D.QueueTriangle(elapsed, timedOut, p, 0f, vector3, vector4, texCoord, color2, color2, color3);
		texturedBatch2D.QueueTriangle(timedOut, vector, p, 0f, vector4, vector5, texCoord, color2, color2, color3);
		texturedBatch2D.QueueTriangle(vector, vector2, p, 0f, vector5, vector6, texCoord, color2, color2, color3);
		texturedBatch2D.QueueTriangle(vector2, elapsed, p, 0f, vector6, vector3, texCoord, color2, color2, color3);
		texturedBatch2D.TransformTriangles(camera.ViewportMatrix, count);
		texturedBatch2D.Flush();
	}

	public void DrawIceOverlay(Camera camera, float factor)
	{
		Vector2 viewportSize = camera.ViewportSize;
		float num = (camera.Eye.HasValue ? 1.3f : 1f);
		float num2 = (camera.Eye.HasValue ? MathUtils.Pow(factor, 0.4f) : factor);
		Vector2 vector = (camera.Eye.HasValue ? viewportSize : new Vector2(1f));
		float num3 = vector.Length();
		Point2 point = new Point2((int)MathUtils.Round(12f * viewportSize.X / viewportSize.Y), (int)MathUtils.Round(12f));
		if (m_iceVertices == null || m_cellsCount != point)
		{
			m_cellsCount = point;
			m_random.Seed(0);
			m_iceVertices = new Vector2[(point.X + 1) * (point.Y + 1)];
			for (int i = 0; i <= point.X; i++)
			{
				for (int j = 0; j <= point.Y; j++)
				{
					float num4 = i;
					float num5 = j;
					if (i != 0 && i != point.X)
					{
						num4 += m_random.Float(-0.4f, 0.4f);
					}
					if (j != 0 && j != point.Y)
					{
						num5 += m_random.Float(-0.4f, 0.4f);
					}
					float x = num4 / (float)point.X;
					float y = num5 / (float)point.Y;
					m_iceVertices[i + j * (point.X + 1)] = new Vector2(x, y);
				}
			}
		}
		Vector3 vector2 = Vector3.UnitX / camera.ProjectionMatrix.M11 * 2f * 0.2f * num;
		Vector3 vector3 = Vector3.UnitY / camera.ProjectionMatrix.M22 * 2f * 0.2f * num;
		Vector3 vector4 = -0.2f * Vector3.UnitZ - 0.5f * (vector2 + vector3);
		if (!m_light.HasValue || Time.PeriodicEvent(0.05, 0.0))
		{
			m_light = LightingManager.CalculateSmoothLight(m_subsystemTerrain, camera.ViewPosition) ?? m_light ?? 1f;
		}
		Color color = Color.MultiplyColorOnly(Color.White, m_light.Value);
		m_random.Seed(0);
		Texture2D texture = ContentManager.Get<Texture2D>("Textures/IceOverlay");
		TexturedBatch3D texturedBatch3D = m_primitivesRenderer3D.TexturedBatch(texture, useAlphaTest: false, 0, DepthStencilState.None, RasterizerState.CullNoneScissor, BlendState.AlphaBlend, SamplerState.PointWrap);
		Vector2 vector5 = new Vector2(viewportSize.X / viewportSize.Y, 1f);
		Vector2 vector6 = new Vector2(point.X - 1, point.Y - 1);
		for (int k = 0; k < point.X; k++)
		{
			for (int l = 0; l < point.Y; l++)
			{
				float num6 = 1f - (new Vector2((float)(2 * k) / vector6.X - 1f, (float)(2 * l) / vector6.Y - 1f) * vector).Length() / num3;
				if (num6 + m_random.Float(0f, 0.05f) < num2)
				{
					Vector2 vector7 = m_iceVertices[k + l * (point.X + 1)];
					Vector2 vector8 = m_iceVertices[k + 1 + l * (point.X + 1)];
					Vector2 vector9 = m_iceVertices[k + 1 + (l + 1) * (point.X + 1)];
					Vector2 vector10 = m_iceVertices[k + (l + 1) * (point.X + 1)];
					Vector3 vector11 = vector4 + vector7.X * vector2 + vector7.Y * vector3;
					Vector3 p = vector4 + vector8.X * vector2 + vector8.Y * vector3;
					Vector3 vector12 = vector4 + vector9.X * vector2 + vector9.Y * vector3;
					Vector3 p2 = vector4 + vector10.X * vector2 + vector10.Y * vector3;
					Vector2 vector13 = vector7 * vector5;
					Vector2 texCoord = vector8 * vector5;
					Vector2 vector14 = vector9 * vector5;
					Vector2 texCoord2 = vector10 * vector5;
					Color color2 = color * MathUtils.Saturate(num6);
					texturedBatch3D.QueueTriangle(vector11, p, vector12, vector13, texCoord, vector14, color2);
					texturedBatch3D.QueueTriangle(vector12, p2, vector11, vector14, texCoord2, vector13, color2);
				}
			}
		}
		texturedBatch3D.Flush(camera.ProjectionMatrix);
	}

	public void DrawFloatingMessage(Camera camera, string message, float factor)
	{
		BitmapFont font = ContentManager.Get<BitmapFont>("Fonts/Pericles");
		if (!camera.Eye.HasValue)
		{
			Vector2 position = camera.ViewportSize / 2f;
			position.X += 0.07f * camera.ViewportSize.X * (float)MathUtils.Sin(1.7300000190734863 * Time.FrameStartTime);
			position.Y += 0.07f * camera.ViewportSize.Y * (float)MathUtils.Cos(1.1200000047683716 * Time.FrameStartTime);
			FontBatch2D fontBatch2D = m_primitivesRenderer2D.FontBatch(font, 1, DepthStencilState.None, null, BlendState.AlphaBlend);
			int count = fontBatch2D.TriangleVertices.Count;
			fontBatch2D.QueueText(message, position, 0f, Color.White * factor, TextAnchor.HorizontalCenter | TextAnchor.VerticalCenter, Vector2.One * camera.GameWidget.GlobalScale, Vector2.Zero);
			fontBatch2D.TransformTriangles(camera.ViewportMatrix, count);
			fontBatch2D.Flush();
		}
		else
		{
			Vector3 position2 = -4f * Vector3.UnitZ;
			position2.X += 0.28f * (float)MathUtils.Sin(1.7300000190734863 * Time.FrameStartTime);
			position2.Y += 0.28f * (float)MathUtils.Cos(1.1200000047683716 * Time.FrameStartTime);
			FontBatch3D fontBatch3D = m_primitivesRenderer3D.FontBatch(font, 1, DepthStencilState.None, RasterizerState.CullNoneScissor, BlendState.AlphaBlend);
			fontBatch3D.QueueText(message, position2, 0.008f * Vector3.UnitX, -0.008f * Vector3.UnitY, Color.White * factor, TextAnchor.HorizontalCenter | TextAnchor.VerticalCenter, Vector2.Zero);
			fontBatch3D.Flush(camera.ProjectionMatrix);
		}
	}

	public void DrawMessage(Camera camera, string message, float factor)
	{
		BitmapFont inGracePeriod = ContentManager.Get<BitmapFont>("Fonts/Pericles");
		if (!camera.Eye.HasValue)
		{
			Vector2 position = new Vector2(camera.ViewportSize.X / 2f, camera.ViewportSize.Y - 25f);
			FontBatch2D fontBatch2D = m_primitivesRenderer2D.FontBatch(inGracePeriod, 0, DepthStencilState.None, null, BlendState.AlphaBlend);
			int count = fontBatch2D.TriangleVertices.Count;
			fontBatch2D.QueueText(message, position, 0f, Color.Gray * factor, TextAnchor.HorizontalCenter | TextAnchor.Bottom, Vector2.One * camera.GameWidget.GlobalScale, Vector2.Zero);
			fontBatch2D.TransformTriangles(camera.ViewportMatrix, count);
			fontBatch2D.Flush();
		}
		else
		{
			Vector3 position2 = -4f * Vector3.UnitZ + -0.24f / camera.ProjectionMatrix.M22 * 2f * Vector3.UnitY;
			FontBatch3D fontBatch3D = m_primitivesRenderer3D.FontBatch(inGracePeriod, 0, DepthStencilState.None, RasterizerState.CullNoneScissor, BlendState.AlphaBlend);
			fontBatch3D.QueueText(message, position2, 0.0104f * Vector3.UnitX, -0.0104f * Vector3.UnitY, Color.White * factor, TextAnchor.HorizontalCenter | TextAnchor.VerticalCenter, Vector2.Zero);
			fontBatch3D.Flush(camera.ProjectionMatrix);
		}
	}

	public ComponentScreenOverlays()
	{
		m_overlayColor = Color.White;
		m_primitivesRenderer2D = new PrimitivesRenderer2D();
		m_primitivesRenderer3D = new PrimitivesRenderer3D();
		m_random = new Random(0);
		m_snowflakes = new List<Snowflake>();
	}

	static ComponentScreenOverlays()
	{
		m_drawOrders = new int[1] { 1101 };
	}

	public void DrawBlackOverlay(Camera camera, float factor)
	{
		factor = MathUtils.Saturate(factor);
		float num = 0.8f * (1f - factor);
		Color color = m_overlayColor * num;
		Vector2 viewportSize = camera.ViewportSize;
		Vector2 vector = new Vector2(0f, 0f);
		Vector2 p = new Vector2(viewportSize.X, 0f);
		Vector2 vector2 = new Vector2(viewportSize.X, viewportSize.Y);
		Vector2 p2 = new Vector2(0f, viewportSize.Y);
		FlatBatch2D flatBatch2D = m_primitivesRenderer2D.FlatBatch(0, DepthStencilState.None, null, BlendState.AlphaBlend);
		int count = flatBatch2D.TriangleVertices.Count;
		flatBatch2D.QueueTriangle(vector, p, vector2, 0f, color, color, color);
		flatBatch2D.QueueTriangle(vector2, p2, vector, 0f, color, color, color);
		flatBatch2D.TransformTriangles(camera.ViewportMatrix, count);
		flatBatch2D.Flush();
	}

	private void UpdateSnowflakes(float dt)
	{
		if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative)
		{
			return;
		}
		for (int num = m_snowflakes.Count - 1; num >= 0; num--)
		{
			Snowflake value = m_snowflakes[num];
			float meltingSpeed = GetMeltingSpeed();
			value.TimeToLive -= dt * meltingSpeed;
			m_snowflakes[num] = value;
			if (value.TimeToLive <= 0f)
			{
				m_snowflakes.RemoveAt(num);
			}
		}
		float lightningIntensity = m_subsystemWeather.m_lightningIntensity;
		float num2;
		if (lightningIntensity == 0f)
		{
			num2 = 0.3f;
		}
		else
		{
			num2 = ((!(lightningIntensity > 0.1f) || !(lightningIntensity < 0.5f)) ? 2f : 1f);
		}
		if (m_componentVitalStats.IsUnderSnowfall)
		{
			float num3 = dt * 3f * num2;
			if (m_random.Float(0f, 1f) < num3)
			{
				AddSnowflake();
			}
		}
	}

	private void AddSnowflake()
	{
		float x = m_random.Float(0f, 1f);
		float y = m_random.Float(0f, 1f);
		int textureSlot = m_random.Int(0, m_textureSlotsCount * m_textureSlotsCount - 1);
		float num = 10f;
		float rotation = m_random.Float(0f, (float)Math.PI * 2f);
		Snowflake item = new Snowflake
		{
			Position = new Vector2(x, y),
			TextureSlot = textureSlot,
			TimeToLive = num,
			TotalTime = num,
			Rotation = rotation
		};
		m_snowflakes.Add(item);
	}

	private void DrawSnowflakes(Camera camera)
	{
		Vector2 viewportSize = camera.ViewportSize;
		TexturedBatch2D texturedBatch2D = m_primitivesRenderer2D.TexturedBatch(m_snowflakeTexture, useAlphaTest: false, 0, DepthStencilState.None, null, BlendState.AlphaBlend, SamplerState.PointClamp);
		int count = texturedBatch2D.TriangleVertices.Count;
		foreach (Snowflake snowflake in m_snowflakes)
		{
			Vector2 vector = new Vector2(snowflake.Position.X * viewportSize.X, snowflake.Position.Y * viewportSize.Y);
			float num = 120f;
			float num2 = MathUtils.Saturate(snowflake.TimeToLive / snowflake.TotalTime);
			int num3 = snowflake.TextureSlot % m_textureSlotsCount;
			float num4 = snowflake.TextureSlot / m_textureSlotsCount;
			float num5 = 1f / (float)m_textureSlotsCount;
			float num6 = (float)num3 * num5;
			float num7 = num4 * num5;
			float x = num6 + num5;
			float y = num7 + num5;
			float rotation = snowflake.Rotation;
			float num8 = MathUtils.Cos(rotation);
			float num9 = MathUtils.Sin(rotation);
			Vector2[] array = new Vector2[4]
			{
				new Vector2((0f - num) / 2f, (0f - num) / 2f),
				new Vector2(num / 2f, (0f - num) / 2f),
				new Vector2(num / 2f, num / 2f),
				new Vector2((0f - num) / 2f, num / 2f)
			};
			for (int i = 0; i < array.Length; i++)
			{
				float x2 = array[i].X;
				float y2 = array[i].Y;
				float x3 = x2 * num8 - y2 * num9;
				float y3 = x2 * num9 + y2 * num8;
				array[i] = vector + new Vector2(x3, y3);
			}
			Vector2 p = array[0];
			Vector2 p2 = array[1];
			Vector2 p3 = array[2];
			Vector2 p4 = array[3];
			Vector2 texCoord = new Vector2(num6, num7);
			Vector2 texCoord2 = new Vector2(x, num7);
			Vector2 texCoord3 = new Vector2(x, y);
			Vector2 texCoord4 = new Vector2(num6, y);
			Color color = Color.White * num2;
			texturedBatch2D.QueueQuad(p, p2, p3, p4, 0f, texCoord, texCoord2, texCoord3, texCoord4, color);
		}
		texturedBatch2D.TransformTriangles(camera.ViewportMatrix, count);
		texturedBatch2D.Flush();
	}

	private float GetMeltingSpeed()
	{
		float temperature = m_componentVitalStats.Temperature;
		if (temperature >= 12f)
		{
			return 10f;
		}
		if (temperature >= 10.5f)
		{
			return 8f;
		}
		if (temperature >= 9f)
		{
			return 2f;
		}
		if (temperature >= 7.5f)
		{
			return 1f;
		}
		if (temperature >= 6f)
		{
			return 0.4f;
		}
		return 0.05f;
	}

	private void UpdateAchievementDisplay(float dt)
	{
		PlayerAchievements playerAchievements = m_componentPlayer?.PlayerAchievements;
		if (m_currentAchievement == null && playerAchievements != null && playerAchievements.AchievementQueue.Count > 0)
		{
			m_currentAchievement = playerAchievements.AchievementQueue.Dequeue();
			m_achievementDisplayTime = m_subsystemTime.GameTime;
		}
		if (m_currentAchievement != null && m_subsystemTime.GameTime - m_achievementDisplayTime > 11.0)
		{
			m_currentAchievement = null;
		}
	}

	private void DrawAchievementOverlay(Camera camera)
	{
		double num = m_subsystemTime.GameTime - m_achievementDisplayTime;
		float num2 = MathUtils.Saturate((float)((num < 0.5) ? (num / 0.5) : ((num < 10.5) ? 1.0 : (1.0 - (num - 10.5) / 0.5))));
		Vector2 viewportSize = camera.ViewportSize;
		Vector2 vector = new Vector2(300f, 100f) * camera.GameWidget.GlobalScale;
		float num3 = 20f * camera.GameWidget.GlobalScale;
		float num4 = (1f - num2) * (vector.X + num3);
		Vector2 vector2 = new Vector2(viewportSize.X - num3 - vector.X / 2f + num4, num3 + vector.Y / 2f);
		Texture2D texture2D;
		Texture2D texture2D2;
		Color color;
		switch (m_currentAchievement.Tier)
		{
		case PlayerAchievements.AchievementTier.Master:
			texture2D = m_achievementTextureMaster;
			texture2D2 = m_achievementTextureExMaster;
			color = new Color(180, 140, 255);
			break;
		case PlayerAchievements.AchievementTier.Evil:
			texture2D = m_achievementTextureEvil;
			texture2D2 = m_achievementTextureExEvil;
			color = new Color(255, 80, 80);
			break;
		default:
			texture2D = m_achievementTextureCommon;
			texture2D2 = m_achievementTextureExCommon;
			color = new Color(255, 224, 96);
			break;
		}
		TexturedBatch2D texturedBatch2D = m_primitivesRenderer2D.TexturedBatch(texture2D, useAlphaTest: false, 0, DepthStencilState.None, null, BlendState.AlphaBlend, SamplerState.PointClamp);
		int count = texturedBatch2D.TriangleVertices.Count;
		texturedBatch2D.QueueQuad(vector2 - vector / 2f, vector2 + vector / 2f, 0f, Vector2.Zero, Vector2.One, Color.White * num2);
		texturedBatch2D.TransformTriangles(camera.ViewportMatrix, count);
		texturedBatch2D.Flush();
		float num5 = 64f * camera.GameWidget.GlobalScale;
		Vector2 vector3 = vector2 - vector / 2f + new Vector2(10f, (vector.Y - num5) / 2f);
		BitmapFont bitmapFont = ContentManager.Get<BitmapFont>("Fonts/Pericles");
		float globalScale = camera.GameWidget.GlobalScale;
		float num6 = globalScale * 0.8f;
		float maxWidth = vector.X - num5 - 30f * globalScale;
		string text = WrapText(bitmapFont, m_currentAchievement.Name, maxWidth, globalScale);
		string text2 = WrapText(bitmapFont, m_currentAchievement.Description, maxWidth, num6);
		float y = bitmapFont.MeasureText(text, Vector2.One * globalScale, Vector2.Zero).Y;
		float y2 = bitmapFont.MeasureText(text2, Vector2.One * num6, Vector2.Zero).Y;
		float num7 = vector.Y * ((float)texture2D2.Height / (float)texture2D.Height);
		float num8 = y + 4f * globalScale + y2 - (vector.Y - 12f * globalScale);
		int num9 = ((num8 > 0f) ? ((int)Math.Ceiling(num8 / num7)) : 0);
		for (int i = 0; i < num9; i++)
		{
			Vector2 vector4 = new Vector2(vector2.X, vector2.Y + vector.Y / 2f + num7 / 2f + (float)i * num7);
			TexturedBatch2D texturedBatch2D2 = m_primitivesRenderer2D.TexturedBatch(texture2D2, useAlphaTest: false, 0, DepthStencilState.None, null, BlendState.AlphaBlend, SamplerState.PointClamp);
			int count2 = texturedBatch2D2.TriangleVertices.Count;
			texturedBatch2D2.QueueQuad(vector4 - new Vector2(vector.X, num7) / 2f, vector4 + new Vector2(vector.X, num7) / 2f, 0f, Vector2.Zero, Vector2.One, Color.White * num2);
			texturedBatch2D2.TransformTriangles(camera.ViewportMatrix, count2);
			texturedBatch2D2.Flush();
		}
		Vector2 vector5 = vector3 + new Vector2(num5 * 0.5f);
		if (m_currentAchievement.IconValue.HasValue)
		{
			Block obj = BlocksManager.Blocks[Terrain.ExtractContents(m_currentAchievement.IconValue.Value)];
			DrawBlockEnvironmentData drawBlockEnvironmentData = new DrawBlockEnvironmentData
			{
				SubsystemTerrain = m_subsystemTerrain
			};
			Matrix matrix = Matrix.CreateLookAt(obj.GetIconViewOffset(m_currentAchievement.IconValue.Value, drawBlockEnvironmentData), Vector3.Zero, Vector3.UnitY);
			Matrix matrix2 = Matrix.CreateOrthographic(3.6f, 3.6f, -10f, 10f);
			Matrix matrix3 = MatrixUtils.CreateScaleTranslation(num5, 0f - num5, vector5.X, vector5.Y) * MatrixUtils.CreateScaleTranslation(2f / camera.ViewportSize.X, -2f / camera.ViewportSize.Y, -1f, 1f);
			drawBlockEnvironmentData.ViewProjectionMatrix = matrix * matrix2 * matrix3;
			float iconViewScale = obj.GetIconViewScale(m_currentAchievement.IconValue.Value, drawBlockEnvironmentData);
			Matrix matrix4 = Matrix.CreateTranslation(obj.GetIconBlockOffset(m_currentAchievement.IconValue.Value, drawBlockEnvironmentData));
			obj.DrawBlock(m_primitivesRenderer3D, m_currentAchievement.IconValue.Value, Color.White * num2, iconViewScale, ref matrix4, drawBlockEnvironmentData);
			m_primitivesRenderer3D.Flush(Matrix.Identity);
		}
		else if (!string.IsNullOrEmpty(m_currentAchievement.Texture))
		{
			Texture2D texture = ContentManager.Get<Texture2D>(m_currentAchievement.Texture);
			TexturedBatch2D texturedBatch2D3 = m_primitivesRenderer2D.TexturedBatch(texture, useAlphaTest: false, 0, DepthStencilState.None, null, BlendState.AlphaBlend, SamplerState.PointClamp);
			int count3 = texturedBatch2D3.TriangleVertices.Count;
			texturedBatch2D3.QueueQuad(vector3, vector3 + new Vector2(num5), 0f, Vector2.Zero, Vector2.One, Color.White * num2);
			texturedBatch2D3.TransformTriangles(camera.ViewportMatrix, count3);
			texturedBatch2D3.Flush();
		}
		Vector2 vector6 = vector3 + new Vector2(num5 + 10f * globalScale, -6f * globalScale);
		FontBatch2D fontBatch2D = m_primitivesRenderer2D.FontBatch(bitmapFont, 0, DepthStencilState.None, null, BlendState.AlphaBlend);
		int count4 = fontBatch2D.TriangleVertices.Count;
		fontBatch2D.QueueText(text, vector6, 0f, color * num2, TextAnchor.Default, Vector2.One * globalScale, Vector2.Zero);
		fontBatch2D.TransformTriangles(camera.ViewportMatrix, count4);
		fontBatch2D.Flush();
		float y3 = bitmapFont.MeasureText(text, Vector2.One * globalScale, Vector2.Zero).Y;
		Vector2 position = vector6 + new Vector2(0f, y3 + 4f * globalScale);
		FontBatch2D fontBatch2D2 = m_primitivesRenderer2D.FontBatch(bitmapFont, 0, DepthStencilState.None, null, BlendState.AlphaBlend);
		int count5 = fontBatch2D2.TriangleVertices.Count;
		fontBatch2D2.QueueText(text2, position, 0f, Color.White * num2, TextAnchor.Default, Vector2.One * num6, Vector2.Zero);
		fontBatch2D2.TransformTriangles(camera.ViewportMatrix, count5);
		fontBatch2D2.Flush();
	}

	private static bool TryParseBlockValue(string s, out int value)
	{
		value = 0;
		if (string.IsNullOrEmpty(s))
		{
			return false;
		}
		string[] array = s.Split(':');
		if (!int.TryParse(array[0], out var result))
		{
			return false;
		}
		int data = ((array.Length > 1 && int.TryParse(array[1], out var result2)) ? result2 : 0);
		value = Terrain.MakeBlockValue(result, 0, data);
		return true;
	}

	private static string WrapText(BitmapFont font, string text, float maxWidth, float scale)
	{
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}
		string[] array = text.Split(' ');
		StringBuilder flatBatch2D = new StringBuilder();
		StringBuilder count = new StringBuilder();
		float count2 = font.MeasureText(" ", Vector2.One * scale, Vector2.Zero).X;
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			float x = font.MeasureText(text2, Vector2.One * scale, Vector2.Zero).X;
			float num = font.MeasureText(flatBatch2D.ToString(), Vector2.One * scale, Vector2.Zero).X;
			if (num > 0f)
			{
				num += count2;
			}
			if (num + x <= maxWidth)
			{
				if (flatBatch2D.Length > 0)
				{
					flatBatch2D.Append(' ');
				}
				flatBatch2D.Append(text2);
				continue;
			}
			if (count.Length > 0)
			{
				count.Append('\n');
			}
			count.Append(flatBatch2D);
			flatBatch2D.Clear();
			flatBatch2D.Append(text2);
		}
		if (flatBatch2D.Length > 0)
		{
			if (count.Length > 0)
			{
				count.Append('\n');
			}
			count.Append(flatBatch2D);
		}
		return count.ToString();
	}

	private void UpdateWounds(float dt)
	{
		if (m_wounds == null || m_wounds.Count == 0)
		{
			return;
		}
		for (int num = m_wounds.Count - 1; num >= 0; num--)
		{
			ScreenWound value = m_wounds[num];
			value.RemainingTime -= dt;
			value.PulseT += dt;
			if (value.RemainingTime <= 0f)
			{
				m_wounds.RemoveAt(num);
			}
			else
			{
				m_wounds[num] = value;
			}
		}
	}

	private void DrawScreenWounds(Camera camera)
	{
		if (m_wounds == null || m_wounds.Count == 0)
		{
			return;
		}
		Vector2 v = camera.ViewportSize;
		TexturedBatch2D texturedBatch2D = m_primitivesRenderer2D.TexturedBatch(m_damageTexture, useAlphaTest: false, 0, DepthStencilState.None, null, BlendState.NonPremultiplied, SamplerState.PointClamp);
		int count = texturedBatch2D.TriangleVertices.Count;
		TexturedBatch2D texturedBatch2D2 = null;
		int start = 0;
		for (int i = 0; i < m_wounds.Count; i++)
		{
			ScreenWound screenWound = m_wounds[i];
			float num = 1f + 0.025f * (float)MathUtils.Sin(2.6 * (double)screenWound.PulseT + (double)screenWound.PulsePhase);
			Vector2 vector = new Vector2(1f * (float)MathUtils.Sin(5.1 * (double)screenWound.PulseT + (double)screenWound.ShakePhase), 1f * (float)MathUtils.Cos(4.7 * (double)screenWound.PulseT + (double)screenWound.ShakePhase));
			Vector2 vector2 = new Vector2(screenWound.Position.X * v.X, screenWound.Position.Y * v.Y) + vector;
			float num2 = 0.5f * screenWound.BaseSize * num;
			float num3 = MathUtils.Cos(screenWound.Rotation);
			float num4 = MathUtils.Sin(screenWound.Rotation);
			Vector2 vector3 = new Vector2(0f - num2, 0f - num2);
			Vector2 vector4 = new Vector2(num2, 0f - num2);
			Vector2 vector5 = new Vector2(num2, num2);
			Vector2 vector6 = new Vector2(0f - num2, num2);
			Vector2[] array = new Vector2[4] { vector3, vector4, vector5, vector6 };
			for (int j = 0; j < 4; j++)
			{
				float x = array[j].X;
				float y = array[j].Y;
				array[j] = vector2 + new Vector2(x * num3 - y * num4, x * num4 + y * num3);
			}
			float num5 = ((screenWound.RemainingTime < 10f) ? MathUtils.Saturate(screenWound.RemainingTime / 10f) : 1f);
			Color color = Color.White * num5;
			if (!screenWound.Edge)
			{
				int num6 = MathUtils.Clamp(screenWound.TextureSlot, 0, m_damageSlotsCount * m_damageSlotsCount - 1);
				int num7 = num6 % m_damageSlotsCount;
				float num8 = num6 / m_damageSlotsCount;
				float num9 = 1f / (float)m_damageSlotsCount;
				float num10 = (float)num7 * num9;
				float num11 = num8 * num9;
				float x2 = num10 + num9;
				float y2 = num11 + num9;
				texturedBatch2D.QueueQuad(array[0], array[1], array[2], array[3], 0f, new Vector2(num10, y2), new Vector2(x2, y2), new Vector2(x2, num11), new Vector2(num10, num11), color);
			}
			else
			{
				if (texturedBatch2D2 == null)
				{
					texturedBatch2D2 = m_primitivesRenderer2D.TexturedBatch(m_bloodPuddleTexture, useAlphaTest: false, 0, DepthStencilState.None, null, BlendState.AlphaBlend, SamplerState.PointClamp);
					start = texturedBatch2D2.TriangleVertices.Count;
				}
				texturedBatch2D2.QueueQuad(array[0], array[1], array[2], array[3], 0f, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(0f, 0f), color);
			}
		}
		texturedBatch2D.TransformTriangles(camera.ViewportMatrix, count);
		texturedBatch2D.Flush();
		if (texturedBatch2D2 != null)
		{
			texturedBatch2D2.TransformTriangles(camera.ViewportMatrix, start);
			texturedBatch2D2.Flush();
		}
	}

	public void OnDamage(float damageAmount, bool diedNow)
	{
		if (!(damageAmount < 0.25f))
		{
			int seed = (int)(Time.FrameStartTime * 1000000.0) ^ m_wounds.Count ^ GetHashCode();
			m_random.Seed(seed);
			AddWound(edgePuddle: false);
			int num = m_random.Int(3, 5);
			for (int i = 0; i < num; i++)
			{
				AddWound(edgePuddle: true);
			}
		}
	}

	public void OnHealed(float healAmount)
	{
		if (m_wounds == null || m_wounds.Count == 0)
		{
			return;
		}
		float num = 60f * (healAmount / 0.05f);
		if (num <= 0f)
		{
			return;
		}
		for (int num2 = m_wounds.Count - 1; num2 >= 0; num2--)
		{
			ScreenWound value = m_wounds[num2];
			value.RemainingTime -= num;
			if (value.RemainingTime <= 0f)
			{
				m_wounds.RemoveAt(num2);
			}
			else
			{
				m_wounds[num2] = value;
			}
		}
	}

	private void AddWound(bool edgePuddle)
	{
		if ((!edgePuddle && m_damageTexture == null) || (edgePuddle && m_bloodPuddleTexture == null))
		{
			return;
		}
		ScreenWound item = new ScreenWound
		{
			Edge = edgePuddle
		};
		int num = 12;
		float num2 = (edgePuddle ? 0.12f : 0.18f);
		Vector2 vector = (edgePuddle ? MakeEdgePosition() : MakeNearEdgePosition());
		while (num-- > 0)
		{
			bool flag = true;
			for (int i = 0; i < m_wounds.Count; i++)
			{
				if ((m_wounds[i].Position - vector).Length() < num2)
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				break;
			}
			vector = (edgePuddle ? MakeEdgePosition() : MakeNearEdgePosition());
		}
		item.Position = vector;
		if (edgePuddle)
		{
			item.BaseSize = m_random.Float(210f, 330f) * m_componentPlayer.GameWidget.GlobalScale;
		}
		else
		{
			item.BaseSize = m_random.Float(330f, 480f) * m_componentPlayer.GameWidget.GlobalScale;
		}
		item.Rotation = m_random.Float(0f, (float)Math.PI * 2f);
		item.TextureSlot = m_random.Int(0, m_damageSlotsCount * m_damageSlotsCount - 1);
		item.TotalTime = 300f;
		item.RemainingTime = 300f;
		item.PulseT = m_random.Float(0f, 10f);
		item.PulsePhase = m_random.Float(0f, (float)Math.PI * 2f);
		item.ShakePhase = m_random.Float(0f, (float)Math.PI * 2f);
		m_wounds.Add(item);
	}

	private Vector2 MakeNearEdgePosition()
	{
		float num = m_random.Float(0f, 4f);
		float min = 0.15f;
		float max = 0.4f;
		if (num < 1f)
		{
			return new Vector2(m_random.Float(min, max), m_random.Float(0.18f, 0.82f));
		}
		if (num < 2f)
		{
			return new Vector2(1f - m_random.Float(min, max), m_random.Float(0.18f, 0.82f));
		}
		if (num < 3f)
		{
			return new Vector2(m_random.Float(0.18f, 0.82f), m_random.Float(min, max));
		}
		return new Vector2(m_random.Float(0.18f, 0.82f), 1f - m_random.Float(min, max));
	}

	private Vector2 MakeEdgePosition()
	{
		float num = m_random.Float(0f, 4f);
		float num2 = m_random.Float(0f, 0.01f);
		if (num < 1f)
		{
			return new Vector2(num2, m_random.Float(0.05f, 0.95f));
		}
		if (num < 2f)
		{
			return new Vector2(1f - num2, m_random.Float(0.05f, 0.95f));
		}
		if (num < 3f)
		{
			return new Vector2(m_random.Float(0.05f, 0.95f), num2);
		}
		return new Vector2(m_random.Float(0.05f, 0.95f), 1f - num2);
	}
}
