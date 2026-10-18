using System;
using System.Linq;
using System.Xml.Linq;
using Engine;
using Engine.Graphics;
using Engine.Media;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;

namespace Game;

public class InventorySlotWidget : CanvasWidget, IDragTargetWidget
{
	public BevelledRectangleWidget m_rectangleWidget;

	public RectangleWidget m_highlightWidget;

	public BlockIconWidget m_blockIconWidget;

	public LabelWidget m_countWidget;

	public ValueBarWidget m_healthBarWidget;

	public RectangleWidget m_editOverlayWidget;

	public RectangleWidget m_interactiveOverlayWidget;

	public RectangleWidget m_foodOverlayWidget;

	public LabelWidget m_splitLabelWidget;

	public GameWidget m_gameWidget;

	public DragHostWidget m_dragHostWidget;

	public IInventory m_inventory;

	public int m_slotIndex;

	public DragMode? m_dragMode;

	public bool m_focus;

	public int m_lastCount = -1;

	public InventoryDragData m_inventoryDragData;

	public SubsystemTerrain m_subsystemTerrain;

	public ComponentPlayer m_componentPlayer;

	public static double m_lastClickTime;

	public static InventorySlotWidget m_lastClickedSlot;

	public bool HideBlockIcon { get; set; }

	public bool HideEditOverlay { get; set; }

	public bool HideInteractiveOverlay { get; set; }

	public bool HideFoodOverlay { get; set; }

	public bool HideHighlightRectangle { get; set; }

	public bool HideHealthBar { get; set; }

	public bool ProcessingOnly { get; set; }

	public Color CenterColor
	{
		get
		{
			return m_rectangleWidget.CenterColor;
		}
		set
		{
			m_rectangleWidget.CenterColor = value;
		}
	}

	public Color BevelColor
	{
		get
		{
			return m_rectangleWidget.BevelColor;
		}
		set
		{
			m_rectangleWidget.BevelColor = value;
		}
	}

	public Matrix? CustomViewMatrix
	{
		get
		{
			return m_blockIconWidget.CustomViewMatrix;
		}
		set
		{
			m_blockIconWidget.CustomViewMatrix = value;
		}
	}

	public GameWidget GameWidget
	{
		get
		{
			if (m_gameWidget == null)
			{
				for (ContainerWidget result = ParentWidget; result != null; result = result.ParentWidget)
				{
					if (result is GameWidget num)
					{
						m_gameWidget = num;
						break;
					}
				}
			}
			return m_gameWidget;
		}
	}

	public DragHostWidget DragHostWidget
	{
		get
		{
			if (m_dragHostWidget == null)
			{
				m_dragHostWidget = ((GameWidget != null) ? GameWidget.Children.Find<DragHostWidget>(throwIfNotFound: false) : null);
			}
			return m_dragHostWidget;
		}
	}

