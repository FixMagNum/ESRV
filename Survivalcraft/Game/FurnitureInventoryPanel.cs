using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Engine;
using Game.NetWork;
using Game.NetWork.Packages;

namespace Game;

public class FurnitureInventoryPanel : CanvasWidget
{
	public CreativeInventoryWidget m_creativeInventoryWidget;

	public ComponentPlayer m_componentPlayer;

	public ListPanelWidget m_furnitureSetList;

	public GridPanelWidget m_inventoryGrid;

	public ButtonWidget m_addButton;

	public ButtonWidget m_moreButton;

	public int m_pagesCount;

	public int m_assignedPage;

	public bool m_ignoreSelectionChanged;

	public bool m_populateNeeded;

	public static string fName;

	public SubsystemTerrain SubsystemTerrain { get; set; }

	public SubsystemFurnitureBlockBehavior SubsystemFurnitureBlockBehavior { get; set; }

	public ComponentFurnitureInventory ComponentFurnitureInventory { get; set; }

	public FurnitureInventoryPanel(CreativeInventoryWidget creativeInventoryWidget)
	{
		m_creativeInventoryWidget = creativeInventoryWidget;
		ComponentFurnitureInventory = creativeInventoryWidget.Entity.FindComponent<ComponentFurnitureInventory>(throwOnError: true);
		m_componentPlayer = creativeInventoryWidget.Entity.FindComponent<ComponentPlayer>(throwOnError: true);
		SubsystemFurnitureBlockBehavior = ComponentFurnitureInventory.Project.FindSubsystem<SubsystemFurnitureBlockBehavior>(throwOnError: true);
		SubsystemTerrain = ComponentFurnitureInventory.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		XElement node = ContentManager.Get<XElement>("Widgets/FurnitureInventoryPanel");
		LoadContents(this, node);
		m_furnitureSetList = Children.Find<ListPanelWidget>("FurnitureSetList");
		m_inventoryGrid = Children.Find<GridPanelWidget>("InventoryGrid");
		m_addButton = Children.Find<ButtonWidget>("AddButton");
		m_moreButton = Children.Find<ButtonWidget>("MoreButton");
		for (int i = 0; i < m_inventoryGrid.RowsCount; i++)
		{
			for (int j = 0; j < m_inventoryGrid.ColumnsCount; j++)
			{
				InventorySlotWidget widget = new InventorySlotWidget();
				m_inventoryGrid.Children.Add(widget);
				m_inventoryGrid.SetWidgetCell(widget, new Point2(j, i));
			}
		}
		ListPanelWidget furnitureSetList = m_furnitureSetList;
		furnitureSetList.ItemWidgetFactory = (Func<object, Widget>)Delegate.Combine(furnitureSetList.ItemWidgetFactory, (Func<object, Widget>)((object item) => new FurnitureSetItemWidget(this, (FurnitureSet)item)));
		ListPanelWidget furnitureSetList2 = m_furnitureSetList;
		furnitureSetList2.SelectionChanged = (Action)Delegate.Combine(furnitureSetList2.SelectionChanged, (Action)(() =>
		{
			if (!m_ignoreSelectionChanged && ComponentFurnitureInventory.FurnitureSet != m_furnitureSetList.SelectedItem as FurnitureSet)
			{
				ComponentFurnitureInventory.PageIndex = 0;
				ComponentFurnitureInventory.FurnitureSet = m_furnitureSetList.SelectedItem as FurnitureSet;
				if (ComponentFurnitureInventory.FurnitureSet == null)
				{
					m_furnitureSetList.SelectedIndex = 0;
				}
				AssignInventorySlots();
			}
		}));
		m_populateNeeded = true;
	}

