using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Engine;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;
using LiteNetLib.Utils;
using TemplatesDatabase;

namespace Game;

public class PlayerData : IDisposable
{
	public enum SpawnMode
	{
		InitialIntro,
		InitialNoIntro,
		Respawn
	}

	public enum PlayerTag
	{
		Short,
		Fat,
		Tall,
		Dwarf
	}

	public static string fName;

	private Project m_project;

	public bool readyToRestart;

	public bool IsDead;

	public Guid PlayerGUID;

	public SubsystemTerrain m_subsystemTerrain;

	public bool ServerManager;

	public SubsystemGameInfo m_subsystemGameInfo;

	public SubsystemSky m_subsystemSky;

	public GameWidget m_gameWidget;

	public StateMachine m_stateMachine;

	public PlayerClass m_playerClass;

	public string m_name;

	public SpawnMode m_spawnMode;

	public double? m_playerDeathTime;

	public double m_terrainWaitStartTime;

	public SpawnDialog m_spawnDialog;

	public float m_progress;

	public List<Guid> Friends;

	public List<Guid> BlackList;

	private bool m_isMainPlayer;

	public Vector3? ClientCachePosition;

	public bool isSetStatus;

	private static readonly Random m_random;

	public Dictionary<int, Vector3> HomePoints;

	private SubsystemWeather m_subsystemWeather;

	private bool m_seasonShown;

	private LabelWidget m_seasonMainLabel;

	private LabelWidget m_seasonPhaseLabel;

	private LabelWidget m_embraceMainLabel;

	private LabelWidget m_embraceHintLabel;

	private bool m_embraceShown;

	public bool WorldEditManager;

	public string CurrentHardwareId;

	public string PreviousHardwareId;

	public bool IsVanish;

	public bool IsServerAdmin;

	public Project Project => m_project;

	public IClient Client => CommonLib.Net.GetClientByGUID(PlayerGUID);

	public byte? ClientId => Client?.ID;

	public bool IsMainPlayer => m_isMainPlayer;

	public int PlayerIndex { get; set; }

	public SubsystemGameWidgets SubsystemGameWidgets { get; set; }

	public SubsystemPlayers SubsystemPlayers { get; set; }

	public ComponentPlayer ComponentPlayer { get; set; }

	public GameWidget GameWidget { get; set; }

	public Vector3 SpawnPosition { get; set; }

	public double FirstSpawnTime { get; set; }

	public double LastSpawnTime { get; set; }

	public int SpawnsCount { get; set; }

	public string Name
	{
		get
		{
			return m_name;
		}
		set
		{
			if (value != m_name)
			{
				m_name = value;
				IsDefaultName = false;
			}
		}
	}

	public bool IsDefaultName { get; set; }

	public PlayerClass PlayerClass
	{
		get
		{
			return m_playerClass;
		}
		set
		{
			if (SubsystemPlayers.PlayersData.Contains(this))
			{
				throw new InvalidOperationException(LanguageControl.Get(fName, 1));
			}
			m_playerClass = value;
		}
	}

	public float Level { get; set; }

	public string CharacterSkinName { get; set; }

	public WidgetInputDevice InputDevice { get; set; }

	public bool IsReadyForPlaying
	{
		get
		{
			if (!(m_stateMachine.CurrentState == "Playing"))
			{
				return m_stateMachine.CurrentState == "PlayerDead";
			}
			return true;
		}
	}

	public PlayerTag Tag { get; set; }

	public int ExtraLives { get; set; }

	public bool IsCheater { get; set; }

	public void AddFriend(Guid guid)
	{
		if (!Friends.Contains(guid))
		{
			Friends.Add(guid);
		}
	}

	public void RemoveFriend(Guid guid)
	{
		if (Friends.Contains(guid))
		{
			Friends.Remove(guid);
		}
	}

