using System.Collections.Generic;
using Engine;
using TemplatesDatabase;

namespace Game;

public class SubsystemStickyBrushBlockBehavior : SubsystemBlockBehavior
{
	public SubsystemAudio m_subsystemAudio;

	public SubsystemNoise m_subsystemNoise;

	public Random m_random = new Random();

	public SubsystemPlayers m_subsystemPlayers;

	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemParticles m_subsystemParticles;

	public SubsystemGameInfo m_subsystemGameInfo;

	public override int[] HandledBlocks => new int[1] { 357 };

	public override bool OnUse(Ray3 ray, ComponentMiner componentMiner)
	{
		Pickable pickable = FindTargetPickable(componentMiner, ray);
		if (pickable == null)
		{
			return false;
		}
		if (pickable.IsStuckProjectile)
		{
			string text = LanguageControl.Get("SubsystemStickyBrush", "item_stuck");
			componentMiner.ComponentPlayer.ComponentGui.DisplaySmallMessage(text, Color.Red, blinking: true, playNotificationSound: true);
			return false;
		}
		if (IsPickableInPrivateArea(componentMiner, pickable))
		{
			string text2 = LanguageControl.Get("SubsystemStickyBrush", "cannot_use");
			componentMiner.ComponentPlayer.ComponentGui.DisplaySmallMessage(text2, Color.Red, blinking: true, playNotificationSound: true);
			return false;
		}
		if (!pickable.IsSpecialPickable)
		{
			MakePickableStatic(pickable);
			m_subsystemAudio.PlayRandomSound("Audio/Paint", 1f, m_random.Float(-0.2f, 0f), ray.Position, 4f, autoDelay: true);
			componentMiner.DamageActiveTool(1);
		}
		else
		{
			MakePickableDynamic(pickable);
			m_subsystemAudio.PlayRandomSound("Audio/Impacts/Soft", 1f, m_random.Float(-0.2f, 0f), ray.Position, 4f, autoDelay: true);
		}
		return true;
	}

	private void MakePickableDynamic(Pickable pickable)
	{
		int replacementIndex = GetReplacementIndex(Terrain.ExtractContents(pickable.Value));
		bool flag = (Terrain.ExtractData(pickable.Value) & 0x10000) != 0;
		if (replacementIndex != -1 && !flag)
		{
			Vector3 position = pickable.Position;
			Project.FindSubsystem<SubsystemPickables>(throwOnError: true).AddPickable(Terrain.MakeBlockValue(replacementIndex), 1, position, null, null);
			pickable.ToRemove = true;
		}
		else
		{
			pickable.IsSpecialPickable = false;
			pickable.StuckMatrix = null;
			pickable.CreationTime = m_subsystemGameInfo.TotalElapsedGameTime;
		}
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemNoise = Project.FindSubsystem<SubsystemNoise>(throwOnError: true);
		m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
	}

	private Pickable FindTargetPickable(ComponentMiner componentMiner, Ray3 ray)
	{
		SubsystemPickables subsystemPickables = Project.FindSubsystem<SubsystemPickables>(throwOnError: true);
		float num = float.MaxValue;
		Pickable pickable = null;
		foreach (Pickable pickable2 in subsystemPickables.Pickables)
		{
			BoundingBox box = new BoundingBox(pickable2.Position - new Vector3(0.1f), pickable2.Position + new Vector3(0.1f));
			float? num2 = ray.Intersection(box);
			if (num2.HasValue)
			{
				float value = num2.Value;
				if (value < num)
				{
					num = value;
					pickable = pickable2;
				}
			}
		}
		if (pickable != null)
		{
			if (IsPickableInPrivateArea(componentMiner, pickable))
			{
				string text = LanguageControl.Get("SubsystemStickyBrush", "cannot_use");
				componentMiner.ComponentPlayer.ComponentGui.DisplaySmallMessage(text, Color.Red, blinking: true, playNotificationSound: true);
				return null;
			}
			return pickable;
		}
		return null;
	}

	public void Update(float dt)
	{
		foreach (Pickable pickable in Project.FindSubsystem<SubsystemPickables>(throwOnError: true).Pickables)
		{
			if (pickable.IsSpecialPickable)
			{
				pickable.Color = Color.Yellow;
			}
			else
			{
				pickable.Color = pickable.DefaultColor;
			}
		}
	}

	private int GetReplacementIndex(int index)
	{
		if (new Dictionary<int, int>
		{
			{ 345, 241 },
			{ 347, 239 },
			{ 346, 240 },
			{ 177, 242 },
			{ 316, 313 },
			{ 327, 340 },
			{ 328, 338 },
			{ 332, 342 },
			{ 321, 246 },
			{ 118, 246 },
			{ 110, 245 },
			{ 251, 252 },
			{ 176, 247 },
			{ 314, 315 },
			{ 334, 339 },
			{ 335, 341 },
			{ 337, 343 },
			{ 161, 241 },
			{ 162, 241 },
			{ 336, 241 },
			{ 331, 241 },
			{ 77, 239 },
			{ 78, 239 },
			{ 88, 240 },
			{ 89, 240 },
			{ 333, 240 },
			{ 330, 240 }
		}.TryGetValue(index, out var subsystemInventories))
		{
			return subsystemInventories;
		}
		return -1;
	}

	private void MakePickableStatic(Pickable pickable)
	{
		pickable.IsSpecialPickable = true;
		Vector3 position = pickable.Position;
		int value = pickable.Value;
		Block block = BlocksManager.Blocks[Terrain.ExtractContents(value)];
		m_subsystemParticles.AddParticleSystem(block.CreateDebrisParticleSystem(m_subsystemTerrain, position, value, 0.5f));
		pickable.CreationTime = m_subsystemGameInfo.TotalElapsedGameTime;
	}

	private bool IsPickableInPrivateArea(ComponentMiner componentMiner, Pickable pickable)
	{
		SubsystemGameInfo subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		bool flag = SubsystemBedrockBlockBehavior.CheckIsInTerritoriy((int)pickable.Position.X, (int)pickable.Position.Z, out var territoriy);
		if (subsystemGameInfo.WorldSettings.RestrictWorldInteraction && componentMiner.ComponentPlayer != null && !componentMiner.ComponentPlayer.PlayerData.ServerManager && !flag)
		{
			return true;
		}
		if (flag && !componentMiner.ComponentPlayer.PlayerData.Friends.Contains(territoriy.OwnerGuid) && territoriy.OwnerGuid != componentMiner.ComponentPlayer.PlayerData.PlayerGUID)
		{
			string text = LanguageControl.Get("SubsystemStickyBrush", "cannot_use");
			componentMiner.ComponentPlayer.ComponentGui.DisplaySmallMessage(text, Color.Red, blinking: true, playNotificationSound: true);
			return true;
		}
		return false;
	}
}