	public override void Update()
	{
		if (m_populateNeeded)
		{
			Populate();
			m_populateNeeded = false;
		}
		if (ComponentFurnitureInventory.PageIndex != m_assignedPage)
		{
			AssignInventorySlots();
		}
		m_creativeInventoryWidget.PageUpButton.IsEnabled = ComponentFurnitureInventory.PageIndex > 0;
		m_creativeInventoryWidget.PageDownButton.IsEnabled = ComponentFurnitureInventory.PageIndex < m_pagesCount - 1;
		m_creativeInventoryWidget.PageLabel.Text = ((m_pagesCount > 0) ? $"{ComponentFurnitureInventory.PageIndex + 1}/{m_pagesCount}" : string.Empty);
		m_moreButton.IsEnabled = ComponentFurnitureInventory.FurnitureSet != null;
		if (Input.Scroll.HasValue)
		{
			Widget widget = HitTestGlobal(Input.Scroll.Value.XY);
			if (widget != null && widget.IsChildWidgetOf(m_inventoryGrid))
			{
				ComponentFurnitureInventory.PageIndex -= (int)Input.Scroll.Value.Z;
			}
		}
		if (m_creativeInventoryWidget.PageUpButton.IsClicked)
		{
			ComponentFurnitureInventory componentFurnitureInventory = ComponentFurnitureInventory;
			int pageIndex = componentFurnitureInventory.PageIndex - 1;
			componentFurnitureInventory.PageIndex = pageIndex;
		}
		if (m_creativeInventoryWidget.PageDownButton.IsClicked)
		{
			ComponentFurnitureInventory componentFurnitureInventory2 = ComponentFurnitureInventory;
			int pageIndex = componentFurnitureInventory2.PageIndex + 1;
			componentFurnitureInventory2.PageIndex = pageIndex;
		}
		ComponentFurnitureInventory.PageIndex = ((m_pagesCount > 0) ? MathUtils.Clamp(ComponentFurnitureInventory.PageIndex, 0, m_pagesCount - 1) : 0);
		if (m_addButton.IsClicked)
		{
			List<Tuple<string, Action>> items = new List<Tuple<string, Action>>
			{
				new Tuple<string, Action>(LanguageControl.Get(fName, 6), () =>
				{
					if (SubsystemFurnitureBlockBehavior.FurnitureSets.Count < 32)
					{
						NewFurnitureSet();
					}
					else
					{
						DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new MessageDialog(LanguageControl.Get(fName, 24), LanguageControl.Get(fName, 25), LanguageControl.Get("Usual", "ok"), null, null));
					}
				}),
				new Tuple<string, Action>(LanguageControl.Get(fName, 7), () =>
				{
					ImportFurnitureSet(SubsystemTerrain);
				})
			};
			DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new ListSelectionDialog(LanguageControl.Get(fName, 8), items, 64f, (object t) => ((Tuple<string, Action>)t).Item1, (object t) =>
			{
				((Tuple<string, Action>)t).Item2();
			}));
		}
		if (!m_moreButton.IsClicked || ComponentFurnitureInventory.FurnitureSet == null)
		{
			return;
		}
		List<Tuple<string, Action>> items2 = new List<Tuple<string, Action>>
		{
			new Tuple<string, Action>(LanguageControl.Get(fName, 9), () =>
			{
				RenameFurnitureSet();
			}),
			new Tuple<string, Action>(LanguageControl.Get(fName, 28), () =>
			{
				if (SubsystemFurnitureBlockBehavior.GetFurnitureSetDesigns(ComponentFurnitureInventory.FurnitureSet).Count() > 0)
				{
					DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new MessageDialog(LanguageControl.Get("Usual", "warning"), LanguageControl.Get(fName, 26), LanguageControl.Get(fName, 27), LanguageControl.Get(fName, 28), (MessageDialogButton b) =>
					{
						if (b == MessageDialogButton.Button1)
						{
							DeleteFurnitureSet();
						}
					}));
				}
				else
				{
					DeleteFurnitureSet();
				}
			}),
			new Tuple<string, Action>(LanguageControl.Get(fName, 11), () =>
			{
				MoveFurnitureSet(-1);
			}),
			new Tuple<string, Action>(LanguageControl.Get(fName, 12), () =>
			{
				MoveFurnitureSet(1);
			}),
			new Tuple<string, Action>(LanguageControl.Get(fName, 13), () =>
			{
				ExportFurnitureSet();
			})
		};
		DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new ListSelectionDialog(LanguageControl.Get(fName, 14), items2, 64f, (object t) => ((Tuple<string, Action>)t).Item1, (object t) =>
		{
			((Tuple<string, Action>)t).Item2();
		}));
	}

	public override void UpdateCeases()
	{
		base.UpdateCeases();
		ComponentFurnitureInventory.ClearSlots();
		m_populateNeeded = true;
	}

	public void Invalidate()
	{
		m_populateNeeded = true;
	}

	public void Populate()
	{
		ComponentFurnitureInventory.FillSlots();
		try
		{
			m_ignoreSelectionChanged = true;
			m_furnitureSetList.ClearItems();
			m_furnitureSetList.AddItem(null);
			foreach (FurnitureSet furnitureSet in SubsystemFurnitureBlockBehavior.FurnitureSets)
			{
				m_furnitureSetList.AddItem(furnitureSet);
			}
		}
		finally
		{
			m_ignoreSelectionChanged = false;
		}
		m_furnitureSetList.SelectedItem = ComponentFurnitureInventory.FurnitureSet;
		AssignInventorySlots();
	}

	public void AssignInventorySlots()
	{
		List<int> num2 = new List<int>();
		for (int i = 0; i < ComponentFurnitureInventory.SlotsCount; i++)
		{
			int slotValue = ComponentFurnitureInventory.GetSlotValue(i);
			int slotCount = ComponentFurnitureInventory.GetSlotCount(i);
			if (slotValue != 0 && slotCount > 0 && Terrain.ExtractContents(slotValue) == 366)
			{
				int designIndex = FurnitureBlock2.GetDesignIndex(Terrain.ExtractData(slotValue));
				FurnitureDesign design = SubsystemFurnitureBlockBehavior.GetDesign(designIndex);
				if (design != null && design.FurnitureSet == ComponentFurnitureInventory.FurnitureSet)
				{
					num2.Add(i);
				}
			}
		}
		List<InventorySlotWidget> data = new List<InventorySlotWidget>((from w in m_inventoryGrid.Children
			select w as InventorySlotWidget into w
			where w != null
			select w).Cast<InventorySlotWidget>());
		int num3 = ComponentFurnitureInventory.PageIndex * data.Count;
		for (int num4 = 0; num4 < data.Count; num4++)
		{
			if (num3 < num2.Count)
			{
				data[num4].AssignInventorySlot(ComponentFurnitureInventory, num2[num3]);
			}
			else
			{
				data[num4].AssignInventorySlot(null, 0);
			}
			num3++;
		}
		m_pagesCount = (num2.Count + data.Count - 1) / data.Count;
		m_assignedPage = ComponentFurnitureInventory.PageIndex;
	}

	public void NewFurnitueSetLogic(string s, string from = null)
	{
		FurnitureSet num = SubsystemFurnitureBlockBehavior.NewFurnitureSet(s, from);
		ComponentFurnitureInventory.FurnitureSet = num;
		Populate();
		m_furnitureSetList.ScrollToItem(num);
	}

	public void NewFurnitureSet()
	{
		DialogsManager.ShowDialog(ComponentFurnitureInventory.Entity.FindComponent<ComponentPlayer>(throwOnError: true).GameWidget, new TextBoxDialog(LanguageControl.Get(fName, 15), LanguageControl.Get(fName, 16), 20, (string s) =>
		{
			if (s != null)
			{
				NewFurnitueSetLogic(s);
				CommonLib.Net.QueuePackage(new FurniturePackage(s));
			}
		}));
	}

	public void DeleteFurnitureSetLogic(FurnitureSet furnitureSet)
	{
		if (furnitureSet != null)
		{
			int paintColor = SubsystemFurnitureBlockBehavior.FurnitureSets.IndexOf(furnitureSet);
			SubsystemFurnitureBlockBehavior.DeleteFurnitureSet(furnitureSet);
			SubsystemFurnitureBlockBehavior.GarbageCollectDesigns();
			ComponentFurnitureInventory.FurnitureSet = ((paintColor > 0) ? SubsystemFurnitureBlockBehavior.FurnitureSets[paintColor - 1] : null);
			Invalidate();
		}
	}

	public void DeleteFurnitureSet()
	{
		if (m_furnitureSetList.SelectedItem is FurnitureSet furnitureSet)
		{
			DeleteFurnitureSetLogic(furnitureSet);
			CommonLib.Net.QueuePackage(new FurniturePackage(furnitureSet.Name));
		}
	}

	public void RenameFurnitureSetLogic(FurnitureSet furnitureSet, string s)
	{
		if (furnitureSet != null)
		{
			furnitureSet.Name = s;
			Invalidate();
		}
	}

	public void RenameFurnitureSet()
	{
		FurnitureSet furnitureSet = m_furnitureSetList.SelectedItem as FurnitureSet;
		if (furnitureSet == null)
		{
			return;
		}
		ComponentPlayer componentPlayer = ComponentFurnitureInventory.Entity.FindComponent<ComponentPlayer>(throwOnError: true);
		Input.EnterText(componentPlayer.GuiWidget, LanguageControl.Get(fName, 17), furnitureSet.Name, 20, (string s) =>
		{
			if (s != null)
			{
				RenameFurnitureSetLogic(furnitureSet, s);
				CommonLib.Net.QueuePackage(new FurniturePackage(furnitureSet.Name, s));
			}
		});
	}

	public void MoveFurnitureSetLogic(FurnitureSet furnitureSet, int move)
	{
		if (furnitureSet != null)
		{
			SubsystemFurnitureBlockBehavior.MoveFurnitureSet(furnitureSet, move);
			Invalidate();
		}
	}

	public void MoveFurnitureSet(int move)
	{
		if (m_furnitureSetList.SelectedItem is FurnitureSet data)
		{
			MoveFurnitureSetLogic(data, move);
			CommonLib.Net.QueuePackage(new FurniturePackage(data, move));
		}
	}

	private void ImportFurnitureSet(SubsystemTerrain subsystemTerrain, string text)
	{
		int num = 0;
		int num2 = 0;
		List<List<FurnitureDesign>> list = FurnitureDesign.ListChains(FurniturePacksManager.LoadFurniturePack(subsystemTerrain, text));
		List<FurnitureDesign> list2 = new List<FurnitureDesign>();
		SubsystemFurnitureBlockBehavior.GarbageCollectDesigns();
		foreach (List<FurnitureDesign> item in list)
		{
			FurnitureDesign furnitureDesign = SubsystemFurnitureBlockBehavior.TryAddDesignChain(item[0], garbageCollectIfNeeded: false);
			if (furnitureDesign == item[0])
			{
				list2.Add(furnitureDesign);
			}
			else if (furnitureDesign == null)
			{
				num2++;
			}
			else
			{
				num++;
			}
		}
		if (list2.Count > 0)
		{
			FurnitureSet furnitureSet = SubsystemFurnitureBlockBehavior.NewFurnitureSet(FurniturePacksManager.GetDisplayName(text), text);
			if (CommonLib.WorkType == WorkType.Server)
			{
				CommonLib.Net.QueuePackage(new FurniturePackage(furnitureSet));
			}
			foreach (FurnitureDesign item2 in list2)
			{
				SubsystemFurnitureBlockBehavior.AddToFurnitureSet(item2, furnitureSet);
				if (CommonLib.WorkType == WorkType.Server)
				{
					CommonLib.Net.QueuePackage(new FurniturePackage(item2, furnitureSet));
				}
			}
			ComponentFurnitureInventory.FurnitureSet = furnitureSet;
		}
		Invalidate();
		string text2 = string.Format(LanguageControl.Get(fName, 1), list2.Count);
		if (num > 0)
		{
			text2 += string.Format(LanguageControl.Get(fName, 2), num);
		}
		if (num2 > 0)
		{
			text2 += string.Format(LanguageControl.Get(fName, 3), num2, 65535);
		}
		DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new MessageDialog(LanguageControl.Get(fName, 4), text2.Trim(), LanguageControl.Get("Usual", "ok"), null, null));
	}

	public void ImportFurnitureSet(SubsystemTerrain subsystemTerrain)
	{
		FurniturePacksManager.UpdateFurniturePacksList();
		if (FurniturePacksManager.FurniturePackNames.Count() == 0)
		{
			DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new MessageDialog(LanguageControl.Get(fName, 18), LanguageControl.Get(fName, 19), LanguageControl.Ok, null, null));
			return;
		}
		DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new ListSelectionDialog(LanguageControl.Get(fName, 20), FurniturePacksManager.FurniturePackNames, 64f, (object s) => FurniturePacksManager.GetDisplayName((string)s), (object s) =>
		{
			try
			{
				string text = (string)s;
				ImportFurnitureSet(subsystemTerrain, text);
			}
			catch (Exception ex)
			{
				DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new MessageDialog(LanguageControl.Get(fName, 5), ex.Message, LanguageControl.Ok, null, null));
			}
		}));
	}

	public void ExportFurnitureSet()
	{
		try
		{
			FurnitureDesign[] paintColor = SubsystemFurnitureBlockBehavior.GetFurnitureSetDesigns(ComponentFurnitureInventory.FurnitureSet).ToArray();
			string displayName = FurniturePacksManager.GetDisplayName(FurniturePacksManager.CreateFurniturePack(ComponentFurnitureInventory.FurnitureSet.Name, paintColor));
			DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new MessageDialog(LanguageControl.Get(fName, 21), string.Format(LanguageControl.Get(fName, 22), displayName), LanguageControl.Get("Usual", "ok"), null, null));
		}
		catch (Exception ex)
		{
			DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new MessageDialog(LanguageControl.Get(fName, 23), ex.Message, LanguageControl.Get("Usual", "ok"), null, null));
		}
	}

	static FurnitureInventoryPanel()
	{
		fName = "FurnitureInventoryPanel";
	}
}
