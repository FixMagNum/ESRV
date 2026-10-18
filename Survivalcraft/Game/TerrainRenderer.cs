using System;
using System.Collections.Generic;
using System.Linq;
using Engine;
using Engine.Graphics;

namespace Game;

public class TerrainRenderer : IDisposable
{
	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemSky m_subsystemSky;

	public SubsystemAnimatedTextures m_subsystemAnimatedTextures;

	public static Shader OpaqueShader;

	public static Shader AlphatestedShader;

	public static Shader TransparentShader;

	public SamplerState m_samplerState;

	public SamplerState m_samplerStateMips;

	public DynamicArray<TerrainChunk> m_chunksToDraw;

	public static DynamicArray<int> m_tmpIndices;

	public static bool DrawChunksMap;

	public static int ChunksDrawn;

	public static int ChunkDrawCalls;

	public static int ChunkTrianglesDrawn;

	private SubsystemTime m_subsystemTime;

	private SubsystemWeather m_subsystemWeather;

	public SubsystemAudio m_subsystemAudio;

	private SubsystemNaturalTreeScanner.TreeInfo _cachedTree;

	private SubsystemTimeOfDay m_timeOfDay;

	public static Shader AlphatestedOldShader;

	public static Shader TransparentOldShader;

	private SubsystemGameInfo m_subsystemGameInfo;

	public static Shader DistantShader;

	public static Shader DistantSimpleShader;

	private SubsystemMiningLightBehavior m_subsystemMiningLight;

	public string ChunksGpuMemoryUsage
	{
		get
		{
			long text = 0L;
			TerrainChunk[] allocatedChunks = m_subsystemTerrain.Terrain.AllocatedChunks;
			foreach (TerrainChunk terrainChunk in allocatedChunks)
			{
				if (terrainChunk.Geometry == null)
				{
					continue;
				}
				foreach (TerrainChunkGeometry.Buffer buffer in terrainChunk.Buffers)
				{
					text += buffer.VertexBuffer?.GetGpuMemoryUsage() ?? 0;
					text += buffer.IndexBuffer?.GetGpuMemoryUsage() ?? 0;
				}
			}
			string result = $"{text / 1024 / 1024:0.0}MB";
			return result;
		}
	}

	public TerrainRenderer(SubsystemTerrain subsystemTerrain)
	{
		m_samplerState = new SamplerState
		{
			AddressModeU = TextureAddressMode.Clamp,
			AddressModeV = TextureAddressMode.Clamp,
			FilterMode = TextureFilterMode.Point,
			MaxLod = 0f
		};
		m_samplerStateMips = new SamplerState
		{
			AddressModeU = TextureAddressMode.Clamp,
			AddressModeV = TextureAddressMode.Clamp,
			FilterMode = TextureFilterMode.PointMipLinear,
			MaxLod = 4f
		};
		m_chunksToDraw = new DynamicArray<TerrainChunk>();
		m_subsystemTerrain = subsystemTerrain;
		m_subsystemMiningLight = subsystemTerrain.Project.FindSubsystem<SubsystemMiningLightBehavior>(throwOnError: false);
		m_subsystemTime = subsystemTerrain.Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemSky = subsystemTerrain.Project.FindSubsystem<SubsystemSky>(throwOnError: true);
		m_timeOfDay = subsystemTerrain.Project.FindSubsystem<SubsystemTimeOfDay>(throwOnError: true);
		m_subsystemWeather = subsystemTerrain.Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
		m_subsystemAudio = subsystemTerrain.Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemGameInfo = subsystemTerrain.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemAnimatedTextures = subsystemTerrain.SubsystemAnimatedTextures;
		if (OpaqueShader == null)
		{
			OpaqueShader = new Shader(ShaderCodeManager.Get("Shaders/Opaque.vsh"), ShaderCodeManager.Get("Shaders/Opaque.psh"), new ShaderMacro("Opaque"));
		}
		bool num = SettingsManager.ShadersRenderRange <= 0;
		if (AlphatestedShader == null)
		{
			if (num)
			{
				AlphatestedShader = new Shader(ShaderCodeManager.Get("Shaders/OldShaders/AlphaTestedOld.vsh"), ShaderCodeManager.GetFast("Shaders/OldShaders/AlphaTestedOld.psh"), new ShaderMacro("ALPHATESTED"));
				AlphatestedOldShader = AlphatestedShader;
			}
			else
			{
				AlphatestedShader = new Shader(ShaderCodeManager.Get("Shaders/AlphaTested.vsh"), ShaderCodeManager.GetFast("Shaders/AlphaTested.psh"), new ShaderMacro("ALPHATESTED"));
				AlphatestedOldShader = new Shader(ShaderCodeManager.Get("Shaders/OldShaders/AlphaTestedOld.vsh"), ShaderCodeManager.GetFast("Shaders/OldShaders/AlphaTestedOld.psh"), new ShaderMacro("ALPHATESTED"));
			}
		}
		if (TransparentShader == null)
		{
			if (num)
			{
				TransparentShader = new Shader(ShaderCodeManager.Get("Shaders/OldShaders/TransparentOld.vsh"), ShaderCodeManager.GetFast("Shaders/OldShaders/TransparentOld.psh"), new ShaderMacro("Transparent"));
				TransparentOldShader = TransparentShader;
			}
			else
			{
				TransparentShader = new Shader(ShaderCodeManager.Get("Shaders/Transparent.vsh"), ShaderCodeManager.GetFast("Shaders/Transparent.psh"), new ShaderMacro("Transparent"));
				TransparentOldShader = new Shader(ShaderCodeManager.Get("Shaders/OldShaders/TransparentOld.vsh"), ShaderCodeManager.GetFast("Shaders/OldShaders/TransparentOld.psh"), new ShaderMacro("Transparent"));
			}
		}
		if (DistantShader == null && DistantSimpleShader == null)
		{
			try
			{
				if (SettingsManager.DistantChunksDetailLevel >= 4)
				{
					DistantShader = new Shader(ShaderCodeManager.Get("Shaders/Distant.vsh"), ShaderCodeManager.GetFast("Shaders/Distant.psh"));
					DistantSimpleShader = DistantShader;
				}
				else
				{
					DistantSimpleShader = new Shader(ShaderCodeManager.Get("Shaders/DistantSimple.vsh"), ShaderCodeManager.GetFast("Shaders/DistantSimple.psh"));
					DistantShader = DistantSimpleShader;
				}
			}
			catch (Exception ex)
			{
				Log.Error("Failed to load Distant shader: " + ex.Message);
			}
		}
		Display.DeviceReset += Display_DeviceReset;
	}

