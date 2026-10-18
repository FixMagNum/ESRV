using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Hjg.Pngcs.Chunks;

internal abstract class PngChunk
{
	public enum ChunkOrderingConstraint
	{
		NONE,
		BEFORE_PLTE_AND_IDAT,
		AFTER_PLTE_BEFORE_IDAT,
		BEFORE_IDAT,
		NA
	}

	public readonly string Id;

	public readonly bool Crit;

	public readonly bool Pub;

	public readonly bool Safe;

	protected readonly ImageInfo ImgInfo;

	private static Dictionary<string, Type> factoryMap = initFactory();

	public bool Priority { get; set; }

	public int ChunkGroup { get; set; }

	public int Length { get; set; }

	public long Offset { get; set; }

	protected PngChunk(string id, ImageInfo imgInfo)
	{
		Id = id;
		ImgInfo = imgInfo;
		Crit = ChunkHelper.IsCritical(id);
		Pub = ChunkHelper.IsPublic(id);
		Safe = ChunkHelper.IsSafeToCopy(id);
		Priority = false;
		ChunkGroup = -1;
		Length = -1;
		Offset = 0L;
	}

	private static Dictionary<string, Type> initFactory()
	{
		Dictionary<string, Type> dictionary = new Dictionary<string, Type>();
		dictionary.Add("IDAT", typeof(PngChunkIDAT));
		dictionary.Add("IHDR", typeof(PngChunkIHDR));
		dictionary.Add("PLTE", typeof(PngChunkPLTE));
		dictionary.Add("IEND", typeof(PngChunkIEND));
		dictionary.Add("tEXt", typeof(PngChunkTEXT));
		dictionary.Add("iTXt", typeof(PngChunkITXT));
		dictionary.Add("zTXt", typeof(PngChunkZTXT));
		dictionary.Add("bKGD", typeof(PngChunkBKGD));
		dictionary.Add("gAMA", typeof(PngChunkGAMA));
		dictionary.Add("pHYs", typeof(PngChunkPHYS));
		dictionary.Add("iCCP", typeof(PngChunkICCP));
		dictionary.Add("tIME", typeof(PngChunkTIME));
		dictionary.Add("tRNS", typeof(PngChunkTRNS));
		dictionary.Add("cHRM", typeof(PngChunkCHRM));
		dictionary.Add("sBIT", typeof(PngChunkSBIT));
		dictionary.Add("sRGB", typeof(PngChunkSRGB));
		dictionary.Add("hIST", typeof(PngChunkHIST));
		dictionary.Add("sPLT", typeof(PngChunkSPLT));
		dictionary.Add("oFFs", typeof(PngChunkOFFS));
		dictionary.Add("sTER", typeof(PngChunkSTER));
		return dictionary;
	}

	public static void FactoryRegister(string chunkId, Type type)
	{
		factoryMap.Add(chunkId, type);
	}

	internal static bool isKnown(string id)
	{
		return factoryMap.ContainsKey(id);
	}

	internal bool mustGoBeforePLTE()
	{
		return GetOrderingConstraint() == ChunkOrderingConstraint.BEFORE_PLTE_AND_IDAT;
	}

	internal bool mustGoBeforeIDAT()
	{
		ChunkOrderingConstraint orderingConstraint = GetOrderingConstraint();
		if (orderingConstraint != ChunkOrderingConstraint.BEFORE_IDAT && orderingConstraint != ChunkOrderingConstraint.BEFORE_PLTE_AND_IDAT)
		{
			return orderingConstraint == ChunkOrderingConstraint.AFTER_PLTE_BEFORE_IDAT;
		}
		return true;
	}

	internal bool mustGoAfterPLTE()
	{
		return GetOrderingConstraint() == ChunkOrderingConstraint.AFTER_PLTE_BEFORE_IDAT;
	}

	internal static PngChunk Factory(ChunkRaw chunk, ImageInfo info)
	{
		PngChunk pngChunk = FactoryFromId(ChunkHelper.ToString(chunk.IdBytes), info);
		pngChunk.Length = chunk.Length;
		pngChunk.ParseFromRaw(chunk);
		return pngChunk;
	}

	internal static PngChunk FactoryFromId(string cid, ImageInfo info)
	{
		PngChunk pngChunk = null;
		if (factoryMap == null)
		{
			initFactory();
		}
		if (isKnown(cid))
		{
			pngChunk = (PngChunk)factoryMap[cid].GetTypeInfo().DeclaredConstructors.First().Invoke(new object[1] { info });
		}
		if (pngChunk == null)
		{
			pngChunk = new PngChunkUNKNOWN(cid, info);
		}
		return pngChunk;
	}

	public ChunkRaw createEmptyChunk(int len, bool alloc)
	{
		return new ChunkRaw(len, ChunkHelper.ToBytes(Id), alloc);
	}

	public static T CloneChunk<T>(T chunk, ImageInfo info) where T : PngChunk
	{
		PngChunk pngChunk = FactoryFromId(chunk.Id, info);
		if ((object)pngChunk.GetType() != chunk.GetType())
		{
			throw new PngjException("bad class cloning chunk: " + pngChunk.GetType()?.ToString() + " " + chunk.GetType());
		}
		pngChunk.CloneDataFromRead(chunk);
		return (T)pngChunk;
	}

	internal void write(Stream os)
	{
		(CreateRawChunk() ?? throw new PngjException("null chunk ! creation failed for " + this)).WriteChunk(os);
	}

	public override string ToString()
	{
		return "chunk id= " + Id + " (len=" + Length + " off=" + Offset + ") c=" + GetType().Name;
	}

	public abstract ChunkRaw CreateRawChunk();

	public abstract void ParseFromRaw(ChunkRaw c);

	public abstract void CloneDataFromRead(PngChunk other);

	public abstract bool AllowsMultiple();

	public abstract ChunkOrderingConstraint GetOrderingConstraint();
}
