using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Engine;

namespace Game;

public class TerrainSerializer23 : IDisposable
{
	public interface IStorage : IDisposable
	{
		void Open(string directoryName, string suffix);

		int Load(Point2 coords, byte[] buffer);

		void Save(Point2 coords, byte[] buffer, int size);

		bool Exists(Point2 coords);
	}

	public class RegionFileStorage : IStorage, IDisposable
	{
		private struct DirectoryEntry
		{
			public int Offset;

			public int Size;
		}

		private const int MaxOpenedStreams = 100;

		private const int ExtraSpaceBytes = 1024;

		private const int RegionDataOffset = 2052;

		private static uint RegionMagic;

		private static uint RegionChunkMagic;

		private string RegionsDirectoryName;

		private string TmpFilePath;

		private Dictionary<Point2, Stream> StreamsByRegion;

		private Queue<Stream> OpenedStreams;

		private TerrainSerializer23 terrainSerializer;

		private object m_lock;

		private byte[] m_headerBuffer = new byte[8];

		public void Dispose()
		{
			while (OpenedStreams.Count > 0)
			{
				OpenedStreams.Dequeue().Dispose();
			}
		}

		public RegionFileStorage(TerrainSerializer23 terrainSerializer23)
		{
			StreamsByRegion = new Dictionary<Point2, Stream>();
			OpenedStreams = new Queue<Stream>();
			m_lock = new object();
			terrainSerializer = terrainSerializer23;
		}

		public void Open(string directoryName, string suffix)
		{
			RegionsDirectoryName = Storage.CombinePaths(directoryName, "Regions" + suffix);
			Storage.CreateDirectory(RegionsDirectoryName);
			TmpFilePath = Storage.CombinePaths(RegionsDirectoryName, "tmp");
			Storage.DeleteFile(TmpFilePath);
			foreach (string num2 in Storage.ListFileNames(RegionsDirectoryName))
			{
				if (Storage.GetExtension(num2) == ".new")
				{
					string level = Storage.CombinePaths(RegionsDirectoryName, num2);
					string num3 = Storage.ChangeExtension(level, "");
					if (!Storage.FileExists(num3))
					{
						Storage.MoveFile(level, num3);
					}
					else
					{
						Storage.DeleteFile(level);
					}
				}
			}
		}

		public int Load(Point2 coords, byte[] buffer)
		{
			lock (m_lock)
			{
				Point2 num4 = new Point2(coords.X >> 4, coords.Y >> 4);
				Stream num6 = GetRegionStream(num4, createNew: false);
				if (num6 != null)
				{
					int item = (coords.X & 0xF) + ((coords.Y & 0xF) << 4);
					num6.Position = 4 + item * 8;
					num6.Read(m_headerBuffer, 0, 8);
					int num7 = m_headerBuffer[0] | (m_headerBuffer[1] << 8) | (m_headerBuffer[2] << 16) | (m_headerBuffer[3] << 24);
					int num8 = m_headerBuffer[4] | (m_headerBuffer[5] << 8) | (m_headerBuffer[6] << 16) | (m_headerBuffer[7] << 24);
					if (num7 > 0 && num8 > 0 && num8 <= buffer.Length)
					{
						num6.Position = num7;
						num6.Seek(4L, SeekOrigin.Current);
						if (num6.Read(buffer, 0, num8) == num8)
						{
							return num8;
						}
					}
				}
				return -1;
			}
		}