	public InventorySlotWidget()
	{
		Size = new Vector2(72f, 72f);
		WidgetsList children = Children;
		Widget[] array = new Widget[7]
		{
			(m_rectangleWidget = new BevelledRectangleWidget
			{
				BevelSize = -2f,
				DirectionalLight = 0.15f,
				CenterColor = Color.Transparent
			}),
			(m_highlightWidget = new RectangleWidget
			{
				FillColor = Color.Transparent,
				OutlineColor = Color.Transparent
			}),
			(m_blockIconWidget = new BlockIconWidget
			{
				HorizontalAlignment = WidgetAlignment.Center,
				VerticalAlignment = WidgetAlignment.Center,
				Margin = new Vector2(2f, 2f)
			}),
			(m_countWidget = new LabelWidget
			{
				Font = ContentManager.Get<BitmapFont>("Fonts/Pericles"),
				FontScale = 1f,
				HorizontalAlignment = WidgetAlignment.Far,
				VerticalAlignment = WidgetAlignment.Far,
				Margin = new Vector2(6f, 2f)
			}),
			(m_healthBarWidget = new ValueBarWidget
			{
				LayoutDirection = LayoutDirection.Vertical,
				HorizontalAlignment = WidgetAlignment.Near,
				VerticalAlignment = WidgetAlignment.Far,
				BarsCount = 3,
				FlipDirection = true,
				LitBarColor = new Color(32, 128, 0),
				UnlitBarColor = new Color(24, 24, 24, 64),
				BarSize = new Vector2(12f, 12f),
				BarSubtexture = ContentManager.Get<Subtexture>("Textures/Atlas/ProgressBar"),
				Margin = new Vector2(4f, 4f)
			}),
			null,
			null
		};
		StackPanelWidget stackPanelWidget = new StackPanelWidget
		{
			Direction = LayoutDirection.Horizontal,
			HorizontalAlignment = WidgetAlignment.Far,
			Margin = new Vector2(3f, 3f)
		};
		RectangleWidget widget = (m_interactiveOverlayWidget = new RectangleWidget
		{
			Subtexture = ContentManager.Get<Subtexture>("Textures/Atlas/InteractiveItemOverlay"),
			Size = new Vector2(13f, 14f),
			FillColor = new Color(160, 160, 160),
			OutlineColor = Color.Transparent
		});
		stackPanelWidget.Children.Add(widget);
		RectangleWidget widget2 = (m_editOverlayWidget = new RectangleWidget
		{
			Subtexture = ContentManager.Get<Subtexture>("Textures/Atlas/EditItemOverlay"),
			Size = new Vector2(12f, 14f),
			FillColor = new Color(160, 160, 160),
			OutlineColor = Color.Transparent
		});
		stackPanelWidget.Children.Add(widget2);
		RectangleWidget widget3 = (m_foodOverlayWidget = new RectangleWidget
		{
			Subtexture = ContentManager.Get<Subtexture>("Textures/Atlas/FoodItemOverlay"),
			Size = new Vector2(11f, 14f),
			FillColor = new Color(160, 160, 160),
			OutlineColor = Color.Transparent
		});
		stackPanelWidget.Children.Add(widget3);
		array[5] = stackPanelWidget;
		array[6] = (m_splitLabelWidget = new LabelWidget
		{
			Text = "Split",
			Font = ContentManager.Get<BitmapFont>("Fonts/Pericles"),
			Color = new Color(255, 64, 0),
			HorizontalAlignment = WidgetAlignment.Near,
			VerticalAlignment = WidgetAlignment.Near,
			Margin = new Vector2(2f, 0f)
		});
		children.Add(array);
	}

	public void AssignInventorySlot(IInventory inventory, int slotIndex)
	{
		m_inventory = inventory;
		m_slotIndex = slotIndex;
		m_subsystemTerrain = inventory?.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_componentPlayer = ((inventory is Component) ? ((Component)inventory).Entity.FindComponent<ComponentPlayer>() : null);
		m_blockIconWidget.DrawBlockEnvironmentData.SubsystemTerrain = m_subsystemTerrain;
	}

