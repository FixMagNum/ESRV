using Engine;

namespace Game.NetWork;

public class TerrainSerializerNet : TerrainSerializer23
{
	private static readonly string path = Storage.CombinePaths("app:/", "NetChunks23");

	public TerrainSerializerNet()
	{
		if (Storage.DirectoryExists(path))
		{
			Delete(path);
		}
		else
		{
			Storage.CreateDirectory(path);
		}
		m_storage = new RegionFileStorage(this);
		m_storage.Open(path, "");
	}

	public override void Dispose()
	{
		base.Dispose();
		if (Storage.DirectoryExists(path))
		{
			Delete(path);
		}
	}

	public void Delete(string path)
	{
		foreach (string item in Storage.ListDirectoryNames(path))
		{
			string text = Storage.CombinePaths(path, item);
			Delete(text);
		}
		foreach (string item2 in Storage.ListFileNames(path))
		{
			Storage.DeleteFile(Storage.CombinePaths(path, item2));
		}
	}
}
