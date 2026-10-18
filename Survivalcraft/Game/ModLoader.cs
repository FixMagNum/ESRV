using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Engine;
using Engine.Graphics;
using GameEntitySystem;

namespace Game;

public abstract class ModLoader
{
	public ModEntity Entity;

	public virtual void __ModInitialize()
	{
	}

	public virtual void ModDispose()
	{
	}

	public virtual void OnMinerHit(ComponentMiner miner, ComponentBody componentBody, Vector3 hitPoint, Vector3 hitDirection, ref float attackPower, ref float playerProbability, ref float creatureProbability, out bool Hitted)
	{
		Hitted = false;
	}

	public virtual bool OnMinerDig(ComponentMiner miner, TerrainRaycastResult raycastResult, ref float DigProgress, out bool Digged)
	{
		Digged = false;
		return false;
	}

	public virtual bool SetRainAndSnowColor(ref Color rainColor, ref Color snowColor)
	{
		return false;
	}

	public virtual void AttackPowerParameter(ref float impulseFactor, ref float stunTimeFactor)
	{
	}

	public virtual bool ClothingProcessSlotItems(ComponentPlayer componentPlayer, Block block, int slotIndex, int value, int count)
	{
		return false;
	}

	public virtual void OnEatPickable(ComponentEatPickableBehavior eatPickableBehavior, Pickable EatPickable, out bool Dealed)
	{
		Dealed = false;
	}

	public virtual bool OnPlayerSpawned(PlayerData.SpawnMode spawnMode, ComponentPlayer componentPlayer, Vector3 position)
	{
		return false;
	}

	public virtual void OnPlayerDead(PlayerData playerData)
	{
	}

	public virtual bool AttackBody(ComponentBody target, ComponentCreature attacker, Vector3 hitPoint, Vector3 hitDirection, ref float attackPower, bool isMeleeAttack)
	{
		return false;
	}

	public virtual void OnSetModel(ComponentModel componentModel, Model model, out bool IsSet)
	{
		IsSet = false;
	}

	public virtual void OnModelAnimate(ComponentCreatureModel componentCreatureModel, out bool Skip)
	{
		Skip = false;
	}

	public virtual float ApplyArmorProtection(ComponentClothing componentClothing, float attackPower, out bool Applied)
	{
		Applied = false;
		return attackPower;
	}

	public virtual void OnLevelUpdate(ComponentLevel level)
	{
	}

	public virtual void GuiUpdate(ComponentGui componentGui)
	{
	}

	public virtual void GuiDraw(ComponentGui componentGui, Camera camera, int drawOrder)
	{
	}

	public virtual void UpdateInput(ComponentInput componentInput, WidgetInput widgetInput)
	{
	}

	public virtual void DrawToScreen(ViewWidget viewWidget, Widget.DrawContext dc)
	{
	}

	public virtual void ClothingWidgetOpen(ComponentGui componentGui, ClothingWidget clothingWidget)
	{
	}

	public virtual void OnBlockExploded(SubsystemTerrain subsystemTerrain, int x, int y, int z, int value)
	{
	}

	public virtual void OnEntityAdd(Entity entity)
	{
	}

	public virtual void OnEntityRemove(Entity entity)
	{
	}

	public virtual void InitializeCreatureTypes(SubsystemCreatureSpawn spawn, List<SubsystemCreatureSpawn.CreatureType> creatureTypes)
	{
	}

	public virtual void SpawnEntity(SubsystemSpawn spawn, Entity entity, SpawnEntityData spawnEntityData, out bool Spawned)
	{
		Spawned = false;
	}

	public virtual void OnDespawned(Entity entity, ComponentSpawn componentSpawn)
	{
	}

	public virtual void JumpToPlace(out bool Pass)
	{
		Pass = false;
	}

	public virtual void DeadBeforeDrops(ComponentHealth componentHealth, out bool Skip)
	{
		Skip = false;
	}

	public virtual void TerrainChangeCell(SubsystemTerrain subsystemTerrain, int x, int y, int z, int value, out bool Skip)
	{
		Skip = false;
	}

	public virtual void OnCreatureInjure(ComponentHealth componentHealth, float amount, ComponentCreature attacker, bool ignoreInvulnerability, string cause, out bool Skip)
	{
		Skip = false;
	}

	public virtual Color ChangeSkyColor(Color oldColor, Vector3 direction, float timeOfDay, float precipitationIntensity, int temperature)
	{
		return oldColor;
	}

	public virtual void SetShaderParameter(Shader shader, Camera camera)
	{
	}

	public virtual void ModelShaderParameter(Shader shader, Camera camera, List<SubsystemModelsRenderer.ModelData> modelsData, float? alphaThreshold)
	{
	}

	public virtual void SkyDrawExtra(SubsystemSky subsystemSky, Camera camera)
	{
	}

	public virtual int GetMaxInstancesCount()
	{
		return 7;
	}

	public virtual void OnModelRendererDrawExtra(SubsystemModelsRenderer modelsRenderer, ComponentModel componentModel, Camera camera, float? alphaThreshold)
	{
	}

	public virtual void SetHitValueParticleSystem(HitValueParticleSystem hitValueParticleSystem, bool Hit)
	{
	}

	public virtual void OnSaveSpawnData(ComponentSpawn spawn, SpawnEntityData spawnEntityData)
	{
	}

	public virtual void OnReadSpawnData(Entity entity, SpawnEntityData spawnEntityData)
	{
	}

	public virtual void OnTerrainContentsGenerated(TerrainChunk chunk)
	{
	}

	public virtual void SubsystemUpdate(float dt)
	{
	}

	public virtual void OnProjectLoaded(Project project)
	{
	}

	public virtual void OnProjectDisposed()
	{
	}

	public virtual void BlocksInitalized()
	{
	}

	public virtual object BeforeGameLoading(PlayScreen playScreen, object item)
	{
		return item;
	}

	public virtual void OnLoadingStart(List<Action> actions)
	{
	}

	public virtual void OnLoadingFinished(List<Action> actions)
	{
	}

	public virtual void SaveSettings(XElement xElement)
	{
	}

	public virtual void LoadSettings(XElement xElement)
	{
	}

	public virtual void OnXdbLoad(XElement xElement)
	{
	}

	public virtual void ProjectXmlLoad(XElement xElement)
	{
	}

	public virtual void ProjectXmlSave(XElement xElement)
	{
	}

	public virtual void OnCraftingRecipeDecode(List<CraftingRecipe> m_recipes, XElement element, out bool Decoded)
	{
		Decoded = false;
	}

	public virtual bool MatchRecipe(string[] requiredIngredients, string[] actualIngredient, out bool Matched)
	{
		Matched = false;
		return false;
	}

	public virtual int DecodeResult(string result, out bool Decoded)
	{
		Decoded = false;
		return 0;
	}

	public virtual void DecodeIngredient(string ingredient, out string craftingId, out int? data, out bool Decoded)
	{
		Decoded = false;
		craftingId = string.Empty;
		data = null;
	}

	public virtual void OnCameraChange(ComponentPlayer m_componentPlayer, ComponentGui componentGui)
	{
	}

	public virtual void OnCapture()
	{
	}

	public virtual void CallNearbyCreaturesHelp(ComponentHerdBehavior herdBehavior, ComponentCreature target, float maxRange, float maxChaseTime, bool isPersistent)
	{
	}

	public virtual void OnTreasureGenerate(SubsystemTerrain subsystemTerrain, int x, int y, int z, int neighborX, int neighborY, int neighborZ, ref int BlockValue, ref int Count, out bool IsGenerate)
	{
		IsGenerate = false;
	}
}
