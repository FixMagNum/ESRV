using System;
using System.IO;
using System.Runtime.InteropServices;
using Engine.Media;

namespace Engine.Graphics;

public sealed class RenderTarget2D : Texture2D
{
	internal int m_frameBuffer;

	internal int m_depthBuffer;

	public DepthFormat DepthFormat { get; private set; }

	public RenderTarget2D(int width, int height, int mipLevelsCount, ColorFormat colorFormat, DepthFormat depthFormat)
		: base(width, height, mipLevelsCount, colorFormat)
	{
		try
		{
			InitializeRenderTarget2D(width, height, mipLevelsCount, colorFormat, depthFormat);
			AllocateRenderTarget();
		}
		catch
		{
			Dispose();
			throw;
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		DeleteRenderTarget();
	}

	public void GetData<T>(T[] target, int targetStartIndex, Rectangle sourceRectangle) where T : struct
	{
		VerifyParametersGetData(target, targetStartIndex, sourceRectangle);
		GCHandle gCHandle = GCHandle.Alloc(target, GCHandleType.Pinned);
		try
		{
			GLWrapper.BindFramebuffer(m_frameBuffer);
		}
		finally
		{
			gCHandle.Free();
		}
	}

	public void GenerateMipMaps()
	{
	}

	internal override void HandleDeviceLost()
	{
		DeleteRenderTarget();
	}

	internal override void HandleDeviceReset()
	{
		AllocateRenderTarget();
	}

	private void AllocateRenderTarget()
	{
	}

	private void DeleteRenderTarget()
	{
		if (m_depthBuffer != 0)
		{
			m_depthBuffer = 0;
		}
		if (m_frameBuffer != 0)
		{
			GLWrapper.DeleteFramebuffer(m_frameBuffer);
			m_frameBuffer = 0;
		}
	}

	public static void Save(RenderTarget2D renderTarget, Stream stream, ImageFileFormat format, bool saveAlpha)
	{
		if (renderTarget.ColorFormat != ColorFormat.Rgba8888)
		{
			throw new InvalidOperationException("Unsupported color format.");
		}
		Image image = new Image(renderTarget.Width, renderTarget.Height);
		renderTarget.GetData(image.Pixels, 0, new Rectangle(0, 0, renderTarget.Width, renderTarget.Height));
		Image.Save(image, stream, format, saveAlpha);
	}

	public override int GetGpuMemoryUsage()
	{
		return base.GetGpuMemoryUsage() + DepthFormat.GetSize() * base.Width * base.Height;
	}

	private void InitializeRenderTarget2D(int width, int height, int mipLevelsCount, ColorFormat colorFormat, DepthFormat depthFormat)
	{
		DepthFormat = depthFormat;
	}

	private void VerifyParametersGetData<T>(T[] target, int targetStartIndex, Rectangle sourceRectangle) where T : struct
	{
		VerifyNotDisposed();
		int size = base.ColorFormat.GetSize();
		int num = Utilities.SizeOf<T>();
		if (target == null)
		{
			throw new ArgumentNullException("target");
		}
		if (num > size)
		{
			throw new ArgumentNullException("Target array element size is larger than pixel size.");
		}
		if (size % num != 0)
		{
			throw new ArgumentNullException("Pixel size is not an integer multiple of target array element size.");
		}
		if (sourceRectangle.Left < 0 || sourceRectangle.Width <= 0 || sourceRectangle.Top < 0 || sourceRectangle.Height <= 0 || sourceRectangle.Left + sourceRectangle.Width > base.Width || sourceRectangle.Top + sourceRectangle.Height > base.Height)
		{
			throw new ArgumentOutOfRangeException("sourceRectangle");
		}
		if (targetStartIndex < 0 || targetStartIndex >= target.Length)
		{
			throw new ArgumentOutOfRangeException("targetStartIndex");
		}
		if ((target.Length - targetStartIndex) * num < sourceRectangle.Width * sourceRectangle.Height * size)
		{
			throw new InvalidOperationException("Not enough space in target array.");
		}
	}
}
