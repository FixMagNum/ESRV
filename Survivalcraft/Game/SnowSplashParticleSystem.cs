using Engine;
using Engine.Graphics;

namespace Game;

public class SnowSplashParticleSystem : ParticleSystem<SnowSplashParticleSystem.Particle>
{
	public class Particle : Game.Particle
	{
		public float TimeToLive;

		public Color BaseColor;

		public float FadeFactor;

		public int HitFace;

		public Vector3 FaceNormal;
	}

	public Random m_random = new Random();

	public bool m_isActive;

	private TexturedBatch3D m_batch;

	public SnowSplashParticleSystem()
		: base(1000)
	{
		Texture = ContentManager.Get<Texture2D>("Textures/Weather/SnowParticle");
		TextureSlotsCount = 4;
	}

	public override bool Simulate(float dt)
	{
		if (m_isActive)
		{
			dt = MathUtils.Clamp(dt, 0f, 0.1f);
			float cellValue = 1f - SettingsManager.StagePrecipitation;
			bool flag = false;
			for (int i = 0; i < Particles.Length; i++)
			{
				Particle particle = Particles[i];
				if (particle.IsActive)
				{
					particle.Color = particle.BaseColor * MathUtils.Saturate(particle.FadeFactor * particle.TimeToLive) * cellValue;
					particle.TimeToLive -= dt;
					if (particle.TimeToLive <= 0f)
					{
						particle.IsActive = false;
					}
					else
					{
						flag = true;
					}
				}
			}
			if (!flag)
			{
				m_isActive = false;
			}
		}
		return false;
	}

	public void AddSplash(int value, Vector3 position, Vector2 size, Color color, int textureSlot, int hitFace)
	{
		for (int i = 0; i < Particles.Length; i++)
		{
			Particle particle = Particles[i];
			if (!particle.IsActive)
			{
				Block block = BlocksManager.Blocks[Terrain.ExtractContents(value)];
				Vector3 vector = CellFace.FaceToVector3(hitFace);
				float num = ((block is FocalorsWaterBlock) ? 0.05f : 0f);
				particle.IsActive = true;
				particle.Position = position + vector * (0.002f + num);
				particle.FaceNormal = vector;
				particle.Size = size * 2f;
				particle.TextureSlot = textureSlot;
				particle.BaseColor = color;
				particle.HitFace = hitFace;
				particle.TimeToLive = ((block is FocalorsWaterBlock) ? m_random.Float(0.2f, 0.3f) : m_random.Float(0.8f, 1.2f));
				particle.FadeFactor = 1f;
				m_isActive = true;
				break;
			}
		}
	}

	private ParticleBillboardingMode GetBillboardingModeFromHitFace(int hitFace)
	{
		if (hitFace <= 1)
		{
			return ParticleBillboardingMode.Vertical;
		}
		return ParticleBillboardingMode.Horizontal;
	}

	private Vector3 GetFaceOffset(int hitFace, float offset)
	{
		return hitFace switch
		{
			0 => new Vector3(0f, 0f - offset, 0f), 
			1 => new Vector3(0f, offset, 0f), 
			2 => new Vector3(0f, 0f, offset), 
			3 => new Vector3(0f, 0f, 0f - offset), 
			4 => new Vector3(offset, 0f, 0f), 
			5 => new Vector3(0f - offset, 0f, 0f), 
			_ => Vector3.Zero, 
		};
	}

	public override void Draw(Camera camera)
	{
		if (!m_isActive || Texture == null)
		{
			return;
		}
		if (m_batch == null)
		{
			m_batch = SubsystemParticles.PrimitivesRenderer.TexturedBatch(Texture, useAlphaTest: false, 0, DepthStencilState.DepthRead, null, BlendState.AlphaBlend, SamplerState.PointClamp);
		}
		float num = 1f - SettingsManager.StagePrecipitation;
		for (int i = 0; i < Particles.Length; i++)
		{
			Particle particle = Particles[i];
			if (particle.IsActive)
			{
				Vector3 faceNormal = particle.FaceNormal;
				Vector3 vector = Vector3.Normalize(Vector3.Cross((MathUtils.Abs(faceNormal.Y) > 0.9f) ? Vector3.UnitX : Vector3.UnitY, faceNormal));
				Vector3 vector2 = Vector3.Normalize(Vector3.Cross(faceNormal, vector));
				Vector3 vector3 = vector * particle.Size.X * 0.5f;
				Vector3 vector4 = vector2 * particle.Size.Y * 0.5f;
				Vector3 vector5 = particle.Position + faceNormal * 0.0005f;
				Vector3 p = vector5 - vector3 - vector4;
				Vector3 p2 = vector5 + vector3 - vector4;
				Vector3 p3 = vector5 + vector3 + vector4;
				Vector3 p4 = vector5 - vector3 + vector4;
				int num2 = particle.TextureSlot % 4;
				int num3 = particle.TextureSlot / 4;
				Vector2 vector6 = new Vector2((float)num2 * 0.25f, (float)num3 * 0.25f);
				m_batch.QueueQuad(p, p2, p3, p4, vector6 + new Vector2(0f, 0.25f), vector6 + new Vector2(0.25f, 0.25f), vector6 + new Vector2(0.25f, 0f), vector6 + new Vector2(0f, 0f), particle.Color * num);
			}
		}
	}
}
