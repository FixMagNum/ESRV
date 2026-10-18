using System;
using Engine;
using Engine.Graphics;

namespace Game;

public class BevelledRectangleWidget : Widget
{
	public Texture2D m_texture;

	public bool m_textureLinearFilter;

	public Vector2 Size { get; set; }

	public float BevelSize { get; set; }

	public float DirectionalLight { get; set; }

	public float AmbientLight { get; set; }

	public Texture2D Texture
	{
		get
		{
			return m_texture;
		}
		set
		{
			if (value != m_texture)
			{
				m_texture = value;
			}
		}
	}

	public float TextureScale { get; set; }

	public bool TextureLinearFilter
	{
		get
		{
			return m_textureLinearFilter;
		}
		set
		{
			if (value != m_textureLinearFilter)
			{
				m_textureLinearFilter = value;
			}
		}
	}

	public Color CenterColor { get; set; }

	public Color BevelColor { get; set; }

	public Color ShadowColor { get; set; }

	public Color DarkCenterColor { get; set; }

	public Color DarkBevelColor { get; set; }

	public Color DarkCenterColorWithAlpha { get; set; }

	public Color DarkBevelColorWithAlpha { get; set; }

	public Color LightTransparentCenterColor { get; set; }

	public Color LightTransparentBevelColor { get; set; }

	public Color LightTransparentShadowColor { get; set; }

	public float CornerRadius { get; set; }

	public float FrameThickness { get; set; }

	public Color FrameColor { get; set; }

	public bool DisableThemeColors { get; set; }

	public BevelledRectangleWidget()
	{
		Size = new Vector2(float.PositiveInfinity);
		BevelSize = 2f;
		AmbientLight = 0.6f;
		DirectionalLight = 0.4f;
		TextureScale = 1f;
		TextureLinearFilter = false;
		CenterColor = new Color(181, 172, 154);
		BevelColor = new Color(181, 172, 154);
		ShadowColor = new Color(0, 0, 0, 80);
		DarkCenterColor = new Color(45, 45, 45);
		DarkBevelColor = new Color(60, 60, 60);
		DarkCenterColorWithAlpha = new Color(45, 45, 45, 180);
		DarkBevelColorWithAlpha = new Color(60, 60, 60, 180);
		LightTransparentCenterColor = new Color(181, 172, 154, 10);
		LightTransparentBevelColor = new Color(181, 172, 154, 10);
		LightTransparentShadowColor = new Color(0, 0, 0, 50);
		CornerRadius = 6f;
		FrameThickness = 3f;
		FrameColor = new Color(0, 0, 0, 120);
		IsHitTestVisible = false;
		DisableThemeColors = false;
	}

	public override void Draw(DrawContext dc)
	{
		Color cShadow = ShadowColor;
		Color color = CenterColor;
		Color componentMiner = BevelColor;
		if (!DisableThemeColors)
		{
			switch (SettingsManager.ThemeMode)
			{
			default:
				return;
			case ThemeMode.Light:
				color = CenterColor;
				componentMiner = BevelColor;
				break;
			case ThemeMode.Dark:
				color = DarkCenterColor;
				componentMiner = DarkBevelColor;
				cShadow = new Color(0, 0, 0, 100);
				break;
			case ThemeMode.DarkTransparent:
				color = Color.PremultiplyAlpha(DarkCenterColorWithAlpha);
				componentMiner = Color.PremultiplyAlpha(DarkBevelColorWithAlpha);
				cShadow = Color.PremultiplyAlpha(ShadowColor);
				break;
			case ThemeMode.LightTransparent:
				color = Color.PremultiplyAlpha(LightTransparentCenterColor);
				componentMiner = Color.PremultiplyAlpha(LightTransparentBevelColor);
				cShadow = Color.PremultiplyAlpha(LightTransparentShadowColor);
				break;
			case ThemeMode.Custom:
			{
				Color customInterfaceColor = SettingsManager.CustomInterfaceColor;
				Color c = AdjustBrightness(customInterfaceColor, 1.2f);
				color = Color.PremultiplyAlpha(customInterfaceColor);
				componentMiner = Color.PremultiplyAlpha(c);
				cShadow = Color.PremultiplyAlpha(new Color(0, 0, 0, 100));
				break;
			}
			}
		}
		switch (SettingsManager.ButtonStyleMode)
		{
		case ButtonStyleMode.Original:
			DrawOriginal(dc, color, componentMiner, cShadow);
			break;
		case ButtonStyleMode.Smooth:
			DrawSmooth(dc, color, componentMiner, cShadow);
			break;
		case ButtonStyleMode.OvalFramed:
			DrawOvalFramed(dc, color, componentMiner, cShadow);
			break;
		case ButtonStyleMode.Striped:
			DrawStriped(dc, color, componentMiner, cShadow);
			break;
		case ButtonStyleMode.Material:
			DrawMaterial(dc, color, componentMiner, cShadow);
			break;
		case ButtonStyleMode.WoodFramed:
			DrawFramedWood(dc, color, componentMiner, cShadow);
			break;
		case ButtonStyleMode.GoldFramed:
			DrawFramedGold(dc, color, componentMiner, cShadow);
			break;
		}
	}

	public override void MeasureOverride(Vector2 parentAvailableSize)
	{
		IsDrawRequired = BevelColor.A != 0 || CenterColor.A > 0;
		DesiredSize = Size;
	}

