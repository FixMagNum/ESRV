using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Engine;
using Engine.Graphics;
using Game.NetWork;
using Game.NetWork.Packages;

namespace Game;

public class TerrainUpdater
{
	public struct UpdateLocation
	{
		public Vector2 Center;

		public Vector2? LastChunksUpdateCenter;

		public float VisibilityDistance;

		public float ContentDistance;
	}

	private struct UpdateParameters
	{
		public TerrainChunk[] Chunks;

		public Dictionary<int, UpdateLocation> Locations;
	}

	private struct LightSource
	{
		public int X;

		public int Y;

		public int Z;

		public int Light;
	}

	private SubsystemTerrain m_subsystemTerrain;

	public SubsystemSky m_subsystemSky;

	private SubsystemBlockBehaviors m_subsystemBlockBehaviors;

	private Terrain m_terrain;

	private DynamicArray<LightSource> m_lightSources = new DynamicArray<LightSource>();

	private Task m_task;

	private List<Point2> RequestSyncChunkList = new List<Point2>();

	private AutoResetEvent m_updateEvent = new AutoResetEvent(initialState: true);

	private ManualResetEvent m_pauseEvent = new ManualResetEvent(initialState: true);

	private volatile bool m_quitUpdateThread;

	private bool m_unpauseUpdateThread;

	private object m_updateParametersLock = new object();

	private object m_unpauseLock = new object();

	private UpdateParameters m_updateParameters;

	private UpdateParameters m_threadUpdateParameters;

	private int m_lastSkylightValue;

	private int m_synchronousUpdateFrame;

	private Dictionary<int, UpdateLocation?> m_pendingLocations = new Dictionary<int, UpdateLocation?>();

	public static int SlowTerrainUpdate;

	public static bool LogTerrainUpdateStats;

	public SubsystemAnimatedTextures m_subsystemAnimatedTextures;

	public Dictionary<IClient, List<Point2>> waitChunkList = new Dictionary<IClient, List<Point2>>();

	public List<IClient> waitChunkList_toRemove = new List<IClient>();

	private Dictionary<TerrainChunk, double> m_chunkLastUpdateTimes = new Dictionary<TerrainChunk, double>();

	private object m_timersLock = new object();

	public static bool IsBusy;

	public static int MaxServerChunksPerFrame;

	public object m_waitChunkListLock = new object();

	public Dictionary<int, UpdateLocation> UpdateLocations => m_updateParameters.Locations;

	public AutoResetEvent UpdateEvent => m_updateEvent;

	public event Action UpdateProgressFinished;

	public TerrainUpdater(SubsystemTerrain subsystemTerrain)
	{
		m_subsystemTerrain = subsystemTerrain;
		m_subsystemSky = m_subsystemTerrain.Project.FindSubsystem<SubsystemSky>(throwOnError: true);
		m_subsystemBlockBehaviors = m_subsystemTerrain.Project.FindSubsystem<SubsystemBlockBehaviors>(throwOnError: true);
		m_subsystemAnimatedTextures = m_subsystemTerrain.Project.FindSubsystem<SubsystemAnimatedTextures>(throwOnError: true);
		m_terrain = subsystemTerrain.Terrain;
		m_updateParameters.Chunks = new TerrainChunk[0];
		m_updateParameters.Locations = new Dictionary<int, UpdateLocation>();
		m_threadUpdateParameters.Chunks = new TerrainChunk[0];
		m_threadUpdateParameters.Locations = new Dictionary<int, UpdateLocation>();
		SettingsManager.SettingChanged += SettingsManager_SettingChanged;
		SetUpdateLocation(-1, Vector2.Zero, 4f, 4f);
	}

	public void Dispose()
	{
		SettingsManager.SettingChanged -= SettingsManager_SettingChanged;
		lock (m_timersLock)
		{
			m_chunkLastUpdateTimes.Clear();
		}
		m_quitUpdateThread = true;
		UnpauseUpdateThread();
		m_updateEvent.Set();
		if (m_task != null)
		{
			m_task.Wait();
			m_task = null;
		}
		m_pauseEvent.Dispose();
		m_updateEvent.Dispose();
	}

	public void RequestSynchronousUpdate()
	{
		m_synchronousUpdateFrame = Time.FrameIndex;
	}

	public void SetLastChunksUpdateCenter(int locationIndex, Vector2? v)
	{
		if (m_updateParameters.Locations.TryGetValue(locationIndex, out var sizeFromValue))
		{
			sizeFromValue.LastChunksUpdateCenter = v;
			m_pendingLocations[locationIndex] = sizeFromValue;
		}
	}

	public void SetUpdateLocation(int locationIndex, Vector2 center, float visibilityDistance, float contentDistance)
	{
		contentDistance = MathUtils.Max(contentDistance, visibilityDistance);
		m_updateParameters.Locations.TryGetValue(locationIndex, out var value);
		if (contentDistance != value.ContentDistance || visibilityDistance != value.VisibilityDistance || !value.LastChunksUpdateCenter.HasValue || Vector2.DistanceSquared(center, value.LastChunksUpdateCenter.Value) > 64f)
		{
			value.Center = center;
			value.VisibilityDistance = visibilityDistance;
			value.ContentDistance = contentDistance;
			value.LastChunksUpdateCenter = center;
			m_pendingLocations[locationIndex] = value;
			if (CommonLib.WorkType == WorkType.Client)
			{
				CommonLib.Net.QueuePackage(new PlayerDataPackage(value));
			}
		}
	}

	public void RemoveUpdateLocation(int locationIndex)
	{
		m_updateParameters.Locations.Remove(locationIndex);
	}