		public void Save(Point2 coords, byte[] buffer, int size)
		{
			Point2 region = new Point2(coords.X >> 4, coords.Y >> 4);
			Point2 point = new Point2(coords.X & 0xF, coords.Y & 0xF);
			Stream regionStream = GetRegionStream(region, createNew: true);
			string text = null;
			using (BinaryReader reader = new BinaryReader(regionStream, Encoding.UTF8, leaveOpen: true))
			{
				using BinaryWriter writer = new BinaryWriter(regionStream, Encoding.UTF8, leaveOpen: true);
				int num = point.X + 16 * point.Y;
				DirectoryEntry[] array = ReadDirectoryEntries(reader);
				DirectoryEntry directoryEntry = array[num];
				if (directoryEntry.Offset > 0)
				{
					int num2 = FindNextEntryIndex(array, num);
					if (num2 >= 0)
					{
						int num3 = array[num2].Offset - directoryEntry.Offset - 4;
						if (size <= num3)
						{
							WriteData(writer, directoryEntry.Offset, buffer, size);
							Point2 chunk = point;
							DirectoryEntry entry = new DirectoryEntry
							{
								Offset = directoryEntry.Offset,
								Size = size
							};
							WriteDirectoryEntry(writer, chunk, entry);
							regionStream.Flush();
						}
						else
						{
							text = GetRegionPath(region);
							using Stream stream = Storage.OpenFile(TmpFilePath, OpenFileMode.Create);
							using BinaryWriter binaryWriter = new BinaryWriter(stream);
							DirectoryEntry[] array2 = new DirectoryEntry[array.Length];
							int num4 = 2052;
							for (int i = 0; i < array.Length; i++)
							{
								if (i == num)
								{
									array2[i].Offset = num4;
									array2[i].Size = size;
									num4 += CalculateIdealEntrySpace(array2[i].Size);
								}
								else if (array[i].Offset > 0)
								{
									array2[i].Offset = num4;
									array2[i].Size = array[i].Size;
									num4 += CalculateIdealEntrySpace(array2[i].Size);
								}
							}
							ResizeStream(stream, num4);
							binaryWriter.Write(RegionMagic);
							WriteDirectoryEntries(binaryWriter, array2);
							byte[] buffer2 = new byte[array.Max((DirectoryEntry e) => e.Size)];
							for (int num5 = 0; num5 < array.Length; num5++)
							{
								if (num5 == num)
								{
									WriteData(binaryWriter, array2[num5].Offset, buffer, size);
								}
								else if (array[num5].Offset > 0)
								{
									ReadData(reader, array[num5].Offset, buffer2, array[num5].Size);
									WriteData(binaryWriter, array2[num5].Offset, buffer2, array2[num5].Size);
								}
							}
						}
					}
					else
					{
						if (directoryEntry.Offset + 4 + size > regionStream.Length)
						{
							ResizeStream(regionStream, directoryEntry.Offset + CalculateIdealEntrySpace(size));
						}
						WriteData(writer, directoryEntry.Offset, buffer, size);
						Point2 chunk2 = point;
						DirectoryEntry entry = new DirectoryEntry
						{
							Offset = directoryEntry.Offset,
							Size = size
						};
						WriteDirectoryEntry(writer, chunk2, entry);
						regionStream.Flush();
					}
				}
				else
				{
					int num6 = (int)regionStream.Length;
					ResizeStream(regionStream, num6 + CalculateIdealEntrySpace(size));
					WriteData(writer, num6, buffer, size);
					Point2 chunk3 = point;
					DirectoryEntry entry2 = new DirectoryEntry
					{
						Offset = num6,
						Size = size
					};
					WriteDirectoryEntry(writer, chunk3, entry2);
					regionStream.Flush();
				}
			}
			if (text != null)
			{
				regionStream.Dispose();
				string text2 = text + ".new";
				Storage.MoveFile(TmpFilePath, text2);
				Storage.MoveFile(text2, text);
			}
		}

		private string GetRegionPath(Point2 region)
		{
			return string.Format("{0}/Region {1},{2}.dat", new object[3] { RegionsDirectoryName, region.X, region.Y });
		}

