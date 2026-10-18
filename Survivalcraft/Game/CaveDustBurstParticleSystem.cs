using System;
using Engine;
using Engine.Graphics;

namespace Game;

public class CaveDustBurstParticleSystem : ParticleSystem<CaveDustBurstParticleSystem.Particle>
{
	public class Particle : Game.Particle
	{
		public Vector3 Velocity;

		public float TimeToLive;

		public float InitialTimeToLive;
	}

	public float LifeTime = 9f;

	public float FadeIn = 0.5f;

	public float FadeOut = 2.5f;

	public float StartSize = 0.7f;

	public float EndSize = 2.3f;

	public float HorizKick = 0.9f;

	public float UpKick = 0.8f;

	public float SettleDown = 0.25f;

	public float Damping = 0.85f;

	private Random m_random;

	private SubsystemTerrain m_terrain;

	private SubsystemWeather m_weather;

	private Vector3 m_ejectDir;

	private Particle m_particle;

	private float m_age;

	private bool m_firstTick;

	private bool m_escaped;

	public CaveDustBurstParticleSystem(SubsystemTerrain terrain, SubsystemWeather weather, Vector3 spawnInsideBlock)
		: base(1)
	{
		m_random = new Random();
		m_terrain = terrain;
		m_weather = weather;
		Texture = ContentManager.Get<Texture2D>("Textures/CaveSmokeParticle");
		TextureSlotsCount = 3;
		Vector2[] array = new Vector2[8]
		{
			new Vector2(1f, 0f),
			new Vector2(0.7071f, 0.7071f),
			new Vector2(0f, 1f),
			new Vector2(-0.7071f, 0.7071f),
			new Vector2(-1f, 0f),
			new Vector2(-0.7071f, -0.7071f),
			new Vector2(0f, -1f),
			new Vector2(0.7071f, -0.7071f)
		};
		Vector2 vector = array[m_random.Int(0, array.Length - 1)];
		m_ejectDir = new Vector3(vector.X, 0f, vector.Y);
		m_particle = new Particle
		{
			IsActive = true,
			Position = spawnInsideBlock,
			Velocity = Vector3.Zero,
			InitialTimeToLive = LifeTime,
			TimeToLive = LifeTime,
			Size = new Vector2(StartSize),
			FlipX = m_random.Bool(),
			FlipY = m_random.Bool(),
			TextureSlot = 0
		};
		Particles[0] = m_particle;
		m_firstTick = true;
		m_escaped = false;
	}

	public override bool Simulate(float dt)
	{
		dt = MathUtils.Clamp(dt, 0f, 0.033f);
		m_age += dt;
		if (m_firstTick)
		{
			m_firstTick = false;
			for (int i = 0; i < 8; i++)
			{
				int x = Terrain.ToCell(m_particle.Position.X);
				int y = Terrain.ToCell(m_particle.Position.Y);
				int z = Terrain.ToCell(m_particle.Position.Z);
				int num = Terrain.ExtractContents(m_terrain.Terrain.GetCellValue(x, y, z));
				Block block = BlocksManager.Blocks[num];
				if (block is FluidBlock || block is FocalorsFluidBlock || num == 0)
				{
					m_escaped = true;
					break;
				}
				m_particle.Position += m_ejectDir * 0.25f + Vector3.UnitY * 0.18f;
			}
			m_particle.Velocity = Vector3.Zero;
		}
		if (m_escaped)
		{
			Vector2 vector = ((m_weather != null) ? m_weather.CurrentWindVector : Vector2.Zero);
			Vector3 vector2 = new Vector3(vector.X, 0f, vector.Y) * 0.02f;
			m_particle.Position += vector2 * dt;
		}
		else
		{
			m_particle.Position += (m_ejectDir * 0.08f + Vector3.UnitY * 0.06f) * dt;
		}
		float num2 = MathUtils.Saturate(m_age / LifeTime);
		float v = MathUtils.Lerp(StartSize, EndSize, num2);
		m_particle.Size = new Vector2(v);
		float num3;
		if (m_age < FadeIn)
		{
			num3 = MathUtils.Saturate(m_age / Math.Max(0.001f, FadeIn));
		}
		else
		{
			num3 = ((!(m_age > LifeTime - FadeOut)) ? 1f : MathUtils.Saturate((LifeTime - m_age) / Math.Max(0.001f, FadeOut)));
		}
		int x2 = Terrain.ToCell(m_particle.Position.X);
		int y2 = Terrain.ToCell(m_particle.Position.Y);
		int z2 = Terrain.ToCell(m_particle.Position.Z);
		float num4 = LightingManager.LightIntensityByLightValue[m_terrain.Terrain.GetCellLight(x2, y2, z2)];
		Color rgb = new Color(num4, num4, num4) * Color.White;
		m_particle.Color = Color.PremultiplyAlpha(new Color(rgb, (byte)(220f * num3)));
		m_particle.TextureSlot = ((!(num2 < 0.33f)) ? ((num2 < 0.66f) ? 1 : 2) : 0);
		m_particle.TimeToLive -= dt;
		if (m_particle.TimeToLive <= 0f)
		{
			m_particle.IsActive = false;
			return true;
		}
		return false;
	}

	public override void Draw(Camera camera)
	{
		float num = Vector3.Dot(m_particle.Position - camera.ViewPosition, camera.ViewDirection);
		if (num > -0.5f && num <= 4096f)
		{
			base.Draw(camera);
		}
	}
}
