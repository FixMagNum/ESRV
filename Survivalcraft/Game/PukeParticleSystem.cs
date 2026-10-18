using Engine;
using Engine.Graphics;

namespace Game;

public class PukeParticleSystem : ParticleSystem<PukeParticleSystem.Particle>
{
	public class Particle : Game.Particle
	{
		public Vector3 Velocity;

		public float TimeToLive;
	}

	public Random m_random = new Random();

	public SubsystemTerrain m_subsystemTerrain;

	public float m_duration;

	public float m_toGenerate;

	public SubsystemWeather m_subsystemWeather;

	public Vector3 Position { get; set; }

	public Vector3 Direction { get; set; }

	public bool IsStopped { get; set; }

	public override bool Simulate(float dt)
	{
		int num = Terrain.ToCell(Position.X);
		int num2 = Terrain.ToCell(Position.Y);
		int item = Terrain.ToCell(Position.Z);
		int x = 0;
		x = MathUtils.Max(x, m_subsystemTerrain.Terrain.GetCellLight(num + 1, num2, item));
		x = MathUtils.Max(x, m_subsystemTerrain.Terrain.GetCellLight(num - 1, num2, item));
		x = MathUtils.Max(x, m_subsystemTerrain.Terrain.GetCellLight(num, num2 + 1, item));
		x = MathUtils.Max(x, m_subsystemTerrain.Terrain.GetCellLight(num, num2 - 1, item));
		x = MathUtils.Max(x, m_subsystemTerrain.Terrain.GetCellLight(num, num2, item + 1));
		x = MathUtils.Max(x, m_subsystemTerrain.Terrain.GetCellLight(num, num2, item - 1));
		Color white = Color.White;
		float num3 = LightingManager.LightIntensityByLightValue[x];
		white *= num3;
		white.A = byte.MaxValue;
		dt = MathUtils.Clamp(dt, 0f, 0.1f);
		float num4 = MathUtils.Pow(0.03f, dt);
		m_duration += dt;
		if (m_duration > 3.5f)
		{
			IsStopped = true;
		}
		float num5 = MathUtils.Saturate(1.3f * SimplexNoise.Noise(3f * m_duration + (float)(GetHashCode() % 100)) - 0.3f);
		float num6 = 30f * num5;
		m_toGenerate += num6 * dt;
		bool flag = false;
		float currentWindStrength = m_subsystemWeather.CurrentWindStrength;
		Vector2 currentWindVector = m_subsystemWeather.CurrentWindVector;
		Vector3 vector = new Vector3(currentWindVector.X, 0f, currentWindVector.Y) * currentWindStrength;
		for (int i = 0; i < Particles.Length; i++)
		{
			Particle particle = Particles[i];
			if (particle.IsActive)
			{
				flag = true;
				particle.TimeToLive -= dt;
				if (particle.TimeToLive > 0f)
				{
					Vector3 position = particle.Position;
					Vector3 vector2 = particle.Velocity * dt;
					if (currentWindStrength > 0f && m_subsystemWeather.GetWindExposureFactor(position) > 0f)
					{
						vector2 += vector * dt;
					}
					Vector3 vector3 = position + vector2;
					TerrainRaycastResult? terrainRaycastResult = m_subsystemTerrain.Raycast(position, vector3, useInteractionBoxes: false, skipAirBlocks: true, (int value, float distance) => BlocksManager.Blocks[Terrain.ExtractContents(value)].IsCollidable);
					if (terrainRaycastResult.HasValue)
					{
						Plane plane = terrainRaycastResult.Value.CellFace.CalculatePlane();
						vector3 = position;
						if (plane.Normal.X != 0f)
						{
							particle.Velocity *= new Vector3(-0.05f, 0.05f, 0.05f);
						}
						if (plane.Normal.Y != 0f)
						{
							particle.Velocity *= new Vector3(0.05f, -0.05f, 0.05f);
						}
						if (plane.Normal.Z != 0f)
						{
							particle.Velocity *= new Vector3(0.05f, 0.05f, -0.05f);
						}
					}
					particle.Position = vector3;
					particle.Velocity.Y += -9.81f * dt;
					particle.Velocity *= num4;
					particle.Color *= MathUtils.Saturate(particle.TimeToLive);
					particle.TextureSlot = (int)(8.99f * MathUtils.Saturate(3f - particle.TimeToLive));
				}
				else
				{
					particle.IsActive = false;
				}
			}
			else if (!IsStopped && m_toGenerate >= 1f)
			{
				Vector3 vector4 = m_random.Vector3(0f, 1f);
				particle.IsActive = true;
				particle.Position = Position + 0.05f * vector4;
				particle.Color = Color.MultiplyColorOnly(white, m_random.Float(0.7f, 1f));
				particle.Velocity = MathUtils.Lerp(1f, 2.5f, num5) * Vector3.Normalize(Direction + 0.25f * vector4);
				particle.TimeToLive = 3f;
				particle.Size = new Vector2(0.1f);
				particle.FlipX = m_random.Bool();
				particle.FlipY = m_random.Bool();
				m_toGenerate--;
			}
		}
		if (IsStopped)
		{
			return !flag;
		}
		return false;
	}

	public PukeParticleSystem(SubsystemTerrain terrain, SubsystemWeather subsystemWeather)
		: base(80)
	{
		m_subsystemTerrain = terrain;
		m_subsystemWeather = subsystemWeather;
		Texture = ContentManager.Get<Texture2D>("Textures/PukeParticle");
		TextureSlotsCount = 3;
	}
}