	public float GetUpdateProgress(int locationIndex, float visibilityDistance, float contentDistance)
	{
		int num = 0;
		int num2 = 0;
		if (!m_updateParameters.Locations.TryGetValue(locationIndex, out var value))
		{
			return 0f;
		}
		visibilityDistance = MathUtils.Max(MathUtils.Min(visibilityDistance, value.VisibilityDistance) - 8f - 0.1f, 0f);
		contentDistance = MathUtils.Max(MathUtils.Min(contentDistance, value.ContentDistance) - 8f - 0.1f, 0f);
		float num3 = MathUtils.Sqr(visibilityDistance);
		float num4 = MathUtils.Sqr(contentDistance);
		float v = MathUtils.Max(visibilityDistance, contentDistance);
		Point2 point = Terrain.ToChunk(value.Center - new Vector2(v));
		Point2 point2 = Terrain.ToChunk(value.Center + new Vector2(v));
		for (int i = point.X; i <= point2.X; i++)
		{
			for (int j = point.Y; j <= point2.Y; j++)
			{
				TerrainChunk chunkAtCoords = m_terrain.GetChunkAtCoords(i, j);
				float num5 = Vector2.DistanceSquared(v2: new Vector2(((float)i + 0.5f) * 16f, ((float)j + 0.5f) * 16f), v1: value.Center);
				if (num5 <= num3)
				{
					if (chunkAtCoords == null || chunkAtCoords.State < TerrainChunkState.Valid)
					{
						num2++;
					}
					else
					{
						num++;
					}
				}
				else if (num5 <= num4)
				{
					if (chunkAtCoords == null || chunkAtCoords.State < TerrainChunkState.InvalidLight)
					{
						num2++;
					}
					else
					{
						num++;
					}
				}
			}
		}
		if (num2 <= 0)
		{
			UpdateProgressFinished?.Invoke();
			return 1f;
		}
		return (float)num / (float)(num2 + num);
	}

	public void Update()
	{
		if (m_subsystemSky.SkyLightValue != m_lastSkylightValue)
		{
			m_lastSkylightValue = m_subsystemSky.SkyLightValue;
			DowngradeAllChunksState(TerrainChunkState.InvalidLight, forceGeometryRegeneration: false);
		}
		if (!SettingsManager.MultithreadedTerrainUpdate)
		{
			if (m_task != null)
			{
				m_quitUpdateThread = true;
				UnpauseUpdateThread();
				m_updateEvent.Set();
				m_task.Wait();
				m_task = null;
			}
			double realTime = Time.RealTime;
			while (!SynchronousUpdateFunction() && !(Time.RealTime - realTime >= 0.009999999776482582))
			{
			}
		}
		else if (m_task == null)
		{
			m_quitUpdateThread = false;
			m_task = Task.Run((Action)ThreadUpdateFunction);
			UnpauseUpdateThread();
			m_updateEvent.Set();
		}
		if (m_pendingLocations.Count > 0)
		{
			m_pauseEvent.Reset();
			if (m_updateEvent.WaitOne())
			{
				m_pauseEvent.Set();
				try
				{
					foreach (KeyValuePair<int, UpdateLocation?> pendingLocation in m_pendingLocations)
					{
						if (pendingLocation.Value.HasValue)
						{
							m_updateParameters.Locations[pendingLocation.Key] = pendingLocation.Value.Value;
						}
						else
						{
							m_updateParameters.Locations.Remove(pendingLocation.Key);
						}
					}
					if (AllocateAndFreeChunks(m_updateParameters.Locations.Values.ToArray()))
					{
						m_updateParameters.Chunks = m_terrain.AllocatedChunks;
					}
					m_pendingLocations.Clear();
				}
				finally
				{
					m_updateEvent.Set();
				}
			}
		}
		else
		{
			lock (m_updateParametersLock)
			{
				if (SendReceiveChunkStates())
				{
					UnpauseUpdateThread();
				}
			}
		}
		TerrainChunk[] allocatedChunks = m_terrain.AllocatedChunks;
		foreach (TerrainChunk terrainChunk in allocatedChunks)
		{
			if (CommonLib.WorkType == WorkType.Client && terrainChunk.ThreadState == TerrainChunkState.NotLoaded && !terrainChunk.IsRequested)
			{
				terrainChunk.IsRequested = true;
				lock (RequestSyncChunkList)
				{
					RequestSyncChunkList.Add(terrainChunk.Coords);
				}
			}
			if (terrainChunk.State >= TerrainChunkState.InvalidVertices1 && !terrainChunk.AreBehaviorsNotified)
			{
				terrainChunk.AreBehaviorsNotified = true;
				NotifyBlockBehaviors(terrainChunk);
			}
		}
		lock (RequestSyncChunkList)
		{
			if (RequestSyncChunkList.Count > 0 && Time.PeriodicEvent(0.1, 0.0))
			{
				CommonLib.Net.QueuePackage(new SubsystemTerrainPackage(RequestSyncChunkList));
				RequestSyncChunkList.Clear();
			}
		}
		int num = ((CommonLib.WorkType == WorkType.Server) ? MaxServerChunksPerFrame : 2);
		int count = waitChunkList.Count;
		int val = ((count <= 0) ? 1 : Math.Max(1, num / count));
		int num2 = 0;
		List<KeyValuePair<IClient, List<Point2>>> list;
		lock (m_waitChunkListLock)
		{
			list = waitChunkList.ToList();
		}
		foreach (KeyValuePair<IClient, List<Point2>> item in list)
		{
			List<Point2> value = item.Value;
			if (value.Count == 0)
			{
				waitChunkList_toRemove.Add(item.Key);
				continue;
			}
			int num3 = Math.Min(val, value.Count);
			for (int j = 0; j < num3; j++)
			{
				if (CommonLib.WorkType == WorkType.Server && num2 >= num)
				{
					break;
				}
				int index = value.Count - 1;
				Point2 chunkCoord = value[index];
				TerrainChunk chunkAtCoords = m_subsystemTerrain.Terrain.GetChunkAtCoords(chunkCoord.X, chunkCoord.Y);
				IClient key = item.Key;
				if (chunkAtCoords != null)
				{
					if (chunkAtCoords.ThreadState > TerrainChunkState.InvalidContents4)
					{
						CommonLib.Net.QueuePackage(new SubsystemTerrainPackage(chunkAtCoords, useLocal: false)
						{
							To = key
						});
						num2++;
					}
					else
					{
						CommonLib.Net.QueuePackage(new SubsystemTerrainPackage(chunkAtCoords.Coords, 0)
						{
							To = key
						});
					}
				}
				else
				{
					CommonLib.Net.QueuePackage(new SubsystemTerrainPackage(chunkCoord, 1)
					{
						To = key
					});
				}
				value.RemoveAt(index);
			}
		}
		lock (m_waitChunkListLock)
		{
			foreach (IClient item2 in waitChunkList_toRemove)
			{
				waitChunkList.Remove(item2);
			}
		}
		waitChunkList_toRemove.Clear();
	}