	public static void QueueBevelledRectangle(TexturedBatch2D texturedBatch, FlatBatch2D flatBatch, Vector2 c1, Vector2 c2, float depth, float bevelSize, Color color, Color bevelColor, Color shadowColor, float ambientLight, float directionalLight, float textureScale)
	{
		float num = MathUtils.Abs(bevelSize);
		Vector2 vector = c1 + new Vector2(num);
		Vector2 vector2 = c2 - new Vector2(num);
		Vector2 vector3 = c2 + new Vector2(1.5f * num);
		float x = c1.X;
		float x2 = vector.X;
		float x3 = vector2.X;
		float x4 = c2.X;
		float x5 = vector3.X;
		float y = c1.Y;
		float y2 = vector.Y;
		float y3 = vector2.Y;
		float y4 = c2.Y;
		float y5 = vector3.Y;
		float num2 = MathUtils.Saturate(((bevelSize > 0f) ? 1f : (-0.75f)) * directionalLight + ambientLight);
		float num3 = MathUtils.Saturate(((bevelSize > 0f) ? (-0.75f) : 1f) * directionalLight + ambientLight);
		float num4 = MathUtils.Saturate(((bevelSize > 0f) ? (-0.375f) : 0.5f) * directionalLight + ambientLight);
		float num5 = MathUtils.Saturate(((bevelSize > 0f) ? 0.5f : (-0.375f)) * directionalLight + ambientLight);
		float num6 = MathUtils.Saturate(0f * directionalLight + ambientLight);
		Color color2 = new Color((byte)(num4 * (float)(int)bevelColor.R), (byte)(num4 * (float)(int)bevelColor.G), (byte)(num4 * (float)(int)bevelColor.B), bevelColor.A);
		Color color3 = new Color((byte)(num5 * (float)(int)bevelColor.R), (byte)(num5 * (float)(int)bevelColor.G), (byte)(num5 * (float)(int)bevelColor.B), bevelColor.A);
		Color color4 = new Color((byte)(num2 * (float)(int)bevelColor.R), (byte)(num2 * (float)(int)bevelColor.G), (byte)(num2 * (float)(int)bevelColor.B), bevelColor.A);
		Color color5 = new Color((byte)(num3 * (float)(int)bevelColor.R), (byte)(num3 * (float)(int)bevelColor.G), (byte)(num3 * (float)(int)bevelColor.B), bevelColor.A);
		Color color6 = new Color((byte)(num6 * (float)(int)color.R), (byte)(num6 * (float)(int)color.G), (byte)(num6 * (float)(int)color.B), color.A);
		if (texturedBatch != null)
		{
			float num7 = textureScale / (float)texturedBatch.Texture.Width;
			float num8 = textureScale / (float)texturedBatch.Texture.Height;
			float num9 = x * num7;
			float num10 = y * num8;
			float x6 = num9;
			float x7 = (x2 - x) * num7 + num9;
			float x8 = (x3 - x) * num7 + num9;
			float x9 = (x4 - x) * num7 + num9;
			float y6 = num10;
			float y7 = (y2 - y) * num8 + num10;
			float y8 = (y3 - y) * num8 + num10;
			float y9 = (y4 - y) * num8 + num10;
			if (bevelColor.A > 0)
			{
				texturedBatch.QueueQuad(new Vector2(x, y), new Vector2(x2, y2), new Vector2(x3, y2), new Vector2(x4, y), depth, new Vector2(x6, y6), new Vector2(x7, y7), new Vector2(x8, y7), new Vector2(x9, y6), color4);
				texturedBatch.QueueQuad(new Vector2(x3, y2), new Vector2(x3, y3), new Vector2(x4, y4), new Vector2(x4, y), depth, new Vector2(x8, y7), new Vector2(x8, y8), new Vector2(x9, y9), new Vector2(x9, y6), color3);
				texturedBatch.QueueQuad(new Vector2(x, y4), new Vector2(x4, y4), new Vector2(x3, y3), new Vector2(x2, y3), depth, new Vector2(x6, y9), new Vector2(x9, y9), new Vector2(x8, y8), new Vector2(x7, y8), color5);
				texturedBatch.QueueQuad(new Vector2(x, y), new Vector2(x, y4), new Vector2(x2, y3), new Vector2(x2, y2), depth, new Vector2(x6, y6), new Vector2(x6, y9), new Vector2(x7, y8), new Vector2(x7, y7), color2);
			}
			if (color6.A > 0)
			{
				texturedBatch.QueueQuad(new Vector2(x2, y2), new Vector2(x3, y3), depth, new Vector2(x7, y7), new Vector2(x8, y8), color6);
			}
		}
		else if (flatBatch != null)
		{
			if (bevelColor.A > 0)
			{
				flatBatch.QueueQuad(new Vector2(x, y), new Vector2(x2, y2), new Vector2(x3, y2), new Vector2(x4, y), depth, color4);
				flatBatch.QueueQuad(new Vector2(x3, y2), new Vector2(x3, y3), new Vector2(x4, y4), new Vector2(x4, y), depth, color3);
				flatBatch.QueueQuad(new Vector2(x, y4), new Vector2(x4, y4), new Vector2(x3, y3), new Vector2(x2, y3), depth, color5);
				flatBatch.QueueQuad(new Vector2(x, y), new Vector2(x, y4), new Vector2(x2, y3), new Vector2(x2, y2), depth, color2);
			}
			if (color6.A > 0)
			{
				flatBatch.QueueQuad(new Vector2(x2, y2), new Vector2(x3, y3), depth, color6);
			}
		}
		if (bevelSize > 0f && flatBatch != null && shadowColor.A > 0)
		{
			Color color7 = shadowColor;
			Color color8 = new Color(0, 0, 0, 0);
			flatBatch.QueueTriangle(new Vector2(x, y4), new Vector2(x2, y5), new Vector2(x2, y4), depth, color8, color8, color7);
			flatBatch.QueueTriangle(new Vector2(x4, y), new Vector2(x4, y2), new Vector2(x5, y2), depth, color8, color7, color8);
			flatBatch.QueueTriangle(new Vector2(x4, y4), new Vector2(x4, y5), new Vector2(x5, y4), depth, color7, color8, color8);
			flatBatch.QueueQuad(new Vector2(x2, y4), new Vector2(x2, y5), new Vector2(x4, y5), new Vector2(x4, y4), depth, color7, color8, color8, color7);
			flatBatch.QueueQuad(new Vector2(x4, y2), new Vector2(x4, y4), new Vector2(x5, y4), new Vector2(x5, y2), depth, color7, color7, color8, color8);
		}
	}

