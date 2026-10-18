using System.Collections.Generic;
using Engine;
using Game.NetWork;
using TemplatesDatabase;

namespace Game;

public class SubsystemMusketBlockBehavior : SubsystemBlockBehavior, IUpdateable
{
	private class MusketState
	{
		public int LastValue;

		public double AimStartTime;
	}

	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemTime m_subsystemTime;

	public SubsystemProjectiles m_subsystemProjectiles;

	public SubsystemParticles m_subsystemParticles;

	public SubsystemAudio m_subsystemAudio;

	public SubsystemNoise m_subsystemNoise;

	public static string fName;

	public Random m_random;

	public Dictionary<ComponentMiner, double> m_aimStartTimes;

	public SubsystemBodies m_subsystemBodies;

	public SubsystemPlayers m_subsystemPlayers;

	private Dictionary<ComponentPlayer, MusketState> m_activeShooters = new Dictionary<ComponentPlayer, MusketState>();

	public override int[] HandledBlocks => new int[0];

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override bool OnEditInventoryItem(IInventory inventory, int slotIndex, ComponentPlayer componentPlayer)
	{
		componentPlayer.ComponentGui.ModalPanelWidget = ((componentPlayer.ComponentGui.ModalPanelWidget == null) ? new MusketWidget(inventory, slotIndex) : null);
		return true;
	}

