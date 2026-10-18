using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Engine;
using Engine.Graphics;
using Engine.Serialization;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemMovingBlocks : Subsystem, IUpdateable, IDrawable
{
	public class MovingBlockSet : IMovingBlockSet
	{
		public string Id;

		public object Tag;

		public bool Stop;

		public int RemainCounter;

		public Vector3 Position;

		public Vector3 StartPosition;

		public Vector3 TargetPosition;

		public float Speed;

		public float Acceleration;

		public float Drag;

		public Vector2 Smoothness;

		public List<MovingBlock> Blocks;

		public Box Box;

		public Vector3 CurrentVelocity;

		public TerrainGeometry Geometry;

		public DynamicArray<TerrainVertex> Vertices;

		public DynamicArray<int> Indices;

		public Vector3 GeometryOffset;

		public Point3 GeometryGenerationPosition;

		public Vector3 ExternalForces;

		public Quaternion Rotation;

		public List<MovingEntityData> AttachedEntities = new List<MovingEntityData>();

		public int CollisionGroupId;

		public NetPosition NetPosition;

		public NetRotation NetRotation;

		public NetVelocity NetVelocity;

		public float LightUpdateTimer;

		public Point3 LastLightPosition;

		Vector3 IMovingBlockSet.Position => Position;

		string IMovingBlockSet.Id => Id;

		object IMovingBlockSet.Tag => Tag;

		Vector3 IMovingBlockSet.CurrentVelocity => CurrentVelocity;

		ReadOnlyList<MovingBlock> IMovingBlockSet.Blocks => new ReadOnlyList<MovingBlock>(Blocks);

		Quaternion IMovingBlockSet.Rotation
		{
			get
			{
				return Rotation;
			}
			set
			{
				Rotation = value;
			}
		}

		public MovingBlockSet()
		{
			Rotation = Quaternion.Identity;
			ExternalForces = Vector3.Zero;
			Rotation = Quaternion.Identity;
			Vertices = new DynamicArray<TerrainVertex>();
			Indices = new DynamicArray<int>();
			GeometryGenerationPosition = new Point3(int.MaxValue);
			TerrainGeometrySubset terrainGeometrySubset = new TerrainGeometrySubset(Vertices, Indices);
			Geometry = new TerrainGeometry();
			Geometry.SubsetOpaque = terrainGeometrySubset;
			Geometry.SubsetAlphaTest = terrainGeometrySubset;
			Geometry.SubsetTransparent = terrainGeometrySubset;
			Geometry.OpaqueSubsetsByFace = new TerrainGeometrySubset[6] { terrainGeometrySubset, terrainGeometrySubset, terrainGeometrySubset, terrainGeometrySubset, terrainGeometrySubset, terrainGeometrySubset };
			Geometry.AlphaTestSubsetsByFace = new TerrainGeometrySubset[6] { terrainGeometrySubset, terrainGeometrySubset, terrainGeometrySubset, terrainGeometrySubset, terrainGeometrySubset, terrainGeometrySubset };
			Geometry.TransparentSubsetsByFace = new TerrainGeometrySubset[6] { terrainGeometrySubset, terrainGeometrySubset, terrainGeometrySubset, terrainGeometrySubset, terrainGeometrySubset, terrainGeometrySubset };
			NetPosition = new NetPosition(Vector3.Zero);
			NetRotation = new NetRotation(Quaternion.Identity);
			NetVelocity = new NetVelocity(Vector3.Zero);
		}

		public void UpdateBox()
		{
			Point3? point = null;
			Point3? point2 = null;
			foreach (MovingBlock block in Blocks)
			{
				point = (point.HasValue ? Point3.Min(point.Value, block.Offset) : block.Offset);
				point2 = (point2.HasValue ? Point3.Max(point2.Value, block.Offset) : block.Offset);
			}
			if (point.HasValue)
			{
				Box = new Box(point.Value.X, point.Value.Y, point.Value.Z, point2.Value.X - point.Value.X + 1, point2.Value.Y - point.Value.Y + 1, point2.Value.Z - point.Value.Z + 1);
			}
			else
			{
				Box = default;
			}
		}

		public BoundingBox BoundingBox(bool extendToFillCells)
		{
			Vector3 min = new Vector3(Position.X + (float)Box.Left, Position.Y + (float)Box.Top, Position.Z + (float)Box.Near);
			Vector3 max = new Vector3(Position.X + (float)Box.Right, Position.Y + (float)Box.Bottom, Position.Z + (float)Box.Far);
			if (extendToFillCells)
			{
				min.X = MathUtils.Floor(min.X);
				min.Y = MathUtils.Floor(min.Y);
				min.Z = MathUtils.Floor(min.Z);
				max.X = MathUtils.Ceiling(max.X);
				max.Y = MathUtils.Ceiling(max.Y);
				max.Z = MathUtils.Ceiling(max.Z);
			}
			return new BoundingBox(min, max);
		}

		void IMovingBlockSet.SetBlock(Point3 offset, int value)
		{
			Blocks.RemoveAll((MovingBlock b) => b.Offset == offset);
			if (value != 0)
			{
				Blocks.Add(new MovingBlock
				{
					Offset = offset,
					Value = value
				});
			}
			UpdateBox();
			GeometryGenerationPosition = new Point3(int.MaxValue);
		}

		void IMovingBlockSet.Stop()
		{
			Stop = true;
		}
	}

	public SubsystemTime m_subsystemTime;

	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemSky m_subsystemSky;

	public SubsystemAnimatedTextures m_subsystemAnimatedTextures;

	public List<MovingBlockSet> m_movingBlockSets = new List<MovingBlockSet>();

	public List<MovingBlockSet> m_stopped = new List<MovingBlockSet>();

	public List<MovingBlockSet> m_removing = new List<MovingBlockSet>();

	public DynamicArray<TerrainVertex> m_vertices = new DynamicArray<TerrainVertex>();

	public DynamicArray<int> m_indices = new DynamicArray<int>();

	public DynamicArray<IMovingBlockSet> m_result = new DynamicArray<IMovingBlockSet>();

	public Shader m_shader;

	public BlockGeometryGenerator m_blockGeometryGenerator;

	public bool m_canGenerateGeometry;

	public static int[] m_drawOrders = new int[1] { 150 };

	private const string LOG_TAG = "[MovingBlocks DEBUG]";

	private double m_lastSyncTime;

	private const double SyncInterval = 0.2;

	private bool m_clientRequestedSync;

	public int m_generatorCapacity;

	public IReadOnlyList<IMovingBlockSet> MovingBlockSets => m_movingBlockSets;

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public int[] DrawOrders => m_drawOrders;

	public event Action<IMovingBlockSet, Point3> CollidedWithTerrain;

	public event Action<IMovingBlockSet> Stopped;

	public IMovingBlockSet AddMovingBlockSet(Vector3 position, Vector3 targetPosition, float speed, float acceleration, float drag, Vector2 smoothness, IEnumerable<MovingBlock> blocks, string id, object tag, bool testCollision)
	{
		return AddMovingBlockSet(position, targetPosition, speed, acceleration, drag, smoothness, blocks, null, id, tag, 0, testCollision);
	}

	public void RemoveMovingBlockSet(IMovingBlockSet movingBlockSet)
	{
		if (CommonLib.WorkType == WorkType.Client)
		{
			return;
		}
		RemoveMovingBlockSetLogic(movingBlockSet);
		try
		{
			CommonLib.Net.QueuePackage(new MovingBlockPackage(movingBlockSet, stop: false));
		}
		catch (Exception ex)
		{
			Log.Warning("[MovingBlocks] Failed to queue remove package: " + ex.Message);
		}
	}

	public void RemoveMovingBlockSetLogic(IMovingBlockSet movingBlockSet)
	{
		MovingBlockSet movingBlockSet2 = (MovingBlockSet)movingBlockSet;
		if (m_movingBlockSets.Remove(movingBlockSet2))
		{
			m_removing.Add(movingBlockSet2);
			movingBlockSet2.RemainCounter = 4;
		}
	}

	public void FindMovingBlocks(BoundingBox boundingBox, bool extendToFillCells, DynamicArray<IMovingBlockSet> result)
	{
		foreach (MovingBlockSet isWaterLogged in m_movingBlockSets)
		{
			if (ExclusiveBoxIntersection(boundingBox, isWaterLogged.BoundingBox(extendToFillCells)))
			{
				result.Add(isWaterLogged);
			}
		}
	}

	public IMovingBlockSet FindMovingBlocks(string id, object tag)
	{
		foreach (MovingBlockSet movingBlockSet in m_movingBlockSets)
		{
			if (movingBlockSet.Id == id && object.Equals(movingBlockSet.Tag, tag))
			{
				return movingBlockSet;
			}
		}
		return null;
	}

	public MovingBlocksRaycastResult? Raycast(Vector3 start, Vector3 end, bool extendToFillCells)
	{
		Ray3 num = new Ray3(start, Vector3.Normalize(end - start));
		BoundingBox block = new BoundingBox(Vector3.Min(start, end), Vector3.Max(start, end));
		m_result.Clear();
		FindMovingBlocks(block, extendToFillCells, m_result);
		float num2 = float.MaxValue;
		MovingBlockSet movingBlockSet = null;
		foreach (MovingBlockSet item in m_result)
		{
			BoundingBox box = item.BoundingBox(extendToFillCells);
			float? num3 = num.Intersection(box);
			if (num3.HasValue && num3.Value < num2)
			{
				num2 = num3.Value;
				movingBlockSet = item;
			}
		}
		if (movingBlockSet != null)
		{
			return new MovingBlocksRaycastResult
			{
				Ray = num,
				Distance = num2,
				MovingBlockSet = movingBlockSet
			};
		}
		return null;
	}

	public void Update(float dt)
	{
		if (CommonLib.WorkType == WorkType.Client && !m_clientRequestedSync)
		{
			m_clientRequestedSync = true;
			CommonLib.Net.QueuePackage(new MovingBlockPackage(isRequest: true));
		}
		m_canGenerateGeometry = true;
		foreach (MovingBlockSet movingBlockSet2 in m_movingBlockSets)
		{
			if (movingBlockSet2 == null)
			{
				continue;
			}
			int chunkX = Terrain.ToCell(movingBlockSet2.Position.X) >> 4;
			int chunkZ = Terrain.ToCell(movingBlockSet2.Position.Z) >> 4;
			TerrainChunk chunkAtCoords = m_subsystemTerrain.Terrain.GetChunkAtCoords(chunkX, chunkZ);
			if (chunkAtCoords == null || chunkAtCoords.State <= TerrainChunkState.InvalidContents4)
			{
				continue;
			}
			movingBlockSet2.LightUpdateTimer += dt;
			Point3 point = new Point3((int)movingBlockSet2.Position.X, (int)movingBlockSet2.Position.Y, (int)movingBlockSet2.Position.Z);
			int num = Math.Abs(point.X - movingBlockSet2.LastLightPosition.X) + Math.Abs(point.Y - movingBlockSet2.LastLightPosition.Y) + Math.Abs(point.Z - movingBlockSet2.LastLightPosition.Z);
			if (movingBlockSet2.LightUpdateTimer > 0.5f && num > 2)
			{
				movingBlockSet2.GeometryGenerationPosition = new Point3(int.MaxValue);
				movingBlockSet2.LightUpdateTimer = 0f;
			}
			float num2 = movingBlockSet2.Position.Y + (float)movingBlockSet2.Box.Top;
			float num3 = num2 + (float)movingBlockSet2.Box.Height;
			if (num2 <= 1f || num3 >= 255f)
			{
				movingBlockSet2.Stop = true;
				movingBlockSet2.CurrentVelocity = Vector3.Zero;
			}
			if (movingBlockSet2.Position.Y > 260f)
			{
				movingBlockSet2.Position.Y = 260f;
				if (movingBlockSet2.CurrentVelocity.Y > 0f)
				{
					movingBlockSet2.CurrentVelocity.Y = 0f;
				}
			}
			if (movingBlockSet2.Id == "FloatingBlock")
			{
				MovingBlockSet movingBlockSet = movingBlockSet2;
				movingBlockSet.CurrentVelocity.Y = movingBlockSet.CurrentVelocity.Y - 10f * dt;
				movingBlockSet2.Position += movingBlockSet2.CurrentVelocity * dt;
				if (!movingBlockSet2.Stop)
				{
					movingBlockSet2.Stop = false;
				}
				MovingBlocksCollision(movingBlockSet2);
				TerrainCollision(movingBlockSet2);
				if (movingBlockSet2.Stop)
				{
					m_stopped.Add(movingBlockSet2);
				}
			}
			else
			{
				Vector3 externalForces = movingBlockSet2.ExternalForces;
				movingBlockSet2.Speed += (movingBlockSet2.Acceleration * Vector3.Normalize(movingBlockSet2.TargetPosition - movingBlockSet2.Position) + externalForces).Length() * m_subsystemTime.GameTimeDelta;
				if (movingBlockSet2.Drag != 0f)
				{
					movingBlockSet2.Speed *= MathUtils.Pow(1f - movingBlockSet2.Drag, m_subsystemTime.GameTimeDelta);
				}
				float x = Vector3.Distance(movingBlockSet2.StartPosition, movingBlockSet2.Position);
				float num4 = Vector3.Distance(movingBlockSet2.TargetPosition, movingBlockSet2.Position);
				float num5 = ((movingBlockSet2.Smoothness.X > 0f) ? MathUtils.Saturate((MathUtils.Sqrt(x) + 0.05f) / movingBlockSet2.Smoothness.X) : 1f);
				float num6 = ((movingBlockSet2.Smoothness.Y > 0f) ? MathUtils.Saturate((num4 + 0.05f) / movingBlockSet2.Smoothness.Y) : 1f);
				float num7 = num5 * num6;
				float x2 = ((m_subsystemTime.GameTimeDelta > 0f) ? (0.95f / m_subsystemTime.GameTimeDelta) : 0f);
				float num8 = MathUtils.Min(movingBlockSet2.Speed * num7, x2);
				if (num8 * m_subsystemTime.GameTimeDelta >= num4)
				{
					movingBlockSet2.Position = movingBlockSet2.TargetPosition;
					movingBlockSet2.CurrentVelocity = Vector3.Zero;
				}
				else if (num4 > 0f)
				{
					movingBlockSet2.CurrentVelocity = num8 / num4 * (movingBlockSet2.TargetPosition - movingBlockSet2.Position) + externalForces;
					movingBlockSet2.Position += movingBlockSet2.CurrentVelocity * m_subsystemTime.GameTimeDelta;
				}
				movingBlockSet2.Stop = false;
				MovingBlocksCollision(movingBlockSet2);
				TerrainCollision(movingBlockSet2);
				if ((movingBlockSet2.Stop || Vector3.Distance(movingBlockSet2.Position, movingBlockSet2.TargetPosition) < 0.001f) && movingBlockSet2.Position.Y >= 0f && movingBlockSet2.Position.Y < 255f)
				{
					m_stopped.Add(movingBlockSet2);
				}
			}
			bool flag = movingBlockSet2.Id == "FloatingBlock" || movingBlockSet2.Id == "WheelBlock";
			if ((CommonLib.WorkType == WorkType.Client && movingBlockSet2.NetPosition != null) & flag)
			{
				Vector3 v = movingBlockSet2.NetPosition.Get(dt);
				Quaternion q = movingBlockSet2.NetRotation.Get(dt);
				if (Vector3.Distance(movingBlockSet2.Position, v) > 0.1f)
				{
					float f = 0.5f * dt;
					movingBlockSet2.Position = Vector3.Lerp(movingBlockSet2.Position, v, f);
					movingBlockSet2.Rotation = Quaternion.Slerp(movingBlockSet2.Rotation, q, f);
					movingBlockSet2.CurrentVelocity = Vector3.Lerp(movingBlockSet2.CurrentVelocity, movingBlockSet2.NetVelocity.Get(dt), f);
				}
			}
			if (movingBlockSet2.AttachedEntities == null || movingBlockSet2.AttachedEntities.Count <= 0)
			{
				continue;
			}
			Matrix m = Matrix.CreateFromQuaternion(movingBlockSet2.Rotation);
			foreach (MovingEntityData attachedEntity in movingBlockSet2.AttachedEntities)
			{
				if (attachedEntity.Entity != null && attachedEntity.Entity.Entity != null)
				{
					Vector3 vector = Vector3.Transform(new Vector3(attachedEntity.BlockOffset.X, attachedEntity.BlockOffset.Y, attachedEntity.BlockOffset.Z), m);
					attachedEntity.Entity.Position = movingBlockSet2.Position + vector;
				}
			}
		}
		if (CommonLib.WorkType != WorkType.Client)
		{
			foreach (MovingBlockSet item in m_stopped)
			{
				try
				{
					DoStop(item);
				}
				catch (Exception ex)
				{
					Log.Warning($"[MovingBlocks] Cannot stop ship at Y={item.Position.Y}. It will remain moving. Error: {ex.Message}");
				}
				try
				{
					CommonLib.Net.QueuePackage(new MovingBlockPackage(item, isSyncDummy: true));
				}
				catch
				{
				}
			}
		}
		if (CommonLib.WorkType == WorkType.Server && Time.RealTime - m_lastSyncTime > 0.2)
		{
			m_lastSyncTime = Time.RealTime;
			foreach (MovingBlockSet movingBlockSet3 in m_movingBlockSets)
			{
				if (movingBlockSet3.Id == "FloatingBlock" || movingBlockSet3.Id == "WheelBlock")
				{
					MovingBlockPackage package = new MovingBlockPackage(movingBlockSet3, isSyncDummy: false);
					CommonLib.Net.QueuePackage(package);
				}
			}
		}
		m_stopped.Clear();
	}

	public void DoStop(MovingBlockSet item)
	{
		Stopped?.Invoke(item);
	}

	public void Draw(Camera camera, int drawOrder)
	{
		Display.BlendState = BlendState.NonPremultiplied;
		Display.DepthStencilState = DepthStencilState.Default;
		Display.RasterizerState = RasterizerState.CullNoneScissor;
		m_shader.GetParameter("u_texture").SetValue(m_subsystemAnimatedTextures.AnimatedBlocksTexture);
		m_shader.GetParameter("u_samplerState").SetValue(SamplerState.PointClamp);
		m_shader.GetParameter("u_fogColor").SetValue(new Vector3(m_subsystemSky.ViewFogColor));
		m_shader.GetParameter("u_fogStartInvLength").SetValue(new Vector2(m_subsystemSky.ViewFogRange.X, 1f / (m_subsystemSky.ViewFogRange.Y - m_subsystemSky.ViewFogRange.X)));
		m_shader.GetParameter("u_origin").SetValue(Vector2.Zero);
		Vector3 viewPosition = camera.ViewPosition;
		float num = m_subsystemSky.ViewFogRange.Y + 32f;
		num *= num;
		foreach (MovingBlockSet movingBlockSet2 in m_movingBlockSets)
		{
			int x = Terrain.ToCell(movingBlockSet2.Position.X);
			int z = Terrain.ToCell(movingBlockSet2.Position.Z);
			TerrainChunk chunkAtCell = m_subsystemTerrain.Terrain.GetChunkAtCell(x, z);
			if (chunkAtCell == null || chunkAtCell.State <= TerrainChunkState.InvalidContents4 || !(Vector3.DistanceSquared(movingBlockSet2.Position, viewPosition) <= num))
			{
				continue;
			}
			int x2 = Terrain.ToCell(movingBlockSet2.Position.X);
			int z2 = Terrain.ToCell(movingBlockSet2.Position.Z);
			TerrainChunk chunkAtCell2 = m_subsystemTerrain.Terrain.GetChunkAtCell(x2, z2);
			if (chunkAtCell2 == null || chunkAtCell2.State <= TerrainChunkState.InvalidContents4)
			{
				continue;
			}
			m_vertices.Clear();
			m_indices.Clear();
			try
			{
				DrawMovingBlockSet(camera, movingBlockSet2);
			}
			catch (Exception)
			{
			}
			if (m_vertices.Count <= 0)
			{
				continue;
			}
			if (movingBlockSet2.Rotation == default(Quaternion))
			{
				movingBlockSet2.Rotation = Quaternion.Identity;
			}
			Vector3 vector;
			if (movingBlockSet2.Id == "WheelBlock" && movingBlockSet2.Blocks.Count > 0)
			{
				Point3 offset = movingBlockSet2.Blocks[0].Offset;
				foreach (MovingBlock block in movingBlockSet2.Blocks)
				{
					if (Terrain.ExtractContents(block.Value) == 435)
					{
						offset = block.Offset;
						break;
					}
				}
				float x3 = (float)(offset.X - movingBlockSet2.Box.Left) + 1.5f;
				float y = (float)(offset.Y - movingBlockSet2.Box.Top) + 1.5f;
				float z3 = (float)(offset.Z - movingBlockSet2.Box.Near) + 1.5f;
				vector = new Vector3(x3, y, z3);
			}
			else
			{
				vector = -movingBlockSet2.GeometryOffset;
			}
			Matrix value = Matrix.CreateTranslation(-vector) * Matrix.CreateFromQuaternion(movingBlockSet2.Rotation) * Matrix.CreateTranslation(vector) * Matrix.CreateTranslation(movingBlockSet2.Position + movingBlockSet2.GeometryOffset - camera.ViewPosition) * camera.ViewMatrix.OrientationMatrix * camera.ProjectionMatrix;
			m_shader.GetParameter("u_viewProjectionMatrix").SetValue(value);
			m_shader.GetParameter("u_viewPosition").SetValue(Vector3.Zero);
			VertexBuffer vertexBuffer = new VertexBuffer(TerrainVertex.VertexDeclaration, m_vertices.Count);
			IndexBuffer indexBuffer = new IndexBuffer(IndexFormat.ThirtyTwoBits, m_indices.Count);
			vertexBuffer.SetData(m_vertices.Array, 0, m_vertices.Count);
			indexBuffer.SetData(m_indices.Array, 0, m_indices.Count);
			Display.DrawIndexed(PrimitiveType.TriangleList, m_shader, vertexBuffer, indexBuffer, 0, indexBuffer.IndicesCount);
			vertexBuffer.Dispose();
			indexBuffer.Dispose();
		}
		int num2 = 0;
		while (num2 < m_removing.Count)
		{
			MovingBlockSet movingBlockSet = m_removing[num2];
			if (movingBlockSet.RemainCounter > 0)
			{
				movingBlockSet.RemainCounter--;
				num2++;
			}
			else
			{
				m_removing.RemoveAt(num2);
			}
		}
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemSky = Project.FindSubsystem<SubsystemSky>(throwOnError: true);
		m_subsystemAnimatedTextures = Project.FindSubsystem<SubsystemAnimatedTextures>(throwOnError: true);
		m_shader = ContentManager.Get<Shader>("Shaders/AlphaTested");
		if (valuesDictionary.ContainsKey("MovingBlockSets"))
		{
			ValuesDictionary value = valuesDictionary.GetValue<ValuesDictionary>("MovingBlockSets");
			Log.Information(string.Format("{0} Found {1} MovingBlockSets in save file.", "[MovingBlocks DEBUG]", value.Count));
			{
				foreach (ValuesDictionary value2 in value.Values)
				{
					try
					{
						LoadAndAddMovingItem(value2);
					}
					catch (Exception ex)
					{
						Log.Error("[MovingBlocks DEBUG] CRITICAL ERROR loading a MovingBlockSet: " + ex.Message + "\n" + ex.StackTrace);
					}
				}
				return;
			}
		}
		Log.Warning("[MovingBlocks DEBUG] No 'MovingBlockSets' key found in Load.");
	}

	public override void Save(ValuesDictionary valuesDictionary)
	{
		ValuesDictionary valuesDictionary2 = new ValuesDictionary();
		valuesDictionary.SetValue("MovingBlockSets", valuesDictionary2);
		int num = 0;
		foreach (MovingBlockSet movingBlockSet in m_movingBlockSets)
		{
			if (movingBlockSet.AttachedEntities != null && movingBlockSet.AttachedEntities.Count > 0)
			{
				Log.Information(string.Format("{0} Saving BlockSet {1} (ID: {2}) with {3} entities.", "[MovingBlocks DEBUG]", num, movingBlockSet.Id, movingBlockSet.AttachedEntities.Count));
			}
			valuesDictionary2.SetValue(num++.ToString(CultureInfo.InvariantCulture), SaveMovingItem(movingBlockSet));
		}
	}

	public IMovingBlockSet LoadAndAddMovingItem(ValuesDictionary value9)
	{
		Vector3 value10 = value9.GetValue<Vector3>("Position");
		Vector3 value11 = value9.GetValue<Vector3>("TargetPosition");
		float value12 = value9.GetValue<float>("Speed");
		float value13 = value9.GetValue<float>("Acceleration");
		float value14 = value9.GetValue<float>("Drag");
		Vector2 value15 = value9.GetValue("Smoothness", Vector2.Zero);
		string value16 = value9.GetValue<string>("Id", null);
		object value17 = value9.GetValue<object>("Tag", null);
		int value18 = value9.GetValue("CollisionGroupId", 0);
		List<MovingBlock> list = new List<MovingBlock>();
		string[] array = value9.GetValue<string>("Blocks").Split(new char[1] { ';' }, StringSplitOptions.RemoveEmptyEntries);
		foreach (string obj in array)
		{
			MovingBlock item = default;
			string[] array2 = obj.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			item.Value = HumanReadableConverter.ConvertFromString<int>(array2[0]);
			item.Offset.X = HumanReadableConverter.ConvertFromString<int>(array2[1]);
			item.Offset.Y = HumanReadableConverter.ConvertFromString<int>(array2[2]);
			item.Offset.Z = HumanReadableConverter.ConvertFromString<int>(array2[3]);
			list.Add(item);
		}
		List<MovingEntityData> list2 = new List<MovingEntityData>();
		if (value9.ContainsKey("AttachedEntities"))
		{
			ValuesDictionary value19 = value9.GetValue<ValuesDictionary>("AttachedEntities");
			Log.Information($"[MovingBlocks DEBUG] Found 'AttachedEntities' for ID '{value16}'. Count: {value19.Count}");
			foreach (object value22 in value19.Values)
			{
				try
				{
					ValuesDictionary valuesDictionary = (ValuesDictionary)value22;
					Point3 value20 = valuesDictionary.GetValue<Point3>("Offset");
					ValuesDictionary value21 = valuesDictionary.GetValue<ValuesDictionary>("Data");
					string text = value21.GetValue<string>("TemplateName", null);
					if (string.IsNullOrEmpty(text))
					{
						Log.Warning($"[MovingBlocks DEBUG] TemplateName missing for entity at offset {value20}. Defaulting to 'Chest'.");
						text = "Chest";
					}
					else
					{
						Log.Information($"[MovingBlocks DEBUG] Loading attached entity '{text}' at offset {value20}");
					}
					DatabaseObject databaseObject = Project.GameDatabase.Database.FindDatabaseObject(text, Project.GameDatabase.EntityTemplateType, throwIfNotFound: false);
					if (databaseObject == null)
					{
						Log.Error("[MovingBlocks DEBUG] FAILED to find DatabaseObject for template '" + text + "'. Entity will NOT be created!");
						continue;
					}
					ValuesDictionary valuesDictionary2 = new ValuesDictionary();
					valuesDictionary2.PopulateFromDatabaseObject(databaseObject);
					valuesDictionary2.ApplyOverrides(value21);
					Entity entity = Project.CreateEntity(valuesDictionary2);
					if (entity != null)
					{
						ComponentBlockEntity componentBlockEntity = entity.FindComponent<ComponentBlockEntity>();
						if (componentBlockEntity != null)
						{
							list2.Add(new MovingEntityData
							{
								Entity = componentBlockEntity,
								BlockOffset = value20
							});
							Log.Information("[MovingBlocks DEBUG] Successfully added '" + text + "' to MovingBlockSet.");
							continue;
						}
						string text2 = "";
						foreach (Component component in entity.Components)
						{
							text2 = text2 + component.GetType().Name + ", ";
						}
						Log.Error("[MovingBlocks DEBUG] Entity '" + text + "' created, but ComponentBlockEntity is MISSING. Found components: [" + text2 + "]");
					}
					else
					{
						Log.Error("[MovingBlocks DEBUG] Project.CreateEntity returned NULL for '" + text + "'.");
					}
				}
				catch (Exception ex)
				{
					Log.Error("[MovingBlocks DEBUG] Exception while loading entity: " + ex.Message);
				}
			}
		}
		return AddMovingBlockSet(value10, value11, value12, value13, value14, value15, list, list2, value16, value17, value18, testCollision: false);
	}

	public static ValuesDictionary SaveMovingItem(MovingBlockSet movingBlockSet)
	{
		ValuesDictionary valuesDictionary = new ValuesDictionary();
		valuesDictionary.SetValue("Position", movingBlockSet.Position);
		valuesDictionary.SetValue("TargetPosition", movingBlockSet.TargetPosition);
		valuesDictionary.SetValue("Speed", movingBlockSet.Speed);
		valuesDictionary.SetValue("Acceleration", movingBlockSet.Acceleration);
		valuesDictionary.SetValue("Drag", movingBlockSet.Drag);
		if (movingBlockSet.CollisionGroupId != 0)
		{
			valuesDictionary.SetValue("CollisionGroupId", movingBlockSet.CollisionGroupId);
		}
		if (movingBlockSet.Smoothness != Vector2.Zero)
		{
			valuesDictionary.SetValue("Smoothness", movingBlockSet.Smoothness);
		}
		if (movingBlockSet.Id != null)
		{
			valuesDictionary.SetValue("Id", movingBlockSet.Id);
		}
		if (movingBlockSet.Tag != null)
		{
			valuesDictionary.SetValue("Tag", movingBlockSet.Tag);
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (MovingBlock block in movingBlockSet.Blocks)
		{
			stringBuilder.Append(HumanReadableConverter.ConvertToString(block.Value));
			stringBuilder.Append(',');
			stringBuilder.Append(HumanReadableConverter.ConvertToString(block.Offset.X));
			stringBuilder.Append(',');
			stringBuilder.Append(HumanReadableConverter.ConvertToString(block.Offset.Y));
			stringBuilder.Append(',');
			stringBuilder.Append(HumanReadableConverter.ConvertToString(block.Offset.Z));
			stringBuilder.Append(';');
		}
		valuesDictionary.SetValue("Blocks", stringBuilder.ToString());
		if (movingBlockSet.AttachedEntities != null && movingBlockSet.AttachedEntities.Count > 0)
		{
			ValuesDictionary valuesDictionary2 = new ValuesDictionary();
			int num = 0;
			foreach (MovingEntityData attachedEntity in movingBlockSet.AttachedEntities)
			{
				ValuesDictionary valuesDictionary3 = new ValuesDictionary();
				ValuesDictionary valuesDictionary4 = new ValuesDictionary();
				attachedEntity.Entity.Entity.InternalSaveEntity(valuesDictionary4, new EntityToIdMap(new Dictionary<Entity, int>()));
				if (attachedEntity.Entity.Entity.ValuesDictionary.DatabaseObject != null)
				{
					valuesDictionary4.SetValue("TemplateName", attachedEntity.Entity.Entity.ValuesDictionary.DatabaseObject.Name);
				}
				else
				{
					Log.Warning("[MovingBlocks DEBUG] Saving entity without DatabaseObject/TemplateName!");
				}
				valuesDictionary3.SetValue("Data", valuesDictionary4);
				valuesDictionary3.SetValue("Offset", attachedEntity.BlockOffset);
				valuesDictionary2.SetValue(num.ToString(CultureInfo.InvariantCulture), valuesDictionary3);
				num++;
			}
			valuesDictionary.SetValue("AttachedEntities", valuesDictionary2);
		}
		return valuesDictionary;
	}

	public override void Dispose()
	{
		if (m_blockGeometryGenerator != null && m_blockGeometryGenerator.Terrain != null)
		{
			m_blockGeometryGenerator.Terrain.Dispose();
		}
	}

	public void MovingBlocksCollision(MovingBlockSet movingBlockSet)
	{
		BoundingBox color = movingBlockSet.BoundingBox(extendToFillCells: true);
		m_result.Clear();
		FindMovingBlocks(color, extendToFillCells: true, m_result);
		for (int i = 0; i < m_result.Count; i++)
		{
			MovingBlockSet movingBlockSet2 = (MovingBlockSet)m_result.Array[i];
			if (movingBlockSet2 != movingBlockSet && (movingBlockSet.CollisionGroupId == 0 || movingBlockSet2.CollisionGroupId != movingBlockSet.CollisionGroupId))
			{
				movingBlockSet.Stop = true;
				break;
			}
		}
	}

	public void TerrainCollision(MovingBlockSet movingBlockSet)
	{
		Point3 point = new Point3
		{
			X = (int)MathUtils.Floor((float)movingBlockSet.Box.Left + movingBlockSet.Position.X),
			Y = (int)MathUtils.Floor((float)movingBlockSet.Box.Top + movingBlockSet.Position.Y),
			Z = (int)MathUtils.Floor((float)movingBlockSet.Box.Near + movingBlockSet.Position.Z)
		};
		Point3 point2 = new Point3
		{
			X = (int)MathUtils.Ceiling((float)movingBlockSet.Box.Right + movingBlockSet.Position.X),
			Y = (int)MathUtils.Ceiling((float)movingBlockSet.Box.Bottom + movingBlockSet.Position.Y),
			Z = (int)MathUtils.Ceiling((float)movingBlockSet.Box.Far + movingBlockSet.Position.Z)
		};
		for (int i = point.X; i < point2.X; i++)
		{
			for (int j = point.Z; j < point2.Z; j++)
			{
				for (int k = point.Y; k < point2.Y; k++)
				{
					if (Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(i, k, j)) != 0)
					{
						CollidedWithTerrain?.Invoke(movingBlockSet, new Point3(i, k, j));
					}
				}
			}
		}
	}

	public void GenerateGeometry(MovingBlockSet movingBlockSet)
	{
		Point3 point = default;
		point.X = ((movingBlockSet.CurrentVelocity.X > 0f) ? ((int)MathUtils.Floor(movingBlockSet.Position.X)) : (point.X = (int)MathUtils.Ceiling(movingBlockSet.Position.X)));
		point.Y = ((movingBlockSet.CurrentVelocity.Y > 0f) ? ((int)MathUtils.Floor(movingBlockSet.Position.Y)) : (point.Y = (int)MathUtils.Ceiling(movingBlockSet.Position.Y)));
		point.Z = ((movingBlockSet.CurrentVelocity.Z > 0f) ? ((int)MathUtils.Floor(movingBlockSet.Position.Z)) : (point.Z = (int)MathUtils.Ceiling(movingBlockSet.Position.Z)));
		if (!(point != movingBlockSet.GeometryGenerationPosition))
		{
			return;
		}
		int chunkX = point.X >> 4;
		int chunkZ = point.Z >> 4;
		TerrainChunk chunkAtCoords = m_subsystemTerrain.Terrain.GetChunkAtCoords(chunkX, chunkZ);
		if (chunkAtCoords == null || chunkAtCoords.State <= TerrainChunkState.InvalidContents4)
		{
			return;
		}
		movingBlockSet.LastLightPosition = point;
		Point3 p = new Point3(movingBlockSet.Box.Left, movingBlockSet.Box.Top, movingBlockSet.Box.Near);
		Point3 point2 = new Point3(movingBlockSet.Box.Width, movingBlockSet.Box.Height, movingBlockSet.Box.Depth);
		point2.Y = MathUtils.Min(point2.Y, 254);
		int val = point2.X + 16;
		int val2 = point2.Z + 16;
		int x = Math.Max(val, val2) / 16 + 1;
		x = (int)MathUtils.NextPowerOf2((uint)x);
		x = Math.Max(x, 2);
		if (m_blockGeometryGenerator == null || m_generatorCapacity < x)
		{
			if (m_blockGeometryGenerator != null && m_blockGeometryGenerator.Terrain != null)
			{
				m_blockGeometryGenerator.Terrain.Dispose();
			}
			m_generatorCapacity = x;
			m_blockGeometryGenerator = new BlockGeometryGenerator(new Terrain(), m_subsystemTerrain, null, Project.FindSubsystem<SubsystemFurnitureBlockBehavior>(throwOnError: true), null, Project.FindSubsystem<SubsystemPalette>(throwOnError: true));
			for (int i = 0; i < m_generatorCapacity; i++)
			{
				for (int j = 0; j < m_generatorCapacity; j++)
				{
					m_blockGeometryGenerator.Terrain.AllocateChunk(i, j);
				}
			}
		}
		Terrain terrain = m_subsystemTerrain.Terrain;
		for (int k = 0; k < point2.X + 2; k++)
		{
			for (int l = 0; l < point2.Z + 2; l++)
			{
				int x2 = k + p.X + point.X - 1;
				int z = l + p.Z + point.Z - 1;
				if (terrain.GetChunkAtCell(x2, z) != null)
				{
					int shaftValue = terrain.GetShaftValue(x2, z);
					m_blockGeometryGenerator.Terrain.SetTemperature(k, l, Terrain.ExtractTemperature(shaftValue));
					m_blockGeometryGenerator.Terrain.SetHumidity(k, l, Terrain.ExtractHumidity(shaftValue));
					for (int m = 0; m < point2.Y + 2; m++)
					{
						int y = m + p.Y + point.Y - 1;
						int light = Terrain.ExtractLight(terrain.GetCellValue(x2, y, z));
						m_blockGeometryGenerator.Terrain.SetCellValueFast(k, m, l, Terrain.MakeBlockValue(0, light, 0));
					}
				}
			}
		}
		m_blockGeometryGenerator.Terrain.SeasonTemperature = terrain.SeasonTemperature;
		m_blockGeometryGenerator.Terrain.SeasonHumidity = terrain.SeasonHumidity;
		foreach (MovingBlock block in movingBlockSet.Blocks)
		{
			int num = block.Offset.X - p.X + 1;
			int num2 = block.Offset.Y - p.Y + 1;
			int num3 = block.Offset.Z - p.Z + 1;
			if (num >= 0 && num < m_generatorCapacity * 16 && num3 >= 0 && num3 < m_generatorCapacity * 16 && num2 >= 0 && num2 < 256)
			{
				int cellLightFast = m_blockGeometryGenerator.Terrain.GetCellLightFast(num, num2, num3);
				int value = Terrain.ReplaceLight(block.Value, cellLightFast);
				m_blockGeometryGenerator.Terrain.SetCellValueFast(num, num2, num3, value);
			}
		}
		foreach (MovingBlock block2 in movingBlockSet.Blocks)
		{
			int num4 = Terrain.ExtractContents(block2.Value);
			int emittedLightAmount = BlocksManager.Blocks[num4].GetEmittedLightAmount(block2.Value);
			if (emittedLightAmount <= 0)
			{
				continue;
			}
			int num5 = block2.Offset.X - p.X + 1;
			int num6 = block2.Offset.Y - p.Y + 1;
			int num7 = block2.Offset.Z - p.Z + 1;
			int num8 = emittedLightAmount;
			for (int n = -num8; n <= num8; n++)
			{
				for (int num9 = -num8; num9 <= num8; num9++)
				{
					for (int num10 = -num8; num10 <= num8; num10++)
					{
						int num11 = Math.Abs(n) + Math.Abs(num9) + Math.Abs(num10);
						if (num11 > num8)
						{
							continue;
						}
						int num12 = num5 + n;
						int num13 = num6 + num9;
						int num14 = num7 + num10;
						if (num12 < 0 || num13 < 0 || num14 < 0 || num12 >= m_generatorCapacity * 16 || num14 >= m_generatorCapacity * 16 || num13 >= 256)
						{
							continue;
						}
						int num15 = emittedLightAmount - num11;
						if (num15 > 0)
						{
							int cellValueFast = m_blockGeometryGenerator.Terrain.GetCellValueFast(num12, num13, num14);
							int num16 = Terrain.ExtractLight(cellValueFast);
							if (num15 > num16)
							{
								m_blockGeometryGenerator.Terrain.SetCellValueFast(num12, num13, num14, Terrain.ReplaceLight(cellValueFast, num15));
							}
						}
					}
				}
			}
		}
		m_blockGeometryGenerator.ResetCache();
		movingBlockSet.Vertices.Clear();
		movingBlockSet.Indices.Clear();
		for (int num17 = 1; num17 < point2.X + 1; num17++)
		{
			for (int num18 = 1; num18 < point2.Y + 1; num18++)
			{
				for (int num19 = 1; num19 < point2.Z + 1; num19++)
				{
					int cellValueFast2 = m_blockGeometryGenerator.Terrain.GetCellValueFast(num17, num18, num19);
					int num20 = Terrain.ExtractContents(cellValueFast2);
					if (num20 != 0)
					{
						try
						{
							BlocksManager.Blocks[num20].GenerateTerrainVertices(m_blockGeometryGenerator, movingBlockSet.Geometry, cellValueFast2, num17, num18, num19);
						}
						catch
						{
						}
					}
				}
			}
		}
		movingBlockSet.GeometryOffset = new Vector3(p) - new Vector3(1f);
		movingBlockSet.GeometryGenerationPosition = point;
	}

	public void DrawMovingBlockSet(Camera camera, MovingBlockSet movingBlockSet)
	{
		if (!camera.ViewFrustum.Intersection(movingBlockSet.BoundingBox(extendToFillCells: false)))
		{
			return;
		}
		GenerateGeometry(movingBlockSet);
		if (movingBlockSet.Vertices.Count != 0)
		{
			DynamicArray<TerrainVertex> data = movingBlockSet.Vertices;
			DynamicArray<int> indices = movingBlockSet.Indices;
			for (int i = 0; i < indices.Count; i += 3)
			{
				int count = m_vertices.Count;
				int num = indices.Array[i];
				int num2 = indices.Array[i + 1];
				int num3 = indices.Array[i + 2];
				m_vertices.Add(data.Array[num]);
				m_vertices.Add(data.Array[num2]);
				m_vertices.Add(data.Array[num3]);
				m_indices.Add(count);
				m_indices.Add(count + 1);
				m_indices.Add(count + 2);
			}
		}
	}

	public static bool ExclusiveBoxIntersection(BoundingBox b1, BoundingBox b2)
	{
		if (b1.Max.X > b2.Min.X && b1.Min.X < b2.Max.X && b1.Max.Y > b2.Min.Y && b1.Min.Y < b2.Max.Y && b1.Max.Z > b2.Min.Z)
		{
			return b1.Min.Z < b2.Max.Z;
		}
		return false;
	}

	public IMovingBlockSet AddMovingBlockSet(Vector3 position, Vector3 targetPosition, float speed, float acceleration, float drag, Vector2 smoothness, IEnumerable<MovingBlock> blocks, List<MovingEntityData> attachedEntities, string id, object tag, bool testCollision)
	{
		MovingBlockSet color2 = new MovingBlockSet
		{
			Position = position,
			StartPosition = position,
			TargetPosition = targetPosition,
			Speed = speed,
			Acceleration = acceleration,
			Drag = drag,
			Smoothness = smoothness,
			Id = id,
			Tag = tag,
			Blocks = blocks.ToList(),
			Rotation = Quaternion.Identity,
			AttachedEntities = ((attachedEntities != null) ? attachedEntities : new List<MovingEntityData>())
		};
		color2.UpdateBox();
		if (testCollision)
		{
			MovingBlocksCollision(color2);
			if (color2.Stop)
			{
				return null;
			}
		}
		if (m_canGenerateGeometry)
		{
			GenerateGeometry(color2);
		}
		if (CommonLib.WorkType != WorkType.Client)
		{
			m_movingBlockSets.Add(color2);
			CommonLib.Net.QueuePackage(new MovingBlockPackage(color2));
		}
		return color2;
	}

	public IMovingBlockSet AddMovingBlockSet(Vector3 position, Vector3 targetPosition, float speed, float acceleration, float drag, Vector2 smoothness, IEnumerable<MovingBlock> blocks, List<MovingEntityData> attachedEntities, string id, object tag, int collisionGroupId, bool testCollision)
	{
		MovingBlockSet movingBlockSet = new MovingBlockSet
		{
			Position = position,
			StartPosition = position,
			TargetPosition = targetPosition,
			Speed = speed,
			Acceleration = acceleration,
			Drag = drag,
			Smoothness = smoothness,
			Id = id,
			Tag = tag,
			CollisionGroupId = collisionGroupId,
			Blocks = blocks.ToList(),
			Rotation = Quaternion.Identity,
			AttachedEntities = ((attachedEntities != null) ? attachedEntities : new List<MovingEntityData>())
		};
		movingBlockSet.NetPosition = new NetPosition(position);
		movingBlockSet.NetRotation = new NetRotation(Quaternion.Identity);
		movingBlockSet.NetVelocity = new NetVelocity(Vector3.Zero);
		movingBlockSet.UpdateBox();
		if (testCollision)
		{
			MovingBlocksCollision(movingBlockSet);
			if (movingBlockSet.Stop)
			{
				return null;
			}
		}
		if (m_canGenerateGeometry)
		{
			try
			{
				GenerateGeometry(movingBlockSet);
			}
			catch (Exception ex)
			{
				Log.Warning("Geometry failed for ship, creating invisible physics object: " + ex.Message);
			}
		}
		if (CommonLib.WorkType != WorkType.Client)
		{
			m_movingBlockSets.Add(movingBlockSet);
			CommonLib.Net.QueuePackage(new MovingBlockPackage(movingBlockSet));
		}
		return movingBlockSet;
	}

	private void OnPlayerAdded(PlayerData player)
	{
		if (CommonLib.WorkType != WorkType.Server || player.Client == null)
		{
			return;
		}
		Log.Information($"[Ships] Player joined: {player.Name}. Syncing {m_movingBlockSets.Count} moving blocks.");
		foreach (MovingBlockSet movingBlockSet in m_movingBlockSets)
		{
			MovingBlockPackage isWaterLogged = new MovingBlockPackage(movingBlockSet);
			isWaterLogged.To = player.Client;
			CommonLib.Net.QueuePackage(isWaterLogged);
		}
	}

	private void FlushAndDraw()
	{
		if (m_indices.Count != 0)
		{
			VertexBuffer variant = new VertexBuffer(TerrainVertex.VertexDeclaration, m_vertices.Count);
			IndexBuffer indexBuffer = new IndexBuffer(IndexFormat.ThirtyTwoBits, m_indices.Count);
			variant.SetData(m_vertices.Array, 0, m_vertices.Count);
			indexBuffer.SetData(m_indices.Array, 0, m_indices.Count);
			Display.DrawIndexed(PrimitiveType.TriangleList, m_shader, variant, indexBuffer, 0, indexBuffer.IndicesCount);
			variant.Dispose();
			indexBuffer.Dispose();
			m_vertices.Clear();
			m_indices.Clear();
		}
	}
}