	public override void Update()
	{
		base.Update();
		if (m_inventory == null || DragHostWidget == null)
		{
			return;
		}
		WidgetInput input = Input;
		if (input.Hold.HasValue && HitTestGlobal(input.Hold.Value) == this)
		{
			if (input.HoldTime > 0.5f && m_highlightWidget.OutlineColor == Color.Red)
			{
				m_highlightWidget.OutlineColor = Color.Blue;
				m_highlightWidget.FillColor = new Color(0, 0, 255, 128);
			}
			else if (input.HoldTime > 0.5f && m_highlightWidget.OutlineColor == Color.Transparent)
			{
				m_highlightWidget.OutlineColor = Color.Red;
				m_highlightWidget.FillColor = new Color(255, 0, 0, 128);
			}
		}
		else
		{
			m_highlightWidget.OutlineColor = Color.Transparent;
			m_highlightWidget.FillColor = Color.Transparent;
		}
		if (m_inventory.GetSlotCount(m_slotIndex) == 0)
		{
			m_highlightWidget.OutlineColor = Color.Transparent;
			m_highlightWidget.FillColor = Color.Transparent;
		}
		if (m_inventory == null || DragHostWidget == null)
		{
			return;
		}
		ComponentPlayer viewPlayer = GetViewPlayer();
		int slotValue = m_inventory.GetSlotValue(m_slotIndex);
		int num = Terrain.ExtractContents(slotValue);
		Block block = BlocksManager.Blocks[num];
		if (m_componentPlayer != null)
		{
			m_blockIconWidget.DrawBlockEnvironmentData.InWorldMatrix = m_componentPlayer.ComponentBody.Matrix;
		}
		if (m_focus && !input.Press.HasValue)
		{
			m_focus = false;
		}
		else if (input.Tap.HasValue && HitTestGlobal(input.Tap.Value) == this)
		{
			m_focus = true;
		}
		bool flag = false;
		bool flag2 = viewPlayer != null && (viewPlayer.ComponentInput.SplitSourceInventory != null || viewPlayer.ComponentInput.Split10SourceInventory != null);
		if (input.Click.HasValue && HitTestGlobal(input.Click.Value.Start) == this && HitTestGlobal(input.Click.Value.End) == this)
		{
			if (!flag2 && Time.RealTime - m_lastClickTime < 0.35 && m_lastClickedSlot == this)
			{
				flag = true;
				m_lastClickTime = 0.0;
				input.Clear();
			}
			else
			{
				m_lastClickTime = Time.RealTime;
				m_lastClickedSlot = this;
			}
		}
		if (flag || (input.SpecialClick.HasValue && HitTestGlobal(input.SpecialClick.Value.Start) == this && HitTestGlobal(input.SpecialClick.Value.End) == this))
		{
			IInventory inventory = null;
			foreach (InventorySlotWidget item in ((ContainerWidget)RootWidget).AllChildren.OfType<InventorySlotWidget>())
			{
				if (item.m_inventory != null && item.m_inventory != m_inventory && item.Input == Input && item.IsEnabledGlobal && item.IsVisibleGlobal)
				{
					inventory = item.m_inventory;
					break;
				}
			}
			if (inventory != null)
			{
				int num2 = ComponentInventoryBase.FindAcquireSlotForItem(inventory, slotValue);
				if (num2 >= 0)
				{
					HandleMoveItem(m_inventory, m_slotIndex, inventory, num2, m_inventory.GetSlotCount(m_slotIndex));
				}
			}
		}
		if (input.Click.HasValue && HitTestGlobal(input.Click.Value.Start) == this && HitTestGlobal(input.Click.Value.End) == this)
		{
			bool flag3 = false;
			if (viewPlayer != null)
			{
				if (viewPlayer.ComponentInput.SplitSourceInventory == m_inventory && viewPlayer.ComponentInput.SplitSourceSlotIndex == m_slotIndex)
				{
					viewPlayer.ComponentInput.SetSplitSourceInventoryAndSlot(null, -1);
					flag3 = true;
				}
				else if (viewPlayer.ComponentInput.SplitSourceInventory != null)
				{
					flag3 = HandleMoveItem(viewPlayer.ComponentInput.SplitSourceInventory, viewPlayer.ComponentInput.SplitSourceSlotIndex, m_inventory, m_slotIndex, 1);
					AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
				}
			}
			if (viewPlayer != null && viewPlayer.ComponentInput.Split10SourceInventory == m_inventory && viewPlayer.ComponentInput.Split10SourceSlotIndex == m_slotIndex)
			{
				viewPlayer.ComponentInput.SetSplit10SourceInventoryAndSlot(null, -1);
				flag3 = true;
			}
			else if (viewPlayer.ComponentInput.Split10SourceInventory != null)
			{
				flag3 = HandleMoveItem(viewPlayer.ComponentInput.Split10SourceInventory, viewPlayer.ComponentInput.Split10SourceSlotIndex, m_inventory, m_slotIndex, 9);
				AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
			}
			if (!flag3 && m_inventory.ActiveSlotIndex != m_slotIndex && m_slotIndex < 10)
			{
				m_inventory.ActiveSlotIndex = m_slotIndex;
				if (m_inventory.ActiveSlotIndex == m_slotIndex)
				{
					AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
				}
			}
		}
		if (!m_focus || ProcessingOnly || viewPlayer == null)
		{
			return;
		}
		Vector2? hold = input.Hold;
		if (hold.HasValue && HitTestGlobal(hold.Value) == this && !DragHostWidget.IsDragInProgress && m_inventory.GetSlotCount(m_slotIndex) > 0)
		{
			if (input.HoldTime > 0.35f && input.HoldTime <= 1f && (viewPlayer.ComponentInput.SplitSourceInventory != m_inventory || viewPlayer.ComponentInput.SplitSourceSlotIndex != m_slotIndex))
			{
				input.Clear();
				viewPlayer.ComponentInput.SetSplitSourceInventoryAndSlot(m_inventory, m_slotIndex);
				AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
			}
			else if (input.HoldTime > 0.35f && (viewPlayer.ComponentInput.Split10SourceInventory != m_inventory || viewPlayer.ComponentInput.Split10SourceSlotIndex != m_slotIndex))
			{
				input.Clear();
				viewPlayer.ComponentInput.SetSplit10SourceInventoryAndSlot(m_inventory, m_slotIndex);
				AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
			}
		}
		Vector2? drag = input.Drag;
		if (!drag.HasValue || HitTestGlobal(drag.Value) != this || DragHostWidget.IsDragInProgress)
		{
			return;
		}
		int slotCount = m_inventory.GetSlotCount(m_slotIndex);
		if (slotCount > 0)
		{
			DragMode dragMode = input.DragMode;
			if (viewPlayer.ComponentInput.SplitSourceInventory == m_inventory && viewPlayer.ComponentInput.SplitSourceSlotIndex == m_slotIndex)
			{
				dragMode = DragMode.SingleItem;
			}
			if (viewPlayer.ComponentInput.Split10SourceInventory == m_inventory && viewPlayer.ComponentInput.Split10SourceSlotIndex == m_slotIndex)
			{
				dragMode = DragMode.TenItems;
			}
			int num3 = dragMode switch
			{
				DragMode.TenItems => Math.Min(9, slotCount), 
				DragMode.SingleItem => MathUtils.Max(1, slotCount / 2), 
				_ => slotCount, 
			};
			SubsystemTerrain subsystemTerrain = m_inventory.Project.FindSubsystem<SubsystemTerrain>();
			ContainerWidget containerWidget = (ContainerWidget)Widget.LoadWidget(null, ContentManager.Get<XElement>("Widgets/InventoryDragWidget"), null);
			containerWidget.Children.Find<BlockIconWidget>("InventoryDragWidget.Icon").Value = Terrain.ReplaceLight(slotValue, 15);
			containerWidget.Children.Find<BlockIconWidget>("InventoryDragWidget.Icon").DrawBlockEnvironmentData.SubsystemTerrain = subsystemTerrain;
			containerWidget.Children.Find<LabelWidget>("InventoryDragWidget.Name").Text = block.GetDisplayName(subsystemTerrain, slotValue);
			containerWidget.Children.Find<LabelWidget>("InventoryDragWidget.Count").Text = num3.ToString();
			containerWidget.Children.Find<LabelWidget>("InventoryDragWidget.Count").IsVisible = !(m_inventory is ComponentCreativeInventory) && !(m_inventory is ComponentFurnitureInventory);
			DragHostWidget.BeginDrag(containerWidget, new InventoryDragData
			{
				Inventory = m_inventory,
				SlotIndex = m_slotIndex,
				DragMode = dragMode
			}, () =>
			{
				m_dragMode = null;
			});
			m_dragMode = dragMode;
		}
	}