	private void DrawOriginal(DrawContext dc, Color cFill, Color cBevel, Color cShadow)
	{
		if (Texture != null)
		{
			SamplerState samplerState = (TextureLinearFilter ? SamplerState.LinearWrap : SamplerState.PointWrap);
			FlatBatch2D flatBatch2D = dc.PrimitivesRenderer2D.FlatBatch(0, DepthStencilState.None);
			TexturedBatch2D texturedBatch2D = dc.PrimitivesRenderer2D.TexturedBatch(Texture, useAlphaTest: false, 0, DepthStencilState.None, null, null, samplerState);
			int count = flatBatch2D.TriangleVertices.Count;
			int count2 = texturedBatch2D.TriangleVertices.Count;
			QueueBevelledRectangle(texturedBatch2D, flatBatch2D, Vector2.Zero, ActualSize, 0f, BevelSize, cFill * GlobalColorTransform, cBevel * GlobalColorTransform, cShadow * GlobalColorTransform, AmbientLight, DirectionalLight, TextureScale);
			flatBatch2D.TransformTriangles(GlobalTransform, count);
			texturedBatch2D.TransformTriangles(GlobalTransform, count2);
		}
		else
		{
			FlatBatch2D flatBatch2D2 = dc.PrimitivesRenderer2D.FlatBatch(0, DepthStencilState.None);
			int count3 = flatBatch2D2.TriangleVertices.Count;
			QueueBevelledRectangle(null, flatBatch2D2, Vector2.Zero, ActualSize, 0f, BevelSize, cFill * GlobalColorTransform, cBevel * GlobalColorTransform, cShadow * GlobalColorTransform, AmbientLight, DirectionalLight, 0f);
			flatBatch2D2.TransformTriangles(GlobalTransform, count3);
		}
	}

	private void DrawSmooth(DrawContext dc, Color cFill, Color cBevel, Color cShadow)
	{
		FlatBatch2D flatBatch2D = dc.PrimitivesRenderer2D.FlatBatch(0, DepthStencilState.None);
		int count = flatBatch2D.TriangleVertices.Count;
		float num = MathUtils.Min(ActualSize.X, ActualSize.Y);
		QueueStyledRect(cornerRadius: MathUtils.Clamp((CornerRadius > 0f) ? CornerRadius : (num * 0.1f), 4f, 8f), batch: flatBatch2D, c1: Vector2.Zero, c2: ActualSize, depth: 0f, bevelSize: BevelSize, centerColor: cFill * GlobalColorTransform, bevelColor: cBevel * GlobalColorTransform, shadowColor: cShadow * GlobalColorTransform, ambientLight: AmbientLight, directionalLight: DirectionalLight, triToneDiagonal: false, gearWrap: false, gearHeight: 0f, gearPitch: 0f, gearDuty: 0f);
		flatBatch2D.TransformTriangles(GlobalTransform, count);
	}

	private void DrawOvalFramed(DrawContext dc, Color cFillIn, Color cBevel, Color cShadow)
	{
		FlatBatch2D flatBatch2D = dc.PrimitivesRenderer2D.FlatBatch(0, DepthStencilState.None);
		int count = flatBatch2D.TriangleVertices.Count;
		float num = MathUtils.Min(ActualSize.X, ActualSize.Y);
		float num2 = MathUtils.Clamp((CornerRadius > 0f) ? CornerRadius : (num * 0.08f), 4f, 10f);
		Color color = cFillIn;
		if (SettingsManager.ThemeMode == ThemeMode.Light)
		{
			color = new Color((byte)196, (byte)186, (byte)170, cFillIn.A);
		}
		float gearHeight = MathUtils.Clamp(num * 0.12f, 0.9f, 3f);
		float gearPitch = MathUtils.Clamp(num * 0.22f, 10f, 28f);
		float gearDuty = 1.55f;
		QueueStyledRect(flatBatch2D, Vector2.Zero, ActualSize, 0f, num2, BevelSize, color * GlobalColorTransform, cBevel * GlobalColorTransform, cShadow * GlobalColorTransform, AmbientLight, DirectionalLight, triToneDiagonal: false, gearWrap: true, gearHeight, gearPitch, gearDuty);
		float num3 = MathUtils.Max(MathUtils.Abs(BevelSize), 2f) + 1f;
		float num4 = MathUtils.Max(0f, num2 - num3);
		float num5 = num3 + num4;
		float num6 = num3 + num4;
		float num7 = ActualSize.X - (num3 + num4);
		float num8 = ActualSize.Y - (num3 + num4);
		if (num7 > num5 && num8 > num6)
		{
			Color color2 = AdjustBrightness(color, 0.95f) * GlobalColorTransform;
			Color color3 = AdjustBrightness(color, 1.05f) * GlobalColorTransform;
			Color color4 = AdjustBrightness(Color.White, 1f);
			Color color5 = AdjustBrightness(Color.White, 0.96f);
			float num9 = MathUtils.Clamp((num7 - num5) * 0.06f, 6f, 14f);
			float num10 = MathUtils.Clamp(num9 * 0.5f, 3f, 8f);
			for (float num11 = num5; num11 < num7; num11 += num9)
			{
				float num12 = num11;
				float num13 = MathUtils.Min(num11 + num10, num7);
				if (num13 > num12)
				{
					Color color6 = new Color(color2.R * color4.R / 255, color2.G * color4.G / 255, color2.B * color4.B / 255, color2.A);
					Color color7 = new Color(color2.R * color5.R / 255, color2.G * color5.G / 255, color2.B * color5.B / 255, color2.A);
					flatBatch2D.QueueQuad(new Vector2(num12, num6), new Vector2(num13, num6), new Vector2(num13, num8), new Vector2(num12, num8), 0f, color6, color6, color7, color7);
				}
				float num14 = MathUtils.Min(num11 + num10, num7);
				float num15 = MathUtils.Min(num11 + num9, num7);
				if (num15 > num14)
				{
					Color color8 = new Color(color3.R * color4.R / 255, color3.G * color4.G / 255, color3.B * color4.B / 255, color3.A);
					Color color9 = new Color(color3.R * color5.R / 255, color3.G * color5.G / 255, color3.B * color5.B / 255, color3.A);
					flatBatch2D.QueueQuad(new Vector2(num14, num6), new Vector2(num15, num6), new Vector2(num15, num8), new Vector2(num14, num8), 0f, color8, color8, color9, color9);
				}
			}
		}
		flatBatch2D.TransformTriangles(GlobalTransform, count);
	}

