using System;
using LiteNetLib.Utils;

namespace Game.NetWork;

public class Package
{
	public readonly PackageFlag Flag;

	public readonly IClient FromClient;

	public byte FromClientID
	{
		get
		{
			if (FromClient == null)
			{
				return 0;
			}
			return FromClient.ID;
		}
	}

	public Guid Guid
	{
		get
		{
			if (FromClient == null)
			{
				return CommonLib.UserId;
			}
			return FromClient.GUID;
		}
	}

	public bool IsReadOnly => Reader != null;

	public NetDataReader Reader { get; private set; }

	public NetDataWriter Writer { get; private set; }

	public int PackageSize
	{
		get
		{
			if (Writer != null)
			{
				return Writer.Data.Length;
			}
			if (Reader != null)
			{
				return Reader.RawDataSize;
			}
			return 0;
		}
	}

	public Package(IClient sender, NetDataReader reader)
	{
		Flag = (PackageFlag)reader.GetByte();
		FromClient = sender;
		byte[] array = new byte[reader.AvailableBytes];
		reader.GetBytes(array, reader.AvailableBytes);
		Reader = new NetDataReader(array);
	}

	public Package(IClient sender, PackageFlag flag)
	{
		Flag = flag;
		FromClient = sender;
		Writer = new NetDataWriter();
		Writer.Put((byte)flag);
	}

	public Package GetReadOnly()
	{
		return new Package(FromClient, new NetDataReader(Writer));
	}
}