	public override void MeasureOverride(Vector2 parentAvailableSize)
	{
		if (m_inventory != null)
		{
			bool flag = m_inventory is ComponentCreativeInventory || m_inventory is ComponentFurnitureInventory;
			int num = m_inventory.GetSlotCount(m_slotIndex);
			if (!flag && m_dragMode.HasValue)
			{
				num = ((m_dragMode.Value != DragMode.AllItems) ? MathUtils.Max(num - 1, 0) : 0);
			}
			m_rectangleWidget.IsVisible = true;
			if (num > 0)
			{
				int slotValue = m_inventory.GetSlotValue(m_slotIndex);
				int num2 = Terrain.ExtractContents(slotValue);
				Block block = BlocksManager.Blocks[num2];
				bool flag2 = block.GetRotPeriod(slotValue) > 0 && block.GetDamage(slotValue) > 0;
				m_blockIconWidget.Value = Terrain.ReplaceLight(slotValue, 15);
				m_blockIconWidget.IsVisible = !HideBlockIcon;
				if (num != m_lastCount)
				{
					m_countWidget.Text = num.ToString();
					m_lastCount = num;
				}
				m_countWidget.IsVisible = num > 1 && !flag;
				m_editOverlayWidget.IsVisible = !HideEditOverlay && block.IsEditable;
				m_interactiveOverlayWidget.IsVisible = !HideInteractiveOverlay && ((m_subsystemTerrain != null) ? block.IsInteractive(m_subsystemTerrain, slotValue) : block.DefaultIsInteractive);
				m_foodOverlayWidget.IsVisible = !HideFoodOverlay && block.GetRotPeriod(slotValue) > 0;
				m_foodOverlayWidget.FillColor = (flag2 ? new Color(128, 64, 0) : new Color(160, 160, 160));
				if (!flag && !HideHealthBar && block.Durability >= 0)
				{
					int damage = block.GetDamage(slotValue);
					m_healthBarWidget.IsVisible = true;
					m_healthBarWidget.Value = (float)(block.Durability - damage) / (float)block.Durability;
				}
				else
				{
					m_healthBarWidget.IsVisible = false;
				}
			}
			else
			{
				m_blockIconWidget.IsVisible = false;
				m_countWidget.IsVisible = false;
				m_healthBarWidget.IsVisible = false;
				m_editOverlayWidget.IsVisible = false;
				m_interactiveOverlayWidget.IsVisible = false;
				m_foodOverlayWidget.IsVisible = false;
			}
			m_highlightWidget.IsVisible = !HideHighlightRectangle;
			m_highlightWidget.OutlineColor = Color.Transparent;
			m_highlightWidget.FillColor = Color.Transparent;
			m_splitLabelWidget.IsVisible = false;
			if (m_slotIndex == m_inventory.ActiveSlotIndex)
			{
				m_highlightWidget.OutlineColor = new Color(0, 0, 0);
				m_highlightWidget.FillColor = new Color(0, 0, 0, 80);
			}
			if (IsSplitMode())
			{
				m_highlightWidget.OutlineColor = new Color(255, 64, 0);
				m_splitLabelWidget.IsVisible = true;
			}
			if (IsSplit10Mode())
			{
				m_highlightWidget.OutlineColor = new Color(0, 64, 255);
				m_splitLabelWidget.IsVisible = true;
			}
		}
		else
		{
			m_rectangleWidget.IsVisible = false;
			m_highlightWidget.IsVisible = false;
			m_blockIconWidget.IsVisible = false;
			m_countWidget.IsVisible = false;
			m_healthBarWidget.IsVisible = false;
			m_editOverlayWidget.IsVisible = false;
			m_interactiveOverlayWidget.IsVisible = false;
			m_foodOverlayWidget.IsVisible = false;
			m_splitLabelWidget.IsVisible = false;
		}
		IsDrawRequired = m_inventoryDragData != null;
		base.MeasureOverride(parentAvailableSize);
	}

