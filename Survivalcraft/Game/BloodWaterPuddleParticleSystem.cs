using System;
using Engine;
using Engine.Graphics;

namespace Game;

public class BloodWaterPuddleParticleSystem : ParticleSystem<BloodWaterPuddleParticleSystem.Particle>
{
	public class Particle : Game.Particle
	{
		public float Age;

		public float LifeTime;

		public float StartSize;

		public float MaxSize;

		public Vector3 Drift;

		public float SinkSpeed;

		public bool Stopped;

		public Vector3 PreviousPosition;

		public int Blades;

		public float[] Angles = new float[5];

		public float BaseYaw;

		public bool InWater;

		public float WaterSurfaceY;
	}

	private readonly SubsystemTerrain m_subsystemTerrain;

	private TexturedBatch3D m_batch;

	private readonly Random m_random;

	private const int kMinBlades = 2;

	private const int kMaxBlades = 3;

	private const float kBladeWidthScale = 0.65f;

	private const float kTopCapOffset = 0.003f;

	private const float kMaxUnderwaterDepth = 0.06f;

	private const float kHorizCapRadiusScale = 0.6f;

	private const float kHorizCapZBias = 0.0012f;

	private const float kLOD_Dist2_1Blade = 225f;

	private const float kLOD_Dist2_2Blades = 100f;

	private const float TwoPi = (float)Math.PI * 2f;

	private readonly SubsystemFluidBlockBehavior m_fluid;

	private readonly SubsystemGameWidgets m_gameWidgets;

	private readonly SubsystemSky m_sky;

	public BloodWaterPuddleParticleSystem(SubsystemTerrain subsystemTerrain, Vector3 position)
		: base(1)
	{
		if (SettingsManager.DisplayBloodParticles)
		{
			m_random = new Random();
			m_subsystemTerrain = subsystemTerrain;
			m_fluid = subsystemTerrain.Project.FindSubsystem<SubsystemFluidBlockBehavior>(throwOnError: true);
			m_gameWidgets = subsystemTerrain.Project.FindSubsystem<SubsystemGameWidgets>(throwOnError: true);
			m_sky = subsystemTerrain.Project.FindSubsystem<SubsystemSky>(throwOnError: true);
			Texture = ContentManager.Get<Texture2D>("Textures/BloodPuddleParticle");
			TextureSlotsCount = 1;
			Particle particle = Particles[0];
			particle.IsActive = true;
			particle.Position = position;
			particle.PreviousPosition = position;
			particle.Age = 0f;
			particle.LifeTime = m_random.Float(8f, 12f);
			particle.StartSize = m_random.Float(0.25f, 0.5f);
			particle.MaxSize = particle.StartSize * m_random.Float(3.5f, 5.5f);
			particle.Size = new Vector2(particle.StartSize);
			particle.Color = Color.PremultiplyAlpha(new Color(120, 0, 0, 255));
			particle.Blades = m_random.Int(2, 3);
			particle.BaseYaw = m_random.Float(0f, (float)Math.PI * 2f);
			particle.Stopped = false;
			float num = m_random.Float(0.02f, 0.05f);
			float level = m_random.Float(0f, (float)Math.PI * 2f);
			particle.Drift = new Vector3(MathUtils.Cos(level) * num, 0f, MathUtils.Sin(level) * num);
			particle.SinkSpeed = m_random.Float(0.01f, 0.03f);
			for (int data = 0; data < particle.Angles.Length; data++)
			{
				particle.Angles[data] = m_random.Float(0f, (float)Math.PI * 2f);
			}
		}
	}

