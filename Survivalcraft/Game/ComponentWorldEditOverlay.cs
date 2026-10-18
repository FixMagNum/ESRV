using System;
using System.Collections.Generic;
using Engine;
using Engine.Graphics;
using GameEntitySystem;

namespace Game;

public class ComponentWorldEditOverlay : Component, IDrawable
{
	private struct GhostQuad
	{
		public Vector3 p0;

		public Vector3 p1;

		public Vector3 p2;

		public Vector3 p3;

		public Vector2 t0;

		public Vector2 t1;

		public Vector2 t2;

		public Vector2 t3;

		public Vector3 normal;

		public Color color;
	}

	private class GhostMesh
	{
		public BlockMesh Mesh;

		public Matrix Transform;

		public Color Color;
	}

	private readonly PrimitivesRenderer3D _pr3D = new PrimitivesRenderer3D();

	private readonly List<GhostQuad> _ghostQuadsCache = new List<GhostQuad>(8192);

	private readonly List<GhostMesh> _ghostMeshesCache = new List<GhostMesh>(2048);

	private bool _ghostCacheValid;

	private int _ghostCacheHash;

	private Point3 _ghostCacheP3;

	private static readonly DrawBlockEnvironmentData s_ghostEnv;

	private static readonly Color GhostAddTint;

	private static readonly Color GhostRemoveTint;

	private static readonly Vector3[] s_faceNormalsVec;

	private static readonly Vector3[][] s_faceCorners;

	private static readonly Point3[] s_faceToDelta;

	private static readonly int[] s_blockFaceIndexByGhostFace;

	private static readonly BlockColorsMap s_DummyColorsMap;

	public IWorldEditOverlayContext Context { get; set; }

	public bool IsMenuOpen { get; set; }

	public bool PhantomEnabled { get; set; } = true;

	public bool SuppressGhost { get; set; }

	public int[] DrawOrders => new int[1] { 1000 };

	public void InvalidateGhostCache()
	{
		_ghostCacheValid = false;
	}

	public void SetFlags(bool isMenuOpen, bool phantomEnabled)
	{
		IsMenuOpen = isMenuOpen;
		PhantomEnabled = phantomEnabled;
	}

	public void Draw(Camera camera, int drawOrder)
	{
		if (!IsMenuOpen || Context == null || Context.SubsystemTerrain == null)
		{
			return;
		}
		Vector3 viewPosition = camera.ViewPosition;
		float num = MathUtils.Max(SettingsManager.VisibilityRange, 192f);
		if (Context.HasP1)
		{
			Point3 point = Context.P1.CellFace.Point;
			BoundingBox boundingBox = new BoundingBox(new Vector3(point.X, point.Y, point.Z), new Vector3(point.X + 1, point.Y + 1, point.Z + 1));
			if (Vector3.Distance((boundingBox.Min + boundingBox.Max) * 0.5f, viewPosition) <= num)
			{
				_pr3D.FlatBatch(-1, DepthStencilState.None).QueueBoundingBox(boundingBox, Color.Blue);
			}
		}
		if (Context.HasP2)
		{
			Point3 point2 = Context.P2.CellFace.Point;
			BoundingBox boundingBox2 = new BoundingBox(new Vector3(point2.X, point2.Y, point2.Z), new Vector3(point2.X + 1, point2.Y + 1, point2.Z + 1));
			if (Vector3.Distance((boundingBox2.Min + boundingBox2.Max) * 0.5f, viewPosition) <= num)
			{
				_pr3D.FlatBatch(-1, DepthStencilState.None).QueueBoundingBox(boundingBox2, Color.Yellow);
			}
		}
		if (Context.HasP3)
		{
			Point3 point3 = Context.P3.CellFace.Point;
			BoundingBox boundingBox3 = new BoundingBox(new Vector3(point3.X, point3.Y, point3.Z), new Vector3(point3.X + 1, point3.Y + 1, point3.Z + 1));
			if (Vector3.Distance((boundingBox3.Min + boundingBox3.Max) * 0.5f, viewPosition) <= num)
			{
				_pr3D.FlatBatch(-1, DepthStencilState.None).QueueBoundingBox(boundingBox3, Color.Black);
			}
		}
		if (Context.HasP1 && Context.HasP2)
		{
			Point3 point4 = Context.P1.CellFace.Point;
			Point3 point5 = Context.P2.CellFace.Point;
			int num2 = Math.Min(point4.X, point5.X);
			int num3 = Math.Max(point4.X, point5.X) + 1;
			int num4 = Math.Min(point4.Y, point5.Y);
			int num5 = Math.Max(point4.Y, point5.Y) + 1;
			int num6 = Math.Min(point4.Z, point5.Z);
			int num7 = Math.Max(point4.Z, point5.Z) + 1;
			BoundingBox boundingBox4 = new BoundingBox(new Vector3(num2, num4, num6), new Vector3(num3, num5, num7));
			if (Vector3.Distance((boundingBox4.Min + boundingBox4.Max) * 0.5f, viewPosition) <= num)
			{
				_pr3D.FlatBatch(-1, DepthStencilState.None).QueueBoundingBox(boundingBox4, Color.Green);
			}
		}
		if (Context.MemCount > 0 && Context.HasP3)
		{
			Context.GetMemBounds(out var min, out var max);
			Point3 point6 = Context.P3.CellFace.Point;
			Vector3 min2 = new Vector3(point6.X + min.X, point6.Y + min.Y, point6.Z + min.Z);
			Vector3 max2 = new Vector3(point6.X + max.X + 1, point6.Y + max.Y + 1, point6.Z + max.Z + 1);
			BoundingBox boundingBox5 = new BoundingBox(min2, max2);
			if (Vector3.Distance((boundingBox5.Min + boundingBox5.Max) * 0.5f, viewPosition) <= num)
			{
				_pr3D.FlatBatch(-1, DepthStencilState.None).QueueBoundingBox(boundingBox5, Color.Red);
			}
		}
		if (PhantomEnabled)
		{
			DrawGhostPreview(camera);
		}
		_pr3D.Flush(camera.ViewProjectionMatrix);
	}