	private static void QueueRoundedRectFill(FlatBatch2D batch, Vector2 c1, Vector2 c2, float radius, float depth, Color fill, int segPerCorner = 8)
	{
		float x = c1.X;
		float y = c1.Y;
		float x2 = c2.X;
		float y2 = c2.Y;
		float num = MathUtils.Clamp(radius, 0f, 0.5f * MathUtils.Min(x2 - x, y2 - y));
		float x3 = x + num;
		float y3 = y + num;
		float x4 = x2 - num;
		float y4 = y2 - num;
		batch.QueueQuad(new Vector2(x3, y3), new Vector2(x4, y4), depth, fill);
		if (num > 0f)
		{
			batch.QueueQuad(new Vector2(x3, y), new Vector2(x4, y3), depth, fill);
			batch.QueueQuad(new Vector2(x3, y4), new Vector2(x4, y2), depth, fill);
			batch.QueueQuad(new Vector2(x, y3), new Vector2(x3, y4), depth, fill);
			batch.QueueQuad(new Vector2(x4, y3), new Vector2(x2, y4), depth, fill);
			QueueQuarterFill(batch, new Vector2(x3, y3), num, (float)Math.PI, 4.712389f, depth, fill, segPerCorner);
			QueueQuarterFill(batch, new Vector2(x4, y3), num, 4.712389f, (float)Math.PI * 2f, depth, fill, segPerCorner);
			QueueQuarterFill(batch, new Vector2(x4, y4), num, 0f, (float)Math.PI / 2f, depth, fill, segPerCorner);
			QueueQuarterFill(batch, new Vector2(x3, y4), num, (float)Math.PI / 2f, (float)Math.PI, depth, fill, segPerCorner);
		}
	}

	private static void QueueQuarterFill(FlatBatch2D batch, Vector2 center, float r, float a0, float a1, float depth, Color color, int segments)
	{
		float value = (a1 - a0) / (float)segments;
		for (int i = 0; i < segments; i++)
		{
			float x = a0 + (float)i * value;
			float x2 = a0 + (float)(i + 1) * value;
			Vector2 p = center + new Vector2(MathUtils.Cos(x), MathUtils.Sin(x)) * r;
			Vector2 p2 = center + new Vector2(MathUtils.Cos(x2), MathUtils.Sin(x2)) * r;
			batch.QueueTriangle(center, p, p2, depth, color, color, color);
		}
	}

	private static void QueueQuarterRing(FlatBatch2D batch, Vector2 center, float rOuter, float rInner, float a0, float a1, float depth, Color color, int segments)
	{
		float num = (a1 - a0) / (float)segments;
		for (int i = 0; i < segments; i++)
		{
			float x = a0 + (float)i * num;
			float x2 = a0 + (float)(i + 1) * num;
			Vector2 p = center + new Vector2(MathUtils.Cos(x), MathUtils.Sin(x)) * rOuter;
			Vector2 p2 = center + new Vector2(MathUtils.Cos(x2), MathUtils.Sin(x2)) * rOuter;
			Vector2 p3 = center + new Vector2(MathUtils.Cos(x), MathUtils.Sin(x)) * rInner;
			Vector2 vector = center + new Vector2(MathUtils.Cos(x2), MathUtils.Sin(x2)) * rInner;
			batch.QueueTriangle(p, p2, vector, depth, color, color, color);
			batch.QueueTriangle(p, vector, p3, depth, color, color, color);
		}
	}

	private static void QueueEdgeShadow(FlatBatch2D batch, Vector2 c1, Vector2 c2, float radius, float depth, Color shadow)
	{
		float num = 3f;
		float item = c1.X;
		float valuesDictionary2 = c1.Y;
		float spawnChunk = c2.X;
		float value = c2.Y;
		Color color = new Color((int)shadow.R, (int)shadow.G, (int)shadow.B, 0);
		batch.QueueQuad(new Vector2(item + radius, value - num), new Vector2(spawnChunk - radius, value - num), new Vector2(spawnChunk - radius, value), new Vector2(item + radius, value), depth, color, color, shadow, shadow);
		batch.QueueQuad(new Vector2(spawnChunk - num, valuesDictionary2 + radius), new Vector2(spawnChunk, valuesDictionary2 + radius), new Vector2(spawnChunk, value - radius), new Vector2(spawnChunk - num, value - radius), depth, color, shadow, shadow, color);
	}

	private static Color AdjustBrightness(Color c, float mul)
	{
		byte r = (byte)MathUtils.Clamp((int)((float)(int)c.R * mul), 0, 255);
		byte list = (byte)MathUtils.Clamp((int)((float)(int)c.G * mul), 0, 255);
		byte b = (byte)MathUtils.Clamp((int)((float)(int)c.B * mul), 0, 255);
		return new Color(r, list, b, c.A);
	}