		private Stream GetRegionStream(Point2 region, bool createNew)
		{
			if (!StreamsByRegion.TryGetValue(region, out var clothingData) || clothingData == null || !clothingData.CanRead)
			{
				string regionPath = GetRegionPath(region);
				if (Storage.FileExists(regionPath))
				{
					clothingData = Storage.OpenFile(regionPath, OpenFileMode.ReadWrite);
					using (BinaryReader binaryReader = new BinaryReader(clothingData, Encoding.UTF8, leaveOpen: true))
					{
						if (binaryReader.ReadUInt32() != RegionMagic)
						{
							throw new InvalidOperationException($"Invalid region file {region} magic.");
						}
					}
					OpenedStreams.Enqueue(clothingData);
				}
				else if (createNew)
				{
					clothingData = Storage.OpenFile(regionPath, OpenFileMode.Create);
					OpenedStreams.Enqueue(clothingData);
					using BinaryWriter binaryWriter = new BinaryWriter(clothingData, Encoding.UTF8, leaveOpen: true);
					binaryWriter.Write(RegionMagic);
					WriteDirectoryEntries(binaryWriter, new DirectoryEntry[256]);
				}
				else
				{
					clothingData = null;
				}
				StreamsByRegion[region] = clothingData;
				while (OpenedStreams.Count > 100)
				{
					OpenedStreams.Dequeue().Dispose();
				}
			}
			return clothingData;
		}

		private static void ReadData(BinaryReader reader, int offset, byte[] buffer, int size)
		{
			reader.BaseStream.Position = offset;
			if (reader.ReadUInt32() != RegionChunkMagic)
			{
				throw new InvalidOperationException("Invalid region file chunk magic.");
			}
			if (reader.BaseStream.Read(buffer, 0, size) != size)
			{
				throw new InvalidOperationException("Region file is truncated.");
			}
		}

		private static DirectoryEntry ReadDirectoryEntry(BinaryReader reader)
		{
			DirectoryEntry result = new DirectoryEntry
			{
				Offset = reader.ReadInt32(),
				Size = reader.ReadInt32()
			};
			if (result.Size < 0 || result.Size > 1048576)
			{
				throw new InvalidOperationException("Region file entry size out of bounds, likely corrupt region file.");
			}
			return result;
		}

		private static DirectoryEntry ReadDirectoryEntry(BinaryReader reader, Point2 chunk)
		{
			int num = chunk.X + 16 * chunk.Y;
			reader.BaseStream.Position = 4 + num * 8;
			return ReadDirectoryEntry(reader);
		}

		private static DirectoryEntry[] ReadDirectoryEntries(BinaryReader reader)
		{
			reader.BaseStream.Position = 4L;
			DirectoryEntry[] array = new DirectoryEntry[256];
			for (int i = 0; i < 256; i++)
			{
				array[i] = ReadDirectoryEntry(reader);
			}
			return array;
		}

		private static void WriteData(BinaryWriter writer, int offset, byte[] buffer, int size)
		{
			writer.BaseStream.Position = offset;
			writer.Write(RegionChunkMagic);
			writer.BaseStream.Write(buffer, 0, size);
		}

		private static void WriteDirectoryEntry(BinaryWriter writer, DirectoryEntry entry)
		{
			writer.Write(entry.Offset);
			writer.Write(entry.Size);
		}

		private static void WriteDirectoryEntry(BinaryWriter writer, Point2 chunk, DirectoryEntry entry)
		{
			int num = chunk.X + 16 * chunk.Y;
			writer.BaseStream.Position = 4 + num * 8;
			WriteDirectoryEntry(writer, entry);
		}

		private static void WriteDirectoryEntries(BinaryWriter writer, DirectoryEntry[] entries)
		{
			writer.BaseStream.Position = 4L;
			for (int i = 0; i < 256; i++)
			{
				WriteDirectoryEntry(writer, entries[i]);
			}
		}

		private static void ResizeStream(Stream stream, int size)
		{
			if (size > 268435456)
			{
				throw new InvalidOperationException("Region file too large.");
			}
			stream.SetLength(size);
		}

		private static int FindNextEntryIndex(DirectoryEntry[] entries, int index)
		{
			int result = -1;
			int num = int.MaxValue;
			for (int i = 0; i < entries.Length; i++)
			{
				int num2 = entries[i].Offset - entries[index].Offset;
				if (num2 > 0 && num2 < num)
				{
					num = num2;
					result = i;
				}
			}
			return result;
		}

