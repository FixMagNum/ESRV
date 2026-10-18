using System;
using System.Collections.Generic;
using System.Diagnostics;
using Engine;
using Game.NetWork.Packages;

namespace Game.NetWork;

public class PackageManager
{
	private static Dictionary<byte, IPackage> Packages;

	private static List<IPackage> ReceivedPackages;

	private static Dictionary<byte, Action<IPackage>> PackageHandle;

	private static PacketType Last;

	public static void Initialize()
	{
		Packages.Clear();
		RegisterPackage(new ComponentClothingPackage());
		RegisterPackage(new ComponentFluPackage());
		RegisterPackage(new ComponentFurnacePackage());
		RegisterPackage(new ComponentFurnacePackage2());
		RegisterPackage(new FishingFloatPackage());
		RegisterPackage(new ComponentHealthPackage());
		RegisterPackage(new ComponentInventoryPackage());
		RegisterPackage(new ComponentBehaviorPackage());
		RegisterPackage(new ComponentMountPackage());
		RegisterPackage(new ComponentOnFirePackage());
		RegisterPackage(new ComponentPlayerPackage());
		RegisterPackage(new ComponentSicknessPackage());
		RegisterPackage(new ComponentSleepPackage());
		RegisterPackage(new ComponentVitalStatPackage());
		RegisterPackage(new FurniturePackage());
		RegisterPackage(new EntityPackage());
		RegisterPackage(new MessagePackage());
		RegisterPackage(new MovingBlockPackage());
		RegisterPackage(new PickablePackage());
		RegisterPackage(new PlayerDataPackage());
		RegisterPackage(new ProjectilePackage());
		RegisterPackage(new SubsystemBodyPackage());
		RegisterPackage(new SubsystemElectricityPackage());
		RegisterPackage(new SubsystemSkyPackage());
		RegisterPackage(new SubsystemTerrainPackage());
		RegisterPackage(new SubsystemTimePackage());
		RegisterPackage(new SubsystemWeatherPackage());
		RegisterPackage(new TerritoriyPackage());
		RegisterPackage(new ExplosionsPackage());
		RegisterPackage(new BlockEditPackage());
		RegisterPackage(new SubsystemPlayersPackage());
		RegisterPackage(new ComponentAlcoholPackage());
		RegisterPackage(new ComponentFermentationBarrelPackage());
		RegisterPackage(new ComponentAdrenalinePackage());
		RegisterPackage(new WorldEditEntitiesPackage());
		RegisterPackage(new BagPackage());
		RegisterPackage(new PaintGunPackage());
		RegisterPackage(new MovingBlockEditPackage());
		RegisterPackage(new MovingBlockChangePackage());
		RegisterPackage(new AmbientMusicPackage());
		if (!SettingsManager.DebugMode)
		{
			return;
		}
		Log.Warning("=== [NET CHECK] СПИСОК ЗАРЕГИСТРИРОВАННЫХ ПАКЕТОВ ===");
		byte b = 0;
		foreach (byte key in Packages.Keys)
		{
			if (key > b)
			{
				b = key;
			}
		}
		for (int i = 0; i <= b + 5; i++)
		{
			byte b2 = (byte)i;
			if (Packages.ContainsKey(b2))
			{
				IPackage package = Packages[b2];
				Log.Warning($"ID {b2}: {package.GetType().Name}");
			}
			else if (b2 == 48)
			{
				Log.Error($"ID {b2}: >>> ПУСТО (NULL) <<< -- ВОТ ТУТ ОШИБКА!");
			}
			else
			{
				Log.Warning($"ID {b2}: - пустой слот -");
			}
		}
		Log.Warning("==================================================");
	}

	public static void RegisterPackage(IPackage package)
	{
		Packages.Add(package.ID, package);
	}

	public static void UnRegisterPackage(IPackage package)
	{
		if (Packages.ContainsKey(package.ID))
		{
			Packages.Remove(package.ID);
		}
	}

