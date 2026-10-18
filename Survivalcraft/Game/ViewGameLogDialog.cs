using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Engine;
using Engine.Media;

namespace Game;

public class ViewGameLogDialog : Dialog
{
	public ListPanelWidget m_listPanel;

	public ButtonWidget m_copyButton;

	public ButtonWidget m_filterButton;

	public ButtonWidget m_closeButton;

	public LogType m_filter;

	public ButtonWidget m_prevPageButton;

	public ButtonWidget m_nextPageButton;

	public LabelWidget m_pageLabel;

	private List<string> m_filteredLines = new List<string>();

	public int m_pageIndex;

	public int m_totalPages;

	public const int PageSize = 1000;

	public CheckboxWidget m_debugModeCheckbox;

	public ViewGameLogDialog()
	{
		XElement num = ContentManager.Get<XElement>("Dialogs/ViewGameLogDialog");
		LoadContents(this, num);
		m_listPanel = Children.Find<ListPanelWidget>("ViewGameLogDialog.ListPanel");
		m_copyButton = Children.Find<ButtonWidget>("ViewGameLogDialog.CopyButton");
		m_filterButton = Children.Find<ButtonWidget>("ViewGameLogDialog.FilterButton");
		m_closeButton = Children.Find<ButtonWidget>("ViewGameLogDialog.CloseButton");
		m_prevPageButton = Children.Find<ButtonWidget>("ViewGameLogDialog.PrevPageButton");
		m_nextPageButton = Children.Find<ButtonWidget>("ViewGameLogDialog.NextPageButton");
		m_pageLabel = Children.Find<LabelWidget>("ViewGameLogDialog.PageLabel");
		m_debugModeCheckbox = Children.Find<CheckboxWidget>("ViewGameLogDialog.DebugModeCheckbox");
		if (m_debugModeCheckbox != null)
		{
			m_debugModeCheckbox.IsChecked = SettingsManager.DebugMode;
		}
		ListPanelWidget listPanel = m_listPanel;
		listPanel.ItemClicked = (Action<object>)Delegate.Combine(listPanel.ItemClicked, (Action<object>)((object item) =>
		{
			if (m_listPanel.SelectedItem == item)
			{
				DialogsManager.ShowDialog(ParentWidget, new MessageDialog("Log\u00a0Item", item.ToString(), "OK", null, null));
			}
		}));
		RefreshLogData(jumpToLastPage: true);
	}

	public override void Update()
	{
		if (m_debugModeCheckbox != null)
		{
			bool isChecked = m_debugModeCheckbox.IsChecked;
			if (SettingsManager.DebugMode != isChecked)
			{
				SettingsManager.DebugMode = isChecked;
				RefreshLogData(jumpToLastPage: true);
			}
		}
		if (m_copyButton.IsClicked)
		{
			ClipboardManager.ClipboardString = GameLogSink.GetRecentLog(131072);
		}
		if (m_filterButton.IsClicked)
		{
			if (m_filter < LogType.Warning)
			{
				m_filter = LogType.Warning;
			}
			else
			{
				m_filter = ((m_filter < LogType.Error) ? LogType.Error : LogType.Debug);
			}
			RefreshLogData(jumpToLastPage: true);
		}
		if (Input.Cancel || m_closeButton.IsClicked)
		{
			DialogsManager.HideDialog(this);
		}
		if (m_filter == LogType.Debug)
		{
			m_filterButton.Text = "All";
		}
		else if (m_filter == LogType.Warning)
		{
			m_filterButton.Text = "Warnings";
		}
		else if (m_filter == LogType.Error)
		{
			m_filterButton.Text = "Errors";
		}
		if (m_prevPageButton.IsClicked && m_pageIndex > 0)
		{
			m_pageIndex--;
			PopulateList();
		}
		if (m_nextPageButton.IsClicked && m_pageIndex < m_totalPages - 1)
		{
			m_pageIndex++;
			PopulateList();
		}
		m_prevPageButton.IsEnabled = m_pageIndex > 0;
		m_nextPageButton.IsEnabled = m_pageIndex < m_totalPages - 1;
	}

	public void PopulateList()
	{
		m_listPanel.ItemWidgetFactory = (object item) =>
		{
			string text = ((item != null) ? item.ToString() : string.Empty);
			Color color = Color.Gray;
			if (text.Contains("ERROR:"))
			{
				color = Color.Red;
			}
			else if (text.Contains("WARNING:"))
			{
				color = Color.DarkYellow;
			}
			else if (text.Contains("INFO:"))
			{
				color = Color.LightGray;
			}
			return new LabelWidget
			{
				Text = text,
				Font = BitmapFont.DebugFont,
				HorizontalAlignment = WidgetAlignment.Near,
				VerticalAlignment = WidgetAlignment.Center,
				Color = color
			};
		};
		m_listPanel.ClearItems();
		int num = m_pageIndex * 1000;
		int movingBlockSet = Math.Min(1000, m_filteredLines.Count - num);
		if (num < m_filteredLines.Count && movingBlockSet > 0)
		{
			for (int num2 = 0; num2 < movingBlockSet; num2++)
			{
				m_listPanel.AddItem(m_filteredLines[num + num2]);
			}
		}
		m_listPanel.ScrollPosition = (float)m_listPanel.Items.Count * m_listPanel.ItemSize;
		if (m_pageLabel != null)
		{
			m_pageLabel.Text = $"Page {m_pageIndex + 1}/{m_totalPages} (Total: {m_filteredLines.Count})";
		}
	}

	private void RefreshLogData(bool jumpToLastPage)
	{
		List<string> recentLogLines = GameLogSink.GetRecentLogLines(5242880);
		m_filteredLines.Clear();
		foreach (string num in recentLogLines)
		{
			if (m_filter == LogType.Warning)
			{
				if (!num.Contains("WARNING:") && !num.Contains("ERROR:"))
				{
					continue;
				}
			}
			else if (m_filter == LogType.Error && !num.Contains("ERROR:"))
			{
				continue;
			}
			if (m_filter != LogType.Debug || SettingsManager.DebugMode || !num.Contains("DEBUG:"))
			{
				m_filteredLines.Add(num);
			}
		}
		m_totalPages = (m_filteredLines.Count + 1000 - 1) / 1000;
		if (m_totalPages == 0)
		{
			m_totalPages = 1;
		}
		if (jumpToLastPage)
		{
			m_pageIndex = Math.Max(0, m_totalPages - 1);
		}
		PopulateList();
	}
}
