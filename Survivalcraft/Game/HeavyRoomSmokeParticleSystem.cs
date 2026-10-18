using System;
using Engine;
using Engine.Graphics;

namespace Game;

public class HeavyRoomSmokeParticleSystem : ParticleSystem<HeavyRoomSmokeParticleSystem.Particle>
{
	public class Particle : Game.Particle
	{
		public Vector3 Velocity;

		public float TimeToLive;

		public float InitialTimeToLive;
	}

	public Vector3 StartWorldPos;

	public float TargetTopY;

	public Vector2 DriftDir;

	public float DriftSpeed;

	public float LifeTime = 120f;

	public float FadeInTime = 20f;

	public float FadeOutTime = 20f;

	public float StartSize = 0.9f;

	public float EndSize = 2.4f;

	public bool RandomOffsetEnabled = true;

	private bool m_alive;

	private float m_age;

	private Random m_random;

	private SubsystemTerrain m_subsystemTerrain;

	private SubsystemWeather m_subsystemWeather;

	private Particle m_particle;

	private SubsystemSmokeAccumulation m_subsystemSmoke;

	private bool m_isVisible;

	private float m_smokeCheckTimer;

	private float m_currentSmokeLevel;

	private float m_targetSmokeLevel;

	public bool IsAlive => m_alive;

	public Vector3 Position => m_particle.Position;

	public HeavyRoomSmokeParticleSystem(SubsystemTerrain terrain, SubsystemWeather weather, Vector3 startWorldPos, float targetTopY, float columnHeight)
		: base(1)
	{
		m_random = new Random();
		m_subsystemTerrain = terrain;
		m_subsystemWeather = weather;
		m_subsystemSmoke = terrain.Project.FindSubsystem<SubsystemSmokeAccumulation>(throwOnError: true);
		StartWorldPos = startWorldPos;
		TargetTopY = targetTopY;
		m_targetSmokeLevel = 1f;
		m_currentSmokeLevel = 0f;
		m_smokeCheckTimer = 1.1f;
		DriftDir = m_random.Vector2(0f, 1f);
		if (DriftDir.Length() < 0.0001f)
		{
			DriftDir = new Vector2(1f, 0f);
		}
		DriftDir = Vector2.Normalize(DriftDir);
		DriftSpeed = MathUtils.Lerp(0.02f, 0.08f, MathUtils.Saturate(columnHeight / 12f));
		Texture = ContentManager.Get<Texture2D>("Textures/NaturalSmokeParticle");
		TextureSlotsCount = 3;
		m_particle = new Particle
		{
			IsActive = true,
			Position = StartWorldPos + (RandomOffsetEnabled ? new Vector3(m_random.Float(-0.2f, 0.2f), 0f, m_random.Float(-0.2f, 0.2f)) : Vector3.Zero),
			Velocity = Vector3.Zero,
			InitialTimeToLive = LifeTime,
			TimeToLive = LifeTime,
			Size = new Vector2(StartSize),
			FlipX = m_random.Bool(),
			FlipY = m_random.Bool(),
			TextureSlot = 0
		};
		Particles[0] = m_particle;
		m_alive = true;
		m_age = 0f;
	}