	public static void RegisterPackageHanlde(PackageFlag packageFlag, Action<IPackage> action)
	{
		if (!PackageHandle.ContainsKey((byte)packageFlag))
		{
			PackageHandle[(byte)packageFlag] = null;
		}
		Dictionary<byte, Action<IPackage>> packageHandle = PackageHandle;
		packageHandle[(byte)packageFlag] = (Action<IPackage>)Delegate.Combine(packageHandle[(byte)packageFlag], action);
	}

	public static void UnRegisterPackageHanlde(PackageFlag packageFlag, Action<IPackage> action)
	{
		if (PackageHandle.ContainsKey((byte)packageFlag))
		{
			Dictionary<byte, Action<IPackage>> packageHandle = PackageHandle;
			packageHandle[(byte)packageFlag] = (Action<IPackage>)Delegate.Remove(packageHandle[(byte)packageFlag], action);
		}
	}

	public static void HandlePackages(ProjectNet projectNet, NetNode netNode, bool isServer)
	{
		for (int i = 0; i < ReceivedPackages.Count; i++)
		{
			IPackage package = ReceivedPackages[i];
			try
			{
				long timestamp = Stopwatch.GetTimestamp();
				package.Handle(projectNet, netNode, isServer);
				if (PackageHandle.TryGetValue(package.ID, out var value))
				{
					value?.Invoke(package);
				}
				double num = (double)(Stopwatch.GetTimestamp() - timestamp) / ((double)Stopwatch.Frequency / 1000.0);
				if (num > 15.0)
				{
					string arg = ((package.From != null) ? package.From.ID.ToString() : "?");
					NetNode.LogNetworkIssue(string.Format("Slow Handle <- C{0}: {1} took {2:0.0}ms", arg, package.GetType().Name.Replace("Package", ""), num));
				}
			}
			catch (Exception ex)
			{
				string name = package.GetType().Name;
				string text = ((package.From != null) ? $"Client {package.From.ID}" : "Unknown Client");
				Log.Error("[Package Error] Failed to handle " + name + " from " + text + ".");
				Log.Error("Error: " + ex.Message);
				Log.Error("Trace: " + ex.StackTrace);
			}
		}
		ReceivedPackages.Clear();
	}

	public static void DecodePackages(PackageStreamReader reader, IClient from)
	{
		while (reader.BaseStream.Position < reader.BaseStream.Length)
		{
			byte b = reader.ReadByte();
			if (!Packages.TryGetValue(b, out var value))
			{
				Log.Warning($"[Decode] Unknown package ID: {b} received from Client {from.ID}");
				break;
			}
			IPackage package = (IPackage)Activator.CreateInstance(value.GetType());
			package.From = from;
			Last = (PacketType)value.ID;
			try
			{
				long timestamp = Stopwatch.GetTimestamp();
				package.ReadData(reader);
				ReceivedPackages.Add(package);
				double num = (double)(Stopwatch.GetTimestamp() - timestamp) / ((double)Stopwatch.Frequency / 1000.0);
				if (num > 10.0)
				{
					NetNode.LogNetworkIssue(string.Format("Slow Decode <- C{0}: {1} took {2:0.0}ms", from.ID, value.GetType().Name.Replace("Package", ""), num));
				}
			}
			catch (Exception ex)
			{
				Log.Error($"[Decode Error] Failed to read data for {value.GetType().Name} from Client {from.ID}");
				Log.Error($"Stream Position: {reader.BaseStream.Position}/{reader.BaseStream.Length}");
				Log.Error("Error: " + ex.Message);
				break;
			}
		}
	}

	static PackageManager()
	{
		Packages = new Dictionary<byte, IPackage>();
		ReceivedPackages = new List<IPackage>();
		PackageHandle = new Dictionary<byte, Action<IPackage>>();
	}
}
