using System;
using System.Collections.Generic;
using Engine;
using TemplatesDatabase;

namespace Game;

public class SubsystemAnemometerBlockBehavior : SubsystemBlockBehavior, IUpdateable
{
	private class State
	{
		public float Angle;

		public float Omega;

		public float LastKnownStrength;

		public bool HadExposure;

		public LabelWidget ValueLabel;

		public LabelWidget MemoryLabel;

		public Random Rand = new Random();

		public StackPanelWidget Panel;

		public float SampleTimer;

		public bool SampledExposed;

		public float SampledStrength;
	}

	private class PickState
	{
		public float Angle;

		public float Omega;

		public Random Rand = new Random();

		public float SampleTimer;

		public bool SampledExposed;

		public float SampledStrength;
	}

	private SubsystemWeather m_weather;

	private SubsystemPlayers m_players;

	private readonly Dictionary<string, State> _states;

	private const float MaxOmega = 15f;

	private const float AccelLerp = 2f;

	private const float DampingLerp = 0.5f;

	private const float JitterOmega = 0.5f;

	private static readonly float kHudPad;

	private static readonly float kLineStep;

	private SubsystemPickables m_pickables;

	private readonly Dictionary<ushort, PickState> _pickableStates = new Dictionary<ushort, PickState>();

	private const float SlowSamplePeriod = 2.5f;

	public static string fName;

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override int[] HandledBlocks => new int[1] { 430 };

	public override void Load(ValuesDictionary values)
	{
		m_weather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
		m_players = Project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
		m_pickables = Project.FindSubsystem<SubsystemPickables>(throwOnError: true);
		if (m_pickables != null)
		{
			m_pickables.PickableRemoved += OnPickableRemoved;
		}
	}

	public void Update(float dt)
	{
		if (m_weather == null)
		{
			return;
		}
		if (m_pickables != null)
		{
			foreach (Pickable pickable in m_pickables.Pickables)
			{
				if (Terrain.ExtractContents(pickable.Value) == 430)
				{
					if (!_pickableStates.TryGetValue(pickable.Id, out var value))
					{
						value = new PickState();
						_pickableStates[pickable.Id] = value;
						value.SampleTimer = 0f;
					}
					value.SampleTimer -= dt;
					if (value.SampleTimer <= 0f)
					{
						value.SampledExposed = m_weather.GetWindExposureFactor(pickable.Position) > 0f;
						value.SampledStrength = (value.SampledExposed ? MathUtils.Clamp(m_weather.CurrentWindStrength, 0f, 16f) : 0f);
						value.SampleTimer = 2.5f;
					}
					float x = (value.SampledExposed ? (15f * (value.SampledStrength / 16f)) : 0f);
					float num = value.Rand.Float(-1f, 1f) * 0.5f;
					float num2 = (value.SampledExposed ? 2f : 0.5f);
					value.Omega = MathUtils.Lerp(value.Omega, x, MathUtils.Saturate(dt * num2));
					value.Omega = MathUtils.Max(0f, value.Omega + num * dt);
					value.Angle = MathUtils.NormalizeAngle(value.Angle + value.Omega * dt);
				}
			}
		}
		if (m_players == null)
		{
			return;
		}
		foreach (PlayerData playersDatum in m_players.PlayersData)
		{
			ComponentPlayer componentPlayer = playersDatum.ComponentPlayer;
			if (componentPlayer == null)
			{
				continue;
			}
			string key = ((playersDatum.PlayerGUID != Guid.Empty) ? playersDatum.PlayerGUID.ToString() : playersDatum.PlayerIndex.ToString());
			if (!_states.TryGetValue(key, out var value2))
			{
				value2 = new State();
				_states[key] = value2;
				value2.SampleTimer = 0f;
			}
			bool flag = PlayerHasAnemometer(componentPlayer);
			EnsureHud(componentPlayer, value2, flag);
			if (!flag)
			{
				continue;
			}
			bool flag2 = componentPlayer.ComponentMiner != null && Terrain.ExtractContents(componentPlayer.ComponentMiner.ActiveBlockValue) == 430;
			if (flag2)
			{
				value2.SampleTimer = 0f;
			}
			else
			{
				value2.SampleTimer -= dt;
			}
			if (value2.SampleTimer <= 0f)
			{
				Vector3 position = componentPlayer.ComponentCreatureModel?.EyePosition ?? componentPlayer.ComponentBody.Position;
				value2.SampledExposed = m_weather.GetWindExposureFactor(position) > 0f;
				value2.SampledStrength = (value2.SampledExposed ? MathUtils.Clamp(m_weather.CurrentWindStrength, 0f, 16f) : 0f);
				if (value2.SampledExposed)
				{
					value2.HadExposure = true;
					value2.LastKnownStrength = value2.SampledStrength;
				}
				value2.SampleTimer = (flag2 ? 0f : 2.5f);
			}
			float x2 = (value2.SampledExposed ? (15f * (value2.SampledStrength / 16f)) : 0f);
			float num3 = value2.Rand.Float(-1f, 1f) * 0.5f;
			float num4 = (value2.SampledExposed ? 2f : 0.5f);
			value2.Omega = MathUtils.Lerp(value2.Omega, x2, MathUtils.Saturate(dt * num4));
			value2.Omega = MathUtils.Max(0f, value2.Omega + num3 * dt);
			value2.Angle = MathUtils.NormalizeAngle(value2.Angle + value2.Omega * dt);
			UpdateHud(componentPlayer, value2, value2.SampledExposed);
		}
	}