	public void PrepareForDrawing(Camera camera)
	{
		Vector2 xZ = camera.ViewPosition.XZ;
		float num = MathUtils.Sqr(m_subsystemSky.VisibilityRange);
		BoundingFrustum viewFrustum = camera.ViewFrustum;
		int gameWidgetIndex = camera.GameWidget.GameWidgetIndex;
		m_chunksToDraw.Clear();
		float value = (float)MathUtils.Remainder(m_subsystemGameInfo.TotalElapsedGameTime, 16384.0);
		AlphatestedShader.GetParameter("u_time", allowNull: true)?.SetValue(value);
		TerrainChunk[] allocatedChunks = m_subsystemTerrain.Terrain.AllocatedChunks;
		foreach (TerrainChunk terrainChunk in allocatedChunks)
		{
			if (!terrainChunk.FogEnds.ContainsKey(gameWidgetIndex))
			{
				terrainChunk.FogEnds.Add(gameWidgetIndex, 0f);
			}
			if (terrainChunk.NewGeometryData)
			{
				lock (terrainChunk.Geometry)
				{
					if (terrainChunk.NewGeometryData)
					{
						terrainChunk.NewGeometryData = false;
						SetupTerrainChunkGeometryVertexIndexBuffers(terrainChunk);
					}
				}
			}
			terrainChunk.DrawDistanceSquared = Vector2.DistanceSquared(xZ, terrainChunk.Center);
			if (terrainChunk.DrawDistanceSquared <= num)
			{
				if (viewFrustum.Intersection(terrainChunk.BoundingBox))
				{
					m_chunksToDraw.Add(terrainChunk);
				}
				if (terrainChunk.State != TerrainChunkState.Valid)
				{
					continue;
				}
				if (SettingsManager.DistantChunksRange > SettingsManager.VisibilityRange)
				{
					terrainChunk.FogEnds[gameWidgetIndex] = float.MaxValue;
				}
				float num2 = terrainChunk.FogEnds[gameWidgetIndex];
				if (num2 != float.MaxValue)
				{
					if (num2 == 0f)
					{
						StartChunkFadeIn(camera, terrainChunk);
					}
					else
					{
						RunChunkFadeIn(camera, terrainChunk);
					}
				}
			}
			else
			{
				terrainChunk.FogEnds[gameWidgetIndex] = 0f;
			}
		}
		ChunksDrawn = 0;
		ChunkDrawCalls = 0;
		ChunkTrianglesDrawn = 0;
	}

