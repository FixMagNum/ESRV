using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Engine;

namespace Game;

public class ShipConstructionDialog : Dialog
{
	private LabelWidget m_infoLabel;

	private ButtonWidget m_createButton;

	private ButtonWidget m_cancelButton;

	private Action m_onActivate;

	private ComponentPlayer m_player;

	private SubsystemTerrain m_terrain;

	private bool m_isPositionValid;

	private ShipPreviewWidget m_preview3d;

	private ShipPreviewWidget m_preview2d;

	private ButtonWidget m_axisButton;

	private CheckboxWidget m_waterCheckbox;

	private List<Point3> m_shipBlocks;

	private int m_axisIndex;

	public override void Update()
	{
		if (m_cancelButton != null && m_cancelButton.IsClicked)
		{
			DialogsManager.HideDialog(this);
		}
		if (m_createButton != null && m_createButton.IsClicked)
		{
			if (m_onActivate != null)
			{
				m_onActivate();
			}
			DialogsManager.HideDialog(this);
		}
		if (m_axisButton != null && m_axisButton.IsClicked)
		{
			m_axisIndex = (m_axisIndex + 1) % 3;
			Update2DView();
		}
		if (m_waterCheckbox != null)
		{
			if (m_preview3d != null)
			{
				m_preview3d.ShowWater = m_waterCheckbox.IsChecked;
			}
			if (m_preview2d != null)
			{
				m_preview2d.ShowWater = m_waterCheckbox.IsChecked;
			}
		}
	}

	public ShipConstructionDialog(ComponentPlayer player, List<Point3> shipBlocks, SubsystemTerrain terrain, Action onActivate)
	{
		m_player = player;
		m_terrain = terrain;
		m_shipBlocks = shipBlocks;
		m_onActivate = onActivate;
		try
		{
			XElement node = ContentManager.Get<XElement>("Dialogs/ShipConstructionDialog");
			LoadContents(this, node);
			m_preview3d = Children.Find<ShipPreviewWidget>("Preview3D");
			m_preview2d = Children.Find<ShipPreviewWidget>("Preview2D");
			m_infoLabel = Children.Find<LabelWidget>("InfoLabel");
			m_createButton = Children.Find<ButtonWidget>("CreateButton");
			m_cancelButton = Children.Find<ButtonWidget>("CancelButton");
			m_axisButton = Children.Find<ButtonWidget>("AxisButton");
			m_waterCheckbox = Children.Find<CheckboxWidget>("WaterCheckbox");
			BevelledRectangleWidget bevelledRectangleWidget = Children.Find<BevelledRectangleWidget>("Rect2D");
			BevelledRectangleWidget bevelledRectangleWidget2 = Children.Find<BevelledRectangleWidget>("Rect3D");
			LabelWidget labelWidget = Children.Find<LabelWidget>("TitleLabel");
			if (m_waterCheckbox != null)
			{
				m_waterCheckbox.Color = new Color(255, 255, 255);
			}
			if (m_infoLabel != null)
			{
				m_infoLabel.Color = new Color(180, 180, 180);
			}
			if (m_cancelButton != null)
			{
				m_cancelButton.Color = new Color(200, 100, 100);
			}
			if (labelWidget != null)
			{
				labelWidget.Color = new Color(255, 200, 100);
			}
			if (bevelledRectangleWidget != null)
			{
				bevelledRectangleWidget.CenterColor = new Color(20, 20, 30);
			}
			if (bevelledRectangleWidget2 != null)
			{
				bevelledRectangleWidget2.CenterColor = new Color(20, 20, 30);
			}
			CalculatePhysicsAndSetup(shipBlocks);
		}
		catch (Exception ex)
		{
			Log.Error("ShipConstructionDialog Init Failed: " + ex.Message);
			DialogsManager.HideDialog(this);
		}
	}

	private void ValidateShipPosition(List<Point3> blocks)
	{
		m_isPositionValid = true;
		HashSet<Point3> shipBlocks = new HashSet<Point3>(blocks);
		bool flag = false;
		foreach (Point3 block in blocks)
		{
			if (IsTouchingLand(block, shipBlocks))
			{
				flag = true;
				break;
			}
		}
		if (flag)
		{
			m_isPositionValid = false;
			if (m_createButton != null)
			{
				m_createButton.IsEnabled = false;
			}
			if (m_infoLabel != null)
			{
				m_infoLabel.Color = Color.Red;
			}
		}
		else
		{
			if (m_createButton != null)
			{
				m_createButton.IsEnabled = true;
			}
			if (m_infoLabel != null)
			{
				m_infoLabel.Color = Color.White;
			}
		}
	}

