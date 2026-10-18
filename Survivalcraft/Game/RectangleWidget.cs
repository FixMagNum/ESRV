using Engine;
using Engine.Graphics;

namespace Game;

public class RectangleWidget : Widget
{
	public Subtexture m_subtexture;

	public bool m_textureWrap;

	public bool m_textureLinearFilter;

	public bool m_depthWriteEnabled;

	public Vector2 Size { get; set; }

	public float Depth { get; set; }

	public bool DepthWriteEnabled
	{
		get
		{
			return m_depthWriteEnabled;
		}
		set
		{
			if (value != m_depthWriteEnabled)
			{
				m_depthWriteEnabled = value;
			}
		}
	}

	public Subtexture Subtexture
	{
		get
		{
			return m_subtexture;
		}
		set
		{
			if (value != m_subtexture)
			{
				m_subtexture = value;
			}
		}
	}

	public bool TextureWrap
	{
		get
		{
			return m_textureWrap;
		}
		set
		{
			if (value != m_textureWrap)
			{
				m_textureWrap = value;
			}
		}
	}

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

	public bool FlipHorizontal { get; set; }

	public bool FlipVertical { get; set; }

	public Color FillColor { get; set; }

	public Color OutlineColor { get; set; }

	public float OutlineThickness { get; set; }

	public Vector2 Texcoord1 { get; set; }

	public Vector2 Texcoord2 { get; set; }

	public RectangleWidget()
	{
		Size = new Vector2(float.PositiveInfinity);
		TextureLinearFilter = true;
		FillColor = Color.Black;
		OutlineColor = Color.White;
		OutlineThickness = 1f;
		IsHitTestVisible = false;
		Texcoord1 = Vector2.Zero;
		Texcoord2 = Vector2.One;
	}

	public override void Draw(DrawContext dc)
	{
		if (FillColor.A == 0 && (OutlineColor.A == 0 || OutlineThickness <= 0f))
		{
			return;
		}
		DepthStencilState num = (DepthWriteEnabled ? DepthStencilState.DepthWrite : DepthStencilState.None);
		Matrix m = GlobalTransform;
		Vector2 v = Vector2.Zero;
		Vector2 v2 = new Vector2(ActualSize.X, 0f);
		Vector2 v3 = ActualSize;
		Vector2 v4 = new Vector2(0f, ActualSize.Y);
		Vector2.Transform(ref v, ref m, out var result);
		Vector2.Transform(ref v2, ref m, out var result2);
		Vector2.Transform(ref v3, ref m, out var result3);
		Vector2.Transform(ref v4, ref m, out var result4);
		Color color = FillColor * GlobalColorTransform;
		if (color.A != 0)
		{
			if (Subtexture != null)
			{
				SamplerState samplerState;
				if (TextureWrap)
				{
					samplerState = (TextureLinearFilter ? SamplerState.LinearWrap : SamplerState.PointWrap);
				}
				else
				{
					samplerState = (TextureLinearFilter ? SamplerState.LinearClamp : SamplerState.PointClamp);
				}
				TexturedBatch2D texturedBatch2D = dc.PrimitivesRenderer2D.TexturedBatch(Subtexture.Texture, useAlphaTest: true, 0, num, null, null, samplerState);
				Vector2 texCoord = default;
				Vector2 texCoord2 = default;
				Vector2 texCoord3;
				Vector2 texCoord4;
				if (TextureWrap)
				{
					texCoord = Vector2.Zero;
					texCoord3 = new Vector2(ActualSize.X / (float)Subtexture.Texture.Width, 0f);
					texCoord2 = new Vector2(ActualSize.X / (float)Subtexture.Texture.Width, ActualSize.Y / (float)Subtexture.Texture.Height);
					texCoord4 = new Vector2(0f, ActualSize.Y / (float)Subtexture.Texture.Height);
				}
				else
				{
					texCoord.X = MathUtils.Lerp(Subtexture.TopLeft.X, Subtexture.BottomRight.X, Texcoord1.X);
					texCoord.Y = MathUtils.Lerp(Subtexture.TopLeft.Y, Subtexture.BottomRight.Y, Texcoord1.Y);
					texCoord2.X = MathUtils.Lerp(Subtexture.TopLeft.X, Subtexture.BottomRight.X, Texcoord2.X);
					texCoord2.Y = MathUtils.Lerp(Subtexture.TopLeft.Y, Subtexture.BottomRight.Y, Texcoord2.Y);
					texCoord3 = new Vector2(texCoord2.X, texCoord.Y);
					texCoord4 = new Vector2(texCoord.X, texCoord2.Y);
				}
				if (FlipHorizontal)
				{
					Utilities.Swap(ref texCoord.X, ref texCoord3.X);
					Utilities.Swap(ref texCoord2.X, ref texCoord4.X);
				}
				if (FlipVertical)
				{
					Utilities.Swap(ref texCoord.Y, ref texCoord2.Y);
					Utilities.Swap(ref texCoord3.Y, ref texCoord4.Y);
				}
				texturedBatch2D.QueueQuad(result, result2, result3, result4, Depth, texCoord, texCoord3, texCoord2, texCoord4, color);
			}
			else
			{
				dc.PrimitivesRenderer2D.FlatBatch(1, num).QueueQuad(result, result2, result3, result4, Depth, color);
			}
		}
		Color color2 = OutlineColor * GlobalColorTransform;
		if (color2.A != 0 && OutlineThickness > 0f)
		{
			FlatBatch2D flatBatch2D = dc.PrimitivesRenderer2D.FlatBatch(1, num);
			Vector2 vector = Vector2.Normalize(GlobalTransform.Right.XY);
			Vector2 vector2 = -Vector2.Normalize(GlobalTransform.Up.XY);
			int num2 = (int)MathUtils.Max(MathUtils.Round(OutlineThickness * GlobalTransform.Right.Length()), 1f);
			for (int i = 0; i < num2; i++)
			{
				flatBatch2D.QueueLine(result, result2, Depth, color2);
				flatBatch2D.QueueLine(result2, result3, Depth, color2);
				flatBatch2D.QueueLine(result3, result4, Depth, color2);
				flatBatch2D.QueueLine(result4, result, Depth, color2);
				result += vector - vector2;
				result2 += -vector - vector2;
				result3 += -vector + vector2;
				result4 += vector + vector2;
			}
		}
	}

	public override void MeasureOverride(Vector2 parentAvailableSize)
	{
		IsDrawRequired = FillColor.A != 0 || (OutlineColor.A != 0 && OutlineThickness > 0f);
		DesiredSize = Size;
	}
}