	public void DrawOpaque(Camera camera)
	{
		int gameWidgetIndex = camera.GameWidget.GameWidgetIndex;
		Vector3 viewPosition = camera.ViewPosition;
		Vector3 vector = new Vector3(MathUtils.Floor(viewPosition.X), 0f, MathUtils.Floor(viewPosition.Z));
		Matrix value = Matrix.CreateTranslation(vector - viewPosition) * camera.ViewMatrix.OrientationMatrix * camera.ProjectionMatrix;
		Display.BlendState = BlendState.Opaque;
		Display.DepthStencilState = DepthStencilState.Default;
		Display.RasterizerState = RasterizerState.CullCounterClockwiseScissor;
		Shader opaqueShader = OpaqueShader;
		List<SubsystemMiningLightBehavior.MiningLightData> list = new List<SubsystemMiningLightBehavior.MiningLightData>();
		if (m_subsystemMiningLight != null)
		{
			list.AddRange(m_subsystemMiningLight.ActiveLights.Values);
			list.AddRange(m_subsystemMiningLight.ActiveHeadLights.Values);
		}
		List<SubsystemMiningLightBehavior.MiningLightData> list2 = list.OrderBy((SubsystemMiningLightBehavior.MiningLightData l) => Vector3.DistanceSquared(viewPosition, l.Position)).Take(8).ToList();
		for (int num = 1; num <= 8; num++)
		{
			string text = num.ToString();
			if (num <= list2.Count)
			{
				SubsystemMiningLightBehavior.MiningLightData miningLightData = list2[num - 1];
				opaqueShader.GetParameter("u_lp" + text)?.SetValue(miningLightData.Position);
				opaqueShader.GetParameter("u_lc" + text)?.SetValue(new Vector3(0.3575f));
				opaqueShader.GetParameter("u_lr" + text)?.SetValue(miningLightData.Intensity * 1.5f);
			}
			else
			{
				opaqueShader.GetParameter("u_lp" + text)?.SetValue(Vector3.Zero);
				opaqueShader.GetParameter("u_lc" + text)?.SetValue(Vector3.Zero);
				opaqueShader.GetParameter("u_lr" + text)?.SetValue(0f);
			}
		}
		opaqueShader.GetParameter("u_origin", allowNull: true).SetValue(vector.XZ);
		opaqueShader.GetParameter("u_viewProjectionMatrix", allowNull: true).SetValue(value);
		opaqueShader.GetParameter("u_viewPosition", allowNull: true).SetValue(viewPosition);
		opaqueShader.GetParameter("u_samplerState", allowNull: true).SetValue(SettingsManager.TerrainMipmapsEnabled ? m_samplerStateMips : m_samplerState);
		opaqueShader.GetParameter("u_fogYMultiplier", allowNull: true).SetValue(m_subsystemSky.VisibilityRangeYMultiplier);
		opaqueShader.GetParameter("u_fogColor", allowNull: true).SetValue(new Vector3(m_subsystemSky.ViewFogColor));
		ShaderParameter parameter = opaqueShader.GetParameter("u_fogStartInvLength", allowNull: true);
		Point2 point = Terrain.ToChunk(camera.ViewPosition.XZ);
		m_subsystemTerrain.Terrain.GetChunkAtCoords(point.X, point.Y);
		for (int num2 = 0; num2 < m_chunksToDraw.Count; num2++)
		{
			TerrainChunk terrainChunk = m_chunksToDraw[num2];
			float num3 = MathUtils.Min(terrainChunk.FogEnds[gameWidgetIndex], m_subsystemSky.ViewFogRange.Y);
			float num4 = MathUtils.Min(m_subsystemSky.ViewFogRange.X, num3 - 1f);
			parameter.SetValue(new Vector2(num4, 1f / (num3 - num4)));
			int num5 = 16;
			if (viewPosition.Z > terrainChunk.BoundingBox.Min.Z)
			{
				num5 |= 1;
			}
			if (viewPosition.X > terrainChunk.BoundingBox.Min.X)
			{
				num5 |= 2;
			}
			if (viewPosition.Z < terrainChunk.BoundingBox.Max.Z)
			{
				num5 |= 4;
			}
			if (viewPosition.X < terrainChunk.BoundingBox.Max.X)
			{
				num5 |= 8;
			}
			DrawTerrainChunkGeometrySubsets(opaqueShader, terrainChunk, num5);
			ChunksDrawn++;
		}
	}

