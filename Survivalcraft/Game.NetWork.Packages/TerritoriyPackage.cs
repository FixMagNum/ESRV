using System;
using Engine;

namespace Game.NetWork.Packages;

public class TerritoriyPackage : IPackage
{
	public Guid m_guid;

	public bool m_allowPlace;

	public bool m_allowDig;

	public bool m_applyToFirend;

	public bool m_allowBehavior;

	public bool m_isVisible;

	public bool m_blockFire;

	public bool m_blockPvp;

	public Point3 m_coord;

	public byte ID => 23;

	public IClient To { get; set; }

	public IClient Except { get; set; }

	public IClient From { get; set; }

	public ClientState MinNeedState => ClientState.ProjectLoaded;

	public TerritoriyPackage()
	{
	}

	public TerritoriyPackage(Territoriy territoriy)
	{
		m_guid = territoriy.OwnerGuid;
		m_coord = territoriy.OwnChunkCoord;
		m_allowBehavior = territoriy.AllowBlockBehavior;
		m_allowDig = territoriy.AllowDig;
		m_allowPlace = territoriy.AllowPlace;
		m_applyToFirend = territoriy.ApplyToFirend;
		m_isVisible = territoriy.IsVisible;
		m_blockFire = territoriy.BlockFire;
		m_blockPvp = territoriy.BlockPvp;
	}

	public void WriteData(PackageStreamWriter writer)
	{
		writer.Write(m_guid);
		writer.Write(m_coord.X);
		writer.Write(m_coord.Y);
		writer.Write(m_coord.Z);
		byte b = 0;
		if (m_allowBehavior)
		{
			b |= 1;
		}
		if (m_allowDig)
		{
			b |= 2;
		}
		if (m_allowPlace)
		{
			b |= 4;
		}
		if (m_applyToFirend)
		{
			b |= 8;
		}
		if (m_isVisible)
		{
			b |= 0x10;
		}
		if (m_blockFire)
		{
			b |= 0x20;
		}
		if (m_blockPvp)
		{
			b |= 0x40;
		}
		writer.Write(b);
	}

	public void ReadData(PackageStreamReader reader)
	{
		m_guid = reader.ReadGuid();
		int x = reader.ReadInt32();
		int y = reader.ReadInt32();
		int z = reader.ReadInt32();
		m_coord = new Point3(x, y, z);
		byte b = reader.ReadByte();
		m_allowBehavior = (b & 1) > 0;
		m_allowDig = (b & 2) > 0;
		m_allowPlace = (b & 4) > 0;
		m_applyToFirend = (b & 8) > 0;
		m_isVisible = (b & 0x10) > 0;
		m_blockFire = (b & 0x20) > 0;
		m_blockPvp = (b & 0x40) > 0;
	}

	public void Handle(ProjectNet projectNet, NetNode netNode, bool isServer)
	{
		if (!SubsystemBedrockBlockBehavior.Territories.TryGetValue(m_guid, out var value))
		{
			return;
		}
		Territoriy territoriy = value.Find((Territoriy tt) => tt.OwnChunkCoord == m_coord);
		if ((territoriy != null) & isServer)
		{
			PlayerData playerData = ((From != null) ? From.PlayerData : null);
			if (playerData == null || (!playerData.ServerManager && !(playerData.PlayerGUID == territoriy.OwnerGuid)))
			{
				ServerSecurity.Warn(From, "territory change by a non owner");
				return;
			}
		}
		if (territoriy != null)
		{
			territoriy.AllowBlockBehavior = m_allowBehavior;
			territoriy.AllowDig = m_allowDig;
			territoriy.AllowPlace = m_allowPlace;
			territoriy.ApplyToFirend = m_applyToFirend;
			territoriy.IsVisible = m_isVisible;
			territoriy.BlockFire = m_blockFire;
			territoriy.BlockPvp = m_blockPvp;
		}
		if (isServer)
		{
			Except = From;
			netNode.QueuePackage(this);
		}
	}
}
