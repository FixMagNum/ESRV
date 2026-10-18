using System.Collections.Generic;
using Engine;

namespace Game;

public class SeaUrchinBlock : FocalorsAttachedBlock
{
	public const int Index = 226;

	protected override string GetModelName()
	{
		return "Models/SeaUrchin";
	}

	protected override string GetMeshName()
	{
		return "Urchin";
	}

	protected override float GetScale()
	{
		return 3f;
	}

	protected override Color GetColor(int variant)
	{
		return variant switch
		{
			0 => new Color(20, 20, 20), 
			1 => new Color(50, 20, 20), 
			2 => new Color(80, 30, 30), 
			_ => new Color(20, 20, 40), 
		};
	}

	public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
	{
		int variant = FocalorsAttachedBlock.GetVariant(Terrain.ExtractData(oldValue));
		dropValues.Add(new BlockDropValue
		{
			Value = Terrain.MakeBlockValue(BlockIndex),
			Count = MathUtils.Clamp(variant + 1, 1, 3)
		});
		showDebris = true;
	}
}