	private static void QueueStyledRect(FlatBatch2D batch, Vector2 c1, Vector2 c2, float depth, float cornerRadius, float bevelSize, Color centerColor, Color bevelColor, Color shadowColor, float ambientLight, float directionalLight, bool triToneDiagonal, bool gearWrap, float gearHeight, float gearPitch, float gearDuty)
	{
		float list = c1.X;
		float y = c1.Y;
		float key = c2.X;
		float componentCreature = c2.Y;
		float flag = MathUtils.Clamp(cornerRadius, 0f, 0.5f * MathUtils.Min(key - list, componentCreature - y));
		float position = MathUtils.Max(MathUtils.Abs(bevelSize), 1f);
		float v = MathUtils.Max(flag - position, 0f);
		float num = MathUtils.Saturate(((bevelSize > 0f) ? 1f : (-0.75f)) * directionalLight + ambientLight);
		float num2 = MathUtils.Saturate(((bevelSize > 0f) ? (-0.75f) : 1f) * directionalLight + ambientLight);
		float position2 = MathUtils.Saturate(((bevelSize > 0f) ? (-0.375f) : 0.5f) * directionalLight + ambientLight);
		float v2 = MathUtils.Saturate(((bevelSize > 0f) ? 0.5f : (-0.375f)) * directionalLight + ambientLight);
		float num3 = MathUtils.Saturate(ambientLight);
		Color item2 = new Color((byte)(num * (float)(int)bevelColor.R), (byte)(num * (float)(int)bevelColor.G), (byte)(num * (float)(int)bevelColor.B), bevelColor.A);
		Color point = new Color((byte)(num2 * (float)(int)bevelColor.R), (byte)(num2 * (float)(int)bevelColor.G), (byte)(num2 * (float)(int)bevelColor.B), bevelColor.A);
		Color item3 = new Color((byte)(position2 * (float)(int)bevelColor.R), (byte)(position2 * (float)(int)bevelColor.G), (byte)(position2 * (float)(int)bevelColor.B), bevelColor.A);
		Color color = new Color((byte)(v2 * (float)(int)bevelColor.R), (byte)(v2 * (float)(int)bevelColor.G), (byte)(v2 * (float)(int)bevelColor.B), bevelColor.A);
		Color color2 = new Color((byte)(num3 * (float)(int)centerColor.R), (byte)(num3 * (float)(int)centerColor.G), (byte)(num3 * (float)(int)centerColor.B), centerColor.A);
		if (position > 0f)
		{
			batch.QueueQuad(new Vector2(list + flag, y), new Vector2(key - flag, y + position), depth, item2);
			batch.QueueQuad(new Vector2(list + flag, componentCreature - position), new Vector2(key - flag, componentCreature), depth, color);
			batch.QueueQuad(new Vector2(list, y + flag), new Vector2(list + position, componentCreature - flag), depth, item3);
			batch.QueueQuad(new Vector2(key - position, y + flag), new Vector2(key, componentCreature - flag), depth, point);
			if (flag > 0f)
			{
				QueueQuarterRing(batch, new Vector2(list + flag, y + flag), flag, v, (float)Math.PI, 4.712389f, depth, item2, 10);
				QueueQuarterRing(batch, new Vector2(key - flag, y + flag), flag, v, 4.712389f, (float)Math.PI * 2f, depth, item2, 10);
				QueueQuarterRing(batch, new Vector2(key - flag, componentCreature - flag), flag, v, 0f, (float)Math.PI / 2f, depth, color, 10);
				QueueQuarterRing(batch, new Vector2(list + flag, componentCreature - flag), flag, v, (float)Math.PI / 2f, (float)Math.PI, depth, color, 10);
			}
		}
		QueueRoundedRectFill(batch, new Vector2(list + position, y + position), new Vector2(key - position, componentCreature - position), v, depth, color2);
		if (bevelSize > 0f && shadowColor.A > 0)
		{
			QueueEdgeShadow(batch, c1, c2, flag, depth, shadowColor);
		}
		if (triToneDiagonal)
		{
			float num4 = 1.04f;
			float num5 = 0.88f;
			Color color3 = new Color((byte)MathUtils.Clamp((int)((float)(int)color2.R * num4), 0, 255), (byte)MathUtils.Clamp((int)((float)(int)color2.G * num4), 0, 255), (byte)MathUtils.Clamp((int)((float)(int)color2.B * num4), 0, 255), color2.A);
			Color color4 = new Color((byte)MathUtils.Clamp((int)((float)(int)color2.R * num5), 0, 255), (byte)MathUtils.Clamp((int)((float)(int)color2.G * num5), 0, 255), (byte)MathUtils.Clamp((int)((float)(int)color2.B * num5), 0, 255), color2.A);
			float x = list + position + v;
			float y2 = y + position + v;
			float x2 = key - position - v;
			float y3 = componentCreature - position - v;
			Vector2 p = new Vector2(x, y2);
			Vector2 p2 = new Vector2(x2, y2);
			Vector2 p3 = new Vector2(x, y3);
			Vector2 vector = new Vector2(x2, y3);
			batch.QueueTriangle(p, p2, vector, depth, color3, color2, color4);
			batch.QueueTriangle(p, vector, p3, depth, color3, color4, color2);
		}
		if (!gearWrap || !(gearHeight > 0f) || !(gearPitch > 0f) || !(gearDuty > 0f))
		{
			return;
		}
		float num6 = y + flag;
		float num7 = componentCreature - flag;
		float num8 = list + flag;
		float num9 = key - flag;
		Color color5 = new Color((byte)MathUtils.Clamp((int)((float)(int)item2.R * 0.95f), 0, 255), (byte)MathUtils.Clamp((int)((float)(int)item2.G * 0.95f), 0, 255), (byte)MathUtils.Clamp((int)((float)(int)item2.B * 0.95f), 0, 255), (byte)MathUtils.Clamp(item2.A, 120, 255));
		Color color6 = new Color((byte)MathUtils.Clamp((int)((float)(int)color.R * 0.9f), 0, 255), (byte)MathUtils.Clamp((int)((float)(int)color.G * 0.9f), 0, 255), (byte)MathUtils.Clamp((int)((float)(int)color.B * 0.9f), 0, 255), (byte)MathUtils.Clamp(color.A, 120, 255));
		Color color7 = new Color((byte)MathUtils.Clamp((int)((float)(int)item3.R * 0.95f), 0, 255), (byte)MathUtils.Clamp((int)((float)(int)item3.G * 0.95f), 0, 255), (byte)MathUtils.Clamp((int)((float)(int)item3.B * 0.95f), 0, 255), (byte)MathUtils.Clamp(item3.A, 120, 255));
		Color color8 = new Color((byte)MathUtils.Clamp((int)((float)(int)point.R * 0.9f), 0, 255), (byte)MathUtils.Clamp((int)((float)(int)point.G * 0.9f), 0, 255), (byte)MathUtils.Clamp((int)((float)(int)point.B * 0.9f), 0, 255), (byte)MathUtils.Clamp(point.A, 120, 255));
		float num10 = num9 - num8;
		int num11 = MathUtils.Max(1, (int)MathUtils.Floor(num10 / gearPitch));
		float num12 = gearPitch * gearDuty;
		for (int i = 0; i < num11; i++)
		{
			if ((i & 1) == 0)
			{
				float num13 = num8 + (float)i * gearPitch + (gearPitch - num12) * 0.5f;
				float num14 = MathUtils.Min(num13 + num12, num9);
				Vector2 p4 = new Vector2(num13, y);
				Vector2 p5 = new Vector2(num14, y);
				Vector2 vector2 = new Vector2(num14 - num12 * 0.15f, y - gearHeight);
				Vector2 p6 = new Vector2(num13 + num12 * 0.15f, y - gearHeight);
				batch.QueueTriangle(p4, p5, vector2, depth, color5, color5, color5);
				batch.QueueTriangle(p4, vector2, p6, depth, color5, color5, color5);
			}
		}
		float num15 = num9 - num8;
		int num16 = MathUtils.Max(1, (int)MathUtils.Floor(num15 / gearPitch));
		float num17 = gearPitch * gearDuty;
		for (int j = 0; j < num16; j++)
		{
			if ((j & 1) != 0)
			{
				float num18 = num8 + (float)j * gearPitch + (gearPitch - num17) * 0.5f;
				float num19 = MathUtils.Min(num18 + num17, num9);
				Vector2 p7 = new Vector2(num18, componentCreature);
				Vector2 p8 = new Vector2(num19, componentCreature);
				Vector2 vector3 = new Vector2(num19 - num17 * 0.15f, componentCreature + gearHeight);
				Vector2 p9 = new Vector2(num18 + num17 * 0.15f, componentCreature + gearHeight);
				batch.QueueTriangle(p7, p8, vector3, depth, color6, color6, color6);
				batch.QueueTriangle(p7, vector3, p9, depth, color6, color6, color6);
			}
		}
		float num20 = num7 - num6;
		int num21 = MathUtils.Max(1, (int)MathUtils.Floor(num20 / gearPitch));
		float num22 = gearPitch * gearDuty;
		for (int k = 0; k < num21; k++)
		{
			if ((k & 1) == 0)
			{
				float num23 = num6 + (float)k * gearPitch + (gearPitch - num22) * 0.5f;
				float num24 = MathUtils.Min(num23 + num22, num7);
				Vector2 p10 = new Vector2(list, num23);
				Vector2 p11 = new Vector2(list, num24);
				Vector2 vector4 = new Vector2(list - gearHeight, num24 - num22 * 0.15f);
				Vector2 p12 = new Vector2(list - gearHeight, num23 + num22 * 0.15f);
				batch.QueueTriangle(p10, p11, vector4, depth, color7, color7, color7);
				batch.QueueTriangle(p10, vector4, p12, depth, color7, color7, color7);
			}
		}
		float num25 = num7 - num6;
		int num26 = MathUtils.Max(1, (int)MathUtils.Floor(num25 / gearPitch));
		float num27 = gearPitch * gearDuty;
		for (int l = 0; l < num26; l++)
		{
			if ((l & 1) != 0)
			{
				float num28 = num6 + (float)l * gearPitch + (gearPitch - num27) * 0.5f;
				float num29 = MathUtils.Min(num28 + num27, num7);
				Vector2 p13 = new Vector2(key, num28);
				Vector2 p14 = new Vector2(key, num29);
				Vector2 vector5 = new Vector2(key + gearHeight, num29 - num27 * 0.15f);
				Vector2 p15 = new Vector2(key + gearHeight, num28 + num27 * 0.15f);
				batch.QueueTriangle(p13, p14, vector5, depth, color8, color8, color8);
				batch.QueueTriangle(p13, vector5, p15, depth, color8, color8, color8);
			}
		}
	}

