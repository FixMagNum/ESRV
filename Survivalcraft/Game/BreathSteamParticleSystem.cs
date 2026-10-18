using Engine;
using Engine.Graphics;

namespace Game;

public class BreathSteamParticleSystem : ParticleSystem<BreathSteamParticleSystem.Particle>
{
	public class Particle : Game.Particle
	{
		public Vector3 Velocity;

		public float TimeToLive;
	}

	public Random m_random;

	public SubsystemTerrain m_subsystemTerrain;

	public float m_duration;

	public float m_toGenerate;

	public SubsystemWeather m_subsystemWeather;

	public float AlphaMultiplier = 1f;

	public float BaseSize = 0.12f;

	public Vector3 Position { get; set; }

	public Vector3 Direction { get; set; }

	public bool IsStopped { get; set; }

	public override bool Simulate(float dt)
	{
		dt = MathUtils.Clamp(dt, 0f, 0.1f);
		float num = MathUtils.Pow(0.02f, dt);
		m_duration += dt;
		if (m_duration > 1.5f)
		{
			IsStopped = true;
		}
		float num2 = MathUtils.Saturate(1.1f * SimplexNoise.Noise(3f * m_duration + (float)(GetHashCode() % 100)) - 0.3f);
		float num3 = 3f * num2;
		m_toGenerate += num3 * dt;
		Color color;
		if (SettingsManager.SkyLightUseParticles)
		{
			int cellLight = m_subsystemTerrain.Terrain.GetCellLight(Terrain.ToCell(Position.X), Terrain.ToCell(Position.Y), Terrain.ToCell(Position.Z));
			byte b = (byte)(255f * MathUtils.Lerp(0.15f, 1f, (float)cellLight / 15f));
			color = new Color(b, b, b);
		}
		else
		{
			float f = MathUtils.Clamp(m_subsystemWeather.SubsystemSky.SkyLightIntensity, 0f, 1f);
			byte b2 = (byte)(255f * MathUtils.Lerp(0.15f, 1f, f));
			color = new Color(b2, b2, b2);
		}
		bool flag = false;
		for (int i = 0; i < Particles.Length; i++)
		{
			Particle particle = Particles[i];
			if (!particle.IsActive)
			{
				continue;
			}
			flag = true;
			particle.TimeToLive -= dt;
			if (particle.TimeToLive > 0f)
			{
				Vector3 position = particle.Position;
				Vector3 vector = particle.Velocity * dt;
				Vector3 vector2 = position + vector;
				if (m_subsystemTerrain.Raycast(position, vector2, useInteractionBoxes: false, skipAirBlocks: true, (int v, float d) => BlocksManager.Blocks[Terrain.ExtractContents(v)].IsCollidable).HasValue)
				{
					vector2 = position;
				}
				particle.Position = vector2;
				Particle particle2 = particle;
				particle2.Velocity.Y = particle2.Velocity.Y + 0.4f * dt;
				particle.Velocity *= num;
				float num4 = particle.TimeToLive / 1.5f;
				particle.Color = Color.PremultiplyAlpha(new Color(color, (byte)(255f * num4 * AlphaMultiplier)));
				float x = 1f - num4;
				particle.TextureSlot = (int)(4.5f * MathUtils.Saturate(x));
				if (particle.TimeToLive < 0.2f)
				{
					particle.TextureSlot = 8;
				}
				float num5 = (1.5f - particle.TimeToLive) / 1.5f;
				particle.Size = new Vector2(BaseSize * (1f + num5));
			}
			else
			{
				particle.IsActive = false;
			}
		}
		if (!IsStopped && m_toGenerate >= 0.34f)
		{
			for (int num6 = 0; num6 < Particles.Length; num6++)
			{
				if (!Particles[num6].IsActive)
				{
					Particle obj = Particles[num6];
					obj.IsActive = true;
					Vector3 vector3 = m_random.Vector3(0f, 1f);
					obj.Position = Position + 0.05f * vector3 - new Vector3(0f, 0.2f, 0f);
					Color rgb = Color.MultiplyColorOnly(color, m_random.Float(0.8f, 1f));
					byte a = (byte)(255f * AlphaMultiplier);
					obj.Color = Color.PremultiplyAlpha(new Color(rgb, a));
					float num7 = MathUtils.Lerp(0.6f, 0.8f, num2) * m_random.Float(0.9f, 1.1f);
					obj.Velocity = num7 * Vector3.Normalize(Direction + 0.3f * vector3);
					obj.TimeToLive = 1.5f;
					obj.Size = new Vector2(BaseSize * m_random.Float(0.9f, 1.1f));
					obj.FlipX = m_random.Bool();
					obj.FlipY = m_random.Bool();
					m_toGenerate -= 0.34f;
					break;
				}
			}
		}
		if (IsStopped)
		{
			return !flag;
		}
		return false;
	}

	public BreathSteamParticleSystem(SubsystemTerrain terrain, SubsystemWeather subsystemWeather)
		: base(80)
	{
		m_random = new Random();
		m_subsystemTerrain = terrain;
		m_subsystemWeather = subsystemWeather;
		Texture = ContentManager.Get<Texture2D>("Textures/BreathSteamParticle");
		TextureSlotsCount = 3;
	}
}
