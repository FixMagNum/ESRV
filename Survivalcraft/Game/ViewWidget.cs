using System;
using Engine;
using Engine.Graphics;
using Game.NetWork;
using Game.NetWork.Packages;

namespace Game;

public class ViewWidget : TouchInputWidget, IDragTargetWidget
{
	public SubsystemDrawing m_subsystemDrawing;

	public RenderTarget2D m_scalingRenderTarget;

	public RenderTarget2D m_blackoutRenderTarget;

	public double m_lastBlackoutCaptureTime;

	public GameWidget GameWidget { get; set; }

	public Point2? ScalingRenderTargetSize
	{
		get
		{
			if (m_scalingRenderTarget == null)
			{
				return null;
			}
			return new Point2(m_scalingRenderTarget.Width, m_scalingRenderTarget.Height);
		}
	}

	public override void ChangeParent(ContainerWidget parentWidget)
	{
		if (parentWidget is GameWidget)
		{
			GameWidget = (GameWidget)parentWidget;
			m_subsystemDrawing = GameWidget.SubsystemGameWidgets.Project.FindSubsystem<SubsystemDrawing>(throwOnError: true);
			base.ChangeParent(parentWidget);
			return;
		}
		throw new InvalidOperationException("ViewWidget must be a child of GameWidget.");
	}

	public override void MeasureOverride(Vector2 parentAvailableSize)
	{
		IsDrawRequired = true;
		base.MeasureOverride(parentAvailableSize);
	}

