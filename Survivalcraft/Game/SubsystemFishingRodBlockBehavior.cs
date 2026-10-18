using System.Collections.Generic;
using Engine;
using Engine.Audio;
using Game.NetWork;
using TemplatesDatabase;

namespace Game;

public class SubsystemFishingRodBlockBehavior : SubsystemBlockBehavior, IUpdateable
{
	private SubsystemTime m_subsystemTime;

	private SubsystemAudio m_subsystemAudio;

	private Dictionary<ComponentMiner, double> m_aimStartTimes;

	private Random m_random;

	private SubsystemProjectiles m_subsystemProjectiles;

	private SubsystemFishingFloats m_subsystemFishingFloats;

	private SubsystemPlayers m_subsystemPlayers;

	private Dictionary<ComponentPlayer, bool> m_isPressing;

	private Dictionary<ComponentPlayer, double> m_lastAimCallTime;

	private readonly Dictionary<ComponentPlayer, Sound> m_sndAimCreak = new Dictionary<ComponentPlayer, Sound>();

	public override int[] HandledBlocks => new int[1] { 395 };

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override void Load(ValuesDictionary valuesDictionary)
	{
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemProjectiles = Project.FindSubsystem<SubsystemProjectiles>(throwOnError: true);
		m_subsystemFishingFloats = Project.FindSubsystem<SubsystemFishingFloats>(throwOnError: true);
		m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
		m_subsystemFishingFloats.FloatAdded += (FishingFloat f) =>
		{
			ComponentPlayer componentPlayer = f?.Owner?.Entity?.FindComponent<ComponentPlayer>();
			ComponentPlayer componentPlayer2 = componentPlayer;
			if (componentPlayer2 != null)
			{
				FadeLoop(GetAimCreak(componentPlayer2), 0f);
			}
		};
		m_subsystemFishingFloats.FloatRemoved += (FishingFloat f) =>
		{
			ComponentPlayer componentPlayer = f?.Owner?.Entity?.FindComponent<ComponentPlayer>();
			ComponentPlayer componentPlayer2 = componentPlayer;
			if (componentPlayer2 != null)
			{
				StopAimCreak(componentPlayer2);
			}
		};
		base.Load(valuesDictionary);
	}

	public override bool OnEditInventoryItem(IInventory inventory, int slotIndex, ComponentPlayer componentPlayer)
	{
		componentPlayer.ComponentGui.ModalPanelWidget = ((componentPlayer.ComponentGui.ModalPanelWidget == null) ? new FishingRodWidget(inventory, slotIndex) : null);
		return true;
	}