	private void DrawGhostPreview(Camera camera)
	{
		if (SuppressGhost || Context.MemCount <= 0 || !Context.HasP3)
		{
			return;
		}
		EnsureGhostCacheBuilt();
		if (_ghostQuadsCache.Count > 0)
		{
			Texture2D texture = ((Context.SubsystemTerrain != null) ? Context.SubsystemTerrain.SubsystemAnimatedTextures.AnimatedBlocksTexture : BlocksTexturesManager.DefaultBlocksTexture);
			TerrainRaycastResult p = Context.P3;
			Vector3 vector = new Vector3(p.CellFace.Point.X, p.CellFace.Point.Y, p.CellFace.Point.Z);
			Vector3 viewPosition = camera.ViewPosition;
			float num = MathUtils.Max(SettingsManager.VisibilityRange, 192f) + 2f;
			int num2 = 0;
			TexturedBatch3D texturedBatch3D = _pr3D.TexturedBatch(texture, useAlphaTest: true, 0, DepthStencilState.Default, RasterizerState.CullNone, BlendState.AlphaBlend, SamplerState.PointClamp);
			for (int i = 0; i < _ghostQuadsCache.Count; i++)
			{
				GhostQuad ghostQuad = _ghostQuadsCache[i];
				Vector3 vector2 = vector + (ghostQuad.p0 + ghostQuad.p1 + ghostQuad.p2 + ghostQuad.p3) * 0.25f;
				if (Vector3.DistanceSquared(vector2, viewPosition) <= num * num && Vector3.Dot(vector2 - viewPosition, camera.ViewDirection) >= -0.75f && Vector3.Dot(ghostQuad.normal, vector2 - viewPosition) < 0f)
				{
					Vector3 vector3 = ghostQuad.normal * 0.0008f;
					texturedBatch3D.QueueQuad(vector + ghostQuad.p0 + vector3, vector + ghostQuad.p1 + vector3, vector + ghostQuad.p2 + vector3, vector + ghostQuad.p3 + vector3, ghostQuad.t0, ghostQuad.t1, ghostQuad.t2, ghostQuad.t3, ghostQuad.color);
					if (++num2 >= 12000)
					{
						_pr3D.Flush(camera.ViewProjectionMatrix);
						texturedBatch3D = _pr3D.TexturedBatch(texture, useAlphaTest: true, 0, DepthStencilState.Default, RasterizerState.CullNone, BlendState.AlphaBlend, SamplerState.PointClamp);
						num2 = 0;
					}
				}
			}
		}
		if (_ghostMeshesCache.Count > 0)
		{
			Point3 point = Context.P3.CellFace.Point;
			Matrix matrix = Matrix.CreateTranslation(new Vector3(point.X, point.Y, point.Z));
			Vector3 viewPosition2 = camera.ViewPosition;
			Vector3 viewDirection = camera.ViewDirection;
			float num3 = MathUtils.Max(SettingsManager.VisibilityRange, 192f);
			float num4 = (num3 + 48f) * (num3 + 48f);
			List<KeyValuePair<int, float>> list = new List<KeyValuePair<int, float>>(_ghostMeshesCache.Count);
			for (int j = 0; j < _ghostMeshesCache.Count; j++)
			{
				GhostMesh ghostMesh = _ghostMeshesCache[j];
				Vector3 vector4 = new Vector3((float)point.X + 0.5f, (float)point.Y + 0.5f, (float)point.Z + 0.5f) + ghostMesh.Transform.Translation - viewPosition2;
				float num5 = vector4.LengthSquared();
				if (!(num5 > num4))
				{
					float num6 = (float)Math.Sqrt(num5);
					float num7 = ((num6 > 0f) ? Vector3.Dot(vector4 / num6, viewDirection) : 1f);
					if (!(num7 < -0.05f))
					{
						float value = (num7 + 1f) * (1f / (0.0005f + num5));
						list.Add(new KeyValuePair<int, float>(j, value));
					}
				}
			}
			list.Sort((KeyValuePair<int, float> a, KeyValuePair<int, float> b) => b.Value.CompareTo(a.Value));
			int num8 = Math.Min(list.Count, 6000);
			for (int num9 = 0; num9 < num8; num9++)
			{
				int key = list[num9].Key;
				GhostMesh ghostMesh2 = _ghostMeshesCache[key];
				Vector3 vector5 = new Vector3((float)point.X + 0.5f, (float)point.Y + 0.5f, (float)point.Z + 0.5f) + ghostMesh2.Transform.Translation;
				Vector3 vector6 = viewPosition2 - vector5;
				if (vector6.LengthSquared() > 0f)
				{
					vector6 = Vector3.Normalize(vector6);
				}
				Matrix matrix2 = Matrix.CreateTranslation(vector6 * 0.0008f);
				Matrix matrix3 = ghostMesh2.Transform * matrix * matrix2;
				BlocksManager.DrawMeshBlock(_pr3D, ghostMesh2.Mesh, ghostMesh2.Color, 1f, ref matrix3, s_ghostEnv);
				if ((num9 + 1) % 1024 == 0)
				{
					_pr3D.Flush(camera.ViewProjectionMatrix);
				}
			}
		}
		_pr3D.Flush(camera.ViewProjectionMatrix);
	}