	public override bool Simulate(float dt)
	{
		dt = MathUtils.Clamp(dt, 0f, 0.1f);
		bool flag = false;
		for (int i = 0; i < Particles.Length; i++)
		{
			Particle particle = Particles[i];
			if (!particle.IsActive)
			{
				continue;
			}
			particle.Age += dt;
			float num = MathUtils.Saturate(particle.Age / particle.LifeTime);
			float v = MathUtils.Lerp(particle.StartSize, particle.MaxSize, num);
			particle.Size = new Vector2(v);
			if (!particle.Stopped)
			{
				Vector3 vector = particle.Position + particle.Drift * dt + new Vector3(0f, (0f - particle.SinkSpeed) * dt, 0f);
				TerrainRaycastResult? terrainRaycastResult = m_subsystemTerrain.Raycast(particle.Position, vector, useInteractionBoxes: false, skipAirBlocks: false, (int value, float d) => BlocksManager.Blocks[Terrain.ExtractContents(value)].IsCollidable);
				if (terrainRaycastResult.HasValue)
				{
					particle.Position = terrainRaycastResult.Value.HitPoint(0.01f);
					particle.Stopped = true;
				}
				else
				{
					particle.Position = vector;
				}
			}
			particle.InWater = false;
			particle.WaterSurfaceY = 0f;
			int x = Terrain.ToCell(particle.Position.X);
			int y = Terrain.ToCell(particle.Position.Y);
			int z = Terrain.ToCell(particle.Position.Z);
			float? surfaceHeight = m_fluid.GetSurfaceHeight(x, y, z);
			if (surfaceHeight.HasValue)
			{
				particle.InWater = true;
				particle.WaterSurfaceY = surfaceHeight.Value;
				float num2 = particle.Size.Y * 0.5f;
				float num3 = particle.Position.Y + num2;
				float num4 = particle.WaterSurfaceY - 0.012f;
				if (num3 > num4)
				{
					Particle particle2 = particle;
					particle2.Position.Y = particle2.Position.Y - (num3 - num4);
				}
				float num5 = particle.WaterSurfaceY - 0.06f - num2;
				if (particle.Position.Y < num5)
				{
					particle.Position.Y = num5;
				}
			}
			float y2 = m_sky.m_viewFogRange.Y;
			float num6 = MathUtils.Saturate(m_sky.ViewUnderWaterDepth / MathUtils.Max(1f, y2));
			float num7 = 1f - num6;
			Vector3 v2 = ((m_gameWidgets != null && m_gameWidgets.GameWidgets.Count > 0) ? m_gameWidgets.GameWidgets[0].ActiveCamera.ViewPosition : (particle.Position + new Vector3(0f, 10f, 0f)));
			float num8 = Vector3.Distance(particle.Position, v2);
			float num9 = (particle.InWater ? MathUtils.Min(24f, y2) : 24f);
			float num10 = MathUtils.Saturate(1f - num8 / num9);
			byte b = (byte)(MathUtils.Saturate((1f - num) * num10 * num7) * 255f);
			Color color = new Color((byte)120, (byte)0, (byte)0, b);
			if (particle.InWater && m_sky.ViewUnderWaterDepth > 0f)
			{
				float f = MathUtils.Saturate(num8 / num9);
				Color viewFogColor = m_sky.m_viewFogColor;
				color = Color.Lerp(color, new Color(viewFogColor.R, viewFogColor.G, viewFogColor.B, b), f);
			}
			particle.Color = Color.PremultiplyAlpha(color);
			if (particle.Age >= particle.LifeTime || b == 0)
			{
				particle.IsActive = false;
			}
			else
			{
				flag = true;
			}
		}
		return !flag;
	}

	public override void Draw(Camera camera)
	{
		if (Texture == null)
		{
			return;
		}
		if (m_batch == null)
		{
			m_batch = SubsystemParticles.PrimitivesRenderer.TexturedBatch(Texture, useAlphaTest: false, 0, DepthStencilState.DepthRead, null, BlendState.AlphaBlend, SamplerState.PointClamp);
		}
		for (int i = 0; i < Particles.Length; i++)
		{
			Particle particle = Particles[i];
			if (!particle.IsActive)
			{
				continue;
			}
			int num = particle.Blades;
			float num2 = Vector3.DistanceSquared(camera.ViewPosition, particle.Position);
			if (num2 > 225f)
			{
				num = MathUtils.Max(1, num - 2);
			}
			else if (num2 > 100f)
			{
				num = MathUtils.Max(1, num - 1);
			}
			for (int j = 0; j < num; j++)
			{
				float x = particle.BaseYaw + particle.Angles[j];
				Vector3 vector = new Vector3(MathUtils.Cos(x), 0f, MathUtils.Sin(x)) * (particle.Size.X * 0.5f);
				Vector3 vector2 = Vector3.UnitY * (particle.Size.Y * 0.5f);
				Vector3 position = particle.Position;
				if (particle.InWater)
				{
					float num3 = particle.WaterSurfaceY - 0.003f;
					float num4 = position.Y + vector2.Y;
					if (num4 > num3)
					{
						position.Y -= num4 - num3;
					}
				}
				Vector3 p = position - vector - vector2;
				Vector3 p2 = position + vector - vector2;
				Vector3 p3 = position + vector + vector2;
				Vector3 p4 = position - vector + vector2;
				m_batch.QueueQuad(p, p2, p3, p4, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(0f, 0f), particle.Color);
			}
			float num5 = particle.Size.X * 0.5f * 0.6f;
			Vector3 hx = Vector3.UnitX * num5;
			Vector3 hz = Vector3.UnitZ * num5;
			float num6 = particle.Size.Y * 0.5f;
			float y;
			float y2;
			if (particle.InWater)
			{
				float x2 = particle.WaterSurfaceY - 0.006f;
				float x3 = particle.Position.Y + num6;
				y = MathUtils.Min(x2, x3) - 0.0012f;
				float x4 = particle.WaterSurfaceY - 0.06f;
				float x5 = particle.Position.Y - num6;
				y2 = MathUtils.Max(x4, x5) - 0.0012f;
			}
			else
			{
				y = particle.Position.Y + num6 * 0.5f - 0.0012f;
				y2 = particle.Position.Y - num6 * 0.5f - 0.0012f;
			}
			Vector3 center = new Vector3(particle.Position.X, y, particle.Position.Z);
			Vector3 center2 = new Vector3(particle.Position.X, y2, particle.Position.Z);
			QueueHorizontalQuad(center, hx, hz, particle.Color);
			QueueHorizontalQuad(center2, hx, hz, particle.Color);
		}
	}

	private void QueueHorizontalQuad(Vector3 center, Vector3 hx, Vector3 hz, Color color)
	{
		Vector3 p = center - hx - hz;
		Vector3 p2 = center + hx - hz;
		Vector3 p3 = center + hx + hz;
		Vector3 p4 = center - hx + hz;
		m_batch.QueueQuad(p, p2, p3, p4, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(0f, 0f), color);
	}
}
