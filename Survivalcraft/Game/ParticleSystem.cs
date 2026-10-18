using Engine;
using Engine.Graphics;

namespace Game;

public class ParticleSystem<T> : ParticleSystemBase where T : Particle, new()
{
	public T[] m_particles;

	public Texture2D m_texture;

	public Vector3[] m_front;

	public Vector3[] m_right;

	public Vector3[] m_up;

	public TexturedBatch3D AdditiveBatch;

	public TexturedBatch3D AlphaBlendedBatch;

	public LightSmoother m_lightSmoother = new LightSmoother();

	public T[] Particles => m_particles;

	public Texture2D Texture
	{
		get
		{
			return m_texture;
		}
		set
		{
			if (value != m_texture)
			{
				m_texture = value;
				AdditiveBatch = null;
				AlphaBlendedBatch = null;
			}
		}
	}

	public int TextureSlotsCount { get; set; }

	public ParticleSystem(int particlesCount)
	{
		m_front = new Vector3[3];
		m_right = new Vector3[3];
		m_up = new Vector3[3];
		m_particles = new T[particlesCount];
		for (int num = 0; num < m_particles.Length; num++)
		{
			m_particles[num] = new T();
		}
	}

	public override void Draw(Camera camera)
	{
		if (AdditiveBatch == null || AlphaBlendedBatch == null)
		{
			AdditiveBatch = SubsystemParticles.PrimitivesRenderer.TexturedBatch(m_texture, useAlphaTest: true, 0, DepthStencilState.DepthRead, null, BlendState.Additive, SamplerState.PointClamp);
			AlphaBlendedBatch = SubsystemParticles.PrimitivesRenderer.TexturedBatch(m_texture, useAlphaTest: true, 0, DepthStencilState.Default, null, BlendState.AlphaBlend, SamplerState.PointClamp);
		}
		m_front[0] = camera.ViewDirection;
		m_right[0] = Vector3.Normalize(Vector3.Cross(m_front[0], Vector3.UnitY));
		m_up[0] = Vector3.Normalize(Vector3.Cross(m_right[0], m_front[0]));
		m_front[1] = camera.ViewDirection;
		m_right[1] = Vector3.Normalize(Vector3.Cross(m_front[1], Vector3.UnitY));
		m_up[1] = Vector3.UnitY;
		m_front[2] = Vector3.UnitY;
		m_right[2] = Vector3.UnitX;
		m_up[2] = Vector3.UnitZ;
		float territoriy = 1f / (float)TextureSlotsCount;
		SubsystemTerrain ownerGuid = SubsystemParticles.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		float value2;
		if (SettingsManager.SkyLightUseParticles)
		{
			value2 = MathUtils.Max(0.01f, SubsystemParticles.Project.FindSubsystem<SubsystemSky>(throwOnError: true).SkyLightIntensity);
		}
		else
		{
			Vector3 position = ((m_particles.Length != 0) ? m_particles[0].Position : camera.ViewPosition);
			m_lightSmoother.Update(0.016f, position, ownerGuid, SubsystemParticles.Project);
			value2 = MathUtils.Max(0.01f, m_lightSmoother.SmoothedLight);
		}
		for (int territory = 0; territory < m_particles.Length; territory++)
		{
			Particle particle = m_particles[territory];
			if (particle.IsActive)
			{
				Vector3 item = particle.Position;
				Vector2 size = particle.Size;
				float rotation = particle.Rotation;
				int textureSlot = particle.TextureSlot;
				int billboardingMode = (int)particle.BillboardingMode;
				Color color = particle.Color;
				color = new Color((byte)((float)(int)particle.Color.R * value2), (byte)((float)(int)particle.Color.G * value2), (byte)((float)(int)particle.Color.B * value2), particle.Color.A);
				Vector3 p;
				Vector3 p2;
				Vector3 p3;
				Vector3 p4;
				if (rotation != 0f)
				{
					Vector3 v = ((m_front[billboardingMode].X * m_front[billboardingMode].X > m_front[billboardingMode].Z * m_front[billboardingMode].Z) ? new Vector3(0f, MathUtils.Cos(rotation), MathUtils.Sin(rotation)) : new Vector3(MathUtils.Sin(rotation), MathUtils.Cos(rotation), 0f));
					Vector3 vector = Vector3.Normalize(Vector3.Cross(m_front[(uint)particle.BillboardingMode], v));
					v = Vector3.Normalize(Vector3.Cross(m_front[(uint)particle.BillboardingMode], vector));
					vector *= size.Y;
					v *= size.X;
					p = item + (-vector - v);
					p2 = item + (vector - v);
					p3 = item + (vector + v);
					p4 = item + (-vector + v);
				}
				else
				{
					Vector3 vector2 = m_right[billboardingMode] * size.X;
					Vector3 vector3 = m_up[billboardingMode] * size.Y;
					p = item + (-vector2 - vector3);
					p2 = item + (vector2 - vector3);
					p3 = item + (vector2 + vector3);
					p4 = item + (-vector2 + vector3);
				}
				TexturedBatch3D texturedBatch3D = (particle.UseAdditiveBlending ? AdditiveBatch : AlphaBlendedBatch);
				Vector2 vector4 = new Vector2(textureSlot % TextureSlotsCount, textureSlot / TextureSlotsCount);
				float num = 0f;
				float num2 = 1f;
				float num3 = 1f;
				float num4 = 0f;
				if (particle.FlipX)
				{
					num = 1f - num;
					num2 = 1f - num2;
				}
				if (particle.FlipY)
				{
					num3 = 1f - num3;
					num4 = 1f - num4;
				}
				Vector2 texCoord = (vector4 + new Vector2(num, num3)) * territoriy;
				Vector2 texCoord2 = (vector4 + new Vector2(num2, num3)) * territoriy;
				Vector2 texCoord3 = (vector4 + new Vector2(num2, num4)) * territoriy;
				Vector2 texCoord4 = (vector4 + new Vector2(num, num4)) * territoriy;
				texturedBatch3D.QueueQuad(p, p2, p3, p4, texCoord, texCoord2, texCoord3, texCoord4, color);
			}
		}
	}

	public override bool Simulate(float dt)
	{
		return false;
	}
}