	private void EnsureGhostCacheBuilt()
	{
		int num = ComputeMemHash(Context.Memory);
		Point3 point = Context.P3.CellFace.Point;
		if (_ghostCacheValid && _ghostCacheHash == num && _ghostCacheP3.Equals(point))
		{
			return;
		}
		_ghostQuadsCache.Clear();
		_ghostMeshesCache.Clear();
		Dictionary<Point3, int> dictionary = new Dictionary<Point3, int>(Context.MemCount * 2);
		for (int i = 0; i < Context.Memory.Count; i++)
		{
			BlockMem blockMem = Context.Memory[i];
			if (Terrain.ExtractContents(blockMem.id) != 0)
			{
				dictionary[new Point3(blockMem.x, blockMem.y, blockMem.z)] = blockMem.id;
			}
		}
		if (dictionary.Count == 0)
		{
			_ghostCacheValid = true;
			_ghostCacheHash = num;
			_ghostCacheP3 = point;
			return;
		}
		Point3 point2 = new Point3(int.MaxValue, int.MaxValue, int.MaxValue);
		Point3 point3 = new Point3(int.MinValue, int.MinValue, int.MinValue);
		foreach (KeyValuePair<Point3, int> item in dictionary)
		{
			Point3 key = item.Key;
			if (key.X < point2.X)
			{
				point2.X = key.X;
			}
			if (key.Y < point2.Y)
			{
				point2.Y = key.Y;
			}
			if (key.Z < point2.Z)
			{
				point2.Z = key.Z;
			}
			if (key.X > point3.X)
			{
				point3.X = key.X;
			}
			if (key.Y > point3.Y)
			{
				point3.Y = key.Y;
			}
			if (key.Z > point3.Z)
			{
				point3.Z = key.Z;
			}
		}
		foreach (KeyValuePair<Point3, int> item2 in dictionary)
		{
			Point3 key2 = item2.Key;
			int value = item2.Value;
			int num2 = Terrain.ExtractContents(value);
			if (num2 == 0)
			{
				continue;
			}
			Point3 worldCell = new Point3(point.X + key2.X, point.Y + key2.Y, point.Z + key2.Z);
			if (Context.SubsystemTerrain.Terrain.GetCellValue(worldCell.X, worldCell.Y, worldCell.Z) == value)
			{
				continue;
			}
			bool flag = false;
			try
			{
				flag = TryBuildGhostForSpecialBlock(key2, value, dictionary, point);
			}
			catch
			{
				flag = false;
			}
			if (flag)
			{
				continue;
			}
			Block block = BlocksManager.Blocks[num2];
			int textureSlotCount = block.GetTextureSlotCount(value);
			if (textureSlotCount <= 0)
			{
				continue;
			}
			for (int j = 0; j < 6; j++)
			{
				Point3 point4 = s_faceToDelta[j];
				Point3 key3 = new Point3(key2.X + point4.X, key2.Y + point4.Y, key2.Z + point4.Z);
				Point3 point5 = new Point3(point.X + key3.X, point.Y + key3.Y, point.Z + key3.Z);
				bool flag2 = dictionary.TryGetValue(key3, out var value2);
				int cellValue = Context.SubsystemTerrain.Terrain.GetCellValue(point5.X, point5.Y, point5.Z);
				int neighborValue = (flag2 ? value2 : cellValue);
				int face = s_blockFaceIndexByGhostFace[j];
				if (block.ShouldGenerateFace(Context.SubsystemTerrain, face, value, neighborValue))
				{
					int faceTextureSlot = block.GetFaceTextureSlot(face, value);
					if (faceTextureSlot < 0)
					{
						faceTextureSlot = block.GetFaceTextureSlot(-1, value);
					}
					if (faceTextureSlot >= 0)
					{
						float num3 = (float)(faceTextureSlot % textureSlotCount) / (float)textureSlotCount;
						float num4 = (float)(faceTextureSlot / textureSlotCount) / (float)textureSlotCount;
						float u = num3 + 1f / (float)textureSlotCount;
						float v = num4 + 1f / (float)textureSlotCount;
						SetFaceUV(j, num3, num4, u, v, out var t, out var t2, out var t3, out var t4);
						Vector3 vector = new Vector3(key2.X, key2.Y, key2.Z);
						Vector3[] array = s_faceCorners[j];
						Color ghostColor = GetGhostColor(value, isAdd: true, worldCell);
						_ghostQuadsCache.Add(new GhostQuad
						{
							p0 = vector + array[0],
							p1 = vector + array[1],
							p2 = vector + array[2],
							p3 = vector + array[3],
							t0 = t,
							t1 = t2,
							t2 = t3,
							t3 = t4,
							normal = s_faceNormalsVec[j],
							color = ghostColor
						});
					}
				}
			}
		}
		for (int k = point2.Y; k <= point3.Y; k++)
		{
			for (int l = point2.Z; l <= point3.Z; l++)
			{
				for (int m = point2.X; m <= point3.X; m++)
				{
					Point3 point6 = new Point3(m, k, l);
					if (dictionary.TryGetValue(point6, out var value3) && Terrain.ExtractContents(value3) != 0)
					{
						continue;
					}
					Point3 worldCell2 = new Point3(point.X + m, point.Y + k, point.Z + l);
					int cellValue2 = Context.SubsystemTerrain.Terrain.GetCellValue(worldCell2.X, worldCell2.Y, worldCell2.Z);
					int num5 = Terrain.ExtractContents(cellValue2);
					if (num5 == 0)
					{
						continue;
					}
					Block block2 = BlocksManager.Blocks[num5];
					if (block2 is SlabBlock slabBlock)
					{
						int num6 = (SlabBlock.GetIsTop(Terrain.ExtractData(cellValue2)) ? 1 : 0);
						BoundingBox[] array2 = slabBlock.m_collisionBoxes[num6];
						if (array2 != null && array2.Length != 0 && block2.GetTextureSlotCount(cellValue2) > 0)
						{
							for (int n = 0; n < array2.Length; n++)
							{
								AddAabbQuads(point6, array2[n], cellValue2, block2, isAdd: false, worldCell2, dictionary, point);
							}
						}
						continue;
					}
					if (block2 is StairsBlock stairsBlock)
					{
						int variant = StairsBlock.GetVariant(Terrain.ExtractData(cellValue2));
						BoundingBox[] array3 = stairsBlock.m_collisionBoxes[variant];
						if (array3 != null && array3.Length != 0 && block2.GetTextureSlotCount(cellValue2) > 0)
						{
							for (int num7 = 0; num7 < array3.Length; num7++)
							{
								AddAabbQuads(point6, array3[num7], cellValue2, block2, isAdd: false, worldCell2, dictionary, point);
							}
						}
						continue;
					}
					if (block2 is CrossBlock)
					{
						int textureSlotCount2 = block2.GetTextureSlotCount(cellValue2);
						int faceTextureSlot2 = block2.GetFaceTextureSlot(0, cellValue2);
						if (faceTextureSlot2 < 0)
						{
							faceTextureSlot2 = block2.GetFaceTextureSlot(-1, cellValue2);
						}
						if (textureSlotCount2 > 0 && faceTextureSlot2 >= 0)
						{
							float num8 = (float)(faceTextureSlot2 % textureSlotCount2) / (float)textureSlotCount2;
							float num9 = (float)(faceTextureSlot2 / textureSlotCount2) / (float)textureSlotCount2;
							float x = num8 + 1f / (float)textureSlotCount2;
							float y = num9 + 1f / (float)textureSlotCount2;
							Vector2 t5 = new Vector2(num8, y);
							Vector2 t6 = new Vector2(x, y);
							Vector2 t7 = new Vector2(x, num9);
							Vector2 t8 = new Vector2(num8, num9);
							Vector3 vector2 = new Vector3(point6.X, point6.Y, point6.Z);
							Vector3 vector3 = vector2 + new Vector3(0f, 0f, 1f);
							Vector3 vector4 = vector2 + new Vector3(1f, 0f, 0f);
							Vector3 vector5 = vector2 + new Vector3(1f, 1f, 0f);
							Vector3 vector6 = vector2 + new Vector3(0f, 1f, 1f);
							Vector3 vector7 = Vector3.Normalize(new Vector3(1f, 0f, -1f));
							Vector3 vector8 = vector2 + new Vector3(0f, 0f, 0f);
							Vector3 vector9 = vector2 + new Vector3(1f, 0f, 1f);
							Vector3 vector10 = vector2 + new Vector3(1f, 1f, 1f);
							Vector3 vector11 = vector2 + new Vector3(0f, 1f, 0f);
							Vector3 vector12 = Vector3.Normalize(new Vector3(1f, 0f, 1f));
							Color color = Color.PremultiplyAlpha(GetGhostTintFor(cellValue2, isAdd: false));
							_ghostQuadsCache.Add(new GhostQuad
							{
								p0 = vector3,
								p1 = vector4,
								p2 = vector5,
								p3 = vector6,
								t0 = t5,
								t1 = t6,
								t2 = t7,
								t3 = t8,
								normal = vector7,
								color = color
							});
							_ghostQuadsCache.Add(new GhostQuad
							{
								p0 = vector4,
								p1 = vector3,
								p2 = vector6,
								p3 = vector5,
								t0 = t5,
								t1 = t6,
								t2 = t7,
								t3 = t8,
								normal = -vector7,
								color = color
							});
							_ghostQuadsCache.Add(new GhostQuad
							{
								p0 = vector8,
								p1 = vector9,
								p2 = vector10,
								p3 = vector11,
								t0 = t5,
								t1 = t6,
								t2 = t7,
								t3 = t8,
								normal = vector12,
								color = color
							});
							_ghostQuadsCache.Add(new GhostQuad
							{
								p0 = vector9,
								p1 = vector8,
								p2 = vector11,
								p3 = vector10,
								t0 = t5,
								t1 = t6,
								t2 = t7,
								t3 = t8,
								normal = -vector12,
								color = color
							});
						}
						continue;
					}
					int textureSlotCount3 = block2.GetTextureSlotCount(cellValue2);
					if (textureSlotCount3 <= 0)
					{
						continue;
					}
					for (int num10 = 0; num10 < 6; num10++)
					{
						Point3 point7 = s_faceToDelta[num10];
						Point3 key4 = new Point3(point6.X + point7.X, point6.Y + point7.Y, point6.Z + point7.Z);
						Point3 point8 = new Point3(point.X + key4.X, point.Y + key4.Y, point.Z + key4.Z);
						bool flag3 = dictionary.TryGetValue(key4, out var value4);
						int cellValue3 = Context.SubsystemTerrain.Terrain.GetCellValue(point8.X, point8.Y, point8.Z);
						int neighborValue2 = (flag3 ? value4 : cellValue3);
						int face2 = s_blockFaceIndexByGhostFace[num10];
						if (block2.ShouldGenerateFace(Context.SubsystemTerrain, face2, cellValue2, neighborValue2))
						{
							int faceTextureSlot3 = block2.GetFaceTextureSlot(face2, cellValue2);
							if (faceTextureSlot3 < 0)
							{
								faceTextureSlot3 = block2.GetFaceTextureSlot(-1, cellValue2);
							}
							if (faceTextureSlot3 >= 0)
							{
								float num11 = (float)(faceTextureSlot3 % textureSlotCount3) / (float)textureSlotCount3;
								float num12 = (float)(faceTextureSlot3 / textureSlotCount3) / (float)textureSlotCount3;
								float u2 = num11 + 1f / (float)textureSlotCount3;
								float v2 = num12 + 1f / (float)textureSlotCount3;
								SetFaceUV(num10, num11, num12, u2, v2, out var t9, out var t10, out var t11, out var t12);
								Vector3 vector13 = new Vector3(point6.X, point6.Y, point6.Z);
								Vector3[] array4 = s_faceCorners[num10];
								Color ghostColor2 = GetGhostColor(cellValue2, isAdd: false, worldCell2);
								_ghostQuadsCache.Add(new GhostQuad
								{
									p0 = vector13 + array4[0],
									p1 = vector13 + array4[1],
									p2 = vector13 + array4[2],
									p3 = vector13 + array4[3],
									t0 = t9,
									t1 = t10,
									t2 = t11,
									t3 = t12,
									normal = s_faceNormalsVec[num10],
									color = ghostColor2
								});
							}
						}
					}
				}
			}
		}
		_ghostCacheValid = true;
		_ghostCacheHash = num;
		_ghostCacheP3 = point;
	}

