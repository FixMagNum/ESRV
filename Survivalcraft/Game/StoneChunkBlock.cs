using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class StoneChunkBlock : ChunkBlock
{
	public enum StoneKind
	{
		Normal,
		Heated,
		BurnedFood
	}

	public const int Index = 79;

	public BlockMesh m_heatedBlockMesh = new BlockMesh();

	public BlockMesh m_burnedFoodBlockMesh = new BlockMesh();

	public StoneChunkBlock()
		: base(Matrix.CreateScale(0.75f) * Matrix.CreateRotationX(0f) * Matrix.CreateRotationZ(1f), Matrix.CreateScale(0.75f) * Matrix.CreateTranslation(0.1875f, 0.0625f, 0f), new Color(255, 255, 255), smooth: true)
	{
	}

	public static StoneKind GetKind(int data)
	{
		return (StoneKind)(data & 3);
	}

	public static int SetKind(int data, StoneKind kind)
	{
		return (data & -4) | (int)(kind & (StoneKind)3);
	}

	public override IEnumerable<int> GetCreativeValues()
	{
		yield return Terrain.MakeBlockValue(79, 0, SetKind(0, StoneKind.Normal));
		yield return Terrain.MakeBlockValue(79, 0, SetKind(0, StoneKind.Heated));
		yield return Terrain.MakeBlockValue(79, 0, SetKind(0, StoneKind.BurnedFood));
	}

	public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value)
	{
		int size = (int)GetKind(Terrain.ExtractData(value));
		return LanguageControl.GetBlock($"StoneChunkBlock:{size}", "DisplayName");
	}

	public override string GetDescription(int value)
	{
		int kind = (int)GetKind(Terrain.ExtractData(value));
		return LanguageControl.GetBlock($"StoneChunkBlock:{kind}", "Description");
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		switch (GetKind(Terrain.ExtractData(value)))
		{
		case StoneKind.Heated:
		{
			Matrix num = Matrix.CreateScale(0.85f) * matrix;
			BlocksManager.DrawMeshBlock(color: new Color(255, 60, 40), primitivesRenderer: primitivesRenderer, blockMesh: m_heatedBlockMesh, size: size, matrix: ref num, environmentData: environmentData);
			break;
		}
		case StoneKind.BurnedFood:
			BlocksManager.DrawMeshBlock(primitivesRenderer, m_burnedFoodBlockMesh, color, size * 2f, ref matrix, environmentData);
			break;
		default:
			base.DrawBlock(primitivesRenderer, value, color, size, ref matrix, environmentData);
			break;
		}
	}

	public override float GetProjectilePower(int value)
	{
		if (GetKind(Terrain.ExtractData(value)) == StoneKind.Heated)
		{
			return 10f;
		}
		return base.GetProjectilePower(value);
	}

	public override float GetProjectileSpeed(int value)
	{
		if (GetKind(Terrain.ExtractData(value)) == StoneKind.Heated)
		{
			return 22f;
		}
		return base.GetProjectileSpeed(value);
	}

	public override float GetProjectileDamping(int value)
	{
		if (GetKind(Terrain.ExtractData(value)) == StoneKind.Heated)
		{
			return 0.4f;
		}
		return base.GetProjectileDamping(value);
	}

	public override void Initialize()
	{
		base.Initialize();
		m_heatedBlockMesh.AppendBlockMesh(m_standaloneBlockMesh);
		float size = -0.0625f;
		float num = 0.1875f;
		m_heatedBlockMesh.TransformTextureCoordinates(Matrix.CreateTranslation(size, num, 0f));
		m_burnedFoodBlockMesh.AppendBlockMesh(m_standaloneBlockMesh);
		m_burnedFoodBlockMesh.TransformTextureCoordinates(Matrix.CreateTranslation(0.6875f, 0.125f, 0f));
	}
}
