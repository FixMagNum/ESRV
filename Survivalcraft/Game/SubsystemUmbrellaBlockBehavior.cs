using System.Collections.Generic;
using Engine;
using Engine.Audio;
using Game.NetWork;
using TemplatesDatabase;

namespace Game;

public class SubsystemUmbrellaBlockBehavior : SubsystemBlockBehavior, IUpdateable
{
	private SubsystemTerrain m_subsystemTerrain;

	private SubsystemAudio m_subsystemAudio;

	public Random m_random;

	private SubsystemWeather m_subsystemWeather;

	private SubsystemPlayers m_subsystemPlayers;

	private Dictionary<ComponentPlayer, Sound> m_sounds;

	private SubsystemPickables m_subsystemPickables;

	private Dictionary<ushort, Sound> m_pickableSounds;

	private List<ComponentPlayer> m_tempPlayersWithoutUmbrella;

	private HashSet<ushort> m_tempActivePickableIds;

	private List<ushort> m_tempPickableIds;

	private List<Sound> m_tempPickableSounds;

	private List<float> m_tempPickableVolumes;

	private List<int> m_tempPickableCoords;

	private Dictionary<int, List<int>> m_tempGroupedPickables;

	private List<ushort> m_tempSoundsToRemove;

	private SubsystemTime m_subsystemTime;

	private Dictionary<Sound, float> m_targetVolumes = new Dictionary<Sound, float>();

