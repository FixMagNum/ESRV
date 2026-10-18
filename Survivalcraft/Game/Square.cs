using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Engine;

namespace Game;

public class Square : Dialog
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

	private List<string> names;

	private List<Category> m_categories;

	private List<string> names_pos;

	private List<Category> m_categories_pos;

	private ButtonWidget mSelect_mode;

	private ButtonWidget mselect_pos;

	private ButtonWidget Icon_select;

	private LabelWidget mPosition;

	private LabelWidget Mode;

	private BlockIconWidget m_blockIconWidget;

	private int radius;

	private int lenght;

	private int id1;

	private TerrainRaycastResult? Point;

	private SubsystemTerrain m_subsystemTerrain;

	private FurnitureDesignWidget m_previewWidget;

	private FurnitureDesign m_previewDesign;

	public Square(ComponentPlayer player, int id, TerrainRaycastResult? Point1, SubsystemTerrain subsystemTerrain)
	{
		m_subsystemTerrain = subsystemTerrain;
		Point = Point1;
		id1 = id;
		this.player = player;
		names = new List<string>();
		m_categories = new List<Category>();
		names_pos = new List<string>();
		m_categories_pos = new List<Category>();
		LoadContents(this, ContentManager.Get<XElement>("WE/DialogsWE/SquareDialog"));
		m_title = Children.Find<LabelWidget>("Square_Dialog.Title");
		Mode = Children.Find<LabelWidget>("Square_Dialog.Mode");
		mPosition = Children.Find<LabelWidget>("Square_Dialog.Pos");
		Icon_select = Children.Find<ButtonWidget>("Square_Dialog.Icon_select");
		mselect_pos = Children.Find<ButtonWidget>("Square_Dialog.Select_pos");
		m_radius = Children.Find<SliderWidget>("Square_Dialog.Slider1");
		m_lenght = Children.Find<SliderWidget>("Square_Dialog.Slider2");
		plusButton = Children.Find<ButtonWidget>("Square_Dialog.Button4");
		minusButton = Children.Find<ButtonWidget>("Square_Dialog.Button3");
		lenght_plusButton = Children.Find<ButtonWidget>("Square_Dialog.Button2");
		lenght_minusButton = Children.Find<ButtonWidget>("Square_Dialog.Button1");
		mSelect_mode = Children.Find<ButtonWidget>("Square_Dialog.Select_mode");
		m_okButton = Children.Find<ButtonWidget>("Square_Dialog.OK");
		m_cancelButton = Children.Find<ButtonWidget>("Square_Dialog.Cancel");
		m_blockIconWidget = Children.Find<BlockIconWidget>("Square_Dialog.Icon");
		m_previewWidget = Children.Find<FurnitureDesignWidget>("Square_Dialog.Preview3D");
		m_previewDesign = new FurnitureDesign(m_subsystemTerrain);
		m_previewWidget.Design = m_previewDesign;
		m_previewWidget.Mode = FurnitureDesignWidget.ViewMode.Perspective;
		m_title.Text = "Square";
		mPosition.Text = "Flat";
		Mode.Text = "Hollow";
		m_radius.MinValue = 1f;
		m_radius.MaxValue = 100f;
		m_radius.Value = 1f;
		m_lenght.MinValue = 1f;
		m_lenght.MaxValue = 100f;
		m_lenght.Value = 1f;
		m_blockIconWidget.Value = id;
		names.Add("Soild");
		names.Add("Hollow");
		names_pos.Add("Flat");
		names_pos.Add("Pos_X");
		names_pos.Add("Pos_Y");
		foreach (string name in names)
		{
			m_categories.Add(new Category
			{
				Name = name
			});
		}
		foreach (string names_po in names_pos)
		{
			m_categories_pos.Add(new Category
			{
				Name = names_po
			});
		}
		radius = (int)m_radius.Value - 1;
		lenght = (int)m_lenght.Value - 1;
		RebuildPreview();
	}

	public override void Update()
	{
		int num = radius;
		int num2 = lenght;
		int num3 = id1;
		string text = Mode.Text;
		string text2 = mPosition.Text;
		radius = (int)m_radius.Value - 1;
		lenght = (int)m_lenght.Value - 1;
		m_blockIconWidget.Value = id1;
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
		if (mSelect_mode.IsClicked)
		{
			Select_mode(m_categories, names);
		}
		if (mselect_pos.IsClicked)
		{
			Select_pos(m_categories_pos, names_pos);
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
		if (m_okButton.IsClicked)
		{
			bool shell = Mode.Text == "Hollow";
			int pos = 1;
			if (mPosition.Text == "Flat")
			{
				pos = 1;
			}
			else if (mPosition.Text == "Pos_X")
			{
				pos = 0;
			}
			else if (mPosition.Text == "Pos_Y")
			{
				pos = 2;
			}
			player.Entity.FindComponent<ComponentWorldEditIntegration>(throwOnError: true).PreviewSquare(shell, radius + 1, lenght + 1, pos, id1);
			DialogsManager.HideDialog(this);
		}
		if (Input.Cancel || m_cancelButton.IsClicked)
		{
			DialogsManager.HideDialog(this);
		}
		UpdateControls();
		if (num != radius || num2 != lenght || num3 != id1 || text != Mode.Text || text2 != mPosition.Text)
		{
			RebuildPreview();
		}
	}

	private void UpdateControls()
	{
		m_radius.Text = $"{radius + 1} blocks";
		m_lenght.Text = $"{lenght + 1} blocks";
	}

	public void Select_mode(List<Category> m_categories, List<string> a)
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
				if (Mode.Text != text)
				{
					Mode.Text = text;
					RebuildPreview();
				}
			}
		}));
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
		bool flag = Mode.Text == "Hollow";
		int num = radius + 1;
		int num2 = lenght + 1;
		int num3 = 1;
		if (mPosition.Text == "Pos_X")
		{
			num3 = 0;
		}
		else if (mPosition.Text == "Pos_Y")
		{
			num3 = 2;
		}
		int x = ((num3 == 0) ? num2 : (2 * num + 1));
		int x2 = ((num3 == 1) ? num2 : (2 * num + 1));
		int x3 = ((num3 == 2) ? num2 : (2 * num + 1));
		int num4 = MathUtils.Max(x, MathUtils.Max(x2, x3));
		int num5 = MathUtils.Max(1, (num4 + 15) / 16);
		int num6 = MathUtils.Max(2, MathUtils.Min(16, (num4 + num5 - 1) / num5));
		int[] array = new int[num6 * num6 * num6];
		int num7 = Terrain.MakeBlockValue(id1);
		for (int i = 0; i < num2; i++)
		{
			for (int j = -num; j <= num; j++)
			{
				for (int k = -num; k <= num; k++)
				{
					int num8 = Math.Abs(j) + Math.Abs(k);
					if (num8 <= num && (!flag || num8 == num))
					{
						int num9;
						int num10;
						int num11;
						switch (num3)
						{
						case 0:
							num9 = i;
							num10 = j + num;
							num11 = k + num;
							break;
						case 1:
							num9 = j + num;
							num10 = i;
							num11 = k + num;
							break;
						default:
							num9 = j + num;
							num10 = k + num;
							num11 = i;
							break;
						}
						int num12 = MathUtils.Clamp(num9 / num5, 0, num6 - 1);
						int num13 = MathUtils.Clamp(num10 / num5, 0, num6 - 1);
						int num14 = MathUtils.Clamp(num11 / num5, 0, num6 - 1);
						int num15 = num12 + num13 * num6 + num14 * num6 * num6;
						array[num15] = num7;
					}
				}
			}
		}
		m_previewDesign.SetValues(num6, array);
		Point3 location = m_previewDesign.Box.Location;
		Point3 point = new Point3(m_previewDesign.Resolution) - (m_previewDesign.Box.Location + m_previewDesign.Box.Size);
		Point3 delta = new Point3((point.X - location.X) / 2, -location.Y, (point.Z - location.Z) / 2);
		m_previewDesign.Shift(delta);
	}
}