	public void PrepareForDrawing(Camera camera)
	{
		if (CommonLib.WorkType == WorkType.Server)
		{
			return;
		}
		SetUpdateLocation(camera.GameWidget.PlayerData.PlayerIndex, camera.ViewPosition.XZ, SettingsManager.VisibilityRange, 64f);
		if (m_synchronousUpdateFrame != Time.FrameIndex)
		{
			return;
		}
		List<TerrainChunk> list = DetermineSynchronousUpdateChunks(camera.ViewPosition, camera.ViewDirection);
		if (list.Count <= 0)
		{
			return;
		}
		m_updateEvent.WaitOne();
		try
		{
			SendReceiveChunkStates();
			SendReceiveChunkStatesThread();
			foreach (TerrainChunk item in list)
			{
				if (CommonLib.WorkType != WorkType.Client)
				{
					while (item.ThreadState < TerrainChunkState.Valid)
					{
						UpdateChunkSingleStep(item, m_subsystemSky.SkyLightValue);
					}
					continue;
				}
				Task.Run(() =>
				{
					while (item.ThreadState < TerrainChunkState.Valid)
					{
						UpdateChunkSingleStep(item, m_subsystemSky.SkyLightValue);
					}
				});
			}
			SendReceiveChunkStatesThread();
			SendReceiveChunkStates();
		}
		finally
		{
			m_updateEvent.Set();
		}
	}

	public void DowngradeChunkNeighborhoodState(Point2 coordinates, int radius, TerrainChunkState state, bool forceGeometryRegeneration)
	{
		for (int i = -radius; i <= radius; i++)
		{
			for (int j = -radius; j <= radius; j++)
			{
				TerrainChunk chunkAtCoords = m_terrain.GetChunkAtCoords(coordinates.X + i, coordinates.Y + j);
				if (chunkAtCoords == null)
				{
					continue;
				}
				if (chunkAtCoords.State > state)
				{
					chunkAtCoords.State = state;
					if (forceGeometryRegeneration)
					{
						chunkAtCoords.InvalidateSliceContentsHashes();
					}
				}
				chunkAtCoords.WasDowngraded = true;
			}
		}
	}

	public void DowngradeAllChunksState(TerrainChunkState state, bool forceGeometryRegeneration)
	{
		TerrainChunk[] value = m_terrain.AllocatedChunks;
		foreach (TerrainChunk terrainChunk in value)
		{
			if (terrainChunk.State > state)
			{
				terrainChunk.State = state;
				if (forceGeometryRegeneration)
				{
					terrainChunk.InvalidateSliceContentsHashes();
				}
			}
			terrainChunk.WasDowngraded = true;
		}
	}

	private static bool IsChunkInRange(Vector2 chunkCenter, UpdateLocation[] locations)
	{
		for (int value = 0; value < locations.Length; value++)
		{
			float num = Vector2.DistanceSquared(locations[value].Center, chunkCenter);
			float num2 = MathUtils.Sqr(locations[value].ContentDistance);
			if (num <= num2)
			{
				return true;
			}
		}
		return false;
	}

	private bool AllocateAndFreeChunks(UpdateLocation[] locations)
	{
		bool value = false;
		TerrainChunk[] allocatedChunks = m_terrain.AllocatedChunks;
		foreach (TerrainChunk terrainChunk in allocatedChunks)
		{
			if (IsChunkInRange(terrainChunk.Center, locations))
			{
				continue;
			}
			value = true;
			foreach (SubsystemBlockBehavior blockBehavior in m_subsystemBlockBehaviors.BlockBehaviors)
			{
				blockBehavior.OnChunkDiscarding(terrainChunk);
			}
			m_subsystemTerrain.TerrainSerializer.SaveChunk(terrainChunk);
			lock (m_timersLock)
			{
				m_chunkLastUpdateTimes.Remove(terrainChunk);
			}
			m_subsystemTerrain.TerrainRenderer.DisposeTerrainChunkGeometryVertexIndexBuffers(terrainChunk);
			m_terrain.FreeChunk(terrainChunk);
		}
		for (int j = 0; j < locations.Length; j++)
		{
			Point2 point = Terrain.ToChunk(locations[j].Center - new Vector2(locations[j].ContentDistance));
			Point2 point2 = Terrain.ToChunk(locations[j].Center + new Vector2(locations[j].ContentDistance));
			for (int k = point.X; k <= point2.X; k++)
			{
				for (int l = point.Y; l <= point2.Y; l++)
				{
					Vector2 chunkCenter = new Vector2(((float)k + 0.5f) * 16f, ((float)l + 0.5f) * 16f);
					TerrainChunk chunkAtCoords = m_terrain.GetChunkAtCoords(k, l);
					if (chunkAtCoords == null)
					{
						if (IsChunkInRange(chunkCenter, locations))
						{
							value = true;
							m_terrain.AllocateChunk(k, l);
							DowngradeChunkNeighborhoodState(new Point2(k, l), 0, TerrainChunkState.NotLoaded, forceGeometryRegeneration: false);
							DowngradeChunkNeighborhoodState(new Point2(k, l), 1, TerrainChunkState.InvalidLight, forceGeometryRegeneration: false);
						}
					}
					else if (chunkAtCoords.Coords.X != k || chunkAtCoords.Coords.Y != l)
					{
						Log.Error("Chunk wraparound detected at {0}", chunkAtCoords.Coords);
					}
				}
			}
		}
		return value;
	}

