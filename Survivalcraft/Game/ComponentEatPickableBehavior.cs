using System;
using System.Collections.Generic;
using System.Linq;
using Engine;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentEatPickableBehavior : ComponentBehavior, IUpdateable
{
	public SubsystemTime m_subsystemTime;

	public SubsystemPickables m_subsystemPickables;

	public ComponentCreature m_componentCreature;

	public ComponentPathfinding m_componentPathfinding;

	public Dictionary<Pickable, bool> m_pickables;

	public Random m_random;

	public float[] m_foodFactors;

	public float m_importanceLevel;

	public double m_nextFindPickableTime;

	public double m_nextPickablesUpdateTime;

	public Pickable m_pickable;

	public double m_eatTime;

	public float m_satiation;

	public float m_blockedTime;

	public int m_blockedCount;

	public const float m_range = 16f;

	private bool m_isFeed;

	public bool m_seekBait;

	public bool[] m_seekBaitStrength = new bool[5];

	public FishingFloat m_baitFloat;

	public double m_nextFindBaitTime;

	public SubsystemFishingFloats m_subsystemFishingFloats;

	public static readonly float[] m_baitStrengthRadius;

	public Dictionary<ComponentCreature, bool> m_corpses;

	public ComponentCreature m_targetCorpse;

	public double m_nextFindCorpseTime;

	public double m_nextCorpsesUpdateTime;

	public SubsystemWeather m_subsystemWeather;

	public SubsystemTimeOfDay m_subsystemTimeOfDay;

	public SubsystemSky m_subsystemSky;

	public float Satiation => m_satiation;

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override float ImportanceLevel => m_importanceLevel;

	public bool IsFeed
	{
		get
		{
			return m_isFeed;
		}
		set
		{
			if (m_isFeed != value)
			{
				m_isFeed = value;
				if (CommonLib.WorkType == WorkType.Server)
				{
					CommonLib.Net.QueuePackage(new ComponentBehaviorPackage(this, m_isFeed));
				}
			}
		}
	}

	public void Update(float dt)
	{
		if (CommonLib.WorkType == WorkType.Client)
		{
			m_componentCreature.ComponentCreatureModel.FeedOrder = IsFeed;
			return;
		}
		if (m_satiation > 0f)
		{
			m_satiation = MathUtils.Max(m_satiation - 0.01f * m_subsystemTime.GameTimeDelta, 0f);
		}
		m_stateMachine.Update();
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(throwOnError: true);
		m_subsystemFishingFloats = Project.FindSubsystem<SubsystemFishingFloats>(throwOnError: true);
		m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
		m_subsystemTimeOfDay = Project.FindSubsystem<SubsystemTimeOfDay>(throwOnError: true);
		m_subsystemSky = Project.FindSubsystem<SubsystemSky>(throwOnError: true);
		m_componentCreature = Entity.FindComponent<ComponentCreature>(throwOnError: true);
		m_componentPathfinding = Entity.FindComponent<ComponentPathfinding>(throwOnError: true);
		m_foodFactors = new float[EnumUtils.GetEnumValues(typeof(FoodType)).Max() + 1];
		foreach (KeyValuePair<string, object> vector in valuesDictionary.GetValue<ValuesDictionary>("FoodFactors"))
		{
			FoodType vector2 = (FoodType)Enum.Parse(typeof(FoodType), vector.Key, ignoreCase: false);
			m_foodFactors[(int)vector2] = (float)vector.Value;
		}
		m_seekBait = valuesDictionary.GetValue("SeekBait", defaultValue: false);
		if (valuesDictionary.GetValue<object>("BaitStrengths", null) is ValuesDictionary viewDirection)
		{
			foreach (KeyValuePair<string, object> v in viewDirection)
			{
				if (int.TryParse(v.Key, out var vector3) && vector3 >= 1 && vector3 <= 4 && Convert.ToBoolean(v.Value))
				{
					m_seekBaitStrength[vector3] = true;
				}
			}
		}
		if (m_seekBait && !m_seekBaitStrength[1] && !m_seekBaitStrength[2] && !m_seekBaitStrength[3] && !m_seekBaitStrength[4])
		{
			m_seekBaitStrength[1] = true;
		}
		m_corpses = new Dictionary<ComponentCreature, bool>();
		m_subsystemPickables.PickableAdded += (Pickable p) =>
		{
			if (TryAddPickable(p) && m_pickable == null)
			{
				m_pickable = p;
			}
		};
		m_subsystemPickables.PickableRemoved += (Pickable p) =>
		{
			m_pickables.Remove(p);
			if (m_pickable == p)
			{
				m_pickable = null;
			}
		};
		m_stateMachine.AddState("Inactive", () =>
		{
			m_importanceLevel = 0f;
			m_pickable = null;
			if (m_targetCorpse != null && !IsCorpseValid(m_targetCorpse))
			{
				m_targetCorpse = null;
			}
			if (m_baitFloat != null && !IsBaitFloatValid(m_baitFloat))
			{
				LoseBaitTarget(forceResetImportance: true);
			}
		}, () =>
		{
			if (m_satiation < 1f)
			{
				if (m_pickable == null && m_subsystemTime.GameTime > m_nextFindPickableTime)
				{
					m_nextFindPickableTime = m_subsystemTime.GameTime + (double)m_random.Float(2f, 4f);
					m_pickable = FindPickable(m_componentCreature.ComponentBody.Position);
				}
				if (m_seekBait && m_baitFloat == null && m_subsystemTime.GameTime > m_nextFindBaitTime)
				{
					m_nextFindBaitTime = m_subsystemTime.GameTime + (double)m_random.Float(2f, 4.5f);
					m_baitFloat = FindBaitFloat(m_componentCreature.ComponentBody.Position);
				}
				if (m_baitFloat != null && !IsBaitFloatValid(m_baitFloat))
				{
					LoseBaitTarget(forceResetImportance: false);
				}
				if (m_targetCorpse == null && m_subsystemTime.GameTime > m_nextFindCorpseTime)
				{
					m_nextFindCorpseTime = m_subsystemTime.GameTime + (double)m_random.Float(2f, 4f);
					m_targetCorpse = FindCorpse(m_componentCreature.ComponentBody.Position);
				}
				if (m_pickable != null)
				{
					m_importanceLevel = m_random.Float(5f, 10f);
				}
				else if (m_baitFloat != null)
				{
					m_importanceLevel = m_random.Float(6f, 12f);
				}
				else if (m_targetCorpse != null)
				{
					m_importanceLevel = m_random.Float(7f, 13f);
				}
			}
			if (IsActive)
			{
				m_stateMachine.TransitionTo("Move");
				m_blockedCount = 0;
			}
		}, null);
		m_stateMachine.AddState("Move", () =>
		{
			if (m_pickable != null)
			{
				float speed = ((m_satiation == 0f) ? m_random.Float(0.5f, 0.7f) : 0.5f);
				int maxPathfindingPositions = ((m_satiation == 0f) ? 1000 : 500);
				float num = Vector3.Distance(m_componentCreature.ComponentCreatureModel.EyePosition, m_componentCreature.ComponentBody.Position);
				m_componentPathfinding.SetDestination(m_pickable.Position, speed, 1f + num, maxPathfindingPositions, useRandomMovements: true, ignoreHeightDifference: false, raycastDestination: true, null);
				if (m_random.Float(0f, 1f) < 0.66f)
				{
					m_componentCreature.ComponentCreatureSounds.PlayIdleSound(skipIfRecentlyPlayed: true);
				}
			}
			else if (m_baitFloat != null)
			{
				float speed2 = ((m_satiation == 0f) ? m_random.Float(0.55f, 0.75f) : 0.5f);
				int maxPathfindingPositions2 = ((m_satiation == 0f) ? 900 : 450);
				float num2 = Vector3.Distance(m_componentCreature.ComponentCreatureModel.EyePosition, m_componentCreature.ComponentBody.Position);
				m_componentPathfinding.SetDestination(m_baitFloat.Position, speed2, 1f + num2, maxPathfindingPositions2, useRandomMovements: true, ignoreHeightDifference: false, raycastDestination: true, null);
				if (m_random.Float(0f, 1f) < 0.5f)
				{
					m_componentCreature.ComponentCreatureSounds.PlayIdleSound(skipIfRecentlyPlayed: true);
				}
			}
			else if (m_targetCorpse != null)
			{
				Vector3 corpsePosition = GetCorpsePosition(m_targetCorpse);
				float speed3 = ((m_satiation == 0f) ? m_random.Float(0.5f, 0.75f) : 0.5f);
				int maxPathfindingPositions3 = ((m_satiation == 0f) ? 950 : 450);
				float num3 = Vector3.Distance(m_componentCreature.ComponentCreatureModel.EyePosition, m_componentCreature.ComponentBody.Position);
				m_componentPathfinding.SetDestination(corpsePosition, speed3, 1f + num3, maxPathfindingPositions3, useRandomMovements: true, ignoreHeightDifference: false, raycastDestination: true, null);
				if (m_random.Float(0f, 1f) < 0.5f)
				{
					m_componentCreature.ComponentCreatureSounds.PlayIdleSound(skipIfRecentlyPlayed: true);
				}
			}
		}, () =>
		{
			if (m_pickable != null && !m_pickables.ContainsKey(m_pickable))
			{
				m_pickable = null;
			}
			if (m_baitFloat != null && !IsBaitFloatValid(m_baitFloat))
			{
				LoseBaitTarget(forceResetImportance: false);
				if (m_pickable == null && m_targetCorpse == null)
				{
					m_importanceLevel = 0f;
					m_stateMachine.TransitionTo("Inactive");
					return;
				}
			}
			if (m_targetCorpse != null && !IsCorpseValid(m_targetCorpse))
			{
				m_targetCorpse = null;
				if (m_pickable == null && m_baitFloat == null)
				{
					m_importanceLevel = 0f;
					m_stateMachine.TransitionTo("Inactive");
					return;
				}
			}
			if (m_targetCorpse == null && m_pickable == null && m_baitFloat == null && m_subsystemTime.GameTime > m_nextFindCorpseTime)
			{
				m_nextFindCorpseTime = m_subsystemTime.GameTime + (double)m_random.Float(2f, 4f);
				m_targetCorpse = FindCorpse(m_componentCreature.ComponentBody.Position);
				if (m_targetCorpse != null)
				{
					m_importanceLevel = Math.Max(m_importanceLevel, m_random.Float(7f, 13f));
					Vector3 corpsePosition = GetCorpsePosition(m_targetCorpse);
					float speed = ((m_satiation == 0f) ? m_random.Float(0.5f, 0.75f) : 0.5f);
					int maxPathfindingPositions = ((m_satiation == 0f) ? 950 : 450);
					float num = Vector3.Distance(m_componentCreature.ComponentCreatureModel.EyePosition, m_componentCreature.ComponentBody.Position);
					m_componentPathfinding.SetDestination(corpsePosition, speed, 1f + num, maxPathfindingPositions, useRandomMovements: true, ignoreHeightDifference: false, raycastDestination: true, null);
				}
			}
			if (!IsActive)
			{
				m_stateMachine.TransitionTo("Inactive");
			}
			else
			{
				if (m_pickable == null && m_baitFloat == null && m_targetCorpse == null)
				{
					m_importanceLevel = 0f;
				}
				else if (m_componentPathfinding.IsStuck)
				{
					m_importanceLevel = 0f;
					m_satiation += 0.75f;
				}
				else if (!m_componentPathfinding.Destination.HasValue)
				{
					if (m_pickable != null || m_targetCorpse != null)
					{
						m_stateMachine.TransitionTo("Eat");
					}
					else
					{
						m_importanceLevel = 0f;
					}
				}
				else
				{
					if (m_pickable != null && Vector3.DistanceSquared(m_componentPathfinding.Destination.Value, m_pickable.Position) > 0.0625f)
					{
						m_stateMachine.TransitionTo("PickableMoved");
					}
					if (m_targetCorpse != null)
					{
						Vector3 corpsePosition2 = GetCorpsePosition(m_targetCorpse);
						if (Vector3.DistanceSquared(m_componentPathfinding.Destination.Value, corpsePosition2) > 0.25f)
						{
							m_stateMachine.TransitionTo("PickableMoved");
						}
					}
					if (m_baitFloat != null && Vector3.DistanceSquared(m_componentPathfinding.Destination.Value, m_baitFloat.Position) > 0.25f)
					{
						m_stateMachine.TransitionTo("PickableMoved");
					}
				}
				if (m_random.Float(0f, 1f) < 0.1f * m_subsystemTime.GameTimeDelta)
				{
					m_componentCreature.ComponentCreatureSounds.PlayIdleSound(skipIfRecentlyPlayed: true);
				}
				if (m_pickable != null)
				{
					m_componentCreature.ComponentCreatureModel.LookAtOrder = m_pickable.Position;
				}
				else if (m_baitFloat != null)
				{
					m_componentCreature.ComponentCreatureModel.LookAtOrder = m_baitFloat.Position;
				}
				else if (m_targetCorpse != null)
				{
					m_componentCreature.ComponentCreatureModel.LookAtOrder = GetCorpsePosition(m_targetCorpse);
				}
				else
				{
					m_componentCreature.ComponentCreatureModel.LookRandomOrder = true;
				}
			}
		}, null);
		m_stateMachine.AddState("PickableMoved", null, () =>
		{
			if (m_pickable != null)
			{
				m_componentCreature.ComponentCreatureModel.LookAtOrder = m_pickable.Position;
			}
			else if (m_baitFloat != null)
			{
				m_componentCreature.ComponentCreatureModel.LookAtOrder = m_baitFloat.Position;
			}
			else if (m_targetCorpse != null)
			{
				m_componentCreature.ComponentCreatureModel.LookAtOrder = GetCorpsePosition(m_targetCorpse);
			}
			if (m_subsystemTime.PeriodicGameTimeEvent(0.25, (double)(GetHashCode() % 100) * 0.01))
			{
				m_stateMachine.TransitionTo("Move");
			}
		}, null);
		m_stateMachine.AddState("Eat", () =>
		{
			IsFeed = true;
			m_eatTime = m_random.Float(4f, 5f);
			m_blockedTime = 0f;
		}, () =>
		{
			if (!IsActive)
			{
				m_stateMachine.TransitionTo("Inactive");
			}
			if (m_pickable != null)
			{
				if (Vector3.DistanceSquared(new Vector3(m_componentCreature.ComponentCreatureModel.EyePosition.X, m_componentCreature.ComponentBody.Position.Y, m_componentCreature.ComponentCreatureModel.EyePosition.Z), m_pickable.Position) < 0.64000005f)
				{
					m_eatTime -= m_subsystemTime.GameTimeDelta;
					m_blockedTime = 0f;
					if (m_eatTime <= 0.0)
					{
						m_satiation++;
						m_pickable.Count = MathUtils.Max(m_pickable.Count - 1, 0);
						if (m_pickable.Count == 0)
						{
							m_pickable.ToRemove = true;
							m_importanceLevel = 0f;
						}
						else if (m_random.Float(0f, 1f) < 0.5f)
						{
							m_importanceLevel = 0f;
						}
						ComponentHealth componentHealth = m_componentCreature.Entity.FindComponent<ComponentHealth>();
						componentHealth?.Heal(2f * componentHealth.Health);
					}
				}
				else
				{
					float num = Vector3.Distance(m_componentCreature.ComponentCreatureModel.EyePosition, m_componentCreature.ComponentBody.Position);
					m_componentPathfinding.SetDestination(m_pickable.Position, 0.3f, 0.5f + num, 0, useRandomMovements: false, ignoreHeightDifference: true, raycastDestination: false, null);
					m_blockedTime += m_subsystemTime.GameTimeDelta;
				}
			}
			else if (m_targetCorpse != null)
			{
				if (!IsCorpseValid(m_targetCorpse))
				{
					m_targetCorpse = null;
					m_stateMachine.TransitionTo("Inactive");
				}
				else
				{
					Vector3 corpsePosition = GetCorpsePosition(m_targetCorpse);
					if (Vector3.DistanceSquared(new Vector3(m_componentCreature.ComponentCreatureModel.EyePosition.X, m_componentCreature.ComponentBody.Position.Y, m_componentCreature.ComponentCreatureModel.EyePosition.Z), corpsePosition) < 0.64000005f)
					{
						m_eatTime -= m_subsystemTime.GameTimeDelta;
						m_blockedTime = 0f;
						if (m_eatTime <= 0.0)
						{
							m_satiation++;
							ComponentCreatureModel componentCreatureModel = m_targetCorpse.ComponentCreatureModel;
							if (componentCreatureModel != null && componentCreatureModel.CutNextDetachablePart())
							{
								ComponentHealth componentHealth2 = m_componentCreature.Entity.FindComponent<ComponentHealth>();
								componentHealth2?.Heal(2f * componentHealth2.Health);
								SubsystemGameInfo subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
								bool flag = true;
								if (subsystemGameInfo != null)
								{
									ComponentHealth componentHealth3 = m_targetCorpse.ComponentHealth;
									if (componentHealth3 != null && componentHealth3.DeathTime.HasValue && subsystemGameInfo.TotalElapsedGameTime - componentHealth3.DeathTime.Value > 1000.0)
									{
										flag = false;
									}
								}
								if (flag)
								{
									int dropletsCount = m_random.Int(3, 7);
									SubsystemParticles subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
									SubsystemTerrain subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
									float num2 = m_random.Float(-1f, 1f);
									float num3 = m_random.Float(-1f, 1f);
									if (num2 * num2 + num3 * num3 < 0.0001f)
									{
										num2 = 1f;
										num3 = 0f;
									}
									Vector3 direction = Vector3.Normalize(new Vector3(num2, 0f, num3));
									subsystemParticles.AddParticleSystem(new BloodDropletParticleSystem(subsystemTerrain, corpsePosition, direction, dropletsCount, 180f, 3f, 6f, -9.81f));
								}
							}
							m_importanceLevel = 0f;
							m_targetCorpse = null;
							m_stateMachine.TransitionTo("Inactive");
						}
					}
					else
					{
						float num4 = Vector3.Distance(m_componentCreature.ComponentCreatureModel.EyePosition, m_componentCreature.ComponentBody.Position);
						m_componentPathfinding.SetDestination(corpsePosition, 0.3f, 0.5f + num4, 0, useRandomMovements: false, ignoreHeightDifference: true, raycastDestination: false, null);
						m_blockedTime += m_subsystemTime.GameTimeDelta;
					}
				}
			}
			else
			{
				m_stateMachine.TransitionTo("Inactive");
			}
			if (m_blockedTime > 3f)
			{
				m_blockedCount++;
				if (m_blockedCount >= 3)
				{
					m_importanceLevel = 0f;
					m_satiation += 0.75f;
				}
				else
				{
					m_stateMachine.TransitionTo("Move");
				}
			}
			m_componentCreature.ComponentCreatureModel.FeedOrder = true;
			if (m_random.Float(0f, 1f) < 0.1f * m_subsystemTime.GameTimeDelta)
			{
				m_componentCreature.ComponentCreatureSounds.PlayIdleSound(skipIfRecentlyPlayed: true);
			}
			if (m_random.Float(0f, 1f) < 1.5f * m_subsystemTime.GameTimeDelta)
			{
				m_componentCreature.ComponentCreatureSounds.PlayFootstepSound(2f);
			}
		}, () =>
		{
			IsFeed = false;
		});
		m_stateMachine.TransitionTo("Inactive");
		m_pickables = new Dictionary<Pickable, bool>();
		m_random = new Random();
	}

	public float GetFoodFactor(FoodType foodType)
	{
		return m_foodFactors[(int)foodType];
	}

	public Pickable FindPickable(Vector3 position)
	{
		if (m_subsystemTime.GameTime > m_nextPickablesUpdateTime)
		{
			m_nextPickablesUpdateTime = m_subsystemTime.GameTime + (double)m_random.Float(2f, 4f);
			m_pickables.Clear();
			foreach (Pickable pickable in m_subsystemPickables.Pickables)
			{
				TryAddPickable(pickable);
			}
			if (m_pickable != null && !m_pickables.ContainsKey(m_pickable))
			{
				m_pickable = null;
			}
		}
		foreach (Pickable key in m_pickables.Keys)
		{
			float num = Vector3.DistanceSquared(position, key.Position);
			if (m_random.Float(0f, 1f) > num / 256f)
			{
				return key;
			}
		}
		return null;
	}

	public bool TryAddPickable(Pickable pickable)
	{
		Block x = BlocksManager.Blocks[Terrain.ExtractContents(pickable.Value)];
		if (m_foodFactors[(int)x.FoodType] <= 0f)
		{
			return false;
		}
		if (Vector3.DistanceSquared(pickable.Position, m_componentCreature.ComponentBody.Position) >= 256f)
		{
			return false;
		}
		if (IsFishCreature())
		{
			if (!IsInWaterBlock18(pickable.Position))
			{
				return false;
			}
			if (m_subsystemWeather != null && m_subsystemWeather.CurrentWindStrength >= 13f)
			{
				return false;
			}
			float y = MathUtils.Saturate(0.1f * GetFishingResponseMultiplier(m_componentCreature.ComponentBody.Position));
			if (m_random.Float(0f, 1f) >= y)
			{
				return false;
			}
		}
		m_pickables.Add(pickable, value: true);
		return true;
	}

	public ComponentEatPickableBehavior()
	{
		m_pickables = new Dictionary<Pickable, bool>();
		m_random = new Random();
	}

	public bool IsBaitFloatValid(FishingFloat f)
	{
		if (f == null)
		{
			return false;
		}
		if (m_subsystemFishingFloats == null)
		{
			return false;
		}
		if (!m_subsystemFishingFloats.m_floats.Contains(f))
		{
			return false;
		}
		if (f.BobberValue == 0)
		{
			return false;
		}
		if (IsFishCreature() && !IsInWaterBlock18(f.Position))
		{
			return false;
		}
		int baitStrengthFromBobberValue = GetBaitStrengthFromBobberValue(f.BobberValue);
		if (baitStrengthFromBobberValue < 1 || baitStrengthFromBobberValue > 4)
		{
			return false;
		}
		if (!m_seekBaitStrength[baitStrengthFromBobberValue])
		{
			return false;
		}
		if (m_subsystemWeather != null && m_subsystemWeather.CurrentWindStrength >= 13f)
		{
			return false;
		}
		float value = m_baitStrengthRadius[baitStrengthFromBobberValue];
		return Vector3.DistanceSquared(m_componentCreature.ComponentBody.Position, f.Position) <= value * value * 1.2f;
	}

	public FishingFloat FindBaitFloat(Vector3 pos)
	{
		if (!m_seekBait || m_subsystemFishingFloats == null)
		{
			return null;
		}
		float fishingResponseMultiplier = GetFishingResponseMultiplier(pos);
		float num = MathUtils.Saturate(0.1f * fishingResponseMultiplier);
		FishingFloat fishingFloat = null;
		float num2 = float.MaxValue;
		List<FishingFloat> floats = m_subsystemFishingFloats.m_floats;
		for (int i = 0; i < floats.Count; i++)
		{
			FishingFloat fishingFloat2 = floats[i];
			if (fishingFloat2.BobberValue == 0 || (IsFishCreature() && !IsInWaterBlock18(fishingFloat2.Position)))
			{
				continue;
			}
			int baitStrengthFromBobberValue = GetBaitStrengthFromBobberValue(fishingFloat2.BobberValue);
			if (baitStrengthFromBobberValue < 1 || baitStrengthFromBobberValue > 4 || !m_seekBaitStrength[baitStrengthFromBobberValue])
			{
				continue;
			}
			float num3 = m_baitStrengthRadius[baitStrengthFromBobberValue];
			float num4 = Vector3.DistanceSquared(pos, fishingFloat2.Position);
			if (num4 <= num3 * num3)
			{
				float num5 = 1f + 0.15f * (float)(4 - baitStrengthFromBobberValue);
				float num6 = num4 * num5;
				if (num6 < num2)
				{
					num2 = num6;
					fishingFloat = fishingFloat2;
				}
			}
		}
		if (fishingFloat != null)
		{
			if (m_componentCreature != null && m_componentCreature.Category == CreatureCategory.WaterPredator)
			{
				num = MathUtils.Saturate(num + 0.1f);
			}
			if (m_random.Float(0f, 1f) < num)
			{
				return fishingFloat;
			}
		}
		return null;
	}

	static ComponentEatPickableBehavior()
	{
		m_baitStrengthRadius = new float[5] { 0f, 16f, 32f, 64f, 160f };
	}

	private void LoseBaitTarget(bool forceResetImportance)
	{
		m_baitFloat = null;
		m_nextFindBaitTime = m_subsystemTime.GameTime - 0.001;
		if (forceResetImportance)
		{
			m_importanceLevel = 0f;
		}
		if (m_stateMachine.CurrentState != "Inactive")
		{
			m_stateMachine.TransitionTo("Inactive");
		}
	}

	public ComponentCreature FindCorpse(Vector3 position)
	{
		bool flag = m_foodFactors[1] > 0f;
		bool flag2 = m_foodFactors[2] > 0f;
		if (!flag && !flag2)
		{
			return null;
		}
		if (m_subsystemTime.GameTime > m_nextCorpsesUpdateTime)
		{
			m_nextCorpsesUpdateTime = m_subsystemTime.GameTime + (double)m_random.Float(2f, 4f);
			m_corpses.Clear();
			foreach (Entity entity in Project.Entities)
			{
				ComponentCreature componentCreature = entity.FindComponent<ComponentCreature>();
				if (componentCreature != null)
				{
					TryAddCorpse(componentCreature);
				}
			}
			if (m_targetCorpse != null && !IsCorpseValid(m_targetCorpse))
			{
				m_targetCorpse = null;
			}
		}
		foreach (ComponentCreature key in m_corpses.Keys)
		{
			float num = Vector3.DistanceSquared(position, GetCorpsePosition(key));
			if (m_random.Float(0f, 1f) > num / 256f)
			{
				return key;
			}
		}
		return null;
	}

	public bool TryAddCorpse(ComponentCreature c)
	{
		if (c == null || c == m_componentCreature)
		{
			return false;
		}
		ComponentHealth componentHealth = c.ComponentHealth;
		if (componentHealth == null || componentHealth.Health > 0f)
		{
			return false;
		}
		if (!IsCorpseEdible(c))
		{
			return false;
		}
		if (Vector3.DistanceSquared(GetCorpsePosition(c), m_componentCreature.ComponentBody.Position) >= 256f)
		{
			return false;
		}
		ComponentCreatureModel componentCreatureModel = c.ComponentCreatureModel;
		if (componentCreatureModel == null)
		{
			return false;
		}
		componentCreatureModel.BuildDetachablePartsIfNeeded();
		if (componentCreatureModel.m_detachables == null || componentCreatureModel.m_detachables.Count == 0)
		{
			return false;
		}
		bool flag = false;
		for (int i = 0; i < componentCreatureModel.m_detachables.Count; i++)
		{
			if (!componentCreatureModel.m_detachables[i].Hidden)
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			return false;
		}
		m_corpses[c] = true;
		return true;
	}

	private bool IsCorpseValid(ComponentCreature c)
	{
		if (c == null)
		{
			return false;
		}
		ComponentHealth componentHealth = c.ComponentHealth;
		if (componentHealth == null || componentHealth.Health > 0f)
		{
			return false;
		}
		ComponentCreatureModel componentCreatureModel = c.ComponentCreatureModel;
		if (componentCreatureModel == null)
		{
			return false;
		}
		componentCreatureModel.BuildDetachablePartsIfNeeded();
		if (componentCreatureModel.m_detachables == null || componentCreatureModel.m_detachables.Count == 0)
		{
			return false;
		}
		for (int i = 0; i < componentCreatureModel.m_detachables.Count; i++)
		{
			if (!componentCreatureModel.m_detachables[i].Hidden)
			{
				return true;
			}
		}
		return false;
	}

	private bool IsCorpseEdible(ComponentCreature corpse)
	{
		string name = m_componentCreature.Entity.ValuesDictionary.DatabaseObject.Name;
		string name2 = corpse.Entity.ValuesDictionary.DatabaseObject.Name;
		if (string.Equals(name, name2, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		bool flag = m_foodFactors[1] > 0f;
		bool flag2 = m_foodFactors[2] > 0f;
		if (!flag && !flag2)
		{
			return false;
		}
		CreatureCategory category = corpse.Category;
		bool flag3 = category == CreatureCategory.WaterPredator || category == CreatureCategory.WaterOther;
		if (!(flag2 & flag3) && (!flag || flag3))
		{
			return flag & flag2;
		}
		return true;
	}

	private Vector3 GetCorpsePosition(ComponentCreature c)
	{
		BoundingBox boundingBox = c.ComponentBody.BoundingBox;
		return (boundingBox.Min + boundingBox.Max) * 0.5f;
	}

	private float GetFishingResponseMultiplier(Vector3 pos)
	{
		if (m_subsystemWeather == null)
		{
			return 1f;
		}
		float timeOfDayMultiplier = GetTimeOfDayMultiplier();
		float weatherMultiplier = GetWeatherMultiplier(pos);
		float windMultiplier = GetWindMultiplier();
		if (windMultiplier <= 0f)
		{
			return 0f;
		}
		float f = MathUtils.Saturate(1f - m_satiation);
		float num = MathUtils.Lerp(0.7f, 1.1f, f);
		return MathUtils.Clamp(timeOfDayMultiplier * weatherMultiplier * windMultiplier * num, 0f, 8f);
	}

	private float GetTimeOfDayMultiplier()
	{
		if (m_subsystemTimeOfDay == null)
		{
			return 1f;
		}
		float timeOfDay = m_subsystemTimeOfDay.TimeOfDay;
		float sunriseOffset = m_subsystemTimeOfDay.SunriseOffset;
		float sunsetOffset = m_subsystemTimeOfDay.SunsetOffset;
		float num = sunriseOffset + 0.05f;
		float num2 = sunsetOffset - 0.05f;
		if (MathUtils.Abs(timeOfDay - sunriseOffset) <= 0.07f)
		{
			return 4f;
		}
		if (MathUtils.Abs(timeOfDay - sunsetOffset) <= 0.07f)
		{
			return 3f;
		}
		if (timeOfDay >= num && timeOfDay <= num2)
		{
			return 0.4f;
		}
		float num3 = ((m_componentCreature != null && m_componentCreature.Category == CreatureCategory.WaterPredator) ? 4f : 0.6f);
		if (m_subsystemSky != null)
		{
			float f;
			switch (m_subsystemSky.MoonPhase)
			{
			case 0:
				f = 1f;
				break;
			case 1:
			case 7:
				f = 0.8f;
				break;
			case 2:
			case 6:
				f = 0.6f;
				break;
			case 3:
			case 5:
				f = 0.4f;
				break;
			default:
				f = 0f;
				break;
			}
			float x = 4f;
			return MathUtils.Lerp(num3, x, f);
		}
		return num3;
	}

	private float GetWeatherMultiplier(Vector3 pos)
	{
		if (m_subsystemWeather == null)
		{
			return 1f;
		}
		int x = Terrain.ToCell(pos.X);
		int z = Terrain.ToCell(pos.Z);
		float num = MathUtils.Saturate(m_subsystemWeather.GetPrecipitationShaftInfo(x, z).Intensity);
		if (m_subsystemWeather.m_lightningIntensity >= 0.8f)
		{
			return 0.8f;
		}
		if (num > 0f && num < 0.3f)
		{
			return 4f;
		}
		if (num >= 0.3f && num < 0.7f)
		{
			return 2.4f;
		}
		if (num >= 0.7f)
		{
			return 1.6f;
		}
		return 2f;
	}

	private float GetWindMultiplier()
	{
		if (m_subsystemWeather == null)
		{
			return 1f;
		}
		float currentWindStrength = m_subsystemWeather.CurrentWindStrength;
		if (currentWindStrength <= 4f)
		{
			return 2f;
		}
		if (currentWindStrength <= 8f)
		{
			return 1.4f;
		}
		if (currentWindStrength <= 12f)
		{
			return 0.6f;
		}
		return 0f;
	}

	private bool IsFishCreature()
	{
		if (m_componentCreature != null)
		{
			if (m_componentCreature.Category != CreatureCategory.WaterPredator)
			{
				return m_componentCreature.Category == CreatureCategory.WaterOther;
			}
			return true;
		}
		return false;
	}

	private bool IsInWaterBlock18(Vector3 position)
	{
		SubsystemTerrain subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		if (subsystemTerrain == null)
		{
			return false;
		}
		int x = Terrain.ToCell(position.X);
		int num = Terrain.ToCell(position.Y);
		int z = Terrain.ToCell(position.Z);
		Terrain terrain = subsystemTerrain.Terrain;
		if (Terrain.ExtractContents(terrain.GetCellValueFast(x, num, z)) != 18)
		{
			if (num > 0)
			{
				return Terrain.ExtractContents(terrain.GetCellValueFast(x, num - 1, z)) == 18;
			}
			return false;
		}
		return true;
	}

	private int GetBaitStrengthFromBobberValue(int bobberValue)
	{
		int data = Terrain.ExtractData(bobberValue);
		int bait = BobberBlock.GetBait(data);
		if (bait == 0)
		{
			return 0;
		}
		return BobberBaitInfo.GetBaitStrengthByValue(Terrain.MakeBlockValue(bait, 0, data));
	}
}