	public override void Draw(DrawContext dc)
	{
		if (m_inventory != null && m_inventoryDragData != null)
		{
			int slotValue = m_inventoryDragData.Inventory.GetSlotValue(m_inventoryDragData.SlotIndex);
			if (m_inventory.GetSlotProcessCapacity(m_slotIndex, slotValue) >= 0 || m_inventory.GetSlotCapacity(m_slotIndex, slotValue) > 0)
			{
				float num = 80f * GlobalTransform.Right.Length();
				Vector2 center = Vector2.Transform(ActualSize / 2f, GlobalTransform);
				FlatBatch2D flatBatch2D = dc.PrimitivesRenderer2D.FlatBatch(100);
				flatBatch2D.QueueEllipse(center, new Vector2(num), 0f, new Color(0, 0, 0, 96) * GlobalColorTransform, 64);
				flatBatch2D.QueueEllipse(center, new Vector2(num - 0.5f), 0f, new Color(0, 0, 0, 64) * GlobalColorTransform, 64);
				flatBatch2D.QueueEllipse(center, new Vector2(num + 0.5f), 0f, new Color(0, 0, 0, 48) * GlobalColorTransform, 64);
				flatBatch2D.QueueDisc(center, new Vector2(num), 0f, new Color(0, 0, 0, 48) * GlobalColorTransform, 64);
			}
		}
		m_inventoryDragData = null;
	}

	public void DragOver(Widget dragWidget, object data)
	{
		m_inventoryDragData = data as InventoryDragData;
	}

