using System;
using System.Runtime.InteropServices;

namespace Engine.Graphics;

public sealed class VertexBuffer : GraphicsResource
{
	internal int m_buffer;

	public string DebugName
	{
		get
		{
			return string.Empty;
		}
		set
		{
		}
	}

	public VertexDeclaration VertexDeclaration { get; private set; }

	public int VerticesCount { get; private set; }

	public object Tag { get; set; }

	public VertexBuffer(VertexDeclaration vertexDeclaration, int verticesCount)
	{
		InitializeVertexBuffer(vertexDeclaration, verticesCount);
		AllocateBuffer();
	}

	public override void Dispose()
	{
		base.Dispose();
		DeleteBuffer();
	}

	public void SetData<T>(T[] source, int sourceStartIndex, int sourceCount, int targetStartIndex = 0) where T : struct
	{
		VerifyParametersSetData(source, sourceStartIndex, sourceCount, targetStartIndex);
		GCHandle gCHandle = GCHandle.Alloc(source, GCHandleType.Pinned);
		try
		{
			int num = Utilities.SizeOf<T>();
			int vertexStride = VertexDeclaration.VertexStride;
		}
		finally
		{
			gCHandle.Free();
		}
	}

	internal override void HandleDeviceLost()
	{
		DeleteBuffer();
	}

	internal override void HandleDeviceReset()
	{
		AllocateBuffer();
	}

	private void AllocateBuffer()
	{
	}

	private void DeleteBuffer()
	{
		if (m_buffer != 0)
		{
			m_buffer = 0;
		}
	}

	public override int GetGpuMemoryUsage()
	{
		return VertexDeclaration.VertexStride * VerticesCount;
	}

	private void InitializeVertexBuffer(VertexDeclaration vertexDeclaration, int verticesCount)
	{
		if (vertexDeclaration == null)
		{
			throw new ArgumentNullException("vertexDeclaration");
		}
		if (verticesCount <= 0)
		{
			throw new ArgumentException("verticesCount must be greater than 0.");
		}
		VertexDeclaration = vertexDeclaration;
		VerticesCount = verticesCount;
	}

	private void VerifyParametersSetData<T>(T[] source, int sourceStartIndex, int sourceCount, int targetStartIndex = 0) where T : struct
	{
		VerifyNotDisposed();
		int num = Utilities.SizeOf<T>();
		int vertexStride = VertexDeclaration.VertexStride;
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (sourceStartIndex < 0 || sourceCount < 0 || sourceStartIndex + sourceCount > source.Length)
		{
			throw new ArgumentException("Range is out of source bounds.");
		}
		if (targetStartIndex < 0 || targetStartIndex * vertexStride + sourceCount * num > VerticesCount * vertexStride)
		{
			throw new ArgumentException("Range is out of target bounds.");
		}
	}
}
