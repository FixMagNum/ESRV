using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Engine;
using Engine.Graphics;
using Game.NetWork;
using Game.NetWork.Packages;

namespace Game;

public class GameLoadingScreen : Screen
{
	public WorldInfo m_worldInfo;

	public string m_worldSnapshotName;

	public StateMachine m_stateMachine = new StateMachine();

	public static string fName;

	public SpawnDialog spawnDialog = new SpawnDialog();

	private IPEndPoint m_serverEndPoint;

	private byte[] m_projectXmlData;

	private byte[] m_blockTextureData;

	private bool ServerHasReply;

	private string m_password;

	private bool IsAdventureRestart;

	private BlockLoadingWidget m_loadingBlocksWidget;

	private Task m_loadingTask;

	private Exception m_loadingException;

	private XElement m_preloadedProjectNode;

	public GameLoadingScreen()
	{
		XElement node = ContentManager.Get<XElement>("Screens/GameLoadingScreen");
		LoadContents(this, node);
		m_loadingBlocksWidget = Children.Find<BlockLoadingWidget>("LoadingBlocks", throwIfNotFound: false);
		m_stateMachine.AddState("WaitingForFadeIn", null, () =>
		{
			if (!ScreensManager.IsAnimating)
			{
				if (m_serverEndPoint != null)
				{
					SetSpwanDialogMessage(LanguageControl.Get(fName, "Connecting"), 0.2f);
					CommonLib.BlockTexture = null;
					CommonLib.Net.ConnectServer(m_serverEndPoint, m_password);
					CommonLib.Net.OnRecieve += OnReceive;
					DialogsManager.ShowDialog(this, spawnDialog);
					m_stateMachine.TransitionTo("Loading");
				}
				else
				{
					m_stateMachine.TransitionTo("AsyncLoading");
				}
			}
		}, null);
		m_stateMachine.AddState("AsyncLoading", () =>
		{
			m_loadingTask = Task.Run(() =>
			{
				try
				{
					if (!string.IsNullOrEmpty(m_worldSnapshotName) && m_worldSnapshotName != "AdventureRestart")
					{
						WorldsManager.RestoreWorldFromSnapshot(m_worldInfo.DirectoryName, m_worldSnapshotName);
					}
					m_preloadedProjectNode = GameManager.PreloadProjectXml(m_worldInfo);
				}
				catch (Exception loadingException)
				{
					m_loadingException = loadingException;
				}
			});
		}, () =>
		{
			if (m_loadingTask != null && m_loadingTask.IsCompleted)
			{
				if (m_loadingException != null)
				{
					throw m_loadingException;
				}
				m_stateMachine.TransitionTo("Loading");
			}
		}, null);
		m_stateMachine.AddState("RestoringSnapshot", null, null, null);
		m_stateMachine.AddState("Loading", () =>
		{
			SetSpwanDialogMessage(LanguageControl.Get(fName, "Downloading"), 0.4f);
		}, () =>
		{
			if (m_serverEndPoint != null)
			{
				if (!CommonLib.Net.IsConnected)
				{
					string message;
					if ((message = CommonLib.Net.Error) == null)
					{
						message = LanguageControl.Get(fName, "Lost");
					}
					throw new Exception(message);
				}
				if (ServerHasReply)
				{
					ServerHasReply = false;
					m_stateMachine.TransitionTo("WaitServerReply");
				}
			}
			else
			{
				ContainerWidget gamesWidget = ScreensManager.FindScreen<GameScreen>("Game").Children.Find<ContainerWidget>("GamesWidget");
				GameManager.LoadProjectFromXml(m_worldInfo, gamesWidget, m_preloadedProjectNode);
				m_preloadedProjectNode = null;
				if (IsAdventureRestart && CommonLib.WorkType == WorkType.Client)
				{
					CommonLib.Net.QueuePackage(new ComponentPlayerPackage(CommonLib.MainPlayer, ComponentPlayerPackage.PlayerAction.IntoPlaying));
				}
				ScreensManager.SwitchScreen("Game");
			}
		}, null);
		m_stateMachine.AddState("WaitServerReply", () =>
		{
			SetSpwanDialogMessage(LanguageControl.Get(fName, "Waiting"), 0.6f);
		}, () =>
		{
			if (CommonLib.Net.CurrentStage == NetNode.Stage.Connected)
			{
				GameManager.LoadNetProject(m_projectXmlData, ScreensManager.FindScreen<GameScreen>("Game").Children.Find<ContainerWidget>("GamesWidget"));
				m_projectXmlData = null;
				m_blockTextureData = null;
				GC.Collect();
				ScreensManager.SwitchScreen("Game");
			}
		}, null);
	}

	public void OnReceive(Package p)
	{
		if (p.Flag != PackageFlag.NetProject)
		{
			return;
		}
		string s = Encoding.UTF8.GetString(CommonLib.Decompress(p.Reader.GetBytesWithLength()));
		if (p.Reader.GetBool())
		{
			m_blockTextureData = CommonLib.Decompress(p.Reader.GetBytesWithLength());
			using MemoryStream stream = new MemoryStream(m_blockTextureData);
			CommonLib.BlockTexture = Texture2D.Load(stream);
		}
		else
		{
			m_blockTextureData = null;
		}
		m_projectXmlData = Encoding.UTF8.GetBytes(s);
		ServerHasReply = true;
		if (m_loadingBlocksWidget != null)
		{
			m_loadingBlocksWidget.Value = 0.8f;
		}
	}

	public void SetSpwanDialogMessage(string msg, float Progress)
	{
		spawnDialog.LargeMessage = msg;
		spawnDialog.Progress = Progress;
		if (m_loadingBlocksWidget != null)
		{
			m_loadingBlocksWidget.Value = Progress;
		}
	}

	public override void Update()
	{
		if (Input.Back || Input.Cancel)
		{
			ScreensManager.SwitchScreen(ScreensManager.PreviousScreen);
		}
		try
		{
			GameManager.UpdateProject();
			m_stateMachine.Update();
		}
		catch (Exception ex)
		{
			Log.Error(ex);
			ScreensManager.SwitchScreen(ScreensManager.PreviousScreen);
			DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get(fName, 1), ExceptionManager.MakeFullErrorMessage(ex), LanguageControl.Get("Usual", "ok"), null, null));
		}
	}

	public override void Leave()
	{
		DialogsManager.HideDialog(spawnDialog);
		CommonLib.Net.OnRecieve -= OnReceive;
		base.Leave();
	}

	public override void Enter(object[] parameters)
	{
		if (parameters.Length < 3)
		{
			m_worldInfo = (WorldInfo)parameters[0];
			m_worldSnapshotName = (string)parameters[1];
			m_serverEndPoint = null;
			IsAdventureRestart = m_worldSnapshotName == "AdventureRestart";
		}
		else
		{
			m_worldSnapshotName = null;
			m_serverEndPoint = (IPEndPoint)parameters[2];
			if (parameters.Length == 4)
			{
				m_password = (string)parameters[3];
			}
		}
		CommonLib.WorkType = ((m_serverEndPoint != null) ? WorkType.Client : CommonLib.GetCurrentWorkType());
		m_stateMachine.TransitionTo("WaitingForFadeIn");
		ProgressManager.UpdateProgress("Loading World", 0f);
	}

	static GameLoadingScreen()
	{
		fName = "GameLoadingScreen";
	}
}