	public void DrawAlphaTested(Camera camera)
	{
		m_subsystemWeather.ComputeWindParameters(out var windBlocked, out var playersDatum);
		Vector2 currentWindVector = m_subsystemWeather.CurrentWindVector;
		Vector2 value = Vector2.Zero;
		float value2 = 0f;
		float value3 = 0f;
		if (m_subsystemWeather.IsCycloneActive && m_subsystemWeather.ActiveTornadoes.Count > 0)
		{
			Vector2 xZ = camera.ViewPosition.XZ;
			float num = float.MaxValue;
			foreach (Vector2 activeTornado in m_subsystemWeather.ActiveTornadoes)
			{
				float num2 = Vector2.DistanceSquared(xZ, activeTornado);
				if (num2 < num)
				{
					num = num2;
					value = activeTornado;
				}
			}
			value3 = 1f;
			value2 = MathUtils.Lerp(32f, 96f, MathUtils.Saturate(m_subsystemWeather.TornadoStrength));
		}
		Shader s1 = AlphatestedShader;
		Shader alphatestedOldShader = AlphatestedOldShader;
		s1.GetParameter("u_tornadoCenter", allowNull: true)?.SetValue(value);
		s1.GetParameter("u_tornadoRadius", allowNull: true)?.SetValue(value2);
		s1.GetParameter("u_hasTornado", allowNull: true)?.SetValue(value3);
		s1.GetParameter("u_windBlocked", allowNull: true)?.SetValue(windBlocked);
		s1.GetParameter("u_windStrength", allowNull: true)?.SetValue(playersDatum);
		s1.GetParameter("u_windDirection", allowNull: true)?.SetValue(currentWindVector);
		s1.GetParameter("u_treeSwayEnabled", allowNull: true)?.SetValue(SettingsManager.TreeSwayEnabled ? 1f : 0f);
		s1.GetParameter("u_plantWindEnabled", allowNull: true)?.SetValue(SettingsManager.PlantWindAnimationsEnabled ? 1f : 0f);
		s1.GetParameter("u_time", allowNull: true)?.SetValue((float)MathUtils.Remainder(m_subsystemGameInfo.TotalElapsedGameTime, 16384.0));
		s1.GetParameter("u_windPhaseMain", allowNull: true)?.SetValue(m_subsystemWeather.WindPhaseMain);
		s1.GetParameter("u_windPhaseSide", allowNull: true)?.SetValue(m_subsystemWeather.WindPhaseSide);
		int defaultTextureSlot = BlocksManager.Blocks[104].DefaultTextureSlot;
		s1.GetParameter("u_fireBaseSlot", allowNull: true)?.SetValue(new Vector2(defaultTextureSlot % 16, defaultTextureSlot / 16));
		s1.GetParameter("u_fireHeight", allowNull: true)?.SetValue(3f);
		List<SubsystemMiningLightBehavior.MiningLightData> list = new List<SubsystemMiningLightBehavior.MiningLightData>();
		if (m_subsystemMiningLight != null)
		{
			list.AddRange(m_subsystemMiningLight.ActiveLights.Values);
			list.AddRange(m_subsystemMiningLight.ActiveHeadLights.Values);
		}
		List<SubsystemMiningLightBehavior.MiningLightData> list2 = list.OrderBy((SubsystemMiningLightBehavior.MiningLightData l) => Vector3.DistanceSquared(camera.ViewPosition, l.Position)).Take(8).ToList();
		for (int num3 = 1; num3 <= 8; num3++)
		{
			string text = num3.ToString();
			bool flag = num3 <= list2.Count;
			Vector3 value4 = (flag ? list2[num3 - 1].Position : Vector3.Zero);
			Vector3 value5 = (flag ? new Vector3(0.3575f) : Vector3.Zero);
			float value6 = (flag ? (list2[num3 - 1].Intensity * 1.5f) : 0f);
			s1.GetParameter("u_lp" + text, allowNull: true)?.SetValue(value4);
			s1.GetParameter("u_lc" + text, allowNull: true)?.SetValue(value5);
			s1.GetParameter("u_lr" + text, allowNull: true)?.SetValue(value6);
		}
		Vector3 viewPosition = camera.ViewPosition;
		Vector3 vector = new Vector3(MathUtils.Floor(viewPosition.X), 0f, MathUtils.Floor(viewPosition.Z));
		Matrix value7 = Matrix.CreateTranslation(vector - viewPosition) * camera.ViewMatrix.OrientationMatrix * camera.ProjectionMatrix;
		SamplerState value8 = (SettingsManager.TerrainMipmapsEnabled ? m_samplerStateMips : m_samplerState);
		Vector3 value9 = new Vector3(m_subsystemSky.ViewFogColor);
		Shader[] array = new Shader[2] { s1, alphatestedOldShader };
		foreach (Shader obj in array)
		{
			obj.GetParameter("u_origin", allowNull: true).SetValue(vector.XZ);
			obj.GetParameter("u_viewProjectionMatrix", allowNull: true).SetValue(value7);
			obj.GetParameter("u_viewPosition", allowNull: true).SetValue(viewPosition);
			obj.GetParameter("u_samplerState", allowNull: true).SetValue(value8);
			obj.GetParameter("u_fogYMultiplier", allowNull: true).SetValue(m_subsystemSky.VisibilityRangeYMultiplier);
			obj.GetParameter("u_fogColor", allowNull: true).SetValue(value9);
		}
		ShaderParameter parameter = s1.GetParameter("u_fogStartInvLength", allowNull: true);
		ShaderParameter parameter2 = alphatestedOldShader.GetParameter("u_fogStartInvLength", allowNull: true);
		ModsManager.HookAction("SetShaderParameter", (ModLoader mod) =>
		{
			mod.SetShaderParameter(s1, camera);
			return true;
		});
		SubsystemNaturalTreeScanner subsystemNaturalTreeScanner = m_subsystemTerrain.Project.FindSubsystem<SubsystemNaturalTreeScanner>(throwOnError: true);
		if (subsystemNaturalTreeScanner != null)
		{
			float smoothedRootY = subsystemNaturalTreeScanner.SmoothedRootY;
			Vector3 value10 = new Vector3(MathUtils.Floor(viewPosition.X) + 0.5f, smoothedRootY, MathUtils.Floor(viewPosition.Z) + 0.5f);
			s1.GetParameter("u_treeRoot", allowNull: true)?.SetValue(value10);
			s1.GetParameter("u_treeTop", allowNull: true)?.SetValue(new Vector3(value10.X, smoothedRootY + MathUtils.Clamp(SettingsManager.TreeSwayBandHeight, 6f, 50f), value10.Z));
		}
		Display.BlendState = BlendState.Opaque;
		Display.DepthStencilState = DepthStencilState.Default;
		Display.RasterizerState = RasterizerState.CullCounterClockwiseScissor;
		for (int num5 = 0; num5 < m_chunksToDraw.Count; num5++)
		{
			TerrainChunk terrainChunk = m_chunksToDraw[num5];
			float num6 = MathUtils.Min(terrainChunk.FogEnds[camera.GameWidget.GameWidgetIndex], m_subsystemSky.ViewFogRange.Y);
			float num7 = MathUtils.Min(m_subsystemSky.ViewFogRange.X, num6 - 1f);
			Vector2 value11 = new Vector2(num7, 1f / (num6 - num7));
			if (UseOldShaderForChunk(terrainChunk))
			{
				parameter2.SetValue(value11);
				DrawTerrainChunkGeometrySubsets(alphatestedOldShader, terrainChunk, 32);
			}
			else
			{
				parameter.SetValue(value11);
				DrawTerrainChunkGeometrySubsets(s1, terrainChunk, 32);
			}
		}
	}

