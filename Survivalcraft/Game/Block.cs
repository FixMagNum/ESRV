using System;
using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public abstract class Block
{
	public int BlockIndex;

	public string DefaultDisplayName = string.Empty;

	public string DefaultDescription = string.Empty;

	public string DefaultCategory = string.Empty;

	public int DisplayOrder;

	public Vector3 DefaultIconBlockOffset = Vector3.Zero;

	public Vector3 DefaultIconViewOffset = new Vector3(1f);

	public float DefaultIconViewScale = 1f;

	public float FirstPersonScale = 1f;

	public Vector3 FirstPersonOffset = Vector3.Zero;

	public Vector3 FirstPersonRotation = Vector3.Zero;

	public float InHandScale = 1f;

	public Vector3 InHandOffset = Vector3.Zero;

	public Vector3 InHandRotation = Vector3.Zero;

	public string Behaviors = string.Empty;

	public string CraftingId = string.Empty;

	public int DefaultCreativeData;

	public bool IsCollidable = true;

	public bool IsPlaceable = true;

	public bool IsDiggingTransparent;

	public bool IsPlacementTransparent;

	public bool DefaultIsInteractive;

	public bool IsEditable;

	public bool IsNonDuplicable;

	public bool IsGatherable;

	public bool HasCollisionBehavior;

	public bool KillsWhenStuck;

	public bool IsFluidBlocker = true;

	public bool IsTransparent;

	public int DefaultShadowStrength;

	public int LightAttenuation;

	public int DefaultEmittedLightAmount;

	public float ObjectShadowStrength;

	public int DefaultDropContent;

	public float DefaultDropCount = 1f;

	public float DefaultExperienceCount;

	public int RequiredToolLevel;

	public int MaxStacking = 40;

	public float SleepSuitability;

	public float FrictionFactor = 1f;

	public float Density = 4f;

	public bool NoAutoJump;

	public bool NoSmoothRise;

	public int DefaultTextureSlot;

	public float DestructionDebrisScale = 1f;

	public float FuelHeatLevel;

	public float FuelFireDuration;

	public string DefaultSoundMaterialName;

	public float ShovelPower = 1f;

	public float QuarryPower = 1f;

	public float HackPower = 1f;

	public float DefaultMeleePower = 1f;

	public float DefaultMeleeHitProbability = 0.66f;

	public float DefaultProjectilePower = 1f;

	public int ToolLevel;

	public int PlayerLevelRequired = 1;

	public int Durability = -1;

	public BlockDigMethod DigMethod;

	public float DigResilience = 1f;

	public float ProjectileResilience = 1f;

	public bool IsAimable;

	public bool IsStickable;

	public bool AlignToVelocity;

	public float ProjectileSpeed = 15f;

	public float ProjectileDamping = 0.8f;

	public float ProjectileTipOffset;

	public bool DisintegratesOnHit;

	public float ProjectileStickProbability;

	public float DefaultHeat;

	public float FireDuration;

	public float ExplosionResilience;

	public float DefaultExplosionPressure;

	public bool DefaultExplosionIncendiary;

	public bool IsExplosionTransparent;

	public float DefaultNutritionalValue;

	public FoodType FoodType;

	public int DefaultRotPeriod;

	public float DefaultSicknessProbability;

	public static string fName;

	public Random Random = new Random();

	public static BoundingBox[] m_defaultCollisionBoxes;

	public virtual Vector3 GetFirstPersonOffset(int value)
	{
		return FirstPersonOffset;
	}

	public virtual Vector3 GetFirstPersonRotation(int value)
	{
		return FirstPersonRotation;
	}

	public virtual float GetInHandScale(int value)
	{
		return InHandScale;
	}

	public virtual Vector3 GetInHandOffset(int value)
	{
		return InHandOffset;
	}

	public virtual Vector3 GetInHandRotation(int value)
	{
		return InHandRotation;
	}

	public virtual float GetDensity(int value)
	{
		return Density;
	}

	public virtual float GetFirstPersonScale(int value)
	{
		return FirstPersonScale;
	}

	public virtual void Initialize()
	{
		if (Durability < -1 || Durability > 65535)
		{
			throw new InvalidOperationException(string.Format(LanguageControl.Get(fName, 1), DefaultDisplayName));
		}
	}

	public virtual TerrainVertex SetDiggingCrackingTextureTransform(TerrainVertex vertex)
	{
		byte b = (byte)((vertex.Color.R + vertex.Color.G + vertex.Color.B) / 3);
		vertex.Tx = (short)((float)vertex.Tx * 16f);
		vertex.Ty = (short)((float)vertex.Ty * 16f);
		vertex.Color = new Color(b, b, b, (byte)128);
		return vertex;
	}

	public virtual Texture2D GetDiggingCrackingTexture(ComponentMiner miner, float digProgress, int value, Texture2D[] defaultCrackTextures)
	{
		int num = MathUtils.Clamp((int)(digProgress * 8f), 0, 7);
		return defaultCrackTextures[num];
	}

	public virtual bool GetIsDiggingTransparent(int value)
	{
		return IsDiggingTransparent;
	}

	public virtual float GetObjectShadowStrength(int value)
	{
		return ObjectShadowStrength;
	}

	public virtual float GetFuelHeatLevel(int value)
	{
		return FuelHeatLevel;
	}

	public virtual float GetExplosionResilience(int value)
	{
		return ExplosionResilience;
	}

	public virtual float GetExplosionPressure(int value)
	{
		return DefaultExplosionPressure;
	}

	public virtual int GetMaxStacking(int value)
	{
		return MaxStacking;
	}

	public virtual float GetFuelFireDuration(int value)
	{
		return FuelFireDuration;
	}

	public virtual float GetProjectileResilience(int value)
	{
		return ProjectileResilience;
	}

	public virtual float GetFireDuration(int value)
	{
		return FireDuration;
	}

	public virtual float GetProjectileStickProbability(int value)
	{
		return ProjectileStickProbability;
	}

	public virtual bool MatchCrafingId(string CraftId)
	{
		return CraftId == CraftingId;
	}

	public virtual int GetPlayerLevelRequired(int value)
	{
		return PlayerLevelRequired;
	}

	public virtual bool HasCollisionBehavior_(int value)
	{
		return HasCollisionBehavior;
	}

	public virtual string GetDisplayName(SubsystemTerrain subsystemTerrain, int value)
	{
		int num = Terrain.ExtractData(value);
		if (LanguageControl.TryGetBlock($"{GetType().Name}:{num}", "DisplayName", out var result))
		{
			return result;
		}
		return DefaultDisplayName;
	}

	public virtual int GetTextureSlotCount(int value)
	{
		return 16;
	}

	public virtual bool IsEditable_(int value)
	{
		return IsEditable;
	}

	public virtual bool IsAimable_(int value)
	{
		return IsAimable;
	}

	public virtual bool Eat(int value)
	{
		return false;
	}

	public virtual bool CanWear(int value)
	{
		return false;
	}

	public virtual ClothingData GetClothingData(int value)
	{
		return null;
	}

	public virtual int GetToolLevel(int value)
	{
		return ToolLevel;
	}

	public virtual bool IsCollidable_(int value)
	{
		return IsCollidable;
	}

	public virtual bool IsTransparent_(int value)
	{
		return IsTransparent;
	}

	public virtual bool IsFluidBlocker_(int value)
	{
		return IsFluidBlocker;
	}

	public virtual bool IsGatherable_(int value)
	{
		return IsGatherable;
	}

	public virtual bool IsNonDuplicable_(int value)
	{
		return IsNonDuplicable;
	}

	public virtual bool IsPlaceable_(int value)
	{
		return IsPlaceable;
	}

	public virtual bool IsPlacementTransparent_(int value)
	{
		return IsPlacementTransparent;
	}

	public virtual bool IsStickable_(int value)
	{
		return IsStickable;
	}

	public virtual float GetProjectileSpeed(int value)
	{
		return ProjectileSpeed;
	}

	public virtual float GetProjectileDamping(int value)
	{
		return ProjectileDamping;
	}

	public virtual string GetDescription(int value)
	{
		int num = Terrain.ExtractData(value);
		if (LanguageControl.TryGetBlock($"{GetType().Name}:{num}", "Description", out var result))
		{
			return result;
		}
		return DefaultDescription;
	}

	public virtual FoodType GetFoodType(int value)
	{
		return FoodType;
	}

	public virtual string GetCategory(int value)
	{
		return DefaultCategory;
	}

	public virtual float GetDigResilience(int value)
	{
		return DigResilience;
	}

	public virtual BlockDigMethod GetBlockDigMethod(int value)
	{
		return DigMethod;
	}

	public virtual float GetShovelPower(int value)
	{
		return ShovelPower;
	}

	public virtual float GetQuarryPower(int value)
	{
		return QuarryPower;
	}

	public virtual float GetHackPower(int value)
	{
		return HackPower;
	}

	public virtual IEnumerable<int> GetCreativeValues()
	{
		if (DefaultCreativeData >= 0)
		{
			yield return Terrain.ReplaceContents(Terrain.ReplaceData(0, DefaultCreativeData), BlockIndex);
		}
	}

	public virtual bool GetAlignToVelocity(int value)
	{
		return AlignToVelocity;
	}

	public virtual bool IsInteractive(SubsystemTerrain subsystemTerrain, int value)
	{
		return DefaultIsInteractive;
	}

	public virtual IEnumerable<CraftingRecipe> GetProceduralCraftingRecipes()
	{
		yield break;
	}

	public virtual CraftingRecipe GetAdHocCraftingRecipe(SubsystemTerrain subsystemTerrain, string[] ingredients, float heatLevel, ComponentPlayer player)
	{
		return null;
	}

	public virtual bool IsFaceTransparent(SubsystemTerrain subsystemTerrain, int face, int value)
	{
		return IsTransparent;
	}

	public virtual bool ShouldGenerateFace(SubsystemTerrain subsystemTerrain, int face, int value, int neighborValue)
	{
		int num = Terrain.ExtractContents(neighborValue);
		return BlocksManager.Blocks[num].IsFaceTransparent(subsystemTerrain, CellFace.OppositeFace(face), neighborValue);
	}

	public virtual int GetShadowStrength(int value)
	{
		return DefaultShadowStrength;
	}

	public virtual int GetFaceTextureSlot(int face, int value)
	{
		return DefaultTextureSlot;
	}

	public virtual string GetSoundMaterialName(SubsystemTerrain subsystemTerrain, int value)
	{
		return DefaultSoundMaterialName;
	}

	public abstract void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z);

	public virtual void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometrySubset geometry, int value, int x, int y, int z)
	{
	}

	public abstract void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData);

	public virtual BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, TerrainRaycastResult raycastResult)
	{
		return new BlockPlacementData
		{
			Value = value,
			CellFace = raycastResult.CellFace
		};
	}

	public virtual string GetCraftingId(int value)
	{
		return CraftingId;
	}

	public virtual int GetDisplayOrder(int value)
	{
		return DisplayOrder;
	}

	public virtual BlockPlacementData GetDigValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, int toolValue, TerrainRaycastResult raycastResult)
	{
		return new BlockPlacementData
		{
			Value = 0,
			CellFace = raycastResult.CellFace
		};
	}

	public virtual float GetRequiredToolLevel(int value)
	{
		return RequiredToolLevel;
	}

	public virtual void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
	{
		showDebris = DestructionDebrisScale > 0f;
		if (toolLevel < RequiredToolLevel)
		{
			return;
		}
		if (DefaultDropContent != 0)
		{
			int num = (int)DefaultDropCount;
			if (Random.Bool(DefaultDropCount - (float)num))
			{
				num++;
			}
			for (int i = 0; i < num; i++)
			{
				BlockDropValue item = new BlockDropValue
				{
					Value = Terrain.MakeBlockValue(DefaultDropContent),
					Count = 1
				};
				dropValues.Add(item);
			}
		}
		int num2 = (int)DefaultExperienceCount;
		if (Random.Bool(DefaultExperienceCount - (float)num2))
		{
			num2++;
		}
		for (int j = 0; j < num2; j++)
		{
			BlockDropValue item2 = new BlockDropValue
			{
				Value = Terrain.MakeBlockValue(248),
				Count = 1
			};
			dropValues.Add(item2);
		}
	}

	public virtual int GetDamage(int value)
	{
		return (Terrain.ExtractData(value) >> 4) & 0xFFF;
	}

	public virtual int SetDamage(int value, int damage)
	{
		int num = Terrain.ExtractData(value);
		num &= 0xF;
		num |= MathUtils.Clamp(damage, 0, 4095) << 4;
		return Terrain.ReplaceData(value, num);
	}

	public virtual int GetDamageDestructionValue(int value)
	{
		return 0;
	}

	public virtual int GetRotPeriod(int value)
	{
		return DefaultRotPeriod;
	}

	public virtual float GetSicknessProbability(int value)
	{
		return DefaultSicknessProbability;
	}

	public virtual float GetMeleePower(int value)
	{
		return DefaultMeleePower;
	}

	public virtual float GetMeleeHitProbability(int value)
	{
		return DefaultMeleeHitProbability;
	}

	public virtual float GetProjectilePower(int value)
	{
		return DefaultProjectilePower;
	}

	public virtual float GetHeat(int value)
	{
		return MathUtils.Clamp(DefaultHeat, -2f, 5f);
	}

	public virtual float GetBlockHealth(int value)
	{
		int durability = GetDurability(value);
		int damage = GetDamage(value);
		if (Durability > 0)
		{
			return (float)(durability - damage) / (float)durability;
		}
		return -1f;
	}

	public virtual int GetDurability(int value)
	{
		return Durability;
	}

	public virtual bool GetExplosionIncendiary(int value)
	{
		return DefaultExplosionIncendiary;
	}

	public virtual Vector3 GetIconBlockOffset(int value, DrawBlockEnvironmentData environmentData)
	{
		return DefaultIconBlockOffset;
	}

	public virtual Vector3 GetIconViewOffset(int value, DrawBlockEnvironmentData environmentData)
	{
		return DefaultIconViewOffset;
	}

	public virtual float GetIconViewScale(int value, DrawBlockEnvironmentData environmentData)
	{
		return DefaultIconViewScale;
	}

	public virtual BlockDebrisParticleSystem CreateDebrisParticleSystem(SubsystemTerrain subsystemTerrain, Vector3 position, int value, float strength)
	{
		return new BlockDebrisParticleSystem(subsystemTerrain, position, strength, DestructionDebrisScale, Color.White, GetFaceTextureSlot(4, value));
	}

	public virtual BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
	{
		return m_defaultCollisionBoxes;
	}

	public virtual BoundingBox[] GetCustomInteractionBoxes(SubsystemTerrain terrain, int value)
	{
		return GetCustomCollisionBoxes(terrain, value);
	}

	public virtual int GetEmittedLightAmount(int value)
	{
		return DefaultEmittedLightAmount;
	}

	public virtual float GetNutritionalValue(int value)
	{
		return DefaultNutritionalValue;
	}

	public virtual bool ShouldAvoid(int value)
	{
		return false;
	}

	public virtual bool IsSwapAnimationNeeded(int oldValue, int newValue)
	{
		return true;
	}

	public virtual bool IsHeatBlocker(int value)
	{
		return IsCollidable_(value);
	}

	public float? Raycast(Ray3 ray, SubsystemTerrain subsystemTerrain, int value, bool useInteractionBoxes, out int nearestBoxIndex, out BoundingBox nearestBox)
	{
		float? result = null;
		nearestBoxIndex = 0;
		nearestBox = default;
		BoundingBox[] array = (useInteractionBoxes ? GetCustomInteractionBoxes(subsystemTerrain, value) : GetCustomCollisionBoxes(subsystemTerrain, value));
		for (int i = 0; i < array.Length; i++)
		{
			float? num = ray.Intersection(array[i]);
			if (num.HasValue && (!result.HasValue || num.Value < result.Value))
			{
				nearestBoxIndex = i;
				result = num;
			}
		}
		nearestBox = array[nearestBoxIndex];
		return result;
	}

	static Block()
	{
		fName = "Block";
		m_defaultCollisionBoxes = new BoundingBox[1]
		{
			new BoundingBox(Vector3.Zero, Vector3.One)
		};
	}
}
