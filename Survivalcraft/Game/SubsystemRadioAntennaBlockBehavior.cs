using System.Collections.Generic;
using Engine;
using Game.NetWork;
using TemplatesDatabase;

namespace Game;

public class SubsystemRadioAntennaBlockBehavior : SubsystemBlockBehavior, IUpdateable
{
	private struct ColumnInfo
	{
		public int X;

		public int Z;

		public int BaseY;

		public int TopY;

		public int Height;
	}

	private readonly HashSet<Point3> _bases = new HashSet<Point3>();

	private SubsystemAmbientSounds m_ambient;

	private bool m_has;

	private bool m_hold;

	private float m_timer;

	public override int[] HandledBlocks => new int[1] { 418 };

	public new SubsystemTerrain SubsystemTerrain { get; private set; }

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		SubsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_ambient = Project.FindSubsystem<SubsystemAmbientSounds>(throwOnError: true);
	}

	public void Update(float dt)
	{
		ComponentPlayer mainPlayer = Project.FindSubsystem<SubsystemPlayers>(throwOnError: true).MainPlayer;
		if (mainPlayer == null)
		{
			return;
		}
		if (Terrain.ExtractContents(mainPlayer.ComponentMiner.ActiveBlockValue) != 418)
		{
			m_has = false;
			m_hold = false;
			return;
		}
		PlayerInput playerInput = mainPlayer.ComponentInput.PlayerInput;
		if (playerInput.Aim.HasValue && !m_hold)
		{
			if (!m_has)
			{
				m_has = true;
				m_timer = SettingsManager.BuildHoldTimerStart;
				m_hold = true;
			}
		}
		else if (!playerInput.Aim.HasValue)
		{
			m_has = false;
			m_hold = false;
		}
		if (!m_hold)
		{
			return;
		}
		m_timer -= dt;
		if (m_timer <= 0f)
		{
			Ray3 value = playerInput.Aim.Value;
			TerrainRaycastResult? terrainRaycastResult = SubsystemTerrain.Raycast(value.Position + value.Direction, value.Position + value.Direction * 8f, useInteractionBoxes: true, skipAirBlocks: true, null);
			if (terrainRaycastResult.HasValue)
			{
				OnInteract(terrainRaycastResult.Value, mainPlayer.ComponentMiner);
			}
			m_timer = SettingsManager.BuildHoldTimerInterval;
		}
	}

	public override void OnBlockAdded(int value, int oldValue, int x, int y, int z)
	{
		ColumnInfo columnInfo = ScanColumnAt(x, y, z, treatMissingAsAntenna: false);
		if (columnInfo.Height > 0)
		{
			_bases.Add(new Point3(x, columnInfo.BaseY, z));
		}
		ForceColumnRegen(x, z);
	}

	public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded)
	{
		ColumnInfo columnInfo = ScanColumnAt(x, y, z, treatMissingAsAntenna: false);
		if (columnInfo.Height > 0)
		{
			_bases.Add(new Point3(x, columnInfo.BaseY, z));
		}
	}

	public override void OnBlockRemoved(int value, int newValue, int x, int y, int z)
	{
		DestroyUnsupportedAbove(x, y + 1, z);
		RefreshBasesForColumn(x, z);
		ForceColumnRegen(x, z);
	}

	private void DestroyUnsupportedAbove(int x, int startY, int z)
	{
		bool flag = false;
		for (int i = startY; i <= 255 && Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(x, i, z)) == 418; i++)
		{
			if (!flag)
			{
				if (m_ambient != null)
				{
					m_ambient.SubsystemAudio.PlayRandomSound("Audio/Impacts/Metal", 1f, 0f, new Vector3((float)x + 0.5f, (float)i + 0.5f, (float)z + 0.5f), 3f, autoDelay: false);
				}
				flag = true;
			}
			SubsystemTerrain.DestroyCell(0, x, i, z, 0, noDrop: false, noParticleSystem: false);
		}
	}

	private void RefreshBasesForColumn(int x, int z)
	{
		List<Point3> list = null;
		foreach (Point3 basis in _bases)
		{
			if (basis.X == x && basis.Z == z)
			{
				if (list == null)
				{
					list = new List<Point3>();
				}
				list.Add(basis);
			}
		}
		if (list != null)
		{
			foreach (Point3 item in list)
			{
				_bases.Remove(item);
			}
		}
		for (int i = 0; i <= 255; i++)
		{
			if (Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(x, i, z)) == 418)
			{
				_bases.Add(new Point3(x, i, z));
				break;
			}
		}
	}

	public override bool OnInteract(TerrainRaycastResult rr, ComponentMiner miner)
	{
		if (Terrain.ExtractContents(miner.ActiveBlockValue) != 418)
		{
			return false;
		}
		ColumnInfo columnInfo = ScanColumnAt(rr.CellFace.X, rr.CellFace.Y, rr.CellFace.Z, treatMissingAsAntenna: false);
		if (columnInfo.Height == 0)
		{
			columnInfo = ScanColumnAt(rr.CellFace.X, MathUtils.Clamp(rr.CellFace.Y, 0, 255), rr.CellFace.Z, treatMissingAsAntenna: false);
		}
		if (columnInfo.Height == 0)
		{
			int y = rr.CellFace.Y;
			if (y < 255 && IsAir(rr.CellFace.X, y, rr.CellFace.Z))
			{
				Place(rr.CellFace.X, y, rr.CellFace.Z);
				_bases.Add(new Point3(rr.CellFace.X, y, rr.CellFace.Z));
				PlayPlaceSound(rr.CellFace.X, rr.CellFace.Y, rr.CellFace.Z);
				if (miner.Inventory != null && CommonLib.WorkType != WorkType.Client)
				{
					miner.Inventory.RemoveSlotItems(miner.Inventory.ActiveSlotIndex, 1);
				}
				ForceColumnRegen(rr.CellFace.X, rr.CellFace.Z);
				return true;
			}
			return true;
		}
		if (columnInfo.TopY >= 254)
		{
			miner.ComponentPlayer?.ComponentGui?.DisplaySmallMessage(LanguageControl.Get("RadioAntenna", "MaxHeight"), Color.White, blinking: false, playNotificationSound: false);
			return true;
		}
		if (!IsAir(columnInfo.X, columnInfo.TopY + 1, columnInfo.Z))
		{
			miner.ComponentPlayer?.ComponentGui?.DisplaySmallMessage(LanguageControl.Get("RadioAntenna", "BlockedAbove"), Color.White, blinking: false, playNotificationSound: false);
			return true;
		}
		Place(columnInfo.X, columnInfo.TopY + 1, columnInfo.Z);
		_bases.Add(new Point3(columnInfo.X, columnInfo.BaseY, columnInfo.Z));
		PlayPlaceSound(rr.CellFace.X, rr.CellFace.Y, rr.CellFace.Z);
		if (miner.Inventory != null && CommonLib.WorkType != WorkType.Client)
		{
			miner.Inventory.RemoveSlotItems(miner.Inventory.ActiveSlotIndex, 1);
		}
		ForceAntennaVerticalRegen(columnInfo.X, columnInfo.Z, columnInfo.BaseY, columnInfo.TopY + 1);
		return true;
	}

	private ColumnInfo ScanColumnAt(int x, int y, int z, bool treatMissingAsAntenna)
	{
		Terrain terrain = SubsystemTerrain.Terrain;
		if (y < 0 || y > 255)
		{
			return default;
		}
		int num = y;
		while (num >= 0 && Terrain.ExtractContents(terrain.GetCellValue(x, num, z)) != 418 && (!treatMissingAsAntenna || num != y))
		{
			num--;
		}
		if (num < 0)
		{
			return default;
		}
		int i;
		for (i = num; i + 1 <= 255 && Terrain.ExtractContents(terrain.GetCellValue(x, i + 1, z)) == 418; i++)
		{
		}
		int num2 = num;
		while (num2 - 1 >= 0 && Terrain.ExtractContents(terrain.GetCellValue(x, num2 - 1, z)) == 418)
		{
			num2--;
		}
		return new ColumnInfo
		{
			X = x,
			Z = z,
			BaseY = num2,
			TopY = i,
			Height = i - num2 + 1
		};
	}

	private void Place(int x, int y, int z)
	{
		int value = Terrain.MakeBlockValue(418, 0, 0);
		SubsystemTerrain.ChangeCell(x, y, z, value);
	}

	private bool IsAir(int x, int y, int z)
	{
		return Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(x, y, z)) == 0;
	}

	public static int SideLengthFromHeight(int height)
	{
		if (height <= 1)
		{
			return 16;
		}
		return MathUtils.Clamp(16 + 16 * ((height - 1) / 6), 16, 192);
	}

	private void ForceColumnRegen(int x, int z)
	{
		TerrainChunk chunkAtCell = SubsystemTerrain.Terrain.GetChunkAtCell(x, z);
		if (chunkAtCell != null)
		{
			SubsystemTerrain.TerrainUpdater.DowngradeChunkNeighborhoodState(chunkAtCell.Coords, 1, TerrainChunkState.InvalidLight, forceGeometryRegeneration: false);
		}
	}

	private void PlayPlaceSound(int x, int y, int z)
	{
		if (m_ambient != null)
		{
			m_ambient.SubsystemAudio.PlayRandomSound("Audio/Impacts/Metal", 1f, 0f, new Vector3((float)x + 0.5f, (float)y + 0.5f, (float)z + 0.5f), 3f, autoDelay: false);
		}
	}

	private void ForceAntennaVerticalRegen(int x, int z, int baseY, int topY)
	{
		TerrainChunk chunkAtCell = SubsystemTerrain.Terrain.GetChunkAtCell(x, z);
		if (chunkAtCell != null)
		{
			int num = MathUtils.Clamp(baseY >> 4, 0, 15);
			int num2 = MathUtils.Clamp(topY >> 4, 0, 15);
			for (int i = num; i <= num2; i++)
			{
				chunkAtCell.GeneratedSliceContentsHashes[i] = 0;
			}
			SubsystemTerrain.TerrainUpdater.DowngradeChunkNeighborhoodState(chunkAtCell.Coords, 0, TerrainChunkState.InvalidVertices1, forceGeometryRegeneration: false);
		}
	}

	public bool TryGetBestAntennaForRadio(int rx, int ry, int rz, out int bestTopY, out int bestHeight)
	{
		bestTopY = 0;
		bestHeight = 0;
		bool flag = false;
		foreach (Point3 basis in _bases)
		{
			ColumnInfo columnInfo = ScanColumnAt(basis.X, basis.Y, basis.Z, treatMissingAsAntenna: false);
			if (columnInfo.Height > 0)
			{
				int num = SideLengthFromHeight(columnInfo.Height) / 2;
				if (rx >= columnInfo.X - num && rx <= columnInfo.X + num && rz >= columnInfo.Z - num && rz <= columnInfo.Z + num && (!flag || columnInfo.TopY > bestTopY))
				{
					flag = true;
					bestTopY = columnInfo.TopY;
					bestHeight = columnInfo.Height;
				}
			}
		}
		return flag;
	}

	public static int GetTracksAvailableByTopY(int topY)
	{
		if (topY < 65)
		{
			return 0;
		}
		float x = (float)(topY - 65) / 190f;
		x = MathUtils.Saturate(x);
		return MathUtils.Clamp(1 + (int)MathUtils.Floor(7f * x), 1, 7);
	}
}