	public void DrawTransparent(Camera camera)
	{
		Shader transparentShader = TransparentShader;
		Shader transparentOldShader = TransparentOldShader;
		transparentShader.GetParameter("u_time", allowNull: true)?.SetValue((float)MathUtils.Remainder(m_subsystemGameInfo.TotalElapsedGameTime, 16384.0));
		m_subsystemWeather.ComputeWindParameters(out var windBlocked, out var windStrength);
		transparentShader.GetParameter("u_windBlocked", allowNull: true)?.SetValue(windBlocked);
		transparentShader.GetParameter("u_windStrength", allowNull: true)?.SetValue(windStrength);
		transparentShader.GetParameter("u_windStrengthSmooth", allowNull: true)?.SetValue(m_subsystemWeather.PhaseWindStrength);
		transparentShader.GetParameter("u_windDirection", allowNull: true)?.SetValue(m_subsystemWeather.CurrentWindVector);
		transparentShader.GetParameter("u_windPhaseMain", allowNull: true)?.SetValue(m_subsystemWeather.WindPhaseMain);
		transparentShader.GetParameter("u_windPhaseSide", allowNull: true)?.SetValue(m_subsystemWeather.WindPhaseSide);
		SubsystemOceanScanner subsystemOceanScanner = m_subsystemTerrain.Project.FindSubsystem<SubsystemOceanScanner>(throwOnError: true);
		transparentShader.GetParameter("u_oceanStrength", allowNull: true)?.SetValue(subsystemOceanScanner?.OceanWaveStrength ?? 0f);
		transparentShader.GetParameter("u_oceanBaseY", allowNull: true)?.SetValue((subsystemOceanScanner != null) ? ((float)subsystemOceanScanner.OceanLevel + 1f) : 65f);
		transparentShader.GetParameter("u_nonOceanWavesEnabled", allowNull: true)?.SetValue(SettingsManager.WaterWavesAnimationEnabled ? 1f : 0f);
		int gameWidgetIndex = camera.GameWidget.GameWidgetIndex;
		Vector3 viewPos = camera.ViewPosition;
		Vector3 vector = new Vector3(MathUtils.Floor(viewPos.X), 0f, MathUtils.Floor(viewPos.Z));
		Matrix value = Matrix.CreateTranslation(vector - viewPos) * camera.ViewMatrix.OrientationMatrix * camera.ProjectionMatrix;
		Display.BlendState = BlendState.AlphaBlend;
		Display.DepthStencilState = DepthStencilState.Default;
		Display.RasterizerState = ((m_subsystemSky.ViewUnderWaterDepth > 0f) ? RasterizerState.CullClockwiseScissor : RasterizerState.CullCounterClockwiseScissor);
		List<SubsystemMiningLightBehavior.MiningLightData> list = new List<SubsystemMiningLightBehavior.MiningLightData>();
		if (m_subsystemMiningLight != null)
		{
			list.AddRange(m_subsystemMiningLight.ActiveLights.Values);
			list.AddRange(m_subsystemMiningLight.ActiveHeadLights.Values);
		}
		List<SubsystemMiningLightBehavior.MiningLightData> list2 = list.OrderBy((SubsystemMiningLightBehavior.MiningLightData l) => Vector3.DistanceSquared(viewPos, l.Position)).Take(8).ToList();
		for (int num = 0; num < 8; num++)
		{
			string text = (num + 1).ToString();
			bool flag = num < list2.Count;
			transparentShader.GetParameter("u_lightPosition" + text, allowNull: true)?.SetValue(flag ? list2[num].Position : Vector3.Zero);
			transparentShader.GetParameter("u_lightColor" + text, allowNull: true)?.SetValue(flag ? (new Vector3(0.65f, 0.65f, 0.65f) * 0.55f) : Vector3.Zero);
			transparentShader.GetParameter("u_lightRange" + text, allowNull: true)?.SetValue(flag ? (list2[num].Intensity * 1.5f) : 0f);
		}
		SamplerState value2 = (SettingsManager.TerrainMipmapsEnabled ? m_samplerStateMips : m_samplerState);
		Vector3 value3 = new Vector3(m_subsystemSky.ViewFogColor);
		float visibilityRangeYMultiplier = m_subsystemSky.VisibilityRangeYMultiplier;
		Shader[] array = new Shader[2] { transparentShader, transparentOldShader };
		foreach (Shader obj in array)
		{
			obj.GetParameter("u_origin", allowNull: true).SetValue(vector.XZ);
			obj.GetParameter("u_viewProjectionMatrix", allowNull: true).SetValue(value);
			obj.GetParameter("u_viewPosition", allowNull: true).SetValue(viewPos);
			obj.GetParameter("u_samplerState", allowNull: true).SetValue(value2);
			obj.GetParameter("u_fogYMultiplier", allowNull: true).SetValue(visibilityRangeYMultiplier);
			obj.GetParameter("u_fogColor", allowNull: true).SetValue(value3);
		}
		ShaderParameter parameter = transparentShader.GetParameter("u_fogStartInvLength", allowNull: true);
		ShaderParameter parameter2 = transparentOldShader.GetParameter("u_fogStartInvLength", allowNull: true);
		ModsManager.HookAction("SetShaderParameter", (ModLoader modLoader) =>
		{
			modLoader.SetShaderParameter(transparentShader, camera);
			return true;
		});
		for (int num3 = 0; num3 < m_chunksToDraw.Count; num3++)
		{
			TerrainChunk terrainChunk = m_chunksToDraw[num3];
			float num4 = MathUtils.Min(terrainChunk.FogEnds[gameWidgetIndex], m_subsystemSky.ViewFogRange.Y);
			float num5 = MathUtils.Min(m_subsystemSky.ViewFogRange.X, num4 - 1f);
			Vector2 value4 = new Vector2(num5, 1f / (num4 - num5));
			if (UseOldShaderForChunk(terrainChunk))
			{
				parameter2.SetValue(value4);
				DrawTerrainChunkGeometrySubsets(transparentOldShader, terrainChunk, 64);
			}
			else
			{
				parameter.SetValue(value4);
				DrawTerrainChunkGeometrySubsets(transparentShader, terrainChunk, 64);
			}
		}
	}

