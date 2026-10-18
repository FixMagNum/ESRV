using System;
using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class PrecipitationShaftParticleSystem : ParticleSystemBase
{
	public class Particle
	{
		public bool IsActive;

		public bool GenerateSplash;

		public byte TextureSlot;

		public Vector3 Position;

		public Vector2 TexCoord1;

		public Vector2 TexCoord2;

		public Vector2 TexCoord3;

		public Vector2 TexCoord4;

		public float Speed;

		public float YLimit;

		public Vector3 Velocity;

		public float RotationAngle;

		public Vector3 PreviousPosition;

		public int HitFace;

		public float DeviationAngle;
	}

	public const float m_viewHeight = 120f;

	public const int m_particlesCount = 40;

	public SubsystemWeather m_subsystemWeather;

	public GameWidget m_gameWidget;

	public Random m_random;

	public TexturedBatch3D m_batch;

	public PrecipitationType m_precipitationType;

	public bool m_isVisible;

	public bool m_isEmpty;

	public float? m_lastViewY;

	public float m_toCreate;

	public float m_averageSpeed;

	public Texture2D m_texture;

	public Vector2 m_size;

	public float m_intensity;

	public int m_yLimit;

	public int m_topmostValue;

	public int m_topmostBelowValue;

	public double m_lastUpdateTime;

	public float m_lastSkylightIntensity;

	public bool m_needsInitialize;

	private Vector3 m_velocity;

	private static readonly Dictionary<string, Vector2> WindDirectionVectors;

	private float minSpeed;

	private float maxSpeed;

	public float RainAccelerationPerLevel;

	public float SnowAccelerationPerLevel;

	private bool m_wasStormCondition;

	private int m_activeParticlesCount;

	public List<Particle> m_particles = new List<Particle>();

	public float SandAccelerationPerLevel;

	private SubsystemPickables m_subsystemPickables;

	private SubsystemGameInfo m_subsystemGameInfo;

	private double m_lastPickablesCheckTime = double.MinValue;

	private bool m_cachedFlag3;

	private Vector3 m_cachedVector3;

	public Point2 Point { get; set; }

	public PrecipitationShaftParticleSystem(GameWidget gameWidget, SubsystemWeather subsystemWeather, Random random, Point2 point, PrecipitationType precipitationType)
	{
		RainAccelerationPerLevel = 0.3f;
		SnowAccelerationPerLevel = 1f;
		SandAccelerationPerLevel = 1f;
		m_particles = new List<Particle>();
		m_activeParticlesCount = 0;
		m_lastUpdateTime = double.MinValue;
		m_lastSkylightIntensity = float.MinValue;
		m_gameWidget = gameWidget;
		m_subsystemWeather = subsystemWeather;
		m_random = random;
		Point = point;
		m_subsystemPickables = subsystemWeather.Project.FindSubsystem<SubsystemPickables>(throwOnError: true);
		m_subsystemGameInfo = subsystemWeather.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		UpdateParticleCount();
		m_precipitationType = precipitationType;
		for (int num = 0; num < m_activeParticlesCount; num++)
		{
			_ = m_particles[num];
		}
		Initialize();
	}

	public override bool Simulate(float dt)
	{
		if (SettingsManager.StagePrecipitation >= 1f)
		{
			return true;
		}
		if (m_subsystemWeather.SubsystemTime.GameTime - m_lastUpdateTime > 1.0 || MathUtils.Abs(m_lastSkylightIntensity - m_subsystemWeather.SubsystemSky.SkyLightIntensity) > 0.1f)
		{
			m_lastUpdateTime = m_subsystemWeather.SubsystemTime.GameTime;
			m_lastSkylightIntensity = m_subsystemWeather.SubsystemSky.SkyLightIntensity;
			PrecipitationShaftInfo precipitationShaftInfo = m_subsystemWeather.GetPrecipitationShaftInfo(Point.X, Point.Y);
			m_intensity = precipitationShaftInfo.Intensity;
			m_yLimit = precipitationShaftInfo.YLimit;
			m_topmostValue = m_subsystemWeather.SubsystemTerrain.Terrain.GetCellValue(Point.X, precipitationShaftInfo.YLimit - 1, Point.Y);
			m_topmostBelowValue = m_subsystemWeather.SubsystemTerrain.Terrain.GetCellValue(Point.X, precipitationShaftInfo.YLimit - 2, Point.Y);
		}
		if (m_intensity <= 0f && m_isEmpty)
		{
			return false;
		}
		float num = m_subsystemWeather.CurrentWindStrength;
		string num2 = m_subsystemWeather.WindDirection;
		float num3 = 1f;
		int num4 = (int)num - 12;
		if (num4 > 0)
		{
			if (m_precipitationType == PrecipitationType.Rain)
			{
				num3 = 1f + RainAccelerationPerLevel * (float)num4;
			}
			else if (m_precipitationType == PrecipitationType.Snow)
			{
				num3 = 15f + SnowAccelerationPerLevel * (float)num4;
			}
			else if (m_precipitationType == PrecipitationType.Sand)
			{
				num3 = 5f + SandAccelerationPerLevel * (float)num4;
			}
		}
		Vector2 cell = (WindDirectionVectors.ContainsKey(num2) ? WindDirectionVectors[num2] : new Vector2(0f, -1f));
		bool flag = IsStormCondition();
		if (flag != m_wasStormCondition)
		{
			m_needsInitialize = true;
			m_wasStormCondition = flag;
		}
		UpdateParticleCount();
		Camera activeCamera = m_gameWidget.ActiveCamera;
		bool flag2 = false;
		if (m_subsystemGameInfo != null && m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace)
		{
			flag2 = true;
		}
		bool renderWeatherPrecipitationOutsideView = SettingsManager.RenderWeatherPrecipitationOutsideView;
		float intensity = m_intensity;
		if (!m_isEmpty || (intensity > 0f && (flag2 || (float)m_yLimit < activeCamera.ViewPosition.Y + 5f)))
		{
			if (renderWeatherPrecipitationOutsideView)
			{
				m_isVisible = true;
			}
			else
			{
				Vector2 v = Vector2.Normalize(new Vector2(activeCamera.ViewDirection.X, activeCamera.ViewDirection.Z));
				Vector2 v2 = Vector2.Normalize(new Vector2((float)Point.X + 0.5f - activeCamera.ViewPosition.X + 0.7f * v.X, (float)Point.Y + 0.5f - activeCamera.ViewPosition.Z + 0.7f * v.Y));
				float num5 = Vector2.Dot(v, v2);
				m_isVisible = num5 > 0.5f;
			}
			if (m_isVisible)
			{
				bool flag3 = false;
				Vector3 vector = Vector3.Zero;
				GameWidget gameWidget = activeCamera.GameWidget;
				ComponentPlayer componentPlayer = ((gameWidget != null && gameWidget.PlayerData != null) ? gameWidget.PlayerData.ComponentPlayer : null);
				if (componentPlayer != null)
				{
					ComponentMiner componentMiner = componentPlayer.ComponentMiner;
					if (componentMiner != null)
					{
						int slotValue = componentMiner.Inventory.GetSlotValue(componentMiner.Inventory.ActiveSlotIndex);
						if (Terrain.ExtractContents(slotValue) == 379 && (Terrain.ExtractData(slotValue) & 1) == 1)
						{
							flag3 = true;
							vector = componentMiner.ComponentCreature.ComponentCreatureModel.EyePosition;
						}
					}
				}
				if (m_subsystemWeather.SubsystemTime.GameTime - m_lastPickablesCheckTime > 0.25)
				{
					m_lastPickablesCheckTime = m_subsystemWeather.SubsystemTime.GameTime;
					m_cachedFlag3 = false;
					m_cachedVector3 = Vector3.Zero;
					if (m_subsystemPickables != null)
					{
						float num6 = float.PositiveInfinity;
						Vector2 v3 = new Vector2((float)Point.X + 0.5f, (float)Point.Y + 0.5f);
						foreach (Pickable pickable in m_subsystemPickables.Pickables)
						{
							if (Terrain.ExtractContents(pickable.Value) == 379 && (Terrain.ExtractData(pickable.Value) & 1) == 1)
							{
								float num7 = Vector2.DistanceSquared(new Vector2(pickable.Position.X, pickable.Position.Z), v3);
								if (num7 < num6)
								{
									num6 = num7;
									m_cachedFlag3 = true;
									m_cachedVector3 = pickable.Position;
								}
							}
						}
					}
				}
				bool cachedFlag = m_cachedFlag3;
				Vector3 cachedVector = m_cachedVector3;
				if (m_needsInitialize)
				{
					m_needsInitialize = false;
					Initialize();
				}
				float y = activeCamera.ViewPosition.Y;
				float num8;
				float num9;
				if (flag2)
				{
					num8 = y - 15f;
					num9 = y + 15f;
				}
				else
				{
					num8 = y - 15f;
					num9 = y + 15f;
				}
				float num10 = (num9 - num8) / 5f * (float)m_activeParticlesCount * intensity;
				int num11 = (int)num10 + ((m_random.Float(0f, 1f) < num10 - (float)(int)num10) ? 1 : 0);
				m_lastViewY = y;
				m_toCreate += (float)m_activeParticlesCount * intensity * dt;
				m_isEmpty = true;
				bool precisePrecipitationHits = SettingsManager.PrecisePrecipitationHits;
				float num12 = MathUtils.DegToRad(GetParticleAngle(num));
				float num13 = (float)Math.Sin(num12);
				float num14 = (float)Math.Cos(num12);
				float num15 = (flag2 ? 1f : (-1f));
				Vector3 vector2 = new Vector3(cell.X * num13, num15 * num14, cell.Y * num13);
				Vector3 vector3 = new Vector3(cell.X, num15 * 0.3f, cell.Y);
				Vector2 v4 = new Vector2(vector.X, vector.Z);
				float num16 = vector.Y + 1.5f;
				float num17 = 2f;
				Vector2 v5 = new Vector2(cachedVector.X, cachedVector.Z);
				float num18 = cachedVector.Y + 2.7f;
				float num19 = 2f;
				for (int i = 0; i < m_activeParticlesCount; i++)
				{
					Particle particle = m_particles[i];
					if (particle.IsActive)
					{
						if (flag3 && particle.Position.Y <= num16 && Vector2.DistanceSquared(new Vector2(particle.Position.X, particle.Position.Z), v4) < num17)
						{
							particle.IsActive = false;
							continue;
						}
						if (cachedFlag && particle.Position.Y <= num18 && Vector2.DistanceSquared(new Vector2(particle.Position.X, particle.Position.Z), v5) < num19)
						{
							particle.IsActive = false;
							continue;
						}
						if (m_precipitationType == PrecipitationType.Snow)
						{
							float num20 = num12 + particle.DeviationAngle;
							float num21 = (float)Math.Cos(num20);
							float num22 = (float)Math.Sin(num20);
							float y2 = num15 * particle.Speed * num21;
							float num23 = particle.Speed * num22;
							particle.Velocity = new Vector3(cell.X * num23, y2, cell.Y * num23);
						}
						else if (m_precipitationType == PrecipitationType.Rain)
						{
							if (flag)
							{
								float num24 = num12 + particle.DeviationAngle;
								float num25 = (float)Math.Cos(num24);
								float num26 = (float)Math.Sin(num24);
								float y3 = num15 * particle.Speed * num25;
								float num27 = particle.Speed * num26;
								particle.Velocity = new Vector3(cell.X * num27, y3, cell.Y * num27);
							}
							else
							{
								particle.Velocity = vector2 * particle.Speed;
							}
							particle.RotationAngle = (float)Math.Atan2(particle.Velocity.X, flag2 ? ((double)particle.Velocity.Y) : (0.0 - (double)particle.Velocity.Y));
						}
						else if (m_precipitationType == PrecipitationType.Sand)
						{
							if (flag)
							{
								float num28 = num12 + particle.DeviationAngle;
								float num29 = (float)Math.Cos(num28);
								float num30 = (float)Math.Sin(num28);
								float y4 = num15 * particle.Speed * num29 * 0.3f;
								float num31 = particle.Speed * num30;
								particle.Velocity = new Vector3(cell.X * num31, y4, cell.Y * num31);
								particle.RotationAngle += MathUtils.DegToRad(m_random.Float(100f, 360f) * num * dt);
							}
							else
							{
								particle.Velocity = new Vector3(vector3.X * particle.Speed, vector3.Y * particle.Speed, vector3.Z * particle.Speed);
								particle.RotationAngle += MathUtils.DegToRad(m_random.Float(100f, 360f) * num * dt);
							}
						}
						particle.PreviousPosition = particle.Position;
						particle.Position += particle.Velocity * dt;
						if (Vector2.Dot(new Vector2(particle.Position.X - activeCamera.ViewPosition.X, particle.Position.Z - activeCamera.ViewPosition.Z), cell) > 10f)
						{
							particle.IsActive = false;
						}
						else if (precisePrecipitationHits)
						{
							if (!RaycastParticle(particle))
							{
								if (particle.Position.Y >= num8 && particle.Position.Y <= num9)
								{
									m_isEmpty = false;
								}
								else
								{
									particle.IsActive = false;
								}
							}
							else
							{
								if (flag2 || !particle.GenerateSplash || !(Vector3.DistanceSquared(particle.Position, activeCamera.ViewPosition) <= 1225f))
								{
									continue;
								}
								if (!flag)
								{
									Vector3 position = particle.Position;
									if (Terrain.ExtractContents(m_subsystemWeather.SubsystemTerrain.Terrain.GetCellValue(Terrain.ToCell(position.X), Terrain.ToCell(position.Y), Terrain.ToCell(position.Z))) == 92)
									{
										m_subsystemWeather.Project.FindSubsystem<SubsystemParticles>(throwOnError: true).AddParticleSystem(new MagmaSplashParticleSystem(m_subsystemWeather.SubsystemTerrain, position, large: false));
									}
									else if (m_precipitationType == PrecipitationType.Rain && m_random.Bool(0.5f))
									{
										m_subsystemWeather.RainSplashParticleSystem.AddSplash(m_topmostValue, position, m_subsystemWeather.RainColor, particle.HitFace);
									}
									else if (m_precipitationType == PrecipitationType.Snow)
									{
										m_subsystemWeather.SnowSplashParticleSystem.AddSplash(m_topmostValue, position, m_size, m_subsystemWeather.SnowColor, particle.TextureSlot, particle.HitFace);
									}
								}
								else
								{
									particle.GenerateSplash = false;
								}
							}
						}
						else
						{
							int x = Terrain.ToCell(particle.Position.X);
							int y5 = Terrain.ToCell(particle.Position.Y);
							int z = Terrain.ToCell(particle.Position.Z);
							int num32 = Terrain.ExtractContents(m_subsystemWeather.SubsystemTerrain.Terrain.GetCellValue(x, y5, z));
							Block block = BlocksManager.Blocks[num32];
							if (num32 != 0 && !(block is LeavesBlock))
							{
								particle.IsActive = false;
							}
							else if (!flag2 && particle.Position.Y < (float)m_yLimit)
							{
								particle.IsActive = false;
							}
							else if (particle.Position.Y >= num8 && particle.Position.Y <= num9)
							{
								m_isEmpty = false;
							}
						}
					}
					else if (num11 > 0)
					{
						do
						{
							particle.Position.X = (float)Point.X + m_random.Float(0f, 1f);
							particle.Position.Y = m_random.Float(num8, num9);
							particle.Position.Z = (float)Point.Y + m_random.Float(0f, 1f);
						}
						while ((flag3 && particle.Position.Y <= num16 && Vector2.DistanceSquared(new Vector2(particle.Position.X, particle.Position.Z), new Vector2(v4.X, v4.Y)) < num17) || (cachedFlag && particle.Position.Y <= num18 && Vector2.DistanceSquared(new Vector2(particle.Position.X, particle.Position.Z), new Vector2(v5.X, v5.Y)) < num19));
						particle.PreviousPosition = particle.Position;
						particle.IsActive = flag2 || particle.Position.Y >= (float)m_yLimit;
						particle.YLimit = 0f;
						particle.GenerateSplash = false;
						particle.Speed = m_random.Float(minSpeed, maxSpeed) * num3;
						num11--;
						num11--;
					}
					else
					{
						if (!(m_toCreate >= 1f))
						{
							continue;
						}
						do
						{
							particle.Position.X = (float)Point.X + m_random.Float(0f, 1f);
							if (flag2)
							{
								particle.Position.Y = m_random.Float(num8, MathUtils.Min(num9, y + 1f));
							}
							else
							{
								particle.Position.Y = m_random.Float(num8, num9);
							}
							particle.Position.Z = (float)Point.Y + m_random.Float(0f, 1f);
						}
						while ((flag3 && particle.Position.Y <= num16 && Vector2.DistanceSquared(new Vector2(particle.Position.X, particle.Position.Z), new Vector2(v4.X, v4.Y)) < num17) || (cachedFlag && particle.Position.Y <= num18 && Vector2.DistanceSquared(new Vector2(particle.Position.X, particle.Position.Z), new Vector2(v5.X, v5.Y)) < num19));
						particle.PreviousPosition = particle.Position;
						particle.IsActive = flag2 || particle.Position.Y >= (float)m_yLimit;
						particle.YLimit = 0f;
						particle.GenerateSplash = false;
						particle.Speed = m_random.Float(minSpeed, maxSpeed) * num3;
						m_toCreate--;
					}
				}
				m_toCreate -= MathUtils.Floor(m_toCreate);
			}
			else
			{
				m_needsInitialize = true;
			}
		}
		return false;
	}

	public override void Draw(Camera camera)
	{
		if (!m_isVisible || m_isEmpty || camera.GameWidget != m_gameWidget)
		{
			return;
		}
		float num = 1f - SettingsManager.StagePrecipitation;
		if (num <= 0f)
		{
			return;
		}
		if (m_batch == null)
		{
			m_batch = SubsystemParticles.PrimitivesRenderer.TexturedBatch(m_texture, useAlphaTest: false, 0, DepthStencilState.DepthRead, null, BlendState.AlphaBlend, SamplerState.PointClamp);
		}
		float num2 = camera.ViewPosition.Y + 10f;
		float num3 = MathUtils.Clamp(m_subsystemWeather.SubsystemSky.SkyLightIntensity, 0.2f, 1f);
		Vector3 viewDirection = camera.ViewDirection;
		Vector3 vector = Vector3.Normalize(Vector3.Cross(viewDirection, Vector3.UnitY));
		Vector3 vector2 = ((m_precipitationType == PrecipitationType.Rain) ? Vector3.UnitY : Vector3.Normalize(Vector3.Cross(viewDirection, vector)));
		Vector3 vector3 = vector * m_size.X;
		Vector3 vector4 = vector2 * m_size.Y;
		if (m_precipitationType == PrecipitationType.Rain)
		{
			bool skyLightUseParticles = SettingsManager.SkyLightUseParticles;
			for (int i = 0; i < m_activeParticlesCount; i++)
			{
				Particle particle = m_particles[i];
				if (particle.IsActive)
				{
					Vector3 v = new Vector3(0f, -1f, 0f);
					Vector3 v2 = Vector3.Normalize(particle.Velocity);
					Vector3 v3 = Vector3.Cross(v, v2);
					float angle = (float)Math.Acos(Vector3.Dot(v, v2));
					if (v3.LengthSquared() < 1E-06f)
					{
						v3 = Vector3.UnitX;
						angle = ((!(Vector3.Dot(v, v2) < 0f)) ? 0f : ((float)Math.PI));
					}
					else
					{
						v3 = Vector3.Normalize(v3);
					}
					Matrix m = Matrix.CreateFromAxisAngle(v3, angle);
					Vector3 vector5 = Vector3.Transform(-vector3 - vector4, m);
					Vector3 vector6 = Vector3.Transform(vector3 - vector4, m);
					Vector3 vector7 = Vector3.Transform(vector4, m);
					Vector3 p = particle.Position + vector5;
					Vector3 p2 = particle.Position + vector6;
					Vector3 p3 = particle.Position + vector7;
					float num4;
					if (skyLightUseParticles)
					{
						int cellLight = m_subsystemWeather.SubsystemTerrain.Terrain.GetCellLight(Terrain.ToCell(particle.Position.X), Terrain.ToCell(particle.Position.Y), Terrain.ToCell(particle.Position.Z));
						num4 = LightingManager.LightIntensityByLightValue[cellLight];
					}
					else
					{
						num4 = num3;
					}
					Color color = m_subsystemWeather.RainColor * num4 * MathUtils.Min(0.6f * (num2 - particle.Position.Y), 1f) * num;
					m_batch.QueueTriangle(p, p2, p3, particle.TexCoord1, particle.TexCoord2, particle.TexCoord3, color);
				}
			}
		}
		else if (m_precipitationType == PrecipitationType.Snow)
		{
			bool skyLightUseParticles2 = SettingsManager.SkyLightUseParticles;
			for (int j = 0; j < m_activeParticlesCount; j++)
			{
				Particle particle2 = m_particles[j];
				if (particle2.IsActive)
				{
					Vector3 v4 = new Vector3(0f, -1f, 0f);
					Vector3 v5 = Vector3.Normalize(particle2.Velocity);
					Vector3 v6 = Vector3.Cross(v4, v5);
					float angle2 = (float)Math.Acos(Vector3.Dot(v4, v5));
					if (v6.LengthSquared() < 1E-06f)
					{
						v6 = Vector3.UnitX;
						angle2 = ((!(Vector3.Dot(v4, v5) < 0f)) ? 0f : ((float)Math.PI));
					}
					else
					{
						v6 = Vector3.Normalize(v6);
					}
					Matrix m2 = Matrix.CreateFromAxisAngle(v6, angle2);
					Vector3 vector8 = Vector3.Transform(-vector3 - vector4, m2);
					Vector3 vector9 = Vector3.Transform(vector3 - vector4, m2);
					Vector3 vector10 = Vector3.Transform(vector3 + vector4, m2);
					Vector3 vector11 = Vector3.Transform(-vector3 + vector4, m2);
					Vector3 p4 = particle2.Position + vector8;
					Vector3 p5 = particle2.Position + vector9;
					Vector3 p6 = particle2.Position + vector10;
					Vector3 p7 = particle2.Position + vector11;
					float num5;
					if (skyLightUseParticles2)
					{
						int cellLight2 = m_subsystemWeather.SubsystemTerrain.Terrain.GetCellLight(Terrain.ToCell(particle2.Position.X), Terrain.ToCell(particle2.Position.Y), Terrain.ToCell(particle2.Position.Z));
						num5 = LightingManager.LightIntensityByLightValue[cellLight2];
					}
					else
					{
						num5 = num3;
					}
					Color color2 = m_subsystemWeather.SnowColor * num5 * MathUtils.Min(0.6f * (num2 - particle2.Position.Y), 1f) * num;
					m_batch.QueueQuad(p4, p5, p6, p7, particle2.TexCoord1, particle2.TexCoord2, particle2.TexCoord3, particle2.TexCoord4, color2);
				}
			}
		}
		else
		{
			if (m_precipitationType != PrecipitationType.Sand)
			{
				return;
			}
			bool skyLightUseParticles3 = SettingsManager.SkyLightUseParticles;
			for (int k = 0; k < m_activeParticlesCount; k++)
			{
				Particle particle3 = m_particles[k];
				if (particle3.IsActive)
				{
					Vector3 v7 = new Vector3(0f, -1f, 0f);
					Vector3 v8 = Vector3.Normalize(particle3.Velocity);
					Vector3 v9 = Vector3.Cross(v7, v8);
					float angle3 = (float)Math.Acos(Vector3.Dot(v7, v8));
					if (v9.LengthSquared() < 1E-06f)
					{
						v9 = Vector3.UnitX;
						angle3 = ((!(Vector3.Dot(v7, v8) < 0f)) ? 0f : ((float)Math.PI));
					}
					else
					{
						v9 = Vector3.Normalize(v9);
					}
					Matrix m3 = Matrix.CreateFromAxisAngle(v9, angle3);
					Vector3 vector12 = Vector3.Transform(-vector3 - vector4, m3);
					Vector3 vector13 = Vector3.Transform(vector3 - vector4, m3);
					Vector3 vector14 = Vector3.Transform(vector3 + vector4, m3);
					Vector3 vector15 = Vector3.Transform(-vector3 + vector4, m3);
					Vector3 p8 = particle3.Position + vector12;
					Vector3 p9 = particle3.Position + vector13;
					Vector3 p10 = particle3.Position + vector14;
					Vector3 p11 = particle3.Position + vector15;
					float num6;
					if (skyLightUseParticles3)
					{
						int cellLight3 = m_subsystemWeather.SubsystemTerrain.Terrain.GetCellLight(Terrain.ToCell(particle3.Position.X), Terrain.ToCell(particle3.Position.Y), Terrain.ToCell(particle3.Position.Z));
						num6 = LightingManager.LightIntensityByLightValue[cellLight3];
					}
					else
					{
						num6 = num3;
					}
					Color color3 = m_subsystemWeather.SandColor * num6 * MathUtils.Min(0.6f * (num2 - particle3.Position.Y), 1f) * num;
					m_batch.QueueQuad(p8, p9, p10, p11, particle3.TexCoord1, particle3.TexCoord2, particle3.TexCoord3, particle3.TexCoord4, color3);
				}
			}
		}
	}

	public void Initialize()
	{
		m_lastViewY = null;
		m_toCreate = m_random.Float(0f, 0.9f);
		m_batch = null;
		m_lastSkylightIntensity = float.MinValue;
		m_wasStormCondition = IsStormCondition();
		bool flag = IsStormCondition();
		if (m_precipitationType == PrecipitationType.Rain)
		{
			minSpeed = 8f;
			maxSpeed = 12f;
			m_averageSpeed = (minSpeed + maxSpeed) / 2f;
			m_size = (flag ? new Vector2(0.85f, 0.85f) : new Vector2(0.02f, 0.15f));
			ref Texture2D texture = ref m_texture;
			Texture2D texture2D;
			if (flag)
			{
				texture2D = ContentManager.Get<Texture2D>("Textures/Weather/StormRainParticle");
			}
			else
			{
				texture2D = ((m_activeParticlesCount <= 15) ? ContentManager.Get<Texture2D>("Textures/Weather/DrizzleRainParticle") : ContentManager.Get<Texture2D>("Textures/Weather/RainParticle"));
			}
			texture = texture2D;
		}
		else if (m_precipitationType == PrecipitationType.Sand)
		{
			minSpeed = 6f;
			maxSpeed = 10f;
			m_averageSpeed = (minSpeed + maxSpeed) / 2f;
			m_size = (flag ? new Vector2(1f, 1f) : new Vector2(0.1f, 0.1f));
			m_texture = (flag ? ContentManager.Get<Texture2D>("Textures/Weather/StormSandParticle") : ContentManager.Get<Texture2D>("Textures/Weather/SandParticle"));
		}
		else
		{
			if (m_precipitationType != PrecipitationType.Snow)
			{
				throw new InvalidOperationException("Unknown precipitation type.");
			}
			minSpeed = 0.5f;
			maxSpeed = 3f;
			m_averageSpeed = (minSpeed + maxSpeed) / 2f;
			m_size = (flag ? new Vector2(0.65f, 0.65f) : new Vector2(0.03f, 0.03f));
			m_texture = (flag ? ContentManager.Get<Texture2D>("Textures/Weather/StormSnowParticle") : ContentManager.Get<Texture2D>("Textures/Weather/SnowParticle"));
		}
		for (int i = 0; i < m_activeParticlesCount; i++)
		{
			InitializeParticle(m_particles[i]);
		}
	}

	public void UpdateParticleCount()
	{
		float lightningIntensity = m_subsystemWeather.m_lightningIntensity;
		int num = (int)MathUtils.Lerp(4f, 40f, lightningIntensity);
		if (m_activeParticlesCount == num)
		{
			return;
		}
		if (m_activeParticlesCount < num)
		{
			for (int i = m_activeParticlesCount; i < num; i++)
			{
				if (i >= m_particles.Count)
				{
					Particle item = new Particle();
					m_particles.Add(item);
				}
				InitializeParticle(m_particles[i]);
			}
		}
		else
		{
			for (int j = num; j < m_activeParticlesCount; j++)
			{
				m_particles[j].IsActive = false;
			}
		}
		m_activeParticlesCount = num;
	}

	private float GetParticleAngle(float windStrength)
	{
		float x = 70f;
		float num = 16f;
		return MathUtils.Lerp(0f, x, windStrength / num);
	}

	static PrecipitationShaftParticleSystem()
	{
		WindDirectionVectors = new Dictionary<string, Vector2>
		{
			{
				"N",
				new Vector2(0f, -1f)
			},
			{
				"NE",
				new Vector2(0.7071f, -0.7071f)
			},
			{
				"E",
				new Vector2(1f, 0f)
			},
			{
				"SE",
				new Vector2(0.7071f, 0.7071f)
			},
			{
				"S",
				new Vector2(0f, 1f)
			},
			{
				"SW",
				new Vector2(-0.7071f, 0.7071f)
			},
			{
				"W",
				new Vector2(-1f, 0f)
			},
			{
				"NW",
				new Vector2(-0.7071f, -0.7071f)
			}
		};
	}

	public bool RaycastParticle(Particle particle)
	{
		bool blockEntity = false;
		particle.GenerateSplash = false;
		Vector3 position = particle.PreviousPosition;
		Vector3 position2 = particle.Position;
		TerrainRaycastResult? terrainRaycastResult = m_subsystemWeather.SubsystemTerrain.Raycast(position, position2, useInteractionBoxes: false, skipAirBlocks: false, RaycastFilter);
		if (terrainRaycastResult.HasValue)
		{
			blockEntity = true;
			particle.Position = terrainRaycastResult.Value.HitPoint();
			particle.YLimit = particle.Position.Y;
			particle.IsActive = false;
			particle.HitFace = terrainRaycastResult.Value.CellFace.Face;
			int num = Terrain.ExtractContents(terrainRaycastResult.Value.Value);
			Block block = BlocksManager.Blocks[num];
			particle.GenerateSplash = !(block is AirBlock) && !(block is LeavesBlock);
		}
		return blockEntity;
	}

	private bool IsStormCondition()
	{
		float lightningIntensity = m_subsystemWeather.m_lightningIntensity;
		float blockEntity = m_subsystemWeather.CurrentWindStrength;
		if (lightningIntensity >= 0.8f)
		{
			return blockEntity >= 12f;
		}
		return false;
	}

	private void InitializeParticle(Particle particle)
	{
		particle.IsActive = false;
		particle.TextureSlot = (byte)m_random.Int(0, 15);
		if (m_precipitationType == PrecipitationType.Rain)
		{
			if (IsStormCondition())
			{
				particle.TexCoord1 = new Vector2(0f, 1f);
				particle.TexCoord2 = new Vector2(1f, 1f);
				particle.TexCoord3 = new Vector2(0.5f, 0f);
				particle.DeviationAngle = MathUtils.DegToRad(m_random.Float(-5f, 5f));
			}
			else
			{
				particle.DeviationAngle = 0f;
				particle.TexCoord1 = new Vector2(0f, 1f);
				particle.TexCoord2 = new Vector2(1f, 1f);
				particle.TexCoord3 = new Vector2(0.5f, 0f);
			}
		}
		else if (m_precipitationType != PrecipitationType.Sand)
		{
			if (m_precipitationType == PrecipitationType.Snow)
			{
				if (IsStormCondition())
				{
					particle.TexCoord1 = new Vector2(0f, 0f);
					particle.TexCoord2 = new Vector2(1f, 0f);
					particle.TexCoord3 = new Vector2(1f, 1f);
					particle.TexCoord4 = new Vector2(0f, 1f);
					particle.DeviationAngle = MathUtils.DegToRad(m_random.Float(-10f, 10f));
				}
				else
				{
					float num = 0.25f;
					int num2 = particle.TextureSlot % 4;
					int num3 = particle.TextureSlot / 4;
					Vector2 vector = (particle.TexCoord1 = new Vector2((float)num2 * num, (float)num3 * num));
					particle.TexCoord2 = vector + new Vector2(num, 0f);
					particle.TexCoord3 = vector + new Vector2(num, num);
					particle.TexCoord4 = vector + new Vector2(0f, num);
					particle.DeviationAngle = MathUtils.DegToRad(m_random.Float(-10f, 10f));
				}
			}
		}
		else if (IsStormCondition())
		{
			particle.TexCoord1 = new Vector2(0f, 0f);
			particle.TexCoord2 = new Vector2(1f, 0f);
			particle.TexCoord3 = new Vector2(1f, 1f);
			particle.TexCoord4 = new Vector2(0f, 1f);
			particle.DeviationAngle = MathUtils.DegToRad(m_random.Float(-15f, 15f));
		}
		else
		{
			float blockEntity = 0.25f;
			int pickable = particle.TextureSlot % 4;
			int num4 = particle.TextureSlot / 4;
			Vector2 num5 = (particle.TexCoord1 = new Vector2((float)pickable * blockEntity, (float)num4 * blockEntity));
			particle.TexCoord2 = num5 + new Vector2(blockEntity, 0f);
			particle.TexCoord3 = num5 + new Vector2(blockEntity, blockEntity);
			particle.TexCoord4 = num5 + new Vector2(0f, blockEntity);
			particle.DeviationAngle = 0f;
		}
	}

	private bool RaycastFilter(int value, float distance)
	{
		int movingBlockSet = Terrain.ExtractContents(value);
		Block movingEntityData = BlocksManager.Blocks[movingBlockSet];
		if (!(movingEntityData is AirBlock))
		{
			if (movingEntityData is LeavesBlock)
			{
				return m_random.Float(0f, 1f) >= 0.9f;
			}
			return true;
		}
		return false;
	}
}
