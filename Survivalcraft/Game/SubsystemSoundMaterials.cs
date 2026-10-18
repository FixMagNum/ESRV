using System;
using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemSoundMaterials : Subsystem
{
	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemAudio m_subsystemAudio;

	public Random m_random = new Random();

	public ValuesDictionary m_impactsSoundsValuesDictionary;

	public ValuesDictionary m_footstepSoundsValuesDictionary;

	public void PlayImpactSound(int value, Vector3 position, float loudnessMultiplier)
	{
		PlayImpactSound(value, position, loudnessMultiplier, 0f);
	}

	public bool PlayFootstepSound(ComponentCreature componentCreature, float loudnessMultiplier)
	{
		float size = componentCreature.ComponentBody.Size;
		float componentPlayer = MathUtils.Clamp(1f - size, -1.2f, 1.2f);
		float num = MathUtils.Clamp(size, 0.4f, 2.5f);
		string footstepSoundMaterialName = GetFootstepSoundMaterialName(componentCreature);
		if (!string.IsNullOrEmpty(footstepSoundMaterialName))
		{
			string text = componentCreature.ComponentCreatureSounds.ValuesDictionary.GetValue<ValuesDictionary>("CustomFootstepSounds").GetValue<string>(footstepSoundMaterialName, null) ?? m_footstepSoundsValuesDictionary.GetValue<string>(footstepSoundMaterialName, null);
			if (!string.IsNullOrEmpty(text))
			{
				float num2 = m_random.Float(0f, 0.8f) + componentPlayer;
				ComponentPlayer componentPlayer2 = componentCreature as ComponentPlayer;
				if (componentPlayer2 != null)
				{
					ComponentClothing componentClothing = componentPlayer2.Entity.FindComponent<ComponentClothing>();
					if (componentClothing != null)
					{
						bool flag = false;
						foreach (ClothingSlot value4 in Enum.GetValues(typeof(ClothingSlot)))
						{
							foreach (int item in componentClothing.GetClothes(value4))
							{
								ClothingData clothingData = BlocksManager.Blocks[Terrain.ExtractContents(item)].GetClothingData(Terrain.ExtractData(item));
								if (clothingData != null && clothingData.IsExplodes)
								{
									string key = "IsExplodes";
									string text2 = componentCreature.ComponentCreatureSounds.ValuesDictionary.GetValue<ValuesDictionary>("CustomFootstepSounds").GetValue<string>(key, null) ?? m_footstepSoundsValuesDictionary.GetValue<string>(key, null);
									if (!string.IsNullOrEmpty(text2))
									{
										float pitch = m_random.Float(-0.2f, 0.2f);
										m_subsystemAudio.PlayRandomSound(text2, 0.55f * loudnessMultiplier, pitch, componentCreature.ComponentBody.Position, 2f * loudnessMultiplier, autoDelay: true);
									}
									flag = true;
									break;
								}
							}
							if (flag)
							{
								break;
							}
						}
					}
				}
				m_subsystemAudio.PlayRandomSound(text, 0.75f * loudnessMultiplier * num, num2, componentCreature.ComponentBody.Position, 2f * loudnessMultiplier, autoDelay: true);
				if (componentPlayer2 != null && componentPlayer2.ComponentVitalStats.Wetness > 0f)
				{
					string value = m_footstepSoundsValuesDictionary.GetValue<string>("Squishy", null);
					if (!string.IsNullOrEmpty(value))
					{
						float volume = 4.7f * loudnessMultiplier * MathUtils.Pow(componentPlayer2.ComponentVitalStats.Wetness, 4f) * num;
						m_subsystemAudio.PlayRandomSound(value, volume, num2, componentCreature.ComponentBody.Position, 2f * loudnessMultiplier, autoDelay: true);
					}
				}
				if (componentPlayer2 != null)
				{
					ComponentClothing componentClothing2 = componentPlayer2.Entity.FindComponent<ComponentClothing>();
					float num3 = 0f;
					if (componentClothing2 != null)
					{
						foreach (ClothingSlot value5 in Enum.GetValues(typeof(ClothingSlot)))
						{
							foreach (int item2 in componentClothing2.GetClothes(value5))
							{
								ClothingData clothingData2 = BlocksManager.Blocks[Terrain.ExtractContents(item2)].GetClothingData(Terrain.ExtractData(item2));
								if (clothingData2 != null)
								{
									num3 += clothingData2.Weight;
								}
							}
						}
					}
					float num4 = MathUtils.Clamp(num3 / 8f, 0f, 1f);
					if (num4 > 0f)
					{
						string value2 = m_footstepSoundsValuesDictionary.GetValue<string>("ArmorWeight", null);
						if (!string.IsNullOrEmpty(value2))
						{
							m_subsystemAudio.PlayRandomSound(value2, num4, num2, componentCreature.ComponentBody.Position, 2f * num4, autoDelay: true);
						}
					}
				}
				if (size > 1f)
				{
					string value3 = m_footstepSoundsValuesDictionary.GetValue<string>("GiantStomp", null);
					if (!string.IsNullOrEmpty(value3))
					{
						float num5 = MathUtils.Clamp((size - 1f) * 0.8f, 0f, 2f);
						float pitch2 = num2 - 0.3f;
						m_subsystemAudio.PlayRandomSound(value3, num5 * loudnessMultiplier, pitch2, componentCreature.ComponentBody.Position, 4f * loudnessMultiplier * num5, autoDelay: true);
					}
				}
				return true;
			}
		}
		return false;
	}

	public string GetFootstepSoundMaterialName(ComponentCreature componentCreature)
	{
		Vector3 position = componentCreature.ComponentBody.Position;
		if (componentCreature.ComponentBody.ImmersionDepth > 0.2f && componentCreature.ComponentBody.ImmersionFocalorsFluidBlock is FocalorsWaterBlock)
		{
			return "Water";
		}
		if (componentCreature.ComponentLocomotion.LadderValue.HasValue)
		{
			return Terrain.ExtractContents(componentCreature.ComponentLocomotion.LadderValue.Value) switch
			{
				59 => "WoodenLadder", 
				310 => "StrawLadder", 
				_ => "MetalLadder", 
			};
		}
		int cellValue = m_subsystemTerrain.Terrain.GetCellValue(Terrain.ToCell(position.X), Terrain.ToCell(position.Y + 0.1f), Terrain.ToCell(position.Z));
		int num = Terrain.ExtractContents(cellValue);
		string soundMaterialName = BlocksManager.Blocks[num].GetSoundMaterialName(m_subsystemTerrain, cellValue);
		if (string.IsNullOrEmpty(soundMaterialName) && componentCreature.ComponentBody.StandingOnValue.HasValue)
		{
			soundMaterialName = BlocksManager.Blocks[Terrain.ExtractContents(componentCreature.ComponentBody.StandingOnValue.Value)].GetSoundMaterialName(m_subsystemTerrain, componentCreature.ComponentBody.StandingOnValue.Value);
		}
		if (!string.IsNullOrEmpty(soundMaterialName))
		{
			return soundMaterialName;
		}
		return string.Empty;
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_impactsSoundsValuesDictionary = valuesDictionary.GetValue<ValuesDictionary>("ImpactSounds");
		m_footstepSoundsValuesDictionary = valuesDictionary.GetValue<ValuesDictionary>("FootstepSounds");
	}

	public void PlayImpactSound(int value, Vector3 position, float loudnessMultiplier, float pitchOffset)
	{
		int num = Terrain.ExtractContents(value);
		string soundMaterialName = BlocksManager.Blocks[num].GetSoundMaterialName(m_subsystemTerrain, value);
		if (!string.IsNullOrEmpty(soundMaterialName))
		{
			string value2 = m_impactsSoundsValuesDictionary.GetValue<string>(soundMaterialName, null);
			if (!string.IsNullOrEmpty(value2))
			{
				float pitch = m_random.Float(-0.2f, 0.2f) + pitchOffset;
				m_subsystemAudio.PlayRandomSound(value2, 0.5f * loudnessMultiplier, pitch, position, 5f * loudnessMultiplier, autoDelay: true);
			}
		}
	}
}
