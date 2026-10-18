using System;
using Engine;
using Engine.Graphics;

namespace Game;

public class GridValueBarWidget : Widget
{
	private float m_value;

	private int m_barsCount;

	private int m_itemsPerRow;

	private Subtexture m_barSubtexture;

	private float m_flashCount;

	public float Value
	{
		get
		{
			return m_value;
		}
		set
		{
			m_value = MathUtils.Saturate(value);
		}
	}

	public int BarsCount
	{
		get
		{
			return m_barsCount;
		}
		set
		{
			m_barsCount = MathUtils.Clamp(value, 1, 10000);
		}
	}

	public int ItemsPerRow
	{
		get
		{
			return m_itemsPerRow;
		}
		set
		{
			m_itemsPerRow = MathUtils.Clamp(value, 1, 1000);
		}
	}

	public bool StartFromBottom { get; set; } = true;

	public bool FlipDirection { get; set; }

	public Vector2 BarSize { get; set; }

	public Vector2 Spacing { get; set; }

	public Color LitBarColor { get; set; } = new Color(16, 140, 0);

	public Color LitBarColor2 { get; set; } = Color.Transparent;

	public Color UnlitBarColor { get; set; } = new Color(48, 48, 48);

	public bool BarBlending { get; set; } = true;

	public bool HalfBars { get; set; }

	public Subtexture BarSubtexture
	{
		get
		{
			return m_barSubtexture;
		}
		set
		{
			if (value != m_barSubtexture)
			{
				m_barSubtexture = value;
			}
		}
	}

	public bool TextureLinearFilter { get; set; } = true;

	public float MaxHeight { get; set; } = 300f;

	public int ExtraLivesCount { get; set; }

	public GridValueBarWidget()
	{
		IsHitTestVisible = false;
		BarSize = new Vector2(24f);
		Spacing = new Vector2(2f, 2f);
		ItemsPerRow = 10;
		StartFromBottom = true;
	}

	public void Flash(int count)
	{
		m_flashCount = MathUtils.Max(m_flashCount, count);
	}

