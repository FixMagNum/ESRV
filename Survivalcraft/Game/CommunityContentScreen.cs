using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Engine;
using Engine.Graphics;
using Engine.Media;

namespace Game;

public class CommunityContentScreen : Screen
{
	public enum Order
	{
		ByRank,
		ByTime
	}

	public ListPanelWidget m_listPanel;

	public LinkWidget m_moreLink;

	public LabelWidget m_orderLabel;

	public ButtonWidget m_changeOrderButton;

	public LabelWidget m_filterLabel;

	public ButtonWidget m_changeFilterButton;

	public ButtonWidget m_downloadButton;

	public ButtonWidget m_deleteButton;

	public ButtonWidget m_moreOptionsButton;

	public ButtonWidget m_searchKey;

	public TextBoxWidget m_inputKey;

	public LabelWidget m_placeHolder;

	public object m_filter;

	public Order m_order;

	public double m_contentExpiryTime;

	public Dictionary<string, IEnumerable<object>> m_itemsCache = new Dictionary<string, IEnumerable<object>>();

	public CommunityContentScreen()
	{
		XElement node = ContentManager.Get<XElement>("Screens/CommunityContentScreen");
		LoadContents(this, node);
		m_listPanel = Children.Find<ListPanelWidget>("List");
		m_orderLabel = Children.Find<LabelWidget>("Order");
		m_changeOrderButton = Children.Find<ButtonWidget>("ChangeOrder");
		m_filterLabel = Children.Find<LabelWidget>("Filter");
		m_changeFilterButton = Children.Find<ButtonWidget>("ChangeFilter");
		m_downloadButton = Children.Find<ButtonWidget>("Download");
		m_deleteButton = Children.Find<ButtonWidget>("Delete");
		m_moreOptionsButton = Children.Find<ButtonWidget>("MoreOptions");
		m_inputKey = Children.Find<TextBoxWidget>("key");
		m_placeHolder = Children.Find<LabelWidget>("placeholder");
		m_searchKey = Children.Find<ButtonWidget>("Search");
		m_listPanel.ItemWidgetFactory = (object item) =>
		{
			if (item is CommunityContentEntry communityContentEntry)
			{
				XElement node2 = ContentManager.Get<XElement>("Widgets/CommunityContentItem");
				ContainerWidget containerWidget = (ContainerWidget)Widget.LoadWidget(this, node2, null);
				communityContentEntry.IconInstance = containerWidget.Children.Find<RectangleWidget>("CommunityContentItem.Icon");
				communityContentEntry.IconInstance.Subtexture = ((communityContentEntry.Icon == null) ? ExternalContentManager.GetEntryTypeIcon(communityContentEntry.Type) : new Subtexture(communityContentEntry.Icon, Vector2.Zero, Vector2.One));
				containerWidget.Children.Find<LabelWidget>("CommunityContentItem.Text").Text = communityContentEntry.Name;
				containerWidget.Children.Find<LabelWidget>("CommunityContentItem.Details").Text = ExternalContentManager.GetEntryTypeDescription(communityContentEntry.Type) + " " + DataSizeFormatter.Format(communityContentEntry.Size);
				containerWidget.Children.Find<StarRatingWidget>("CommunityContentItem.Rating").Rating = communityContentEntry.RatingsAverage;
				containerWidget.Children.Find<StarRatingWidget>("CommunityContentItem.Rating").IsVisible = communityContentEntry.RatingsAverage > 0f;
				containerWidget.Children.Find<LabelWidget>("CommunityContentItem.ExtraText").Text = communityContentEntry.ExtraText;
				return containerWidget;
			}
			XElement node3 = ContentManager.Get<XElement>("Widgets/CommunityContentItemMore");
			ContainerWidget containerWidget2 = (ContainerWidget)Widget.LoadWidget(this, node3, null);
			m_moreLink = containerWidget2.Children.Find<LinkWidget>("CommunityContentItemMore.Link");
			m_moreLink.Tag = item as string;
			return containerWidget2;
		};
		ListPanelWidget listPanel = m_listPanel;
		listPanel.SelectionChanged = (Action)Delegate.Combine(listPanel.SelectionChanged, (Action)(() =>
		{
			if (m_listPanel.SelectedItem != null && !(m_listPanel.SelectedItem is CommunityContentEntry))
			{
				m_listPanel.SelectedItem = null;
			}
		}));
	}

	public override void Enter(object[] parameters)
	{
		if (parameters.Length != 0 && parameters[0].ToString() == "Mod")
		{
			m_filter = ExternalContentType.Mod;
		}
		else
		{
			m_filter = string.Empty;
		}
		m_order = Order.ByRank;
		m_inputKey.Text = string.Empty;
		PopulateList(null);
	}