	public override bool OnAim(Ray3 aim, ComponentMiner componentMiner, AimState state)
	{
		IInventory inventory = componentMiner.Inventory;
		if (inventory == null)
		{
			return false;
		}
		int activeSlotIndex = inventory.ActiveSlotIndex;
		if (activeSlotIndex < 0)
		{
			return false;
		}
		int slotValue = inventory.GetSlotValue(activeSlotIndex);
		int slotCount = inventory.GetSlotCount(activeSlotIndex);
		if (Terrain.ExtractContents(slotValue) != 395 || slotCount == 0)
		{
			return false;
		}
		int data = Terrain.ExtractData(slotValue);
		bool flag = FishingRodBlock.HasBobber(data);
		ComponentCreature componentCreature = componentMiner.ComponentCreature;
		switch (state)
		{
		case AimState.InProgress:
			componentCreature.ComponentCreatureModel.AimHandAngleOrder = 0.698f;
			break;
		case AimState.Cancelled:
		case AimState.Completed:
			componentCreature.ComponentCreatureModel.AimHandAngleOrder = 0f;
			break;
		}
		bool flag2 = m_subsystemFishingFloats.FindFloat(componentCreature) != null;
		if (!flag2 && !flag)
		{
			return false;
		}
		if (!m_aimStartTimes.TryGetValue(componentMiner, out var value))
		{
			value = m_subsystemTime.GameTime;
			m_aimStartTimes[componentMiner] = value;
		}
		float num = MathUtils.Min((float)(m_subsystemTime.GameTime - value), 1f);
		ComponentPlayer componentPlayer = componentMiner.ComponentPlayer;
		if (componentPlayer != null)
		{
			if (!flag2 & flag)
			{
				float num2 = MathUtils.Saturate(num / 1f);
				componentPlayer.ComponentAimingSights.SetSightsSizeFactor(1f - 0.6f * num2);
				componentPlayer.ComponentAimingSights.ShowAimingSights(aim.Position, aim.Direction);
				Sound aimCreak = GetAimCreak(componentPlayer);
				if (state == AimState.InProgress)
				{
					if (num2 < 1f)
					{
						if (aimCreak.State != SoundState.Playing)
						{
							aimCreak.Pitch = AudioManager.ToEnginePitch(m_random.Float(-0.2f, 0.2f));
						}
						FadeLoop(aimCreak, 0.6f);
					}
					else
					{
						FadeLoop(aimCreak, 0f);
					}
				}
			}
			else if (flag2)
			{
				m_lastAimCallTime[componentPlayer] = m_subsystemTime.GameTime;
				if (state == AimState.InProgress)
				{
					m_isPressing[componentPlayer] = true;
				}
				if (state == AimState.Cancelled || state == AimState.Completed)
				{
					m_isPressing[componentPlayer] = false;
				}
				bool value2 = false;
				m_isPressing.TryGetValue(componentPlayer, out value2);
				componentPlayer.ComponentAimingSights.SetSightsSizeFactor(value2 ? 0.62f : 0.5f);
				componentPlayer.ComponentAimingSights.ShowAimingSights(aim.Position, aim.Direction);
				FadeLoop(GetAimCreak(componentPlayer), 0f);
			}
		}
		switch (state)
		{
		case AimState.Cancelled:
			m_aimStartTimes.Remove(componentMiner);
			if (componentPlayer != null)
			{
				m_isPressing[componentPlayer] = false;
				FadeLoop(GetAimCreak(componentPlayer), 0f);
			}
			return false;
		default:
			return false;
		case AimState.Completed:
		{
			if (flag2)
			{
				bool flag3 = true;
				if (CommonLib.WorkType != WorkType.Client)
				{
					flag3 = m_subsystemFishingFloats.ProcessReelAttempt(componentCreature);
				}
				if (flag3 && CommonLib.WorkType != WorkType.Client)
				{
					m_subsystemFishingFloats.ReelFloat(componentCreature);
				}
				m_aimStartTimes.Remove(componentMiner);
				if (componentPlayer != null)
				{
					m_isPressing[componentPlayer] = false;
					FadeLoop(GetAimCreak(componentPlayer), 0f);
				}
				return true;
			}
			float num3 = MathUtils.Saturate(num / 1f);
			Vector3 position = componentCreature.ComponentCreatureModel.EyePosition + componentCreature.ComponentBody.Matrix.Right * 0.3f;
			Vector3 velocity = aim.Direction * (4f + 16f * num3);
			if (CommonLib.WorkType != WorkType.Client)
			{
				int bobberBait = FishingRodBlock.GetBobberBait(data);
				int baitData = FishingRodBlock.GetRodBaitData(data) & 0xF;
				int rot = FishingRodBlock.GetBobberBaitRot(data) & 1;
				int data2 = BobberBlock.SetBait(0, bobberBait, baitData, rot);
				int bobberValue = Terrain.MakeBlockValue(396, 0, data2);
				m_subsystemFishingFloats.CastFloat(position, velocity, Vector3.Zero, componentCreature, bobberValue);
				data = FishingRodBlock.SetRodBaitData(data, 0);
				data = FishingRodBlock.SetBobber(data, hasBobber: false, 0, 0);
				inventory.RemoveSlotItems(activeSlotIndex, 1);
				inventory.AddSlotItems(activeSlotIndex, Terrain.MakeBlockValue(395, 0, data & -4), 1);
			}
			m_subsystemAudio.PlayRandomSound("Audio/Fishing/Throw", 0.9f, m_random.Float(-0.2f, 0.2f), componentCreature.ComponentCreatureModel.EyePosition, 6f, autoDelay: true);
			if (componentPlayer != null)
			{
				FadeLoop(GetAimCreak(componentPlayer), 0f);
			}
			m_aimStartTimes.Remove(componentMiner);
			if (componentPlayer != null)
			{
				m_isPressing[componentPlayer] = false;
			}
			return true;
		}
		}
	}

