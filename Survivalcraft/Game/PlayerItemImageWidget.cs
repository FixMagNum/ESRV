using Engine;
using Engine.Graphics;

namespace Game;

public class PlayerItemImageWidget : CanvasWidget
{
	public Vector2 Padding;

	public Color? Background;

	public Vector2 startPoint = Vector2.Zero;

	public Vector2 endPoint = Vector2.One;

	public Texture2D Texture { get; set; }

	public Subtexture SubTexture
	{
		set
		{
			Texture = value.Texture;
			startPoint = value.TopLeft;
			endPoint = value.BottomRight;
		}
	}

	public float RotateAngle { get; set; }

	public Color ColorTransForm { get; set; }

	public PlayerItemImageWidget()
	{
		IsDrawRequired = true;
		ColorTransForm = Color.White;
	}

	public override void Draw(DrawContext dc)
	{
		if (Texture != null)
		{
			FlatBatch2D flatBatch2D = dc.PrimitivesRenderer2D.FlatBatch();
			TexturedBatch2D texturedBatch2D = dc.PrimitivesRenderer2D.TexturedBatch(Texture, useAlphaTest: true);
			int count = flatBatch2D.TriangleVertices.Count;
			int count2 = texturedBatch2D.TriangleVertices.Count;
			texturedBatch2D.QueueQuad(Vector2.Zero + Padding, Size, 1f, startPoint, endPoint, ColorTransForm);
			texturedBatch2D.TransformTriangles(Matrix.CreateTranslation((0f - Size.X) / 2f, (0f - Size.Y) / 2f, 0f) * Matrix.CreateRotationZ(RotateAngle) * Matrix.CreateTranslation(Size.X / 2f, Size.Y / 2f, 0f) * GlobalTransform, count2);
			if (Background.HasValue)
			{
				flatBatch2D.QueueQuad(Vector2.Zero, Size, 0f, Background.Value);
				flatBatch2D.TransformTriangles(GlobalTransform, count);
			}
		}
	}
}
