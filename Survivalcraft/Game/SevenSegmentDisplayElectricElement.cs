using Engine;

namespace Game;

public class SevenSegmentDisplayElectricElement : MountedElectricElement
{
	public SubsystemGlow m_subsystemGlow;

	public float m_voltage = float.PositiveInfinity;

	public GlowPoint[] m_glowPoints = new GlowPoint[9];

	public Color m_color;

	public Vector2[] m_centers = new Vector2[7]
	{
		new Vector2(0f, 6f),
		new Vector2(-4f, 3f),
		new Vector2(-4f, -3f),
		new Vector2(0f, -6f),
		new Vector2(4f, -3f),
		new Vector2(4f, 3f),
		new Vector2(0f, 0f)
	};

	public Vector2[] m_sizes = new Vector2[7]
	{
		new Vector2(3.2f, 1f),
		new Vector2(1f, 2.3f),
		new Vector2(1f, 2.3f),
		new Vector2(3.2f, 1f),
		new Vector2(1f, 2.3f),
		new Vector2(1f, 2.3f),
		new Vector2(3.2f, 1f)
	};

	public int[] m_patterns = new int[16]
	{
		63, 6, 91, 79, 102, 109, 125, 7, 127, 111,
		119, 124, 57, 94, 121, 113
	};

	public SevenSegmentDisplayElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace)
		: base(subsystemElectricity, cellFace)
	{
		m_subsystemGlow = subsystemElectricity.Project.FindSubsystem<SubsystemGlow>(throwOnError: true);
	}

	public override void OnAdded()
	{
		CellFace cellFace = CellFaces[0];
		int data = Terrain.ExtractData(SubsystemElectricity.SubsystemTerrain.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z));
		int mountingFace = SevenSegmentDisplayBlock.GetMountingFace(data);
		m_color = LedBlock.LedColors[SevenSegmentDisplayBlock.GetColor(data)];
		int mode = SevenSegmentDisplayBlock.GetMode(data);
		for (int i = 0; i < 9; i++)
		{
			Vector3 vector = new Vector3((float)cellFace.X + 0.5f, (float)cellFace.Y + 0.5f, (float)cellFace.Z + 0.5f);
			Vector3 vector2 = CellFace.FaceToVector3(mountingFace);
			Vector3 vector3 = ((mountingFace < 4) ? Vector3.UnitY : Vector3.UnitX);
			Vector3 vector4 = Vector3.Cross(vector2, vector3);
			m_glowPoints[i] = m_subsystemGlow.AddGlowPoint();
			Vector2 vector5;
			Vector2 vector6;
			if (i < 7)
			{
				vector5 = m_centers[i];
				vector6 = m_sizes[i];
			}
			else if (mode == 0)
			{
				vector5 = Vector2.Zero;
				vector6 = Vector2.Zero;
			}
			else
			{
				float x = ((mode == 1) ? 7f : (-7f));
				float y = ((i == 7) ? 2f : (-2f));
				vector5 = new Vector2(x, y);
				vector6 = new Vector2(1.2f, 1.2f);
			}
			m_glowPoints[i].Position = vector - 0.4375f * CellFace.FaceToVector3(mountingFace) + vector5.X * 0.0625f * vector4 + vector5.Y * 0.0625f * vector3;
			m_glowPoints[i].Forward = vector2;
			m_glowPoints[i].Right = vector4 * vector6.X * 0.0625f;
			m_glowPoints[i].Up = vector3 * vector6.Y * 0.0625f;
			m_glowPoints[i].Color = Color.Transparent;
			m_glowPoints[i].Size = ((i < 7) ? 1.35f : 1f);
			m_glowPoints[i].FarSize = ((i < 7) ? 1.35f : 1f);
			m_glowPoints[i].FarDistance = 1f;
			if (i < 7)
			{
				m_glowPoints[i].Type = ((m_sizes[i].X > m_sizes[i].Y) ? GlowPointType.HorizontalRectangle : GlowPointType.VerticalRectangle);
			}
			else
			{
				m_glowPoints[i].Type = GlowPointType.Square;
			}
		}
	}

	public override void OnRemoved()
	{
		for (int i = 0; i < 9; i++)
		{
			m_subsystemGlow.RemoveGlowPoint(m_glowPoints[i]);
		}
	}

	public override bool Simulate()
	{
		float point = m_voltage;
		m_voltage = 0f;
		foreach (ElectricConnection num2 in Connections)
		{
			if (num2.ConnectorType != ElectricConnectorType.Output && num2.NeighborConnectorType != ElectricConnectorType.Input)
			{
				m_voltage = MathUtils.Max(m_voltage, num2.NeighborElectricElement.GetOutputVoltage(num2.NeighborConnectorFace));
			}
		}
		if (m_voltage != point)
		{
			int position = (int)MathUtils.Round(m_voltage * 15f);
			for (int i = 0; i < 7; i++)
			{
				m_glowPoints[i].Color = (((m_patterns[position] & (1 << i)) != 0) ? m_color : Color.Transparent);
			}
			Color delay = ((m_voltage > 0f) ? m_color : Color.Transparent);
			m_glowPoints[7].Color = delay;
			m_glowPoints[8].Color = delay;
		}
		return false;
	}
}
