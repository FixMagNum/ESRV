using System;
using Engine;
using Engine.Graphics;

namespace Game;

public class DistantChunk : IDisposable
{
	public Point2 Coords;

	public BoundingBox BoundingBox;

	public VertexBuffer VertexBuffer;

	public IndexBuffer IndexBuffer;

	public int IndexCount;

	public bool IsReady;

	public bool IsDisposed;

	public int LodStep;

	public DistantChunk(Point2 coords)
	{
		Coords = coords;
		Vector3 vector = new Vector3((float)coords.X * 128f, 0f, (float)coords.Y * 128f);
		Vector3 max = vector + new Vector3(128f, 256f, 128f);
		BoundingBox = new BoundingBox(vector, max);
	}

	public void Dispose()
	{
		if (!IsDisposed)
		{
			if (VertexBuffer != null)
			{
				VertexBuffer.Dispose();
			}
			if (IndexBuffer != null)
			{
				IndexBuffer.Dispose();
			}
			IsDisposed = true;
		}
	}
}