	public void Dispose()
	{
		Display.DeviceReset -= Display_DeviceReset;
	}

	public void Display_DeviceReset()
	{
		m_subsystemTerrain.TerrainUpdater.DowngradeAllChunksState(TerrainChunkState.InvalidVertices1, forceGeometryRegeneration: false);
		TerrainChunk[] gameWidget = m_subsystemTerrain.Terrain.AllocatedChunks;
		foreach (TerrainChunk chunk in gameWidget)
		{
			DisposeTerrainChunkGeometryVertexIndexBuffers(chunk);
		}
	}

	public void DisposeTerrainChunkGeometryVertexIndexBuffers(TerrainChunk chunk)
	{
		foreach (TerrainChunkGeometry.Buffer buffer in chunk.Buffers)
		{
			buffer.Dispose();
		}
		chunk.Buffers.Clear();
		chunk.InvalidateSliceContentsHashes();
	}

	public void SetupTerrainChunkGeometryVertexIndexBuffers(TerrainChunk chunk)
	{
		DisposeTerrainChunkGeometryVertexIndexBuffers(chunk);
		CompileDrawSubsets(chunk.Draws, chunk.Buffers);
		chunk.CopySliceContentsHashes();
	}

	public static void CompileDrawSubsets(Dictionary<Texture2D, TerrainGeometry[]> list, DynamicArray<TerrainChunkGeometry.Buffer> buffers, Func<TerrainVertex, TerrainVertex> vertexTransform = null)
	{
		foreach (KeyValuePair<Texture2D, TerrainGeometry[]> item2 in list)
		{
			TerrainGeometry[] value = item2.Value;
			int num = 0;
			while (num < 112)
			{
				int num2 = 0;
				int num3 = 0;
				int i;
				for (i = num; i < 112; i++)
				{
					int num4 = i / 16;
					int num5 = i % 16;
					TerrainGeometrySubset terrainGeometrySubset = value[num5].Subsets[num4];
					if (vertexTransform != null)
					{
						DynamicArray<TerrainVertex> dynamicArray = new DynamicArray<TerrainVertex>();
						for (int j = 0; j < terrainGeometrySubset.Vertices.Count; j++)
						{
							TerrainVertex item = vertexTransform(terrainGeometrySubset.Vertices[j]);
							dynamicArray.Add(item);
						}
						terrainGeometrySubset.Vertices = dynamicArray;
					}
					if (num2 + terrainGeometrySubset.Vertices.Count > 65535 && i > num)
					{
						break;
					}
					num2 += terrainGeometrySubset.Vertices.Count;
					num3 += terrainGeometrySubset.Indices.Count;
				}
				if (num2 > 0 && num3 > 0)
				{
					TerrainChunkGeometry.Buffer buffer = new TerrainChunkGeometry.Buffer();
					buffer.Texture = item2.Key;
					buffers.Add(buffer);
					buffer.VertexBuffer = new VertexBuffer(TerrainVertex.VertexDeclaration, num2);
					buffer.IndexBuffer = new IndexBuffer(IndexFormat.ThirtyTwoBits, num3);
					int num6 = 0;
					int num7 = 0;
					for (int k = num; k < i; k++)
					{
						int num8 = k / 16;
						int num9 = k % 16;
						TerrainGeometrySubset terrainGeometrySubset2 = value[num9].Subsets[num8];
						if (num9 == 0 || k == num)
						{
							buffer.SubsetIndexBufferStarts[num8] = num7;
						}
						if (terrainGeometrySubset2.Indices.Count > 0)
						{
							m_tmpIndices.Count = terrainGeometrySubset2.Indices.Count;
							ShiftIndices(terrainGeometrySubset2.Indices.Array, m_tmpIndices.Array, num6, terrainGeometrySubset2.Indices.Count);
							buffer.IndexBuffer.SetData(m_tmpIndices.Array, 0, m_tmpIndices.Count, num7);
							num7 += m_tmpIndices.Count;
						}
						if (terrainGeometrySubset2.Vertices.Count > 0)
						{
							buffer.VertexBuffer.SetData(terrainGeometrySubset2.Vertices.Array, 0, terrainGeometrySubset2.Vertices.Count, num6);
							num6 += terrainGeometrySubset2.Vertices.Count;
						}
						if (num9 == 15 || k == i - 1)
						{
							buffer.SubsetIndexBufferEnds[num8] = num7;
						}
					}
				}
				num = i;
			}
		}
	}

