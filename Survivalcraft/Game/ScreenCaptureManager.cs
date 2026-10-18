using System;
using System.Collections.Generic;
using System.IO;
using Engine;
using Engine.Graphics;
using Engine.Media;

namespace Game;

public static class ScreenCaptureManager
{
	public static readonly string ScreenshotDir = "app:/ScreenCapture";

	public static bool m_captureRequested;

	public static Action m_successHandler;

	public static Action<Exception> m_failureHandler;

	public static void Run()
	{
		if (!m_captureRequested)
		{
			return;
		}
		try
		{
			int num;
			int height;
			switch (SettingsManager.ScreenshotSize)
			{
			case ScreenshotSize.ScreenSize:
			{
				num = MathUtils.Max(Window.ScreenSize.X, Window.ScreenSize.Y);
				height = MathUtils.Min(Window.ScreenSize.X, Window.ScreenSize.Y);
				float num2 = (float)num / (float)height;
				num = MathUtils.Min(num, 2048);
				height = (int)MathUtils.Round((float)num / num2);
				break;
			}
			case ScreenshotSize.HD:
				num = 1280;
				height = 720;
				break;
			case ScreenshotSize.FullHD:
				num = 1920;
				height = 1080;
				break;
			case ScreenshotSize.QuadHD:
				num = 2560;
				height = 1440;
				break;
			case ScreenshotSize.UltraHD:
				num = 3840;
				height = 2160;
				break;
			case ScreenshotSize.QuadUHD:
				num = 7680;
				height = 4320;
				break;
			default:
				num = 1920;
				height = 1080;
				break;
			}
			DateTime now = DateTime.Now;
			Capture(num, height, $"Survivalcraft {now.Year:D4}-{now.Month:D2}-{now.Day:D2} {now.Hour:D2}-{now.Minute:D2}-{now.Second:D2}.jpg");
			m_successHandler?.Invoke();
			GC.Collect();
		}
		catch (Exception ex)
		{
			Log.Error("Error capturing screen. Reason: " + ex.Message);
			m_failureHandler?.Invoke(ex);
		}
		finally
		{
			m_captureRequested = false;
			m_successHandler = null;
			m_failureHandler = null;
		}
	}

	public static void CapturePhoto(Action success, Action<Exception> failure)
	{
		if (!m_captureRequested)
		{
			m_captureRequested = true;
			m_successHandler = success;
			m_failureHandler = failure;
		}
	}

	public static void Capture(int width, int height, string filename)
	{
		if (GameManager.Project == null)
		{
			return;
		}
		using RenderTarget2D boundingBox = new RenderTarget2D(width, height, 1, ColorFormat.Rgba8888, DepthFormat.Depth24Stencil8);
		RenderTarget2D boundingBox2 = Display.RenderTarget;
		Dictionary<ComponentGui, bool> vector = new Dictionary<ComponentGui, bool>();
		ResolutionMode vector2 = ResolutionMode.High;
		try
		{
			if (!SettingsManager.ShowGuiInScreenshots)
			{
				foreach (ComponentPlayer componentPlayer in GameManager.Project.FindSubsystem<SubsystemPlayers>(throwOnError: true).ComponentPlayers)
				{
					vector[componentPlayer.ComponentGui] = componentPlayer.ComponentGui.ControlsContainerWidget.IsVisible;
					componentPlayer.ComponentGui.ControlsContainerWidget.IsVisible = false;
				}
			}
			vector2 = SettingsManager.ResolutionMode;
			SettingsManager.ResolutionMode = ResolutionMode.High;
			Display.RenderTarget = boundingBox;
			ScreensManager.Draw();
			if (SettingsManager.ShowLogoInScreenshots)
			{
				PrimitivesRenderer2D primitivesRenderer2D = new PrimitivesRenderer2D();
				Texture2D texture2D = ContentManager.Get<Texture2D>("Textures/Gui/ScreenCaptureOverlay");
				Vector2 vector3 = new Vector2((width - texture2D.Width) / 2, 0f);
				Vector2 corner = vector3 + new Vector2(texture2D.Width, texture2D.Height);
				primitivesRenderer2D.TexturedBatch(texture2D, useAlphaTest: false, 0, DepthStencilState.None).QueueQuad(vector3, corner, 0f, new Vector2(0f, 0f), new Vector2(1f, 1f), Color.White);
				primitivesRenderer2D.Flush();
			}
		}
		finally
		{
			Display.RenderTarget = boundingBox2;
			foreach (KeyValuePair<ComponentGui, bool> item in vector)
			{
				item.Key.ControlsContainerWidget.IsVisible = item.Value;
			}
			SettingsManager.ResolutionMode = vector2;
		}
		Image num3 = new Image(boundingBox.Width, boundingBox.Height);
		boundingBox.GetData(num3.Pixels, 0, new Engine.Rectangle(0, 0, boundingBox.Width, boundingBox.Height));
		if (!Storage.DirectoryExists(ScreenshotDir))
		{
			Storage.CreateDirectory(ScreenshotDir);
		}
		using Stream v = Storage.OpenFile(Storage.CombinePaths(ScreenshotDir, filename), OpenFileMode.CreateOrOpen);
		Image.Save(num3, v, ImageFileFormat.Jpg, saveAlpha: false);
	}
}