	private void DrawStriped(DrawContext dc, Color cFillIn, Color cBevel, Color cShadow)
	{
		FlatBatch2D entity = dc.PrimitivesRenderer2D.FlatBatch(0, DepthStencilState.None);
		int componentCreature = entity.TriangleVertices.Count;
		float ex = ActualSize.X;
		float y = ActualSize.Y;
		float num = MathUtils.Min(ex, y);
		float num2 = MathUtils.Clamp((CornerRadius > 0f) ? CornerRadius : (num * 0.09f), 4f, 12f);
		float num3 = MathUtils.Max(1f, FrameThickness);
		QueueRoundedRectFill(entity, Vector2.Zero, ActualSize, num2, 0f, cFillIn * GlobalColorTransform, 12);
		Color color = AdjustBrightness(cFillIn, 1.05f) * GlobalColorTransform;
		Color color2 = AdjustBrightness(cFillIn, 0.9f) * GlobalColorTransform;
		QueueQuarterRing(entity, new Vector2(num2, num2), num2, num2 - num3, (float)Math.PI, 4.712389f, 0f, color, 12);
		QueueQuarterRing(entity, new Vector2(ex - num2, num2), num2, num2 - num3, 4.712389f, (float)Math.PI * 2f, 0f, color, 12);
		QueueQuarterRing(entity, new Vector2(ex - num2, y - num2), num2, num2 - num3, 0f, (float)Math.PI / 2f, 0f, color2, 12);
		QueueQuarterRing(entity, new Vector2(num2, y - num2), num2, num2 - num3, (float)Math.PI / 2f, (float)Math.PI, 0f, color2, 12);
		entity.QueueQuad(new Vector2(num2, 0f), new Vector2(ex - num2, num3), 0f, color);
		entity.QueueQuad(new Vector2(0f, num2), new Vector2(num3, y - num2), 0f, color);
		entity.QueueQuad(new Vector2(ex - num3, num2), new Vector2(ex, y - num2), 0f, color2);
		entity.QueueQuad(new Vector2(num2, y - num3), new Vector2(ex - num2, y), 0f, color2);
		float num4 = num2 + 2f;
		float num5 = num4;
		float num6 = num4;
		float num7 = ex - num4;
		float num8 = y - num4;
		float num9 = MathUtils.Clamp(0.07f * (num7 - num5 + (num8 - num6)), 6f, 16f);
		float num10 = num9 * 2f;
		Color color3 = AdjustBrightness(cFillIn, 0.92f) * GlobalColorTransform;
		Color color4 = AdjustBrightness(cFillIn, 1.08f) * GlobalColorTransform;
		Color color5 = AdjustBrightness(cFillIn, 0.96f) * GlobalColorTransform;
		Color color6 = AdjustBrightness(cFillIn, 1.12f) * GlobalColorTransform;
		for (float num11 = num6; num11 < num8; num11 += num10)
		{
			float num12 = num11;
			float num13 = MathUtils.Min(num11 + num9, num8);
			if (num13 > num12)
			{
				entity.QueueQuad(new Vector2(num5, num12), new Vector2(num7, num12), new Vector2(num7, num13), new Vector2(num5, num13), 0f, color3);
			}
			float num14 = num11 + num9;
			float num15 = MathUtils.Min(num11 + num10, num8);
			if (num15 > num14)
			{
				entity.QueueQuad(new Vector2(num5, num14), new Vector2(num7, num14), new Vector2(num7, num15), new Vector2(num5, num15), 0f, color4);
			}
		}
		for (float num16 = num5 + num9 * 0.5f; num16 < num7; num16 += num10)
		{
			float num17 = num16;
			float num18 = MathUtils.Min(num16 + num9, num7);
			if (num18 > num17)
			{
				entity.QueueQuad(new Vector2(num17, num6), new Vector2(num18, num6), new Vector2(num18, num8), new Vector2(num17, num8), 0f, color5);
			}
			float num19 = num16 + num9;
			float num20 = MathUtils.Min(num16 + num10, num7);
			if (num20 > num19)
			{
				entity.QueueQuad(new Vector2(num19, num6), new Vector2(num20, num6), new Vector2(num20, num8), new Vector2(num19, num8), 0f, color6);
			}
		}
		byte a = (byte)MathUtils.Clamp((int)((float)(int)cFillIn.A * 0.12f), 0, 255);
		Color color7 = Color.PremultiplyAlpha(new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, a)) * GlobalColorTransform;
		Color color8 = Color.PremultiplyAlpha(new Color(255, 255, 255, 0)) * GlobalColorTransform;
		Vector2 p = new Vector2(num5, num6);
		Vector2 p2 = new Vector2(num7, num6);
		Vector2 vector = new Vector2(num7, num8);
		Vector2 p3 = new Vector2(num5, num8);
		entity.QueueTriangle(p, p2, vector, 0f, color7, color8, color8);
		entity.QueueTriangle(p, vector, p3, 0f, color7, color8, color8);
		if (cShadow.A > 0)
		{
			QueueEdgeShadow(entity, Vector2.Zero, ActualSize, num2, 0f, cShadow * GlobalColorTransform);
		}
		entity.TransformTriangles(GlobalTransform, componentCreature);
	}

	private void DrawMaterial(DrawContext dc, Color cFillIn, Color cBevel, Color cShadow)
	{
		FlatBatch2D flatBatch2D = dc.PrimitivesRenderer2D.FlatBatch(0, DepthStencilState.None);
		int array = flatBatch2D.TriangleVertices.Count;
		float num = MathUtils.Min(ActualSize.X, ActualSize.Y);
		float array2 = MathUtils.Clamp((CornerRadius > 0f) ? CornerRadius : (num * 0.08f), 4f, 12f);
		Color spawnEntityData = cFillIn * GlobalColorTransform;
		Color color = AdjustBrightness(cFillIn, 0.85f) * GlobalColorTransform;
		QueueRoundedRectFill(flatBatch2D, Vector2.Zero, ActualSize, array2, 0f, spawnEntityData, 10);
		float num2 = 1f;
		QueueQuarterRing(flatBatch2D, new Vector2(array2, array2), array2, array2 - num2, (float)Math.PI, 4.712389f, 0f, color, 10);
		QueueQuarterRing(flatBatch2D, new Vector2(ActualSize.X - array2, array2), array2, array2 - num2, 4.712389f, (float)Math.PI * 2f, 0f, color, 10);
		QueueQuarterRing(flatBatch2D, new Vector2(ActualSize.X - array2, ActualSize.Y - array2), array2, array2 - num2, 0f, (float)Math.PI / 2f, 0f, color, 10);
		QueueQuarterRing(flatBatch2D, new Vector2(array2, ActualSize.Y - array2), array2, array2 - num2, (float)Math.PI / 2f, (float)Math.PI, 0f, color, 10);
		flatBatch2D.QueueQuad(new Vector2(array2, 0f), new Vector2(ActualSize.X - array2, num2), 0f, color);
		flatBatch2D.QueueQuad(new Vector2(array2, ActualSize.Y - num2), new Vector2(ActualSize.X - array2, ActualSize.Y), 0f, color);
		flatBatch2D.QueueQuad(new Vector2(0f, array2), new Vector2(num2, ActualSize.Y - array2), 0f, color);
		flatBatch2D.QueueQuad(new Vector2(ActualSize.X - num2, array2), new Vector2(ActualSize.X, ActualSize.Y - array2), 0f, color);
		flatBatch2D.TransformTriangles(GlobalTransform, array);
	}

	private void DrawFramedWood(DrawContext dc, Color cFillIn, Color cBevel, Color cShadow)
	{
		FlatBatch2D flatBatch2D = dc.PrimitivesRenderer2D.FlatBatch(0, DepthStencilState.None);
		int stringBuilder = flatBatch2D.TriangleVertices.Count;
		float num = MathUtils.Min(ActualSize.X, ActualSize.Y);
		float spawnsDatum = MathUtils.Clamp((CornerRadius > 0f) ? CornerRadius : (num * 0.1f), 6f, 12f);
		QueueRoundedRectFill(flatBatch2D, new Vector2(FrameThickness, FrameThickness), new Vector2(ActualSize.X - FrameThickness, ActualSize.Y - FrameThickness), MathUtils.Max(0f, spawnsDatum - FrameThickness), 0f, cFillIn * GlobalColorTransform, 10);
		Color c = new Color(158, 123, 84, (int)cBevel.A);
		Color color = AdjustBrightness(c, 1.15f);
		Color color2 = AdjustBrightness(c, 0.8f);
		Color color3 = AdjustBrightness(c, 0.95f);
		float num2 = MathUtils.Max(1f, FrameThickness);
		float num3 = 0f;
		float num4 = 0f;
		float x = ActualSize.X;
		float y = ActualSize.Y;
		flatBatch2D.QueueQuad(new Vector2(num3 + spawnsDatum, num4), new Vector2(x - spawnsDatum, num4 + num2), 0f, color * GlobalColorTransform);
		flatBatch2D.QueueQuad(new Vector2(num3 + spawnsDatum, y - num2), new Vector2(x - spawnsDatum, y), 0f, color2 * GlobalColorTransform);
		flatBatch2D.QueueQuad(new Vector2(num3, num4 + spawnsDatum), new Vector2(num3 + num2, y - spawnsDatum), 0f, color3 * GlobalColorTransform);
		flatBatch2D.QueueQuad(new Vector2(x - num2, num4 + spawnsDatum), new Vector2(x, y - spawnsDatum), 0f, color2 * GlobalColorTransform);
		QueueQuarterRing(flatBatch2D, new Vector2(num3 + spawnsDatum, num4 + spawnsDatum), spawnsDatum, spawnsDatum - num2, (float)Math.PI, 4.712389f, 0f, color * GlobalColorTransform, 12);
		QueueQuarterRing(flatBatch2D, new Vector2(x - spawnsDatum, num4 + spawnsDatum), spawnsDatum, spawnsDatum - num2, 4.712389f, (float)Math.PI * 2f, 0f, color3 * GlobalColorTransform, 12);
		QueueQuarterRing(flatBatch2D, new Vector2(x - spawnsDatum, y - spawnsDatum), spawnsDatum, spawnsDatum - num2, 0f, (float)Math.PI / 2f, 0f, color2 * GlobalColorTransform, 12);
		QueueQuarterRing(flatBatch2D, new Vector2(num3 + spawnsDatum, y - spawnsDatum), spawnsDatum, spawnsDatum - num2, (float)Math.PI / 2f, (float)Math.PI, 0f, color2 * GlobalColorTransform, 12);
		QueueEdgeShadow(flatBatch2D, Vector2.Zero, ActualSize, spawnsDatum, 0f, cShadow * GlobalColorTransform);
		flatBatch2D.TransformTriangles(GlobalTransform, stringBuilder);
	}

	private void DrawFramedGold(DrawContext dc, Color cFillIn, Color cBevel, Color cShadow)
	{
		FlatBatch2D flatBatch2D = dc.PrimitivesRenderer2D.FlatBatch(0, DepthStencilState.None);
		int count = flatBatch2D.TriangleVertices.Count;
		float num = MathUtils.Min(ActualSize.X, ActualSize.Y);
		float num2 = MathUtils.Clamp((CornerRadius > 0f) ? CornerRadius : (num * 0.1f), 6f, 12f);
		QueueRoundedRectFill(flatBatch2D, new Vector2(FrameThickness, FrameThickness), new Vector2(ActualSize.X - FrameThickness, ActualSize.Y - FrameThickness), MathUtils.Max(0f, num2 - FrameThickness), 0f, cFillIn * GlobalColorTransform, 10);
		Color c = new Color(212, 175, 55, 255);
		Color color = AdjustBrightness(c, 1.25f);
		Color color2 = AdjustBrightness(c, 1.1f);
		Color color3 = AdjustBrightness(c, 0.78f);
		float num3 = MathUtils.Max(1f, FrameThickness);
		float num4 = 0f;
		float num5 = 0f;
		float x = ActualSize.X;
		float y = ActualSize.Y;
		flatBatch2D.QueueQuad(new Vector2(num4 + num2, num5), new Vector2(x - num2, num5 + num3), 0f, color * GlobalColorTransform);
		flatBatch2D.QueueQuad(new Vector2(num4, num5 + num2), new Vector2(num4 + num3, y - num2), 0f, color2 * GlobalColorTransform);
		flatBatch2D.QueueQuad(new Vector2(x - num3, num5 + num2), new Vector2(x, y - num2), 0f, color3 * GlobalColorTransform);
		flatBatch2D.QueueQuad(new Vector2(num4 + num2, y - num3), new Vector2(x - num2, y), 0f, color3 * GlobalColorTransform);
		QueueQuarterRing(flatBatch2D, new Vector2(num4 + num2, num5 + num2), num2, num2 - num3, (float)Math.PI, 4.712389f, 0f, color * GlobalColorTransform, 12);
		QueueQuarterRing(flatBatch2D, new Vector2(x - num2, num5 + num2), num2, num2 - num3, 4.712389f, (float)Math.PI * 2f, 0f, color2 * GlobalColorTransform, 12);
		QueueQuarterRing(flatBatch2D, new Vector2(x - num2, y - num2), num2, num2 - num3, 0f, (float)Math.PI / 2f, 0f, color3 * GlobalColorTransform, 12);
		QueueQuarterRing(flatBatch2D, new Vector2(num4 + num2, y - num2), num2, num2 - num3, (float)Math.PI / 2f, (float)Math.PI, 0f, color3 * GlobalColorTransform, 12);
		float num6 = MathUtils.Clamp(num3 * 0.45f, 0.5f, 2f);
		Color color4 = AdjustBrightness(c, 1.35f) * GlobalColorTransform;
		flatBatch2D.QueueQuad(new Vector2(num4 + num2, num5), new Vector2(x - num2, num5 + num6), 0f, color4);
		flatBatch2D.QueueQuad(new Vector2(num4, num5 + num2), new Vector2(num4 + num6, y - num2), 0f, color4);
		flatBatch2D.TransformTriangles(GlobalTransform, count);
	}
}