	public void DrawTerrainChunkGeometrySubsets(Shader shader, TerrainChunk chunk, int subsetsMask, bool ApplyTexture = true)
	{
		foreach (TerrainChunkGeometry.Buffer buffer in chunk.Buffers)
		{
			int num = int.MaxValue;
			int num2 = 0;
			for (int i = 0; i < 8; i++)
			{
				if (i < 7 && (subsetsMask & (1 << i)) != 0)
				{
					if (buffer.SubsetIndexBufferEnds[i] > 0)
					{
						if (num == int.MaxValue)
						{
							num = buffer.SubsetIndexBufferStarts[i];
						}
						num2 = buffer.SubsetIndexBufferEnds[i];
					}
					continue;
				}
				if (num2 > num)
				{
					if (ApplyTexture)
					{
						shader.GetParameter("u_texture", allowNull: true).SetValue(buffer.Texture);
					}
					Display.DrawIndexed(PrimitiveType.TriangleList, shader, buffer.VertexBuffer, buffer.IndexBuffer, num, num2 - num);
					ChunkTrianglesDrawn += (num2 - num) / 3;
					ChunkDrawCalls++;
				}
				num = int.MaxValue;
			}
		}
	}

	public void StartChunkFadeIn(Camera camera, TerrainChunk chunk)
	{
		Vector3 viewPosition = camera.ViewPosition;
		Vector2 v = new Vector2(chunk.Origin.X, chunk.Origin.Y);
		Vector2 v2 = new Vector2(chunk.Origin.X + 16, chunk.Origin.Y);
		Vector2 v3 = new Vector2(chunk.Origin.X, chunk.Origin.Y + 16);
		Vector2 v4 = new Vector2(chunk.Origin.X + 16, chunk.Origin.Y + 16);
		float x = Vector2.Distance(viewPosition.XZ, v);
		float x2 = Vector2.Distance(viewPosition.XZ, v2);
		float x3 = Vector2.Distance(viewPosition.XZ, v3);
		float x4 = Vector2.Distance(viewPosition.XZ, v4);
		chunk.FogEnds[camera.GameWidget.GameWidgetIndex] = MathUtils.Max(MathUtils.Min(x, x2, x3, x4), 0.001f);
	}

	public void RunChunkFadeIn(Camera camera, TerrainChunk chunk)
	{
		chunk.FogEnds[camera.GameWidget.GameWidgetIndex] += 32f * Time.FrameDuration;
		if (chunk.FogEnds[camera.GameWidget.GameWidgetIndex] >= m_subsystemSky.ViewFogRange.Y)
		{
			chunk.FogEnds[camera.GameWidget.GameWidgetIndex] = float.MaxValue;
		}
	}

	public static void ShiftIndices(int[] source, int[] destination, int shift, int count)
	{
		for (int i = 0; i < count; i++)
		{
			destination[i] = source[i] + shift;
		}
	}

	static TerrainRenderer()
	{
		m_tmpIndices = new DynamicArray<int>();
	}