	public void DragDrop(Widget dragWidget, object data)
	{
		InventoryDragData path = data as InventoryDragData;
		if (m_inventory != null && path != null)
		{
			HandleDragDrop(path.Inventory, path.SlotIndex, path.DragMode, m_inventory, m_slotIndex, ProcessingOnly, m_componentPlayer);
		}
	}

	public ComponentPlayer GetViewPlayer()
	{
		if (GameWidget == null)
		{
			return null;
		}
		return GameWidget.PlayerData.ComponentPlayer;
	}

	public bool IsSplitMode()
	{
		ComponentPlayer list = GetViewPlayer();
		if (list != null && m_inventory != null && m_inventory == list.ComponentInput.SplitSourceInventory)
		{
			return m_slotIndex == list.ComponentInput.SplitSourceSlotIndex;
		}
		return false;
	}

	public static bool HandleMoveItem(IInventory sourceInventory, int sourceSlotIndex, IInventory targetInventory, int targetSlotIndex, int count)
	{
		int slotValue = sourceInventory.GetSlotValue(sourceSlotIndex);
		int slotValue2 = targetInventory.GetSlotValue(targetSlotIndex);
		int slotCount = sourceInventory.GetSlotCount(sourceSlotIndex);
		int slotCount2 = targetInventory.GetSlotCount(targetSlotIndex);
		if (sourceInventory.Project.FindSubsystem<SubsystemGameInfo>().WorldSettings.IsBlockDiable(slotValue))
		{
			if (sourceInventory is Component component)
			{
				component.Entity.FindComponent<ComponentPlayer>()?.ComponentGui.DisplaySmallMessage("This item has been disabled.", Color.Red, blinking: false, playNotificationSound: true);
			}
			return false;
		}
		InventorySlot inventorySlot = new InventorySlot();
		inventorySlot.inventoryId = sourceInventory.Id;
		inventorySlot.slotIndex = sourceSlotIndex;
		InventorySlot inventorySlot2 = new InventorySlot();
		inventorySlot2.inventoryId = targetInventory.Id;
		inventorySlot2.slotIndex = targetSlotIndex;
		inventorySlot2.count = count;
		if (CommonLib.WorkType == WorkType.Client)
		{
			IPackage package = new ComponentInventoryPackage(inventorySlot, inventorySlot2, ComponentInventoryPackage.EventType.HandleMoveItem);
			CommonLib.Net.QueuePackage(package);
		}
		if (slotCount2 == 0 || slotValue == slotValue2)
		{
			int num = MathUtils.Min(targetInventory.GetSlotCapacity(targetSlotIndex, slotValue) - slotCount2, slotCount, count);
			if (num > 0)
			{
				int count2 = sourceInventory.RemoveSlotItems(sourceSlotIndex, num);
				targetInventory.AddSlotItems(targetSlotIndex, slotValue, count2);
				if (targetInventory is Component component2)
				{
					ComponentCreature componentCreature = component2.Entity.FindComponent<ComponentCreature>();
					if (componentCreature != null && componentCreature.PlayerAchievements != null && Terrain.ExtractContents(slotValue) == 27)
					{
						PlayerAchievements playerAchievements = componentCreature.PlayerAchievements;
						if (playerAchievements.Master == 0L)
						{
							playerAchievements.Master = 1L;
							playerAchievements.EnqueueMasterAchievement();
							ComponentPlayer componentPlayer = componentCreature.Entity.FindComponent<ComponentPlayer>();
							if (componentPlayer != null)
							{
								PlayerStats playerStats = componentPlayer.PlayerStats;
								if (playerStats.PlayerKills + playerStats.LandCreatureKills + playerStats.WaterCreatureKills + playerStats.AirCreatureKills == 0L && playerAchievements.MasterPacifist == 0L)
								{
									playerAchievements.MasterPacifist = 1L;
									playerAchievements.EnqueueMasterPacifistAchievement();
								}
							}
						}
					}
					if (componentCreature != null && componentCreature.PlayerAchievements != null && Terrain.ExtractContents(slotValue) == 150)
					{
						PlayerAchievements playerAchievements2 = componentCreature.PlayerAchievements;
						if (playerAchievements2.FoundCoalBlock == 0L)
						{
							playerAchievements2.FoundCoalBlock = 1L;
							playerAchievements2.EnqueueFoundCoalBlockAchievement();
						}
					}
					if (componentCreature != null && componentCreature.PlayerAchievements != null)
					{
						int num2 = Terrain.ExtractContents(slotValue);
						int num3 = Terrain.ExtractData(slotValue);
						if (num2 == 203 && (num3 & 0xFF) == 44)
						{
							PlayerAchievements playerAchievements3 = componentCreature.PlayerAchievements;
							if (playerAchievements3.CriticalOverheat == 0L)
							{
								playerAchievements3.CriticalOverheat = 1L;
								playerAchievements3.EnqueueCriticalOverheatAchievement();
							}
						}
					}
					if (componentCreature != null && componentCreature.PlayerAchievements != null && Terrain.ExtractContents(slotValue) == 332)
					{
						PlayerAchievements playerAchievements4 = componentCreature.PlayerAchievements;
						if (playerAchievements4.BreadAboveAll == 0L)
						{
							playerAchievements4.BreadAboveAll = 1L;
							playerAchievements4.EnqueueBreadAboveAllAchievement();
						}
					}
				}
				return true;
			}
		}
		return false;
	}

