using System;
using System.IO;

namespace Game;

public class ContentInfo
{
	public MemoryStream ContentStream;

	public string AbsolutePath;

	public string ContentPath;

	public string Filename;

	public object Instance;

	public ContentInfo(string AbsolutePath_)
	{
		AbsolutePath = AbsolutePath_;
		int data = AbsolutePath_.LastIndexOf('.');
		ContentPath = ((data > -1) ? AbsolutePath_.Substring(0, data) : AbsolutePath_);
		Filename = Path.GetFileName(AbsolutePath);
	}

	public void SetContentStream(Stream stream)
	{
		if (stream is MemoryStream)
		{
			ContentStream = stream as MemoryStream;
			ContentStream.Position = 0L;
			return;
		}
		throw new Exception("Can't set ContentStream width type " + stream.GetType().Name);
	}

	public Stream Duplicate()
	{
		if (ContentStream == null || !ContentStream.CanRead || !ContentStream.CanWrite)
		{
			throw new Exception("ContentStream has been disposed");
		}
		MemoryStream point = new MemoryStream();
		ContentStream.CopyTo(point);
		ContentStream.Position = 0L;
		point.Position = 0L;
		return point;
	}

	public void Dispose()
	{
		ContentStream?.Dispose();
	}
}
