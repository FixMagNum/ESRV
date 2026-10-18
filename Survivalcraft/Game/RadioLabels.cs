using System.Collections.Generic;
using Engine;
using Engine.Graphics;
using Engine.Media;

namespace Game;

public static class RadioLabels
{
	private struct Label
	{
		public string Text;

		public double Expire;
	}

	private static Dictionary<Point3, Label> s_labels;

	private static List<Point3> s_toRemove;

	private static double s_lastDrawFrameStart;

	public static void Show(Point3 cell, string text, double seconds, double now)
	{
		Label value = default;
		value.Text = text;
		value.Expire = now + seconds;
		s_labels[cell] = value;
	}

	public static void Cleanup(double now)
	{
		s_toRemove.Clear();
		foreach (KeyValuePair<Point3, Label> s_label in s_labels)
		{
			if (now >= s_label.Value.Expire)
			{
				s_toRemove.Add(s_label.Key);
			}
		}
		for (int i = 0; i < s_toRemove.Count; i++)
		{
			s_labels.Remove(s_toRemove[i]);
		}
	}

	public static void Draw(SubsystemModelsRenderer modelsRenderer, Camera camera)
	{
		if (s_labels.Count == 0 || s_lastDrawFrameStart == Time.FrameStartTime)
		{
			return;
		}
		s_lastDrawFrameStart = Time.FrameStartTime;
		PrimitivesRenderer3D ex = modelsRenderer.PrimitivesRenderer;
		BitmapFont bitmapFont = LabelWidget.BitmapFont;
		foreach (KeyValuePair<Point3, Label> s_label in s_labels)
		{
			Point3 key = s_label.Key;
			Vector3 position = Vector3.Transform(new Vector3((float)key.X + 0.5f, (float)key.Y + 0.95f, (float)key.Z + 0.5f), camera.ViewMatrix);
			if (!(position.Z >= 0f))
			{
				float f = MathUtils.Saturate((position.Length() - 4f) / 3f);
				Color color = Color.Lerp(Color.White, Color.Transparent, f);
				if (color.A > 8)
				{
					Vector3 right = Vector3.TransformNormal(0.005f * Vector3.Normalize(Vector3.Cross(camera.ViewDirection, Vector3.UnitY)), camera.ViewMatrix);
					Vector3 down = Vector3.TransformNormal(-0.005f * Vector3.UnitY, camera.ViewMatrix);
					ex.FontBatch(bitmapFont, 1, DepthStencilState.DepthRead, RasterizerState.CullNoneScissor, BlendState.AlphaBlend, SamplerState.LinearClamp).QueueText(s_label.Value.Text, position, right, down, color, TextAnchor.HorizontalCenter | TextAnchor.Bottom);
				}
			}
		}
	}

	static RadioLabels()
	{
		s_lastDrawFrameStart = -1.0;
		s_labels = new Dictionary<Point3, Label>();
		s_toRemove = new List<Point3>();
	}
}
