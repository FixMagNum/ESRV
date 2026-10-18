using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class GrassTrapBlock : Block
{
	public const int Index = 87;

	public BlockMesh m_blockMesh = new BlockMesh();

	public BlockMesh m_standaloneBlockMesh = new BlockMesh();

	public BoundingBox[] m_collisionBoxes = new BoundingBox[1];

	public override void Initialize()
	{
		Model model = ContentManager.Get<Model>("Models/GrassTrap");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("GrassTrap").ParentBone);
		Color color = BlockColorsMap.GrassColorsMap.Lookup(8, 15);
		m_blockMesh.AppendModelMeshPart(model.FindMesh("GrassTrap").MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0.5f, 0.72f, 0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_standaloneBlockMesh.AppendModelMeshPart(model.FindMesh("GrassTrap").MeshParts[0], boneAbsoluteTransform, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, color);
		m_collisionBoxes[0] = new BoundingBox(new Vector3(0f, 0.72f, 0f), new Vector3(1f, 0.95f, 1f));
		base.Initialize();
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		SubsystemWeather.Season currentSeason = generator.SubsystemTerrain.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).CurrentSeason;
		Color color = BlockColorsMap.GrassColorsMap.LookupSeasonalGrassColor(currentSeason, generator.Terrain, x, y, z);
		generator.GenerateShadedMeshVertices(this, x, y, z, m_blockMesh, color, null, null, geometry.SubsetOpaque);
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		Color color2 = color * BlockColorsMap.GrassColorsMap.Lookup(environmentData.Temperature, environmentData.Humidity);
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color2, size, ref matrix, environmentData);
	}

	public override BlockDebrisParticleSystem CreateDebrisParticleSystem(SubsystemTerrain sst, Vector3 pos, int value, float str)
	{
		SubsystemWeather.Season currentSeason = sst.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).CurrentSeason;
		int x = Terrain.ToCell(pos.X);
		int y = Terrain.ToCell(pos.Y);
		int z = Terrain.ToCell(pos.Z);
		Color color = BlockColorsMap.GrassColorsMap.LookupSeasonalGrassColor(currentSeason, sst.Terrain, x, y, z);
		return new BlockDebrisParticleSystem(sst, pos, str, DestructionDebrisScale, color, 2);
	}

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
	{
		return m_collisionBoxes;
	}

	public override bool ShouldGenerateFace(SubsystemTerrain subsystemTerrain, int face, int value, int neighborValue)
	{
		if (Terrain.ExtractContents(neighborValue) != 87)
		{
			return base.ShouldGenerateFace(subsystemTerrain, face, value, neighborValue);
		}
		return false;
	}

	public override IEnumerable<CraftingRecipe> GetProceduralCraftingRecipes()
	{
		string text = "oakleaves|birchleaves|spruceleaves|tallspruceleaves|mimosaleaves";
		string text2 = "oakleaves|birchleaves|spruceleaves|tallspruceleaves|mimosaleaves|oakleaves";
		CraftingRecipe craftingRecipe = new CraftingRecipe
		{
			ResultCount = 4,
			ResultValue = Terrain.MakeBlockValue(87, 0, 0),
			RequiredHeatLevel = 0f,
			Description = LanguageControl.GetBlock("GrassTrapBlock", "CRDescription:0")
		};
		craftingRecipe.Ingredients[0] = text;
		craftingRecipe.Ingredients[1] = text2;
		craftingRecipe.Ingredients[3] = "stick";
		craftingRecipe.Ingredients[4] = "stick";
		yield return craftingRecipe;
	}
}