	public override void Update()
	{
		m_placeHolder.IsVisible = string.IsNullOrEmpty(m_inputKey.Text);
		CommunityContentEntry array = m_listPanel.SelectedItem as CommunityContentEntry;
		m_downloadButton.IsEnabled = array != null;
		m_deleteButton.IsEnabled = UserManager.ActiveUser != null && array != null && array.UserId == UserManager.ActiveUser.UniqueId;
		m_orderLabel.Text = GetOrderDisplayName(m_order);
		m_filterLabel.Text = GetFilterDisplayName(m_filter);
		if (m_changeOrderButton.IsClicked)
		{
			List<Order> i = EnumUtils.GetEnumValues(typeof(Order)).Cast<Order>().ToList();
			DialogsManager.ShowDialog(null, new ListSelectionDialog(LanguageControl.Get(GetType().Name, "Order Type"), i, 60f, (object item) => GetOrderDisplayName((Order)item), (object item) =>
			{
				m_order = (Order)item;
				PopulateList(null);
			}));
		}
		if (m_searchKey.IsClicked)
		{
			PopulateList(null);
		}
		if (m_changeFilterButton.IsClicked)
		{
			List<object> list = new List<object>();
			list.Add(string.Empty);
			foreach (ExternalContentType item in from ExternalContentType t in EnumUtils.GetEnumValues(typeof(ExternalContentType))
				where ExternalContentManager.IsEntryTypeDownloadSupported(t)
				select t)
			{
				list.Add(item);
			}
			if (UserManager.ActiveUser != null)
			{
				list.Add(UserManager.ActiveUser.UniqueId);
			}
			DialogsManager.ShowDialog(null, new ListSelectionDialog(LanguageControl.Get(GetType().Name, "Filter"), list, 60f, (object item) => GetFilterDisplayName(item), (object item) =>
			{
				m_filter = item;
				PopulateList(null);
			}));
		}
		if (m_downloadButton.IsClicked && array != null)
		{
			DownloadEntry(array);
		}
		if (m_deleteButton.IsClicked && array != null)
		{
			DeleteEntry(array);
		}
		if (m_moreOptionsButton.IsClicked)
		{
			DialogsManager.ShowDialog(null, new MoreCommunityLinkDialog());
		}
		if (m_moreLink != null && m_moreLink.IsClicked)
		{
			PopulateList((string)m_moreLink.Tag);
		}
		if (Input.Back || Children.Find<BevelledButtonWidget>("TopBar.Back").IsClicked)
		{
			ScreensManager.SwitchScreen("Content");
		}
		if (Input.Hold.HasValue && Input.HoldTime > 2f && Input.Hold.Value.Y < 20f)
		{
			m_contentExpiryTime = 0.0;
			Task.Delay(250).Wait();
		}
	}

