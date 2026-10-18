using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Engine;
using Engine.Serialization;
using TemplatesDatabase;

namespace Game;

public class SubsystemSaplingBlockBehavior : SubsystemBlockBehavior, IUpdateable
{
	public class SaplingData
	{
		public Point3 Point;

		public TreeType Type;

		public double MatureTime;
	}

	public SubsystemGameInfo m_subsystemGameInfo;

	public Dictionary<Point3, SaplingData> m_saplings = new Dictionary<Point3, SaplingData>();

	public Dictionary<Point3, SaplingData>.ValueCollection.Enumerator m_enumerator;

	public Random m_random = new Random();

	public StringBuilder m_stringBuilder = new StringBuilder();

	public override int[] HandledBlocks => new int[1] { 119 };

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
	{
		int cellContents = SubsystemTerrain.Terrain.GetCellContents(x, y - 1, z);
		if (BlocksManager.Blocks[cellContents].IsTransparent)
		{
			SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
		}
	}

	public override void OnBlockAdded(int value, int oldValue, int x, int y, int z)
	{
		float designIndex;
		if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative)
		{
			designIndex = m_random.Float(1f, 3f);
		}
		else
		{
			float design = Project.FindSubsystem<SubsystemTimeOfDay>(throwOnError: true).DayDuration;
			designIndex = m_random.Float(5f * design, 10f * design);
		}
		AddSapling(new SaplingData
		{
			Point = new Point3(x, y, z),
			Type = (TreeType)Terrain.ExtractData(value),
			MatureTime = m_subsystemGameInfo.TotalElapsedGameTime + (double)designIndex
		});
	}

	public override void OnBlockRemoved(int value, int newValue, int x, int y, int z)
	{
		RemoveSapling(new Point3(x, y, z));
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_enumerator = m_saplings.Values.GetEnumerator();
		ValuesDictionary designIndex = valuesDictionary.GetValue<ValuesDictionary>("Saplings", null);
		if (designIndex == null)
		{
			return;
		}
		foreach (object mainValue in designIndex.Values)
		{
			try
			{
				string num = (string)mainValue;
				SaplingData saplingData = LoadSaplingData(num);
				if (saplingData != null)
				{
					AddSapling(saplingData);
				}
			}
			catch (Exception)
			{
			}
		}
	}

	public override void Save(ValuesDictionary valuesDictionary)
	{
		ValuesDictionary designIndex = new ValuesDictionary();
		valuesDictionary.SetValue("Saplings", designIndex);
		int rotation = 0;
		foreach (SaplingData value in m_saplings.Values)
		{
			designIndex.SetValue(rotation++.ToString(CultureInfo.InvariantCulture), SaveSaplingData(value));
		}
	}

	public void Update(float dt)
	{
		for (int designIndex = 0; designIndex < 10; designIndex++)
		{
			if (!m_enumerator.MoveNext())
			{
				m_enumerator = m_saplings.Values.GetEnumerator();
				break;
			}
			MatureSapling(m_enumerator.Current);
		}
	}

	public SaplingData LoadSaplingData(string data)
	{
		string[] faceTextureSlot = data.Split(new string[1] { ";" }, StringSplitOptions.None);
		if (faceTextureSlot.Length != 3)
		{
			throw new InvalidOperationException("Invalid sapling data string.");
		}
		return new SaplingData
		{
			Point = HumanReadableConverter.ConvertFromString<Point3>(faceTextureSlot[0]),
			Type = HumanReadableConverter.ConvertFromString<TreeType>(faceTextureSlot[1]),
			MatureTime = HumanReadableConverter.ConvertFromString<double>(faceTextureSlot[2])
		};
	}

	public string SaveSaplingData(SaplingData saplingData)
	{
		m_stringBuilder.Length = 0;
		m_stringBuilder.Append(HumanReadableConverter.ConvertToString(saplingData.Point));
		m_stringBuilder.Append(';');
		m_stringBuilder.Append(HumanReadableConverter.ConvertToString(saplingData.Type));
		m_stringBuilder.Append(';');
		m_stringBuilder.Append(HumanReadableConverter.ConvertToString(saplingData.MatureTime));
		return m_stringBuilder.ToString();
	}

	public void MatureSapling(SaplingData saplingData)
	{
		if (m_subsystemGameInfo.TotalElapsedGameTime < saplingData.MatureTime)
		{
			return;
		}
		int data = saplingData.Point.X;
		int y = saplingData.Point.Y;
		int z = saplingData.Point.Z;
		TreeType treeType = saplingData.Type;
		if (m_random.Float(0f, 1f) < 0.01f)
		{
			switch (treeType)
			{
			case TreeType.Oak:
				treeType = TreeType.OldOak;
				break;
			case TreeType.Birch:
				treeType = TreeType.OldBirch;
				break;
			case TreeType.Spruce:
				treeType = TreeType.OldSpruce;
				break;
			case TreeType.TallSpruce:
				treeType = TreeType.OldTallSpruce;
				break;
			case TreeType.Mimosa:
				treeType = TreeType.OldMimosa;
				break;
			}
		}
		TerrainChunk chunkAtCell = SubsystemTerrain.Terrain.GetChunkAtCell(data - 6, z - 6);
		TerrainChunk chunkAtCell2 = SubsystemTerrain.Terrain.GetChunkAtCell(data - 6, z + 6);
		TerrainChunk chunkAtCell3 = SubsystemTerrain.Terrain.GetChunkAtCell(data + 6, z - 6);
		TerrainChunk chunkAtCell4 = SubsystemTerrain.Terrain.GetChunkAtCell(data + 6, z + 6);
		if (chunkAtCell != null && chunkAtCell.State == TerrainChunkState.Valid && chunkAtCell2 != null && chunkAtCell2.State == TerrainChunkState.Valid && chunkAtCell3 != null && chunkAtCell3.State == TerrainChunkState.Valid && chunkAtCell4 != null && chunkAtCell4.State == TerrainChunkState.Valid)
		{
			int cellContents = SubsystemTerrain.Terrain.GetCellContents(data, y - 1, z);
			if (cellContents != 2 && cellContents != 8)
			{
				SubsystemTerrain.ChangeCell(data, y, z, Terrain.MakeBlockValue(28, 0, 0));
			}
			else if (SubsystemTerrain.Terrain.GetCellLight(data, y + 1, z) >= 9)
			{
				bool flag = false;
				for (int i = data - 1; i <= data + 1; i++)
				{
					for (int j = z - 1; j <= z + 1; j++)
					{
						int cellContents2 = SubsystemTerrain.Terrain.GetCellContents(i, y - 1, j);
						if (BlocksManager.Blocks[cellContents2] is FocalorsWaterBlock)
						{
							flag = true;
							break;
						}
					}
				}
				float probability;
				if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative)
				{
					probability = 1f;
				}
				else
				{
					int num = SubsystemTerrain.Terrain.GetTemperature(data, z) + SubsystemWeather.GetTemperatureAdjustmentAtHeight(y);
					int num2 = SubsystemTerrain.Terrain.GetHumidity(data, z);
					if (flag)
					{
						num = (num + 10) / 2;
						num2 = MathUtils.Max(num2, 12);
					}
					probability = 2f * PlantsManager.CalculateTreeProbability(treeType, num, num2, y);
				}
				if (!m_random.Bool(probability))
				{
					SubsystemTerrain.ChangeCell(data, y, z, Terrain.MakeBlockValue(28, 0, 0));
					return;
				}
				SubsystemTerrain.ChangeCell(data, y, z, Terrain.MakeBlockValue(0, 0, 0));
				if (!GrowTree(data, y, z, treeType))
				{
					SubsystemTerrain.ChangeCell(data, y, z, Terrain.MakeBlockValue(28, 0, 0));
				}
			}
			else if (m_subsystemGameInfo.TotalElapsedGameTime > saplingData.MatureTime + 1200.0)
			{
				SubsystemTerrain.ChangeCell(data, y, z, Terrain.MakeBlockValue(28, 0, 0));
			}
		}
		else
		{
			saplingData.MatureTime = m_subsystemGameInfo.TotalElapsedGameTime;
		}
	}

	public bool GrowTree(int x, int y, int z, TreeType treeType)
	{
		ReadOnlyList<TerrainBrush> designIndex = PlantsManager.GetTreeBrushes(treeType);
		for (int design = 0; design < 20; design++)
		{
			TerrainBrush num = designIndex[m_random.Int(0, designIndex.Count - 1)];
			bool flag = true;
			TerrainBrush.Cell[] cells = num.Cells;
			for (int i = 0; i < cells.Length; i++)
			{
				TerrainBrush.Cell cell = cells[i];
				if (cell.Y >= 0 && (cell.X != 0 || cell.Y != 0 || cell.Z != 0))
				{
					int cellContents = SubsystemTerrain.Terrain.GetCellContents(cell.X + x, cell.Y + y, cell.Z + z);
					if (cellContents != 0 && !(BlocksManager.Blocks[cellContents] is LeavesBlock))
					{
						flag = false;
						break;
					}
				}
			}
			if (flag)
			{
				num.Paint(SubsystemTerrain, x, y, z);
				return true;
			}
		}
		return false;
	}

	public void AddSapling(SaplingData saplingData)
	{
		m_saplings[saplingData.Point] = saplingData;
		m_enumerator = m_saplings.Values.GetEnumerator();
	}

	public void RemoveSapling(Point3 point)
	{
		m_saplings.Remove(point);
		m_enumerator = m_saplings.Values.GetEnumerator();
	}
}