	public static bool HandleDragDrop(IInventory sourceInventory, int sourceSlotIndex, DragMode dragMode, IInventory targetInventory, int targetSlotIndex, bool processingOnly, ComponentPlayer componentPlayer = null)
	{
		int slotValue = sourceInventory.GetSlotValue(sourceSlotIndex);
		int slotValue2 = targetInventory.GetSlotValue(targetSlotIndex);
		int num = sourceInventory.GetSlotCount(sourceSlotIndex);
		int slotCount = targetInventory.GetSlotCount(targetSlotIndex);
		int slotCapacity = targetInventory.GetSlotCapacity(targetSlotIndex, slotValue);
		int slotProcessCapacity = targetInventory.GetSlotProcessCapacity(targetSlotIndex, slotValue);
		if (sourceInventory.Project.FindSubsystem<SubsystemGameInfo>().WorldSettings.IsBlockDiable(slotValue))
		{
			if (sourceInventory is Component component)
			{
				component.Entity.FindComponent<ComponentPlayer>()?.ComponentGui.DisplaySmallMessage("This item has been disabled.", Color.Red, blinking: false, playNotificationSound: true);
			}
			return false;
		}
		InventorySlot inventorySlot = new InventorySlot();
		inventorySlot.inventoryId = sourceInventory.Id;
		inventorySlot.slotIndex = sourceSlotIndex;
		InventorySlot inventorySlot2 = new InventorySlot();
		inventorySlot2.inventoryId = targetInventory.Id;
		inventorySlot2.slotIndex = targetSlotIndex;
		if (CommonLib.WorkType == WorkType.Client)
		{
			ComponentInventoryPackage componentInventoryPackage = new ComponentInventoryPackage(inventorySlot, inventorySlot2, ComponentInventoryPackage.EventType.HandleDragDrop);
			componentInventoryPackage.m_dragMode = dragMode;
			componentInventoryPackage.m_processingOnly = processingOnly;
			CommonLib.Net.QueuePackage(componentInventoryPackage);
		}
		switch (dragMode)
		{
		case DragMode.SingleItem:
			num = MathUtils.Max(1, num / 2);
			break;
		case DragMode.TenItems:
			num = MathUtils.Min(num, 10);
			break;
		}
		bool flag = false;
		if (slotProcessCapacity > 0)
		{
			int count = MathUtils.Min(num, slotProcessCapacity);
			int processCount = sourceInventory.RemoveSlotItems(sourceSlotIndex, count);
			targetInventory.ProcessSlotItems(sourceInventory, sourceSlotIndex, targetSlotIndex, slotValue, num, processCount, out var processedValue, out var processedCount);
			if (processedValue != 0 && processedCount != 0)
			{
				int count2 = MathUtils.Min(sourceInventory.GetSlotCapacity(sourceSlotIndex, processedValue), processedCount);
				sourceInventory.AddSlotItems(sourceSlotIndex, processedValue, count2);
			}
			flag = true;
		}
		else if (!processingOnly && (slotCount == 0 || slotValue == slotValue2) && slotCount < slotCapacity)
		{
			int num2 = MathUtils.Min(slotCapacity - slotCount, num);
			if (num2 > 0)
			{
				int count3 = sourceInventory.RemoveSlotItems(sourceSlotIndex, num2);
				targetInventory.AddSlotItems(targetSlotIndex, slotValue, count3);
				flag = true;
			}
		}
		else if (!processingOnly && targetInventory.GetSlotCapacity(targetSlotIndex, slotValue) >= num && sourceInventory.GetSlotCapacity(sourceSlotIndex, slotValue2) >= slotCount && sourceInventory.GetSlotCount(sourceSlotIndex) == num)
		{
			int count4 = targetInventory.RemoveSlotItems(targetSlotIndex, slotCount);
			int count5 = sourceInventory.RemoveSlotItems(sourceSlotIndex, num);
			targetInventory.AddSlotItems(targetSlotIndex, slotValue, count5);
			sourceInventory.AddSlotItems(sourceSlotIndex, slotValue2, count4);
			flag = true;
		}
		if (flag && componentPlayer != null && componentPlayer.PlayerData.IsMainPlayer)
		{
			AudioManager.PlaySound("Audio/UI/ItemMoved", 1f, 0f, 0f);
		}
		if (flag && targetInventory is Component component2)
		{
			ComponentCreature componentCreature = component2.Entity.FindComponent<ComponentCreature>();
			if (componentCreature != null && componentCreature.PlayerAchievements != null && Terrain.ExtractContents(slotValue) == 27)
			{
				PlayerAchievements playerAchievements = componentCreature.PlayerAchievements;
				if (playerAchievements.Master == 0L)
				{
					playerAchievements.Master = 1L;
					playerAchievements.EnqueueMasterAchievement();
					ComponentPlayer componentPlayer2 = componentCreature.Entity.FindComponent<ComponentPlayer>();
					if (componentPlayer2 != null)
					{
						PlayerStats playerStats = componentPlayer2.PlayerStats;
						if (playerStats.PlayerKills + playerStats.LandCreatureKills + playerStats.WaterCreatureKills + playerStats.AirCreatureKills == 0L && playerAchievements.MasterPacifist == 0L)
						{
							playerAchievements.MasterPacifist = 1L;
							playerAchievements.EnqueueMasterPacifistAchievement();
						}
					}
				}
			}
			if (componentCreature != null && componentCreature.PlayerAchievements != null && Terrain.ExtractContents(slotValue) == 150)
			{
				PlayerAchievements playerAchievements2 = componentCreature.PlayerAchievements;
				if (playerAchievements2.FoundCoalBlock == 0L)
				{
					playerAchievements2.FoundCoalBlock = 1L;
					playerAchievements2.EnqueueFoundCoalBlockAchievement();
				}
			}
			if (componentCreature != null && componentCreature.PlayerAchievements != null)
			{
				int num3 = Terrain.ExtractContents(slotValue);
				int num4 = Terrain.ExtractData(slotValue);
				if (num3 == 203 && (num4 & 0xFF) == 44)
				{
					PlayerAchievements playerAchievements3 = componentCreature.PlayerAchievements;
					if (playerAchievements3.CriticalOverheat == 0L)
					{
						playerAchievements3.CriticalOverheat = 1L;
						playerAchievements3.EnqueueCriticalOverheatAchievement();
					}
				}
			}
			if (componentCreature != null && componentCreature.PlayerAchievements != null && Terrain.ExtractContents(slotValue) == 332)
			{
				PlayerAchievements playerAchievements4 = componentCreature.PlayerAchievements;
				if (playerAchievements4.BreadAboveAll == 0L)
				{
					playerAchievements4.BreadAboveAll = 1L;
					playerAchievements4.EnqueueBreadAboveAllAchievement();
				}
			}
		}
		return flag;
	}

	public bool IsSplit10Mode()
	{
		ComponentPlayer viewPlayer = GetViewPlayer();
		if (viewPlayer != null && m_inventory != null && m_inventory == viewPlayer.ComponentInput.Split10SourceInventory)
		{
			return m_slotIndex == viewPlayer.ComponentInput.Split10SourceSlotIndex;
		}
		return false;
	}
}
