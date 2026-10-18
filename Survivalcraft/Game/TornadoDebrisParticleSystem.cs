using Engine;
using Engine.Graphics;

namespace Game;

public class TornadoDebrisParticleSystem : ParticleSystem<TornadoDebrisParticleSystem.Particle>
{
	public class Particle : Game.Particle
	{
		public float TimeToLive;

		public float InitialTimeToLive;

		public float Age;

		public float Angle;

		public float AngularSpeed;

		public float RadialMul;

		public float RadialPulseAmp;

		public float RadialPulseSpeed;

		public float RadialPhase;

		public float HeightBase;

		public float HeightAmp;

		public float HeightSpeed;

		public float HeightPhase;

		public float SizeMul;
	}

	private float m_toGenerate;

	private Random m_random;

	private SubsystemTerrain m_subsystemTerrain;

	private SubsystemWeather m_subsystemWeather;

	public bool m_visible;

	public Vector3 m_position;

	public float m_maxVisibilityDistance;

	public float m_age;

	public Vector3 Position { get; set; }

	public Vector3 SwirlCenter { get; set; }

	public float Omega { get; set; }

	public float ParticleSize { get; set; }

	public float BaseAlpha { get; set; }

	public Vector2 LifetimeMinMax { get; set; }

	public int DesiredCount { get; set; }

	public float SpawnRateMultiplier { get; set; }

	public float BaseY { get; set; }

	public float TopY { get; set; }

	public float SpawnRadius { get; set; }

	public float TopRadius { get; set; }

	public Vector3 Direction { get; set; }

	public Color Color { get; set; } = Color.White;

	public bool IsStopped { get; set; }

	public TornadoDebrisParticleSystem(SubsystemTerrain terrain, SubsystemWeather weather, Vector3 position, float maxVisibilityDistance)
		: base(400)
	{
		m_subsystemTerrain = terrain;
		m_subsystemWeather = weather;
		m_random = new Random();
		Position = position;
		m_position = position;
		m_maxVisibilityDistance = maxVisibilityDistance;
		Texture = ContentManager.Get<Texture2D>("Textures/TornadoDebrisParticle");
		TextureSlotsCount = 4;
		Color = new Color(0.2f, 0.2f, 0.2f);
		Direction = new Vector3(0f, 1f, 0f);
		ParticleSize = 0.35f;
		BaseAlpha = 255f;
		LifetimeMinMax = new Vector2(8f, 14f);
		DesiredCount = 100;
		SpawnRateMultiplier = 1f;
		BaseY = 65f;
		TopY = 140f;
		SpawnRadius = 8f;
		TopRadius = 2f;
		Omega = 2f;
	}

