using System;
using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class PistonBlock : Block, IElectricElementBlock
{
	public const int Index = 237;

	public BlockMesh[] m_standaloneBlockMeshes = new BlockMesh[4];

	public BlockMesh[] m_blockMeshesByData = new BlockMesh[48];

	public new static string fName;

	public override void Initialize()
	{
		Model componentSickness = ContentManager.Get<Model>("Models/Pistons");
		for (PistonMode f = PistonMode.Pushing; f <= PistonMode.StrictPulling; f++)
		{
			for (int num = 0; num < 2; num++)
			{
				string f2 = ((num == 0) ? "PistonRetracted" : "PistonExtended");
				Matrix f3 = BlockMesh.GetBoneAbsoluteTransform(componentSickness.FindMesh(f2).ParentBone);
				for (int x = 0; x < 6; x++)
				{
					int forwardVector = SetFace(SetIsExtended(SetMode(0, f), num != 0), x);
					Matrix rightVector;
					if (x < 4)
					{
						rightVector = Matrix.CreateTranslation(0f, -0.5f, 0f) * Matrix.CreateRotationY((float)x * (float)Math.PI / 2f + (float)Math.PI) * Matrix.CreateTranslation(0.5f, 0.5f, 0.5f);
					}
					else
					{
						rightVector = ((x != 4) ? (Matrix.CreateTranslation(0f, -0.5f, 0f) * Matrix.CreateRotationX(-(float)Math.PI / 2f) * Matrix.CreateTranslation(0.5f, 0.5f, 0.5f)) : (Matrix.CreateTranslation(0f, -0.5f, 0f) * Matrix.CreateRotationX((float)Math.PI / 2f) * Matrix.CreateTranslation(0.5f, 0.5f, 0.5f)));
					}
					m_blockMeshesByData[forwardVector] = new BlockMesh();
					m_blockMeshesByData[forwardVector].AppendModelMeshPart(componentSickness.FindMesh(f2).MeshParts[0], f3 * rightVector, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
					if (num == 0)
					{
						switch (f)
						{
						case PistonMode.StrictPulling:
							m_blockMeshesByData[forwardVector].TransformTextureCoordinates(Matrix.CreateTranslation(0f, 0.125f, 0f), 1 << x);
							break;
						case PistonMode.Pulling:
							m_blockMeshesByData[forwardVector].TransformTextureCoordinates(Matrix.CreateTranslation(0f, 0.0625f, 0f), 1 << x);
							break;
						}
					}
				}
			}
			Matrix funnyTimer = BlockMesh.GetBoneAbsoluteTransform(componentSickness.FindMesh("PistonRetracted").ParentBone);
			m_standaloneBlockMeshes[(int)f] = new BlockMesh();
			m_standaloneBlockMeshes[(int)f].AppendModelMeshPart(componentSickness.FindMesh("PistonRetracted").MeshParts[0], funnyTimer * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			switch (f)
			{
			case PistonMode.StrictPulling:
				m_standaloneBlockMeshes[(int)f].TransformTextureCoordinates(Matrix.CreateTranslation(0f, 0.125f, 0f), 4);
				break;
			case PistonMode.Pulling:
				m_standaloneBlockMeshes[(int)f].TransformTextureCoordinates(Matrix.CreateTranslation(0f, 0.0625f, 0f), 4);
				break;
			}
		}
	}

	public override bool IsFaceTransparent(SubsystemTerrain subsystemTerrain, int face, int value)
	{
		int data = Terrain.ExtractData(value);
		int face2 = GetFace(data);
		if (GetIsExtended(data) && face != face2)
		{
			return face != CellFace.OppositeFace(face2);
		}
		return false;
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		int num = Terrain.ExtractData(value) & 0x3F;
		if (num < m_blockMeshesByData.Length && m_blockMeshesByData[num] != null)
		{
			generator.GenerateShadedMeshVertices(this, x, y, z, m_blockMeshesByData[num], Color.White, null, null, geometry.SubsetOpaque);
		}
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		int mode = (int)GetMode(Terrain.ExtractData(value));
		if (mode < m_standaloneBlockMeshes.Length && m_standaloneBlockMeshes[mode] != null)
		{
			BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMeshes[mode], color, 1f * size, ref matrix, environmentData);
		}
	}

	public ElectricElement CreateElectricElement(SubsystemElectricity subsystemElectricity, int value, int x, int y, int z)
	{
		return new PistonElectricElement(subsystemElectricity, new Point3(x, y, z));
	}

	public ElectricConnectorType? GetConnectorType(SubsystemTerrain terrain, int value, int face, int connectorFace, int x, int y, int z)
	{
		return ElectricConnectorType.Input;
	}

	public int GetConnectionMask(int value)
	{
		return int.MaxValue;
	}

	public override IEnumerable<int> GetCreativeValues()
	{
		yield return Terrain.MakeBlockValue(237, 0, SetMode(SetMaxExtension(0, 7), PistonMode.Pushing));
		yield return Terrain.MakeBlockValue(237, 0, SetMode(SetMaxExtension(0, 7), PistonMode.Pulling));
		yield return Terrain.MakeBlockValue(237, 0, SetMode(SetMaxExtension(0, 7), PistonMode.StrictPulling));
	}

	public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value)
	{
		PistonMode mode = GetMode(Terrain.ExtractData(value));
		return LanguageControl.GetBlock($"{fName}:{mode switch
		{
			PistonMode.StrictPulling => 452, 
			PistonMode.Pulling => 450, 
			_ => 448, 
		}}", "DisplayName");
	}

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, TerrainRaycastResult raycastResult)
	{
		Vector3 lastWalkOrder = Matrix.CreateFromQuaternion(componentMiner.ComponentCreature.ComponentCreatureModel.EyeRotation).Forward;
		float vector = float.PositiveInfinity;
		int vector2 = 0;
		for (int flag = 0; flag < 6; flag++)
		{
			float vector4 = Vector3.Dot(CellFace.FaceToVector3(flag), lastWalkOrder);
			if (vector4 < vector)
			{
				vector = vector4;
				vector2 = flag;
			}
		}
		int vector5 = Terrain.ExtractData(value);
		return new BlockPlacementData
		{
			Value = Terrain.MakeBlockValue(BlockIndex, 0, SetFace(vector5, vector2)),
			CellFace = raycastResult.CellFace
		};
	}

	public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
	{
		int data = Terrain.ExtractData(oldValue);
		dropValues.Add(new BlockDropValue
		{
			Value = Terrain.MakeBlockValue(237, 0, SetFace(SetIsExtended(data, isExtended: false), 0)),
			Count = 1
		});
		showDebris = true;
	}

	public static bool GetIsExtended(int data)
	{
		return (data & 1) != 0;
	}

	public static int SetIsExtended(int data, bool isExtended)
	{
		return (data & -2) | (isExtended ? 1 : 0);
	}

	public static PistonMode GetMode(int data)
	{
		return (PistonMode)((data >> 1) & 3);
	}

	public static int SetMode(int data, PistonMode mode)
	{
		return (data & -7) | ((int)(mode & (PistonMode)3) << 1);
	}

	public static int GetFace(int data)
	{
		return (data >> 3) & 7;
	}

	public static int SetFace(int data, int face)
	{
		return (data & -57) | ((face & 7) << 3);
	}

	public static int GetMaxExtension(int data)
	{
		return (data >> 6) & 7;
	}

	public static int SetMaxExtension(int data, int maxExtension)
	{
		return (data & -449) | ((maxExtension & 7) << 6);
	}

	public static int GetPullCount(int data)
	{
		return (data >> 9) & 7;
	}

	public static int SetPullCount(int data, int pullCount)
	{
		return (data & -3585) | ((pullCount & 7) << 9);
	}

	public static int GetSpeed(int data)
	{
		return (data >> 12) & 0xF;
	}

	public static int SetSpeed(int data, int speed)
	{
		return (int)((data & 0xFFFF0FFFu) | ((speed & 0xF) << 12));
	}

	public static bool GetWaitingForTerrain(int data)
	{
		return data >> 14 != 0;
	}

	public static int SetWaitingForTerrain(int data, bool value)
	{
		return (data & -16385) | ((value ? 1 : 0) << 14);
	}

	static PistonBlock()
	{
		fName = "PistonBlock";
	}
}
