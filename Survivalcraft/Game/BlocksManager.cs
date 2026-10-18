using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Engine;
using Engine.Graphics;
using Engine.Media;
using Engine.Serialization;
using GameEntitySystem;

namespace Game;

public static class BlocksManager
{
	public struct ImageExtrusionKey
	{
		public Image Image;

		public int Slot;

		public override int GetHashCode()
		{
			return Image.GetHashCode() ^ Slot.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj != null)
			{
				ImageExtrusionKey x = (ImageExtrusionKey)obj;
				if (x.Image == Image)
				{
					return x.Slot == Slot;
				}
				return false;
			}
			return false;
		}
	}

	public static Block[] m_blocks = new Block[1024];

	public static FluidBlock[] m_fluidBlocks = new FluidBlock[1024];

	public static List<string> m_categories = new List<string>();

	public static DrawBlockEnvironmentData m_defaultEnvironmentData = new DrawBlockEnvironmentData();

	public static Vector4[] m_slotTexCoords = new Vector4[256];

	public static Dictionary<ImageExtrusionKey, BlockMesh> m_imageExtrusionsCache = new Dictionary<ImageExtrusionKey, BlockMesh>();

	public static bool DrawImageExtrusionEnabled = true;

	public static FocalorsFluidBlock[] m_focalorsFluidBlocks = new FocalorsFluidBlock[1024];

	public static Block[] Blocks => m_blocks;

	public static FluidBlock[] FluidBlocks => m_fluidBlocks;

	public static ReadOnlyList<string> Categories => new ReadOnlyList<string>(m_categories);

	public static FocalorsFluidBlock[] FocalorsFluidBlocks => m_focalorsFluidBlocks;

	public static void Initialize()
	{
		for (int i = 0; i < m_blocks.Length; i++)
		{
			m_blocks[i] = null;
			m_fluidBlocks[i] = null;
			m_focalorsFluidBlocks[i] = null;
		}
		m_categories.Clear();
		m_categories.Add("Terrain");
		m_categories.Add("Plants");
		m_categories.Add("Construction");
		m_categories.Add("Items");
		m_categories.Add("Tools");
		m_categories.Add("Weapons");
		m_categories.Add("Clothes");
		m_categories.Add("Electrics");
		m_categories.Add("Food");
		m_categories.Add("Spawner Eggs");
		m_categories.Add("Painted");
		m_categories.Add("Dyed");
		m_categories.Add("Fireworks");
		CalculateSlotTexCoordTables();
		foreach (ModEntity pickable in ModsManager.ModList)
		{
			for (int j = 0; j < pickable.Blocks.Count; j++)
			{
				Block block = pickable.Blocks[j];
				m_blocks[block.BlockIndex] = block;
				if (block is FluidBlock)
				{
					m_fluidBlocks[block.BlockIndex] = block as FluidBlock;
				}
				if (block is FocalorsFluidBlock)
				{
					m_focalorsFluidBlocks[block.BlockIndex] = block as FocalorsFluidBlock;
				}
			}
		}
		for (int k = 0; k < m_blocks.Length; k++)
		{
			if (m_blocks[k] == null)
			{
				m_blocks[k] = Blocks[0];
			}
		}
		foreach (ModEntity mod in ModsManager.ModList)
		{
			mod.LoadBlocksData();
		}
		for (int l = 0; l < m_blocks.Length; l++)
		{
			Block block2 = m_blocks[l];
			try
			{
				block2.Initialize();
				if (string.IsNullOrEmpty(block2.CraftingId))
				{
					block2.CraftingId = block2.GetType().Name.ToString();
				}
			}
			catch (Exception ex)
			{
				LoadingScreen.Warning("Загрузка блока " + block2.GetType().FullName + " ошибка:" + ex.Message);
			}
			foreach (int creativeValue in block2.GetCreativeValues())
			{
				AddCategory(block2.GetCategory(creativeValue));
			}
		}
		GameManager.ProjectDisposed += (Project project) =>
		{
			m_imageExtrusionsCache.Clear();
		};
		ModsManager.HookAction("BlocksInitalized", (ModLoader modLoader) =>
		{
			modLoader.BlocksInitalized();
			return false;
		});
	}

	public static void AddCategory(string category)
	{
		if (!m_categories.Contains(category))
		{
			m_categories.Add(category);
		}
	}

	public static Block FindBlockByTypeName(string typeName, bool throwIfNotFound)
	{
		Block block = Blocks.FirstOrDefault((Block b) => b.GetType().Name == typeName);
		if ((block == null) & throwIfNotFound)
		{
			throw new InvalidOperationException(string.Format(LanguageControl.Get("BlocksManager", 1), typeName));
		}
		return block;
	}

	public static Block[] FindBlocksByCraftingId(string craftingId)
	{
		List<Block> list = new List<Block>();
		Block[] blocks = Blocks;
		foreach (Block block in blocks)
		{
			if (block.MatchCrafingId(craftingId))
			{
				list.Add(block);
			}
		}
		return list.ToArray();
	}

	public static void DrawCubeBlock(PrimitivesRenderer3D primitivesRenderer, int value, Vector3 size, ref Matrix matrix, Color color, Color topColor, DrawBlockEnvironmentData environmentData)
	{
		DrawCubeBlock(primitivesRenderer, value, size, ref matrix, color, topColor, environmentData, (environmentData.SubsystemTerrain != null) ? environmentData.SubsystemTerrain.SubsystemAnimatedTextures.AnimatedBlocksTexture : BlocksTexturesManager.DefaultBlocksTexture);
	}

	public static void DrawCubeBlock(PrimitivesRenderer3D primitivesRenderer, int value, Vector3 size, ref Matrix matrix, Color color, Color topColor, DrawBlockEnvironmentData environmentData, Texture2D texture)
	{
		environmentData = environmentData ?? m_defaultEnvironmentData;
		TexturedBatch3D texturedBatch3D = primitivesRenderer.TexturedBatch(texture, useAlphaTest: true, 0, null, RasterizerState.CullCounterClockwiseScissor, null, SamplerState.PointClamp);
		float s = LightingManager.LightIntensityByLightValue[environmentData.Light];
		color = Color.MultiplyColorOnly(color, s);
		topColor = Color.MultiplyColorOnly(topColor, s);
		Vector3 translation = matrix.Translation;
		Vector3 vector = matrix.Right * size.X;
		Vector3 vector2 = matrix.Up * size.Y;
		Vector3 vector3 = matrix.Forward * size.Z;
		Vector3 v = translation + 0.5f * (-vector - vector2 - vector3);
		Vector3 v2 = translation + 0.5f * (vector - vector2 - vector3);
		Vector3 v3 = translation + 0.5f * (-vector + vector2 - vector3);
		Vector3 v4 = translation + 0.5f * (vector + vector2 - vector3);
		Vector3 v5 = translation + 0.5f * (-vector - vector2 + vector3);
		Vector3 v6 = translation + 0.5f * (vector - vector2 + vector3);
		Vector3 v7 = translation + 0.5f * (-vector + vector2 + vector3);
		Vector3 v8 = translation + 0.5f * (vector + vector2 + vector3);
		if (environmentData.ViewProjectionMatrix.HasValue)
		{
			Matrix m = environmentData.ViewProjectionMatrix.Value;
			Vector3.Transform(ref v, ref m, out v);
			Vector3.Transform(ref v2, ref m, out v2);
			Vector3.Transform(ref v3, ref m, out v3);
			Vector3.Transform(ref v4, ref m, out v4);
			Vector3.Transform(ref v5, ref m, out v5);
			Vector3.Transform(ref v6, ref m, out v6);
			Vector3.Transform(ref v7, ref m, out v7);
			Vector3.Transform(ref v8, ref m, out v8);
		}
		int num = Terrain.ExtractContents(value);
		Block obj = Blocks[num];
		Vector4 zero = Vector4.Zero;
		int textureSlotCount = obj.GetTextureSlotCount(value);
		int faceTextureSlot = obj.GetFaceTextureSlot(0, value);
		zero.X = (float)(faceTextureSlot % textureSlotCount) / (float)textureSlotCount;
		zero.Y = (float)(faceTextureSlot / textureSlotCount) / (float)textureSlotCount;
		zero.W = zero.Y + 1f / (float)textureSlotCount;
		zero.Z = zero.X + 1f / (float)textureSlotCount;
		texturedBatch3D.QueueQuad(color: Color.MultiplyColorOnly(color, LightingManager.CalculateLighting(-matrix.Forward)), p1: v, p2: v3, p3: v4, p4: v2, texCoord1: new Vector2(zero.X, zero.W), texCoord2: new Vector2(zero.X, zero.Y), texCoord3: new Vector2(zero.Z, zero.Y), texCoord4: new Vector2(zero.Z, zero.W));
		faceTextureSlot = obj.GetFaceTextureSlot(2, value);
		zero.X = (float)(faceTextureSlot % textureSlotCount) / (float)textureSlotCount;
		zero.Y = (float)(faceTextureSlot / textureSlotCount) / (float)textureSlotCount;
		zero.W = zero.Y + 1f / (float)textureSlotCount;
		zero.Z = zero.X + 1f / (float)textureSlotCount;
		texturedBatch3D.QueueQuad(color: Color.MultiplyColorOnly(color, LightingManager.CalculateLighting(matrix.Forward)), p1: v5, p2: v6, p3: v8, p4: v7, texCoord1: new Vector2(zero.Z, zero.W), texCoord2: new Vector2(zero.X, zero.W), texCoord3: new Vector2(zero.X, zero.Y), texCoord4: new Vector2(zero.Z, zero.Y));
		faceTextureSlot = obj.GetFaceTextureSlot(5, value);
		zero.X = (float)(faceTextureSlot % textureSlotCount) / (float)textureSlotCount;
		zero.Y = (float)(faceTextureSlot / textureSlotCount) / (float)textureSlotCount;
		zero.W = zero.Y + 1f / (float)textureSlotCount;
		zero.Z = zero.X + 1f / (float)textureSlotCount;
		texturedBatch3D.QueueQuad(color: Color.MultiplyColorOnly(color, LightingManager.CalculateLighting(-matrix.Up)), p1: v, p2: v2, p3: v6, p4: v5, texCoord1: new Vector2(zero.X, zero.Y), texCoord2: new Vector2(zero.Z, zero.Y), texCoord3: new Vector2(zero.Z, zero.W), texCoord4: new Vector2(zero.X, zero.W));
		faceTextureSlot = obj.GetFaceTextureSlot(4, value);
		zero.X = (float)(faceTextureSlot % textureSlotCount) / (float)textureSlotCount;
		zero.Y = (float)(faceTextureSlot / textureSlotCount) / (float)textureSlotCount;
		zero.W = zero.Y + 1f / (float)textureSlotCount;
		zero.Z = zero.X + 1f / (float)textureSlotCount;
		texturedBatch3D.QueueQuad(color: Color.MultiplyColorOnly(topColor, LightingManager.CalculateLighting(matrix.Up)), p1: v3, p2: v7, p3: v8, p4: v4, texCoord1: new Vector2(zero.X, zero.W), texCoord2: new Vector2(zero.X, zero.Y), texCoord3: new Vector2(zero.Z, zero.Y), texCoord4: new Vector2(zero.Z, zero.W));
		faceTextureSlot = obj.GetFaceTextureSlot(1, value);
		zero.X = (float)(faceTextureSlot % textureSlotCount) / (float)textureSlotCount;
		zero.Y = (float)(faceTextureSlot / textureSlotCount) / (float)textureSlotCount;
		zero.W = zero.Y + 1f / (float)textureSlotCount;
		zero.Z = zero.X + 1f / (float)textureSlotCount;
		texturedBatch3D.QueueQuad(color: Color.MultiplyColorOnly(color, LightingManager.CalculateLighting(-matrix.Right)), p1: v, p2: v5, p3: v7, p4: v3, texCoord1: new Vector2(zero.Z, zero.W), texCoord2: new Vector2(zero.X, zero.W), texCoord3: new Vector2(zero.X, zero.Y), texCoord4: new Vector2(zero.Z, zero.Y));
		faceTextureSlot = obj.GetFaceTextureSlot(3, value);
		zero.X = (float)(faceTextureSlot % textureSlotCount) / (float)textureSlotCount;
		zero.Y = (float)(faceTextureSlot / textureSlotCount) / (float)textureSlotCount;
		zero.W = zero.Y + 1f / (float)textureSlotCount;
		zero.Z = zero.X + 1f / (float)textureSlotCount;
		texturedBatch3D.QueueQuad(color: Color.MultiplyColorOnly(color, LightingManager.CalculateLighting(matrix.Right)), p1: v2, p2: v4, p3: v8, p4: v6, texCoord1: new Vector2(zero.X, zero.W), texCoord2: new Vector2(zero.X, zero.Y), texCoord3: new Vector2(zero.Z, zero.Y), texCoord4: new Vector2(zero.Z, zero.W));
	}

	public static void DrawFlatOrImageExtrusionBlock(PrimitivesRenderer3D primitivesRenderer, int value, float size, ref Matrix matrix, Texture2D texture, Color color, bool isEmissive, DrawBlockEnvironmentData environmentData)
	{
		environmentData = environmentData ?? m_defaultEnvironmentData;
		if (DrawImageExtrusionEnabled && texture == null && !isEmissive && (environmentData.DrawBlockMode == DrawBlockMode.FirstPerson || environmentData.DrawBlockMode == DrawBlockMode.ThirdPerson))
		{
			DrawImageExtrusionBlock(primitivesRenderer, value, size, ref matrix, color, environmentData);
		}
		else
		{
			DrawFlatBlock(primitivesRenderer, value, size, ref matrix, texture, color, isEmissive, environmentData);
		}
	}

	public static void DrawFlatBlock(PrimitivesRenderer3D primitivesRenderer, int value, float size, ref Matrix matrix, Texture2D texture, Color color, bool isEmissive, DrawBlockEnvironmentData environmentData)
	{
		environmentData = environmentData ?? m_defaultEnvironmentData;
		int num = Terrain.ExtractContents(value);
		Block obj = Blocks[num];
		Vector4 zero = Vector4.Zero;
		int textureSlotCount = obj.GetTextureSlotCount(value);
		int faceTextureSlot = obj.GetFaceTextureSlot(-1, value);
		zero.X = (float)(faceTextureSlot % textureSlotCount) / (float)textureSlotCount;
		zero.Y = (float)(faceTextureSlot / textureSlotCount) / (float)textureSlotCount;
		zero.W = zero.Y + 1f / (float)textureSlotCount;
		zero.Z = zero.X + 1f / (float)textureSlotCount;
		if (texture == null)
		{
			texture = ((environmentData.SubsystemTerrain != null) ? environmentData.SubsystemTerrain.SubsystemAnimatedTextures.AnimatedBlocksTexture : BlocksTexturesManager.DefaultBlocksTexture);
		}
		if (!isEmissive)
		{
			float s = LightingManager.LightIntensityByLightValue[environmentData.Light];
			color = Color.MultiplyColorOnly(color, s);
		}
		Vector3 translation = matrix.Translation;
		Vector3 vector;
		Vector3 vector2;
		if (environmentData.BillboardDirection.HasValue)
		{
			vector = Vector3.Normalize(Vector3.Cross(environmentData.BillboardDirection.Value, Vector3.UnitY));
			vector2 = -Vector3.Normalize(Vector3.Cross(environmentData.BillboardDirection.Value, vector));
		}
		else
		{
			vector = matrix.Right;
			vector2 = matrix.Up;
		}
		Vector3 v = translation + 0.85f * size * (-vector - vector2);
		Vector3 v2 = translation + 0.85f * size * (vector - vector2);
		Vector3 v3 = translation + 0.85f * size * (-vector + vector2);
		Vector3 v4 = translation + 0.85f * size * (vector + vector2);
		if (environmentData.ViewProjectionMatrix.HasValue)
		{
			Matrix m = environmentData.ViewProjectionMatrix.Value;
			Vector3.Transform(ref v, ref m, out v);
			Vector3.Transform(ref v2, ref m, out v2);
			Vector3.Transform(ref v3, ref m, out v3);
			Vector3.Transform(ref v4, ref m, out v4);
		}
		TexturedBatch3D texturedBatch3D = primitivesRenderer.TexturedBatch(texture, useAlphaTest: true, 0, null, RasterizerState.CullCounterClockwiseScissor, BlendState.AlphaBlend, SamplerState.PointClamp);
		texturedBatch3D.QueueQuad(v, v3, v4, v2, new Vector2(zero.X, zero.W), new Vector2(zero.X, zero.Y), new Vector2(zero.Z, zero.Y), new Vector2(zero.Z, zero.W), color);
		if (!environmentData.BillboardDirection.HasValue)
		{
			texturedBatch3D.QueueQuad(v, v2, v4, v3, new Vector2(zero.X, zero.W), new Vector2(zero.Z, zero.W), new Vector2(zero.Z, zero.Y), new Vector2(zero.X, zero.Y), color);
		}
	}

	public static void DrawImageExtrusionBlock(PrimitivesRenderer3D primitivesRenderer, int value, float size, ref Matrix matrix, Color color, DrawBlockEnvironmentData environmentData)
	{
		environmentData = environmentData ?? m_defaultEnvironmentData;
		int terrainRaycastResult = Terrain.ExtractContents(value);
		Block position = Blocks[terrainRaycastResult];
		try
		{
			Image dynamicArray = ((environmentData.SubsystemTerrain == null) ? ((Image)BlocksTexturesManager.DefaultBlocksTexture.Tag) : ((Image)environmentData.SubsystemTerrain.SubsystemAnimatedTextures.AnimatedBlocksTexture.Tag));
			BlockMesh imageExtrusionBlockMesh = GetImageExtrusionBlockMesh(dynamicArray, position.GetFaceTextureSlot(-1, value));
			DrawMeshBlock(primitivesRenderer, imageExtrusionBlockMesh, color, 1.7f * size, ref matrix, environmentData);
		}
		catch (Exception)
		{
		}
	}

	public static BlockMesh GetImageExtrusionBlockMesh(Image image, int slot)
	{
		ImageExtrusionKey key = new ImageExtrusionKey
		{
			Image = image,
			Slot = slot
		};
		if (!m_imageExtrusionsCache.TryGetValue(key, out var value))
		{
			value = new BlockMesh();
			int num = (int)MathUtils.Round(m_slotTexCoords[slot].X * (float)image.Width);
			int num2 = (int)MathUtils.Round(m_slotTexCoords[slot].Y * (float)image.Height);
			int num3 = (int)MathUtils.Round(m_slotTexCoords[slot].Z * (float)image.Width);
			int num4 = (int)MathUtils.Round(m_slotTexCoords[slot].W * (float)image.Height);
			int num5 = MathUtils.Max(num3 - num, num4 - num2);
			value.AppendImageExtrusion(image, new Engine.Rectangle(num, num2, num3 - num, num4 - num2), new Vector3(1f / (float)num5, 1f / (float)num5, 1f / 12f), Color.White, 0);
			m_imageExtrusionsCache.Add(key, value);
		}
		return value;
	}

	public static void DrawMeshBlock(PrimitivesRenderer3D primitivesRenderer, BlockMesh blockMesh, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		environmentData = environmentData ?? m_defaultEnvironmentData;
		Texture2D texture = ((environmentData.SubsystemTerrain != null) ? environmentData.SubsystemTerrain.SubsystemAnimatedTextures.AnimatedBlocksTexture : BlocksTexturesManager.DefaultBlocksTexture);
		DrawMeshBlock(primitivesRenderer, blockMesh, texture, Color.White, size, ref matrix, environmentData);
	}

	public static void DrawMeshBlock(PrimitivesRenderer3D primitivesRenderer, BlockMesh blockMesh, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		environmentData = environmentData ?? m_defaultEnvironmentData;
		Texture2D texture = ((environmentData.SubsystemTerrain != null) ? environmentData.SubsystemTerrain.SubsystemAnimatedTextures.AnimatedBlocksTexture : BlocksTexturesManager.DefaultBlocksTexture);
		DrawMeshBlock(primitivesRenderer, blockMesh, texture, color, size, ref matrix, environmentData);
	}

	public static void DrawMeshBlock(PrimitivesRenderer3D primitivesRenderer, BlockMesh blockMesh, Texture2D texture, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		environmentData = environmentData ?? m_defaultEnvironmentData;
		float num = LightingManager.LightIntensityByLightValue[environmentData.Light];
		Vector4 vector = new Vector4(color);
		vector.X *= num;
		vector.Y *= num;
		vector.Z *= num;
		bool flag = vector == Vector4.One;
		TexturedBatch3D texturedBatch3D = primitivesRenderer.TexturedBatch(texture, useAlphaTest: true, 0, null, RasterizerState.CullCounterClockwiseScissor, null, SamplerState.PointClamp);
		bool flag2 = false;
		Matrix m = ((!environmentData.ViewProjectionMatrix.HasValue) ? matrix : (matrix * environmentData.ViewProjectionMatrix.Value));
		if (size != 1f)
		{
			m = Matrix.CreateScale(size) * m;
		}
		if (m.M14 != 0f || m.M24 != 0f || m.M34 != 0f || m.M44 != 1f)
		{
			flag2 = true;
		}
		int count = blockMesh.Vertices.Count;
		BlockMeshVertex[] array = blockMesh.Vertices.Array;
		int count2 = blockMesh.Indices.Count;
		ushort[] array2 = blockMesh.Indices.Array;
		DynamicArray<VertexPositionColorTexture> triangleVertices = texturedBatch3D.TriangleVertices;
		int count3 = triangleVertices.Count;
		int count4 = triangleVertices.Count;
		triangleVertices.Count += count;
		for (int i = 0; i < count; i++)
		{
			BlockMeshVertex blockMeshVertex = array[i];
			if (flag2)
			{
				Vector4 v = new Vector4(blockMeshVertex.Position, 1f);
				Vector4.Transform(ref v, ref m, out v);
				float num2 = 1f / v.W;
				blockMeshVertex.Position = new Vector3(v.X * num2, v.Y * num2, v.Z * num2);
			}
			else
			{
				Vector3.Transform(ref blockMeshVertex.Position, ref m, out blockMeshVertex.Position);
			}
			if (flag || blockMeshVertex.IsEmissive)
			{
				triangleVertices.Array[count4++] = new VertexPositionColorTexture(blockMeshVertex.Position, blockMeshVertex.Color, blockMeshVertex.TextureCoordinates);
				continue;
			}
			Color color2 = new Color((byte)((float)(int)blockMeshVertex.Color.R * vector.X), (byte)((float)(int)blockMeshVertex.Color.G * vector.Y), (byte)((float)(int)blockMeshVertex.Color.B * vector.Z), (byte)((float)(int)blockMeshVertex.Color.A * vector.W));
			triangleVertices.Array[count4++] = new VertexPositionColorTexture(blockMeshVertex.Position, color2, blockMeshVertex.TextureCoordinates);
		}
		DynamicArray<ushort> triangleIndices = texturedBatch3D.TriangleIndices;
		int count5 = triangleIndices.Count;
		triangleIndices.Count += count2;
		for (int j = 0; j < count2; j++)
		{
			triangleIndices.Array[count5++] = (ushort)(count3 + array2[j]);
		}
	}

	public static int DamageItem(int value, int damageCount)
	{
		int num = Terrain.ExtractContents(value);
		Block block = Blocks[num];
		if (block.Durability < 0)
		{
			return value;
		}
		int num2 = block.GetDamage(value) + damageCount;
		if (num2 <= block.Durability)
		{
			return block.SetDamage(value, num2);
		}
		return block.GetDamageDestructionValue(value);
	}

	public static void LoadBlocksData(string data)
	{
		Dictionary<Block, bool> cellContents = new Dictionary<Block, bool>();
		data = data.Replace("\r", string.Empty);
		string[] array = data.Split(new char[1] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
		string[] array2 = null;
		for (int i = 0; i < array.Length; i++)
		{
			if (string.IsNullOrEmpty(array[i]))
			{
				continue;
			}
			string[] array3 = array[i].Split(';');
			if (i == 0)
			{
				array2 = new string[array3.Length - 1];
				Array.Copy(array3, 1, array2, 0, array3.Length - 1);
				continue;
			}
			if (array3.Length != array2.Length + 1)
			{
				throw new InvalidOperationException(string.Format(LanguageControl.Get("BlocksManager", 2), (array3.Length != 0) ? array3[0] : LanguageControl.Unknown));
			}
			string typeName = array3[0];
			if (string.IsNullOrEmpty(typeName))
			{
				continue;
			}
			Block block = m_blocks.FirstOrDefault((Block v) => v.GetType().Name == typeName);
			if (block == null)
			{
				throw new InvalidOperationException(string.Format(LanguageControl.Get("BlocksManager", 3), typeName));
			}
			cellContents.Add(block, value: true);
			Dictionary<string, FieldInfo> dictionary = new Dictionary<string, FieldInfo>();
			foreach (FieldInfo runtimeField in block.GetType().GetRuntimeFields())
			{
				if (runtimeField.IsPublic && !runtimeField.IsStatic)
				{
					dictionary.Add(runtimeField.Name, runtimeField);
				}
			}
			for (int num = 1; num < array3.Length; num++)
			{
				string text = array2[num - 1];
				string text2 = array3[num];
				if (string.IsNullOrEmpty(text2))
				{
					continue;
				}
				if (!dictionary.TryGetValue(text, out var value))
				{
					throw new InvalidOperationException(string.Format(LanguageControl.Get("BlocksManager", 5), text));
				}
				object value2;
				if (text2.StartsWith("#"))
				{
					string refTypeName = text2.Substring(1);
					object obj = ((!string.IsNullOrEmpty(refTypeName)) ? ((object)(m_blocks.FirstOrDefault((Block v) => v.GetType().Name == refTypeName) ?? throw new InvalidOperationException(string.Format(LanguageControl.Get("BlocksManager", 6), refTypeName))).BlockIndex) : ((object)block.BlockIndex));
					value2 = obj;
				}
				else
				{
					value2 = HumanReadableConverter.ConvertFromString(value.FieldType, text2);
				}
				value.SetValue(block, value2);
			}
		}
	}

	public static void CalculateSlotTexCoordTables()
	{
		for (int num = 0; num < 256; num++)
		{
			m_slotTexCoords[num] = TextureSlotToTextureCoords(num);
		}
	}

	public static Vector4 TextureSlotToTextureCoords(int slot)
	{
		int num = slot % 16;
		int num2 = slot / 16;
		float x = ((float)num + 0.001f) / 16f;
		float y = ((float)num2 + 0.001f) / 16f;
		float z = ((float)(num + 1) - 0.001f) / 16f;
		float w = ((float)(num2 + 1) - 0.001f) / 16f;
		return new Vector4(x, y, z, w);
	}

	public static Vector4[] GetslotTexCoords(int textureSlotCount)
	{
		int value = textureSlotCount * textureSlotCount;
		Vector4[] array = new Vector4[value];
		for (int i = 0; i < value; i++)
		{
			int num = i % textureSlotCount;
			int data = i / textureSlotCount;
			float saplingData = ((float)num + 0.001f) / (float)textureSlotCount;
			float y = ((float)data + 0.001f) / (float)textureSlotCount;
			float z = ((float)(num + 1) - 0.001f) / (float)textureSlotCount;
			float w = ((float)(data + 1) - 0.001f) / (float)textureSlotCount;
			array[i] = new Vector4(saplingData, y, z, w);
		}
		return array;
	}

	public static Block GetBlock(string ModSpace, string TypeFullName)
	{
		if (ModsManager.GetModEntity(ModSpace, out var num))
		{
			Block block = num.Blocks.Find((Block p) => p.GetType().Name == TypeFullName);
			if (block != null)
			{
				return block;
			}
		}
		return null;
	}
}
