using Engine;
using Engine.Graphics;
using Game.NetWork;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemAnimatedTextures : Subsystem, IUpdateable
{
	public SubsystemTime m_subsystemTime;

	public SubsystemBlocksTexture m_subsystemBlocksTexture;

	public RenderTarget2D m_animatedBlocksTexture;

	public PrimitivesRenderer2D m_primitivesRenderer = new PrimitivesRenderer2D();

	public Random m_random = new Random();

	public bool m_waterOrder;

	public Vector2 m_waterOffset1;

	public Vector2 m_waterOffset2;

	public bool m_magmaOrder;

	public Vector2 m_magmaOffset1;

	public Vector2 m_magmaOffset2;

	public double m_lastAnimateGameTime;

	public bool ShowAnimatedTexture;

	private SubsystemWeather m_subsystemWeather;

	private Vector2 m_waterWindOffset1;

	private Vector2 m_waterWindOffset2;

	private Vector2 m_magmaWindOffset1;

	private Vector2 m_magmaWindOffset2;

	private float windExposureFactor;

	public SubsystemAudio m_subsystemAudio;

	public float m_animationTimer;

	public Texture2D AnimatedBlocksTexture
	{
		get
		{
			if (CommonLib.WorkType == WorkType.Client && CommonLib.BlockTexture != null)
			{
				return CommonLib.BlockTexture;
			}
			if (DisableTextureAnimation || m_animatedBlocksTexture == null)
			{
				return m_subsystemBlocksTexture.BlocksTexture;
			}
			return m_animatedBlocksTexture;
		}
	}

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public bool DisableTextureAnimation => !SettingsManager.BlockAnimationsEnabled;

	public void Update(float dt)
	{
		if (!SettingsManager.BlockAnimationsEnabled || m_subsystemTime.FixedTimeStep.HasValue)
		{
			return;
		}
		m_animationTimer += dt;
		if (!(m_animationTimer < 0.033f))
		{
			float projectileSpeed = m_animationTimer;
			m_animationTimer = 0f;
			m_lastAnimateGameTime = m_subsystemTime.GameTime;
			Texture2D num = m_subsystemBlocksTexture.BlocksTexture;
			if (m_animatedBlocksTexture == null || m_animatedBlocksTexture.Width != num.Width || m_animatedBlocksTexture.Height != num.Height || m_animatedBlocksTexture.MipLevelsCount > 1 != SettingsManager.TerrainMipmapsEnabled)
			{
				Utilities.Dispose(ref m_animatedBlocksTexture);
				m_animatedBlocksTexture = new RenderTarget2D(num.Width, num.Height, (!SettingsManager.TerrainMipmapsEnabled) ? 1 : 4, ColorFormat.Rgba8888, DepthFormat.None);
			}
			Engine.Rectangle eyePosition = Display.ScissorRectangle;
			RenderTarget2D vector = Display.RenderTarget;
			Display.RenderTarget = m_animatedBlocksTexture;
			try
			{
				Display.Clear(new Vector4(Color.Transparent));
				m_primitivesRenderer.TexturedBatch(num, useAlphaTest: false, -1, DepthStencilState.None, RasterizerState.CullNone, BlendState.Opaque, SamplerState.PointClamp).QueueQuad(new Vector2(0f, 0f), new Vector2(m_animatedBlocksTexture.Width, m_animatedBlocksTexture.Height), 0f, Vector2.Zero, Vector2.One, Color.White);
				AnimateWaterBlocksTexture(projectileSpeed);
				AnimateMagmaBlocksTexture(projectileSpeed);
				m_primitivesRenderer.Flush();
			}
			finally
			{
				Display.RenderTarget = vector;
				Display.ScissorRectangle = eyePosition;
			}
			if (SettingsManager.TerrainMipmapsEnabled)
			{
				m_animatedBlocksTexture.GenerateMipMaps();
			}
		}
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemBlocksTexture = Project.FindSubsystem<SubsystemBlocksTexture>(throwOnError: true);
		Display.DeviceReset += Display_DeviceReset;
	}

	public override void Dispose()
	{
		Utilities.Dispose(ref m_animatedBlocksTexture);
		Display.DeviceReset -= Display_DeviceReset;
	}

	public void Display_DeviceReset()
	{
		m_animatedBlocksTexture = null;
	}

	public void DrawBlocksTextureSlot(TexturedBatch2D batch, int slotX, int slotY, Vector2 tc1, Vector2 tc2, Vector2 tcOffset, Color color)
	{
		float num = (float)m_animatedBlocksTexture.Width / 16f;
		batch.QueueQuad(new Vector2(slotX, slotY) * num, new Vector2(slotX + 1, slotY + 1) * num, 0f, (tc1 + tcOffset) / 16f, (tc2 + tcOffset) / 16f, color);
	}

	public void AnimateWaterBlocksTexture(float dt)
	{
		TexturedBatch2D componentCreature = m_primitivesRenderer.TexturedBatch(m_subsystemBlocksTexture.BlocksTexture, useAlphaTest: false, 0, DepthStencilState.None, null, BlendState.AlphaBlend, SamplerState.LinearClamp);
		int flag = BlocksManager.Blocks[18].DefaultTextureSlot % 16;
		int maxRange = BlocksManager.Blocks[18].DefaultTextureSlot / 16;
		double maxChaseTime = m_subsystemTime.GameTime;
		double num = maxChaseTime - (double)dt;
		float num2 = MathUtils.Min((float)MathUtils.Remainder(maxChaseTime, 2.0), 1f);
		float num3 = MathUtils.Min((float)MathUtils.Remainder(maxChaseTime + 1.0, 2.0), 1f);
		byte b = (byte)(255f * num2);
		byte b2 = (byte)(255f * num3);
		if (MathUtils.Remainder(maxChaseTime, 2.0) >= 1.0 && MathUtils.Remainder(num, 2.0) < 1.0)
		{
			m_waterOrder = true;
			m_waterOffset2 = new Vector2(m_random.Float(0f, 1f), m_random.Float(0f, 1f));
			m_waterWindOffset2 = Vector2.Zero;
		}
		else if (MathUtils.Remainder(maxChaseTime + 1.0, 2.0) >= 1.0 && MathUtils.Remainder(num + 1.0, 2.0) < 1.0)
		{
			m_waterOrder = false;
			m_waterOffset1 = new Vector2(m_random.Float(0f, 1f), m_random.Float(0f, 1f));
			m_waterWindOffset1 = Vector2.Zero;
		}
		float num4 = 0f;
		foreach (Vector3 listenerPosition in m_subsystemAudio.ListenerPositions)
		{
			num4 = MathUtils.Max(num4, m_subsystemWeather.GetWindExposureFactor(listenerPosition));
		}
		m_subsystemWeather.ComputeWindParameters(out var _, out var windStrength);
		Vector2 currentWindVector = m_subsystemWeather.CurrentWindVector;
		float num5 = windStrength;
		float num6 = 0.04f * num4;
		m_waterWindOffset1 += currentWindVector * (num5 * num6) * dt;
		m_waterWindOffset2 += currentWindVector * (num5 * num6) * dt;
		Vector2 vector = m_waterOffset1 + m_waterWindOffset1;
		Vector2 vector2 = m_waterOffset2 + m_waterWindOffset2;
		vector.X = MathUtils.Remainder(vector.X, 1f);
		vector.Y = MathUtils.Remainder(vector.Y, 1f);
		vector2.X = MathUtils.Remainder(vector2.X, 1f);
		vector2.Y = MathUtils.Remainder(vector2.Y, 1f);
		Vector2 tcOffset = new Vector2(flag, maxRange) - (m_waterOrder ? vector : vector2);
		Vector2 tcOffset2 = new Vector2(flag, maxRange) - (m_waterOrder ? vector2 : vector);
		Color color = (m_waterOrder ? new Color(b, b, b, b) : new Color(b2, b2, b2, b2));
		Color color2 = (m_waterOrder ? new Color(b2, b2, b2, b2) : new Color(b, b, b, b));
		float num7 = (float)maxChaseTime;
		float num8 = MathUtils.Floor(MathUtils.Remainder(1.75f * num7, 1f) * 16f) / 16f;
		float num9 = 1f - num8;
		float num10 = MathUtils.Floor(MathUtils.Remainder(1.75f / MathUtils.Sqrt(2f) * num7, 1f) * 16f) / 16f;
		float num11 = 1f - num10;
		Vector2 tc = new Vector2(0f, 0f);
		Vector2 tc2 = new Vector2(1f, 1f);
		DrawBlocksTextureSlot(componentCreature, flag, maxRange, tc, tc2, tcOffset, color);
		DrawBlocksTextureSlot(componentCreature, flag, maxRange, tc, tc2, tcOffset2, color2);
		tc = new Vector2(num8, 0f);
		tc2 = new Vector2(num8 + 1f, 1f);
		DrawBlocksTextureSlot(componentCreature, flag - 1, maxRange, tc, tc2, tcOffset, color);
		DrawBlocksTextureSlot(componentCreature, flag - 1, maxRange, tc, tc2, tcOffset2, color2);
		tc = new Vector2(num9, 0f);
		tc2 = new Vector2(num9 + 1f, 1f);
		DrawBlocksTextureSlot(componentCreature, flag + 1, maxRange, tc, tc2, tcOffset, color);
		DrawBlocksTextureSlot(componentCreature, flag + 1, maxRange, tc, tc2, tcOffset2, color2);
		tc = new Vector2(0f, num8);
		tc2 = new Vector2(1f, num8 + 1f);
		DrawBlocksTextureSlot(componentCreature, flag, maxRange - 1, tc, tc2, tcOffset, color);
		DrawBlocksTextureSlot(componentCreature, flag, maxRange - 1, tc, tc2, tcOffset2, color2);
		tc = new Vector2(0f, num9);
		tc2 = new Vector2(1f, num9 + 1f);
		DrawBlocksTextureSlot(componentCreature, flag, maxRange + 1, tc, tc2, tcOffset, color);
		DrawBlocksTextureSlot(componentCreature, flag, maxRange + 1, tc, tc2, tcOffset2, color2);
		tc = new Vector2(num10, num11);
		tc2 = new Vector2(num10 + 1f, num11 + 1f);
		DrawBlocksTextureSlot(componentCreature, flag - 1, maxRange + 1, tc, tc2, tcOffset, color);
		DrawBlocksTextureSlot(componentCreature, flag - 1, maxRange + 1, tc, tc2, tcOffset2, color2);
		tc = new Vector2(num11, num11);
		tc2 = new Vector2(num11 + 1f, num11 + 1f);
		DrawBlocksTextureSlot(componentCreature, flag + 1, maxRange + 1, tc, tc2, tcOffset, color);
		DrawBlocksTextureSlot(componentCreature, flag + 1, maxRange + 1, tc, tc2, tcOffset2, color2);
		tc = new Vector2(num10, num10);
		tc2 = new Vector2(num10 + 1f, num10 + 1f);
		DrawBlocksTextureSlot(componentCreature, flag - 1, maxRange - 1, tc, tc2, tcOffset, color);
		DrawBlocksTextureSlot(componentCreature, flag - 1, maxRange - 1, tc, tc2, tcOffset2, color2);
		tc = new Vector2(num11, num10);
		tc2 = new Vector2(num11 + 1f, num10 + 1f);
		DrawBlocksTextureSlot(componentCreature, flag + 1, maxRange - 1, tc, tc2, tcOffset, color);
		DrawBlocksTextureSlot(componentCreature, flag + 1, maxRange - 1, tc, tc2, tcOffset2, color2);
	}

	public void AnimateMagmaBlocksTexture(float dt)
	{
		TexturedBatch2D pickable = m_primitivesRenderer.TexturedBatch(m_subsystemBlocksTexture.BlocksTexture, useAlphaTest: false, 0, DepthStencilState.None, null, BlendState.AlphaBlend, SamplerState.LinearClamp);
		int num = BlocksManager.Blocks[92].DefaultTextureSlot % 16;
		int position = BlocksManager.Blocks[92].DefaultTextureSlot / 16;
		double num2 = 0.5 * m_subsystemTime.GameTime;
		double num3 = num2 - 0.5 * (double)dt;
		float pickable2 = MathUtils.Min((float)MathUtils.Remainder(num2, 2.0), 1f);
		float num4 = MathUtils.Min((float)MathUtils.Remainder(num2 + 1.0, 2.0), 1f);
		byte b = (byte)(255f * pickable2);
		byte b2 = (byte)(255f * num4);
		if (MathUtils.Remainder(num2, 2.0) >= 1.0 && MathUtils.Remainder(num3, 2.0) < 1.0)
		{
			m_magmaOrder = true;
			m_magmaOffset2 = new Vector2(m_random.Float(0f, 1f), m_random.Float(0f, 1f));
			m_magmaWindOffset2 = Vector2.Zero;
		}
		else if (MathUtils.Remainder(num2 + 1.0, 2.0) >= 1.0 && MathUtils.Remainder(num3 + 1.0, 2.0) < 1.0)
		{
			m_magmaOrder = false;
			m_magmaOffset1 = new Vector2(m_random.Float(0f, 1f), m_random.Float(0f, 1f));
			m_magmaWindOffset1 = Vector2.Zero;
		}
		float num5 = 0f;
		foreach (Vector3 listenerPosition in m_subsystemAudio.ListenerPositions)
		{
			num5 = MathUtils.Max(num5, m_subsystemWeather.GetWindExposureFactor(listenerPosition));
		}
		m_subsystemWeather.ComputeWindParameters(out var _, out var windStrength);
		Vector2 currentWindVector = m_subsystemWeather.CurrentWindVector;
		float num6 = windStrength;
		float num7 = 0.04f * num5;
		m_magmaWindOffset1 += currentWindVector * (num6 * num7) * dt;
		m_magmaWindOffset2 += currentWindVector * (num6 * num7) * dt;
		Vector2 vector = m_magmaOffset1 + m_magmaWindOffset1;
		Vector2 vector2 = m_magmaOffset2 + m_magmaWindOffset2;
		vector.X = MathUtils.Remainder(vector.X, 1f);
		vector.Y = MathUtils.Remainder(vector.Y, 1f);
		vector2.X = MathUtils.Remainder(vector2.X, 1f);
		vector2.Y = MathUtils.Remainder(vector2.Y, 1f);
		Vector2 tcOffset = new Vector2(num, position) - (m_magmaOrder ? vector : vector2);
		Vector2 tcOffset2 = new Vector2(num, position) - (m_magmaOrder ? vector2 : vector);
		Color color = (m_magmaOrder ? new Color(b, b, b, b) : new Color(b2, b2, b2, b2));
		Color color2 = (m_magmaOrder ? new Color(b2, b2, b2, b2) : new Color(b, b, b, b));
		float num8 = (float)(0.4 * m_subsystemTime.GameTime);
		float num9 = MathUtils.Floor(MathUtils.Remainder(num8, 1f) * 16f) / 16f;
		float num10 = 1f - num9;
		float num11 = MathUtils.Floor(MathUtils.Remainder(num8 / MathUtils.Sqrt(2f), 1f) * 16f) / 16f;
		float num12 = 1f - num11;
		Vector2 tc = new Vector2(0f, 0f);
		Vector2 tc2 = new Vector2(1f, 1f);
		DrawBlocksTextureSlot(pickable, num, position, tc, tc2, tcOffset, color);
		DrawBlocksTextureSlot(pickable, num, position, tc, tc2, tcOffset2, color2);
		tc = new Vector2(num9, 0f);
		tc2 = new Vector2(num9 + 1f, 1f);
		DrawBlocksTextureSlot(pickable, num - 1, position, tc, tc2, tcOffset, color);
		DrawBlocksTextureSlot(pickable, num - 1, position, tc, tc2, tcOffset2, color2);
		tc = new Vector2(num10, 0f);
		tc2 = new Vector2(num10 + 1f, 1f);
		DrawBlocksTextureSlot(pickable, num + 1, position, tc, tc2, tcOffset, color);
		DrawBlocksTextureSlot(pickable, num + 1, position, tc, tc2, tcOffset2, color2);
		tc = new Vector2(0f, num9);
		tc2 = new Vector2(1f, num9 + 1f);
		DrawBlocksTextureSlot(pickable, num, position - 1, tc, tc2, tcOffset, color);
		DrawBlocksTextureSlot(pickable, num, position - 1, tc, tc2, tcOffset2, color2);
		tc = new Vector2(0f, num10);
		tc2 = new Vector2(1f, num10 + 1f);
		DrawBlocksTextureSlot(pickable, num, position + 1, tc, tc2, tcOffset, color);
		DrawBlocksTextureSlot(pickable, num, position + 1, tc, tc2, tcOffset2, color2);
		tc = new Vector2(num11, num12);
		tc2 = new Vector2(num11 + 1f, num12 + 1f);
		DrawBlocksTextureSlot(pickable, num - 1, position + 1, tc, tc2, tcOffset, color);
		DrawBlocksTextureSlot(pickable, num - 1, position + 1, tc, tc2, tcOffset2, color2);
		tc = new Vector2(num12, num12);
		tc2 = new Vector2(num12 + 1f, num12 + 1f);
		DrawBlocksTextureSlot(pickable, num + 1, position + 1, tc, tc2, tcOffset, color);
		DrawBlocksTextureSlot(pickable, num + 1, position + 1, tc, tc2, tcOffset2, color2);
		tc = new Vector2(num11, num11);
		tc2 = new Vector2(num11 + 1f, num11 + 1f);
		DrawBlocksTextureSlot(pickable, num - 1, position - 1, tc, tc2, tcOffset, color);
		DrawBlocksTextureSlot(pickable, num - 1, position - 1, tc, tc2, tcOffset2, color2);
		tc = new Vector2(num12, num11);
		tc2 = new Vector2(num12 + 1f, num11 + 1f);
		DrawBlocksTextureSlot(pickable, num + 1, position - 1, tc, tc2, tcOffset, color);
		DrawBlocksTextureSlot(pickable, num + 1, position - 1, tc, tc2, tcOffset2, color2);
	}
}
