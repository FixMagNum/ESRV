using System;
using Engine;
using Engine.Graphics;

namespace Game;

public class UnderwaterBubblesParticleSystem : ParticleSystem<UnderwaterBubblesParticleSystem.Particle>
{
	public class Particle : Game.Particle
	{
		public Vector3 Velocity;

		public float TimeToLive;
	}

	private SubsystemTerrain m_subsystemTerrain;

	private float m_duration;

	public float m_toGenerate;

	private Random m_random = new Random();

	private readonly float generationSpeedFactor;

	public Vector3 Position { get; set; }

	public bool IsStopped { get; set; }

	public override bool Simulate(float dt)
	{
		dt = MathUtils.Clamp(dt, 0f, 0.1f);
		m_duration += dt;
		if (m_duration > 3f)
		{
			IsStopped = true;
		}
		m_toGenerate += 2f * dt * generationSpeedFactor;
		for (int i = 0; i < Particles.Length; i++)
		{
			Particle particle = Particles[i];
			if (particle.IsActive)
			{
				particle.TimeToLive -= dt;
				if (particle.TimeToLive > 0f)
				{
					Vector3 position = particle.Position;
					Vector3 vector = particle.Velocity * dt;
					Vector3 position2 = position + vector;
					int x = Terrain.ToCell(position2.X);
					int num = Terrain.ToCell(position2.Y);
					int z = Terrain.ToCell(position2.Z);
					int cellValue = m_subsystemTerrain.Terrain.GetCellValue(x, num, z);
					int num2 = Terrain.ExtractContents(cellValue);
					Block obj = BlocksManager.Blocks[num2];
					bool flag = false;
					if (obj is FocalorsFluidBlock)
					{
						float levelHeight = FocalorsFluidBlock.GetLevelHeight(FocalorsFluidBlock.GetLevel(Terrain.ExtractData(cellValue)));
						float num3 = (float)num + levelHeight;
						if (position2.Y > num3)
						{
							flag = true;
						}
					}
					else if (num2 != 18)
					{
						flag = true;
					}
					if (flag)
					{
						particle.IsActive = false;
						continue;
					}
					particle.Position = position2;
					particle.Velocity += new Vector3(0f, 0.2f, 0f) * dt;
					float x2 = particle.TimeToLive / 3f;
					Color color = BlockColorsMap.WaterColorsMap.Lookup(m_subsystemTerrain.Terrain, x, num, z);
					SubsystemSky subsystemSky = m_subsystemTerrain.Project.FindSubsystem<SubsystemSky>(throwOnError: true);
					float y = subsystemSky.m_viewFogRange.Y;
					float num4 = MathUtils.Saturate(subsystemSky.ViewUnderWaterDepth / MathUtils.Max(1f, y));
					float num5 = MathUtils.Saturate(x2) * (1f - num4);
					Color c = new Color(color.R, color.G, color.B, (byte)(255f * num5));
					ReadOnlyList<GameWidget> gameWidgets = m_subsystemTerrain.Project.FindSubsystem<SubsystemGameWidgets>(throwOnError: true).GameWidgets;
					if (gameWidgets.Count > 0)
					{
						Vector3 viewPosition = gameWidgets[0].ActiveCamera.ViewPosition;
						float num6 = Vector3.Distance(particle.Position, viewPosition);
						float num7 = MathUtils.Saturate(1f - num6 / 24f);
						c *= num7;
					}
					else
					{
						c = Color.Transparent;
					}
					particle.Color = Color.PremultiplyAlpha(c);
					float num8 = 1f - particle.TimeToLive / 3f;
					particle.TextureSlot = (int)(num8 * 8f + 0.5f);
					particle.TextureSlot = MathUtils.Clamp(particle.TextureSlot, 0, 8);
				}
				else
				{
					particle.IsActive = false;
				}
			}
			else if (!IsStopped && m_toGenerate >= 1f)
			{
				float num9 = 0.4f;
				float x3 = m_random.Float(0f - num9, num9);
				float z2 = m_random.Float(0f - num9, num9);
				float y2 = m_random.Float(0.1f, 0.5f);
				particle.IsActive = true;
				particle.Position = Position + new Vector3(x3, y2, z2);
				particle.Color = Color.White;
				particle.Velocity = new Vector3(0f, MathUtils.Lerp(0.4f, 0.8f, m_random.Float(0f, 1f)), 0f);
				particle.TimeToLive = 3f;
				float v = m_random.Float(0.15f, 0.25f);
				particle.Size = new Vector2(v);
				int x4 = Terrain.ToCell(particle.Position.X);
				int y3 = Terrain.ToCell(particle.Position.Y);
				int z3 = Terrain.ToCell(particle.Position.Z);
				Color color2 = BlockColorsMap.WaterColorsMap.Lookup(m_subsystemTerrain.Terrain, x4, y3, z3);
				particle.Color = Color.PremultiplyAlpha(new Color(color2.R, color2.G, color2.B, byte.MaxValue));
				particle.FlipX = m_random.Bool();
				particle.FlipY = m_random.Bool();
				m_toGenerate--;
			}
		}
		if (IsStopped)
		{
			return !Array.Exists(Particles, (Particle p) => p.IsActive);
		}
		return false;
	}

	public UnderwaterBubblesParticleSystem(SubsystemTerrain terrain, float generationSpeedFactor)
		: base(100)
	{
		m_subsystemTerrain = terrain;
		this.generationSpeedFactor = generationSpeedFactor;
		Texture = ContentManager.Get<Texture2D>("Textures/UnderwaterBubbles");
		TextureSlotsCount = 3;
	}
}