	private bool IsTouchingLand(Point3 p, HashSet<Point3> shipBlocks)
	{
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				for (int k = -1; k <= 1; k++)
				{
					if (i == 0 && j == 0 && k == 0)
					{
						continue;
					}
					Point3 item = new Point3(p.X + i, p.Y + j, p.Z + k);
					if (!shipBlocks.Contains(item))
					{
						int num = Terrain.ExtractContents(m_terrain.Terrain.GetCellValue(item.X, item.Y, item.Z));
						Block block = BlocksManager.Blocks[num];
						if (num != 0 && !(block is FluidBlock))
						{
							return true;
						}
					}
				}
			}
		}
		return false;
	}

	private void CalculatePhysicsAndSetup(List<Point3> blocks)
	{
		if (blocks.Count == 0)
		{
			return;
		}
		double num = 0.0;
		Vector3 zero = Vector3.Zero;
		int num2 = int.MaxValue;
		int num3 = int.MaxValue;
		int num4 = int.MinValue;
		int num5 = int.MinValue;
		Dictionary<int, float> dictionary = new Dictionary<int, float>();
		int num6 = int.MaxValue;
		int num7 = int.MinValue;
		foreach (Point3 block in blocks)
		{
			int cellValue = m_terrain.Terrain.GetCellValue(block.X, block.Y, block.Z);
			int num8 = Terrain.ExtractContents(cellValue);
			float density = BlocksManager.Blocks[num8].GetDensity(cellValue);
			num += (double)density;
			zero += new Vector3(block) * density;
			num2 = Math.Min(num2, block.X);
			num3 = Math.Min(num3, block.Z);
			num4 = Math.Max(num4, block.X);
			num5 = Math.Max(num5, block.Z);
			if (!dictionary.ContainsKey(block.Y))
			{
				dictionary[block.Y] = 0f;
			}
			dictionary[block.Y]++;
			if (block.Y < num6)
			{
				num6 = block.Y;
			}
			if (block.Y > num7)
			{
				num7 = block.Y;
			}
		}
		Vector3 comWorld = ((num > 0.0) ? (zero / (float)num) : new Vector3(blocks[0]));
		Vector3 geomCenterWorld = new Vector3((float)(num2 + num4) / 2f, (float)(num6 + num7) / 2f, (float)(num3 + num5) / 2f);
		float num9 = (float)num;
		float num10 = 0f;
		float waterLevelY = num6;
		for (int i = num6; i <= num7; i++)
		{
			if (dictionary.ContainsKey(i))
			{
				float num11 = dictionary[i];
				if (num10 + num11 >= num9)
				{
					float num12 = (num9 - num10) / num11;
					waterLevelY = (float)i + num12;
					num10 = num9;
					break;
				}
				num10 += num11;
				waterLevelY = i + 1;
			}
		}
		if (num10 < num9)
		{
			waterLevelY = num7 + 1;
		}
		int num13 = num4 - num2 + 1;
		int num14 = num7 - num6 + 1;
		int num15 = num5 - num3 + 1;
		float num16 = (float)num / (float)blocks.Count;
		float num17 = 1f;
		float num18 = Math.Abs(comWorld.X - geomCenterWorld.X);
		float num19 = Math.Abs(comWorld.Z - geomCenterWorld.Z);
		float num20 = Math.Max(num13, num15);
		float num21 = (num18 + num19) / num20;
		float num22 = num16 / num17;
		Color coMColor = Color.Green;
		if (num22 > 0.95f)
		{
			coMColor = Color.Red;
		}
		else if (num22 > 0.8f || num21 > 0.15f)
		{
			coMColor = Color.Yellow;
			if (num21 > 0.3f)
			{
				coMColor = Color.Red;
			}
		}
		if (m_preview3d != null)
		{
			m_preview3d.Setup(blocks, m_terrain, comWorld, geomCenterWorld, waterLevelY);
			m_preview3d.ShowCoM = true;
			m_preview3d.CoMColor = coMColor;
			m_preview3d.Mode = ShipPreviewWidget.ViewMode.Perspective;
		}
		if (m_preview2d != null)
		{
			m_preview2d.Setup(blocks, m_terrain, comWorld, geomCenterWorld, waterLevelY);
			m_preview2d.ShowCoM = true;
			m_preview2d.CoMColor = coMColor;
		}
		Update2DView();
		if (m_infoLabel != null)
		{
			string text = $"{num16:F2} / {num17:F1}";
			m_infoLabel.Text = $"Mass: {num:F1}\nDensity: {text}\nBlocks: {blocks.Count}\nSize: {num13}x{num14}x{num15}";
		}
		ValidateShipPosition(blocks);
	}

	private void Update2DView()
	{
		if (m_preview2d != null && m_axisButton != null)
		{
			switch (m_axisIndex)
			{
			case 0:
				m_preview2d.Mode = ShipPreviewWidget.ViewMode.Front;
				m_axisButton.Text = "View: Front";
				break;
			case 1:
				m_preview2d.Mode = ShipPreviewWidget.ViewMode.Side;
				m_axisButton.Text = "View: Side";
				break;
			case 2:
				m_preview2d.Mode = ShipPreviewWidget.ViewMode.Top;
				m_axisButton.Text = "View: Top";
				break;
			}
		}
	}

	private T SafeFind<T>(string name) where T : Widget
	{
		T val = Children.Find<T>(name);
		if (val == null)
		{
			Log.Warning("ShipConstructionDialog: Widget '" + name + "' not found in XML!");
		}
		return val;
	}
}