	public override bool OnAim(Ray3 aim, ComponentMiner componentMiner, AimState state)
	{
		IInventory inventory = componentMiner.Inventory;
		if (inventory != null)
		{
			int activeSlotIndex = inventory.ActiveSlotIndex;
			if (activeSlotIndex >= 0)
			{
				int slotValue = inventory.GetSlotValue(activeSlotIndex);
				int slotCount = inventory.GetSlotCount(activeSlotIndex);
				int num = Terrain.ExtractContents(slotValue);
				int data = Terrain.ExtractData(slotValue);
				int num2 = slotValue;
				int num3 = 0;
				if (num == 212 && slotCount > 0)
				{
					if (!m_aimStartTimes.TryGetValue(componentMiner, out var value))
					{
						value = m_subsystemTime.GameTime;
						m_aimStartTimes[componentMiner] = value;
					}
					float num4 = (float)(m_subsystemTime.GameTime - value);
					float num5 = (float)MathUtils.Remainder(m_subsystemTime.GameTime, 1000.0);
					Vector3 vector = ((componentMiner.ComponentCreature.ComponentBody.IsSneaking ? 0.01f : 0.03f) + 0.2f * MathUtils.Saturate((num4 - 2.5f) / 6f)) * new Vector3
					{
						X = SimplexNoise.OctavedNoise(num5, 2f, 3, 2f, 0.5f),
						Y = SimplexNoise.OctavedNoise(num5 + 100f, 2f, 3, 2f, 0.5f),
						Z = SimplexNoise.OctavedNoise(num5 + 200f, 2f, 3, 2f, 0.5f)
					};
					aim.Direction = Vector3.Normalize(aim.Direction + vector);
					switch (state)
					{
					case AimState.InProgress:
					{
						if (num4 >= 10f)
						{
							componentMiner.ComponentCreature.ComponentCreatureSounds.PlayMoanSound();
							return true;
						}
						if (num4 > 0.5f && !MusketBlock.GetHammerState(Terrain.ExtractData(num2)))
						{
							num2 = Terrain.MakeBlockValue(num, 0, MusketBlock.SetHammerState(Terrain.ExtractData(num2), state: true));
							m_subsystemAudio.PlaySound("Audio/HammerCock", 1f, m_random.Float(-0.1f, 0.1f), componentMiner.ComponentPlayer.ComponentCreatureModel.EyePosition, 64f, autoDelay: false);
							TriggerMusketFear(aim, componentMiner, 10f);
						}
						ComponentFirstPersonModel componentFirstPersonModel = componentMiner.Entity.FindComponent<ComponentFirstPersonModel>();
						if (componentFirstPersonModel != null)
						{
							componentMiner.ComponentPlayer?.ComponentAimingSights.ShowAimingSights(aim.Position, aim.Direction);
							componentFirstPersonModel.ItemOffsetOrder = new Vector3(-0.21f, 0.15f, 0.08f);
							componentFirstPersonModel.ItemRotationOrder = new Vector3(-0.7f, 0f, 0f);
						}
						componentMiner.ComponentCreature.ComponentCreatureModel.AimHandAngleOrder = 1.4f;
						componentMiner.ComponentCreature.ComponentCreatureModel.InHandItemOffsetOrder = new Vector3(-0.08f, -0.08f, 0.07f);
						componentMiner.ComponentCreature.ComponentCreatureModel.InHandItemRotationOrder = new Vector3(-1.7f, 0f, 0f);
						break;
					}
					case AimState.Cancelled:
						if (MusketBlock.GetHammerState(Terrain.ExtractData(num2)))
						{
							num2 = Terrain.MakeBlockValue(num, 0, MusketBlock.SetHammerState(Terrain.ExtractData(num2), state: false));
							m_subsystemAudio.PlaySound("Audio/HammerUncock", 1f, m_random.Float(-0.1f, 0.1f), componentMiner.ComponentPlayer.ComponentCreatureModel.EyePosition, 64f, autoDelay: false);
						}
						m_aimStartTimes.Remove(componentMiner);
						break;
					case AimState.Completed:
					{
						bool flag = false;
						int value2 = 0;
						int num6 = 0;
						float num7 = 0f;
						Vector3 vector2 = Vector3.Zero;
						MusketBlock.LoadState loadState = MusketBlock.GetLoadState(data);
						BulletBlock.BulletType? bulletType = MusketBlock.GetBulletType(data);
						if (MusketBlock.GetHammerState(Terrain.ExtractData(num2)))
						{
							switch (loadState)
							{
							case MusketBlock.LoadState.Empty:
								componentMiner.ComponentPlayer?.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 0), Color.White, blinking: true, playNotificationSound: false);
								break;
							case MusketBlock.LoadState.Gunpowder:
							case MusketBlock.LoadState.Wad:
								flag = true;
								componentMiner.ComponentPlayer?.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 1), Color.White, blinking: true, playNotificationSound: false);
								break;
							case MusketBlock.LoadState.Loaded:
								flag = true;
								if (bulletType == BulletBlock.BulletType.Buckshot)
								{
									value2 = Terrain.MakeBlockValue(214, 0, BulletBlock.SetBulletType(0, BulletBlock.BulletType.BuckshotBall));
									num6 = 8;
									vector2 = new Vector3(0.04f, 0.04f, 0.25f);
									num7 = 80f;
									num3 = 10;
								}
								else if (bulletType == BulletBlock.BulletType.BuckshotBall)
								{
									value2 = Terrain.MakeBlockValue(214, 0, BulletBlock.SetBulletType(0, BulletBlock.BulletType.BuckshotBall));
									num6 = 1;
									vector2 = new Vector3(0.06f, 0.06f, 0f);
									num7 = 60f;
									num3 = 10;
								}
								else if (bulletType.HasValue)
								{
									value2 = Terrain.MakeBlockValue(214, 0, BulletBlock.SetBulletType(0, bulletType.Value));
									num6 = 1;
									num7 = 120f;
									num3 = 1;
								}
								break;
							}
						}
						if (flag)
						{
							if (componentMiner.ComponentCreature.ComponentBody.ImmersionFactor > 0.4f)
							{
								m_subsystemAudio.PlaySound("Audio/MusketMisfire", 1f, m_random.Float(-0.1f, 0.1f), componentMiner.ComponentCreature.ComponentCreatureModel.EyePosition, 8f, autoDelay: true);
							}
							else
							{
								Vector3 vector3 = componentMiner.ComponentCreature.ComponentCreatureModel.EyePosition + componentMiner.ComponentCreature.ComponentBody.Matrix.Right * 0.3f - componentMiner.ComponentCreature.ComponentBody.Matrix.Up * 0.2f;
								Vector3 vector4 = Vector3.Normalize(vector3 + aim.Direction * 10f - vector3);
								Vector3 vector5 = Vector3.Normalize(Vector3.Cross(vector4, Vector3.UnitY));
								Vector3 vector6 = Vector3.Normalize(Vector3.Cross(vector4, vector5));
								for (int i = 0; i < num6; i++)
								{
									Vector3 vector7 = m_random.Float(0f - vector2.X, vector2.X) * vector5 + m_random.Float(0f - vector2.Y, vector2.Y) * vector6 + m_random.Float(0f - vector2.Z, vector2.Z) * vector4;
									Projectile projectile = m_subsystemProjectiles.FireProjectile(value2, vector3, num7 * (vector4 + vector7), Vector3.Zero, componentMiner.ComponentCreature);
									if (projectile != null)
									{
										projectile.ProjectileStoppedAction = ProjectileStoppedAction.Disappear;
									}
								}
								componentMiner.ComponentCreature.ComponentBody.ApplyImpulse(-4f * vector4);
							}
							num2 = Terrain.MakeBlockValue(Terrain.ExtractContents(num2), 0, MusketBlock.SetLoadState(Terrain.ExtractData(num2), MusketBlock.LoadState.Empty));
						}
						if (MusketBlock.GetHammerState(Terrain.ExtractData(num2)))
						{
							num2 = Terrain.MakeBlockValue(Terrain.ExtractContents(num2), 0, MusketBlock.SetHammerState(Terrain.ExtractData(num2), state: false));
							m_subsystemAudio.PlaySound("Audio/HammerRelease", 1f, m_random.Float(-0.1f, 0.1f), componentMiner.ComponentCreature.ComponentCreatureModel.EyePosition, 64f, autoDelay: false);
						}
						m_aimStartTimes.Remove(componentMiner);
						if (CommonLib.WorkType != WorkType.Client)
						{
							if (num2 != slotValue)
							{
								inventory.RemoveSlotItems(activeSlotIndex, 1);
								inventory.AddSlotItems(activeSlotIndex, num2, 1);
							}
							if (num3 > 0)
							{
								componentMiner.DamageActiveTool(num3);
							}
						}
						return true;
					}
					}
					if (CommonLib.WorkType != WorkType.Client)
					{
						if (num2 != slotValue)
						{
							inventory.RemoveSlotItems(activeSlotIndex, 1);
							inventory.AddSlotItems(activeSlotIndex, num2, 1);
						}
						if (num3 > 0)
						{
							componentMiner.DamageActiveTool(num3);
						}
					}
					else if (num2 != slotValue)
					{
						inventory.RemoveNetSlotItems(activeSlotIndex, 1);
						inventory.AddNetSlotItems(activeSlotIndex, num2, 1);
					}
				}
			}
		}
		return false;
	}

	public override int GetProcessInventoryItemCapacity(IInventory inventory, int slotIndex, int value)
	{
		int num = Terrain.ExtractContents(value);
		MusketBlock.LoadState voltage = MusketBlock.GetLoadState(Terrain.ExtractData(inventory.GetSlotValue(slotIndex)));
		if (voltage == MusketBlock.LoadState.Empty && num == 109)
		{
			return 1;
		}
		if (voltage == MusketBlock.LoadState.Gunpowder && num == 205)
		{
			return 1;
		}
		if (voltage == MusketBlock.LoadState.Wad && num == 214)
		{
			return 1;
		}
		return 0;
	}

	public override void ProcessInventoryItem(IInventory inventory, int slotIndex, int value, int count, int processCount, out int processedValue, out int processedCount)
	{
		processedValue = value;
		processedCount = count;
		if (processCount == 1)
		{
			int modelMesh = Terrain.ExtractData(inventory.GetSlotValue(slotIndex));
			MusketBlock.LoadState boneAbsoluteTransform = MusketBlock.GetLoadState(modelMesh);
			BulletBlock.BulletType? bulletType = MusketBlock.GetBulletType(modelMesh);
			switch (boneAbsoluteTransform)
			{
			case MusketBlock.LoadState.Empty:
				boneAbsoluteTransform = MusketBlock.LoadState.Gunpowder;
				bulletType = null;
				break;
			case MusketBlock.LoadState.Gunpowder:
				boneAbsoluteTransform = MusketBlock.LoadState.Wad;
				bulletType = null;
				break;
			case MusketBlock.LoadState.Wad:
			{
				boneAbsoluteTransform = MusketBlock.LoadState.Loaded;
				int data = Terrain.ExtractData(value);
				bulletType = BulletBlock.GetBulletType(data);
				break;
			}
			}
			processedValue = 0;
			processedCount = 0;
			inventory.RemoveSlotItems(slotIndex, 1);
			inventory.AddSlotItems(slotIndex, Terrain.MakeBlockValue(212, 0, MusketBlock.SetBulletType(MusketBlock.SetLoadState(modelMesh, boneAbsoluteTransform), bulletType)), 1);
		}
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemBodies = Project.FindSubsystem<SubsystemBodies>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemProjectiles = Project.FindSubsystem<SubsystemProjectiles>(throwOnError: true);
		m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemNoise = Project.FindSubsystem<SubsystemNoise>(throwOnError: true);
		m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
		base.Load(valuesDictionary);
	}

	public SubsystemMusketBlockBehavior()
	{
		m_random = new Random();
		m_aimStartTimes = new Dictionary<ComponentMiner, double>();
	}

	static SubsystemMusketBlockBehavior()
	{
		fName = "SubsystemMusketBlockBehavior";
	}

	private void TriggerMusketFear(Ray3 aim, ComponentMiner componentMiner, float radius)
	{
		Vector3 position = aim.Position;
		Vector3 v = Vector3.Normalize(aim.Direction);
		foreach (ComponentBody body in m_subsystemBodies.Bodies)
		{
			ComponentCreature componentCreature = body.Entity.FindComponent<ComponentCreature>();
			if (componentCreature == null)
			{
				continue;
			}
			Vector3 position2 = componentCreature.ComponentBody.Position;
			if (!(Vector3.Distance(position, position2) <= radius))
			{
				continue;
			}
			Vector3 vector = position2 - position;
			float num = vector.Length();
			if (num > 0.01f)
			{
				Vector3 v2 = vector / num;
				if (Vector3.Dot(v, v2) > 0.7f)
				{
					body.Entity.FindComponent<ComponentRunAwayBehavior>()?.RunAwayFrom(componentMiner.ComponentCreature.ComponentBody);
				}
			}
		}
	}

	public void Update(float dt)
	{
		foreach (ComponentPlayer componentPlayer in m_subsystemPlayers.ComponentPlayers)
		{
			if (componentPlayer.ComponentHealth.Health <= 0f)
			{
				m_activeShooters.Remove(componentPlayer);
				continue;
			}
			IInventory inventory = componentPlayer.ComponentMiner.Inventory;
			int activeSlotIndex = inventory.ActiveSlotIndex;
			int num = ((activeSlotIndex >= 0) ? inventory.GetSlotValue(activeSlotIndex) : 0);
			int num2 = Terrain.ExtractContents(num);
			int data = Terrain.ExtractData(num);
			if (num2 != 212)
			{
				m_activeShooters.Remove(componentPlayer);
				continue;
			}
			if (!m_activeShooters.TryGetValue(componentPlayer, out var value))
			{
				value = new MusketState
				{
					LastValue = num,
					AimStartTime = m_subsystemTime.GameTime
				};
				m_activeShooters[componentPlayer] = value;
			}
			int data2 = Terrain.ExtractData(value.LastValue);
			MusketBlock.LoadState loadState = MusketBlock.GetLoadState(data);
			MusketBlock.LoadState loadState2 = MusketBlock.GetLoadState(data2);
			bool hammerState = MusketBlock.GetHammerState(data);
			bool hammerState2 = MusketBlock.GetHammerState(data2);
			if (((loadState2 != MusketBlock.LoadState.Empty && loadState == MusketBlock.LoadState.Empty) & hammerState2) && !hammerState)
			{
				SpawnFireEffects(componentPlayer);
			}
			if (!hammerState2 & hammerState)
			{
				m_subsystemAudio.PlaySound("Audio/HammerCock", 1f, m_random.Float(-0.1f, 0.1f), componentPlayer.ComponentCreatureModel.EyePosition, 16f, autoDelay: true);
			}
			if (hammerState2 && !hammerState && loadState2 == MusketBlock.LoadState.Empty)
			{
				m_subsystemAudio.PlaySound("Audio/HammerRelease", 1f, m_random.Float(-0.1f, 0.1f), componentPlayer.ComponentCreatureModel.EyePosition, 16f, autoDelay: true);
			}
			value.LastValue = num;
		}
	}

	private void SpawnFireEffects(ComponentPlayer player)
	{
		Vector3 eyePosition = player.ComponentCreatureModel.EyePosition;
		Vector3 forwardVector = player.ComponentCreatureModel.EyeRotation.GetForwardVector();
		Vector3 position = eyePosition + forwardVector * 0.8f + player.ComponentBody.Matrix.Right * 0.15f - Vector3.UnitY * 0.1f;
		m_subsystemAudio.PlaySound("Audio/MusketFire", 1f, m_random.Float(-0.1f, 0.1f), eyePosition, 40f, autoDelay: true);
		m_subsystemParticles.AddParticleSystem(new GunSmokeParticleSystem(m_subsystemTerrain, position, forwardVector));
		m_subsystemNoise.MakeNoise(eyePosition, 1f, 40f);
	}
}
