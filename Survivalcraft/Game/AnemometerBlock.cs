using Engine;
using Engine.Graphics;

namespace Game;

public class AnemometerBlock : Block
{
	public const int Index = 430;

	public BlockMesh m_caseMesh;

	public BlockMesh m_pointerMesh;

	public override void Initialize()
	{
		Model model = ContentManager.Get<Model>("Models/Anemometer");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Case").ParentBone);
		Matrix boneAbsoluteTransform2 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Pointer").ParentBone);
		Matrix matrix = Matrix.CreateTranslation(0f, -0.01f, 0f);
		m_caseMesh.AppendModelMeshPart(model.FindMesh("Case").MeshParts[0], boneAbsoluteTransform * matrix, makeEmissive: false, flipWindingOrder: false, doubleSided: true, flipNormals: false, Color.White);
		m_pointerMesh.AppendModelMeshPart(model.FindMesh("Pointer").MeshParts[0], boneAbsoluteTransform2 * matrix, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		base.Initialize();
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator g, TerrainGeometry geom, int value, int x, int y, int z)
	{
	}

	public override void DrawBlock(PrimitivesRenderer3D pr, int value, Color color, float size, ref Matrix m, DrawBlockEnvironmentData env)
	{
		float radians = 0f;
		SubsystemTerrain subsystemTerrain = env?.SubsystemTerrain;
		if (subsystemTerrain != null)
		{
			SubsystemAnemometerBlockBehavior subsystemAnemometerBlockBehavior = subsystemTerrain.Project.FindSubsystem<SubsystemAnemometerBlockBehavior>(throwOnError: true);
			if (subsystemAnemometerBlockBehavior != null)
			{
				radians = subsystemAnemometerBlockBehavior.GetPointerAngleForRender(env.InWorldMatrix.Translation);
			}
		}
		Matrix matrix = m;
		Matrix matrix2 = Matrix.CreateRotationY(radians) * m;
		BlocksManager.DrawMeshBlock(pr, m_caseMesh, color, size * 6f, ref matrix, env);
		BlocksManager.DrawMeshBlock(pr, m_pointerMesh, color, size * 6f, ref matrix2, env);
	}

	public AnemometerBlock()
	{
		m_caseMesh = new BlockMesh();
		m_pointerMesh = new BlockMesh();
	}
}
