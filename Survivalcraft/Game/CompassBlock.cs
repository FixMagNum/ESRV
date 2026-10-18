using System;
using Engine;
using Engine.Graphics;

namespace Game;

public class CompassBlock : Block
{
	public const int Index = 117;

	public BlockMesh m_caseMesh;

	public BlockMesh m_pointerMesh;

	public BlockMesh m_rotationCaseMesh;

	public override void Initialize()
	{
		Model model = ContentManager.Get<Model>("Models/Compass");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Case").ParentBone);
		Matrix boneAbsoluteTransform2 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Pointer").ParentBone);
		Matrix boneAbsoluteTransform3 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("RotationCase").ParentBone);
		m_caseMesh.AppendModelMeshPart(model.FindMesh("Case").MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.01f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: true, flipNormals: false, Color.White);
		m_rotationCaseMesh.AppendModelMeshPart(model.FindMesh("RotationCase").MeshParts[0], boneAbsoluteTransform3 * Matrix.CreateTranslation(0f, -0.01f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: true, flipNormals: false, Color.White);
		m_pointerMesh.AppendModelMeshPart(model.FindMesh("Pointer").MeshParts[0], boneAbsoluteTransform2 * Matrix.CreateTranslation(0f, -0.01f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		base.Initialize();
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		float radians = 0f;
		float radians2 = 0f;
		if (environmentData != null && environmentData.SubsystemTerrain != null)
		{
			Vector3 forward = environmentData.InWorldMatrix.Forward;
			Vector3 translation = environmentData.InWorldMatrix.Translation;
			Vector2 vector = new Vector2(forward.X, forward.Z);
			Vector2 vector2 = SubsystemWeather.WindDirectionVectors["N"];
			float num = MathUtils.Atan2(vector2.Y, vector2.X);
			radians2 = MathUtils.NormalizeAngle(MathUtils.Atan2(vector.Y, vector.X) - num);
			ComponentPlayer mainPlayer = environmentData.SubsystemTerrain.Project.FindSubsystem<SubsystemPlayers>(throwOnError: true).MainPlayer;
			if (mainPlayer != null)
			{
				Guid playerGUID = mainPlayer.PlayerData.PlayerGUID;
				Vector3 vector3 = environmentData.SubsystemTerrain.Project.FindSubsystem<SubsystemMagnetBlockBehavior>(throwOnError: true).FindNearestCompassTarget(translation, playerGUID);
				if (vector3 != Vector3.Zero)
				{
					Vector3 vector4 = vector3 - translation;
					float num2 = MathUtils.Atan2(vector4.X, vector4.Z);
					float num3 = MathUtils.Atan2(forward.X, forward.Z);
					radians = MathUtils.NormalizeAngle(num2 - num3 + (float)Math.PI);
				}
			}
		}
		Matrix matrix2 = matrix;
		Matrix matrix3 = Matrix.CreateRotationY(radians2) * matrix;
		Matrix matrix4 = Matrix.CreateRotationY(radians) * matrix;
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_caseMesh, color, size * 6f, ref matrix2, environmentData);
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_rotationCaseMesh, color, size * 6f, ref matrix3, environmentData);
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_pointerMesh, color, size * 6f, ref matrix4, environmentData);
	}

	public CompassBlock()
	{
		m_caseMesh = new BlockMesh();
		m_pointerMesh = new BlockMesh();
		m_rotationCaseMesh = new BlockMesh();
	}
}