	private bool SendReceiveChunkStates()
	{
		bool result = false;
		TerrainChunk[] chunks = m_updateParameters.Chunks;
		foreach (TerrainChunk terrainChunk in chunks)
		{
			if (terrainChunk.WasDowngraded)
			{
				terrainChunk.DowngradedState = terrainChunk.State;
				terrainChunk.WasDowngraded = false;
				result = true;
			}
			else if (terrainChunk.UpgradedState.HasValue)
			{
				terrainChunk.State = terrainChunk.UpgradedState.Value;
			}
			terrainChunk.UpgradedState = null;
		}
		return result;
	}

	private void SendReceiveChunkStatesThread()
	{
		TerrainChunk[] chunks = m_threadUpdateParameters.Chunks;
		foreach (TerrainChunk item in chunks)
		{
			if (item.DowngradedState.HasValue)
			{
				item.ThreadState = item.DowngradedState.Value;
				item.DowngradedState = null;
			}
			else if (item.WasUpgraded)
			{
				item.UpgradedState = item.ThreadState;
			}
			item.WasUpgraded = false;
		}
	}

	private void ThreadUpdateFunction()
	{
		while (!m_quitUpdateThread)
		{
			m_pauseEvent.WaitOne();
			m_updateEvent.WaitOne();
			try
			{
				if (!SynchronousUpdateFunction())
				{
					continue;
				}
				lock (m_unpauseLock)
				{
					if (!m_unpauseUpdateThread)
					{
						m_pauseEvent.Reset();
					}
					m_unpauseUpdateThread = false;
				}
			}
			catch (Exception)
			{
			}
			finally
			{
				m_updateEvent.Set();
			}
		}
	}

	private bool SynchronousUpdateFunction()
	{
		lock (m_updateParametersLock)
		{
			m_threadUpdateParameters = m_updateParameters;
			SendReceiveChunkStatesThread();
		}
		TerrainChunk terrainChunk = FindBestChunkToUpdate(out var desiredState);
		bool flag;
		lock (m_waitChunkListLock)
		{
			flag = waitChunkList.Values.Any((List<Point2> list) => list.Count > 0);
		}
		if ((terrainChunk != null) | flag)
		{
			if (terrainChunk != null && terrainChunk.ThreadState < desiredState)
			{
				UpdateChunkSingleStep(terrainChunk, m_subsystemSky.SkyLightValue);
			}
			IsBusy = true;
			return false;
		}
		IsBusy = false;
		return true;
	}

	private TerrainChunk FindBestChunkToUpdate(out TerrainChunkState desiredState)
	{
		TerrainChunk[] chunks = m_threadUpdateParameters.Chunks;
		UpdateLocation[] array = m_threadUpdateParameters.Locations.Values.ToArray();
		float num = float.MaxValue;
		TerrainChunk result = null;
		desiredState = TerrainChunkState.NotLoaded;
		TerrainChunk[] array2 = chunks;
		foreach (TerrainChunk terrainChunk in array2)
		{
			if (terrainChunk.ThreadState >= TerrainChunkState.Valid)
			{
				continue;
			}
			for (int j = 0; j < array.Length; j++)
			{
				float num2 = Vector2.DistanceSquared(array[j].Center, terrainChunk.Center);
				float num3 = num2;
				if (terrainChunk.ThreadState > TerrainChunkState.NotLoaded)
				{
					num3 += 14400f;
				}
				if (terrainChunk.IsFluidUpdateOnly)
				{
					bool flag;
					double value;
					lock (m_timersLock)
					{
						flag = m_chunkLastUpdateTimes.TryGetValue(terrainChunk, out value);
					}
					if (flag && Time.RealTime - value < 1.0)
					{
						num3 += 10000000f;
					}
				}
				if (num3 < num)
				{
					if (num2 <= MathUtils.Sqr(array[j].VisibilityDistance))
					{
						desiredState = TerrainChunkState.Valid;
						num = num3;
						result = terrainChunk;
					}
					else if (terrainChunk.ThreadState < TerrainChunkState.InvalidVertices1 && num2 <= MathUtils.Sqr(array[j].ContentDistance))
					{
						desiredState = TerrainChunkState.InvalidVertices1;
						num = num3;
						result = terrainChunk;
					}
				}
			}
		}
		return result;
	}