	public override int[] HandledBlocks => new int[1] { 379 };

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

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
		if (Terrain.ExtractContents(slotValue) != 379)
		{
			return false;
		}
		if (CommonLib.WorkType != WorkType.Client)
		{
			int num = Terrain.ExtractData(slotValue);
			int num2 = (((num & 1) == 0) ? 1 : 0);
			int value = Terrain.MakeBlockValue(379, 0, (num & -2) | num2);
			if (inventory.GetSlotCapacity(activeSlotIndex, value) < 1)
			{
				return false;
			}
			inventory.RemoveSlotItems(activeSlotIndex, 1);
			inventory.AddSlotItems(activeSlotIndex, value, 1);
			componentMiner.DamageActiveTool(1);
		}
		m_subsystemAudio.PlayRandomSound("Audio/UmbrellaInteract", 1f, m_random.Float(0.2f, 0.6f), componentMiner.ComponentPlayer.Entity.FindComponent<ComponentCreature>().ComponentBody.Position, 4f, 0f);
		return true;
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
		m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
		m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_pickableSounds = new Dictionary<ushort, Sound>();
		m_sounds = new Dictionary<ComponentPlayer, Sound>();
	}

	public SubsystemUmbrellaBlockBehavior()
	{
		m_tempPlayersWithoutUmbrella = new List<ComponentPlayer>();
		m_tempActivePickableIds = new HashSet<ushort>();
		m_tempPickableIds = new List<ushort>();
		m_tempPickableSounds = new List<Sound>();
		m_tempPickableVolumes = new List<float>();
		m_tempPickableCoords = new List<int>();
		m_tempGroupedPickables = new Dictionary<int, List<int>>();
		m_tempSoundsToRemove = new List<ushort>();
		m_random = new Random();
	}

	public void Update(float dt)
	{
		if (m_subsystemTime.PeriodicGameTimeEvent(0.25, 0.0))
		{
			UpdateHeavyLogic();
		}
		float speed = 2f;
		ProcessSmoothVolume(m_sounds, dt, speed);
		ProcessSmoothVolume(m_pickableSounds, dt, speed);
	}

	private void UpdateHeavyLogic()
	{
		m_tempPlayersWithoutUmbrella.Clear();
		m_tempActivePickableIds.Clear();
		m_tempPickableIds.Clear();
		m_tempPickableSounds.Clear();
		m_tempPickableVolumes.Clear();
		m_tempPickableCoords.Clear();
		m_tempGroupedPickables.Clear();
		m_tempSoundsToRemove.Clear();
		foreach (ComponentPlayer componentPlayer in m_subsystemPlayers.ComponentPlayers)
		{
			bool flag = false;
			ComponentMiner componentMiner = componentPlayer.ComponentMiner;
			if (componentMiner != null)
			{
				int slotValue = componentMiner.Inventory.GetSlotValue(componentMiner.Inventory.ActiveSlotIndex);
				if (Terrain.ExtractContents(slotValue) == 379 && (Terrain.ExtractData(slotValue) & 1) != 0)
				{
					ComponentBody componentBody = componentPlayer.Entity.FindComponent<ComponentCreature>().ComponentBody;
					int x = Terrain.ToCell(componentBody.Position.X);
					int z = Terrain.ToCell(componentBody.Position.Z);
					PrecipitationShaftInfo precipitationShaftInfo = m_subsystemWeather.GetPrecipitationShaftInfo(x, z);
					if (precipitationShaftInfo.Type == PrecipitationType.Rain && precipitationShaftInfo.Intensity > 0f)
					{
						float y = componentBody.BoundingBox.Max.Y;
						Vector3 start = new Vector3(componentBody.Position.X, y + 0.1f, componentBody.Position.Z);
						Vector3 end = new Vector3(componentBody.Position.X, (float)precipitationShaftInfo.YLimit + 0.1f, componentBody.Position.Z);
						if (!m_subsystemTerrain.Raycast(start, end, useInteractionBoxes: false, skipAirBlocks: true, (int value7, float distanceRay) => BlocksManager.Blocks[Terrain.ExtractContents(value7)].IsCollidable).HasValue)
						{
							flag = true;
							if (!m_sounds.TryGetValue(componentPlayer, out var value))
							{
								value = m_subsystemAudio.CreateSound("Audio/Rain/UmbrellaRain");
								value.IsLooped = true;
								value.CurrentPosition = m_random.Float(0f, value.Duration);
								value.Play();
								m_sounds[componentPlayer] = value;
							}
							float value2 = MathUtils.Saturate(SettingsManager.SoundsVolume * precipitationShaftInfo.Intensity);
							m_targetVolumes[value] = value2;
						}
					}
				}
			}
			if (!flag)
			{
				m_tempPlayersWithoutUmbrella.Add(componentPlayer);
			}
		}
		foreach (ComponentPlayer item3 in m_tempPlayersWithoutUmbrella)
		{
			if (m_sounds.TryGetValue(item3, out var value3))
			{
				value3.Stop();
				m_targetVolumes.Remove(value3);
				value3.Dispose();
			}
			m_sounds.Remove(item3);
		}
		if (m_subsystemPickables == null)
		{
			return;
		}
		ReadOnlyList<Pickable> pickables = m_subsystemPickables.Pickables;
		float num = 4f;
		for (int num2 = 0; num2 < pickables.Count; num2++)
		{
			Pickable pickable = pickables[num2];
			if (Terrain.ExtractContents(pickable.Value) != 379 || (Terrain.ExtractData(pickable.Value) & 1) == 0)
			{
				continue;
			}
			Vector3 p = new Vector3(pickable.Position.X, pickable.Position.Y + 0.5f, pickable.Position.Z);
			float distance = m_subsystemAudio.CalculateListenerDistance(p);
			float num3 = m_subsystemAudio.CalculateVolume(distance, 4f, 6f);
			if (!(num3 > 0.01f))
			{
				continue;
			}
			int x2 = Terrain.ToCell(pickable.Position.X);
			int z2 = Terrain.ToCell(pickable.Position.Z);
			PrecipitationShaftInfo precipitationShaftInfo2 = m_subsystemWeather.GetPrecipitationShaftInfo(x2, z2);
			if (precipitationShaftInfo2.Type != PrecipitationType.Rain || !(precipitationShaftInfo2.Intensity > 0f))
			{
				continue;
			}
			Vector3 vector = new Vector3(pickable.Position.X, pickable.Position.Y + 2.7f, pickable.Position.Z);
			Vector3 start2 = new Vector3(vector.X, vector.Y + 0.1f, vector.Z);
			Vector3 end2 = new Vector3(vector.X, (float)precipitationShaftInfo2.YLimit + 0.1f, vector.Z);
			if (!m_subsystemTerrain.Raycast(start2, end2, useInteractionBoxes: false, skipAirBlocks: true, (int val, float dist) => BlocksManager.Blocks[Terrain.ExtractContents(val)].IsCollidable).HasValue)
			{
				if (!m_pickableSounds.TryGetValue(pickable.Id, out var value4))
				{
					value4 = m_subsystemAudio.CreateSound("Audio/Rain/UmbrellaRain");
					value4.IsLooped = true;
					value4.CurrentPosition = m_random.Float(0f, value4.Duration);
					value4.Play();
					m_pickableSounds[pickable.Id] = value4;
				}
				float item = MathUtils.Saturate(SettingsManager.SoundsVolume * precipitationShaftInfo2.Intensity * num3);
				int num4 = (int)MathUtils.Floor(vector.X / num);
				int num5 = (int)MathUtils.Floor(vector.Z / num);
				int item2 = (num4 << 16) ^ (num5 & 0xFFFF);
				m_tempPickableIds.Add(pickable.Id);
				m_tempPickableSounds.Add(value4);
				m_tempPickableVolumes.Add(item);
				m_tempPickableCoords.Add(item2);
				m_tempActivePickableIds.Add(pickable.Id);
			}
		}
		for (int num6 = 0; num6 < m_tempPickableIds.Count; num6++)
		{
			int key = m_tempPickableCoords[num6];
			if (!m_tempGroupedPickables.TryGetValue(key, out var value5))
			{
				value5 = new List<int>();
				m_tempGroupedPickables.Add(key, value5);
			}
			value5.Add(num6);
		}
		int num7 = 3;
		foreach (List<int> value7 in m_tempGroupedPickables.Values)
		{
			int count = value7.Count;
			int num8 = ((count < num7) ? count : num7);
			for (int num9 = 0; num9 < num8; num9++)
			{
				int num10 = -1;
				float num11 = -1f;
				for (int num12 = 0; num12 < value7.Count; num12++)
				{
					int index = value7[num12];
					float num13 = m_tempPickableVolumes[index];
					if (num13 > num11)
					{
						num11 = num13;
						num10 = num12;
					}
				}
				if (num10 < 0)
				{
					break;
				}
				int index2 = value7[num10];
				Sound key2 = m_tempPickableSounds[index2];
				float value6 = m_tempPickableVolumes[index2];
				m_targetVolumes[key2] = value6;
				value7.RemoveAt(num10);
			}
			for (int num14 = 0; num14 < value7.Count; num14++)
			{
				int index3 = value7[num14];
				m_targetVolumes[m_tempPickableSounds[index3]] = 0f;
			}
		}
		if (m_pickableSounds.Count <= 0)
		{
			return;
		}
		foreach (KeyValuePair<ushort, Sound> pickableSound in m_pickableSounds)
		{
			if (!m_tempActivePickableIds.Contains(pickableSound.Key))
			{
				if (pickableSound.Value != null)
				{
					pickableSound.Value.Stop();
					m_targetVolumes.Remove(pickableSound.Value);
					pickableSound.Value.Dispose();
				}
				m_tempSoundsToRemove.Add(pickableSound.Key);
			}
		}
		for (int num15 = 0; num15 < m_tempSoundsToRemove.Count; num15++)
		{
			m_pickableSounds.Remove(m_tempSoundsToRemove[num15]);
		}
	}

	private void UpdateVolumesSmoothly(float dt)
	{
	}

	private void ProcessSmoothVolume<T>(Dictionary<T, Sound> sourceDict, float dt, float speed)
	{
		foreach (Sound value2 in sourceDict.Values)
		{
			if (!m_targetVolumes.TryGetValue(value2, out var value))
			{
				value = 0f;
			}
			if (value2.Volume == value)
			{
				continue;
			}
			float x = value - value2.Volume;
			float num = MathUtils.Sign(x) * MathUtils.Min(MathUtils.Abs(x), speed * dt);
			value2.Volume += num;
			if (value2.Volume <= 0.01f && value <= 0.01f)
			{
				if (value2.State == SoundState.Playing)
				{
					value2.Pause();
				}
			}
			else if (value2.State != SoundState.Playing)
			{
				value2.Play();
			}
		}
	}
}
