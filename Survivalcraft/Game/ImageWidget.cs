using System;
using Engine;
using Engine.Graphics;
using Engine.Media;

namespace Game;

public class ImageWidget : CanvasWidget
{
	public Vector2 Padding;

	public Color? Background;

	public Vector2 startPoint = Vector2.Zero;

	public Vector2 endPoint = Vector2.One;

	private Image m_cachedImage;

	private bool m_baked;

	public Texture2D Texture { get; set; }

	public Subtexture SubTexture
	{
		set
		{
			Texture = value.Texture;
			startPoint = value.TopLeft;
			endPoint = value.BottomRight;
			m_baked = true;
		}
	}

	public float RotateAngle { get; set; }

	public Color ColorTransForm { get; set; }

	public string SourcePath { get; private set; }

	public bool UseQuantize16 { get; private set; }

	public int SampleStep { get; private set; } = 1;

	public int Rotate90 { get; private set; }

	public Color[] WorldPalette { get; private set; }

	public int PaletteOffset { get; private set; }

	public bool ForcePointFilter { get; set; }

	public ImageWidget()
	{
		IsDrawRequired = true;
		ColorTransForm = Color.White;
		startPoint = Vector2.Zero;
		endPoint = Vector2.One;
	}

	public override void Draw(DrawContext dc)
	{
		if (!m_baked && !string.IsNullOrEmpty(SourcePath))
		{
			TryBakeTexture();
		}
		FlatBatch2D num = dc.PrimitivesRenderer2D.FlatBatch();
		int num2 = num.TriangleVertices.Count;
		if (Background.HasValue)
		{
			num.QueueQuad(Vector2.Zero, ActualSize, 0f, Background.Value);
		}
		if (Texture != null)
		{
			SamplerState num3 = ((ForcePointFilter || UseQuantize16 || SampleStep > 1) ? SamplerState.PointClamp : SamplerState.LinearClamp);
			TexturedBatch2D num4 = dc.PrimitivesRenderer2D.TexturedBatch(Texture, useAlphaTest: false, 0, null, null, null, num3);
			int i = num4.TriangleVertices.Count;
			Vector2 j = Padding;
			Vector2 k = ActualSize - Padding;
			if (k.X < j.X + 1f || k.Y < j.Y + 1f)
			{
				num.TransformTriangles(GlobalTransform, num2);
				return;
			}
			Vector2 l = startPoint;
			Vector2 texCoord = endPoint;
			num4.QueueQuad(j, k, 1f, l, texCoord, ColorTransForm);
			Matrix matrix = Matrix.CreateTranslation((0f - (j.X + k.X)) * 0.5f, (0f - (j.Y + k.Y)) * 0.5f, 0f) * Matrix.CreateRotationZ(RotateAngle) * Matrix.CreateTranslation((j.X + k.X) * 0.5f, (j.Y + k.Y) * 0.5f, 0f) * GlobalTransform;
			num4.TransformTriangles(matrix, i);
		}
		num.TransformTriangles(GlobalTransform, num2);
	}

	public void SetSource(string absolutePath)
	{
		if (SourcePath != absolutePath)
		{
			SourcePath = absolutePath;
			m_cachedImage = null;
			MarkDirty();
		}
	}

	public override void MeasureOverride(Vector2 parentAvailableSize)
	{
		IsDrawRequired = true;
		DesiredSize = parentAvailableSize;
	}

	private void TryBakeTexture()
	{
		if (m_cachedImage == null && !string.IsNullOrEmpty(SourcePath))
		{
			try
			{
				m_cachedImage = Image.Load(SourcePath);
			}
			catch
			{
				m_cachedImage = null;
			}
		}
		if (m_cachedImage == null)
		{
			DisposeTexture();
			m_baked = true;
			return;
		}
		int num = Math.Max(1, m_cachedImage.Width);
		int num2 = Math.Max(1, m_cachedImage.Height);
		int num3 = Math.Max(1, SampleStep);
		int num4 = Math.Max(1, num / num3);
		int num5 = Math.Max(1, num2 / num3);
		int num6 = ((Rotate90 == 0) ? num4 : num5);
		int num7 = ((Rotate90 == 0) ? num5 : num4);
		Color[] array = new Color[num6 * num7];
		Color[] worldPalette = WorldPalette;
		int num8 = PaletteOffset & 0xF;
		for (int i = 0; i < num5; i++)
		{
			int y = Math.Min(num2 - 1, i * num3);
			for (int j = 0; j < num4; j++)
			{
				int x = Math.Min(num - 1, j * num3);
				Color color = m_cachedImage.GetPixel(x, y);
				if (color.A != 0)
				{
					color.A = byte.MaxValue;
				}
				if (UseQuantize16 && worldPalette != null && worldPalette.Length >= 16)
				{
					int num9 = 0;
					int num10 = int.MaxValue;
					int num11 = Math.Min(16, worldPalette.Length);
					for (int k = 0; k < num11; k++)
					{
						byte b = (byte)(worldPalette[k].R - color.R);
						int num12 = worldPalette[k].G - color.G;
						int num13 = worldPalette[k].B - color.B;
						int num14 = worldPalette[k].A - color.A;
						int num15 = b * b + num12 * num12 + num13 * num13 + num14 * num14;
						if (num15 < num10)
						{
							num10 = num15;
							num9 = k;
						}
					}
					int num16 = (num9 + num8) % num11;
					color = worldPalette[num16];
					color.A = byte.MaxValue;
				}
				int num17;
				int num18;
				if (Rotate90 == 0)
				{
					num17 = j;
					num18 = i;
				}
				else if (Rotate90 < 0)
				{
					num17 = i;
					num18 = num7 - 1 - j;
				}
				else
				{
					num17 = num6 - 1 - i;
					num18 = j;
				}
				int num19 = num18 * num6 + num17;
				if (num19 < array.Length)
				{
					array[num19] = color;
				}
			}
		}
		try
		{
			DisposeTexture();
			Texture2D texture2D = new Texture2D(num6, num7, 1, ColorFormat.Rgba8888);
			texture2D.SetData(0, array);
			Texture = texture2D;
			startPoint = Vector2.Zero;
			endPoint = Vector2.One;
		}
		catch
		{
			DisposeTexture();
		}
		m_baked = true;
	}

	private void MarkDirty()
	{
		m_baked = false;
		RotateAngle = 0f;
	}

	private void DisposeTexture()
	{
		if (Texture != null)
		{
			try
			{
				Texture.Dispose();
			}
			catch
			{
			}
			Texture = null;
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		DisposeTexture();
		m_cachedImage = null;
	}

	public void ConfigureFilter(bool quantize16, int sampleStep, int rotate90, Color[] palette, int paletteOffset = 0)
	{
		UseQuantize16 = quantize16;
		SampleStep = Math.Max(1, sampleStep);
		Rotate90 = MathUtils.Clamp(rotate90, -1, 1);
		WorldPalette = palette;
		PaletteOffset = MathUtils.Clamp(paletteOffset, 0, 15);
		MarkDirty();
	}
}
