using System;
using System.Collections.Generic;
using Engine;
using Game.NetWork;
using TemplatesDatabase;

namespace Game;

public class SubsystemMugBlockBehavior : SubsystemBlockBehavior, IUpdateable
{
	private class MugUseState
	{
		public double UseStartRealTime;

		public ComponentPlayer PairedWith;

		public double AnimStartRealTime;

		public bool ClinkDone;

		public bool GulpDone;

		public bool SoloMsgDone;

		public List<ComponentPlayer> GroupMembers;
	}

	private const double UseWindowSeconds = 3.0;

	private const double ClinkAfterSeconds = 2.0;

	private SubsystemAudio m_subsystemAudio;

	private SubsystemTime m_subsystemTime;

	private SubsystemPlayers m_subsystemPlayers;

	private readonly Dictionary<ComponentPlayer, MugUseState> m_active = new Dictionary<ComponentPlayer, MugUseState>();

	private static readonly List<ComponentPlayer> s_waiting;

	private static readonly List<ComponentPlayer> s_toClear;

	private Random m_random;

	private SubsystemParticles m_subsystemParticles;

	private SubsystemTerrain m_subsystemTerrain;

	public override int[] HandledBlocks => new int[1] { 429 };

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
		m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_random = new Random();
	}

	public override bool OnUse(Ray3 ray, ComponentMiner componentMiner)
	{
		if (Terrain.ExtractContents(componentMiner.ActiveBlockValue) != 429)
		{
			return false;
		}
		double realTime = Time.RealTime;
		if (m_active.TryGetValue(componentMiner.ComponentPlayer, out var value) && realTime - value.UseStartRealTime < 3.0)
		{
			return true;
		}
		m_active[componentMiner.ComponentPlayer] = new MugUseState
		{
			UseStartRealTime = realTime,
			PairedWith = null,
			AnimStartRealTime = 0.0,
			ClinkDone = false,
			GulpDone = false,
			SoloMsgDone = false
		};
		return true;
	}

	public void Update(float dt)
	{
		if (m_active.Count == 0)
		{
			return;
		}
		double realTime = Time.RealTime;
		TryPairWaitingPlayers(realTime);
		s_toClear.Clear();
		foreach (KeyValuePair<ComponentPlayer, MugUseState> item in m_active)
		{
			ComponentPlayer key = item.Key;
			MugUseState value = item.Value;
			double num = realTime - value.UseStartRealTime;
			ComponentCreatureModel componentCreatureModel = key.ComponentCreatureModel;
			ComponentLocomotion componentLocomotion = key.ComponentLocomotion;
			ComponentFirstPersonModel componentFirstPersonModel = key.Entity.FindComponent<ComponentFirstPersonModel>(throwOnError: false);
			bool flag = Terrain.ExtractContents(key.ComponentMiner.ActiveBlockValue) == 429;
			if ((num < 3.0) & flag)
			{
				float f = (float)(num / 3.0);
				float x = MathUtils.DegToRad(70f);
				float num2 = SimplexNoise.Noise(3f * (float)realTime + (float)key.GetHashCode() * 0.07f);
				float aimHandAngleOrder = MathUtils.Lerp(0f, x, f) + MathUtils.DegToRad(3f) * num2;
				if (componentCreatureModel != null)
				{
					componentCreatureModel.AimHandAngleOrder = aimHandAngleOrder;
				}
				if (componentFirstPersonModel != null)
				{
					float num3 = SimplexNoise.Noise(2.3f * (float)realTime + 11.1f);
					float num4 = SimplexNoise.Noise(2.1f * (float)realTime - 7.7f);
					componentFirstPersonModel.ItemOffsetOrder = new Vector3(0f, 0.32f + 0.02f * num4, 0.17f);
					componentFirstPersonModel.ItemRotationOrder = new Vector3(-0.2f + 0.1f * num3, 0f, 0f);
				}
			}
			if ((num >= 3.0 && num < 5.0) & flag)
			{
				float x2 = (float)((num - 3.0) / 2.0);
				x2 = MathUtils.Saturate(x2);
				float num5 = SimplexNoise.Noise(3f * (float)realTime + (float)key.GetHashCode() * 0.13f);
				float x3 = MathUtils.DegToRad(70f);
				float x4 = MathUtils.DegToRad(120f);
				float aimHandAngleOrder2 = MathUtils.Lerp(x3, x4, x2) + MathUtils.DegToRad(2.5f) * num5;
				if (componentCreatureModel != null)
				{
					componentCreatureModel.AimHandAngleOrder = aimHandAngleOrder2;
				}
				float num6 = MathUtils.DegToRad(MathUtils.Lerp(10f, 60f, x2)) + MathUtils.DegToRad(2f) * num5;
				componentLocomotion.LookOrder = new Vector2(componentLocomotion.LookOrder.X, MathUtils.Clamp(num6 - componentLocomotion.LookAngles.Y, -2f, 2f));
				if (componentFirstPersonModel != null)
				{
					float num7 = SimplexNoise.Noise(2f * (float)realTime + 3.3f);
					float num8 = SimplexNoise.Noise(1.7f * (float)realTime - 5.9f);
					componentFirstPersonModel.ItemOffsetOrder = new Vector3(0f, 0.35f + 0.03f * num8, 0.15f);
					componentFirstPersonModel.ItemRotationOrder = new Vector3(0f - MathUtils.Lerp(0f, 1.5f, x2) + 0.05f * num7, 0f, 0f);
				}
			}
			if (value.GroupMembers != null && value.GroupMembers.Count > 1)
			{
				double num9 = Math.Max(0.0, realTime - value.AnimStartRealTime);
				if (!value.ClinkDone && num >= 3.0 && num9 >= 2.0 && key == value.GroupMembers[0])
				{
					Vector3 zero = Vector3.Zero;
					for (int i = 0; i < value.GroupMembers.Count; i++)
					{
						zero += value.GroupMembers[i].ComponentBody.Position;
					}
					zero /= (float)value.GroupMembers.Count;
					float pitch = m_random.Float(-0.2f, 0.2f);
					m_subsystemAudio.PlaySound("Audio/MugClink", 1f, pitch, zero, 32f, autoDelay: true);
					for (int j = 0; j < value.GroupMembers.Count; j++)
					{
						ComponentPlayer componentPlayer = value.GroupMembers[j];
						bool flag2 = HasDrink(componentPlayer);
						string text = LanguageControl.Get("SubsystemMug", flag2 ? "cheers" : "cheers_empty");
						componentPlayer.ComponentGui?.DisplaySmallMessage(text, Color.White, blinking: true, playNotificationSound: true);
						bool flag3 = m_random.Bool();
						string directory;
						if (componentPlayer.PlayerData.PlayerClass != PlayerClass.Male)
						{
							directory = (flag3 ? "Audio/Creatures/FemaleYellFast" : "Audio/Creatures/FemaleYellSlow");
						}
						else
						{
							directory = (flag3 ? "Audio/Creatures/MaleYellFast" : "Audio/Creatures/MaleYellSlow");
						}
						float pitch2 = m_random.Float(-0.2f, 0.2f);
						m_subsystemAudio.PlayRandomSound(directory, 1f, pitch2, componentPlayer.ComponentBody.Position, 32f, autoDelay: false);
						SpawnClinkSplash(componentPlayer, value.GroupMembers.Count >= 3);
						m_active[componentPlayer].ClinkDone = true;
					}
				}
			}
			if (!(num >= 3.0) || value.GulpDone)
			{
				continue;
			}
			if ((value.GroupMembers == null || value.GroupMembers.Count <= 1) && !value.SoloMsgDone)
			{
				if (!HasDrink(key))
				{
					string text2 = LanguageControl.Get("SubsystemMug", "solo_empty");
					key.ComponentGui?.DisplaySmallMessage(text2, Color.White, blinking: true, playNotificationSound: true);
				}
				else
				{
					string text3 = LanguageControl.Get("SubsystemMug", "solo");
					key.ComponentGui?.DisplaySmallMessage(text3, Color.White, blinking: true, playNotificationSound: true);
				}
				value.SoloMsgDone = true;
			}
			if (num >= 5.0)
			{
				if (HasDrink(key))
				{
					float pitch3 = m_random.Float(-0.2f, 0.2f);
					m_subsystemAudio.PlayRandomSound("Audio/Creatures/HumanDrink", 1f, pitch3, key.ComponentBody.Position, 16f, autoDelay: true);
					EmptyActiveMug(key);
				}
				value.GulpDone = true;
				s_toClear.Add(key);
			}
		}
		foreach (ComponentPlayer item2 in s_toClear)
		{
			m_active.Remove(item2);
		}
		s_toClear.Clear();
	}

	private void TryPairWaitingPlayers(double now)
	{
		s_waiting.Clear();
		foreach (KeyValuePair<ComponentPlayer, MugUseState> item in m_active)
		{
			ComponentPlayer key = item.Key;
			MugUseState value = item.Value;
			if ((value.GroupMembers == null || value.GroupMembers.Count <= 1) && now - value.UseStartRealTime <= 3.0 && Terrain.ExtractContents(key.ComponentMiner.ActiveBlockValue) == 429)
			{
				s_waiting.Add(key);
			}
		}
		int count = s_waiting.Count;
		if (count == 0)
		{
			return;
		}
		bool[] array = new bool[count];
		for (int i = 0; i < count; i++)
		{
			if (array[i])
			{
				continue;
			}
			List<ComponentPlayer> list = new List<ComponentPlayer>();
			Queue<int> queue = new Queue<int>();
			queue.Enqueue(i);
			array[i] = true;
			while (queue.Count > 0)
			{
				int index = queue.Dequeue();
				ComponentPlayer componentPlayer = s_waiting[index];
				list.Add(componentPlayer);
				for (int j = 0; j < count; j++)
				{
					if (!array[j])
					{
						ComponentPlayer componentPlayer2 = s_waiting[j];
						if (Vector3.DistanceSquared(componentPlayer.ComponentBody.Position, componentPlayer2.ComponentBody.Position) <= 9f)
						{
							array[j] = true;
							queue.Enqueue(j);
						}
					}
				}
			}
			if (list.Count >= 2)
			{
				for (int k = 0; k < list.Count; k++)
				{
					ComponentPlayer key2 = list[k];
					MugUseState mugUseState = m_active[key2];
					mugUseState.GroupMembers = list;
					mugUseState.AnimStartRealTime = now;
				}
			}
		}
		s_waiting.Clear();
	}

	private void EmptyActiveMug(ComponentPlayer player)
	{
		try
		{
			IInventory inventory = player.ComponentMiner?.Inventory;
			if (inventory == null)
			{
				return;
			}
			int activeSlotIndex = inventory.ActiveSlotIndex;
			if (activeSlotIndex < 0 || activeSlotIndex >= inventory.SlotsCount)
			{
				return;
			}
			int slotValue = inventory.GetSlotValue(activeSlotIndex);
			if (Terrain.ExtractContents(slotValue) != 429 || inventory.GetSlotCount(activeSlotIndex) <= 0)
			{
				return;
			}
			MugBlock.DrinkType drink = MugBlock.GetDrink(slotValue);
			int data = Terrain.ExtractData(slotValue);
			int num = Terrain.MakeBlockValue(429, 0, MugBlock.SetDrink(data, MugBlock.DrinkType.None));
			if (num == slotValue)
			{
				return;
			}
			if (CommonLib.WorkType != WorkType.Client)
			{
				float num2 = 0f;
				switch (drink)
				{
				case MugBlock.DrinkType.Beer:
					num2 = 0.75f;
					break;
				case MugBlock.DrinkType.Kvass:
					num2 = 0.1f;
					break;
				case MugBlock.DrinkType.PumpkinTincture:
					num2 = 0.35f;
					break;
				case MugBlock.DrinkType.BlackBrew:
					num2 = 100f;
					break;
				}
				float num3 = ComputeAlcoholLevelMultiplier(player.PlayerData.Level);
				num2 *= num3;
				ComponentAlcohol componentAlcohol = player.Entity.FindComponent<ComponentAlcohol>(throwOnError: false);
				if (componentAlcohol != null && num2 > 0f)
				{
					componentAlcohol.AddAlcohol(num2);
				}
				if (drink == MugBlock.DrinkType.Kvass)
				{
					ComponentVitalStats componentVitalStats = player.Entity.FindComponent<ComponentVitalStats>(throwOnError: false);
					if (componentVitalStats != null)
					{
						componentVitalStats.Food += 0.2f;
					}
					player.ComponentHealth.Heal(0.05f);
				}
				inventory.RemoveSlotItems(activeSlotIndex, 1);
				inventory.AddSlotItems(activeSlotIndex, num, 1);
			}
			else
			{
				inventory.RemoveNetSlotItems(activeSlotIndex, 1);
				inventory.AddNetSlotItems(activeSlotIndex, num, 1);
			}
		}
		catch
		{
		}
	}

	static SubsystemMugBlockBehavior()
	{
		s_waiting = new List<ComponentPlayer>();
		s_toClear = new List<ComponentPlayer>();
	}

	private bool HasDrink(ComponentPlayer player)
	{
		try
		{
			IInventory text = player.ComponentMiner?.Inventory;
			if (text != null)
			{
				int activeSlotIndex = text.ActiveSlotIndex;
				if (activeSlotIndex >= 0 && activeSlotIndex < text.SlotsCount)
				{
					int slotValue = text.GetSlotValue(activeSlotIndex);
					if (Terrain.ExtractContents(slotValue) == 429)
					{
						return MugBlock.GetDrink(slotValue) > MugBlock.DrinkType.None;
					}
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private static float ComputeAlcoholLevelMultiplier(float level)
	{
		float f = (MathUtils.Clamp(level, 1f, 21f) - 1f) / 20f;
		return MathUtils.Lerp(1f, 2f / 15f, f);
	}

	private void SpawnClinkSplash(ComponentPlayer p, bool large)
	{
		try
		{
			if (HasDrink(p) && m_subsystemParticles != null && m_subsystemTerrain != null && p != null)
			{
				ComponentCreatureModel componentCreatureModel = p.ComponentCreatureModel;
				ComponentBody componentBody = p.ComponentBody;
				if (componentCreatureModel != null && componentBody != null)
				{
					Vector3 eyePosition = componentCreatureModel.EyePosition;
					Vector3 forwardVector = componentCreatureModel.EyeRotation.GetForwardVector();
					Vector3 position = eyePosition - 0.15f * Vector3.UnitY + 0.25f * forwardVector;
					m_subsystemParticles.AddParticleSystem(new WaterSplashParticleSystem(m_subsystemTerrain, position, large));
				}
			}
		}
		catch
		{
		}
	}
}