	public override void Draw(DrawContext dc)
	{
		BaseBatch baseBatch = ((BarSubtexture == null) ? ((BaseBatch)dc.PrimitivesRenderer2D.FlatBatch(0, DepthStencilState.None)) : ((BaseBatch)dc.PrimitivesRenderer2D.TexturedBatch(BarSubtexture.Texture, useAlphaTest: false, 0, DepthStencilState.None, null, null, TextureLinearFilter ? SamplerState.LinearClamp : SamplerState.PointClamp)));
		int start = 0;
		int count;
		if (baseBatch is TexturedBatch2D)
		{
			count = ((TexturedBatch2D)baseBatch).TriangleVertices.Count;
		}
		else
		{
			start = ((FlatBatch2D)baseBatch).LineVertices.Count;
			count = ((FlatBatch2D)baseBatch).TriangleVertices.Count;
		}
		int num = (int)Math.Ceiling((float)BarsCount / (float)ItemsPerRow);
		if (num == 0)
		{
			return;
		}
		int num2 = 6;
		float num3 = (float)num2 * BarSize.Y + (float)(num2 - 1) * Spacing.Y;
		if (MaxHeight > 0f && MaxHeight < num3)
		{
			num3 = MaxHeight;
		}
		float num4 = (float)num * BarSize.Y + (float)(num - 1) * Spacing.Y;
		float num5 = Spacing.Y;
		if (num4 > num3 && num > 1)
		{
			num5 = (num3 - (float)num * BarSize.Y) / (float)(num - 1);
			num4 = num3;
		}
		float num6 = 0f;
		if (StartFromBottom)
		{
			num6 = 0f - (num4 - BarSize.Y);
		}
		if (ExtraLivesCount > 0)
		{
			int num7 = num - 1;
			int num8 = MathUtils.Min(ExtraLivesCount, ItemsPerRow);
			for (int i = 0; i < num8; i++)
			{
				float num9 = (float)i * (BarSize.X + Spacing.X);
				Vector2 vector = new Vector2(y: ((!StartFromBottom) ? ((float)num7 * (BarSize.Y + num5)) : ((float)(num - 1 - num7) * (BarSize.Y + num5))) + num5 / 2f + num6, x: num9 + Spacing.X / 2f) + BarSize / 2f;
				Vector2 vector2 = BarSize * 1.7f;
				Vector2 vector3 = vector - vector2 / 2f;
				if (baseBatch is TexturedBatch2D)
				{
					((TexturedBatch2D)baseBatch).QueueQuad(vector3, vector3 + vector2, 0f, BarSubtexture.TopLeft, BarSubtexture.BottomRight, Color.Yellow * GlobalColorTransform);
				}
				else
				{
					((FlatBatch2D)baseBatch).QueueQuad(vector3, vector3 + vector2, 0f, Color.Yellow * GlobalColorTransform);
				}
			}
		}
		int num10 = (HalfBars ? 1 : 2);
		for (int num11 = 2 * BarsCount - num10; num11 >= 0; num11 -= num10)
		{
			bool flag = num11 % 2 == 0;
			int num12 = num11 / 2;
			int num13 = num12 % ItemsPerRow;
			int num14 = num12 / ItemsPerRow;
			float num15 = (float)num13 * (BarSize.X + Spacing.X);
			Vector2 vector4 = new Vector2(y: ((!StartFromBottom) ? ((float)num14 * (BarSize.Y + num5)) : ((float)(num - 1 - num14) * (BarSize.Y + num5))) + num5 / 2f + num6, x: num15 + Spacing.X / 2f);
			float num16 = 0.5f * (float)num11;
			float num17 = (FlipDirection ? MathUtils.Clamp((Value - ((float)BarsCount - num16 - 1f) / (float)BarsCount) * (float)BarsCount, 0f, 1f) : MathUtils.Clamp((Value - num16 / (float)BarsCount) * (float)BarsCount, 0f, 1f));
			if (!BarBlending)
			{
				num17 = MathUtils.Ceiling(num17);
			}
			float num18 = ((m_flashCount > 0f) ? (1f - MathUtils.Abs(MathUtils.Sin(m_flashCount * (float)Math.PI))) : 1f);
			Color c = LitBarColor;
			if (LitBarColor2 != Color.Transparent && BarsCount > 1)
			{
				c = Color.Lerp(LitBarColor, LitBarColor2, num16 / (float)(BarsCount - 1));
			}
			Color color = Color.Lerp(UnlitBarColor, c, num17) * num18 * GlobalColorTransform;
			if (HalfBars)
			{
				if (flag)
				{
					Vector2 zero = Vector2.Zero;
					Vector2 vector5 = new Vector2(0.5f, 1f);
					if (baseBatch is TexturedBatch2D)
					{
						Vector2 topLeft = BarSubtexture.TopLeft;
						Vector2 texCoord = new Vector2(MathUtils.Lerp(BarSubtexture.TopLeft.X, BarSubtexture.BottomRight.X, 0.5f), BarSubtexture.BottomRight.Y);
						((TexturedBatch2D)baseBatch).QueueQuad(vector4 + zero * BarSize, vector4 + vector5 * BarSize, 0f, topLeft, texCoord, color);
					}
					else
					{
						((FlatBatch2D)baseBatch).QueueQuad(vector4 + zero * BarSize, vector4 + vector5 * BarSize, 0f, color);
					}
				}
				else
				{
					Vector2 vector6 = new Vector2(0.5f, 0f);
					Vector2 one = Vector2.One;
					if (baseBatch is TexturedBatch2D)
					{
						Vector2 texCoord2 = new Vector2(MathUtils.Lerp(BarSubtexture.TopLeft.X, BarSubtexture.BottomRight.X, 0.5f), BarSubtexture.TopLeft.Y);
						Vector2 bottomRight = BarSubtexture.BottomRight;
						((TexturedBatch2D)baseBatch).QueueQuad(vector4 + vector6 * BarSize, vector4 + one * BarSize, 0f, texCoord2, bottomRight, color);
					}
					else
					{
						((FlatBatch2D)baseBatch).QueueQuad(vector4 + vector6 * BarSize, vector4 + one * BarSize, 0f, color);
					}
				}
			}
			else if (baseBatch is TexturedBatch2D)
			{
				((TexturedBatch2D)baseBatch).QueueQuad(vector4, vector4 + BarSize, 0f, BarSubtexture.TopLeft, BarSubtexture.BottomRight, color);
			}
			else
			{
				((FlatBatch2D)baseBatch).QueueQuad(vector4, vector4 + BarSize, 0f, color);
				((FlatBatch2D)baseBatch).QueueRectangle(vector4, vector4 + BarSize, 0f, Color.MultiplyColorOnly(color, 0.75f));
			}
		}
		if (baseBatch is TexturedBatch2D)
		{
			((TexturedBatch2D)baseBatch).TransformTriangles(GlobalTransform, count);
		}
		else
		{
			((FlatBatch2D)baseBatch).TransformLines(GlobalTransform, start);
			((FlatBatch2D)baseBatch).TransformTriangles(GlobalTransform, count);
		}
		m_flashCount = MathUtils.Max(m_flashCount - 4f * Time.FrameDuration, 0f);
	}

	public override void MeasureOverride(Vector2 parentAvailableSize)
	{
		IsDrawRequired = true;
		int num = (int)Math.Ceiling((float)BarsCount / (float)ItemsPerRow);
		if (num == 0)
		{
			DesiredSize = Vector2.Zero;
			return;
		}
		int num2 = ((BarsCount >= ItemsPerRow) ? ItemsPerRow : BarsCount);
		float x = (float)num2 * BarSize.X + (float)num2 * Spacing.X;
		float num3;
		if (StartFromBottom)
		{
			num3 = BarSize.Y;
		}
		else
		{
			num3 = (float)num * BarSize.Y + (float)(num - 1) * Spacing.Y;
			int num4 = 6;
			float num5 = (float)num4 * BarSize.Y + (float)(num4 - 1) * Spacing.Y;
			if (MaxHeight > 0f && MaxHeight < num5)
			{
				num5 = MaxHeight;
			}
			if (num3 > num5)
			{
				num3 = num5;
			}
		}
		DesiredSize = new Vector2(x, num3);
	}
}