	private Color GetGhostTintFor(int value, bool isAdd)
	{
		Color result = (isAdd ? GhostAddTint : GhostRemoveTint);
		int num = Terrain.ExtractContents(value);
		if (num == 0)
		{
			return result;
		}
		if (BlocksManager.Blocks[num] is IPaintableBlock paintableBlock)
		{
			int? paintColor = paintableBlock.GetPaintColor(value);
			if (paintColor.HasValue)
			{
				Color color = SubsystemPalette.GetColor(Context.SubsystemTerrain, paintColor);
				return new Color(color.R, color.G, color.B, result.A);
			}
		}
		return result;
	}

	private static int ComputeMemHash(IReadOnlyList<BlockMem> mem)
	{
		int num = mem.Count;
		for (int i = 0; i < mem.Count; i++)
		{
			BlockMem blockMem = mem[i];
			num = (num * 16777619) ^ blockMem.x;
			num = (num * 16777619) ^ blockMem.y;
			num = (num * 16777619) ^ blockMem.z;
			num = (num * 16777619) ^ blockMem.id;
		}
		return num;
	}

	static ComponentWorldEditOverlay()
	{
		s_DummyColorsMap = BlockColorsMap.GrassColorsMap;
		s_ghostEnv = new DrawBlockEnvironmentData
		{
			Light = 15
		};
		GhostAddTint = new Color(255, 255, 255, 180);
		GhostRemoveTint = new Color(255, 64, 64, 160);
		s_faceNormalsVec = new Vector3[6]
		{
			new Vector3(0f, 0f, 1f),
			new Vector3(1f, 0f, 0f),
			new Vector3(0f, 0f, -1f),
			new Vector3(-1f, 0f, 0f),
			new Vector3(0f, 1f, 0f),
			new Vector3(0f, -1f, 0f)
		};
		s_faceCorners = new Vector3[6][]
		{
			new Vector3[4]
			{
				new Vector3(0f, 0f, 1f),
				new Vector3(1f, 0f, 1f),
				new Vector3(1f, 1f, 1f),
				new Vector3(0f, 1f, 1f)
			},
			new Vector3[4]
			{
				new Vector3(1f, 0f, 0f),
				new Vector3(1f, 0f, 1f),
				new Vector3(1f, 1f, 1f),
				new Vector3(1f, 1f, 0f)
			},
			new Vector3[4]
			{
				new Vector3(1f, 0f, 0f),
				new Vector3(0f, 0f, 0f),
				new Vector3(0f, 1f, 0f),
				new Vector3(1f, 1f, 0f)
			},
			new Vector3[4]
			{
				new Vector3(0f, 0f, 1f),
				new Vector3(0f, 0f, 0f),
				new Vector3(0f, 1f, 0f),
				new Vector3(0f, 1f, 1f)
			},
			new Vector3[4]
			{
				new Vector3(0f, 1f, 0f),
				new Vector3(1f, 1f, 0f),
				new Vector3(1f, 1f, 1f),
				new Vector3(0f, 1f, 1f)
			},
			new Vector3[4]
			{
				new Vector3(0f, 0f, 1f),
				new Vector3(1f, 0f, 1f),
				new Vector3(1f, 0f, 0f),
				new Vector3(0f, 0f, 0f)
			}
		};
		s_faceToDelta = new Point3[6]
		{
			new Point3(0, 0, 1),
			new Point3(1, 0, 0),
			new Point3(0, 0, -1),
			new Point3(-1, 0, 0),
			new Point3(0, 1, 0),
			new Point3(0, -1, 0)
		};
		s_blockFaceIndexByGhostFace = new int[6] { 2, 3, 0, 1, 4, 5 };
	}

