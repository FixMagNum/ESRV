using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Engine;
using Engine.Graphics;
using Engine.Media;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentWorldEditIntegration : Component, IUpdateable, IDrawable, IWorldEditOverlayContext
{
	private enum WeActionType
	{
		RegionChange,
		SetP1,
		SetP2,
		SetP3,
		CopyToMemory,
		MemChange
	}

	private struct CellDelta
	{
		public int X;

		public int Y;

		public int Z;

		public int Before;

		public int After;
	}

	private class WeAction
	{
		public WeActionType Type;

		public List<CellDelta> Deltas;

		public TerrainRaycastResult OldRay;

		public TerrainRaycastResult NewRay;

		public List<BlockMem> OldMem;

		public List<BlockMem> NewMem;

		public Dictionary<Point3, EntitySnapshot> OldMemEntities;

		public Dictionary<Point3, EntitySnapshot> NewMemEntities;

		public Dictionary<int, FurnitureSnapshot> OldMemFurniture;

		public Dictionary<int, FurnitureSnapshot> NewMemFurniture;
	}

	public class EntitySnapshot
	{
		public Guid Owner;

		public List<InventorySnapshot> Inventories = new List<InventorySnapshot>();
	}

	public class InventorySnapshot
	{
		public string TypeName;

		public int[] Values;

		public int[] Counts;
	}

	private enum WeAccess
	{
		Disabled,
		Limited,
		Full
	}

	private class FurnitureSnapshot
	{
		public int Contents;

		public int[] OriginalIndices;

		public int[] CurrentIndices;

		public FurnitureDesign HeadClone;

		public bool IsLightEmitter;

		public int ShadowStrengthFactor;
	}

	public struct EntitySnapshotOp
	{
		public Point3 Position;

		public int Value;

		public EntitySnapshot Snapshot;
	}

	private class PendingBatch
	{
		public List<Point3> Coords;

		public List<int> Values;

		public bool IsClient;
	}

	private ComponentPlayer m_componentPlayer;

	private SubsystemTerrain m_subsystemTerrain;

	private SubsystemGameInfo m_subsystemGameInfo;

	private BitmapButtonWidget _weF1;

	private BitmapButtonWidget _weF2;

	private BitmapButtonWidget _weF3;

	private BitmapButtonWidget _weF5;

	private BitmapButtonWidget _weF6;

	private BitmapButtonWidget _weF7;

	private BitmapButtonWidget _weF8;

	private BitmapButtonWidget _weF9;

	private BitmapButtonWidget _weF10;

	private BitmapButtonWidget _weF11;

	private BitmapButtonWidget _weToggle;

	private StackPanelWidget _weTop;

	private StackPanelWidget _weBottom;

	private bool _uiLoaded;

	private LookControlMode _oldLook;

	private readonly PrimitivesRenderer3D _pr3D;

	private static TerrainRaycastResult s_p1;

	private static TerrainRaycastResult s_p2;

	private static TerrainRaycastResult s_p3;

	private static int s_selectedBlock;

	private static int s_replaceableBlock;

	private static int s_memCount;

	private static readonly List<BlockMem> s_mem;

	private BitmapButtonWidget _weUndo;

	private BitmapButtonWidget _weRedo;

	private readonly List<WeAction> _undoList;

	private readonly List<WeAction> _redoList;

	private List<CellDelta> _txDeltas;

	private int _historyCapacity;

	private static readonly Color GhostTint;

	private static readonly Vector3[][] s_faceCorners;

	private static readonly Point3[] s_faceToDelta;

	private static readonly int[] s_blockFaceIndexByGhostFace;

	private static readonly Vector3[] s_faceNormalsVec;

	private BitmapButtonWidget _btnOverlay;

	private BitmapButtonWidget _btnRotV;

	private BitmapButtonWidget _btnRotH;

	private BitmapButtonWidget _btnMirrorV;

	private BitmapButtonWidget _btnMirrorH;

	private bool _overlayEnabled = true;

	private static readonly HashSet<int> s_memDropIds;

	private SubsystemBlockEntities m_subsystemBlockEntities;

	private static Dictionary<Point3, EntitySnapshot> s_memEntities;

	private static readonly CultureInfo s_DotCulture;

	private ComponentWorldEditOverlay _overlay;

	private static readonly int[] s_paletteToBlockId;

	private static Dictionary<int, FurnitureSnapshot> s_memFurniture;

	private List<EntitySnapshotOp> _pendingEntityOps;

	private Queue<PendingBatch> _pendingBatches = new Queue<PendingBatch>();

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public int[] DrawOrders => new int[1] { 1000 };

	public int HistoryCapacity
	{
		get
		{
			return _historyCapacity;
		}
		set
		{
			SetHistoryCapacity(value);
		}
	}

	public bool HasP1 => !object.Equals(s_p1, default(TerrainRaycastResult));

	public bool HasP2 => !object.Equals(s_p2, default(TerrainRaycastResult));

	public bool HasP3 => !object.Equals(s_p3, default(TerrainRaycastResult));

	public TerrainRaycastResult P1 => s_p1;

	public TerrainRaycastResult P2 => s_p2;

	public TerrainRaycastResult P3 => s_p3;

	public int MemCount => s_memCount;

	public IReadOnlyList<BlockMem> Memory => s_mem;

	public SubsystemTerrain SubsystemTerrain => m_subsystemTerrain;

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		m_componentPlayer = Entity.FindComponent<ComponentPlayer>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		if (m_componentPlayer?.PlayerData == null || !m_componentPlayer.PlayerData.IsMainPlayer)
		{
			_overlay = Entity.FindComponent<ComponentWorldEditOverlay>(throwOnError: false);
			if (_overlay != null)
			{
				_overlay.SetFlags(isMenuOpen: false, phantomEnabled: false);
				_overlay.IsMenuOpen = false;
				_overlay.Context = null;
			}
			_uiLoaded = false;
			return;
		}
		GameWidget gameWidget = m_componentPlayer.GameWidget;
		_weF1 = gameWidget.Children.Find<BitmapButtonWidget>("F1");
		_weF2 = gameWidget.Children.Find<BitmapButtonWidget>("F2");
		_weF3 = gameWidget.Children.Find<BitmapButtonWidget>("F3");
		_weF5 = gameWidget.Children.Find<BitmapButtonWidget>("F5");
		_weF6 = gameWidget.Children.Find<BitmapButtonWidget>("F6");
		_weF7 = gameWidget.Children.Find<BitmapButtonWidget>("F7");
		_weF8 = gameWidget.Children.Find<BitmapButtonWidget>("F8");
		_weF9 = gameWidget.Children.Find<BitmapButtonWidget>("F9");
		_weF10 = gameWidget.Children.Find<BitmapButtonWidget>("F10");
		_weF11 = gameWidget.Children.Find<BitmapButtonWidget>("F11");
		_weToggle = gameWidget.Children.Find<BitmapButtonWidget>("WorldEditMenu");
		m_subsystemBlockEntities = Project.FindSubsystem<SubsystemBlockEntities>(throwOnError: true);
		_weTop = gameWidget.Children.Find<StackPanelWidget>("WorldEditMenuContainerTop");
		_weBottom = gameWidget.Children.Find<StackPanelWidget>("WorldEditMenuContainerBottom");
		_weUndo = gameWidget.Children.Find<BitmapButtonWidget>("UndoButton");
		_weRedo = gameWidget.Children.Find<BitmapButtonWidget>("RedoButton");
		_historyCapacity = MathUtils.Clamp(valuesDictionary.GetValue("WE_UndoHistoryCapacity", 10), 1, 100);
		_btnOverlay = gameWidget.Children.Find<BitmapButtonWidget>("ButtonOverlay");
		_btnRotV = gameWidget.Children.Find<BitmapButtonWidget>("ButtonRotationVertical");
		_btnRotH = gameWidget.Children.Find<BitmapButtonWidget>("ButtonRotationHorizontal");
		_btnMirrorV = gameWidget.Children.Find<BitmapButtonWidget>("ButtonMirrorVertical");
		_btnMirrorH = gameWidget.Children.Find<BitmapButtonWidget>("ButtonMirrorHorizontal");
		if (_btnOverlay != null)
		{
			_btnOverlay.IsAutoCheckingEnabled = true;
			_btnOverlay.IsChecked = true;
			_overlayEnabled = true;
		}
		TrimHistory(_undoList, _historyCapacity);
		TrimHistory(_redoList, _historyCapacity);
		_overlay = Entity.FindComponent<ComponentWorldEditOverlay>(throwOnError: false);
		if (_overlay == null)
		{
			_overlay = new ComponentWorldEditOverlay();
			Entity.Components.Add(_overlay);
		}
		_overlay.Context = this;
		_overlay.SetFlags(_weToggle != null && _weToggle.IsChecked, _overlayEnabled);
		_uiLoaded = true;
		if (GetAccess() == WeAccess.Disabled)
		{
			if (_weToggle != null)
			{
				_weToggle.IsChecked = false;
			}
			if (_overlay != null)
			{
				_overlay.SetFlags(isMenuOpen: false, phantomEnabled: false);
				_overlay.IsMenuOpen = false;
			}
		}
		UpdateUndoRedoButtons();
	}

	public void Update(float dt)
	{
		if (_pendingBatches != null && _pendingBatches.Count > 0)
		{
			PendingBatch pendingBatch = _pendingBatches.Dequeue();
			try
			{
				if (CommonLib.WorkType != WorkType.Client)
				{
					for (int i = 0; i < pendingBatch.Coords.Count; i++)
					{
						Point3 point = pendingBatch.Coords[i];
						m_subsystemTerrain.ChangeCellNoSend(point.X, point.Y, point.Z, pendingBatch.Values[i]);
					}
				}
				CommonLib.Net.QueuePackage(new SubsystemTerrainPackage(pendingBatch.Coords, pendingBatch.Values, pendingBatch.IsClient));
				if (_pendingBatches.Count == 0 && m_componentPlayer != null && m_componentPlayer.ComponentGui != null)
				{
					m_componentPlayer.ComponentGui.DisplaySmallMessage("Массовая операция WorldEdit завершена.", Color.LightGreen, blinking: false, playNotificationSound: false);
				}
			}
			catch (Exception message)
			{
				Log.Error(message);
			}
		}
		if (!_uiLoaded || m_componentPlayer == null || m_componentPlayer.PlayerData == null || !m_componentPlayer.PlayerData.IsMainPlayer)
		{
			return;
		}
		WidgetInput input = m_componentPlayer.GameWidget.Input;
		WeAccess access = GetAccess();
		if (access == WeAccess.Disabled)
		{
			if (_weToggle != null)
			{
				_weToggle.IsChecked = false;
			}
			if (_overlay != null)
			{
				_overlay.SetFlags(isMenuOpen: false, phantomEnabled: false);
				_overlay.IsMenuOpen = false;
			}
			return;
		}
		if (KeyBindings.IsActionDownOnce(input, KeyAction.WeToggleWe) && _weToggle != null)
		{
			_weToggle.IsChecked = !_weToggle.IsChecked;
			if (_weToggle.IsChecked)
			{
				_oldLook = SettingsManager.LookControlMode;
				SettingsManager.LookControlMode = LookControlMode.SplitTouch;
			}
			else
			{
				SettingsManager.LookControlMode = _oldLook;
			}
		}
		if (_weToggle != null && _weToggle.IsClicked)
		{
			if (_weToggle.IsChecked)
			{
				_oldLook = SettingsManager.LookControlMode;
				SettingsManager.LookControlMode = LookControlMode.SplitTouch;
			}
			else
			{
				SettingsManager.LookControlMode = _oldLook;
			}
		}
		if (_overlay != null)
		{
			_overlay.IsMenuOpen = _weToggle != null && _weToggle.IsChecked;
		}
		if (KeyOrClick(input, KeyAction.WeSetP1, _weF1) && TryRay(out var hit))
		{
			TerrainRaycastResult oldRay = s_p1;
			TerrainRaycastResult terrainRaycastResult = hit;
			if (HasP2)
			{
				int weMaxBlocksAllowed = GetWeMaxBlocksAllowed();
				if (weMaxBlocksAllowed != int.MaxValue)
				{
					int x = s_p2.CellFace.X;
					int y = s_p2.CellFace.Y;
					int z = s_p2.CellFace.Z;
					int num = terrainRaycastResult.CellFace.X;
					int num2 = terrainRaycastResult.CellFace.Y;
					int num3 = terrainRaycastResult.CellFace.Z;
					int num4 = Math.Abs(num - x) + 1;
					int num5 = Math.Abs(num2 - y) + 1;
					int num6 = Math.Abs(num3 - z) + 1;
					long num7 = (long)num4 * (long)num5 * num6;
					int j;
					for (j = 0; j < 3; j++)
					{
						if (num7 <= weMaxBlocksAllowed)
						{
							break;
						}
						if (num4 >= num5 && num4 >= num6)
						{
							int num8 = Math.Max(1, weMaxBlocksAllowed / Math.Max(1, num5 * num6));
							if (num8 < num4)
							{
								num4 = num8;
								num = ((num < x) ? (x - (num4 - 1)) : (x + (num4 - 1)));
							}
						}
						else if (num5 >= num4 && num5 >= num6)
						{
							int num9 = Math.Max(1, weMaxBlocksAllowed / Math.Max(1, num4 * num6));
							if (num9 < num5)
							{
								num5 = num9;
								num2 = ((num2 < y) ? (y - (num5 - 1)) : (y + (num5 - 1)));
							}
						}
						else
						{
							int num10 = Math.Max(1, weMaxBlocksAllowed / Math.Max(1, num4 * num5));
							if (num10 < num6)
							{
								num6 = num10;
								num3 = ((num3 < z) ? (z - (num6 - 1)) : (z + (num6 - 1)));
							}
						}
						num7 = (long)num4 * (long)num5 * num6;
					}
					if (j > 0)
					{
						m_componentPlayer.ComponentGui.DisplaySmallMessage(string.Format(LanguageControl.Get("WE_Notifications", "selection_clamped"), Fmt(weMaxBlocksAllowed), num4, num5, num6), Color.Orange, blinking: true, playNotificationSound: true);
					}
					terrainRaycastResult.CellFace = new CellFace
					{
						X = num,
						Y = num2,
						Z = num3
					};
				}
			}
			s_p1 = terrainRaycastResult;
			s_selectedBlock = m_subsystemTerrain.Terrain.GetCellValue(s_p1.CellFace.X, s_p1.CellFace.Y, s_p1.CellFace.Z);
			PushUndo(new WeAction
			{
				Type = WeActionType.SetP1,
				OldRay = oldRay,
				NewRay = s_p1
			});
			ClearRedoHistory();
			UpdateUndoRedoButtons();
			m_componentPlayer.ComponentGui.DisplaySmallMessage(string.Format(LanguageControl.Get("WE_Notifications", "pos1_set"), s_p1.CellFace.X, s_p1.CellFace.Y, s_p1.CellFace.Z, s_selectedBlock), Color.SkyBlue, blinking: false, playNotificationSound: false);
		}
		if (KeyOrClick(input, KeyAction.WeSetP2, _weF2) && TryRay(out hit))
		{
			TerrainRaycastResult oldRay2 = s_p2;
			TerrainRaycastResult terrainRaycastResult2 = hit;
			if (HasP1)
			{
				int weMaxBlocksAllowed2 = GetWeMaxBlocksAllowed();
				if (weMaxBlocksAllowed2 != int.MaxValue)
				{
					int x2 = s_p1.CellFace.X;
					int y2 = s_p1.CellFace.Y;
					int z2 = s_p1.CellFace.Z;
					int num11 = terrainRaycastResult2.CellFace.X;
					int num12 = terrainRaycastResult2.CellFace.Y;
					int num13 = terrainRaycastResult2.CellFace.Z;
					int num14 = Math.Abs(num11 - x2) + 1;
					int num15 = Math.Abs(num12 - y2) + 1;
					int num16 = Math.Abs(num13 - z2) + 1;
					long num17 = (long)num14 * (long)num15 * num16;
					int k;
					for (k = 0; k < 3; k++)
					{
						if (num17 <= weMaxBlocksAllowed2)
						{
							break;
						}
						if (num14 >= num15 && num14 >= num16)
						{
							int num18 = Math.Max(1, weMaxBlocksAllowed2 / Math.Max(1, num15 * num16));
							if (num18 < num14)
							{
								num14 = num18;
								num11 = ((num11 < x2) ? (x2 - (num14 - 1)) : (x2 + (num14 - 1)));
							}
						}
						else if (num15 >= num14 && num15 >= num16)
						{
							int num19 = Math.Max(1, weMaxBlocksAllowed2 / Math.Max(1, num14 * num16));
							if (num19 < num15)
							{
								num15 = num19;
								num12 = ((num12 < y2) ? (y2 - (num15 - 1)) : (y2 + (num15 - 1)));
							}
						}
						else
						{
							int num20 = Math.Max(1, weMaxBlocksAllowed2 / Math.Max(1, num14 * num15));
							if (num20 < num16)
							{
								num16 = num20;
								num13 = ((num13 < z2) ? (z2 - (num16 - 1)) : (z2 + (num16 - 1)));
							}
						}
						num17 = (long)num14 * (long)num15 * num16;
					}
					if (k > 0)
					{
						m_componentPlayer.ComponentGui.DisplaySmallMessage(string.Format(LanguageControl.Get("WE_Notifications", "selection_clamped"), Fmt(weMaxBlocksAllowed2), num14, num15, num16), Color.Orange, blinking: true, playNotificationSound: true);
					}
					terrainRaycastResult2.CellFace = new CellFace
					{
						X = num11,
						Y = num12,
						Z = num13
					};
				}
			}
			s_p2 = terrainRaycastResult2;
			s_replaceableBlock = m_subsystemTerrain.Terrain.GetCellValue(s_p2.CellFace.X, s_p2.CellFace.Y, s_p2.CellFace.Z);
			PushUndo(new WeAction
			{
				Type = WeActionType.SetP2,
				OldRay = oldRay2,
				NewRay = s_p2
			});
			ClearRedoHistory();
			UpdateUndoRedoButtons();
			m_componentPlayer.ComponentGui.DisplaySmallMessage(string.Format(LanguageControl.Get("WE_Notifications", "pos2_set"), s_p2.CellFace.X, s_p2.CellFace.Y, s_p2.CellFace.Z, s_replaceableBlock), Color.LightYellow, blinking: false, playNotificationSound: false);
		}
		if (KeyOrClick(input, KeyAction.WeSetP3, _weF3) && TryRay(out hit))
		{
			TerrainRaycastResult oldRay3 = s_p3;
			s_p3 = hit;
			int weMaxBlocksAllowed3 = GetWeMaxBlocksAllowed();
			if (weMaxBlocksAllowed3 != int.MaxValue && s_memCount > 0)
			{
				GetMemBounds(out var min, out var max);
				int num21 = max.X - min.X + 1;
				int num22 = max.Y - min.Y + 1;
				int num23 = max.Z - min.Z + 1;
				if ((long)num21 * (long)num22 * num23 > weMaxBlocksAllowed3)
				{
					int num24 = num21;
					int num25 = num22;
					int num26 = num23;
					int l;
					for (l = 0; l < 3; l++)
					{
						if ((long)num24 * (long)num25 * num26 <= weMaxBlocksAllowed3)
						{
							break;
						}
						if (num24 >= num25 && num24 >= num26)
						{
							int num27 = Math.Max(1, weMaxBlocksAllowed3 / Math.Max(1, num25 * num26));
							if (num27 < num24)
							{
								num24 = num27;
							}
						}
						else if (num25 >= num24 && num25 >= num26)
						{
							int num28 = Math.Max(1, weMaxBlocksAllowed3 / Math.Max(1, num24 * num26));
							if (num28 < num25)
							{
								num25 = num28;
							}
						}
						else
						{
							int num29 = Math.Max(1, weMaxBlocksAllowed3 / Math.Max(1, num24 * num25));
							if (num29 < num26)
							{
								num26 = num29;
							}
						}
					}
					if (l > 0)
					{
						m_componentPlayer.ComponentGui.DisplaySmallMessage(string.Format(LanguageControl.Get("WE_Notifications", "selection_clamped"), Fmt(weMaxBlocksAllowed3), num24, num25, num26), Color.Orange, blinking: true, playNotificationSound: true);
					}
					int x3 = min.X;
					int y3 = min.Y;
					int z3 = min.Z;
					int num30 = x3 + num24 - 1;
					int num31 = y3 + num25 - 1;
					int num32 = z3 + num26 - 1;
					List<BlockMem> list = CloneMemList(s_mem);
					Dictionary<Point3, EntitySnapshot> oldMemEntities = CloneEntitiesDict(s_memEntities);
					Dictionary<int, FurnitureSnapshot> oldMemFurniture = CloneFurnitureDict(s_memFurniture);
					for (int num33 = s_mem.Count - 1; num33 >= 0; num33--)
					{
						BlockMem blockMem = s_mem[num33];
						if (blockMem.x < x3 || blockMem.x > num30 || blockMem.y < y3 || blockMem.y > num31 || blockMem.z < z3 || blockMem.z > num32)
						{
							s_mem.RemoveAt(num33);
						}
					}
					List<Point3> list2 = null;
					foreach (KeyValuePair<Point3, EntitySnapshot> s_memEntity in s_memEntities)
					{
						Point3 key = s_memEntity.Key;
						if (key.X < x3 || key.X > num30 || key.Y < y3 || key.Y > num31 || key.Z < z3 || key.Z > num32)
						{
							if (list2 == null)
							{
								list2 = new List<Point3>();
							}
							list2.Add(key);
						}
					}
					if (list2 != null)
					{
						for (int m = 0; m < list2.Count; m++)
						{
							s_memEntities.Remove(list2[m]);
						}
					}
					s_memCount = s_mem.Count;
					if (s_memCount != list.Count)
					{
						List<BlockMem> newMem = CloneMemList(s_mem);
						Dictionary<Point3, EntitySnapshot> newMemEntities = CloneEntitiesDict(s_memEntities);
						Dictionary<int, FurnitureSnapshot> newMemFurniture = CloneFurnitureDict(s_memFurniture);
						PushUndo(new WeAction
						{
							Type = WeActionType.MemChange,
							OldMem = list,
							NewMem = newMem,
							OldMemEntities = oldMemEntities,
							NewMemEntities = newMemEntities,
							OldMemFurniture = oldMemFurniture,
							NewMemFurniture = newMemFurniture
						});
						InvalidateGhostCache();
					}
				}
			}
			PushUndo(new WeAction
			{
				Type = WeActionType.SetP3,
				OldRay = oldRay3,
				NewRay = s_p3
			});
			ClearRedoHistory();
			UpdateUndoRedoButtons();
			m_componentPlayer.ComponentGui.DisplaySmallMessage(string.Format(LanguageControl.Get("WE_Notifications", "pos3_set"), s_p3.CellFace.X, s_p3.CellFace.Y, s_p3.CellFace.Z), Color.Red, blinking: false, playNotificationSound: false);
		}
		if (KeyOrClick(input, KeyAction.WeCopyToMemory, _weF9))
		{
			WE_CopyToMemory(m_subsystemTerrain);
		}
		if (access == WeAccess.Full && KeyOrClick(input, KeyAction.WePasteFromMemory, _weF10))
		{
			WE_PasteFromMemory(m_subsystemTerrain);
		}
		if (access == WeAccess.Full && KeyOrClick(input, KeyAction.WeEmbedFromMemory, _weF5))
		{
			WE_EmbedFromMemory(m_subsystemTerrain);
		}
		if (access == WeAccess.Full && KeyOrClick(input, KeyAction.WeFillRegion, _weF6))
		{
			WE_FillRegion(m_subsystemTerrain, s_selectedBlock);
		}
		if (access == WeAccess.Full && KeyOrClick(input, KeyAction.WeReplaceInRegion, _weF7))
		{
			WE_ReplaceInRegion(m_subsystemTerrain, s_replaceableBlock, s_selectedBlock);
		}
		if (access == WeAccess.Full && KeyOrClick(input, KeyAction.WeClearRegion, _weF8))
		{
			WE_ClearRegion(m_subsystemTerrain);
		}
		if (KeyOrClick(input, KeyAction.WeOpenMenu, _weF11))
		{
			WE_OpenMainMenu(m_subsystemTerrain, access == WeAccess.Limited);
		}
		if (access == WeAccess.Full && (KeyBindings.IsActionDownOnce(input, KeyAction.WeUndo) || (_weUndo != null && _weUndo.IsClicked)) && CanUndo())
		{
			PerformUndo();
		}
		if (access == WeAccess.Full && (KeyBindings.IsActionDownOnce(input, KeyAction.WeRedo) || (_weRedo != null && _weRedo.IsClicked)) && CanRedo())
		{
			PerformRedo();
		}
		bool flag = KeyBindings.IsActionDownOnce(input, KeyAction.WeToggleOverlay);
		if (flag || (_btnOverlay != null && _btnOverlay.IsClicked))
		{
			if (flag)
			{
				_overlayEnabled = !_overlayEnabled;
			}
			else
			{
				_overlayEnabled = _btnOverlay.IsChecked;
			}
			if (_btnOverlay != null)
			{
				_btnOverlay.IsChecked = _overlayEnabled;
			}
			if (_overlay != null)
			{
				_overlay.PhantomEnabled = _overlayEnabled;
				_overlay.InvalidateGhostCache();
			}
			m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get("WE_Notifications", _overlayEnabled ? "overlay_on" : "overlay_off"), _overlayEnabled ? Color.LightCyan : Color.LightGray, blinking: false, playNotificationSound: false);
		}
		if (KeyOrClick(input, KeyAction.WeRotateH, _btnRotH))
		{
			RotateMemoryHorizontal();
		}
		if (KeyOrClick(input, KeyAction.WeRotateV, _btnRotV))
		{
			RotateMemoryVertical();
		}
		if (KeyOrClick(input, KeyAction.WeMirrorH, _btnMirrorH))
		{
			MirrorMemoryHorizontal();
		}
		if (KeyOrClick(input, KeyAction.WeMirrorV, _btnMirrorV))
		{
			MirrorMemoryVertical();
		}
		UpdateUndoRedoButtons();
		if (_overlay != null)
		{
			_overlay.SetFlags(_weToggle != null && _weToggle.IsChecked, _overlayEnabled);
		}
	}

	public void Draw(Camera camera, int drawOrder)
	{
	}

	private bool TryRay(out TerrainRaycastResult hit)
	{
		hit = default;
		ComponentPlayer componentPlayer = m_componentPlayer;
		ComponentMiner componentMiner = componentPlayer?.ComponentMiner;
		ComponentBody componentBody = componentPlayer?.ComponentBody;
		Camera camera = (componentPlayer?.GameWidget)?.ActiveCamera;
		if (componentMiner == null || componentBody == null || camera == null || float.IsNaN(camera.ViewPosition.X) || float.IsNaN(camera.ViewDirection.X))
		{
			Vector3 position = componentBody?.Position ?? Vector3.Zero;
			int x = Terrain.ToCell(position.X);
			int y = Terrain.ToCell(position.Y - 0.1f);
			int z = Terrain.ToCell(position.Z);
			hit.CellFace = new CellFace
			{
				X = x,
				Y = y,
				Z = z
			};
			hit.Ray = new Ray3(position, Vector3.UnitY);
			hit.Distance = 0f;
			return true;
		}
		Ray3 ray = new Ray3(camera.ViewPosition, camera.ViewDirection);
		if (componentMiner.Raycast(ray, RaycastMode.Digging, raycastTerrain: true, raycastBodies: false, raycastMovingBlocks: false) is TerrainRaycastResult terrainRaycastResult)
		{
			hit = terrainRaycastResult;
			return true;
		}
		Vector3 position2 = componentBody.Position;
		int x2 = Terrain.ToCell(position2.X);
		int y2 = Terrain.ToCell(position2.Y - 0.1f);
		int z2 = Terrain.ToCell(position2.Z);
		hit.CellFace = new CellFace
		{
			X = x2,
			Y = y2,
			Z = z2
		};
		hit.Ray = new Ray3(position2, Vector3.UnitY);
		hit.Distance = 0f;
		return true;
	}

	private void GetAabb(out Point3 min, out Point3 max)
	{
		int x = Math.Min(s_p1.CellFace.Point.X, s_p2.CellFace.Point.X);
		int y = Math.Min(s_p1.CellFace.Point.Y, s_p2.CellFace.Point.Y);
		int z = Math.Min(s_p1.CellFace.Point.Z, s_p2.CellFace.Point.Z);
		int x2 = Math.Max(s_p1.CellFace.Point.X, s_p2.CellFace.Point.X);
		int y2 = Math.Max(s_p1.CellFace.Point.Y, s_p2.CellFace.Point.Y);
		int z2 = Math.Max(s_p1.CellFace.Point.Z, s_p2.CellFace.Point.Z);
		min = new Point3(x, y, z);
		max = new Point3(x2, y2, z2);
	}

	private bool Check12(string msgIfMissing)
	{
		if (object.Equals(s_p1, default(TerrainRaycastResult)) || object.Equals(s_p2, default(TerrainRaycastResult)))
		{
			m_componentPlayer.ComponentGui.DisplaySmallMessage(msgIfMissing, Color.White, blinking: false, playNotificationSound: false);
			return false;
		}
		return true;
	}

	private bool Check3(string msgIfMissing)
	{
		if (object.Equals(s_p3, default(TerrainRaycastResult)))
		{
			m_componentPlayer.ComponentGui.DisplaySmallMessage(msgIfMissing, Color.White, blinking: false, playNotificationSound: false);
			return false;
		}
		return true;
	}

	private void WE_CopyToMemory(SubsystemTerrain terrain)
	{
		if (!Check12(LanguageControl.Get("WE_Notifications", "missing_p1p2")))
		{
			return;
		}
		List<BlockMem> num = CloneMemList(s_mem);
		Dictionary<Point3, EntitySnapshot> num5 = CloneEntitiesDict(s_memEntities);
		Dictionary<int, FurnitureSnapshot> num6 = CloneFurnitureDict(s_memFurniture);
		s_mem.Clear();
		s_memEntities.Clear();
		s_memFurniture.Clear();
		s_memCount = 0;
		GetAabb(out var num7, out var num8);
		for (int i = num7.X; i <= num8.X; i++)
		{
			for (int j = num7.Y; j <= num8.Y; j++)
			{
				for (int k = num7.Z; k <= num8.Z; k++)
				{
					bool flag = true;
					if (m_componentPlayer != null && m_componentPlayer.PlayerData != null && !m_componentPlayer.PlayerData.ServerManager)
					{
						bool flag2 = SubsystemBedrockBlockBehavior.CheckIsInTerritoriy(i, k, out var territoriy);
						WorldSettings worldSettings = ((m_subsystemGameInfo != null) ? m_subsystemGameInfo.WorldSettings : null);
						if (worldSettings != null && worldSettings.RestrictWorldInteraction && !flag2)
						{
							flag = false;
						}
						else if (flag2 && territoriy != null)
						{
							bool flag3 = territoriy.OwnerGuid == m_componentPlayer.PlayerData.PlayerGUID;
							bool flag4 = m_componentPlayer.PlayerData.Friends.Contains(territoriy.OwnerGuid) && territoriy.ApplyToFirend;
							flag = flag3 | flag4;
						}
					}
					if (!flag)
					{
						continue;
					}
					int cellValue = terrain.Terrain.GetCellValue(i, j, k);
					if (ShouldSkip(cellValue) && Terrain.ExtractContents(cellValue) != 0)
					{
						continue;
					}
					BlockMem blockMem = new BlockMem
					{
						x = i - num7.X,
						y = j - num7.Y,
						z = k - num7.Z,
						id = cellValue
					};
					s_mem.Add(blockMem);
					s_memCount = s_mem.Count;
					if (Terrain.ExtractContents(cellValue) != 0)
					{
						EntitySnapshot entitySnapshot = CaptureEntitySnapshot(i, j, k);
						if (entitySnapshot != null)
						{
							s_memEntities[new Point3(blockMem.x, blockMem.y, blockMem.z)] = entitySnapshot;
						}
					}
				}
			}
		}
		CaptureFurnitureSnapshotsFromMem();
		EnsureFurniturePresentAndRemap();
		List<BlockMem> num9 = CloneMemList(s_mem);
		Dictionary<Point3, EntitySnapshot> num10 = CloneEntitiesDict(s_memEntities);
		Dictionary<int, FurnitureSnapshot> newMemFurniture = CloneFurnitureDict(s_memFurniture);
		PushUndo(new WeAction
		{
			Type = WeActionType.CopyToMemory,
			OldMem = num,
			NewMem = num9,
			OldMemEntities = num5,
			NewMemEntities = num10,
			OldMemFurniture = num6,
			NewMemFurniture = newMemFurniture
		});
		ClearRedoHistory();
		UpdateUndoRedoButtons();
		InvalidateGhostCache();
		GetAabb(out var min, out var max);
		int x = max.X - min.X + 1;
		int y = max.Y - min.Y + 1;
		int z = max.Z - min.Z + 1;
		m_componentPlayer.ComponentGui.DisplaySmallMessage(string.Format(LanguageControl.Get("WE_Notifications", "copied"), Fmt(s_memCount), DimsXYZ(x, y, z)), Color.MintGreen, blinking: false, playNotificationSound: false);
	}

	private void WE_PasteFromMemory(SubsystemTerrain terrain)
	{
		if (!Check3(LanguageControl.Get("WE_Notifications", "missing_p3")))
		{
			return;
		}
		EnsureFurniturePresentAndRemap();
		int weMaxBlocksAllowed = GetWeMaxBlocksAllowed();
		if (weMaxBlocksAllowed != int.MaxValue && s_memCount > 0)
		{
			GetMemBounds(out var min, out var max);
			int num = max.X - min.X + 1;
			int num2 = max.Y - min.Y + 1;
			int num3 = max.Z - min.Z + 1;
			if ((long)num * (long)num2 * num3 > weMaxBlocksAllowed)
			{
				int num4 = num;
				int num5 = num2;
				int num6 = num3;
				for (int i = 0; i < 3; i++)
				{
					if ((long)num4 * (long)num5 * num6 <= weMaxBlocksAllowed)
					{
						break;
					}
					if (num4 >= num5 && num4 >= num6)
					{
						int num7 = Math.Max(1, weMaxBlocksAllowed / Math.Max(1, num5 * num6));
						if (num7 < num4)
						{
							num4 = num7;
						}
					}
					else if (num5 >= num4 && num5 >= num6)
					{
						int num8 = Math.Max(1, weMaxBlocksAllowed / Math.Max(1, num4 * num6));
						if (num8 < num5)
						{
							num5 = num8;
						}
					}
					else
					{
						int num9 = Math.Max(1, weMaxBlocksAllowed / Math.Max(1, num4 * num5));
						if (num9 < num6)
						{
							num6 = num9;
						}
					}
				}
				int x = min.X;
				int y = min.Y;
				int z = min.Z;
				int num10 = x + num4 - 1;
				int num11 = y + num5 - 1;
				int num12 = z + num6 - 1;
				List<BlockMem> list = CloneMemList(s_mem);
				Dictionary<Point3, EntitySnapshot> oldMemEntities = CloneEntitiesDict(s_memEntities);
				Dictionary<int, FurnitureSnapshot> oldMemFurniture = CloneFurnitureDict(s_memFurniture);
				for (int num13 = s_mem.Count - 1; num13 >= 0; num13--)
				{
					BlockMem blockMem = s_mem[num13];
					if (blockMem.x < x || blockMem.x > num10 || blockMem.y < y || blockMem.y > num11 || blockMem.z < z || blockMem.z > num12)
					{
						s_mem.RemoveAt(num13);
					}
				}
				List<Point3> list2 = null;
				foreach (KeyValuePair<Point3, EntitySnapshot> s_memEntity in s_memEntities)
				{
					Point3 key = s_memEntity.Key;
					if (key.X < x || key.X > num10 || key.Y < y || key.Y > num11 || key.Z < z || key.Z > num12)
					{
						if (list2 == null)
						{
							list2 = new List<Point3>();
						}
						list2.Add(key);
					}
				}
				if (list2 != null)
				{
					for (int j = 0; j < list2.Count; j++)
					{
						s_memEntities.Remove(list2[j]);
					}
				}
				s_memCount = s_mem.Count;
				if (s_memCount != list.Count)
				{
					List<BlockMem> newMem = CloneMemList(s_mem);
					Dictionary<Point3, EntitySnapshot> newMemEntities = CloneEntitiesDict(s_memEntities);
					Dictionary<int, FurnitureSnapshot> newMemFurniture = CloneFurnitureDict(s_memFurniture);
					PushUndo(new WeAction
					{
						Type = WeActionType.MemChange,
						OldMem = list,
						NewMem = newMem,
						OldMemEntities = oldMemEntities,
						NewMemEntities = newMemEntities,
						OldMemFurniture = oldMemFurniture,
						NewMemFurniture = newMemFurniture
					});
					InvalidateGhostCache();
				}
			}
		}
		BeginTx();
		Point3 point = s_p3.CellFace.Point;
		int num14 = 0;
		int num15 = 0;
		for (int k = 0; k < s_mem.Count; k++)
		{
			BlockMem blockMem2 = s_mem[k];
			int x2 = point.X + blockMem2.x;
			int y2 = point.Y + blockMem2.y;
			int z2 = point.Z + blockMem2.z;
			bool flag = false;
			if (CommonLib.WorkType != WorkType.Client)
			{
				int num16 = Terrain.ExtractContents(blockMem2.id);
				if (num16 != 0)
				{
					flag = s_memDropIds.Contains(num16) || ((m_subsystemGameInfo != null) ? m_subsystemGameInfo.WorldSettings : null)?.IsBlockDiable(blockMem2.id) == true;
				}
			}
			if (flag)
			{
				num15++;
				continue;
			}
			int cellValue = terrain.Terrain.GetCellValue(x2, y2, z2);
			ChangeCellTracked(terrain, x2, y2, z2, blockMem2.id);
			if (terrain.Terrain.GetCellValue(x2, y2, z2) != cellValue || CommonLib.WorkType == WorkType.Client)
			{
				num14++;
			}
			else
			{
				num15++;
			}
			if (Terrain.ExtractContents(blockMem2.id) != 0 && s_memEntities.TryGetValue(new Point3(blockMem2.x, blockMem2.y, blockMem2.z), out var value))
			{
				Point3 dest = new Point3(x2, y2, z2);
				if (CommonLib.WorkType != WorkType.Client)
				{
					ApplyEntitySnapshotTo(dest, blockMem2.id, value);
				}
				else
				{
					AddPendingEntityOp(dest, blockMem2.id, value);
				}
			}
		}
		EndTxPush("PasteFromMemory");
		if (CommonLib.WorkType == WorkType.Client && _pendingEntityOps != null && _pendingEntityOps.Count > 0)
		{
			CommonLib.Net.QueuePackage(new WorldEditEntitiesPackage(_pendingEntityOps));
			_pendingEntityOps.Clear();
		}
		GetMemBounds(out var min2, out var max2);
		int num17 = max2.X - min2.X + 1;
		int num18 = max2.Y - min2.Y + 1;
		int num19 = max2.Z - min2.Z + 1;
		m_componentPlayer.ComponentGui.DisplaySmallMessage(string.Format(LanguageControl.Get("WE_Notifications", "pasted"), num14.ToString("N0"), $"{num17}x{num18}x{num19}", (num15 > 0) ? string.Format(LanguageControl.Get("WE_Notifications", "skipped_suffix"), num15.ToString("N0")) : string.Empty), Color.LightCyan, blinking: false, playNotificationSound: false);
	}

	private void WE_PasteRegionAtP3(SubsystemTerrain terrain)
	{
		if (!Check12(LanguageControl.Get("WE_Notifications", "missing_p1p2")) || !Check3(LanguageControl.Get("WE_Notifications", "missing_p3")))
		{
			return;
		}
		GetAabb(out var min, out var max);
		BeginTx();
		int num = 0;
		int num2 = 0;
		for (int i = min.Y; i <= max.Y; i++)
		{
			for (int j = min.Z; j <= max.Z; j++)
			{
				for (int k = min.X; k <= max.X; k++)
				{
					int cellValue = terrain.Terrain.GetCellValue(k, i, j);
					int num3 = Terrain.ExtractContents(cellValue);
					if (num3 == 0 || cellValue == 15360)
					{
						num2++;
						continue;
					}
					if (CommonLib.WorkType != WorkType.Client)
					{
						if (s_memDropIds.Contains(num3))
						{
							num2++;
							continue;
						}
						if (((m_subsystemGameInfo != null) ? m_subsystemGameInfo.WorldSettings : null)?.IsBlockDiable(cellValue) == true)
						{
							num2++;
							continue;
						}
					}
					int x = s_p3.CellFace.Point.X + (k - min.X);
					int y = s_p3.CellFace.Point.Y + (i - min.Y);
					int z = s_p3.CellFace.Point.Z + (j - min.Z);
					int cellValue2 = terrain.Terrain.GetCellValue(x, y, z);
					ChangeCellTracked(terrain, x, y, z, cellValue);
					if (terrain.Terrain.GetCellValue(x, y, z) != cellValue2 || CommonLib.WorkType == WorkType.Client)
					{
						num++;
						EntitySnapshot entitySnapshot = CaptureEntitySnapshot(k, i, j);
						if (entitySnapshot != null)
						{
							Point3 dest = new Point3(x, y, z);
							if (CommonLib.WorkType != WorkType.Client)
							{
								ApplyEntitySnapshotTo(dest, cellValue, entitySnapshot);
							}
							else
							{
								AddPendingEntityOp(dest, cellValue, entitySnapshot);
							}
						}
					}
					else
					{
						num2++;
					}
				}
			}
		}
		EndTxPush("PasteRegionAtP3");
		if (CommonLib.WorkType == WorkType.Client && _pendingEntityOps != null && _pendingEntityOps.Count > 0)
		{
			CommonLib.Net.QueuePackage(new WorldEditEntitiesPackage(_pendingEntityOps));
			_pendingEntityOps.Clear();
		}
		int num4 = max.X - min.X + 1;
		int num5 = max.Y - min.Y + 1;
		int num6 = max.Z - min.Z + 1;
		m_componentPlayer.ComponentGui.DisplaySmallMessage(string.Format(LanguageControl.Get("WE_Notifications", "pasted"), num.ToString("N0"), $"{num4}x{num5}x{num6}", (num2 > 0) ? string.Format(LanguageControl.Get("WE_Notifications", "skipped_suffix"), num2.ToString("N0")) : string.Empty), Color.LightCyan, blinking: false, playNotificationSound: false);
	}

	private void WE_FillRegion(SubsystemTerrain terrain, int blockId)
	{
		if (!Check12(LanguageControl.Get("WE_Notifications", "missing_p1p2")))
		{
			return;
		}
		if (CommonLib.WorkType != WorkType.Client && ShouldSkip(blockId))
		{
			((m_componentPlayer != null) ? m_componentPlayer.ComponentGui : null)?.DisplaySmallMessage(LanguageControl.Get("WE_Notifications", "block_disabled_selected"), Color.Red, blinking: true, playNotificationSound: true);
			return;
		}
		GetAabb(out var min, out var max);
		BeginTx();
		for (int i = min.X; i <= max.X; i++)
		{
			for (int j = min.Y; j <= max.Y; j++)
			{
				for (int k = min.Z; k <= max.Z; k++)
				{
					bool flag = true;
					if (m_componentPlayer != null && m_componentPlayer.PlayerData != null && !m_componentPlayer.PlayerData.ServerManager)
					{
						bool flag2 = SubsystemBedrockBlockBehavior.CheckIsInTerritoriy(i, k, out var territoriy);
						WorldSettings worldSettings = ((m_subsystemGameInfo != null) ? m_subsystemGameInfo.WorldSettings : null);
						if (worldSettings != null && worldSettings.RestrictWorldInteraction && !flag2)
						{
							flag = false;
						}
						else if (flag2 && territoriy != null)
						{
							bool flag3 = territoriy.OwnerGuid == m_componentPlayer.PlayerData.PlayerGUID;
							bool flag4 = m_componentPlayer.PlayerData.Friends.Contains(territoriy.OwnerGuid);
							bool applyToFirend = territoriy.ApplyToFirend;
							flag = flag3 || (flag4 & applyToFirend);
						}
					}
					if (flag)
					{
						ChangeCellTracked(terrain, i, j, k, blockId);
					}
				}
			}
		}
		EndTxPush("FillRegion");
	}

	private void WE_ReplaceInRegion(SubsystemTerrain terrain, int fromId, int toId)
	{
		if (!Check12(LanguageControl.Get("WE_Notifications", "missing_p1p2")))
		{
			return;
		}
		if (CommonLib.WorkType != WorkType.Client && ShouldSkip(toId))
		{
			((m_componentPlayer != null) ? m_componentPlayer.ComponentGui : null)?.DisplaySmallMessage(LanguageControl.Get("WE_Notifications", "block_disabled_target"), Color.Red, blinking: true, playNotificationSound: true);
			return;
		}
		GetAabb(out var min, out var max);
		BeginTx();
		for (int i = min.X; i <= max.X; i++)
		{
			for (int j = min.Y; j <= max.Y; j++)
			{
				for (int k = min.Z; k <= max.Z; k++)
				{
					bool flag = true;
					if (m_componentPlayer != null && m_componentPlayer.PlayerData != null && !m_componentPlayer.PlayerData.ServerManager)
					{
						bool flag2 = SubsystemBedrockBlockBehavior.CheckIsInTerritoriy(i, k, out var territoriy);
						WorldSettings worldSettings = ((m_subsystemGameInfo != null) ? m_subsystemGameInfo.WorldSettings : null);
						if (worldSettings != null && worldSettings.RestrictWorldInteraction && !flag2)
						{
							flag = false;
						}
						else if (flag2 && territoriy != null)
						{
							bool flag3 = territoriy.OwnerGuid == m_componentPlayer.PlayerData.PlayerGUID;
							bool flag4 = m_componentPlayer.PlayerData.Friends.Contains(territoriy.OwnerGuid);
							bool applyToFirend = territoriy.ApplyToFirend;
							flag = flag3 || (flag4 & applyToFirend);
						}
					}
					if (flag)
					{
						int cellValue = terrain.Terrain.GetCellValue(i, j, k);
						int num = ((cellValue == fromId) ? toId : cellValue);
						if (num != cellValue)
						{
							ChangeCellTracked(terrain, i, j, k, num);
						}
					}
				}
			}
		}
		EndTxPush("ReplaceInRegion");
	}

	private void WE_ClearRegion(SubsystemTerrain terrain)
	{
		if (!Check12(LanguageControl.Get("WE_Notifications", "missing_p1p2")))
		{
			return;
		}
		GetAabb(out var min, out var max);
		BeginTx();
		for (int i = min.X; i <= max.X; i++)
		{
			for (int j = min.Y; j <= max.Y; j++)
			{
				for (int k = min.Z; k <= max.Z; k++)
				{
					ChangeCellTracked(terrain, i, j, k, 0);
				}
			}
		}
		EndTxPush("ClearRegion");
		int num = max.X - min.X + 1;
		int num2 = max.Y - min.Y + 1;
		int num3 = max.Z - min.Z + 1;
		long num4 = (long)num * (long)num2 * num3;
		m_componentPlayer.ComponentGui.DisplaySmallMessage(string.Format(LanguageControl.Get("WE_Notifications", "pasted"), num4.ToString("N0"), $"{num}x{num2}x{num3}", string.Empty), Color.LightRed, blinking: false, playNotificationSound: false);
	}

	public ComponentWorldEditIntegration()
	{
		_historyCapacity = 10;
		_undoList = new List<WeAction>(10);
		_redoList = new List<WeAction>(10);
		_oldLook = LookControlMode.EntireScreen;
		_pr3D = new PrimitivesRenderer3D();
	}

	static ComponentWorldEditIntegration()
	{
		s_paletteToBlockId = new int[16]
		{
			16456, 49224, 81992, 114760, 147528, 180296, 213064, 245832, 278600, 311368,
			344136, 376904, 409672, 442440, 475208, 507976
		};
		s_DotCulture = CreateDotCulture();
		s_memEntities = new Dictionary<Point3, EntitySnapshot>();
		s_memDropIds = new HashSet<int> { 6, 7 };
		s_mem = new List<BlockMem>();
		s_memFurniture = new Dictionary<int, FurnitureSnapshot>();
	}

	private static List<BlockMem> CloneMemList(List<BlockMem> src)
	{
		List<BlockMem> list = new List<BlockMem>(src.Count);
		for (int i = 0; i < src.Count; i++)
		{
			list.Add(src[i]);
		}
		return list;
	}

	private void BeginTx()
	{
		_txDeltas = new List<CellDelta>(256);
		if (_overlay != null)
		{
			_overlay.SuppressGhost = true;
		}
	}

	private void ChangeCellTracked(SubsystemTerrain terrain, int x, int y, int z, int newVal)
	{
		int num = terrain.Terrain.GetCellValue(x, y, z);
		if (num == newVal)
		{
			return;
		}
		bool num2 = true;
		if (m_componentPlayer != null && m_componentPlayer.PlayerData != null && !m_componentPlayer.PlayerData.ServerManager)
		{
			bool f2 = SubsystemBedrockBlockBehavior.CheckIsInTerritoriy(x, z, out var f3);
			WorldSettings x3 = ((m_subsystemGameInfo != null) ? m_subsystemGameInfo.WorldSettings : null);
			if (x3 != null && x3.RestrictWorldInteraction && !f2)
			{
				num2 = false;
			}
			else if (f2 && f3 != null)
			{
				bool flag = f3.OwnerGuid == m_componentPlayer.PlayerData.PlayerGUID;
				bool x4 = m_componentPlayer.PlayerData.Friends.Contains(f3.OwnerGuid) && f3.ApplyToFirend;
				if (!flag && !x4)
				{
					bool x5 = Terrain.ExtractContents(num) != 0;
					int x7 = Terrain.ExtractContents(newVal);
					bool flag2 = x7 == 0;
					bool flag3 = !x5 && x7 != 0;
					if (flag2)
					{
						num2 = f3.AllowDig;
					}
					else
					{
						num2 = ((!flag3) ? (f3.AllowDig && f3.AllowPlace) : f3.AllowPlace);
					}
				}
			}
		}
		if (num2)
		{
			_txDeltas.Add(new CellDelta
			{
				X = x,
				Y = y,
				Z = z,
				Before = num,
				After = newVal
			});
		}
		else
		{
			m_componentPlayer?.ComponentGui?.DisplaySmallMessage(LanguageControl.Get("WE_Notifications", "no_access_territory"), Color.Orange, blinking: true, playNotificationSound: true);
		}
	}

	private void EndTxPush(string _)
	{
		if (_txDeltas != null && _txDeltas.Count > 0)
		{
			bool isClient = CommonLib.WorkType == WorkType.Client;
			int num = 2000;
			for (int i = 0; i < _txDeltas.Count; i += num)
			{
				int num2 = Math.Min(num, _txDeltas.Count - i);
				List<Point3> list = new List<Point3>(num2);
				List<int> list2 = new List<int>(num2);
				for (int j = 0; j < num2; j++)
				{
					CellDelta cellDelta = _txDeltas[i + j];
					list.Add(new Point3(cellDelta.X, cellDelta.Y, cellDelta.Z));
					list2.Add(cellDelta.After);
				}
				_pendingBatches.Enqueue(new PendingBatch
				{
					Coords = list,
					Values = list2,
					IsClient = isClient
				});
			}
			PushUndo(new WeAction
			{
				Type = WeActionType.RegionChange,
				Deltas = _txDeltas
			});
			ClearRedoHistory();
			UpdateUndoRedoButtons();
		}
		_txDeltas = null;
		if (_overlay != null)
		{
			_overlay.SuppressGhost = false;
			_overlay.InvalidateGhostCache();
		}
	}

	private void PushUndo(WeAction a)
	{
		_undoList.Add(a);
		if (_undoList.Count > _historyCapacity)
		{
			_undoList.RemoveAt(0);
		}
	}

	private WeAction PopUndo()
	{
		if (_undoList.Count == 0)
		{
			return null;
		}
		WeAction result = _undoList[_undoList.Count - 1];
		_undoList.RemoveAt(_undoList.Count - 1);
		return result;
	}

	private void PushRedo(WeAction a)
	{
		_redoList.Add(a);
		if (_redoList.Count > _historyCapacity)
		{
			_redoList.RemoveAt(0);
		}
	}

	private WeAction PopRedo()
	{
		if (_redoList.Count == 0)
		{
			return null;
		}
		WeAction result = _redoList[_redoList.Count - 1];
		_redoList.RemoveAt(_redoList.Count - 1);
		return result;
	}

	private void ClearRedoHistory()
	{
		_redoList.Clear();
	}

	private bool CanUndo()
	{
		return _undoList.Count > 0;
	}

	private bool CanRedo()
	{
		return _redoList.Count > 0;
	}

	private void PerformUndo()
	{
		WeAction weAction = PopUndo();
		if (weAction != null)
		{
			ApplyAction(weAction, undo: true);
			PushRedo(weAction);
			UpdateUndoRedoButtons();
		}
	}

	private void PerformRedo()
	{
		WeAction weAction = PopRedo();
		if (weAction != null)
		{
			ApplyAction(weAction, undo: false);
			PushUndo(weAction);
			UpdateUndoRedoButtons();
		}
	}

	private void ApplyAction(WeAction a, bool undo)
	{
		switch (a.Type)
		{
		case WeActionType.RegionChange:
		{
			if (a.Deltas == null)
			{
				break;
			}
			bool isClient = CommonLib.WorkType == WorkType.Client;
			int num = 2000;
			for (int i = 0; i < a.Deltas.Count; i += num)
			{
				int num2 = Math.Min(num, a.Deltas.Count - i);
				List<Point3> list2 = new List<Point3>(num2);
				List<int> list3 = new List<int>(num2);
				if (undo)
				{
					for (int j = 0; j < num2; j++)
					{
						CellDelta cellDelta = a.Deltas[a.Deltas.Count - 1 - (i + j)];
						list2.Add(new Point3(cellDelta.X, cellDelta.Y, cellDelta.Z));
						list3.Add(cellDelta.Before);
					}
				}
				else
				{
					for (int k = 0; k < num2; k++)
					{
						CellDelta cellDelta2 = a.Deltas[i + k];
						list2.Add(new Point3(cellDelta2.X, cellDelta2.Y, cellDelta2.Z));
						list3.Add(cellDelta2.After);
					}
				}
				_pendingBatches.Enqueue(new PendingBatch
				{
					Coords = list2,
					Values = list3,
					IsClient = isClient
				});
			}
			break;
		}
		case WeActionType.SetP1:
			s_p1 = (undo ? a.OldRay : a.NewRay);
			break;
		case WeActionType.SetP2:
			s_p2 = (undo ? a.OldRay : a.NewRay);
			break;
		case WeActionType.SetP3:
			s_p3 = (undo ? a.OldRay : a.NewRay);
			EnsureFurniturePresentAndRemap();
			InvalidateGhostCache();
			break;
		case WeActionType.CopyToMemory:
		case WeActionType.MemChange:
		{
			List<BlockMem> list = (undo ? a.OldMem : a.NewMem);
			Dictionary<Point3, EntitySnapshot> dictionary = (undo ? a.OldMemEntities : a.NewMemEntities);
			Dictionary<int, FurnitureSnapshot> dictionary2 = (undo ? a.OldMemFurniture : a.NewMemFurniture);
			s_mem.Clear();
			if (list != null && list.Count > 0)
			{
				s_mem.AddRange(list);
			}
			s_memCount = s_mem.Count;
			if (dictionary != null)
			{
				s_memEntities = CloneEntitiesDict(dictionary);
			}
			else
			{
				s_memEntities.Clear();
			}
			if (dictionary2 != null)
			{
				s_memFurniture = CloneFurnitureDict(dictionary2);
			}
			else
			{
				s_memFurniture.Clear();
			}
			EnsureFurniturePresentAndRemap();
			InvalidateGhostCache();
			break;
		}
		}
	}

	private void UpdateUndoRedoButtons()
	{
		bool flag = GetAccess() == WeAccess.Full;
		SetBtnState(_weUndo, flag && CanUndo());
		SetBtnState(_weRedo, flag && CanRedo());
	}

	private void SetBtnState(BitmapButtonWidget btn, bool enabled)
	{
		if (btn != null)
		{
			btn.IsEnabled = enabled;
			btn.ColorTransform = Color.White * (enabled ? 1f : 0.5f);
		}
	}

	public override void Save(ValuesDictionary values, EntityToIdMap idMap)
	{
		base.Save(values, idMap);
		values.SetValue("WE_UndoHistoryCapacity", _historyCapacity);
	}

	private void SetHistoryCapacity(int cap)
	{
		cap = MathUtils.Clamp(cap, 1, 100);
		if (cap != _historyCapacity)
		{
			_historyCapacity = cap;
			TrimHistory(_undoList, _historyCapacity);
			TrimHistory(_redoList, _historyCapacity);
			UpdateUndoRedoButtons();
		}
	}

	private static void TrimHistory(List<WeAction> list, int cap)
	{
		int num = list.Count - cap;
		if (num > 0)
		{
			list.RemoveRange(0, num);
		}
	}

	private void InvalidateGhostCache()
	{
		if (_overlay != null)
		{
			_overlay.InvalidateGhostCache();
		}
	}

	private void TransformMemory(Func<BlockMem, BlockMem> map, string actionUiName)
	{
		if (s_memCount <= 0)
		{
			m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get("WE_Notifications", "mem_empty"), Color.LightGray, blinking: true, playNotificationSound: true);
			return;
		}
		List<BlockMem> num = CloneMemList(s_mem);
		Dictionary<Point3, EntitySnapshot> dictionary = CloneEntitiesDict(s_memEntities);
		Dictionary<int, FurnitureSnapshot> oldMemFurniture = CloneFurnitureDict(s_memFurniture);
		Dictionary<Point3, int> dictionary2 = new Dictionary<Point3, int>(num.Count);
		for (int i = 0; i < num.Count; i++)
		{
			BlockMem blockMem = num[i];
			dictionary2[new Point3(blockMem.x, blockMem.y, blockMem.z)] = blockMem.id;
		}
		List<BlockMem> list = new List<BlockMem>(num.Count);
		int num2 = int.MaxValue;
		int num3 = int.MaxValue;
		int num4 = int.MaxValue;
		for (int j = 0; j < num.Count; j++)
		{
			BlockMem blockMem2 = map(num[j]);
			list.Add(blockMem2);
			if (blockMem2.x < num2)
			{
				num2 = blockMem2.x;
			}
			if (blockMem2.y < num3)
			{
				num3 = blockMem2.y;
			}
			if (blockMem2.z < num4)
			{
				num4 = blockMem2.z;
			}
		}
		if (num2 == int.MaxValue)
		{
			num2 = 0;
			num3 = 0;
			num4 = 0;
		}
		Dictionary<Point3, EntitySnapshot> dictionary3 = new Dictionary<Point3, EntitySnapshot>(dictionary.Count);
		foreach (KeyValuePair<Point3, EntitySnapshot> item in dictionary)
		{
			Point3 key = item.Key;
			int value = 0;
			dictionary2.TryGetValue(key, out value);
			BlockMem blockMem3 = map(new BlockMem
			{
				x = key.X,
				y = key.Y,
				z = key.Z,
				id = value
			});
			Point3 key2 = new Point3(blockMem3.x, blockMem3.y, blockMem3.z);
			dictionary3[key2] = item.Value;
		}
		s_mem.Clear();
		for (int k = 0; k < list.Count; k++)
		{
			BlockMem blockMem4 = list[k];
			s_mem.Add(new BlockMem
			{
				x = blockMem4.x - num2,
				y = blockMem4.y - num3,
				z = blockMem4.z - num4,
				id = blockMem4.id
			});
		}
		s_memCount = s_mem.Count;
		s_memEntities.Clear();
		foreach (KeyValuePair<Point3, EntitySnapshot> item2 in dictionary3)
		{
			Point3 key3 = item2.Key;
			Point3 key4 = new Point3(key3.X - num2, key3.Y - num3, key3.Z - num4);
			s_memEntities[key4] = item2.Value;
		}
		EnsureFurniturePresentAndRemap();
		List<BlockMem> newMem = CloneMemList(s_mem);
		Dictionary<Point3, EntitySnapshot> newMemEntities = CloneEntitiesDict(s_memEntities);
		Dictionary<int, FurnitureSnapshot> newMemFurniture = CloneFurnitureDict(s_memFurniture);
		PushUndo(new WeAction
		{
			Type = WeActionType.MemChange,
			OldMem = num,
			NewMem = newMem,
			OldMemEntities = dictionary,
			NewMemEntities = newMemEntities,
			OldMemFurniture = oldMemFurniture,
			NewMemFurniture = newMemFurniture
		});
		ClearRedoHistory();
		UpdateUndoRedoButtons();
		InvalidateGhostCache();
		GetMemBounds(out var _, out var _);
		m_componentPlayer.ComponentGui.DisplaySmallMessage(string.Format(LanguageControl.Get("WE_Notifications", "transform_ok"), actionUiName, Fmt(s_memCount)), Color.LightGreen, blinking: false, playNotificationSound: false);
	}

	private void RotateMemoryHorizontal()
	{
		TransformMemory((BlockMem b) =>
		{
			int id = TransformBlockRotateY90(b.id);
			return new BlockMem
			{
				x = b.z,
				y = b.y,
				z = -b.x,
				id = id
			};
		}, LanguageControl.Get("WE_Dialogs", "transform_rotate_h"));
	}

	private void RotateMemoryVertical()
	{
		TransformMemory((BlockMem b) =>
		{
			int id = TransformBlockRotateX90(b.id);
			return new BlockMem
			{
				x = b.x,
				y = b.z,
				z = -b.y,
				id = id
			};
		}, LanguageControl.Get("WE_Dialogs", "transform_rotate_v"));
	}

	private void MirrorMemoryHorizontal()
	{
		TransformMemory((BlockMem b) =>
		{
			int id = TransformBlockMirrorX(b.id);
			return new BlockMem
			{
				x = -b.x,
				y = b.y,
				z = b.z,
				id = id
			};
		}, LanguageControl.Get("WE_Dialogs", "transform_mirror_h"));
	}

	private void MirrorMemoryVertical()
	{
		TransformMemory((BlockMem b) =>
		{
			int id = TransformBlockMirrorY(b.id);
			return new BlockMem
			{
				x = b.x,
				y = -b.y,
				z = b.z,
				id = id
			};
		}, LanguageControl.Get("WE_Dialogs", "transform_mirror_v"));
	}

	public void GetMemBounds(out Point3 min, out Point3 max)
	{
		min = new Point3(int.MaxValue, int.MaxValue, int.MaxValue);
		max = new Point3(int.MinValue, int.MinValue, int.MinValue);
		if (s_memCount <= 0)
		{
			min = new Point3(0, 0, 0);
			max = new Point3(0, 0, 0);
			return;
		}
		for (int i = 0; i < s_mem.Count; i++)
		{
			BlockMem blockMem = s_mem[i];
			if (blockMem.x < min.X)
			{
				min.X = blockMem.x;
			}
			if (blockMem.y < min.Y)
			{
				min.Y = blockMem.y;
			}
			if (blockMem.z < min.Z)
			{
				min.Z = blockMem.z;
			}
			if (blockMem.x > max.X)
			{
				max.X = blockMem.x;
			}
			if (blockMem.y > max.Y)
			{
				max.Y = blockMem.y;
			}
			if (blockMem.z > max.Z)
			{
				max.Z = blockMem.z;
			}
		}
	}

	private static int TransformBlockRotateY90(int value)
	{
		int num = Terrain.ExtractContents(value);
		int num2 = Terrain.ExtractData(value);
		switch (num)
		{
		case 216:
		{
			int direction = DispenserBlock.GetDirection(num2);
			if (direction >= 0 && direction <= 3)
			{
				direction = (direction + 1) & 3;
				num2 = DispenserBlock.SetDirection(num2, direction);
			}
			return Terrain.MakeBlockValue(num, 0, num2);
		}
		case 237:
		{
			int data6 = Terrain.ExtractData(value);
			int num15 = PistonBlock.GetFace(data6);
			if (num15 >= 0 && num15 <= 3)
			{
				num15 = (num15 + 1) & 3;
			}
			data6 = PistonBlock.SetFace(data6, num15);
			return Terrain.MakeBlockValue(num, 0, data6);
		}
		case 133:
		{
			int data4 = Terrain.ExtractData(value);
			int wireFacesBitmask = WireBlock.GetWireFacesBitmask(value);
			int valueOrDefault = WireBlock.GetColor(data4).GetValueOrDefault();
			bool hasValue = WireBlock.GetColor(data4).HasValue;
			int num13 = 0;
			for (int i = 0; i < 6; i++)
			{
				if ((wireFacesBitmask & (1 << i)) != 0)
				{
					int num14 = i;
					if (i >= 0 && i <= 3)
					{
						num14 = (i + 1) & 3;
					}
					num13 |= 1 << num14;
				}
			}
			int data5 = WireBlock.SetWireFacesBitmask(0, num13);
			if (hasValue)
			{
				data5 = WireBlock.SetColor(data5, valueOrDefault);
			}
			return Terrain.MakeBlockValue(num, 0, data5);
		}
		default:
			if (BlocksManager.Blocks[num] is WireThroughBlock)
			{
				int data = Terrain.ExtractData(value);
				int num3 = WireThroughBlock.GetWiredFace(data);
				switch (num3)
				{
				case 0:
					num3 = 1;
					break;
				case 1:
					num3 = 0;
					break;
				}
				data = WireThroughBlock.SetWiredFace(data, num3);
				return Terrain.MakeBlockValue(num, 0, data);
			}
			switch (num)
			{
			case 238:
			{
				int data2 = Terrain.ExtractData(value);
				int num10 = PistonHeadBlock.GetFace(data2);
				if (num10 >= 0 && num10 <= 3)
				{
					num10 = (num10 + 1) & 3;
				}
				data2 = PistonHeadBlock.SetFace(data2, num10);
				return Terrain.MakeBlockValue(num, 0, data2);
			}
			case 138:
			{
				int voltageLevel = BatteryBlock.GetVoltageLevel(num2);
				int num12 = BatteryBlock.GetMountingFace(num2);
				if (num12 >= 0 && num12 <= 3)
				{
					num12 = (num12 + 1) & 3;
				}
				int data3 = BatteryBlock.SetMountingFace(0, num12);
				data3 = BatteryBlock.SetVoltageLevel(data3, voltageLevel);
				return Terrain.MakeBlockValue(num, 0, data3);
			}
			case 428:
			{
				int num11 = FermentationBarrelBlock.GetMountFace(num2);
				if (num11 >= 0 && num11 <= 3)
				{
					num11 = (num11 + 3) & 3;
				}
				num2 = FermentationBarrelBlock.SetMountFace(num2, num11);
				return Terrain.MakeBlockValue(num, 0, num2);
			}
			default:
				if (BlocksManager.Blocks[num] is BarrelBlock)
				{
					int num4 = BarrelBlock.GetVariant(num2) & 7;
					if (num4 >= 0 && num4 <= 3)
					{
						num4 = (num4 + 3) & 3;
					}
					num2 = BarrelBlock.SetVariant(num2, num4);
					return Terrain.MakeBlockValue(num, 0, num2);
				}
				if (num == 64 || num == 65 || num == 303 || num == 304 || num == 45 || num == 27)
				{
					int num5 = Terrain.ExtractData(value) & 3;
					num5 = (num5 + 1) & 3;
					num2 = (num2 & -4) | num5;
					return Terrain.MakeBlockValue(num, 0, num2);
				}
				if (BlocksManager.Blocks[num] is StairsBlock)
				{
					int rotation = StairsBlock.GetRotation(num2);
					num2 = StairsBlock.SetRotation(num2, (rotation + 1) & 3);
					return Terrain.MakeBlockValue(num, 0, num2);
				}
				switch (num)
				{
				case 227:
				{
					int rotation5 = FurnitureBlock.GetRotation(num2);
					num2 = FurnitureBlock.SetRotation(num2, (rotation5 + 1) & 3);
					return Terrain.MakeBlockValue(num, 0, num2);
				}
				case 366:
				{
					int rotation4 = FurnitureBlock2.GetRotation(num2);
					num2 = FurnitureBlock2.SetRotation(num2, (rotation4 + 1) & 3);
					return Terrain.MakeBlockValue(num, 0, num2);
				}
				default:
					if (BlocksManager.Blocks[num] is DoorBlock)
					{
						int rotation2 = DoorBlock.GetRotation(num2);
						bool open = DoorBlock.GetOpen(num2);
						bool rightHanded = DoorBlock.GetRightHanded(num2);
						rotation2 = (rotation2 + 1) & 3;
						num2 = DoorBlock.SetRotation(num2, rotation2);
						num2 = DoorBlock.SetOpen(num2, open);
						num2 = DoorBlock.SetRightHanded(num2, rightHanded);
						return Terrain.MakeBlockValue(num, 0, num2);
					}
					if (BlocksManager.Blocks[num] is TrapdoorBlock)
					{
						int rotation3 = TrapdoorBlock.GetRotation(num2);
						bool open2 = TrapdoorBlock.GetOpen(num2);
						bool upsideDown = TrapdoorBlock.GetUpsideDown(num2);
						rotation3 = (rotation3 + 1) & 3;
						num2 = TrapdoorBlock.SetRotation(num2, rotation3);
						num2 = TrapdoorBlock.SetOpen(num2, open2);
						num2 = TrapdoorBlock.SetUpsideDown(num2, upsideDown);
						return Terrain.MakeBlockValue(num, 0, num2);
					}
					if (BlocksManager.Blocks[num] is ButtonBlock || BlocksManager.Blocks[num] is MotionDetectorBlock)
					{
						int num6 = num2 & 7;
						switch (num6)
						{
						case 0:
							num6 = 1;
							break;
						case 1:
							num6 = 2;
							break;
						case 2:
							num6 = 3;
							break;
						case 3:
							num6 = 0;
							break;
						}
						num2 = (num2 & -8) | num6;
						return Terrain.MakeBlockValue(num, 0, num2);
					}
					if (BlocksManager.Blocks[num] is SwitchBlock)
					{
						int num7 = (num2 >> 1) & 7;
						switch (num7)
						{
						case 0:
							num7 = 1;
							break;
						case 1:
							num7 = 2;
							break;
						case 2:
							num7 = 3;
							break;
						case 3:
							num7 = 0;
							break;
						}
						num2 = (num2 & -15) | (num7 << 1);
						return Terrain.MakeBlockValue(num, 0, num2);
					}
					if (BlocksManager.Blocks[num] is RotateableMountedElectricElementBlock)
					{
						int num8 = (num2 >> 2) & 7;
						int num9 = num2 & 3;
						switch (num8)
						{
						case 0:
							num8 = 1;
							break;
						case 1:
							num8 = 2;
							break;
						case 2:
							num8 = 3;
							break;
						case 3:
							num8 = 0;
							break;
						}
						if (num8 == 4 || num8 == 5)
						{
							num9 = (num9 + 1) & 3;
						}
						num2 = (num8 << 2) | num9;
						return Terrain.MakeBlockValue(num, 0, num2);
					}
					return value;
				}
			}
		}
	}

	private static int TransformBlockRotateX90(int value)
	{
		int num = Terrain.ExtractContents(value);
		int num2 = Terrain.ExtractData(value);
		switch (num)
		{
		case 216:
		{
			int num15 = DispenserBlock.GetDirection(num2);
			switch (num15)
			{
			case 0:
				num15 = 5;
				break;
			case 5:
				num15 = 2;
				break;
			case 2:
				num15 = 4;
				break;
			case 4:
				num15 = 0;
				break;
			}
			num2 = DispenserBlock.SetDirection(num2, num15);
			return Terrain.MakeBlockValue(num, 0, num2);
		}
		case 237:
		{
			int data6 = Terrain.ExtractData(value);
			int face4 = PistonBlock.GetFace(data6);
			int face5 = face4;
			switch (face4)
			{
			case 0:
				face5 = 5;
				break;
			case 5:
				face5 = 2;
				break;
			case 2:
				face5 = 4;
				break;
			case 4:
				face5 = 0;
				break;
			}
			data6 = PistonBlock.SetFace(data6, face5);
			return Terrain.MakeBlockValue(num, 0, data6);
		}
		case 133:
		{
			int data4 = Terrain.ExtractData(value);
			int wireFacesBitmask = WireBlock.GetWireFacesBitmask(value);
			int valueOrDefault = WireBlock.GetColor(data4).GetValueOrDefault();
			bool hasValue = WireBlock.GetColor(data4).HasValue;
			int num13 = 0;
			for (int i = 0; i < 6; i++)
			{
				if ((wireFacesBitmask & (1 << i)) != 0)
				{
					int num14 = i;
					switch (i)
					{
					case 0:
						num14 = 5;
						break;
					case 5:
						num14 = 2;
						break;
					case 2:
						num14 = 4;
						break;
					case 4:
						num14 = 0;
						break;
					}
					num13 |= 1 << num14;
				}
			}
			int data5 = WireBlock.SetWireFacesBitmask(0, num13);
			if (hasValue)
			{
				data5 = WireBlock.SetColor(data5, valueOrDefault);
			}
			return Terrain.MakeBlockValue(num, 0, data5);
		}
		default:
			if (BlocksManager.Blocks[num] is ButtonBlock || BlocksManager.Blocks[num] is MotionDetectorBlock)
			{
				int num3 = num2 & 7;
				switch (num3)
				{
				case 0:
					num3 = 5;
					break;
				case 5:
					num3 = 2;
					break;
				case 2:
					num3 = 4;
					break;
				case 4:
					num3 = 0;
					break;
				}
				num2 = (num2 & -8) | num3;
				return Terrain.MakeBlockValue(num, 0, num2);
			}
			if (BlocksManager.Blocks[num] is SwitchBlock)
			{
				int num4 = (num2 >> 1) & 7;
				switch (num4)
				{
				case 0:
					num4 = 5;
					break;
				case 5:
					num4 = 2;
					break;
				case 2:
					num4 = 4;
					break;
				case 4:
					num4 = 0;
					break;
				}
				num2 = (num2 & -15) | (num4 << 1);
				return Terrain.MakeBlockValue(num, 0, num2);
			}
			if (BlocksManager.Blocks[num] is WireThroughBlock)
			{
				int data = Terrain.ExtractData(value);
				int num5 = WireThroughBlock.GetWiredFace(data);
				switch (num5)
				{
				case 0:
					num5 = 4;
					break;
				case 4:
					num5 = 0;
					break;
				}
				data = WireThroughBlock.SetWiredFace(data, num5);
				return Terrain.MakeBlockValue(num, 0, data);
			}
			switch (num)
			{
			case 238:
			{
				int data2 = Terrain.ExtractData(value);
				int face = PistonHeadBlock.GetFace(data2);
				int face2 = face;
				switch (face)
				{
				case 0:
					face2 = 5;
					break;
				case 5:
					face2 = 2;
					break;
				case 2:
					face2 = 4;
					break;
				case 4:
					face2 = 0;
					break;
				}
				data2 = PistonHeadBlock.SetFace(data2, face2);
				return Terrain.MakeBlockValue(num, 0, data2);
			}
			case 138:
			{
				int voltageLevel = BatteryBlock.GetVoltageLevel(num2);
				int mountingFace = BatteryBlock.GetMountingFace(num2);
				int face3 = mountingFace;
				switch (mountingFace)
				{
				case 0:
					face3 = 5;
					break;
				case 5:
					face3 = 2;
					break;
				case 2:
					face3 = 4;
					break;
				case 4:
					face3 = 0;
					break;
				}
				int data3 = BatteryBlock.SetMountingFace(0, face3);
				data3 = BatteryBlock.SetVoltageLevel(data3, voltageLevel);
				return Terrain.MakeBlockValue(num, 0, data3);
			}
			case 428:
			{
				int num12 = FermentationBarrelBlock.GetMountFace(num2);
				switch (num12)
				{
				case 0:
					num12 = 4;
					break;
				case 4:
					num12 = 2;
					break;
				case 2:
					num12 = 5;
					break;
				case 5:
					num12 = 0;
					break;
				}
				num2 = FermentationBarrelBlock.SetMountFace(num2, num12);
				return Terrain.MakeBlockValue(num, 0, num2);
			}
			default:
				if (BlocksManager.Blocks[num] is BarrelBlock)
				{
					int num6 = BarrelBlock.GetVariant(num2) & 7;
					switch (num6)
					{
					case 0:
						num6 = 4;
						break;
					case 4:
						num6 = 2;
						break;
					case 2:
						num6 = 5;
						break;
					case 5:
						num6 = 0;
						break;
					}
					num2 = BarrelBlock.SetVariant(num2, num6);
					return Terrain.MakeBlockValue(num, 0, num2);
				}
				if (num == 64 || num == 65 || num == 303 || num == 304 || num == 45 || num == 27)
				{
					int num7 = Terrain.ExtractData(value) & 3;
					if ((num7 & 1) == 0)
					{
						num7 ^= 2;
					}
					num2 = (num2 & -4) | num7;
					return Terrain.MakeBlockValue(num, 0, num2);
				}
				if (BlocksManager.Blocks[num] is StairsBlock)
				{
					int rotation = StairsBlock.GetRotation(num2);
					bool isUpsideDown = StairsBlock.GetIsUpsideDown(num2);
					num2 = StairsBlock.SetIsUpsideDown(num2, !isUpsideDown);
					if (rotation == 0 || rotation == 2)
					{
						num2 = StairsBlock.SetRotation(num2, rotation ^ 2);
					}
					return Terrain.MakeBlockValue(num, 0, num2);
				}
				switch (num)
				{
				case 227:
				{
					int rotation2 = FurnitureBlock.GetRotation(num2);
					int rotation3 = (((rotation2 & 1) == 0) ? (rotation2 ^ 2) : rotation2);
					num2 = FurnitureBlock.SetRotation(num2, rotation3);
					return Terrain.MakeBlockValue(num, 0, num2);
				}
				case 366:
				{
					int rotation4 = FurnitureBlock2.GetRotation(num2);
					int rotation5 = (((rotation4 & 1) == 0) ? (rotation4 ^ 2) : rotation4);
					num2 = FurnitureBlock2.SetRotation(num2, rotation5);
					return Terrain.MakeBlockValue(num, 0, num2);
				}
				default:
					if (BlocksManager.Blocks[num] is TrapdoorBlock)
					{
						int num8 = TrapdoorBlock.GetRotation(num2);
						bool open = TrapdoorBlock.GetOpen(num2);
						bool upsideDown = TrapdoorBlock.GetUpsideDown(num2);
						upsideDown = !upsideDown;
						if ((num8 & 1) == 0)
						{
							num8 ^= 2;
						}
						num2 = TrapdoorBlock.SetUpsideDown(num2, upsideDown);
						num2 = TrapdoorBlock.SetRotation(num2, num8);
						num2 = TrapdoorBlock.SetOpen(num2, open);
						return Terrain.MakeBlockValue(num, 0, num2);
					}
					if (BlocksManager.Blocks[num] is RotateableMountedElectricElementBlock)
					{
						int num9 = (num2 >> 2) & 7;
						int num10 = num2 & 3;
						int num11 = num9;
						switch (num11)
						{
						case 0:
							num9 = 5;
							break;
						case 5:
							num9 = 2;
							break;
						case 2:
							num9 = 4;
							break;
						case 4:
							num9 = 0;
							break;
						}
						switch (num11)
						{
						case 1:
						case 3:
							num10 = (num10 + 1) & 3;
							break;
						case 4:
						case 5:
							switch (num10)
							{
							case 0:
								num10 = 2;
								break;
							case 2:
								num10 = 0;
								break;
							}
							break;
						}
						num2 = (num9 << 2) | num10;
						return Terrain.MakeBlockValue(num, 0, num2);
					}
					return value;
				}
			}
		}
	}

	private static int TransformBlockMirrorX(int value)
	{
		int num = Terrain.ExtractContents(value);
		int num2 = Terrain.ExtractData(value);
		if (BlocksManager.Blocks[num] is ButtonBlock || BlocksManager.Blocks[num] is MotionDetectorBlock)
		{
			int num3 = num2 & 7;
			switch (num3)
			{
			case 1:
				num3 = 3;
				break;
			case 3:
				num3 = 1;
				break;
			}
			num2 = (num2 & -8) | num3;
			return Terrain.MakeBlockValue(num, 0, num2);
		}
		if (BlocksManager.Blocks[num] is SwitchBlock)
		{
			int num4 = (num2 >> 1) & 7;
			switch (num4)
			{
			case 1:
				num4 = 3;
				break;
			case 3:
				num4 = 1;
				break;
			}
			num2 = (num2 & -15) | (num4 << 1);
			return Terrain.MakeBlockValue(num, 0, num2);
		}
		switch (num)
		{
		case 216:
		{
			int num19 = DispenserBlock.GetDirection(num2);
			switch (num19)
			{
			case 1:
				num19 = 3;
				break;
			case 3:
				num19 = 1;
				break;
			}
			num2 = DispenserBlock.SetDirection(num2, num19);
			return Terrain.MakeBlockValue(num, 0, num2);
		}
		case 133:
		{
			int data = Terrain.ExtractData(value);
			int wireFacesBitmask = WireBlock.GetWireFacesBitmask(value);
			int valueOrDefault = WireBlock.GetColor(data).GetValueOrDefault();
			bool hasValue = WireBlock.GetColor(data).HasValue;
			int num14 = 0;
			for (int i = 0; i < 6; i++)
			{
				if ((wireFacesBitmask & (1 << i)) != 0)
				{
					int num15 = i;
					switch (i)
					{
					case 1:
						num15 = 3;
						break;
					case 3:
						num15 = 1;
						break;
					}
					num14 |= 1 << num15;
				}
			}
			int data2 = WireBlock.SetWireFacesBitmask(0, num14);
			if (hasValue)
			{
				data2 = WireBlock.SetColor(data2, valueOrDefault);
			}
			return Terrain.MakeBlockValue(num, 0, data2);
		}
		case 237:
		{
			int data3 = Terrain.ExtractData(value);
			int num16 = PistonBlock.GetFace(data3);
			switch (num16)
			{
			case 1:
				num16 = 3;
				break;
			case 3:
				num16 = 1;
				break;
			}
			data3 = PistonBlock.SetFace(data3, num16);
			return Terrain.MakeBlockValue(num, 0, data3);
		}
		case 238:
		{
			int data4 = Terrain.ExtractData(value);
			int num18 = PistonHeadBlock.GetFace(data4);
			switch (num18)
			{
			case 1:
				num18 = 3;
				break;
			case 3:
				num18 = 1;
				break;
			}
			data4 = PistonHeadBlock.SetFace(data4, num18);
			return Terrain.MakeBlockValue(num, 0, data4);
		}
		case 138:
		{
			int voltageLevel = BatteryBlock.GetVoltageLevel(num2);
			int num20 = BatteryBlock.GetMountingFace(num2);
			switch (num20)
			{
			case 1:
				num20 = 3;
				break;
			case 3:
				num20 = 1;
				break;
			}
			int data5 = BatteryBlock.SetMountingFace(0, num20);
			data5 = BatteryBlock.SetVoltageLevel(data5, voltageLevel);
			return Terrain.MakeBlockValue(num, 0, data5);
		}
		case 428:
		{
			int num17 = FermentationBarrelBlock.GetMountFace(num2);
			switch (num17)
			{
			case 1:
				num17 = 3;
				break;
			case 3:
				num17 = 1;
				break;
			}
			num2 = FermentationBarrelBlock.SetMountFace(num2, num17);
			return Terrain.MakeBlockValue(num, 0, num2);
		}
		default:
			if (BlocksManager.Blocks[num] is BarrelBlock)
			{
				int num5 = BarrelBlock.GetVariant(num2) & 7;
				switch (num5)
				{
				case 1:
					num5 = 3;
					break;
				case 3:
					num5 = 1;
					break;
				}
				num2 = BarrelBlock.SetVariant(num2, num5);
				return Terrain.MakeBlockValue(num, 0, num2);
			}
			if (num == 64 || num == 65 || num == 303 || num == 304 || num == 45 || num == 27)
			{
				int num6 = Terrain.ExtractData(value) & 3;
				switch (num6)
				{
				case 1:
					num6 = 3;
					break;
				case 3:
					num6 = 1;
					break;
				}
				num2 = (num2 & -4) | num6;
				return Terrain.MakeBlockValue(num, 0, num2);
			}
			if (BlocksManager.Blocks[num] is StairsBlock)
			{
				int num7 = StairsBlock.GetRotation(num2);
				switch (num7)
				{
				case 1:
					num7 = 3;
					break;
				case 3:
					num7 = 1;
					break;
				}
				num2 = StairsBlock.SetRotation(num2, num7);
				return Terrain.MakeBlockValue(num, 0, num2);
			}
			switch (num)
			{
			case 227:
			{
				int num12 = FurnitureBlock.GetRotation(num2);
				switch (num12)
				{
				case 1:
					num12 = 3;
					break;
				case 3:
					num12 = 1;
					break;
				}
				num2 = FurnitureBlock.SetRotation(num2, num12);
				return Terrain.MakeBlockValue(num, 0, num2);
			}
			case 366:
			{
				int num13 = FurnitureBlock2.GetRotation(num2);
				switch (num13)
				{
				case 1:
					num13 = 3;
					break;
				case 3:
					num13 = 1;
					break;
				}
				num2 = FurnitureBlock2.SetRotation(num2, num13);
				return Terrain.MakeBlockValue(num, 0, num2);
			}
			default:
				if (BlocksManager.Blocks[num] is DoorBlock)
				{
					int num8 = DoorBlock.GetRotation(num2);
					switch (num8)
					{
					case 1:
						num8 = 3;
						break;
					case 3:
						num8 = 1;
						break;
					}
					bool rightHanded = DoorBlock.GetRightHanded(num2);
					rightHanded = !rightHanded;
					num2 = DoorBlock.SetRotation(num2, num8);
					num2 = DoorBlock.SetRightHanded(num2, rightHanded);
					return Terrain.MakeBlockValue(num, 0, num2);
				}
				if (BlocksManager.Blocks[num] is TrapdoorBlock)
				{
					int num9 = TrapdoorBlock.GetRotation(num2);
					switch (num9)
					{
					case 1:
						num9 = 3;
						break;
					case 3:
						num9 = 1;
						break;
					}
					num2 = TrapdoorBlock.SetRotation(num2, num9);
					return Terrain.MakeBlockValue(num, 0, num2);
				}
				if (BlocksManager.Blocks[num] is RotateableMountedElectricElementBlock)
				{
					int num10 = (num2 >> 2) & 7;
					int num11 = num2 & 3;
					switch (num10)
					{
					case 1:
						num10 = 3;
						break;
					case 3:
						num10 = 1;
						break;
					}
					switch (num11)
					{
					case 1:
						num11 = 3;
						break;
					case 3:
						num11 = 1;
						break;
					}
					num2 = (num10 << 2) | num11;
					return Terrain.MakeBlockValue(num, 0, num2);
				}
				return value;
			}
		}
	}

	private static int TransformBlockMirrorY(int value)
	{
		int num = Terrain.ExtractContents(value);
		int num2 = Terrain.ExtractData(value);
		if (BlocksManager.Blocks[num] is ButtonBlock || BlocksManager.Blocks[num] is MotionDetectorBlock)
		{
			int num3 = num2 & 7;
			switch (num3)
			{
			case 4:
				num3 = 5;
				break;
			case 5:
				num3 = 4;
				break;
			}
			num2 = (num2 & -8) | num3;
			return Terrain.MakeBlockValue(num, 0, num2);
		}
		if (BlocksManager.Blocks[num] is SwitchBlock)
		{
			int num4 = (num2 >> 1) & 7;
			switch (num4)
			{
			case 4:
				num4 = 5;
				break;
			case 5:
				num4 = 4;
				break;
			}
			num2 = (num2 & -15) | (num4 << 1);
			return Terrain.MakeBlockValue(num, 0, num2);
		}
		switch (num)
		{
		case 216:
		{
			int num11 = DispenserBlock.GetDirection(num2);
			switch (num11)
			{
			case 4:
				num11 = 5;
				break;
			case 5:
				num11 = 4;
				break;
			}
			num2 = DispenserBlock.SetDirection(num2, num11);
			return Terrain.MakeBlockValue(num, 0, num2);
		}
		case 133:
		{
			int wireFacesBitmask = WireBlock.GetWireFacesBitmask(value);
			int valueOrDefault = WireBlock.GetColor(num2).GetValueOrDefault();
			bool hasValue = WireBlock.GetColor(num2).HasValue;
			int num8 = 0;
			for (int i = 0; i < 6; i++)
			{
				if ((wireFacesBitmask & (1 << i)) != 0)
				{
					int num9 = i;
					switch (i)
					{
					case 4:
						num9 = 5;
						break;
					case 5:
						num9 = 4;
						break;
					}
					num8 |= 1 << num9;
				}
			}
			int data = WireBlock.SetWireFacesBitmask(0, num8);
			if (hasValue)
			{
				data = WireBlock.SetColor(data, valueOrDefault);
			}
			return Terrain.MakeBlockValue(num, 0, data);
		}
		case 237:
		{
			int num10 = PistonBlock.GetFace(num2);
			switch (num10)
			{
			case 4:
				num10 = 5;
				break;
			case 5:
				num10 = 4;
				break;
			}
			num2 = PistonBlock.SetFace(num2, num10);
			return Terrain.MakeBlockValue(num, 0, num2);
		}
		case 238:
		{
			int num14 = PistonHeadBlock.GetFace(num2);
			switch (num14)
			{
			case 4:
				num14 = 5;
				break;
			case 5:
				num14 = 4;
				break;
			}
			num2 = PistonHeadBlock.SetFace(num2, num14);
			return Terrain.MakeBlockValue(num, 0, num2);
		}
		case 138:
		{
			int voltageLevel = BatteryBlock.GetVoltageLevel(num2);
			int num13 = BatteryBlock.GetMountingFace(num2);
			switch (num13)
			{
			case 4:
				num13 = 5;
				break;
			case 5:
				num13 = 4;
				break;
			}
			int data2 = BatteryBlock.SetMountingFace(0, num13);
			data2 = BatteryBlock.SetVoltageLevel(data2, voltageLevel);
			return Terrain.MakeBlockValue(num, 0, data2);
		}
		case 428:
		{
			int num12 = FermentationBarrelBlock.GetMountFace(num2);
			switch (num12)
			{
			case 4:
				num12 = 5;
				break;
			case 5:
				num12 = 4;
				break;
			}
			num2 = FermentationBarrelBlock.SetMountFace(num2, num12);
			return Terrain.MakeBlockValue(num, 0, num2);
		}
		default:
			if (BlocksManager.Blocks[num] is BarrelBlock)
			{
				int num5 = BarrelBlock.GetVariant(num2) & 7;
				switch (num5)
				{
				case 4:
					num5 = 5;
					break;
				case 5:
					num5 = 4;
					break;
				}
				num2 = BarrelBlock.SetVariant(num2, num5);
				return Terrain.MakeBlockValue(num, 0, num2);
			}
			if (BlocksManager.Blocks[num] is StairsBlock)
			{
				bool isUpsideDown = StairsBlock.GetIsUpsideDown(num2);
				num2 = StairsBlock.SetIsUpsideDown(num2, !isUpsideDown);
				return Terrain.MakeBlockValue(num, 0, num2);
			}
			if (BlocksManager.Blocks[num] is SlabBlock)
			{
				bool isTop = SlabBlock.GetIsTop(num2);
				num2 = SlabBlock.SetIsTop(num2, !isTop);
				return Terrain.MakeBlockValue(num, 0, num2);
			}
			if (BlocksManager.Blocks[num] is TrapdoorBlock)
			{
				bool upsideDown = TrapdoorBlock.GetUpsideDown(num2);
				num2 = TrapdoorBlock.SetUpsideDown(num2, !upsideDown);
				return Terrain.MakeBlockValue(num, 0, num2);
			}
			if (BlocksManager.Blocks[num] is RotateableMountedElectricElementBlock)
			{
				int num6 = (num2 >> 2) & 7;
				int num7 = num2 & 3;
				switch (num6)
				{
				case 4:
					num6 = 5;
					break;
				case 5:
					num6 = 4;
					break;
				}
				if (num6 >= 0 && num6 <= 3)
				{
					switch (num7)
					{
					case 0:
						num7 = 2;
						break;
					case 2:
						num7 = 0;
						break;
					}
				}
				num2 = (num6 << 2) | num7;
				return Terrain.MakeBlockValue(num, 0, num2);
			}
			return value;
		}
	}

	private void ApplyEntitySnapshotTo(Point3 dest, int placedValue, EntitySnapshot snap)
	{
		if (snap == null || CommonLib.WorkType == WorkType.Client)
		{
			return;
		}
		int num = Terrain.ExtractContents(placedValue);
		ComponentBlockEntity num2 = m_subsystemBlockEntities.GetBlockEntity(dest.X, dest.Y, dest.Z);
		if (num2 == null)
		{
			string num4 = GetEntityTemplateNameForBlock(num);
			if (!string.IsNullOrEmpty(num4))
			{
				ComponentMiner f = null;
				if (m_componentPlayer != null)
				{
					f = m_componentPlayer.ComponentMiner;
				}
				m_subsystemBlockEntities.CreateBlockEntity(num4, dest, f);
				num2 = m_subsystemBlockEntities.GetBlockEntity(dest.X, dest.Y, dest.Z);
			}
		}
		if (num2 == null)
		{
			return;
		}
		Guid num5 = snap.Owner;
		if (num5 == Guid.Empty && m_componentPlayer != null && m_componentPlayer.PlayerData != null)
		{
			num5 = m_componentPlayer.PlayerData.PlayerGUID;
		}
		num2.Owner = num5;
		foreach (IInventory x4 in num2.Entity.FindComponents<IInventory>())
		{
			InventorySnapshot x5 = null;
			for (int i = 0; i < snap.Inventories.Count; i++)
			{
				if (snap.Inventories[i].TypeName == x4.GetType().FullName)
				{
					x5 = snap.Inventories[i];
					break;
				}
			}
			if (x5 == null && snap.Inventories.Count > 0)
			{
				x5 = snap.Inventories[0];
			}
			if (x5 == null)
			{
				continue;
			}
			int slotsCount = x4.SlotsCount;
			for (int j = 0; j < slotsCount; j++)
			{
				int slotCount = x4.GetSlotCount(j);
				if (slotCount > 0)
				{
					x4.RemoveSlotItems(j, slotCount);
				}
				int num6 = ((x5.Values != null && j < x5.Values.Length) ? x5.Values[j] : 0);
				int num7 = ((x5.Counts != null && j < x5.Counts.Length) ? x5.Counts[j] : 0);
				if (num7 > 0 && num6 != 0)
				{
					x4.AddSlotItems(j, num6, num7);
				}
			}
		}
	}

	public static string GetEntityTemplateNameForBlock(int contents)
	{
		switch (contents)
		{
		case 64:
		case 65:
			return "Furnace";
		case 303:
		case 304:
			return "Furnace2";
		case 216:
			return "Dispenser";
		case 428:
			return "FermentationBarrel";
		case 45:
			return "Chest";
		case 27:
			return "CraftingTable";
		default:
			return null;
		}
	}

	private static Dictionary<Point3, EntitySnapshot> CloneEntitiesDict(Dictionary<Point3, EntitySnapshot> src)
	{
		Dictionary<Point3, EntitySnapshot> dictionary = new Dictionary<Point3, EntitySnapshot>(src.Count);
		foreach (KeyValuePair<Point3, EntitySnapshot> item in src)
		{
			EntitySnapshot value = item.Value;
			EntitySnapshot entitySnapshot = new EntitySnapshot();
			entitySnapshot.Owner = value.Owner;
			for (int i = 0; i < value.Inventories.Count; i++)
			{
				InventorySnapshot inventorySnapshot = value.Inventories[i];
				InventorySnapshot inventorySnapshot2 = new InventorySnapshot();
				inventorySnapshot2.TypeName = inventorySnapshot.TypeName;
				if (inventorySnapshot.Values != null)
				{
					inventorySnapshot2.Values = new int[inventorySnapshot.Values.Length];
					for (int j = 0; j < inventorySnapshot.Values.Length; j++)
					{
						inventorySnapshot2.Values[j] = inventorySnapshot.Values[j];
					}
				}
				if (inventorySnapshot.Counts != null)
				{
					inventorySnapshot2.Counts = new int[inventorySnapshot.Counts.Length];
					for (int k = 0; k < inventorySnapshot.Counts.Length; k++)
					{
						inventorySnapshot2.Counts[k] = inventorySnapshot.Counts[k];
					}
				}
				entitySnapshot.Inventories.Add(inventorySnapshot2);
			}
			dictionary[item.Key] = entitySnapshot;
		}
		return dictionary;
	}

	private EntitySnapshot CaptureEntitySnapshot(int x, int y, int z)
	{
		ComponentBlockEntity blockEntity = m_subsystemBlockEntities.GetBlockEntity(x, y, z);
		if (blockEntity == null)
		{
			return null;
		}
		EntitySnapshot entitySnapshot = new EntitySnapshot();
		entitySnapshot.Owner = blockEntity.Owner;
		foreach (IInventory item in blockEntity.Entity.FindComponents<IInventory>())
		{
			InventorySnapshot inventorySnapshot = new InventorySnapshot();
			inventorySnapshot.TypeName = item.GetType().FullName;
			int slotsCount = item.SlotsCount;
			inventorySnapshot.Values = new int[slotsCount];
			inventorySnapshot.Counts = new int[slotsCount];
			for (int i = 0; i < slotsCount; i++)
			{
				inventorySnapshot.Values[i] = item.GetSlotValue(i);
				inventorySnapshot.Counts[i] = item.GetSlotCount(i);
			}
			entitySnapshot.Inventories.Add(inventorySnapshot);
		}
		return entitySnapshot;
	}

	public void Coppy_zone(string path, Point3 Point1, Point3 Point2, SubsystemTerrain subsystemTerrain, ComponentPlayer player, bool includeFurniture)
	{
		int num = Math.Min(Point1.X, Point2.X);
		int num2 = Math.Max(Point1.X, Point2.X);
		int num3 = Math.Min(Point1.Y, Point2.Y);
		int num4 = Math.Max(Point1.Y, Point2.Y);
		int num5 = Math.Min(Point1.Z, Point2.Z);
		int num6 = Math.Max(Point1.Z, Point2.Z);
		Dictionary<Point3, EntitySnapshot> dictionary = new Dictionary<Point3, EntitySnapshot>();
		Dictionary<int, FurnitureSnapshot> dictionary2 = new Dictionary<int, FurnitureSnapshot>();
		try
		{
			Storage.OpenFile(path, OpenFileMode.Create)?.Dispose();
		}
		catch
		{
		}
		using (Stream output = Storage.OpenFile(path, OpenFileMode.ReadWrite))
		{
			using BinaryWriter binaryWriter = new BinaryWriter(output);
			binaryWriter.Write(new byte[7] { 79, 110, 101, 75, 101, 121, 0 });
			binaryWriter.Write(0);
			int num7 = 0;
			for (int i = 0; i <= num2 - num; i++)
			{
				for (int j = 0; j <= num4 - num3; j++)
				{
					for (int k = 0; k <= num6 - num5; k++)
					{
						int x = ((Point1.X > Point2.X) ? (Point1.X - i) : (Point1.X + i));
						int y = ((Point1.Y > Point2.Y) ? (Point1.Y - j) : (Point1.Y + j));
						int z = ((Point1.Z > Point2.Z) ? (Point1.Z - k) : (Point1.Z + k));
						bool flag = true;
						if (m_componentPlayer != null && m_componentPlayer.PlayerData != null && !m_componentPlayer.PlayerData.ServerManager)
						{
							bool flag2 = SubsystemBedrockBlockBehavior.CheckIsInTerritoriy(x, z, out var territoriy);
							WorldSettings worldSettings = ((m_subsystemGameInfo != null) ? m_subsystemGameInfo.WorldSettings : null);
							if (worldSettings != null && worldSettings.RestrictWorldInteraction && !flag2)
							{
								flag = false;
							}
							else if (flag2 && territoriy != null)
							{
								bool flag3 = territoriy.OwnerGuid == m_componentPlayer.PlayerData.PlayerGUID;
								bool flag4 = m_componentPlayer.PlayerData.Friends.Contains(territoriy.OwnerGuid);
								bool applyToFirend = territoriy.ApplyToFirend;
								flag = flag3 || (flag4 & applyToFirend);
							}
						}
						if (!flag)
						{
							continue;
						}
						int value = subsystemTerrain.Terrain.GetCellValueFast(x, y, z);
						int num8 = Terrain.ExtractContents(value);
						if (!includeFurniture && (num8 == 227 || num8 == 366))
						{
							value = 0;
						}
						int num9 = ((Point1.X > Point2.X) ? (-i) : i);
						int num10 = ((Point1.Y > Point2.Y) ? (-j) : j);
						int num11 = ((Point1.Z > Point2.Z) ? (-k) : k);
						binaryWriter.Write(num9);
						binaryWriter.Write(num10);
						binaryWriter.Write(num11);
						binaryWriter.Write(value);
						num7++;
						if (num8 != 0)
						{
							EntitySnapshot entitySnapshot = CaptureEntitySnapshot(x, y, z);
							if (entitySnapshot != null)
							{
								Point3 key = new Point3(num9, num10, num11);
								dictionary[key] = entitySnapshot;
							}
						}
						if (!includeFurniture || (num8 != 227 && num8 != 366))
						{
							continue;
						}
						int data = Terrain.ExtractData(value);
						int index = ((num8 == 227) ? FurnitureBlock.GetDesignIndex(data) : FurnitureBlock2.GetDesignIndex(data));
						FurnitureDesign design = subsystemTerrain.SubsystemFurnitureBlockBehavior.GetDesign(index);
						if (design == null)
						{
							continue;
						}
						List<int> list = new List<int>();
						HashSet<int> hashSet = new HashSet<int>();
						FurnitureDesign furnitureDesign = design;
						while (furnitureDesign != null && !hashSet.Contains(furnitureDesign.Index))
						{
							hashSet.Add(furnitureDesign.Index);
							list.Add(furnitureDesign.Index);
							furnitureDesign = furnitureDesign.LinkedDesign;
						}
						if (list.Count == 0)
						{
							continue;
						}
						int num12 = list[0];
						int num13 = 0;
						for (int l = 1; l < list.Count; l++)
						{
							if (list[l] < num12)
							{
								num12 = list[l];
								num13 = l;
							}
						}
						List<int> list2 = new List<int>(list.Count);
						for (int m = 0; m < list.Count; m++)
						{
							list2.Add(list[(num13 + m) % list.Count]);
						}
						List<FurnitureDesign> list3 = design.CloneChain();
						if (list3 != null && list3.Count != 0)
						{
							List<FurnitureDesign> list4 = new List<FurnitureDesign>(list3.Count);
							for (int n = 0; n < list3.Count; n++)
							{
								list4.Add(list3[(num13 + n) % list3.Count]);
							}
							for (int num14 = 0; num14 < list4.Count - 1; num14++)
							{
								list4[num14].LinkedDesign = list4[num14 + 1];
							}
							list4[list4.Count - 1].LinkedDesign = null;
							int key2 = BuildFurnitureKey(num8, num12);
							if (!dictionary2.ContainsKey(key2))
							{
								FurnitureSnapshot value2 = new FurnitureSnapshot
								{
									Contents = num8,
									OriginalIndices = list2.ToArray(),
									CurrentIndices = null,
									HeadClone = list4[0],
									IsLightEmitter = list4[0].IsLightEmitter,
									ShadowStrengthFactor = list4[0].ShadowStrengthFactor
								};
								dictionary2[key2] = value2;
							}
						}
					}
				}
			}
			binaryWriter.BaseStream.Position = 7L;
			binaryWriter.Write(num7);
			binaryWriter.BaseStream.Position = binaryWriter.BaseStream.Length;
			if (includeFurniture && dictionary2.Count > 0)
			{
				binaryWriter.Write(new byte[4] { 70, 85, 82, 78 });
				binaryWriter.Write(dictionary2.Count);
				foreach (KeyValuePair<int, FurnitureSnapshot> item in dictionary2)
				{
					FurnitureSnapshot value3 = item.Value;
					binaryWriter.Write(value3.Contents);
					int num15 = ((value3.OriginalIndices != null) ? value3.OriginalIndices.Length : 0);
					binaryWriter.Write(num15);
					for (int num16 = 0; num16 < num15; num16++)
					{
						binaryWriter.Write(value3.OriginalIndices[num16]);
					}
					List<FurnitureDesign> list5 = ((value3.HeadClone != null) ? value3.HeadClone.CloneChain() : null);
					int num17 = list5?.Count ?? 0;
					binaryWriter.Write(num17);
					for (int num18 = 0; num18 < num17; num18++)
					{
						FurnitureDesign furnitureDesign2 = list5[num18];
						int resolution = furnitureDesign2.Resolution;
						int[] array = new int[resolution * resolution * resolution];
						for (int num19 = 0; num19 < array.Length; num19++)
						{
							array[num19] = furnitureDesign2.GetValue(num19);
						}
						binaryWriter.Write(resolution);
						binaryWriter.Write(array.Length);
						for (int num20 = 0; num20 < array.Length; num20++)
						{
							binaryWriter.Write(array[num20]);
						}
					}
				}
			}
			if (dictionary.Count > 0)
			{
				binaryWriter.Write(new byte[4] { 69, 78, 84, 83 });
				binaryWriter.Write(dictionary.Count);
				foreach (KeyValuePair<Point3, EntitySnapshot> item2 in dictionary)
				{
					Point3 key3 = item2.Key;
					EntitySnapshot value4 = item2.Value;
					binaryWriter.Write(key3.X);
					binaryWriter.Write(key3.Y);
					binaryWriter.Write(key3.Z);
					binaryWriter.Write(value4.Owner.ToString());
					int num21 = ((value4.Inventories != null) ? value4.Inventories.Count : 0);
					binaryWriter.Write(num21);
					for (int num22 = 0; num22 < num21; num22++)
					{
						InventorySnapshot inventorySnapshot = value4.Inventories[num22];
						binaryWriter.Write(inventorySnapshot.TypeName ?? string.Empty);
						int num23 = ((inventorySnapshot.Values != null) ? inventorySnapshot.Values.Length : 0);
						binaryWriter.Write(num23);
						for (int num24 = 0; num24 < num23; num24++)
						{
							int value5 = inventorySnapshot.Values[num24];
							int value6 = ((inventorySnapshot.Counts != null && num24 < inventorySnapshot.Counts.Length) ? inventorySnapshot.Counts[num24] : 0);
							binaryWriter.Write(value5);
							binaryWriter.Write(value6);
						}
					}
				}
			}
		}
		int num25 = num2 - num + 1;
		int num26 = num4 - num3 + 1;
		int num27 = num6 - num5 + 1;
		if (player != null && player.ComponentGui != null)
		{
			player.ComponentGui.DisplaySmallMessage(string.Format(LanguageControl.Get("WE_Notifications", "file_copy_success"), Fmt(num25 * num26 * num27), DimsXYZ(num25, num26, num27), includeFurniture ? LanguageControl.Get("WE_Notifications", "furn_suffix") : string.Empty), Color.MintGreen, blinking: true, playNotificationSound: true);
		}
	}

	public void Paste_zone(Point3 anchor, SubsystemTerrain subsystemTerrain, ComponentPlayer player, string path, bool includeFurniture)
	{
		try
		{
			int num = int.MaxValue;
			int num2 = int.MaxValue;
			int num3 = int.MaxValue;
			List<BlockMem> oldMem = CloneMemList(s_mem);
			Dictionary<Point3, EntitySnapshot> oldMemEntities = CloneEntitiesDict(s_memEntities);
			Dictionary<int, FurnitureSnapshot> oldMemFurniture = CloneFurnitureDict(s_memFurniture);
			List<BlockMem> list = new List<BlockMem>(4096);
			bool flag = false;
			List<(Point3, EntitySnapshot)> list2 = new List<(Point3, EntitySnapshot)>(64);
			using (Stream input = Storage.OpenFile(path, OpenFileMode.Read))
			{
				using BinaryReader binaryReader = new BinaryReader(input);
				binaryReader.BaseStream.Position = 7L;
				int num4 = binaryReader.ReadInt32();
				for (int i = 0; i < num4; i++)
				{
					int num5 = binaryReader.ReadInt32();
					int num6 = binaryReader.ReadInt32();
					int num7 = binaryReader.ReadInt32();
					int num8 = binaryReader.ReadInt32();
					if (!includeFurniture)
					{
						int num9 = Terrain.ExtractContents(num8);
						if (num9 == 227 || num9 == 366)
						{
							num8 = 0;
						}
					}
					list.Add(new BlockMem
					{
						x = num5,
						y = num6,
						z = num7,
						id = num8
					});
					if (num5 < num)
					{
						num = num5;
					}
					if (num6 < num2)
					{
						num2 = num6;
					}
					if (num7 < num3)
					{
						num3 = num7;
					}
				}
				if (includeFurniture && binaryReader.BaseStream.Position + 8 <= binaryReader.BaseStream.Length)
				{
					long position = binaryReader.BaseStream.Position;
					byte[] array = binaryReader.ReadBytes(4);
					if (array.Length == 4 && array[0] == 70 && array[1] == 85 && array[2] == 82 && array[3] == 78)
					{
						int num10 = binaryReader.ReadInt32();
						s_memFurniture.Clear();
						for (int j = 0; j < num10; j++)
						{
							int contents = binaryReader.ReadInt32();
							int val = binaryReader.ReadInt32();
							int[] array2 = new int[Math.Max(0, val)];
							for (int k = 0; k < array2.Length; k++)
							{
								array2[k] = binaryReader.ReadInt32();
							}
							int num11 = binaryReader.ReadInt32();
							FurnitureDesign furnitureDesign = null;
							FurnitureDesign furnitureDesign2 = null;
							for (int l = 0; l < num11; l++)
							{
								int resolution = binaryReader.ReadInt32();
								int num12 = binaryReader.ReadInt32();
								int[] array3 = new int[num12];
								for (int m = 0; m < num12; m++)
								{
									array3[m] = binaryReader.ReadInt32();
								}
								FurnitureDesign furnitureDesign3 = new FurnitureDesign(m_subsystemTerrain);
								furnitureDesign3.SetValues(resolution, array3);
								if (furnitureDesign == null)
								{
									furnitureDesign = furnitureDesign3;
								}
								else
								{
									furnitureDesign2.LinkedDesign = furnitureDesign3;
								}
								furnitureDesign2 = furnitureDesign3;
							}
							if (furnitureDesign == null || array2.Length == 0)
							{
								continue;
							}
							int num13 = array2[0];
							int num14 = 0;
							for (int n = 1; n < array2.Length; n++)
							{
								if (array2[n] < num13)
								{
									num13 = array2[n];
									num14 = n;
								}
							}
							int[] array4 = new int[array2.Length];
							for (int num15 = 0; num15 < array2.Length; num15++)
							{
								array4[num15] = array2[(num14 + num15) % array2.Length];
							}
							List<FurnitureDesign> list3 = new List<FurnitureDesign>(array2.Length);
							FurnitureDesign furnitureDesign4 = furnitureDesign;
							while (furnitureDesign4 != null && list3.Count < array2.Length)
							{
								list3.Add(furnitureDesign4);
								furnitureDesign4 = furnitureDesign4.LinkedDesign;
							}
							if (list3.Count > 0)
							{
								List<FurnitureDesign> list4 = new List<FurnitureDesign>(list3.Count);
								for (int num16 = 0; num16 < list3.Count; num16++)
								{
									list4.Add(list3[(num14 + num16) % list3.Count]);
								}
								for (int num17 = 0; num17 < list4.Count - 1; num17++)
								{
									list4[num17].LinkedDesign = list4[num17 + 1];
								}
								list4[list4.Count - 1].LinkedDesign = null;
								furnitureDesign = list4[0];
							}
							int key = BuildFurnitureKey(contents, num13);
							if (!s_memFurniture.ContainsKey(key))
							{
								s_memFurniture[key] = new FurnitureSnapshot
								{
									Contents = contents,
									OriginalIndices = array4,
									CurrentIndices = null,
									HeadClone = furnitureDesign,
									IsLightEmitter = furnitureDesign.IsLightEmitter,
									ShadowStrengthFactor = furnitureDesign.ShadowStrengthFactor
								};
							}
						}
						flag = true;
					}
					else
					{
						binaryReader.BaseStream.Position = position;
					}
				}
				if (binaryReader.BaseStream.Position + 8 <= binaryReader.BaseStream.Length)
				{
					long position2 = binaryReader.BaseStream.Position;
					byte[] array5 = binaryReader.ReadBytes(4);
					if (array5.Length == 4 && array5[0] == 69 && array5[1] == 78 && array5[2] == 84 && array5[3] == 83)
					{
						int num18 = binaryReader.ReadInt32();
						for (int num19 = 0; num19 < num18; num19++)
						{
							int x = binaryReader.ReadInt32();
							int y = binaryReader.ReadInt32();
							int z = binaryReader.ReadInt32();
							string input2 = binaryReader.ReadString();
							int num20 = binaryReader.ReadInt32();
							EntitySnapshot entitySnapshot = new EntitySnapshot();
							entitySnapshot.Owner = (Guid.TryParse(input2, out var result) ? result : Guid.Empty);
							for (int num21 = 0; num21 < num20; num21++)
							{
								string typeName = binaryReader.ReadString();
								int num22 = binaryReader.ReadInt32();
								InventorySnapshot inventorySnapshot = new InventorySnapshot();
								inventorySnapshot.TypeName = typeName;
								inventorySnapshot.Values = new int[num22];
								inventorySnapshot.Counts = new int[num22];
								for (int num23 = 0; num23 < num22; num23++)
								{
									inventorySnapshot.Values[num23] = binaryReader.ReadInt32();
									inventorySnapshot.Counts[num23] = binaryReader.ReadInt32();
								}
								entitySnapshot.Inventories.Add(inventorySnapshot);
							}
							list2.Add((new Point3(x, y, z), entitySnapshot));
						}
					}
					else
					{
						binaryReader.BaseStream.Position = position2;
					}
				}
			}
			s_mem.Clear();
			s_memEntities.Clear();
			for (int num24 = 0; num24 < list.Count; num24++)
			{
				BlockMem blockMem = list[num24];
				s_mem.Add(new BlockMem
				{
					x = blockMem.x - num,
					y = blockMem.y - num2,
					z = blockMem.z - num3,
					id = blockMem.id
				});
			}
			s_memCount = s_mem.Count;
			for (int num25 = 0; num25 < list2.Count; num25++)
			{
				Point3 item = list2[num25].Item1;
				EntitySnapshot item2 = list2[num25].Item2;
				Point3 key2 = new Point3(item.X - num, item.Y - num2, item.Z - num3);
				s_memEntities[key2] = item2;
			}
			if (!flag)
			{
				CaptureFurnitureSnapshotsFromMem();
			}
			EnsureFurniturePresentAndRemap();
			int weMaxBlocksAllowed = GetWeMaxBlocksAllowed();
			if (weMaxBlocksAllowed != int.MaxValue && s_memCount > 0)
			{
				GetMemBounds(out var min, out var max);
				int num26 = max.X - min.X + 1;
				int num27 = max.Y - min.Y + 1;
				int num28 = max.Z - min.Z + 1;
				if ((long)num26 * (long)num27 * num28 > weMaxBlocksAllowed)
				{
					int num29 = num26;
					int num30 = num27;
					int num31 = num28;
					for (int num32 = 0; num32 < 3; num32++)
					{
						if ((long)num29 * (long)num30 * num31 <= weMaxBlocksAllowed)
						{
							break;
						}
						if (num29 >= num30 && num29 >= num31)
						{
							int num33 = Math.Max(1, weMaxBlocksAllowed / Math.Max(1, num30 * num31));
							if (num33 < num29)
							{
								num29 = num33;
							}
						}
						else if (num30 >= num29 && num30 >= num31)
						{
							int num34 = Math.Max(1, weMaxBlocksAllowed / Math.Max(1, num29 * num31));
							if (num34 < num30)
							{
								num30 = num34;
							}
						}
						else
						{
							int num35 = Math.Max(1, weMaxBlocksAllowed / Math.Max(1, num29 * num30));
							if (num35 < num31)
							{
								num31 = num35;
							}
						}
					}
					int x2 = min.X;
					int y2 = min.Y;
					int z2 = min.Z;
					int num36 = x2 + num29 - 1;
					int num37 = y2 + num30 - 1;
					int num38 = z2 + num31 - 1;
					for (int num39 = s_mem.Count - 1; num39 >= 0; num39--)
					{
						BlockMem blockMem2 = s_mem[num39];
						if (blockMem2.x < x2 || blockMem2.x > num36 || blockMem2.y < y2 || blockMem2.y > num37 || blockMem2.z < z2 || blockMem2.z > num38)
						{
							s_mem.RemoveAt(num39);
						}
					}
					List<Point3> list5 = null;
					foreach (KeyValuePair<Point3, EntitySnapshot> s_memEntity in s_memEntities)
					{
						Point3 key3 = s_memEntity.Key;
						if (key3.X < x2 || key3.X > num36 || key3.Y < y2 || key3.Y > num37 || key3.Z < z2 || key3.Z > num38)
						{
							if (list5 == null)
							{
								list5 = new List<Point3>();
							}
							list5.Add(key3);
						}
					}
					if (list5 != null)
					{
						for (int num40 = 0; num40 < list5.Count; num40++)
						{
							s_memEntities.Remove(list5[num40]);
						}
					}
					s_memCount = s_mem.Count;
				}
			}
			TerrainRaycastResult oldRay = s_p3;
			s_p3 = new TerrainRaycastResult
			{
				CellFace = new CellFace
				{
					X = anchor.X,
					Y = anchor.Y,
					Z = anchor.Z
				},
				Ray = new Ray3(new Vector3((float)anchor.X + 0.5f, (float)anchor.Y + 0.5f, (float)anchor.Z + 0.5f), Vector3.UnitY),
				Distance = 0f
			};
			List<BlockMem> newMem = CloneMemList(s_mem);
			Dictionary<Point3, EntitySnapshot> newMemEntities = CloneEntitiesDict(s_memEntities);
			Dictionary<int, FurnitureSnapshot> newMemFurniture = CloneFurnitureDict(s_memFurniture);
			PushUndo(new WeAction
			{
				Type = WeActionType.MemChange,
				OldMem = oldMem,
				NewMem = newMem,
				OldMemEntities = oldMemEntities,
				NewMemEntities = newMemEntities,
				OldMemFurniture = oldMemFurniture,
				NewMemFurniture = newMemFurniture
			});
			PushUndo(new WeAction
			{
				Type = WeActionType.SetP3,
				OldRay = oldRay,
				NewRay = s_p3
			});
			ClearRedoHistory();
			UpdateUndoRedoButtons();
			InvalidateGhostCache();
			if (player != null && player.ComponentGui != null)
			{
				player.ComponentGui.DisplaySmallMessage(string.Format(LanguageControl.Get("WE_Notifications", "file_preview_loaded"), s_memCount.ToString("N0"), flag ? LanguageControl.Get("WE_Notifications", "furn_suffix") : string.Empty), Color.LightCyan, blinking: false, playNotificationSound: false);
			}
		}
		catch (Exception message)
		{
			Log.Error(message);
			GetMemBounds(out var min2, out var max2);
			int x3 = max2.X - min2.X + 1;
			int y3 = max2.Y - min2.Y + 1;
			int z3 = max2.Z - min2.Z + 1;
			if (player != null && player.ComponentGui != null)
			{
				player.ComponentGui.DisplaySmallMessage(string.Format(LanguageControl.Get("WE_Notifications", "file_preview_loaded_dims"), Fmt(s_memCount), DimsXYZ(x3, y3, z3)), Color.LightCyan, blinking: false, playNotificationSound: false);
			}
		}
	}

	private static CultureInfo CreateDotCulture()
	{
		CultureInfo cultureInfo = (CultureInfo)CultureInfo.InvariantCulture.Clone();
		cultureInfo.NumberFormat.NumberGroupSeparator = ".";
		cultureInfo.NumberFormat.NumberDecimalDigits = 0;
		return cultureInfo;
	}

	private static string Fmt(long v)
	{
		return v.ToString("N0", s_DotCulture);
	}

	private static string DimsXYZ(int x, int y, int z)
	{
		return $"XYZ: {x}x{y}x{z}";
	}

	public void PreviewFrameOrBox(bool frame, int blockId)
	{
		if (frame)
		{
			PreviewBoxFrameFrom12(blockId);
		}
		else
		{
			PreviewBoxSolidFrom12(blockId);
		}
	}

	public void PreviewMountain(int radius, int size, int idTop, int idMid, int idBase)
	{
		Random random = new Random();
		double num = Math.PI / 2.0;
		float num2 = (float)size + 0.8f;
		double num3 = 10.0;
		double num4 = 15.0;
		float f = random.Float((float)num3, (float)num4) + 10f;
		List<BlockMem> f2 = new List<BlockMem>(radius * radius * Math.Max(1, size + 5));
		for (int x3 = -radius; x3 < radius; x3++)
		{
			for (int i = -radius; i < radius; i++)
			{
				double num5 = Math.Cos(num * (double)x3 / (double)radius) * Math.Cos(num * (double)i / (double)radius) * (double)num2;
				double x5 = Math.Sin(num * (double)x3 * 1.39999997615814 / (double)radius + 4.0) * Math.Cos(num * (double)i * 1.39999997615814 / (double)radius + 7.0) * (double)num2 * 0.25;
				double x7 = Math.Cos(num * (double)x3 * 1.39999997615814 * 2.0 / (double)radius + 4.0 * (double)f) * Math.Sin(num * (double)i * 1.39999997615814 * 2.0 / (double)radius + 8.0 * (double)f) * (double)num2 * 0.200000002980232;
				double num6 = Math.Sin(num * (double)x3 * 1.39999997615814 * 4.0 / (double)radius + 4.0 * (double)f * 1.5) * Math.Sin(num * (double)i * 1.39999997615814 * 4.0 / (double)radius + 8.0 * (double)f * 1.5) * (double)num2 * 0.150000005960464;
				double num7 = num5 - x5 + x7 - num6;
				if (!(num7 > 0.0))
				{
					continue;
				}
				int num8 = (int)num7;
				for (int j = 0; j <= num8; j++)
				{
					int id = idBase;
					switch (j)
					{
					case 0:
						id = idTop;
						break;
					case 1:
					case 2:
					case 3:
						id = idMid;
						break;
					}
					f2.Add(new BlockMem
					{
						x = x3,
						y = num8 - j,
						z = i,
						id = id
					});
				}
			}
		}
		LoadPreviewToMemory(f2, LanguageControl.Get("WE_Dialogs", "shape_mountain"));
	}

	public void PreviewMazeFrom12(int blockId, int wallHeight)
	{
		Point3 point = new Point3(s_p1.CellFace.Point.X, s_p1.CellFace.Point.Y, s_p1.CellFace.Point.Z);
		Point3 point2 = new Point3(s_p2.CellFace.Point.X, s_p2.CellFace.Point.Y, s_p2.CellFace.Point.Z);
		if (point.X < point2.X)
		{
			int x = point.X;
			point.X = point2.X;
			point2.X = x;
		}
		if (point.Y < point2.Y)
		{
			int y = point.Y;
			point.Y = point2.Y;
			point2.Y = y;
		}
		if (point.Z < point2.Z)
		{
			int z = point.Z;
			point.Z = point2.Z;
			point2.Z = z;
		}
		int num = point.X - point2.X;
		int num2 = point.Z - point2.Z;
		if (num < 5 || num2 < 5)
		{
			m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get("WE_Notifications", "maze_too_small"), Color.Orange, blinking: true, playNotificationSound: true);
			return;
		}
		bool[,] boolArray = new Maze(num / 2, num2 / 2).GetBoolArray();
		List<BlockMem> list = new List<BlockMem>(num * num2 * Math.Max(1, wallHeight));
		int num3 = ((num % 2 != 0) ? (num - 1) : num);
		int num4 = ((num2 % 2 != 0) ? (num2 - 1) : num2);
		for (int i = 0; i <= num3; i++)
		{
			for (int j = 0; j <= num4; j++)
			{
				if ((i != 1 || j != 0) && (i != num3 || j != num4 - 1) && boolArray[i, j])
				{
					for (int k = 0; k < wallHeight; k++)
					{
						list.Add(new BlockMem
						{
							x = i,
							y = k,
							z = j,
							id = blockId
						});
					}
				}
			}
		}
		LoadPreviewToMemory(list, LanguageControl.Get("WE_Dialogs", "shape_maze"));
	}

	public void PreviewBoxSolidFrom12(int blockId)
	{
		GetAabb(out var min, out var max);
		int num = max.X - min.X + 1;
		int num2 = max.Y - min.Y + 1;
		int num3 = max.Z - min.Z + 1;
		List<BlockMem> list = new List<BlockMem>(num * num2 * num3);
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num2; j++)
			{
				for (int k = 0; k < num3; k++)
				{
					list.Add(new BlockMem
					{
						x = i,
						y = j,
						z = k,
						id = blockId
					});
				}
			}
		}
		LoadPreviewToMemory(list, LanguageControl.Get("WE_Dialogs", "shape_box_solid"));
	}

	public void PreviewBoxFrameFrom12(int blockId)
	{
		GetAabb(out var min, out var max);
		int num = max.X - min.X + 1;
		int num2 = max.Y - min.Y + 1;
		int num3 = max.Z - min.Z + 1;
		List<BlockMem> list = new List<BlockMem>(2 * (num * num2 + num2 * num3 + num3 * num));
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num2; j++)
			{
				for (int k = 0; k < num3; k++)
				{
					if (i == 0 || i == num - 1 || j == 0 || j == num2 - 1 || k == 0 || k == num3 - 1)
					{
						list.Add(new BlockMem
						{
							x = i,
							y = j,
							z = k,
							id = blockId
						});
					}
				}
			}
		}
		LoadPreviewToMemory(list, LanguageControl.Get("WE_Dialogs", "shape_box_frame"));
	}

	public void PreviewRound(bool shell, int radius, int length, int pos, int blockId)
	{
		List<BlockMem> list = new List<BlockMem>((2 * radius + 1) * (2 * radius + 1) * Math.Max(1, length));
		for (int i = 0; i < length; i++)
		{
			for (int j = -radius; j <= radius; j++)
			{
				for (int k = -radius; k <= radius; k++)
				{
					int num = (int)Math.Sqrt(j * j + k * k);
					if (num > radius)
					{
						continue;
					}
					if (!shell)
					{
						BlockMem blockMem = new BlockMem();
						switch (pos)
						{
						case 2:
							blockMem.x = i;
							blockMem.y = j;
							blockMem.z = k;
							break;
						case 1:
							blockMem.x = j;
							blockMem.y = i;
							blockMem.z = k;
							break;
						default:
							blockMem.x = j;
							blockMem.y = k;
							blockMem.z = i;
							break;
						}
						blockMem.id = blockId;
						list.Add(blockMem);
					}
					else if (num == radius)
					{
						BlockMem blockMem2 = new BlockMem();
						switch (pos)
						{
						case 2:
							blockMem2.x = i;
							blockMem2.y = j;
							blockMem2.z = k;
							break;
						case 1:
							blockMem2.x = j;
							blockMem2.y = i;
							blockMem2.z = k;
							break;
						default:
							blockMem2.x = j;
							blockMem2.y = k;
							blockMem2.z = i;
							break;
						}
						blockMem2.id = blockId;
						list.Add(blockMem2);
					}
				}
			}
		}
		LoadPreviewToMemory(list, LanguageControl.Get("WE_Dialogs", "shape_round"));
	}

	public void PreviewSquare(bool shell, int r, int length, int pos, int blockId)
	{
		List<BlockMem> num = new List<BlockMem>((2 * r + 1) * (2 * r + 1) * Math.Max(1, length));
		for (int i = 0; i < length; i++)
		{
			for (int j = -r; j <= r; j++)
			{
				for (int k = -r; k <= r; k++)
				{
					int num2 = Math.Abs(j) + Math.Abs(k);
					if (num2 <= r && (!shell || num2 == r))
					{
						BlockMem blockMem = new BlockMem();
						switch (pos)
						{
						case 0:
							blockMem.x = i;
							blockMem.y = j;
							blockMem.z = k;
							break;
						case 1:
							blockMem.x = j;
							blockMem.y = i;
							blockMem.z = k;
							break;
						default:
							blockMem.x = j;
							blockMem.y = k;
							blockMem.z = i;
							break;
						}
						blockMem.id = blockId;
						num.Add(blockMem);
					}
				}
			}
		}
		LoadPreviewToMemory(num, LanguageControl.Get("WE_Dialogs", "shape_square"));
	}

	public void PreviewPrism(bool shell, int r, int blockId)
	{
		List<BlockMem> list = new List<BlockMem>((2 * r + 1) * (2 * r + 1) * (2 * r + 1));
		for (int i = -r; i <= r; i++)
		{
			for (int j = -r; j <= r; j++)
			{
				for (int k = -r; k <= r; k++)
				{
					int num = Math.Abs(i) + Math.Abs(j) + Math.Abs(k);
					if (num <= r && (!shell || num == r))
					{
						list.Add(new BlockMem
						{
							x = i,
							y = j,
							z = k,
							id = blockId
						});
					}
				}
			}
		}
		LoadPreviewToMemory(list, LanguageControl.Get("WE_Dialogs", "shape_prism"));
	}

	public void PreviewSphere(bool shell, int r, int blockId)
	{
		List<BlockMem> list = new List<BlockMem>((2 * r + 1) * (2 * r + 1) * (2 * r + 1));
		for (int i = -r; i <= r; i++)
		{
			for (int j = -r; j <= r; j++)
			{
				for (int k = -r; k <= r; k++)
				{
					int num = (int)Math.Sqrt(i * i + j * j + k * k);
					if (num <= r && (!shell || num == r))
					{
						list.Add(new BlockMem
						{
							x = i,
							y = j,
							z = k,
							id = blockId
						});
					}
				}
			}
		}
		LoadPreviewToMemory(list, LanguageControl.Get("WE_Dialogs", "shape_sphere"));
	}

	public void PreviewLineFromP1P2(int blockId)
	{
		Point3 num = new Point3(s_p1.CellFace.X, s_p1.CellFace.Y, s_p1.CellFace.Z);
		Point3 point = new Point3(s_p2.CellFace.X, s_p2.CellFace.Y, s_p2.CellFace.Z);
		int num2 = point.X - num.X;
		int num3 = point.Y - num.Y;
		int num4 = point.Z - num.Z;
		int num5 = Math.Max(Math.Max(Math.Abs(num2), Math.Abs(num3)), Math.Abs(num4));
		if (num5 <= 0)
		{
			LoadPreviewToMemory(new List<BlockMem>(1)
			{
				new BlockMem
				{
					x = 0,
					y = 0,
					z = 0,
					id = blockId
				}
			}, LanguageControl.Get("WE_Dialogs", "shape_line"));
			return;
		}
		List<BlockMem> num6 = new List<BlockMem>(num5 + 1);
		for (int f = 0; f <= num5; f++)
		{
			int f2 = num.X + (int)Math.Round((double)f / (double)num5 * (double)num2);
			int f3 = num.Y + (int)Math.Round((double)f / (double)num5 * (double)num3);
			int x3 = num.Z + (int)Math.Round((double)f / (double)num5 * (double)num4);
			num6.Add(new BlockMem
			{
				x = f2,
				y = f3,
				z = x3,
				id = blockId
			});
		}
		LoadPreviewToMemory(num6, LanguageControl.Get("WE_Dialogs", "shape_line"));
	}

	private void LoadPreviewToMemory(List<BlockMem> src, string actionUiName)
	{
		List<BlockMem> oldMem = CloneMemList(s_mem);
		Dictionary<Point3, EntitySnapshot> oldMemEntities = CloneEntitiesDict(s_memEntities);
		Dictionary<int, FurnitureSnapshot> oldMemFurniture = CloneFurnitureDict(s_memFurniture);
		int num = int.MaxValue;
		int num2 = int.MaxValue;
		int num3 = int.MaxValue;
		for (int i = 0; i < src.Count; i++)
		{
			BlockMem blockMem = src[i];
			if (!ShouldSkip(blockMem.id))
			{
				if (blockMem.x < num)
				{
					num = blockMem.x;
				}
				if (blockMem.y < num2)
				{
					num2 = blockMem.y;
				}
				if (blockMem.z < num3)
				{
					num3 = blockMem.z;
				}
			}
		}
		if (num == int.MaxValue)
		{
			num = 0;
			num2 = 0;
			num3 = 0;
		}
		s_mem.Clear();
		s_memEntities.Clear();
		s_memFurniture.Clear();
		for (int j = 0; j < src.Count; j++)
		{
			BlockMem blockMem2 = src[j];
			if (!ShouldSkip(blockMem2.id))
			{
				BlockMem item = new BlockMem
				{
					x = blockMem2.x - num,
					y = blockMem2.y - num2,
					z = blockMem2.z - num3,
					id = blockMem2.id
				};
				s_mem.Add(item);
			}
		}
		s_memCount = s_mem.Count;
		CaptureFurnitureSnapshotsFromMem();
		EnsureFurniturePresentAndRemap();
		int weMaxBlocksAllowed = GetWeMaxBlocksAllowed();
		if (weMaxBlocksAllowed != int.MaxValue && s_memCount > 0)
		{
			GetMemBounds(out var min, out var max);
			int num4 = max.X - min.X + 1;
			int num5 = max.Y - min.Y + 1;
			int num6 = max.Z - min.Z + 1;
			if ((long)num4 * (long)num5 * num6 > weMaxBlocksAllowed)
			{
				int num7 = num4;
				int num8 = num5;
				int num9 = num6;
				for (int k = 0; k < 3; k++)
				{
					if ((long)num7 * (long)num8 * num9 <= weMaxBlocksAllowed)
					{
						break;
					}
					if (num7 >= num8 && num7 >= num9)
					{
						int num10 = Math.Max(1, weMaxBlocksAllowed / Math.Max(1, num8 * num9));
						if (num10 < num7)
						{
							num7 = num10;
						}
					}
					else if (num8 >= num7 && num8 >= num9)
					{
						int num11 = Math.Max(1, weMaxBlocksAllowed / Math.Max(1, num7 * num9));
						if (num11 < num8)
						{
							num8 = num11;
						}
					}
					else
					{
						int num12 = Math.Max(1, weMaxBlocksAllowed / Math.Max(1, num7 * num8));
						if (num12 < num9)
						{
							num9 = num12;
						}
					}
				}
				int x = min.X;
				int y = min.Y;
				int z = min.Z;
				int num13 = x + num7 - 1;
				int num14 = y + num8 - 1;
				int num15 = z + num9 - 1;
				for (int num16 = s_mem.Count - 1; num16 >= 0; num16--)
				{
					BlockMem blockMem3 = s_mem[num16];
					if (blockMem3.x < x || blockMem3.x > num13 || blockMem3.y < y || blockMem3.y > num14 || blockMem3.z < z || blockMem3.z > num15)
					{
						s_mem.RemoveAt(num16);
					}
				}
				List<Point3> list = null;
				foreach (KeyValuePair<Point3, EntitySnapshot> s_memEntity in s_memEntities)
				{
					Point3 key = s_memEntity.Key;
					if (key.X < x || key.X > num13 || key.Y < y || key.Y > num14 || key.Z < z || key.Z > num15)
					{
						if (list == null)
						{
							list = new List<Point3>();
						}
						list.Add(key);
					}
				}
				if (list != null)
				{
					for (int l = 0; l < list.Count; l++)
					{
						s_memEntities.Remove(list[l]);
					}
				}
				s_memCount = s_mem.Count;
			}
		}
		List<BlockMem> newMem = CloneMemList(s_mem);
		Dictionary<Point3, EntitySnapshot> newMemEntities = CloneEntitiesDict(s_memEntities);
		Dictionary<int, FurnitureSnapshot> newMemFurniture = CloneFurnitureDict(s_memFurniture);
		PushUndo(new WeAction
		{
			Type = WeActionType.MemChange,
			OldMem = oldMem,
			NewMem = newMem,
			OldMemEntities = oldMemEntities,
			NewMemEntities = newMemEntities,
			OldMemFurniture = oldMemFurniture,
			NewMemFurniture = newMemFurniture
		});
		ClearRedoHistory();
		UpdateUndoRedoButtons();
		InvalidateGhostCache();
		GetMemBounds(out var min2, out var max2);
		int x2 = max2.X - min2.X + 1;
		int y2 = max2.Y - min2.Y + 1;
		int z2 = max2.Z - min2.Z + 1;
		m_componentPlayer.ComponentGui.DisplaySmallMessage(string.Format(LanguageControl.Get("WE_Notifications", "preview_ready"), actionUiName, Fmt(s_memCount), DimsXYZ(x2, y2, z2)), Color.LightCyan, blinking: false, playNotificationSound: false);
	}

	private bool ShouldSkip(int value)
	{
		int num = Terrain.ExtractContents(value);
		if (num == 0)
		{
			return true;
		}
		if (s_memDropIds.Contains(num))
		{
			return true;
		}
		SubsystemGameInfo subsystemGameInfo = m_subsystemGameInfo;
		if (subsystemGameInfo == null)
		{
			return false;
		}
		return subsystemGameInfo.WorldSettings?.IsBlockDiable(value) == true;
	}

	private WeAccess GetAccess()
	{
		WorldSettings worldSettings = m_subsystemGameInfo?.WorldSettings;
		if (worldSettings == null)
		{
			return WeAccess.Disabled;
		}
		if (!worldSettings.IsWorldEditIntegrated || worldSettings.GameMode == GameMode.Embrace)
		{
			return WeAccess.Disabled;
		}
		if (worldSettings.WEManagersOnly)
		{
			bool flag = false;
			if (m_componentPlayer != null)
			{
				PlayerData playerData = m_componentPlayer.PlayerData;
				if (playerData != null)
				{
					flag = playerData.WorldEditManager;
				}
			}
			if (!flag)
			{
				return WeAccess.Disabled;
			}
		}
		if (worldSettings.GameMode != GameMode.Creative)
		{
			return WeAccess.Limited;
		}
		return WeAccess.Full;
	}

	private void WE_OpenMainMenu(SubsystemTerrain terrain, bool limited = false)
	{
		List<string> items;
		if (!limited)
		{
			List<string> list = new List<string>();
			list.Add(LanguageControl.Get("WE_Dialogs", "menu_image"));
			list.Add(LanguageControl.Get("WE_Dialogs", "menu_round"));
			list.Add(LanguageControl.Get("WE_Dialogs", "menu_sphere"));
			list.Add(LanguageControl.Get("WE_Dialogs", "menu_prism"));
			list.Add(LanguageControl.Get("WE_Dialogs", "menu_square"));
			list.Add(LanguageControl.Get("WE_Dialogs", "menu_frame_or_box"));
			list.Add(LanguageControl.Get("WE_Dialogs", "menu_maze"));
			list.Add(LanguageControl.Get("WE_Dialogs", "menu_mountain"));
			list.Add(LanguageControl.Get("WE_Dialogs", "menu_line"));
			list.Add(LanguageControl.Get("WE_Dialogs", "menu_copy_paste_file"));
			items = list;
			list.Add(LanguageControl.Get("WE_Dialogs", "menu_undo_redo"));
		}
		else
		{
			List<string> list2 = new List<string>();
			list2.Add(LanguageControl.Get("WE_Dialogs", "menu_image"));
			list2.Add(LanguageControl.Get("WE_Dialogs", "menu_copy_paste_file"));
			items = list2;
		}
		DialogsManager.ShowDialog(m_componentPlayer.GameWidget, new ListSelectionDialog(string.Empty, items, 56f, (object c) => c?.ToString() ?? string.Empty, (object c) =>
		{
			if (c == null)
			{
				return;
			}
			string text = c.ToString();
			try
			{
				if (text == LanguageControl.Get("WE_Dialogs", "menu_image"))
				{
					DialogsManager.ShowDialog(m_componentPlayer.GameWidget, new ImageDialog(m_componentPlayer, this, terrain));
				}
				else if (text == LanguageControl.Get("WE_Dialogs", "menu_round"))
				{
					DialogsManager.ShowDialog(m_componentPlayer.GameWidget, new Round(m_componentPlayer, s_selectedBlock, null, terrain));
				}
				else if (text == LanguageControl.Get("WE_Dialogs", "menu_sphere"))
				{
					DialogsManager.ShowDialog(m_componentPlayer.GameWidget, new Sphere(m_componentPlayer, s_selectedBlock, null, terrain));
				}
				else if (text == LanguageControl.Get("WE_Dialogs", "menu_prism"))
				{
					DialogsManager.ShowDialog(m_componentPlayer.GameWidget, new Prism(m_componentPlayer, s_selectedBlock, null, terrain));
				}
				else if (text == LanguageControl.Get("WE_Dialogs", "menu_square"))
				{
					DialogsManager.ShowDialog(m_componentPlayer.GameWidget, new Square(m_componentPlayer, s_selectedBlock, null, terrain));
				}
				else if (text == LanguageControl.Get("WE_Dialogs", "menu_frame_or_box"))
				{
					if (Check12(LanguageControl.Get("WE_Notifications", "missing_p1p2")))
					{
						DialogsManager.ShowDialog(m_componentPlayer.GameWidget, new Rectangle(m_componentPlayer, s_selectedBlock, s_p1, s_p2, terrain));
					}
				}
				else if (text == LanguageControl.Get("WE_Dialogs", "menu_maze"))
				{
					if (Check12(LanguageControl.Get("WE_Notifications", "missing_p1p2")))
					{
						int wallHeight = Math.Max(1, Math.Abs(s_p1.CellFace.Point.Y - s_p2.CellFace.Point.Y) + 1);
						PreviewMazeFrom12(s_selectedBlock, wallHeight);
					}
				}
				else if (text == LanguageControl.Get("WE_Dialogs", "menu_mountain"))
				{
					DialogsManager.ShowDialog(m_componentPlayer.GameWidget, new Mountain(m_componentPlayer, null, terrain));
				}
				else if (text == LanguageControl.Get("WE_Dialogs", "menu_line"))
				{
					if (Check12(LanguageControl.Get("WE_Notifications", "missing_p1p2")))
					{
						PreviewLineFromP1P2(s_selectedBlock);
					}
				}
				else if (text == LanguageControl.Get("WE_Dialogs", "menu_copy_paste_file"))
				{
					DialogsManager.ShowDialog(m_componentPlayer.GameWidget, new ZoneDialog(m_componentPlayer, s_p1, s_p2, s_p3, terrain));
				}
				else if (text == LanguageControl.Get("WE_Dialogs", "menu_undo_redo"))
				{
					DialogsManager.ShowDialog(m_componentPlayer.GameWidget, new UndoRedoSettingsDialog(this));
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex);
				m_componentPlayer.ComponentGui.DisplaySmallMessage(string.Format(LanguageControl.Get("WE_Notifications", "we_error_prefix"), ex.Message), Color.White, blinking: true, playNotificationSound: true);
			}
		}));
	}

	private void WE_EmbedFromMemory(SubsystemTerrain terrain)
	{
		if (!Check3(LanguageControl.Get("WE_Notifications", "missing_p3")))
		{
			return;
		}
		EnsureFurniturePresentAndRemap();
		int weMaxBlocksAllowed = GetWeMaxBlocksAllowed();
		if (weMaxBlocksAllowed != int.MaxValue && s_memCount > 0)
		{
			GetMemBounds(out var min, out var max);
			int num = max.X - min.X + 1;
			int num2 = max.Y - min.Y + 1;
			int num3 = max.Z - min.Z + 1;
			if ((long)num * (long)num2 * num3 > weMaxBlocksAllowed)
			{
				int num4 = num;
				int num5 = num2;
				int num6 = num3;
				for (int i = 0; i < 3; i++)
				{
					if ((long)num4 * (long)num5 * num6 <= weMaxBlocksAllowed)
					{
						break;
					}
					if (num4 >= num5 && num4 >= num6)
					{
						int num7 = Math.Max(1, weMaxBlocksAllowed / Math.Max(1, num5 * num6));
						if (num7 < num4)
						{
							num4 = num7;
						}
					}
					else if (num5 >= num4 && num5 >= num6)
					{
						int num8 = Math.Max(1, weMaxBlocksAllowed / Math.Max(1, num4 * num6));
						if (num8 < num5)
						{
							num5 = num8;
						}
					}
					else
					{
						int num9 = Math.Max(1, weMaxBlocksAllowed / Math.Max(1, num4 * num5));
						if (num9 < num6)
						{
							num6 = num9;
						}
					}
				}
				int x = min.X;
				int y = min.Y;
				int z = min.Z;
				int num10 = x + num4 - 1;
				int num11 = y + num5 - 1;
				int num12 = z + num6 - 1;
				List<BlockMem> list = CloneMemList(s_mem);
				Dictionary<Point3, EntitySnapshot> oldMemEntities = CloneEntitiesDict(s_memEntities);
				Dictionary<int, FurnitureSnapshot> oldMemFurniture = CloneFurnitureDict(s_memFurniture);
				for (int num13 = s_mem.Count - 1; num13 >= 0; num13--)
				{
					BlockMem blockMem = s_mem[num13];
					if (blockMem.x < x || blockMem.x > num10 || blockMem.y < y || blockMem.y > num11 || blockMem.z < z || blockMem.z > num12)
					{
						s_mem.RemoveAt(num13);
					}
				}
				List<Point3> list2 = null;
				foreach (KeyValuePair<Point3, EntitySnapshot> s_memEntity in s_memEntities)
				{
					Point3 key = s_memEntity.Key;
					if (key.X < x || key.X > num10 || key.Y < y || key.Y > num11 || key.Z < z || key.Z > num12)
					{
						if (list2 == null)
						{
							list2 = new List<Point3>();
						}
						list2.Add(key);
					}
				}
				if (list2 != null)
				{
					for (int j = 0; j < list2.Count; j++)
					{
						s_memEntities.Remove(list2[j]);
					}
				}
				s_memCount = s_mem.Count;
				if (s_memCount != list.Count)
				{
					List<BlockMem> newMem = CloneMemList(s_mem);
					Dictionary<Point3, EntitySnapshot> newMemEntities = CloneEntitiesDict(s_memEntities);
					Dictionary<int, FurnitureSnapshot> newMemFurniture = CloneFurnitureDict(s_memFurniture);
					PushUndo(new WeAction
					{
						Type = WeActionType.MemChange,
						OldMem = list,
						NewMem = newMem,
						OldMemEntities = oldMemEntities,
						NewMemEntities = newMemEntities,
						OldMemFurniture = oldMemFurniture,
						NewMemFurniture = newMemFurniture
					});
					InvalidateGhostCache();
				}
			}
		}
		BeginTx();
		Point3 point = s_p3.CellFace.Point;
		int num14 = 0;
		int num15 = 0;
		for (int k = 0; k < s_mem.Count; k++)
		{
			BlockMem blockMem2 = s_mem[k];
			int num16 = Terrain.ExtractContents(blockMem2.id);
			if (num16 == 0)
			{
				num15++;
				continue;
			}
			bool flag = false;
			if (CommonLib.WorkType != WorkType.Client)
			{
				flag = s_memDropIds.Contains(num16) || (((m_subsystemGameInfo != null) ? m_subsystemGameInfo.WorldSettings : null)?.IsBlockDiable(blockMem2.id) ?? false);
			}
			if (flag)
			{
				num15++;
				continue;
			}
			int x2 = point.X + blockMem2.x;
			int y2 = point.Y + blockMem2.y;
			int z2 = point.Z + blockMem2.z;
			int cellValue = terrain.Terrain.GetCellValue(x2, y2, z2);
			ChangeCellTracked(terrain, x2, y2, z2, blockMem2.id);
			if (terrain.Terrain.GetCellValue(x2, y2, z2) != cellValue || CommonLib.WorkType == WorkType.Client)
			{
				num14++;
			}
			else
			{
				num15++;
			}
			if (s_memEntities.TryGetValue(new Point3(blockMem2.x, blockMem2.y, blockMem2.z), out var value))
			{
				Point3 dest = new Point3(x2, y2, z2);
				if (CommonLib.WorkType != WorkType.Client)
				{
					ApplyEntitySnapshotTo(dest, blockMem2.id, value);
				}
				else
				{
					AddPendingEntityOp(dest, blockMem2.id, value);
				}
			}
		}
		EndTxPush("EmbedFromMemory");
		if (CommonLib.WorkType == WorkType.Client && _pendingEntityOps != null && _pendingEntityOps.Count > 0)
		{
			CommonLib.Net.QueuePackage(new WorldEditEntitiesPackage(_pendingEntityOps));
			_pendingEntityOps.Clear();
		}
		GetMemBounds(out var min2, out var max2);
		int num17 = max2.X - min2.X + 1;
		int num18 = max2.Y - min2.Y + 1;
		int num19 = max2.Z - min2.Z + 1;
		m_componentPlayer.ComponentGui.DisplaySmallMessage(string.Format(LanguageControl.Get("WE_Notifications", "embedded"), num14.ToString("N0"), $"{num17}x{num18}x{num19}", (num15 > 0) ? string.Format(LanguageControl.Get("WE_Notifications", "skipped_suffix"), num15.ToString("N0")) : string.Empty), Color.LightMagenta, blinking: false, playNotificationSound: false);
	}

	private Color GetClosestColor(Color[] colorArray, Color baseColor)
	{
		if (baseColor.A == 0)
		{
			return new Color(0, 0, 0, 0);
		}
		Color result = Color.Black;
		int num = int.MaxValue;
		for (int i = 0; i < colorArray.Length; i++)
		{
			int diff = GetDiff(colorArray[i], baseColor);
			if (diff < num)
			{
				num = diff;
				result = colorArray[i];
			}
		}
		return result;
	}

	private int GetDiff(Color a, Color b)
	{
		byte b2 = (byte)(a.A - b.A);
		int num = a.R - b.R;
		int num2 = a.G - b.G;
		int num3 = a.B - b.B;
		return b2 * b2 + num * num + num2 * num2 + num3 * num3;
	}

	private int MapColorToBlockId(Color c, SubsystemPalette p)
	{
		if (c.A == 0)
		{
			return 0;
		}
		for (int i = 0; i < p.m_colors.Length && i < s_paletteToBlockId.Length; i++)
		{
			if (p.m_colors[i] == c)
			{
				return s_paletteToBlockId[i];
			}
		}
		return 1;
	}

	private int CreateFurnitureDesign(bool rot, bool pos, int resol, List<int> color, SubsystemTerrain ter, string name)
	{
		FurnitureDesign furnitureDesign = new FurnitureDesign(ter);
		Dictionary<Point3, int> dictionary = new Dictionary<Point3, int>(resol * resol);
		for (int i = 0; i < resol; i++)
		{
			for (int j = 0; j < resol; j++)
			{
				Point3 key;
				if (pos)
				{
					key = ((!rot) ? new Point3(i, j, 0) : new Point3(0, j, i));
				}
				else
				{
					key = new Point3(i, 0, j);
				}
				dictionary[key] = color[i + j * resol];
			}
		}
		int[] array = new int[resol * resol * resol];
		foreach (KeyValuePair<Point3, int> item in dictionary)
		{
			Point3 key2 = item.Key;
			array[key2.X + key2.Y * resol + key2.Z * resol * resol] = item.Value;
		}
		furnitureDesign.SetValues(resol, array);
		furnitureDesign.Name = name;
		FurnitureDesign furnitureDesign2 = ter.SubsystemFurnitureBlockBehavior.TryAddDesign(furnitureDesign);
		return Terrain.MakeBlockValue(366, 0, FurnitureBlock2.SetDesignIndex(0, furnitureDesign2.Index, furnitureDesign2.ShadowStrengthFactor, isLightEmitter: false));
	}

	private static int BuildFurnitureKey(int contents, int designIndex)
	{
		return (contents << 16) | (designIndex & 0xFFFF);
	}

	private static Dictionary<int, FurnitureSnapshot> CloneFurnitureDict(Dictionary<int, FurnitureSnapshot> src)
	{
		Dictionary<int, FurnitureSnapshot> dictionary = new Dictionary<int, FurnitureSnapshot>(src.Count);
		foreach (KeyValuePair<int, FurnitureSnapshot> item in src)
		{
			FurnitureSnapshot value = item.Value;
			FurnitureDesign headClone = ((value.HeadClone != null) ? value.HeadClone.CloneChain()[0] : null);
			FurnitureSnapshot value2 = new FurnitureSnapshot
			{
				Contents = value.Contents,
				OriginalIndices = (int[])((value.OriginalIndices != null) ? value.OriginalIndices.Clone() : null),
				CurrentIndices = (int[])((value.CurrentIndices != null) ? value.CurrentIndices.Clone() : null),
				HeadClone = headClone,
				IsLightEmitter = value.IsLightEmitter,
				ShadowStrengthFactor = value.ShadowStrengthFactor
			};
			dictionary[item.Key] = value2;
		}
		return dictionary;
	}

	private void CaptureFurnitureSnapshotsFromMem()
	{
		if (m_subsystemTerrain == null || m_subsystemTerrain.SubsystemFurnitureBlockBehavior == null)
		{
			return;
		}
		for (int i = 0; i < s_mem.Count; i++)
		{
			BlockMem blockMem = s_mem[i];
			int num = Terrain.ExtractContents(blockMem.id);
			if (num != 227 && num != 366)
			{
				continue;
			}
			int data = Terrain.ExtractData(blockMem.id);
			int num2 = ((num == 227) ? FurnitureBlock.GetDesignIndex(data) : FurnitureBlock2.GetDesignIndex(data));
			int key = BuildFurnitureKey(num, num2);
			if (s_memFurniture.ContainsKey(key))
			{
				continue;
			}
			FurnitureDesign design = m_subsystemTerrain.SubsystemFurnitureBlockBehavior.GetDesign(num2);
			if (design != null)
			{
				List<int> list = new List<int>(8);
				HashSet<int> hashSet = new HashSet<int>();
				FurnitureDesign furnitureDesign = design;
				while (furnitureDesign != null && !hashSet.Contains(furnitureDesign.Index))
				{
					hashSet.Add(furnitureDesign.Index);
					list.Add(furnitureDesign.Index);
					furnitureDesign = furnitureDesign.LinkedDesign;
				}
				List<FurnitureDesign> list2 = design.CloneChain();
				if (list2 != null && list2.Count != 0)
				{
					FurnitureSnapshot value = new FurnitureSnapshot
					{
						Contents = num,
						OriginalIndices = list.ToArray(),
						CurrentIndices = list.ToArray(),
						HeadClone = list2[0],
						IsLightEmitter = list2[0].IsLightEmitter,
						ShadowStrengthFactor = list2[0].ShadowStrengthFactor
					};
					s_memFurniture[key] = value;
				}
			}
		}
	}

	private void EnsureFurniturePresentAndRemap()
	{
		if (m_subsystemTerrain == null || m_subsystemTerrain.SubsystemFurnitureBlockBehavior == null)
		{
			return;
		}
		SubsystemFurnitureBlockBehavior subsystemFurnitureBlockBehavior = m_subsystemTerrain.SubsystemFurnitureBlockBehavior;
		Dictionary<int, int> dictionary = new Dictionary<int, int>(s_memFurniture.Count * 4);
		foreach (KeyValuePair<int, FurnitureSnapshot> item in s_memFurniture)
		{
			FurnitureSnapshot value = item.Value;
			if (value == null || value.HeadClone == null)
			{
				continue;
			}
			FurnitureDesign design = value.HeadClone.CloneChain()[0];
			FurnitureDesign furnitureDesign = subsystemFurnitureBlockBehavior.TryAddDesignChain(design, garbageCollectIfNeeded: true);
			if (furnitureDesign == null)
			{
				continue;
			}
			List<int> list = new List<int>();
			HashSet<int> hashSet = new HashSet<int>();
			FurnitureDesign furnitureDesign2 = furnitureDesign;
			while (furnitureDesign2 != null && !hashSet.Contains(furnitureDesign2.Index))
			{
				hashSet.Add(furnitureDesign2.Index);
				list.Add(furnitureDesign2.Index);
				furnitureDesign2 = furnitureDesign2.LinkedDesign;
			}
			int num = ((value.OriginalIndices != null) ? value.OriginalIndices.Length : 0);
			int num2 = ((value.CurrentIndices != null) ? value.CurrentIndices.Length : 0);
			int num3 = Math.Min(list.Count, Math.Max(num, num2));
			for (int i = 0; i < num3; i++)
			{
				int value2 = list[i];
				if (i < num)
				{
					int designIndex = value.OriginalIndices[i];
					dictionary[BuildFurnitureKey(value.Contents, designIndex)] = value2;
				}
				if (i < num2)
				{
					int designIndex2 = value.CurrentIndices[i];
					dictionary[BuildFurnitureKey(value.Contents, designIndex2)] = value2;
				}
			}
			value.CurrentIndices = list.ToArray();
		}
		for (int j = 0; j < s_mem.Count; j++)
		{
			BlockMem blockMem = s_mem[j];
			int num4 = Terrain.ExtractContents(blockMem.id);
			if (num4 != 227 && num4 != 366)
			{
				continue;
			}
			int data = Terrain.ExtractData(blockMem.id);
			int designIndex3 = ((num4 == 227) ? FurnitureBlock.GetDesignIndex(data) : FurnitureBlock2.GetDesignIndex(data));
			int key = BuildFurnitureKey(num4, designIndex3);
			if (dictionary.TryGetValue(key, out var value3))
			{
				s_memFurniture.TryGetValue(key, out var value4);
				FurnitureDesign design2 = m_subsystemTerrain.SubsystemFurnitureBlockBehavior.GetDesign(value3);
				if (num4 == 227)
				{
					int shadowStrengthFactor = value4?.ShadowStrengthFactor ?? design2?.ShadowStrengthFactor ?? FurnitureBlock.GetShadowStrengthFactor(data);
					bool isLightEmitter = value4?.IsLightEmitter ?? design2?.IsLightEmitter ?? FurnitureBlock.GetIsLightEmitter(data);
					int data2 = FurnitureBlock.SetDesignIndex(data, value3, shadowStrengthFactor, isLightEmitter);
					s_mem[j] = new BlockMem
					{
						x = blockMem.x,
						y = blockMem.y,
						z = blockMem.z,
						id = Terrain.ReplaceData(blockMem.id, data2)
					};
				}
				else
				{
					int shadowStrengthFactor2 = value4?.ShadowStrengthFactor ?? design2?.ShadowStrengthFactor ?? FurnitureBlock2.GetShadowStrengthFactor(data);
					bool isLightEmitter2 = value4?.IsLightEmitter ?? design2?.IsLightEmitter ?? FurnitureBlock2.GetIsLightEmitter(data);
					int data3 = FurnitureBlock2.SetDesignIndex(data, value3, shadowStrengthFactor2, isLightEmitter2);
					s_mem[j] = new BlockMem
					{
						x = blockMem.x,
						y = blockMem.y,
						z = blockMem.z,
						id = Terrain.ReplaceData(blockMem.id, data3)
					};
				}
			}
		}
	}

	public FurnitureDesign TryGetMemFurnitureDesign(int contents, int designIndex)
	{
		foreach (KeyValuePair<int, FurnitureSnapshot> item in s_memFurniture)
		{
			FurnitureSnapshot value = item.Value;
			if (value == null || value.HeadClone == null || value.Contents != contents)
			{
				continue;
			}
			int[] array = value.CurrentIndices ?? value.OriginalIndices;
			if (array == null || array.Length == 0)
			{
				continue;
			}
			FurnitureDesign furnitureDesign = value.HeadClone;
			int num = 0;
			while (num < array.Length && furnitureDesign != null)
			{
				if (array[num] == designIndex)
				{
					return furnitureDesign;
				}
				num++;
				furnitureDesign = furnitureDesign.LinkedDesign;
			}
		}
		return null;
	}

	private static bool KeyOrClick(WidgetInput input, KeyAction action, ButtonWidget btn)
	{
		if (!KeyBindings.IsActionDownOnce(input, action))
		{
			return btn?.IsClicked ?? false;
		}
		return true;
	}

	private int GetWeMaxBlocksAllowed()
	{
		WorldSettings worldSettings = ((m_subsystemGameInfo != null) ? m_subsystemGameInfo.WorldSettings : null);
		if (worldSettings == null || !worldSettings.IsWorldEditIntegrated || worldSettings.WEInterventionUnlimited)
		{
			return int.MaxValue;
		}
		int num = worldSettings.WEInterventionLimit;
		if (num < 10000)
		{
			num = 10000;
		}
		if (num > 1000000)
		{
			num = 1000000;
		}
		return num;
	}

	public void PreviewImageFromFile(int rotate90, bool useFurniture, int scaleStep, int furnitureResolution, bool horizontally, bool rotateFront, string filePath, int paletteOffset)
	{
		try
		{
			Image image;
			try
			{
				image = Image.Load(filePath);
			}
			catch
			{
				ComponentPlayer componentPlayer = m_componentPlayer;
				if (componentPlayer != null && componentPlayer.ComponentGui != null)
				{
					componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get("WE_Notifications", "img_error_read"), Color.White, blinking: true, playNotificationSound: true);
				}
				return;
			}
			if (rotate90 < 0)
			{
				Image image2 = new Image(image.Height, image.Width);
				for (int i = 0; i < image2.Width; i++)
				{
					for (int j = 0; j < image2.Height; j++)
					{
						image2.SetPixel(i, j, image.GetPixel(image.Width - 1 - j, i));
					}
				}
				image = image2;
			}
			else if (rotate90 > 0)
			{
				Image image3 = new Image(image.Height, image.Width);
				for (int k = 0; k < image3.Width; k++)
				{
					for (int l = 0; l < image3.Height; l++)
					{
						image3.SetPixel(k, l, image.GetPixel(l, image.Height - 1 - k));
					}
				}
				image = image3;
			}
			if (horizontally)
			{
				Image image4 = new Image(image.Height, image.Width);
				for (int m = 0; m < image4.Width; m++)
				{
					for (int n = 0; n < image4.Height; n++)
					{
						image4.SetPixel(m, image4.Height - 1 - n, image.GetPixel(n, m));
					}
				}
				image = image4;
			}
			scaleStep = Math.Max(1, scaleStep);
			int num = MathUtils.Clamp(furnitureResolution, 2, 16);
			Func<int, int, int, int, long> func = (int w, int h, int s, int frq) =>
			{
				int num26 = Math.Max(1, w / Math.Max(1, s));
				int num27 = Math.Max(1, h / Math.Max(1, s));
				long num28 = Math.Max(1, (num26 + frq - 1) / frq);
				int num29 = Math.Max(1, (num27 + frq - 1) / frq);
				return num28 * num29;
			};
			if (useFurniture)
			{
				int num2 = 0;
				for (int num3 = 0; num3 < 65535; num3++)
				{
					if (m_subsystemTerrain.SubsystemFurnitureBlockBehavior.GetDesign(num3) != null)
					{
						num2++;
					}
				}
				int num4 = Math.Max(1, 65535 - num2);
				for (int num5 = 0; num5 < 128; num5++)
				{
					if (func(image.Width, image.Height, scaleStep, num) <= num4)
					{
						break;
					}
					if (num < 16)
					{
						num = Math.Min(16, num * 2);
						continue;
					}
					double num6 = Math.Sqrt((double)func(image.Width, image.Height, scaleStep, num) / (double)num4);
					int num7 = Math.Max(1, image.Width / scaleStep);
					int num8 = Math.Max(1, image.Height / scaleStep);
					int num9 = Math.Max(1, (int)Math.Floor((double)num7 / num6));
					int num10 = Math.Max(1, (int)Math.Floor((double)num8 / num6));
					int val = Math.Max(1, Math.Max((int)Math.Ceiling((double)image.Width / (double)num9), (int)Math.Ceiling((double)image.Height / (double)num10)));
					scaleStep = Math.Max(scaleStep + 1, val);
				}
			}
			int num11 = Math.Max(1, image.Width / scaleStep);
			int num12 = Math.Max(1, image.Height / scaleStep);
			Image image5 = new Image(num11, num12);
			for (int num13 = 0; num13 < num11; num13++)
			{
				for (int num14 = 0; num14 < num12; num14++)
				{
					int x = Math.Min(image.Width - 1, num13 * scaleStep);
					int y = Math.Min(image.Height - 1, num14 * scaleStep);
					Color pixel = image.GetPixel(x, y);
					if (pixel.A != 0)
					{
						pixel.A = byte.MaxValue;
					}
					image5.SetPixel(num11 - 1 - num13, num14, pixel);
				}
			}
			image = image5;
			Color[] array = ((m_subsystemTerrain.SubsystemPalette != null) ? m_subsystemTerrain.SubsystemPalette.m_colors : null);
			int offset = paletteOffset & 0xF;
			List<BlockMem> list = new List<BlockMem>();
			if (useFurniture)
			{
				int num15 = Math.Max(1, (image.Width + num - 1) / num);
				int num16 = Math.Max(1, (image.Height + num - 1) / num);
				for (int num17 = 0; num17 < num15; num17++)
				{
					for (int num18 = 0; num18 < num16; num18++)
					{
						List<int> list2 = new List<int>(num * num);
						bool flag = false;
						for (int num19 = 0; num19 < num; num19++)
						{
							for (int num20 = 0; num20 < num; num20++)
							{
								int num21 = num17 * num + num19;
								int num22 = num18 * num + num20;
								if (num21 >= image.Width || num22 >= image.Height)
								{
									list2.Add(0);
									continue;
								}
								Color pixel2 = image.GetPixel(num21, num22);
								if (pixel2.A != 0)
								{
									pixel2.A = byte.MaxValue;
								}
								Color c = ((array != null) ? GetClosestPaletteColorWithOffset(array, pixel2, offset) : pixel2);
								int num23 = MapColorToBlockId(c, m_subsystemTerrain.SubsystemPalette);
								if (num23 != 0)
								{
									flag = true;
								}
								list2.Add(num23);
							}
						}
						if (!flag)
						{
							continue;
						}
						int id = CreateFurnitureDesign(rotateFront, horizontally, num, list2, m_subsystemTerrain, $"x {num17}  y {num18}");
						BlockMem blockMem = new BlockMem();
						if (horizontally)
						{
							if (rotateFront)
							{
								blockMem.x = 0;
								blockMem.y = num17;
								blockMem.z = num18;
							}
							else
							{
								blockMem.x = num18;
								blockMem.y = num17;
								blockMem.z = 0;
							}
						}
						else
						{
							blockMem.x = num18;
							blockMem.y = 0;
							blockMem.z = num17;
						}
						blockMem.id = id;
						list.Add(blockMem);
					}
				}
			}
			else
			{
				for (int num24 = 0; num24 < image.Width; num24++)
				{
					for (int num25 = 0; num25 < image.Height; num25++)
					{
						Color pixel3 = image.GetPixel(num24, num25);
						if (pixel3.A != 0)
						{
							pixel3.A = byte.MaxValue;
						}
						Color c2 = ((array != null) ? GetClosestPaletteColorWithOffset(array, pixel3, offset) : pixel3);
						int id2 = MapColorToBlockId(c2, m_subsystemTerrain.SubsystemPalette);
						BlockMem blockMem2 = new BlockMem();
						if (horizontally)
						{
							if (rotateFront)
							{
								blockMem2.x = 0;
								blockMem2.y = num24;
								blockMem2.z = num25;
							}
							else
							{
								blockMem2.x = num25;
								blockMem2.y = num24;
								blockMem2.z = 0;
							}
						}
						else
						{
							blockMem2.x = num25;
							blockMem2.y = 0;
							blockMem2.z = num24;
						}
						blockMem2.id = id2;
						list.Add(blockMem2);
					}
				}
			}
			LoadPreviewToMemory(list, "Image");
		}
		catch (Exception ex)
		{
			Log.Error(ex);
			ComponentPlayer componentPlayer2 = m_componentPlayer;
			if (componentPlayer2 != null && componentPlayer2.ComponentGui != null)
			{
				componentPlayer2.ComponentGui.DisplaySmallMessage("Image preview failed: " + ex.Message, Color.White, blinking: true, playNotificationSound: true);
			}
		}
	}

	private Color GetClosestPaletteColorWithOffset(Color[] palette, Color c, int offset)
	{
		if (palette == null || palette.Length == 0)
		{
			return c;
		}
		int num = Math.Min(16, palette.Length);
		int num2 = 0;
		int num3 = int.MaxValue;
		for (int i = 0; i < num; i++)
		{
			byte b = (byte)(palette[i].R - c.R);
			int num4 = palette[i].G - c.G;
			int num5 = palette[i].B - c.B;
			int num6 = palette[i].A - c.A;
			int num7 = b * b + num4 * num4 + num5 * num5 + num6 * num6;
			if (num7 < num3)
			{
				num3 = num7;
				num2 = i;
			}
		}
		int num8 = (num2 + (offset & 0xF)) % num;
		return palette[num8];
	}

	public void PreviewRectangle(int mode, int sizeX, int sizeY, int sizeZ, int blockId)
	{
		sizeX = Math.Max(1, sizeX);
		sizeY = Math.Max(1, sizeY);
		sizeZ = Math.Max(1, sizeZ);
		List<BlockMem> list = new List<BlockMem>(sizeX * sizeY * sizeZ);
		for (int item = 0; item < sizeX; item++)
		{
			for (int text = 0; text < sizeY; text++)
			{
				for (int i = 0; i < sizeZ; i++)
				{
					if (ShouldPlaceRectangleBlock(mode, item, text, i, sizeX, sizeY, sizeZ))
					{
						list.Add(new BlockMem
						{
							x = item,
							y = text,
							z = i,
							id = blockId
						});
					}
				}
			}
		}
		LoadPreviewToMemory(list, LanguageControl.Get("WE_Dialogs", "shape_rectangle"));
	}

	private static bool ShouldPlaceRectangleBlock(int mode, int x, int y, int z, int sizeX, int sizeY, int sizeZ)
	{
		if (mode == 0)
		{
			return true;
		}
		bool flag = x == 0;
		bool flag2 = x == sizeX - 1;
		bool region = y == 0;
		bool regionStream = y == sizeY - 1;
		bool num = z == 0;
		bool num2 = z == sizeZ - 1;
		bool num3 = flag | flag2 | region | regionStream | num | num2;
		if (mode == 1)
		{
			return num3;
		}
		return ((flag | flag2) ? 1 : 0) + ((region | regionStream) ? 1 : 0) + ((num | num2) ? 1 : 0) >= 2;
	}

	private void AddPendingEntityOp(Point3 dest, int value, EntitySnapshot snap)
	{
		if (snap != null)
		{
			if (_pendingEntityOps == null)
			{
				_pendingEntityOps = new List<EntitySnapshotOp>();
			}
			EntitySnapshotOp region = default;
			region.Position = dest;
			region.Value = value;
			region.Snapshot = snap;
			_pendingEntityOps.Add(region);
		}
	}
}
