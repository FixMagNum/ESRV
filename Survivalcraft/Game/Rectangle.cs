using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Engine;

namespace Game;

public class Rectangle : Dialog
{
	private ComponentPlayer player;

	private LabelWidget m_title;

	private ButtonWidget m_okButton;

	private ButtonWidget m_cancelButton;

	private List<string> names_pos = new List<string>();

	private List<Category> m_categories_pos = new List<Category>();

	private ButtonWidget mselect_pos;

	private ButtonWidget Icon_select;

	private LabelWidget mPosition;

	private BlockIconWidget m_blockIconWidget;

	private int pos1;

	private int id1;

	private TerrainRaycastResult? Point;

	private TerrainRaycastResult? Point2;

	private SubsystemTerrain m_subsystemTerrain;

	private FurnitureDesignWidget m_previewWidget;

	private FurnitureDesign m_previewDesign;

	public Rectangle(ComponentPlayer player, int id, TerrainRaycastResult? Point1, TerrainRaycastResult? Point2, SubsystemTerrain subsystemTerrain)
	{
		m_subsystemTerrain = subsystemTerrain;
		Point = Point1;
		this.Point2 = Point2;
		id1 = id;
		this.player = player;
		LoadContents(this, ContentManager.Get<XElement>("WE/DialogsWE/PillarDialog"));
		m_title = Children.Find<LabelWidget>("Pillar_Dialog.Title");
		mPosition = Children.Find<LabelWidget>("Pillar_Dialog.Pos");
		Icon_select = Children.Find<ButtonWidget>("Pillar_Dialog.Icon_select");
		mselect_pos = Children.Find<ButtonWidget>("Pillar_Dialog.Select_pos");
		m_okButton = Children.Find<ButtonWidget>("Pillar_Dialog.OK");
		m_cancelButton = Children.Find<ButtonWidget>("Pillar_Dialog.Cancel");
		m_blockIconWidget = Children.Find<BlockIconWidget>("Pillar_Dialog.Icon");
		m_previewWidget = Children.Find<FurnitureDesignWidget>("Pillar_Dialog.Preview3D");
		m_previewDesign = new FurnitureDesign(m_subsystemTerrain);
		m_previewWidget.Design = m_previewDesign;
		m_previewWidget.Mode = FurnitureDesignWidget.ViewMode.Perspective;
		m_title.Text = "Frame or box";
		mPosition.Text = "Frame";
		m_blockIconWidget.Value = id;
		names_pos.Add("Frame");
		names_pos.Add("Hollow Box");
		names_pos.Add("Solid Box");
		foreach (string names_po in names_pos)
		{
			m_categories_pos.Add(new Category
			{
				Name = names_po
			});
		}
		RebuildPreview();
	}

