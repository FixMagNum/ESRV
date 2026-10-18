using System;
using System.Collections.Generic;
using Engine;
using Engine.Graphics;
using TemplatesDatabase;

namespace Game;

public class SubsystemStringsBlockBehavior : SubsystemBlockBehavior, IDrawable, IUpdateable
{
	private struct RopeChain
	{
		public Vector3 Start;

		public Vector3 End;

		public StringBlock.StringKind Kind;

		public bool IsOnFloor;

		public bool IsIsolated;

		public int? Color;
	}

	private struct AttachmentData
	{
		public Vector3 Start;

		public Vector3 End;

		public float T;

		public bool IsOnFloor;
	}

	private SubsystemTerrain m_subsystemTerrain;

	private SubsystemWeather m_subsystemWeather;

	private SubsystemTime m_subsystemTime;

	private PrimitivesRenderer3D m_primitivesRenderer;

	private HashSet<Point3> m_activeStrings;

	private Random m_random;

	private LightSmoother m_lightSmoother;

	private List<RopeChain> m_ropeChains;

	private bool m_chainsNeedUpdate;

	private float m_windUpdateTimer;

	private Vector3 m_currentWindOffset;

	public Dictionary<Point3, int> m_foodValues;

	private Dictionary<Point3, float> m_foodProgress;

	private Dictionary<Point3, WaterDripsParticleSystem> m_fatDripSystems;

	private SubsystemParticles m_subsystemParticles;

	private SubsystemPickables m_subsystemPickables;

	private SubsystemAudio m_subsystemAudio;

	public SubsystemGameInfo m_subsystemGameInfo;

	private List<Point3> m_foodUpdateBuffer;

	private Dictionary<Point3, AttachmentData> m_foodAttachmentPoints;

	private Dictionary<Point3, float> m_lightCache = new Dictionary<Point3, float>();

	public SubsystemFluidDynamics m_subsystemFluidDynamics;

	public override int[] HandledBlocks => new int[1] { 198 };

