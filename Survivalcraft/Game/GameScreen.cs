using System;
using System.IO;
using System.Text;
using System.Xml.Linq;
using Engine;
using Engine.Graphics;
using Game.NetWork;

namespace Game;

public class GameScreen : Screen
{
	public double m_lastAutosaveTime;

	public static Stream m_positionStream;

	public static StreamWriter m_positionWriter;

	public static Stream m_inventoryStream;

	public static StreamWriter m_inventoryWriter;

	public static Stream m_chatStream;

	public static StreamWriter m_chatWriter;

	public GameScreen()
	{
		XElement node = ContentManager.Get<XElement>("Screens/GameScreen");
		LoadContents(this, node);
		IsDrawRequired = true;
		Window.Deactivated += () =>
		{
			GameManager.SaveProject(waitForCompletion: true, showErrorDialog: false);
		};
		EnsurePositionLogOpened();
		EnsureInventoryLogOpened();
		EnsureChatLogOpened();
	}

	public override void Enter(object[] parameters)
	{
		if (GameManager.Project != null)
		{
			GameManager.Project.FindSubsystem<SubsystemAudio>(throwOnError: true).Unmute();
			if (CommonLib.WorkType == WorkType.Client)
			{
				Window.Frame -= CommonLib.Net.Update;
			}
			else if (CommonLib.WorkType == WorkType.Server)
			{
				Window.Frame -= CommonLib.Net.Update;
			}
		}
		MusicManager.CurrentMix = MusicManager.Mix.None;
	}

	public override void Leave()
	{
		if (GameManager.Project != null)
		{
			GameManager.Project.FindSubsystem<SubsystemAudio>(throwOnError: true).Mute();
			GameManager.SaveProject(waitForCompletion: true, showErrorDialog: true);
		}
		if (CommonLib.WorkType == WorkType.Server)
		{
			Window.Frame += CommonLib.Net.Update;
		}
		else if (CommonLib.WorkType == WorkType.Client)
		{
			Window.Frame += CommonLib.Net.Update;
		}
		ShowHideCursors(show: true);
		MusicManager.CurrentMix = MusicManager.Mix.Menu;
	}

	public override void Update()
	{
		try
		{
			if (!CommonLib.IsWorkTypeValid() && ScreensManager.CurrentScreen == this)
			{
				DialogsManager.HideAllDialogs();
				ScreensManager.SwitchScreen("MainMenu");
				if (CommonLib.Net.Error != null)
				{
					ClipboardManager.ClipboardString = CommonLib.Net.Error;
					DialogsManager.Alert(CommonLib.Net.Error);
				}
				else
				{
					DialogsManager.Alert("Connection To Server Lost");
				}
			}
			if (GameManager.Project != null)
			{
				double territoriy = Time.RealTime;
				if (territoriy - m_lastAutosaveTime > 120.0)
				{
					m_lastAutosaveTime = territoriy;
					GameManager.SaveProject(waitForCompletion: false, showErrorDialog: true);
				}
				if (CommonLib.WorkType != WorkType.Local)
				{
					CommonLib.Net.Update();
				}
				GameManager.UpdateProject();
			}
			ShowHideCursors(GameManager.Project == null || DialogsManager.HasDialogs(this) || (RootWidget != null && DialogsManager.HasDialogs(RootWidget)) || ScreensManager.CurrentScreen != this);
		}
		catch (Exception ex)
		{
			Log.Error("GameScreen.Update crashed: " + ex.ToString());
		}
	}

	public override void Draw(DrawContext dc)
	{
		if (!ScreensManager.IsAnimating && SettingsManager.ResolutionMode == ResolutionMode.High)
		{
			Display.Clear(Color.Black, 1f, 0);
		}
	}

	public void ShowHideCursors(bool show)
	{
		Input.IsMouseCursorVisible = show;
		Input.IsPadCursorVisible = show;
		Input.IsVrCursorVisible = show;
	}

