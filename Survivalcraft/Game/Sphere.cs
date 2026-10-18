using System.Collections.Generic;
using System.Xml.Linq;
using Engine;

namespace Game;

public class Sphere : Dialog
{
	private ComponentPlayer player;

	private LabelWidget m_title;

	private SliderWidget m_radius;

	private ButtonWidget plusButton;

	private ButtonWidget minusButton;

	private ButtonWidget m_okButton;

	private ButtonWidget m_cancelButton;

	private List<string> names = new List<string>();

	private List<Category> m_categories = new List<Category>();

	private ButtonWidget mSelect_mode;

	private ButtonWidget Icon_select;

	private LabelWidget Mode;

	private BlockIconWidget m_blockIconWidget;

	private int radius;

	private int id1;

	private TerrainRaycastResult? Point;

	private SubsystemTerrain m_subsystemTerrain;

	private FurnitureDesignWidget m_previewWidget;

	private FurnitureDesign m_previewDesign;

	public Sphere(ComponentPlayer player, int id, TerrainRaycastResult? Point1, SubsystemTerrain subsystemTerrain)
	{
		Point = Point1;
		m_subsystemTerrain = subsystemTerrain;
		id1 = id;
		this.player = player;
		LoadContents(this, ContentManager.Get<XElement>("WE/DialogsWE/SphereDialog"));
		m_title = Children.Find<LabelWidget>("Sphere_Dialog.Title");
		Mode = Children.Find<LabelWidget>("Sphere_Dialog.Mode");
		Icon_select = Children.Find<ButtonWidget>("Sphere_Dialog.Icon_select");
		m_radius = Children.Find<SliderWidget>("Sphere_Dialog.Slider1");
		plusButton = Children.Find<ButtonWidget>("Sphere_Dialog.Button4");
		minusButton = Children.Find<ButtonWidget>("Sphere_Dialog.Button3");
		mSelect_mode = Children.Find<ButtonWidget>("Sphere_Dialog.Select_mode");
		m_okButton = Children.Find<ButtonWidget>("Sphere_Dialog.OK");
		m_cancelButton = Children.Find<ButtonWidget>("Sphere_Dialog.Cancel");
		m_blockIconWidget = Children.Find<BlockIconWidget>("Sphere_Dialog.Icon");
		m_previewWidget = Children.Find<FurnitureDesignWidget>("Sphere_Dialog.Preview3D");
		m_previewDesign = new FurnitureDesign(m_subsystemTerrain);
		m_previewWidget.Design = m_previewDesign;
		m_previewWidget.Mode = FurnitureDesignWidget.ViewMode.Perspective;
		m_title.Text = "Sphere";
		Mode.Text = "Hollow";
		m_radius.MinValue = 1f;
		m_radius.MaxValue = 100f;
		m_radius.Value = 1f;
		names.Add("Soild");
		names.Add("Hollow");
		foreach (string name in names)
		{
			m_categories.Add(new Category
			{
				Name = name
			});
		}
		radius = (int)m_radius.Value - 1;
		m_blockIconWidget.Value = id;
		RebuildPreview();
	}

	public override void Update()
	{
		int num = radius;
		int num2 = id1;
		string text = Mode.Text;
		radius = (int)m_radius.Value - 1;
		m_blockIconWidget.Value = id1;
		if (plusButton.IsClicked)
		{
			m_radius.Value = MathUtils.Min(m_radius.Value + 1f, (int)m_radius.MaxValue);
		}
		if (minusButton.IsClicked)
		{
			m_radius.Value = MathUtils.Max(m_radius.Value - 1f, (int)m_radius.MinValue);
		}
		if (mSelect_mode.IsClicked)
		{
			Select_mode(m_categories, names);
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
			player.Entity.FindComponent<ComponentWorldEditIntegration>(throwOnError: true).PreviewSphere(Mode.Text == "Hollow", radius, id1);
			DialogsManager.HideDialog(this);
		}
		if (Input.Cancel || m_cancelButton.IsClicked)
		{
			DialogsManager.HideDialog(this);
		}
		UpdateControls();
		if (num != radius || num2 != id1 || text != Mode.Text)
		{
			RebuildPreview();
		}
	}

	private void UpdateControls()
	{
		m_radius.Text = $"{radius + 1} blocks";
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

	private void RebuildPreview()
	{
		bool flag = Mode.Text == "Hollow";
		int num = radius + 1;
		int num2 = 2 * num + 1;
		int num3 = MathUtils.Max(1, (num2 + 15) / 16);
		int num4 = MathUtils.Max(2, MathUtils.Min(16, (num2 + num3 - 1) / num3));
		int[] array = new int[num4 * num4 * num4];
		int num5 = Terrain.MakeBlockValue(id1);
		int num6 = num * num;
		int num7 = (num - 1) * (num - 1);
		for (int i = -num; i <= num; i++)
		{
			for (int j = -num; j <= num; j++)
			{
				for (int k = -num; k <= num; k++)
				{
					int num8 = k * k + i * i + j * j;
					if (num8 <= num6 && (!flag || num8 >= num7))
					{
						int num9 = MathUtils.Clamp((k + num) / num3, 0, num4 - 1);
						int num10 = MathUtils.Clamp((i + num) / num3, 0, num4 - 1);
						int num11 = MathUtils.Clamp((j + num) / num3, 0, num4 - 1);
						array[num9 + num10 * num4 + num11 * num4 * num4] = num5;
					}
				}
			}
		}
		m_previewDesign.SetValues(num4, array);
		Point3 location = m_previewDesign.Box.Location;
		Point3 point = new Point3(m_previewDesign.Resolution) - (m_previewDesign.Box.Location + m_previewDesign.Box.Size);
		Point3 delta = new Point3((point.X - location.X) / 2, -location.Y, (point.Z - location.Z) / 2);
		m_previewDesign.Shift(delta);
	}
}
