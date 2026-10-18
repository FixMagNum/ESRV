using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Engine;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemElectricity : Subsystem, IUpdateable
{
	public class NetSimulate
	{
		public int StartStep;

		public Dictionary<Point3, float> SaveData = new Dictionary<Point3, float>();
	}

	private static ElectricConnectionPath[] m_connectionPathsTable;

	private static ElectricConnectorDirection?[] m_connectorDirectionsTable;

	private static int[] m_connectorFacesTable;

	private float m_remainingSimulationTime;

	private Dictionary<Point3, float> m_persistentElementsVoltages = new Dictionary<Point3, float>();

	private Dictionary<ElectricElement, bool> m_electricElements = new Dictionary<ElectricElement, bool>();

	private Dictionary<CellFace, ElectricElement> m_electricElementsByCellFace = new Dictionary<CellFace, ElectricElement>();

	private Dictionary<Point3, bool> m_pointsToUpdate = new Dictionary<Point3, bool>();

	private Dictionary<Point3, ElectricElement> m_electricElementsToAdd = new Dictionary<Point3, ElectricElement>();

	private Dictionary<ElectricElement, bool> m_electricElementsToRemove = new Dictionary<ElectricElement, bool>();

	private Dictionary<Point3, bool> m_wiresToUpdate = new Dictionary<Point3, bool>();

	private List<Dictionary<ElectricElement, bool>> m_listsCache = new List<Dictionary<ElectricElement, bool>>();

	private Dictionary<int, Dictionary<ElectricElement, bool>> m_futureSimulateLists = new Dictionary<int, Dictionary<ElectricElement, bool>>();

	private Dictionary<ElectricElement, bool> m_nextStepSimulateList;

	private DynamicArray<ElectricConnectionPath> m_tmpConnectionPaths = new DynamicArray<ElectricConnectionPath>();

	private Dictionary<CellFace, bool> m_tmpVisited = new Dictionary<CellFace, bool>();

	private Dictionary<CellFace, bool> m_tmpResult = new Dictionary<CellFace, bool>();

	public static bool DebugDrawElectrics;

	public static int SimulatedElectricElements;

	public const float CircuitStepDuration = 0.01f;

	public List<NetSimulate> list = new List<NetSimulate>();

	private Dictionary<Point3, float> m_lastSyncedVoltages;

	public SubsystemTime SubsystemTime { get; private set; }

	public SubsystemTerrain SubsystemTerrain { get; private set; }

	public SubsystemAudio SubsystemAudio { get; private set; }

	public int FrameStartCircuitStep { get; private set; }

	public int CircuitStep { get; private set; }

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public void OnElectricElementBlockGenerated(int x, int y, int z)
	{
		m_pointsToUpdate[new Point3(x, y, z)] = false;
	}

	public void OnElectricElementBlockAdded(int x, int y, int z)
	{
		m_pointsToUpdate[new Point3(x, y, z)] = true;
	}

	public void OnElectricElementBlockRemoved(int x, int y, int z)
	{
		m_pointsToUpdate[new Point3(x, y, z)] = true;
	}

	public void OnElectricElementBlockModified(int x, int y, int z)
	{
		m_pointsToUpdate[new Point3(x, y, z)] = true;
	}

	public void OnChunkDiscarding(TerrainChunk chunk)
	{
		foreach (CellFace key in m_electricElementsByCellFace.Keys)
		{
			if (key.X >= chunk.Origin.X && key.X < chunk.Origin.X + 16 && key.Z >= chunk.Origin.Y && key.Z < chunk.Origin.Y + 16)
			{
				m_pointsToUpdate[new Point3(key.X, key.Y, key.Z)] = false;
			}
		}
	}

	public static ElectricConnectorDirection? GetConnectorDirection(int mountingFace, int rotation, int connectorFace)
	{
		ElectricConnectorDirection? result = m_connectorDirectionsTable[6 * mountingFace + connectorFace];
		if (result.HasValue)
		{
			if (result.Value < ElectricConnectorDirection.In)
			{
				return (ElectricConnectorDirection)((int)(result.Value + rotation) % 4);
			}
			return result;
		}
		return null;
	}

	public static int GetConnectorFace(int mountingFace, ElectricConnectorDirection connectionDirection)
	{
		return m_connectorFacesTable[(int)(5 * mountingFace + connectionDirection)];
	}

	public void GetAllConnectedNeighbors(int x, int y, int z, int mountingFace, DynamicArray<ElectricConnectionPath> list)
	{
		int cellValue = SubsystemTerrain.Terrain.GetCellValue(x, y, z);
		if (!(BlocksManager.Blocks[Terrain.ExtractContents(cellValue)] is IElectricElementBlock electricElementBlock))
		{
			return;
		}
		for (ElectricConnectorDirection electricConnectorDirection = ElectricConnectorDirection.Top; electricConnectorDirection < (ElectricConnectorDirection)5; electricConnectorDirection++)
		{
			for (int i = 0; i < 4; i++)
			{
				ElectricConnectionPath electricConnectionPath = m_connectionPathsTable[20 * mountingFace + 4 * (int)electricConnectorDirection + i];
				if (electricConnectionPath == null)
				{
					break;
				}
				ElectricConnectorType? connectorType = electricElementBlock.GetConnectorType(SubsystemTerrain, cellValue, mountingFace, electricConnectionPath.ConnectorFace, x, y, z);
				if (!connectorType.HasValue)
				{
					break;
				}
				int x2 = x + electricConnectionPath.NeighborOffsetX;
				int y2 = y + electricConnectionPath.NeighborOffsetY;
				int z2 = z + electricConnectionPath.NeighborOffsetZ;
				int cellValue2 = SubsystemTerrain.Terrain.GetCellValue(x2, y2, z2);
				if (!(BlocksManager.Blocks[Terrain.ExtractContents(cellValue2)] is IElectricElementBlock electricElementBlock2))
				{
					continue;
				}
				ElectricConnectorType? connectorType2 = electricElementBlock2.GetConnectorType(SubsystemTerrain, cellValue2, electricConnectionPath.NeighborFace, electricConnectionPath.NeighborConnectorFace, x2, y2, z2);
				if (connectorType2.HasValue && ((connectorType.Value != ElectricConnectorType.Input && connectorType2.Value != ElectricConnectorType.Output) || (connectorType.Value != ElectricConnectorType.Output && connectorType2.Value != ElectricConnectorType.Input)))
				{
					int connectionMask = electricElementBlock.GetConnectionMask(cellValue);
					int connectionMask2 = electricElementBlock2.GetConnectionMask(cellValue2);
					if ((connectionMask & connectionMask2) != 0)
					{
						list.Add(electricConnectionPath);
					}
				}
			}
		}
	}

	public ElectricElement GetElectricElement(int x, int y, int z, int mountingFace)
	{
		m_electricElementsByCellFace.TryGetValue(new CellFace(x, y, z, mountingFace), out var value);
		return value;
	}

	public void QueueElectricElementForSimulation(ElectricElement electricElement, int circuitStep)
	{
		if (circuitStep == CircuitStep + 1)
		{
			if (m_nextStepSimulateList == null && !m_futureSimulateLists.TryGetValue(CircuitStep + 1, out m_nextStepSimulateList))
			{
				m_nextStepSimulateList = GetListFromCache();
				m_futureSimulateLists.Add(CircuitStep + 1, m_nextStepSimulateList);
			}
			m_nextStepSimulateList[electricElement] = true;
		}
		else if (circuitStep > CircuitStep + 1)
		{
			if (!m_futureSimulateLists.TryGetValue(circuitStep, out var value))
			{
				value = GetListFromCache();
				m_futureSimulateLists.Add(circuitStep, value);
			}
			value[electricElement] = true;
		}
	}

	public void QueueElectricElementConnectionsForSimulation(ElectricElement electricElement, int circuitStep)
	{
		foreach (ElectricConnection connection in electricElement.Connections)
		{
			if (connection.ConnectorType != ElectricConnectorType.Input && connection.NeighborConnectorType != ElectricConnectorType.Output)
			{
				QueueElectricElementForSimulation(connection.NeighborElectricElement, circuitStep);
			}
		}
	}

	public float? ReadPersistentVoltage(Point3 point)
	{
		if (m_persistentElementsVoltages.TryGetValue(point, out var value))
		{
			return value;
		}
		return null;
	}

	public void WritePersistentVoltage(Point3 point, float voltage)
	{
		m_persistentElementsVoltages[point] = voltage;
	}

	public void Update(float dt)
	{
		if (CommonLib.WorkType != WorkType.Client)
		{
			if (m_lastSyncedVoltages == null)
			{
				m_lastSyncedVoltages = new Dictionary<Point3, float>();
			}
			NetSimulate netSimulate = new NetSimulate();
			netSimulate.StartStep = CircuitStep;
			bool flag = false;
			foreach (KeyValuePair<Point3, float> persistentElementsVoltage in m_persistentElementsVoltages)
			{
				if (!m_lastSyncedVoltages.TryGetValue(persistentElementsVoltage.Key, out var value) || value != persistentElementsVoltage.Value)
				{
					netSimulate.SaveData.Add(persistentElementsVoltage.Key, persistentElementsVoltage.Value);
					flag = true;
				}
			}
			FrameStartCircuitStep = CircuitStep;
			SimulatedElectricElements = 0;
			m_remainingSimulationTime = MathUtils.Min(m_remainingSimulationTime + dt, 0.1f);
			while (m_remainingSimulationTime >= 0.01f)
			{
				UpdateElectricElements();
				int circuitStep = CircuitStep + 1;
				CircuitStep = circuitStep;
				m_remainingSimulationTime -= 0.01f;
				m_nextStepSimulateList = null;
				if (!m_futureSimulateLists.TryGetValue(CircuitStep, out var value2))
				{
					continue;
				}
				m_futureSimulateLists.Remove(CircuitStep);
				SimulatedElectricElements += value2.Count;
				foreach (ElectricElement key in value2.Keys)
				{
					if (m_electricElements.ContainsKey(key))
					{
						SimulateElectricElement(key);
					}
				}
				ReturnListToCache(value2);
			}
			if (!SubsystemTime.PeriodicGameTimeEvent(0.04, 0.0))
			{
				return;
			}
			if (flag)
			{
				foreach (KeyValuePair<Point3, float> saveDatum in netSimulate.SaveData)
				{
					m_lastSyncedVoltages[saveDatum.Key] = saveDatum.Value;
				}
				list.Add(netSimulate);
				CommonLib.Net.QueuePackage(new SubsystemElectricityPackage(list));
			}
			list.Clear();
			return;
		}
		UpdateElectricElements();
		m_remainingSimulationTime = MathUtils.Min(m_remainingSimulationTime + dt, 0.1f);
		FrameStartCircuitStep = CircuitStep;
		while (m_remainingSimulationTime >= 0.01f)
		{
			UpdateElectricElements();
			if (list.Count > 0 && CircuitStep <= list[0].StartStep)
			{
				if (list[0].StartStep == CircuitStep)
				{
					foreach (KeyValuePair<Point3, float> saveDatum2 in list[0].SaveData)
					{
						if (m_persistentElementsVoltages.TryGetValue(saveDatum2.Key, out var _))
						{
							m_persistentElementsVoltages[saveDatum2.Key] = saveDatum2.Value;
						}
					}
				}
				m_nextStepSimulateList = null;
				if (m_futureSimulateLists.TryGetValue(CircuitStep, out var value4))
				{
					m_futureSimulateLists.Remove(CircuitStep);
					SimulatedElectricElements += value4.Count;
					foreach (ElectricElement key2 in value4.Keys)
					{
						if (m_electricElements.ContainsKey(key2))
						{
							SimulateElectricElement(key2);
						}
					}
					ReturnListToCache(value4);
				}
				if (list[0].StartStep == CircuitStep)
				{
					list.RemoveAt(0);
					m_remainingSimulationTime -= 0.01f;
				}
				int circuitStep2 = CircuitStep + 1;
				CircuitStep = circuitStep2;
			}
			else
			{
				m_remainingSimulationTime -= 0.01f;
			}
		}
		list.Clear();
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		SubsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		SubsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		SubsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		CircuitStep = valuesDictionary.GetValue("Step", 0);
		string[] array = valuesDictionary.GetValue<string>("VoltagesByCell").Split(new char[1] { ';' }, StringSplitOptions.RemoveEmptyEntries);
		int num = 0;
		while (true)
		{
			if (num < array.Length)
			{
				string[] array2 = array[num].Split(',');
				if (array2.Length != 4)
				{
					break;
				}
				int x = int.Parse(array2[0], CultureInfo.InvariantCulture);
				int y = int.Parse(array2[1], CultureInfo.InvariantCulture);
				int z = int.Parse(array2[2], CultureInfo.InvariantCulture);
				float value = float.Parse(array2[3], CultureInfo.InvariantCulture);
				m_persistentElementsVoltages[new Point3(x, y, z)] = value;
				num++;
				continue;
			}
			return;
		}
		throw new InvalidOperationException("Invalid number of tokens.");
	}

	public override void Save(ValuesDictionary valuesDictionary)
	{
		int num = 0;
		StringBuilder stringBuilder = new StringBuilder();
		foreach (KeyValuePair<Point3, float> persistentElementsVoltage in m_persistentElementsVoltages)
		{
			if (num > 500)
			{
				break;
			}
			stringBuilder.Append(persistentElementsVoltage.Key.X.ToString(CultureInfo.InvariantCulture));
			stringBuilder.Append(',');
			stringBuilder.Append(persistentElementsVoltage.Key.Y.ToString(CultureInfo.InvariantCulture));
			stringBuilder.Append(',');
			stringBuilder.Append(persistentElementsVoltage.Key.Z.ToString(CultureInfo.InvariantCulture));
			stringBuilder.Append(',');
			stringBuilder.Append(persistentElementsVoltage.Value.ToString(CultureInfo.InvariantCulture));
			stringBuilder.Append(';');
			num++;
		}
		valuesDictionary.SetValue("VoltagesByCell", stringBuilder.ToString());
		valuesDictionary.SetValue("Step", CircuitStep);
	}

	private static ElectricConnectionPath GetConnectionPath(int mountingFace, ElectricConnectorDirection localConnector, int neighborIndex)
	{
		return m_connectionPathsTable[16 * mountingFace + 4 * (int)localConnector + neighborIndex];
	}

	private void SimulateElectricElement(ElectricElement electricElement)
	{
		if (electricElement.Simulate())
		{
			QueueElectricElementConnectionsForSimulation(electricElement, CircuitStep + 1);
		}
	}

	private void AddElectricElement(ElectricElement electricElement)
	{
		m_electricElements.Add(electricElement, value: true);
		foreach (CellFace z in electricElement.CellFaces)
		{
			m_electricElementsByCellFace.Add(z, electricElement);
			m_tmpConnectionPaths.Clear();
			GetAllConnectedNeighbors(z.X, z.Y, z.Z, z.Face, m_tmpConnectionPaths);
			foreach (ElectricConnectionPath rgb in m_tmpConnectionPaths)
			{
				CellFace num2 = new CellFace(z.X + rgb.NeighborOffsetX, z.Y + rgb.NeighborOffsetY, z.Z + rgb.NeighborOffsetZ, rgb.NeighborFace);
				if (m_electricElementsByCellFace.TryGetValue(num2, out var num5) && num5 != electricElement)
				{
					int windVelocity = SubsystemTerrain.Terrain.GetCellValue(z.X, z.Y, z.Z);
					int num6 = Terrain.ExtractContents(windVelocity);
					ElectricConnectorType vector = ((IElectricElementBlock)BlocksManager.Blocks[num6]).GetConnectorType(SubsystemTerrain, windVelocity, z.Face, rgb.ConnectorFace, z.X, z.Y, z.Z).Value;
					int num7 = SubsystemTerrain.Terrain.GetCellValue(num2.X, num2.Y, num2.Z);
					int vector2 = Terrain.ExtractContents(num7);
					ElectricConnectorType unitY = ((IElectricElementBlock)BlocksManager.Blocks[vector2]).GetConnectorType(SubsystemTerrain, num7, num2.Face, rgb.NeighborConnectorFace, num2.X, num2.Y, num2.Z).Value;
					electricElement.Connections.Add(new ElectricConnection
					{
						CellFace = z,
						ConnectorFace = rgb.ConnectorFace,
						ConnectorType = vector,
						NeighborElectricElement = num5,
						NeighborCellFace = num2,
						NeighborConnectorFace = rgb.NeighborConnectorFace,
						NeighborConnectorType = unitY
					});
					num5.Connections.Add(new ElectricConnection
					{
						CellFace = num2,
						ConnectorFace = rgb.NeighborConnectorFace,
						ConnectorType = unitY,
						NeighborElectricElement = electricElement,
						NeighborCellFace = z,
						NeighborConnectorFace = rgb.ConnectorFace,
						NeighborConnectorType = vector
					});
				}
			}
		}
		QueueElectricElementForSimulation(electricElement, CircuitStep + 1);
		QueueElectricElementConnectionsForSimulation(electricElement, CircuitStep + 2);
		electricElement.OnAdded();
	}

	private void RemoveElectricElement(ElectricElement electricElement)
	{
		electricElement.OnRemoved();
		QueueElectricElementConnectionsForSimulation(electricElement, CircuitStep + 1);
		m_electricElements.Remove(electricElement);
		foreach (CellFace cellFace in electricElement.CellFaces)
		{
			m_electricElementsByCellFace.Remove(cellFace);
		}
		foreach (ElectricConnection connection in electricElement.Connections)
		{
			int num = connection.NeighborElectricElement.Connections.FirstIndex((ElectricConnection c) => c.NeighborElectricElement == electricElement);
			if (num >= 0)
			{
				connection.NeighborElectricElement.Connections.RemoveAt(num);
			}
		}
	}

	private void UpdateElectricElements()
	{
		foreach (KeyValuePair<Point3, bool> item in m_pointsToUpdate)
		{
			Point3 key = item.Key;
			int cellValue = SubsystemTerrain.Terrain.GetCellValue(key.X, key.Y, key.Z);
			for (int j = 0; j < 6; j++)
			{
				ElectricElement electricElement = GetElectricElement(key.X, key.Y, key.Z, j);
				if (electricElement != null)
				{
					if (electricElement is WireDomainElectricElement)
					{
						m_wiresToUpdate[key] = true;
					}
					else
					{
						m_electricElementsToRemove[electricElement] = true;
					}
				}
			}
			if (item.Value)
			{
				m_persistentElementsVoltages.Remove(key);
			}
			int num = Terrain.ExtractContents(cellValue);
			if (BlocksManager.Blocks[num] is IElectricWireElementBlock)
			{
				m_wiresToUpdate[key] = true;
			}
			else if (BlocksManager.Blocks[num] is IElectricElementBlock electricElementBlock)
			{
				ElectricElement electricElement2 = electricElementBlock.CreateElectricElement(this, cellValue, key.X, key.Y, key.Z);
				if (electricElement2 != null)
				{
					m_electricElementsToAdd[key] = electricElement2;
				}
			}
		}
		RemoveWireDomains();
		foreach (KeyValuePair<ElectricElement, bool> item2 in m_electricElementsToRemove)
		{
			RemoveElectricElement(item2.Key);
		}
		AddWireDomains();
		foreach (ElectricElement value in m_electricElementsToAdd.Values)
		{
			AddElectricElement(value);
		}
		m_pointsToUpdate.Clear();
		m_wiresToUpdate.Clear();
		m_electricElementsToAdd.Clear();
		m_electricElementsToRemove.Clear();
	}

	private void AddWireDomains()
	{
		m_tmpVisited.Clear();
		foreach (Point3 currentWindStrength in m_wiresToUpdate.Keys)
		{
			for (int x = currentWindStrength.X - 1; x <= currentWindStrength.X + 1; x++)
			{
				for (int num = currentWindStrength.Y - 1; num <= currentWindStrength.Y + 1; num++)
				{
					for (int minLength = currentWindStrength.Z - 1; minLength <= currentWindStrength.Z + 1; minLength++)
					{
						for (int maxLength = 0; maxLength < 6; maxLength++)
						{
							m_tmpResult.Clear();
							ScanWireDomain(new CellFace(x, num, minLength, maxLength), m_tmpVisited, m_tmpResult);
							if (m_tmpResult.Count > 0)
							{
								WireDomainElectricElement width = new WireDomainElectricElement(this, m_tmpResult.Keys);
								AddElectricElement(width);
							}
						}
					}
				}
			}
		}
	}

	private void RemoveWireDomains()
	{
		foreach (Point3 flag in m_wiresToUpdate.Keys)
		{
			for (int viewPosition = flag.X - 1; viewPosition <= flag.X + 1; viewPosition++)
			{
				for (int viewDirection = flag.Y - 1; viewDirection <= flag.Y + 1; viewDirection++)
				{
					for (int i = flag.Z - 1; i <= flag.Z + 1; i++)
					{
						for (int f = 0; f < 6; f++)
						{
							if (m_electricElementsByCellFace.TryGetValue(new CellFace(viewPosition, viewDirection, i, f), out var num3) && num3 is WireDomainElectricElement)
							{
								RemoveElectricElement(num3);
							}
						}
					}
				}
			}
		}
	}

	private void ScanWireDomain(CellFace startCellFace, Dictionary<CellFace, bool> visited, Dictionary<CellFace, bool> result)
	{
		DynamicArray<CellFace> dynamicArray = new DynamicArray<CellFace>();
		dynamicArray.Add(startCellFace);
		while (dynamicArray.Count > 0)
		{
			CellFace key = dynamicArray.Array[--dynamicArray.Count];
			if (visited.ContainsKey(key))
			{
				continue;
			}
			TerrainChunk chunkAtCell = SubsystemTerrain.Terrain.GetChunkAtCell(key.X, key.Z);
			if (chunkAtCell == null || !chunkAtCell.AreBehaviorsNotified)
			{
				continue;
			}
			int cellValue = SubsystemTerrain.Terrain.GetCellValue(key.X, key.Y, key.Z);
			int num = Terrain.ExtractContents(cellValue);
			if (!(BlocksManager.Blocks[num] is IElectricWireElementBlock electricWireElementBlock))
			{
				continue;
			}
			int connectedWireFacesMask = electricWireElementBlock.GetConnectedWireFacesMask(cellValue, key.Face);
			if (connectedWireFacesMask == 0)
			{
				continue;
			}
			for (int i = 0; i < 6; i++)
			{
				if ((connectedWireFacesMask & (1 << i)) == 0)
				{
					continue;
				}
				CellFace key2 = new CellFace(key.X, key.Y, key.Z, i);
				visited.Add(key2, value: true);
				result.Add(key2, value: true);
				m_tmpConnectionPaths.Clear();
				GetAllConnectedNeighbors(key2.X, key2.Y, key2.Z, key2.Face, m_tmpConnectionPaths);
				foreach (ElectricConnectionPath tmpConnectionPath in m_tmpConnectionPaths)
				{
					int x = key2.X + tmpConnectionPath.NeighborOffsetX;
					int y = key2.Y + tmpConnectionPath.NeighborOffsetY;
					int z = key2.Z + tmpConnectionPath.NeighborOffsetZ;
					dynamicArray.Add(new CellFace(x, y, z, tmpConnectionPath.NeighborFace));
				}
			}
		}
	}

	private Dictionary<ElectricElement, bool> GetListFromCache()
	{
		if (m_listsCache.Count > 0)
		{
			Dictionary<ElectricElement, bool> result = m_listsCache[m_listsCache.Count - 1];
			m_listsCache.RemoveAt(m_listsCache.Count - 1);
			return result;
		}
		return new Dictionary<ElectricElement, bool>();
	}

	private void ReturnListToCache(Dictionary<ElectricElement, bool> list)
	{
		list.Clear();
		m_listsCache.Add(list);
	}

	private void DebugDraw()
	{
	}

	static SubsystemElectricity()
	{
		m_connectionPathsTable = new ElectricConnectionPath[120]
		{
			new ElectricConnectionPath(0, 1, -1, 4, 4, 0),
			new ElectricConnectionPath(0, 1, 0, 0, 4, 5),
			new ElectricConnectionPath(0, 1, -1, 2, 4, 5),
			new ElectricConnectionPath(0, 0, 0, 5, 4, 2),
			new ElectricConnectionPath(-1, 0, -1, 3, 3, 0),
			new ElectricConnectionPath(-1, 0, 0, 0, 3, 1),
			new ElectricConnectionPath(-1, 0, -1, 2, 3, 1),
			new ElectricConnectionPath(0, 0, 0, 1, 3, 2),
			new ElectricConnectionPath(0, -1, -1, 5, 5, 0),
			new ElectricConnectionPath(0, -1, 0, 0, 5, 4),
			new ElectricConnectionPath(0, -1, -1, 2, 5, 4),
			new ElectricConnectionPath(0, 0, 0, 4, 5, 2),
			new ElectricConnectionPath(1, 0, -1, 1, 1, 0),
			new ElectricConnectionPath(1, 0, 0, 0, 1, 3),
			new ElectricConnectionPath(1, 0, -1, 2, 1, 3),
			new ElectricConnectionPath(0, 0, 0, 3, 1, 2),
			new ElectricConnectionPath(0, 0, -1, 2, 2, 0),
			null,
			null,
			null,
			new ElectricConnectionPath(-1, 1, 0, 4, 4, 1),
			new ElectricConnectionPath(0, 1, 0, 1, 4, 5),
			new ElectricConnectionPath(-1, 1, 0, 3, 4, 5),
			new ElectricConnectionPath(0, 0, 0, 5, 4, 3),
			new ElectricConnectionPath(-1, 0, 1, 0, 0, 1),
			new ElectricConnectionPath(0, 0, 1, 1, 0, 2),
			new ElectricConnectionPath(-1, 0, 1, 3, 0, 2),
			new ElectricConnectionPath(0, 0, 0, 2, 0, 3),
			new ElectricConnectionPath(-1, -1, 0, 5, 5, 1),
			new ElectricConnectionPath(0, -1, 0, 1, 5, 4),
			new ElectricConnectionPath(-1, -1, 0, 3, 5, 4),
			new ElectricConnectionPath(0, 0, 0, 4, 5, 3),
			new ElectricConnectionPath(-1, 0, -1, 2, 2, 1),
			new ElectricConnectionPath(0, 0, -1, 1, 2, 0),
			new ElectricConnectionPath(-1, 0, -1, 3, 2, 0),
			new ElectricConnectionPath(0, 0, 0, 0, 2, 3),
			new ElectricConnectionPath(-1, 0, 0, 3, 3, 1),
			null,
			null,
			null,
			new ElectricConnectionPath(0, 1, 1, 4, 4, 2),
			new ElectricConnectionPath(0, 1, 0, 2, 4, 5),
			new ElectricConnectionPath(0, 1, 1, 0, 4, 5),
			new ElectricConnectionPath(0, 0, 0, 5, 4, 0),
			new ElectricConnectionPath(1, 0, 1, 1, 1, 2),
			new ElectricConnectionPath(1, 0, 0, 2, 1, 3),
			new ElectricConnectionPath(1, 0, 1, 0, 1, 3),
			new ElectricConnectionPath(0, 0, 0, 3, 1, 0),
			new ElectricConnectionPath(0, -1, 1, 5, 5, 2),
			new ElectricConnectionPath(0, -1, 0, 2, 5, 4),
			new ElectricConnectionPath(0, -1, 1, 0, 5, 4),
			new ElectricConnectionPath(0, 0, 0, 4, 5, 0),
			new ElectricConnectionPath(-1, 0, 1, 3, 3, 2),
			new ElectricConnectionPath(-1, 0, 0, 2, 3, 1),
			new ElectricConnectionPath(-1, 0, 1, 0, 3, 1),
			new ElectricConnectionPath(0, 0, 0, 1, 3, 0),
			new ElectricConnectionPath(0, 0, 1, 0, 0, 2),
			null,
			null,
			null,
			new ElectricConnectionPath(1, 1, 0, 4, 4, 3),
			new ElectricConnectionPath(0, 1, 0, 3, 4, 5),
			new ElectricConnectionPath(1, 1, 0, 1, 4, 5),
			new ElectricConnectionPath(0, 0, 0, 5, 4, 1),
			new ElectricConnectionPath(1, 0, -1, 2, 2, 3),
			new ElectricConnectionPath(0, 0, -1, 3, 2, 0),
			new ElectricConnectionPath(1, 0, -1, 1, 2, 0),
			new ElectricConnectionPath(0, 0, 0, 0, 2, 1),
			new ElectricConnectionPath(1, -1, 0, 5, 5, 3),
			new ElectricConnectionPath(0, -1, 0, 3, 5, 4),
			new ElectricConnectionPath(1, -1, 0, 1, 5, 4),
			new ElectricConnectionPath(0, 0, 0, 4, 5, 1),
			new ElectricConnectionPath(1, 0, 1, 0, 0, 3),
			new ElectricConnectionPath(0, 0, 1, 3, 0, 2),
			new ElectricConnectionPath(1, 0, 1, 1, 0, 2),
			new ElectricConnectionPath(0, 0, 0, 2, 0, 1),
			new ElectricConnectionPath(1, 0, 0, 1, 1, 3),
			null,
			null,
			null,
			new ElectricConnectionPath(0, -1, -1, 2, 2, 4),
			new ElectricConnectionPath(0, 0, -1, 4, 2, 0),
			new ElectricConnectionPath(0, -1, -1, 5, 2, 0),
			new ElectricConnectionPath(0, 0, 0, 0, 2, 5),
			new ElectricConnectionPath(-1, -1, 0, 3, 3, 4),
			new ElectricConnectionPath(-1, 0, 0, 4, 3, 1),
			new ElectricConnectionPath(-1, -1, 0, 5, 3, 1),
			new ElectricConnectionPath(0, 0, 0, 1, 3, 5),
			new ElectricConnectionPath(0, -1, 1, 0, 0, 4),
			new ElectricConnectionPath(0, 0, 1, 4, 0, 2),
			new ElectricConnectionPath(0, -1, 1, 5, 0, 2),
			new ElectricConnectionPath(0, 0, 0, 2, 0, 5),
			new ElectricConnectionPath(1, -1, 0, 1, 1, 4),
			new ElectricConnectionPath(1, 0, 0, 4, 1, 3),
			new ElectricConnectionPath(1, -1, 0, 5, 1, 3),
			new ElectricConnectionPath(0, 0, 0, 3, 1, 5),
			new ElectricConnectionPath(0, -1, 0, 5, 5, 4),
			null,
			null,
			null,
			new ElectricConnectionPath(0, 1, -1, 2, 2, 5),
			new ElectricConnectionPath(0, 0, -1, 5, 2, 0),
			new ElectricConnectionPath(0, 1, -1, 4, 2, 0),
			new ElectricConnectionPath(0, 0, 0, 0, 2, 4),
			new ElectricConnectionPath(1, 1, 0, 1, 1, 5),
			new ElectricConnectionPath(1, 0, 0, 5, 1, 3),
			new ElectricConnectionPath(1, 1, 0, 4, 1, 3),
			new ElectricConnectionPath(0, 0, 0, 3, 1, 4),
			new ElectricConnectionPath(0, 1, 1, 0, 0, 5),
			new ElectricConnectionPath(0, 0, 1, 5, 0, 2),
			new ElectricConnectionPath(0, 1, 1, 4, 0, 2),
			new ElectricConnectionPath(0, 0, 0, 2, 0, 4),
			new ElectricConnectionPath(-1, 1, 0, 3, 3, 5),
			new ElectricConnectionPath(-1, 0, 0, 5, 3, 1),
			new ElectricConnectionPath(-1, 1, 0, 4, 3, 1),
			new ElectricConnectionPath(0, 0, 0, 1, 3, 4),
			new ElectricConnectionPath(0, 1, 0, 4, 4, 5),
			null,
			null,
			null
		};
		m_connectorDirectionsTable = new ElectricConnectorDirection?[36]
		{
			null,
			ElectricConnectorDirection.Right,
			ElectricConnectorDirection.In,
			ElectricConnectorDirection.Left,
			ElectricConnectorDirection.Top,
			ElectricConnectorDirection.Bottom,
			ElectricConnectorDirection.Left,
			null,
			ElectricConnectorDirection.Right,
			ElectricConnectorDirection.In,
			ElectricConnectorDirection.Top,
			ElectricConnectorDirection.Bottom,
			ElectricConnectorDirection.In,
			ElectricConnectorDirection.Left,
			null,
			ElectricConnectorDirection.Right,
			ElectricConnectorDirection.Top,
			ElectricConnectorDirection.Bottom,
			ElectricConnectorDirection.Right,
			ElectricConnectorDirection.In,
			ElectricConnectorDirection.Left,
			null,
			ElectricConnectorDirection.Top,
			ElectricConnectorDirection.Bottom,
			ElectricConnectorDirection.Bottom,
			ElectricConnectorDirection.Right,
			ElectricConnectorDirection.Top,
			ElectricConnectorDirection.Left,
			null,
			ElectricConnectorDirection.In,
			ElectricConnectorDirection.Top,
			ElectricConnectorDirection.Right,
			ElectricConnectorDirection.Bottom,
			ElectricConnectorDirection.Left,
			ElectricConnectorDirection.In,
			null
		};
		m_connectorFacesTable = new int[30]
		{
			4, 3, 5, 1, 2, 4, 0, 5, 2, 3,
			4, 1, 5, 3, 0, 4, 2, 5, 0, 1,
			2, 1, 0, 3, 5, 0, 1, 2, 3, 4
		};
		DebugDrawElectrics = false;
	}
}
