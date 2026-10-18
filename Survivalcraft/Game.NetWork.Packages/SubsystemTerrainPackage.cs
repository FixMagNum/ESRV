using System;
using System.Collections.Generic;
using Engine;

namespace Game.NetWork.Packages;

public class SubsystemTerrainPackage : IPackage, ISpatialPackage
{
	public enum DataType
	{
		RequestSyncChunks,
		SyncTerrainChunk,
		ReplyResult,
		ChangeCell,
		RequestChangeCell,
		ChangeCellsBatch,
		RequestChangeCellsBatch
	}

	public DataType m_type;

	public List<Point2> RelateChunks = new List<Point2>();

	public TerrainChunk Chunk;

	public int m_X;

	public byte m_Y;

	public int m_Z;

	public int m_value;

	public bool m_useLocal;

	public Point2 m_chunkCoords;

	public List<Point3> m_modifyCells = new List<Point3>(0);

	public List<int> m_modifyValues = new List<int>(0);

	public byte m_result;

	public byte[] DecompressedData;

	[ThreadStatic]
	private static byte[] s_chunkDataBuffer;

	public bool m_isFluidUpdateOnly;

	public byte ID => 2;

	public IClient To { get; set; }

	public IClient Except { get; set; }

	public IClient From { get; set; }

	public ClientState MinNeedState => ClientState.ProjectLoaded;

	public Vector3 SpatialPosition
	{
		get
		{
			switch (m_type)
			{
			case DataType.SyncTerrainChunk:
			case DataType.ReplyResult:
				return new Vector3(m_chunkCoords.X * 16 + 8, 128f, m_chunkCoords.Y * 16 + 8);
			case DataType.ChangeCell:
			case DataType.RequestChangeCell:
				return new Vector3(m_X, (int)m_Y, m_Z);
			case DataType.ChangeCellsBatch:
			case DataType.RequestChangeCellsBatch:
				if (m_modifyCells != null && m_modifyCells.Count > 0)
				{
					return new Vector3(m_modifyCells[0].X, m_modifyCells[0].Y, m_modifyCells[0].Z);
				}
				return Vector3.Zero;
			default:
				return Vector3.Zero;
			}
		}
	}

	public SubsystemTerrainPackage()
	{
	}

	public SubsystemTerrainPackage(List<Point2> points)
	{
		m_type = DataType.RequestSyncChunks;
		RelateChunks.AddRange(points);
	}

	public SubsystemTerrainPackage(Point2 chunkCoord, byte result)
	{
		m_result = result;
		m_type = DataType.ReplyResult;
		m_chunkCoords = chunkCoord;
	}

	public SubsystemTerrainPackage(TerrainChunk chunk, bool useLocal)
	{
		m_type = DataType.SyncTerrainChunk;
		Chunk = chunk;
		m_chunkCoords = Chunk.Coords;
		m_useLocal = useLocal;
		m_isFluidUpdateOnly = chunk.IsFluidUpdateOnly;
	}

	public SubsystemTerrainPackage(int x, int y, int z, int v, bool request = false)
	{
		m_type = (request ? DataType.RequestChangeCell : DataType.ChangeCell);
		m_X = x;
		m_Y = (byte)y;
		m_Z = z;
		m_value = v;
	}

	public void WriteData(PackageStreamWriter writer)
	{
		writer.Write((byte)m_type);
		switch (m_type)
		{
		case DataType.RequestSyncChunks:
			writer.Write(RelateChunks.Count);
			{
				foreach (Point2 relateChunk in RelateChunks)
				{
					writer.Write(relateChunk);
				}
				break;
			}
		case DataType.SyncTerrainChunk:
			writer.Write(m_useLocal);
			writer.Write(m_isFluidUpdateOnly);
			writer.Write(Chunk.Coords);
			if (!m_useLocal)
			{
				if (s_chunkDataBuffer == null)
				{
					s_chunkDataBuffer = new byte[263168];
				}
				Buffer.BlockCopy(Chunk.Cells, 0, s_chunkDataBuffer, 0, 262144);
				Buffer.BlockCopy(Chunk.Shafts, 0, s_chunkDataBuffer, 262144, 1024);
				byte[] array = CommonLib.Compress(s_chunkDataBuffer);
				writer.Write(array.Length);
				writer.Write(array);
			}
			break;
		case DataType.ChangeCell:
		case DataType.RequestChangeCell:
			writer.Write(m_X);
			writer.Write(m_Y);
			writer.Write(m_Z);
			writer.Write(m_value);
			break;
		case DataType.ChangeCellsBatch:
		case DataType.RequestChangeCellsBatch:
		{
			int num = ((m_modifyCells != null) ? m_modifyCells.Count : 0);
			writer.Write(num);
			for (int i = 0; i < num; i++)
			{
				Point3 point = m_modifyCells[i];
				int value = ((m_modifyValues != null && i < m_modifyValues.Count) ? m_modifyValues[i] : 0);
				writer.Write(point.X);
				writer.Write((byte)point.Y);
				writer.Write(point.Z);
				writer.Write(value);
			}
			break;
		}
		case DataType.ReplyResult:
			writer.Write(m_chunkCoords);
			writer.Write(m_result);
			break;
		}
	}