	public override bool Simulate(float dt)
	{
		if (!m_alive)
		{
			return true;
		}
		dt = MathUtils.Clamp(dt, 0f, 0.1f);
		m_age += dt;
		m_smokeCheckTimer += dt;
		if (m_smokeCheckTimer >= 1f)
		{
			m_smokeCheckTimer = 0f;
			if (m_subsystemSmoke != null)
			{
				int x = Terrain.ToCell(m_particle.Position.X);
				int y = Terrain.ToCell(m_particle.Position.Y);
				int z = Terrain.ToCell(m_particle.Position.Z);
				m_targetSmokeLevel = MathUtils.Saturate(m_subsystemSmoke.GetSmokeLevel(x, y, z));
			}
			else
			{
				m_targetSmokeLevel = 1f;
			}
		}
		m_currentSmokeLevel = MathUtils.Lerp(m_currentSmokeLevel, m_targetSmokeLevel, dt * 2f);
		int x2 = Terrain.ToCell(m_particle.Position.X);
		int y2 = Terrain.ToCell(m_particle.Position.Y);
		int z2 = Terrain.ToCell(m_particle.Position.Z);
		int cellLight = m_subsystemTerrain.Terrain.GetCellLight(x2, y2, z2);
		float num = LightingManager.LightIntensityByLightValue[cellLight];
		Color rgb = new Color(num, num, num) * Color.White;
		float y3 = (TargetTopY - StartWorldPos.Y) / Math.Max(0.0001f, LifeTime);
		Vector2 currentWindVector = m_subsystemWeather.CurrentWindVector;
		Vector3 vector = new Vector3(currentWindVector.X, 0f, currentWindVector.Y) * 0.15f;
		Vector3 vector2 = new Vector3(DriftDir.X, 0f, DriftDir.Y) * DriftSpeed + vector * dt;
		m_particle.Position += new Vector3(vector2.X, y3, vector2.Z) * dt;
		float num2 = MathUtils.Saturate(m_age / LifeTime);
		float currentSmokeLevel = m_currentSmokeLevel;
		float num3 = MathUtils.Lerp(StartSize, EndSize, num2);
		m_particle.Size = new Vector2(num3 * currentSmokeLevel);
		float num4;
		if (m_age < FadeInTime)
		{
			num4 = MathUtils.Saturate(m_age / Math.Max(0.001f, FadeInTime));
		}
		else
		{
			num4 = ((!(m_age > LifeTime - FadeOutTime)) ? 1f : MathUtils.Saturate((LifeTime - m_age) / Math.Max(0.001f, FadeOutTime)));
		}
		num4 *= MathUtils.Lerp(1f, 0.7f, num2);
		if (currentSmokeLevel < 0.01f)
		{
			float num5 = MathUtils.Saturate(m_age / 10f);
			num4 *= 1f - num5;
			m_particle.TimeToLive = MathUtils.Min(m_particle.TimeToLive, 10f - m_age);
		}
		else
		{
			num4 *= currentSmokeLevel;
		}
		byte a = (byte)(200f * MathUtils.Saturate(num4));
		m_particle.Color = Color.PremultiplyAlpha(new Color(rgb, a));
		m_particle.TextureSlot = ((!(num2 < 0.33f)) ? ((num2 < 0.66f) ? 1 : 2) : 0);
		m_particle.TimeToLive -= dt;
		if (m_particle.TimeToLive <= 0f)
		{
			m_particle.IsActive = false;
			m_alive = false;
			return true;
		}
		return false;
	}

	public override void Draw(Camera camera)
	{
		float index = Vector3.Dot(m_particle.Position - camera.ViewPosition, camera.ViewDirection);
		if (index <= -2f || index > 4096f)
		{
			return;
		}
		float text = Vector3.DistanceSquared(m_particle.Position, camera.ViewPosition);
		if (text > 3600f)
		{
			return;
		}
		float num = MathUtils.Sqrt(text);
		m_isVisible = false;
		if (num > 4f && m_subsystemSmoke != null)
		{
			Vector3 vector = (camera.ViewPosition - m_particle.Position) / num;
			Vector3 vector2 = m_particle.Position + vector * 1f;
			int x = Terrain.ToCell(vector2.X);
			int y = Terrain.ToCell(vector2.Y);
			int z = Terrain.ToCell(vector2.Z);
			if (m_subsystemSmoke.GetSmokeLevel(x, y, z) > 0.98f)
			{
				return;
			}
		}
		TerrainRaycastResult? terrainRaycastResult = m_subsystemTerrain.Raycast(camera.ViewPosition, m_particle.Position, useInteractionBoxes: false, skipAirBlocks: true, (int val, float d) =>
		{
			int num2 = Terrain.ExtractContents(val);
			Block block = BlocksManager.Blocks[num2];
			return block.IsCollidable && !block.IsTransparent;
		});
		if (!terrainRaycastResult.HasValue || terrainRaycastResult.Value.Distance >= num - 0.2f)
		{
			m_isVisible = true;
			base.Draw(camera);
		}
	}

	public HeavyRoomSmokeParticleSystem(SubsystemTerrain terrain, SubsystemWeather weather, Vector3 startWorldPos, float targetTopY, float columnHeight, SubsystemSmokeAccumulation smoke, float initialAge01)
		: this(terrain, weather, startWorldPos, targetTopY, columnHeight)
	{
		m_subsystemSmoke = smoke ?? m_subsystemSmoke;
		if (initialAge01 > 0f)
		{
			InitializeProgress(initialAge01);
		}
	}

	public void InitializeProgress(float age01)
	{
		age01 = MathUtils.Saturate(age01);
		m_age = age01 * LifeTime;
		m_particle.TimeToLive = MathUtils.Max(0.01f, LifeTime - m_age);
		float num = (TargetTopY - StartWorldPos.Y) / Math.Max(0.0001f, LifeTime);
		m_particle.Position = new Vector3(m_particle.Position.X, StartWorldPos.Y + num * m_age, m_particle.Position.Z);
		float num2 = MathUtils.Saturate(m_age / LifeTime);
		float currentSmokeLevel = m_currentSmokeLevel;
		float num3 = MathUtils.Lerp(StartSize, EndSize, num2);
		m_particle.Size = new Vector2(num3 * currentSmokeLevel);
		m_particle.TextureSlot = ((!(num2 < 0.33f)) ? ((num2 < 0.66f) ? 1 : 2) : 0);
	}
}
