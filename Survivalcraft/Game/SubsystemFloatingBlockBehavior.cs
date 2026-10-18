using System;
using System.Collections.Generic;
using Engine;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemFloatingBlockBehavior : SubsystemBlockBehavior, IUpdateable
{
	public const int FloatingBlockIndex = 434;

	public const string MovingBlockId = "FloatingBlock";

	public SubsystemMovingBlocks m_subsystemMovingBlocks;

	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemWeather m_subsystemWeather;

	public SubsystemOceanScanner m_subsystemOceanScanner;

	public SubsystemSoundMaterials m_subsystemSoundMaterials;

	public SubsystemAudio m_subsystemAudio;

	public const int SafetyLimit = 100000;

	private static readonly Point3[] m_neighborOffsets;

	public SubsystemBlockEntities m_subsystemBlockEntities;

	private Random m_random = new Random();

	public const string WheelBlockId = "WheelBlock";

	private const float WaterDensity = 1f;

	private const float Gravity = 10f;

	private const float AirDrag = 0.5f;

	private const float WaterDrag = 2f;

	public SubsystemFocalorsFluidBlockBehavior m_subsystemFocalorsFluidBlockBehavior;

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override int[] HandledBlocks => new int[1] { 434 };

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemMovingBlocks = Project.FindSubsystem<SubsystemMovingBlocks>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemFocalorsFluidBlockBehavior = Project.FindSubsystem<SubsystemFocalorsFluidBlockBehavior>(throwOnError: true);
		m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
		m_subsystemOceanScanner = Project.FindSubsystem<SubsystemOceanScanner>(throwOnError: true);
		m_subsystemSoundMaterials = Project.FindSubsystem<SubsystemSoundMaterials>(throwOnError: true);
		m_subsystemBlockEntities = Project.FindSubsystem<SubsystemBlockEntities>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemMovingBlocks.CollidedWithTerrain += HandleCollision;
		m_subsystemMovingBlocks.Stopped += HandleStopped;
	}

	public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
	{
	}

	private void HandleCollision(IMovingBlockSet mbs, Point3 p)
	{
		if (!(mbs.Id != "FloatingBlock"))
		{
			int num = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(p.X, p.Y, p.Z));
			if (!(BlocksManager.Blocks[num] is FocalorsFluidBlock))
			{
				mbs.Stop();
			}
		}
	}

	public void Update(float dt)
	{
		foreach (IMovingBlockSet movingBlockSet2 in m_subsystemMovingBlocks.MovingBlockSets)
		{
			if (movingBlockSet2 is SubsystemMovingBlocks.MovingBlockSet movingBlockSet)
			{
				if (movingBlockSet.Id == "FloatingBlock")
				{
					ApplyBuoyancyAndWind(movingBlockSet, dt);
					UpdateAttachedEntities(movingBlockSet, dt);
				}
				else if (movingBlockSet.Id == "WheelBlock")
				{
					UpdateWheelAnimation(movingBlockSet, dt);
				}
			}
		}
	}

	private void ApplyBuoyancyAndWind(SubsystemMovingBlocks.MovingBlockSet set, float dt)
	{
		if (!(set.Tag is ValuesDictionary valuesDictionary))
		{
			return;
		}
		float value = valuesDictionary.GetValue<float>("Mass");
		Vector3 value2 = valuesDictionary.GetValue<Vector3>("LocalCoM");
		Vector3 vector = valuesDictionary.GetValue<Vector3>("AngularVelocity");
		Vector3 vector2 = set.Position + Vector3.Transform(value2, set.Rotation);
		float num = MathUtils.Saturate((float)set.Blocks.Count / 1000f);
		float num2 = MathUtils.Lerp(0.1f, 0.95f, num);
		float x = MathUtils.Lerp(0.98f, 0.9f, num);
		float f = MathUtils.Lerp(1f, 0.15f, num);
		Vector3 zero = Vector3.Zero;
		Vector3 zero2 = Vector3.Zero;
		zero += new Vector3(0f, -10f, 0f) * value;
		int num3 = Math.Max(1, set.Blocks.Count / 300);
		float num4 = 64f;
		int num5 = 0;
		float num6 = 14f;
		for (int i = 0; i < set.Blocks.Count; i += num3)
		{
			Vector3 v = new Vector3(set.Blocks[i].Offset);
			Vector3 vector3 = set.Position + Vector3.Transform(v, set.Rotation);
			if (!(vector3.Y <= set.Position.Y + 20f))
			{
				continue;
			}
			int x2 = Terrain.ToCell(vector3.X);
			int z = Terrain.ToCell(vector3.Z);
			int num7 = Terrain.ToCell(vector3.Y);
			float? surfaceHeight = m_subsystemFocalorsFluidBlockBehavior.GetSurfaceHeight(x2, num7, z, out var surfaceFluidBlock);
			if (!surfaceHeight.HasValue)
			{
				surfaceHeight = m_subsystemFocalorsFluidBlockBehavior.GetSurfaceHeight(x2, num7 - 1, z, out surfaceFluidBlock);
			}
			float num8 = (surfaceHeight.HasValue ? surfaceHeight.Value : num4);
			Vector3 v2 = Vector3.UnitY;
			if (m_subsystemOceanScanner != null && surfaceHeight.HasValue)
			{
				num8 = m_subsystemOceanScanner.GetBlockWaveSurfaceY(vector3, num8);
				v2 = m_subsystemOceanScanner.GetBlockWaveNormal(vector3, num8);
			}
			float num9 = MathUtils.Clamp(num8 - (vector3.Y - 0.5f), 0f, 1f);
			if (num9 > 0.01f)
			{
				num5++;
				Vector3 vector4 = Vector3.Lerp(Vector3.UnitY, v2, f);
				float num10 = num6 * num9 * (float)num3;
				Vector3 vector5 = vector4 * num10;
				if (num > 0.5f)
				{
					vector5.X *= 1f - num2;
					vector5.Z *= 1f - num2;
				}
				zero += vector5;
				Vector3 vector6 = vector3 - vector2;
				zero2 += Vector3.Cross(vector6, vector5);
				Vector3 vector7 = set.CurrentVelocity + Vector3.Cross(vector, vector6);
				float num11 = 4f * (1f + num * 2f);
				Vector3 vector8 = -vector7 * (num11 * num9 * (float)num3);
				zero += vector8;
				zero2 += Vector3.Cross(vector6, vector8);
			}
		}
		Vector3 vector9 = zero / value;
		if (vector9.LengthSquared() > 40000f)
		{
			vector9 = Vector3.Normalize(vector9) * 200f;
		}
		set.CurrentVelocity += vector9 * dt;
		float num12 = (float)Math.Pow(set.Blocks.Count, 0.33000001311302185) * 2f;
		float num13 = Math.Max(value * num12 * num12 * 0.05f * (1f + num * 10f), value);
		Vector3 vector10 = zero2 / num13;
		if (vector10.Length() > 10f)
		{
			vector10 = Vector3.Normalize(vector10) * 10f;
		}
		vector += vector10 * dt;
		set.CurrentVelocity *= MathUtils.Pow(0.99f - num * 0.01f, dt);
		vector *= MathUtils.Pow(x, dt);
		float num14 = vector.Length();
		if (num14 * dt > 0.0001f)
		{
			float num15 = MathUtils.Lerp(2.5f, 0.5f, num);
			if (num14 > num15)
			{
				vector = Vector3.Normalize(vector) * num15;
				num14 = num15;
			}
			Quaternion quaternion = Quaternion.CreateFromAxisAngle(Vector3.Normalize(vector), num14 * dt);
			set.Rotation = Quaternion.Normalize(set.Rotation * quaternion);
		}
		valuesDictionary.SetValue("AngularVelocity", vector);
	}

	private void HandleStopped(IMovingBlockSet mbs)
	{
		if (!(mbs is SubsystemMovingBlocks.MovingBlockSet movingBlockSet))
		{
			return;
		}
		if (movingBlockSet.Id == "WheelBlock")
		{
			if (movingBlockSet.CollisionGroupId == 0)
			{
				return;
			}
			{
				foreach (IMovingBlockSet movingBlockSet4 in m_subsystemMovingBlocks.MovingBlockSets)
				{
					if (movingBlockSet4 is SubsystemMovingBlocks.MovingBlockSet { Id: "FloatingBlock" } movingBlockSet2 && movingBlockSet2.CollisionGroupId == movingBlockSet.CollisionGroupId)
					{
						movingBlockSet2.Stop = true;
						break;
					}
				}
				return;
			}
		}
		if (movingBlockSet.Id != "FloatingBlock")
		{
			return;
		}
		DisassembleBlockSet(movingBlockSet);
		if (movingBlockSet.CollisionGroupId == 0)
		{
			return;
		}
		foreach (IMovingBlockSet item in new List<IMovingBlockSet>(m_subsystemMovingBlocks.MovingBlockSets))
		{
			if (item is SubsystemMovingBlocks.MovingBlockSet { Id: "WheelBlock" } movingBlockSet3 && movingBlockSet3.CollisionGroupId == movingBlockSet.CollisionGroupId)
			{
				DisassembleBlockSet(movingBlockSet3);
			}
		}
	}

	public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner)
	{
		if (Terrain.ExtractData(raycastResult.Value) == 1)
		{
			componentMiner.ComponentPlayer?.ComponentGui.DisplaySmallMessage("Ship is already active!", Color.White, blinking: false, playNotificationSound: false);
			return false;
		}
		Point3 startNode = new Point3(raycastResult.CellFace.X, raycastResult.CellFace.Y, raycastResult.CellFace.Z);
		List<Point3> fullShipBody = CollectFullShipVolume(startNode);
		if (componentMiner.ComponentPlayer != null)
		{
			componentMiner.ComponentPlayer.ComponentGui.DisplaySmallMessage($"Detected Giant Ship: {fullShipBody.Count} blocks", Color.Green, blinking: false, playNotificationSound: false);
			DialogsManager.ShowDialog(componentMiner.ComponentPlayer.GameWidget, new ShipConstructionDialog(componentMiner.ComponentPlayer, fullShipBody, m_subsystemTerrain, () =>
			{
				ActivateShip(fullShipBody);
			}));
		}
		return true;
	}

	private void ActivateShip(List<Point3> fullBody)
	{
		if (fullBody.Count == 0)
		{
			return;
		}
		foreach (Point3 item in fullBody)
		{
			if (Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(item.X, item.Y, item.Z)) == 434)
			{
				m_subsystemTerrain.ChangeCell(item.X, item.Y, item.Z, Terrain.MakeBlockValue(434, 0, 1));
			}
		}
		m_subsystemAudio.PlaySound("Audio/BlockPlaced", 1f, 0f, new Vector3(fullBody[0].X, fullBody[0].Y, fullBody[0].Z), 10f, autoDelay: true);
		double num = 0.0;
		Vector3 zero = Vector3.Zero;
		foreach (Point3 item2 in fullBody)
		{
			int cellValue = m_subsystemTerrain.Terrain.GetCellValue(item2.X, item2.Y, item2.Z);
			int num2 = Terrain.ExtractContents(cellValue);
			float density = BlocksManager.Blocks[num2].GetDensity(cellValue);
			num += (double)density;
			zero += new Vector3(item2) * density;
		}
		Vector3 zero2 = Vector3.Zero;
		ValuesDictionary valuesDictionary = new ValuesDictionary();
		valuesDictionary.SetValue("Mass", (float)num);
		valuesDictionary.SetValue("LocalCoM", zero2);
		valuesDictionary.SetValue("AngularVelocity", Vector3.Zero);
		StartFloatingFromList(fullBody, valuesDictionary);
	}

	private List<Point3> CollectFullShipVolume(Point3 startNode)
	{
		return FindBlocksAroundMarkers(startNode);
	}

	public void SetMovingBlockData(IMovingBlockSet mbs, Point3 offset, int newData)
	{
		if (!(mbs is SubsystemMovingBlocks.MovingBlockSet movingBlockSet))
		{
			return;
		}
		for (int i = 0; i < movingBlockSet.Blocks.Count; i++)
		{
			if (movingBlockSet.Blocks[i].Offset == offset)
			{
				MovingBlock movingBlock = movingBlockSet.Blocks[i];
				int value = Terrain.MakeBlockValue(Terrain.ExtractContents(movingBlock.Value), 0, newData);
				movingBlockSet.Blocks[i] = new MovingBlock
				{
					Offset = movingBlock.Offset,
					Value = value
				};
				movingBlockSet.GeometryGenerationPosition = new Point3(int.MaxValue);
				break;
			}
		}
	}

	private void UpdateWheelAnimation(SubsystemMovingBlocks.MovingBlockSet wheelSet, float dt)
	{
		SubsystemMovingBlocks.MovingBlockSet movingBlockSet = null;
		foreach (SubsystemMovingBlocks.MovingBlockSet movingBlockSet2 in m_subsystemMovingBlocks.MovingBlockSets)
		{
			if (movingBlockSet2.Id == "FloatingBlock" && movingBlockSet2.CollisionGroupId == wheelSet.CollisionGroupId)
			{
				movingBlockSet = movingBlockSet2;
				break;
			}
		}
		if (movingBlockSet == null)
		{
			return;
		}
		ValuesDictionary valuesDictionary = wheelSet.Tag as ValuesDictionary;
		if (valuesDictionary == null)
		{
			valuesDictionary = new ValuesDictionary();
			valuesDictionary.SetValue("Offset", wheelSet.Position - movingBlockSet.Position);
			valuesDictionary.SetValue("Angle", 0f);
			wheelSet.Tag = valuesDictionary;
		}
		Vector3 value = valuesDictionary.GetValue<Vector3>("Offset");
		float value2 = valuesDictionary.GetValue<float>("Angle");
		float num = 0f;
		if (wheelSet.Blocks.Count > 0)
		{
			int data = 0;
			foreach (MovingBlock block in wheelSet.Blocks)
			{
				if (Terrain.ExtractContents(block.Value) == 435)
				{
					data = Terrain.ExtractData(block.Value);
					break;
				}
			}
			switch (ShipWheelBlock.GetMode(data))
			{
			case ShipWheelBlock.WheelMode.Left:
				num = 3f;
				break;
			case ShipWheelBlock.WheelMode.Right:
				num = -3f;
				break;
			}
		}
		value2 += num * dt;
		if (value2 > (float)Math.PI * 2f)
		{
			value2 -= (float)Math.PI * 2f;
		}
		if (value2 < (float)Math.PI * -2f)
		{
			value2 += (float)Math.PI * 2f;
		}
		valuesDictionary.SetValue("Angle", value2);
		wheelSet.Position = movingBlockSet.Position + Vector3.Transform(value, movingBlockSet.Rotation);
		wheelSet.CurrentVelocity = movingBlockSet.CurrentVelocity;
		if (wheelSet.Blocks.Count <= 0)
		{
			return;
		}
		int data2 = 0;
		foreach (MovingBlock block2 in wheelSet.Blocks)
		{
			if (Terrain.ExtractContents(block2.Value) == 435)
			{
				data2 = Terrain.ExtractData(block2.Value);
				break;
			}
		}
		Quaternion quaternion = Quaternion.CreateFromAxisAngle(CellFace.FaceToVector3(ShipWheelBlock.GetMountFace(data2)), value2);
		wheelSet.Rotation = movingBlockSet.Rotation * quaternion;
	}

	private MovingEntityData ExtractEntityData(ComponentBlockEntity blockEntity, Point3 offset)
	{
		DatabaseObject databaseObject = blockEntity.Entity.ValuesDictionary.DatabaseObject;
		if (databaseObject != null)
		{
			ValuesDictionary valuesDictionary = new ValuesDictionary();
			valuesDictionary.PopulateFromDatabaseObject(databaseObject);
			ValuesDictionary valuesDictionary2 = new ValuesDictionary();
			blockEntity.Entity.InternalSaveEntity(valuesDictionary2, new EntityToIdMap(new Dictionary<Entity, int>()));
			valuesDictionary.ApplyOverrides(valuesDictionary2);
			ComponentBlockEntity componentBlockEntity = Project.CreateEntity(valuesDictionary).FindComponent<ComponentBlockEntity>();
			if (componentBlockEntity != null)
			{
				return new MovingEntityData
				{
					Entity = componentBlockEntity,
					BlockOffset = offset
				};
			}
		}
		return null;
	}

	private void DisassembleBlockSet(SubsystemMovingBlocks.MovingBlockSet mbs)
	{
		m_subsystemMovingBlocks.RemoveMovingBlockSet(mbs);
		Quaternion rotation = mbs.Rotation;
		float angle = MathUtils.Atan2(2f * rotation.Y * rotation.W - 2f * rotation.X * rotation.Z, 1f - 2f * rotation.Y * rotation.Y - 2f * rotation.Z * rotation.Z);
		Quaternion quaternion = Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle);
		Quaternion q = ((MathUtils.Abs(Quaternion.Dot(rotation, quaternion)) > 0.85f) ? quaternion : rotation);
		foreach (MovingBlock block in mbs.Blocks)
		{
			Vector3 vector = Vector3.Transform(new Vector3(block.Offset), q);
			Vector3 vector2 = mbs.Position + vector;
			int x = Terrain.ToCell(MathUtils.Round(vector2.X));
			int y = Terrain.ToCell(MathUtils.Round(vector2.Y));
			int z = Terrain.ToCell(MathUtils.Round(vector2.Z));
			int value = block.Value;
			if (Terrain.ExtractContents(value) == 435)
			{
				int data = Terrain.ExtractData(value);
				data = ShipWheelBlock.SetMode(data, ShipWheelBlock.WheelMode.Rest);
				value = Terrain.MakeBlockValue(435, 0, data);
			}
			if (Terrain.ExtractContents(value) == 434)
			{
				value = Terrain.MakeBlockValue(434, 0, 0);
			}
			m_subsystemTerrain.ChangeCell(x, y, z, value);
		}
		if (mbs.AttachedEntities != null)
		{
			foreach (MovingEntityData attachedEntity in mbs.AttachedEntities)
			{
				Vector3 vector3 = Vector3.Transform(new Vector3(attachedEntity.BlockOffset), q);
				Vector3 vector4 = mbs.Position + vector3;
				int x2 = Terrain.ToCell(MathUtils.Round(vector4.X));
				int y2 = Terrain.ToCell(MathUtils.Round(vector4.Y));
				int z2 = Terrain.ToCell(MathUtils.Round(vector4.Z));
				ComponentBlockEntity blockEntity = m_subsystemBlockEntities.GetBlockEntity(x2, y2, z2);
				ComponentBlockEntity entity = attachedEntity.Entity;
				if (blockEntity != null && entity != null)
				{
					blockEntity.Owner = entity.Owner;
					ComponentInventoryBase componentInventoryBase = entity.Entity.FindComponent<ComponentInventoryBase>();
					ComponentInventoryBase componentInventoryBase2 = blockEntity.Entity.FindComponent<ComponentInventoryBase>();
					if (componentInventoryBase != null && componentInventoryBase2 != null)
					{
						int num = Math.Min(componentInventoryBase.SlotsCount, componentInventoryBase2.SlotsCount);
						for (int i = 0; i < num; i++)
						{
							int slotValue = componentInventoryBase.GetSlotValue(i);
							int slotCount = componentInventoryBase.GetSlotCount(i);
							if (slotValue != 0 && slotCount > 0)
							{
								componentInventoryBase2.AddSlotItems(i, slotValue, slotCount);
							}
						}
					}
					ComponentFurnace componentFurnace = entity.Entity.FindComponent<ComponentFurnace>();
					ComponentFurnace componentFurnace2 = blockEntity.Entity.FindComponent<ComponentFurnace>();
					if (componentFurnace != null && componentFurnace2 != null)
					{
						componentFurnace2.m_fireTimeRemaining = componentFurnace.m_fireTimeRemaining;
						componentFurnace2.m_smeltingProgress = componentFurnace.m_smeltingProgress;
						componentFurnace2.m_heatLevel = componentFurnace.m_heatLevel;
						componentFurnace2.m_updateSmeltingRecipe = true;
						int cellValue = m_subsystemTerrain.Terrain.GetCellValue(x2, y2, z2);
						int num2 = ((componentFurnace2.m_heatLevel > 0f) ? 65 : 64);
						if (Terrain.ExtractContents(cellValue) != num2)
						{
							m_subsystemTerrain.ChangeCell(x2, y2, z2, Terrain.ReplaceContents(cellValue, num2));
						}
						if (CommonLib.WorkType != WorkType.Client)
						{
							ComponentFurnacePackage package = new ComponentFurnacePackage(componentFurnace2.Entity.EntityId, componentFurnace2.m_fireTimeRemaining, componentFurnace2.m_smeltingProgress, componentFurnace2.m_heatLevel);
							CommonLib.Net.QueuePackage(package);
						}
					}
					ComponentFurnace2 componentFurnace3 = entity.Entity.FindComponent<ComponentFurnace2>();
					ComponentFurnace2 componentFurnace4 = blockEntity.Entity.FindComponent<ComponentFurnace2>();
					if (componentFurnace3 != null && componentFurnace4 != null)
					{
						componentFurnace4.m_fireTimeRemaining = componentFurnace3.m_fireTimeRemaining;
						componentFurnace4.m_smeltingProgress = componentFurnace3.m_smeltingProgress;
						componentFurnace4.m_heatLevel = componentFurnace3.m_heatLevel;
						int cellValue2 = m_subsystemTerrain.Terrain.GetCellValue(x2, y2, z2);
						int activeBlockID = componentFurnace4.GetActiveBlockID();
						if (Terrain.ExtractContents(cellValue2) != activeBlockID)
						{
							m_subsystemTerrain.ChangeCell(x2, y2, z2, Terrain.ReplaceContents(cellValue2, activeBlockID));
						}
						if (CommonLib.WorkType != WorkType.Client)
						{
							ComponentFurnacePackage2 package2 = new ComponentFurnacePackage2(componentFurnace2.Entity.EntityId, componentFurnace2.m_fireTimeRemaining, componentFurnace2.m_smeltingProgress, componentFurnace2.m_heatLevel);
							CommonLib.Net.QueuePackage(package2);
						}
					}
					try
					{
						ValuesDictionary valuesDictionary = new ValuesDictionary();
						entity.Entity.InternalSaveEntity(valuesDictionary, new EntityToIdMap(new Dictionary<Entity, int>()));
						foreach (string key in valuesDictionary.Keys)
						{
							ValuesDictionary value2 = valuesDictionary.GetValue<ValuesDictionary>(key);
							if (value2 != null)
							{
								if (value2.ContainsKey("Position"))
								{
									value2.Remove("Position");
								}
								if (value2.ContainsKey("Coordinates"))
								{
									value2.Remove("Coordinates");
								}
							}
						}
						blockEntity.Entity.publicLoadEntity(valuesDictionary, new IdToEntityMap(new Dictionary<int, Entity>()));
					}
					catch (Exception ex)
					{
						Log.Warning("[Ships Warning] Generic entity transfer failed: " + ex.Message);
					}
				}
				if (entity.Entity.IsAddedToProject)
				{
					Project.RemoveEntity(entity.Entity, disposeEntity: true);
				}
			}
			mbs.AttachedEntities.Clear();
		}
		if (mbs.Blocks.Count > 0)
		{
			m_subsystemSoundMaterials.PlayImpactSound(mbs.Blocks[0].Value, mbs.Position, 1f);
		}
	}

	static SubsystemFloatingBlockBehavior()
	{
		m_neighborOffsets = new Point3[6]
		{
			new Point3(1, 0, 0),
			new Point3(-1, 0, 0),
			new Point3(0, 1, 0),
			new Point3(0, -1, 0),
			new Point3(0, 0, 1),
			new Point3(0, 0, -1)
		};
	}

	public void UpdateMovingBlockID(SubsystemMovingBlocks.MovingBlockSet mbs, Point3 offset, int newBlockID)
	{
		for (int i = 0; i < mbs.Blocks.Count; i++)
		{
			if (mbs.Blocks[i].Offset == offset)
			{
				MovingBlock movingBlock = mbs.Blocks[i];
				if (Terrain.ExtractContents(movingBlock.Value) != newBlockID)
				{
					int data = Terrain.ExtractData(movingBlock.Value);
					int value = Terrain.MakeBlockValue(newBlockID, 0, data);
					mbs.Blocks[i] = new MovingBlock
					{
						Offset = movingBlock.Offset,
						Value = value
					};
					mbs.GeometryGenerationPosition = new Point3(int.MaxValue);
				}
				break;
			}
		}
	}

	private void UpdateAttachedEntities(SubsystemMovingBlocks.MovingBlockSet shipSet, float dt)
	{
		if (shipSet.AttachedEntities == null)
		{
			return;
		}
		foreach (MovingEntityData attachedEntity in shipSet.AttachedEntities)
		{
			if (attachedEntity.Entity != null)
			{
				ProcessShipEntity(attachedEntity, shipSet, dt);
			}
		}
	}

	private void ProcessShipEntity(MovingEntityData data, SubsystemMovingBlocks.MovingBlockSet shipSet, float dt)
	{
		Entity entity = data.Entity.Entity;
		UpdateFurnace2Logic(entity, data, shipSet, dt);
		UpdateFurnaceLogic(entity, data, shipSet, dt);
	}

	private bool UpdateFurnace2Logic(Entity e, MovingEntityData data, SubsystemMovingBlocks.MovingBlockSet shipSet, float dt)
	{
		ComponentFurnace2 componentFurnace = e.FindComponent<ComponentFurnace2>();
		if (componentFurnace != null)
		{
			if (componentFurnace.TickMoving(dt))
			{
				int activeBlockID = componentFurnace.GetActiveBlockID();
				UpdateMovingBlockID(shipSet, data.BlockOffset, activeBlockID);
			}
			return true;
		}
		return false;
	}

	private bool UpdateFurnaceLogic(Entity e, MovingEntityData data, SubsystemMovingBlocks.MovingBlockSet shipSet, float dt)
	{
		ComponentFurnace componentFurnace = e.FindComponent<ComponentFurnace>();
		if (componentFurnace != null)
		{
			if (componentFurnace.TickMoving(dt))
			{
				int activeBlockID = componentFurnace.GetActiveBlockID();
				UpdateMovingBlockID(shipSet, data.BlockOffset, activeBlockID);
			}
			return true;
		}
		return false;
	}

	public void UpdateMovingBlockValue(SubsystemMovingBlocks.MovingBlockSet mbs, Point3 offset, int newValue)
	{
		if (mbs == null)
		{
			return;
		}
		bool flag = false;
		int num = -1;
		for (int i = 0; i < mbs.Blocks.Count; i++)
		{
			if (mbs.Blocks[i].Offset == offset)
			{
				if (mbs.Blocks[i].Value == newValue)
				{
					return;
				}
				mbs.Blocks[i] = new MovingBlock
				{
					Offset = offset,
					Value = newValue
				};
				flag = true;
				num = i;
				break;
			}
		}
		if (!flag)
		{
			return;
		}
		int num2 = Terrain.ExtractContents(newValue);
		if (num2 == 56 || num2 == 57 || num2 == 58)
		{
			int num3 = Terrain.ExtractData(newValue);
			bool open = DoorBlock.GetOpen(num3);
			Point3 point = (((num3 & 8) != 0) ? (offset - new Point3(0, 1, 0)) : (offset + new Point3(0, 1, 0)));
			for (int j = 0; j < mbs.Blocks.Count; j++)
			{
				if (j == num || !(mbs.Blocks[j].Offset == point))
				{
					continue;
				}
				int value = mbs.Blocks[j].Value;
				if (Terrain.ExtractContents(value) == num2)
				{
					int data = Terrain.ExtractData(value);
					if (DoorBlock.GetOpen(data) != open)
					{
						int data2 = DoorBlock.SetOpen(data, open);
						int value2 = Terrain.ReplaceData(value, data2);
						mbs.Blocks[j] = new MovingBlock
						{
							Offset = point,
							Value = value2
						};
					}
				}
				break;
			}
		}
		mbs.GeometryGenerationPosition = new Point3(int.MaxValue);
	}

	public void ModifyMovingBlock(SubsystemMovingBlocks.MovingBlockSet ship, Point3 offset, int newValue)
	{
		if (CommonLib.WorkType == WorkType.Client)
		{
			MovingBlockChangePackage package = new MovingBlockChangePackage(ship.CollisionGroupId, offset, newValue);
			CommonLib.Net.QueuePackage(package);
			return;
		}
		UpdateMovingBlockValue(ship, offset, newValue);
		if (CommonLib.WorkType == WorkType.Server)
		{
			MovingBlockChangePackage package2 = new MovingBlockChangePackage(ship.CollisionGroupId, offset, newValue);
			CommonLib.Net.QueuePackage(package2);
		}
	}

	private List<Point3> FindBlocksAroundMarkers(Point3 startNode)
	{
		HashSet<Point3> hashSet = new HashSet<Point3>();
		HashSet<Point3> hashSet2 = new HashSet<Point3>();
		Queue<Point3> queue = new Queue<Point3>();
		queue.Enqueue(startNode);
		hashSet2.Add(startNode);
		int num = 0;
		while (queue.Count > 0 && num < 10000)
		{
			Point3 point = queue.Dequeue();
			num++;
			int num2 = 25;
			for (int i = -num2; i <= num2; i++)
			{
				for (int j = -num2; j <= num2; j++)
				{
					for (int k = -num2; k <= num2; k++)
					{
						Point3 item = new Point3(point.X + i, point.Y + j, point.Z + k);
						if (hashSet.Contains(item))
						{
							continue;
						}
						int num3 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(item.X, item.Y, item.Z));
						if (num3 != 0 && num3 != 18 && num3 != 92 && num3 != 391)
						{
							if (num3 == 434 && !hashSet2.Contains(item))
							{
								hashSet2.Add(item);
								queue.Enqueue(item);
							}
							hashSet.Add(item);
						}
					}
				}
			}
		}
		return new List<Point3>(hashSet);
	}

	public void StartFloatingFromList(List<Point3> blocks, ValuesDictionary shipData)
	{
		if (blocks == null || blocks.Count == 0)
		{
			return;
		}
		List<Point3> playerGUID = new List<Point3>();
		List<Point3> list = new List<Point3>();
		foreach (Point3 block in blocks)
		{
			switch (Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(block.X, block.Y, block.Z)))
			{
			case 435:
				list.Add(block);
				break;
			default:
				playerGUID.Add(block);
				break;
			case 0:
				break;
			}
		}
		if (playerGUID.Count == 0 && list.Count == 0)
		{
			return;
		}
		Vector3 zero = Vector3.Zero;
		if (playerGUID.Count > 0)
		{
			foreach (Point3 item2 in playerGUID)
			{
				zero += new Vector3(item2);
			}
			zero /= (float)playerGUID.Count;
		}
		else
		{
			foreach (Point3 item3 in list)
			{
				zero += new Vector3(item3);
			}
			zero /= (float)list.Count;
		}
		Vector3 zero2 = Vector3.Zero;
		if (list.Count > 0)
		{
			foreach (Point3 item4 in list)
			{
				zero2 += new Vector3(item4);
			}
			zero2 /= (float)list.Count;
		}
		if (m_random == null)
		{
			m_random = new Random();
		}
		int collisionGroupId = m_random.Int(1, int.MaxValue);
		List<MovingBlock> list2 = new List<MovingBlock>();
		List<MovingEntityData> list3 = new List<MovingEntityData>();
		List<MovingBlock> list4 = new List<MovingBlock>();
		List<MovingEntityData> list5 = new List<MovingEntityData>();
		foreach (Point3 block2 in blocks)
		{
			int cellValue = m_subsystemTerrain.Terrain.GetCellValue(block2.X, block2.Y, block2.Z);
			int num = Terrain.ExtractContents(cellValue);
			if (num == 0)
			{
				continue;
			}
			ComponentBlockEntity blockEntity = m_subsystemBlockEntities.GetBlockEntity(block2.X, block2.Y, block2.Z);
			MovingEntityData movingEntityData = null;
			Point3 offset = ((num != 435) ? (block2 - new Point3((int)zero.X, (int)zero.Y, (int)zero.Z)) : (block2 - new Point3((int)zero2.X, (int)zero2.Y, (int)zero2.Z)));
			if (blockEntity != null)
			{
				movingEntityData = ExtractEntityData(blockEntity, offset);
				m_subsystemBlockEntities.DetachBlockEntity(block2.X, block2.Y, block2.Z);
				Project.RemoveEntity(blockEntity.Entity, disposeEntity: true);
			}
			MovingBlock item = new MovingBlock
			{
				Value = cellValue,
				Offset = offset
			};
			if (num == 435)
			{
				list4.Add(item);
				if (movingEntityData != null)
				{
					list5.Add(movingEntityData);
				}
			}
			else
			{
				list2.Add(item);
				if (movingEntityData != null)
				{
					list3.Add(movingEntityData);
				}
			}
		}
		try
		{
			Vector3 targetPosition = zero - new Vector3(0f, 1000f, 0f);
			if (list2.Count > 0)
			{
				m_subsystemMovingBlocks.AddMovingBlockSet(zero, targetPosition, 0f, 0f, 0f, Vector2.Zero, list2, list3, "FloatingBlock", shipData, collisionGroupId, testCollision: true);
			}
			if (list4.Count > 0)
			{
				Vector3 value = zero2 - zero;
				ValuesDictionary valuesDictionary = new ValuesDictionary();
				valuesDictionary.SetValue("Offset", value);
				valuesDictionary.SetValue("Angle", 0f);
				m_subsystemMovingBlocks.AddMovingBlockSet(zero2, targetPosition, 0f, 0f, 0f, Vector2.Zero, list4, list5, "WheelBlock", valuesDictionary, collisionGroupId, testCollision: true);
			}
			foreach (Point3 block3 in blocks)
			{
				m_subsystemTerrain.ChangeCell(block3.X, block3.Y, block3.Z, 0);
			}
		}
		catch (Exception ex)
		{
			Log.Error("Failed to create ship: " + ex.Message);
		}
	}
}