	public override void Draw(DrawContext dc)
	{
		if (GameWidget.PlayerData.ComponentPlayer != null && GameWidget.PlayerData.IsReadyForPlaying)
		{
			if (CommonLib.WorkType == WorkType.Local)
			{
				DrawToScreen(dc);
			}
			else if (GameWidget.PlayerData.IsMainPlayer)
			{
				DrawToScreen(dc);
			}
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		Utilities.Dispose(ref m_scalingRenderTarget);
		Utilities.Dispose(ref m_blackoutRenderTarget);
	}

	public void DragOver(Widget dragWidget, object data)
	{
	}

	public void NetDragDrop(Vector3 worldPos, InventoryDragData inventoryDragData, int count)
	{
		SubsystemPickables subsystemPickables = GameManager.Project.FindSubsystem<SubsystemPickables>(throwOnError: true);
		int slotValue = inventoryDragData.Inventory.GetSlotValue(inventoryDragData.SlotIndex);
		int num = inventoryDragData.Inventory.RemoveSlotItems(inventoryDragData.SlotIndex, count);
		if (num > 0)
		{
			subsystemPickables.AddPickable(slotValue, num, GameWidget.ActiveCamera.ViewPosition, worldPos, null);
		}
	}

	public void DragDrop(Widget dragWidget, object data)
	{
		InventoryDragData inventoryDragData = data as InventoryDragData;
		Vector2 vector = dragWidget.WidgetToScreen(dragWidget.ActualSize / 2f);
		Vector3 vector2 = Vector3.Normalize(GameWidget.ActiveCamera.ScreenToWorld(new Vector3(vector.X, vector.Y, 1f), Matrix.Identity) - GameWidget.ActiveCamera.ViewPosition) * 12f;
		ComponentPlayer componentPlayer = GameWidget.PlayerData.ComponentPlayer;
		int count = ((componentPlayer != null && componentPlayer.ComponentInput.SplitSourceInventory == inventoryDragData.Inventory && componentPlayer.ComponentInput.SplitSourceSlotIndex == inventoryDragData.SlotIndex) ? 1 : ((inventoryDragData.DragMode != DragMode.SingleItem) ? inventoryDragData.Inventory.GetSlotCount(inventoryDragData.SlotIndex) : MathUtils.Min(inventoryDragData.Inventory.GetSlotCount(inventoryDragData.SlotIndex), 1)));
		if (inventoryDragData != null && GameManager.Project != null)
		{
			if (CommonLib.WorkType == WorkType.Client)
			{
				CommonLib.Net.QueuePackage(new ComponentPlayerPackage(GameWidget.PlayerData, inventoryDragData.Inventory.Id, inventoryDragData.SlotIndex, vector2, count));
			}
			else
			{
				NetDragDrop(vector2, inventoryDragData, count);
			}
		}
	}

	public void SetupScalingRenderTarget()
	{
		float num;
		if (SettingsManager.ResolutionMode != ResolutionMode.Low)
		{
			num = ((SettingsManager.ResolutionMode != ResolutionMode.Medium) ? 1f : 0.75f);
		}
		else
		{
			num = 0.5f;
		}
		float num2 = 1f;
		ComponentPlayer componentPlayer = ((GameWidget != null && GameWidget.PlayerData != null) ? GameWidget.PlayerData.ComponentPlayer : null);
		if (componentPlayer != null && componentPlayer.ComponentHealth != null)
		{
			num2 = componentPlayer.ComponentHealth.Health;
		}
		bool flag = (SettingsManager.VisionDefectsEnabled ? MathUtils.Saturate((0.3f - num2) / 0.25f) : 0f) > 0f;
		float num3 = 0f;
		float num4 = 0f;
		if (componentPlayer != null)
		{
			ComponentAlcohol componentAlcohol = componentPlayer.Entity.FindComponent<ComponentAlcohol>(throwOnError: false);
			if (componentAlcohol != null)
			{
				num3 = componentAlcohol.Alcohol;
			}
			if (componentPlayer.ComponentScreenOverlays != null)
			{
				num4 = componentPlayer.ComponentScreenOverlays.BlackoutFactor;
			}
		}
		bool flag2 = num3 >= 0.2f;
		float num5 = GlobalTransform.Right.Length();
		float num6 = GlobalTransform.Up.Length();
		Vector2 vector = new Vector2(ActualSize.X * num5, ActualSize.Y * num6);
		bool flag3 = ((num < 1f || GlobalColorTransform != Color.White) | flag | flag2) || num4 > 0.01f;
		if (SettingsManager.HeatAndUnderwaterBlurEnabled)
		{
			ComponentVitalStats componentVitalStats = componentPlayer?.Entity.FindComponent<ComponentVitalStats>();
			if (componentVitalStats != null && componentVitalStats.HeatHazeFactor > 0.01f)
			{
				flag3 = true;
			}
		}
		float num7 = ((flag | flag2) ? 1f : num);
		Point2 point = new Point2
		{
			X = (int)MathUtils.Round(vector.X * num7),
			Y = (int)MathUtils.Round(vector.Y * num7)
		};
		if (flag3 && point.X > 0 && point.Y > 0)
		{
			if (m_scalingRenderTarget == null || m_scalingRenderTarget.Width != point.X || m_scalingRenderTarget.Height != point.Y)
			{
				Utilities.Dispose(ref m_scalingRenderTarget);
				m_scalingRenderTarget = new RenderTarget2D(point.X, point.Y, 1, ColorFormat.Rgba8888, DepthFormat.Depth24Stencil8);
			}
			Display.RenderTarget = m_scalingRenderTarget;
			Display.Clear(Color.Black, 1f, 0);
		}
		else
		{
			Utilities.Dispose(ref m_scalingRenderTarget);
		}
	}

	public void ApplyScalingRenderTarget(DrawContext dc)
	{
		if (m_scalingRenderTarget == null)
		{
			return;
		}
		BlendState blendState = ((GlobalColorTransform.A < byte.MaxValue) ? BlendState.AlphaBlend : BlendState.Opaque);
		float num = 0f;
		float num2 = 0f;
		ComponentPlayer componentPlayer = ((GameWidget != null && GameWidget.PlayerData != null) ? GameWidget.PlayerData.ComponentPlayer : null);
		if (componentPlayer != null)
		{
			ComponentVitalStats componentVitalStats = componentPlayer.Entity.FindComponent<ComponentVitalStats>();
			if (componentVitalStats != null && SettingsManager.HeatAndUnderwaterBlurEnabled)
			{
				num = componentVitalStats.HeatHazeFactor;
			}
			num2 = componentPlayer.ComponentScreenOverlays.BlackoutFactor;
		}
		if (num2 > 0.01f)
		{
			if (m_blackoutRenderTarget == null || m_blackoutRenderTarget.Width != m_scalingRenderTarget.Width || m_blackoutRenderTarget.Height != m_scalingRenderTarget.Height)
			{
				Utilities.Dispose(ref m_blackoutRenderTarget);
				m_blackoutRenderTarget = new RenderTarget2D(m_scalingRenderTarget.Width, m_scalingRenderTarget.Height, 1, ColorFormat.Rgba8888, DepthFormat.Depth24Stencil8);
			}
			float num3 = MathUtils.Lerp(0.05f, 0.5f, num2);
			float num4 = 1f + 0.3f * MathUtils.Sin((float)Time.RealTime * 3.7f) + 0.2f * MathUtils.Cos((float)Time.RealTime * 6.2f);
			float num5 = num3 * num4;
			if (Time.RealTime - m_lastBlackoutCaptureTime > (double)num5)
			{
				m_lastBlackoutCaptureTime = Time.RealTime;
				RenderTarget2D renderTarget = Display.RenderTarget;
				Display.RenderTarget = m_blackoutRenderTarget;
				Display.Clear(Color.Black, 1f, 0);
				TexturedBatch2D texturedBatch2D = dc.PrimitivesRenderer2D.TexturedBatch(m_scalingRenderTarget, useAlphaTest: false, 0, DepthStencilState.None, RasterizerState.CullNoneScissor, BlendState.Opaque, SamplerState.PointClamp);
				int count = texturedBatch2D.TriangleVertices.Count;
				texturedBatch2D.QueueQuad(Vector2.Zero, new Vector2(m_blackoutRenderTarget.Width, m_blackoutRenderTarget.Height), 0f, Vector2.Zero, Vector2.One, Color.White);
				texturedBatch2D.TransformTriangles(Matrix.Identity, count);
				dc.PrimitivesRenderer2D.Flush();
				Display.RenderTarget = renderTarget;
			}
		}
		else
		{
			Utilities.Dispose(ref m_blackoutRenderTarget);
		}
		if (num > 0.01f)
		{
			float time = (float)Time.FrameStartTime;
			float strength = num * 0.01f;
			float freq = 10f;
			float speed = 3f;
			int num6 = 50;
			int num7 = 30;
			TexturedBatch2D texturedBatch2D2 = dc.PrimitivesRenderer2D.TexturedBatch(m_scalingRenderTarget, useAlphaTest: false, 0, DepthStencilState.None, RasterizerState.CullNoneScissor, blendState, SamplerState.LinearClamp);
			int count2 = texturedBatch2D2.TriangleVertices.Count;
			for (int i = 0; i < num7; i++)
			{
				for (int j = 0; j < num6; j++)
				{
					float num8 = (float)j / (float)num6;
					float num9 = (float)i / (float)num7;
					float num10 = (float)(j + 1) / (float)num6;
					float num11 = (float)(i + 1) / (float)num7;
					Vector2 p = new Vector2(num8 * ActualSize.X, num9 * ActualSize.Y);
					Vector2 p2 = new Vector2(num10 * ActualSize.X, num9 * ActualSize.Y);
					Vector2 p3 = new Vector2(num10 * ActualSize.X, num11 * ActualSize.Y);
					Vector2 p4 = new Vector2(num8 * ActualSize.X, num11 * ActualSize.Y);
					p += CalculateHeatOffset(num8, num9, time, freq, speed, strength);
					p2 += CalculateHeatOffset(num10, num9, time, freq, speed, strength);
					p3 += CalculateHeatOffset(num10, num11, time, freq, speed, strength);
					p4 += CalculateHeatOffset(num8, num11, time, freq, speed, strength);
					texturedBatch2D2.QueueQuad(p, p2, p3, p4, 0f, new Vector2(num8, num9), new Vector2(num10, num9), new Vector2(num10, num11), new Vector2(num8, num11), GlobalColorTransform);
				}
			}
			texturedBatch2D2.TransformTriangles(GlobalTransform, count2, texturedBatch2D2.TriangleVertices.Count - count2);
			dc.PrimitivesRenderer2D.Flush();
		}
		else
		{
			TexturedBatch2D texturedBatch2D3 = dc.PrimitivesRenderer2D.TexturedBatch(m_scalingRenderTarget, useAlphaTest: false, 0, DepthStencilState.None, RasterizerState.CullNoneScissor, blendState, SamplerState.PointClamp);
			int count3 = texturedBatch2D3.TriangleVertices.Count;
			texturedBatch2D3.QueueQuad(Vector2.Zero, ActualSize, 0f, Vector2.Zero, Vector2.One, GlobalColorTransform);
			texturedBatch2D3.TransformTriangles(GlobalTransform, count3);
			dc.PrimitivesRenderer2D.Flush();
		}
		if (m_blackoutRenderTarget != null && num2 > 0.01f)
		{
			float num12 = MathUtils.Clamp(num2 * 0.8f, 0f, 0.8f);
			Color color = Color.White * num12;
			TexturedBatch2D texturedBatch2D4 = dc.PrimitivesRenderer2D.TexturedBatch(m_blackoutRenderTarget, useAlphaTest: false, 0, DepthStencilState.None, RasterizerState.CullNoneScissor, BlendState.AlphaBlend, SamplerState.LinearClamp);
			int count4 = texturedBatch2D4.TriangleVertices.Count;
			texturedBatch2D4.QueueQuad(Vector2.Zero, ActualSize, 0f, Vector2.Zero, Vector2.One, color);
			texturedBatch2D4.TransformTriangles(GlobalTransform, count4);
			dc.PrimitivesRenderer2D.Flush();
		}
		if (SettingsManager.VisionDefectsEnabled)
		{
			float num13 = 1f;
			if (componentPlayer != null && componentPlayer.ComponentHealth != null)
			{
				num13 = componentPlayer.ComponentHealth.Health;
			}
			float num14 = MathUtils.Saturate((0.3f - num13) / 0.25f);
			if (num14 > 0f)
			{
				int num15 = MathUtils.Clamp((int)MathUtils.Floor(1f + 3f * num14), 1, 3);
				float f = MathUtils.Saturate((0.3f - num13) / 0.3f);
				float num16 = MathUtils.Lerp(0.3f, 0.3f, f);
				float num17 = (float)(Time.FrameStartTime * (Math.PI * 2.0) * (double)num16);
				float num18 = 0.85f + 0.15f * MathUtils.Sin(num17);
				float num19 = MathUtils.Lerp(0f, 7.8f, num14) * num18;
				float num20 = (float)(int)GlobalColorTransform.A / 255f;
				float num21 = MathUtils.Lerp(0.078f, 0.208f, num14) * num20;
				TexturedBatch2D texturedBatch2D5 = dc.PrimitivesRenderer2D.TexturedBatch(m_scalingRenderTarget, useAlphaTest: false, 0, DepthStencilState.None, RasterizerState.CullNoneScissor, BlendState.AlphaBlend, SamplerState.PointClamp);
				int count5 = texturedBatch2D5.TriangleVertices.Count;
				for (int k = 0; k < num15; k++)
				{
					float x = num17 + (float)Math.PI * 2f * (float)k / (float)num15;
					Vector2 vector = new Vector2(MathUtils.Cos(x), MathUtils.Sin(x)) * num19;
					texturedBatch2D5.QueueQuad(vector, ActualSize + vector, 0f, Vector2.Zero, Vector2.One, Color.White * num21);
				}
				texturedBatch2D5.TransformTriangles(GlobalTransform, count5);
				dc.PrimitivesRenderer2D.Flush();
			}
		}
		ComponentPlayer componentPlayer2 = ((GameWidget != null && GameWidget.PlayerData != null) ? GameWidget.PlayerData.ComponentPlayer : null);
		float num22 = 0f;
		if (componentPlayer2 != null)
		{
			ComponentAlcohol componentAlcohol = componentPlayer2.Entity.FindComponent<ComponentAlcohol>(throwOnError: false);
			if (componentAlcohol != null)
			{
				num22 = componentAlcohol.Alcohol;
			}
		}
		if (num22 >= 0.2f)
		{
			int num23 = 3;
			float f2 = MathUtils.Saturate((num22 - 0.2f) / 9.8f);
			float num24 = MathUtils.Lerp(0.25f, 0.06f, f2);
			float num25 = (float)(Time.FrameStartTime * (Math.PI * 2.0) * (double)num24);
			float num26 = 0.85f + 0.15f * MathUtils.Sin(num25);
			float f3 = MathUtils.Pow(MathUtils.Saturate((num22 - 0.2f) / 0.8f), 1.25f);
			float f4 = MathUtils.Pow(MathUtils.Saturate((num22 - 1f) / 9f), 1.5f);
			float num27 = MathUtils.Lerp(0f, 78f, f3);
			float num28 = MathUtils.Lerp(0f, 172f, f4);
			float num29 = (num27 + num28) * num26;
			float num30 = (float)(int)GlobalColorTransform.A / 255f;
			float num31 = MathUtils.Lerp(0.1f, 0.4f, MathUtils.Saturate((num22 - 0.2f) / 9.8f)) * num30;
			TexturedBatch2D texturedBatch2D6 = dc.PrimitivesRenderer2D.TexturedBatch(m_scalingRenderTarget, useAlphaTest: false, 0, DepthStencilState.None, RasterizerState.CullNoneScissor, BlendState.AlphaBlend, SamplerState.PointClamp);
			int count6 = texturedBatch2D6.TriangleVertices.Count;
			for (int l = 0; l < num23; l++)
			{
				float x2 = num25 + (float)Math.PI * 2f * (float)l / (float)num23;
				Vector2 vector2 = new Vector2(MathUtils.Cos(x2), MathUtils.Sin(x2)) * num29;
				texturedBatch2D6.QueueQuad(vector2, ActualSize + vector2, 0f, Vector2.Zero, Vector2.One, Color.White * num31);
			}
			texturedBatch2D6.TransformTriangles(GlobalTransform, count6);
			dc.PrimitivesRenderer2D.Flush();
		}
	}

	public void DrawToScreen(DrawContext dc)
	{
		GameWidget.ActiveCamera.PrepareForDrawing(null);
		RenderTarget2D renderTarget = Display.RenderTarget;
		SetupScalingRenderTarget();
		try
		{
			m_subsystemDrawing.Draw(GameWidget.ActiveCamera);
		}
		finally
		{
			Display.RenderTarget = renderTarget;
		}
		ApplyScalingRenderTarget(dc);
	}

	private Vector2 CalculateHeatOffset(float u, float v, float time, float freq, float speed, float strength)
	{
		float num = 1f;
		if (v > 0.6f)
		{
			num = 1f - (v - 0.6f) / 0.4f;
			num = MathUtils.Max(0f, num);
		}
		float num2 = MathUtils.Saturate(MathUtils.Min(MathUtils.Min(u, 1f - u), MathUtils.Min(v, 1f - v)) * 10f);
		float num3 = num * num2;
		if (num3 <= 0.001f)
		{
			return Vector2.Zero;
		}
		float num4 = MathUtils.Sin(v * 10f - time * 2f) * 0.5f;
		float num5 = MathUtils.Cos(u * 25f + time * 8f) * 0.3f;
		float num6 = MathUtils.Sin((u + v) * 15f - time * 5f) * 0.2f;
		float x = (num4 + num5 + num6) * strength * ActualSize.X * num3;
		float y = (num5 + num4) * strength * 0.5f * ActualSize.Y * num3;
		return new Vector2(x, y);
	}
}
