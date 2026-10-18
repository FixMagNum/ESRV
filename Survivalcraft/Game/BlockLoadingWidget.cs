using System;
using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class BlockLoadingWidget : Widget
{
	private class Firefly
	{
		public Vector2 Position;

		public Vector2 Velocity;

		public float Size;

		public float Alpha;

		public int State;

		public float AnimationTimer;

		public int TextureIndex;

		public bool IsStar;
	}

	private Random m_random;

	private float m_value = -1f;

	public const int m_barsCount = 5;

	public const float m_barSize = 8f;

	public const float m_barsSpacing = 24f;

	public int m_boxIndex;

	public double m_lastBoxesStepTime;

	private Texture2D m_texture;

	private List<Firefly> m_fireflies;

	private double m_lastSpawnTime;

	private bool m_starsInitialized;

	private bool m_showFireflies;

	public float Value
	{
		get
		{
			return m_value;
		}
		set
		{
			m_value = value;
		}
	}

	public Color LitBarColor { get; set; }

	public Color UnlitBarColor { get; set; }

	public BlockLoadingWidget()
	{
		m_random = new Random();
		m_fireflies = new List<Firefly>();
		m_showFireflies = m_random.Float(0f, 1f) > 0.5f;
		IsHitTestVisible = true;
		LitBarColor = new Color(16, 140, 0);
		UnlitBarColor = new Color(48, 48, 48);
	}

	public override void MeasureOverride(Vector2 parentAvailableSize)
	{
		IsDrawRequired = true;
		DesiredSize = parentAvailableSize;
	}

	public override void Draw(DrawContext dc)
	{
		if (Time.RealTime - m_lastBoxesStepTime > 0.25)
		{
			m_boxIndex++;
			m_lastBoxesStepTime = Time.RealTime;
		}
		Vector2 vector = ActualSize / 2f - new Vector2(60f, 6f);
		FlatBatch2D flatBatch2D = dc.PrimitivesRenderer2D.FlatBatch();
		int count = flatBatch2D.TriangleVertices.Count;
		for (int i = 0; i < 5; i++)
		{
			Vector2 vector2 = vector + new Vector2(((float)i + 0.5f) * 24f, 6f);
			Color color = ((i == m_boxIndex % 5) ? LitBarColor : UnlitBarColor);
			float v = ((i == m_boxIndex % 5) ? 12f : 8f);
			flatBatch2D.QueueQuad(vector2 - new Vector2(v) / 2f, vector2 + new Vector2(v) / 2f, 0f, color * GlobalColorTransform);
		}
		flatBatch2D.TransformTriangles(GlobalTransform, count);
		if (m_texture == null)
		{
			return;
		}
		TexturedBatch2D texturedBatch2D = dc.PrimitivesRenderer2D.TexturedBatch(m_texture, useAlphaTest: false, 0, DepthStencilState.None, null, BlendState.AlphaBlend, SamplerState.LinearClamp);
		int count2 = texturedBatch2D.TriangleVertices.Count;
		foreach (Firefly firefly in m_fireflies)
		{
			float v2 = firefly.Size / 2f;
			Vector2 corner = firefly.Position - new Vector2(v2);
			Vector2 corner2 = firefly.Position + new Vector2(v2);
			Color color2 = Color.White * firefly.Alpha * ((float)(int)GlobalColorTransform.A / 255f);
			int num = firefly.TextureIndex % 3;
			int num2 = firefly.TextureIndex / 3;
			float num3 = 1f / 3f;
			Vector2 vector3 = new Vector2((float)num * num3, (float)num2 * num3);
			Vector2 texCoord = vector3 + new Vector2(num3, num3);
			texturedBatch2D.QueueQuad(corner, corner2, 0f, vector3, texCoord, color2);
		}
		texturedBatch2D.TransformTriangles(GlobalTransform, count2);
	}

	public override void Update()
	{
		if (m_texture == null)
		{
			try
			{
				m_texture = ContentManager.Get<Texture2D>("Textures/FirefliesParticle");
			}
			catch
			{
			}
		}
		if (!m_starsInitialized && ActualSize.X > 0f)
		{
			for (int i = 0; i < 400; i++)
			{
				SpawnStar();
			}
			m_starsInitialized = true;
		}
		float frameDuration = Time.FrameDuration;
		int count = m_fireflies.FindAll((Firefly f) => !f.IsStar).Count;
		if (m_showFireflies && Time.RealTime - m_lastSpawnTime > 0.1 && count < 40)
		{
			m_lastSpawnTime = Time.RealTime;
			SpawnFirefly(isShard: false);
		}
		if (Input != null && Input.Tap.HasValue)
		{
			Vector2 value = Input.Tap.Value;
			value = Vector2.Transform(value, Matrix.Invert(GlobalTransform));
			CheckInteraction(value);
		}
		for (int num = m_fireflies.Count - 1; num >= 0; num--)
		{
			Firefly firefly = m_fireflies[num];
			if (!firefly.IsStar)
			{
				if (firefly.State == 0)
				{
					firefly.Position += firefly.Velocity * frameDuration;
					firefly.Position += new Vector2(MathUtils.Sin((float)Time.RealTime * 2f + firefly.Position.Y * 0.02f), MathUtils.Cos((float)Time.RealTime * 1.5f + firefly.Position.X * 0.02f)) * 50f * frameDuration;
					if (IsOutOfBounds(firefly.Position, 150f))
					{
						m_fireflies.RemoveAt(num);
					}
				}
				else if (firefly.State == 1)
				{
					firefly.AnimationTimer += frameDuration * 8f;
					float num2 = 1f + MathUtils.Sin(firefly.AnimationTimer * (float)Math.PI) * 0.5f;
					firefly.Size = 50f * num2;
					if (firefly.AnimationTimer >= 1f)
					{
						ExplodeFirefly(firefly.Position, firefly.Size);
						m_fireflies.RemoveAt(num);
					}
				}
				else if (firefly.State == 2)
				{
					Firefly firefly2 = firefly;
					firefly2.Velocity.Y = firefly2.Velocity.Y + 1200f * frameDuration;
					firefly.Position += firefly.Velocity * frameDuration;
					firefly.Alpha -= 1.2f * frameDuration;
					firefly.Size -= 10f * frameDuration;
					if (firefly.Alpha <= 0f || firefly.Size <= 0f || firefly.Position.Y > ActualSize.Y + 50f)
					{
						m_fireflies.RemoveAt(num);
					}
				}
			}
		}
	}

	private void CheckInteraction(Vector2 touch)
	{
		foreach (Firefly firefly in m_fireflies)
		{
			if (firefly.State == 0 && Vector2.Distance(touch, firefly.Position) < firefly.Size)
			{
				firefly.State = 1;
				firefly.AnimationTimer = 0f;
				firefly.Velocity = Vector2.Zero;
				break;
			}
		}
	}

	private void ExplodeFirefly(Vector2 pos, float baseSize)
	{
		int num = m_random.Int(6, 12);
		for (int i = 0; i < num; i++)
		{
			SpawnFirefly(isShard: true, pos);
		}
	}

	private bool IsOutOfBounds(Vector2 pos, float margin)
	{
		if (!(pos.X < 0f - margin) && !(pos.X > ActualSize.X + margin) && !(pos.Y < 0f - margin))
		{
			return pos.Y > ActualSize.Y + margin;
		}
		return true;
	}

	private void SpawnFirefly(bool isShard, Vector2? pos = null)
	{
		Firefly num = new Firefly();
		num.TextureIndex = m_random.Int(0, 9);
		if (isShard)
		{
			num.Position = pos.Value;
			num.State = 2;
			num.Size = m_random.Float(15f, 30f);
			num.Alpha = 1f;
			float x = m_random.Float(0f, (float)Math.PI * 2f);
			float num2 = m_random.Float(100f, 400f);
			num.Velocity = new Vector2(MathUtils.Cos(x), MathUtils.Sin(x)) * num2;
			Firefly firefly = num;
			firefly.Velocity.Y = firefly.Velocity.Y - 150f;
		}
		else
		{
			num.State = 0;
			num.Position = new Vector2(m_random.Float(0f, ActualSize.X), m_random.Float(0f, ActualSize.Y));
			num.Velocity = new Vector2(m_random.Float(-60f, 60f), m_random.Float(-60f, 60f));
			num.Size = m_random.Float(40f, 80f);
			num.Alpha = m_random.Float(0.5f, 1f);
		}
		m_fireflies.Add(num);
	}

	private void SpawnStar()
	{
		Firefly inext = new Firefly();
		inext.IsStar = true;
		inext.TextureIndex = m_random.Int(0, 9);
		inext.State = 0;
		inext.Position = new Vector2(m_random.Float(0f, ActualSize.X), m_random.Float(0f, ActualSize.Y));
		inext.Velocity = Vector2.Zero;
		inext.Size = m_random.Float(5f, 11f);
		inext.Alpha = m_random.Float(0.3f, 0.9f);
		m_fireflies.Add(inext);
	}
}
