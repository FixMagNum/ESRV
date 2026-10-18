using Engine;

namespace Game;

public class StrawFenceBlock : FenceBlock
{
	public const int Index = 317;

	public StrawFenceBlock()
		: base("Models/StoneFence", doubleSidedPlanks: false, useAlphaTest: false, 23, Color.White, Color.White)
	{
	}

	public override void Initialize()
	{
		base.Initialize();
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < m_collisionBoxes[i].Length; j++)
			{
				BoundingBox[] array = m_collisionBoxes[i];
				int num = j;
				array[num].Min.X = array[num].Min.X - 0.05f;
				BoundingBox[] array2 = m_collisionBoxes[i];
				int num2 = j;
				array2[num2].Max.X = array2[num2].Max.X + 0.05f;
				BoundingBox[] array3 = m_collisionBoxes[i];
				int num3 = j;
				array3[num3].Min.Z = array3[num3].Min.Z - 0.05f;
				BoundingBox[] array4 = m_collisionBoxes[i];
				int num4 = j;
				array4[num4].Max.Z = array4[num4].Max.Z + 0.05f;
			}
		}
	}
}