	public void ReadData(PackageStreamReader reader)
	{
		m_type = (DataType)reader.ReadByte();
		switch (m_type)
		{
		case DataType.RequestSyncChunks:
		{
			int package = reader.ReadInt32();
			for (int timestamp = 0; timestamp < package; timestamp++)
			{
				RelateChunks.Add(reader.ReadPoint2());
			}
			break;
		}
		case DataType.SyncTerrainChunk:
			m_useLocal = reader.ReadBoolean();
			m_isFluidUpdateOnly = reader.ReadBoolean();
			m_chunkCoords = reader.ReadPoint2();
			if (!m_useLocal)
			{
				int value = reader.ReadInt32();
				DecompressedData = CommonLib.Decompress(reader.ReadBytes(value));
			}
			break;
		case DataType.ReplyResult:
			m_chunkCoords = reader.ReadPoint2();
			m_result = reader.ReadByte();
			break;
		case DataType.ChangeCell:
		case DataType.RequestChangeCell:
			m_X = reader.ReadInt32();
			m_Y = reader.ReadByte();
			m_Z = reader.ReadInt32();
			m_value = reader.ReadInt32();
			break;
		case DataType.ChangeCellsBatch:
		case DataType.RequestChangeCellsBatch:
		{
			int num = reader.ReadInt32();
			if (m_modifyCells == null)
			{
				m_modifyCells = new List<Point3>(num);
			}
			else
			{
				m_modifyCells.Clear();
			}
			if (m_modifyValues == null)
			{
				m_modifyValues = new List<int>(num);
			}
			else
			{
				m_modifyValues.Clear();
			}
			for (int arg = 0; arg < num; arg++)
			{
				int x = reader.ReadInt32();
				byte ex = reader.ReadByte();
				int name = reader.ReadInt32();
				int text = reader.ReadInt32();
				m_modifyCells.Add(new Point3(x, ex, name));
				m_modifyValues.Add(text);
			}
			break;
		}
		}
	}

