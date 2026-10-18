using System;
using System.Collections.Generic;
using Engine;
using Game.NetWork;
using Game.NetWork.Packages;
using TemplatesDatabase;

namespace Game;

public class SubsystemFermentationBarrelBlockBehavior : SubsystemBlockBehavior, IUpdateable
{
	private SubsystemBlockEntities m_subsystemBlockEntities;

	private SubsystemTerrain m_subsystemTerrain;

	private SubsystemExplosions m_subsystemExplosions;

	private SubsystemAmbientSounds m_subsystemAmbientSounds;

	private HashSet<Point3> m_activeBarrels = new HashSet<Point3>();

	public override int[] HandledBlocks => new int[1] { 428 };

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemBlockEntities = Project.FindSubsystem<SubsystemBlockEntities>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemExplosions = Project.FindSubsystem<SubsystemExplosions>(throwOnError: true);
		m_subsystemAmbientSounds = Project.FindSubsystem<SubsystemAmbientSounds>(throwOnError: true);
	}

	public override void OnBlockAdded(int value, int oldValue, int x, int y, int z, ComponentMiner miner)
	{
		if (Terrain.ExtractContents(value) == 428 && m_subsystemBlockEntities.GetBlockEntity(x, y, z) == null)
		{
			m_subsystemBlockEntities.CreateBlockEntity("FermentationBarrel", new Point3(x, y, z), miner);
		}
		if (Terrain.ExtractContents(value) == 428 && FermentationBarrelBlock.GetActive(Terrain.ExtractData(value)))
		{
			m_activeBarrels.Add(new Point3(x, y, z));
		}
	}

	public override void OnBlockRemoved(int value, int newValue, int x, int y, int z)
	{
		m_activeBarrels.Remove(new Point3(x, y, z));
		if (CommonLib.WorkType == WorkType.Client)
		{
			return;
		}
		ComponentBlockEntity blockEntity = m_subsystemBlockEntities.GetBlockEntity(x, y, z);
		if (blockEntity == null)
		{
			return;
		}
		ComponentFermentationBarrel componentFermentationBarrel = blockEntity.Entity.FindComponent<ComponentFermentationBarrel>(throwOnError: true);
		if (Terrain.ExtractContents(value) != 428 || Terrain.ExtractContents(newValue) == 428)
		{
			return;
		}
		Vector3 position = new Vector3((float)x + 0.5f, (float)y + 0.5f, (float)z + 0.5f);
		foreach (IInventory item in blockEntity.Entity.FindComponents<IInventory>())
		{
			item.DropAllItems(position);
		}
		bool active = FermentationBarrelBlock.GetActive(Terrain.ExtractData(value));
		if (((componentFermentationBarrel != null) & active) && !componentFermentationBarrel.SafeStopped && componentFermentationBarrel.State != ComponentFermentationBarrel.FermentationState.Ready)
		{
			m_subsystemExplosions.AddExplosion(x, y, z, 60f, isIncendiary: false, noExplosionSound: false, blockEntity.OwnPlayerData, disableItemSpawn: false);
		}
		SubsystemTerrain.Project.RemoveEntity(blockEntity.Entity, disposeEntity: true);
	}

	public override bool OnInteract(TerrainRaycastResult r, ComponentMiner miner)
	{
		if (CommonLib.WorkType == WorkType.Client && CommonLib.MainPlayer == miner.ComponentPlayer)
		{
			IPackage package = new BlockEditPackage(new Point3(r.CellFace.X, r.CellFace.Y, r.CellFace.Z), BlockEditPackage.EventType.OpenInventoryByPoint);
			CommonLib.Net.QueuePackage(package);
			return true;
		}
		ComponentBlockEntity blockEntity = m_subsystemBlockEntities.GetBlockEntity(r.CellFace.X, r.CellFace.Y, r.CellFace.Z);
		if (blockEntity != null && miner.ComponentPlayer != null)
		{
			if (miner.ComponentPlayer.PlayerData.IsMainPlayer)
			{
				ComponentFermentationBarrel barrel = blockEntity.Entity.FindComponent<ComponentFermentationBarrel>(throwOnError: true);
				miner.ComponentPlayer.ComponentGui.ModalPanelWidget = new FermentationBarrelWidget(miner.Inventory, barrel);
				AudioManager.PlaySound("Audio/BlockPlaced", 1f, 0f, 0f);
			}
			return true;
		}
		return false;
	}

	public void Update(float dt)
	{
		if (!Time.PeriodicEvent(0.5, 0.0))
		{
			return;
		}
		float num = float.MaxValue;
		float num2 = 0f;
		List<Point3> list = new List<Point3>();
		foreach (Point3 activeBarrel in m_activeBarrels)
		{
			if (!IsBarrelActiveAt(activeBarrel.X, activeBarrel.Y, activeBarrel.Z))
			{
				list.Add(activeBarrel);
				continue;
			}
			float num3 = m_subsystemAmbientSounds.SubsystemAudio.CalculateListenerDistanceSquared(new Vector3(activeBarrel.X, activeBarrel.Y, activeBarrel.Z));
			if (num3 <= 50f)
			{
				num2 = Math.Max(num2, 1f - Math.Min(num3 / 50f, 1f));
			}
			num = Math.Min(num, num3);
		}
		for (int i = 0; i < list.Count; i++)
		{
			m_activeBarrels.Remove(list[i]);
		}
		float fermentationSoundVolume = ((num > 50f) ? 0f : num2);
		m_subsystemAmbientSounds.FermentationSoundVolume = fermentationSoundVolume;
	}

	public override void OnBlockModified(int value, int oldValue, int x, int y, int z)
	{
		int num = Terrain.ExtractContents(value);
		int craftingRecipe = Terrain.ExtractContents(oldValue);
		Point3 num2 = new Point3(x, y, z);
		if (num != 428 && craftingRecipe == 428)
		{
			m_activeBarrels.Remove(num2);
		}
		else if (num == 428 && craftingRecipe != 428)
		{
			if (FermentationBarrelBlock.GetActive(Terrain.ExtractData(value)))
			{
				m_activeBarrels.Add(num2);
			}
		}
		else if (num == 428 && craftingRecipe == 428)
		{
			bool active = FermentationBarrelBlock.GetActive(Terrain.ExtractData(value));
			bool active2 = FermentationBarrelBlock.GetActive(Terrain.ExtractData(oldValue));
			if (active && !active2)
			{
				m_activeBarrels.Add(num2);
			}
			else if (!active & active2)
			{
				m_activeBarrels.Remove(num2);
			}
		}
	}

	private bool IsBarrelActiveAt(int x, int y, int z)
	{
		int cellValue = m_subsystemTerrain.Terrain.GetCellValue(x, y, z);
		if (Terrain.ExtractContents(cellValue) == 428)
		{
			return FermentationBarrelBlock.GetActive(Terrain.ExtractData(cellValue));
		}
		return false;
	}

	public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded)
	{
		if (Terrain.ExtractContents(value) == 428 && FermentationBarrelBlock.GetActive(Terrain.ExtractData(value)))
		{
			m_activeBarrels.Add(new Point3(x, y, z));
		}
	}
}