	public override bool Simulate(float dt)
	{
		if (IsStopped || (!m_visible && m_age >= 2f))
		{
			return true;
		}
		dt = MathUtils.Clamp(dt, 0f, 0.1f);
		int x = Terrain.ToCell(Position.X);
		int y = Terrain.ToCell(Position.Y);
		int item = Terrain.ToCell(Position.Z);
		int cellLight = m_subsystemTerrain.Terrain.GetCellLight(x, y, item);
		float num = MathUtils.Max(LightingManager.LightIntensityByLightValue[cellLight], 0.6f);
		Color rgb = new Color(num, num, num) * Color;
		int num2 = 0;
		for (int i = 0; i < Particles.Length; i++)
		{
			Particle particle = Particles[i];
			if (!particle.IsActive)
			{
				continue;
			}
			particle.TimeToLive -= dt;
			particle.Age += dt;
			if (particle.TimeToLive <= 0f)
			{
				particle.IsActive = false;
				continue;
			}
			float x2 = particle.HeightPhase + particle.HeightSpeed * particle.Age;
			float x3 = particle.HeightBase + particle.HeightAmp * MathUtils.Sin(x2);
			x3 = MathUtils.Clamp(x3, BaseY, TopY);
			float f = ((TopY > BaseY) ? MathUtils.Saturate((x3 - BaseY) / (TopY - BaseY)) : 0f);
			float num3 = MathUtils.Lerp(SpawnRadius, TopRadius, f);
			float num4 = 1f + particle.RadialPulseAmp * MathUtils.Sin(particle.RadialPhase + particle.RadialPulseSpeed * particle.Age);
			float num5 = MathUtils.Clamp(num3 * particle.RadialMul * num4, 0.5f * TopRadius, SpawnRadius * 1.5f);
			particle.Angle += particle.AngularSpeed * dt;
			float num6 = MathUtils.Cos(particle.Angle);
			float num7 = MathUtils.Sin(particle.Angle);
			Vector3 swirlCenter = SwirlCenter;
			Vector3 position = new Vector3(swirlCenter.X + num5 * num6, x3, swirlCenter.Z + num5 * num7);
			int x4 = Terrain.ToCell(position.X);
			int num8 = Terrain.ToCell(position.Y);
			int z = Terrain.ToCell(position.Z);
			if (num8 < 0 || num8 > 255)
			{
				particle.IsActive = false;
				continue;
			}
			int num9 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(x4, num8, z));
			if (BlocksManager.Blocks[num9].IsCollidable)
			{
				particle.IsActive = false;
				continue;
			}
			particle.Position = position;
			float num10 = MathUtils.Saturate(particle.TimeToLive / particle.InitialTimeToLive);
			float num11 = MathUtils.Lerp(1.2f, 0.7f, f);
			particle.Size = new Vector2(ParticleSize * particle.SizeMul * num11);
			particle.Color = Color.PremultiplyAlpha(new Color(rgb, (byte)(BaseAlpha * num10)));
			num2++;
		}
		m_toGenerate += SpawnRateMultiplier * dt * 12f;
		int num12 = MathUtils.Max(0, DesiredCount - num2);
		while (num12 > 0 && m_toGenerate >= 1f)
		{
			int num13 = -1;
			for (int j = 0; j < Particles.Length; j++)
			{
				if (!Particles[j].IsActive)
				{
					num13 = j;
					break;
				}
			}
			if (num13 < 0)
			{
				break;
			}
			Particle particle2 = Particles[num13];
			particle2.IsActive = true;
			float y2 = BaseY + m_random.Float(0f, 5f);
			float num14 = m_random.Float(0f, 6.283185f);
			float num15 = SpawnRadius * m_random.Float(0.85f, 1.2f);
			particle2.Angle = num14;
			particle2.AngularSpeed = Omega * m_random.Float(0.9f, 1.3f);
			particle2.RadialMul = m_random.Float(0.95f, 1.25f);
			particle2.RadialPulseAmp = m_random.Float(0.08f, 0.2f);
			particle2.RadialPhase = m_random.Float(0f, 6.283185f);
			particle2.RadialPulseSpeed = m_random.Float(0.25f, 0.55f);
			float num16 = MathUtils.Max(10f, TopY - BaseY);
			float num17 = m_random.Float(0f, 1f);
			num17 *= num17;
			particle2.HeightBase = BaseY + MathUtils.Lerp(0f, num16 * 0.75f, num17);
			particle2.HeightAmp = m_random.Float(num16 * 0.1f, num16 * 0.22f);
			particle2.HeightPhase = m_random.Float(0f, 6.283185f);
			particle2.HeightSpeed = m_random.Float(0.15f, 0.35f);
			particle2.Position = new Vector3(SwirlCenter.X + num15 * MathUtils.Cos(num14), y2, SwirlCenter.Z + num15 * MathUtils.Sin(num14));
			particle2.Age = 0f;
			particle2.TimeToLive = m_random.Float(LifetimeMinMax.X, LifetimeMinMax.Y);
			particle2.InitialTimeToLive = particle2.TimeToLive;
			particle2.SizeMul = m_random.Float(0.5f, 1f);
			particle2.Size = new Vector2(ParticleSize * particle2.SizeMul);
			particle2.TextureSlot = m_random.Int(0, 15);
			particle2.FlipX = m_random.Bool();
			particle2.FlipY = m_random.Bool();
			particle2.Color = Color.PremultiplyAlpha(new Color(rgb, (byte)BaseAlpha));
			m_toGenerate--;
			num12--;
		}
		m_visible = false;
		if (IsStopped)
		{
			return num2 == 0;
		}
		return false;
	}

	public override void Draw(Camera camera)
	{
		float num = Vector3.Dot(m_position - camera.ViewPosition, camera.ViewDirection);
		if (num > -0.5f && num <= m_maxVisibilityDistance && Vector3.DistanceSquared(m_position, camera.ViewPosition) <= m_maxVisibilityDistance * m_maxVisibilityDistance)
		{
			m_visible = true;
			base.Draw(camera);
		}
		else
		{
			m_visible = false;
		}
	}
}
