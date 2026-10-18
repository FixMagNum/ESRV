using System;
using System.Collections.Generic;
using System.Linq;
using Engine;
using Engine.Graphics;
using Game.NetWork;
using Game.NetWork.Packages;

namespace Game;

public class FurnitureBlock : Block, IPaintableBlock, IElectricElementBlock, IWaterloggable
{
	public const int Index = 227;

	public Matrix[] m_matrices = new Matrix[4];

	public int[][] m_facesMaps = new int[4][]
	{
		new int[6] { 0, 1, 2, 3, 4, 5 },
		new int[6] { 1, 2, 3, 0, 4, 5 },
		new int[6] { 2, 3, 0, 1, 4, 5 },
		new int[6] { 3, 0, 1, 2, 4, 5 }
	};

	public int[][] m_reverseFacesMaps = new int[4][]
	{
		new int[6] { 0, 1, 2, 3, 4, 5 },
		new int[6] { 3, 0, 1, 2, 4, 5 },
		new int[6] { 2, 3, 0, 1, 4, 5 },
		new int[6] { 1, 2, 3, 0, 4, 5 }
	};

	public string LangKey;

	public new static string fName;

	public const int WaterLoggedBit = 32768;

	public override void Initialize()
	{
		LangKey = GetType().Name;
		for (int i = 0; i < 4; i++)
		{
			m_matrices[i] = Matrix.CreateTranslation(new Vector3(-0.5f, 0f, -0.5f)) * Matrix.CreateRotationY((float)i * (float)Math.PI / 2f) * Matrix.CreateTranslation(new Vector3(0.5f, 0f, 0.5f));
		}
		base.Initialize();
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		if (generator.SubsystemFurnitureBlockBehavior == null)
		{
			return;
		}
		int data = Terrain.ExtractData(value);
		if (IsWaterLogged(data) && BlocksManager.Blocks[18] is FocalorsFluidBlock focalorsFluidBlock)
		{
			Color sideColor;
			Color color = (sideColor = BlockColorsMap.WaterColorsMap.LookupSeasonalWaterColor(generator.SubsystemTerrain.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).CurrentSeason, BlockColorsMap.WaterColorsMap.Lookup(generator.Terrain, x, y, z)));
			sideColor.A = byte.MaxValue;
			Color topColor = color;
			topColor.A = 0;
			focalorsFluidBlock.GenerateWaterloggedFluidVertices(generator, x, y, z, sideColor, topColor, geometry.TransparentSubsetsByFace);
		}
		int designIndex = GetDesignIndex(data);
		int rotation = GetRotation(data);
		FurnitureDesign design = generator.SubsystemFurnitureBlockBehavior.GetDesign(designIndex);
		if (design == null)
		{
			return;
		}
		FurnitureGeometry geometry2 = design.Geometry;
		int mountingFacesMask = design.MountingFacesMask;
		for (int i = 0; i < 6; i++)
		{
			int num = CellFace.OppositeFace((i < 4) ? ((i + rotation) % 4) : i);
			byte b = (byte)(LightingManager.LightIntensityByLightValueAndFace[15 + 16 * num] * 255f);
			Color color2 = new Color(b, b, b);
			bool flag = false;
			Point3 point = CellFace.FaceToPoint3(num);
			int cellValueFast = generator.Terrain.GetCellValueFast(x + point.X, y + point.Y, z + point.Z);
			int num2 = Terrain.ExtractContents(cellValueFast);
			if (!BlocksManager.Blocks[num2].IsFaceTransparent(generator.SubsystemTerrain, CellFace.OppositeFace(num), cellValueFast))
			{
				flag = true;
			}
			else if (num2 == BlockIndex)
			{
				int data2 = Terrain.ExtractData(cellValueFast);
				int designIndex2 = GetDesignIndex(data2);
				int rotation2 = GetRotation(data2);
				FurnitureDesign design2 = generator.SubsystemFurnitureBlockBehavior.GetDesign(designIndex2);
				if (design2 != null && design.MatchOuterFace(num, rotation, design2, rotation2))
				{
					flag = true;
				}
			}
			if (!flag && geometry2.SubsetOpaqueByFace[i] != null)
			{
				generator.GenerateShadedMeshVertices(this, x, y, z, geometry2.SubsetOpaqueByFace[i], color2, m_matrices[rotation], m_facesMaps[rotation], geometry.OpaqueSubsetsByFace[num]);
			}
			if (!flag && geometry2.SubsetAlphaTestByFace[i] != null)
			{
				generator.GenerateShadedMeshVertices(this, x, y, z, geometry2.SubsetAlphaTestByFace[i], color2, m_matrices[rotation], m_facesMaps[rotation], geometry.AlphaTestSubsetsByFace[num]);
			}
			if (geometry2.SubsetOpaqueInnerByFace[i] != null)
			{
				generator.GenerateShadedMeshVertices(this, x, y, z, geometry2.SubsetOpaqueInnerByFace[i], color2, m_matrices[rotation], m_facesMaps[rotation], geometry.OpaqueSubsetsByFace[num]);
			}
			if (geometry2.SubsetAlphaTestInnerByFace[i] != null)
			{
				generator.GenerateShadedMeshVertices(this, x, y, z, geometry2.SubsetAlphaTestInnerByFace[i], color2, m_matrices[rotation], m_facesMaps[rotation], geometry.AlphaTestSubsetsByFace[num]);
			}
			int num3 = CellFace.OppositeFace((i < 4) ? ((i - rotation + 4) % 4) : i);
			if ((mountingFacesMask & (1 << num3)) != 0)
			{
				generator.GenerateWireVertices(value, x, y, z, i, 0f, Vector2.Zero, geometry.SubsetOpaque);
			}
		}
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		if (environmentData.SubsystemTerrain == null)
		{
			return;
		}
		int designIndex = GetDesignIndex(Terrain.ExtractData(value));
		FurnitureDesign design = environmentData.SubsystemTerrain.SubsystemFurnitureBlockBehavior.GetDesign(designIndex);
		if (design == null)
		{
			return;
		}
		Matrix matrix2 = Matrix.CreateTranslation(new Vector3
		{
			X = -0.5f * (float)(design.Box.Left + design.Box.Right) / (float)design.Resolution,
			Y = -0.5f * (float)(design.Box.Top + design.Box.Bottom) / (float)design.Resolution,
			Z = -0.5f * (float)(design.Box.Near + design.Box.Far) / (float)design.Resolution
		} * size) * matrix;
		FurnitureGeometry geometry = design.Geometry;
		for (int i = 0; i < 6; i++)
		{
			float s = LightingManager.LightIntensityByLightValueAndFace[environmentData.Light + 16 * CellFace.OppositeFace(i)];
			Color color2 = Color.MultiplyColorOnly(color, s);
			if (geometry.SubsetOpaqueByFace[i] != null)
			{
				BlocksManager.DrawMeshBlock(primitivesRenderer, geometry.SubsetOpaqueByFace[i], color2, size, ref matrix2, environmentData);
			}
			if (geometry.SubsetAlphaTestByFace[i] != null)
			{
				BlocksManager.DrawMeshBlock(primitivesRenderer, geometry.SubsetAlphaTestByFace[i], color2, size, ref matrix2, environmentData);
			}
			if (geometry.SubsetOpaqueInnerByFace[i] != null)
			{
				BlocksManager.DrawMeshBlock(primitivesRenderer, geometry.SubsetOpaqueInnerByFace[i], color2, size, ref matrix2, environmentData);
			}
			if (geometry.SubsetAlphaTestInnerByFace[i] != null)
			{
				BlocksManager.DrawMeshBlock(primitivesRenderer, geometry.SubsetAlphaTestInnerByFace[i], color2, size, ref matrix2, environmentData);
			}
		}
	}

	public override bool IsFaceTransparent(SubsystemTerrain subsystemTerrain, int face, int value)
	{
		if (subsystemTerrain != null)
		{
			int data = Terrain.ExtractData(value);
			int rotation = GetRotation(data);
			int designIndex = GetDesignIndex(data);
			FurnitureDesign design = subsystemTerrain.SubsystemFurnitureBlockBehavior.GetDesign(designIndex);
			if (design != null)
			{
				return ((1 << m_reverseFacesMaps[rotation][face]) & design.TransparentFacesMask) != 0;
			}
		}
		return false;
	}

	public override int GetShadowStrength(int value)
	{
		int data = Terrain.ExtractData(value);
		if (GetIsLightEmitter(data))
		{
			return -99;
		}
		return GetShadowStrengthFactor(data) * 3 + 1;
	}

	public override int GetEmittedLightAmount(int value)
	{
		if (!GetIsLightEmitter(Terrain.ExtractData(value)))
		{
			return 0;
		}
		return 15;
	}

	public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value)
	{
		if (subsystemTerrain != null)
		{
			int flatBatch2D = GetDesignIndex(Terrain.ExtractData(value));
			FurnitureDesign count = subsystemTerrain.SubsystemFurnitureBlockBehavior.GetDesign(flatBatch2D);
			if (count != null)
			{
				if (!string.IsNullOrEmpty(count.Name))
				{
					return count.Name;
				}
				return count.GetDefaultName();
			}
		}
		return LanguageControl.GetBlock(fName + ":0", "DisplayName");
	}

	public override bool IsInteractive(SubsystemTerrain subsystemTerrain, int value)
	{
		if (subsystemTerrain != null)
		{
			int designIndex = GetDesignIndex(Terrain.ExtractData(value));
			FurnitureDesign design = subsystemTerrain.SubsystemFurnitureBlockBehavior.GetDesign(designIndex);
			if (design != null)
			{
				if (design.InteractionMode != FurnitureInteractionMode.Multistate && design.InteractionMode != FurnitureInteractionMode.ElectricButton && design.InteractionMode != FurnitureInteractionMode.ElectricSwitch)
				{
					return design.InteractionMode == FurnitureInteractionMode.ConnectedMultistate;
				}
				return true;
			}
		}
		return base.IsInteractive(subsystemTerrain, value);
	}

	public override string GetSoundMaterialName(SubsystemTerrain subsystemTerrain, int value)
	{
		if (subsystemTerrain != null)
		{
			int designIndex = GetDesignIndex(Terrain.ExtractData(value));
			FurnitureDesign design = subsystemTerrain.SubsystemFurnitureBlockBehavior.GetDesign(designIndex);
			if (design != null)
			{
				int mainValue = design.MainValue;
				int num = Terrain.ExtractContents(mainValue);
				return BlocksManager.Blocks[num].GetSoundMaterialName(subsystemTerrain, mainValue);
			}
		}
		return base.GetSoundMaterialName(subsystemTerrain, value);
	}

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain subsystemTerrain, int value)
	{
		if (subsystemTerrain != null)
		{
			int data = Terrain.ExtractData(value);
			int designIndex = GetDesignIndex(data);
			int rotation = GetRotation(data);
			FurnitureDesign design = subsystemTerrain.SubsystemFurnitureBlockBehavior.GetDesign(designIndex);
			if (design != null)
			{
				return design.GetCollisionBoxes(rotation);
			}
		}
		return base.GetCustomCollisionBoxes(subsystemTerrain, value);
	}

	public override BoundingBox[] GetCustomInteractionBoxes(SubsystemTerrain subsystemTerrain, int value)
	{
		if (subsystemTerrain != null)
		{
			int data = Terrain.ExtractData(value);
			int designIndex = GetDesignIndex(data);
			int rotation = GetRotation(data);
			FurnitureDesign design = subsystemTerrain.SubsystemFurnitureBlockBehavior.GetDesign(designIndex);
			if (design != null)
			{
				return design.GetInteractionBoxes(rotation);
			}
		}
		return base.GetCustomInteractionBoxes(subsystemTerrain, value);
	}

	public override BlockDebrisParticleSystem CreateDebrisParticleSystem(SubsystemTerrain subsystemTerrain, Vector3 position, int value, float strength)
	{
		int node = GetFaceTextureSlot(4, value);
		int designIndex = GetDesignIndex(Terrain.ExtractData(value));
		FurnitureDesign design = subsystemTerrain.SubsystemFurnitureBlockBehavior.GetDesign(designIndex);
		if (design != null)
		{
			int mainValue = design.MainValue;
			int num = Terrain.ExtractContents(mainValue);
			return BlocksManager.Blocks[num].CreateDebrisParticleSystem(subsystemTerrain, position, mainValue, strength);
		}
		return new BlockDebrisParticleSystem(subsystemTerrain, position, strength, DestructionDebrisScale, Color.White, node);
	}

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, TerrainRaycastResult raycastResult)
	{
		int rotation = 0;
		if (raycastResult.CellFace.Face < 4)
		{
			rotation = CellFace.OppositeFace(raycastResult.CellFace.Face);
		}
		else
		{
			Vector3 forward = Matrix.CreateFromQuaternion(componentMiner.ComponentCreature.ComponentCreatureModel.EyeRotation).Forward;
			float num = Vector3.Dot(forward, Vector3.UnitZ);
			float num2 = Vector3.Dot(forward, Vector3.UnitX);
			float num3 = Vector3.Dot(forward, -Vector3.UnitZ);
			float num4 = Vector3.Dot(forward, -Vector3.UnitX);
			if (num == MathUtils.Max(num, num2, num3, num4))
			{
				rotation = 0;
			}
			else if (num2 == MathUtils.Max(num, num2, num3, num4))
			{
				rotation = 1;
			}
			else if (num3 == MathUtils.Max(num, num2, num3, num4))
			{
				rotation = 2;
			}
			else if (num4 == MathUtils.Max(num, num2, num3, num4))
			{
				rotation = 3;
			}
		}
		int data = SetRotation(Terrain.ExtractData(value), rotation);
		return new BlockPlacementData
		{
			CellFace = raycastResult.CellFace,
			Value = Terrain.ReplaceData(value, data)
		};
	}

	public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
	{
		showDebris = true;
		int model = Terrain.ExtractData(oldValue);
		model = SetRotation(model, 0);
		dropValues.Add(new BlockDropValue
		{
			Value = Terrain.MakeBlockValue(227, 0, model),
			Count = 1
		});
	}

	public override float GetIconViewScale(int value, DrawBlockEnvironmentData environmentData)
	{
		if (environmentData.SubsystemTerrain != null)
		{
			int designIndex = GetDesignIndex(Terrain.ExtractData(value));
			FurnitureDesign design = environmentData.SubsystemTerrain.SubsystemFurnitureBlockBehavior.GetDesign(designIndex);
			if (design != null)
			{
				float num = (float)design.Resolution / (float)MathUtils.Max(design.Box.Width, design.Box.Height, design.Box.Depth);
				return DefaultIconViewScale * num;
			}
		}
		return base.GetIconViewScale(value, environmentData);
	}

	public int? GetPaintColor(int value)
	{
		return null;
	}

	public int Paint(SubsystemTerrain terrain, int value, int? color)
	{
		int data = Terrain.ExtractData(value);
		int designIndex = GetDesignIndex(data);
		FurnitureDesign design = terrain.SubsystemFurnitureBlockBehavior.GetDesign(designIndex);
		if (design != null)
		{
			List<FurnitureDesign> list = design.CloneChain();
			foreach (FurnitureDesign item in list)
			{
				item.Paint(color);
			}
			FurnitureDesign furnitureDesign = terrain.SubsystemFurnitureBlockBehavior.TryAddDesignChain(list[0], garbageCollectIfNeeded: true);
			if (furnitureDesign != null)
			{
				int data2 = SetDesignIndex(data, furnitureDesign.Index, furnitureDesign.ShadowStrengthFactor, furnitureDesign.IsLightEmitter);
				if (IsWaterLogged(data))
				{
					data2 = SetWaterLogged(data2, isWaterLogged: true);
				}
				CommonLib.Net.QueuePackage(new FurniturePackage(list[0], garbageCollectIfNeeded: true));
				return Terrain.ReplaceData(value, data2);
			}
			DisplayError();
		}
		return value;
	}

	public override CraftingRecipe GetAdHocCraftingRecipe(SubsystemTerrain terrain, string[] ingredients, float heatLevel, ComponentPlayer player)
	{
		if (heatLevel != 0f)
		{
			return null;
		}
		int result = 0;
		int data = 0;
		int num = 0;
		List<FurnitureDesign> list = new List<FurnitureDesign>();
		for (int i = 0; i < ingredients.Length; i++)
		{
			if (string.IsNullOrEmpty(ingredients[i]))
			{
				continue;
			}
			CraftingRecipesManager.DecodeIngredient(ingredients[i], out var craftingId, out var data2);
			if (craftingId == BlocksManager.Blocks[227].CraftingId)
			{
				FurnitureDesign design = terrain.SubsystemFurnitureBlockBehavior.GetDesign(GetDesignIndex(data2.GetValueOrDefault()));
				if (design == null)
				{
					return null;
				}
				list.Add(design);
			}
			else if (craftingId == BlocksManager.Blocks[142].CraftingId)
			{
				result++;
			}
			else if (craftingId == BlocksManager.Blocks[141].CraftingId)
			{
				data++;
			}
			else
			{
				if (!(craftingId == BlocksManager.Blocks[133].CraftingId))
				{
					return null;
				}
				num++;
			}
		}
		if (list.Count == 1 && result == 1 && data == 0 && num == 0)
		{
			FurnitureDesign furnitureDesign = list[0].Clone();
			furnitureDesign.InteractionMode = FurnitureInteractionMode.ElectricButton;
			FurnitureDesign furnitureDesign2 = terrain.SubsystemFurnitureBlockBehavior.TryAddDesignChain(furnitureDesign, garbageCollectIfNeeded: true);
			if (furnitureDesign2 == null)
			{
				DisplayError();
				return null;
			}
			CommonLib.Net.QueuePackage(new FurniturePackage(furnitureDesign2, garbageCollectIfNeeded: true));
			return new CraftingRecipe
			{
				ResultValue = Terrain.MakeBlockValue(227, 0, SetDesignIndex(0, furnitureDesign2.Index, furnitureDesign2.ShadowStrengthFactor, furnitureDesign2.IsLightEmitter)),
				ResultCount = 1,
				Description = LanguageControl.Get(LangKey, 0),
				Ingredients = (string[])ingredients.Clone()
			};
		}
		if (list.Count == 2 && result == 0 && data == 1 && num == 0)
		{
			List<FurnitureDesign> list2 = list.Select((FurnitureDesign d) => d.Clone()).ToList();
			for (int num2 = 0; num2 < list2.Count; num2++)
			{
				list2[num2].InteractionMode = FurnitureInteractionMode.ElectricSwitch;
				list2[num2].LinkedDesign = list2[(num2 + 1) % list2.Count];
			}
			FurnitureDesign furnitureDesign3 = terrain.SubsystemFurnitureBlockBehavior.TryAddDesignChain(list2[0], garbageCollectIfNeeded: true);
			if (furnitureDesign3 == null)
			{
				DisplayError();
				return null;
			}
			CommonLib.Net.QueuePackage(new FurniturePackage(furnitureDesign3, garbageCollectIfNeeded: true));
			return new CraftingRecipe
			{
				ResultValue = Terrain.MakeBlockValue(227, 0, SetDesignIndex(0, furnitureDesign3.Index, furnitureDesign3.ShadowStrengthFactor, furnitureDesign3.IsLightEmitter)),
				ResultCount = 1,
				Description = LanguageControl.Get(LangKey, 0),
				Ingredients = (string[])ingredients.Clone()
			};
		}
		if (list.Count < 2 || result != 0 || data != 0 || num > 1)
		{
			return null;
		}
		List<FurnitureDesign> list3 = list.Select((FurnitureDesign d) => d.Clone()).ToList();
		for (int num3 = 0; num3 < list3.Count; num3++)
		{
			list3[num3].InteractionMode = ((num == 0) ? FurnitureInteractionMode.Multistate : FurnitureInteractionMode.ConnectedMultistate);
			list3[num3].LinkedDesign = list3[(num3 + 1) % list3.Count];
		}
		FurnitureDesign furnitureDesign4 = terrain.SubsystemFurnitureBlockBehavior.TryAddDesignChain(list3[0], garbageCollectIfNeeded: true);
		if (furnitureDesign4 == null)
		{
			DisplayError();
			return null;
		}
		CommonLib.Net.QueuePackage(new FurniturePackage(furnitureDesign4, garbageCollectIfNeeded: true));
		return new CraftingRecipe
		{
			ResultValue = Terrain.MakeBlockValue(227, 0, SetDesignIndex(0, furnitureDesign4.Index, furnitureDesign4.ShadowStrengthFactor, furnitureDesign4.IsLightEmitter)),
			ResultCount = 1,
			Description = LanguageControl.Get(LangKey, 0),
			Ingredients = (string[])ingredients.Clone()
		};
	}

	public ElectricElement CreateElectricElement(SubsystemElectricity subsystemElectricity, int value, int x, int y, int z)
	{
		int face = GetDesignIndex(Terrain.ExtractData(value));
		FurnitureDesign design = subsystemElectricity.SubsystemTerrain.SubsystemFurnitureBlockBehavior.GetDesign(face);
		if (design != null)
		{
			if (design.InteractionMode == FurnitureInteractionMode.Multistate || design.InteractionMode == FurnitureInteractionMode.ConnectedMultistate)
			{
				return new MultistateFurnitureElectricElement(subsystemElectricity, new Point3(x, y, z));
			}
			if (design.InteractionMode == FurnitureInteractionMode.ElectricButton)
			{
				return new ButtonFurnitureElectricElement(subsystemElectricity, new Point3(x, y, z));
			}
			if (design.InteractionMode == FurnitureInteractionMode.ElectricSwitch)
			{
				return new SwitchFurnitureElectricElement(subsystemElectricity, new Point3(x, y, z), value);
			}
		}
		return null;
	}

	public ElectricConnectorType? GetConnectorType(SubsystemTerrain terrain, int value, int face, int connectorFace, int x, int y, int z)
	{
		int data = Terrain.ExtractData(value);
		int data2 = GetRotation(data);
		int face2 = GetDesignIndex(data);
		FurnitureDesign design = terrain.SubsystemFurnitureBlockBehavior.GetDesign(face2);
		if (design != null)
		{
			int num = CellFace.OppositeFace((face < 4) ? ((face - data2 + 4) % 4) : face);
			if ((design.MountingFacesMask & (1 << num)) != 0 && SubsystemElectricity.GetConnectorDirection(face, 0, connectorFace).HasValue)
			{
				Point3 point = CellFace.FaceToPoint3(face);
				int cellValue = terrain.Terrain.GetCellValue(x - point.X, y - point.Y, z - point.Z);
				if (!BlocksManager.Blocks[Terrain.ExtractContents(cellValue)].IsFaceTransparent(terrain, CellFace.OppositeFace(num), cellValue))
				{
					if (design.InteractionMode == FurnitureInteractionMode.Multistate || design.InteractionMode == FurnitureInteractionMode.ConnectedMultistate)
					{
						return ElectricConnectorType.Input;
					}
					if (design.InteractionMode == FurnitureInteractionMode.ElectricButton || design.InteractionMode == FurnitureInteractionMode.ElectricSwitch)
					{
						return ElectricConnectorType.Output;
					}
				}
			}
		}
		return null;
	}

	public int GetConnectionMask(int value)
	{
		return int.MaxValue;
	}

	public void DisplayError()
	{
		DialogsManager.Alert(LanguageControl.Get(LangKey, 1));
	}

	public static int GetRotation(int data)
	{
		return data & 3;
	}

	public static int SetRotation(int data, int rotation)
	{
		return (data & -4) | (rotation & 3);
	}

	public static int GetDesignIndex(int data)
	{
		return (data >> 2) & 0x3FF;
	}

	public static int SetDesignIndex(int data, int designIndex, int shadowStrengthFactor, bool isLightEmitter)
	{
		data = (data & -4093) | ((designIndex & 0x3FF) << 2);
		data = (data & -12289) | ((shadowStrengthFactor & 3) << 12);
		data = (data & -16385) | ((isLightEmitter ? 1 : 0) << 14);
		return data;
	}

	public static FurnitureDesign GetDesign(SubsystemFurnitureBlockBehavior subsystemFurnitureBlockBehavior, int value)
	{
		int designIndex = GetDesignIndex(Terrain.ExtractData(value));
		return subsystemFurnitureBlockBehavior.GetDesign(designIndex);
	}

	public static int GetShadowStrengthFactor(int data)
	{
		return (data >> 12) & 3;
	}

	public static bool GetIsLightEmitter(int data)
	{
		return ((data >> 14) & 1) != 0;
	}

	static FurnitureBlock()
	{
		fName = "FurnitureBlock";
	}

	public bool IsWaterLogged(int data)
	{
		return (data & 0x8000) != 0;
	}

	public int SetWaterLogged(int data, bool isWaterLogged)
	{
		if (isWaterLogged)
		{
			return data | 0x8000;
		}
		return data & -32769;
	}
}
