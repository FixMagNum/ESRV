using System;
using System.Xml.Linq;
using Engine;

namespace Game;

public class Mountain : Dialog
{
	private ComponentPlayer player;

	private LabelWidget m_title;

	private SliderWidget m_radius;

	private SliderWidget m_lenght;

	private ButtonWidget plusButton;

	private ButtonWidget minusButton;

	private ButtonWidget lenght_plusButton;

	private ButtonWidget lenght_minusButton;

	private ButtonWidget m_okButton;

	private ButtonWidget m_cancelButton;

	private ButtonWidget Icon_select;

	private ButtonWidget Icon_select1;

	private ButtonWidget Icon_select2;

	private BlockIconWidget m_blockIconWidget;

	private BlockIconWidget m_blockIconWidget1;

	private BlockIconWidget m_blockIconWidget2;

	private int radius;

	private int lenght;

	private int id1;

	private int id2;

	private int id3;

	private TerrainRaycastResult? Point;

	private SubsystemTerrain m_subsystemTerrain;

	private FurnitureDesignWidget m_previewWidget;

	private FurnitureDesign m_previewDesign;

	public Mountain(ComponentPlayer player, TerrainRaycastResult? Point1, SubsystemTerrain subsystemTerrain)
	{
		m_subsystemTerrain = subsystemTerrain;
		Point = Point1;
		id1 = 3;
		id2 = 2;
		id3 = 8;
		this.player = player;
		LoadContents(this, ContentManager.Get<XElement>("WE/DialogsWE/MountainDialog"));
		m_title = Children.Find<LabelWidget>("Mountain_Dialog.Title");
		Icon_select = Children.Find<ButtonWidget>("Mountain_Dialog.Icon_select");
		Icon_select1 = Children.Find<ButtonWidget>("Mountain_Dialog.Icon_select1");
		Icon_select2 = Children.Find<ButtonWidget>("Mountain_Dialog.Icon_select2");
		m_radius = Children.Find<SliderWidget>("Mountain_Dialog.Slider1");
		m_lenght = Children.Find<SliderWidget>("Mountain_Dialog.Slider2");
		plusButton = Children.Find<ButtonWidget>("Mountain_Dialog.Button4");
		minusButton = Children.Find<ButtonWidget>("Mountain_Dialog.Button3");
		lenght_plusButton = Children.Find<ButtonWidget>("Mountain_Dialog.Button2");
		lenght_minusButton = Children.Find<ButtonWidget>("Mountain_Dialog.Button1");
		m_okButton = Children.Find<ButtonWidget>("Mountain_Dialog.OK");
		m_cancelButton = Children.Find<ButtonWidget>("Mountain_Dialog.Cancel");
		m_blockIconWidget = Children.Find<BlockIconWidget>("Mountain_Dialog.Icon");
		m_blockIconWidget1 = Children.Find<BlockIconWidget>("Mountain_Dialog.Icon1");
		m_blockIconWidget2 = Children.Find<BlockIconWidget>("Mountain_Dialog.Icon2");
		m_previewWidget = Children.Find<FurnitureDesignWidget>("Mountain_Dialog.Preview3D");
		m_previewDesign = new FurnitureDesign(m_subsystemTerrain);
		m_previewWidget.Design = m_previewDesign;
		m_previewWidget.Mode = FurnitureDesignWidget.ViewMode.Perspective;
		m_title.Text = "Mountain";
		m_radius.MinValue = 1f;
		m_radius.MaxValue = 100f;
		m_radius.Value = 1f;
		m_lenght.MinValue = 1f;
		m_lenght.MaxValue = 100f;
		m_lenght.Value = 1f;
		radius = (int)m_radius.Value - 1;
		lenght = (int)m_lenght.Value - 1;
		RebuildPreview();
	}