	public void Handle(ProjectNet projectNet, NetNode netNode, bool isServer)
	{
		SubsystemTerrain b = projectNet.FindSubsystem<SubsystemTerrain>();
		switch (m_type)
		{
		case DataType.RequestSyncChunks:
			lock (b.TerrainUpdater.m_waitChunkListLock)
			{
				if (!b.TerrainUpdater.waitChunkList.TryGetValue(From, out var num3))
				{
					num3 = new List<Point2>();
					b.TerrainUpdater.waitChunkList.Add(From, num3);
				}
				foreach (Point2 relateChunk in RelateChunks)
				{
					if (!num3.Contains(relateChunk))
					{
						if (num3.Count < 256)
						{
							num3.Add(relateChunk);
							continue;
						}
						CommonLib.Net.QueuePackage(new SubsystemTerrainPackage(relateChunk, 1)
						{
							To = From
						});
					}
				}
				break;
			}
		case DataType.SyncTerrainChunk:
		{
			TerrainChunk terrainChunk = b.Terrain.GetChunkAtCoords(m_chunkCoords.X, m_chunkCoords.Y);
			if (terrainChunk == null)
			{
				terrainChunk = b.Terrain.AllocateChunk(m_chunkCoords.X, m_chunkCoords.Y);
			}
			if (terrainChunk == null)
			{
				break;
			}
			if (m_useLocal)
			{
				terrainChunk.ThreadState = TerrainChunkState.InvalidContents1;
			}
			else
			{
				if (terrainChunk.Cells == null)
				{
					terrainChunk.Cells = new int[65536];
				}
				if (terrainChunk.Shafts == null)
				{
					terrainChunk.Shafts = new int[256];
				}
				Buffer.BlockCopy(DecompressedData, 0, terrainChunk.Cells, 0, 262144);
				Buffer.BlockCopy(DecompressedData, 262144, terrainChunk.Shafts, 0, 1024);
				terrainChunk.ThreadState = TerrainChunkState.InvalidLight;
			}
			terrainChunk.IsFluidUpdateOnly = m_isFluidUpdateOnly;
			terrainChunk.IsRequested = false;
			terrainChunk.IsLoaded = true;
			terrainChunk.WasUpgraded = true;
			break;
		}
		case DataType.ReplyResult:
		{
			TerrainChunk chunkAtCoords = b.Terrain.GetChunkAtCoords(m_chunkCoords.X, m_chunkCoords.Y);
			if (chunkAtCoords != null)
			{
				chunkAtCoords.IsRequested = false;
				chunkAtCoords.WasUpgraded = true;
				chunkAtCoords.WasDowngraded = true;
			}
			break;
		}
		case DataType.ChangeCell:
		case DataType.RequestChangeCell:
		{
			int num = m_X >> 4;
			int num2 = m_Z >> 4;
			if (isServer)
			{
				List<Point3> cells = new List<Point3>
				{
					new Point3(m_X, m_Y, m_Z)
				};
				List<int> values = new List<int> { m_value };
				List<Point3> list3 = new List<Point3>();
				List<int> list4 = new List<int>();
				ServerSecurity.FilterTerrainEdits(projectNet, From, cells, values, list3, list4);
				if (list3.Count != 0)
				{
					if (m_type == DataType.RequestChangeCell)
					{
						b.ChangeCell(list3[0].X, list3[0].Y, list3[0].Z, list4[0]);
					}
					else
					{
						b.ChangeCellNet(list3[0].X, list3[0].Y, list3[0].Z, list4[0]);
					}
				}
			}
			else if (b.Terrain.GetChunkAtCoords(num, num2) == null)
			{
				CommonLib.Net.QueuePackage(new SubsystemTerrainPackage(new List<Point2>
				{
					new Point2(num, num2)
				}));
			}
			else if (m_type == DataType.RequestChangeCell)
			{
				b.ChangeCell(m_X, m_Y, m_Z, m_value);
			}
			else
			{
				b.ChangeCellNet(m_X, m_Y, m_Z, m_value);
			}
			break;
		}
		case DataType.ChangeCellsBatch:
		{
			if (isServer)
			{
				List<Point3> cells2 = ((m_modifyCells != null) ? m_modifyCells : new List<Point3>());
				List<int> values2 = ((m_modifyValues != null) ? m_modifyValues : new List<int>());
				List<Point3> list5 = new List<Point3>();
				List<int> list6 = new List<int>();
				ServerSecurity.FilterTerrainEdits(projectNet, From, cells2, values2, list5, list6);
				for (int j = 0; j < list5.Count; j++)
				{
					b.ChangeCellNet(list5[j].X, list5[j].Y, list5[j].Z, list6[j]);
				}
				break;
			}
			int num4 = ((m_modifyCells != null) ? m_modifyCells.Count : 0);
			for (int k = 0; k < num4; k++)
			{
				Point3 point = m_modifyCells[k];
				int value = ((m_modifyValues != null && k < m_modifyValues.Count) ? m_modifyValues[k] : 0);
				int num5 = point.X >> 4;
				int num6 = point.Z >> 4;
				if (b.Terrain.GetChunkAtCoords(num5, num6) == null)
				{
					CommonLib.Net.QueuePackage(new SubsystemTerrainPackage(new List<Point2>
					{
						new Point2(num5, num6)
					}));
				}
				else
				{
					b.ChangeCellNet(point.X, point.Y, point.Z, value);
				}
			}
			break;
		}
		case DataType.RequestChangeCellsBatch:
		{
			List<Point3> list = new List<Point3>();
			List<int> list2 = new List<int>();
			ServerSecurity.FilterTerrainEdits(projectNet, From, m_modifyCells, m_modifyValues, list, list2);
			for (int i = 0; i < list.Count; i++)
			{
				b.ChangeCellNet(list[i].X, list[i].Y, list[i].Z, list2[i]);
			}
			if (list.Count > 0)
			{
				CommonLib.Net.QueuePackage(new SubsystemTerrainPackage(list, list2, request: false));
			}
			break;
		}
		}
	}

	public SubsystemTerrainPackage(List<Point3> cells, List<int> values, bool request)
	{
		m_type = (request ? DataType.RequestChangeCellsBatch : DataType.ChangeCellsBatch);
		int num = Math.Min(cells?.Count ?? 0, values?.Count ?? 0);
		m_modifyCells = new List<Point3>(num);
		m_modifyValues = new List<int>(num);
		for (int i = 0; i < num; i++)
		{
			m_modifyCells.Add(cells[i]);
			m_modifyValues.Add(values[i]);
		}
	}
}
