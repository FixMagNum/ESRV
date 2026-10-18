using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Engine;
using Game.NetWork;
using GameEntitySystem;
using LiteNetLib.Utils;
using TemplatesDatabase;
using XmlUtilities;

namespace Game;

public static class GameManager
{
	public static WorldInfo m_worldInfo;

	public static double m_lastAutoBackupTime;

	public static Project m_project;

	public static SubsystemUpdate m_subsystemUpdate;

	public static ManualResetEvent m_saveCompleted = new ManualResetEvent(initialState: true);

	public static Project Project => m_project;

	public static WorldInfo WorldInfo => m_worldInfo;

	public static event Action<Project> ProjectDisposed;

	public static void LoadProject(WorldInfo worldInfo, ContainerWidget gamesWidget)
	{
		XElement data = PreloadProjectXml(worldInfo);
		LoadProjectFromXml(worldInfo, gamesWidget, data);
	}

	public static void LoadNetProject(byte[] data, ContainerWidget gameWidget)
	{
		DisposeProject();
		MemoryStream stream = new MemoryStream(data);
		ValuesDictionary color = new ValuesDictionary();
		ValuesDictionary valuesDictionary = new ValuesDictionary();
		ValuesDictionary valuesDictionary2 = new ValuesDictionary();
		color.SetValue("GameInfo", valuesDictionary);
		color.SetValue("Views", valuesDictionary2);
		valuesDictionary.SetValue("WorldDirectoryName", "");
		valuesDictionary2.SetValue("GamesWidget", gameWidget);
		XElement projectNode = XmlUtils.LoadXmlFromStream(stream, Encoding.UTF8, throwOnError: true);
		ProjectData projectData = new ProjectData(DatabaseManager.GameDatabase, projectNode, color, ignoreInvalidEntities: true);
		m_project = new ProjectNet(DatabaseManager.GameDatabase, projectData);
		m_subsystemUpdate = m_project.FindSubsystem<SubsystemUpdate>(throwOnError: true);
		if (CommonLib.WorkType != WorkType.Client)
		{
			return;
		}
		foreach (PlayerData pd in Project.FindSubsystem<SubsystemPlayers>().AllPlayersData)
		{
			CommonLib.Net.SendPackage(PackageFlag.ClientKnowPlayer, (NetDataWriter w) =>
			{
				w.PutGuid(pd.PlayerGUID);
			});
		}
	}

	public static void SaveProject(bool waitForCompletion, bool showErrorDialog)
	{
		if (m_project == null || CommonLib.WorkType == WorkType.Client)
		{
			return;
		}
		double realTime = Time.RealTime;
		ProjectData projectData = m_project.Save();
		m_saveCompleted.WaitOne();
		m_saveCompleted.Reset();
		SubsystemGameInfo subsystemGameInfo = m_project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		string projectFileName = Storage.CombinePaths(subsystemGameInfo.DirectoryName, "Project.xml");
		Exception e = null;
		Task.Run(() =>
		{
			try
			{
				if (!string.IsNullOrEmpty(subsystemGameInfo.DirectoryName))
				{
					XElement xElement = new XElement("Project");
					projectData.Save(xElement);
					XmlUtils.SetAttributeValue(xElement, "Version", VersionsManager.SerializationVersion);
					Storage.CreateDirectory(subsystemGameInfo.DirectoryName);
					using Stream stream = Storage.OpenFile(projectFileName, OpenFileMode.Create);
					XmlUtils.SaveXmlToStream(xElement, stream, Encoding.UTF8, throwOnError: true);
				}
				if (realTime - m_lastAutoBackupTime > 1200.0)
				{
					m_lastAutoBackupTime = realTime;
					WorldsManager.MakeQuickWorldBackup(m_worldInfo.DirectoryName);
				}
			}
			catch (Exception ex)
			{
				e = ex;
				if (showErrorDialog)
				{
					Dispatcher.Dispatch(() =>
					{
						Log.Error(e);
						DialogsManager.ShowDialog(null, new MessageDialog("Error saving game", e.Message, "OK", null, null));
					});
				}
			}
			finally
			{
				m_saveCompleted.Set();
			}
		});
		if (waitForCompletion)
		{
			m_saveCompleted.WaitOne();
		}
		double realTime2 = Time.RealTime;
		Log.Verbose($"Saved project, {MathUtils.Round((realTime2 - realTime) * 1000.0)}ms");
	}

