using System;
using System.Net;
using System.Net.Sockets;

namespace Game.NetWork;

internal class WebTime
{
	private const string ntpServer = "ntp.aliyun.com";

	private const byte serverReplyTime = 40;

	public static DateTime getWebTime()
	{
		try
		{
			byte[] array = new byte[48];
			array[0] = 27;
			IPEndPoint remoteEP = new IPEndPoint(Dns.GetHostEntry("ntp.aliyun.com").AddressList[0], 123);
			Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			socket.Connect(remoteEP);
			socket.ReceiveTimeout = 10;
			socket.Send(array);
			socket.Receive(array);
			socket.Close();
			ulong x = BitConverter.ToUInt32(array, 40);
			ulong x2 = BitConverter.ToUInt32(array, 44);
			ulong num = swapEndian(x);
			x2 = swapEndian(x2);
			ulong num2 = num * 1000 + x2 * 1000 / 4294967296L;
			return new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(num2).ToLocalTime();
		}
		catch
		{
			DialogsManager.Confirm("网络检查不通过,请确保网络通畅再打开游戏!", (MessageDialogButton btn) =>
			{
				Environment.Exit(0);
			});
			return new DateTime(0L);
		}
	}

	private static uint swapEndian(ulong x)
	{
		return (uint)(((x & 0xFF) << 24) + ((x & 0xFF00) << 8) + ((x & 0xFF0000) >> 8) + ((x & 0xFF000000u) >> 24));
	}
}
