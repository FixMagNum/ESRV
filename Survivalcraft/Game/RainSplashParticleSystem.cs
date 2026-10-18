using Engine;
using Engine.Graphics;

namespace Game;

public class RainSplashParticleSystem : ParticleSystem<RainSplashParticleSystem.Particle>
{
	public class Particle : Game.Particle
	{
		public Vector3 Velocity;

		public float Duration;

		public float TimeToLive;

		public Color BaseColor;

		public float BaseSize1;

		public float BaseSize2;

		public float Gravity;

		public float FadeFactor;

		public int HitFace;

		public Vector3 FaceNormal;
	}

	public Random m_random = new Random();

	public bool m_isActive;

	private TexturedBatch3D m_batch;

	public RainSplashParticleSystem()
		: base(1500)
	{
		Texture = ContentManager.Get<Texture2D>("Textures/Weather/RainSplashParticle");
		TextureSlotsCount = 1;
	}

	public override bool Simulate(float dt)
	{
		if (m_isActive)
		{
			dt = MathUtils.Clamp(dt, 0f, 0.1f);
			float i = 1f - SettingsManager.StagePrecipitation;
			float x = MathUtils.Pow(0.0005f, dt);
			bool z = false;
			for (int y = 0; y < Particles.Length; y++)
			{
				Particle result = Particles[y];
				if (result.IsActive)
				{
					result.Position += result.Velocity * dt;
					Particle particle = result;
					particle.Velocity.Y = particle.Velocity.Y + result.Gravity * dt;
					result.Velocity *= x;
					result.Size = new Vector2(MathUtils.Lerp(result.BaseSize1, result.BaseSize2, (result.Duration - result.TimeToLive) / result.Duration));
					result.Color = result.BaseColor * MathUtils.Saturate(result.FadeFactor * result.TimeToLive) * i;
					result.TimeToLive -= dt;
					result.FlipX = m_random.Bool();
					result.FlipY = m_random.Bool();
					if (result.TimeToLive <= 0f)
					{
						result.IsActive = false;
					}
					else
					{
						z = true;
					}
				}
			}
			if (!z)
			{
				m_isActive = false;
			}
		}
		return false;
	}

	public void AddSplash(int value, Vector3 position, Color color, int hitFace)
	{
		for (int x = 0; x < Particles.Length; x++)
		{
			Particle num = Particles[x];
			if (!num.IsActive)
			{
				Block z = BlocksManager.Blocks[Terrain.ExtractContents(value)];
				num.IsActive = true;
				num.Position = position + CellFace.FaceToVector3(hitFace) * 0.002f;
				num.BaseColor = color;
				num.HitFace = hitFace;
				num.FaceNormal = CellFace.FaceToVector3(hitFace);
				num.Velocity = Vector3.Zero;
				num.Gravity = 0f;
				if (z is FocalorsWaterBlock || z is LayeredIceBlock)
				{
					num.BaseSize1 = 0.02f;
					num.BaseSize2 = 0.09f;
					num.Duration = (num.TimeToLive = m_random.Float(0.6f, 0.8f));
					num.FadeFactor = 1.6f;
				}
				else if (z is CrossBlock)
				{
					num.Gravity = -14.81f;
					num.BaseSize1 = m_random.Float(0.015f, 0.025f);
					num.BaseSize2 = num.BaseSize1;
					num.Duration = (num.TimeToLive = m_random.Float(0.4f, 0.6f));
					num.FadeFactor = 2.8f;
					SubsystemWeather subsystemWeather = SubsystemParticles.Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
					Vector2 point2 = subsystemWeather.CurrentWindVector;
					float currentWindStrength = subsystemWeather.CurrentWindStrength;
					Vector3 velocity = new Vector3(point2.X, 0f, point2.Y) * (currentWindStrength * 0.1f);
					num.Velocity = velocity;
				}
				else if (z.IsCollidable)
				{
					num.BaseSize1 = 0.03f;
					num.BaseSize2 = 0.08f;
					num.Duration = (num.TimeToLive = m_random.Float(0.25f, 0.3f));
					num.FadeFactor = 2.8f;
				}
				else
				{
					num.BaseSize1 = m_random.Float(0.015f, 0.025f);
					num.BaseSize2 = num.BaseSize1;
					num.Duration = (num.TimeToLive = m_random.Float(0.25f, 0.3f));
					num.FadeFactor = 2.8f;
				}
				float chunkAtCell = SubsystemParticles.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).m_lightningIntensity;
				float i = MathUtils.Lerp(0.8f, 2f, MathUtils.Saturate(chunkAtCell));
				num.BaseSize1 *= i;
				num.BaseSize2 *= i;
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

	private Vector3 GetFaceNormal(int hitFace)
	{
		return hitFace switch
		{
			0 => Vector3.UnitY, 
			1 => Vector3.UnitY, 
			2 => Vector3.UnitZ, 
			3 => -Vector3.UnitZ, 
			4 => Vector3.UnitX, 
			5 => -Vector3.UnitX, 
			_ => Vector3.UnitY, 
		};
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
		float max = 1f - SettingsManager.StagePrecipitation;
		for (int num = 0; num < Particles.Length; num++)
		{
			Particle num2 = Particles[num];
			if (num2.IsActive)
			{
				Vector3 faceNormal = num2.FaceNormal;
				Vector3 item = Vector3.Normalize(Vector3.Cross((MathUtils.Abs(faceNormal.Y) > 0.9f) ? Vector3.UnitX : Vector3.UnitY, faceNormal));
				Vector3 vector = Vector3.Normalize(Vector3.Cross(faceNormal, item));
				Vector2 size = num2.Size;
				item *= size.X * 0.5f;
				vector *= size.Y * 0.5f;
				float num3 = 0.0005f;
				Vector3 vector2 = num2.Position + faceNormal * num3;
				Vector3 p = vector2 - item - vector;
				Vector3 p2 = vector2 + item - vector;
				Vector3 p3 = vector2 + item + vector;
				Vector3 p4 = vector2 - item + vector;
				Color color = num2.Color * max;
				m_batch.QueueQuad(p, p2, p3, p4, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(0f, 0f), color);
			}
		}
	}
}