	public static void UpdateProject()
	{
		if (m_project != null)
		{
			m_subsystemUpdate.Update();
		}
	}

	public static void DisposeProject()
	{
		if (SettingsManager.NightVisionEnabled)
		{
			SettingsManager.NightVisionEnabled = false;
		}
		if (m_project != null)
		{
			ProjectDisposed?.Invoke(m_project);
			m_project.Dispose();
			m_project = null;
			m_subsystemUpdate = null;
			m_worldInfo = null;
			GC.Collect();
		}
	}

	public static XElement PreloadProjectXml(WorldInfo worldInfo)
	{
		WorldsManager.RepairWorldIfNeeded(worldInfo.DirectoryName);
		VersionsManager.UpgradeWorld(worldInfo.DirectoryName);
		using Stream stream = Storage.OpenFile(Storage.CombinePaths(worldInfo.DirectoryName, "Project.xml"), OpenFileMode.Read);
		return XmlUtils.LoadXmlFromStream(stream, null, throwOnError: true);
	}

	public static void LoadProjectFromXml(WorldInfo worldInfo, ContainerWidget gamesWidget, XElement projectNode)
	{
		DisposeProject();
		ValuesDictionary valuesDictionary = new ValuesDictionary();
		ValuesDictionary valuesDictionary2 = new ValuesDictionary();
		valuesDictionary.SetValue("GameInfo", valuesDictionary2);
		valuesDictionary2.SetValue("WorldDirectoryName", worldInfo.DirectoryName);
		ValuesDictionary valuesDictionary3 = new ValuesDictionary();
		valuesDictionary.SetValue("Views", valuesDictionary3);
		valuesDictionary3.SetValue("GamesWidget", gamesWidget);
		ProjectData projectData = new ProjectData(DatabaseManager.GameDatabase, projectNode, valuesDictionary, ignoreInvalidEntities: true);
		m_project = new ProjectNet(DatabaseManager.GameDatabase, projectData);
		m_subsystemUpdate = m_project.FindSubsystem<SubsystemUpdate>(throwOnError: true);
		m_worldInfo = worldInfo;
		Log.Information("Loaded world, GameMode={0}, StartingPosition={1}, WorldName={2}, VisibilityRange={3}, Resolution={4}", worldInfo.WorldSettings.GameMode, worldInfo.WorldSettings.StartingPositionMode, worldInfo.WorldSettings.Name, SettingsManager.VisibilityRange.ToString(), SettingsManager.ResolutionMode.ToString());
		AnalyticsManager.LogEvent("[GameManager] Loaded world", new AnalyticsParameter("GameMode", worldInfo.WorldSettings.GameMode.ToString()), new AnalyticsParameter("EnvironmentBehaviorMode", worldInfo.WorldSettings.EnvironmentBehaviorMode.ToString()), new AnalyticsParameter("TerrainGenerationMode", worldInfo.WorldSettings.TerrainGenerationMode.ToString()), new AnalyticsParameter("WorldDirectory", worldInfo.DirectoryName), new AnalyticsParameter("WorldName", worldInfo.WorldSettings.Name), new AnalyticsParameter("WorldSeedString", worldInfo.WorldSettings.Seed), new AnalyticsParameter("VisibilityRange", SettingsManager.VisibilityRange.ToString()), new AnalyticsParameter("Resolution", SettingsManager.ResolutionMode.ToString()));
	}
}
