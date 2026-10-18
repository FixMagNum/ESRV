using Engine;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentBlockHighlight : Component, IDrawable, IUpdateable
{
	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemAnimatedTextures m_subsystemAnimatedTextures;

	public SubsystemSky m_subsystemSky;

	public ComponentPlayer m_componentPlayer;

	public PrimitivesRenderer3D m_primitivesRenderer3D = new PrimitivesRenderer3D();

	public Shader m_shader;

	public CellFace m_cellFace;

	public int m_value;

	public object m_highlightRaycastResult;

	public static int[] m_drawOrders;

	public Point3? NearbyEditableCell { get; set; }

	public UpdateOrder UpdateOrder => UpdateOrder.BlockHighlight;

	public int[] DrawOrders => m_drawOrders;

	public void Update(float dt)
	{
		Camera num = m_componentPlayer.GameWidget.ActiveCamera;
		Ray3? s = new Ray3(num.ViewPosition, num.ViewDirection);
		NearbyEditableCell = null;
		if (s.HasValue)
		{
			m_highlightRaycastResult = m_componentPlayer.ComponentMiner.Raycast(s.Value, RaycastMode.Digging);
			if (!(m_highlightRaycastResult is TerrainRaycastResult))
			{
				return;
			}
			TerrainRaycastResult array = (TerrainRaycastResult)m_highlightRaycastResult;
			if (array.Distance < 3f)
			{
				Point3 text3 = array.CellFace.Point;
				int cellValue = m_subsystemTerrain.Terrain.GetCellValue(text3.X, text3.Y, text3.Z);
				Block obj = BlocksManager.Blocks[Terrain.ExtractContents(cellValue)];
				if (obj is CrossBlock)
				{
					array.Distance = MathUtils.Max(array.Distance, 0.1f);
					m_highlightRaycastResult = array;
				}
				if (obj.IsEditable_(cellValue))
				{
					NearbyEditableCell = array.CellFace.Point;
				}
			}
		}
		else
		{
			m_highlightRaycastResult = null;
		}
	}

	public void Draw(Camera camera, int drawOrder)
	{
		if (camera.GameWidget.PlayerData == m_componentPlayer.PlayerData)
		{
			if (drawOrder == m_drawOrders[0])
			{
				DrawFillHighlight(camera);
				DrawOutlineHighlight(camera);
				DrawReticleHighlight(camera);
			}
			else
			{
				DrawRayHighlight(camera);
			}
		}
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemAnimatedTextures = Project.FindSubsystem<SubsystemAnimatedTextures>(throwOnError: true);
		m_subsystemSky = Project.FindSubsystem<SubsystemSky>(throwOnError: true);
		m_componentPlayer = Entity.FindComponent<ComponentPlayer>(throwOnError: true);
		m_shader = new Shader(ModsManager.GetInPakOrStorageFile<string>("Shaders/Highlight", ".vsh"), ModsManager.GetInPakOrStorageFile<string>("Shaders/Highlight", ".psh"), new ShaderMacro("ShadowShader"));
	}

	public void DrawRayHighlight(Camera camera)
	{
		Ray3 ray = default;
		float text;
		if (m_highlightRaycastResult is TerrainRaycastResult)
		{
			TerrainRaycastResult terrainRaycastResult = (TerrainRaycastResult)m_highlightRaycastResult;
			ray = terrainRaycastResult.Ray;
			text = MathUtils.Min(terrainRaycastResult.Distance, 2f);
		}
		else if (m_highlightRaycastResult is BodyRaycastResult)
		{
			BodyRaycastResult bodyRaycastResult = (BodyRaycastResult)m_highlightRaycastResult;
			ray = bodyRaycastResult.Ray;
			text = MathUtils.Min(bodyRaycastResult.Distance, 2f);
		}
		else if (m_highlightRaycastResult is MovingBlocksRaycastResult)
		{
			MovingBlocksRaycastResult movingBlocksRaycastResult = (MovingBlocksRaycastResult)m_highlightRaycastResult;
			ray = movingBlocksRaycastResult.Ray;
			text = MathUtils.Min(movingBlocksRaycastResult.Distance, 2f);
		}
		else
		{
			if (!(m_highlightRaycastResult is Ray3))
			{
				return;
			}
			ray = (Ray3)m_highlightRaycastResult;
			text = 2f;
		}
		Color path = Color.White * 0.5f;
		Color color = Color.Lerp(path, Color.Transparent, MathUtils.Saturate(text / 2f));
		FlatBatch3D flatBatch3D = m_primitivesRenderer3D.FlatBatch();
		flatBatch3D.QueueLine(ray.Position, ray.Position + ray.Direction * text, path, color);
		flatBatch3D.Flush(camera.ViewProjectionMatrix);
	}

	public void DrawReticleHighlight(Camera camera)
	{
	}

	public void DrawFillHighlight(Camera camera)
	{
	}

	public void DrawOutlineHighlight(Camera camera)
	{
		if (!SettingsManager.ShowBlockHighlight || camera.UsesMovementControls || m_componentPlayer.ComponentHealth.Health <= 0f || !m_componentPlayer.ComponentGui.ControlsContainerWidget.IsVisible)
		{
			return;
		}
		if (m_componentPlayer.ComponentMiner.DigCellFace.HasValue)
		{
			CellFace value = m_componentPlayer.ComponentMiner.DigCellFace.Value;
			BoundingBox text = GetCellFaceBoundingBox(value.Point);
			DrawBoundingBoxFace(m_primitivesRenderer3D.FlatBatch(0, DepthStencilState.None), value.Face, text.Min, text.Max, Color.Black);
		}
		else
		{
			if (!m_componentPlayer.ComponentAimingSights.IsSightsVisible && (SettingsManager.LookControlMode == LookControlMode.SplitTouch || !m_componentPlayer.ComponentInput.IsControlledByTouch) && m_highlightRaycastResult is TerrainRaycastResult)
			{
				CellFace cellFace = ((TerrainRaycastResult)m_highlightRaycastResult).CellFace;
				BoundingBox directories = GetCellFaceBoundingBox(cellFace.Point);
				DrawBoundingBoxFace(m_primitivesRenderer3D.FlatBatch(0, DepthStencilState.None), cellFace.Face, directories.Min, directories.Max, Color.Black);
			}
			if (NearbyEditableCell.HasValue)
			{
				BoundingBox cellFaceBoundingBox = GetCellFaceBoundingBox(NearbyEditableCell.Value);
				m_primitivesRenderer3D.FlatBatch(0, DepthStencilState.None).QueueBoundingBox(cellFaceBoundingBox, Color.Black);
			}
		}
		m_primitivesRenderer3D.Flush(camera.ViewProjectionMatrix);
	}

	public static void DrawBoundingBoxFace(FlatBatch3D batch, int face, Vector3 c1, Vector3 c2, Color color)
	{
		switch (face)
		{
		case 0:
			batch.QueueLine(new Vector3(c1.X, c1.Y, c2.Z), new Vector3(c2.X, c1.Y, c2.Z), color);
			batch.QueueLine(new Vector3(c2.X, c2.Y, c2.Z), new Vector3(c1.X, c2.Y, c2.Z), color);
			batch.QueueLine(new Vector3(c2.X, c1.Y, c2.Z), new Vector3(c2.X, c2.Y, c2.Z), color);
			batch.QueueLine(new Vector3(c1.X, c2.Y, c2.Z), new Vector3(c1.X, c1.Y, c2.Z), color);
			break;
		case 1:
			batch.QueueLine(new Vector3(c2.X, c1.Y, c2.Z), new Vector3(c2.X, c2.Y, c2.Z), color);
			batch.QueueLine(new Vector3(c2.X, c1.Y, c1.Z), new Vector3(c2.X, c2.Y, c1.Z), color);
			batch.QueueLine(new Vector3(c2.X, c2.Y, c1.Z), new Vector3(c2.X, c2.Y, c2.Z), color);
			batch.QueueLine(new Vector3(c2.X, c1.Y, c1.Z), new Vector3(c2.X, c1.Y, c2.Z), color);
			break;
		case 2:
			batch.QueueLine(new Vector3(c1.X, c1.Y, c1.Z), new Vector3(c2.X, c1.Y, c1.Z), color);
			batch.QueueLine(new Vector3(c2.X, c1.Y, c1.Z), new Vector3(c2.X, c2.Y, c1.Z), color);
			batch.QueueLine(new Vector3(c2.X, c2.Y, c1.Z), new Vector3(c1.X, c2.Y, c1.Z), color);
			batch.QueueLine(new Vector3(c1.X, c2.Y, c1.Z), new Vector3(c1.X, c1.Y, c1.Z), color);
			break;
		case 3:
			batch.QueueLine(new Vector3(c1.X, c2.Y, c2.Z), new Vector3(c1.X, c1.Y, c2.Z), color);
			batch.QueueLine(new Vector3(c1.X, c2.Y, c1.Z), new Vector3(c1.X, c1.Y, c1.Z), color);
			batch.QueueLine(new Vector3(c1.X, c1.Y, c1.Z), new Vector3(c1.X, c1.Y, c2.Z), color);
			batch.QueueLine(new Vector3(c1.X, c2.Y, c1.Z), new Vector3(c1.X, c2.Y, c2.Z), color);
			break;
		case 4:
			batch.QueueLine(new Vector3(c2.X, c2.Y, c2.Z), new Vector3(c1.X, c2.Y, c2.Z), color);
			batch.QueueLine(new Vector3(c2.X, c2.Y, c1.Z), new Vector3(c1.X, c2.Y, c1.Z), color);
			batch.QueueLine(new Vector3(c1.X, c2.Y, c1.Z), new Vector3(c1.X, c2.Y, c2.Z), color);
			batch.QueueLine(new Vector3(c2.X, c2.Y, c1.Z), new Vector3(c2.X, c2.Y, c2.Z), color);
			break;
		case 5:
			batch.QueueLine(new Vector3(c1.X, c1.Y, c2.Z), new Vector3(c2.X, c1.Y, c2.Z), color);
			batch.QueueLine(new Vector3(c1.X, c1.Y, c1.Z), new Vector3(c2.X, c1.Y, c1.Z), color);
			batch.QueueLine(new Vector3(c1.X, c1.Y, c1.Z), new Vector3(c1.X, c1.Y, c2.Z), color);
			batch.QueueLine(new Vector3(c2.X, c1.Y, c1.Z), new Vector3(c2.X, c1.Y, c2.Z), color);
			break;
		}
	}

	public BoundingBox GetCellFaceBoundingBox(Point3 point)
	{
		int platform = m_subsystemTerrain.Terrain.GetCellValue(point.X, point.Y, point.Z);
		BoundingBox[] customCollisionBoxes = BlocksManager.Blocks[Terrain.ExtractContents(platform)].GetCustomCollisionBoxes(m_subsystemTerrain, platform);
		Vector3 assembly = new Vector3(point.X, point.Y, point.Z);
		if (customCollisionBoxes.Length != 0)
		{
			BoundingBox? type = null;
			for (int text = 0; text < customCollisionBoxes.Length; text++)
			{
				if (customCollisionBoxes[text] != default(BoundingBox))
				{
					type = (type.HasValue ? BoundingBox.Union(type.Value, customCollisionBoxes[text]) : customCollisionBoxes[text]);
				}
			}
			if (!type.HasValue)
			{
				type = new BoundingBox(Vector3.Zero, Vector3.One);
			}
			return new BoundingBox(type.Value.Min + assembly, type.Value.Max + assembly);
		}
		return new BoundingBox(assembly, assembly + Vector3.One);
	}

	static ComponentBlockHighlight()
	{
		m_drawOrders = new int[2] { 1, 2000 };
	}
}