	private List<TerrainChunk> DetermineSynchronousUpdateChunks(Vector3 viewPosition, Vector3 viewDirection)
	{
		Vector3 vector = Vector3.Normalize(Vector3.Cross(viewDirection, Vector3.UnitY));
		Vector3 vector2 = Vector3.Normalize(Vector3.Cross(viewDirection, vector));
		Vector3[] array = new Vector3[6]
		{
			viewPosition,
			viewPosition + 6f * viewDirection,
			viewPosition + 6f * viewDirection - 6f * vector,
			viewPosition + 6f * viewDirection + 6f * vector,
			viewPosition + 6f * viewDirection - 2f * vector2,
			viewPosition + 6f * viewDirection + 2f * vector2
		};
		List<TerrainChunk> list = new List<TerrainChunk>();
		Vector3[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			Vector3 vector3 = array2[i];
			TerrainChunk chunkAtCell = m_terrain.GetChunkAtCell(Terrain.ToCell(vector3.X), Terrain.ToCell(vector3.Z));
			if (chunkAtCell != null && chunkAtCell.State < TerrainChunkState.Valid && !list.Contains(chunkAtCell))
			{
				list.Add(chunkAtCell);
			}
		}
		return list;
	}

	public void UpdateChunkSingleStep(TerrainChunk chunk, int skylightValue)
	{
		switch (chunk.ThreadState)
		{
		case TerrainChunkState.NotLoaded:
			if (CommonLib.WorkType == WorkType.Client)
			{
				chunk.WasUpgraded = true;
			}
			else if (m_subsystemTerrain.TerrainSerializer.LoadChunk(chunk))
			{
				chunk.ThreadState = TerrainChunkState.InvalidLight;
				chunk.WasUpgraded = true;
				chunk.IsLoaded = true;
			}
			else
			{
				chunk.ThreadState = TerrainChunkState.InvalidContents1;
				chunk.WasUpgraded = true;
			}
			break;
		case TerrainChunkState.InvalidContents1:
			m_subsystemTerrain.TerrainContentsGenerator.GenerateChunkContentsPass1(chunk);
			chunk.ThreadState = TerrainChunkState.InvalidContents2;
			chunk.WasUpgraded = true;
			break;
		case TerrainChunkState.InvalidContents2:
			m_subsystemTerrain.TerrainContentsGenerator.GenerateChunkContentsPass2(chunk);
			chunk.ThreadState = TerrainChunkState.InvalidContents3;
			chunk.WasUpgraded = true;
			break;
		case TerrainChunkState.InvalidContents3:
			m_subsystemTerrain.TerrainContentsGenerator.GenerateChunkContentsPass3(chunk);
			chunk.ThreadState = TerrainChunkState.InvalidContents4;
			chunk.WasUpgraded = true;
			break;
		case TerrainChunkState.InvalidContents4:
			m_subsystemTerrain.TerrainContentsGenerator.GenerateChunkContentsPass4(chunk);
			chunk.ThreadState = TerrainChunkState.InvalidLight;
			chunk.WasUpgraded = true;
			break;
		case TerrainChunkState.InvalidLight:
			GenerateChunkSunLightAndHeight(chunk, skylightValue);
			chunk.ThreadState = TerrainChunkState.InvalidPropagatedLight;
			chunk.WasUpgraded = true;
			chunk.LightPropagationMask = 0;
			break;
		case TerrainChunkState.InvalidPropagatedLight:
		{
			for (int i = -2; i <= 2; i++)
			{
				for (int j = -2; j <= 2; j++)
				{
					TerrainChunk chunkAtCell = m_terrain.GetChunkAtCell(chunk.Origin.X + i * 16, chunk.Origin.Y + j * 16);
					if (chunkAtCell != null && chunkAtCell.ThreadState < TerrainChunkState.InvalidPropagatedLight)
					{
						UpdateChunkSingleStep(chunkAtCell, skylightValue);
						return;
					}
				}
			}
			m_lightSources.Clear();
			for (int k = -1; k <= 1; k++)
			{
				for (int l = -1; l <= 1; l++)
				{
					int num = CalculateLightPropagationBitIndex(k, l);
					if (((chunk.LightPropagationMask >> num) & 1) == 0)
					{
						TerrainChunk chunkAtCell2 = m_terrain.GetChunkAtCell(chunk.Origin.X + k * 16, chunk.Origin.Y + l * 16);
						if (chunkAtCell2 != null)
						{
							GenerateChunkLightSources(chunkAtCell2);
							UpdateNeighborsLightPropagationBitmasks(chunkAtCell2);
						}
					}
				}
			}
			PropagateLight();
			chunk.ThreadState = TerrainChunkState.InvalidVertices1;
			chunk.WasUpgraded = true;
			break;
		}
		case TerrainChunkState.InvalidVertices1:
			if (CommonLib.WorkType != WorkType.Server)
			{
				lock (chunk.Geometry)
				{
					chunk.NewGeometryData = false;
					GenerateChunkVertices(chunk, even: true);
				}
			}
			chunk.ThreadState = TerrainChunkState.InvalidVertices2;
			chunk.WasUpgraded = true;
			break;
		case TerrainChunkState.InvalidVertices2:
			if (CommonLib.WorkType != WorkType.Server)
			{
				lock (chunk.Geometry)
				{
					GenerateChunkVertices(chunk, even: false);
					chunk.NewGeometryData = true;
				}
			}
			if (chunk.IsFluidUpdateOnly)
			{
				lock (m_timersLock)
				{
					m_chunkLastUpdateTimes[chunk] = Time.RealTime;
				}
				chunk.IsFluidUpdateOnly = false;
			}
			chunk.ThreadState = TerrainChunkState.Valid;
			chunk.WasUpgraded = true;
			break;
		}
	}

