using System;
using System.Collections.Generic;
using System.Linq;
using Engine;
using Engine.Serialization;
using TemplatesDatabase;

namespace Game;

public class SubsystemMagnetBlockBehavior : SubsystemBlockBehavior
{
	public SubsystemPlayers m_subsystemPlayers;

	public DynamicArray<Vector3> m_magnets = new DynamicArray<Vector3>();

	public const int MaxMagnets = 8;

	public static Dictionary<Guid, Vector3> PlayerMagnets;

	public override int[] HandledBlocks => new int[1] { 167 };

	public int MagnetsCount => m_magnets.Count;

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		string value = valuesDictionary.GetValue<string>("Magnets");
		m_magnets = new DynamicArray<Vector3>(HumanReadableConverter.ValuesListFromString<Vector3>(';', value));
		if (valuesDictionary.ContainsKey("OwnerGuids"))
		{
			ValuesDictionary value2 = valuesDictionary.GetValue<ValuesDictionary>("OwnerGuids");
			{
				foreach (KeyValuePair<string, object> item in value2)
				{
					if (!Guid.TryParse(item.Key, out var result))
					{
						result = Guid.Empty;
					}
					Vector3 value3 = value2.GetValue<Vector3>(item.Key);
					PlayerMagnets[result] = value3;
				}
				return;
			}
		}
		PlayerMagnets = new Dictionary<Guid, Vector3>();
	}

	public override void Save(ValuesDictionary valuesDictionary)
	{
		base.Save(valuesDictionary);
		string value = HumanReadableConverter.ValuesListToString(';', m_magnets.ToArray());
		valuesDictionary.SetValue("Magnets", value);
		ValuesDictionary valuesDictionary2 = new ValuesDictionary();
		foreach (KeyValuePair<Guid, Vector3> playerMagnet in PlayerMagnets)
		{
			valuesDictionary2.SetValue(playerMagnet.Key.ToString(), playerMagnet.Value);
		}
		valuesDictionary.SetValue("OwnerGuids", valuesDictionary2);
	}

	public override void OnBlockAdded(int value, int oldValue, int x, int y, int z)
	{
		m_magnets.Add(new Vector3(x, y, z));
	}

	public override void OnBlockRemoved(int value, int newValue, int x, int y, int z)
	{
		base.OnBlockRemoved(value, newValue, x, y, z);
		Vector3 magnetPosition = new Vector3(x, y, z);
		Guid key = PlayerMagnets.FirstOrDefault((KeyValuePair<Guid, Vector3> m) => m.Value == magnetPosition).Key;
		if (key != Guid.Empty)
		{
			PlayerMagnets.Remove(key);
		}
	}

	public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
	{
		int random = SubsystemTerrain.Terrain.GetCellContents(x, y - 1, z);
		if (BlocksManager.Blocks[random].IsTransparent)
		{
			SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
		}
	}

	public override void OnBlockPlaced(ComponentMiner miner, int x, int y, int z, ref BlockPlacementData placementData, int itemValue)
	{
		ComponentPlayer componentPlayer = miner.ComponentPlayer;
		if (componentPlayer != null)
		{
			Guid playerGUID = componentPlayer.PlayerData.PlayerGUID;
			if (PlayerMagnets.ContainsKey(playerGUID))
			{
				componentPlayer.ComponentGui.DisplaySmallMessage("You can only place one magnet block", Color.Red, blinking: false, playNotificationSound: true);
				return;
			}
			PlayerMagnets[playerGUID] = new Vector3(x, y, z);
		}
		else
		{
			PlayerMagnets[Guid.Empty] = new Vector3(x, y, z);
		}
		base.OnBlockPlaced(miner, x, y, z, ref placementData, itemValue);
	}

	static SubsystemMagnetBlockBehavior()
	{
		PlayerMagnets = new Dictionary<Guid, Vector3>();
	}

	public Vector3 FindNearestCompassTarget(Vector3 compassPosition, Guid ownerGuid)
	{
		float num = float.MaxValue;
		Vector3 vector = Vector3.Zero;
		foreach (KeyValuePair<Guid, Vector3> text in PlayerMagnets)
		{
			if (text.Key == ownerGuid)
			{
				float color = Vector3.DistanceSquared(compassPosition, text.Value);
				if (color < num)
				{
					num = color;
					vector = text.Value;
				}
			}
		}
		if (vector != Vector3.Zero)
		{
			return vector + new Vector3(0.5f);
		}
		return new Vector3(0f, 0f, 0f);
	}
}