	public int[] DrawOrders => new int[1] { 10 };

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemFluidDynamics = Project.FindSubsystem<SubsystemFluidDynamics>(throwOnError: true);
		if (!valuesDictionary.ContainsKey("StringFoods"))
		{
			return;
		}
		foreach (ValuesDictionary value2 in valuesDictionary.GetValue<ValuesDictionary>("StringFoods").Values)
		{
			Point3 value = value2.GetValue<Point3>("Pos");
			m_foodValues[value] = value2.GetValue<int>("Val");
			m_foodProgress[value] = value2.GetValue<float>("Prog");
		}
	}

	public override void OnBlockAdded(int value, int oldValue, int x, int y, int z)
	{
		m_activeStrings.Add(new Point3(x, y, z));
		m_chainsNeedUpdate = true;
		CheckSupport(x, y, z);
	}

	public override void OnBlockRemoved(int value, int newValue, int x, int y, int z)
	{
		m_activeStrings.Remove(new Point3(x, y, z));
		m_chainsNeedUpdate = true;
		DropFood(new Point3(x, y, z));
	}

	public override void OnNeighborBlockChanged(int x, int y, int z, int nx, int ny, int nz)
	{
		CheckSupport(x, y, z);
	}

	private void CheckSupport(int x, int y, int z)
	{
		int cellValue = m_subsystemTerrain.Terrain.GetCellValue(x, y, z);
		if (Terrain.ExtractContents(cellValue) != 198)
		{
			return;
		}
		int faceMask = StringBlock.GetFaceMask(Terrain.ExtractData(cellValue));
		bool flag = false;
		for (int i = 0; i < 6; i++)
		{
			if ((faceMask & (1 << i)) != 0)
			{
				Point3 point = CellFace.FaceToPoint3(i);
				int num = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(x + point.X, y + point.Y, z + point.Z));
				if (BlocksManager.Blocks[num].IsCollidable || num == 198)
				{
					flag = true;
					break;
				}
			}
		}
		if (!flag && Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(x, y + 1, z)) == 198)
		{
			flag = true;
		}
		if (!flag)
		{
			m_subsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
		}
	}

	public void Update(float dt)
	{
		if (m_chainsNeedUpdate)
		{
			RebuildRopeChains();
			RecalculateAttachmentPoints();
			m_chainsNeedUpdate = false;
		}
		float currentWindStrength = m_subsystemWeather.CurrentWindStrength;
		Vector3 v = Vector3.Zero;
		if (currentWindStrength > 2f)
		{
			float num = MathUtils.Min(currentWindStrength / 10f, 1f) * 0.5f;
			float num2 = MathUtils.Max(currentWindStrength - 10f, 0f) / 6f * 0.25f;
			float num3 = num + num2;
			v = new Vector3(m_subsystemWeather.CurrentWindVector.X * num3, 0f, m_subsystemWeather.CurrentWindVector.Y * num3);
		}
		m_currentWindOffset = Vector3.Lerp(m_currentWindOffset, v, 2f * dt);
		if (m_foodUpdateBuffer.Count == 0 && m_foodValues.Count > 0)
		{
			m_foodUpdateBuffer.AddRange(m_foodValues.Keys);
		}
		float num4 = MathUtils.Max((float)m_foodValues.Count / 5f, 1f);
		m_windUpdateTimer += num4 * dt;
		int num5 = MathUtils.Min((int)m_windUpdateTimer, m_foodUpdateBuffer.Count);
		m_windUpdateTimer -= num5;
		float num6 = ((m_foodValues.Count > 0) ? ((float)m_foodValues.Count / num4) : 5f);
		for (int i = 0; i < num5; i++)
		{
			int index = m_foodUpdateBuffer.Count - 1;
			Point3 point = m_foodUpdateBuffer[index];
			m_foodUpdateBuffer.RemoveAt(index);
			if (!m_foodValues.ContainsKey(point))
			{
				continue;
			}
			float num7 = num6;
			if (m_subsystemTerrain.Terrain.GetChunkAtCell(point.X, point.Z) == null)
			{
				continue;
			}
			int num8 = m_foodValues[point];
			Block block = BlocksManager.Blocks[Terrain.ExtractContents(num8)];
			bool flag = false;
			for (int j = 1; j <= 5; j++)
			{
				int num9 = point.Y - j;
				if (num9 < 0)
				{
					break;
				}
				int cellValue = m_subsystemTerrain.Terrain.GetCellValue(point.X, num9, point.Z);
				int num10 = Terrain.ExtractContents(cellValue);
				if ((num10 == 209 || num10 == 354) && Terrain.ExtractData(cellValue) > 0)
				{
					flag = true;
					break;
				}
				if (BlocksManager.Blocks[num10].IsCollidable)
				{
					break;
				}
			}
			Vector3 position = new Vector3((float)point.X + 0.5f, (float)point.Y + 0.5f, (float)point.Z + 0.5f);
			bool theoreticalWindExposure = m_subsystemWeather.GetTheoreticalWindExposure(position);
			int rotPeriod = block.GetRotPeriod(num8);
			bool flag2 = (Terrain.ExtractData(num8) & 0x10000) != 0;
			if (rotPeriod > 0)
			{
				float num11 = (flag ? 10f : m_subsystemFluidDynamics.GetTemperatureAtPoint(point));
				if (num11 <= 0f && !flag2)
				{
					num8 = Terrain.ReplaceData(num8, Terrain.ExtractData(num8) | 0x10000);
					m_foodValues[point] = num8;
					flag2 = true;
				}
				else if ((num11 > 0f) & flag2)
				{
					num8 = Terrain.ReplaceData(num8, Terrain.ExtractData(num8) & -65537);
					m_foodValues[point] = num8;
					flag2 = false;
				}
			}
			if (flag)
			{
				m_foodProgress[point] += num7 / 120f;
				if (!m_fatDripSystems.ContainsKey(point))
				{
					WaterDripsParticleSystem waterDripsParticleSystem = new WaterDripsParticleSystem(m_subsystemTerrain, m_subsystemWeather, position, isBlockSource: true);
					waterDripsParticleSystem.UseHangingPhase = true;
					waterDripsParticleSystem.DripRateMultiplier = 0.5f;
					m_subsystemParticles.AddParticleSystem(waterDripsParticleSystem);
					m_fatDripSystems[point] = waterDripsParticleSystem;
				}
				if (m_foodProgress[point] >= 1f)
				{
					int processedFood = GetProcessedFood(num8, 2);
					if (processedFood != 0)
					{
						int damage = block.GetDamage(num8);
						m_foodValues[point] = BlocksManager.Blocks[Terrain.ExtractContents(processedFood)].SetDamage(processedFood, damage);
						m_foodProgress[point] = 0f;
						RemoveFatDrips(point);
						m_subsystemAudio.PlayRandomSound("Audio/Sizzles", 1f, 0f, position, 3f, autoDelay: true);
					}
					else
					{
						m_foodProgress[point] = 0f;
					}
				}
				continue;
			}
			if (theoreticalWindExposure)
			{
				RemoveFatDrips(point);
				float num12 = MathUtils.Max(1f, currentWindStrength);
				m_foodProgress[point] += num7 * num12 / 600f;
				if (m_foodProgress[point] >= 1f)
				{
					int processedFood2 = GetProcessedFood(num8, 1);
					if (processedFood2 != 0)
					{
						int damage2 = block.GetDamage(num8);
						m_foodValues[point] = BlocksManager.Blocks[Terrain.ExtractContents(processedFood2)].SetDamage(processedFood2, damage2);
						m_foodProgress[point] = 0f;
						m_subsystemAudio.PlayRandomSound("Audio/Sizzles", 0.5f, 0f, position, 3f, autoDelay: true);
					}
					else
					{
						m_foodProgress[point] = 0f;
					}
				}
				continue;
			}
			RemoveFatDrips(point);
			m_foodProgress[point] = 0f;
			if (rotPeriod <= 0 || flag2 || m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative || m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Adventure)
			{
				continue;
			}
			float itemDurabilityMultiplier = m_subsystemGameInfo.WorldSettings.ItemDurabilityMultiplier;
			if (!(itemDurabilityMultiplier > 0f))
			{
				continue;
			}
			float num13 = num7 / ((float)rotPeriod * itemDurabilityMultiplier);
			if (m_random.Float(0f, 1f) < num13)
			{
				int num14 = block.GetDamage(num8) + 1;
				if (num14 > 1)
				{
					m_foodValues[point] = block.GetDamageDestructionValue(num8);
				}
				else
				{
					m_foodValues[point] = block.SetDamage(num8, num14);
				}
			}
		}
	}

	public void Draw(Camera camera, int drawOrder)
	{
		float num = 1024f;
		FlatBatch3D flatBatch3D = m_primitivesRenderer.FlatBatch(0, DepthStencilState.Default);
		double gameTime = m_subsystemTime.GameTime;
		foreach (RopeChain ropeChain in m_ropeChains)
		{
			float num2 = Vector3.DistanceSquared(ropeChain.Start, camera.ViewPosition);
			float num3 = Vector3.DistanceSquared(ropeChain.End, camera.ViewPosition);
			if (!(num2 <= num) && !(num3 <= num))
			{
				continue;
			}
			Vector3 vector = (ropeChain.Start + ropeChain.End) * 0.5f;
			float num4 = Vector3.Distance(ropeChain.Start, ropeChain.End);
			float num5 = ((MathUtils.Abs(ropeChain.Start.Y - ropeChain.End.Y) > num4 * 0.8f) ? 0.02f : (num4 * 0.1f));
			Vector3 vector2 = (ropeChain.IsOnFloor ? vector : (vector + new Vector3(0f, 0f - num5, 0f)));
			bool flag = false;
			float num6 = float.MaxValue;
			Vector3 end = vector;
			Vector3[] array = new Vector3[3] { ropeChain.Start, ropeChain.End, vector2 };
			foreach (Vector3 vector3 in array)
			{
				Vector3 v = vector3 - camera.ViewPosition;
				float num7 = Vector3.Dot(v, camera.ViewDirection);
				float num8 = v.LengthSquared();
				if (num8 < num6)
				{
					num6 = num8;
				}
				if (!flag && num7 > -1.5f && (num8 <= 4f || (num7 >= 0f && num7 * num7 >= num8 * 0.15f)))
				{
					flag = true;
					end = vector3;
				}
			}
			if (!flag)
			{
				continue;
			}
			if (num6 > 9f && num4 < 4f)
			{
				TerrainRaycastResult? terrainRaycastResult = m_subsystemTerrain.Raycast(camera.ViewPosition, end, useInteractionBoxes: false, skipAirBlocks: true, (int val, float d) =>
				{
					Block block = BlocksManager.Blocks[Terrain.ExtractContents(val)];
					return block.IsCollidable && !block.IsTransparent;
				});
				if (terrainRaycastResult.HasValue && terrainRaycastResult.Value.Distance * terrainRaycastResult.Value.Distance < num6 - 0.5f)
				{
					continue;
				}
			}
			Point3 cell = new Point3((int)Math.Floor(vector.X), (int)Math.Floor(vector.Y), (int)Math.Floor(vector.Z));
			float stableLight = GetStableLight(cell, vector);
			Vector3 vector4 = Vector3.Zero;
			if (m_currentWindOffset != Vector3.Zero && !ropeChain.IsOnFloor)
			{
				float windExposureFactor = m_subsystemWeather.GetWindExposureFactor(vector);
				if (windExposureFactor > 0f)
				{
					float num9 = MathUtils.Sin((float)gameTime * 2f + vector.X + vector.Z);
					vector4 = m_currentWindOffset * num9 * windExposureFactor;
				}
			}
			bool isRough = false;
			int num10 = (int)Math.Floor(vector.X);
			int num11 = (int)Math.Floor(vector.Y);
			int num12 = (int)Math.Floor(vector.Z);
			for (int num13 = 0; num13 < 6; num13++)
			{
				Point3 point = CellFace.FaceToPoint3(num13);
				int num14 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(num10 + point.X, num11 + point.Y, num12 + point.Z));
				if (num14 == 2 || num14 == 7 || num14 == 8 || num14 == 6 || num14 == 62 || num14 == 72)
				{
					isRough = true;
					break;
				}
			}
			FlatBatch3D batch = flatBatch3D;
			Vector3 start = ropeChain.Start;
			Vector3 end2 = ropeChain.End;
			StringBlock.StringKind kind = ropeChain.Kind;
			float light = stableLight;
			Vector3 wind = vector4;
			Vector3 start2 = ropeChain.Start;
			DrawStringSegment(batch, start, end2, kind, ropeChain.Color, light, wind, start2.GetHashCode(), ropeChain.IsOnFloor, isRough, ropeChain.IsIsolated, camera);
		}
		foreach (KeyValuePair<Point3, int> foodValue in m_foodValues)
		{
			Point3 key = foodValue.Key;
			Vector3 vector5 = new Vector3((float)key.X + 0.5f, (float)key.Y + 0.5f, (float)key.Z + 0.5f);
			if (!(Vector3.DistanceSquared(camera.ViewPosition, vector5) <= num))
			{
				continue;
			}
			Vector3 v2 = vector5 - camera.ViewPosition;
			float num15 = Vector3.Dot(v2, camera.ViewDirection);
			if (!(num15 > -1.5f))
			{
				continue;
			}
			float num16 = v2.LengthSquared();
			if (!(num16 <= 4f) && (!(num15 >= 0f) || !(num15 * num15 >= num16 * 0.15f)))
			{
				continue;
			}
			if (num16 > 9f)
			{
				TerrainRaycastResult? terrainRaycastResult2 = m_subsystemTerrain.Raycast(camera.ViewPosition, vector5, useInteractionBoxes: false, skipAirBlocks: true, (int val, float d) =>
				{
					Block block = BlocksManager.Blocks[Terrain.ExtractContents(val)];
					return block.IsCollidable && !block.IsTransparent;
				});
				if (terrainRaycastResult2.HasValue && terrainRaycastResult2.Value.Distance * terrainRaycastResult2.Value.Distance < num16 - 0.5f)
				{
					continue;
				}
			}
			float windExposureFactor2 = m_subsystemWeather.GetWindExposureFactor(vector5);
			Vector3 vector6 = Vector3.Zero;
			if (windExposureFactor2 > 0f && m_currentWindOffset != Vector3.Zero)
			{
				vector6 = m_currentWindOffset * MathUtils.Sin((float)gameTime * 2f + vector5.X + vector5.Z) * windExposureFactor2;
			}
			Vector3 vector7 = vector5;
			Vector3 vector9;
			if (m_foodAttachmentPoints.TryGetValue(key, out var value))
			{
				Vector3 start3 = value.Start;
				Vector3 end3 = value.End;
				Vector3 vector8;
				if (value.IsOnFloor)
				{
					vector8 = (start3 + end3) * 0.5f;
				}
				else
				{
					float num17 = Vector3.Distance(start3, end3);
					float num18 = ((MathUtils.Abs(start3.Y - end3.Y) > num17 * 0.8f) ? 0.02f : (num17 * 0.1f));
					vector8 = (start3 + end3) * 0.5f + new Vector3(0f, 0f - num18, 0f);
					if (windExposureFactor2 > 0f && m_currentWindOffset != Vector3.Zero)
					{
						float num19 = MathUtils.Sin((float)gameTime * 2f + ((start3 + end3) * 0.5f).X + ((start3 + end3) * 0.5f).Z);
						vector8 += m_currentWindOffset * num19 * windExposureFactor2;
					}
				}
				float t = value.T;
				vector7 = (1f - t) * (1f - t) * start3 + 2f * (1f - t) * t * vector8 + t * t * end3;
				vector9 = vector7;
			}
			else
			{
				vector9 = vector7 + vector6 * 0.5f;
			}
			float pitch = 0f;
			float roll = 0f;
			if (windExposureFactor2 > 0f && m_currentWindOffset != Vector3.Zero)
			{
				float currentWindStrength = m_subsystemWeather.CurrentWindStrength;
				float num20 = MathUtils.Min(currentWindStrength / 10f, 1f) * 0.5f;
				float num21 = MathUtils.Max(currentWindStrength - 10f, 0f) / 6f * 0.25f;
				float num22 = num20 + num21;
				float num23 = MathUtils.Sin((float)gameTime * 3f + (float)key.X + (float)key.Z) * num22 * windExposureFactor2 * 1.5f;
				Vector2 currentWindVector = m_subsystemWeather.CurrentWindVector;
				if (currentWindVector.LengthSquared() > 0.001f)
				{
					currentWindVector = Vector2.Normalize(currentWindVector);
					pitch = currentWindVector.Y * num23;
					roll = (0f - currentWindVector.X) * num23;
				}
			}
			float num24 = (float)(((key.X * 374761393) ^ (key.Y * 668265261) ^ (key.Z * 1234567)) % 1000) / 1000f * ((float)Math.PI * 2f);
			Matrix matrix;
			if ((StringBlock.GetFaceMask(Terrain.ExtractData(m_subsystemTerrain.Terrain.GetCellValueFast(key.X, key.Y, key.Z))) & 0x20) != 0)
			{
				Vector3 position = new Vector3((float)key.X + 0.5f, (float)key.Y + 0.05f, (float)key.Z + 0.5f);
				matrix = GetItemMatrix(foodValue.Value, num24) * Matrix.CreateTranslation(position);
			}
			else
			{
				Vector3 vector10 = vector9 + Vector3.Transform(new Vector3(0f, -0.15f, 0f), Matrix.CreateFromYawPitchRoll(0f, pitch, roll));
				float stableLight2 = GetStableLight(key, vector5);
				Color color = GetStringColor(StringBlock.StringKind.Thread, 0.5f) * stableLight2;
				flatBatch3D.QueueLine(vector9, vector10, color);
				matrix = Matrix.CreateTranslation(0f, -0.05f, 0f) * Matrix.CreateRotationY(num24) * Matrix.CreateFromYawPitchRoll(0f, pitch, roll) * Matrix.CreateTranslation(vector10);
			}
			DrawBlockEnvironmentData drawBlockEnvironmentData = new DrawBlockEnvironmentData();
			drawBlockEnvironmentData.SubsystemTerrain = m_subsystemTerrain;
			drawBlockEnvironmentData.InWorldMatrix = matrix;
			drawBlockEnvironmentData.Light = (int)(GetStableLight(key, vector5) * 15f);
			drawBlockEnvironmentData.Humidity = m_subsystemTerrain.Terrain.GetSeasonalHumidity(key.X, key.Z);
			drawBlockEnvironmentData.Temperature = m_subsystemTerrain.Terrain.GetSeasonalTemperature(key.X, key.Z);
			BlocksManager.Blocks[Terrain.ExtractContents(foodValue.Value)].DrawBlock(m_primitivesRenderer, foodValue.Value, Color.White, 0.25f, ref matrix, drawBlockEnvironmentData);
		}
		m_primitivesRenderer.Flush(camera.ViewProjectionMatrix);
	}

	private Color GetStringColor(StringBlock.StringKind kind, float noise)
	{
		return kind switch
		{
			StringBlock.StringKind.Tendon => Color.Lerp(new Color(167, 109, 97), new Color(236, 184, 170), noise), 
			StringBlock.StringKind.Bark => Color.Lerp(new Color(60, 40, 20), new Color(90, 60, 40), noise), 
			StringBlock.StringKind.BoiledBark => Color.Lerp(new Color(30, 20, 10), new Color(45, 30, 20), noise), 
			_ => Color.Lerp(new Color(200, 190, 150), new Color(230, 220, 180), noise), 
		};
	}

	public SubsystemStringsBlockBehavior()
	{
		m_foodUpdateBuffer = new List<Point3>();
		m_foodAttachmentPoints = new Dictionary<Point3, AttachmentData>();
		m_foodValues = new Dictionary<Point3, int>();
		m_foodProgress = new Dictionary<Point3, float>();
		m_fatDripSystems = new Dictionary<Point3, WaterDripsParticleSystem>();
		m_ropeChains = new List<RopeChain>();
		m_chainsNeedUpdate = true;
		m_currentWindOffset = Vector3.Zero;
		m_lightSmoother = new LightSmoother();
		m_random = new Random();
		m_primitivesRenderer = new PrimitivesRenderer3D();
		m_activeStrings = new HashSet<Point3>();
	}

	public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded)
	{
		m_activeStrings.Add(new Point3(x, y, z));
		m_chainsNeedUpdate = true;
	}

	private bool IsAnchor(Point3 p)
	{
		int num = 0;
		Point3 point = default;
		Point3 point2 = default;
		for (int i = 0; i < 6; i++)
		{
			Point3 point3 = p + CellFace.FaceToPoint3(i);
			if (m_activeStrings.Contains(point3))
			{
				num++;
				if (num == 1)
				{
					point = point3;
				}
				if (num == 2)
				{
					point2 = point3;
				}
			}
		}
		if (num == 2)
		{
			return !(point - p == p - point2);
		}
		return true;
	}

	private void RebuildRopeChains()
	{
		m_ropeChains.Clear();
		HashSet<Point3> hashSet = new HashSet<Point3>();
		foreach (Point3 activeString in m_activeStrings)
		{
			int data = Terrain.ExtractData(m_subsystemTerrain.Terrain.GetCellValueFast(activeString.X, activeString.Y, activeString.Z));
			int faceMask = StringBlock.GetFaceMask(data);
			int num = 0;
			for (int i = 0; i < 6; i++)
			{
				if ((faceMask & (1 << i)) != 0)
				{
					num++;
				}
			}
			if (num <= 1)
			{
				continue;
			}
			StringBlock.StringKind kind = StringBlock.GetKind(data);
			Vector3 nodePosition = GetNodePosition(activeString, out var _);
			Vector3 vector = new Vector3((float)activeString.X + 0.5f, (float)activeString.Y + 0.5f, (float)activeString.Z + 0.5f);
			for (int j = 0; j < 6; j++)
			{
				if ((faceMask & (1 << j)) != 0)
				{
					Vector3 start = vector + CellFace.FaceToVector3(j) * 0.48f;
					List<RopeChain> ropeChains = m_ropeChains;
					RopeChain item = new RopeChain
					{
						Start = start,
						End = nodePosition,
						Kind = kind,
						IsOnFloor = (j == 5),
						IsIsolated = false,
						Color = StringBlock.GetColor(data)
					};
					ropeChains.Add(item);
				}
			}
		}
		foreach (Point3 activeString2 in m_activeStrings)
		{
			if (IsAnchor(activeString2))
			{
				BuildChainsFrom(activeString2, hashSet);
			}
		}
		foreach (Point3 activeString3 in m_activeStrings)
		{
			for (int k = 0; k < 6; k++)
			{
				Point3 item2 = activeString3 + CellFace.FaceToPoint3(k);
				if (m_activeStrings.Contains(item2))
				{
					Point3 item3 = new Point3(activeString3.X + item2.X, activeString3.Y + item2.Y, activeString3.Z + item2.Z);
					if (!hashSet.Contains(item3))
					{
						BuildChainsFrom(activeString3, hashSet);
					}
				}
			}
		}
		foreach (Point3 activeString4 in m_activeStrings)
		{
			bool flag = false;
			for (int l = 0; l < 6; l++)
			{
				if (m_activeStrings.Contains(activeString4 + CellFace.FaceToPoint3(l)))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				int data2 = Terrain.ExtractData(m_subsystemTerrain.Terrain.GetCellValueFast(activeString4.X, activeString4.Y, activeString4.Z));
				Vector3 nodePosition2 = GetNodePosition(activeString4, out var isOnFloor2);
				List<RopeChain> ropeChains2 = m_ropeChains;
				RopeChain item4 = new RopeChain
				{
					Start = nodePosition2,
					End = (isOnFloor2 ? nodePosition2 : (nodePosition2 + new Vector3(0f, -0.25f, 0f))),
					Kind = StringBlock.GetKind(data2),
					IsOnFloor = isOnFloor2,
					IsIsolated = true,
					Color = StringBlock.GetColor(data2)
				};
				ropeChains2.Add(item4);
			}
		}
	}

	private void BuildChainsFrom(Point3 anchor, HashSet<Point3> visitedSegments)
	{
		StringBlock.StringKind kind = StringBlock.GetKind(Terrain.ExtractData(m_subsystemTerrain.Terrain.GetCellValueFast(anchor.X, anchor.Y, anchor.Z)));
		for (int i = 0; i < 6; i++)
		{
			Point3 point = anchor + CellFace.FaceToPoint3(i);
			if (!m_activeStrings.Contains(point))
			{
				continue;
			}
			Point3 item = new Point3(anchor.X + point.X, anchor.Y + point.Y, anchor.Z + point.Z);
			if (visitedSegments.Contains(item))
			{
				continue;
			}
			Point3 point2 = point;
			Point3 point3 = anchor;
			visitedSegments.Add(item);
			while (!IsAnchor(point2) && point2 != anchor)
			{
				Point3 point4 = point2;
				for (int j = 0; j < 6; j++)
				{
					Point3 point5 = point2 + CellFace.FaceToPoint3(j);
					if (point5 != point3 && m_activeStrings.Contains(point5))
					{
						point4 = point5;
						break;
					}
				}
				if (point4 == point2)
				{
					break;
				}
				item = new Point3(point2.X + point4.X, point2.Y + point4.Y, point2.Z + point4.Z);
				visitedSegments.Add(item);
				point3 = point2;
				point2 = point4;
			}
			Vector3 nodePosition = GetNodePosition(anchor, out var isOnFloor);
			Vector3 nodePosition2 = GetNodePosition(point2, out var isOnFloor2);
			m_ropeChains.Add(new RopeChain
			{
				Start = nodePosition,
				End = nodePosition2,
				Kind = kind,
				IsOnFloor = (isOnFloor & isOnFloor2),
				Color = StringBlock.GetColor(Terrain.ExtractData(m_subsystemTerrain.Terrain.GetCellValueFast(anchor.X, anchor.Y, anchor.Z)))
			});
		}
	}

	private Vector3 GetNodePosition(Point3 p, out bool isOnFloor)
	{
		Vector3 vector = new Vector3((float)p.X + 0.5f, (float)p.Y + 0.5f, (float)p.Z + 0.5f);
		int faceMask = StringBlock.GetFaceMask(Terrain.ExtractData(m_subsystemTerrain.Terrain.GetCellValueFast(p.X, p.Y, p.Z)));
		isOnFloor = false;
		if (faceMask != 0)
		{
			Vector3 zero = Vector3.Zero;
			for (int i = 0; i < 6; i++)
			{
				if ((faceMask & (1 << i)) != 0)
				{
					zero += CellFace.FaceToVector3(i) * 0.48f;
					if (i == 5)
					{
						isOnFloor = true;
					}
				}
			}
			return vector + zero;
		}
		return vector;
	}

	public override void Save(ValuesDictionary valuesDictionary)
	{
		base.Save(valuesDictionary);
		ValuesDictionary valuesDictionary2 = new ValuesDictionary();
		int num = 0;
		foreach (KeyValuePair<Point3, int> foodValue in m_foodValues)
		{
			ValuesDictionary valuesDictionary3 = new ValuesDictionary();
			valuesDictionary3.SetValue("Pos", foodValue.Key);
			valuesDictionary3.SetValue("Val", foodValue.Value);
			valuesDictionary3.SetValue("Prog", m_foodProgress[foodValue.Key]);
			valuesDictionary2.SetValue(num.ToString(), valuesDictionary3);
			num++;
		}
		valuesDictionary.SetValue("StringFoods", valuesDictionary2);
	}

	public override void OnChunkDiscarding(TerrainChunk chunk)
	{
		List<Point3> list = new List<Point3>();
		foreach (Point3 key in m_fatDripSystems.Keys)
		{
			if (key.X >= chunk.Origin.X && key.X < chunk.Origin.X + 16 && key.Z >= chunk.Origin.Y && key.Z < chunk.Origin.Y + 16)
			{
				list.Add(key);
			}
		}
		foreach (Point3 item in list)
		{
			RemoveFatDrips(item);
		}
	}

	private void RemoveFatDrips(Point3 p)
	{
		if (m_fatDripSystems.TryGetValue(p, out var value))
		{
			value.IsStopped = true;
			m_fatDripSystems.Remove(p);
		}
	}

	private void DropFood(Point3 p)
	{
		if (m_foodValues.TryGetValue(p, out var value))
		{
			m_subsystemPickables.AddPickable(value, 1, new Vector3((float)p.X + 0.5f, (float)p.Y + 0.5f, (float)p.Z + 0.5f), null, null);
			m_foodValues.Remove(p);
			m_foodProgress.Remove(p);
			RemoveFatDrips(p);
		}
	}

	public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner)
	{
		Point3 point = raycastResult.CellFace.Point;
		if (SubsystemBedrockBlockBehavior.CheckIsInTerritoriy(point.X, point.Z, out var territoriy) && !componentMiner.ComponentPlayer.PlayerData.ServerManager)
		{
			bool flag = componentMiner.ComponentPlayer.PlayerData.Friends.Contains(territoriy.OwnerGuid);
			if (territoriy.OwnerGuid != componentMiner.ComponentPlayer.PlayerData.PlayerGUID && (!flag || !territoriy.ApplyToFirend) && !territoriy.AllowBlockBehavior)
			{
				if (Time.PeriodicEvent(1.0, 0.0))
				{
					string text = LanguageControl.Get("ComponentMiner", "no_block_behavior_permission");
					componentMiner.ComponentPlayer.ComponentGui.DisplaySmallMessage(text, Color.Yellow, blinking: false, playNotificationSound: false);
				}
				return true;
			}
		}
		int activeBlockValue = componentMiner.ActiveBlockValue;
		int num = Terrain.ExtractContents(activeBlockValue);
		if (m_foodValues.TryGetValue(point, out var _))
		{
			DropFood(point);
			m_subsystemAudio.PlaySound("Audio/PickableCollected", 0.5f, 0f, new Vector3(point.X, point.Y, point.Z), 2f, autoDelay: true);
			return true;
		}
		if (num != 0 && num != 128 && num != 129)
		{
			m_foodValues[point] = activeBlockValue;
			m_foodProgress[point] = 0f;
			m_chainsNeedUpdate = true;
			componentMiner.RemoveActiveTool(1);
			m_subsystemAudio.PlaySound("Audio/UI/ButtonClick", 0.5f, 0f, new Vector3(point.X, point.Y, point.Z), 2f, autoDelay: true);
			return true;
		}
		return false;
	}

	private int GetProcessedFood(int currentValue, int state)
	{
		int num = Terrain.ExtractContents(currentValue);
		int data = Terrain.ExtractData(currentValue);
		switch (num)
		{
		case 161:
			return Terrain.MakeBlockValue(345, 0, BoiledBirdBlock.SetState(0, state));
		case 88:
			return Terrain.MakeBlockValue(346, 0, BoiledMeatBlock.SetState(0, state));
		case 77:
			return Terrain.MakeBlockValue(347, 0, BoiledFishBlock.SetState(0, state));
		case 442:
			if (CactusChunkBlock.GetCookState(data) == CactusChunkBlock.CookState.Boiled)
			{
				int data2 = CactusChunkBlock.SetCookState(data, CactusChunkBlock.CookState.BoiledAndDried);
				return Terrain.MakeBlockValue(442, 0, data2);
			}
			break;
		}
		if (state == 2)
		{
			switch (num)
			{
			case 333:
				return Terrain.MakeBlockValue(330);
			case 336:
				return Terrain.MakeBlockValue(331);
			case 207:
				if (FurBlock.GetState(data) == FurBlock.FurState.Oiled)
				{
					return Terrain.MakeBlockValue(207, 0, FurBlock.SetState(data, FurBlock.FurState.Tanned));
				}
				break;
			}
			if (num == 159 && LeatherBlock.GetState(data) == LeatherBlock.LeatherState.Oiled)
			{
				return Terrain.MakeBlockValue(159, 0, LeatherBlock.SetState(data, LeatherBlock.LeatherState.Tanned));
			}
		}
		return 0;
	}

	private void DrawThickLine(FlatBatch3D batch, Vector3 p1, Vector3 p2, Vector3 viewDir, Color color, float thickness)
	{
		Vector3 v = p2 - p1;
		if (v.LengthSquared() > 1E-05f)
		{
			Vector3 vector = Vector3.Normalize(Vector3.Cross(viewDir, v)) * (thickness * 0.5f);
			batch.QueueQuad(p1 - vector, p1 + vector, p2 + vector, p2 - vector, color);
		}
	}

	private void RecalculateAttachmentPoints()
	{
		m_foodAttachmentPoints.Clear();
		foreach (Point3 key in m_foodValues.Keys)
		{
			int faceMask = StringBlock.GetFaceMask(Terrain.ExtractData(m_subsystemTerrain.Terrain.GetCellValueFast(key.X, key.Y, key.Z)));
			int num = 0;
			for (int i = 0; i < 6; i++)
			{
				if ((faceMask & (1 << i)) != 0)
				{
					num++;
				}
			}
			if (num > 2)
			{
				Vector3 nodePosition = GetNodePosition(key, out var isOnFloor);
				m_foodAttachmentPoints[key] = new AttachmentData
				{
					Start = nodePosition,
					End = nodePosition,
					T = 0f,
					IsOnFloor = isOnFloor
				};
				continue;
			}
			Vector3 vector = new Vector3((float)key.X + 0.5f, (float)key.Y + 0.5f, (float)key.Z + 0.5f);
			float num2 = float.MaxValue;
			bool flag = false;
			AttachmentData value = default;
			foreach (RopeChain ropeChain in m_ropeChains)
			{
				Vector3 start = ropeChain.Start;
				Vector3 end = ropeChain.End;
				Vector3 vector2 = end - start;
				float num3 = vector2.LengthSquared();
				if (!(num3 > 0.0001f))
				{
					continue;
				}
				float num4 = Vector3.Dot(vector - start, vector2) / num3;
				if (num4 >= -0.1f && num4 <= 1.1f)
				{
					Vector3 vector3 = start + vector2 * num4;
					float num5 = Vector3.DistanceSquared(new Vector3(vector.X, 0f, vector.Z), new Vector3(vector3.X, 0f, vector3.Z));
					float num6 = vector.Y - vector3.Y;
					float num7 = num5 + num6 * num6 * 0.01f;
					if (num7 < 0.5f && num7 < num2)
					{
						num2 = num7;
						value.Start = start;
						value.End = end;
						value.T = MathUtils.Clamp(num4, 0f, 1f);
						value.IsOnFloor = ropeChain.IsOnFloor;
						flag = true;
					}
				}
			}
			if (flag)
			{
				m_foodAttachmentPoints[key] = value;
			}
			else if (faceMask != 0)
			{
				Vector3 nodePosition2 = GetNodePosition(key, out var isOnFloor2);
				m_foodAttachmentPoints[key] = new AttachmentData
				{
					Start = nodePosition2,
					End = nodePosition2,
					T = 0f,
					IsOnFloor = isOnFloor2
				};
			}
		}
	}

	private Matrix GetItemMatrix(int value, float rotationRadians)
	{
		Matrix result = Matrix.CreateRotationY(rotationRadians);
		if (SubsystemPickables.IsSpecialItem(value))
		{
			result = Matrix.CreateScale(1.1f) * Matrix.CreateRotationX(MathUtils.DegToRad(90f)) * Matrix.CreateRotationZ(MathUtils.DegToRad(90f)) * Matrix.CreateRotationY(rotationRadians);
		}
		else if (SubsystemPickables.IsBigAss(value))
		{
			result = Matrix.CreateScale(3f) * Matrix.CreateRotationX(MathUtils.DegToRad(90f)) * Matrix.CreateRotationZ(MathUtils.DegToRad(90f)) * Matrix.CreateRotationY(rotationRadians);
		}
		else if (SubsystemPickables.IsHorizontalItem(value))
		{
			result = Matrix.CreateRotationX(MathUtils.DegToRad(90f)) * Matrix.CreateRotationZ(0f) * Matrix.CreateRotationY(rotationRadians);
		}
		return result;
	}

	public override void OnBlockModified(int value, int oldValue, int x, int y, int z)
	{
		m_chainsNeedUpdate = true;
		CheckSupport(x, y, z);
	}

	private float GetStableLight(Point3 cell, Vector3 position)
	{
		TerrainChunk chunkAtCell = m_subsystemTerrain.Terrain.GetChunkAtCell(cell.X, cell.Z);
		if (chunkAtCell != null && chunkAtCell.State == TerrainChunkState.Valid)
		{
			int cellLight = m_subsystemTerrain.Terrain.GetCellLight(cell.X, cell.Y, cell.Z);
			int x = m_lightSmoother.CalculateMinersLight(position, Project);
			float num = MathUtils.Saturate((float)MathUtils.Max(cellLight, x) / 15f);
			m_lightCache[cell] = num;
			return num;
		}
		float value = 0f;
		m_lightCache.TryGetValue(cell, out value);
		return value;
	}

	private void DrawStringSegment(FlatBatch3D batch, Vector3 start, Vector3 end, StringBlock.StringKind fallbackKind, int? ropeColor, float light, Vector3 wind, int seed, bool isOnFloor, bool isRough, bool isIsolated, Camera camera)
	{
		Vector3 viewDirection = camera.ViewDirection;
		float num = 0.015f;
		if (isIsolated & isOnFloor)
		{
			int num2 = 12;
			Vector3 p = start;
			m_random.Seed(seed);
			int num3 = (int)Math.Floor(start.X);
			int num4 = (int)Math.Floor(start.Y);
			int num5 = (int)Math.Floor(start.Z);
			float noise = (float)(MathUtils.Abs((num3 * 73856093) ^ (num4 * 19349663) ^ (num5 * 83492791)) % 1000) / 1000f;
			Color color = GetStringColor(fallbackKind, noise) * light;
			if (ropeColor.HasValue)
			{
				color *= SubsystemPalette.GetColor(m_subsystemTerrain, ropeColor);
			}
			for (int i = 1; i <= num2; i++)
			{
				float x = (float)i * 1.5f;
				float num6 = 0.12f * (1f - (float)i / (float)num2);
				Vector3 vector = start + new Vector3(MathUtils.Cos(x) * num6, 0.02f + m_random.Float(0f, 0.02f), MathUtils.Sin(x) * num6);
				DrawThickLine(batch, p, vector, viewDirection, color, num * 0.8f);
				p = vector;
			}
			return;
		}
		float num7 = Vector3.Distance(start, end);
		int num8 = (int)MathUtils.Clamp(num7 * 4f, 8f, 32f);
		Vector3 vector2;
		if (isOnFloor)
		{
			vector2 = (start + end) * 0.5f;
		}
		else
		{
			float num9 = ((MathUtils.Abs(start.Y - end.Y) > num7 * 0.8f) ? 0.02f : (num7 * 0.1f));
			vector2 = (start + end) * 0.5f + new Vector3(0f, 0f - num9, 0f) + wind;
		}
		Vector3 vector3 = start;
		m_random.Seed(seed);
		for (int j = 1; j <= num8; j++)
		{
			float num10 = (float)j / (float)num8;
			Vector3 vector4 = (1f - num10) * (1f - num10) * start + 2f * (1f - num10) * num10 * vector2 + num10 * num10 * end;
			Vector3 vector5 = (vector3 + vector4) * 0.5f;
			int num11 = (int)Math.Floor(vector5.X);
			int num12 = (int)Math.Floor(vector5.Y);
			int num13 = (int)Math.Floor(vector5.Z);
			Vector3 vector6 = Vector3.Lerp(start, end, num10);
			int cellValueFast = m_subsystemTerrain.Terrain.GetCellValueFast((int)Math.Floor(vector6.X), (int)Math.Floor(vector6.Y), (int)Math.Floor(vector6.Z));
			int? index = ropeColor;
			StringBlock.StringKind stringKind = fallbackKind;
			if (Terrain.ExtractContents(cellValueFast) == 198)
			{
				int data = Terrain.ExtractData(cellValueFast);
				index = StringBlock.GetColor(data);
				stringKind = StringBlock.GetKind(data);
			}
			float noise2 = (float)(MathUtils.Abs((num11 * 73856093) ^ (num12 * 19349663) ^ (num13 * 83492791)) % 1000) / 1000f;
			Color color2 = GetStringColor(stringKind, noise2) * light;
			if (index.HasValue)
			{
				color2 *= SubsystemPalette.GetColor(m_subsystemTerrain, index);
			}
			float num14 = num;
			if (stringKind == StringBlock.StringKind.Bark || stringKind == StringBlock.StringKind.BoiledBark)
			{
				num14 *= 2.5f;
				if (j < num8)
				{
					vector4 += new Vector3(m_random.Float(-0.06f, 0.06f), m_random.Float(-0.06f, 0.06f), m_random.Float(-0.06f, 0.06f));
				}
			}
			else if (isRough && j < num8 && (!isIsolated | isOnFloor))
			{
				vector4 += new Vector3(m_random.Float(-0.1f, 0.1f), m_random.Float(-0.03f, 0.03f), m_random.Float(-0.1f, 0.1f));
			}
			DrawThickLine(batch, vector3, vector4, viewDirection, color2, num14);
			if (stringKind == StringBlock.StringKind.Tendon && j > 1 && j < num8 && m_random.Float(0f, 1f) > 0.25f)
			{
				Vector3 v = new Vector3(m_random.Float(-1f, 1f), m_random.Float(-1f, 0.2f), m_random.Float(-1f, 1f));
				v = Vector3.Normalize(v) * m_random.Float(0.08f, 0.25f);
				DrawThickLine(batch, vector4, vector4 + v, viewDirection, color2 * 0.8f, num14 * 0.8f);
			}
			vector3 = vector4;
		}
	}
}
