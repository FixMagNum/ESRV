using System;
using System.Collections.Generic;
using System.Linq;
using Engine;
using Engine.Audio;
using Engine.Graphics;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemFishingFloats : Subsystem, IUpdateable, IDrawable
{
	public List<FishingFloat> m_floats;

	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemParticles m_subsystemParticles;

	public SubsystemAudio m_subsystemAudio;

	public SubsystemTime m_subsystemTime;

	public SubsystemSky m_subsystemSky;

	public PrimitivesRenderer3D m_primitivesRenderer;

	public DrawBlockEnvironmentData m_drawBlockEnvironmentData;

	public SubsystemOceanScanner m_oceanScanner;

	public Random m_random;

	public SubsystemBodies m_subsystemBodies;

	public float m_reelMinSpeed;

	public float m_reelMaxSpeed;

	public float m_reelCatchDistance;

	public float LightMassKg;

	public float HeavyMassKg;

	public float MinSpeedScaleHeavy;

	public float AbortRatePerSec;

	public float AttemptResistReduction;

	public float StuckEffectPeriod;

	public float ReelGravity;

	public float ReelJitter;

	public float PullAccelMax;

	private Dictionary<FishingFloat, bool> m_fishAligned;

	private Dictionary<FishingFloat, int> m_fishMistakes;

	private Dictionary<FishingFloat, Vector2> m_mgOffset;

	public float MG_RadiusMin;

	public float MG_RadiusMax;

	public float MG_SpeedMin;

	public float MG_SpeedMax;

	public float MG_SpeedResistPow;

	public float MG_SuccessFactorSmall;

	public float MG_SuccessFactorHeavy;

	public float MG_SuccessAbsMin;

	public float MG_ColorExponent;

	private Dictionary<FishingFloat, float> m_targetAngle;

	private Dictionary<FishingFloat, float> m_targetSpeed;

	public float AreaRadiusMin;

	public float AreaRadiusMax;

	public float GreenFracMin;

	public float GreenFracMax;

	public float YellowFracMin;

	public float YellowFracMax;

	public float TargetSpeedMin;

	public float TargetSpeedMax;

	public float MidMassThreshold;

	public float NearImpossibleMass;

	public float ImpossibleMass;

	public float MaxResistMass;

	public float AreaBlocksSmall;

	public float AreaBlocksMidMax;

	public float AreaBlocksHeavy;

	public float AreaBlocksNearImpossible;

	public float GreenSizeFracSmall;

	public float GreenSizeFracHeavy;

	public float YellowOuterFracSmall;

	public float YellowOuterFracHeavy;

	public float GreenAngleMin;

	public float TargetOrbitSpeedMin;

	public float TargetOrbitSpeedMax;

	public float TargetRadJitter;

	public float TargetRadReseedMin;

	public float TargetRadReseedMax;

	private Dictionary<FishingFloat, float> m_targetAngularSpeed;

	private Dictionary<FishingFloat, float> m_targetInnerRad;

	private Dictionary<FishingFloat, float> m_targetNextRadReseed;

	public float AreaScale;

	public float ReelImpulseAddMin;

	public float ReelImpulseAddMax;

	public float ReelImpulseMaxTime;

	public float ReelImpulseDrainMin;

	public float ReelImpulseDrainMax;

	public float MaxLineLength;

	public float PlayerMassKg;

	public float RopeSnapStrength;

	public float PivotFishMassKg;

	public float VisualTensionImpulseDuration;

	public float VisualTensionRelaxDuration;

	public float VisualRelaxPerFreeBlock;

	public float VisualSagMax;

	private Dictionary<FishingFloat, double> m_ropeImpulseUntil;

	private Dictionary<FishingFloat, double> m_ropeRelaxUntil;

	public SubsystemWeather m_subsystemWeather;

	private Dictionary<FishingFloat, float> m_ropeTensionSmoothed;

	private Dictionary<FishingFloat, double> m_baitDisappearAt;

	private Dictionary<FishingFloat, double> m_surpriseUntil;

	private Dictionary<FishingFloat, double> m_detachAt;

	private readonly Dictionary<ComponentPlayer, int> m_rodDamageAttemptCounter;

	private readonly Dictionary<ComponentPlayer, int> m_rodDamageThreshold;

	private readonly Dictionary<FishingFloat, int> m_redClicks;

	private readonly Dictionary<FishingFloat, Sound> m_sFastUnwind = new Dictionary<FishingFloat, Sound>();

	private readonly Dictionary<FishingFloat, Sound> m_sFastRewind = new Dictionary<FishingFloat, Sound>();

	private readonly Dictionary<FishingFloat, Sound> m_sUnwind = new Dictionary<FishingFloat, Sound>();

	private readonly Dictionary<FishingFloat, Sound> m_sRewind = new Dictionary<FishingFloat, Sound>();

	private readonly Dictionary<FishingFloat, Sound> m_sCreakHeavy = new Dictionary<FishingFloat, Sound>();

	private readonly Dictionary<FishingFloat, float> m_lastBlockDist = new Dictionary<FishingFloat, float>();

	public SubsystemFocalorsFluidBlockBehavior m_subsystemFluidBlockBehavior;

	public int[] DrawOrders => new int[1] { 10 };

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public event Action<FishingFloat> FloatAdded;

	public event Action<FishingFloat> FloatRemoved;

	public void AddFloat(Vector3 position, Vector3 velocity, Vector3 angularVelocity, ComponentCreature owner)
	{
		if (CommonLib.WorkType != WorkType.Client)
		{
			FishingFloat f = AddFloatNet(position, velocity, angularVelocity, owner, 0);
			CommonLib.Net.QueuePackage(new FishingFloatPackage(FishingFloatPackage.FishingFloatAction.Cast, f));
		}
	}

	public void Update(float dt)
	{
		float pullAccelMax = PullAccelMax;
		int listIndex = m_floats.Count - 1;
		while (listIndex >= 0)
		{
			FishingFloat fishingFloat = m_floats[listIndex];
			ComponentCreature owner = fishingFloat.Owner;
			Vector3 vector = Vector3.Zero;
			if (owner != null)
			{
				vector = owner.ComponentCreatureModel.EyePosition + owner.ComponentBody.Matrix.Right * 0.5f + owner.ComponentBody.Matrix.Up * 0.35f;
				bool flag = false;
				ComponentHumanModel componentHumanModel = owner.Entity.FindComponent<ComponentHumanModel>();
				if (componentHumanModel != null && componentHumanModel.OffhandRodValue != 0 && Terrain.ExtractContents(componentHumanModel.OffhandRodValue) == 395)
				{
					flag = true;
				}
				if (!flag)
				{
					ComponentMiner componentMiner = owner.Entity.FindComponent<ComponentMiner>();
					if (componentMiner != null && componentMiner.Inventory != null)
					{
						int slotsCount = componentMiner.Inventory.SlotsCount;
						for (int i = 0; i < slotsCount; i++)
						{
							if (flag)
							{
								break;
							}
							if (Terrain.ExtractContents(componentMiner.Inventory.GetSlotValue(i)) == 395)
							{
								flag = true;
							}
						}
					}
				}
				if (!flag)
				{
					if (fishingFloat.StuckBody != null)
					{
						fishingFloat.StuckBody = null;
						ClearMiniGame(fishingFloat);
						ResetPlayerSightColor(owner);
						if (CommonLib.WorkType != WorkType.Client)
						{
							CommonLib.Net.QueuePackage(new FishingFloatPackage(FishingFloatPackage.FishingFloatAction.Detach, fishingFloat));
						}
					}
					if (CommonLib.WorkType != WorkType.Client)
					{
						CommonLib.Net.QueuePackage(new FishingFloatPackage(FishingFloatPackage.FishingFloatAction.Remove, fishingFloat));
					}
					RemoveAndReturn(listIndex, fishingFloat, owner);
					listIndex--;
					continue;
				}
			}
			if (fishingFloat.StuckBody != null)
			{
				TickBaitTimer(fishingFloat);
				if (TryAutoDetachIfExpired(fishingFloat, owner))
				{
					listIndex--;
					continue;
				}
			}
			if (owner != null && Vector3.DistanceSquared((fishingFloat.StuckBody != null) ? fishingFloat.StuckBody.Position : fishingFloat.Position, vector) < m_reelCatchDistance * m_reelCatchDistance && (fishingFloat.StuckBody != null || fishingFloat.IsRetracting))
			{
				fishingFloat.StuckBody = null;
				if (CommonLib.WorkType != WorkType.Client)
				{
					CommonLib.Net.QueuePackage(new FishingFloatPackage(FishingFloatPackage.FishingFloatAction.Detach, fishingFloat));
				}
				RemoveAndReturn(listIndex, fishingFloat, owner);
				listIndex--;
				continue;
			}
			if (fishingFloat.IsRetracting)
			{
				if (owner == null)
				{
					if (CommonLib.WorkType != WorkType.Client)
					{
						CommonLib.Net.QueuePackage(new FishingFloatPackage(FishingFloatPackage.FishingFloatAction.Remove, fishingFloat));
					}
					RemoveAndReturn(listIndex, fishingFloat, null);
					listIndex--;
					continue;
				}
				if (fishingFloat.StuckBody == null)
				{
					UpdateRetractingNoFish(fishingFloat, vector, dt);
					listIndex--;
					continue;
				}
				if (m_subsystemBodies.Bodies.Contains(fishingFloat.StuckBody))
				{
					UpdateRetractingFish(fishingFloat, fishingFloat.StuckBody, owner, vector, pullAccelMax, dt, ref listIndex);
					ApplyRopeConstraint(fishingFloat, fishingFloat.StuckBody, owner, vector, dt);
					listIndex--;
					continue;
				}
				fishingFloat.StuckBody = null;
				ClearMiniGame(fishingFloat);
				ResetPlayerSightColor(owner);
				if (CommonLib.WorkType != WorkType.Client)
				{
					CommonLib.Net.QueuePackage(new FishingFloatPackage(FishingFloatPackage.FishingFloatAction.Detach, fishingFloat));
				}
				listIndex--;
				continue;
			}
			if (fishingFloat.StuckBody != null && !m_subsystemBodies.Bodies.Contains(fishingFloat.StuckBody))
			{
				fishingFloat.StuckBody = null;
				ClearMiniGame(fishingFloat);
				ResetPlayerSightColor(owner);
				if (CommonLib.WorkType != WorkType.Client)
				{
					CommonLib.Net.QueuePackage(new FishingFloatPackage(FishingFloatPackage.FishingFloatAction.Detach, fishingFloat));
				}
			}
			if (fishingFloat.StuckBody == null)
			{
				UpdateFreeFloatPhysics(fishingFloat, dt);
				listIndex--;
				continue;
			}
			Vector3 position = fishingFloat.StuckBody.Matrix.Translation + fishingFloat.BodyOffset;
			if (fishingFloat.StuckBody.Entity != null)
			{
				ComponentCreatureModel componentCreatureModel = fishingFloat.StuckBody.Entity.FindComponent<ComponentCreatureModel>();
				if (componentCreatureModel != null)
				{
					position = componentCreatureModel.EyePosition;
				}
			}
			fishingFloat.Position = position;
			fishingFloat.Velocity = fishingFloat.StuckBody.Velocity;
			UpdateFishFight(fishingFloat, fishingFloat.StuckBody, owner, dt);
			ApplyRopeConstraint(fishingFloat, fishingFloat.StuckBody, owner, vector, dt);
			if (IsWater(fishingFloat.Position))
			{
				fishingFloat.EffectTimer -= dt;
				if (fishingFloat.EffectTimer <= 0f)
				{
					fishingFloat.EffectTimer = StuckEffectPeriod + m_random.Float(-0.25f, 0.25f);
					m_subsystemParticles.AddParticleSystem(new WaterSplashParticleSystem(m_subsystemTerrain, fishingFloat.Position, large: false));
					m_subsystemAudio.PlayRandomSound("Audio/Splashes", 0.4f, m_random.Float(-0.3f, 0.3f), fishingFloat.Position, 4f, autoDelay: true);
				}
			}
			listIndex--;
		}
	}

	public void Draw(Camera camera, int drawOrder)
	{
		m_drawBlockEnvironmentData.SubsystemTerrain = m_subsystemTerrain;
		m_drawBlockEnvironmentData.InWorldMatrix = Matrix.Identity;
		float num = MathUtils.Sqr(m_subsystemSky.VisibilityRange);
		FlatBatch3D flatBatch3D = m_primitivesRenderer.FlatBatch(0, DepthStencilState.Default);
		foreach (FishingFloat @float in m_floats)
		{
			Vector3 position = @float.Position;
			if (Vector3.DistanceSquared(camera.ViewPosition, position) < num && camera.ViewFrustum.Intersection(position))
			{
				int x = Terrain.ToCell(position.X);
				int y = Terrain.ToCell(position.Y);
				int z = Terrain.ToCell(position.Z);
				m_drawBlockEnvironmentData.Humidity = m_subsystemTerrain.Terrain.GetSeasonalHumidity(x, z);
				m_drawBlockEnvironmentData.Temperature = m_subsystemTerrain.Terrain.GetSeasonalTemperature(x, z) + SubsystemWeather.GetTemperatureAdjustmentAtHeight(y);
				m_drawBlockEnvironmentData.Light = m_subsystemTerrain.Terrain.GetCellLightFast(x, y, z);
				m_drawBlockEnvironmentData.BillboardDirection = camera.ViewDirection;
				m_drawBlockEnvironmentData.InWorldMatrix.Translation = position;
				Matrix matrix = Matrix.CreateFromAxisAngle(Vector3.Normalize((@float.Rotation.LengthSquared() > 0f) ? @float.Rotation : Vector3.UnitY), @float.Rotation.Length());
				matrix.Translation = position;
				int value = ((@float.BobberValue != 0) ? @float.BobberValue : Terrain.MakeBlockValue(396, 0, 0));
				BlocksManager.Blocks[Terrain.ExtractContents(value)].DrawBlock(m_primitivesRenderer, value, Color.White, 0.3f, ref matrix, m_drawBlockEnvironmentData);
			}
			if (@float.Owner == null)
			{
				continue;
			}
			ComponentCreature owner = @float.Owner;
			Vector3 vector = owner.ComponentCreatureModel.EyePosition;
			ComponentHumanModel componentHumanModel = owner.Entity.FindComponent<ComponentHumanModel>();
			if (componentHumanModel != null && componentHumanModel.OffhandRodValue != 0 && Terrain.ExtractContents(componentHumanModel.OffhandRodValue) == 395)
			{
				vector = GetRodTipLeft(owner, Terrain.ExtractData(componentHumanModel.OffhandRodValue), camera);
			}
			else
			{
				ComponentMiner componentMiner = owner.Entity.FindComponent<ComponentMiner>();
				if (componentMiner != null && componentMiner.Inventory != null)
				{
					int activeSlotIndex = componentMiner.Inventory.ActiveSlotIndex;
					if (activeSlotIndex >= 0)
					{
						int slotValue = componentMiner.Inventory.GetSlotValue(activeSlotIndex);
						if (Terrain.ExtractContents(slotValue) == 395)
						{
							vector = GetRodTip(owner, Terrain.ExtractData(slotValue), camera);
						}
					}
				}
			}
			Vector3 vector2 = vector;
			Vector3 position2 = @float.Position;
			float num2 = Vector3.Distance(vector2, position2);
			float num3 = MathUtils.Max(MaxLineLength - num2, 0f);
			float x2 = ((num2 >= MaxLineLength) ? 1f : MathUtils.Saturate(1f - num3 * VisualRelaxPerFreeBlock));
			double gameTime = m_subsystemTime.GameTime;
			double value2 = 0.0;
			double value3 = 0.0;
			m_ropeImpulseUntil.TryGetValue(@float, out value2);
			m_ropeRelaxUntil.TryGetValue(@float, out value3);
			float x3 = 0f;
			if (gameTime < value2)
			{
				x3 = 1f;
			}
			else if (gameTime < value3 && value3 > value2)
			{
				x3 = MathUtils.Saturate((float)((value3 - gameTime) / Math.Max(1E-06, value3 - value2)));
			}
			float num4 = MathUtils.Clamp(MathUtils.Max(x2, x3), 0f, 1f);
			if (!m_ropeTensionSmoothed.TryGetValue(@float, out var value4))
			{
				value4 = num4;
			}
			float num5 = MathUtils.Lerp(value4, num4, 0.2f);
			m_ropeTensionSmoothed[@float] = num5;
			float num6 = MathUtils.Lerp(MathUtils.Min(num2 * 0.3f, VisualSagMax), 0f, num5);
			if (owner.ComponentBody.IsSneaking)
			{
				vector2 += new Vector3(0f, (0f - num6) * 0.2f, 0f);
			}
			Vector3 vector3 = (vector2 + position2) * 0.5f + Vector3.UnitY * (0f - num6);
			if (m_subsystemWeather != null)
			{
				float currentWindStrength = m_subsystemWeather.CurrentWindStrength;
				if (currentWindStrength >= 6f)
				{
					Vector2 currentWindVector = m_subsystemWeather.CurrentWindVector;
					Vector3 vector4 = Vector3.Normalize(new Vector3(currentWindVector.X, 0f, currentWindVector.Y));
					float windExposureFactor = m_subsystemWeather.GetWindExposureFactor((vector2 + position2) * 0.5f);
					if (windExposureFactor > 0f)
					{
						float x4 = MathUtils.Lerp(0f, 0.35f, MathUtils.Saturate((currentWindStrength - 6f) / 4f)) + MathUtils.Lerp(0f, 0.55f, MathUtils.Saturate((currentWindStrength - 10f) / 6f));
						x4 = MathUtils.Clamp(x4, 0f, 0.9f);
						float num7 = (float)gameTime;
						float f = MathUtils.Saturate((currentWindStrength - 6f) / 10f);
						float num8 = (float)((double)((float)(@float.GetHashCode() & 0x3FF) * 0.0173f) + @float.CreationTime * 0.37);
						float num9 = (float)((double)((float)((@float.GetHashCode() >> 10) & 0x3FF) * 0.0271f) + @float.CreationTime * 0.19);
						float x5 = 0.5f + 0.5f * MathUtils.Sin(num7 * MathUtils.Lerp(1.2f, 2.4f, f) + num8);
						float num10 = 0.5f + 0.5f * MathUtils.Sin(num7 * MathUtils.Lerp(4f, 7.2f, f) + num9);
						float num11 = MathUtils.Saturate(MathUtils.Pow(x5, 2f) * (0.7f + 0.3f * num10));
						Vector3 vector5 = Vector3.Normalize(position2 - vector2);
						Vector3 v = vector4 - vector5 * Vector3.Dot(vector4, vector5);
						if (v.LengthSquared() > 1E-06f)
						{
							v = Vector3.Normalize(v);
							float num12 = MathUtils.Sin(num7 * MathUtils.Lerp(4f, 7.2f, f) + num9 * 0.7f);
							float num13 = x4 * windExposureFactor * num11;
							float num14 = MathUtils.Max(0f, num13 + num13 * 0.08f * num12);
							vector3 += v * (num2 * num14);
						}
					}
				}
			}
			Vector3 p = vector2;
			for (int i = 1; i <= 16; i++)
			{
				float num15 = (float)i / 16f;
				float num16 = 1f - num15;
				Vector3 vector6 = num16 * num16 * vector2 + 2f * num16 * num15 * vector3 + num15 * num15 * position2;
				if (i < 16 && m_subsystemWeather != null)
				{
					float currentWindStrength2 = m_subsystemWeather.CurrentWindStrength;
					if (currentWindStrength2 >= 6f)
					{
						Vector2 currentWindVector2 = m_subsystemWeather.CurrentWindVector;
						Vector3 vector7 = Vector3.Normalize(new Vector3(currentWindVector2.X, 0f, currentWindVector2.Y));
						float windExposureFactor2 = m_subsystemWeather.GetWindExposureFactor(vector6);
						if (windExposureFactor2 > 0f)
						{
							Vector3 vector8 = Vector3.Normalize(vector6 - vector2);
							if (vector8.LengthSquared() < 1E-06f)
							{
								vector8 = Vector3.Normalize(position2 - vector2);
							}
							Vector3 v2 = vector7 - vector8 * Vector3.Dot(vector7, vector8);
							if (v2.LengthSquared() > 1E-06f)
							{
								v2 = Vector3.Normalize(v2);
								float num17 = (float)gameTime;
								float num18 = MathUtils.Saturate((currentWindStrength2 - 6f) / 10f);
								float num19 = num15 * num15 * (1f - num15);
								float num20 = (float)((double)((float)(@float.GetHashCode() & 0x3FF) * 0.0173f) + @float.CreationTime * 0.37 + (double)((float)i * 0.13f));
								float num21 = (float)((double)((float)((@float.GetHashCode() >> 10) & 0x3FF) * 0.0271f) + @float.CreationTime * 0.19 + (double)((float)i * 0.07f));
								float x6 = 0.5f + 0.5f * MathUtils.Sin(num17 * MathUtils.Lerp(1.2f, 2.4f, num18) + num20);
								float num22 = 0.5f + 0.5f * MathUtils.Sin(num17 * MathUtils.Lerp(1.8f, 3.2f, num18) + num21);
								float num23 = MathUtils.Saturate(MathUtils.Pow(x6, 2f) * (0.65f + 0.35f * num22));
								float num24 = (MathUtils.Lerp(0f, 0.25f, MathUtils.Saturate((currentWindStrength2 - 6f) / 4f)) + MathUtils.Lerp(0f, 0.35f, MathUtils.Saturate((currentWindStrength2 - 10f) / 6f))) * windExposureFactor2 * num23 * num19;
								float num25 = MathUtils.Sin(num17 * MathUtils.Lerp(1.2f, 2f, num18) + num21 * 0.6f);
								float num26 = MathUtils.Max(0f, num24 + num24 * 0.08f * num25);
								float num27 = ((currentWindStrength2 >= 12f) ? (0.02f * num18 * MathUtils.Sin(num17 * 1.1f + (float)i * 0.3f)) : 0f);
								vector6 += v2 * (num2 * num26) + Vector3.UnitY * num27;
							}
						}
					}
				}
				flatBatch3D.QueueLine(p, vector6, Color.Black);
				p = vector6;
			}
		}
		m_primitivesRenderer.Flush(camera.ViewProjectionMatrix);
	}

	public static bool QuaternionToAxisAngle(Quaternion q, out Vector3 axis, out float angle)
	{
		if (q.W > 1f)
		{
			q = Quaternion.Normalize(q);
		}
		angle = 2f * (float)Math.Acos(q.W);
		float num = (float)Math.Sqrt(1.0 - (double)(q.W * q.W));
		if (num < 0.001f)
		{
			axis = new Vector3(q.X, q.Y, q.Z);
		}
		else
		{
			axis = new Vector3(q.X / num, q.Y / num, q.Z / num);
		}
		return true;
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemFluidBlockBehavior = Project.FindSubsystem<SubsystemFocalorsFluidBlockBehavior>(throwOnError: true);
		m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemSky = Project.FindSubsystem<SubsystemSky>(throwOnError: true);
		m_subsystemBodies = Project.FindSubsystem<SubsystemBodies>(throwOnError: true);
		m_oceanScanner = Project.FindSubsystem<SubsystemOceanScanner>(throwOnError: true);
		m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
		FloatRemoved += (FishingFloat f) =>
		{
			CleanupFloatSounds(f);
		};
	}

	public bool IsWater(Vector3 pos)
	{
		int cellContents = m_subsystemTerrain.Terrain.GetCellContents(Terrain.ToCell(pos.X), Terrain.ToCell(pos.Y), Terrain.ToCell(pos.Z));
		return BlocksManager.Blocks[cellContents] is FocalorsWaterBlock;
	}

	public SubsystemFishingFloats()
	{
		m_rodDamageAttemptCounter = new Dictionary<ComponentPlayer, int>();
		m_rodDamageThreshold = new Dictionary<ComponentPlayer, int>();
		m_redClicks = new Dictionary<FishingFloat, int>();
		m_baitDisappearAt = new Dictionary<FishingFloat, double>();
		m_surpriseUntil = new Dictionary<FishingFloat, double>();
		m_detachAt = new Dictionary<FishingFloat, double>();
		PivotFishMassKg = 140f;
		MaxLineLength = 25f;
		PlayerMassKg = 80f;
		RopeSnapStrength = 1f;
		AreaBlocksSmall = 1.5f;
		AreaBlocksMidMax = 7.5f;
		AreaBlocksHeavy = 3f;
		AreaBlocksNearImpossible = 1.5f;
		GreenSizeFracSmall = 0.35f;
		GreenSizeFracHeavy = 0.1f;
		YellowOuterFracSmall = 0.7f;
		YellowOuterFracHeavy = 0.35f;
		GreenAngleMin = 0.01f;
		TargetOrbitSpeedMin = 0.4f;
		TargetOrbitSpeedMax = 2f;
		TargetRadJitter = 0.25f;
		TargetRadReseedMin = 0.8f;
		TargetRadReseedMax = 1.8f;
		m_targetAngle = new Dictionary<FishingFloat, float>();
		m_targetAngularSpeed = new Dictionary<FishingFloat, float>();
		m_targetInnerRad = new Dictionary<FishingFloat, float>();
		m_targetNextRadReseed = new Dictionary<FishingFloat, float>();
		AreaScale = 1f;
		MG_RadiusMin = 0.018f;
		MG_RadiusMax = 0.055f;
		MG_SpeedMin = 0.35f;
		MG_SpeedMax = 1.8f;
		MG_SpeedResistPow = 0.65f;
		MG_SuccessFactorSmall = 0.75f;
		MG_SuccessFactorHeavy = 0.32f;
		MG_SuccessAbsMin = 0.012f;
		MG_ColorExponent = 0.6f;
		m_mgOffset = new Dictionary<FishingFloat, Vector2>();
		m_fishAligned = new Dictionary<FishingFloat, bool>();
		m_fishMistakes = new Dictionary<FishingFloat, int>();
		m_targetAngle = new Dictionary<FishingFloat, float>();
		m_targetSpeed = new Dictionary<FishingFloat, float>();
		m_reelMinSpeed = 3f;
		m_reelMaxSpeed = 12f;
		m_reelCatchDistance = 0.9f;
		PullAccelMax = 5f;
		LightMassKg = 30f;
		MidMassThreshold = 300f;
		NearImpossibleMass = 1000f;
		ImpossibleMass = 1500f;
		MaxResistMass = 1500f;
		MinSpeedScaleHeavy = 0.05f;
		AbortRatePerSec = 1.2f;
		AttemptResistReduction = 0.85f;
		StuckEffectPeriod = 0.9f;
		ReelGravity = 6f;
		ReelJitter = 2f;
		AreaRadiusMin = 0.03f;
		AreaRadiusMax = 0.14f;
		GreenFracMax = 0.55f;
		GreenFracMin = 0.05f;
		YellowFracMax = 0.85f;
		YellowFracMin = 0.25f;
		TargetSpeedMin = 0.4f;
		TargetSpeedMax = 2.8f;
		m_random = new Random();
		m_floats = new List<FishingFloat>();
		m_primitivesRenderer = new PrimitivesRenderer3D();
		m_drawBlockEnvironmentData = new DrawBlockEnvironmentData();
		ReelImpulseAddMin = 0.6f;
		ReelImpulseAddMax = 1.6f;
		ReelImpulseMaxTime = 2.5f;
		ReelImpulseDrainMin = 0.7f;
		ReelImpulseDrainMax = 1.6f;
		VisualTensionImpulseDuration = 0.5f;
		VisualTensionRelaxDuration = 1f;
		VisualRelaxPerFreeBlock = 0.03f;
		VisualSagMax = 5f;
		m_ropeImpulseUntil = new Dictionary<FishingFloat, double>();
		m_ropeRelaxUntil = new Dictionary<FishingFloat, double>();
		m_ropeTensionSmoothed = new Dictionary<FishingFloat, float>();
	}

	public float GetTargetSurfaceY(Vector3 position)
	{
		int x = Terrain.ToCell(position.X);
		int y = Terrain.ToCell(position.Y);
		int z = Terrain.ToCell(position.Z);
		float? surfaceHeight = m_subsystemFluidBlockBehavior.GetSurfaceHeight(x, y, z);
		bool flag = m_oceanScanner != null && position.Y >= 45f && m_oceanScanner.IsOcean;
		bool hasValue = surfaceHeight.HasValue;
		if (flag)
		{
			return m_oceanScanner.GetWaveSurfaceY(position);
		}
		if (flag & hasValue)
		{
			return Math.Max(m_oceanScanner.GetWaveSurfaceY(position), surfaceHeight.Value);
		}
		if (hasValue)
		{
			if (m_oceanScanner != null)
			{
				float waveSurfaceY = m_oceanScanner.GetWaveSurfaceY(position);
				if (Math.Abs(waveSurfaceY - surfaceHeight.Value) < 1f)
				{
					return waveSurfaceY;
				}
			}
			return surfaceHeight.Value;
		}
		return position.Y;
	}

	public void CastOrReel(Vector3 position, Vector3 velocity, Vector3 angularVelocity, ComponentCreature owner, int bobberValue)
	{
		if (owner != null && FindFloat(owner) != null)
		{
			ReelFloat(owner);
		}
		else
		{
			CastFloat(position, velocity, angularVelocity, owner, bobberValue);
		}
	}

	public FishingFloat FindFloat(ComponentCreature owner)
	{
		if (owner == null)
		{
			return null;
		}
		for (int num = m_floats.Count - 1; num >= 0; num--)
		{
			FishingFloat fishingFloat = m_floats[num];
			if (fishingFloat.Owner == owner)
			{
				return fishingFloat;
			}
		}
		return null;
	}

	private void RemoveAndReturn(int idx, FishingFloat f, ComponentCreature owner)
	{
		CleanupFloatSounds(f);
		m_floats.RemoveAt(idx);
		int value = ((f.BobberValue != 0) ? f.BobberValue : Terrain.MakeBlockValue(396, 0, 0));
		ResetPlayerSightColor(owner);
		ClearRopeTimers(f);
		m_ropeTensionSmoothed.Remove(f);
		if (owner != null)
		{
			ComponentPlayer componentPlayer = owner.Entity.FindComponent<ComponentPlayer>();
			if (componentPlayer != null)
			{
				IInventory inventory = componentPlayer.ComponentMiner.Inventory;
				int num = -1;
				int activeSlotIndex = inventory.ActiveSlotIndex;
				if (activeSlotIndex >= 0)
				{
					int slotValue = inventory.GetSlotValue(activeSlotIndex);
					if (inventory.GetSlotCount(activeSlotIndex) > 0 && Terrain.ExtractContents(slotValue) == 395)
					{
						num = activeSlotIndex;
					}
				}
				if (num < 0)
				{
					for (int i = 0; i < inventory.SlotsCount; i++)
					{
						int slotValue2 = inventory.GetSlotValue(i);
						if (inventory.GetSlotCount(i) > 0 && Terrain.ExtractContents(slotValue2) == 395)
						{
							num = i;
							break;
						}
					}
				}
				if (num >= 0)
				{
					int data = Terrain.ExtractData(inventory.GetSlotValue(num));
					int data2 = Terrain.ExtractData(value);
					int bait = BobberBlock.GetBait(data2);
					int rot = BobberBlock.GetBaitRot(data2) & 1;
					int baitData = ((bait != 0) ? (BobberBlock.GetBaitData(data2) & 0xF) : 0);
					int data3 = FishingRodBlock.SetRodBaitData(data, baitData);
					data3 = FishingRodBlock.SetBobber(data3, hasBobber: true, bait, rot);
					data3 &= -4;
					inventory.RemoveSlotItems(num, 1);
					inventory.AddSlotItems(num, Terrain.MakeBlockValue(395, 0, data3), 1);
					if (owner != null)
					{
						m_subsystemAudio.PlayRandomSound("Audio/Fishing/Snapping", 0.9f, m_random.Float(-0.2f, 0.2f), owner.ComponentCreatureModel.EyePosition, 4f, autoDelay: true);
					}
					FloatRemoved?.Invoke(f);
					if (CommonLib.WorkType != WorkType.Client)
					{
						CommonLib.Net.QueuePackage(new FishingFloatPackage(FishingFloatPackage.FishingFloatAction.Remove, f));
					}
					return;
				}
				int num2 = ComponentInventoryBase.FindAcquireSlotForItem(inventory, value);
				if (num2 >= 0)
				{
					inventory.AddSlotItems(num2, value, 1);
					if (owner != null)
					{
						m_subsystemAudio.PlayRandomSound("Audio/Fishing/Snapping", 0.9f, m_random.Float(-0.2f, 0.2f), owner.ComponentCreatureModel.EyePosition, 4f, autoDelay: true);
					}
					FloatRemoved?.Invoke(f);
					if (CommonLib.WorkType != WorkType.Client)
					{
						CommonLib.Net.QueuePackage(new FishingFloatPackage(FishingFloatPackage.FishingFloatAction.Remove, f));
					}
					return;
				}
			}
		}
		SubsystemPickables subsystemPickables = Project.FindSubsystem<SubsystemPickables>(throwOnError: true);
		if (subsystemPickables != null && CommonLib.WorkType != WorkType.Client)
		{
			subsystemPickables.AddPickable(value, 1, f.Position, Vector3.Zero, null);
		}
		if (owner != null)
		{
			m_subsystemAudio.PlayRandomSound("Audio/Fishing/Snapping", 0.9f, m_random.Float(-0.2f, 0.2f), owner.ComponentCreatureModel.EyePosition, 4f, autoDelay: true);
		}
		FloatRemoved?.Invoke(f);
		if (CommonLib.WorkType != WorkType.Client)
		{
			CommonLib.Net.QueuePackage(new FishingFloatPackage(FishingFloatPackage.FishingFloatAction.Remove, f));
		}
	}

	private FishingFloat CreateFloat(Vector3 position, Vector3 velocity, Vector3 angularVelocity, ComponentCreature owner, int bobberValue)
	{
		Vector3 angularVelocity2 = ((angularVelocity.LengthSquared() > 0f) ? angularVelocity : m_random.Vector3(-2f, 2f));
		return new FishingFloat
		{
			Position = position,
			Velocity = velocity,
			Rotation = Vector3.Zero,
			AngularVelocity = angularVelocity2,
			Owner = owner,
			CreationTime = m_subsystemTime.GameTime,
			IsInWater = false,
			IsFloating = false,
			VerticalAlignFactor = 0f,
			HasSplash = false,
			StuckBody = null,
			BodyOffset = Vector3.Zero,
			IsRetracting = false,
			BobberValue = bobberValue,
			ReelSpeedScale = 1f,
			ReelAbortProbPerSec = 0f,
			ReelAttempts = 0,
			ReelWasAborted = false,
			EffectTimer = 0f,
			FightState = FishingFloat.FishFightState.Calm,
			FightTimer = m_random.Float(1f, 3f),
			Fatigue = 0f,
			ReelImpulseTimeLeft = 0f
		};
	}

	private float ComputeBaseResist(float massKg)
	{
		if (massKg <= LightMassKg)
		{
			return 0f;
		}
		if (massKg >= MaxResistMass)
		{
			return 1f;
		}
		return MathUtils.Saturate((massKg - LightMassKg) / (MaxResistMass - LightMassKg));
	}

	private float ComputeResistNow(FishingFloat f, float massKg)
	{
		float num = ComputeBaseResist(massKg);
		float num2 = 1f - 0.7f * MathUtils.Saturate(f.Fatigue);
		return num * num2;
	}

	private float ComputeResistLegacy(float massKg, int attempts)
	{
		float num = ComputeBaseResist(massKg) * 2f;
		for (int i = 0; i < attempts; i++)
		{
			num *= AttemptResistReduction;
		}
		return num;
	}

	private void UpdateRetractingNoFish(FishingFloat f, Vector3 handPos, float dt)
	{
		Vector3 vector = handPos - f.Position;
		float num = vector.Length();
		if (!(num < m_reelCatchDistance))
		{
			Vector3 vector2 = ((num > 1E-06f) ? (vector / num) : Vector3.Zero);
			float num2 = MathUtils.Lerp(m_reelMinSpeed * 4f, m_reelMaxSpeed * 4f, MathUtils.Saturate(num / 8f));
			f.Position += vector2 * num2 * dt;
			f.Velocity = vector2 * num2;
			f.Velocity.Y = f.Velocity.Y - ReelGravity * dt;
			f.AngularVelocity += m_random.Vector3(0f - ReelJitter, ReelJitter) * dt;
			f.Rotation += f.AngularVelocity * dt;
			f.AngularVelocity *= MathUtils.Pow(0.9f, dt);
			f.IsInWater = false;
			f.IsFloating = false;
			f.VerticalAlignFactor = 1f;
			SetRodTension(f.Owner, 0f, hasFish: false);
			Sound sound = EnsureLoop(m_sFastRewind, f, "Audio/Fishing/FastLineRewind");
			if (sound.State != SoundState.Playing)
			{
				sound.Pitch = AudioManager.ToEnginePitch(m_random.Float(-0.2f, 0.2f));
			}
			FadeLoop(sound, 0.7f, dt);
			if (m_sFastUnwind.TryGetValue(f, out var value))
			{
				FadeLoop(value, 0f, dt);
			}
			if (m_sUnwind.TryGetValue(f, out var value2))
			{
				FadeLoop(value2, 0f, dt);
			}
			if (m_sRewind.TryGetValue(f, out var value3))
			{
				FadeLoop(value3, 0f, dt);
			}
			if (m_sCreakHeavy.TryGetValue(f, out var value4))
			{
				FadeLoop(value4, 0f, dt);
			}
		}
	}

	private void UpdateRetractingFish(FishingFloat f, ComponentBody hookedBody, ComponentCreature owner, Vector3 handPos, float pullAccelMax, float dt, ref int listIndex)
	{
		float mass = hookedBody.Mass;
		float num = MathUtils.Lerp(ReelImpulseDrainMax, ReelImpulseDrainMin, MathUtils.Saturate(f.Fatigue));
		f.ReelImpulseTimeLeft = MathUtils.Max(f.ReelImpulseTimeLeft - dt * num, 0f);
		bool flag = f.ReelImpulseTimeLeft > 0f;
		float num2 = ComputeBaseResist(mass);
		float x = ComputeResistNow(f, mass);
		float num3 = ComputeMassPullScale(mass);
		float num4 = 1f;
		if (owner != null)
		{
			ComponentPlayer componentPlayer = owner.Entity.FindComponent<ComponentPlayer>();
			if (componentPlayer != null)
			{
				float num5 = MathUtils.Clamp(componentPlayer.PlayerData.Level, 1f, 21f);
				num4 = 1f + (num5 - 1f) / 20f;
			}
		}
		SetRodTension(owner, mass, flag);
		if (!flag)
		{
			f.Fatigue = MathUtils.Max(f.Fatigue - 0.04f * dt, 0f);
			Vector3 position = hookedBody.Matrix.Translation + f.BodyOffset;
			if (hookedBody.Entity != null)
			{
				ComponentCreatureModel componentCreatureModel = hookedBody.Entity.FindComponent<ComponentCreatureModel>();
				if (componentCreatureModel != null)
				{
					position = componentCreatureModel.EyePosition;
				}
			}
			f.Position = position;
			f.Velocity = hookedBody.Velocity;
			bool flag2 = (f.IsFloating = (f.IsInWater = IsWater(f.Position)));
			f.VerticalAlignFactor = (flag2 ? MathUtils.Clamp(f.VerticalAlignFactor + dt * 3f, 0f, 1f) : 1f);
			if (flag2)
			{
				f.EffectTimer -= dt;
				if (f.EffectTimer <= 0f)
				{
					f.EffectTimer = StuckEffectPeriod;
					m_subsystemParticles.AddParticleSystem(new WaterSplashParticleSystem(m_subsystemTerrain, f.Position, large: false));
					m_subsystemAudio.PlayRandomSound("Audio/Splashes", 0.4f, 0f, f.Position, 4f, autoDelay: true);
				}
			}
			if (f.IsRetracting)
			{
				f.IsRetracting = false;
			}
			return;
		}
		if (num2 > 0f && num3 > 0f)
		{
			float num6 = ((num2 >= 1f) ? 1f : (AbortRatePerSec * num2));
			int mGSeed = GetMGSeed(f);
			float num7 = Hash01(mGSeed, 60) * ((float)Math.PI * 2f);
			if (0.5f + 0.5f * MathUtils.Sin((float)m_subsystemTime.GameTime * (0.25f + 1.5f * num6) + num7) > 0.98f)
			{
				f.IsRetracting = false;
				f.ReelWasAborted = true;
				f.ReelImpulseTimeLeft = 0f;
				f.Fatigue = MathUtils.Clamp(f.Fatigue + 0.03f, 0f, 1f);
				if (IsWater(f.Position))
				{
					m_subsystemParticles.AddParticleSystem(new WaterSplashParticleSystem(m_subsystemTerrain, f.Position, large: false));
					m_subsystemAudio.PlayRandomSound("Audio/Splashes", 0.4f, 0f, f.Position, 4f, autoDelay: true);
				}
				return;
			}
		}
		Vector3 vector = handPos - hookedBody.Position;
		float num8 = vector.Length();
		if (num8 < m_reelCatchDistance)
		{
			return;
		}
		float num9 = MathUtils.Lerp(m_reelMinSpeed, m_reelMaxSpeed, MathUtils.Saturate(num8 / 8f));
		float num10 = MathUtils.Lerp(1f, MinSpeedScaleHeavy, MathUtils.Saturate(x));
		float num11 = num9 * num10 * num3 * num4;
		float num12 = PullAccelMax * num10 * num3 * num4;
		Vector3 vector2 = new Vector3(vector.X, 0f, vector.Z);
		float num13 = vector2.Length();
		Vector3 vector3 = ((num13 > 1E-06f) ? (vector2 / num13) : Vector3.Zero);
		Vector3 velocity = hookedBody.Velocity;
		Vector3 vector4 = vector3 * num11 - new Vector3(velocity.X, 0f, velocity.Z);
		float num14 = vector4.Length();
		float num15 = num12 * dt;
		if (num14 > num15 && num14 > 1E-06f)
		{
			vector4 *= num15 / num14;
		}
		velocity.X += vector4.X;
		velocity.Z += vector4.Z;
		if (hookedBody.ImmersionFactor > 0.3f && num3 > 0f)
		{
			float num16 = MathUtils.Saturate(1f - num13 / 5f);
			float num17 = MathUtils.Lerp(1f, 0.3f, MathUtils.Saturate((mass - LightMassKg) / (NearImpossibleMass - LightMassKg)));
			float num18 = (vector.Y / num8 * num11 - velocity.Y) * num16 * num17;
			float num19 = num12 * 100f * dt;
			if (MathUtils.Abs(num18) > num19)
			{
				num18 = (float)Math.Sign(num18) * num19;
			}
			velocity.Y += num18;
		}
		if (CommonLib.WorkType != WorkType.Client)
		{
			hookedBody.Velocity = velocity;
			if (hookedBody.Velocity.LengthSquared() > 625f)
			{
				hookedBody.Velocity = Vector3.Normalize(hookedBody.Velocity) * 25f;
			}
		}
		f.Fatigue = MathUtils.Clamp(f.Fatigue + dt * (0.08f + 0.2f * num2) * num3, 0f, 1f);
		if (m_fishAligned.TryGetValue(f, out var value) & value)
		{
			f.Fatigue = MathUtils.Clamp(f.Fatigue + dt * 0.15f * num3, 0f, 1f);
		}
		Vector3 position2 = hookedBody.Matrix.Translation + f.BodyOffset;
		if (hookedBody.Entity != null)
		{
			ComponentCreatureModel componentCreatureModel2 = hookedBody.Entity.FindComponent<ComponentCreatureModel>();
			if (componentCreatureModel2 != null)
			{
				position2 = componentCreatureModel2.EyePosition;
			}
		}
		f.Position = position2;
		f.Velocity = hookedBody.Velocity;
		bool flag3 = (f.IsFloating = (f.IsInWater = IsWater(f.Position)));
		f.VerticalAlignFactor = (flag3 ? MathUtils.Clamp(f.VerticalAlignFactor + dt * 3f, 0f, 1f) : 1f);
		if (flag3)
		{
			f.EffectTimer -= dt;
			if (f.EffectTimer <= 0f)
			{
				f.EffectTimer = StuckEffectPeriod;
				m_subsystemParticles.AddParticleSystem(new WaterSplashParticleSystem(m_subsystemTerrain, f.Position, large: false));
				m_subsystemAudio.PlayRandomSound("Audio/Splashes", 0.4f, 0f, f.Position, 4f, autoDelay: true);
			}
		}
		UpdateLineStepSounds(f, owner, dt);
		UpdateHeavyCreak(f, mass, dt);
		if (m_sFastUnwind.TryGetValue(f, out var value2))
		{
			FadeLoop(value2, 0f, dt);
		}
		if (m_sFastRewind.TryGetValue(f, out var value3))
		{
			FadeLoop(value3, 0f, dt);
		}
	}

	private void UpdateFishFight(FishingFloat f, ComponentBody body, ComponentCreature owner, float dt)
	{
		float mass = body.Mass;
		float num = ComputeBaseResist(mass);
		SetRodTension(owner, mass, hasFish: true);
		int mGSeed = GetMGSeed(f);
		float num2 = (float)m_subsystemTime.GameTime;
		float num3 = Hash01(mGSeed, 50) * ((float)Math.PI * 2f);
		float num4 = MathUtils.Lerp(0.25f, 0.8f, num);
		float num5 = 0.5f + 0.5f * MathUtils.Sin(num2 * num4 + num3);
		if (num5 < 0.4f)
		{
			f.FightState = FishingFloat.FishFightState.Calm;
		}
		else if (num5 < 0.75f)
		{
			f.FightState = FishingFloat.FishFightState.Run;
		}
		else
		{
			f.FightState = FishingFloat.FishFightState.Tired;
		}
		if (f.FightState == FishingFloat.FishFightState.Calm)
		{
			f.Fatigue = MathUtils.Max(f.Fatigue - 0.07f * dt, 0f);
		}
		else if (f.FightState == FishingFloat.FishFightState.Tired)
		{
			f.Fatigue = MathUtils.Clamp(f.Fatigue + 0.08f * (0.25f + num) * dt * 1.5f, 0f, 1f);
		}
		else
		{
			f.Fatigue = MathUtils.Clamp(f.Fatigue + 0.02f * num * dt, 0f, 1f);
		}
		if (f.IsFloating = (f.IsInWater = IsWater(f.Position)))
		{
			f.VerticalAlignFactor = MathUtils.Clamp(f.VerticalAlignFactor + dt * 3f, 0f, 1f);
			f.EffectTimer -= dt;
			if (f.EffectTimer <= 0f)
			{
				f.EffectTimer = StuckEffectPeriod;
				m_subsystemParticles.AddParticleSystem(new WaterSplashParticleSystem(m_subsystemTerrain, f.Position, large: false));
				m_subsystemAudio.PlayRandomSound("Audio/Splashes", 0.4f, 0f, f.Position, 4f, autoDelay: true);
				return;
			}
		}
		else
		{
			f.VerticalAlignFactor = 1f;
		}
		UpdateHeavyCreak(f, body.Mass, dt);
		UpdateLineStepSounds(f, owner, dt);
	}

	private void UpdateFreeFloatPhysics(FishingFloat f, float dt)
	{
		int num = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(Terrain.ToCell(f.Position.X), Terrain.ToCell(f.Position.Y), Terrain.ToCell(f.Position.Z)));
		if (num == 92 || BlocksManager.Blocks[num] is FocalorsMagmaBlock)
		{
			if (CommonLib.WorkType != WorkType.Client)
			{
				CommonLib.Net.QueuePackage(new FishingFloatPackage(FishingFloatPackage.FishingFloatAction.Remove, f));
			}
			RemoveAndReturn(m_floats.IndexOf(f), f, null);
			return;
		}
		Vector3 position = f.Position;
		Vector3 velocity = f.Velocity;
		Vector3 end = position + velocity * dt;
		BodyRaycastResult? bodyRaycastResult = m_subsystemBodies.Raycast(position, end, 0.2f, (ComponentBody body, float dist) => f.Owner == null || body != f.Owner.ComponentBody);
		if (bodyRaycastResult.HasValue)
		{
			Vector3 pos = bodyRaycastResult.Value.ComponentBody?.Matrix.Translation ?? f.Position;
			if (!IsHookingForbiddenInRegion(f.Owner, pos))
			{
				f.StuckBody = bodyRaycastResult.Value.ComponentBody;
				f.BodyOffset = f.Position - f.StuckBody.Matrix.Translation;
				f.EffectTimer = StuckEffectPeriod;
				InitMiniGame(f);
				InitHookState(f);
				f.Velocity += new Vector3(0f, -2f, 0f);
				StopDispose(m_sFastUnwind, f);
				StopDispose(m_sFastRewind, f);
				if (m_sUnwind.TryGetValue(f, out var value))
				{
					FadeLoop(value, 0f, dt);
				}
				if (m_sRewind.TryGetValue(f, out value))
				{
					FadeLoop(value, 0f, dt);
				}
				if (CommonLib.WorkType != WorkType.Client)
				{
					CommonLib.Net.QueuePackage(new FishingFloatPackage(FishingFloatPackage.FishingFloatAction.Attach, f));
				}
				return;
			}
		}
		Vector3 vector = velocity;
		Vector3 end2 = position + vector * dt;
		TerrainRaycastResult? terrainRaycastResult = m_subsystemTerrain.Raycast(position, end2, useInteractionBoxes: false, skipAirBlocks: true, (int cellValue, float distance) => BlocksManager.Blocks[Terrain.ExtractContents(cellValue)].IsCollidable);
		if (terrainRaycastResult.HasValue)
		{
			Plane plane = terrainRaycastResult.Value.CellFace.CalculatePlane();
			if (plane.Normal.X != 0f)
			{
				vector *= new Vector3(-0.3f, 0.3f, 0.3f);
			}
			if (plane.Normal.Y != 0f)
			{
				vector *= new Vector3(0.3f, -0.3f, 0.3f);
			}
			if (plane.Normal.Z != 0f)
			{
				vector *= new Vector3(0.3f, 0.3f, -0.3f);
			}
			float num2 = vector.Length();
			vector = num2 * Vector3.Normalize(vector + m_random.Vector3(num2 / 6f, num2 / 3f));
			f.AngularVelocity *= -0.3f;
		}
		int x = Terrain.ToCell(position.X);
		int y = Terrain.ToCell(position.Y);
		int z = Terrain.ToCell(position.Z);
		float? surfaceHeight = m_subsystemFluidBlockBehavior.GetSurfaceHeight(x, y, z);
		bool hasValue = surfaceHeight.HasValue;
		float num3 = surfaceHeight ?? position.Y;
		bool flag = m_oceanScanner != null && m_oceanScanner.IsOcean && position.Y >= 45f;
		bool flag2 = hasValue && IsWater(new Vector3(position.X, num3 + 0.01f, position.Z)) && !flag;
		bool flag3 = hasValue && !flag && !flag2;
		float num4 = ((flag | flag2 | flag3) ? m_oceanScanner.GetWaveSurfaceY(position) : num3);
		float num5;
		if (flag)
		{
			num5 = num4;
		}
		else if (flag2)
		{
			num5 = Math.Max(num4, num3);
		}
		else if (flag3)
		{
			num5 = MathUtils.Clamp(num4, num3 - 1f, num3 + 1f);
		}
		else
		{
			num5 = ((!hasValue) ? position.Y : num3);
		}
		bool flag4 = (flag | hasValue) && position.Y <= num5;
		if (m_subsystemWeather != null)
		{
			float currentWindStrength = m_subsystemWeather.CurrentWindStrength;
			if (currentWindStrength >= 6f && m_subsystemWeather.GetWindExposureFactor(position) > 0f)
			{
				float num6 = ((!(currentWindStrength < 10f)) ? MathUtils.Lerp(0.005f, 0.03f, MathUtils.Saturate((currentWindStrength - 10f) / 6f)) : MathUtils.Lerp(0f, 0.005f, MathUtils.Saturate((currentWindStrength - 6f) / 4f)));
				if (flag4)
				{
					num6 *= 0.5f;
				}
				Vector3 vector2 = new Vector3(m_subsystemWeather.CurrentWindVector.X, 0f, m_subsystemWeather.CurrentWindVector.Y);
				vector += vector2 * num6;
			}
		}
		bool isInWater = f.IsInWater;
		if (flag4)
		{
			f.IsInWater = true;
			if (!f.HasSplash && (flag | flag2 | flag3) && num5 > num3)
			{
				m_subsystemParticles.AddParticleSystem(new WaterSplashParticleSystem(m_subsystemTerrain, new Vector3(position.X, num5, position.Z), large: false));
				m_subsystemAudio.PlayRandomSound("Audio/Splashes", 1f, 0f, position, 6f, autoDelay: true);
				f.HasSplash = true;
			}
			vector *= (isInWater ? MathUtils.Pow(0.02f, dt) : 0.1f);
			f.Position = new Vector3(position.X + vector.X * dt, MathUtils.Lerp(position.Y, num5 + 0.01f, 5f * dt), position.Z + vector.Z * dt);
			f.VerticalAlignFactor = MathUtils.Clamp(f.VerticalAlignFactor + dt * 3f, 0f, 1f);
			f.IsFloating = true;
		}
		else
		{
			f.IsInWater = false;
			f.IsFloating = false;
			vector.Y -= 10f * dt;
			vector *= MathUtils.Pow(0.99f, dt);
			Vector3 vector3 = f.Position + vector * dt;
			TerrainRaycastResult? terrainRaycastResult2 = m_subsystemTerrain.Raycast(f.Position, vector3, useInteractionBoxes: false, skipAirBlocks: true, (int val, float d) => BlocksManager.Blocks[Terrain.ExtractContents(val)].IsCollidable);
			if (terrainRaycastResult2.HasValue)
			{
				f.Position = terrainRaycastResult2.Value.HitPoint();
				vector = Vector3.Zero;
			}
			else
			{
				f.Position = vector3;
			}
		}
		f.Velocity = vector;
		f.Rotation += f.AngularVelocity * dt;
		QuaternionToAxisAngle(Quaternion.Slerp(Quaternion.CreateFromAxisAngle(Vector3.Normalize((f.Rotation.LengthSquared() > 0f) ? f.Rotation : Vector3.UnitY), f.Rotation.Length()), Quaternion.Identity, f.VerticalAlignFactor), out var axis, out var angle);
		f.Rotation = axis * angle;
		Sound value2;
		if (f.StuckBody == null && !f.IsInWater && f.Velocity.LengthSquared() > 1f)
		{
			Sound sound = EnsureLoop(m_sFastUnwind, f, "Audio/Fishing/FastLineUnwinding");
			if (sound.State != SoundState.Playing)
			{
				sound.Pitch = AudioManager.ToEnginePitch(m_random.Float(-0.2f, 0.2f));
			}
			FadeLoop(sound, 0.7f, dt);
		}
		else if (m_sFastUnwind.TryGetValue(f, out value2))
		{
			FadeLoop(value2, 0f, dt);
		}
	}

	private void UpdateAimMiniGame(FishingFloat f, ComponentCreature owner, float dt)
	{
		if (owner == null || f.StuckBody == null)
		{
			return;
		}
		ComponentPlayer componentPlayer = owner.Entity.FindComponent<ComponentPlayer>();
		if (componentPlayer != null)
		{
			InitMiniGame(f);
			float mass = f.StuckBody.Mass;
			float num = (float)(m_subsystemTime.GameTime - f.CreationTime);
			Vector3 eyePosition = owner.ComponentCreatureModel.EyePosition;
			Vector3 vector = f.Position - eyePosition;
			float num2 = vector.Length();
			if (num2 < 0.1f)
			{
				num2 = 0.1f;
			}
			Vector3 vector2 = vector / num2;
			Vector3 v = Vector3.Cross(Vector3.UnitY, vector2);
			if (v.LengthSquared() < 1E-06f)
			{
				v = Vector3.Cross(Vector3.UnitX, vector2);
			}
			v = Vector3.Normalize(v);
			Vector3 vector3 = Vector3.Normalize(Vector3.Cross(vector2, v));
			float x = MathUtils.Atan(ComputeBlockAreaRadius(mass) * AreaScale / MathUtils.Max(0.1f, 6f));
			x = MathUtils.Clamp(x, 0.01f, 1.4f);
			float f2 = MathUtils.Saturate(mass / NearImpossibleMass);
			float num3 = MathUtils.Lerp(GreenSizeFracSmall, GreenSizeFracHeavy, f2);
			float num4 = MathUtils.Lerp(YellowOuterFracSmall, YellowOuterFracHeavy, f2);
			if (num4 < num3)
			{
				num4 = num3;
			}
			if (mass >= ImpossibleMass)
			{
				num3 = 0f;
				num4 = 0f;
			}
			float num5 = MathUtils.Max(GreenAngleMin, x * num3);
			float num6 = m_targetAngle[f];
			float num7 = m_targetAngularSpeed[f];
			float x2 = num6 + num * num7;
			float num8 = m_targetInnerRad[f];
			float num9 = m_targetNextRadReseed[f];
			float num10 = MathUtils.Clamp(num8 + 0.12f * MathUtils.Sin(num * num9 + num6 * 0.37f), 0.15f, 0.95f);
			float x3 = num10 * (x * 0.95f);
			float num11 = MathUtils.Sin(x3);
			Vector3 v2 = MathUtils.Cos(x3) * vector2 + num11 * (MathUtils.Cos(x2) * v + MathUtils.Sin(x2) * vector3);
			v2 = Vector3.Normalize(v2);
			m_mgOffset[f] = new Vector2(MathUtils.Cos(x2) * num10, MathUtils.Sin(x2) * num10);
			Camera camera = ((componentPlayer.GameWidget != null) ? componentPlayer.GameWidget.ActiveCamera : null);
			Vector3 v3 = ((camera == null || !(camera.ViewDirection.LengthSquared() > 1E-06f)) ? Vector3.Normalize(owner.ComponentBody.Matrix.Forward) : Vector3.Normalize(camera.ViewDirection));
			bool flag = MathUtils.Acos(MathUtils.Clamp(Vector3.Dot(v3, vector2), -1f, 1f)) > x;
			float num12 = MathUtils.Acos(MathUtils.Clamp(Vector3.Dot(v3, v2), -1f, 1f));
			Color targetSightsColor;
			if (flag)
			{
				targetSightsColor = new Color(160, 160, 160, 255);
			}
			else if (mass >= ImpossibleMass)
			{
				targetSightsColor = new Color(255, 0, 0, 255);
			}
			else if (num12 <= num5)
			{
				targetSightsColor = new Color(0, 255, 0, 255);
			}
			else
			{
				targetSightsColor = ((!(num12 <= x * num4)) ? new Color(255, 0, 0, 255) : new Color(255, 255, 0, 255));
			}
			float num13 = MathUtils.Saturate(mass / NearImpossibleMass);
			Color targetSightsTintColor;
			if (flag)
			{
				targetSightsTintColor = new Color(160, 160, 160, 255);
			}
			else if (mass >= ImpossibleMass)
			{
				targetSightsTintColor = Color.Black;
			}
			else if (num13 < 0.33f)
			{
				targetSightsTintColor = new Color(0, 80, 0, 255);
			}
			else
			{
				targetSightsTintColor = ((!(num13 < 0.66f)) ? new Color(110, 0, 25, 255) : new Color(200, 100, 0, 255));
			}
			bool value = !flag && mass < ImpossibleMass && num12 <= num5;
			m_fishAligned[f] = value;
			componentPlayer.ComponentAimingSights.SetTargetSightsSizeFactor(MathUtils.Lerp(0.48f, 0.08f, num13));
			componentPlayer.ComponentAimingSights.SetTargetSightsColor(targetSightsColor);
			componentPlayer.ComponentAimingSights.SetTargetSightsTintColor(targetSightsTintColor);
			componentPlayer.ComponentAimingSights.ShowTargetSights(eyePosition, v2);
		}
	}

	public bool ProcessReelAttempt(ComponentCreature owner)
	{
		FishingFloat fishingFloat = FindFloat(owner);
		if (fishingFloat == null || fishingFloat.StuckBody == null)
		{
			return true;
		}
		if (IsOwnerRidingHookedBoat(owner, fishingFloat))
		{
			fishingFloat.StuckBody = null;
			fishingFloat.IsRetracting = true;
			fishingFloat.ReelImpulseTimeLeft = 0f;
			ClearMiniGame(fishingFloat);
			ResetPlayerSightColor(owner);
			if (CommonLib.WorkType != WorkType.Client)
			{
				CommonLib.Net.QueuePackage(new FishingFloatPackage(FishingFloatPackage.FishingFloatAction.Detach, fishingFloat));
			}
			return true;
		}
		MarkRopeImpulse(fishingFloat);
		TryTriggerFishStruggle(fishingFloat, 5.75f);
		InitMiniGame(fishingFloat);
		float mass = fishingFloat.StuckBody.Mass;
		Vector3 eyePosition = owner.ComponentCreatureModel.EyePosition;
		Vector3 vector = fishingFloat.Position - eyePosition;
		float num = vector.Length();
		if (num < 0.1f)
		{
			num = 0.1f;
		}
		Vector3 vector2 = vector / num;
		Vector3 v = Vector3.Cross(Vector3.UnitY, vector2);
		if (v.LengthSquared() < 1E-06f)
		{
			v = Vector3.Cross(Vector3.UnitX, vector2);
		}
		v = Vector3.Normalize(v);
		Vector3 vector3 = Vector3.Normalize(Vector3.Cross(vector2, v));
		float x = MathUtils.Atan(ComputeBlockAreaRadius(mass) * AreaScale / MathUtils.Max(0.1f, 6f));
		x = MathUtils.Clamp(x, 0.01f, 1.4f);
		float f = MathUtils.Saturate(mass / NearImpossibleMass);
		float num2 = MathUtils.Lerp(GreenSizeFracSmall, GreenSizeFracHeavy, f);
		float num3 = MathUtils.Lerp(YellowOuterFracSmall, YellowOuterFracHeavy, f);
		if (num3 < num2)
		{
			num3 = num2;
		}
		if (mass >= ImpossibleMass)
		{
			num2 = 0f;
			num3 = 0f;
		}
		float num4 = MathUtils.Max(GreenAngleMin, x * num2);
		float num5 = (float)(m_subsystemTime.GameTime - fishingFloat.CreationTime);
		float num6 = m_targetAngle[fishingFloat];
		float num7 = m_targetAngularSpeed[fishingFloat];
		float x2 = num6 + num5 * num7;
		float num8 = m_targetInnerRad[fishingFloat];
		float num9 = m_targetNextRadReseed[fishingFloat];
		float x3 = MathUtils.Clamp(num8 + 0.12f * MathUtils.Sin(num5 * num9 + num6 * 0.37f), 0.15f, 0.95f) * (x * 0.95f);
		float num10 = MathUtils.Sin(x3);
		Vector3 v2 = MathUtils.Cos(x3) * vector2 + num10 * (MathUtils.Cos(x2) * v + MathUtils.Sin(x2) * vector3);
		v2 = Vector3.Normalize(v2);
		ComponentPlayer componentPlayer = owner?.Entity.FindComponent<ComponentPlayer>();
		Camera camera = ((componentPlayer != null && componentPlayer.GameWidget != null) ? componentPlayer.GameWidget.ActiveCamera : null);
		Vector3 v3 = ((camera == null || !(camera.ViewDirection.LengthSquared() > 1E-06f)) ? Vector3.Normalize(owner.ComponentBody.Matrix.Forward) : Vector3.Normalize(camera.ViewDirection));
		bool flag = MathUtils.Acos(MathUtils.Clamp(Vector3.Dot(v3, vector2), -1f, 1f)) > x;
		float num11 = MathUtils.Acos(MathUtils.Clamp(Vector3.Dot(v3, v2), -1f, 1f));
		bool flag2 = num11 <= num4;
		bool flag3 = !flag2 && num11 <= x * num3;
		bool flag4 = !flag2 && !flag3;
		float f2 = (MathUtils.Clamp(componentPlayer?.PlayerData.Level ?? 1f, 1f, 21f) - 1f) / 20f;
		ComponentVitalStats vital = owner?.Entity.FindComponent<ComponentVitalStats>();
		Action<float> action = (float cost) =>
		{
			if (vital != null && cost > 0f)
			{
				vital.Stamina = MathUtils.Clamp(vital.Stamina - cost, 0f, 1f);
			}
		};
		float obj = MathUtils.Lerp(0.3f, 0.1f, f2);
		float obj2 = MathUtils.Lerp(0.2f, 0.05f, f2);
		float obj3 = MathUtils.Lerp(0.05f, 0.01f, f2);
		if (flag)
		{
			m_subsystemAudio.PlayRandomSound("Audio/Fishing/BadTry", 0.85f, m_random.Float(-0.2f, 0.2f), owner.ComponentCreatureModel.EyePosition, 3f, autoDelay: true);
			if (ShouldDamageRod(owner))
			{
				ApplyRodDamage(owner);
			}
			fishingFloat.StuckBody = null;
			fishingFloat.IsRetracting = true;
			fishingFloat.ReelImpulseTimeLeft = 0f;
			ClearMiniGame(fishingFloat);
			ResetPlayerSightColor(owner);
			if (CommonLib.WorkType != WorkType.Client)
			{
				CommonLib.Net.QueuePackage(new FishingFloatPackage(FishingFloatPackage.FishingFloatAction.Detach, fishingFloat));
			}
			return true;
		}
		if (mass >= ImpossibleMass)
		{
			action(obj);
			int value = 0;
			m_fishMistakes.TryGetValue(fishingFloat, out value);
			value++;
			m_fishMistakes[fishingFloat] = value;
			fishingFloat.Fatigue = MathUtils.Max(fishingFloat.Fatigue - 0.05f, 0f);
			fishingFloat.StuckBody = null;
			fishingFloat.IsRetracting = true;
			fishingFloat.ReelImpulseTimeLeft = 0f;
			ClearMiniGame(fishingFloat);
			ResetPlayerSightColor(owner);
			if (CommonLib.WorkType != WorkType.Client)
			{
				CommonLib.Net.QueuePackage(new FishingFloatPackage(FishingFloatPackage.FishingFloatAction.Detach, fishingFloat));
			}
			return true;
		}
		if (flag4)
		{
			m_subsystemAudio.PlayRandomSound("Audio/Fishing/BadTry", 0.85f, m_random.Float(-0.2f, 0.2f), owner.ComponentCreatureModel.EyePosition, 3f, autoDelay: true);
			action(obj);
			int value2 = 0;
			m_redClicks.TryGetValue(fishingFloat, out value2);
			value2++;
			m_redClicks[fishingFloat] = value2;
			fishingFloat.Fatigue = MathUtils.Max(fishingFloat.Fatigue - 0.05f, 0f);
			if (value2 >= 3)
			{
				fishingFloat.StuckBody = null;
				fishingFloat.IsRetracting = true;
				fishingFloat.ReelImpulseTimeLeft = 0f;
				ClearMiniGame(fishingFloat);
				ResetPlayerSightColor(owner);
				if (CommonLib.WorkType != WorkType.Client)
				{
					CommonLib.Net.QueuePackage(new FishingFloatPackage(FishingFloatPackage.FishingFloatAction.Detach, fishingFloat));
				}
				return true;
			}
			return false;
		}
		if (flag3)
		{
			m_subsystemAudio.PlayRandomSound("Audio/Footsteps/Soft", 0.75f, m_random.Float(-0.2f, 0.2f), owner.ComponentCreatureModel.EyePosition, 2.5f, autoDelay: true);
			action(obj2);
			return false;
		}
		action(obj3);
		int value3 = 0;
		m_fishMistakes.TryGetValue(fishingFloat, out value3);
		value3 = MathUtils.Max(value3 - 1, 0);
		m_fishMistakes[fishingFloat] = value3;
		fishingFloat.Fatigue = MathUtils.Clamp(fishingFloat.Fatigue + 0.08f, 0f, 1f);
		if (fishingFloat.StuckBody != null)
		{
			float num12 = new Vector3(vector2.X, 0f, vector2.Z).Length();
			float num13 = MathUtils.Saturate(1f - num12 / 10f);
			if (num13 > 0f)
			{
				float num14 = ComputeMassPullScale(mass);
				float num15 = 6f * num13 * num14 * (IsInSurprise(fishingFloat) ? 3f : 1f);
				Vector3 velocity = fishingFloat.StuckBody.Velocity;
				float y = fishingFloat.StuckBody.Position.Y;
				float num16 = ((owner.ComponentCreatureModel.EyePosition.Y >= y) ? 1f : (-1f));
				velocity.Y += num15 * num16;
				fishingFloat.StuckBody.Velocity = velocity;
			}
		}
		float num17 = MathUtils.Lerp(ReelImpulseAddMin, ReelImpulseAddMax, fishingFloat.Fatigue) * (IsInSurprise(fishingFloat) ? 20f : 1f);
		fishingFloat.ReelImpulseTimeLeft = MathUtils.Clamp(fishingFloat.ReelImpulseTimeLeft + num17, 0f, ReelImpulseMaxTime);
		if (HasBait(fishingFloat) && m_baitDisappearAt.TryGetValue(fishingFloat, out var value4))
		{
			m_baitDisappearAt[fishingFloat] = value4 + 0.5;
		}
		if (!fishingFloat.IsRetracting)
		{
			fishingFloat.IsRetracting = true;
		}
		m_subsystemAudio.PlayRandomSound("Audio/Fishing/GoodTry", 0.9f, m_random.Float(-0.2f, 0.2f), owner.ComponentCreatureModel.EyePosition, 3f, autoDelay: true);
		return true;
	}

	public void InitMiniGame(FishingFloat f)
	{
		if (!m_targetAngle.ContainsKey(f))
		{
			float f2 = MathUtils.Saturate(((f.StuckBody != null) ? f.StuckBody.Mass : 0f) / NearImpossibleMass);
			int mGSeed = GetMGSeed(f);
			float value = Hash01(mGSeed, 1) * ((float)Math.PI * 2f);
			float value2 = MathUtils.Lerp(TargetOrbitSpeedMin, TargetOrbitSpeedMax, f2) * MathUtils.Lerp(0.85f, 1.25f, Hash01(mGSeed, 2));
			float value3 = MathUtils.Lerp(0.2f, 0.9f, Hash01(mGSeed, 3));
			float value4 = MathUtils.Lerp(0.6f, 1.6f, Hash01(mGSeed, 4));
			m_targetAngle[f] = value;
			m_targetAngularSpeed[f] = value2;
			m_targetInnerRad[f] = value3;
			m_targetNextRadReseed[f] = value4;
			m_fishMistakes[f] = 0;
			m_fishAligned[f] = false;
			m_mgOffset[f] = Vector2.Zero;
			m_redClicks[f] = 0;
		}
	}

	public void ClearMiniGame(FishingFloat f)
	{
		m_fishAligned.Remove(f);
		m_fishMistakes.Remove(f);
		m_targetAngle.Remove(f);
		m_targetAngularSpeed.Remove(f);
		m_targetInnerRad.Remove(f);
		m_targetNextRadReseed.Remove(f);
		ClearRopeTimers(f);
		m_mgOffset.Remove(f);
		m_redClicks.Remove(f);
		CleanupHookState(f);
		if (f != null)
		{
			f.ReelImpulseTimeLeft = 0f;
		}
	}

	public void ResetPlayerSightColor(ComponentCreature owner)
	{
		owner?.Entity.FindComponent<ComponentPlayer>()?.ComponentAimingSights.SetTargetSightsColor(Color.White);
	}

	private float ComputeMassPullScale(float massKg)
	{
		if (massKg >= ImpossibleMass)
		{
			return 0f;
		}
		if (massKg >= NearImpossibleMass)
		{
			return MathUtils.Lerp(0.15f, 0f, (massKg - NearImpossibleMass) / (ImpossibleMass - NearImpossibleMass));
		}
		if (massKg >= MidMassThreshold)
		{
			return MathUtils.Lerp(0.5f, 0.15f, (massKg - MidMassThreshold) / (NearImpossibleMass - MidMassThreshold));
		}
		if (massKg >= LightMassKg)
		{
			return MathUtils.Lerp(1f, 0.5f, (massKg - LightMassKg) / (MidMassThreshold - LightMassKg));
		}
		return 1f;
	}

	private float ComputeAreaRadius(float massKg)
	{
		float f = MathUtils.Saturate(massKg / NearImpossibleMass);
		return MathUtils.Lerp(AreaRadiusMin, AreaRadiusMax, f);
	}

	private float ComputeBlockAreaRadius(float massKg)
	{
		if (massKg <= LightMassKg)
		{
			massKg = LightMassKg;
		}
		if (massKg < MidMassThreshold)
		{
			float x = (massKg - LightMassKg) / Math.Max(1f, MidMassThreshold - LightMassKg);
			return MathUtils.Lerp(AreaBlocksSmall, AreaBlocksMidMax, MathUtils.Saturate(x));
		}
		if (massKg < NearImpossibleMass)
		{
			float x2 = (massKg - MidMassThreshold) / Math.Max(1f, NearImpossibleMass - MidMassThreshold);
			return MathUtils.Lerp(AreaBlocksMidMax, AreaBlocksHeavy, MathUtils.Saturate(x2));
		}
		if (massKg < ImpossibleMass)
		{
			float x3 = (massKg - NearImpossibleMass) / Math.Max(1f, ImpossibleMass - NearImpossibleMass);
			return MathUtils.Lerp(AreaBlocksHeavy, AreaBlocksNearImpossible, MathUtils.Saturate(x3));
		}
		return AreaBlocksNearImpossible;
	}

	public bool ReelFloatNet(ComponentCreature owner)
	{
		if (owner == null)
		{
			return false;
		}
		FishingFloat fishingFloat = FindFloat(owner);
		if (fishingFloat == null)
		{
			return false;
		}
		fishingFloat.IsRetracting = true;
		fishingFloat.ReelAttempts++;
		fishingFloat.ReelWasAborted = false;
		if (fishingFloat.StuckBody != null)
		{
			fishingFloat.Fatigue = MathUtils.Clamp(fishingFloat.Fatigue + (1f - AttemptResistReduction) * 0.35f, 0f, 1f);
		}
		float massKg = ((fishingFloat.StuckBody != null) ? fishingFloat.StuckBody.Mass : 0f);
		float num = ComputeBaseResist(massKg);
		float f = ComputeResistNow(fishingFloat, massKg);
		fishingFloat.ReelSpeedScale = MathUtils.Lerp(1f, MinSpeedScaleHeavy, f);
		fishingFloat.ReelAbortProbPerSec = AbortRatePerSec * num;
		fishingFloat.Velocity = Vector3.Zero;
		fishingFloat.AngularVelocity = Vector3.Zero;
		return true;
	}

	public FishingFloat CastFloatNet(Vector3 position, Vector3 velocity, Vector3 angularVelocity, ComponentCreature owner, int bobberValue)
	{
		return AddFloatNet(position, velocity, angularVelocity, owner, bobberValue);
	}

	public FishingFloat AddFloatNet(Vector3 position, Vector3 velocity, Vector3 angularVelocity, ComponentCreature owner, int bobberValue)
	{
		FishingFloat fishingFloat = CreateFloat(position, velocity, angularVelocity, owner, bobberValue);
		m_floats.Add(fishingFloat);
		FloatAdded?.Invoke(fishingFloat);
		return fishingFloat;
	}

	public void CastFloat(Vector3 position, Vector3 velocity, Vector3 angularVelocity, ComponentCreature owner, int bobberValue)
	{
		if (CommonLib.WorkType != WorkType.Client)
		{
			FishingFloat f = CastFloatNet(position, velocity, angularVelocity, owner, bobberValue);
			CommonLib.Net.QueuePackage(new FishingFloatPackage(FishingFloatPackage.FishingFloatAction.Cast, f));
		}
	}

	public void ReelFloat(ComponentCreature owner)
	{
		if (owner != null && CommonLib.WorkType != WorkType.Client && ReelFloatNet(owner))
		{
			CommonLib.Net.QueuePackage(new FishingFloatPackage(FishingFloatPackage.FishingFloatAction.Reel, owner));
		}
	}

	public void RemoveFloatClient(FishingFloat f)
	{
		int num = m_floats.IndexOf(f);
		if (num >= 0)
		{
			m_floats.RemoveAt(num);
			ClearRopeTimers(f);
			m_ropeTensionSmoothed.Remove(f);
			if (FloatRemoved != null)
			{
				FloatRemoved(f);
			}
		}
	}

	private Vector3 GetRodTip(ComponentCreature cr, int rodData, Camera camera)
	{
		FishingRodBlock fishingRodBlock = (FishingRodBlock)BlocksManager.Blocks[395];
		int num = rodData & 3;
		ModelBone modelBone = cr.ComponentCreatureModel.Model.FindBone("Hand2");
		Matrix matrix = cr.ComponentCreatureModel.AbsoluteBoneTransformsForCamera[modelBone.Index] * camera.InvertedViewMatrix;
		matrix.Right = Vector3.Normalize(matrix.Right);
		matrix.Up = Vector3.Normalize(matrix.Up);
		matrix.Forward = Vector3.Normalize(matrix.Forward);
		Vector3 inHandOffset = fishingRodBlock.InHandOffset;
		Matrix m = Matrix.CreateRotationY(MathUtils.DegToRad(fishingRodBlock.InHandRotation.Y)) * Matrix.CreateRotationZ(MathUtils.DegToRad(fishingRodBlock.InHandRotation.Z)) * Matrix.CreateRotationX(MathUtils.DegToRad(fishingRodBlock.InHandRotation.X)) * Matrix.CreateTranslation(inHandOffset) * Matrix.CreateTranslation(new Vector3(0.05f, -0.19f, -0.8f) * (cr.ComponentBody.BoxSize.Y / 1.77f)) * matrix;
		return Vector3.Transform(fishingRodBlock.m_lineOffsets[num], m);
	}

	private void SetRodTension(ComponentCreature owner, float massKg, bool hasFish)
	{
		if (owner == null)
		{
			return;
		}
		ComponentMiner componentMiner = owner.Entity.FindComponent<ComponentMiner>();
		if (componentMiner == null || componentMiner.Inventory == null)
		{
			return;
		}
		int activeSlotIndex = componentMiner.Inventory.ActiveSlotIndex;
		if (activeSlotIndex < 0)
		{
			return;
		}
		int slotValue = componentMiner.Inventory.GetSlotValue(activeSlotIndex);
		if (Terrain.ExtractContents(slotValue) != 395)
		{
			return;
		}
		int num = Terrain.ExtractData(slotValue);
		int num2 = num & 3;
		int num3 = 0;
		if (hasFish)
		{
			if (massKg >= NearImpossibleMass)
			{
				num3 = 2;
			}
			else if (massKg >= MidMassThreshold)
			{
				num3 = 1;
			}
		}
		if (num3 != num2)
		{
			componentMiner.Inventory.RemoveSlotItems(activeSlotIndex, 1);
			componentMiner.Inventory.AddSlotItems(activeSlotIndex, Terrain.MakeBlockValue(395, 0, (num & -4) | num3), 1);
		}
	}

	private Vector3 GetRodTipLeft(ComponentCreature cr, int rodData, Camera camera)
	{
		FishingRodBlock fishingRodBlock = (FishingRodBlock)BlocksManager.Blocks[395];
		int num = rodData & 3;
		ModelBone modelBone = cr.ComponentCreatureModel.Model.FindBone("Hand1");
		Matrix matrix = cr.ComponentCreatureModel.AbsoluteBoneTransformsForCamera[modelBone.Index] * camera.InvertedViewMatrix;
		matrix.Right = Vector3.Normalize(matrix.Right);
		matrix.Up = Vector3.Normalize(matrix.Up);
		matrix.Forward = Vector3.Normalize(matrix.Forward);
		Vector3 inHandOffset = fishingRodBlock.InHandOffset;
		Matrix m = Matrix.CreateRotationY(MathUtils.DegToRad(fishingRodBlock.InHandRotation.Y)) * Matrix.CreateRotationZ(MathUtils.DegToRad(fishingRodBlock.InHandRotation.Z)) * Matrix.CreateRotationX(MathUtils.DegToRad(fishingRodBlock.InHandRotation.X)) * Matrix.CreateTranslation(inHandOffset) * Matrix.CreateTranslation(new Vector3(-0.05f, -0.19f, -0.8f) * (cr.ComponentBody.BoxSize.Y / 1.77f)) * matrix;
		Vector3 v = fishingRodBlock.m_lineOffsets[num];
		v.X = 0f - v.X;
		return Vector3.Transform(v, m);
	}

	private void ApplyRopeConstraint(FishingFloat f, ComponentBody fishBody, ComponentCreature owner, Vector3 handPos, float dt)
	{
		if (owner == null || fishBody == null)
		{
			return;
		}
		Vector3 vector = fishBody.Matrix.Translation + f.BodyOffset - handPos;
		float num = vector.Length();
		if (num <= MaxLineLength || num <= 1E-05f)
		{
			return;
		}
		Vector3 vector2 = vector / MathUtils.Max(num, 1E-05f);
		float num2 = num - MaxLineLength;
		float playerMassKg = PlayerMassKg;
		float num3 = MathUtils.Max(fishBody.Mass, 0.1f);
		float num4 = playerMassKg + num3;
		float num5 = num3 / num4;
		float num6 = playerMassKg / num4;
		float num7 = MathUtils.Clamp((num3 - PivotFishMassKg) / MathUtils.Max(1f, PivotFishMassKg), -1f, 1f);
		float num8 = MathUtils.Min(num2, RopeSnapStrength * dt);
		float num9 = 1f + 0.6f * MathUtils.Max(num7, 0f);
		Vector3 vector3 = vector2 * (num8 * num9);
		owner.ComponentBody.ApplyDirectMove(+vector3 * num5);
		fishBody.ApplyDirectMove(-vector3 * num6);
		float num10 = Vector3.Dot(owner.ComponentBody.Velocity, -vector2);
		if (num10 > 0f)
		{
			owner.ComponentBody.Velocity -= num10 * -vector2;
		}
		float num11 = Vector3.Dot(fishBody.Velocity, vector2);
		if (num11 > 0f)
		{
			float num12 = ((num7 <= 0f) ? 1f : MathUtils.Lerp(1f, 0.2f, num7));
			fishBody.Velocity -= num11 * vector2 * num12;
		}
		ComponentLocomotion componentLocomotion = owner.Entity.FindComponent<ComponentLocomotion>();
		if (componentLocomotion != null)
		{
			float magnitude = MathUtils.Saturate(num2 / 3f) * num5 * (1f + 2f * MathUtils.Max(0f - num7, 0f));
			ApplyPullOrder(componentLocomotion, owner.ComponentBody, vector2, magnitude);
		}
		Entity entity = fishBody.Entity;
		if (entity != null)
		{
			ComponentLocomotion componentLocomotion2 = entity.FindComponent<ComponentLocomotion>();
			if (componentLocomotion2 != null)
			{
				float magnitude2 = MathUtils.Saturate(num2 / 3f) * num6 * (1f + 2f * MathUtils.Max(num7, 0f));
				ApplyPullOrder(componentLocomotion2, fishBody, -vector2, magnitude2);
			}
		}
	}

	private void ApplyPullOrder(ComponentLocomotion loc, ComponentBody body, Vector3 worldDir, float magnitude)
	{
		magnitude = MathUtils.Saturate(magnitude);
		if (magnitude <= 0f || worldDir.LengthSquared() <= 1E-06f)
		{
			return;
		}
		if (body.ImmersionFactor > 0.5f)
		{
			Vector3 vector = Vector3.Normalize(worldDir) * magnitude;
			if (loc.SwimOrder.HasValue)
			{
				Vector3 vector2 = vector + loc.SwimOrder.Value;
				float num = vector2.Length();
				loc.SwimOrder = ((num > 1f && num > 1E-06f) ? (vector2 / num) : vector2);
			}
			else
			{
				loc.SwimOrder = vector;
			}
			return;
		}
		if (loc.IsCreativeFlyEnabled)
		{
			Vector3 vector3 = new Vector3(body.Matrix.Right.X, 0f, body.Matrix.Right.Z);
			if (vector3.LengthSquared() > 1E-06f)
			{
				vector3 = Vector3.Normalize(vector3);
			}
			ComponentPlayer player = body.Player;
			Camera camera = ((player != null && player.GameWidget != null) ? player.GameWidget.ActiveCamera : null);
			Vector3 vector4 = ((camera == null) ? Vector3.Normalize(body.Matrix.Forward + 0.1f * Vector3.UnitY) : Vector3.Normalize(new Vector3(camera.ViewDirection.X, camera.ViewDirection.Y + 0.1f, camera.ViewDirection.Z)));
			Vector3 v = Vector3.Normalize(new Vector3(vector4.X, 0f, vector4.Z));
			Vector3 v2 = Vector3.Normalize(new Vector3(worldDir.X, 0f, worldDir.Z));
			float x = MathUtils.Clamp(Vector3.Dot(v2, vector3), -1f, 1f) * magnitude;
			float y = MathUtils.Clamp(Vector3.Dot(v2, v), -1f, 1f) * magnitude;
			float y2 = MathUtils.Clamp(worldDir.Y, -1f, 1f) * magnitude;
			Vector2 vector5 = new Vector2(x, y);
			if (loc.WalkOrder.HasValue)
			{
				Vector2 vector6 = vector5 + loc.WalkOrder.Value;
				float num2 = vector6.LengthSquared();
				loc.WalkOrder = ((num2 > 1f && num2 > 1E-06f) ? (vector6 / MathUtils.Sqrt(num2)) : vector6);
			}
			else
			{
				loc.WalkOrder = vector5;
			}
			Vector3 vector7 = new Vector3(0f, y2, 0f);
			if (loc.FlyOrder.HasValue)
			{
				Vector3 vector8 = vector7 + loc.FlyOrder.Value;
				float num3 = vector8.Length();
				loc.FlyOrder = ((num3 > 1f && num3 > 1E-06f) ? (vector8 / num3) : vector8);
			}
			else
			{
				loc.FlyOrder = vector7;
			}
			return;
		}
		Vector3 vector9 = new Vector3(body.Matrix.Right.X, 0f, body.Matrix.Right.Z);
		Vector3 vector10 = new Vector3(body.Matrix.Forward.X, 0f, body.Matrix.Forward.Z);
		if (vector9.LengthSquared() > 1E-06f)
		{
			vector9 = Vector3.Normalize(vector9);
		}
		if (vector10.LengthSquared() > 1E-06f)
		{
			vector10 = Vector3.Normalize(vector10);
		}
		Vector3 v3 = new Vector3(worldDir.X, 0f, worldDir.Z);
		if (!(v3.LengthSquared() <= 1E-06f))
		{
			v3 = Vector3.Normalize(v3);
			float x2 = MathUtils.Clamp(Vector3.Dot(v3, vector9), -1f, 1f) * magnitude;
			float y3 = MathUtils.Clamp(Vector3.Dot(v3, vector10), -1f, 1f) * magnitude;
			Vector2 vector11 = new Vector2(x2, y3);
			if (loc.WalkOrder.HasValue)
			{
				Vector2 vector12 = vector11 + loc.WalkOrder.Value;
				float num4 = vector12.LengthSquared();
				loc.WalkOrder = ((num4 > 1f && num4 > 1E-06f) ? (vector12 / MathUtils.Sqrt(num4)) : vector12);
			}
			else
			{
				loc.WalkOrder = vector11;
			}
		}
	}

	private void MarkRopeImpulse(FishingFloat f)
	{
		double gameTime = m_subsystemTime.GameTime;
		m_ropeImpulseUntil[f] = gameTime + (double)VisualTensionImpulseDuration;
		m_ropeRelaxUntil[f] = gameTime + (double)VisualTensionImpulseDuration + (double)VisualTensionRelaxDuration;
	}

	private void ClearRopeTimers(FishingFloat f)
	{
		m_ropeImpulseUntil.Remove(f);
		m_ropeRelaxUntil.Remove(f);
	}

	private void TryTriggerFishStruggle(FishingFloat f, float seconds)
	{
		if (f == null)
		{
			return;
		}
		ComponentBody stuckBody = f.StuckBody;
		if (stuckBody != null && stuckBody.Entity != null)
		{
			ComponentFishStruggleBehavior componentFishStruggleBehavior = stuckBody.Entity.FindComponent<ComponentFishStruggleBehavior>();
			if (componentFishStruggleBehavior != null)
			{
				Vector3 vector = f.Owner?.ComponentCreatureModel.EyePosition ?? f.Position;
				float num = MathUtils.Saturate(Vector3.Distance(f.Position, vector) / MathUtils.Max(1f, MaxLineLength));
				float num2 = MathUtils.Saturate(0.5f + 0.5f * num);
				ComponentFishStruggleBehavior.AttemptQuality quality = ((!(num2 < 0.6f)) ? ((num2 < 0.85f) ? ComponentFishStruggleBehavior.AttemptQuality.Yellow : ComponentFishStruggleBehavior.AttemptQuality.Red) : ComponentFishStruggleBehavior.AttemptQuality.Green);
				componentFishStruggleBehavior.ActivateStruggle(vector, num2, seconds, quality, MaxLineLength);
			}
		}
	}

	public FishingFloat FindFloatByBody(ComponentBody body)
	{
		if (body == null)
		{
			return null;
		}
		for (int num = m_floats.Count - 1; num >= 0; num--)
		{
			FishingFloat fishingFloat = m_floats[num];
			if (fishingFloat != null && fishingFloat.StuckBody == body)
			{
				return fishingFloat;
			}
		}
		return null;
	}

	public float GetFatigueForBody(ComponentBody body)
	{
		FishingFloat fishingFloat = FindFloatByBody(body);
		if (fishingFloat == null)
		{
			return 0f;
		}
		return MathUtils.Saturate(fishingFloat.Fatigue);
	}

	private bool HasBait(FishingFloat f)
	{
		if (f != null && f.BobberValue != 0)
		{
			return BobberBlock.HasBait(Terrain.ExtractData(f.BobberValue));
		}
		return false;
	}

	private void RemoveBait(FishingFloat f)
	{
		if (f != null && HasBait(f))
		{
			int contents = Terrain.ExtractContents(f.BobberValue);
			int data = Terrain.ExtractData(f.BobberValue);
			data = BobberBlock.SetBait(data, 0, 0);
			f.BobberValue = Terrain.MakeBlockValue(contents, 0, data);
			if (CommonLib.WorkType != WorkType.Client)
			{
				CommonLib.Net.QueuePackage(new FishingFloatPackage(FishingFloatPackage.FishingFloatAction.Sync, f));
			}
		}
	}

	private static float GetBaitBaseSeconds(int strength)
	{
		return strength switch
		{
			1 => 6f, 
			2 => 3f, 
			3 => 10f, 
			4 => 25f, 
			_ => 10f, 
		};
	}

	private static int GetBaitStrengthFromValue(int bobberValue)
	{
		if (bobberValue == 0)
		{
			return 0;
		}
		int bait = BobberBlock.GetBait(Terrain.ExtractData(bobberValue));
		if (bait == 0)
		{
			return 0;
		}
		int index = BobberBaitInfo.GetIndex(bait);
		if (index <= 1)
		{
			return 1;
		}
		return index switch
		{
			2 => 2, 
			3 => 3, 
			_ => 4, 
		};
	}

	private void InitHookState(FishingFloat f)
	{
		double gameTime = m_subsystemTime.GameTime;
		if (HasBait(f))
		{
			float baitBaseSeconds = GetBaitBaseSeconds(GetBaitStrengthFromValue(f.BobberValue));
			m_baitDisappearAt[f] = gameTime + (double)baitBaseSeconds;
		}
		else
		{
			m_baitDisappearAt.Remove(f);
		}
		m_surpriseUntil[f] = gameTime + 1.5;
		float num = MathUtils.Saturate((((f.StuckBody != null) ? f.StuckBody.Mass : 0f) - LightMassKg) / MathUtils.Max(1f, NearImpossibleMass - LightMassKg));
		int mGSeed = GetMGSeed(f);
		float num2 = 8f + (1f - num) * 10f;
		float num3 = MathUtils.Lerp(-2f, 2f, Hash01(mGSeed, 70));
		float num4 = MathUtils.Clamp(num2 + num3, 8f, 24f);
		m_detachAt[f] = gameTime + (double)num4;
	}

	private void CleanupHookState(FishingFloat f)
	{
		m_baitDisappearAt.Remove(f);
		m_surpriseUntil.Remove(f);
		m_detachAt.Remove(f);
	}

	private void TickBaitTimer(FishingFloat f)
	{
		if (f != null && !IsDeadOrVehicle(f.StuckBody))
		{
			double gameTime = m_subsystemTime.GameTime;
			if (HasBait(f) && !m_baitDisappearAt.TryGetValue(f, out var _))
			{
				float baitBaseSeconds = GetBaitBaseSeconds(GetBaitStrengthFromValue(f.BobberValue));
				m_baitDisappearAt[f] = gameTime + (double)baitBaseSeconds;
			}
			if (m_baitDisappearAt.TryGetValue(f, out var value2) && gameTime >= value2 && HasBait(f))
			{
				RemoveBait(f);
			}
		}
	}

	private bool IsInSurprise(FishingFloat f)
	{
		if (m_surpriseUntil.TryGetValue(f, out var value))
		{
			return m_subsystemTime.GameTime < value;
		}
		return false;
	}

	private bool TryAutoDetachIfExpired(FishingFloat f, ComponentCreature owner)
	{
		if (f == null || f.StuckBody == null)
		{
			return false;
		}
		if (IsDeadOrVehicle(f.StuckBody))
		{
			return false;
		}
		if (!m_detachAt.TryGetValue(f, out var xElement))
		{
			return false;
		}
		if (m_subsystemTime.GameTime < xElement)
		{
			return false;
		}
		f.StuckBody = null;
		ClearMiniGame(f);
		ResetPlayerSightColor(owner);
		CleanupHookState(f);
		if (CommonLib.WorkType != WorkType.Client)
		{
			CommonLib.Net.QueuePackage(new FishingFloatPackage(FishingFloatPackage.FishingFloatAction.Detach, f));
		}
		return true;
	}

	private int GetMGSeed(FishingFloat f)
	{
		int a = Terrain.ToCell(f.Position.X);
		int b = Terrain.ToCell(f.Position.Y);
		int c = Terrain.ToCell(f.Position.Z);
		int num = (int)MathUtils.Round(((f.StuckBody != null) ? f.StuckBody.Mass : 0f) * 10f);
		int num2 = 0;
		if (f.BobberValue != 0)
		{
			num2 = BobberBlock.GetBait(Terrain.ExtractData(f.BobberValue));
		}
		return HashInts(a, b, c, (num << 1) ^ (num2 * 1315423911));
	}

	private static int HashInts(int a, int b, int c, int d)
	{
		return (((((((-2128831035 ^ a) * 16777619) ^ b) * 16777619) ^ c) * 16777619) ^ d) * 16777619;
	}

	private static float Hash01(int h)
	{
		int num = (h ^ (h >>> 16)) * 2146121005;
		int num2 = (num ^ (num >>> 15)) * -2073254261;
		return (float)((num2 ^ (num2 >>> 16)) & 0x7FFFFFFF) / 2.1474836E+09f;
	}

	private float Hash01(int seed, int salt)
	{
		return Hash01(HashInts(seed, salt, -1640531527, -2048144777));
	}

	private static bool IsHookingForbiddenInRegion(ComponentCreature owner, Vector3 pos)
	{
		ComponentPlayer componentPlayer = null;
		SubsystemGameInfo subsystemGameInfo = null;
		if (owner != null)
		{
			Entity entity = owner.Entity;
			if (entity != null)
			{
				componentPlayer = entity.FindComponent<ComponentPlayer>();
				subsystemGameInfo = entity.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
			}
		}
		int x = Terrain.ToCell(pos.X);
		int z = Terrain.ToCell(pos.Z);
		bool flag = SubsystemBedrockBlockBehavior.CheckIsInTerritoriy(x, z, out var territoriy);
		if (subsystemGameInfo != null && subsystemGameInfo.WorldSettings.RestrictWorldInteraction && componentPlayer != null && !componentPlayer.PlayerData.ServerManager && !flag)
		{
			return true;
		}
		if (!flag)
		{
			return false;
		}
		if (!territoriy.AllowBlockBehavior)
		{
			return true;
		}
		if (componentPlayer == null)
		{
			return true;
		}
		PlayerData playerData = componentPlayer.PlayerData;
		if (!(territoriy.OwnerGuid == playerData.PlayerGUID))
		{
			if (playerData.Friends.Contains(territoriy.OwnerGuid))
			{
				return !territoriy.ApplyToFirend;
			}
			return true;
		}
		return false;
	}

	private bool ShouldDamageRod(ComponentCreature owner)
	{
		if (owner == null)
		{
			return false;
		}
		ComponentPlayer componentPlayer = owner.Entity.FindComponent<ComponentPlayer>();
		if (componentPlayer == null)
		{
			return false;
		}
		if (!m_rodDamageAttemptCounter.TryGetValue(componentPlayer, out var value))
		{
			value = 0;
		}
		if (!m_rodDamageThreshold.TryGetValue(componentPlayer, out var value2) || value2 <= 0)
		{
			value2 = m_random.Int(10, 15);
			m_rodDamageThreshold[componentPlayer] = value2;
		}
		value++;
		bool flag = value >= value2;
		if (flag)
		{
			value = 0;
			m_rodDamageThreshold[componentPlayer] = m_random.Int(10, 15);
		}
		m_rodDamageAttemptCounter[componentPlayer] = value;
		return flag;
	}

	private void ApplyRodDamage(ComponentCreature owner)
	{
		if (owner == null)
		{
			return;
		}
		ComponentMiner componentMiner = owner.Entity.FindComponent<ComponentMiner>();
		if (componentMiner == null || componentMiner.Inventory == null)
		{
			return;
		}
		int activeSlotIndex = componentMiner.Inventory.ActiveSlotIndex;
		if (activeSlotIndex >= 0 && Terrain.ExtractContents(componentMiner.Inventory.GetSlotValue(activeSlotIndex)) == 395 && componentMiner.Inventory.GetSlotCount(activeSlotIndex) > 0)
		{
			componentMiner.DamageActiveTool(1);
		}
		else
		{
			for (int i = 0; i < componentMiner.Inventory.SlotsCount; i++)
			{
				int slotValue = componentMiner.Inventory.GetSlotValue(i);
				if (componentMiner.Inventory.GetSlotCount(i) > 0 && Terrain.ExtractContents(slotValue) == 395)
				{
					int damage = BlocksManager.Blocks[395].GetDamage(slotValue);
					int value = BlocksManager.Blocks[395].SetDamage(slotValue, damage + 1);
					componentMiner.Inventory.RemoveSlotItems(i, 1);
					componentMiner.Inventory.AddSlotItems(i, value, 1);
					break;
				}
			}
		}
		FishingFloat fishingFloat = FindFloat(owner);
		if (fishingFloat == null)
		{
			return;
		}
		bool flag = false;
		ComponentHumanModel componentHumanModel = owner.Entity.FindComponent<ComponentHumanModel>();
		if (componentHumanModel != null && componentHumanModel.OffhandRodValue != 0 && Terrain.ExtractContents(componentHumanModel.OffhandRodValue) == 395)
		{
			flag = true;
		}
		if (!flag && componentMiner != null && componentMiner.Inventory != null)
		{
			int slotsCount = componentMiner.Inventory.SlotsCount;
			for (int j = 0; j < slotsCount; j++)
			{
				if (flag)
				{
					break;
				}
				if (componentMiner.Inventory.GetSlotCount(j) > 0 && Terrain.ExtractContents(componentMiner.Inventory.GetSlotValue(j)) == 395)
				{
					flag = true;
				}
			}
		}
		if (flag)
		{
			return;
		}
		if (fishingFloat.StuckBody != null)
		{
			fishingFloat.StuckBody = null;
			ClearMiniGame(fishingFloat);
			ResetPlayerSightColor(owner);
			if (CommonLib.WorkType != WorkType.Client)
			{
				CommonLib.Net.QueuePackage(new FishingFloatPackage(FishingFloatPackage.FishingFloatAction.Detach, fishingFloat));
			}
		}
		fishingFloat.IsRetracting = true;
		fishingFloat.ReelImpulseTimeLeft = 0f;
	}

	private Sound EnsureLoop(Dictionary<FishingFloat, Sound> map, FishingFloat f, string path)
	{
		if (!map.TryGetValue(f, out var value) || value.State == SoundState.Disposed)
		{
			value = m_subsystemAudio.CreateSound(path);
			value.IsLooped = true;
			value.Volume = 0f;
			map[f] = value;
		}
		return value;
	}

	private static void FadeLoop(Sound s, float target, float dt)
	{
		float f = MathUtils.Saturate(4f * dt);
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

	private void StopDispose(Dictionary<FishingFloat, Sound> map, FishingFloat f)
	{
		if (map.TryGetValue(f, out var value))
		{
			value.Stop();
			value.Dispose();
			map.Remove(f);
		}
	}

	private void CleanupFloatSounds(FishingFloat f)
	{
		StopDispose(m_sFastUnwind, f);
		StopDispose(m_sFastRewind, f);
		StopDispose(m_sUnwind, f);
		StopDispose(m_sRewind, f);
		StopDispose(m_sCreakHeavy, f);
		m_lastBlockDist.Remove(f);
	}

	private void UpdateLineStepSounds(FishingFloat f, ComponentCreature owner, float dt)
	{
		if (owner == null || f.StuckBody == null)
		{
			if (m_sUnwind.TryGetValue(f, out var value))
			{
				FadeLoop(value, 0f, dt);
			}
			if (m_sRewind.TryGetValue(f, out var value2))
			{
				FadeLoop(value2, 0f, dt);
			}
			return;
		}
		float num = Vector3.Distance(owner.ComponentCreatureModel.EyePosition, f.Position);
		if (!m_lastBlockDist.TryGetValue(f, out var value3))
		{
			value3 = num;
		}
		m_lastBlockDist[f] = num;
		float num2 = ((dt > 1E-05f) ? ((num - value3) / dt) : 0f);
		float num3 = MathUtils.Abs(num2);
		float num4 = 0.05f;
		float num5 = 0f;
		if (num3 > num4)
		{
			num5 = MathUtils.Saturate((num3 - num4) / 2f);
		}
		float target = 0.15f + 0.7f * num5;
		if (num5 > 0f)
		{
			if (num2 > 0f)
			{
				Sound sound = EnsureLoop(m_sUnwind, f, "Audio/Fishing/LineUnwinding");
				if (sound.State != SoundState.Playing)
				{
					sound.Pitch = AudioManager.ToEnginePitch(m_random.Float(-0.2f, 0.2f));
				}
				FadeLoop(sound, target, dt);
				if (m_sRewind.TryGetValue(f, out var value4))
				{
					FadeLoop(value4, 0f, dt);
				}
			}
			else
			{
				Sound sound2 = EnsureLoop(m_sRewind, f, "Audio/Fishing/LineRewind");
				if (sound2.State != SoundState.Playing)
				{
					sound2.Pitch = AudioManager.ToEnginePitch(m_random.Float(-0.2f, 0.2f));
				}
				FadeLoop(sound2, target, dt);
				if (m_sUnwind.TryGetValue(f, out var value5))
				{
					FadeLoop(value5, 0f, dt);
				}
			}
		}
		else
		{
			if (m_sUnwind.TryGetValue(f, out var value6))
			{
				FadeLoop(value6, 0f, dt);
			}
			if (m_sRewind.TryGetValue(f, out var value7))
			{
				FadeLoop(value7, 0f, dt);
			}
		}
	}

	private void UpdateHeavyCreak(FishingFloat f, float massKg, float dt)
	{
		Sound value;
		if (massKg >= NearImpossibleMass)
		{
			Sound sound = EnsureLoop(m_sCreakHeavy, f, "Audio/Fishing/Creak");
			if (sound.State != SoundState.Playing)
			{
				sound.Pitch = AudioManager.ToEnginePitch(m_random.Float(-0.2f, 0.2f));
			}
			FadeLoop(sound, 0.6f, dt);
		}
		else if (m_sCreakHeavy.TryGetValue(f, out value))
		{
			FadeLoop(value, 0f, dt);
		}
	}

	private bool IsDeadOrVehicle(ComponentBody body)
	{
		if (body == null || body.Entity == null)
		{
			return false;
		}
		switch (body.Entity.ValuesDictionary.DatabaseObject.Name)
		{
		case "Boat":
		case "Raft":
		case "Sailing_Boat":
			return true;
		default:
		{
			ComponentHealth componentHealth = body.Entity.FindComponent<ComponentHealth>();
			if (componentHealth != null)
			{
				return componentHealth.Health <= 0f;
			}
			return false;
		}
		}
	}

	private bool IsOwnerRidingHookedBoat(ComponentCreature owner, FishingFloat f)
	{
		if (owner == null || f == null || f.StuckBody == null || f.StuckBody.Entity == null)
		{
			return false;
		}
		Entity entity = f.StuckBody.Entity;
		if (entity.FindComponent<ComponentBoat>() == null)
		{
			return false;
		}
		Entity entity2 = owner.Entity;
		if (entity2 == null)
		{
			return false;
		}
		ComponentRider componentRider = entity2.FindComponent<ComponentRider>();
		if (componentRider != null && componentRider.Mount != null)
		{
			return componentRider.Mount.Entity == entity;
		}
		return false;
	}
}
