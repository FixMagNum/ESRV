using Engine.Media;

namespace Engine.Graphics;

public class FontBatch2D : BaseFontBatch
{
	public void QueueText(string text, Vector2 position, float depth, Color color, TextAnchor anchor = TextAnchor.Default)
	{
		QueueText(text, position, depth, color, anchor, Vector2.One, Vector2.Zero);
	}

	public void QueueText(string text, Vector2 position, float depth, Color color, TextAnchor anchor, Vector2 scale, Vector2 spacing, float angle = 0f)
	{
		Vector2 vector2;
		Vector2 vector3;
		Vector2 vector6;
		if (angle != 0f)
		{
			Vector2 vector = new Vector2(MathUtils.Cos(angle), MathUtils.Sin(angle));
			vector2 = vector;
			vector3 = new Vector2(0f - vector.Y, vector.X);
			Vector2 vector4 = CalculateTextOffset(text, anchor, scale, spacing);
			Vector2 vector5 = vector2 * vector4.X + vector3 * vector4.Y;
			vector2 *= scale.X * base.Font.Scale;
			vector3 *= scale.Y * base.Font.Scale;
			vector6 = position + vector5;
		}
		else
		{
			vector2 = new Vector2(scale.X * base.Font.Scale, 0f);
			vector3 = new Vector2(0f, scale.Y * base.Font.Scale);
			vector6 = position + CalculateTextOffset(text, anchor, scale, spacing);
		}
		spacing += base.Font.Spacing;
		vector6 += 0.5f * (vector2 * spacing.X + vector3 * spacing.Y);
		if ((anchor & TextAnchor.DisableSnapToPixels) == 0)
		{
			vector6 = Vector2.Round(vector6);
		}
		Vector2 vector7 = vector6;
		int num = 0;
		foreach (char c in text)
		{
			switch (c)
			{
			case '\n':
				num++;
				vector7 = vector6 + (float)num * (base.Font.GlyphHeight + spacing.Y) * vector3;
				continue;
			case '\r':
				continue;
			}
			BitmapFont.Glyph glyph = base.Font.GetGlyph(c);
			if (!glyph.IsBlank)
			{
				Vector2 vector8 = vector2 * (glyph.TexCoord2.X - glyph.TexCoord1.X) * base.Font.Texture.Width;
				Vector2 vector9 = vector3 * (glyph.TexCoord2.Y - glyph.TexCoord1.Y) * base.Font.Texture.Height;
				Vector2 vector10 = vector2 * glyph.Offset.X + vector3 * glyph.Offset.Y;
				Vector2 vector11 = vector7 + vector10;
				Vector2 vector12 = vector11 + vector8;
				Vector2 vector13 = vector11 + vector9;
				Vector2 vector14 = vector11 + vector8 + vector9;
				int count = TriangleVertices.Count;
				TriangleVertices.Count += 4;
				TriangleVertices.Array[count] = new VertexPositionColorTexture(new Vector3(vector11.X, vector11.Y, depth), color, new Vector2(glyph.TexCoord1.X, glyph.TexCoord1.Y));
				TriangleVertices.Array[count + 1] = new VertexPositionColorTexture(new Vector3(vector12.X, vector12.Y, depth), color, new Vector2(glyph.TexCoord2.X, glyph.TexCoord1.Y));
				TriangleVertices.Array[count + 2] = new VertexPositionColorTexture(new Vector3(vector14.X, vector14.Y, depth), color, new Vector2(glyph.TexCoord2.X, glyph.TexCoord2.Y));
				TriangleVertices.Array[count + 3] = new VertexPositionColorTexture(new Vector3(vector13.X, vector13.Y, depth), color, new Vector2(glyph.TexCoord1.X, glyph.TexCoord2.Y));
				int count2 = TriangleIndices.Count;
				TriangleIndices.Count += 6;
				TriangleIndices.Array[count2] = (ushort)count;
				TriangleIndices.Array[count2 + 1] = (ushort)(count + 1);
				TriangleIndices.Array[count2 + 2] = (ushort)(count + 2);
				TriangleIndices.Array[count2 + 3] = (ushort)(count + 2);
				TriangleIndices.Array[count2 + 4] = (ushort)(count + 3);
				TriangleIndices.Array[count2 + 5] = (ushort)count;
			}
			vector7 += vector2 * (glyph.Width + spacing.X);
		}
	}

	public void TransformTriangles(Matrix matrix, int start = 0, int end = -1)
	{
		VertexPositionColorTexture[] array = TriangleVertices.Array;
		if (end < 0)
		{
			end = TriangleVertices.Count;
		}
		for (int i = start; i < end; i++)
		{
			Vector2 v = array[i].Position.XY;
			Vector2.Transform(ref v, ref matrix, out v);
			array[i].Position.X = v.X;
			array[i].Position.Y = v.Y;
		}
	}

	public void Flush(bool clearAfterFlush = true)
	{
		Flush(PrimitivesRenderer2D.ViewportMatrix(), clearAfterFlush);
	}
}
