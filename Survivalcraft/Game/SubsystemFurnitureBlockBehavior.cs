using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Engine;
using Engine.Serialization;
using Game.NetWork;
using Game.NetWork.Packages;
using TemplatesDatabase;

namespace Game;

public class SubsystemFurnitureBlockBehavior : SubsystemBlockBehavior
{
	public const int MaxFurnitureSetNameLength = 64;

	public SubsystemAudio m_subsystemAudio;

	public SubsystemSoundMaterials m_subsystemSoundMaterials;

	public SubsystemItemsScanner m_subsystemItemsScanner;

	public SubsystemGameInfo m_subsystemGameInfo;

	public SubsystemPickables m_subsystemPickables;

	public SubsystemParticles m_subsystemParticles;

	public static string fName = "SubsystemFurnitureBlockBehavior";

	public List<FurnitureSet> m_furnitureSets = new List<FurnitureSet>();

	public FurnitureDesign[] m_furnitureDesigns = new FurnitureDesign[65535];

	public Dictionary<Point3, List<FireParticleSystem>> m_particleSystemsByCell = new Dictionary<Point3, List<FireParticleSystem>>();

	public override int[] HandledBlocks => new int[0];

	public ReadOnlyList<FurnitureSet> FurnitureSets => new ReadOnlyList<FurnitureSet>(m_furnitureSets);

	public FurnitureDesign GetDesign(int index)
	{
		if (index < 0 || index >= m_furnitureDesigns.Length)
		{
			return null;
		}
		return m_furnitureDesigns[index];
	}

	public FurnitureDesign FindMatchingDesign(FurnitureDesign design)
	{
		for (int i = 0; i < m_furnitureDesigns.Length; i++)
		{
			if (m_furnitureDesigns[i] != null && m_furnitureDesigns[i].Compare(design))
			{
				return m_furnitureDesigns[i];
			}
		}
		return null;
	}

	public FurnitureDesign FindMatchingDesignChain(FurnitureDesign design)
	{
		FurnitureDesign furnitureDesign = FindMatchingDesign(design);
		if (furnitureDesign != null && design.CompareChain(furnitureDesign))
		{
			return furnitureDesign;
		}
		return null;
	}

	public FurnitureDesign TryAddDesign(FurnitureDesign design)
	{
		for (int i = 0; i < m_furnitureDesigns.Length; i++)
		{
			if (m_furnitureDesigns[i] != null && m_furnitureDesigns[i].Compare(design))
			{
				return m_furnitureDesigns[i];
			}
		}
		for (int j = 0; j < m_furnitureDesigns.Length; j++)
		{
			if (m_furnitureDesigns[j] == null)
			{
				AddDesign(j, design);
				return design;
			}
		}
		GarbageCollectDesigns();
		for (int k = 0; k < m_furnitureDesigns.Length; k++)
		{
			if (m_furnitureDesigns[k] == null)
			{
				AddDesign(k, design);
				return design;
			}
		}
		return null;
	}

	public FurnitureDesign TryAddDesignChain(FurnitureDesign design, bool garbageCollectIfNeeded)
	{
		FurnitureDesign furnitureDesign = FindMatchingDesignChain(design);
		if (furnitureDesign != null)
		{
			return furnitureDesign;
		}
		List<FurnitureDesign> list = design.ListChain();
		if (garbageCollectIfNeeded && m_furnitureDesigns.Count((FurnitureDesign d) => d == null) < list.Count)
		{
			GarbageCollectDesigns();
		}
		if (m_furnitureDesigns.Count((FurnitureDesign d) => d == null) < list.Count)
		{
			return null;
		}
		int num = 0;
		for (int num2 = 0; num2 < m_furnitureDesigns.Length; num2++)
		{
			if (num >= list.Count)
			{
				break;
			}
			if (m_furnitureDesigns[num2] == null)
			{
				AddDesign(num2, list[num]);
				num++;
			}
		}
		if (num != list.Count)
		{
			throw new InvalidOperationException("public error.");
		}
		return design;
	}