	public override void Update()
	{
		m_blockIconWidget.Value = id1;
		bool flag = false;
		if (mselect_pos.IsClicked)
		{
			Select_pos(m_categories_pos, names_pos);
			flag = true;
		}
		if (Icon_select.IsClicked && player != null && player.GameWidget != null)
		{
			int[] items = new int[28]
			{
				2, 3, 67, 66, 4, 5, 26, 73, 21, 46,
				47, 15, 62, 68, 126, 71, 359, 388, 365, 231,
				60, 44, 404, 400, 401, 72, 344, 353
			};
			DialogsManager.ShowDialog(player.GameWidget, new ListSelectionDialog("Select Block", items, 72f, (object index) =>
			{
				ContainerWidget containerWidget = (ContainerWidget)Widget.LoadWidget(null, ContentManager.Get<XElement>("Widgets/SelectBlockItem"), null);
				containerWidget.Children.Find<BlockIconWidget>("SelectBlockItem.Block").Contents = (int)index;
				containerWidget.Children.Find<LabelWidget>("SelectBlockItem.Text").Text = BlocksManager.Blocks[(int)index].GetDisplayName(null, Terrain.MakeBlockValue((int)index));
				return containerWidget;
			}, (object index) =>
			{
				id1 = (int)index;
				m_blockIconWidget.Value = id1;
				RebuildPreview();
			}));
		}
		if (m_okButton.IsClicked)
		{
			if (!Point.HasValue || !Point2.HasValue || player == null)
			{
				DialogsManager.HideDialog(this);
				return;
			}
			if (mPosition.Text == "Frame")
			{
				pos1 = 2;
			}
			else if (mPosition.Text == "Hollow Box")
			{
				pos1 = 1;
			}
			else
			{
				pos1 = 0;
			}
			Point3 point = Point.Value.CellFace.Point;
			Point3 point2 = Point2.Value.CellFace.Point;
			int sizeX = Math.Abs(point.X - point2.X) + 1;
			int sizeY = Math.Abs(point.Y - point2.Y) + 1;
			int sizeZ = Math.Abs(point.Z - point2.Z) + 1;
			player.Entity.FindComponent<ComponentWorldEditIntegration>(throwOnError: true).PreviewRectangle(pos1, sizeX, sizeY, sizeZ, id1);
			DialogsManager.HideDialog(this);
		}
		if (Input.Cancel || m_cancelButton.IsClicked)
		{
			DialogsManager.HideDialog(this);
		}
		if (flag)
		{
			RebuildPreview();
		}
		UpdateControls();
	}

	private void UpdateControls()
	{
	}

	public void Select_pos(List<Category> m_categories, List<string> a)
	{
		if (player == null || player.GameWidget == null)
		{
			return;
		}
		DialogsManager.ShowDialog(player.GameWidget, new ListSelectionDialog(string.Empty, m_categories, 56f, (object c) => new LabelWidget
		{
			Text = ((Category)c).Name,
			Color = Color.White,
			HorizontalAlignment = WidgetAlignment.Center,
			VerticalAlignment = WidgetAlignment.Center
		}, (object c) =>
		{
			if (c != null)
			{
				int index = m_categories.IndexOf((Category)c);
				string text = a[index];
				if (mPosition.Text != text)
				{
					mPosition.Text = text;
					RebuildPreview();
				}
			}
		}));
	}

	private void RebuildPreview()
	{
		if (m_previewDesign == null)
		{
			return;
		}
		int num = 12;
		int[] array = new int[num * num * num];
		int num2 = Terrain.MakeBlockValue(id1);
		int mode = ((mPosition.Text == "Frame") ? 2 : ((mPosition.Text == "Hollow Box") ? 1 : 0));
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num; j++)
			{
				for (int k = 0; k < num; k++)
				{
					if (ShouldPlaceRectangleBlockPreview(mode, i, j, k, num, num, num))
					{
						int num3 = i + j * num + k * num * num;
						array[num3] = num2;
					}
				}
			}
		}
		m_previewDesign.SetValues(num, array);
		Point3 location = m_previewDesign.Box.Location;
		Point3 point = new Point3(m_previewDesign.Resolution) - (m_previewDesign.Box.Location + m_previewDesign.Box.Size);
		Point3 delta = new Point3((point.X - location.X) / 2, -location.Y, (point.Z - location.Z) / 2);
		m_previewDesign.Shift(delta);
	}

	private static bool ShouldPlaceRectangleBlockPreview(int mode, int x, int y, int z, int sizeX, int sizeY, int sizeZ)
	{
		if (mode == 0)
		{
			return true;
		}
		bool flag = x == 0;
		bool flag2 = x == sizeX - 1;
		bool flag3 = y == 0;
		bool flag4 = y == sizeY - 1;
		bool flag5 = z == 0;
		bool flag6 = z == sizeZ - 1;
		bool result = flag | flag2 | flag3 | flag4 | flag5 | flag6;
		if (mode == 1)
		{
			return result;
		}
		return ((flag | flag2) ? 1 : 0) + ((flag3 | flag4) ? 1 : 0) + ((flag5 | flag6) ? 1 : 0) >= 2;
	}
}
