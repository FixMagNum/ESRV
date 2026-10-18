using System;
using System.Collections.Generic;
using System.Linq;
using Engine;
using Engine.Serialization;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemPlayers : Subsystem, IUpdateable
{
	private struct BanConnectionInfo
	{
		public string IpAddress;

		public string HardwareId;
	}

	public class KeptInventoryData
	{
		public int[] SlotValues;

		public int[] SlotCounts;

		public int ActiveSlotIndex;

		public Dictionary<int, List<int>> Clothes = new Dictionary<int, List<int>>();
	}

	public class PlayerSnapshot
	{
		public int[] InventoryValues;

		public int[] InventoryCounts;

		public float Level;

		public Vector3 Position;

		public Guid CurrentRegionGuid;
	}

	public SubsystemTime m_subsystemTime;

	public List<PlayerData> m_playersData;

	public List<PlayerData> m_offlinePlayers;

	public Dictionary<Guid, EntityData> m_offlinePlayerEntities;

	public List<ComponentPlayer> m_componentPlayers;

	public int m_nextPlayerIndex;

	private PlayerData m_mainPlayer;

	private Dictionary<int, PlayerData> usedIndies;

	public List<string> BlackPlayerGuidList;

	public List<string> NoMsgPlayerGuidList;

	public SubsystemTerrain SubsystemTerrain;

	private Dictionary<Point3, List<ComponentMiner>> m_blockMiningStates;

	public Dictionary<string, string> BlackPlayerNames;

	private Dictionary<Guid, BanConnectionInfo> m_lastConnections;

	public Dictionary<Guid, KeptInventoryData> m_keptInventories;

	private double m_lastDebugTime;

	private double m_lastSnapshotTime;

	public Dictionary<Guid, PlayerSnapshot> m_playerSnapshots = new Dictionary<Guid, PlayerSnapshot>();

	public Dictionary<string, string> NoMsgPlayerNames;

	public ReadOnlyList<PlayerData> PlayersData => new ReadOnlyList<PlayerData>(m_playersData);

	public IEnumerable<PlayerData> AllPlayersData => m_playersData.Concat(m_offlinePlayers);

	public ReadOnlyList<ComponentPlayer> ComponentPlayers => new ReadOnlyList<ComponentPlayer>(m_componentPlayers);

	public Vector3 GlobalSpawnPosition { get; set; }

	public UpdateOrder UpdateOrder => UpdateOrder.SubsystemPlayers;

	public PlayerData MainPlayerData
	{
		get
		{
			if (m_mainPlayer == null)
			{
				m_mainPlayer = PlayersData.FirstOrDefault((PlayerData p) => p.IsMainPlayer);
			}
			return m_mainPlayer;
		}
	}

	public ComponentPlayer MainPlayer => MainPlayerData?.ComponentPlayer;

	public event Action<PlayerData> PlayerAdded;

	public event Action<PlayerData> PlayerRemoved;

	public bool IsPlayer(Entity entity)
	{
		foreach (ComponentPlayer componentPlayer in m_componentPlayers)
		{
			if (entity == componentPlayer.Entity)
			{
				return true;
			}
		}
		return false;
	}

	public ComponentPlayer FindNearestPlayer(Vector3 position)
	{
		ComponentPlayer data = null;
		float num = float.MaxValue;
		foreach (ComponentPlayer componentPlayer in ComponentPlayers)
		{
			float num2 = Vector3.DistanceSquared(componentPlayer.ComponentBody.Position, position);
			if (num2 < num)
			{
				num = num2;
				data = componentPlayer;
			}
		}
		return data;
	}

	public void FindUnusedIndex(PlayerData playerData)
	{
		for (int data = 0; data < int.MaxValue; data++)
		{
			if (!usedIndies.ContainsKey(data))
			{
				usedIndies.Add(data, playerData);
				playerData.PlayerIndex = data;
				break;
			}
		}
	}

	public void AddPlayerData(PlayerData playerData)
	{
		DebugLog($"Вызван AddPlayerData для {playerData.Name} (GUID: {playerData.PlayerGUID})");
		m_offlinePlayers.RemoveAll((PlayerData p) => p.PlayerGUID == playerData.PlayerGUID);
		string unique;
		string text = (unique = playerData.Name);
		int num = 1;
		while (AllPlayersData.Any((PlayerData p) => string.Equals(p.Name, unique, StringComparison.OrdinalIgnoreCase)))
		{
			num++;
			unique = text + num;
		}
		if (!string.Equals(playerData.Name, unique, StringComparison.OrdinalIgnoreCase))
		{
			Log.Information("Auto-rename (duplicate) " + playerData.Name + " -> " + unique);
			playerData.Name = unique;
		}
		if (m_playersData.Contains(playerData) || m_playersData.Any((PlayerData pd) => pd.PlayerGUID == playerData.PlayerGUID))
		{
			DebugLog("ОШИБКА: Игрок " + playerData.Name + " уже есть в списке!");
			throw new InvalidOperationException("Player already added.");
		}
		m_playersData.Add(playerData);
		DumpPlayerList("После AddPlayerData");
		try
		{
			if (ProjectNet.subsystemGameInfo != null && ProjectNet.subsystemGameInfo.WorldSettings.RequirePassword)
			{
				PlayerPasswordRepository.Initialize(ProjectNet.subsystemGameInfo.WorldSettings);
				PlayerPasswordRepository.UpdateNick(playerData.PlayerGUID, playerData.Name);
			}
		}
		catch
		{
		}
		try
		{
			if (m_lastConnections.TryGetValue(playerData.PlayerGUID, out var value) && !string.IsNullOrEmpty(value.HardwareId))
			{
				if (!string.IsNullOrEmpty(playerData.CurrentHardwareId) && !string.Equals(playerData.CurrentHardwareId, value.HardwareId, StringComparison.OrdinalIgnoreCase))
				{
					playerData.PreviousHardwareId = playerData.CurrentHardwareId;
				}
				if (string.IsNullOrEmpty(playerData.CurrentHardwareId) || !string.Equals(playerData.CurrentHardwareId, value.HardwareId, StringComparison.OrdinalIgnoreCase))
				{
					playerData.CurrentHardwareId = value.HardwareId;
				}
			}
		}
		catch
		{
		}
		FindUnusedIndex(playerData);
		PlayerAdded?.Invoke(playerData);
		if (playerData.m_stateMachine.CurrentState == null)
		{
			playerData.m_stateMachine.TransitionTo("FirstUpdate");
		}
		if (CommonLib.WorkType == WorkType.Server)
		{
			string msg = LanguageControl.Get("SubsystemGameWidgets", "player_joined").Replace("{0}", playerData.Name);
			Project.FindSubsystem<SubsystemGameWidgets>().AddMessage(msg, null, 0);
		}
		SubsystemTerrain.TerrainUpdater.SetLastChunksUpdateCenter(-1, null);
	}

	public void RemovePlayerData(PlayerData playerData, bool disposeEntity = true)
	{
		if (!m_playersData.Contains(playerData))
		{
			throw new InvalidOperationException("Player does not exist.");
		}
		m_playersData.Remove(playerData);
		if (usedIndies.ContainsKey(playerData.PlayerIndex))
		{
			usedIndies.Remove(playerData.PlayerIndex);
		}
		if (playerData.ComponentPlayer != null)
		{
			try
			{
				Project.RemoveEntity(playerData.ComponentPlayer.Entity, disposeEntity);
			}
			catch
			{
			}
		}
		PlayerRemoved?.Invoke(playerData);
		if (m_playerSnapshots.ContainsKey(playerData.PlayerGUID))
		{
			m_playerSnapshots.Remove(playerData.PlayerGUID);
		}
		playerData.Dispose();
		SubsystemTerrain.TerrainUpdater.SetLastChunksUpdateCenter(-1, null);
	}

	public void Update(float dt)
	{
		UpdateBlockMining(dt);
		if (m_playersData.Count != 0)
		{
			m_playersData.All((PlayerData p) => !p.IsMainPlayer);
		}
		foreach (PlayerData playersDatum in m_playersData)
		{
			try
			{
				playersDatum.Update();
				if (playersDatum.ComponentPlayer == null || playersDatum.ComponentPlayer.Entity == null)
				{
					continue;
				}
				ComponentFlu componentFlu = playersDatum.ComponentPlayer.Entity.FindComponent<ComponentFlu>();
				if (componentFlu != null && componentFlu.IsCoughing && playersDatum.ComponentPlayer.ComponentBody != null)
				{
					Vector3 position = playersDatum.ComponentPlayer.ComponentBody.Position;
					foreach (PlayerData playersDatum2 in m_playersData)
					{
						if (playersDatum2 == playersDatum || playersDatum2.ComponentPlayer == null || playersDatum2.ComponentPlayer.ComponentBody == null)
						{
							continue;
						}
						Vector3 position2 = playersDatum2.ComponentPlayer.ComponentBody.Position;
						if (!(Vector3.Distance(position, position2) <= 2f) || playersDatum2.ComponentPlayer.Entity == null)
						{
							continue;
						}
						ComponentFlu componentFlu2 = playersDatum2.ComponentPlayer.Entity.FindComponent<ComponentFlu>();
						if (componentFlu2 != null && !componentFlu2.HasFlu)
						{
							componentFlu2.StartFlu();
							PlayerAchievements playerAchievements = playersDatum.ComponentPlayer.PlayerAchievements;
							if (playerAchievements != null && playerAchievements.Pandemic == 0L)
							{
								playerAchievements.Pandemic = 1L;
								playerAchievements.EnqueuePandemicAchievement();
							}
						}
					}
				}
				if (CommonLib.WorkType != WorkType.Server || playersDatum.ComponentPlayer == null)
				{
					continue;
				}
				ComponentMiner componentMiner = playersDatum.ComponentPlayer.Entity.FindComponent<ComponentMiner>();
				Vector3 position3 = playersDatum.ComponentPlayer.ComponentBody.Position;
				float level = playersDatum.Level;
				Guid currentRegion = Guid.Empty;
				if (SubsystemBedrockBlockBehavior.CheckIsInTerritoriy(Terrain.ToCell(position3.X), Terrain.ToCell(position3.Z), out var territoriy))
				{
					currentRegion = territoriy.OwnerGuid;
				}
				if (!m_playerSnapshots.ContainsKey(playersDatum.PlayerGUID))
				{
					PlayerSnapshot playerSnapshot = new PlayerSnapshot();
					playerSnapshot.Position = position3;
					playerSnapshot.Level = level;
					playerSnapshot.CurrentRegionGuid = currentRegion;
					if (componentMiner != null && componentMiner.Inventory != null)
					{
						int num = ((componentMiner.Inventory is ComponentCreativeInventory) ? Math.Min(25, componentMiner.Inventory.SlotsCount) : componentMiner.Inventory.SlotsCount);
						playerSnapshot.InventoryValues = new int[num];
						playerSnapshot.InventoryCounts = new int[num];
						for (int num2 = 0; num2 < num; num2++)
						{
							playerSnapshot.InventoryValues[num2] = componentMiner.Inventory.GetSlotValue(num2);
							playerSnapshot.InventoryCounts[num2] = componentMiner.Inventory.GetSlotCount(num2);
						}
						if (GameScreen.m_inventoryWriter != null)
						{
							GameScreen.m_inventoryWriter.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " [JOIN] Full Inventory of " + playersDatum.Name + ":");
							for (int num3 = 0; num3 < num; num3++)
							{
								if (playerSnapshot.InventoryCounts[num3] > 0)
								{
									int num4 = Terrain.ExtractContents(playerSnapshot.InventoryValues[num3]);
									string displayName = BlocksManager.Blocks[num4].GetDisplayName(SubsystemTerrain, playerSnapshot.InventoryValues[num3]);
									GameScreen.m_inventoryWriter.WriteLine($"  Slot {num3}: {displayName} (Id:{num4}) x{playerSnapshot.InventoryCounts[num3]}");
								}
							}
							GameScreen.m_inventoryWriter.Flush();
						}
					}
					m_playerSnapshots[playersDatum.PlayerGUID] = playerSnapshot;
					continue;
				}
				PlayerSnapshot playerSnapshot2 = m_playerSnapshots[playersDatum.PlayerGUID];
				if (Vector3.DistanceSquared(playerSnapshot2.Position, position3) > 10000f || playerSnapshot2.CurrentRegionGuid != currentRegion)
				{
					if (GameScreen.m_positionWriter != null)
					{
						int num5 = Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(Terrain.ToCell(position3.X), Terrain.ToCell(position3.Y - 0.1f), Terrain.ToCell(position3.Z)));
						string displayName2 = BlocksManager.Blocks[num5].GetDisplayName(SubsystemTerrain, num5);
						string text = " (Wilderness)";
						if (currentRegion != Guid.Empty)
						{
							if (currentRegion == playersDatum.PlayerGUID)
							{
								text = " (Owner: Self)";
							}
							else
							{
								PlayerData playerData = FindPlayerData((PlayerData p) => p.PlayerGUID == currentRegion);
								string text2 = ((playerData != null) ? playerData.Name : "Unknown");
								text = " (Owner: " + text2 + ")";
							}
						}
						string text3 = ((playerSnapshot2.CurrentRegionGuid != currentRegion) ? "changed region" : "moved >100 blocks");
						GameScreen.m_positionWriter.WriteLine(string.Format("{0} [{1}] {2}{3}. Pos: {4:F1}, {5:F1}, {6:F1}. Block below: {7}", DateTime.Now.ToString("HH:mm:ss"), playersDatum.Name, text3, text, position3.X, position3.Y, position3.Z, displayName2));
						GameScreen.m_positionWriter.Flush();
					}
					playerSnapshot2.Position = position3;
					playerSnapshot2.CurrentRegionGuid = currentRegion;
				}
				if (Math.Abs(level - playerSnapshot2.Level) >= 1f)
				{
					if (GameScreen.m_inventoryWriter != null)
					{
						GameScreen.m_inventoryWriter.WriteLine(string.Format("{0} [{1}] Level changed: {2:F1} -> {3:F1}", DateTime.Now.ToString("HH:mm:ss"), playersDatum.Name, playerSnapshot2.Level, level));
						GameScreen.m_inventoryWriter.Flush();
					}
					playerSnapshot2.Level = level;
				}
				if (!(Time.RealTime - m_lastSnapshotTime > 15.0) || componentMiner == null || componentMiner.Inventory == null)
				{
					continue;
				}
				int num6 = ((componentMiner.Inventory is ComponentCreativeInventory) ? Math.Min(25, componentMiner.Inventory.SlotsCount) : componentMiner.Inventory.SlotsCount);
				bool flag = false;
				for (int num7 = 0; num7 < num6; num7++)
				{
					int slotValue = componentMiner.Inventory.GetSlotValue(num7);
					int slotCount = componentMiner.Inventory.GetSlotCount(num7);
					if (playerSnapshot2.InventoryValues[num7] != slotValue || playerSnapshot2.InventoryCounts[num7] != slotCount)
					{
						if (!flag && GameScreen.m_inventoryWriter != null)
						{
							GameScreen.m_inventoryWriter.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " [INV_CHANGE] " + playersDatum.Name + ":");
							flag = true;
						}
						string text4 = ((playerSnapshot2.InventoryCounts[num7] > 0) ? BlocksManager.Blocks[Terrain.ExtractContents(playerSnapshot2.InventoryValues[num7])].GetDisplayName(SubsystemTerrain, playerSnapshot2.InventoryValues[num7]) : "Empty");
						string text5 = ((slotCount > 0) ? BlocksManager.Blocks[Terrain.ExtractContents(slotValue)].GetDisplayName(SubsystemTerrain, slotValue) : "Empty");
						if (GameScreen.m_inventoryWriter != null)
						{
							GameScreen.m_inventoryWriter.WriteLine($"  Slot {num7}: [{text4} x{playerSnapshot2.InventoryCounts[num7]}] -> [{text5} x{slotCount}]");
						}
						playerSnapshot2.InventoryValues[num7] = slotValue;
						playerSnapshot2.InventoryCounts[num7] = slotCount;
					}
				}
				if (flag && GameScreen.m_inventoryWriter != null)
				{
					GameScreen.m_inventoryWriter.Flush();
				}
			}
			catch (Exception)
			{
			}
		}
	}

	public override void Dispose()
	{
		foreach (PlayerData playersDatum in m_playersData)
		{
			playersDatum.Dispose();
		}
	}

	public void FindPlayerByClientId(byte id, Action<ComponentPlayer> action)
	{
		PlayerData playerData = m_playersData.Find((PlayerData p) => p.ClientId == id);
		if (playerData != null)
		{
			action(playerData.ComponentPlayer);
		}
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		SubsystemTerrain = Project.FindSubsystem<SubsystemTerrain>();
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_nextPlayerIndex = valuesDictionary.GetValue<int>("NextPlayerIndex");
		GlobalSpawnPosition = valuesDictionary.GetValue<Vector3>("GlobalSpawnPosition");
		foreach (string value3 in valuesDictionary.GetValue("BlackPlayerGuidList", new ValuesDictionary()).Values)
		{
			BlackPlayerGuidList.Add(value3);
		}
		foreach (string value4 in valuesDictionary.GetValue("NoMsgPlayerGuidList", new ValuesDictionary()).Values)
		{
			NoMsgPlayerGuidList.Add(value4);
		}
		foreach (KeyValuePair<string, object> item in valuesDictionary.GetValue("NoMsgPlayerNames", new ValuesDictionary()))
		{
			string key = item.Key;
			string text = item.Value as string;
			if (!string.IsNullOrEmpty(key) && text != null)
			{
				NoMsgPlayerNames[key] = text;
			}
		}
		foreach (KeyValuePair<string, object> item2 in valuesDictionary.GetValue("BlackPlayerNames", new ValuesDictionary()))
		{
			string key2 = item2.Key;
			string text2 = item2.Value as string;
			if (!string.IsNullOrEmpty(key2) && text2 != null)
			{
				BlackPlayerNames[key2] = text2;
				if (!BlackPlayerGuidList.Contains(key2))
				{
					BlackPlayerGuidList.Add(key2);
				}
			}
		}
		foreach (KeyValuePair<string, object> item3 in valuesDictionary.GetValue("KeptInventories", new ValuesDictionary()))
		{
			if (!Guid.TryParse(item3.Key, out var result))
			{
				continue;
			}
			ValuesDictionary valuesDictionary2 = (ValuesDictionary)item3.Value;
			KeptInventoryData keptInventoryData = new KeptInventoryData();
			keptInventoryData.ActiveSlotIndex = valuesDictionary2.GetValue<int>("ActiveSlot");
			keptInventoryData.SlotValues = StringToIntArray(valuesDictionary2.GetValue<string>("Values"));
			keptInventoryData.SlotCounts = StringToIntArray(valuesDictionary2.GetValue<string>("Counts"));
			if (valuesDictionary2.ContainsKey("Clothes"))
			{
				ValuesDictionary value = valuesDictionary2.GetValue<ValuesDictionary>("Clothes");
				foreach (string key4 in value.Keys)
				{
					int key3 = int.Parse(key4);
					keptInventoryData.Clothes[key3] = new List<int>(HumanReadableConverter.ValuesListFromString<int>(';', value.GetValue<string>(key4)));
				}
			}
			m_keptInventories[result] = keptInventoryData;
		}
		if (CommonLib.WorkType != WorkType.Client)
		{
			foreach (KeyValuePair<string, object> item4 in valuesDictionary.GetValue("OfflinePlayerEntities", new ValuesDictionary()))
			{
				if (Guid.TryParseExact(item4.Key, "N", out var result2))
				{
					EntityData value2 = new EntityData(Project.GameDatabase, (ValuesDictionary)item4.Value);
					m_offlinePlayerEntities.Add(result2, value2);
				}
			}
			{
				foreach (ValuesDictionary value5 in valuesDictionary.GetValue<ValuesDictionary>("Players").Values)
				{
					PlayerData playerData = new PlayerData(Project);
					playerData.Load(value5);
					if (AllPlayersData.Any((PlayerData p) => p.PlayerGUID == playerData.PlayerGUID))
					{
						playerData.Dispose();
						continue;
					}
					if (((CommonLib.WorkType != WorkType.Local && CommonLib.Net.GetClientByGUID(playerData.PlayerGUID) == null) || (CommonLib.WorkType == WorkType.Local && !playerData.IsMainPlayer)) && m_offlinePlayerEntities.ContainsKey(playerData.PlayerGUID))
					{
						m_offlinePlayers.Add(playerData);
						continue;
					}
					if (!usedIndies.ContainsKey(playerData.PlayerIndex))
					{
						usedIndies.Add(playerData.PlayerIndex, playerData);
					}
					else
					{
						FindUnusedIndex(playerData);
					}
					m_playersData.Add(playerData);
				}
				return;
			}
		}
		foreach (object value6 in valuesDictionary.GetValue<ValuesDictionary>("Players").Values)
		{
			ValuesDictionary valuesDictionary4 = (ValuesDictionary)value6;
			PlayerData playerData2 = new PlayerData(Project);
			playerData2.Load(valuesDictionary4);
			if (AllPlayersData.Any((PlayerData p) => p.PlayerGUID == playerData2.PlayerGUID))
			{
				playerData2.Dispose();
				continue;
			}
			if ((CommonLib.WorkType != WorkType.Local && CommonLib.Net.GetClientByGUID(playerData2.PlayerGUID) == null) || (CommonLib.WorkType == WorkType.Local && !playerData2.IsMainPlayer))
			{
				m_offlinePlayers.Add(playerData2);
				continue;
			}
			if (!usedIndies.ContainsKey(playerData2.PlayerIndex))
			{
				usedIndies.Add(playerData2.PlayerIndex, playerData2);
			}
			else
			{
				FindUnusedIndex(playerData2);
			}
			m_playersData.Add(playerData2);
		}
	}

	public override void Save(ValuesDictionary valuesDictionary)
	{
		ValuesDictionary valuesDictionary2 = new ValuesDictionary();
		ValuesDictionary valuesDictionary3 = new ValuesDictionary();
		for (int i = 0; i < BlackPlayerGuidList.Count; i++)
		{
			valuesDictionary3.SetValue(i.ToString(), BlackPlayerGuidList[i]);
		}
		valuesDictionary.SetValue("BlackPlayerGuidList", valuesDictionary3);
		ValuesDictionary valuesDictionary4 = new ValuesDictionary();
		for (int j = 0; j < NoMsgPlayerGuidList.Count; j++)
		{
			valuesDictionary4.SetValue(j.ToString(), NoMsgPlayerGuidList[j]);
		}
		valuesDictionary.SetValue("NoMsgPlayerGuidList", valuesDictionary4);
		ValuesDictionary valuesDictionary5 = new ValuesDictionary();
		foreach (KeyValuePair<string, string> blackPlayerName in BlackPlayerNames)
		{
			valuesDictionary5.SetValue(blackPlayerName.Key, blackPlayerName.Value);
		}
		valuesDictionary.SetValue("BlackPlayerNames", valuesDictionary5);
		ValuesDictionary valuesDictionary6 = new ValuesDictionary();
		foreach (KeyValuePair<string, string> noMsgPlayerName in NoMsgPlayerNames)
		{
			valuesDictionary6.SetValue(noMsgPlayerName.Key, noMsgPlayerName.Value);
		}
		valuesDictionary.SetValue("NoMsgPlayerNames", valuesDictionary6);
		valuesDictionary.SetValue("GlobalSpawnPosition", GlobalSpawnPosition);
		valuesDictionary.SetValue("Players", valuesDictionary2);
		ValuesDictionary value = new ValuesDictionary();
		valuesDictionary.SetValue("OfflinePlayerEntities", value);
		ValuesDictionary valuesDictionary7 = new ValuesDictionary();
		foreach (KeyValuePair<Guid, KeptInventoryData> keptInventory in m_keptInventories)
		{
			ValuesDictionary valuesDictionary8 = new ValuesDictionary();
			valuesDictionary8.SetValue("ActiveSlot", keptInventory.Value.ActiveSlotIndex);
			valuesDictionary8.SetValue("Values", IntArrayToString(keptInventory.Value.SlotValues));
			valuesDictionary8.SetValue("Counts", IntArrayToString(keptInventory.Value.SlotCounts));
			ValuesDictionary valuesDictionary9 = new ValuesDictionary();
			foreach (KeyValuePair<int, List<int>> item in keptInventory.Value.Clothes)
			{
				valuesDictionary9.SetValue(item.Key.ToString(), HumanReadableConverter.ValuesListToString(';', item.Value.ToArray()));
			}
			valuesDictionary8.SetValue("Clothes", valuesDictionary9);
			valuesDictionary7.SetValue(keptInventory.Key.ToString(), valuesDictionary8);
		}
		valuesDictionary.SetValue("KeptInventories", valuesDictionary7);
		int num = 0;
		foreach (PlayerData allPlayersDatum in AllPlayersData)
		{
			ValuesDictionary valuesDictionary10 = new ValuesDictionary();
			allPlayersDatum.Save(valuesDictionary10);
			valuesDictionary2.SetValue(num++.ToString(), valuesDictionary10);
		}
		ValuesDictionary valuesDictionary11 = new ValuesDictionary();
		valuesDictionary.SetValue("OfflinePlayerEntities", valuesDictionary11);
		if (Project is ProjectNet { SendToClientMode: not false })
		{
			return;
		}
		foreach (KeyValuePair<Guid, EntityData> offlinePlayerEntity in m_offlinePlayerEntities)
		{
			ValuesDictionary valuesDictionary12 = new ValuesDictionary();
			valuesDictionary11.SetValue(offlinePlayerEntity.Key.ToString("N"), valuesDictionary12);
			offlinePlayerEntity.Value.Save(valuesDictionary12);
		}
	}

	public override void OnEntityAdded(Entity entity)
	{
		foreach (PlayerData playersDatum in m_playersData)
		{
			playersDatum.OnEntityAdded(entity);
		}
		UpdateComponentPlayers();
		ComponentPlayer componentPlayer = entity.FindComponent<ComponentPlayer>();
		if (componentPlayer == null)
		{
			return;
		}
		Guid playerGUID = componentPlayer.PlayerData.PlayerGUID;
		if (!m_keptInventories.ContainsKey(playerGUID))
		{
			return;
		}
		KeptInventoryData keptInventoryData = m_keptInventories[playerGUID];
		ComponentMiner componentMiner = entity.FindComponent<ComponentMiner>();
		if (componentMiner != null && componentMiner.Inventory != null)
		{
			IInventory inventory = componentMiner.Inventory;
			for (int i = 0; i < inventory.SlotsCount; i++)
			{
				inventory.RemoveSlotItems(i, inventory.GetSlotCount(i));
			}
			for (int j = 0; j < inventory.SlotsCount && j < keptInventoryData.SlotValues.Length; j++)
			{
				if (keptInventoryData.SlotCounts[j] > 0)
				{
					inventory.AddSlotItems(j, keptInventoryData.SlotValues[j], keptInventoryData.SlotCounts[j]);
				}
			}
			componentMiner.Inventory.ActiveSlotIndex = keptInventoryData.ActiveSlotIndex;
		}
		ComponentClothing componentClothing = entity.FindComponent<ComponentClothing>();
		if (componentClothing != null && keptInventoryData.Clothes != null)
		{
			foreach (KeyValuePair<int, List<int>> item in keptInventoryData.Clothes)
			{
				componentClothing.SetClothes((ClothingSlot)item.Key, item.Value);
			}
		}
		m_keptInventories.Remove(playerGUID);
	}

	public override void OnEntityRemoved(Entity entity)
	{
		ComponentPlayer componentPlayer = entity.FindComponent<ComponentPlayer>();
		if (componentPlayer != null && componentPlayer.ComponentHealth.Health <= 0f && CommonLib.WorkType == WorkType.Server && GameScreen.m_inventoryWriter != null)
		{
			ComponentMiner componentMiner = componentPlayer.Entity.FindComponent<ComponentMiner>();
			if (componentMiner != null && componentMiner.Inventory != null)
			{
				int num = ((componentMiner.Inventory is ComponentCreativeInventory) ? Math.Min(25, componentMiner.Inventory.SlotsCount) : componentMiner.Inventory.SlotsCount);
				GameScreen.m_inventoryWriter.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " [DEATH] Full Inventory of " + componentPlayer.PlayerData.Name + ":");
				GameScreen.m_inventoryWriter.WriteLine("  Level at death: " + componentPlayer.PlayerData.Level.ToString("0.0"));
				for (int i = 0; i < num; i++)
				{
					if (componentMiner.Inventory.GetSlotCount(i) > 0)
					{
						int num2 = Terrain.ExtractContents(componentMiner.Inventory.GetSlotValue(i));
						string displayName = BlocksManager.Blocks[num2].GetDisplayName(componentPlayer.Project.FindSubsystem<SubsystemTerrain>(), componentMiner.Inventory.GetSlotValue(i));
						GameScreen.m_inventoryWriter.WriteLine($"  Slot {i}: {displayName} (Id:{num2}) x{componentMiner.Inventory.GetSlotCount(i)}");
					}
				}
				GameScreen.m_inventoryWriter.Flush();
			}
		}
		if (componentPlayer != null && componentPlayer.ComponentHealth.Health <= 0f && componentPlayer.Project.FindSubsystem<SubsystemGameInfo>().WorldSettings.KeepInventoryAfterDeath)
		{
			ComponentMiner componentMiner2 = entity.FindComponent<ComponentMiner>();
			if (componentMiner2 != null && componentMiner2.Inventory != null)
			{
				IInventory inventory = componentMiner2.Inventory;
				KeptInventoryData keptInventoryData = new KeptInventoryData();
				keptInventoryData.SlotValues = new int[inventory.SlotsCount];
				keptInventoryData.SlotCounts = new int[inventory.SlotsCount];
				keptInventoryData.ActiveSlotIndex = componentMiner2.Inventory.ActiveSlotIndex;
				for (int j = 0; j < inventory.SlotsCount; j++)
				{
					keptInventoryData.SlotValues[j] = inventory.GetSlotValue(j);
					keptInventoryData.SlotCounts[j] = inventory.GetSlotCount(j);
				}
				ComponentClothing componentClothing = entity.FindComponent<ComponentClothing>();
				if (componentClothing != null)
				{
					foreach (int enumValue in EnumUtils.GetEnumValues(typeof(ClothingSlot)))
					{
						keptInventoryData.Clothes[enumValue] = new List<int>(componentClothing.GetClothes((ClothingSlot)enumValue));
					}
				}
				m_keptInventories[componentPlayer.PlayerData.PlayerGUID] = keptInventoryData;
			}
		}
		foreach (PlayerData allPlayersDatum in AllPlayersData)
		{
			allPlayersDatum.OnEntityRemoved(entity);
		}
		UpdateComponentPlayers();
	}

	public void UpdateComponentPlayers()
	{
		m_componentPlayers.Clear();
		foreach (PlayerData y in m_playersData)
		{
			if (y.ComponentPlayer != null)
			{
				m_componentPlayers.Add(y.ComponentPlayer);
			}
		}
	}

	public void MakePlayerOffline(PlayerData playerData, bool showMsg = true)
	{
		PlayerData playerData2 = m_playersData.Find((PlayerData p) => playerData != null && p.PlayerGUID == playerData.PlayerGUID);
		if (playerData2 == null)
		{
			return;
		}
		playerData2.isSetStatus = false;
		TerrainUpdater terrainUpdater = SubsystemTerrain.TerrainUpdater;
		if (playerData2.Client != null)
		{
			object waitChunkListLock = terrainUpdater.m_waitChunkListLock;
			if (waitChunkListLock != null)
			{
				lock (waitChunkListLock)
				{
					if (terrainUpdater.waitChunkList.ContainsKey(playerData2.Client))
					{
						terrainUpdater.waitChunkList.Remove(playerData2.Client);
					}
				}
			}
		}
		ComponentPlayer componentPlayer = playerData2.ComponentPlayer;
		if (componentPlayer != null)
		{
			if (showMsg && CommonLib.WorkType == WorkType.Server)
			{
				string msg = LanguageControl.Get("SubsystemGameWidgets", "player_left").Replace("{0}", playerData2.Name);
				Project.FindSubsystem<SubsystemGameWidgets>().AddMessage(msg, null, 0);
			}
			EntityDataList entityDataList = Project.SaveEntities(new Entity[1] { componentPlayer.Entity });
			ValuesDictionary valuesDictionary = new ValuesDictionary();
			entityDataList.EntitiesData[0].Save(valuesDictionary);
			m_offlinePlayerEntities[playerData2.PlayerGUID] = new EntityData(Project.GameDatabase, valuesDictionary);
			Project.RemoveEntity(componentPlayer.Entity, disposeEntity: true);
		}
		m_offlinePlayers.Add(playerData2);
		SubsystemTerrain.TerrainUpdater.RemoveUpdateLocation(playerData2.PlayerIndex);
		m_playersData.Remove(playerData2);
		if (usedIndies.ContainsKey(playerData2.PlayerIndex))
		{
			usedIndies.Remove(playerData2.PlayerIndex);
		}
		PlayerRemoved?.Invoke(playerData2);
		SubsystemTerrain.TerrainUpdater.SetLastChunksUpdateCenter(-1, null);
		UpdateComponentPlayers();
	}

	public Entity MakePlayerOnline(Guid playerGuid)
	{
		DebugLog($"Вызван MakePlayerOnline для GUID: {playerGuid}");
		PlayerData playerData = m_offlinePlayers.FirstOrDefault((PlayerData p) => p.PlayerGUID == playerGuid);
		if (playerData != null)
		{
			if (IsGuidBanned(playerData.PlayerGUID))
			{
				DebugLog("Игрок " + playerData.Name + " забанен, вход отменен.");
				if (playerData.Client != null)
				{
					CommonLib.Net.RemoveClientImmediate(playerData.Client, LanguageControl.Get("SubsystemGameWidgets", "blacklisted_by_admin"));
				}
				return null;
			}
			m_offlinePlayers.Remove(playerData);
			playerData.m_stateMachine.TransitionTo("FirstUpdate");
			DebugLog("Переносим игрока " + playerData.Name + " в список активных...");
			AddPlayerData(playerData);
			if (m_offlinePlayerEntities.ContainsKey(playerGuid) && CommonLib.WorkType == WorkType.Server)
			{
				DebugLog("Восстанавливаем Entity для " + playerData.Name + "...");
				EntityDataList entityDataList = new EntityDataList
				{
					EntitiesData = new List<EntityData>(1)
				};
				entityDataList.EntitiesData.Add(m_offlinePlayerEntities[playerGuid]);
				m_offlinePlayerEntities.Remove(playerGuid);
				Entity entity = Project.LoadEntities(entityDataList)[0];
				entity.EntityId = 0;
				if (entity.FindComponent<ComponentPlayer>().PlayerData.PlayerGUID != playerGuid)
				{
					throw new Exception("Данные игрока не синхронизированы с сервером");
				}
				Project.AddEntity(entity);
				DebugLog("Entity восстановлена. Успех.");
				return entity;
			}
		}
		else
		{
			DebugLog($"Игрок с GUID {playerGuid} не найден в OfflinePlayers.");
		}
		return null;
	}

	public void AddNoMsgList(PlayerData playerData)
	{
		string data = playerData.PlayerGUID.ToString();
		if (!NoMsgPlayerGuidList.Contains(data))
		{
			NoMsgPlayerGuidList.Add(data);
			NoMsgPlayerNames[data] = playerData.Name ?? string.Empty;
			DialogsManager.Alert(LanguageControl.Get("SubsystemGameWidgets", "mute_success").Replace("{0}", playerData.Name));
			if (playerData.ClientId.HasValue)
			{
				CommonLib.Net.QueuePackage(new MessagePackage(null, LanguageControl.Get("SubsystemGameWidgets", "muted_by_admin"), 0, new List<byte> { playerData.ClientId.Value }));
				CommonLib.Net.QueuePackage(new PlayerDataPackage(playerData.PlayerGUID, add: true));
			}
		}
	}

	public void RemoveNoMsgList(PlayerData playerData)
	{
		string text = playerData.PlayerGUID.ToString();
		if (NoMsgPlayerGuidList.Contains(text))
		{
			NoMsgPlayerGuidList.Remove(text);
			NoMsgPlayerNames.Remove(text);
			DialogsManager.Alert(LanguageControl.Get("SubsystemGameWidgets", "unmute_success").Replace("{0}", playerData.Name));
			if (playerData.ClientId.HasValue)
			{
				CommonLib.Net.QueuePackage(new MessagePackage(null, LanguageControl.Get("SubsystemGameWidgets", "unmuted_by_admin"), 0, new List<byte> { playerData.ClientId.Value }));
				CommonLib.Net.QueuePackage(new PlayerDataPackage(playerData.PlayerGUID, add: false));
			}
		}
	}

	public void AddBlackList(PlayerData playerData)
	{
		string text = playerData.PlayerGUID.ToString();
		bool flag = false;
		if (!BlackPlayerGuidList.Contains(text))
		{
			BlackPlayerGuidList.Add(text);
			flag = true;
		}
		string text2 = playerData.Name ?? string.Empty;
		if (text2.IndexOf('|') >= 0)
		{
			text2 = text2.Replace('|', ' ');
		}
		string text3 = string.Empty;
		string text4 = string.Empty;
		if (m_lastConnections.TryGetValue(playerData.PlayerGUID, out var value))
		{
			if (!string.IsNullOrEmpty(value.IpAddress))
			{
				text3 = value.IpAddress;
			}
			if (!string.IsNullOrEmpty(value.HardwareId))
			{
				text4 = value.HardwareId;
			}
		}
		if (string.IsNullOrEmpty(text4))
		{
			if (!string.IsNullOrEmpty(playerData.CurrentHardwareId))
			{
				text4 = playerData.CurrentHardwareId;
			}
			else if (playerData.IsMainPlayer || CommonLib.WorkType == WorkType.Local)
			{
				text4 = CommonLib.HardwareId;
			}
		}
		string value2 = ((string.IsNullOrEmpty(text3) && string.IsNullOrEmpty(text4)) ? text2 : (text2 + "|" + (text3 ?? string.Empty) + "|" + (text4 ?? string.Empty)));
		BlackPlayerNames[text] = value2;
		if (playerData.Client != null)
		{
			CommonLib.Net.RemoveClientImmediate(playerData.Client, LanguageControl.Get("SubsystemGameWidgets", "blacklisted_by_admin"));
		}
		if (flag)
		{
			DialogsManager.Alert(LanguageControl.Get("SubsystemGameWidgets", "blacklist_success").Replace("{0}", playerData.Name));
		}
	}

	public PlayerData FindPlayerData(Func<PlayerData, bool> f)
	{
		return m_playersData.FirstOrDefault(f) ?? m_offlinePlayers.FirstOrDefault(f);
	}

	public static bool IsMainPlayer(Entity e)
	{
		return e.FindComponent<ComponentPlayer>()?.PlayerData.IsMainPlayer ?? false;
	}

	public SubsystemPlayers()
	{
		m_keptInventories = new Dictionary<Guid, KeptInventoryData>();
		m_blockMiningStates = new Dictionary<Point3, List<ComponentMiner>>();
		m_playersData = new List<PlayerData>();
		m_offlinePlayers = new List<PlayerData>();
		m_offlinePlayerEntities = new Dictionary<Guid, EntityData>();
		m_componentPlayers = new List<ComponentPlayer>();
		usedIndies = new Dictionary<int, PlayerData>();
		BlackPlayerGuidList = new List<string>();
		NoMsgPlayerGuidList = new List<string>();
		BlackPlayerNames = new Dictionary<string, string>();
		NoMsgPlayerNames = new Dictionary<string, string>();
		m_lastConnections = new Dictionary<Guid, BanConnectionInfo>();
	}

	private void UpdateBlockMining(float dt)
	{
		List<Point3> list = new List<Point3>();
		foreach (KeyValuePair<Point3, List<ComponentMiner>> blockMiningState in m_blockMiningStates)
		{
			Point3 key = blockMiningState.Key;
			List<ComponentMiner> value = blockMiningState.Value;
			float num = 0f;
			foreach (ComponentMiner item in value)
			{
				if (item.DigCellFace.HasValue && item.DigCellFace.Value.X == key.X && item.DigCellFace.Value.Y == key.Y && item.DigCellFace.Value.Z == key.Z)
				{
					float num2 = item.CalculateDigTime(SubsystemTerrain.Terrain.GetCellValue(key.X, key.Y, key.Z), Terrain.ExtractContents(item.ActiveBlockValue));
					if (num2 > 0f)
					{
						item.m_digProgress += dt / num2;
						num += item.m_digProgress;
					}
				}
				else
				{
					item.DigCellFace = null;
				}
			}
			if (value.All((ComponentMiner m) => !m.DigCellFace.HasValue))
			{
				list.Add(key);
			}
			else if (num >= 1f)
			{
				CooperativeDigBlock(key, value, dt);
				list.Add(key);
			}
		}
		if (CommonLib.WorkType == WorkType.Server && Time.RealTime - m_lastSnapshotTime > 15.0)
		{
			m_lastSnapshotTime = Time.RealTime;
		}
		foreach (Point3 item2 in list)
		{
			m_blockMiningStates.Remove(item2);
		}
	}

	public void StartMiningBlock(ComponentMiner miner, Point3 blockPosition)
	{
		if (!m_blockMiningStates.ContainsKey(blockPosition))
		{
			m_blockMiningStates[blockPosition] = new List<ComponentMiner>();
		}
		if (!m_blockMiningStates[blockPosition].Contains(miner))
		{
			m_blockMiningStates[blockPosition].Add(miner);
			miner.DigCellFace = new CellFace(blockPosition.X, blockPosition.Y, blockPosition.Z, 0);
			miner.m_digStartTime = m_subsystemTime.GameTime;
		}
	}

	public void StopMiningBlock(ComponentMiner miner, Point3 blockPosition)
	{
		if (m_blockMiningStates.ContainsKey(blockPosition))
		{
			m_blockMiningStates[blockPosition].Remove(miner);
			miner.DigCellFace = null;
			if (m_blockMiningStates[blockPosition].Count == 0)
			{
				m_blockMiningStates.Remove(blockPosition);
			}
		}
	}

	private void CooperativeDigBlock(Point3 blockPosition, List<ComponentMiner> miners, float dt)
	{
		int cellValue = SubsystemTerrain.Terrain.GetCellValue(blockPosition.X, blockPosition.Y, blockPosition.Z);
		int num = Terrain.ExtractContents(cellValue);
		Block block = BlocksManager.Blocks[num];
		int num2 = 0;
		foreach (ComponentMiner miner in miners)
		{
			int activeBlockValue = miner.ActiveBlockValue;
			if (miner.CanUseTool(activeBlockValue) && activeBlockValue > num2)
			{
				num2 = activeBlockValue;
			}
		}
		int num3 = Terrain.ExtractContents(num2);
		float num4 = BlocksManager.Blocks[num3].ToolLevel;
		float requiredToolLevel = block.GetRequiredToolLevel(cellValue);
		if (num4 < requiredToolLevel)
		{
			num4 = 0f;
		}
		TerrainRaycastResult raycastResult = new TerrainRaycastResult
		{
			CellFace = new CellFace(blockPosition.X, blockPosition.Y, blockPosition.Z, 0)
		};
		if (num3 == 367)
		{
			(miners.FirstOrDefault((ComponentMiner m) => Terrain.ExtractContents(m.ActiveBlockValue) == 367) ?? miners[0]).Destroy3x3Around(raycastResult.CellFace);
		}
		else
		{
			BlockPlacementData digValue = block.GetDigValue(SubsystemTerrain, miners[0], cellValue, num2, raycastResult);
			SubsystemCaveInstability subsystemCaveInstability = Project.FindSubsystem<SubsystemCaveInstability>(throwOnError: true);
			if (subsystemCaveInstability != null && Array.IndexOf(subsystemCaveInstability.HandledBlocks, num) >= 0)
			{
				subsystemCaveInstability.MarkPlayerBreak(new Point3(digValue.CellFace.X, digValue.CellFace.Y, digValue.CellFace.Z));
			}
			SubsystemTerrain.DestroyCell((int)num4, digValue.CellFace.X, digValue.CellFace.Y, digValue.CellFace.Z, digValue.Value, noDrop: false, noParticleSystem: false);
		}
		foreach (ComponentMiner miner2 in miners)
		{
			ComponentCreature componentCreature = miner2.ComponentCreature;
			if (componentCreature == null)
			{
				continue;
			}
			if (componentCreature.PlayerStats != null)
			{
				componentCreature.PlayerStats.BlocksDug++;
			}
			if (componentCreature.PlayerAchievements == null)
			{
				continue;
			}
			if (num == 10 || num == 255 || num == 9 || num == 11)
			{
				componentCreature.PlayerAchievements.TreesChopping++;
				if (componentCreature.PlayerAchievements.TreesChopping == 1)
				{
					componentCreature.PlayerAchievements.EnqueueTreeChoppingAchievement();
				}
			}
			if (num == 112 && num4 <= 2f && componentCreature.PlayerAchievements.HatedDay == 0L)
			{
				componentCreature.PlayerAchievements.HatedDay = 1L;
				componentCreature.PlayerAchievements.EnqueueHatedDayAchievement();
			}
			if (num != 112 || componentCreature.PlayerAchievements.MasochistSecondNatureName != 0L || !(componentCreature is ComponentPlayer componentPlayer) || !(componentPlayer.ComponentVitalStats.Food < 0.1f))
			{
				continue;
			}
			ComponentClothing componentClothing = componentPlayer.Entity.FindComponent<ComponentClothing>();
			if (componentClothing == null)
			{
				continue;
			}
			bool flag = false;
			foreach (int item in componentClothing.GetClothes(ClothingSlot.Sleeves))
			{
				int num5 = Terrain.ExtractContents(item);
				int value = Terrain.ExtractData(item);
				ClothingData clothingData = BlocksManager.Blocks[num5].GetClothingData(value);
				if (clothingData != null && clothingData.MinerSpeed == 0.6f)
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				componentCreature.PlayerAchievements.MasochistSecondNatureName = 1L;
				componentCreature.PlayerAchievements.EnqueueMasochistSecondNatureAchievement();
			}
		}
		foreach (ComponentMiner miner3 in miners)
		{
			miner3.m_digProgress = 0f;
			if (block.IsCollidable)
			{
				miner3.DamageActiveTool(1);
			}
			miner3.DigCellFace = null;
		}
	}

	public void RegisterConnectionInfo(Guid playerGuid, string ipAddress, string hardwareId)
	{
		if (!m_lastConnections.TryGetValue(playerGuid, out var value))
		{
			value = default;
		}
		value.IpAddress = ipAddress;
		value.HardwareId = hardwareId;
		m_lastConnections[playerGuid] = value;
		if (string.IsNullOrEmpty(hardwareId))
		{
			return;
		}
		PlayerData playerData = FindPlayerData((PlayerData p) => p.PlayerGUID == playerGuid);
		if (playerData != null)
		{
			if (!string.IsNullOrEmpty(playerData.CurrentHardwareId) && !string.Equals(playerData.CurrentHardwareId, hardwareId, StringComparison.OrdinalIgnoreCase))
			{
				playerData.PreviousHardwareId = playerData.CurrentHardwareId;
			}
			if (string.IsNullOrEmpty(playerData.CurrentHardwareId) || !string.Equals(playerData.CurrentHardwareId, hardwareId, StringComparison.OrdinalIgnoreCase))
			{
				playerData.CurrentHardwareId = hardwareId;
			}
		}
	}

	public bool IsGuidBanned(Guid guid)
	{
		return BlackPlayerGuidList.Contains(guid.ToString());
	}

	public bool IsBanned(Guid guid, string ipAddress, string hardwareId)
	{
		string num = guid.ToString();
		if (BlackPlayerGuidList.Contains(num))
		{
			return true;
		}
		if (string.IsNullOrEmpty(hardwareId))
		{
			return false;
		}
		foreach (KeyValuePair<string, string> blackPlayerName in BlackPlayerNames)
		{
			string[] array = (blackPlayerName.Value ?? string.Empty).Split('|');
			string text = ((array.Length >= 3) ? array[2] : string.Empty);
			if (!string.IsNullOrEmpty(text) && string.Equals(hardwareId, text, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	public void RemoveBlackList(PlayerData playerData)
	{
		string terrain = playerData.PlayerGUID.ToString();
		if (BlackPlayerGuidList.Contains(terrain))
		{
			BlackPlayerGuidList.Remove(terrain);
		}
		if (BlackPlayerNames.ContainsKey(terrain))
		{
			BlackPlayerNames.Remove(terrain);
		}
	}

	private string IntArrayToString(int[] array)
	{
		if (array == null)
		{
			return string.Empty;
		}
		return string.Join(",", array);
	}

	private int[] StringToIntArray(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return new int[0];
		}
		string[] node = s.Split(',');
		int[] array = new int[node.Length];
		for (int i = 0; i < node.Length; i++)
		{
			int.TryParse(node[i], out array[i]);
		}
		return array;
	}

	private void DebugLog(string message)
	{
	}

	private void DumpPlayerList(string context)
	{
		DebugLog("--- Список игроков (" + context + ") ---");
		foreach (PlayerData topColor in m_playersData)
		{
			string text = ((topColor.Client != null) ? $"ID {topColor.Client.ID}" : "NULL");
			string text2 = ((topColor.ComponentPlayer != null) ? "Alive" : "NoEntity");
			DebugLog($"   > Игрок: {topColor.Name} | GUID: {topColor.PlayerGUID} | Client: {text} | Entity: {text2}");
		}
		DebugLog("-------------------------------------");
	}
}