	private void GenerateChunkSunLightAndHeight(TerrainChunk chunk, int skylightValue)
	{
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				int inventory = 0;
				int num = 255;
				int num2 = 255;
				int num3 = TerrainChunk.CalculateCellIndex(i, 255, j);
				while (num2 >= 0)
				{
					int cellValueFast = chunk.GetCellValueFast(num3);
					if (Terrain.ExtractContents(cellValueFast) != 0)
					{
						inventory = num2;
						break;
					}
					cellValueFast = Terrain.ReplaceLight(cellValueFast, skylightValue);
					chunk.SetCellValueFast(num3, cellValueFast);
					num2--;
					num3--;
				}
				num2 = 0;
				num3 = TerrainChunk.CalculateCellIndex(i, 0, j);
				while (num2 <= inventory + 1)
				{
					int pickable = chunk.GetCellValueFast(num3);
					int item = Terrain.ExtractContents(pickable);
					if (BlocksManager.Blocks[item].IsTransparent)
					{
						num = num2;
						break;
					}
					pickable = Terrain.ReplaceLight(pickable, 0);
					chunk.SetCellValueFast(num3, pickable);
					num2++;
					num3++;
				}
				int inventory2 = skylightValue;
				num2 = inventory;
				num3 = TerrainChunk.CalculateCellIndex(i, inventory, j);
				if (inventory2 > 0)
				{
					while (num2 >= num)
					{
						int cellValueFast2 = chunk.GetCellValueFast(num3);
						int num4 = Terrain.ExtractContents(cellValueFast2);
						if (num4 != 0)
						{
							Block block = BlocksManager.Blocks[num4];
							if (!block.IsTransparent || block.LightAttenuation >= inventory2)
							{
								break;
							}
							inventory2 -= block.LightAttenuation;
						}
						cellValueFast2 = Terrain.ReplaceLight(cellValueFast2, inventory2);
						chunk.SetCellValueFast(num3, cellValueFast2);
						num2--;
						num3--;
					}
				}
				int sunlightHeight = num2 + 1;
				while (num2 >= num)
				{
					int projectile = chunk.GetCellValueFast(num3);
					projectile = Terrain.ReplaceLight(projectile, 0);
					chunk.SetCellValueFast(num3, projectile);
					num2--;
					num3--;
				}
				chunk.SetTopHeightFast(i, j, inventory);
				chunk.SetBottomHeightFast(i, j, num);
				chunk.SetSunlightHeightFast(i, j, sunlightHeight);
			}
		}
	}

	private void GenerateChunkLightSources(TerrainChunk chunk)
	{
		Block[] inventory = BlocksManager.Blocks;
		for (int movingBlockSet = 0; movingBlockSet < 16; movingBlockSet++)
		{
			for (int slotCapacity = 0; slotCapacity < 16; slotCapacity++)
			{
				int num = movingBlockSet + chunk.Origin.X;
				int num2 = slotCapacity + chunk.Origin.Y;
				TerrainChunk chunkAtCell = m_terrain.GetChunkAtCell(num - 1, num2);
				TerrainChunk chunkAtCell2 = m_terrain.GetChunkAtCell(num + 1, num2);
				TerrainChunk chunkAtCell3 = m_terrain.GetChunkAtCell(num, num2 - 1);
				TerrainChunk chunkAtCell4 = m_terrain.GetChunkAtCell(num, num2 + 1);
				if (chunkAtCell == null || chunkAtCell2 == null || chunkAtCell3 == null || chunkAtCell4 == null)
				{
					continue;
				}
				int topHeightFast = chunk.GetTopHeightFast(movingBlockSet, slotCapacity);
				int bottomHeightFast = chunk.GetBottomHeightFast(movingBlockSet, slotCapacity);
				int x = num - 1 - chunkAtCell.Origin.X;
				int z = num2 - chunkAtCell.Origin.Y;
				int x2 = num + 1 - chunkAtCell2.Origin.X;
				int z2 = num2 - chunkAtCell2.Origin.Y;
				int x3 = num - chunkAtCell3.Origin.X;
				int z3 = num2 - 1 - chunkAtCell3.Origin.Y;
				int x4 = num - chunkAtCell4.Origin.X;
				int z4 = num2 + 1 - chunkAtCell4.Origin.Y;
				int shaftValueFast = chunkAtCell.GetShaftValueFast(x, z);
				int shaftValueFast2 = chunkAtCell2.GetShaftValueFast(x2, z2);
				int shaftValueFast3 = chunkAtCell3.GetShaftValueFast(x3, z3);
				int shaftValueFast4 = chunkAtCell4.GetShaftValueFast(x4, z4);
				int x5 = Terrain.ExtractSunlightHeight(shaftValueFast);
				int x6 = Terrain.ExtractSunlightHeight(shaftValueFast2);
				int x7 = Terrain.ExtractSunlightHeight(shaftValueFast3);
				int x8 = Terrain.ExtractSunlightHeight(shaftValueFast4);
				int num3 = MathUtils.Min(x5, x6, x7, x8);
				int num4 = bottomHeightFast;
				int num5 = TerrainChunk.CalculateCellIndex(movingBlockSet, bottomHeightFast, slotCapacity);
				while (num4 <= topHeightFast)
				{
					int cellValueFast = chunk.GetCellValueFast(num5);
					int num6 = 0;
					Block block = inventory[Terrain.ExtractContents(cellValueFast)];
					if (num4 >= num3 && block.IsTransparent)
					{
						int cellLightFast = chunkAtCell.GetCellLightFast(x, num4, z);
						int cellLightFast2 = chunkAtCell2.GetCellLightFast(x2, num4, z2);
						int cellLightFast3 = chunkAtCell3.GetCellLightFast(x3, num4, z3);
						int cellLightFast4 = chunkAtCell4.GetCellLightFast(x4, num4, z4);
						num6 = MathUtils.Max(cellLightFast, cellLightFast2, cellLightFast3, cellLightFast4) - 1 - block.LightAttenuation;
					}
					if (block.DefaultEmittedLightAmount > 0)
					{
						num6 = MathUtils.Max(num6, block.GetEmittedLightAmount(cellValueFast));
					}
					if (num6 > Terrain.ExtractLight(cellValueFast))
					{
						chunk.SetCellValueFast(num5, Terrain.ReplaceLight(cellValueFast, num6));
						m_lightSources.Add(new LightSource
						{
							X = num,
							Y = num4,
							Z = num2,
							Light = num6
						});
					}
					num4++;
					num5++;
				}
			}
		}
	}

	private void PropagateLightSource(int x, int y, int z, int light)
	{
		TerrainChunk chunkAtCell = m_terrain.GetChunkAtCell(x, z);
		if (chunkAtCell == null)
		{
			return;
		}
		int index = TerrainChunk.CalculateCellIndex(x & 0xF, y, z & 0xF);
		int cellValueFast = chunkAtCell.GetCellValueFast(index);
		int num = Terrain.ExtractContents(cellValueFast);
		Block block = BlocksManager.Blocks[num];
		if (block.IsTransparent)
		{
			int num2 = light - block.LightAttenuation - 1;
			if (num2 > Terrain.ExtractLight(cellValueFast))
			{
				m_lightSources.Add(new LightSource
				{
					X = x,
					Y = y,
					Z = z,
					Light = num2
				});
				chunkAtCell.SetCellValueFast(index, Terrain.ReplaceLight(cellValueFast, num2));
			}
		}
	}

	private void PropagateLight()
	{
		for (int i = 0; i < m_lightSources.Count && i < 120000; i++)
		{
			LightSource lightSource = m_lightSources.Array[i];
			int light = lightSource.Light;
			if (light > 1)
			{
				PropagateLightSource(lightSource.X - 1, lightSource.Y, lightSource.Z, light);
				PropagateLightSource(lightSource.X + 1, lightSource.Y, lightSource.Z, light);
				if (lightSource.Y > 0)
				{
					PropagateLightSource(lightSource.X, lightSource.Y - 1, lightSource.Z, light);
				}
				if (lightSource.Y < 255)
				{
					PropagateLightSource(lightSource.X, lightSource.Y + 1, lightSource.Z, light);
				}
				PropagateLightSource(lightSource.X, lightSource.Y, lightSource.Z - 1, light);
				PropagateLightSource(lightSource.X, lightSource.Y, lightSource.Z + 1, light);
			}
		}
	}

	public void GenerateChunkVertices(TerrainChunk chunk, bool even)
	{
		m_subsystemTerrain.BlockGeometryGenerator.ResetCache();
		if (!chunk.Draws.TryGetValue(m_subsystemAnimatedTextures.AnimatedBlocksTexture, out var i))
		{
			i = new TerrainGeometry[16];
			for (int j = 0; j < 16; j++)
			{
				TerrainGeometry terrainGeometry = new TerrainGeometry(chunk.Draws, j);
				i[j] = terrainGeometry;
			}
			chunk.Draws.Add(m_subsystemAnimatedTextures.AnimatedBlocksTexture, i);
		}
		TerrainChunk slotCount = m_terrain.GetChunkAtCoords(chunk.Coords.X - 1, chunk.Coords.Y - 1);
		bool flag = m_terrain.GetChunkAtCoords(chunk.Coords.X, chunk.Coords.Y - 1) != null;
		TerrainChunk slotValue = m_terrain.GetChunkAtCoords(chunk.Coords.X + 1, chunk.Coords.Y - 1);
		bool flag2 = m_terrain.GetChunkAtCoords(chunk.Coords.X - 1, chunk.Coords.Y) != null;
		TerrainChunk chunkAtCoords = m_terrain.GetChunkAtCoords(chunk.Coords.X + 1, chunk.Coords.Y);
		TerrainChunk chunkAtCoords2 = m_terrain.GetChunkAtCoords(chunk.Coords.X - 1, chunk.Coords.Y + 1);
		TerrainChunk chunkAtCoords3 = m_terrain.GetChunkAtCoords(chunk.Coords.X, chunk.Coords.Y + 1);
		TerrainChunk chunkAtCoords4 = m_terrain.GetChunkAtCoords(chunk.Coords.X + 1, chunk.Coords.Y + 1);
		int num = 0;
		int num2 = 0;
		int num3 = 16;
		int num4 = 16;
		if (!flag2)
		{
			num++;
		}
		if (!flag)
		{
			num2++;
		}
		if (chunkAtCoords == null)
		{
			num3--;
		}
		if (chunkAtCoords3 == null)
		{
			num4--;
		}
		for (int k = 0; k < 16; k++)
		{
			if (k % 2 == 0 != even)
			{
				continue;
			}
			chunk.SliceContentsHashes[k] = CalculateChunkSliceContentsHash(chunk, k);
			int num5 = chunk.GeneratedSliceContentsHashes[k];
			if (num5 != 0 && num5 == chunk.SliceContentsHashes[k])
			{
				continue;
			}
			foreach (KeyValuePair<Texture2D, TerrainGeometry[]> draw in chunk.Draws)
			{
				TerrainGeometrySubset[] subsets = draw.Value[k].Subsets;
				for (int l = 0; l < subsets.Length; l++)
				{
					subsets[l].Vertices.Clear();
					subsets[l].Indices.Clear();
				}
			}
			for (int m = num; m < num3; m++)
			{
				for (int n = num2; n < num4; n++)
				{
					switch (m)
					{
					case 15:
						if ((n == 0 && slotValue == null) || (n == 15 && chunkAtCoords4 == null))
						{
							continue;
						}
						break;
					case 0:
						if ((n == 0 && slotCount == null) || (n == 15 && chunkAtCoords2 == null))
						{
							continue;
						}
						break;
					}
					int num6 = m + chunk.Origin.X;
					int num7 = n + chunk.Origin.Y;
					int bottomHeightFast = chunk.GetBottomHeightFast(m, n);
					int bottomHeight = m_terrain.GetBottomHeight(num6 - 1, num7);
					int bottomHeight2 = m_terrain.GetBottomHeight(num6 + 1, num7);
					int bottomHeight3 = m_terrain.GetBottomHeight(num6, num7 - 1);
					int bottomHeight4 = m_terrain.GetBottomHeight(num6, num7 + 1);
					int x = MathUtils.Min(bottomHeightFast - 1, MathUtils.Min(bottomHeight, bottomHeight2, bottomHeight3, bottomHeight4));
					int x2 = chunk.GetTopHeightFast(m, n) + 1;
					int num8 = MathUtils.Max(16 * k, x, 1);
					int num9 = MathUtils.Min(16 * (k + 1), x2, 255);
					int num10 = TerrainChunk.CalculateCellIndex(m, 0, n);
					for (int num11 = num8; num11 < num9; num11++)
					{
						int cellValueFast = chunk.GetCellValueFast(num10 + num11);
						int num12 = Terrain.ExtractContents(cellValueFast);
						if (num12 != 0)
						{
							BlocksManager.Blocks[num12].GenerateTerrainVertices(m_subsystemTerrain.BlockGeometryGenerator, i[k], cellValueFast, num6, num11, num7);
						}
					}
				}
			}
		}
	}

	private static int CalculateLightPropagationBitIndex(int x, int z)
	{
		return x + 1 + 3 * (z + 1);
	}

	private void UpdateNeighborsLightPropagationBitmasks(TerrainChunk chunk)
	{
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				TerrainChunk chunkAtCoords = m_terrain.GetChunkAtCoords(chunk.Coords.X + i, chunk.Coords.Y + j);
				if (chunkAtCoords != null)
				{
					int num = CalculateLightPropagationBitIndex(-i, -j);
					chunkAtCoords.LightPropagationMask |= 1 << num;
				}
			}
		}
	}

	private int CalculateChunkSliceContentsHash(TerrainChunk chunk, int sliceIndex)
	{
		int num = 1;
		int num2 = chunk.Origin.X - 1;
		int num3 = chunk.Origin.X + 16 + 1;
		int num4 = chunk.Origin.Y - 1;
		int num5 = chunk.Origin.Y + 16 + 1;
		int x = MathUtils.Max(16 * sliceIndex - 1, 0);
		int x2 = MathUtils.Min(16 * (sliceIndex + 1) + 1, 256);
		for (int i = num2; i < num3; i++)
		{
			for (int j = num4; j < num5; j++)
			{
				TerrainChunk chunkAtCell = m_terrain.GetChunkAtCell(i, j);
				if (chunkAtCell != null)
				{
					int x3 = i & 0xF;
					int z = j & 0xF;
					int shaftValueFast = chunkAtCell.GetShaftValueFast(x3, z);
					int num6 = Terrain.ExtractBottomHeight(shaftValueFast);
					int num7 = Terrain.ExtractTopHeight(shaftValueFast);
					int num8 = MathUtils.Max(x, num6 - 1);
					int num9 = MathUtils.Min(x2, num7 + 2);
					int num10 = TerrainChunk.CalculateCellIndex(x3, num8, z);
					int num11 = num10 + num9 - num8;
					while (num10 < num11)
					{
						num += chunkAtCell.GetCellValueFast(num10++);
						num *= 31;
					}
					num += Terrain.ExtractTemperature(shaftValueFast);
					num *= 31;
					num += Terrain.ExtractHumidity(shaftValueFast);
					num *= 31;
					num += num8;
					num *= 31;
				}
			}
		}
		num += m_terrain.SeasonTemperature;
		num *= 31;
		num += m_terrain.SeasonHumidity;
		return num * 31;
	}

	private void NotifyBlockBehaviors(TerrainChunk chunk)
	{
		foreach (SubsystemBlockBehavior blockBehavior in m_subsystemBlockBehaviors.BlockBehaviors)
		{
			blockBehavior.OnChunkInitialized(chunk);
		}
		bool isLoaded = chunk.IsLoaded;
		SubsystemBlockBehavior[][] item = new SubsystemBlockBehavior[1024][];
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				int x = i + chunk.Origin.X;
				int z = j + chunk.Origin.Y;
				int num = TerrainChunk.CalculateCellIndex(i, 0, j);
				int num2 = 0;
				while (num2 < 255)
				{
					int cellValueFast = chunk.GetCellValueFast(num);
					int num3 = Terrain.ExtractContents(cellValueFast);
					if (num3 != 0)
					{
						SubsystemBlockBehavior[] array = item[num3];
						if (array == null)
						{
							array = (item[num3] = m_subsystemBlockBehaviors.GetBlockBehaviors(num3));
						}
						for (int k = 0; k < array.Length; k++)
						{
							array[k].OnBlockGenerated(cellValueFast, x, num2, z, isLoaded);
						}
					}
					num2++;
					num++;
				}
			}
		}
	}

	private void UnpauseUpdateThread()
	{
		lock (m_unpauseLock)
		{
			m_unpauseUpdateThread = true;
			m_pauseEvent.Set();
		}
	}

	private void SettingsManager_SettingChanged(string name)
	{
		if (name == "Brightness")
		{
			DowngradeAllChunksState(TerrainChunkState.InvalidVertices1, forceGeometryRegeneration: true);
		}
	}

	static TerrainUpdater()
	{
		MaxServerChunksPerFrame = 6;
	}
}
