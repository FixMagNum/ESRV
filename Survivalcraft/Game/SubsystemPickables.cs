using System;
using System.Collections.Generic;
using System.Linq;
using Engine;
using Engine.Graphics;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemPickables : Subsystem, IDrawable, IUpdateable
{
	public SubsystemAudio m_subsystemAudio;

	public SubsystemPlayers m_subsystemPlayers;

	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemSky m_subsystemSky;

	public SubsystemTime m_subsystemTime;

	public SubsystemGameInfo m_subsystemGameInfo;

	public SubsystemParticles m_subsystemParticles;

	public SubsystemExplosions m_subsystemExplosions;

	public SubsystemBlockBehaviors m_subsystemBlockBehaviors;

	public SubsystemFireBlockBehavior m_subsystemFireBlockBehavior;

	public SubsystemFluidBlockBehavior m_subsystemFluidBlockBehavior;

	public List<ComponentPlayer> m_tmpPlayers;

	public List<Pickable> m_pickables;

	public List<Pickable> m_pickablesToRemove;

	public PrimitivesRenderer3D m_primitivesRenderer;

	public Random m_random;

	public DrawBlockEnvironmentData m_drawBlockEnvironmentData;

	public static int[] m_drawOrders;

	public List<ushort> useMaps;

	private static readonly int[] specialItemIndices;

	private static readonly int[] lightItemIndices;

	private static readonly int[] horizontalItemIndices;

	public SubsystemWeather m_subsystemWeather;

	private Dictionary<Pickable, double> m_cookingTimers = new Dictionary<Pickable, double>();

	private float m_smokeTimer;

	private static readonly int[] bigass;

	private Dictionary<int, int> rawToCookedFoodMap;

	public SubsystemFocalorsFluidBlockBehavior m_subsystemFocalorsFluidBlockBehavior;

	public SubsystemMiningLightBehavior m_subsystemMiningLightBehavior;

	private HashSet<Pickable> m_hotPickables;

	public ReadOnlyList<Pickable> Pickables => new ReadOnlyList<Pickable>(m_pickables);

	public int[] DrawOrders => m_drawOrders;

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public event Action<Pickable> PickableAdded;

	public event Action<Pickable> PickableRemoved;

	public Pickable AddPickable(int value, int count, Vector3 position, Vector3? velocity, Matrix? stuckMatrix)
	{
		if (CommonLib.WorkType == WorkType.Client)
		{
			return null;
		}
		if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative && !velocity.HasValue && !stuckMatrix.HasValue)
		{
			int num = Terrain.ExtractContents(value);
			int num2 = Terrain.ExtractData(value);
			if (num != 366 && num2 == 0)
			{
				return null;
			}
		}
		Pickable pickable = CreatePickable(null, value, count, position, velocity, stuckMatrix);
		pickable.Net_ToRemove = true;
		CommonLib.Net.QueuePackage(new PickablePackage(pickable, PickablePackage.PickType.Create));
		return pickable;
	}

	public Pickable CreatePickable(ushort? id, int value, int count, Vector3 position, Vector3? velocity, Matrix? stuckMatrix)
	{
		Pickable pickable = new Pickable();
		pickable.Id = (id.HasValue ? id.Value : FindAvailableId());
		pickable.Value = value;
		pickable.Count = count;
		pickable.Position = position;
		pickable.StuckMatrix = stuckMatrix;
		pickable.CreationTime = m_subsystemGameInfo.TotalElapsedGameTime;
		if (m_random.Float(0f, 1f) > 0.2f)
		{
			pickable.Rotation = m_random.Vector3(0f, 6.28f);
		}
		else
		{
			pickable.Rotation = new Vector3(0f, m_random.Float(0f, 6.28f), 0f);
		}
		pickable.IsSpecialPickable = false;
		if (velocity.HasValue)
		{
			pickable.Velocity = velocity.Value;
		}
		else if (Terrain.ExtractContents(value) == 248)
		{
			Vector2 vector = m_random.Vector2(1.5f, 2f);
			pickable.Velocity = new Vector3(vector.X, 3f, vector.Y);
		}
		else
		{
			pickable.Velocity = new Vector3(m_random.Float(-0.5f, 0.5f), m_random.Float(1f, 1.2f), m_random.Float(-0.5f, 0.5f));
		}
		PickableAdded?.Invoke(pickable);
		m_pickables.Add(pickable);
		return pickable;
	}

	public ushort FindAvailableId()
	{
		for (ushort index = 1; index < ushort.MaxValue; index++)
		{
			if (!useMaps.Contains(index))
			{
				useMaps.Add(index);
				return index;
			}
		}
		return 0;
	}

	public void Draw(Camera camera, int drawOrder)
	{
		double totalElapsedGameTime = m_subsystemGameInfo.TotalElapsedGameTime;
		m_drawBlockEnvironmentData.SubsystemTerrain = m_subsystemTerrain;
		float num = MathUtils.Min(m_subsystemSky.VisibilityRange, 30f);
		int num2 = 0;
		foreach (Pickable pickable in m_pickables)
		{
			Vector3 vector = pickable.Position + new Vector3(0f, 0.2f, 0f);
			if (IsSpecialItem(pickable.Value))
			{
				vector.Y -= 0.12f;
			}
			if (IsBigAss(pickable.Value))
			{
				vector.Y -= 0.1f;
			}
			float num3 = Vector3.Dot(camera.ViewDirection, vector - camera.ViewPosition);
			int num4 = Terrain.ExtractContents(pickable.Value);
			int num5 = Terrain.ExtractData(pickable.Value);
			bool flag = num4 == 379 && (num5 & 1) == 1;
			if ((!(num3 > -0.5f) || !(num3 < num)) && (!flag || !(num3 < 60f)))
			{
				continue;
			}
			if (num3 > 0.5f)
			{
				pickable.LastLookedAtTime = totalElapsedGameTime;
			}
			int num6 = Terrain.ExtractContents(pickable.Value);
			Block block = BlocksManager.Blocks[num6];
			int x = Terrain.ToCell(vector.X);
			int num7 = Terrain.ToCell(vector.Y);
			int z = Terrain.ToCell(vector.Z);
			TerrainChunk chunkAtCell = m_subsystemTerrain.Terrain.GetChunkAtCell(x, z);
			if (chunkAtCell != null && chunkAtCell.State >= TerrainChunkState.InvalidVertices1 && num7 >= 0 && num7 < 255)
			{
				m_drawBlockEnvironmentData.Humidity = m_subsystemTerrain.Terrain.GetSeasonalHumidity(x, z);
				m_drawBlockEnvironmentData.Temperature = m_subsystemTerrain.Terrain.GetSeasonalTemperature(x, z) + SubsystemWeather.GetTemperatureAdjustmentAtHeight(num7);
				float f = MathUtils.Max(vector.Y - (float)num7 - 0.75f, 0f) / 0.25f;
				int x2 = (int)MathUtils.Lerp(m_subsystemTerrain.Terrain.GetCellLightFast(x, num7, z), m_subsystemTerrain.Terrain.GetCellLightFast(x, num7 + 1, z), f);
				int currentMax = 0;
				if (m_subsystemMiningLightBehavior != null)
				{
					CalculateMinersLightAtPosition(vector, m_subsystemMiningLightBehavior.ActiveLights, ref currentMax);
					CalculateMinersLightAtPosition(vector, m_subsystemMiningLightBehavior.ActiveHeadLights, ref currentMax);
				}
				pickable.Light = MathUtils.Max(x2, currentMax);
			}
			if (IsLightItem(pickable.Value))
			{
				m_drawBlockEnvironmentData.Light = 12;
			}
			else
			{
				m_drawBlockEnvironmentData.Light = pickable.Light;
			}
			m_drawBlockEnvironmentData.BillboardDirection = pickable.Position - camera.ViewPosition;
			Matrix matrix = Matrix.CreateTranslation(vector);
			Matrix matrix2 = Matrix.CreateFromYawPitchRoll(pickable.Rotation.Y, pickable.Rotation.X, pickable.Rotation.Z);
			if (IsSpecialItem(pickable.Value))
			{
				matrix = Matrix.CreateScale(1.1f) * Matrix.CreateRotationX(MathUtils.DegToRad(90f)) * Matrix.CreateRotationZ(MathUtils.DegToRad(90f)) * matrix2 * Matrix.CreateTranslation(vector);
			}
			else if (IsBigAss(pickable.Value))
			{
				matrix = Matrix.CreateScale(3f) * Matrix.CreateRotationX(MathUtils.DegToRad(90f)) * Matrix.CreateRotationZ(MathUtils.DegToRad(90f)) * matrix2 * Matrix.CreateTranslation(vector);
			}
			else if (IsHorizontalItem(pickable.Value))
			{
				matrix = Matrix.CreateRotationX(MathUtils.DegToRad(90f)) * matrix2 * Matrix.CreateTranslation(new Vector3(vector.X, vector.Y - 0.1f, vector.Z));
			}
			else
			{
				if (pickable.StuckMatrix.HasValue)
				{
					Matrix matrix3 = pickable.StuckMatrix.Value;
					block.DrawBlock(m_primitivesRenderer, pickable.Value, Color.White, 0.39f, ref matrix3, m_drawBlockEnvironmentData);
					continue;
				}
				matrix = matrix2 * Matrix.CreateTranslation(vector);
			}
			m_drawBlockEnvironmentData.InWorldMatrix = matrix;
			block.DrawBlock(m_primitivesRenderer, pickable.Value, Color.White, 0.39f, ref matrix, m_drawBlockEnvironmentData);
			num2++;
			if (num2 >= 40)
			{
				m_primitivesRenderer.Flush(camera.ViewProjectionMatrix);
				num2 = 0;
			}
		}
		m_primitivesRenderer.Flush(camera.ViewProjectionMatrix);
	}

	public void OnPlayerGetPickable(Pickable pickable, ComponentPlayer tmpPlayer, Vector3 PositionFix, float distance)
	{
		if (pickable.IsSpecialPickable)
		{
			return;
		}
		bool flag = Terrain.ExtractContents(pickable.Value) == 248;
		IInventory inventory = tmpPlayer.ComponentMiner.Inventory;
		if (!flag && ComponentInventoryBase.FindAcquireSlotForItem(inventory, pickable.Value) < 0)
		{
			return;
		}
		if (distance < 1f)
		{
			if (flag)
			{
				tmpPlayer.ComponentLevel.AddExperience(pickable.Count, playSound: true);
				pickable.ToRemove = true;
				return;
			}
			pickable.Count = ComponentInventoryBase.AcquireItems(inventory, pickable.Value, pickable.Count);
			if (pickable.Count == 0 || CommonLib.WorkType == WorkType.Client)
			{
				pickable.ToRemove = true;
				pickable.PlaySound = true;
				PlayPickableCollectedSound(pickable);
			}
		}
		else if (!pickable.StuckMatrix.HasValue)
		{
			pickable.FlyToPosition = PositionFix + 0.1f * MathUtils.Sqrt(distance) * tmpPlayer.ComponentBody.Velocity;
			CommonLib.Net.QueuePackage(new PickablePackage(pickable, PickablePackage.PickType.SetFlyToPosition));
		}
	}

	public void PlayPickableCollectedSound(Pickable pickable)
	{
		m_subsystemAudio.PlaySound("Audio/PickableCollected", 0.7f, -0.4f, pickable.Position, 2f, autoDelay: false);
	}

	public bool PickableAction(ushort id, Action<Pickable> action, bool requestSync = true)
	{
		bool slotValue = false;
		for (int data = 0; data < m_pickables.Count; data++)
		{
			if (m_pickables[data].Id != id)
			{
				continue;
			}
			if (action != null)
			{
				bool isSpecialPickable = m_pickables[data].IsSpecialPickable;
				action(m_pickables[data]);
				if (isSpecialPickable && !m_pickables[data].IsSpecialPickable)
				{
					m_pickables[data].CreationTime = m_subsystemGameInfo.TotalElapsedGameTime;
				}
			}
			slotValue = true;
			break;
		}
		if ((!slotValue && CommonLib.WorkType == WorkType.Client) & requestSync)
		{
			CommonLib.Net.QueuePackage(new PickablePackage(new Pickable
			{
				Id = id
			}, PickablePackage.PickType.RequestSync));
		}
		return slotValue;
	}

	public void Update(float dt)
	{
		double totalElapsedGameTime = m_subsystemGameInfo.TotalElapsedGameTime;
		float currentWindStrength = m_subsystemWeather.CurrentWindStrength;
		Vector2 currentWindVector = m_subsystemWeather.CurrentWindVector;
		m_tmpPlayers.Clear();
		foreach (ComponentPlayer componentPlayer2 in m_subsystemPlayers.ComponentPlayers)
		{
			if (componentPlayer2.ComponentHealth.Health > 0f)
			{
				m_tmpPlayers.Add(componentPlayer2);
			}
		}
		List<ComponentPlayer> list = m_tmpPlayers.Where((ComponentPlayer p) => p.PlayerData.IsCheater).ToList();
		float num = MathUtils.Pow(0.5f, dt);
		float num2 = MathUtils.Pow(0.001f, dt);
		foreach (Pickable pickable16 in m_pickables)
		{
			if (pickable16.ToRemove)
			{
				m_pickablesToRemove.Add(pickable16);
				continue;
			}
			int num3 = m_pickables.Count - m_pickablesToRemove.Count;
			int num4 = Terrain.ExtractContents(pickable16.Value);
			float num5 = ((num4 == 362 || num4 == 363 || num4 == 318 || num4 == 19 || num4 == 197) ? 300f : MathUtils.Lerp(5400f, 5400f, MathUtils.Saturate((float)num3 / 60f)));
			if ((pickable16.IsSpecialPickable ? 0.0 : (totalElapsedGameTime - pickable16.CreationTime)) > (double)num5)
			{
				pickable16.ToRemove = true;
				m_pickablesToRemove.Add(pickable16);
				continue;
			}
			TerrainChunk chunkAtCell = m_subsystemTerrain.Terrain.GetChunkAtCell(Terrain.ToCell(pickable16.Position.X), Terrain.ToCell(pickable16.Position.Z));
			if (chunkAtCell == null || chunkAtCell.State <= TerrainChunkState.InvalidContents4)
			{
				continue;
			}
			int num6 = Terrain.ExtractContents(pickable16.Value);
			if (rawToCookedFoodMap.ContainsKey(num6))
			{
				int num7 = Terrain.ToCell(pickable16.Position.X);
				int num8 = Terrain.ToCell(pickable16.Position.Y);
				int num9 = Terrain.ToCell(pickable16.Position.Z);
				int seasonalTemperature = m_subsystemTerrain.Terrain.GetSeasonalTemperature(num7, num9);
				int seasonalHumidity = m_subsystemTerrain.Terrain.GetSeasonalHumidity(num7, num9);
				bool flag = seasonalTemperature > 8 && seasonalHumidity < 6;
				bool flag2 = false;
				if (m_subsystemTime.PeriodicGameTimeEvent(1.0, (double)(pickable16.GetHashCode() % 100) / 100.0))
				{
					for (int num10 = -1; num10 <= 1; num10++)
					{
						for (int num11 = -1; num11 <= 1; num11++)
						{
							for (int num12 = -1; num12 <= 1; num12++)
							{
								int cellContents = m_subsystemTerrain.Terrain.GetCellContents(num7 + num10, num8 + num11, num9 + num12);
								if (cellContents == 92 || ((cellContents == 65 || cellContents == 304) && num11 == -1))
								{
									flag2 = true;
									break;
								}
							}
							if (flag2)
							{
								break;
							}
						}
						if (flag2)
						{
							break;
						}
					}
					if (flag2)
					{
						m_hotPickables.Add(pickable16);
					}
					else
					{
						m_hotPickables.Remove(pickable16);
					}
				}
				else
				{
					flag2 = m_hotPickables.Contains(pickable16);
				}
				if (flag | flag2)
				{
					if (!m_cookingTimers.ContainsKey(pickable16))
					{
						m_cookingTimers[pickable16] = totalElapsedGameTime;
					}
					else if (totalElapsedGameTime - m_cookingTimers[pickable16] >= 240.0)
					{
						int value = Terrain.MakeBlockValue(rawToCookedFoodMap[num6]);
						pickable16.Value = value;
						pickable16.ToRemove = false;
						m_cookingTimers.Remove(pickable16);
						pickable16.CreationTime = totalElapsedGameTime - 30.0;
						int num13 = Terrain.ExtractContents(pickable16.Value);
						Block block = BlocksManager.Blocks[num13];
						if (block.GetRotPeriod(pickable16.Value) > 0)
						{
							int damage = block.GetDamage(pickable16.Value);
							damage++;
							pickable16.Value = block.SetDamage(pickable16.Value, damage);
						}
					}
					m_smokeTimer += dt;
					if (m_smokeTimer >= 0.7f)
					{
						Vector3 position = pickable16.Position;
						Vector3 direction = new Vector3(0f, 0.02f, 0f);
						GunSmokeParticleSystem gunSmokeParticleSystem = new GunSmokeParticleSystem(m_subsystemTerrain, position, direction)
						{
							ParticleSize = 0.2f,
							SpawnRateMultiplier = 0.06f
						};
						gunSmokeParticleSystem.m_direction = direction;
						m_subsystemParticles.AddParticleSystem(gunSmokeParticleSystem);
						m_smokeTimer = 0f;
					}
				}
			}
			if (pickable16.ToRemove)
			{
				m_pickablesToRemove.Add(pickable16);
			}
			else if (!pickable16.StuckMatrix.HasValue && !pickable16.IsSpecialPickable && (!pickable16.IsDeletePickable || pickable16.IsPersistent))
			{
				Vector3 vector = new Vector3(currentWindVector.X, 0f, currentWindVector.Y);
				float windExposureFactor = m_subsystemWeather.GetWindExposureFactor(pickable16.Position);
				if (windExposureFactor > 0f)
				{
					Vector3 vector2 = vector * currentWindStrength * windExposureFactor * dt;
					if (currentWindStrength >= 10f && windExposureFactor > 0.1f)
					{
						float num14;
						if (currentWindStrength >= 16f)
						{
							num14 = m_random.Float(0.1f, 0.2f);
						}
						else
						{
							num14 = ((!(currentWindStrength >= 13f)) ? m_random.Float(0.01f, 0.05f) : m_random.Float(0.05f, 0.1f));
						}
						if (m_random.Float(0f, 1f) < 0.3f)
						{
							vector2.Y += num14;
						}
					}
					pickable16.Velocity += vector2;
				}
			}
			else if (pickable16.IsStuckProjectile)
			{
				pickable16.Position = pickable16.StuckMatrix.Value.Translation;
				pickable16.Velocity = Vector3.Zero;
			}
			else if (pickable16.IsSpecialPickable)
			{
				pickable16.Velocity = Vector3.Zero;
			}
			Block block2 = BlocksManager.Blocks[Terrain.ExtractContents(pickable16.Value)];
			int num15 = m_pickables.Count - m_pickablesToRemove.Count;
			float num16 = ((num6 == 362 || num6 == 363 || num6 == 318 || num6 == 19 || num6 == 197) ? 300f : MathUtils.Lerp(5400f, 5400f, MathUtils.Saturate((float)num15 / 60f)));
			double num17 = totalElapsedGameTime - pickable16.CreationTime;
			if (pickable16.IsSpecialPickable)
			{
				num17 = 0.0;
			}
			if (currentWindStrength > 10f && !pickable16.IsSpecialPickable)
			{
				float windExposureFactor2 = m_subsystemWeather.GetWindExposureFactor(pickable16.Position);
				if (windExposureFactor2 > 0f)
				{
					Vector3 vector3 = new Vector3(currentWindVector.X, 0f, currentWindVector.Y);
					pickable16.Velocity += vector3 * windExposureFactor2 * dt;
					if (windExposureFactor2 >= 0.5f && m_random.Float(0f, 1f) < 0.1f)
					{
						Pickable pickable = pickable16;
						pickable.Velocity.Y = pickable.Velocity.Y + m_random.Float(0.1f, 0.3f);
					}
				}
			}
			if (num17 > (double)num16)
			{
				pickable16.ToRemove = true;
				continue;
			}
			TerrainChunk chunkAtCell2 = m_subsystemTerrain.Terrain.GetChunkAtCell(Terrain.ToCell(pickable16.Position.X), Terrain.ToCell(pickable16.Position.Z));
			if (chunkAtCell2 == null || chunkAtCell2.State <= TerrainChunkState.InvalidContents4)
			{
				continue;
			}
			if (list.Count > 0 && !pickable16.StuckMatrix.HasValue && !pickable16.IsSpecialPickable)
			{
				foreach (ComponentPlayer item in list)
				{
					Vector3 v = pickable16.Position - item.ComponentBody.Position;
					if (v.LengthSquared() < 64f)
					{
						pickable16.FlyToPosition = null;
						if (pickable16.Velocity.LengthSquared() < 0.5f)
						{
							Vector3 v2 = Vector3.Normalize(v);
							v2 += new Vector3(0f, 0.8f, 0f);
							v2 = Vector3.Normalize(v2);
							float num18 = 7f;
							pickable16.Velocity = v2 * num18;
							m_subsystemParticles.AddParticleSystem(new GunSmokeParticleSystem(m_subsystemTerrain, pickable16.Position, Vector3.UnitY * 0.2f)
							{
								ParticleSize = 0.2f,
								SpawnRateMultiplier = 0.06f
							});
						}
					}
				}
			}
			Vector3 position2 = pickable16.Position;
			Vector3 vector4 = position2 + pickable16.Velocity * dt;
			if (!pickable16.FlyToPosition.HasValue && num17 > 0.5)
			{
				foreach (ComponentPlayer tmpPlayer in m_tmpPlayers)
				{
					Vector3 vector5 = tmpPlayer.ComponentBody.Position + new Vector3(0f, 0.75f, 0f);
					float num19 = (vector5 - pickable16.Position).LengthSquared();
					if (num19 < 3.0625f)
					{
						pickable16.GetPickPlayer = tmpPlayer.PlayerData.ClientId;
						pickable16.PostionFix = vector5;
						pickable16.distance = num19;
						if (CommonLib.WorkType != WorkType.Client)
						{
							OnPlayerGetPickable(pickable16, tmpPlayer, vector5, num19);
						}
					}
				}
			}
			if (pickable16.FlyToPosition.HasValue)
			{
				Vector3 vector6 = pickable16.FlyToPosition.Value - pickable16.Position;
				float num20 = vector6.LengthSquared();
				if (num20 >= 0.25f)
				{
					pickable16.Velocity = 6f * vector6 / MathUtils.Sqrt(num20);
				}
				else
				{
					pickable16.FlyToPosition = null;
				}
			}
			else
			{
				Block block3 = null;
				float? surfaceHeight = null;
				Vector2? vector7 = null;
				int x = Terrain.ToCell(pickable16.Position.X);
				int y = Terrain.ToCell(pickable16.Position.Y + 0.1f);
				int z = Terrain.ToCell(pickable16.Position.Z);
				int num21 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellContents(x, y, z));
				if (BlocksManager.Blocks[num21] is FocalorsFluidBlock || num21 == 92)
				{
					vector7 = m_subsystemFocalorsFluidBlockBehavior.CalculateFlowSpeed(x, y, z, out var surfaceBlock, out surfaceHeight);
					block3 = surfaceBlock ?? BlocksManager.Blocks[num21];
				}
				else
				{
					vector7 = m_subsystemFluidBlockBehavior.CalculateFlowSpeed(x, y, z, out var surfaceBlock2, out surfaceHeight);
					block3 = surfaceBlock2;
				}
				float num22 = 10f;
				if (block3 is MagmaBlock || block3 is FocalorsMagmaBlock)
				{
					num22 = 0.5f;
				}
				else if (m_subsystemSky != null && m_subsystemSky.IsSolarEclipse)
				{
					num22 = 5f;
				}
				if (!pickable16.StuckMatrix.HasValue)
				{
					TerrainRaycastResult? terrainRaycastResult = m_subsystemTerrain.Raycast(position2, vector4, useInteractionBoxes: false, skipAirBlocks: true, (int value2, float distance) => BlocksManager.Blocks[Terrain.ExtractContents(value2)].IsCollidable);
					if (terrainRaycastResult.HasValue)
					{
						int contents = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(terrainRaycastResult.Value.CellFace.X, terrainRaycastResult.Value.CellFace.Y, terrainRaycastResult.Value.CellFace.Z));
						SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(contents);
						for (int num23 = 0; num23 < blockBehaviors.Length; num23++)
						{
							blockBehaviors[num23].OnHitByProjectile(terrainRaycastResult.Value.CellFace, pickable16);
						}
						if (m_subsystemTerrain.Raycast(position2, position2, useInteractionBoxes: false, skipAirBlocks: true, (int value2, float distance) => BlocksManager.Blocks[Terrain.ExtractContents(value2)].IsCollidable).HasValue)
						{
							int num24 = Terrain.ToCell(position2.X);
							int num25 = Terrain.ToCell(position2.Y);
							int num26 = Terrain.ToCell(position2.Z);
							int num27 = 0;
							int num28 = 0;
							int num29 = 0;
							int? num30 = null;
							for (int num31 = -3; num31 <= 3; num31++)
							{
								for (int num32 = -3; num32 <= 3; num32++)
								{
									for (int num33 = -3; num33 <= 3; num33++)
									{
										if (!BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num31 + num24, num32 + num25, num33 + num26)].IsCollidable)
										{
											int num34 = num31 * num31 + num32 * num32 + num33 * num33;
											if (!num30.HasValue || num34 < num30.Value)
											{
												num27 = num31 + num24;
												num28 = num32 + num25;
												num29 = num33 + num26;
												num30 = num34;
											}
										}
									}
								}
							}
							if (num30.HasValue)
							{
								pickable16.FlyToPosition = new Vector3(num27, num28, num29) + new Vector3(0.5f);
							}
							else
							{
								pickable16.ToRemove = true;
							}
						}
						else
						{
							Plane plane = terrainRaycastResult.Value.CellFace.CalculatePlane();
							bool flag3 = vector7.HasValue && vector7.Value != Vector2.Zero;
							if (plane.Normal.X != 0f)
							{
								float num35 = ((flag3 || MathUtils.Sqrt(MathUtils.Sqr(pickable16.Velocity.Y) + MathUtils.Sqr(pickable16.Velocity.Z)) > 10f) ? 0.95f : 0.25f);
								pickable16.Velocity *= new Vector3(0f - num35, num35, num35);
							}
							if (plane.Normal.Y != 0f)
							{
								float num36 = ((flag3 || MathUtils.Sqrt(MathUtils.Sqr(pickable16.Velocity.X) + MathUtils.Sqr(pickable16.Velocity.Z)) > 10f) ? 0.95f : 0.25f);
								pickable16.Velocity *= new Vector3(num36, 0f - num36, num36);
								if (flag3)
								{
									Pickable pickable2 = pickable16;
									pickable2.Velocity.Y = pickable2.Velocity.Y + 0.1f * plane.Normal.Y;
								}
							}
							if (plane.Normal.Z != 0f)
							{
								float num37 = ((flag3 || MathUtils.Sqrt(MathUtils.Sqr(pickable16.Velocity.X) + MathUtils.Sqr(pickable16.Velocity.Y)) > 10f) ? 0.95f : 0.25f);
								pickable16.Velocity *= new Vector3(num37, num37, 0f - num37);
							}
							vector4 = position2;
						}
					}
				}
				else
				{
					Vector3 vector8 = pickable16.StuckMatrix.Value.Translation + pickable16.StuckMatrix.Value.Up * block2.ProjectileTipOffset;
					if (!m_subsystemTerrain.Raycast(vector8, vector8, useInteractionBoxes: false, skipAirBlocks: true, (int value2, float distance) => BlocksManager.Blocks[Terrain.ExtractContents(value2)].IsCollidable).HasValue)
					{
						pickable16.Position = pickable16.StuckMatrix.Value.Translation;
						pickable16.Velocity = Vector3.Zero;
						pickable16.StuckMatrix = null;
					}
				}
				if ((block3 is WaterBlock || block3 is FocalorsWaterBlock) && !pickable16.SplashGenerated && pickable16.Velocity.LengthSquared() > 2f)
				{
					m_subsystemParticles.AddParticleSystem(new WaterSplashParticleSystem(m_subsystemTerrain, pickable16.Position, large: false));
					m_subsystemAudio.PlayRandomSound("Audio/Splashes", 1f, m_random.Float(-0.2f, 0.2f), pickable16.Position, 6f, autoDelay: true);
					pickable16.SplashGenerated = true;
				}
				else if ((block3 is MagmaBlock || block3 is FocalorsMagmaBlock) && pickable16.Value != 344 && pickable16.Value != 367)
				{
					if (!pickable16.LavaContactTime.HasValue)
					{
						pickable16.LavaContactTime = totalElapsedGameTime;
						m_subsystemParticles.AddParticleSystem(new MagmaSplashParticleSystem(m_subsystemTerrain, pickable16.Position, large: false));
						m_subsystemAudio.PlayRandomSound("Audio/Sizzles", 1f, m_random.Float(-0.2f, 0.2f), pickable16.Position, 3f, autoDelay: true);
						pickable16.SplashGenerated = true;
					}
					else if (totalElapsedGameTime - pickable16.LavaContactTime >= 1.0)
					{
						if (Terrain.ExtractContents(pickable16.Value) == 380)
						{
							ComponentPlayer componentPlayer = null;
							float num38 = float.MaxValue;
							foreach (ComponentPlayer tmpPlayer2 in m_tmpPlayers)
							{
								float num39 = Vector3.DistanceSquared(tmpPlayer2.ComponentBody.Position, pickable16.Position);
								if (num39 < num38 && num39 < 256f)
								{
									num38 = num39;
									componentPlayer = tmpPlayer2;
								}
							}
							if (componentPlayer != null && componentPlayer.PlayerAchievements.ForTheSafetyOfOurSanity == 0L)
							{
								componentPlayer.PlayerAchievements.ForTheSafetyOfOurSanity = 1L;
								componentPlayer.PlayerAchievements.EnqueueForTheSafetyOfOurSanityAchievement();
							}
						}
						pickable16.ToRemove = true;
						m_subsystemParticles.AddParticleSystem(new MagmaSplashParticleSystem(m_subsystemTerrain, pickable16.Position, large: false));
						m_subsystemExplosions.TryExplodeBlock(Terrain.ToCell(pickable16.Position.X), Terrain.ToCell(pickable16.Position.Y), Terrain.ToCell(pickable16.Position.Z), pickable16.Value);
					}
					else if (totalElapsedGameTime - pickable16.LavaContactTime >= 1.0)
					{
						pickable16.ToRemove = true;
						m_subsystemParticles.AddParticleSystem(new MagmaSplashParticleSystem(m_subsystemTerrain, pickable16.Position, large: false));
						m_subsystemExplosions.TryExplodeBlock(Terrain.ToCell(pickable16.Position.X), Terrain.ToCell(pickable16.Position.Y), Terrain.ToCell(pickable16.Position.Z), pickable16.Value);
					}
				}
				else
				{
					pickable16.LavaContactTime = null;
					if (block3 == null)
					{
						if (m_subsystemTime.PeriodicGameTimeEvent(1.0, (double)(pickable16.GetHashCode() % 100) / 100.0) && (m_subsystemTerrain.Terrain.GetCellContents(Terrain.ToCell(pickable16.Position.X), Terrain.ToCell(pickable16.Position.Y + 0.1f), Terrain.ToCell(pickable16.Position.Z)) == 104 || m_subsystemFireBlockBehavior.IsCellOnFire(Terrain.ToCell(pickable16.Position.X), Terrain.ToCell(pickable16.Position.Y + 0.1f), Terrain.ToCell(pickable16.Position.Z))))
						{
							m_subsystemAudio.PlayRandomSound("Audio/Sizzles", 1f, m_random.Float(-0.2f, 0.2f), pickable16.Position, 3f, autoDelay: true);
							if (Terrain.ExtractContents(pickable16.Value) == 380)
							{
								foreach (ComponentPlayer componentPlayer3 in m_subsystemPlayers.ComponentPlayers)
								{
									if (componentPlayer3.PlayerData.IsMainPlayer)
									{
										PlayerAchievements playerAchievements = componentPlayer3.PlayerAchievements;
										if (playerAchievements != null && playerAchievements.ForTheSafetyOfOurSanity == 0L)
										{
											playerAchievements.ForTheSafetyOfOurSanity = 1L;
											playerAchievements.EnqueueForTheSafetyOfOurSanityAchievement();
										}
									}
								}
							}
							pickable16.ToRemove = true;
							m_subsystemExplosions.TryExplodeBlock(Terrain.ToCell(pickable16.Position.X), Terrain.ToCell(pickable16.Position.Y), Terrain.ToCell(pickable16.Position.Z), pickable16.Value);
						}
						pickable16.SplashGenerated = false;
					}
				}
				if (vector7.HasValue && surfaceHeight.HasValue)
				{
					float num40 = surfaceHeight.Value - pickable16.Position.Y;
					float num41 = MathUtils.Saturate(3f * num40);
					if (block3 is QuicksandBlock)
					{
						Pickable pickable3 = pickable16;
						pickable3.Velocity.X = MathUtils.Lerp(pickable3.Velocity.X, 0f, 5f * dt);
						Pickable pickable4 = pickable16;
						pickable4.Velocity.Z = MathUtils.Lerp(pickable4.Velocity.Z, 0f, 5f * dt);
						Pickable pickable5 = pickable16;
						pickable5.Velocity.Y = MathUtils.Lerp(pickable5.Velocity.Y, -0.3f, 6f * dt);
						Pickable pickable6 = pickable16;
						pickable6.Velocity.Y = pickable6.Velocity.Y * num2;
					}
					else
					{
						Pickable pickable7 = pickable16;
						pickable7.Velocity.X = pickable7.Velocity.X + 4f * dt * (vector7.Value.X - pickable16.Velocity.X);
						Pickable pickable8 = pickable16;
						pickable8.Velocity.Y = pickable8.Velocity.Y - 10f * dt;
						Pickable pickable9 = pickable16;
						pickable9.Velocity.Y = pickable9.Velocity.Y + 10f * (1f / block2.Density * num41) * dt;
						Pickable pickable10 = pickable16;
						pickable10.Velocity.Z = pickable10.Velocity.Z + 4f * dt * (vector7.Value.Y - pickable16.Velocity.Z);
						Pickable pickable11 = pickable16;
						pickable11.Velocity.Y = pickable11.Velocity.Y * num2;
					}
				}
				else
				{
					Pickable pickable12 = pickable16;
					pickable12.Velocity.Y = pickable12.Velocity.Y - num22 * dt;
					pickable16.Velocity *= num;
				}
			}
			float num42 = pickable16.Velocity.Length();
			if (num42 > 0.5f)
			{
				float num43 = (float)(pickable16.Id * 13 % 100) / 100f - 0.5f;
				float num44 = (float)(pickable16.Id * 17 % 100) / 100f - 0.5f;
				float num45 = ((block2.Density > 0.7f) ? (0.7f / block2.Density) : 1f);
				float num46 = 0.7f * num45;
				if (MathUtils.Abs(num43 + num44) > 0.4f)
				{
					Pickable pickable13 = pickable16;
					pickable13.Rotation.X = pickable13.Rotation.X + num42 * dt * (1.5f * num46) * num43;
					Pickable pickable14 = pickable16;
					pickable14.Rotation.Z = pickable14.Rotation.Z + num42 * dt * (1.5f * num46) * num44;
				}
				Pickable pickable15 = pickable16;
				pickable15.Rotation.Y = pickable15.Rotation.Y + num42 * dt * (1.2f * num46);
			}
			else if (num42 < 0.1f && totalElapsedGameTime - pickable16.LastLookedAtTime > 0.1)
			{
				pickable16.Rotation.X = 0f;
				pickable16.Rotation.Z = 0f;
			}
			pickable16.Position = vector4;
		}
		if (CommonLib.WorkType != WorkType.Client)
		{
			if (Time.PeriodicEvent(5.0, 0.0))
			{
				CommonLib.Net.QueuePackage(new PickablePackage(m_pickables));
			}
			foreach (Pickable item2 in m_pickablesToRemove)
			{
				m_hotPickables.Remove(item2);
				m_pickables.Remove(item2);
				PickableRemoved?.Invoke(item2);
				useMaps.Remove(item2.Id);
				CommonLib.Net.QueuePackage(new PickablePackage(item2, PickablePackage.PickType.Delete));
			}
			m_pickablesToRemove.Clear();
		}
		if (CommonLib.WorkType == WorkType.Server)
		{
			m_pickables.RemoveAll((Pickable p) => p.IsDeletePickable && !p.IsPersistent);
		}
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
		m_subsystemMiningLightBehavior = Project.FindSubsystem<SubsystemMiningLightBehavior>(throwOnError: false);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemSky = Project.FindSubsystem<SubsystemSky>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
		m_subsystemExplosions = Project.FindSubsystem<SubsystemExplosions>(throwOnError: true);
		m_subsystemBlockBehaviors = Project.FindSubsystem<SubsystemBlockBehaviors>(throwOnError: true);
		m_subsystemFireBlockBehavior = Project.FindSubsystem<SubsystemFireBlockBehavior>(throwOnError: true);
		m_subsystemFluidBlockBehavior = Project.FindSubsystem<SubsystemFluidBlockBehavior>(throwOnError: true);
		m_subsystemFocalorsFluidBlockBehavior = Project.FindSubsystem<SubsystemFocalorsFluidBlockBehavior>(throwOnError: true);
		foreach (ValuesDictionary boneAbsoluteTransform in valuesDictionary.GetValue<ValuesDictionary>("Pickables").Values.Where((object v) => v is ValuesDictionary))
		{
			Pickable blockMesh = new Pickable();
			blockMesh.Id = boneAbsoluteTransform.GetValue("Id", blockMesh.Id);
			blockMesh.Value = boneAbsoluteTransform.GetValue("Value", blockMesh.Value);
			blockMesh.Count = boneAbsoluteTransform.GetValue("Count", blockMesh.Count);
			blockMesh.Position = boneAbsoluteTransform.GetValue("Position", blockMesh.Position);
			blockMesh.Velocity = boneAbsoluteTransform.GetValue("Velocity", blockMesh.Velocity);
			blockMesh.CreationTime = boneAbsoluteTransform.GetValue("CreationTime", blockMesh.CreationTime);
			object value = boneAbsoluteTransform.GetValue<object>("Rotation", null);
			if (value is Vector3 rotation)
			{
				blockMesh.Rotation = rotation;
			}
			else if (value is float y)
			{
				blockMesh.Rotation = new Vector3(0f, y, 0f);
			}
			else
			{
				blockMesh.Rotation = new Vector3(0f, m_random.Float(0f, (float)Math.PI * 2f), 0f);
			}
			blockMesh.IsSpecialPickable = boneAbsoluteTransform.GetValue("IsSpecialPickable", defaultValue: false);
			if (blockMesh.Id == 0)
			{
				blockMesh.Id = FindAvailableId();
			}
			else
			{
				useMaps.Add(blockMesh.Id);
			}
			if (boneAbsoluteTransform.ContainsKey("StuckMatrix"))
			{
				blockMesh.StuckMatrix = boneAbsoluteTransform.GetValue("StuckMatrix", blockMesh.StuckMatrix);
			}
			m_pickables.Add(blockMesh);
		}
	}

	public override void Save(ValuesDictionary valuesDictionary)
	{
		ValuesDictionary valuesDictionary2 = new ValuesDictionary();
		valuesDictionary.SetValue("Pickables", valuesDictionary2);
		int num = 0;
		foreach (Pickable pickable in m_pickables)
		{
			if (pickable.IsPersistent)
			{
				ValuesDictionary valuesDictionary3 = new ValuesDictionary();
				valuesDictionary2.SetValue(num.ToString(), valuesDictionary3);
				valuesDictionary3.SetValue("Id", pickable.Id);
				valuesDictionary3.SetValue("Value", pickable.Value);
				valuesDictionary3.SetValue("Count", pickable.Count);
				valuesDictionary3.SetValue("Position", pickable.Position);
				valuesDictionary3.SetValue("Velocity", pickable.Velocity);
				valuesDictionary3.SetValue("CreationTime", pickable.CreationTime);
				valuesDictionary3.SetValue("Rotation", pickable.Rotation);
				valuesDictionary3.SetValue("IsSpecialPickable", pickable.IsSpecialPickable);
				if (pickable.StuckMatrix.HasValue)
				{
					valuesDictionary3.SetValue("StuckMatrix", pickable.StuckMatrix.Value);
				}
				num++;
			}
		}
	}

	public SubsystemPickables()
	{
		rawToCookedFoodMap = new Dictionary<int, int>
		{
			{ 161, 162 },
			{ 77, 78 },
			{ 88, 89 },
			{ 333, 330 },
			{ 336, 331 },
			{ 118, 321 }
		};
		m_tmpPlayers = new List<ComponentPlayer>();
		m_pickables = new List<Pickable>();
		m_pickablesToRemove = new List<Pickable>();
		m_primitivesRenderer = new PrimitivesRenderer3D();
		m_random = new Random();
		m_drawBlockEnvironmentData = new DrawBlockEnvironmentData();
		useMaps = new List<ushort>();
		m_hotPickables = new HashSet<Pickable>();
	}

	static SubsystemPickables()
	{
		bigass = new int[1] { 367 };
		horizontalItemIndices = new int[37]
		{
			56, 318, 310, 58, 194, 59, 213, 23, 97, 210,
			211, 189, 163, 164, 329, 193, 331, 336, 202, 78,
			77, 346, 333, 89, 88, 330, 240, 166, 94, 347,
			239, 98, 203, 57, 384, 385, 432
		};
		specialItemIndices = new int[85]
		{
			413, 414, 415, 165, 323, 324, 182, 370, 357, 254,
			31, 317, 196, 198, 206, 130, 167, 325, 218, 114,
			115, 37, 38, 29, 222, 113, 36, 219, 172, 171,
			169, 108, 120, 121, 230, 160, 349, 123, 122, 195,
			221, 125, 124, 191, 212, 175, 345, 162, 161, 241,
			215, 133, 224, 137, 145, 134, 135, 140, 143, 156,
			146, 184, 186, 188, 320, 187, 157, 181, 180, 183,
			139, 141, 142, 179, 152, 151, 199, 410, 411, 405,
			406, 407, 409, 431, 395
		};
		lightItemIndices = new int[3] { 93, 17, 132 };
		m_drawOrders = new int[1] { 10 };
	}

	public static bool IsSpecialItem(int value)
	{
		int model = Terrain.ExtractContents(value);
		return specialItemIndices.Contains(model);
	}

	public static bool IsLightItem(int value)
	{
		int tintedColor = Terrain.ExtractContents(value);
		return lightItemIndices.Contains(tintedColor);
	}

	public static bool IsHorizontalItem(int value)
	{
		int tintedColor = Terrain.ExtractContents(value);
		return horizontalItemIndices.Contains(tintedColor);
	}

	public Pickable AddSpecialPickable(int value, int count, Vector3 position, Vector3? velocity, Matrix? stuckMatrix)
	{
		if (CommonLib.WorkType == WorkType.Client)
		{
			return null;
		}
		Pickable pickable = CreatePickable(null, value, count, position, velocity, stuckMatrix);
		pickable.Net_ToRemove = true;
		pickable.IsSpecialPickable = true;
		CommonLib.Net.QueuePackage(new PickablePackage(pickable, PickablePackage.PickType.Create));
		return pickable;
	}

	public Pickable AddDeletePickable(int value, int count, Vector3 position, Vector3? velocity, Matrix? stuckMatrix)
	{
		if (CommonLib.WorkType == WorkType.Client)
		{
			return null;
		}
		Pickable pickable = CreatePickable(null, value, count, position, velocity, stuckMatrix);
		pickable.IsDeletePickable = true;
		pickable.IsPersistent = false;
		CommonLib.Net.QueuePackage(new PickablePackage(pickable, PickablePackage.PickType.Create));
		return pickable;
	}

	public static bool IsBigAss(int value)
	{
		int value2 = Terrain.ExtractContents(value);
		return bigass.Contains(value2);
	}

	private void CalculateMinersLightAtPosition<T>(Vector3 pos, Dictionary<T, SubsystemMiningLightBehavior.MiningLightData> lights, ref int currentMax)
	{
		foreach (SubsystemMiningLightBehavior.MiningLightData value in lights.Values)
		{
			float num = Vector3.DistanceSquared(pos, value.Position);
			float num2 = value.Intensity * 2f;
			if (num < num2 * num2)
			{
				float num3 = MathUtils.Sqrt(num);
				int num4 = (int)((1f - num3 / num2) * 15f);
				if (num4 > currentMax)
				{
					currentMax = num4;
				}
			}
		}
	}

	public int ClearLoosePickables()
	{
		int kind = 0;
		foreach (Pickable item in new List<Pickable>(m_pickables))
		{
			m_pickables.Remove(item);
			PickableRemoved?.Invoke(item);
			useMaps.Remove(item.Id);
			CommonLib.Net.QueuePackage(new PickablePackage(item, PickablePackage.PickType.Delete));
			kind++;
		}
		return kind;
	}
}
