using System;
using Engine;
using Engine.Graphics;

namespace Game;

public class WaterDripsParticleSystem : ParticleSystem<WaterDripsParticleSystem.Particle>
{
	public class Particle : Game.Particle
	{
		public Vector3 Velocity;

		public float TimeToLive;

		public bool IsHanging;
	}

	public float DripRateMultiplier = 1f;

	public float SizeMultiplier = 1f;

	public float WetnessLevel = 1f;

	public bool IsStopped;

	public bool IsBlockSource;

	private Random m_random = new Random();

	private SubsystemTerrain m_subsystemTerrain;

	private SubsystemWeather m_subsystemWeather;

	private TexturedBatch3D m_batch;

	public Vector3 Position;

	public Vector3 Velocity;

	public float ToGenerate;

	private Texture2D m_textureDrizzle;

	private Texture2D m_textureStorm;

	private float m_checkWeatherTimer;

	public bool UseHangingPhase;

	public float EmitterHeight = 1.8f;

	public WaterDripsParticleSystem(SubsystemTerrain terrain, SubsystemWeather weather, Vector3 position, bool isBlockSource)
		: base(20)
	{
		m_subsystemTerrain = terrain;
		m_subsystemWeather = weather;
		Position = position;
		IsBlockSource = isBlockSource;
		m_textureDrizzle = ContentManager.Get<Texture2D>("Textures/Weather/RainParticle");
		m_textureStorm = ContentManager.Get<Texture2D>("Textures/Weather/DrizzleRainParticle");
		Texture = m_textureDrizzle;
		TextureSlotsCount = 1;
	}

