using System.Collections.Generic;
using Engine;
using Engine.Graphics;
using Game.NetWork;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentDiggingCracks : Component, IDrawable
{
	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemSky m_subsystemSky;

	public ComponentMiner m_componentMiner;

	public Texture2D[] m_textures;

	private Texture2D defaultTexture;

	private int defaultSlice;

	public Shader m_shader;

	public Dictionary<Texture2D, TerrainGeometry[]> DrawItem;

	public TerrainGeometry m_geometry;

	public Point3 m_point;

	public int m_value;

	public static int[] m_drawOrders;

	private SubsystemPlayers m_players;

	private SubsystemSky m_sky;

	public DynamicArray<TerrainChunkGeometry.Buffer> Buffers = new DynamicArray<TerrainChunkGeometry.Buffer>();

	private Texture2D[] m_texturesShovel;

	private Texture2D[] m_texturesQuarry;

	private Texture2D[] m_texturesHack;

	public SubsystemTime m_subsystemTime;

	private SubsystemTimeOfDay m_timeOfDay;

	private SubsystemGameInfo m_subsystemGameInfo;

	public int[] DrawOrders => m_drawOrders;

	public void DisposeBuffers()
	{
		foreach (TerrainChunkGeometry.Buffer buffer in Buffers)
		{
			buffer.Dispose();
		}
		Buffers.Clear();
	}

	public void Draw(Camera camera, int drawOrder)
	{
		if (!m_componentMiner.DigCellFace.HasValue || m_componentMiner.DigCracksProgress <= 0f)
		{
			return;
		}
		Point3 point = m_componentMiner.DigCellFace.Value.Point;
		if (CommonLib.WorkType == WorkType.Client && (m_players.MainPlayer == null || Vector3.DistanceSquared(m_players.MainPlayer.GameWidget.ActiveCamera.ViewPosition, new Vector3(point)) > (float)MathUtils.Sqr(SettingsManager.VisibilityRange)))
		{
			return;
		}
		int cellValue = m_subsystemTerrain.Terrain.GetCellValue(point.X, point.Y, point.Z);
		int num = Terrain.ExtractContents(cellValue);
		Block block = BlocksManager.Blocks[num];
		if (cellValue != m_value || point != m_point)
		{
			foreach (KeyValuePair<Texture2D, TerrainGeometry[]> item in DrawItem)
			{
				TerrainGeometrySubset[] subsets = item.Value[defaultSlice].Subsets;
				for (int i = 0; i < subsets.Length; i++)
				{
					subsets[i].Indices.Clear();
					subsets[i].Vertices.Clear();
				}
			}
			DisposeBuffers();
			block.GenerateTerrainVertices(m_subsystemTerrain.BlockGeometryGenerator, DrawItem[defaultTexture][defaultSlice], cellValue, point.X, point.Y, point.Z);
			m_point = point;
			m_value = cellValue;
			TerrainRenderer.CompileDrawSubsets(DrawItem, Buffers, block.SetDiggingCrackingTextureTransform);
		}
		Vector3 viewPosition = camera.ViewPosition;
		Vector3 vector = new Vector3(MathUtils.Floor(viewPosition.X), 0f, MathUtils.Floor(viewPosition.Z));
		Matrix value = Matrix.CreateTranslation(vector - viewPosition) * camera.ViewMatrix.OrientationMatrix * camera.ProjectionMatrix;
		try
		{
			Display.BlendState = BlendState.NonPremultiplied;
			Display.DepthStencilState = DepthStencilState.Default;
			Display.RasterizerState = RasterizerState.CullCounterClockwiseScissor;
			m_shader.GetParameter("u_origin").SetValue(vector.XZ);
			m_shader.GetParameter("u_viewProjectionMatrix").SetValue(value);
			m_shader.GetParameter("u_viewPosition").SetValue(viewPosition);
			m_shader.GetParameter("u_samplerState").SetValue(SamplerState.PointWrap);
			m_shader.GetParameter("u_fogColor").SetValue(new Vector3(m_subsystemSky.ViewFogColor));
			m_shader.GetParameter("u_fogStartInvLength").SetValue(new Vector2(m_subsystemSky.ViewFogRange.X, 1f / (m_subsystemSky.ViewFogRange.Y - m_subsystemSky.ViewFogRange.X)));
			m_shader.GetParameter("u_fogYMultiplier").SetValue(m_subsystemSky.VisibilityRangeYMultiplier);
			m_shader.GetParameter("u_time").SetValue(m_timeOfDay.TimeOfDay * m_timeOfDay.DayDuration);
			Texture2D[] array = m_textures;
			int activeBlockValue = m_componentMiner.ActiveBlockValue;
			Block block2 = BlocksManager.Blocks[Terrain.ExtractContents(activeBlockValue)];
			if (block2.GetShovelPower(activeBlockValue) > 7f)
			{
				array = m_texturesShovel;
			}
			else if (block2.GetQuarryPower(activeBlockValue) > 7f)
			{
				array = m_texturesQuarry;
			}
			else if (block2.GetHackPower(activeBlockValue) > 7f)
			{
				array = m_texturesHack;
			}
			float num2 = m_componentMiner.DigCracksProgress;
			if (num2 > 0f || m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative)
			{
				if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative)
				{
					num2 = m_componentMiner.DigProgress;
				}
				int num3 = (int)MathUtils.Clamp(num2 * (float)array.Length, 0f, array.Length - 1);
				Texture2D value2 = array[num3];
				m_shader.GetParameter("u_texture").SetValue(value2);
				for (int j = 0; j < Buffers.Count; j++)
				{
					Display.DrawIndexed(PrimitiveType.TriangleList, m_shader, Buffers[j].VertexBuffer, Buffers[j].IndexBuffer, 0, Buffers[j].IndexBuffer.IndicesCount);
				}
			}
		}
		catch
		{
		}
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		m_players = Project.FindSubsystem<SubsystemPlayers>();
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_timeOfDay = Project.FindSubsystem<SubsystemTimeOfDay>(throwOnError: true);
		m_sky = Project.FindSubsystem<SubsystemSky>();
		defaultTexture = Project.FindSubsystem<SubsystemAnimatedTextures>().AnimatedBlocksTexture;
		DrawItem = new Dictionary<Texture2D, TerrainGeometry[]>();
		TerrainGeometry[] array = new TerrainGeometry[16];
		for (int i = 0; i < 16; i++)
		{
			array[i] = new TerrainGeometry(DrawItem, i);
		}
		DrawItem.Add(defaultTexture, array);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemSky = Project.FindSubsystem<SubsystemSky>(throwOnError: true);
		m_componentMiner = Entity.FindComponent<ComponentMiner>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_shader = new Shader(ShaderCodeManager.Get("Shaders/AlphaTested.vsh"), ShaderCodeManager.GetFast("Shaders/AlphaTested.psh"), new ShaderMacro("ALPHATESTED"));
		m_textures = new Texture2D[8];
		for (int j = 0; j < 8; j++)
		{
			m_textures[j] = ContentManager.Get<Texture2D>($"Textures/Cracks{j + 1}");
		}
		m_texturesShovel = new Texture2D[8];
		m_texturesQuarry = new Texture2D[8];
		m_texturesHack = new Texture2D[8];
		for (int k = 0; k < 8; k++)
		{
			m_texturesShovel[k] = ContentManager.Get<Texture2D>($"Textures/CrackShovel{k + 1}");
			m_texturesQuarry[k] = ContentManager.Get<Texture2D>($"Textures/CrackQuarry{k + 1}");
			m_texturesHack[k] = ContentManager.Get<Texture2D>($"Textures/CrackHack{k + 1}");
		}
	}

	static ComponentDiggingCracks()
	{
		m_drawOrders = new int[1] { 200 };
	}
}
