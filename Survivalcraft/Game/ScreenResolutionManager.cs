using Engine;

namespace Game;

public static class ScreenResolutionManager
{
	public static float ApproximateScreenDpi { get; set; }

	public static float ApproximateScreenInches => MathUtils.Sqrt(Window.ScreenSize.X * Window.ScreenSize.X + Window.ScreenSize.Y * Window.ScreenSize.Y) / ApproximateScreenDpi;

	static ScreenResolutionManager()
	{
		ApproximateScreenDpi = 0.5f * (float)(Window.ScreenSize.X / Window.Size.X + Window.ScreenSize.Y / Window.Size.Y);
		ApproximateScreenDpi = MathUtils.Clamp(ApproximateScreenDpi, 96f, 800f);
	}
}