	private bool UseOldShaderForChunk(TerrainChunk chunk)
	{
		if (AlphatestedShader == AlphatestedOldShader)
		{
			return true;
		}
		int visibilityRange = SettingsManager.VisibilityRange;
		int num = MathUtils.Min(SettingsManager.ShadersRenderRange, visibilityRange);
		if (num >= visibilityRange)
		{
			return false;
		}
		float num2 = num * num;
		return chunk.DrawDistanceSquared >= num2;
	}

	public void DrawDistantChunks(Camera camera, IEnumerable<DistantChunk> chunks, Vector3 lightDir)
	{
		Shader shader = ((SettingsManager.DistantChunksDetailLevel >= 4) ? DistantShader : DistantSimpleShader);
		if (shader == null)
		{
			return;
		}
		Display.BlendState = BlendState.Opaque;
		Display.DepthStencilState = DepthStencilState.Default;
		Display.RasterizerState = RasterizerState.CullNone;
		Vector3 viewPosition = camera.ViewPosition;
		Vector3 vector = new Vector3(MathUtils.Floor(viewPosition.X), 0f, MathUtils.Floor(viewPosition.Z));
		Matrix value = Matrix.CreateTranslation(vector - viewPosition) * camera.ViewMatrix.OrientationMatrix * camera.ProjectionMatrix;
		try
		{
			shader.GetParameter("u_viewProjectionMatrix", allowNull: true).SetValue(value);
			shader.GetParameter("u_origin", allowNull: true).SetValue(vector.XZ);
			shader.GetParameter("u_viewPosition", allowNull: true).SetValue(viewPosition);
			shader.GetParameter("u_fogYMultiplier", allowNull: true).SetValue(m_subsystemSky.VisibilityRangeYMultiplier);
			shader.GetParameter("u_fogColor", allowNull: true).SetValue(new Vector3(m_subsystemSky.ViewFogColor));
			shader.GetParameter("u_lightDirection", allowNull: true).SetValue(lightDir);
		}
		catch (Exception ex)
		{
			if (SettingsManager.DebugMode)
			{
				Log.Error("[DrawDistantChunks] Params Init Crash: " + ex.Message);
			}
		}
		float skyLightIntensity = m_subsystemSky.SkyLightIntensity;
		float num;
		switch (m_subsystemSky.MoonPhase)
		{
		case 0:
			num = 0.18f;
			break;
		case 1:
		case 7:
			num = 0.14f;
			break;
		case 2:
		case 6:
			num = 0.1f;
			break;
		case 3:
		case 5:
			num = 0.06f;
			break;
		case 4:
			num = 0.02f;
			break;
		default:
			num = 0.1f;
			break;
		}
		if (m_subsystemSky.IsSolarEclipse)
		{
			num *= 0.2f;
		}
		float value2 = MathUtils.Max(skyLightIntensity, num);
		try
		{
			shader.GetParameter("u_brightness", allowNull: true).SetValue(value2);
		}
		catch (Exception ex2)
		{
			if (SettingsManager.DebugMode)
			{
				Log.Error("[DrawDistantChunks] u_brightness Crash: " + ex2.Message);
			}
		}
		float f = m_subsystemSky.Project.FindSubsystem<SubsystemWeather>(throwOnError: true)?.GlobalPrecipitationIntensity ?? 0f;
		float num2 = (float)SettingsManager.VisibilityRange + (float)SettingsManager.DistantChunksRange;
		float num3 = MathUtils.Lerp(num2 * 0.7f, num2 * 0.3f, f);
		float num4 = MathUtils.Lerp(num2 * 0.95f, num2 * 0.7f, f);
		float z = ((float)SettingsManager.VisibilityRange * 0.8f + 50f) * 0.5f;
		Vector3 value3 = new Vector3(num3, 1f / MathUtils.Max(num4 - num3, 0.1f), z);
		try
		{
			shader.GetParameter("u_fogStartInvLength", allowNull: true).SetValue(value3);
		}
		catch (Exception ex3)
		{
			if (SettingsManager.DebugMode)
			{
				Log.Error("[DrawDistantChunks] u_fogStartInvLength Crash: " + ex3.Message);
			}
		}
		foreach (DistantChunk chunk in chunks)
		{
			if (!chunk.IsReady || chunk.VertexBuffer == null || !camera.ViewFrustum.Intersection(chunk.BoundingBox))
			{
				continue;
			}
			try
			{
				Display.DrawIndexed(PrimitiveType.TriangleList, shader, chunk.VertexBuffer, chunk.IndexBuffer, 0, chunk.IndexCount);
				ChunksDrawn++;
				ChunkDrawCalls++;
				ChunkTrianglesDrawn += chunk.IndexCount / 3;
			}
			catch (Exception ex4)
			{
				if (SettingsManager.DebugMode)
				{
					Log.Error("[DrawDistantChunks] DrawIndexed Crash for LOD: " + ex4.Message);
				}
			}
		}
	}
}