	public PlayerData(Project project)
	{
		PlayerGUID = CommonLib.UserId;
		CurrentHardwareId = string.Empty;
		PreviousHardwareId = string.Empty;
		Tag = PlayerTag.Tall;
		m_stateMachine = new StateMachine();
		Friends = new List<Guid>();
		BlackList = new List<Guid>();
		m_project = project;
		SubsystemPlayers = project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
		SubsystemGameWidgets = project.FindSubsystem<SubsystemGameWidgets>(throwOnError: true);
		m_subsystemTerrain = project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemGameInfo = project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemSky = project.FindSubsystem<SubsystemSky>(throwOnError: true);
		m_subsystemWeather = project.FindSubsystem<SubsystemWeather>(throwOnError: true);
		m_playerClass = PlayerClass.Male;
		Level = 1f;
		FirstSpawnTime = -1.0;
		LastSpawnTime = -1.0;
		RandomizeCharacterSkin();
		ResetName();
		InputDevice = WidgetInputDevice.None;
		m_stateMachine.AddState("FirstUpdate", null, () =>
		{
			if ((CommonLib.WorkType != WorkType.Local && CommonLib.Net.GetClientByGUID(PlayerGUID) == null) || (CommonLib.WorkType == WorkType.Local && !IsMainPlayer))
			{
				m_stateMachine.TransitionTo("MakePlayerOffline");
			}
			else if (ComponentPlayer != null)
			{
				UpdateSpawnDialog(string.Format(LanguageControl.Get(fName, 4), Name, MathUtils.Floor(Level)), null, 0f, resetProgress: true);
				m_stateMachine.TransitionTo("WaitForTerrain");
			}
			else
			{
				m_stateMachine.TransitionTo("PrepareSpawn");
			}
		}, null);
		m_stateMachine.AddState("MakePlayerOffline", () =>
		{
			AsyncDispatcher.Dispatch(() =>
			{
				SubsystemPlayers.MakePlayerOffline(this, showMsg: false);
			});
		}, null, null);
		m_stateMachine.AddState("PrepareSpawn", () =>
		{
			if (SpawnPosition == Vector3.Zero)
			{
				if (CommonLib.WorkType != WorkType.Local && m_subsystemGameInfo.WorldSettings.RandomSpawnPosition)
				{
					SubsystemPlayers.GlobalSpawnPosition = Vector3.Zero;
				}
				if (SubsystemPlayers.GlobalSpawnPosition == Vector3.Zero)
				{
					PlayerData playerData = SubsystemPlayers.PlayersData.FirstOrDefault((PlayerData pd) => pd.SpawnPosition != Vector3.Zero);
					if (playerData != null && CommonLib.WorkType == WorkType.Local)
					{
						SpawnPosition = playerData.SpawnPosition;
						m_spawnMode = SpawnMode.InitialNoIntro;
					}
					else
					{
						SpawnPosition = m_subsystemTerrain.TerrainContentsGenerator.FindCoarseSpawnPosition();
						m_spawnMode = ((CommonLib.WorkType == WorkType.Server) ? SpawnMode.InitialNoIntro : SpawnMode.InitialIntro);
					}
					SubsystemPlayers.GlobalSpawnPosition = SpawnPosition;
				}
				else
				{
					SpawnPosition = SubsystemPlayers.GlobalSpawnPosition;
					m_spawnMode = SpawnMode.InitialNoIntro;
				}
			}
			else
			{
				m_spawnMode = SpawnMode.Respawn;
			}
			if (m_spawnMode == SpawnMode.Respawn)
			{
				if (CommonLib.WorkType != WorkType.Client)
				{
					UpdateSpawnDialog(string.Format(LanguageControl.Get(fName, 2), Name, MathUtils.Floor(Level)), GetRandomHint(), 0f, resetProgress: true);
				}
				else
				{
					UpdateSpawnDialog(string.Format(LanguageControl.Get("PlayerData", "connecting_world"), Name, MathUtils.Floor(Level)), GetRandomHint(), 0f, resetProgress: true);
				}
			}
			else
			{
				UpdateSpawnDialog(string.Format(LanguageControl.Get(fName, 4), Name, MathUtils.Floor(Level)), null, 0f, resetProgress: true);
			}
			if (CommonLib.WorkType != WorkType.Client || IsMainPlayer)
			{
				m_subsystemTerrain.TerrainUpdater.SetUpdateLocation(PlayerIndex, SpawnPosition.XZ, 0f, 64f);
			}
			m_terrainWaitStartTime = Time.FrameStartTime;
		}, () =>
		{
			if (Time.PeriodicEvent(0.1, 0.0))
			{
				float updateProgress = m_subsystemTerrain.TerrainUpdater.GetUpdateProgress(PlayerIndex, 0f, 64f);
				UpdateSpawnDialog(null, null, 0.5f * updateProgress, resetProgress: false);
				if (updateProgress >= 1f || Time.FrameStartTime - m_terrainWaitStartTime >= 15.0)
				{
					switch (m_spawnMode)
					{
					case SpawnMode.InitialIntro:
						SpawnPosition = FindIntroSpawnPosition(SpawnPosition.XZ);
						break;
					case SpawnMode.InitialNoIntro:
						SpawnPosition = FindNoIntroSpawnPosition(SpawnPosition, respawn: false);
						break;
					case SpawnMode.Respawn:
						SpawnPosition = FindNoIntroSpawnPosition(SpawnPosition, respawn: true);
						break;
					default:
						throw new InvalidOperationException(LanguageControl.Get(fName, 5));
					}
					m_stateMachine.TransitionTo("WaitForTerrain");
				}
			}
		}, null);
		m_stateMachine.AddState("WaitForTerrain", () =>
		{
			m_terrainWaitStartTime = Time.FrameStartTime;
			Vector2 center = ((ComponentPlayer != null) ? ComponentPlayer.ComponentBody.Position.XZ : SpawnPosition.XZ);
			if (ClientCachePosition.HasValue)
			{
				center = ClientCachePosition.Value.XZ;
				ClientCachePosition = null;
			}
			if (CommonLib.WorkType != WorkType.Client || IsMainPlayer)
			{
				m_subsystemTerrain.TerrainUpdater.SetUpdateLocation(PlayerIndex, center, MathUtils.Min(SettingsManager.VisibilityRange, 32f), 0f);
			}
		}, () =>
		{
			if (Time.PeriodicEvent(0.1, 0.0))
			{
				float updateProgress = m_subsystemTerrain.TerrainUpdater.GetUpdateProgress(PlayerIndex, MathUtils.Min(SettingsManager.VisibilityRange, 64f), 0f);
				UpdateSpawnDialog(null, null, 0.5f + 0.5f * updateProgress, resetProgress: false);
				if ((updateProgress >= 1f && Time.FrameStartTime - m_terrainWaitStartTime > 2.0) || Time.FrameStartTime - m_terrainWaitStartTime >= 15.0)
				{
					if (ComponentPlayer == null || ComponentPlayer.PlayerData.PlayerGUID == Guid.Empty)
					{
						if (CommonLib.WorkType == WorkType.Client)
						{
							m_stateMachine.TransitionTo("WaitForPlayerEntity");
							return;
						}
						SpawnPlayer(SpawnPosition, m_spawnMode);
					}
					m_stateMachine.TransitionTo("Playing");
				}
			}
		}, null);
		m_stateMachine.AddState("WaitForPlayerEntity", null, () =>
		{
			UpdateSpawnDialog(LanguageControl.Get("PlayerData", "waiting_for_server_player_entity"), null, 0f, resetProgress: false);
			if (Time.PeriodicEvent(0.1, 0.0) && ComponentPlayer != null)
			{
				m_stateMachine.TransitionTo("Playing");
			}
		}, null);
		m_stateMachine.AddState("Playing", () =>
		{
			HideSpawnDialog();
			if (CommonLib.WorkType == WorkType.Client)
			{
				CommonLib.Net.Send(CommonLib.Net.MakePackage(PackageFlag.ClientState, (NetDataWriter w) =>
				{
					w.Put(4);
				}));
			}
		}, () =>
		{
			if (ComponentPlayer == null)
			{
				m_stateMachine.TransitionTo("PrepareSpawn");
			}
			else if (m_playerDeathTime.HasValue)
			{
				m_stateMachine.TransitionTo("PlayerDead");
			}
			else if (ComponentPlayer.ComponentHealth.Health <= 0f)
			{
				m_playerDeathTime = Time.RealTime;
			}
			if (!IsMainPlayer && CommonLib.WorkType != WorkType.Client && !isSetStatus)
			{
				if (Client != null && Client.State != ClientState.Playing)
				{
					ComponentPlayer.ComponentHealth.IsInvulnerable = true;
				}
				else
				{
					isSetStatus = true;
					if (m_subsystemGameInfo.WorldSettings.GameMode != GameMode.Creative)
					{
						ComponentPlayer.ComponentHealth.IsInvulnerable = false;
					}
				}
			}
		}, null);
		m_stateMachine.AddState("PlayerDead", () =>
		{
			isSetStatus = false;
			GameWidget.ActiveCamera = GameWidget.FindCamera<DeathCamera>();
			if (ComponentPlayer != null)
			{
				string text = ComponentPlayer.ComponentHealth.CauseOfDeath;
				if (string.IsNullOrEmpty(text))
				{
					text = LanguageControl.Get(fName, 12);
				}
				string arg = string.Format(LanguageControl.Get(fName, 13), text);
				if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Cruel)
				{
					ComponentPlayer.ComponentGui.DisplayLargeMessage(LanguageControl.Get(fName, 6), string.Format(LanguageControl.Get(fName, 7), arg, LanguageControl.Get("GameMode", m_subsystemGameInfo.WorldSettings.GameMode.ToString())), 30f, 1.5f);
				}
				else if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Adventure && !m_subsystemGameInfo.WorldSettings.IsAdventureRespawnAllowed)
				{
					ComponentPlayer.ComponentGui.DisplayLargeMessage(LanguageControl.Get(fName, 6), string.Format(LanguageControl.Get(fName, 8), arg), 30f, 1.5f);
				}
				else
				{
					ComponentPlayer.ComponentGui.DisplayLargeMessage(LanguageControl.Get(fName, 6), string.Format(LanguageControl.Get(fName, 9), arg), 30f, 1.5f);
					if (CommonLib.WorkType == WorkType.Server)
					{
						SubsystemGameWidgets.AddMessage(Name + " <c=red>" + text + "</c>", null, 0);
					}
				}
				IsDead = true;
			}
			Level = MathUtils.Max(MathUtils.Floor(Level / 2f), 1f);
		}, () =>
		{
			if (CommonLib.WorkType == WorkType.Client && !IsMainPlayer)
			{
				NetRestart();
				m_stateMachine.TransitionTo("PrepareSpawn");
			}
			else if (ComponentPlayer == null)
			{
				if (IsDead && readyToRestart)
				{
					IsDead = false;
					readyToRestart = false;
					m_stateMachine.TransitionTo("PrepareSpawn");
				}
			}
			else if ((IsMainPlayer && Time.RealTime - m_playerDeathTime.Value > 1.5 && !DialogsManager.HasDialogs(ComponentPlayer.GuiWidget) && ComponentPlayer.GameWidget.Input.Any) || readyToRestart)
			{
				if (CommonLib.WorkType == WorkType.Client && IsMainPlayer)
				{
					CommonLib.Net.QueuePackage(new ComponentPlayerPackage(this, ComponentPlayerPackage.PlayerAction.Restart));
				}
				IsDead = true;
				readyToRestart = true;
				NetRestart();
			}
		}, null);
	}

	public void NetRestart()
	{
		WorldSettings inventory = m_subsystemGameInfo.WorldSettings;
		int activeBlockValue = inventory.LivesLimit;
		GameMode num = inventory.GameMode;
		switch (num)
		{
		case GameMode.Cruel:
			activeBlockValue = 1;
			break;
		case GameMode.Embrace:
			activeBlockValue = 7;
			break;
		}
		if (activeBlockValue > 0 && (IsCheater || SpawnsCount >= activeBlockValue + ExtraLives))
		{
			m_stateMachine.TransitionTo(null);
			if ((num == GameMode.Cruel || num == GameMode.Embrace || num == GameMode.RealChallenging || (inventory.LockWorldSettings && inventory.LivesLimit > 0)) && (CommonLib.WorkType == WorkType.Server || CommonLib.WorkType == WorkType.Local))
			{
				SubsystemPlayers.AddBlackList(this);
			}
			string data = ((num == GameMode.Cruel) ? "cruel_mode_death" : "respawn_not_allowed");
			string num3 = ((num == GameMode.Cruel) ? "cannot_respawn_in_cruel_mode" : "lives_limit_reached");
			DialogsManager.ShowDialog(ComponentPlayer.GuiWidget, new MessageDialog(LanguageControl.Get("PlayerData", data), LanguageControl.Get("PlayerData", num3), LanguageControl.Yes, LanguageControl.No, (MessageDialogButton btn) =>
			{
				CommonLib.Net.StopImmediate();
			}));
		}
		else if (!inventory.IsAdventureRespawnAllowed)
		{
			m_stateMachine.TransitionTo(null);
			if (CommonLib.WorkType == WorkType.Server)
			{
				SubsystemPlayers.AddBlackList(this);
			}
			DialogsManager.ShowDialog(ComponentPlayer.GuiWidget, new MessageDialog(LanguageControl.Get("PlayerData", "respawn_not_allowed"), LanguageControl.Get("PlayerData", "server_prohibited_respawn"), LanguageControl.Yes, LanguageControl.No, (MessageDialogButton btn) =>
			{
				CommonLib.Net.StopImmediate();
			}));
		}
		else if (num == GameMode.Adventure && !inventory.IsAdventureRespawnAllowed)
		{
			ScreensManager.SwitchScreen("GameLoading", GameManager.WorldInfo, "AdventureRestart");
		}
		else
		{
			m_project.RemoveEntity(ComponentPlayer.Entity, disposeEntity: true);
		}
	}

	public void ResetState()
	{
		ComponentPlayer = null;
		m_gameWidget = null;
		m_stateMachine.TransitionTo("FirstUpdate");
	}

	public void SetMain()
	{
		m_isMainPlayer = true;
	}

	public void Dispose()
	{
		HideSpawnDialog();
	}

	public void RandomizeCharacterSkin()
	{
		Random random = new Random();
		CharacterSkinsManager.UpdateCharacterSkinsList();
		string[] array = CharacterSkinsManager.CharacterSkinsNames.Where((string n) => CharacterSkinsManager.IsBuiltIn(n) && CharacterSkinsManager.GetPlayerClass(n) == m_playerClass).ToArray();
		string[] second = SubsystemPlayers.PlayersData.Select((PlayerData pd) => pd.CharacterSkinName).ToArray();
		string[] array2 = array.Except(second).ToArray();
		CharacterSkinName = ((array2.Length != 0) ? array2[random.Int(0, array2.Length - 1)] : array[random.Int(0, array.Length - 1)]);
	}

	public void ResetName()
	{
		m_name = CharacterSkinsManager.GetDisplayName(CharacterSkinName);
		IsDefaultName = true;
	}

	public static bool VerifyName(string name)
	{
		return name.Length >= 2;
	}

	public void Update()
	{
		if (m_spawnDialog != null)
		{
			GameWidget gameWidget = GameWidget;
			if (gameWidget != null && (gameWidget.Input?.Tap).HasValue)
			{
				if (m_subsystemGameInfo != null && m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace)
				{
					if (m_embraceMainLabel != null)
					{
						m_embraceMainLabel.Text = GetRandomEmbraceHint();
						m_embraceMainLabel.Color = new Color(128, 0, 32);
						m_embraceMainLabel.IsVisible = true;
					}
					if (m_embraceHintLabel != null)
					{
						m_embraceHintLabel.Text = LanguageControl.Get("PlayerDataHints", "777");
						m_embraceHintLabel.Color = new Color(128, 0, 32);
						m_embraceHintLabel.IsVisible = true;
					}
					m_spawnDialog.SmallMessage = string.Empty;
				}
				else
				{
					m_spawnDialog.SmallMessage = GetRandomHint();
				}
			}
		}
		m_stateMachine.Update();
	}

	public void Load(ValuesDictionary valuesDictionary)
	{
		SpawnPosition = valuesDictionary.GetValue("SpawnPosition", Vector3.Zero);
		FirstSpawnTime = valuesDictionary.GetValue("FirstSpawnTime", 0.0);
		LastSpawnTime = valuesDictionary.GetValue("LastSpawnTime", 0.0);
		SpawnsCount = valuesDictionary.GetValue("SpawnsCount", 0);
		ExtraLives = valuesDictionary.GetValue("ExtraLives", 0);
		IsCheater = valuesDictionary.GetValue("IsCheater", defaultValue: false);
		Name = valuesDictionary.GetValue("Name", "Walter");
		PlayerGUID = valuesDictionary.GetValue("PlayerGUID", CommonLib.UserId);
		string value = valuesDictionary.GetValue("CurrentHardwareId", string.Empty);
		PreviousHardwareId = valuesDictionary.GetValue("PreviousHardwareId", string.Empty);
		if (PlayerGUID == CommonLib.UserId)
		{
			string hardwareId = CommonLib.HardwareId;
			if (!string.IsNullOrEmpty(value) && !string.Equals(value, hardwareId, StringComparison.OrdinalIgnoreCase))
			{
				PreviousHardwareId = value;
			}
			CurrentHardwareId = hardwareId;
		}
		else
		{
			CurrentHardwareId = value;
		}
		PlayerClass = valuesDictionary.GetValue("PlayerClass", PlayerClass.Male);
		Level = valuesDictionary.GetValue("Level", 1f);
		CharacterSkinName = valuesDictionary.GetValue("CharacterSkinName", CharacterSkinsManager.CharacterSkinsNames[0]);
		InputDevice = valuesDictionary.GetValue("InputDevice", InputDevice);
		ServerManager = valuesDictionary.GetValue("ServerManager", ServerManager);
		IsServerAdmin = valuesDictionary.GetValue("IsServerAdmin", IsServerAdmin);
		IsVanish = valuesDictionary.GetValue("IsVanish", IsVanish);
		WorldEditManager = valuesDictionary.GetValue("WorldEditManager", defaultValue: false);
		Tag = valuesDictionary.GetValue("PlayerTag", PlayerTag.Tall);
		m_stateMachine.TransitionTo("FirstUpdate");
		foreach (Guid value3 in valuesDictionary.GetValue("Friends", new ValuesDictionary()).Values)
		{
			Friends.Add(value3);
		}
		m_isMainPlayer = PlayerGUID == CommonLib.UserId;
		if (IsMainPlayer && CommonLib.WorkType == WorkType.Server)
		{
			ServerManager = true;
		}
		ValuesDictionary value2 = valuesDictionary.GetValue("HomePoints", new ValuesDictionary());
		HomePoints = new Dictionary<int, Vector3>();
		foreach (string key in value2.Keys)
		{
			if (int.TryParse(key, out var result))
			{
				HomePoints[result] = value2.GetValue<Vector3>(key);
			}
		}
	}

	public void Save(ValuesDictionary valuesDictionary)
	{
		valuesDictionary.SetValue("SpawnPosition", SpawnPosition);
		valuesDictionary.SetValue("FirstSpawnTime", FirstSpawnTime);
		valuesDictionary.SetValue("LastSpawnTime", LastSpawnTime);
		valuesDictionary.SetValue("SpawnsCount", SpawnsCount);
		valuesDictionary.SetValue("ExtraLives", ExtraLives);
		valuesDictionary.SetValue("IsCheater", IsCheater);
		valuesDictionary.SetValue("Name", Name);
		valuesDictionary.SetValue("PlayerGUID", PlayerGUID);
		valuesDictionary.SetValue("CurrentHardwareId", CurrentHardwareId ?? string.Empty);
		valuesDictionary.SetValue("PreviousHardwareId", PreviousHardwareId ?? string.Empty);
		valuesDictionary.SetValue("PlayerClass", PlayerClass);
		valuesDictionary.SetValue("Level", Level);
		valuesDictionary.SetValue("CharacterSkinName", CharacterSkinName);
		valuesDictionary.SetValue("InputDevice", InputDevice);
		valuesDictionary.SetValue("ServerManager", ServerManager);
		valuesDictionary.SetValue("IsServerAdmin", IsServerAdmin);
		valuesDictionary.SetValue("IsVanish", IsVanish);
		valuesDictionary.SetValue("WorldEditManager", WorldEditManager);
		valuesDictionary.SetValue("PlayerTag", Tag);
		ValuesDictionary valuesDictionary2 = new ValuesDictionary();
		int num = 0;
		foreach (Guid friend in Friends)
		{
			valuesDictionary2.SetValue(num++.ToString(), friend);
		}
		valuesDictionary.SetValue("Friends", valuesDictionary2);
		if (HomePoints == null || HomePoints.Count <= 0)
		{
			return;
		}
		ValuesDictionary valuesDictionary3 = new ValuesDictionary();
		foreach (KeyValuePair<int, Vector3> homePoint in HomePoints)
		{
			valuesDictionary3.SetValue(homePoint.Key.ToString(), homePoint.Value);
		}
		valuesDictionary.SetValue("HomePoints", valuesDictionary3);
	}

	public void OnEntityAdded(Entity entity)
	{
		ComponentPlayer componentPlayer = entity.FindComponent<ComponentPlayer>();
		if (componentPlayer != null && componentPlayer.PlayerData == this)
		{
			if (ComponentPlayer != null)
			{
				throw new InvalidOperationException(string.Format(LanguageControl.Get(fName, 10), PlayerGUID));
			}
			ComponentPlayer = componentPlayer;
			GameWidget.ActiveCamera = GameWidget.FindCamera<FppCamera>();
			GameWidget.Target = componentPlayer;
			if (FirstSpawnTime < 0.0)
			{
				FirstSpawnTime = m_subsystemGameInfo.TotalElapsedGameTime;
			}
			if (CommonLib.WorkType == WorkType.Client && IsMainPlayer && m_stateMachine.CurrentState == "PrepareSpawn")
			{
				ClientCachePosition = ComponentPlayer.ComponentBody.Position;
				m_stateMachine.TransitionTo("WaitForTerrain");
			}
		}
	}

	public void OnEntityRemoved(Entity entity)
	{
		if (ComponentPlayer != null && entity == ComponentPlayer.Entity)
		{
			ComponentPlayer = null;
			m_playerDeathTime = null;
		}
	}

	public Vector3 FindIntroSpawnPosition(Vector2 desiredSpawnPosition)
	{
		if (m_subsystemGameInfo != null && m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace)
		{
			Vector3 vector = m_subsystemTerrain.TerrainContentsGenerator.FindCoarseSpawnPosition();
			return new Vector3(vector.X, 460f, vector.Z);
		}
		Vector2 vector2 = Vector2.Zero;
		float num = float.MinValue;
		for (int i = -30; i <= 30; i += 2)
		{
			for (int j = -30; j <= 30; j += 2)
			{
				int num2 = Terrain.ToCell(desiredSpawnPosition.X) + i;
				int num3 = Terrain.ToCell(desiredSpawnPosition.Y) + j;
				float num4 = ScoreIntroSpawnPosition(desiredSpawnPosition, num2, num3);
				if (num4 > num)
				{
					num = num4;
					vector2 = new Vector2(num2, num3);
				}
			}
		}
		float num5 = m_subsystemTerrain.Terrain.CalculateTopmostCellHeight(Terrain.ToCell(vector2.X), Terrain.ToCell(vector2.Y)) + 1;
		return new Vector3(vector2.X + 0.5f, num5 + 0.01f, vector2.Y + 0.5f);
	}

	public Vector3 FindNoIntroSpawnPosition(Vector3 desiredSpawnPosition, bool respawn)
	{
		if (m_subsystemGameInfo != null && m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace)
		{
			return new Vector3(desiredSpawnPosition.X + 0.5f, 460f, desiredSpawnPosition.Z + 0.5f);
		}
		Vector3 vector = Vector3.Zero;
		float num = float.MinValue;
		for (int i = -8; i <= 8; i++)
		{
			for (int j = -8; j <= 8; j++)
			{
				for (int k = -8; k <= 8; k++)
				{
					int num2 = Terrain.ToCell(desiredSpawnPosition.X) + i;
					int num3 = Terrain.ToCell(desiredSpawnPosition.Y) + j;
					int num4 = Terrain.ToCell(desiredSpawnPosition.Z) + k;
					float num5 = ScoreNoIntroSpawnPosition(desiredSpawnPosition, num2, num3, num4);
					if (num5 > num)
					{
						num = num5;
						vector = new Vector3(num2, num3, num4);
					}
				}
			}
		}
		return new Vector3(vector.X + 0.5f, vector.Y + 0.01f, vector.Z + 0.5f);
	}

	public float ScoreIntroSpawnPosition(Vector2 desiredSpawnPosition, int x, int z)
	{
		float num = -0.01f * Vector2.Distance(new Vector2(x, z), desiredSpawnPosition);
		int num2 = m_subsystemTerrain.Terrain.CalculateTopmostCellHeight(x, z);
		if (num2 < 64 || num2 > 85)
		{
			num -= 5f;
		}
		if (m_subsystemTerrain.Terrain.GetSeasonalTemperature(x, z) < 8)
		{
			num -= 5f;
		}
		int cellContents = m_subsystemTerrain.Terrain.GetCellContents(x, num2, z);
		if (BlocksManager.Blocks[cellContents].IsTransparent)
		{
			num -= 5f;
		}
		for (int i = x - 1; i <= x + 1; i++)
		{
			for (int j = z - 1; j <= z + 1; j++)
			{
				if (m_subsystemTerrain.Terrain.GetCellContents(i, num2 + 2, j) != 0)
				{
					num--;
				}
			}
		}
		Vector2 vector = ComponentIntro.FindOceanDirection(m_subsystemTerrain.TerrainContentsGenerator, new Vector2(x, z));
		Vector3 vector2 = new Vector3(x, (float)num2 + 1.5f, z);
		for (int k = -1; k <= 1; k++)
		{
			Vector3 end = vector2 + new Vector3(30f * vector.X, 5f * (float)k, 30f * vector.Y);
			TerrainRaycastResult? terrainRaycastResult = m_subsystemTerrain.Raycast(vector2, end, useInteractionBoxes: false, skipAirBlocks: true, (int value, float distance) => Terrain.ExtractContents(value) != 0);
			if (terrainRaycastResult.HasValue)
			{
				CellFace cellFace = terrainRaycastResult.Value.CellFace;
				int cellContents2 = m_subsystemTerrain.Terrain.GetCellContents(cellFace.X, cellFace.Y, cellFace.Z);
				if (cellContents2 != 18 && cellContents2 != 0)
				{
					num -= 2f;
				}
			}
		}
		return num;
	}

	public float ScoreNoIntroSpawnPosition(Vector3 desiredSpawnPosition, int x, int y, int z)
	{
		float cellContents = -0.01f * Vector3.Distance(new Vector3(x, y, z), desiredSpawnPosition);
		if (y < 1 || y >= 255)
		{
			cellContents -= 100f;
		}
		Block obj = BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(x, y - 1, z)];
		Block block = BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(x, y, z)];
		Block block2 = BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(x, y + 1, z)];
		if (obj.IsTransparent)
		{
			cellContents -= 10f;
		}
		if (!obj.IsCollidable)
		{
			cellContents -= 10f;
		}
		if (block.IsCollidable)
		{
			cellContents -= 10f;
		}
		if (block2.IsCollidable)
		{
			cellContents -= 10f;
		}
		foreach (PlayerData playersDatum in SubsystemPlayers.PlayersData)
		{
			if (playersDatum != this && Vector3.DistanceSquared(playersDatum.SpawnPosition, new Vector3(x, y, z)) < (float)MathUtils.Sqr(2))
			{
				cellContents--;
			}
		}
		return cellContents;
	}

	public bool CheckIsPointInWater(Point3 p)
	{
		bool cellValue = true;
		for (int num = p.X - 1; num < p.X + 1; num++)
		{
			for (int cellContents2 = p.Z - 1; cellContents2 < p.Z + 1; cellContents2++)
			{
				for (int num2 = p.Y; num2 > 0; num2--)
				{
					int cellContents3 = m_subsystemTerrain.Terrain.GetCellContents(p.X, num2, p.Z);
					Block block = BlocksManager.Blocks[cellContents3];
					if (block.IsCollidable)
					{
						return false;
					}
					if (block is FocalorsWaterBlock)
					{
						break;
					}
				}
			}
		}
		return cellValue;
	}

	public void SpawnPlayer(Vector3 position, SpawnMode spawnMode)
	{
		bool num = m_subsystemGameInfo != null && m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace;
		ComponentMount componentCreature = null;
		if (!num && spawnMode != SpawnMode.Respawn && CheckIsPointInWater(Terrain.ToCell(position)))
		{
			Entity entity = DatabaseManager.CreateEntity(m_project, "Boat", throwIfNotFound: true);
			entity.FindComponent<ComponentBody>(throwOnError: true).Position = position;
			entity.FindComponent<ComponentBody>(throwOnError: true).Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathUtils.DegToRad(45f));
			componentCreature = entity.FindComponent<ComponentMount>(throwOnError: true);
			m_project.AddEntity(entity);
			position.Y += entity.FindComponent<ComponentBody>(throwOnError: true).BoxSize.Y;
		}
		string text = "";
		string amount = "";
		string value = "";
		string value2 = "";
		if (!num && spawnMode != SpawnMode.Respawn)
		{
			if (PlayerClass == PlayerClass.Female)
			{
				if (CharacterSkinsManager.IsBuiltIn(CharacterSkinName) && CharacterSkinName.Contains("5"))
				{
					text = "";
					amount = "";
					value = "";
					value2 = "";
				}
				else if (CharacterSkinsManager.IsBuiltIn(CharacterSkinName) && CharacterSkinName.Contains("2"))
				{
					text = "";
					amount = MakeClothingValue(37, 2);
					value = MakeClothingValue(16, 14);
					value2 = MakeClothingValue(26, 6) + ";" + MakeClothingValue(27, 0);
				}
				else if (CharacterSkinsManager.IsBuiltIn(CharacterSkinName) && CharacterSkinName.Contains("3"))
				{
					text = MakeClothingValue(31, 0);
					amount = MakeClothingValue(13, 7) + ";" + MakeClothingValue(5, 0);
					value = MakeClothingValue(17, 15);
					value2 = MakeClothingValue(29, 0);
				}
				else if (CharacterSkinsManager.IsBuiltIn(CharacterSkinName) && CharacterSkinName.Contains("4"))
				{
					text = MakeClothingValue(30, 7);
					amount = MakeClothingValue(14, 6);
					value = MakeClothingValue(25, 7);
					value2 = MakeClothingValue(26, 6) + ";" + MakeClothingValue(8, 0);
				}
				else
				{
					text = MakeClothingValue(30, 12);
					amount = MakeClothingValue(37, 3) + ";" + MakeClothingValue(1, 3);
					value = MakeClothingValue(0, 12);
					value2 = MakeClothingValue(26, 6) + ";" + MakeClothingValue(29, 0);
				}
			}
			else if (CharacterSkinsManager.IsBuiltIn(CharacterSkinName) && CharacterSkinName.Contains("5"))
			{
				text = MakeClothingValue(54, 15) + ";" + MakeClothingValue(42, 15) + ";" + MakeClothingValue(32, 0);
				amount = MakeClothingValue(24, 3) + ";" + MakeClothingValue(49, 15);
				value = MakeClothingValue(16, 8);
				value2 = MakeClothingValue(26, 14) + ";" + MakeClothingValue(8, 0);
			}
			else if (CharacterSkinsManager.IsBuiltIn(CharacterSkinName) && CharacterSkinName.Contains("6"))
			{
				text = "";
				amount = MakeClothingValue(14, 0) + ";" + MakeClothingValue(13, 15);
				value = MakeClothingValue(12, 13);
				value2 = MakeClothingValue(27, 0);
			}
			else if (CharacterSkinsManager.IsBuiltIn(CharacterSkinName) && CharacterSkinName.Contains("7"))
			{
				text = MakeClothingValue(11, 0);
				amount = MakeClothingValue(37, 0) + ";" + MakeClothingValue(13, 0);
				value = MakeClothingValue(12, 0);
				value2 = MakeClothingValue(8, 0);
			}
			else if (CharacterSkinsManager.IsBuiltIn(CharacterSkinName) && CharacterSkinName.Contains("8"))
			{
				text = MakeClothingValue(45, 0);
				amount = MakeClothingValue(37, 0);
				value = MakeClothingValue(56, 0);
				value2 = MakeClothingValue(29, 0);
			}
			else if (CharacterSkinsManager.IsBuiltIn(CharacterSkinName) && CharacterSkinName.Contains("9"))
			{
				text = MakeClothingValue(58, 0);
				amount = MakeClothingValue(55, 0);
				value = "";
				value2 = MakeClothingValue(8, 0);
			}
			else if (CharacterSkinsManager.IsBuiltIn(CharacterSkinName) && CharacterSkinName.Contains("2"))
			{
				text = "";
				amount = MakeClothingValue(13, 0) + ";" + MakeClothingValue(5, 0);
				value = MakeClothingValue(25, 8);
				value2 = MakeClothingValue(26, 6) + ";" + MakeClothingValue(29, 0);
			}
			else if (CharacterSkinsManager.IsBuiltIn(CharacterSkinName) && CharacterSkinName.Contains("3"))
			{
				text = MakeClothingValue(32, 0);
				amount = MakeClothingValue(37, 5);
				value = MakeClothingValue(0, 15);
				value2 = MakeClothingValue(26, 6) + ";" + MakeClothingValue(8, 0);
			}
			else if (CharacterSkinsManager.IsBuiltIn(CharacterSkinName) && CharacterSkinName.Contains("4"))
			{
				text = MakeClothingValue(31, 0);
				amount = MakeClothingValue(15, 14);
				value = MakeClothingValue(0, 0);
				value2 = MakeClothingValue(26, 6) + ";" + MakeClothingValue(8, 0);
			}
			else
			{
				text = MakeClothingValue(32, 0);
				amount = MakeClothingValue(37, 0) + ";" + MakeClothingValue(1, 9);
				value = MakeClothingValue(0, 12);
				value2 = MakeClothingValue(26, 6) + ";" + MakeClothingValue(29, 0);
			}
		}
		ValuesDictionary overrides = new ValuesDictionary
		{
			{
				"Player",
				new ValuesDictionary
				{
					{ "PlayerIndex", PlayerIndex },
					{ "PlayerGuid", PlayerGUID }
				}
			},
			{
				"Intro",
				new ValuesDictionary { 
				{
					"PlayIntro",
					spawnMode == SpawnMode.InitialIntro
				} }
			},
			{
				"Clothing",
				new ValuesDictionary { 
				{
					"Clothes",
					new ValuesDictionary
					{
						{ "Feet", value2 },
						{ "Legs", value },
						{ "Torso", amount },
						{ "Head", text }
					}
				} }
			}
		};
		Vector2 v = ComponentIntro.FindOceanDirection(m_subsystemTerrain.TerrainContentsGenerator, position.XZ);
		string entityTemplateName = ((PlayerClass == PlayerClass.Male) ? "MalePlayer" : "FemalePlayer");
		Entity entity2 = DatabaseManager.CreateEntity(m_project, entityTemplateName, overrides, throwIfNotFound: true);
		ComponentBody componentBody = entity2.FindComponent<ComponentBody>(throwOnError: true);
		componentBody.Position = position;
		componentBody.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, Vector2.Angle(v, -Vector2.UnitY));
		if (CommonLib.WorkType != WorkType.Local)
		{
			componentBody.netPosition = new NetPosition(componentBody.Position);
			componentBody.netRotation = new NetRotation(componentBody.Rotation);
		}
		m_project.AddEntity(entity2);
		if (componentCreature != null)
		{
			entity2.FindComponent<ComponentRider>(throwOnError: true).StartMounting(componentCreature);
		}
		SpawnsCount++;
		LastSpawnTime = m_subsystemGameInfo.TotalElapsedGameTime;
	}

	public string GetEntityTemplateName()
	{
		if (PlayerClass == PlayerClass.Female)
		{
			return "FemalePlayer";
		}
		return "MalePlayer";
	}

	public void UpdateSpawnDialog(string largeMessage, string smallMessage, float progress, bool resetProgress)
	{
		if (resetProgress)
		{
			m_progress = 0f;
		}
		m_progress = MathUtils.Max(progress, m_progress);
		if (m_spawnDialog == null)
		{
			m_spawnDialog = new SpawnDialog();
			DialogsManager.ShowDialog(GameWidget.GuiWidget, m_spawnDialog);
			if ((CommonLib.WorkType == WorkType.Local && IsMainPlayer) || (CommonLib.WorkType == WorkType.Server && IsMainPlayer))
			{
				m_spawnDialog.SmallMessage = GetRandomHint();
			}
			m_seasonShown = false;
			m_embraceShown = false;
		}
		if (m_subsystemGameInfo != null && m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace)
		{
			if (largeMessage != null)
			{
				m_spawnDialog.LargeMessage = string.Empty;
			}
			if (m_embraceMainLabel == null || m_embraceMainLabel.ParentWidget != m_spawnDialog)
			{
				m_embraceMainLabel = new LabelWidget
				{
					Name = "EmbraceMainLabel",
					HorizontalAlignment = WidgetAlignment.Center,
					VerticalAlignment = WidgetAlignment.Center,
					Margin = new Vector2(0f, 0f),
					FontScale = 1.2f,
					Color = new Color(128, 0, 32)
				};
				m_spawnDialog.Children.Add(m_embraceMainLabel);
			}
			if (!m_embraceShown || smallMessage != null)
			{
				m_embraceMainLabel.Text = GetRandomEmbraceHint();
				m_embraceMainLabel.Color = new Color(128, 0, 32);
				m_embraceMainLabel.IsVisible = true;
				m_embraceShown = true;
			}
			float num = ((m_spawnDialog != null) ? (0.2f * m_spawnDialog.ActualSize.Y) : 0f);
			m_embraceMainLabel.RenderTransform = Matrix.CreateTranslation(0f, 0f - num, 0f);
			m_spawnDialog.SmallMessage = string.Empty;
			if (m_embraceHintLabel == null || m_embraceHintLabel.ParentWidget != m_spawnDialog)
			{
				m_embraceHintLabel = new LabelWidget
				{
					Name = "EmbraceHintLabel",
					HorizontalAlignment = WidgetAlignment.Center,
					VerticalAlignment = WidgetAlignment.Far,
					Margin = new Vector2(0f, -22f),
					FontScale = 1f,
					Color = new Color(128, 0, 32)
				};
				m_spawnDialog.Children.Add(m_embraceHintLabel);
			}
			m_embraceHintLabel.Text = LanguageControl.Get("PlayerDataHints", "777");
			m_embraceHintLabel.Color = new Color(128, 0, 32);
			m_embraceHintLabel.IsVisible = true;
		}
		else
		{
			if (largeMessage != null)
			{
				m_spawnDialog.LargeMessage = largeMessage;
			}
			if (smallMessage != null)
			{
				m_spawnDialog.SmallMessage = smallMessage;
			}
			if (m_embraceMainLabel != null)
			{
				m_embraceMainLabel.IsVisible = false;
			}
			if (m_embraceHintLabel != null)
			{
				m_embraceHintLabel.IsVisible = false;
			}
		}
		ApplySeasonOnce();
		m_spawnDialog.Progress = m_progress;
	}

	public void HideSpawnDialog()
	{
		if (m_spawnDialog != null)
		{
			DialogsManager.HideDialog(m_spawnDialog);
			m_spawnDialog = null;
		}
		m_seasonShown = false;
		m_embraceShown = false;
		m_seasonMainLabel = null;
		m_seasonPhaseLabel = null;
		m_embraceMainLabel = null;
		m_embraceHintLabel = null;
	}

	public static string MakeClothingValue(int index, int color)
	{
		return Terrain.MakeBlockValue(203, 0, ClothingBlock.SetClothingIndex(ClothingBlock.SetClothingColor(0, color), index)).ToString(CultureInfo.InvariantCulture);
	}

	static PlayerData()
	{
		fName = "PlayerData";
		m_random = new Random();
	}

	public static string GetRandomHint()
	{
		int max = 218;
		return LanguageControl.Get("PlayerDataHints", m_random.Int(1, max).ToString());
	}

	private void ApplySeasonOnce()
	{
		if (m_spawnDialog == null || m_seasonShown)
		{
			return;
		}
		SubsystemWeather subsystemWeather;
		if ((subsystemWeather = m_subsystemWeather) == null)
		{
			subsystemWeather = m_project?.FindSubsystem<SubsystemWeather>(throwOnError: true);
		}
		SubsystemWeather subsystemWeather2 = subsystemWeather;
		if (subsystemWeather2 != null)
		{
			bool flag = false;
			bool flag2 = false;
			SubsystemWeather.Season currentSeason = subsystemWeather2.CurrentSeason;
			SubsystemWeather.Season season = currentSeason;
			switch (currentSeason)
			{
			case SubsystemWeather.Season.EarlyAutumn:
				flag = true;
				season = SubsystemWeather.Season.Autumn;
				break;
			case SubsystemWeather.Season.LateAutumn:
				flag2 = true;
				season = SubsystemWeather.Season.Autumn;
				break;
			case SubsystemWeather.Season.LateWinter:
				flag2 = true;
				season = SubsystemWeather.Season.Winter;
				break;
			case SubsystemWeather.Season.EarlySpring:
				flag = true;
				season = SubsystemWeather.Season.Spring;
				break;
			case SubsystemWeather.Season.LateSpring:
				flag2 = true;
				season = SubsystemWeather.Season.Spring;
				break;
			case SubsystemWeather.Season.EarlySummer:
				flag = true;
				season = SubsystemWeather.Season.Summer;
				break;
			case SubsystemWeather.Season.LateSummer:
				flag2 = true;
				season = SubsystemWeather.Season.Summer;
				break;
			}
			string text = LanguageControl.Get("Seasons", season.ToString());
			string text2;
			if (flag)
			{
				text2 = LanguageControl.Get("Seasons", "Early");
			}
			else
			{
				text2 = (flag2 ? LanguageControl.Get("Seasons", "Late") : null);
			}
			Color color = season switch
			{
				SubsystemWeather.Season.Autumn => new Color(210, 120, 40), 
				SubsystemWeather.Season.Anabiosis => new Color(90, 140, 220), 
				SubsystemWeather.Season.Winter => new Color(120, 180, 255), 
				SubsystemWeather.Season.Spring => new Color(96, 180, 72), 
				SubsystemWeather.Season.Summer => new Color(240, 200, 60), 
				_ => new Color(200, 200, 200), 
			};
			Color color2 = color;
			if (flag)
			{
				color2 = Color.Lerp(color, Color.White, 0.35f);
			}
			if (flag2)
			{
				color2 = Color.Lerp(color, Color.Black, 0.25f);
			}
			if (m_seasonMainLabel == null || m_seasonMainLabel.ParentWidget != m_spawnDialog)
			{
				m_seasonMainLabel = new LabelWidget
				{
					Name = "SeasonMainLabel",
					HorizontalAlignment = WidgetAlignment.Center,
					VerticalAlignment = WidgetAlignment.Center,
					Margin = new Vector2(0f, 56f)
				};
				m_spawnDialog.Children.Add(m_seasonMainLabel);
			}
			if (m_seasonPhaseLabel == null || m_seasonPhaseLabel.ParentWidget != m_spawnDialog)
			{
				m_seasonPhaseLabel = new LabelWidget
				{
					Name = "SeasonPhaseLabel",
					HorizontalAlignment = WidgetAlignment.Center,
					VerticalAlignment = WidgetAlignment.Center,
					Margin = new Vector2(0f, 78f)
				};
				m_spawnDialog.Children.Add(m_seasonPhaseLabel);
			}
			m_seasonMainLabel.Text = text;
			m_seasonMainLabel.Color = color;
			m_seasonMainLabel.IsVisible = true;
			if (!string.IsNullOrEmpty(text2))
			{
				m_seasonPhaseLabel.Text = text2;
				m_seasonPhaseLabel.Color = color2;
				m_seasonPhaseLabel.IsVisible = true;
			}
			else
			{
				m_seasonPhaseLabel.IsVisible = false;
			}
			m_seasonShown = true;
		}
	}

	public static string GetRandomEmbraceHint()
	{
		int campfireSize = 29;
		return LanguageControl.Get("EmbraceHints", m_random.Int(1, campfireSize).ToString());
	}
}
