using Engine;
using Engine.Graphics;

namespace Game;

public class CycloneDebrisFlybyParticleSystem : ParticleSystem<CycloneDebrisFlybyParticleSystem.Particle>
{
	public class Particle : Game.Particle
	{
		public float TimeToLive;

		public float InitialTimeToLive;

		public float Age;

		public Vector3 Velocity;

		public float SideDrift;

		public float SideDrift2;

		public float VerticalDrift;

		public float WobbleFreq;

		public float WobblePhase;

		public float SizeMul;
	}

	private float m_toGenerate;

	private Random m_random;

	private SubsystemTerrain m_subsystemTerrain;

	public bool m_visible;

	public Vector3 m_anchorPosition;

	public float m_age;

	public Vector3 AnchorPosition { get; set; }

	public Vector3 WindVelocity { get; set; }

	public float SpawnRatePerSecond { get; set; }

	public float SpawnBackOffset { get; set; }

	public Vector2 LifetimeMinMax { get; set; }

	public float ParticleSize { get; set; }

	public float BaseAlpha { get; set; }

	public Vector3 SpawnAreaHalfSize { get; set; }

	public int MaxCount { get; set; }

	public float MaxVisibilityDistance { get; set; }

	public Color Color { get; set; } = Color.White;

	public bool IsStopped { get; set; }

	public CycloneDebrisFlybyParticleSystem(SubsystemTerrain terrain, Vector3 anchorPosition, float maxVisibilityDistance)
		: base(200)
	{
		m_subsystemTerrain = terrain;
		m_random = new Random();
		AnchorPosition = anchorPosition;
		m_anchorPosition = anchorPosition;
		MaxVisibilityDistance = MathUtils.Max(40f, maxVisibilityDistance);
		Texture = ContentManager.Get<Texture2D>("Textures/TornadoDebrisParticle");
		TextureSlotsCount = 4;
		Color = new Color(0.2f, 0.2f, 0.2f);
		BaseAlpha = 200f;
		ParticleSize = 0.35f;
		SpawnRatePerSecond = 6f;
		LifetimeMinMax = new Vector2(2.2f, 5f);
		SpawnAreaHalfSize = new Vector3(2.5f, 1.2f, 0.8f);
		SpawnBackOffset = 8f;
		MaxCount = 160;
	}

	public override bool Simulate(float dt)
	{
		if (IsStopped || (!m_visible && m_age >= 1.5f))
		{
			return true;
		}
		dt = MathUtils.Clamp(dt, 0f, 0.1f);
		int x = Terrain.ToCell(m_anchorPosition.X);
		int y = Terrain.ToCell(m_anchorPosition.Y);
		int z = Terrain.ToCell(m_anchorPosition.Z);
		int cellLight = m_subsystemTerrain.Terrain.GetCellLight(x, y, z);
		float num = MathUtils.Max(LightingManager.LightIntensityByLightValue[cellLight], 0.6f);
		Color rgb = new Color(num, num, num) * Color;
		int num2 = 0;
		for (int i = 0; i < Particles.Length; i++)
		{
			Particle particle = Particles[i];
			if (particle.IsActive)
			{
				particle.TimeToLive -= dt;
				particle.Age += dt;
				if (particle.TimeToLive <= 0f)
				{
					particle.IsActive = false;
					continue;
				}
				float num3 = 0.7f + 0.3f * MathUtils.Sin(particle.Age * particle.WobbleFreq + particle.WobblePhase);
				Vector3 velocity = particle.Velocity;
				velocity.Y += particle.VerticalDrift;
				velocity.X += particle.SideDrift * num3;
				velocity.Z += particle.SideDrift2 * num3;
				particle.Position += velocity * dt;
				float num4 = MathUtils.Saturate(particle.TimeToLive / particle.InitialTimeToLive);
				particle.Size = new Vector2(ParticleSize * particle.SizeMul);
				particle.Color = Color.PremultiplyAlpha(new Color(rgb, (byte)(BaseAlpha * num4)));
				num2++;
			}
		}
		m_toGenerate += MathUtils.Max(0f, SpawnRatePerSecond) * dt;
		int num5 = MathUtils.Max(0, MaxCount - num2);
		Vector3 windVelocity = WindVelocity;
		float num6 = windVelocity.Length();
		if (num6 < 0.0001f)
		{
			m_toGenerate = 0f;
			m_visible = false;
			if (IsStopped)
			{
				return num2 == 0;
			}
			return false;
		}
		windVelocity /= num6;
		Vector3 vector = windVelocity;
		vector.Y = 0f;
		float num7 = vector.Length();
		if (num7 < 0.0001f)
		{
			vector = new Vector3(0f, 0f, 1f);
		}
		else
		{
			vector /= num7;
		}
		Vector3 vector2 = new Vector3(0f - vector.Z, 0f, vector.X);
		Vector3 unitY = Vector3.UnitY;
		while (num5 > 0 && m_toGenerate >= 1f)
		{
			int num8 = -1;
			for (int j = 0; j < Particles.Length; j++)
			{
				if (!Particles[j].IsActive)
				{
					num8 = j;
					break;
				}
			}
			if (num8 < 0)
			{
				break;
			}
			Particle particle2 = Particles[num8];
			particle2.IsActive = true;
			float num9 = m_random.Float(0f - SpawnAreaHalfSize.X, SpawnAreaHalfSize.X);
			float num10 = m_random.Float(0f - SpawnAreaHalfSize.Y, SpawnAreaHalfSize.Y);
			float num11 = m_random.Float(0f - SpawnAreaHalfSize.Z, SpawnAreaHalfSize.Z);
			Vector3 vector3 = AnchorPosition - vector * SpawnBackOffset;
			particle2.Position = vector3 + vector2 * num9 + unitY * num10 + vector * num11;
			float num12 = m_random.Float(1.5f, 2.3f);
			particle2.Velocity = WindVelocity * num12;
			particle2.SideDrift = m_random.Float(-0.8f, 0.8f) * 0.5f;
			particle2.SideDrift2 = m_random.Float(-0.8f, 0.8f) * 0.5f;
			particle2.VerticalDrift = m_random.Float(-0.2f, 0.35f) * 0.5f;
			particle2.WobbleFreq = m_random.Float(3f, 7f);
			particle2.WobblePhase = m_random.Float(0f, 6.283185f);
			particle2.Age = 0f;
			particle2.InitialTimeToLive = m_random.Float(LifetimeMinMax.X, LifetimeMinMax.Y);
			particle2.TimeToLive = particle2.InitialTimeToLive;
			particle2.SizeMul = m_random.Float(0.5f, 1.1f);
			particle2.Size = new Vector2(ParticleSize * particle2.SizeMul);
			particle2.TextureSlot = m_random.Int(0, 15);
			particle2.FlipX = m_random.Bool();
			particle2.FlipY = m_random.Bool();
			particle2.Color = Color.PremultiplyAlpha(new Color(rgb, (byte)BaseAlpha));
			m_toGenerate--;
			num5--;
			num2++;
		}
		m_visible = num2 > 0;
		if (IsStopped)
		{
			return num2 == 0;
		}
		return false;
	}

	public override void Draw(Camera camera)
	{
		float num = Vector3.Dot(m_anchorPosition - camera.ViewPosition, camera.ViewDirection);
		if (num > -0.5f && num <= MaxVisibilityDistance && Vector3.DistanceSquared(m_anchorPosition, camera.ViewPosition) <= MaxVisibilityDistance * MaxVisibilityDistance)
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