	private static void SetFaceUV(int ghostFaceIndex, float u0, float v0, float u1, float v1, out Vector2 t0, out Vector2 t1, out Vector2 t2, out Vector2 t3)
	{
		switch (ghostFaceIndex)
		{
		case 0:
			t0 = new Vector2(u0, v1);
			t1 = new Vector2(u0, v0);
			t2 = new Vector2(u1, v0);
			t3 = new Vector2(u1, v1);
			break;
		case 1:
			t0 = new Vector2(u1, v1);
			t1 = new Vector2(u0, v1);
			t2 = new Vector2(u0, v0);
			t3 = new Vector2(u1, v0);
			break;
		case 2:
			t0 = new Vector2(u1, v1);
			t1 = new Vector2(u1, v0);
			t2 = new Vector2(u0, v0);
			t3 = new Vector2(u0, v1);
			break;
		case 3:
			t0 = new Vector2(u0, v1);
			t1 = new Vector2(u1, v1);
			t2 = new Vector2(u1, v0);
			t3 = new Vector2(u0, v0);
			break;
		case 4:
			t0 = new Vector2(u0, v0);
			t1 = new Vector2(u1, v0);
			t2 = new Vector2(u1, v1);
			t3 = new Vector2(u0, v1);
			break;
		case 5:
			t0 = new Vector2(u0, v1);
			t1 = new Vector2(u1, v1);
			t2 = new Vector2(u1, v0);
			t3 = new Vector2(u0, v0);
			break;
		default:
			t0 = new Vector2(u0, v1);
			t1 = new Vector2(u0, v0);
			t2 = new Vector2(u1, v0);
			t3 = new Vector2(u1, v1);
			break;
		}
	}