	public void PopulateList(string cursor)
	{
		string slotValue = string.Empty;
		if (SettingsManager.CommunityContentMode == CommunityContentMode.Strict)
		{
			slotValue = "1";
		}
		if (SettingsManager.CommunityContentMode == CommunityContentMode.Normal)
		{
			slotValue = "0";
		}
		string clothingBlock = ((m_filter is string) ? ((string)m_filter) : string.Empty);
		string text2 = ((m_filter is ExternalContentType) ? LanguageControl.Get(GetType().Name, m_filter.ToString()) : string.Empty);
		string text3 = LanguageControl.Get(GetType().Name, m_order.ToString());
		string cacheKey = clothingBlock + "\n" + text2 + "\n" + text3 + "\n" + slotValue + "\n" + m_inputKey.Text;
		m_moreLink = null;
		if (string.IsNullOrEmpty(cursor))
		{
			m_listPanel.ClearItems();
			m_listPanel.ScrollPosition = 0f;
			if (m_contentExpiryTime != 0.0 && Time.RealTime < m_contentExpiryTime && m_itemsCache.TryGetValue(cacheKey, out var image))
			{
				foreach (object item3 in image)
				{
					m_listPanel.AddItem(item3);
				}
				return;
			}
		}
		CancellableBusyDialog busyDialog = new CancellableBusyDialog(LanguageControl.Get(GetType().Name, 2), autoHideOnCancel: false);
		DialogsManager.ShowDialog(null, busyDialog);
		CommunityContentManager.List(cursor, clothingBlock, text2, slotValue, text3, m_inputKey.Text, busyDialog.Progress, (List<CommunityContentEntry> list, string nextCursor) =>
		{
			DialogsManager.HideDialog(busyDialog);
			m_contentExpiryTime = Time.RealTime + 300.0;
			while (m_listPanel.Items.Count > 0 && !(m_listPanel.Items[m_listPanel.Items.Count - 1] is CommunityContentEntry))
			{
				m_listPanel.RemoveItemAt(m_listPanel.Items.Count - 1);
			}
			foreach (CommunityContentEntry item2 in list)
			{
				m_listPanel.AddItem(item2);
				if (item2.Icon == null && !string.IsNullOrEmpty(item2.IconSrc))
				{
					WebManager.Get(item2.IconSrc, null, null, new CancellableProgress(), (byte[] data) =>
					{
						Dispatcher.Dispatch(() =>
						{
							if (data.Length != 0)
							{
								try
								{
									Texture2D texture2D = Texture2D.Load(Image.Load(new MemoryStream(data), ImageFileFormat.Png));
									item2.Icon = texture2D;
									if (item2.IconInstance != null)
									{
										item2.IconInstance.Subtexture = new Subtexture(texture2D, Vector2.Zero, Vector2.One);
									}
								}
								catch (Exception)
								{
								}
							}
						});
					}, (Exception e) =>
					{
					});
				}
				else if (item2.IconInstance != null)
				{
					item2.IconInstance.Subtexture = new Subtexture(item2.Icon, Vector2.Zero, Vector2.One);
				}
			}
			if (list.Count > 0 && !string.IsNullOrEmpty(nextCursor))
			{
				m_listPanel.AddItem(nextCursor);
			}
			m_itemsCache[cacheKey] = new List<object>(m_listPanel.Items);
		}, (Exception error) =>
		{
			DialogsManager.HideDialog(busyDialog);
			DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Error, error.Message, LanguageControl.Ok, null, null));
		});
	}

	public void DownloadEntry(CommunityContentEntry entry)
	{
		string userId = ((UserManager.ActiveUser != null) ? UserManager.ActiveUser.UniqueId : string.Empty);
		CancellableBusyDialog busyDialog = new CancellableBusyDialog(string.Format(LanguageControl.Get(GetType().Name, 1), entry.Name), autoHideOnCancel: false);
		DialogsManager.ShowDialog(null, busyDialog);
		CommunityContentManager.Download(entry.Address, entry.Name, entry.Type, userId, busyDialog.Progress, () =>
		{
			DialogsManager.HideDialog(busyDialog);
		}, (Exception error) =>
		{
			DialogsManager.HideDialog(busyDialog);
			DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Error, error.Message, LanguageControl.Ok, null, null));
		});
	}

	public void DeleteEntry(CommunityContentEntry entry)
	{
		if (UserManager.ActiveUser == null)
		{
			return;
		}
		DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get(GetType().Name, 4), LanguageControl.Get(GetType().Name, 5), LanguageControl.Yes, LanguageControl.No, (MessageDialogButton button) =>
		{
			if (button == MessageDialogButton.Button1)
			{
				CancellableBusyDialog busyDialog = new CancellableBusyDialog(string.Format(LanguageControl.Get(GetType().Name, 3), entry.Name), autoHideOnCancel: false);
				DialogsManager.ShowDialog(null, busyDialog);
				CommunityContentManager.Delete(entry.Address, UserManager.ActiveUser.UniqueId, busyDialog.Progress, () =>
				{
					DialogsManager.HideDialog(busyDialog);
					DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get(GetType().Name, 6), LanguageControl.Get(GetType().Name, 7), LanguageControl.Ok, null, null));
				}, (Exception error) =>
				{
					DialogsManager.HideDialog(busyDialog);
					DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Error, error.Message, LanguageControl.Ok, null, null));
				});
			}
		}));
	}

	public static string GetFilterDisplayName(object filter)
	{
		if (filter is string)
		{
			if (!string.IsNullOrEmpty((string)filter))
			{
				return LanguageControl.Get(typeof(CommunityContentScreen).Name, 8);
			}
			return LanguageControl.Get(typeof(CommunityContentScreen).Name, 9);
		}
		if (filter is ExternalContentType)
		{
			return ExternalContentManager.GetEntryTypeDescription((ExternalContentType)filter);
		}
		throw new InvalidOperationException(LanguageControl.Get(typeof(CommunityContentScreen).Name, 10));
	}

	public static string GetOrderDisplayName(Order order)
	{
		return order switch
		{
			Order.ByRank => LanguageControl.Get(typeof(CommunityContentScreen).Name, 11), 
			Order.ByTime => LanguageControl.Get(typeof(CommunityContentScreen).Name, 12), 
			_ => throw new InvalidOperationException(LanguageControl.Get(typeof(CommunityContentScreen).Name, 13)), 
		};
	}
}
