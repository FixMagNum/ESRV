using Engine;
using Engine.Graphics;

namespace Game;

public class SeagrassBlock : WaterPlantBlock
{
	public const int Index = 233;

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		Color color2 = BlockColorsMap.SeagrassColorsMap.Lookup(environmentData.Temperature, environmentData.Humidity);
		BlocksManager.DrawFlatBlock(primitivesRenderer, value, size, ref matrix, null, color * color2, isEmissive: false, environmentData);
	}

	protected override Color GetColor(Terrain terrain, int x, int y, int z)
	{
		return BlockColorsMap.SeagrassColorsMap.Lookup(terrain, x, y, z);
	}

	public override int GetFaceTextureSlot(int face, int value)
	{
		return 105;
	}
}