	private string BuildInventoryBox(IInventory inventory, string playerName, SubsystemTerrain terrain, bool isCreative)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("");
		string size = " Inventory of " + playerName;
		stringBuilder.AppendLine("║" + size.PadRight(38) + "║");
		string num = "╟" + new string('─', 38) + "╢";
		stringBuilder.AppendLine(num);
		int num2 = inventory.SlotsCount;
		if (isCreative && inventory is ComponentCreativeInventory)
		{
			num2 = Math.Min(25, inventory.SlotsCount);
		}
		bool end = false;
		for (int bodyRaycastResult = 0; bodyRaycastResult < num2; bodyRaycastResult++)
		{
			int movingBlocksRaycastResult = inventory.GetSlotValue(bodyRaycastResult);
			int terrainRaycastResult = inventory.GetSlotCount(bodyRaycastResult);
			if (movingBlocksRaycastResult != 0 && terrainRaycastResult != 0)
			{
				int num3 = Terrain.ExtractContents(movingBlocksRaycastResult);
				string num4 = BlocksManager.Blocks[num3].GetDisplayName(terrain, movingBlocksRaycastResult);
				string num5 = $" Slot {bodyRaycastResult}: {num4} (Item{num3}):{terrainRaycastResult}";
				stringBuilder.AppendLine("║" + num5.PadRight(38) + "║");
				end = true;
			}
		}
		if (!end)
		{
			string activeSlotIndex = " No items in inventory";
			stringBuilder.AppendLine("║" + activeSlotIndex.PadRight(38) + "║");
		}
		string inflateAmount = "╚" + new string('═', 38) + "╝";
		stringBuilder.AppendLine(inflateAmount);
		return stringBuilder.ToString();
	}

	private string BuildLevelBox(PlayerData playerData)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("");
		string cellContents = " Level of " + playerData.Name;
		stringBuilder.AppendLine("║" + cellContents.PadRight(38) + "║");
		string blockBehaviors = "╟" + new string('─', 38) + "╢";
		stringBuilder.AppendLine(blockBehaviors);
		string i = " Level: " + playerData.Level.ToString("0.0");
		stringBuilder.AppendLine("║" + i.PadRight(38) + "║");
		string value = "╚" + new string('═', 38) + "╝";
		stringBuilder.AppendLine(value);
		return stringBuilder.ToString();
	}

	private static void EnsurePositionLogOpened()
	{
		try
		{
			if (m_positionStream == null)
			{
				string path = Storage.CombinePaths("app:/Bugs", "Position.log");
				Storage.CreateDirectory("app:/Bugs");
				m_positionStream = Storage.OpenFile(path, OpenFileMode.CreateOrOpen);
				m_positionStream.Position = m_positionStream.Length;
				m_positionWriter = new StreamWriter(m_positionStream);
			}
		}
		catch (Exception ex)
		{
			Log.Error("Error creating Position.log. Reason: {0}", ex.Message);
		}
	}

	private static void EnsureInventoryLogOpened()
	{
		try
		{
			if (m_inventoryStream == null)
			{
				string path = Storage.CombinePaths("app:/Bugs", "Inventory.log");
				Storage.CreateDirectory("app:/Bugs");
				m_inventoryStream = Storage.OpenFile(path, OpenFileMode.CreateOrOpen);
				m_inventoryStream.Position = m_inventoryStream.Length;
				m_inventoryWriter = new StreamWriter(m_inventoryStream);
			}
		}
		catch (Exception ex)
		{
			Log.Error("Error creating Inventory.log. Reason: {0}", ex.Message);
		}
	}

	public static void EnsureChatLogOpened()
	{
		try
		{
			if (m_chatStream == null)
			{
				string path = Storage.CombinePaths("app:/Bugs", "Chat.log");
				Storage.CreateDirectory("app:/Bugs");
				m_chatStream = Storage.OpenFile(path, OpenFileMode.CreateOrOpen);
				m_chatStream.Position = m_chatStream.Length;
				m_chatWriter = new StreamWriter(m_chatStream);
			}
		}
		catch (Exception ex)
		{
			Log.Error("Error creating Chat.log. Reason: " + ex.Message);
		}
	}
}
