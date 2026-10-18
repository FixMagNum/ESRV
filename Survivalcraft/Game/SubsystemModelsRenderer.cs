using System;
using System.Collections.Generic;
using System.Linq;
using Engine;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemModelsRenderer : Subsystem, IDrawable
{
	public class ModelData : IComparable<ModelData>
	{
		public ComponentModel ComponentModel;

		public ComponentBody ComponentBody;

		public float Light;

		public double NextLightTime;

		public int LastAnimateFrame;

		public int CompareTo(ModelData other)
		{
			int num = ((ComponentModel != null) ? ComponentModel.PrepareOrder : 0);
			int num2 = ((other.ComponentModel != null) ? other.ComponentModel.PrepareOrder : 0);
			return num - num2;
		}
	}

	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemSky m_subsystemSky;

	public SubsystemShadows m_subsystemShadows;

	public SubsystemTimeOfDay m_subsystemTimeOfDay;

	public PrimitivesRenderer3D m_primitivesRenderer = new PrimitivesRenderer3D();

	public static ModelShader ShaderOpaque;

	public static ModelShader ShaderAlphaTested;

	private ModelShader m_shaderOpaque;

	private ModelShader m_shaderAlphaTested;

	private Vector3 m_sunLightDirection;

	public int MaxInstancesCount;

	public Dictionary<ComponentModel, ModelData> m_componentModels = new Dictionary<ComponentModel, ModelData>();

	public List<ModelData> m_modelsToPrepare = new List<ModelData>();

	public List<ModelData>[] m_modelsToDraw = new List<ModelData>[4]
	{
		new List<ModelData>(),
		new List<ModelData>(),
		new List<ModelData>(),
		new List<ModelData>()
	};

	public static bool DisableDrawingModels;

	public int ModelsDrawn;

	public int[] m_drawOrders = new int[4] { -10000, 1, 99, 201 };

	public PrimitivesRenderer3D PrimitivesRenderer => m_primitivesRenderer;

	public int[] DrawOrders => m_drawOrders;

	public void Draw(Camera camera, int drawOrder)
	{
		if (drawOrder == m_drawOrders[0])
		{
			ModelsDrawn = 0;
			List<ModelData>[] modelsToDraw = m_modelsToDraw;
			for (int i = 0; i < modelsToDraw.Length; i++)
			{
				modelsToDraw[i].Clear();
			}
			m_modelsToPrepare.Clear();
			foreach (ModelData value in m_componentModels.Values)
			{
				if (value.ComponentModel.Model != null)
				{
					value.ComponentModel.CalculateIsVisible(camera);
					if (value.ComponentModel.IsVisibleForCamera)
					{
						m_modelsToPrepare.Add(value);
					}
				}
			}
			m_modelsToPrepare.Sort();
			foreach (ModelData item in m_modelsToPrepare)
			{
				PrepareModel(item, camera);
				m_modelsToDraw[(int)item.ComponentModel.RenderingMode].Add(item);
			}
		}
		if (!DisableDrawingModels)
		{
			m_sunLightDirection = 1.25f * Vector3.TransformNormal(SunVector(m_subsystemSky), camera.ViewMatrix);
			if (drawOrder == m_drawOrders[1])
			{
				Display.DepthStencilState = DepthStencilState.Default;
				Display.RasterizerState = RasterizerState.CullCounterClockwiseScissor;
				Display.BlendState = BlendState.Opaque;
				DrawModels(camera, m_modelsToDraw[0], null);
				Display.RasterizerState = RasterizerState.CullNoneScissor;
				DrawModels(camera, m_modelsToDraw[1], 0f);
				Display.RasterizerState = RasterizerState.CullCounterClockwiseScissor;
				m_primitivesRenderer.Flush(camera.ProjectionMatrix, clearAfterFlush: true, 0);
			}
			else if (drawOrder == m_drawOrders[2])
			{
				Display.DepthStencilState = DepthStencilState.Default;
				Display.RasterizerState = RasterizerState.CullNoneScissor;
				Display.BlendState = BlendState.AlphaBlend;
				DrawModels(camera, m_modelsToDraw[2], null);
			}
			else if (drawOrder == m_drawOrders[3])
			{
				Display.DepthStencilState = DepthStencilState.Default;
				Display.RasterizerState = RasterizerState.CullNoneScissor;
				Display.BlendState = BlendState.AlphaBlend;
				DrawModels(camera, m_modelsToDraw[3], null);
				if (ShaderOpaque != null)
				{
					m_primitivesRenderer.Flush(camera.ProjectionMatrix);
				}
				else
				{
					m_primitivesRenderer.Flush(camera.ProjectionMatrix);
				}
			}
		}
		else
		{
			m_primitivesRenderer.Clear();
		}
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		m_subsystemTimeOfDay = Project.FindSubsystem<SubsystemTimeOfDay>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemSky = Project.FindSubsystem<SubsystemSky>(throwOnError: true);
		m_subsystemShadows = Project.FindSubsystem<SubsystemShadows>(throwOnError: true);
		ModsManager.HookAction("GetMaxInstancesCount", (ModLoader modLoader) =>
		{
			MaxInstancesCount = Math.Max(modLoader.GetMaxInstancesCount(), MaxInstancesCount);
			return false;
		});
		m_shaderOpaque = new ModelShader(ShaderCodeManager.GetFast("Shaders/Model.vsh"), ShaderCodeManager.GetFast("Shaders/Model.psh"), useAlphaThreshold: false, MaxInstancesCount);
		m_shaderAlphaTested = new ModelShader(ShaderCodeManager.GetFast("Shaders/Model.vsh"), ShaderCodeManager.GetFast("Shaders/Model.psh"), useAlphaThreshold: true, MaxInstancesCount);
	}

	public override void OnEntityAdded(Entity entity)
	{
		foreach (ComponentModel item in entity.FindComponents<ComponentModel>())
		{
			ModelData value = new ModelData
			{
				ComponentModel = item,
				ComponentBody = item.Entity.FindComponent<ComponentBody>(),
				Light = m_subsystemSky.SkyLightIntensity
			};
			m_componentModels.Add(item, value);
		}
	}

	public override void OnEntityRemoved(Entity entity)
	{
		foreach (ComponentModel item in entity.FindComponents<ComponentModel>())
		{
			m_componentModels.Remove(item);
		}
	}

	public void PrepareModel(ModelData modelData, Camera camera)
	{
		if (Time.FrameIndex > modelData.LastAnimateFrame)
		{
			modelData.ComponentModel.Animate();
			modelData.LastAnimateFrame = Time.FrameIndex;
		}
		if (Time.FrameStartTime >= modelData.NextLightTime)
		{
			float? num = CalculateModelLight(modelData);
			if (num.HasValue)
			{
				modelData.Light = num.Value;
			}
			modelData.NextLightTime = Time.FrameStartTime + 0.1;
		}
		modelData.ComponentModel.CalculateAbsoluteBonesTransforms(camera);
	}

	public void DrawModels(Camera camera, List<ModelData> modelsData, float? alphaThreshold)
	{
		DrawInstancedModels(camera, modelsData, alphaThreshold);
		DrawModelsExtras(camera, modelsData);
	}

	public void DrawInstancedModels(Camera camera, List<ModelData> modelsData, float? alphaThreshold)
	{
		ModelShader modelShader = null;
		if (ShaderOpaque != null && ShaderAlphaTested != null)
		{
			modelShader = (alphaThreshold.HasValue ? ShaderAlphaTested : ShaderOpaque);
		}
		else
		{
			modelShader = (alphaThreshold.HasValue ? m_shaderAlphaTested : m_shaderOpaque);
		}
		modelShader.LightDirection1 = -Vector3.TransformNormal(LightingManager.DirectionToLight1, camera.ViewMatrix);
		modelShader.LightDirection2 = -Vector3.TransformNormal(LightingManager.DirectionToLight2, camera.ViewMatrix);
		modelShader.FogColor = new Vector3(m_subsystemSky.ViewFogColor);
		modelShader.FogStartInvLength = new Vector2(m_subsystemSky.ViewFogRange.X, 1f / (m_subsystemSky.ViewFogRange.Y - m_subsystemSky.ViewFogRange.X));
		float visibilityRangeYMultiplier = m_subsystemSky.VisibilityRangeYMultiplier;
		modelShader.FogYMultiplier = visibilityRangeYMultiplier * visibilityRangeYMultiplier - 1f;
		modelShader.WorldUp = Vector3.TransformNormal(Vector3.UnitY, camera.ViewMatrix);
		modelShader.Transforms.View = Matrix.Identity;
		modelShader.Transforms.Projection = camera.ProjectionMatrix;
		modelShader.SamplerState = SamplerState.PointClamp;
		List<SubsystemMiningLightBehavior.MiningLightData> list = new List<SubsystemMiningLightBehavior.MiningLightData>();
		SubsystemMiningLightBehavior subsystemMiningLightBehavior = Project.FindSubsystem<SubsystemMiningLightBehavior>(throwOnError: false);
		if (subsystemMiningLightBehavior != null)
		{
			list.AddRange(subsystemMiningLightBehavior.ActiveLights.Values);
			list.AddRange(subsystemMiningLightBehavior.ActiveHeadLights.Values);
		}
		List<SubsystemMiningLightBehavior.MiningLightData> list2 = list.OrderBy((SubsystemMiningLightBehavior.MiningLightData l) => Vector3.DistanceSquared(camera.ViewPosition, l.Position)).Take(8).ToList();
		for (int num = 0; num < 8; num++)
		{
			string text = (num + 1).ToString();
			if (num < list2.Count)
			{
				SubsystemMiningLightBehavior.MiningLightData miningLightData = list2[num];
				Vector3 value = Vector3.Transform(miningLightData.Position, camera.ViewMatrix);
				modelShader.GetParameter("u_lightPosition" + text)?.SetValue(value);
				modelShader.GetParameter("u_lightColor" + text)?.SetValue(new Vector3(0.65f, 0.65f, 0.65f) * 0.55f);
				modelShader.GetParameter("u_lightRange" + text)?.SetValue(miningLightData.Intensity * 1.5f);
			}
			else
			{
				modelShader.GetParameter("u_lightPosition" + text)?.SetValue(Vector3.Zero);
				modelShader.GetParameter("u_lightColor" + text)?.SetValue(Vector3.Zero);
				modelShader.GetParameter("u_lightRange" + text)?.SetValue(0f);
			}
		}
		if (alphaThreshold.HasValue)
		{
			modelShader.AlphaThreshold = alphaThreshold.Value;
		}
		ModsManager.HookAction("ModelShaderParameter", (ModLoader modLoader) =>
		{
			modLoader.ModelShaderParameter(modelShader, camera, modelsData, alphaThreshold);
			return true;
		});
		ModsManager.HookAction("SetShaderParameter", (ModLoader modLoader) =>
		{
			modLoader.SetShaderParameter(modelShader, camera);
			return true;
		});
		foreach (ModelData modelsDatum in modelsData)
		{
			ComponentModel componentModel = modelsDatum.ComponentModel;
			Vector3 vector = (componentModel.DiffuseColor.HasValue ? componentModel.DiffuseColor.Value : Vector3.One);
			float num2 = (componentModel.Opacity.HasValue ? componentModel.Opacity.Value : 1f);
			modelShader.InstancesCount = componentModel.AbsoluteBoneTransformsForCamera.Length;
			modelShader.MaterialColor = new Vector4(vector * num2, num2);
			modelShader.EmissionColor = (componentModel.EmissionColor.HasValue ? componentModel.EmissionColor.Value : Vector4.Zero);
			modelShader.AmbientLightColor = new Vector3(LightingManager.LightAmbient * modelsDatum.Light);
			modelShader.DiffuseLightColor1 = new Vector3(modelsDatum.Light);
			modelShader.DiffuseLightColor2 = new Vector3(modelsDatum.Light);
			modelShader.Texture = componentModel.TextureOverride;
			Array.Copy(componentModel.AbsoluteBoneTransformsForCamera, modelShader.Transforms.World, componentModel.AbsoluteBoneTransformsForCamera.Length);
			InstancedModelData instancedModelData = InstancedModelsManager.GetInstancedModelData(componentModel.Model, componentModel.MeshDrawOrders);
			Display.DrawIndexed(PrimitiveType.TriangleList, modelShader, instancedModelData.VertexBuffer, instancedModelData.IndexBuffer, 0, instancedModelData.IndexBuffer.IndicesCount);
			ModelsDrawn++;
			ModsManager.HookAction("OnModelRendererDrawExtra", (ModLoader modLoader) =>
			{
				modLoader.OnModelRendererDrawExtra(this, componentModel, camera, alphaThreshold);
				return false;
			});
		}
	}

	public void DrawModelsExtras(Camera camera, List<ModelData> modelsData)
	{
		foreach (ModelData modelsDatum in modelsData)
		{
			if (modelsDatum.ComponentBody != null && modelsDatum.ComponentModel.CastsShadow)
			{
				Vector3 shadowPosition = modelsDatum.ComponentBody.Position + new Vector3(0f, 0.1f, 0f);
				BoundingBox boundingBox = modelsDatum.ComponentBody.BoundingBox;
				float shadowDiameter = 2.25f * (boundingBox.Max.X - boundingBox.Min.X);
				m_subsystemShadows.QueueShadow(camera, shadowPosition, shadowDiameter, modelsDatum.ComponentModel.Opacity ?? 1f);
			}
			modelsDatum.ComponentModel.DrawExtras(camera);
		}
	}

	public float? CalculateModelLight(ModelData modelData)
	{
		Vector3 p;
		if (modelData.ComponentBody != null)
		{
			p = modelData.ComponentBody.Position;
			p.Y += 0.95f * (modelData.ComponentBody.BoundingBox.Max.Y - modelData.ComponentBody.BoundingBox.Min.Y);
		}
		else
		{
			Matrix? boneTransform = modelData.ComponentModel.GetBoneTransform(modelData.ComponentModel.Model.RootBone.Index);
			p = ((!boneTransform.HasValue) ? Vector3.Zero : (boneTransform.Value.Translation + new Vector3(0f, 0.9f, 0f)));
		}
		return LightingManager.CalculateSmoothLight(m_subsystemTerrain, p);
	}

	public Vector3 SunVector(SubsystemSky subsystemSky)
	{
		float timeOfDay = subsystemSky.m_subsystemTimeOfDay.TimeOfDay;
		float num = 2f * timeOfDay * (float)Math.PI;
		float x = num + (float)Math.PI;
		float f = MathUtils.Max(SubsystemSky.CalculateDawnGlowIntensity(timeOfDay), SubsystemSky.CalculateDuskGlowIntensity(timeOfDay));
		float num2 = MathUtils.Lerp(90f, 160f, f);
		Vector3 vector = new Vector3
		{
			X = 0f - MathUtils.Sin(x),
			Y = 0f - MathUtils.Cos(x),
			Z = 0f
		};
		Vector3 unitZ = Vector3.UnitZ;
		Vector3 vector2 = Vector3.Cross(unitZ, vector);
		return Vector3.Normalize(vector * 900f - num2 * unitZ - num * vector2);
	}

	public void ShadowDraw(SubsystemShadows subsystemShadows, Camera camera, Vector3 shadowPosition, float shadowDiameter, float alpha)
	{
		if (!SettingsManager.ObjectsShadowsEnabled)
		{
			return;
		}
		float num = Vector3.DistanceSquared(camera.ViewPosition, shadowPosition);
		if (num > 1024f)
		{
			return;
		}
		float num2 = MathUtils.Sqrt(num);
		float num3 = MathUtils.Saturate(4f * (1f - num2 / 32f));
		float num4 = shadowDiameter / 2f;
		int num5 = Terrain.ToCell(shadowPosition.X - num4);
		int num6 = Terrain.ToCell(shadowPosition.Z - num4);
		int num7 = Terrain.ToCell(shadowPosition.X + num4);
		int num8 = Terrain.ToCell(shadowPosition.Z + num4);
		for (int i = num5; i <= num7; i++)
		{
			for (int j = num6; j <= num8; j++)
			{
				int num9 = MathUtils.Min(Terrain.ToCell(shadowPosition.Y), 255);
				int num10 = MathUtils.Max(num9 - 2, 0);
				for (int num11 = num9; num11 >= num10; num11--)
				{
					int cellValueFast = subsystemShadows.m_subsystemTerrain.Terrain.GetCellValueFast(i, num11, j);
					int num12 = Terrain.ExtractContents(cellValueFast);
					Block block = BlocksManager.Blocks[num12];
					if (block.ObjectShadowStrength > 0f)
					{
						BoundingBox[] customCollisionBoxes = block.GetCustomCollisionBoxes(subsystemShadows.m_subsystemTerrain, cellValueFast);
						for (int k = 0; k < customCollisionBoxes.Length; k++)
						{
							BoundingBox boundingBox = customCollisionBoxes[k];
							float num13 = boundingBox.Max.Y + (float)num11;
							if (shadowPosition.Y - num13 > -0.5f)
							{
								float num14 = camera.ViewPosition.Y - num13;
								if (num14 > 0f)
								{
									float num15 = MathUtils.Max(num14 * 0.01f, 0.005f);
									float num16 = MathUtils.Saturate(1f - (shadowPosition.Y - num13) / 2f);
									Vector3 p = new Vector3(boundingBox.Min.X + (float)i, num13 + num15, boundingBox.Min.Z + (float)j);
									Vector3 p2 = new Vector3(boundingBox.Max.X + (float)i, num13 + num15, boundingBox.Min.Z + (float)j);
									Vector3 p3 = new Vector3(boundingBox.Max.X + (float)i, num13 + num15, boundingBox.Max.Z + (float)j);
									Vector3 p4 = new Vector3(boundingBox.Min.X + (float)i, num13 + num15, boundingBox.Max.Z + (float)j);
									subsystemShadows.DrawShadowOverQuad(p, p2, p3, p4, shadowPosition, shadowDiameter, 0.45f * block.ObjectShadowStrength * alpha * num3 * num16);
								}
							}
						}
						break;
					}
					if (num12 == 18)
					{
						break;
					}
				}
			}
		}
	}
}