		private static int CalculateIdealEntrySpace(int size)
		{
			return size + 1024 + 4;
		}

		private static uint MakeFourCC(string s)
		{
			return ((uint)s[3] << 24) | ((uint)s[2] << 16) | ((uint)s[1] << 8) | s[0];
		}

		public bool Exists(Point2 coords)
		{
			lock (m_lock)
			{
				Point2 region = new Point2(coords.X >> 4, coords.Y >> 4);
				Stream regionStream = GetRegionStream(region, createNew: false);
				if (regionStream != null)
				{
					int num = (coords.X & 0xF) + ((coords.Y & 0xF) << 4);
					regionStream.Position = 4 + num * 8;
					regionStream.Read(m_headerBuffer, 0, 4);
					return (m_headerBuffer[0] | (m_headerBuffer[1] << 8) | (m_headerBuffer[2] << 16) | (m_headerBuffer[3] << 24)) > 0;
				}
				return false;
			}
		}

		static RegionFileStorage()
		{
			RegionMagic = MakeFourCC("RGN1");
			RegionChunkMagic = MakeFourCC("CHK1");
		}
	}

	private const int WorstCaseChunkDataSize = 262400;

	private object m_lock = new object();

	public IStorage m_storage;

	private byte[] m_storageBuffer = new byte[262400];

	private byte[] m_compressBuffer = new byte[262400];

	public TerrainSerializer23(string directoryName, string suffix = "")
	{
		m_storage = new RegionFileStorage(this);
		m_storage.Open(directoryName, suffix);
	}

	public TerrainSerializer23()
	{
	}

	public bool LoadChunk(TerrainChunk chunk)
	{
		return LoadChunkData(chunk);
	}

	public void SaveChunk(TerrainChunk chunk)
	{
		if (chunk.State > TerrainChunkState.InvalidContents4 && chunk.ModificationCounter > 0)
		{
			SaveChunkData(chunk);
			chunk.ModificationCounter = 0;
		}
	}

	public bool LoadChunkData(TerrainChunk chunk)
	{
		lock (m_lock)
		{
			try
			{
				int position = m_storage.Load(chunk.Coords, m_storageBuffer);
				if (position < 0)
				{
					return false;
				}
				DecompressChunkData(chunk, m_storageBuffer, position);
			}
			catch (Exception e)
			{
				Log.Error(ExceptionManager.MakeFullErrorMessage(string.Format("Error loading chunk ({0},{1}).", new object[2]
				{
					chunk.Coords.X,
					chunk.Coords.Y
				}), e));
			}
			return true;
		}
	}

	public void SaveChunkData(TerrainChunk chunk)
	{
		lock (m_lock)
		{
			try
			{
				int size = CompressChunkData(chunk, m_storageBuffer);
				m_storage.Save(chunk.Coords, m_storageBuffer, size);
			}
			catch (Exception e)
			{
				Log.Error(ExceptionManager.MakeFullErrorMessage(string.Format("Error saving chunk ({0},{1}).", new object[2]
				{
					chunk.Coords.X,
					chunk.Coords.Y
				}), e));
			}
		}
	}

	public virtual void Dispose()
	{
		Utilities.Dispose(ref m_storage);
	}

