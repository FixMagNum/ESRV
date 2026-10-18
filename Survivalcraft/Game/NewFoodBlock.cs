using Engine;
using Engine.Graphics;

namespace Game;

public abstract class NewFoodBlock : Block
{
	public static int m_compostValue;

	public BlockMesh m_standaloneBlockMesh = new BlockMesh();

	public string m_modelName;

	public Matrix m_tcTransform;

	public int m_rottenValue;

	public const int FrozenBit = 65536;

	public new string FoodType { get; set; }

	public override void Initialize()
	{
		Model componentBody = ContentManager.Get<Model>(m_modelName);
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(componentBody.Meshes[0].ParentBone);
		m_standaloneBlockMesh.AppendModelMeshPart(componentBody.Meshes[0].MeshParts[0], boneAbsoluteTransform, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_standaloneBlockMesh.TransformTextureCoordinates(m_tcTransform);
		base.Initialize();
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		if (GetFrozen(Terrain.ExtractData(value)))
		{
			color *= new Color(160, 200, 255);
		}
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color, 2f * size, ref matrix, environmentData);
	}

	public override int GetDamageDestructionValue(int value)
	{
		return m_rottenValue;
	}

	public NewFoodBlock(string modelName, Matrix tcTransform, int rottenValue)
	{
		m_modelName = modelName;
		m_tcTransform = tcTransform;
		m_rottenValue = rottenValue;
	}

	static NewFoodBlock()
	{
		m_compostValue = Terrain.MakeBlockValue(369, 0, 0);
	}

	public static bool GetFrozen(int data)
	{
		return (data & 0x10000) != 0;
	}

	public static int SetFrozen(int data, bool frozen)
	{
		if (frozen)
		{
			return data | 0x10000;
		}
		return data & -65537;
	}

	public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value)
	{
		string displayName = base.GetDisplayName(subsystemTerrain, value);
		if (FoodBlock.GetFrozen(Terrain.ExtractData(value)))
		{
			return LanguageControl.Get("FoodBlock", "Frozen") + " " + displayName;
		}
		return displayName;
	}
}