	public float GetPointerAngleForRender(Vector3 worldPos)
	{
		if (m_pickables != null)
		{
			Pickable pickable = null;
			float num = 0.09f;
			foreach (Pickable pickable2 in m_pickables.Pickables)
			{
				if (Terrain.ExtractContents(pickable2.Value) == 430)
				{
					float num2 = Vector3.DistanceSquared(pickable2.Position + new Vector3(0f, 0.2f, 0f), worldPos);
					if (num2 < num)
					{
						num = num2;
						pickable = pickable2;
					}
				}
			}
			if (pickable != null && _pickableStates.TryGetValue(pickable.Id, out var value))
			{
				return value.Angle;
			}
		}
		if (m_players == null)
		{
			return 0f;
		}
		ComponentPlayer componentPlayer = null;
		float num3 = float.MaxValue;
		foreach (PlayerData playersDatum in m_players.PlayersData)
		{
			ComponentPlayer componentPlayer2 = playersDatum.ComponentPlayer;
			if (componentPlayer2 != null)
			{
				float num4 = Vector3.DistanceSquared(componentPlayer2.ComponentBody.Position, worldPos);
				if (num4 < num3)
				{
					num3 = num4;
					componentPlayer = componentPlayer2;
				}
			}
		}
		if (componentPlayer != null)
		{
			string key = ((componentPlayer.PlayerData.PlayerGUID != Guid.Empty) ? componentPlayer.PlayerData.PlayerGUID.ToString() : componentPlayer.PlayerData.PlayerIndex.ToString());
			if (_states.TryGetValue(key, out var value2))
			{
				return value2.Angle;
			}
		}
		return 0f;
	}

	private void EnsureHud(ComponentPlayer cp, State s, bool visible)
	{
		float globalScale = cp.GameWidget.GlobalScale;
		if (s.Panel == null)
		{
			s.Panel = new StackPanelWidget
			{
				Direction = LayoutDirection.Vertical,
				HorizontalAlignment = WidgetAlignment.Far,
				VerticalAlignment = WidgetAlignment.Far,
				Margin = new Vector2(kHudPad * globalScale, kHudPad * globalScale)
			};
			cp.GuiWidget.Children.Add(s.Panel);
		}
		if (s.ValueLabel == null)
		{
			s.ValueLabel = new LabelWidget
			{
				Text = ""
			};
			s.Panel.Children.Add(s.ValueLabel);
		}
		if (s.MemoryLabel == null)
		{
			s.MemoryLabel = new LabelWidget
			{
				Text = LanguageControl.Get(fName, 0),
				Color = new Color(255, 255, 255, 128),
				IsVisible = false,
				Margin = new Vector2(0f, kLineStep * globalScale)
			};
			s.Panel.Children.Add(s.MemoryLabel);
		}
		s.Panel.IsVisible = visible;
		if (!visible)
		{
			s.MemoryLabel.IsVisible = false;
		}
	}

	private void UpdateHud(ComponentPlayer cp, State s, bool exposed)
	{
		if (s.ValueLabel != null)
		{
			string text;
			Color color;
			if (exposed)
			{
				float num = MathUtils.Clamp(m_weather.CurrentWindStrength, 0f, 16f);
				text = string.Format(LanguageControl.Get(fName, 1), MathUtils.Round(num));
				color = ColorFor(num);
				color.A = byte.MaxValue;
				s.MemoryLabel.IsVisible = false;
			}
			else if (!s.HadExposure)
			{
				text = LanguageControl.Get(fName, 2);
				color = Color.White;
				color.A = byte.MaxValue;
				s.MemoryLabel.IsVisible = false;
			}
			else
			{
				float lastKnownStrength = s.LastKnownStrength;
				text = string.Format(LanguageControl.Get(fName, 1), MathUtils.Round(lastKnownStrength));
				color = ColorFor(lastKnownStrength);
				color.A = 140;
				s.MemoryLabel.IsVisible = true;
				s.MemoryLabel.Color = new Color(255, 255, 255, 128);
			}
			s.ValueLabel.Text = text;
			s.ValueLabel.Color = color;
		}
	}

	private static Color ColorFor(float strength)
	{
		if (strength > 12f)
		{
			return Color.Red;
		}
		if (strength >= 9f)
		{
			return Color.Yellow;
		}
		return Color.White;
	}

	private bool PlayerHasAnemometer(ComponentPlayer cp)
	{
		IInventory inventory = cp.ComponentMiner?.Inventory;
		if (inventory == null)
		{
			return false;
		}
		int num = ((Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true).WorldSettings.GameMode == GameMode.Creative && inventory is ComponentCreativeInventory) ? MathUtils.Min(25, inventory.SlotsCount) : inventory.SlotsCount);
		for (int i = 0; i < num; i++)
		{
			int slotValue = inventory.GetSlotValue(i);
			int slotCount = inventory.GetSlotCount(i);
			if (slotValue != 0 && slotCount > 0 && Terrain.ExtractContents(slotValue) == 430)
			{
				return true;
			}
		}
		return false;
	}

	public SubsystemAnemometerBlockBehavior()
	{
		_states = new Dictionary<string, State>();
	}

	static SubsystemAnemometerBlockBehavior()
	{
		fName = "SubsystemAnemometer";
		kLineStep = 1f;
		kHudPad = 5f;
	}

	private void OnPickableRemoved(Pickable p)
	{
		if (Terrain.ExtractContents(p.Value) == 430)
		{
			_pickableStates.Remove(p.Id);
		}
	}
}
