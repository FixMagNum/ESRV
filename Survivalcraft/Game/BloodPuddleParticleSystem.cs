using Engine;
using Engine.Graphics;

namespace Game;

public class BloodPuddleParticleSystem : ParticleSystem<BloodPuddleParticleSystem.Particle>
{
	public class Particle : Game.Particle
	{
		public float Age;

		public float StartSize;

		public float MaxSize;

		public int HitFace;
	}

	private const float GrowTime = 0.8f;

	private const float FadeStart = 2f;

	private const float FadeDuration = 20f;

	private readonly SubsystemTerrain m_subsystemTerrain;

	private TexturedBatch3D m_batch;

	public Random m_random;

	private SubsystemSky m_subsystemSky;

	public BloodPuddleParticleSystem(SubsystemTerrain subsystemTerrain, Vector3 position, int hitFace)
		: base(32)
	{
		if (SettingsManager.DisplayBloodParticles)
		{
			m_random = new Random();
			m_subsystemTerrain = subsystemTerrain;
			Texture = ContentManager.Get<Texture2D>("Textures/BloodPuddleParticle");
			TextureSlotsCount = 1;
			AddPuddle(position, hitFace);
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
			if (particle.Age < 2f)
			{
				float f = particle.Age / 2f;
				float v = MathUtils.Lerp(particle.StartSize, particle.MaxSize, f);
				particle.Size = new Vector2(v);
			}
			else
			{
				particle.Size = new Vector2(particle.MaxSize);
			}
			byte a = byte.MaxValue;
			if (particle.Age > 2f)
			{
				float num = (particle.Age - 2f) / 20f;
				a = (byte)(MathUtils.Saturate(1f - num) * 255f);
			}
			float num2;
			if (SettingsManager.SkyLightUseParticles)
			{
				int cellLight = m_subsystemTerrain.Terrain.GetCellLight(Terrain.ToCell(particle.Position.X), Terrain.ToCell(particle.Position.Y), Terrain.ToCell(particle.Position.Z));
				num2 = LightingManager.LightIntensityByLightValue[cellLight];
			}
			else
			{
				if (m_subsystemSky == null)
				{
					m_subsystemSky = m_subsystemTerrain.Project.FindSubsystem<SubsystemSky>(throwOnError: true);
				}
				num2 = MathUtils.Clamp(m_subsystemSky.SkyLightIntensity, 0.2f, 1f);
			}
			byte r = (byte)(120f * num2);
			particle.Color = Color.PremultiplyAlpha(new Color(r, (byte)0, (byte)0, a));
			if (MathUtils.Remainder(particle.Age, 0.5f) < dt)
			{
				Vector3 vector = particle.Position - CellFace.FaceToVector3(particle.HitFace) * 0.1f;
				int num3 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(Terrain.ToCell(vector.X), Terrain.ToCell(vector.Y), Terrain.ToCell(vector.Z)));
				if (!BlocksManager.Blocks[num3].IsCollidable)
				{
					particle.IsActive = false;
					continue;
				}
			}
			if (particle.Age >= 22f)
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
			if (particle.IsActive)
			{
				Vector3 vector = CellFace.FaceToVector3(particle.HitFace);
				Vector3 vector2 = Vector3.Normalize(Vector3.Cross((MathUtils.Abs(vector.Y) > 0.9f) ? Vector3.UnitZ : Vector3.UnitY, vector));
				Vector3 vector3 = Vector3.Normalize(Vector3.Cross(vector, vector2));
				Vector3 vector4 = vector2 * particle.Size.X * 0.5f;
				Vector3 vector5 = vector3 * particle.Size.Y * 0.5f;
				Vector3 vector6 = particle.Position + vector * 0.002f;
				Vector3 p = vector6 - vector4 - vector5;
				Vector3 p2 = vector6 + vector4 - vector5;
				Vector3 p3 = vector6 + vector4 + vector5;
				Vector3 p4 = vector6 - vector4 + vector5;
				m_batch.QueueQuad(p, p2, p3, p4, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(0f, 0f), particle.Color);
			}
		}
	}

	private void AddPuddle(Vector3 position, int hitFace)
	{
		for (int i = 0; i < Particles.Length; i++)
		{
			Particle particle = Particles[i];
			if (particle.IsActive)
			{
				continue;
			}
			float num = m_random.Float(0.325f, 1.65f);
			particle.IsActive = true;
			particle.Position = position;
			particle.HitFace = hitFace;
			particle.StartSize = 0.15f * num;
			particle.MaxSize = particle.StartSize * 4f;
			particle.Size = new Vector2(particle.StartSize);
			particle.Age = 0f;
			float num2;
			if (SettingsManager.SkyLightUseParticles)
			{
				int cellLight = m_subsystemTerrain.Terrain.GetCellLight(Terrain.ToCell(position.X), Terrain.ToCell(position.Y), Terrain.ToCell(position.Z));
				num2 = LightingManager.LightIntensityByLightValue[cellLight];
			}
			else
			{
				if (m_subsystemSky == null)
				{
					m_subsystemSky = m_subsystemTerrain.Project.FindSubsystem<SubsystemSky>(throwOnError: true);
				}
				num2 = MathUtils.Clamp(m_subsystemSky.SkyLightIntensity, 0.2f, 1f);
			}
			byte r = (byte)(120f * num2);
			particle.Color = Color.PremultiplyAlpha(new Color(r, (byte)0, (byte)0, byte.MaxValue));
			break;
		}
	}
}