	private int CompressChunkData(TerrainChunk chunk, byte[] buffer)
	{
		int num = 0;
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				int shaftValueFast = chunk.GetShaftValueFast(i, j);
				m_compressBuffer[num++] = (byte)((Terrain.ExtractTemperature(shaftValueFast) << 4) | Terrain.ExtractHumidity(shaftValueFast));
			}
		}
		int num2 = 0;
		int num3 = -1;
		for (int k = 0; k < 256; k++)
		{
			for (int l = 0; l < 16; l++)
			{
				for (int m = 0; m < 16; m++)
				{
					int num4 = Terrain.ReplaceLight(chunk.GetCellValueFast(m, k, l), 0);
					if (num2 == 0)
					{
						num3 = num4;
						num2 = 1;
						continue;
					}
					if (num4 != num3)
					{
						num = WriteRleValueToBuffer(m_compressBuffer, num, num3, num2);
						num3 = num4;
						num2 = 1;
						continue;
					}
					num2++;
					if (num2 == 271)
					{
						num = WriteRleValueToBuffer(m_compressBuffer, num, num3, num2);
						num2 = 0;
					}
				}
			}
		}
		if (num2 > 0)
		{
			num = WriteRleValueToBuffer(m_compressBuffer, num, num3, num2);
		}
		MemoryStream memoryStream = new MemoryStream(buffer);
		using (DeflateStream deflateStream = new DeflateStream(memoryStream, CompressionLevel.Fastest, leaveOpen: true))
		{
			deflateStream.Write(m_compressBuffer, 0, num);
		}
		return (int)memoryStream.Position;
	}

	private void DecompressChunkData(TerrainChunk chunk, byte[] buffer, int size)
	{
		using (DeflateStream deflateStream = new DeflateStream(new MemoryStream(buffer, 0, size), CompressionMode.Decompress))
		{
			int num = 0;
			int num2;
			while ((num2 = deflateStream.Read(m_compressBuffer, num, m_compressBuffer.Length - num)) > 0)
			{
				num += num2;
				if (num == m_compressBuffer.Length)
				{
					throw new InvalidOperationException("Deflate buffer overflow.");
				}
			}
			size = num;
		}
		int num3 = 0;
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				byte b = m_compressBuffer[num3++];
				int value = Terrain.ReplaceTemperature(Terrain.ReplaceHumidity(0, b & 0xF), b >> 4);
				chunk.SetShaftValueFast(i, j, value);
			}
		}
		int num4 = 0;
		int num5 = 0;
		int num6 = 0;
		while (num3 < size)
		{
			num3 = ReadRleValueFromBuffer(m_compressBuffer, num3, out var value2, out var count);
			for (int k = 0; k < count; k++)
			{
				chunk.SetCellValueFast(num4, num5, num6, value2);
				num4++;
				if (num4 >= 16)
				{
					num4 = 0;
					num6++;
					if (num6 >= 16)
					{
						num6 = 0;
						num5++;
					}
				}
			}
		}
		if (num4 != 0 || num5 != 256 || num6 != 0)
		{
			throw new InvalidOperationException("Corrupt chunk data.");
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int ReadIntFromBuffer(byte[] buffer, int i)
	{
		return buffer[i] + (buffer[i + 1] << 8) + (buffer[i + 2] << 16) + (buffer[i + 3] << 24);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int ReadRleValueFromBuffer(byte[] buffer, int i, out int value, out int count)
	{
		int value2 = ReadIntFromBuffer(buffer, i);
		int num = Terrain.ExtractLight(value2);
		value = Terrain.ReplaceLight(value2, 0);
		if (num < 15)
		{
			count = num + 1;
			return i + 4;
		}
		count = buffer[i + 4] + 16;
		return i + 5;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void WriteIntToBuffer(byte[] buffer, int i, int data)
	{
		buffer[i] = (byte)data;
		buffer[i + 1] = (byte)(data >> 8);
		buffer[i + 2] = (byte)(data >> 16);
		buffer[i + 3] = (byte)(data >> 24);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int WriteRleValueToBuffer(byte[] buffer, int i, int value, int count)
	{
		if (count < 16)
		{
			int data = Terrain.ReplaceLight(value, count - 1);
			WriteIntToBuffer(buffer, i, data);
			return i + 4;
		}
		if (count <= 271)
		{
			int data2 = Terrain.ReplaceLight(value, 15);
			WriteIntToBuffer(buffer, i, data2);
			buffer[i + 4] = (byte)(count - 16);
			return i + 5;
		}
		throw new InvalidOperationException("Count too large.");
	}
}