	public override int GetProcessInventoryItemCapacity(IInventory inv, int slotIndex, int value)
	{
		int slotValue = inv.GetSlotValue(slotIndex);
		if (Terrain.ExtractContents(slotValue) != 395)
		{
			return 0;
		}
		int data = Terrain.ExtractData(slotValue);
		int num = ((value != 0) ? Terrain.ExtractContents(value) : 0);
		switch (num)
		{
		case 396:
			if (!FishingRodBlock.HasBobber(data))
			{
				return 1;
			}
			return 0;
		case 0:
			if (!FishingRodBlock.HasBobber(data))
			{
				return 0;
			}
			if (FishingRodBlock.GetBobberBait(data) == 0)
			{
				return 0;
			}
			return 1;
		default:
		{
			if (!FishingRodBlock.HasBobber(data))
			{
				return 0;
			}
			if (!BobberBaitInfo.IsAllowedBait(num))
			{
				return 0;
			}
			int bobberBait = FishingRodBlock.GetBobberBait(data);
			if (bobberBait != 0 && bobberBait == num)
			{
				return 0;
			}
			return 1;
		}
		}
	}

	public override void ProcessInventoryItem(IInventory inv, int slotIndex, int value, int count, int procCap, out int pValue, out int pCount)
	{
		pValue = value;
		pCount = count;
		if (procCap != 1)
		{
			return;
		}
		int slotValue = inv.GetSlotValue(slotIndex);
		if (Terrain.ExtractContents(slotValue) != 395)
		{
			return;
		}
		int data = Terrain.ExtractData(slotValue);
		bool flag = FishingRodBlock.HasBobber(data);
		int num = ((value != 0) ? Terrain.ExtractContents(value) : 0);
		bool flag2 = CommonLib.WorkType != WorkType.Client;
		if (num == 396 && !flag)
		{
			int data2 = Terrain.ExtractData(value);
			int bait = BobberBlock.GetBait(data2);
			int baitData = BobberBlock.GetBaitData(data2) & 0xF;
			int rot = BobberBlock.GetBaitRot(data2) & 1;
			if (flag2)
			{
				int data3 = FishingRodBlock.SetRodBaitData(data, baitData);
				data3 = FishingRodBlock.SetBobber(data3, hasBobber: true, bait, rot);
				int value2 = Terrain.MakeBlockValue(395, 0, data3);
				inv.RemoveSlotItems(slotIndex, 1);
				inv.AddSlotItems(slotIndex, value2, 1);
			}
			pValue = value;
			pCount = count - 1;
			if (pCount <= 0)
			{
				pValue = 0;
				pCount = 0;
			}
		}
		else
		{
			if (!flag)
			{
				return;
			}
			int bobberBait = FishingRodBlock.GetBobberBait(data);
			if (num == 0)
			{
				if (bobberBait == 0)
				{
					pValue = 0;
					pCount = 0;
					return;
				}
				int damage = FishingRodBlock.GetBobberBaitRot(data) & 1;
				int data4 = FishingRodBlock.GetRodBaitData(data) & 0xF;
				int value3 = Terrain.MakeBlockValue(bobberBait, 0, data4);
				if (bobberBait != 318)
				{
					value3 = BlocksManager.Blocks[bobberBait].SetDamage(value3, damage);
				}
				int num2 = ComponentInventoryBase.FindAcquireSlotForItem(inv, value3);
				if (num2 == slotIndex)
				{
					num2 = -1;
				}
				if (num2 < 0)
				{
					for (int i = 0; i < inv.SlotsCount; i++)
					{
						if (i != slotIndex && inv.GetSlotValue(i) == 0)
						{
							num2 = i;
							break;
						}
					}
				}
				if (num2 < 0)
				{
					ComponentPlayer componentPlayer = null;
					foreach (ComponentPlayer componentPlayer3 in m_subsystemPlayers.ComponentPlayers)
					{
						if (componentPlayer3.ComponentMiner != null && componentPlayer3.ComponentMiner.Inventory == inv)
						{
							componentPlayer = componentPlayer3;
							break;
						}
					}
					if (componentPlayer != null && componentPlayer.ComponentGui != null)
					{
						componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(FishingRodWidget.fName, 8), Color.White, blinking: true, playNotificationSound: false);
					}
				}
				else
				{
					if (flag2)
					{
						int data5 = FishingRodBlock.SetRodBaitData(data, 0);
						data5 = FishingRodBlock.SetBobber(data5, hasBobber: true, 0, 0);
						int value4 = Terrain.MakeBlockValue(395, 0, data5);
						inv.RemoveSlotItems(slotIndex, 1);
						inv.AddSlotItems(slotIndex, value4, 1);
						inv.AddSlotItems(num2, value3, 1);
					}
					pValue = 0;
					pCount = 0;
				}
			}
			else
			{
				if (!BobberBaitInfo.IsAllowedBait(num) || (bobberBait != 0 && bobberBait == num))
				{
					return;
				}
				int damage2 = FishingRodBlock.GetBobberBaitRot(data) & 1;
				int num3 = -1;
				if (bobberBait != 0)
				{
					int data6 = FishingRodBlock.GetRodBaitData(data) & 0xF;
					int value5 = Terrain.MakeBlockValue(bobberBait, 0, data6);
					if (bobberBait != 318)
					{
						value5 = BlocksManager.Blocks[bobberBait].SetDamage(value5, damage2);
					}
					int num4 = ComponentInventoryBase.FindAcquireSlotForItem(inv, value5);
					if (num4 == slotIndex || (num4 >= 0 && inv.GetSlotValue(num4) == value))
					{
						num4 = -1;
					}
					if (num4 < 0)
					{
						for (int j = 0; j < inv.SlotsCount; j++)
						{
							if (j != slotIndex && inv.GetSlotValue(j) == 0)
							{
								num4 = j;
								break;
							}
						}
					}
					if (num4 < 0)
					{
						ComponentPlayer componentPlayer2 = null;
						foreach (ComponentPlayer componentPlayer4 in m_subsystemPlayers.ComponentPlayers)
						{
							if (componentPlayer4.ComponentMiner != null && componentPlayer4.ComponentMiner.Inventory == inv)
							{
								componentPlayer2 = componentPlayer4;
								break;
							}
						}
						if (componentPlayer2 != null && componentPlayer2.ComponentGui != null)
						{
							componentPlayer2.ComponentGui.DisplaySmallMessage(LanguageControl.Get(FishingRodWidget.fName, 8), Color.White, blinking: true, playNotificationSound: false);
						}
						return;
					}
					num3 = num4;
				}
				int baitData2 = Terrain.ExtractData(value) & 0xF;
				int rot2 = BlocksManager.Blocks[num].GetDamage(value) & 1;
				if (flag2)
				{
					int data7 = FishingRodBlock.SetRodBaitData(data, baitData2);
					data7 = FishingRodBlock.SetBobber(data7, hasBobber: true, num, rot2);
					int value6 = Terrain.MakeBlockValue(395, 0, data7);
					inv.RemoveSlotItems(slotIndex, 1);
					inv.AddSlotItems(slotIndex, value6, 1);
					if (num3 >= 0 && bobberBait != 0)
					{
						int data8 = FishingRodBlock.GetRodBaitData(data) & 0xF;
						int value7 = Terrain.MakeBlockValue(bobberBait, 0, data8);
						if (bobberBait != 318)
						{
							value7 = BlocksManager.Blocks[bobberBait].SetDamage(value7, damage2);
						}
						inv.AddSlotItems(num3, value7, 1);
					}
				}
				pValue = 0;
				pCount = 0;
			}
		}
	}

	public SubsystemFishingRodBlockBehavior()
	{
		m_isPressing = new Dictionary<ComponentPlayer, bool>();
		m_lastAimCallTime = new Dictionary<ComponentPlayer, double>();
		m_aimStartTimes = new Dictionary<ComponentMiner, double>();
		m_random = new Random();
	}

	public void Update(float dt)
	{
		foreach (ComponentPlayer componentPlayer in m_subsystemPlayers.ComponentPlayers)
		{
			ComponentMiner componentMiner = componentPlayer.ComponentMiner;
			if (componentMiner == null || componentMiner.Inventory == null)
			{
				continue;
			}
			IInventory inventory = componentMiner.Inventory;
			int activeSlotIndex = inventory.ActiveSlotIndex;
			int value = ((activeSlotIndex >= 0) ? inventory.GetSlotValue(activeSlotIndex) : 0);
			bool num = activeSlotIndex >= 0 && Terrain.ExtractContents(value) == 395 && inventory.GetSlotCount(activeSlotIndex) > 0;
			ComponentCreature componentCreature = componentPlayer.Entity.FindComponent<ComponentCreature>();
			bool flag = componentCreature != null && m_subsystemFishingFloats.FindFloat(componentCreature) != null;
			bool isCreative = false;
			SubsystemGameInfo subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
			if (subsystemGameInfo != null && subsystemGameInfo.WorldSettings != null)
			{
				isCreative = subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative;
			}
			ComponentHumanModel componentHumanModel = componentCreature?.Entity.FindComponent<ComponentHumanModel>();
			if (componentHumanModel != null)
			{
				componentHumanModel.ForceLeftRodWhileFishing = false;
				componentHumanModel.FishingLineUseLeftHand = false;
				componentHumanModel.OffhandRodValue = 0;
			}
			int rodSlotIndex2;
			if (num)
			{
				if ((componentCreature != null) & flag)
				{
					if (m_lastAimCallTime.TryGetValue(componentPlayer, out var value2))
					{
						if (m_subsystemTime.GameTime - value2 > 0.05)
						{
							m_isPressing[componentPlayer] = false;
						}
					}
					else
					{
						m_isPressing[componentPlayer] = false;
					}
					m_isPressing.TryGetValue(componentPlayer, out var value3);
					float sightsSizeFactor = (value3 ? 0.62f : 0.5f);
					componentCreature.ComponentCreatureModel.AimHandAngleOrder = (value3 ? 0.698f : 0f);
					Camera camera = ((componentPlayer.GameWidget != null) ? componentPlayer.GameWidget.ActiveCamera : null);
					Vector3 position;
					Vector3 direction;
					if (camera != null)
					{
						position = camera.ViewPosition;
						direction = camera.ViewDirection;
					}
					else
					{
						position = componentCreature.ComponentCreatureModel.EyePosition;
						direction = componentCreature.ComponentBody.Matrix.Forward;
					}
					componentPlayer.ComponentAimingSights.SetSightsSizeFactor(sightsSizeFactor);
					componentPlayer.ComponentAimingSights.ShowAimingSights(position, direction);
					FadeLoop(GetAimCreak(componentPlayer), 0f);
				}
				else
				{
					FadeLoop(GetAimCreak(componentPlayer), 0f);
				}
			}
			else if (flag)
			{
				if (FindRodInInventory(inventory, isCreative, out var rodSlotIndex))
				{
					if (componentHumanModel != null)
					{
						componentHumanModel.ForceLeftRodWhileFishing = true;
						componentHumanModel.FishingLineUseLeftHand = true;
						componentHumanModel.OffhandRodValue = inventory.GetSlotValue(rodSlotIndex);
					}
					m_isPressing[componentPlayer] = false;
					if (componentCreature != null)
					{
						componentCreature.ComponentCreatureModel.AimHandAngleOrder = 0f;
						Camera camera2 = ((componentPlayer.GameWidget != null) ? componentPlayer.GameWidget.ActiveCamera : null);
						Vector3 position2;
						Vector3 direction2;
						if (camera2 != null)
						{
							position2 = camera2.ViewPosition;
							direction2 = camera2.ViewDirection;
						}
						else
						{
							position2 = componentCreature.ComponentCreatureModel.EyePosition;
							direction2 = componentCreature.ComponentBody.Matrix.Forward;
						}
						componentPlayer.ComponentAimingSights.SetSightsSizeFactor(0.5f);
						componentPlayer.ComponentAimingSights.ShowAimingSights(position2, direction2);
					}
				}
				else if (componentCreature != null && CommonLib.WorkType != WorkType.Client)
				{
					m_subsystemFishingFloats.ReelFloat(componentCreature);
				}
				FadeLoop(GetAimCreak(componentPlayer), 0f);
			}
			else if (FindRodInInventory(inventory, isCreative, out rodSlotIndex2))
			{
				FadeLoop(GetAimCreak(componentPlayer), 0f);
			}
			else
			{
				StopAimCreak(componentPlayer);
			}
		}
	}

	private bool FindRodInInventory(IInventory inventory, bool isCreative, out int rodSlotIndex)
	{
		rodSlotIndex = -1;
		int num = inventory.SlotsCount;
		if (isCreative && inventory is ComponentCreativeInventory)
		{
			num = MathUtils.Min(25, num);
		}
		for (int i = 0; i < num; i++)
		{
			int slotValue = inventory.GetSlotValue(i);
			if (inventory.GetSlotCount(i) > 0 && Terrain.ExtractContents(slotValue) == 395)
			{
				rodSlotIndex = i;
				return true;
			}
		}
		return false;
	}

	private Sound GetAimCreak(ComponentPlayer p)
	{
		if (!m_sndAimCreak.TryGetValue(p, out var value) || value.State == SoundState.Disposed)
		{
			value = m_subsystemAudio.CreateSound("Audio/Fishing/Creak");
			value.IsLooped = true;
			value.Volume = 0f;
			m_sndAimCreak[p] = value;
		}
		return value;
	}

	private static void FadeLoop(Sound s, float target)
	{
		float f = MathUtils.Saturate(2f * Time.FrameDuration);
		float num = (s.Volume = MathUtils.Lerp(s.Volume, SettingsManager.SoundsVolume * target, f));
		if (num > 0.5f * AudioManager.MinAudibleVolume)
		{
			s.Play();
		}
		else
		{
			s.Pause();
		}
	}

	private void StopAimCreak(ComponentPlayer p, bool dispose = true)
	{
		if (p == null || !m_sndAimCreak.TryGetValue(p, out var value) || value == null)
		{
			return;
		}
		try
		{
			value.Volume = 0f;
			value.Pause();
			value.Stop();
		}
		catch
		{
		}
		if (dispose)
		{
			try
			{
				value.Dispose();
			}
			catch
			{
			}
			m_sndAimCreak.Remove(p);
		}
	}

	public override bool OnUse(Ray3 ray, ComponentMiner componentMiner)
	{
		IInventory inventory = componentMiner.Inventory;
		if (inventory == null)
		{
			return false;
		}
		int activeSlotIndex = inventory.ActiveSlotIndex;
		if (activeSlotIndex < 0)
		{
			return false;
		}
		int slotValue = inventory.GetSlotValue(activeSlotIndex);
		int slotCount = inventory.GetSlotCount(activeSlotIndex);
		if (Terrain.ExtractContents(slotValue) != 395 || slotCount == 0)
		{
			return false;
		}
		ComponentCreature componentCreature = componentMiner.ComponentCreature;
		if (componentCreature == null)
		{
			return false;
		}
		if (m_subsystemFishingFloats.FindFloat(componentCreature) == null)
		{
			return false;
		}
		ComponentPlayer componentPlayer = componentMiner.ComponentPlayer;
		bool flag = true;
		if (CommonLib.WorkType != WorkType.Client)
		{
			flag = m_subsystemFishingFloats.ProcessReelAttempt(componentCreature);
		}
		if (flag && CommonLib.WorkType != WorkType.Client)
		{
			m_subsystemFishingFloats.ReelFloat(componentCreature);
		}
		m_aimStartTimes.Remove(componentMiner);
		if (componentPlayer != null)
		{
			m_isPressing[componentPlayer] = false;
			FadeLoop(GetAimCreak(componentPlayer), 0f);
		}
		return true;
	}
}
