using System;
using System.Collections.Generic;
using Engine;
using GameEntitySystem;

namespace Game.NetWork;

public static class ServerSecurity
{
	public const int MaxTerrainEditsPerPackage = 2048;

	private static readonly Dictionary<string, double> s_lastWarn = new Dictionary<string, double>();

	public const float InteractReach = 10f;

	public static void ApplyServerRecord(PlayerData incoming, PlayerData serverRecord)
	{
		if (serverRecord != null)
		{
			incoming.ServerManager = serverRecord.ServerManager;
			incoming.IsServerAdmin = serverRecord.IsServerAdmin;
			incoming.IsVanish = serverRecord.IsVanish;
			incoming.WorldEditManager = serverRecord.WorldEditManager;
			incoming.IsCheater = serverRecord.IsCheater;
			incoming.Level = serverRecord.Level;
			incoming.ExtraLives = serverRecord.ExtraLives;
			incoming.SpawnsCount = serverRecord.SpawnsCount;
			incoming.FirstSpawnTime = serverRecord.FirstSpawnTime;
			incoming.LastSpawnTime = serverRecord.LastSpawnTime;
			incoming.Friends = ((serverRecord.Friends != null) ? new List<Guid>(serverRecord.Friends) : new List<Guid>());
			incoming.HomePoints = ((serverRecord.HomePoints != null) ? new Dictionary<int, Vector3>(serverRecord.HomePoints) : new Dictionary<int, Vector3>());
		}
		else
		{
			incoming.ServerManager = false;
			incoming.IsServerAdmin = false;
			incoming.IsVanish = false;
			incoming.WorldEditManager = false;
			incoming.IsCheater = false;
			incoming.Level = 1f;
			incoming.ExtraLives = 0;
			incoming.SpawnsCount = 0;
			incoming.FirstSpawnTime = 0.0;
			incoming.LastSpawnTime = 0.0;
			incoming.Friends = new List<Guid>();
			incoming.HomePoints = new Dictionary<int, Vector3>();
		}
	}

	public static bool CanUseCreativeFly(ProjectNet projectNet, PlayerData player)
	{
		if (player == null)
		{
			return false;
		}
		if (player.ServerManager)
		{
			return true;
		}
		SubsystemGameInfo subsystemGameInfo = projectNet.FindSubsystem<SubsystemGameInfo>();
		if (subsystemGameInfo != null && subsystemGameInfo.WorldSettings != null)
		{
			return subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative;
		}
		return false;
	}

	public static bool CanEditCell(PlayerData player, WorldSettings worldSettings, int x, int z, int oldValue, int newValue)
	{
		if (player.ServerManager)
		{
			return true;
		}
		bool flag = SubsystemBedrockBlockBehavior.CheckIsInTerritoriy(x, z, out var territoriy);
		if (worldSettings != null && worldSettings.RestrictWorldInteraction && !flag)
		{
			return false;
		}
		if (flag && territoriy != null)
		{
			bool flag2 = territoriy.OwnerGuid == player.PlayerGUID;
			bool flag3 = player.Friends != null && player.Friends.Contains(territoriy.OwnerGuid) && territoriy.ApplyToFirend;
			if (!flag2 && !flag3)
			{
				bool flag4 = Terrain.ExtractContents(oldValue) != 0;
				if (Terrain.ExtractContents(newValue) == 0)
				{
					return territoriy.AllowDig;
				}
				if (!flag4)
				{
					return territoriy.AllowPlace;
				}
				if (territoriy.AllowDig)
				{
					return territoriy.AllowPlace;
				}
				return false;
			}
		}
		return true;
	}

	public static void Warn(IClient from, string reason, string detail = null)
	{
		string text = ((from != null) ? from.ID.ToString() : "NULL");
		string key = text + "|" + reason;
		double realTime = Time.RealTime;
		lock (s_lastWarn)
		{
			if (s_lastWarn.TryGetValue(key, out var value) && realTime - value < 5.0)
			{
				return;
			}
			s_lastWarn[key] = realTime;
		}
		string text2 = "?";
		PlayerData playerData = from?.PlayerData;
		if (playerData != null && playerData.Name != null)
		{
			text2 = playerData.Name;
		}
		string text3 = "[SECURITY] " + reason + " | client " + text + " (" + text2 + ")";
		if (!string.IsNullOrEmpty(detail))
		{
			text3 = text3 + " | " + detail;
		}
		Log.Warning(text3);
	}

	public static bool IsInReach(PlayerData player, Point3 point)
	{
		if (player == null)
		{
			return false;
		}
		if (player.ServerManager)
		{
			return true;
		}
		ComponentPlayer componentPlayer = player.ComponentPlayer;
		if (componentPlayer == null)
		{
			return false;
		}
		Vector3 position = componentPlayer.ComponentBody.Position;
		float num = (float)point.X + 0.5f - position.X;
		float num2 = (float)point.Y + 0.5f - position.Y;
		float num3 = (float)point.Z + 0.5f - position.Z;
		return num * num + num2 * num2 + num3 * num3 <= 100f;
	}