	private Color GetBiomeTintForBlock(Block block, int value, Point3 worldCell)
	{
		SubsystemTerrain subsystemTerrain = Context.SubsystemTerrain;
		Terrain terrain = subsystemTerrain.Terrain;
		SubsystemWeather.Season currentSeason = subsystemTerrain.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).CurrentSeason;
		int x = worldCell.X;
		int y = worldCell.Y;
		int z = worldCell.Z;
		if (block is GrassBlock || block is GrassSlabBlock || block is GrassTrapBlock || block is TallGrassBlock || block is RyeBlock)
		{
			return BlockColorsMap.GrassColorsMap.LookupSeasonalGrassColor(currentSeason, terrain, x, y, z);
		}
		if (block is OakLeavesBlock)
		{
			return BlockColorsMap.OakLeavesColorsMap.LookupSeasonalLeavesColor(currentSeason, BlockColorsMap.OakLeavesColorsMap.Lookup(terrain, x, y, z), block.BlockIndex);
		}
		if (block is BirchLeavesBlock)
		{
			return BlockColorsMap.BirchLeavesColorsMap.LookupSeasonalLeavesColor(currentSeason, BlockColorsMap.BirchLeavesColorsMap.Lookup(terrain, x, y, z), block.BlockIndex);
		}
		if (block is SpruceLeavesBlock)
		{
			return BlockColorsMap.SpruceLeavesColorsMap.LookupSeasonalLeavesColor(currentSeason, BlockColorsMap.SpruceLeavesColorsMap.Lookup(terrain, x, y, z), block.BlockIndex);
		}
		if (block is TallSpruceLeavesBlock)
		{
			return BlockColorsMap.TallSpruceLeavesColorsMap.LookupSeasonalLeavesColor(currentSeason, BlockColorsMap.TallSpruceLeavesColorsMap.Lookup(terrain, x, y, z), block.BlockIndex);
		}
		if (block is MimosaLeavesBlock)
		{
			return BlockColorsMap.MimosaLeavesColorsMap.LookupSeasonalLeavesColor(currentSeason, BlockColorsMap.MimosaLeavesColorsMap.Lookup(terrain, x, y, z), block.BlockIndex);
		}
		if (block is IvyBlock)
		{
			return BlockColorsMap.IvyColorsMap.Lookup(terrain, x, y, z);
		}
		if (block is KelpBlock)
		{
			return BlockColorsMap.KelpColorsMap.Lookup(terrain, x, y, z);
		}
		if (block is SeagrassBlock)
		{
			return BlockColorsMap.SeagrassColorsMap.Lookup(terrain, x, y, z);
		}
		if (block is FocalorsWaterBlock || block is LayeredIceBlock)
		{
			Color baseColor = BlockColorsMap.WaterColorsMap.Lookup(terrain, x, y, z);
			return BlockColorsMap.WaterColorsMap.LookupSeasonalWaterColor(currentSeason, baseColor);
		}
		if (block is SandBlock)
		{
			return BlockColorsMap.SandColorsMap.LookupSand(terrain, x, y, z);
		}
		if (block is SoilBlock || block is DirtBlock)
		{
			return s_DummyColorsMap.LookupSeasonalDirtColor(currentSeason, terrain, x, y, z);
		}
		return Color.White;
	}

	private Color GetGhostColor(int value, bool isAdd, Point3 worldCell)
	{
		Color c = (isAdd ? GhostAddTint : GhostRemoveTint);
		int num = Terrain.ExtractContents(value);
		if (num == 0)
		{
			return Color.PremultiplyAlpha(c);
		}
		Block block = BlocksManager.Blocks[num];
		Color color = Color.White;
		if (block is IPaintableBlock paintableBlock)
		{
			int? paintColor = paintableBlock.GetPaintColor(value);
			if (paintColor.HasValue)
			{
				color = SubsystemPalette.GetColor(Context.SubsystemTerrain, paintColor);
			}
		}
		Color biomeTintForBlock = GetBiomeTintForBlock(block, value, worldCell);
		byte r = (byte)(color.R * biomeTintForBlock.R / 255);
		byte g = (byte)(color.G * biomeTintForBlock.G / 255);
		byte b = (byte)(color.B * biomeTintForBlock.B / 255);
		return Color.PremultiplyAlpha(new Color(r, g, b, c.A));
	}

	private void AddAabbQuads(Point3 cellLocal, BoundingBox box, int value, Block block, bool isAdd, Point3 worldCell, Dictionary<Point3, int> mem, Point3 p3Origin)
	{
		int textureSlotCount = block.GetTextureSlotCount(value);
		if (textureSlotCount <= 0)
		{
			return;
		}
		for (int i = 0; i < 6; i++)
		{
			Vector3 min = box.Min;
			Vector3 max = box.Max;
			Vector3 vector;
			Vector3 vector2;
			Vector3 vector3;
			Vector3 vector4;
			Vector3 normal;
			switch (i)
			{
			case 0:
				vector = new Vector3(min.X, min.Y, max.Z);
				vector2 = new Vector3(max.X, min.Y, max.Z);
				vector3 = new Vector3(max.X, max.Y, max.Z);
				vector4 = new Vector3(min.X, max.Y, max.Z);
				normal = new Vector3(0f, 0f, 1f);
				break;
			case 1:
				vector = new Vector3(max.X, min.Y, min.Z);
				vector2 = new Vector3(max.X, min.Y, max.Z);
				vector3 = new Vector3(max.X, max.Y, max.Z);
				vector4 = new Vector3(max.X, max.Y, min.Z);
				normal = new Vector3(1f, 0f, 0f);
				break;
			case 2:
				vector = new Vector3(max.X, min.Y, min.Z);
				vector2 = new Vector3(min.X, min.Y, min.Z);
				vector3 = new Vector3(min.X, max.Y, min.Z);
				vector4 = new Vector3(max.X, max.Y, min.Z);
				normal = new Vector3(0f, 0f, -1f);
				break;
			case 3:
				vector = new Vector3(min.X, min.Y, max.Z);
				vector2 = new Vector3(min.X, min.Y, min.Z);
				vector3 = new Vector3(min.X, max.Y, min.Z);
				vector4 = new Vector3(min.X, max.Y, max.Z);
				normal = new Vector3(-1f, 0f, 0f);
				break;
			case 4:
				vector = new Vector3(min.X, max.Y, min.Z);
				vector2 = new Vector3(max.X, max.Y, min.Z);
				vector3 = new Vector3(max.X, max.Y, max.Z);
				vector4 = new Vector3(min.X, max.Y, max.Z);
				normal = new Vector3(0f, 1f, 0f);
				break;
			default:
				vector = new Vector3(min.X, min.Y, max.Z);
				vector2 = new Vector3(max.X, min.Y, max.Z);
				vector3 = new Vector3(max.X, min.Y, min.Z);
				vector4 = new Vector3(min.X, min.Y, min.Z);
				normal = new Vector3(0f, -1f, 0f);
				break;
			}
			bool flag = false;
			if ((i == 0 && MathUtils.Abs(max.Z - 1f) < 1E-06f) || (i == 2 && MathUtils.Abs(min.Z - 0f) < 1E-06f) || (i == 1 && MathUtils.Abs(max.X - 1f) < 1E-06f) || (i == 3 && MathUtils.Abs(min.X - 0f) < 1E-06f) || (i == 4 && MathUtils.Abs(max.Y - 1f) < 1E-06f) || (i == 5 && MathUtils.Abs(min.Y - 0f) < 1E-06f))
			{
				Point3 point = s_faceToDelta[i];
				Point3 key = new Point3(cellLocal.X + point.X, cellLocal.Y + point.Y, cellLocal.Z + point.Z);
				Point3 point2 = new Point3(p3Origin.X + key.X, p3Origin.Y + key.Y, p3Origin.Z + key.Z);
				bool flag2 = mem.TryGetValue(key, out var value2);
				int cellValue = Context.SubsystemTerrain.Terrain.GetCellValue(point2.X, point2.Y, point2.Z);
				int neighborValue = (flag2 ? value2 : cellValue);
				int face = s_blockFaceIndexByGhostFace[i];
				if (!block.ShouldGenerateFace(Context.SubsystemTerrain, face, value, neighborValue))
				{
					flag = true;
				}
			}
			if (!flag)
			{
				int face2 = s_blockFaceIndexByGhostFace[i];
				int faceTextureSlot = block.GetFaceTextureSlot(face2, value);
				if (faceTextureSlot < 0)
				{
					faceTextureSlot = block.GetFaceTextureSlot(-1, value);
				}
				if (faceTextureSlot >= 0)
				{
					float num = (float)(faceTextureSlot % textureSlotCount) / (float)textureSlotCount;
					float num2 = (float)(faceTextureSlot / textureSlotCount) / (float)textureSlotCount;
					float u = num + 1f / (float)textureSlotCount;
					float v = num2 + 1f / (float)textureSlotCount;
					SetFaceUV(i, num, num2, u, v, out var t, out var t2, out var t3, out var t4);
					Vector3 vector5 = new Vector3(cellLocal.X, cellLocal.Y, cellLocal.Z);
					Color ghostColor = GetGhostColor(value, isAdd, worldCell);
					_ghostQuadsCache.Add(new GhostQuad
					{
						p0 = vector5 + vector,
						p1 = vector5 + vector2,
						p2 = vector5 + vector3,
						p3 = vector5 + vector4,
						t0 = t,
						t1 = t2,
						t2 = t3,
						t3 = t4,
						normal = normal,
						color = ghostColor
					});
				}
			}
		}
	}

	private bool TryBuildGhostForSpecialBlock(Point3 cellLocal, int value, Dictionary<Point3, int> mem, Point3 p3Origin)
	{
		Point3 point = Context.P3.CellFace.Point;
		Point3 worldCell = new Point3(point.X + cellLocal.X, point.Y + cellLocal.Y, point.Z + cellLocal.Z);
		int num = Terrain.ExtractContents(value);
		Block block = BlocksManager.Blocks[num];
		if (num == 227 || num == 366)
		{
			try
			{
				int data = Terrain.ExtractData(value);
				int rotation;
				FurnitureDesign furnitureDesign;
				if (num == 227)
				{
					rotation = FurnitureBlock.GetRotation(data);
					int designIndex = FurnitureBlock.GetDesignIndex(data);
					furnitureDesign = Context.SubsystemTerrain.SubsystemFurnitureBlockBehavior.GetDesign(designIndex) ?? Context.TryGetMemFurnitureDesign(num, designIndex);
				}
				else
				{
					rotation = FurnitureBlock2.GetRotation(data);
					int designIndex2 = FurnitureBlock2.GetDesignIndex(data);
					furnitureDesign = Context.SubsystemTerrain.SubsystemFurnitureBlockBehavior.GetDesign(designIndex2) ?? Context.TryGetMemFurnitureDesign(num, designIndex2);
				}
				if (furnitureDesign == null)
				{
					return false;
				}
				float radians = (float)rotation * (float)Math.PI * 0.5f;
				Matrix transform = Matrix.CreateTranslation(new Vector3(-0.5f, 0f, -0.5f)) * Matrix.CreateRotationY(radians) * Matrix.CreateTranslation(new Vector3(0.5f, 0f, 0.5f)) * Matrix.CreateTranslation(new Vector3(cellLocal.X, cellLocal.Y, cellLocal.Z));
				for (int i = 0; i < 6; i++)
				{
					BlockMesh blockMesh = furnitureDesign.Geometry.SubsetOpaqueByFace[i];
					BlockMesh blockMesh2 = furnitureDesign.Geometry.SubsetAlphaTestByFace[i];
					if (blockMesh != null)
					{
						_ghostMeshesCache.Add(new GhostMesh
						{
							Mesh = blockMesh,
							Transform = transform,
							Color = GetGhostColor(value, isAdd: true, worldCell)
						});
					}
					if (blockMesh2 != null)
					{
						_ghostMeshesCache.Add(new GhostMesh
						{
							Mesh = blockMesh2,
							Transform = transform,
							Color = GetGhostColor(value, isAdd: true, worldCell)
						});
					}
				}
				return true;
			}
			catch
			{
				return false;
			}
		}
		if (block is SlabBlock slabBlock)
		{
			int num2 = (SlabBlock.GetIsTop(Terrain.ExtractData(value)) ? 1 : 0);
			BoundingBox[] array = slabBlock.m_collisionBoxes[num2];
			if (array != null && array.Length != 0 && block.GetTextureSlotCount(value) > 0)
			{
				for (int j = 0; j < array.Length; j++)
				{
					AddAabbQuads(cellLocal, array[j], value, block, isAdd: true, worldCell, mem, p3Origin);
				}
				return true;
			}
			return false;
		}
		if (block is StairsBlock stairsBlock)
		{
			int variant = StairsBlock.GetVariant(Terrain.ExtractData(value));
			BoundingBox[] array2 = stairsBlock.m_collisionBoxes[variant];
			if (array2 != null && array2.Length != 0 && block.GetTextureSlotCount(value) > 0)
			{
				for (int k = 0; k < array2.Length; k++)
				{
					AddAabbQuads(cellLocal, array2[k], value, block, isAdd: true, worldCell, mem, p3Origin);
				}
				return true;
			}
			return false;
		}
		if (block is CrossBlock)
		{
			int textureSlotCount = block.GetTextureSlotCount(value);
			int faceTextureSlot = block.GetFaceTextureSlot(0, value);
			if (faceTextureSlot < 0)
			{
				faceTextureSlot = block.GetFaceTextureSlot(-1, value);
			}
			if (textureSlotCount <= 0 || faceTextureSlot < 0)
			{
				return false;
			}
			float num3 = (float)(faceTextureSlot % textureSlotCount) / (float)textureSlotCount;
			float num4 = (float)(faceTextureSlot / textureSlotCount) / (float)textureSlotCount;
			float x = num3 + 1f / (float)textureSlotCount;
			float y = num4 + 1f / (float)textureSlotCount;
			Vector2 t = new Vector2(num3, y);
			Vector2 t2 = new Vector2(x, y);
			Vector2 t3 = new Vector2(x, num4);
			Vector2 t4 = new Vector2(num3, num4);
			Vector3 vector = new Vector3(cellLocal.X, cellLocal.Y, cellLocal.Z);
			Vector3 vector2 = vector + new Vector3(0f, 0f, 1f);
			Vector3 vector3 = vector + new Vector3(1f, 0f, 0f);
			Vector3 vector4 = vector + new Vector3(1f, 1f, 0f);
			Vector3 vector5 = vector + new Vector3(0f, 1f, 1f);
			Vector3 vector6 = Vector3.Normalize(new Vector3(1f, 0f, -1f));
			Vector3 vector7 = vector + new Vector3(0f, 0f, 0f);
			Vector3 vector8 = vector + new Vector3(1f, 0f, 1f);
			Vector3 vector9 = vector + new Vector3(1f, 1f, 1f);
			Vector3 vector10 = vector + new Vector3(0f, 1f, 0f);
			Vector3 vector11 = Vector3.Normalize(new Vector3(1f, 0f, 1f));
			Color ghostColor = GetGhostColor(value, isAdd: true, worldCell);
			_ghostQuadsCache.Add(new GhostQuad
			{
				p0 = vector2,
				p1 = vector3,
				p2 = vector4,
				p3 = vector5,
				t0 = t,
				t1 = t2,
				t2 = t3,
				t3 = t4,
				normal = vector6,
				color = ghostColor
			});
			_ghostQuadsCache.Add(new GhostQuad
			{
				p0 = vector3,
				p1 = vector2,
				p2 = vector5,
				p3 = vector4,
				t0 = t,
				t1 = t2,
				t2 = t3,
				t3 = t4,
				normal = -vector6,
				color = ghostColor
			});
			_ghostQuadsCache.Add(new GhostQuad
			{
				p0 = vector7,
				p1 = vector8,
				p2 = vector9,
				p3 = vector10,
				t0 = t,
				t1 = t2,
				t2 = t3,
				t3 = t4,
				normal = vector11,
				color = ghostColor
			});
			_ghostQuadsCache.Add(new GhostQuad
			{
				p0 = vector8,
				p1 = vector7,
				p2 = vector10,
				p3 = vector9,
				t0 = t,
				t1 = t2,
				t2 = t3,
				t3 = t4,
				normal = -vector11,
				color = ghostColor
			});
			return true;
		}
		return false;
	}
}
