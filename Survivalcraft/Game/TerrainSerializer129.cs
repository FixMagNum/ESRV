using System;
using System.Collections.Generic;
using System.IO;
using Engine;

namespace Game;

public class TerrainSerializer129 : IDisposable
{
	public const int MaxChunks = 65536;

	public const int TocEntryBytesCount = 12;

	public const int TocBytesCount = 786444;

	public const int ChunkSizeX = 16;

	public const int ChunkSizeY = 128;

	public const int ChunkSizeZ = 16;

	public const int ChunkBitsX = 4;

	public const int ChunkBitsZ = 4;

	public const int ChunkBytesCount = 132112;

	public const string ChunksFileName = "Chunks32.dat";

	public Terrain m_terrain;

	public byte[] m_buffer = new byte[131072];

	public Dictionary<Point2, long> m_chunkOffsets = new Dictionary<Point2, long>();

	public Stream m_stream;

	public TerrainSerializer129(Terrain terrain, string directoryName)
	{
		m_terrain = terrain;
		string num = Storage.CombinePaths(directoryName, "Chunks32.dat");
		if (!Storage.FileExists(num))
		{
			using Stream block = Storage.OpenFile(num, OpenFileMode.Create);
			for (int i = 0; i < 65537; i++)
			{
				WriteTOCEntry(block, 0, 0, -1);
			}
		}
		m_stream = Storage.OpenFile(num, OpenFileMode.ReadWrite);
		while (true)
		{
			ReadTOCEntry(m_stream, out var cx, out var cz, out var index);
			if (index >= 0)
			{
				m_chunkOffsets[new Point2(cx, cz)] = 786444 + 132112L * (long)index;
				continue;
			}
			break;
		}
	}

	public bool LoadChunk(TerrainChunk chunk)
	{
		return LoadChunkBlocks(chunk);
	}

	public void SaveChunk(TerrainChunk chunk)
	{
		if (chunk.State > TerrainChunkState.InvalidContents4 && chunk.ModificationCounter > 0)
		{
			SaveChunkBlocks(chunk);
			chunk.ModificationCounter = 0;
		}
	}

	public void Dispose()
	{
		Utilities.Dispose(ref m_stream);
	}

	public static void ReadChunkHeader(Stream stream)
	{
		int num = ReadInt(stream);
		int num2 = ReadInt(stream);
		ReadInt(stream);
		ReadInt(stream);
		if (num != -559038737 || num2 != -2)
		{
			throw new InvalidOperationException("Invalid chunk header.");
		}
	}

	public static void WriteChunkHeader(Stream stream, int cx, int cz)
	{
		WriteInt(stream, -559038737);
		WriteInt(stream, -2);
		WriteInt(stream, cx);
		WriteInt(stream, cz);
	}

	public static void ReadTOCEntry(Stream stream, out int cx, out int cz, out int index)
	{
		cx = ReadInt(stream);
		cz = ReadInt(stream);
		index = ReadInt(stream);
	}

	public static void WriteTOCEntry(Stream stream, int cx, int cz, int index)
	{
		WriteInt(stream, cx);
		WriteInt(stream, cz);
		WriteInt(stream, index);
	}

	public unsafe bool LoadChunkBlocks(TerrainChunk chunk)
	{
		bool result = false;
		int num = chunk.Origin.X >> 4;
		int num2 = chunk.Origin.Y >> 4;
		try
		{
			if (!m_chunkOffsets.TryGetValue(new Point2(num, num2), out var value))
			{
				return result;
			}
			m_stream.Seek(value, SeekOrigin.Begin);
			ReadChunkHeader(m_stream);
			m_stream.Read(m_buffer, 0, 131072);
			fixed (byte* ptr = &m_buffer[0])
			{
				int* ptr2 = (int*)ptr;
				for (int i = 0; i < 16; i++)
				{
					for (int j = 0; j < 16; j++)
					{
						int num3 = TerrainChunk.CalculateCellIndex(i, 0, j);
						int num4 = 0;
						while (num4 < 128)
						{
							chunk.SetCellValueFast(num3, *ptr2);
							num4++;
							num3++;
							ptr2++;
						}
					}
				}
			}
			m_stream.Read(m_buffer, 0, 1024);
			fixed (byte* ptr = &m_buffer[0])
			{
				int* ptr3 = (int*)ptr;
				for (int k = 0; k < 16; k++)
				{
					for (int l = 0; l < 16; l++)
					{
						m_terrain.SetShaftValue(k + chunk.Origin.X, l + chunk.Origin.Y, *ptr3);
						ptr3++;
					}
				}
			}
			result = true;
			return result;
		}
		catch (Exception e)
		{
			Log.Error(ExceptionManager.MakeFullErrorMessage($"Error loading data for chunk ({num},{num2}).", e));
			return result;
		}
	}

	public unsafe void SaveChunkBlocks(TerrainChunk chunk)
	{
		int cellContents = chunk.Origin.X >> 4;
		int num = chunk.Origin.Y >> 4;
		try
		{
			bool flag = false;
			if (m_chunkOffsets.TryGetValue(new Point2(cellContents, num), out var value))
			{
				m_stream.Seek(value, SeekOrigin.Begin);
			}
			else
			{
				flag = true;
				value = m_stream.Length;
				m_stream.Seek(value, SeekOrigin.Begin);
			}
			WriteChunkHeader(m_stream, cellContents, num);
			fixed (byte* ptr = &m_buffer[0])
			{
				int* ptr2 = (int*)ptr;
				for (int i = 0; i < 16; i++)
				{
					for (int j = 0; j < 16; j++)
					{
						int num2 = TerrainChunk.CalculateCellIndex(i, 0, j);
						int num3 = 0;
						while (num3 < 128)
						{
							*ptr2 = chunk.GetCellValueFast(num2);
							num3++;
							num2++;
							ptr2++;
						}
					}
				}
			}
			m_stream.Write(m_buffer, 0, 131072);
			fixed (byte* ptr = &m_buffer[0])
			{
				int* ptr3 = (int*)ptr;
				for (int k = 0; k < 16; k++)
				{
					for (int l = 0; l < 16; l++)
					{
						*ptr3 = m_terrain.GetShaftValue(k + chunk.Origin.X, l + chunk.Origin.Y);
						ptr3++;
					}
				}
			}
			m_stream.Write(m_buffer, 0, 1024);
			if (flag)
			{
				m_stream.Flush();
				int num4 = m_chunkOffsets.Count % 65536 * 3 * 4;
				m_stream.Seek(num4, SeekOrigin.Begin);
				WriteInt(m_stream, cellContents);
				WriteInt(m_stream, num);
				WriteInt(m_stream, m_chunkOffsets.Count);
				m_chunkOffsets[new Point2(cellContents, num)] = value;
			}
			m_stream.Flush();
		}
		catch (Exception e)
		{
			Log.Error(ExceptionManager.MakeFullErrorMessage($"Error writing data for chunk ({cellContents},{num}).", e));
		}
	}

	public static int ReadInt(Stream stream)
	{
		return stream.ReadByte() + (stream.ReadByte() << 8) + (stream.ReadByte() << 16) + (stream.ReadByte() << 24);
	}

	public static void WriteInt(Stream stream, int value)
	{
		stream.WriteByte((byte)value);
		stream.WriteByte((byte)(value >> 8));
		stream.WriteByte((byte)(value >> 16));
		stream.WriteByte((byte)(value >> 24));
	}
}