	public static bool CanAccessInventory(PlayerData player, IInventory inventory)
	{
		if (player == null || inventory == null)
		{
			return false;
		}
		if (player.ServerManager)
		{
			return true;
		}
		if (inventory is BagInventory)
		{
			return true;
		}
		if (!(inventory is Component { Entity: not null } component))
		{
			return false;
		}
		ComponentPlayer componentPlayer = player.ComponentPlayer;
		if (componentPlayer != null && component.Entity == componentPlayer.Entity)
		{
			return true;
		}
		ComponentBlockEntity componentBlockEntity = component.Entity.FindComponent<ComponentBlockEntity>(throwOnError: false);
		if (componentBlockEntity != null)
		{
			return IsInReach(player, componentBlockEntity.Coordinates);
		}
		return false;
	}

	public static bool CanUseWorldEdit(ProjectNet projectNet, PlayerData player)
	{
		if (player == null)
		{
			return false;
		}
		if (player.ServerManager)
		{
			return true;
		}
		WorldSettings worldSettings = projectNet.FindSubsystem<SubsystemGameInfo>()?.WorldSettings;
		if (worldSettings == null || !worldSettings.IsWorldEditIntegrated || worldSettings.GameMode == GameMode.Embrace)
		{
			return false;
		}
		if (worldSettings.WEManagersOnly && !player.WorldEditManager)
		{
			return false;
		}
		if (worldSettings.GameMode == GameMode.Creative)
		{
			return false;
		}
		return player.ComponentPlayer != null;
	}

	public static void FilterTerrainEdits(ProjectNet projectNet, IClient from, List<Point3> cells, List<int> values, List<Point3> allowedCells, List<int> allowedValues)
	{
		PlayerData playerData = from?.PlayerData;
		if (playerData == null)
		{
			Warn(from, "terrain edit from a connection without a player");
			return;
		}
		int num = Math.Min(cells?.Count ?? 0, values?.Count ?? 0);
		if (num == 0)
		{
			return;
		}
		if (num > 2048)
		{
			Warn(from, "terrain edit package is too large", "cells=" + num + ", limit=" + 2048);
			return;
		}
		SubsystemTerrain subsystemTerrain = projectNet.FindSubsystem<SubsystemTerrain>();
		WorldSettings worldSettings = projectNet.FindSubsystem<SubsystemGameInfo>()?.WorldSettings;
		bool serverManager = playerData.ServerManager;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		if (!serverManager && worldSettings != null && worldSettings.GameMode == GameMode.Creative)
		{
			Warn(from, "terrain change request in creative by a non ServerManager", "gameMode=Creative");
			return;
		}
		if (!serverManager)
		{
			if (worldSettings == null || !worldSettings.IsWorldEditIntegrated || worldSettings.GameMode == GameMode.Embrace)
			{
				string text = ((worldSettings != null) ? worldSettings.GameMode.ToString() : "null");
				Warn(from, "terrain edit while WorldEdit is disabled", "integrated=" + (worldSettings?.IsWorldEditIntegrated ?? false) + ", gameMode=" + text);
				return;
			}
			if (worldSettings.WEManagersOnly && !playerData.WorldEditManager)
			{
				Warn(from, "terrain edit by a non WorldEdit manager", "WEManagersOnly=true, WorldEditManager=false");
				return;
			}
			ComponentPlayer componentPlayer = playerData.ComponentPlayer;
			if (componentPlayer == null)
			{
				Warn(from, "terrain edit from a player that is not in the world", "ComponentPlayer is null");
				return;
			}
			Vector3 position = componentPlayer.ComponentBody.Position;
			num3 = position.X;
			num4 = position.Z;
			float num5 = Math.Max(SettingsManager.VisibilityRange, 64) + 32;
			num2 = num5 * num5;
		}
		int num6 = 0;
		int num7 = 0;
		int num8 = 0;
		for (int i = 0; i < num; i++)
		{
			Point3 item = cells[i];
			int num9 = values[i];
			int num10 = Terrain.ExtractContents(num9);
			if (item.Y < 0 || item.Y >= 256 || num10 < 0 || num10 >= BlocksManager.Blocks.Length || BlocksManager.Blocks[num10] == null)
			{
				num8++;
				continue;
			}
			if (subsystemTerrain.Terrain.GetChunkAtCell(item.X, item.Z) == null)
			{
				num8++;
				continue;
			}
			if (!serverManager)
			{
				float num11 = (float)item.X + 0.5f - num3;
				float num12 = (float)item.Z + 0.5f - num4;
				if (num11 * num11 + num12 * num12 > num2)
				{
					num6++;
					continue;
				}
				int cellValue = subsystemTerrain.Terrain.GetCellValue(item.X, item.Y, item.Z);
				if (!CanEditCell(playerData, worldSettings, item.X, item.Z, cellValue, num9))
				{
					num7++;
					continue;
				}
			}
			allowedCells.Add(item);
			allowedValues.Add(num9);
		}
		if (num6 + num7 + num8 > 0)
		{
			string text2 = (serverManager ? "manager" : (num3.ToString("0.0") + "/" + num4.ToString("0.0")));
			Warn(from, "terrain edit rejected", "of " + num + " cells: range=" + num6 + ", territory=" + num7 + ", invalid=" + num8 + ", player=" + text2);
		}
	}
}