	public void ScanDesign(CellFace start, Vector3 direction, ComponentMiner componentMiner)
	{
		FurnitureDesign design = null;
		FurnitureDesign num = null;
		Dictionary<Point3, int> valuesDictionary = new Dictionary<Point3, int>();
		Point3 point = start.Point;
		Point3 point2 = start.Point;
		int startValue = SubsystemTerrain.Terrain.GetCellValue(start.Point.X, start.Point.Y, start.Point.Z);
		int num2 = Terrain.ExtractContents(startValue);
		if (BlocksManager.Blocks[num2] is FurnitureBlock2)
		{
			int designIndex = FurnitureBlock2.GetDesignIndex(Terrain.ExtractData(startValue));
			num = GetDesign(designIndex);
			if (num == null)
			{
				componentMiner.ComponentPlayer?.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 0), Color.White, blinking: true, playNotificationSound: false);
				return;
			}
			design = num.Clone();
			design.LinkedDesign = null;
			design.InteractionMode = FurnitureInteractionMode.None;
			valuesDictionary.Add(start.Point, startValue);
		}
		else
		{
			Stack<Point3> stack = new Stack<Point3>();
			stack.Push(start.Point);
			while (stack.Count > 0)
			{
				Point3 key = stack.Pop();
				if (valuesDictionary.ContainsKey(key))
				{
					continue;
				}
				int cellValue = SubsystemTerrain.Terrain.GetCellValue(key.X, key.Y, key.Z);
				if (IsValueDisallowed(cellValue))
				{
					componentMiner.ComponentPlayer?.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 1), Color.White, blinking: true, playNotificationSound: false);
					return;
				}
				if (IsValueAllowed(cellValue))
				{
					if (key.X < point.X)
					{
						point.X = key.X;
					}
					if (key.Y < point.Y)
					{
						point.Y = key.Y;
					}
					if (key.Z < point.Z)
					{
						point.Z = key.Z;
					}
					if (key.X > point2.X)
					{
						point2.X = key.X;
					}
					if (key.Y > point2.Y)
					{
						point2.Y = key.Y;
					}
					if (key.Z > point2.Z)
					{
						point2.Z = key.Z;
					}
					if (MathUtils.Abs(point.X - point2.X) >= 16 || MathUtils.Abs(point.Y - point2.Y) >= 16 || MathUtils.Abs(point.Z - point2.Z) >= 16)
					{
						componentMiner.ComponentPlayer?.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 2), Color.White, blinking: true, playNotificationSound: false);
						return;
					}
					valuesDictionary[key] = cellValue;
					stack.Push(new Point3(key.X - 1, key.Y, key.Z));
					stack.Push(new Point3(key.X + 1, key.Y, key.Z));
					stack.Push(new Point3(key.X, key.Y - 1, key.Z));
					stack.Push(new Point3(key.X, key.Y + 1, key.Z));
					stack.Push(new Point3(key.X, key.Y, key.Z - 1));
					stack.Push(new Point3(key.X, key.Y, key.Z + 1));
				}
			}
			if (valuesDictionary.Count == 0)
			{
				componentMiner.ComponentPlayer?.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 0), Color.White, blinking: true, playNotificationSound: false);
				return;
			}
			design = new FurnitureDesign(SubsystemTerrain);
			Point3 point3 = point2 - point;
			int num3 = MathUtils.Max(MathUtils.Max(point3.X, point3.Y, point3.Z) + 1, 2);
			int[] array = new int[num3 * num3 * num3];
			foreach (KeyValuePair<Point3, int> item in valuesDictionary)
			{
				Point3 point4 = item.Key - point;
				array[point4.X + point4.Y * num3 + point4.Z * num3 * num3] = item.Value;
			}
			design.SetValues(num3, array);
			int steps = ((start.Face > 3) ? CellFace.Vector3ToFace(direction, 3) : CellFace.OppositeFace(start.Face));
			design.Rotate(1, steps);
			Point3 location = design.Box.Location;
			Point3 point5 = new Point3(design.Resolution) - (design.Box.Location + design.Box.Size);
			Point3 delta = new Point3((point5.X - location.X) / 2, -location.Y, (point5.Z - location.Z) / 2);
			design.Shift(delta);
		}
		BuildFurnitureDialog dialog = new BuildFurnitureDialog(design, num, (bool result) =>
		{
			if (result)
			{
				if (CommonLib.WorkType != WorkType.Client)
				{
					CreateDesign(componentMiner, design, valuesDictionary, start, startValue);
				}
				CommonLib.Net.QueuePackage(new FurniturePackage(design, valuesDictionary, start, startValue, CommonLib.WorkType == WorkType.Client));
			}
		});
		if (componentMiner.ComponentPlayer != null)
		{
			DialogsManager.ShowDialog(componentMiner.ComponentPlayer.GuiWidget, dialog);
		}
	}

	public FurnitureDesign CreateDesign(ComponentMiner componentMiner, FurnitureDesign design, Dictionary<Point3, int> valuesDictionary, CellFace start, int startValue, bool tryAdd = true)
	{
		if (tryAdd)
		{
			design = TryAddDesign(design);
		}
		else
		{
			AddDesign(design.Index, design);
		}
		if (design == null)
		{
			componentMiner.ComponentPlayer?.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 3), Color.White, blinking: true, playNotificationSound: false);
		}
		else
		{
			if (m_subsystemGameInfo.WorldSettings.GameMode != GameMode.Creative)
			{
				foreach (KeyValuePair<Point3, int> item in valuesDictionary)
				{
					SubsystemTerrain.DestroyCell(0, item.Key.X, item.Key.Y, item.Key.Z, 0, noDrop: true, noParticleSystem: true);
				}
			}
			int num = AddPickable(componentMiner.ComponentPlayer, design);
			Action action = () =>
			{
				m_subsystemSoundMaterials.PlayImpactSound(startValue, new Vector3(start.Point), 1f);
			};
			for (int num2 = 0; num2 < 3; num2++)
			{
				Time.QueueTimeDelayedExecution(Time.FrameStartTime + (double)((float)num2 * 0.25f), action);
			}
			if (componentMiner.ComponentCreature.PlayerStats != null)
			{
				componentMiner.ComponentCreature.PlayerStats.FurnitureItemsMade += num;
			}
		}
		return design;
	}

	public void SwitchToNextState(int x, int y, int z, bool playSound)
	{
		HashSet<Point3> hashSet = new HashSet<Point3>();
		List<Point3> list = new List<Point3>
		{
			new Point3(x, y, z)
		};
		int num = 0;
		while (num < list.Count && num < 4096)
		{
			Point3 item = list[num++];
			if (!hashSet.Add(item))
			{
				continue;
			}
			int cellValue = SubsystemTerrain.Terrain.GetCellValue(item.X, item.Y, item.Z);
			if (Terrain.ExtractContents(cellValue) != 366)
			{
				continue;
			}
			int data = Terrain.ExtractData(cellValue);
			int designIndex = FurnitureBlock2.GetDesignIndex(data);
			FurnitureDesign design = GetDesign(designIndex);
			if (design != null && design.LinkedDesign != null && design.LinkedDesign.Index >= 0 && (list.Count == 1 || design.InteractionMode == FurnitureInteractionMode.ConnectedMultistate))
			{
				int data2 = FurnitureBlock2.SetDesignIndex(data, design.LinkedDesign.Index, design.LinkedDesign.ShadowStrengthFactor, design.LinkedDesign.IsLightEmitter);
				int value = Terrain.ReplaceData(cellValue, data2);
				SubsystemTerrain.ChangeCell(item.X, item.Y, item.Z, value);
				if (design.InteractionMode == FurnitureInteractionMode.ConnectedMultistate)
				{
					list.Add(new Point3(item.X - 1, item.Y, item.Z));
					list.Add(new Point3(item.X + 1, item.Y, item.Z));
					list.Add(new Point3(item.X, item.Y - 1, item.Z));
					list.Add(new Point3(item.X, item.Y + 1, item.Z));
					list.Add(new Point3(item.X, item.Y, item.Z - 1));
					list.Add(new Point3(item.X, item.Y, item.Z + 1));
				}
			}
		}
		if (playSound)
		{
			m_subsystemAudio.PlaySound("Audio/BlockPlaced", 1f, 0f, new Vector3(x, y, z), 2f, autoDelay: true);
		}
	}

	public void GarbageCollectDesigns()
	{
		GarbageCollectDesigns(m_subsystemItemsScanner.ScanItems());
	}

	public FurnitureSet NewFurnitureSet(string name, string importedFrom)
	{
		if (name.Length > 64)
		{
			name = name.Substring(0, 64);
		}
		int voltage = 0;
		while (FurnitureSets.FirstOrDefault((FurnitureSet fs) => fs.Name == name) != null)
		{
			voltage++;
			name = ((voltage > 0) ? (name + voltage.ToString(CultureInfo.InvariantCulture)) : name);
		}
		FurnitureSet cellFace = new FurnitureSet
		{
			Name = name,
			ImportedFrom = importedFrom
		};
		m_furnitureSets.Add(cellFace);
		return cellFace;
	}

	public void DeleteFurnitureSet(FurnitureSet furnitureSet)
	{
		foreach (FurnitureDesign furnitureSetDesign in GetFurnitureSetDesigns(furnitureSet))
		{
			furnitureSetDesign.FurnitureSet = null;
		}
		m_furnitureSets.Remove(furnitureSet);
	}

	public void MoveFurnitureSet(FurnitureSet furnitureSet, int move)
	{
		int num = m_furnitureSets.IndexOf(furnitureSet);
		if (num >= 0)
		{
			m_furnitureSets.RemoveAt(num);
			m_furnitureSets.Insert(MathUtils.Clamp(num + move, 0, m_furnitureSets.Count), furnitureSet);
		}
	}

	public void AddToFurnitureSet(FurnitureDesign design, FurnitureSet furnitureSet)
	{
		foreach (FurnitureDesign item in design.ListChain())
		{
			item.FurnitureSet = furnitureSet;
		}
	}

	public IEnumerable<FurnitureDesign> GetFurnitureSetDesigns(FurnitureSet furnitureSet)
	{
		return m_furnitureDesigns.Where((FurnitureDesign fd) => fd != null && fd.FurnitureSet == furnitureSet);
	}

	public static List<FurnitureDesign> LoadFurnitureDesigns(SubsystemTerrain subsystemTerrain, ValuesDictionary valuesDictionary)
	{
		List<FurnitureDesign> list = new List<FurnitureDesign>();
		foreach (KeyValuePair<string, object> current in valuesDictionary)
		{
			int index = int.Parse(current.Key, CultureInfo.InvariantCulture);
			ValuesDictionary valuesDictionary2 = (ValuesDictionary)current.Value;
			FurnitureDesign item = new FurnitureDesign(index, subsystemTerrain, valuesDictionary2);
			list.Add(item);
		}
		foreach (FurnitureDesign design in list)
		{
			if (design.m_loadTimeLinkedDesignIndex >= 0)
			{
				design.LinkedDesign = list.FirstOrDefault((FurnitureDesign d) => d.Index == design.m_loadTimeLinkedDesignIndex);
			}
		}
		return list;
	}

	public static void SaveFurnitureDesigns(ValuesDictionary valuesDictionary, ICollection<FurnitureDesign> designs)
	{
		foreach (FurnitureDesign num in designs)
		{
			valuesDictionary.SetValue(num.Index.ToString(CultureInfo.InvariantCulture), num.Save());
		}
	}

	public override void OnBlockAdded(int value, int oldValue, int x, int y, int z)
	{
		AddTerrainFurniture(value);
		AddParticleSystems(value, x, y, z);
	}

	public override void OnBlockRemoved(int value, int newValue, int x, int y, int z)
	{
		RemoveTerrainFurniture(value);
		RemoveParticleSystems(x, y, z);
	}

	public override void OnBlockModified(int value, int oldValue, int x, int y, int z)
	{
		RemoveTerrainFurniture(oldValue);
		RemoveParticleSystems(x, y, z);
		AddTerrainFurniture(value);
		AddParticleSystems(value, x, y, z);
	}

	public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded)
	{
		if (!isLoaded)
		{
			AddTerrainFurniture(value);
		}
		AddParticleSystems(value, x, y, z);
	}

	public override void OnChunkDiscarding(TerrainChunk chunk)
	{
		List<Point3> list = new List<Point3>();
		foreach (Point3 key in m_particleSystemsByCell.Keys)
		{
			if (key.X >= chunk.Origin.X && key.X < chunk.Origin.X + 16 && key.Z >= chunk.Origin.Y && key.Z < chunk.Origin.Y + 16)
			{
				list.Add(key);
			}
		}
		foreach (Point3 item in list)
		{
			RemoveParticleSystems(item.X, item.Y, item.Z);
		}
	}

	public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner)
	{
		int cellValue = SubsystemTerrain.Terrain.GetCellValue(raycastResult.CellFace.X, raycastResult.CellFace.Y, raycastResult.CellFace.Z);
		if (Terrain.ExtractContents(cellValue) == 366)
		{
			int designIndex = FurnitureBlock2.GetDesignIndex(Terrain.ExtractData(cellValue));
			FurnitureDesign design = GetDesign(designIndex);
			if (design != null && (design.InteractionMode == FurnitureInteractionMode.Multistate || design.InteractionMode == FurnitureInteractionMode.ConnectedMultistate))
			{
				SwitchToNextState(raycastResult.CellFace.X, raycastResult.CellFace.Y, raycastResult.CellFace.Z, playSound: true);
				return true;
			}
		}
		return false;
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemSoundMaterials = Project.FindSubsystem<SubsystemSoundMaterials>(throwOnError: true);
		m_subsystemItemsScanner = Project.FindSubsystem<SubsystemItemsScanner>(throwOnError: true);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(throwOnError: true);
		m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		ValuesDictionary value = valuesDictionary.GetValue<ValuesDictionary>("FurnitureDesigns");
		foreach (FurnitureDesign item in LoadFurnitureDesigns(SubsystemTerrain, value))
		{
			m_furnitureDesigns[item.Index] = item;
		}
		foreach (ValuesDictionary item2 in valuesDictionary.GetValue<ValuesDictionary>("FurnitureSets").Values.Where((object v) => v is ValuesDictionary))
		{
			string value2 = item2.GetValue<string>("Name");
			string value3 = item2.GetValue<string>("ImportedFrom", null);
			string value4 = item2.GetValue<string>("Indices");
			int[] array = HumanReadableConverter.ValuesListFromString<int>(';', value4);
			FurnitureSet furnitureSet = new FurnitureSet
			{
				Name = value2,
				ImportedFrom = value3
			};
			m_furnitureSets.Add(furnitureSet);
			int[] array2 = array;
			foreach (int num2 in array2)
			{
				if (num2 >= 0 && num2 < m_furnitureDesigns.Length && m_furnitureDesigns[num2] != null)
				{
					m_furnitureDesigns[num2].FurnitureSet = furnitureSet;
				}
			}
		}
		m_subsystemItemsScanner.ItemsScanned += GarbageCollectDesigns;
	}

	public override void Save(ValuesDictionary valuesDictionary)
	{
		base.Save(valuesDictionary);
		GarbageCollectDesigns();
		ValuesDictionary dragTargetWidget = new ValuesDictionary();
		valuesDictionary.SetValue("FurnitureDesigns", dragTargetWidget);
		SaveFurnitureDesigns(dragTargetWidget, m_furnitureDesigns.Where((FurnitureDesign d) => d != null).ToArray());
		ValuesDictionary valuesDictionary2 = new ValuesDictionary();
		valuesDictionary.SetValue("FurnitureSets", valuesDictionary2);
		int num = 0;
		foreach (FurnitureSet furnitureSet in FurnitureSets)
		{
			ValuesDictionary valuesDictionary3 = new ValuesDictionary();
			valuesDictionary2.SetValue(num.ToString(CultureInfo.InvariantCulture), valuesDictionary3);
			valuesDictionary3.SetValue("Name", furnitureSet.Name);
			if (furnitureSet.ImportedFrom != null)
			{
				valuesDictionary3.SetValue("ImportedFrom", furnitureSet.ImportedFrom);
			}
			string value = HumanReadableConverter.ValuesListToString(';', (from d in GetFurnitureSetDesigns(furnitureSet)
				select d.Index).ToArray());
			valuesDictionary3.SetValue("Indices", value);
			num++;
		}
	}

	public void AddDesign(int index, FurnitureDesign design)
	{
		m_furnitureDesigns[index] = design;
		design.Index = index;
		design.m_terrainUseCount = 0;
	}

	public void AddTerrainFurniture(int value)
	{
		if (Terrain.ExtractContents(value) == 366)
		{
			int designIndex = FurnitureBlock2.GetDesignIndex(Terrain.ExtractData(value));
			if (designIndex < m_furnitureDesigns.Length && m_furnitureDesigns[designIndex] != null)
			{
				m_furnitureDesigns[designIndex].m_terrainUseCount++;
			}
		}
	}

	public void RemoveTerrainFurniture(int value)
	{
		if (Terrain.ExtractContents(value) == 366)
		{
			int designIndex = FurnitureBlock2.GetDesignIndex(Terrain.ExtractData(value));
			if (designIndex < m_furnitureDesigns.Length && m_furnitureDesigns[designIndex] != null)
			{
				m_furnitureDesigns[designIndex].m_terrainUseCount = MathUtils.Max(m_furnitureDesigns[designIndex].m_terrainUseCount - 1, 0);
			}
		}
	}

	public void GarbageCollectDesigns(ReadOnlyList<ScannedItemData> allExistingItems)
	{
		if (CommonLib.WorkType == WorkType.Client)
		{
			return;
		}
		for (int i = 0; i < m_furnitureDesigns.Length; i++)
		{
			if (m_furnitureDesigns[i] != null)
			{
				m_furnitureDesigns[i].m_gcUsed = m_furnitureDesigns[i].m_terrainUseCount > 0;
			}
		}
		foreach (ScannedItemData item in allExistingItems)
		{
			if (Terrain.ExtractContents(item.Value) == 366)
			{
				int designIndex = FurnitureBlock2.GetDesignIndex(Terrain.ExtractData(item.Value));
				FurnitureDesign design = GetDesign(designIndex);
				if (design != null)
				{
					design.m_gcUsed = true;
				}
			}
		}
		for (int j = 0; j < m_furnitureDesigns.Length; j++)
		{
			if (m_furnitureDesigns[j] != null && m_furnitureDesigns[j].m_gcUsed)
			{
				FurnitureDesign linkedDesign = m_furnitureDesigns[j].LinkedDesign;
				while (linkedDesign != null && !linkedDesign.m_gcUsed)
				{
					linkedDesign.m_gcUsed = true;
					linkedDesign = linkedDesign.LinkedDesign;
				}
			}
		}
		List<int> list = new List<int>();
		for (int k = 0; k < m_furnitureDesigns.Length; k++)
		{
			if (m_furnitureDesigns[k] != null && !m_furnitureDesigns[k].m_gcUsed && m_furnitureDesigns[k].FurnitureSet == null)
			{
				FurnitureDesign furnitureDesign = m_furnitureDesigns[k];
				list.Add(furnitureDesign.Index);
				furnitureDesign.Index = -1;
				m_furnitureDesigns[k] = null;
			}
		}
		if (CommonLib.WorkType == WorkType.Server)
		{
			CommonLib.Net.QueuePackage(new FurniturePackage(list));
		}
	}

	public static bool IsValueAllowed(int value)
	{
		return Array.IndexOf(new int[31]
		{
			3, 4, 5, 7, 9, 10, 11, 15, 17, 18,
			21, 26, 31, 46, 47, 66, 67, 68, 71, 72,
			73, 92, 126, 150, 208, 231, 255, 307, 359, 365,
			388
		}, Terrain.ExtractContents(value)) >= 0;
	}

	public static bool IsValueDisallowed(int value)
	{
		int num = Terrain.ExtractContents(value);
		int data = Terrain.ExtractData(value);
		if ((num == 18 || num == 92) && FluidBlock.GetLevel(data) != 0)
		{
			return FluidBlock.GetIsTop(data);
		}
		return false;
	}

	public void AddParticleSystems(int value, int x, int y, int z)
	{
		if (Terrain.ExtractContents(value) != 366)
		{
			return;
		}
		int data = Terrain.ExtractData(value);
		int rotation = FurnitureBlock2.GetRotation(data);
		int designIndex = FurnitureBlock2.GetDesignIndex(data);
		FurnitureDesign design = GetDesign(designIndex);
		if (design == null)
		{
			return;
		}
		List<FireParticleSystem> list = new List<FireParticleSystem>();
		BoundingBox[] torchPoints = design.GetTorchPoints(rotation);
		if (torchPoints.Length != 0)
		{
			BoundingBox[] array = torchPoints;
			for (int i = 0; i < array.Length; i++)
			{
				BoundingBox boundingBox = array[i];
				if (SettingsManager.FireParticlesFromLights)
				{
					float num = (boundingBox.Size().X + boundingBox.Size().Y + boundingBox.Size().Z) / 3f;
					float size = MathUtils.Clamp(1.5f * num, 0.1f, 1f);
					FireParticleSystem fireParticleSystem = new FireParticleSystem(new Vector3(x, y, z) + boundingBox.Center(), size, 24f);
					m_subsystemParticles.AddParticleSystem(fireParticleSystem);
					list.Add(fireParticleSystem);
				}
			}
		}
		if (list.Count > 0)
		{
			m_particleSystemsByCell[new Point3(x, y, z)] = list;
		}
	}

	public void RemoveParticleSystems(int x, int y, int z)
	{
		if (!m_particleSystemsByCell.TryGetValue(new Point3(x, y, z), out var value))
		{
			return;
		}
		foreach (FireParticleSystem item in value)
		{
			item.IsStopped = true;
		}
		m_particleSystemsByCell.Remove(new Point3(x, y, z));
	}

	public int AddPickable(ComponentPlayer player, FurnitureDesign design, bool isNonCreativeCopy = false)
	{
		ComponentMiner componentMiner = player.ComponentMiner;
		int value = Terrain.MakeBlockValue(366, 0, FurnitureBlock2.SetDesignIndex(0, design.Index, design.ShadowStrengthFactor, design.IsLightEmitter));
		int num = MathUtils.Clamp(design.Resolution, 4, 8) - (isNonCreativeCopy ? 1 : 0);
		Matrix matrix = componentMiner.ComponentCreature.ComponentBody.Matrix;
		Vector3 position = matrix.Translation + 1f * matrix.Forward + 1f * Vector3.UnitY;
		m_subsystemPickables.AddPickable(value, num, position, null, null);
		componentMiner.DamageActiveTool(1);
		componentMiner.Poke(forceRestart: false);
		return num;
	}
}
