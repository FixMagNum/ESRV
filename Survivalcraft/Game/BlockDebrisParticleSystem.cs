using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class BlockDebrisParticleSystem : ParticleSystem<BlockDebrisParticleSystem.Particle>
{
	public class Particle : Game.Particle
	{
		public Vector3 Velocity;

		public float TimeToLive;

		public Vector3 AngularVelocity;

		public Quaternion Orientation;
	}

	public Random m_random = new Random();

	public SubsystemTerrain m_subsystemTerrain;

	private TexturedBatch3D m_batch3D;

	private readonly bool m_is3D;

	public BlockDebrisParticleSystem(SubsystemTerrain terrain, Vector3 position, float strength, float scale, Color color, int textureSlot)
		: base((int)(50f * strength))
	{
		if (!SettingsManager.BlockBreakParticles)
		{
			return;
		}
		m_subsystemTerrain = terrain;
		m_is3D = SettingsManager.BlockDebris3D;
		Texture = terrain.Project.FindSubsystem<SubsystemBlocksTexture>(throwOnError: true).BlocksTexture;
		int num = Terrain.ToCell(position.X);
		int num2 = Terrain.ToCell(position.Y);
		int num3 = Terrain.ToCell(position.Z);
		int x = 0;
		x = MathUtils.Max(x, terrain.Terrain.GetCellLight(num + 1, num2, num3));
		x = MathUtils.Max(x, terrain.Terrain.GetCellLight(num - 1, num2, num3));
		x = MathUtils.Max(x, terrain.Terrain.GetCellLight(num, num2 + 1, num3));
		x = MathUtils.Max(x, terrain.Terrain.GetCellLight(num, num2 - 1, num3));
		x = MathUtils.Max(x, terrain.Terrain.GetCellLight(num, num2, num3 + 1));
		x = MathUtils.Max(x, terrain.Terrain.GetCellLight(num, num2, num3 - 1));
		TextureSlotsCount = 32;
		float num4 = LightingManager.LightIntensityByLightValue[x];
		color *= num4;
		color.A = byte.MaxValue;
		float num5 = MathUtils.Sqrt(strength);
		for (int i = 0; i < Particles.Length; i++)
		{
			Particle particle = Particles[i];
			particle.IsActive = true;
			Vector3 vector = new Vector3(m_random.Float(-1f, 1f), m_random.Float(-1f, 1f), m_random.Float(-1f, 1f));
			particle.Position = position + strength * 0.45f * vector;
			particle.Color = Color.MultiplyColorOnly(color, m_random.Float(0.7f, 1f));
			float num6 = (m_is3D ? 1.6f : 1f);
			float num7 = m_random.Float(0.6f, 1.6f);
			float v = num5 * scale * 0.05f * num6 * num7;
			particle.Size = new Vector2(v);
			particle.TimeToLive = num5 * m_random.Float(1f, 3f);
			particle.Velocity = num5 * 2f * (vector + new Vector3(m_random.Float(-0.2f, 0.2f), 0.6f, m_random.Float(-0.2f, 0.2f)));
			if (m_is3D)
			{
				particle.AngularVelocity = m_random.Vector3(1f) * 4f;
				particle.Orientation = Quaternion.Identity;
			}
			else
			{
				particle.AngularVelocity = Vector3.Zero;
				particle.Orientation = Quaternion.Identity;
			}
			if (m_is3D)
			{
				int num8 = textureSlot % 16 * 2 + m_random.Int(0, 1);
				int num9 = textureSlot / 16 * 2 + m_random.Int(0, 1);
				particle.TextureSlot = num8 + num9 * 32;
			}
			else
			{
				particle.TextureSlot = textureSlot % 16 * 2 + m_random.Int(0, 1) + 32 * (textureSlot / 16 * 2 + m_random.Int(0, 1));
			}
		}
	}

	public override bool Simulate(float dt)
	{
		dt = MathUtils.Clamp(dt, 0f, 0.1f);
		float num = MathUtils.Pow(0.1f, dt);
		bool flag = false;
		for (int i = 0; i < Particles.Length; i++)
		{
			Particle particle = Particles[i];
			if (!particle.IsActive)
			{
				continue;
			}
			flag = true;
			particle.TimeToLive -= dt;
			if (particle.TimeToLive > 0f)
			{
				Vector3 position = particle.Position;
				Vector3 vector = position + particle.Velocity * dt;
				if (m_is3D)
				{
					particle.Orientation *= Quaternion.CreateFromYawPitchRoll(particle.AngularVelocity.Y * dt, particle.AngularVelocity.X * dt, particle.AngularVelocity.Z * dt);
				}
				TerrainRaycastResult? terrainRaycastResult = m_subsystemTerrain.Raycast(position, vector, useInteractionBoxes: false, skipAirBlocks: true, (int value, float distance) => BlocksManager.Blocks[Terrain.ExtractContents(value)].IsCollidable);
				if (terrainRaycastResult.HasValue)
				{
					Plane plane = terrainRaycastResult.Value.CellFace.CalculatePlane();
					vector = position;
					Vector3 normal = plane.Normal;
					particle.Velocity -= 2f * Vector3.Dot(particle.Velocity, normal) * normal;
					particle.Velocity *= 0.25f;
					particle.AngularVelocity *= 0.5f;
					if (plane.Normal.X != 0f)
					{
						particle.Velocity *= new Vector3(-0.25f, 0.25f, 0.25f);
					}
					if (plane.Normal.Y != 0f)
					{
						particle.Velocity *= new Vector3(0.25f, -0.25f, 0.25f);
					}
					if (plane.Normal.Z != 0f)
					{
						particle.Velocity *= new Vector3(0.25f, 0.25f, -0.25f);
					}
				}
				particle.Position = vector;
				Particle particle2 = particle;
				particle2.Velocity.Y = particle2.Velocity.Y + -9.81f * dt;
				particle.Velocity *= num;
				particle.Color *= MathUtils.Saturate(particle.TimeToLive);
			}
			else
			{
				particle.IsActive = false;
			}
		}
		return !flag;
	}

	public override void Draw(Camera cam)
	{
		if (!m_is3D)
		{
			base.Draw(cam);
			return;
		}
		if (m_batch3D == null)
		{
			m_batch3D = m_subsystemTerrain.Project.FindSubsystem<SubsystemParticles>(throwOnError: true).PrimitivesRenderer.TexturedBatch(Texture, useAlphaTest: false, 0, DepthStencilState.DepthRead, null, BlendState.NonPremultiplied, SamplerState.PointClamp);
		}
		SubsystemMiningLightBehavior num = m_subsystemTerrain.Project.FindSubsystem<SubsystemMiningLightBehavior>(throwOnError: false);
		List<SubsystemMiningLightBehavior.MiningLightData> list = null;
		if (num != null && (num.ActiveLights.Count > 0 || num.ActiveHeadLights.Count > 0))
		{
			list = new List<SubsystemMiningLightBehavior.MiningLightData>();
			list.AddRange(num.ActiveLights.Values);
			list.AddRange(num.ActiveHeadLights.Values);
		}
		for (int i = 0; i < Particles.Length; i++)
		{
			Particle particle = Particles[i];
			if (!particle.IsActive)
			{
				continue;
			}
			float num2 = particle.Size.X * 0.5f;
			Matrix m = Matrix.CreateFromQuaternion(particle.Orientation);
			Vector3 vector = Vector3.Transform(Vector3.UnitX * num2, m);
			Vector3 vector2 = Vector3.Transform(Vector3.UnitY * num2, m);
			Vector3 vector3 = Vector3.Transform(Vector3.UnitZ * num2, m);
			Vector3 position = particle.Position;
			Color color = particle.Color;
			if (list != null)
			{
				Vector3 zero = Vector3.Zero;
				foreach (SubsystemMiningLightBehavior.MiningLightData item in list)
				{
					float num3 = Vector3.DistanceSquared(position, item.Position);
					float num4 = item.Intensity * 1.5f;
					if (num3 < num4 * num4)
					{
						float num5 = MathUtils.Sqrt(num3);
						float num6 = MathUtils.Max(0f, 1f - num5 / num4);
						num6 *= num6;
						Vector3 vector4 = new Vector3(0.65f, 0.65f, 0.65f) * 0.55f;
						zero += vector4 * num6;
					}
				}
				Vector3 vector5 = new Vector3((int)color.R, (int)color.G, (int)color.B) / 255f + zero;
				color = new Color(MathUtils.Min(vector5.X, 1f), MathUtils.Min(vector5.Y, 1f), MathUtils.Min(vector5.Z, 1f), (float)(int)color.A / 255f);
			}
			int num7 = i * 7;
			QueueFace(position - vector - vector2 - vector3, position + vector - vector2 - vector3, position + vector + vector2 - vector3, position - vector + vector2 - vector3, particle, color, num7);
			QueueFace(position - vector - vector2 + vector3, position + vector - vector2 + vector3, position + vector + vector2 + vector3, position - vector + vector2 + vector3, particle, color, num7 + 1);
			QueueFace(position - vector - vector2 - vector3, position - vector - vector2 + vector3, position - vector + vector2 + vector3, position - vector + vector2 - vector3, particle, color, num7 + 2);
			QueueFace(position + vector - vector2 - vector3, position + vector - vector2 + vector3, position + vector + vector2 + vector3, position + vector + vector2 - vector3, particle, color, num7 + 3);
			QueueFace(position - vector - vector2 - vector3, position + vector - vector2 - vector3, position + vector - vector2 + vector3, position - vector - vector2 + vector3, particle, color, num7 + 4);
			QueueFace(position - vector + vector2 - vector3, position + vector + vector2 - vector3, position + vector + vector2 + vector3, position - vector + vector2 + vector3, particle, color, num7 + 5);
		}
	}

	private void QueueFace(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Particle p, Color color, int seed)
	{
		int textureSlot = p.TextureSlot;
		float num = (float)(textureSlot & 0x1F) * (1f / 32f);
		float num2 = (float)(textureSlot >> 5) * (1f / 32f);
		float num3 = 1f / 32f;
		int num4 = (seed ^ p.GetHashCode()) * 486187739;
		float num5 = (float)(num4 & 0xFF) / 255f;
		float num6 = (float)((num4 >> 8) & 0xFF) / 255f;
		float num7 = 0.25f;
		float num8 = 1f - num7;
		float num9 = num + num5 * num8 * num3;
		float num10 = num2 + num6 * num8 * num3;
		float x = num9 + num3 * num7;
		float y = num10 + num3 * num7;
		m_batch3D.QueueQuad(d, c, b, a, new Vector2(num9, y), new Vector2(x, y), new Vector2(x, num10), new Vector2(num9, num10), color);
	}
}