	public override void Update()
	{
		int num = radius;
		int num2 = lenght;
		int num3 = id1;
		int num4 = id2;
		int num5 = id3;
		radius = (int)m_radius.Value - 1;
		lenght = (int)m_lenght.Value - 1;
		m_blockIconWidget.Value = id1;
		m_blockIconWidget1.Value = id2;
		m_blockIconWidget2.Value = id3;
		if (plusButton.IsClicked)
		{
			m_radius.Value = MathUtils.Min(m_radius.Value + 1f, (int)m_radius.MaxValue);
		}
		if (minusButton.IsClicked)
		{
			m_radius.Value = MathUtils.Max(m_radius.Value - 1f, (int)m_radius.MinValue);
		}
		if (lenght_plusButton.IsClicked)
		{
			m_lenght.Value = MathUtils.Min(m_lenght.Value + 1f, (int)m_lenght.MaxValue);
		}
		if (lenght_minusButton.IsClicked)
		{
			m_lenght.Value = MathUtils.Max(m_lenght.Value - 1f, (int)m_lenght.MinValue);
		}
		if (Icon_select.IsClicked && player != null && player.GameWidget != null)
		{
			DialogsManager.ShowDialog(player.GameWidget, new ListSelectionDialog("Select Block", new int[29]
			{
				2, 3, 67, 66, 4, 5, 26, 73, 21, 46,
				47, 15, 62, 68, 126, 71, 359, 388, 365, 231,
				60, 44, 359, 404, 400, 401, 72, 344, 353
			}, 72f, (object index) =>
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
		if (Icon_select1.IsClicked && player != null && player.GameWidget != null)
		{
			DialogsManager.ShowDialog(player.GameWidget, new ListSelectionDialog("Select Block", new int[29]
			{
				2, 3, 67, 66, 4, 5, 26, 73, 21, 46,
				47, 15, 62, 68, 126, 71, 359, 388, 365, 231,
				60, 44, 359, 404, 400, 401, 72, 344, 353
			}, 72f, (object index) =>
			{
				ContainerWidget containerWidget = (ContainerWidget)Widget.LoadWidget(null, ContentManager.Get<XElement>("Widgets/SelectBlockItem"), null);
				containerWidget.Children.Find<BlockIconWidget>("SelectBlockItem.Block").Contents = (int)index;
				containerWidget.Children.Find<LabelWidget>("SelectBlockItem.Text").Text = BlocksManager.Blocks[(int)index].GetDisplayName(null, Terrain.MakeBlockValue((int)index));
				return containerWidget;
			}, (object index) =>
			{
				id2 = (int)index;
				m_blockIconWidget1.Value = id2;
				RebuildPreview();
			}));
		}
		if (Icon_select2.IsClicked && player != null && player.GameWidget != null)
		{
			DialogsManager.ShowDialog(player.GameWidget, new ListSelectionDialog("Select Block", new int[29]
			{
				2, 3, 67, 66, 4, 5, 26, 73, 21, 46,
				47, 15, 62, 68, 126, 71, 359, 388, 365, 231,
				60, 44, 359, 404, 400, 401, 72, 344, 353
			}, 72f, (object index) =>
			{
				ContainerWidget containerWidget = (ContainerWidget)Widget.LoadWidget(null, ContentManager.Get<XElement>("Widgets/SelectBlockItem"), null);
				containerWidget.Children.Find<BlockIconWidget>("SelectBlockItem.Block").Contents = (int)index;
				containerWidget.Children.Find<LabelWidget>("SelectBlockItem.Text").Text = BlocksManager.Blocks[(int)index].GetDisplayName(null, Terrain.MakeBlockValue((int)index));
				return containerWidget;
			}, (object index) =>
			{
				id3 = (int)index;
				m_blockIconWidget2.Value = id3;
				RebuildPreview();
			}));
		}
		if (m_okButton.IsClicked)
		{
			player.Entity.FindComponent<ComponentWorldEditIntegration>(throwOnError: true).PreviewMountain(radius, lenght, id1, id2, id3);
			DialogsManager.HideDialog(this);
		}
		if (Input.Cancel || m_cancelButton.IsClicked)
		{
			DialogsManager.HideDialog(this);
		}
		UpdateControls();
		if (num != radius || num2 != lenght || num3 != id1 || num4 != id2 || num5 != id3)
		{
			RebuildPreview();
		}
	}

	private void UpdateControls()
	{
		m_radius.Text = $"{radius + 1} blocks";
		m_lenght.Text = $"{lenght + 1} blocks";
	}

	private void RebuildPreview()
	{
		int num = radius + 1;
		int num2 = lenght + 1;
		double customCollisionBoxes = Math.PI / 2.0;
		float i = (float)num2 + 0.8f;
		double boundingBox = 22.5;
		int num3 = 2 * num;
		int[,] array = new int[num3, num3];
		int num4 = 0;
		for (int j = -num; j < num; j++)
		{
			for (int k = -num; k < num; k++)
			{
				double num5 = Math.Cos(customCollisionBoxes * (double)j / (double)num) * Math.Cos(customCollisionBoxes * (double)k / (double)num) * (double)i;
				double num6 = Math.Sin(customCollisionBoxes * (double)j * 1.4 / (double)num + 4.0) * Math.Cos(customCollisionBoxes * (double)k * 1.4 / (double)num + 7.0) * (double)i * 0.25;
				double num7 = Math.Cos(customCollisionBoxes * (double)j * 2.8 / (double)num + 4.0 * boundingBox) * Math.Sin(customCollisionBoxes * (double)k * 2.8 / (double)num + 8.0 * boundingBox) * (double)i * 0.2;
				double num8 = Math.Sin(customCollisionBoxes * (double)j * 5.6 / (double)num + 4.0 * boundingBox * 1.5) * Math.Sin(customCollisionBoxes * (double)k * 5.6 / (double)num + 8.0 * boundingBox * 1.5) * (double)i * 0.15;
				double num9 = num5 - num6 + num7 - num8;
				int num10 = (array[j + num, k + num] = ((num9 > 0.0) ? ((int)num9) : 0));
				if (num10 > num4)
				{
					num4 = num10;
				}
			}
		}
		num4 = MathUtils.Max(1, num4);
		int num11 = MathUtils.Max(num3, MathUtils.Max(num4, num3));
		int num12 = MathUtils.Max(1, (num11 + 15) / 16);
		int num13 = MathUtils.Max(2, MathUtils.Min(16, (num11 + num12 - 1) / num12));
		int[] array2 = new int[num13 * num13 * num13];
		int num14 = Terrain.MakeBlockValue(id1);
		int num15 = Terrain.MakeBlockValue(id2);
		int num16 = Terrain.MakeBlockValue(id3);
		for (int l = 0; l < num3; l++)
		{
			for (int m = 0; m < num3; m++)
			{
				int num17 = array[l, m];
				if (num17 <= 0)
				{
					continue;
				}
				for (int n = 0; n <= num17; n++)
				{
					int num18 = num16;
					if (n == 0)
					{
						num18 = num14;
					}
					else if (n <= 3)
					{
						num18 = num15;
					}
					int num19 = MathUtils.Clamp(l / num12, 0, num13 - 1);
					int num20 = MathUtils.Clamp((num17 - n) / num12, 0, num13 - 1);
					int num21 = MathUtils.Clamp(m / num12, 0, num13 - 1);
					array2[num19 + num20 * num13 + num21 * num13 * num13] = num18;
				}
			}
		}
		m_previewDesign.SetValues(num13, array2);
		Point3 location = m_previewDesign.Box.Location;
		Point3 point = new Point3(m_previewDesign.Resolution) - (m_previewDesign.Box.Location + m_previewDesign.Box.Size);
		Point3 delta = new Point3((point.X - location.X) / 2, -location.Y, (point.Z - location.Z) / 2);
		m_previewDesign.Shift(delta);
	}
}