	public override bool Simulate(float dt)
	{
		dt = MathUtils.Clamp(dt, 0f, 0.1f);
		m_checkWeatherTimer -= dt;
		if (m_checkWeatherTimer <= 0f)
		{
			m_checkWeatherTimer = 0.5f;
			Texture = ((m_subsystemWeather.CurrentWindStrength > 8f && m_subsystemWeather.m_lightningIntensity > 0.5f) ? m_textureStorm : m_textureDrizzle);
		}
		Vector3 vector = new Vector3(m_subsystemWeather.CurrentWindVector.X, 0f, m_subsystemWeather.CurrentWindVector.Y) * m_subsystemWeather.CurrentWindStrength;
		if (!IsStopped)
		{
			float num = 0f;
			if (UseHangingPhase)
			{
				num = ((WetnessLevel >= 1.5f) ? 1.6f : 0.4f);
			}
			else if (WetnessLevel >= 0.5f)
			{
				num = ((WetnessLevel >= 1.5f) ? 5f : 1.5f);
			}
			else if (WetnessLevel > 0f)
			{
				num = 0.5f;
			}
			num *= DripRateMultiplier;
			ToGenerate += num * dt;
			while (ToGenerate >= 1f)
			{
				for (int i = 0; i < Particles.Length; i++)
				{
					Particle particle = Particles[i];
					if (particle.IsActive)
					{
						continue;
					}
					particle.IsActive = true;
					float maxLength = (IsBlockSource ? 0.4f : 0.15f);
					Vector2 vector2 = m_random.Vector2(0f, maxLength);
					if (UseHangingPhase && IsBlockSource)
					{
						particle.Position = Position + new Vector3(vector2.X, -0.35f, vector2.Y);
						particle.Velocity = new Vector3(0f, -0.15f, 0f);
						particle.IsHanging = true;
					}
					else if (IsBlockSource)
					{
						particle.Position = Position + new Vector3(vector2.X, -0.55f, vector2.Y);
						particle.Velocity = new Vector3(0f, -2.8f, 0f) + vector * 0.1f + Velocity;
						particle.IsHanging = false;
					}
					else
					{
						float y = m_random.Float(0.1f * EmitterHeight, EmitterHeight);
						particle.Position = Position + new Vector3(vector2.X, y, vector2.Y);
						particle.Velocity = new Vector3(0f, -2.8f, 0f) + vector * 0.1f + Velocity;
						particle.IsHanging = false;
					}
					int x = Terrain.ToCell(particle.Position.X);
					int y2 = Terrain.ToCell(particle.Position.Y);
					int z = Terrain.ToCell(particle.Position.Z);
					int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(x, y2, z));
					Block block = BlocksManager.Blocks[num2];
					if (!particle.IsHanging && num2 != 0 && !(block is WaterBlock) && block.IsCollidable)
					{
						particle.IsActive = false;
						break;
					}
					particle.TimeToLive = m_random.Float(0.8f, 1.5f);
					if (particle.IsHanging)
					{
						particle.TimeToLive++;
					}
					float num3 = ((WetnessLevel >= 1.5f) ? 1.5f : 1f);
					particle.Size = new Vector2(0.02f, 0.25f) * SizeMultiplier * num3 * m_random.Float(0.8f, 1.2f);
					if (particle.IsHanging)
					{
						particle.Size.Y = 0.1f;
					}
					particle.Color = Color.White * 0.6f;
					break;
				}
				ToGenerate--;
			}
		}
		bool flag = false;
		for (int j = 0; j < Particles.Length; j++)
		{
			Particle particle2 = Particles[j];
			if (!particle2.IsActive)
			{
				continue;
			}
			flag = true;
			particle2.TimeToLive -= dt;
			if (particle2.TimeToLive <= 0f)
			{
				particle2.IsActive = false;
				continue;
			}
			if (particle2.IsHanging)
			{
				if (particle2.Position.Y < Position.Y - 0.55f)
				{
					particle2.IsHanging = false;
					particle2.Velocity = new Vector3(0f, 0f, 0f);
				}
				else
				{
					particle2.Position += particle2.Velocity * dt;
					particle2.Size.Y = MathUtils.Lerp(particle2.Size.Y, 0.2f, dt * 5f);
				}
				continue;
			}
			float y3 = MathUtils.Lerp(particle2.Velocity.Y, -9f, dt * 5f);
			Vector3 vector3 = vector * 0.2f;
			particle2.Velocity.X = MathUtils.Lerp(particle2.Velocity.X, vector3.X, dt * 2f);
			particle2.Velocity.Y = y3;
			particle2.Velocity.Z = MathUtils.Lerp(particle2.Velocity.Z, vector3.Z, dt * 2f);
			Vector3 vector4 = particle2.Position + particle2.Velocity * dt;
			TerrainRaycastResult? terrainRaycastResult = m_subsystemTerrain.Raycast(particle2.Position, vector4, useInteractionBoxes: false, skipAirBlocks: true, null);
			if (terrainRaycastResult.HasValue)
			{
				particle2.IsActive = false;
				m_subsystemWeather.RainSplashParticleSystem.AddSplash(terrainRaycastResult.Value.Value, terrainRaycastResult.Value.HitPoint(), Color.White * 0.4f, terrainRaycastResult.Value.CellFace.Face);
			}
			else
			{
				particle2.Position = vector4;
			}
			float num4 = MathUtils.Saturate(particle2.TimeToLive * 2f);
			particle2.Color = Color.White * (0.5f * num4);
			float num5 = particle2.Velocity.Length();
			particle2.Size.Y = 0.15f + num5 * 0.02f;
		}
		if (IsStopped)
		{
			return !flag;
		}
		return false;
	}

	public override void Draw(Camera camera)
	{
		if (IsStopped && !Particles[0].IsActive && ToGenerate < 1f)
		{
			return;
		}
		if (m_batch == null)
		{
			m_batch = m_subsystemTerrain.Project.FindSubsystem<SubsystemParticles>(throwOnError: true).PrimitivesRenderer.TexturedBatch(Texture, useAlphaTest: false, 0, DepthStencilState.DepthRead, null, BlendState.AlphaBlend, SamplerState.PointClamp);
		}
		Vector3 viewDirection = camera.ViewDirection;
		Vector3 v = new Vector3(0f, -1f, 0f);
		for (int i = 0; i < Particles.Length; i++)
		{
			Particle particle = Particles[i];
			if (particle.IsActive)
			{
				Vector3 v2 = Vector3.Normalize(particle.Velocity);
				Vector3 v3 = Vector3.Cross(v, v2);
				float angle = (float)Math.Acos(Vector3.Dot(v, v2));
				v3 = ((!(v3.LengthSquared() < 1E-06f)) ? Vector3.Normalize(v3) : Vector3.UnitX);
				Matrix m = Matrix.CreateFromAxisAngle(v3, angle);
				Vector3 vector = Vector3.Normalize(Vector3.Cross(viewDirection, Vector3.UnitY)) * particle.Size.X;
				Vector3 vector2 = Vector3.UnitY * particle.Size.Y;
				Vector3 p = particle.Position + Vector3.Transform(-vector - vector2, m);
				Vector3 p2 = particle.Position + Vector3.Transform(vector - vector2, m);
				Vector3 p3 = particle.Position + Vector3.Transform(vector2, m);
				m_batch.QueueTriangle(p, p2, p3, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0f), particle.Color);
			}
		}
	}
}
