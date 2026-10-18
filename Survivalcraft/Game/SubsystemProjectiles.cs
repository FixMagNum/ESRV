using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Engine;
using Engine.Graphics;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemProjectiles : Subsystem, IUpdateable, IDrawable
{
	public SubsystemAudio m_subsystemAudio;

	public SubsystemSoundMaterials m_subsystemSoundMaterials;

	public SubsystemParticles m_subsystemParticles;

	public SubsystemPickables m_subsystemPickables;

	public SubsystemBodies m_subsystemBodies;

	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemSky m_subsystemSky;

	public SubsystemTime m_subsystemTime;

	public SubsystemNoise m_subsystemNoise;

	public SubsystemExplosions m_subsystemExplosions;

	public SubsystemGameInfo m_subsystemGameInfo;

	public SubsystemBlockBehaviors m_subsystemBlockBehaviors;

	public SubsystemFluidBlockBehavior m_subsystemFluidBlockBehavior;

	public SubsystemFireBlockBehavior m_subsystemFireBlockBehavior;

	public List<Projectile> m_projectiles = new List<Projectile>();

	public List<Projectile> m_projectilesToRemove = new List<Projectile>();

	public PrimitivesRenderer3D m_primitivesRenderer = new PrimitivesRenderer3D();

	public Random m_random = new Random();

	public DrawBlockEnvironmentData m_drawBlockEnvironmentData = new DrawBlockEnvironmentData();

	public const float BodyInflateAmount = 0.2f;

	public static int[] m_drawOrders;

	public SubsystemPlayers m_subsystemPlayers;

	public ReadOnlyList<Projectile> Projectiles => new ReadOnlyList<Projectile>(m_projectiles);

	public int[] DrawOrders => m_drawOrders;

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public event Action<Projectile> ProjectileAdded;

	public event Action<Projectile> ProjectileRemoved;

	public Projectile AddProjectile(int value, Vector3 position, Vector3 velocity, Vector3 angularVelocity, ComponentCreature owner)
	{
		if (CommonLib.WorkType == WorkType.Client)
		{
			return null;
		}
		Projectile model = AddProjectileNet(value, position, velocity, angularVelocity, owner);
		CommonLib.Net.QueuePackage(new ProjectilePackage(model));
		return model;
	}

	public Projectile AddProjectileNet(int value, Vector3 position, Vector3 velocity, Vector3 angularVelocity, ComponentCreature owner)
	{
		Projectile projectile = new Projectile
		{
			Value = value,
			Position = position,
			Velocity = velocity,
			Rotation = Vector3.Zero,
			AngularVelocity = angularVelocity,
			CreationTime = m_subsystemGameInfo.TotalElapsedGameTime,
			IsInWater = IsWater(position),
			Owner = owner,
			ProjectileStoppedAction = ProjectileStoppedAction.TurnIntoPickable
		};
		m_projectiles.Add(projectile);
		ProjectileAdded?.Invoke(projectile);
		if (owner != null && owner.PlayerStats != null)
		{
			owner.PlayerStats.RangedAttacks++;
		}
		return projectile;
	}

	public Projectile FireProjectile(int value, Vector3 position, Vector3 velocity, Vector3 angularVelocity, ComponentCreature owner)
	{
		if (CommonLib.WorkType == WorkType.Client)
		{
			return null;
		}
		Projectile projectile = FireProjectileNet(value, position, velocity, angularVelocity, owner);
		if (projectile != null)
		{
			projectile.IsFireProjectile = true;
			CommonLib.Net.QueuePackage(new ProjectilePackage(projectile));
		}
		return projectile;
	}

	public Projectile FireProjectileNet(int value, Vector3 position, Vector3 velocity, Vector3 angularVelocity, ComponentCreature owner)
	{
		int num = Terrain.ExtractContents(value);
		Block block = BlocksManager.Blocks[num];
		Vector3 vector = Vector3.Normalize(velocity);
		Vector3 vector2 = position;
		if (owner != null)
		{
			Ray3 ray = new Ray3(position + vector * 5f, -vector);
			BoundingBox boundingBox = owner.ComponentBody.BoundingBox;
			boundingBox.Min -= new Vector3(0.4f);
			boundingBox.Max += new Vector3(0.4f);
			float? num2 = ray.Intersection(boundingBox);
			if (num2.HasValue)
			{
				if (num2.Value == 0f)
				{
					return null;
				}
				vector2 = position + vector * (5f - num2.Value + 0.1f);
			}
		}
		Vector3 end = vector2 + vector * block.ProjectileTipOffset;
		if (!m_subsystemTerrain.Raycast(position, end, useInteractionBoxes: false, skipAirBlocks: true, (int testValue, float distance) => BlocksManager.Blocks[Terrain.ExtractContents(testValue)].IsCollidable).HasValue)
		{
			Projectile projectile = AddProjectileNet(value, vector2, velocity, angularVelocity, owner);
			SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(num);
			for (int num3 = 0; num3 < blockBehaviors.Length; num3++)
			{
				blockBehaviors[num3].OnFiredAsProjectile(projectile);
			}
			return projectile;
		}
		return null;
	}

	public void AddTrail(Projectile projectile, Vector3 offset, ITrailParticleSystem particleSystem)
	{
		RemoveTrail(projectile);
		projectile.TrailParticleSystem = particleSystem;
		projectile.TrailOffset = offset;
	}

	public void RemoveTrail(Projectile projectile)
	{
		if (projectile.TrailParticleSystem != null)
		{
			if (m_subsystemParticles.ContainsParticleSystem((ParticleSystemBase)projectile.TrailParticleSystem))
			{
				m_subsystemParticles.RemoveParticleSystem((ParticleSystemBase)projectile.TrailParticleSystem);
			}
			projectile.TrailParticleSystem = null;
		}
	}

	public void Draw(Camera camera, int drawOrder)
	{
		m_drawBlockEnvironmentData.SubsystemTerrain = m_subsystemTerrain;
		m_drawBlockEnvironmentData.InWorldMatrix = Matrix.Identity;
		float num = MathUtils.Sqr(m_subsystemSky.VisibilityRange);
		foreach (Projectile projectile in m_projectiles)
		{
			Vector3 position = projectile.Position;
			if (!projectile.NoChunk && Vector3.DistanceSquared(camera.ViewPosition, position) < num && camera.ViewFrustum.Intersection(position))
			{
				int x = Terrain.ToCell(position.X);
				int num2 = Terrain.ToCell(position.Y);
				int z = Terrain.ToCell(position.Z);
				int num3 = Terrain.ExtractContents(projectile.Value);
				Block block = BlocksManager.Blocks[num3];
				TerrainChunk chunkAtCell = m_subsystemTerrain.Terrain.GetChunkAtCell(x, z);
				if (chunkAtCell != null && chunkAtCell.State >= TerrainChunkState.InvalidVertices1 && num2 >= 0 && num2 < 255)
				{
					m_drawBlockEnvironmentData.Humidity = m_subsystemTerrain.Terrain.GetSeasonalHumidity(x, z);
					m_drawBlockEnvironmentData.Temperature = m_subsystemTerrain.Terrain.GetSeasonalTemperature(x, z) + SubsystemWeather.GetTemperatureAdjustmentAtHeight(num2);
					projectile.Light = m_subsystemTerrain.Terrain.GetCellLightFast(x, num2, z);
				}
				m_drawBlockEnvironmentData.Light = projectile.Light;
				m_drawBlockEnvironmentData.BillboardDirection = (block.AlignToVelocity ? ((Vector3?)null) : new Vector3?(camera.ViewDirection));
				m_drawBlockEnvironmentData.InWorldMatrix.Translation = position;
				Matrix matrix;
				if (block.AlignToVelocity)
				{
					CalculateVelocityAlignMatrix(block, position, projectile.Velocity, out matrix);
				}
				else if (projectile.Rotation != Vector3.Zero)
				{
					matrix = Matrix.CreateFromAxisAngle(Vector3.Normalize(projectile.Rotation), projectile.Rotation.Length());
					matrix.Translation = projectile.Position;
				}
				else
				{
					matrix = Matrix.CreateTranslation(projectile.Position);
				}
				block.DrawBlock(m_primitivesRenderer, projectile.Value, Color.White, 0.3f, ref matrix, m_drawBlockEnvironmentData);
			}
		}
		m_primitivesRenderer.Flush(camera.ViewProjectionMatrix);
	}

	public void Update(float dt)
	{
		double displayName = m_subsystemGameInfo.TotalElapsedGameTime;
		ComponentPlayer componentPlayer = null;
		foreach (PlayerData playersDatum in m_subsystemPlayers.PlayersData)
		{
			if (playersDatum.IsMainPlayer && playersDatum.IsCheater)
			{
				componentPlayer = playersDatum.ComponentPlayer;
				break;
			}
		}
		foreach (Projectile projectile3 in m_projectiles)
		{
			if (componentPlayer != null && componentPlayer.ComponentBody != null && !projectile3.ToRemove)
			{
				Vector3 v = componentPlayer.ComponentBody.Position + new Vector3(0f, 1f, 0f) - projectile3.Position;
				if (v.LengthSquared() > 0.5f)
				{
					float num = projectile3.Velocity.Length();
					if (num < 15f)
					{
						num = MathUtils.Lerp(num, 15f, dt * 5f);
					}
					Vector3 v2 = Vector3.Normalize(projectile3.Velocity);
					if (num < 0.1f)
					{
						v2 = Vector3.Normalize(v);
					}
					Vector3 v3 = Vector3.Normalize(v);
					Vector3 vector = Vector3.Normalize(Vector3.Lerp(v2, v3, dt * 15.5f));
					projectile3.Velocity = vector * num;
					projectile3.Velocity += vector * (5f * dt);
				}
			}
			if (projectile3.ToRemove)
			{
				m_projectilesToRemove.Add(projectile3);
				continue;
			}
			Block block = BlocksManager.Blocks[Terrain.ExtractContents(projectile3.Value)];
			if (Terrain.ExtractContents(projectile3.Value) != 396 && displayName - projectile3.CreationTime > 40.0)
			{
				projectile3.ToRemove = true;
			}
			TerrainChunk chunkAtCell = m_subsystemTerrain.Terrain.GetChunkAtCell(Terrain.ToCell(projectile3.Position.X), Terrain.ToCell(projectile3.Position.Z));
			if (chunkAtCell == null || chunkAtCell.State <= TerrainChunkState.InvalidContents4)
			{
				projectile3.NoChunk = true;
				if (projectile3.TrailParticleSystem != null)
				{
					projectile3.TrailParticleSystem.IsStopped = true;
				}
				continue;
			}
			projectile3.NoChunk = false;
			Vector3 position = projectile3.Position;
			Vector3 vector2 = position + projectile3.Velocity * dt;
			Vector3 vector3 = block.ProjectileTipOffset * Vector3.Normalize(projectile3.Velocity);
			BodyRaycastResult? bodyRaycastResult = m_subsystemBodies.Raycast(position + vector3, vector2 + vector3, 0.2f, (ComponentBody body, float distance) => true);
			TerrainRaycastResult? terrainRaycastResult = m_subsystemTerrain.Raycast(position + vector3, vector2 + vector3, useInteractionBoxes: false, skipAirBlocks: true, (int value, float distance) => BlocksManager.Blocks[Terrain.ExtractContents(value)].IsCollidable);
			bool flag = block.DisintegratesOnHit;
			if (terrainRaycastResult.HasValue || bodyRaycastResult.HasValue)
			{
				CellFace? cellFace = (terrainRaycastResult.HasValue ? new CellFace?(terrainRaycastResult.Value.CellFace) : ((CellFace?)null));
				ComponentBody componentBody = (bodyRaycastResult.HasValue ? bodyRaycastResult.Value.ComponentBody : null);
				SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(Terrain.ExtractContents(projectile3.Value));
				for (int num2 = 0; num2 < blockBehaviors.Length; num2++)
				{
					flag |= blockBehaviors[num2].OnHitAsProjectile(cellFace, componentBody, projectile3);
				}
				projectile3.ToRemove = flag;
			}
			Vector3? vector4 = null;
			if (bodyRaycastResult.HasValue && (!terrainRaycastResult.HasValue || bodyRaycastResult.Value.Distance < terrainRaycastResult.Value.Distance))
			{
				if (projectile3.Velocity.Length() > 10f)
				{
					ComponentMiner.AttackBody(bodyRaycastResult.Value.ComponentBody, projectile3.Owner, bodyRaycastResult.Value.HitPoint(), Vector3.Normalize(projectile3.Velocity), block.GetProjectilePower(projectile3.Value), isMeleeAttack: false);
					if (projectile3.Owner != null && projectile3.Owner.PlayerStats != null)
					{
						projectile3.Owner.PlayerStats.RangedHits++;
					}
				}
				if (projectile3.IsIncendiary)
				{
					bodyRaycastResult.Value.ComponentBody.Entity.FindComponent<ComponentOnFire>()?.SetOnFire(projectile3?.Owner, m_random.Float(6f, 8f));
				}
				if (IsTorchArrow(projectile3.Value))
				{
					m_subsystemPickables.AddPickable(Terrain.MakeBlockValue(31), 1, projectile3.Position, Vector3.Zero, null);
					projectile3.ToRemove = true;
					continue;
				}
				vector2 = position;
				projectile3.Velocity *= -0.05f;
				projectile3.Velocity += m_random.Vector3(0.33f * projectile3.Velocity.Length());
				projectile3.AngularVelocity *= -0.05f;
			}
			else if (terrainRaycastResult.HasValue)
			{
				CellFace cellFace2 = terrainRaycastResult.Value.CellFace;
				int cellValue = m_subsystemTerrain.Terrain.GetCellValue(cellFace2.X, cellFace2.Y, cellFace2.Z);
				int num3 = Terrain.ExtractContents(cellValue);
				Block block2 = BlocksManager.Blocks[num3];
				float num4 = projectile3.Velocity.Length();
				SubsystemBlockBehavior[] blockBehaviors2 = m_subsystemBlockBehaviors.GetBlockBehaviors(num3);
				for (int num5 = 0; num5 < blockBehaviors2.Length; num5++)
				{
					blockBehaviors2[num5].OnHitByProjectile(cellFace2, projectile3);
				}
				if (IsTorchArrow(projectile3.Value))
				{
					int cellContents = m_subsystemTerrain.Terrain.GetCellContents(cellFace2.X, cellFace2.Y, cellFace2.Z);
					if (BlocksManager.Blocks[cellContents].IsCollidable)
					{
						PlaceTorchOnBlock(cellFace2, projectile3.Position);
					}
					else
					{
						m_subsystemPickables.AddPickable(Terrain.MakeBlockValue(31), 1, projectile3.Position, Vector3.Zero, null);
					}
					projectile3.ToRemove = true;
					continue;
				}
				if (num4 > 10f && m_random.Float(0f, 1f) > block2.ProjectileResilience)
				{
					m_subsystemTerrain.DestroyCell(0, cellFace2.X, cellFace2.Y, cellFace2.Z, 0, noDrop: true, noParticleSystem: false);
					m_subsystemSoundMaterials.PlayImpactSound(cellValue, position, 1f);
				}
				if (projectile3.IsIncendiary)
				{
					m_subsystemFireBlockBehavior.SetCellOnFire(terrainRaycastResult.Value.CellFace.X, terrainRaycastResult.Value.CellFace.Y, terrainRaycastResult.Value.CellFace.Z, 1f);
					Vector3 vector5 = projectile3.Position - 0.75f * Vector3.Normalize(projectile3.Velocity);
					for (int num6 = 0; num6 < 8; num6++)
					{
						Vector3 vector6 = ((num6 == 0) ? Vector3.Normalize(projectile3.Velocity) : m_random.Vector3(1.5f));
						TerrainRaycastResult? terrainRaycastResult2 = m_subsystemTerrain.Raycast(vector5, vector5 + vector6, useInteractionBoxes: false, skipAirBlocks: true, (int value, float distance) => true);
						if (terrainRaycastResult2.HasValue)
						{
							m_subsystemFireBlockBehavior.SetCellOnFire(terrainRaycastResult2.Value.CellFace.X, terrainRaycastResult2.Value.CellFace.Y, terrainRaycastResult2.Value.CellFace.Z, 1f);
						}
					}
				}
				if (num4 > 5f)
				{
					m_subsystemSoundMaterials.PlayImpactSound(cellValue, position, 1f);
				}
				if (block.IsStickable && num4 > 10f && m_random.Bool(block2.ProjectileStickProbability))
				{
					Vector3 vector7 = Vector3.Normalize(projectile3.Velocity);
					float num7 = MathUtils.Lerp(0.1f, 0.2f, MathUtils.Saturate((num4 - 15f) / 20f));
					vector4 = position + terrainRaycastResult.Value.Distance * Vector3.Normalize(projectile3.Velocity) + vector7 * num7;
				}
				else
				{
					Plane plane = cellFace2.CalculatePlane();
					vector2 = position;
					if (plane.Normal.X != 0f)
					{
						projectile3.Velocity *= new Vector3(-0.3f, 0.3f, 0.3f);
					}
					if (plane.Normal.Y != 0f)
					{
						projectile3.Velocity *= new Vector3(0.3f, -0.3f, 0.3f);
					}
					if (plane.Normal.Z != 0f)
					{
						projectile3.Velocity *= new Vector3(0.3f, 0.3f, -0.3f);
					}
					float num8 = projectile3.Velocity.Length();
					projectile3.Velocity = num8 * Vector3.Normalize(projectile3.Velocity + m_random.Vector3(num8 / 6f, num8 / 3f));
					projectile3.AngularVelocity *= -0.3f;
				}
				MakeProjectileNoise(projectile3);
			}
			if (terrainRaycastResult.HasValue || bodyRaycastResult.HasValue)
			{
				if (flag)
				{
					m_subsystemParticles.AddParticleSystem(block.CreateDebrisParticleSystem(m_subsystemTerrain, projectile3.Position, projectile3.Value, 1f));
				}
				else if (!projectile3.ToRemove && (vector4.HasValue || projectile3.Velocity.Length() < 1f))
				{
					if (projectile3.ProjectileStoppedAction == ProjectileStoppedAction.TurnIntoPickable)
					{
						int num9 = BlocksManager.DamageItem(projectile3.Value, 1);
						if (num9 != 0)
						{
							if (vector4.HasValue)
							{
								CalculateVelocityAlignMatrix(block, vector4.Value, projectile3.Velocity, out var matrix);
								m_subsystemPickables.AddPickable(num9, 1, projectile3.Position, Vector3.Zero, matrix).IsStuckProjectile = true;
							}
							else
							{
								m_subsystemPickables.AddPickable(num9, 1, position, Vector3.Zero, null);
							}
						}
						projectile3.ToRemove = true;
					}
					else if (projectile3.ProjectileStoppedAction == ProjectileStoppedAction.Disappear)
					{
						projectile3.ToRemove = true;
					}
				}
			}
			float num10 = (projectile3.IsInWater ? MathUtils.Pow(0.001f, dt) : MathUtils.Pow(block.ProjectileDamping, dt));
			Projectile projectile = projectile3;
			projectile.Velocity.Y = projectile.Velocity.Y + -10f * dt;
			projectile3.Velocity *= num10;
			projectile3.AngularVelocity *= num10;
			projectile3.Position = vector2;
			projectile3.Rotation += projectile3.AngularVelocity * dt;
			if (projectile3.TrailParticleSystem != null)
			{
				if (!m_subsystemParticles.ContainsParticleSystem((ParticleSystemBase)projectile3.TrailParticleSystem))
				{
					m_subsystemParticles.AddParticleSystem((ParticleSystemBase)projectile3.TrailParticleSystem);
				}
				Vector3 vector8 = ((projectile3.TrailOffset != Vector3.Zero) ? Vector3.TransformNormal(projectile3.TrailOffset, Matrix.CreateFromAxisAngle(Vector3.Normalize(projectile3.Rotation), projectile3.Rotation.Length())) : Vector3.Zero);
				projectile3.TrailParticleSystem.Position = projectile3.Position + vector8;
				if (projectile3.IsInWater)
				{
					projectile3.TrailParticleSystem.IsStopped = true;
				}
			}
			bool flag2 = IsWater(projectile3.Position);
			if (projectile3.IsInWater != flag2)
			{
				if (flag2)
				{
					float num11 = new Vector2(projectile3.Velocity.X + projectile3.Velocity.Z).Length();
					if (num11 > 6f && num11 > 4f * MathUtils.Abs(projectile3.Velocity.Y))
					{
						projectile3.Velocity *= 0.5f;
						Projectile projectile2 = projectile3;
						projectile2.Velocity.Y = projectile2.Velocity.Y * -1f;
						flag2 = false;
					}
					else
					{
						projectile3.Velocity *= 0.2f;
					}
					float? surfaceHeight = m_subsystemFluidBlockBehavior.GetSurfaceHeight(Terrain.ToCell(projectile3.Position.X), Terrain.ToCell(projectile3.Position.Y), Terrain.ToCell(projectile3.Position.Z));
					if (surfaceHeight.HasValue)
					{
						m_subsystemParticles.AddParticleSystem(new WaterSplashParticleSystem(m_subsystemTerrain, new Vector3(projectile3.Position.X, surfaceHeight.Value, projectile3.Position.Z), large: false));
						m_subsystemAudio.PlayRandomSound("Audio/Splashes", 1f, m_random.Float(-0.2f, 0.2f), projectile3.Position, 6f, autoDelay: true);
						MakeProjectileNoise(projectile3);
					}
				}
				projectile3.IsInWater = flag2;
			}
			if (IsMagma(projectile3.Position))
			{
				m_subsystemParticles.AddParticleSystem(new MagmaSplashParticleSystem(m_subsystemTerrain, projectile3.Position, large: false));
				m_subsystemAudio.PlayRandomSound("Audio/Sizzles", 1f, m_random.Float(-0.2f, 0.2f), projectile3.Position, 3f, autoDelay: true);
				projectile3.ToRemove = true;
				m_subsystemExplosions.TryExplodeBlock(Terrain.ToCell(projectile3.Position.X), Terrain.ToCell(projectile3.Position.Y), Terrain.ToCell(projectile3.Position.Z), projectile3.Value);
			}
			if (m_subsystemTime.PeriodicGameTimeEvent(1.0, (double)(projectile3.GetHashCode() % 100) / 100.0) && (m_subsystemFireBlockBehavior.IsCellOnFire(Terrain.ToCell(projectile3.Position.X), Terrain.ToCell(projectile3.Position.Y + 0.1f), Terrain.ToCell(projectile3.Position.Z)) || m_subsystemFireBlockBehavior.IsCellOnFire(Terrain.ToCell(projectile3.Position.X), Terrain.ToCell(projectile3.Position.Y + 0.1f) - 1, Terrain.ToCell(projectile3.Position.Z))))
			{
				m_subsystemAudio.PlayRandomSound("Audio/Sizzles", 1f, m_random.Float(-0.2f, 0.2f), projectile3.Position, 3f, autoDelay: true);
				projectile3.ToRemove = true;
				m_subsystemExplosions.TryExplodeBlock(Terrain.ToCell(projectile3.Position.X), Terrain.ToCell(projectile3.Position.Y), Terrain.ToCell(projectile3.Position.Z), projectile3.Value);
			}
		}
		foreach (Projectile item in m_projectilesToRemove)
		{
			if (item.TrailParticleSystem != null)
			{
				item.TrailParticleSystem.IsStopped = true;
			}
			m_projectiles.Remove(item);
			ProjectileRemoved?.Invoke(item);
		}
		m_projectilesToRemove.Clear();
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemSoundMaterials = Project.FindSubsystem<SubsystemSoundMaterials>(throwOnError: true);
		m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(throwOnError: true);
		m_subsystemBodies = Project.FindSubsystem<SubsystemBodies>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemSky = Project.FindSubsystem<SubsystemSky>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemNoise = Project.FindSubsystem<SubsystemNoise>(throwOnError: true);
		m_subsystemExplosions = Project.FindSubsystem<SubsystemExplosions>(throwOnError: true);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemBlockBehaviors = Project.FindSubsystem<SubsystemBlockBehaviors>(throwOnError: true);
		m_subsystemFluidBlockBehavior = Project.FindSubsystem<SubsystemFluidBlockBehavior>(throwOnError: true);
		m_subsystemFireBlockBehavior = Project.FindSubsystem<SubsystemFireBlockBehavior>(throwOnError: true);
		m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
		foreach (ValuesDictionary boneAbsoluteTransform in valuesDictionary.GetValue<ValuesDictionary>("Projectiles").Values.Where((object v) => v is ValuesDictionary))
		{
			Projectile matrix2 = new Projectile
			{
				Value = boneAbsoluteTransform.GetValue<int>("Value"),
				Position = boneAbsoluteTransform.GetValue<Vector3>("Position"),
				Velocity = boneAbsoluteTransform.GetValue<Vector3>("Velocity"),
				CreationTime = boneAbsoluteTransform.GetValue<double>("CreationTime")
			};
			m_projectiles.Add(matrix2);
		}
	}

	public override void Save(ValuesDictionary valuesDictionary)
	{
		ValuesDictionary valuesDictionary2 = new ValuesDictionary();
		valuesDictionary.SetValue("Projectiles", valuesDictionary2);
		int num = 0;
		foreach (Projectile projectile in m_projectiles)
		{
			ValuesDictionary valuesDictionary3 = new ValuesDictionary();
			valuesDictionary2.SetValue(num.ToString(CultureInfo.InvariantCulture), valuesDictionary3);
			valuesDictionary3.SetValue("Value", projectile.Value);
			valuesDictionary3.SetValue("Position", projectile.Position);
			valuesDictionary3.SetValue("Velocity", projectile.Velocity);
			valuesDictionary3.SetValue("CreationTime", projectile.CreationTime);
			num++;
		}
	}

	public bool IsWater(Vector3 position)
	{
		int mountingFace = m_subsystemTerrain.Terrain.GetCellContents(Terrain.ToCell(position.X), Terrain.ToCell(position.Y), Terrain.ToCell(position.Z));
		return BlocksManager.Blocks[mountingFace] is FocalorsWaterBlock;
	}

	public bool IsMagma(Vector3 position)
	{
		int cellContents = m_subsystemTerrain.Terrain.GetCellContents(Terrain.ToCell(position.X), Terrain.ToCell(position.Y), Terrain.ToCell(position.Z));
		return BlocksManager.Blocks[cellContents] is FocalorsMagmaBlock;
	}

	public void MakeProjectileNoise(Projectile projectile)
	{
		if (m_subsystemTime.GameTime - projectile.LastNoiseTime > 0.5)
		{
			m_subsystemNoise.MakeNoise(projectile.Position, 0.25f, 6f);
			projectile.LastNoiseTime = m_subsystemTime.GameTime;
		}
	}

	public static void CalculateVelocityAlignMatrix(Block projectileBlock, Vector3 position, Vector3 velocity, out Matrix matrix)
	{
		matrix = Matrix.Identity;
		matrix.Up = Vector3.Normalize(velocity);
		matrix.Right = Vector3.Normalize(Vector3.Cross(matrix.Up, Vector3.UnitY));
		matrix.Forward = Vector3.Normalize(Vector3.Cross(matrix.Up, matrix.Right));
		matrix.Translation = position;
	}

	static SubsystemProjectiles()
	{
		m_drawOrders = new int[1] { 10 };
	}

	private bool IsTorchArrow(int value)
	{
		int mountingFace = Terrain.ExtractContents(value);
		if (BlocksManager.Blocks[mountingFace] is ArrowBlock)
		{
			return ArrowBlock.GetArrowType(Terrain.ExtractData(value)) == ArrowBlock.ArrowType.TorchArrow;
		}
		return false;
	}

	private void PlaceTorchOnBlock(CellFace cellFace, Vector3 projectilePosition)
	{
		int data = m_subsystemTerrain.Terrain.GetCellContents(cellFace.X, cellFace.Y, cellFace.Z);
		_ = BlocksManager.Blocks[data];
		for (int mountingFace = 0; mountingFace < 6; mountingFace++)
		{
			if (mountingFace != 5)
			{
				int x = cellFace.X + CellFace.FaceToPoint3(mountingFace).X;
				int y = cellFace.Y + CellFace.FaceToPoint3(mountingFace).Y;
				int z = cellFace.Z + CellFace.FaceToPoint3(mountingFace).Z;
				if (m_subsystemTerrain.Terrain.GetCellContents(x, y, z) == 0)
				{
					int value = Terrain.MakeBlockValue(31, 0, mountingFace);
					m_subsystemTerrain.ChangeCell(x, y, z, value);
					return;
				}
			}
		}
		m_subsystemPickables.AddPickable(Terrain.MakeBlockValue(31), 1, projectilePosition, Vector3.Zero, null);
	}
}
