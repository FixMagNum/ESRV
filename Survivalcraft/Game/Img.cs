using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Engine;
using Engine.Media;

namespace Game;

public class Img : Dialog
{
	private ComponentPlayer player;

	private ButtonWidget m_okButton;

	private ButtonWidget m_cancelButton;

	private ButtonWidget m_type_creatingButton;

	private LabelWidget m_type_creatingLabel;

	private SliderWidget m_resizeSlider;

	private SliderWidget m_furniture_resolutionSlider;

	private SliderWidget m_deep_colorSlider;

	private SliderWidget m_color_ofsetSlider;

	private ButtonWidget m_posButton;

	private ButtonWidget m_rotButton;

	private LabelWidget m_posLabel;

	private LabelWidget m_rotLabel;

	private CheckboxWidget m_colors_useBox;

	private CheckboxWidget m_color_saveBox;

	private CheckboxWidget m_mirrorBox;

	private Point3 Point;

	private SubsystemTerrain m_subsystemTerrain;

	private string pos_txt;

	private string rot_txt;

	private string t_c_txt;

	private string path_img;

	private int resize;

	private int furnit_resol;

	private int deep_color;

	private int ofst_color;

	private bool type_cr;

	private bool pos;

	private bool rot;

	private ComponentWorldEditIntegration _we;

	private ButtonWidget m_buildButton;

	private SliderWidget m_scaleSlider;

	private FurnitureDesignWidget m_previewWidget;

	private FurnitureDesign m_previewDesign;

	private int _lastPercent;

	private string _lastParamsKey;

	private ButtonWidget m_resizePlus;

	private ButtonWidget m_resizeMinus;

	private ButtonWidget m_furnResPlus;

	private ButtonWidget m_furnResMinus;

	private SliderWidget m_rot3Slider;

	private LabelWidget m_furnitureCountLabel;

	private LabelWidget m_blocksCountLabel;

	private int _rot90;

	private bool _previewFrozen;

	private bool _forcePreview16;

	private int _previewScaleStep;

	private string _lastFrozenPreviewKey;

	private LabelWidget m_furnResTitleLabel;

	private ImageWidget m_image2DWidget;

	private int _srcW = 1;

	private int _srcH = 1;

	private string _lastFilterKey;

	private int _freeSlotsCached = -1;

	private int _previewSampleStep;

	public override void Update()
	{
		if (_we == null || player == null || string.IsNullOrEmpty(path_img))
		{
			if (Input.Cancel || (m_cancelButton != null && m_cancelButton.IsClicked))
			{
				DialogsManager.HideDialog(this);
			}
			return;
		}
		if (m_type_creatingButton != null && m_type_creatingButton.IsClicked)
		{
			Select_type_creating();
		}
		if (m_posButton != null && m_posButton.IsClicked)
		{
			Select_pos();
		}
		if (m_rotButton != null && m_rotButton.IsClicked)
		{
			Select_rot();
		}
		type_cr = t_c_txt == "Furniture";
		pos = pos_txt == "Vertical";
		rot = rot_txt == "Front";
		if (m_resizeSlider != null)
		{
			resize = (int)m_resizeSlider.Value;
		}
		if (m_furniture_resolutionSlider != null)
		{
			furnit_resol = (int)m_furniture_resolutionSlider.Value;
		}
		if (resize < 1)
		{
			resize = 1;
		}
		furnit_resol = MathUtils.Clamp(furnit_resol, 2, 16);
		if (m_color_ofsetSlider != null)
		{
			ofst_color = (int)m_color_ofsetSlider.Value;
			ofst_color &= 15;
			m_color_ofsetSlider.Text = $"Palette shift: {ofst_color}";
		}
		if (m_resizePlus != null && m_resizePlus.IsClicked && m_resizeSlider != null)
		{
			m_resizeSlider.Value = MathUtils.Min(m_resizeSlider.Value + 1f, m_resizeSlider.MaxValue);
		}
		if (m_resizeMinus != null && m_resizeMinus.IsClicked && m_resizeSlider != null)
		{
			m_resizeSlider.Value = MathUtils.Max(m_resizeSlider.Value - 1f, m_resizeSlider.MinValue);
		}
		if (type_cr && m_furniture_resolutionSlider != null)
		{
			if (m_furnResPlus != null && m_furnResPlus.IsClicked)
			{
				m_furniture_resolutionSlider.Value = MathUtils.Min(m_furniture_resolutionSlider.Value + 1f, m_furniture_resolutionSlider.MaxValue);
			}
			if (m_furnResMinus != null && m_furnResMinus.IsClicked)
			{
				m_furniture_resolutionSlider.Value = MathUtils.Max(m_furniture_resolutionSlider.Value - 1f, m_furniture_resolutionSlider.MinValue);
			}
		}
		if (m_rot3Slider != null)
		{
			float num = MathUtils.Clamp(MathUtils.Round(m_rot3Slider.Value), 0f, 2f);
			if (m_rot3Slider.Value != num)
			{
				m_rot3Slider.Value = num;
			}
			int num2 = (int)num - 1;
			if (num2 != _rot90)
			{
				_rot90 = num2;
				UpdatePreview2D();
			}
			SliderWidget rot3Slider = m_rot3Slider;
			string text;
			if (_rot90 < 0)
			{
				text = "−90°";
			}
			else
			{
				text = ((_rot90 > 0) ? "+90°" : "0°");
			}
			rot3Slider.Text = text;
		}
		if (_srcW == 1 && _srcH == 1)
		{
			try
			{
				Image image = Image.Load(path_img);
				_srcW = Math.Max(1, image.Width);
				_srcH = Math.Max(1, image.Height);
			}
			catch
			{
				_srcW = 1;
				_srcH = 1;
			}
		}
		int srcW = _srcW;
		int srcH = _srcH;
		int scaleStep = resize;
		int furnitureRes = furnit_resol;
		FitFurnitureToBudget(srcW, srcH, type_cr, ref scaleStep, ref furnitureRes, out var tilesX, out var tilesY);
		int val = Math.Max(1, resize / Math.Max(1, furnit_resol));
		_previewSampleStep = (type_cr ? Math.Max(scaleStep, val) : resize);
		int scaledW = Math.Max(1, srcW / Math.Max(1, scaleStep));
		int scaledH = Math.Max(1, srcH / Math.Max(1, scaleStep));
		UpdateControls(scaledW, scaledH, tilesX, tilesY, canBuild: true, null);
		if (m_okButton != null && m_okButton.IsClicked)
		{
			try
			{
				_we.PreviewImageFromFile(_rot90, type_cr, scaleStep, furnitureRes, pos, rot, path_img, ofst_color);
				DialogsManager.HideDialog(this);
			}
			catch (Exception ex)
			{
				Log.Error(ex);
				DialogsManager.ShowDialog(player.GameWidget, new MessageDialog("Build failed", ex.Message, null, "OK", null));
			}
		}
		if (Input.Cancel || (m_cancelButton != null && m_cancelButton.IsClicked))
		{
			DialogsManager.HideDialog(this);
		}
		UpdatePreview2D();
	}

	public void Select_type_creating()
	{
		List<string> names = new List<string>();
		names.Add("Furniture");
		names.Add("Blocks");
		ContainerWidget value2;
		if ((value2 = ParentWidget) == null)
		{
			value2 = player?.GameWidget;
		}
		ContainerWidget containerWidget = value2;
		if (containerWidget == null)
		{
			return;
		}
		DialogsManager.ShowDialog(containerWidget, new ListSelectionDialog(string.Empty, names, 56f, (object c) => new LabelWidget
		{
			Text = c.ToString(),
			Color = Color.White,
			HorizontalAlignment = WidgetAlignment.Center,
			VerticalAlignment = WidgetAlignment.Center
		}, (object c) =>
		{
			if (c != null)
			{
				int index = names.IndexOf(c.ToString());
				string text = names[index];
				t_c_txt = text;
			}
		}));
	}

	public void Select_pos()
	{
		List<string> names = new List<string>();
		names.Add("Horizontally");
		names.Add("Vertical");
		ContainerWidget containerWidget;
		if ((containerWidget = ParentWidget) == null)
		{
			containerWidget = player?.GameWidget;
		}
		ContainerWidget containerWidget2 = containerWidget;
		if (containerWidget2 == null)
		{
			return;
		}
		DialogsManager.ShowDialog(containerWidget2, new ListSelectionDialog(string.Empty, names, 56f, (object c) => new LabelWidget
		{
			Text = c.ToString(),
			Color = Color.White,
			HorizontalAlignment = WidgetAlignment.Center,
			VerticalAlignment = WidgetAlignment.Center
		}, (object c) =>
		{
			if (c != null)
			{
				int index = names.IndexOf(c.ToString());
				string text = names[index];
				pos_txt = text;
			}
		}));
	}

	public void Select_rot()
	{
		List<string> names = new List<string>();
		names.Add("Front");
		names.Add("Side");
		ContainerWidget containerWidget;
		if ((containerWidget = ParentWidget) == null)
		{
			containerWidget = player?.GameWidget;
		}
		ContainerWidget containerWidget2 = containerWidget;
		if (containerWidget2 == null)
		{
			return;
		}
		DialogsManager.ShowDialog(containerWidget2, new ListSelectionDialog(string.Empty, names, 56f, (object c) => new LabelWidget
		{
			Text = c.ToString(),
			Color = Color.White,
			HorizontalAlignment = WidgetAlignment.Center,
			VerticalAlignment = WidgetAlignment.Center
		}, (object c) =>
		{
			if (c != null)
			{
				int index = names.IndexOf(c.ToString());
				string text = names[index];
				rot_txt = text;
			}
		}));
	}

	public Img(ComponentPlayer player, ComponentWorldEditIntegration we, SubsystemTerrain terrain, string path)
	{
		this.player = player;
		_we = we;
		m_subsystemTerrain = terrain;
		path_img = path;
		try
		{
			LoadContents(this, ContentManager.Get<XElement>("WE/DialogsWE/ImgDialog"));
		}
		catch (Exception ex)
		{
			Log.Error(ex);
			DialogsManager.ShowDialog(player?.GameWidget, new MessageDialog("UI error", "Не найден контент WE/DialogsWE/ImgDialog\n" + ex.Message, null, "OK", null));
			return;
		}
		m_okButton = Children.Find<ButtonWidget>("Img_Dialog.OK");
		m_cancelButton = Children.Find<ButtonWidget>("Img_Dialog.Cancel");
		m_type_creatingButton = Children.Find<ButtonWidget>("Img_Dialog.type_cr_btn");
		m_type_creatingLabel = Children.Find<LabelWidget>("Img_Dialog.type_cr_text");
		m_resizeSlider = Children.Find<SliderWidget>("Img_Dialog.resize_sl");
		m_furniture_resolutionSlider = Children.Find<SliderWidget>("Img_Dialog.furn_res_sl");
		m_posButton = Children.Find<ButtonWidget>("Img_Dialog.pos_sel");
		m_rotButton = Children.Find<ButtonWidget>("Img_Dialog.rot_sel");
		m_posLabel = Children.Find<LabelWidget>("Img_Dialog.pos_txt");
		m_rotLabel = Children.Find<LabelWidget>("Img_Dialog.rot_txt");
		m_mirrorBox = Children.Find<CheckboxWidget>("Img_Dialog.mirror", throwIfNotFound: false);
		if (m_mirrorBox != null)
		{
			m_mirrorBox.IsVisible = false;
		}
		m_image2DWidget = Children.Find<ImageWidget>("ImgDialog.Preview2D");
		m_resizePlus = Children.Find<ButtonWidget>("Img_Dialog.resize_plus", throwIfNotFound: false);
		m_resizeMinus = Children.Find<ButtonWidget>("Img_Dialog.resize_minus", throwIfNotFound: false);
		m_furnResPlus = Children.Find<ButtonWidget>("Img_Dialog.furn_res_plus", throwIfNotFound: false);
		m_furnResMinus = Children.Find<ButtonWidget>("Img_Dialog.furn_res_minus", throwIfNotFound: false);
		m_furnitureCountLabel = Children.Find<LabelWidget>("Img_Dialog.furniture_count", throwIfNotFound: false);
		m_blocksCountLabel = Children.Find<LabelWidget>("Img_Dialog.blocks_count", throwIfNotFound: false);
		m_furnResTitleLabel = Children.Find<LabelWidget>("Img_Dialog.furn_res_txt", throwIfNotFound: false);
		m_rot3Slider = Children.Find<SliderWidget>("Img_Dialog.rot3_sl", throwIfNotFound: false);
		m_color_ofsetSlider = Children.Find<SliderWidget>("Img_Dialog.color_offset_sl", throwIfNotFound: false);
		if (m_color_ofsetSlider != null)
		{
			m_color_ofsetSlider.MinValue = 0f;
			m_color_ofsetSlider.MaxValue = 15f;
			m_color_ofsetSlider.Value = 0f;
			m_color_ofsetSlider.Text = "Palette shift: 0";
		}
		if (m_rot3Slider != null)
		{
			m_rot3Slider.MinValue = 0f;
			m_rot3Slider.MaxValue = 2f;
			m_rot3Slider.Value = 1f;
			m_rot3Slider.Text = "0°";
		}
		if (m_resizeSlider != null)
		{
			m_resizeSlider.MinValue = 1f;
			m_resizeSlider.MaxValue = 64f;
			m_resizeSlider.Value = 2f;
		}
		if (m_furniture_resolutionSlider != null)
		{
			m_furniture_resolutionSlider.MinValue = 2f;
			m_furniture_resolutionSlider.MaxValue = 16f;
			m_furniture_resolutionSlider.Value = 8f;
		}
		t_c_txt = "Furniture";
		pos_txt = "Vertical";
		rot_txt = "Front";
		_rot90 = 0;
		try
		{
			Image image = Image.Load(path_img);
			_srcW = Math.Max(1, image.Width);
			_srcH = Math.Max(1, image.Height);
		}
		catch
		{
			_srcW = 1;
			_srcH = 1;
		}
		_freeSlotsCached = CountFreeFurnitureSlots();
		if (m_image2DWidget != null)
		{
			m_image2DWidget.SetSource(path_img);
			Color[] palette = ((m_subsystemTerrain != null && m_subsystemTerrain.SubsystemPalette != null) ? m_subsystemTerrain.SubsystemPalette.m_colors : null);
			int scaleStep = (int)((m_resizeSlider != null) ? m_resizeSlider.Value : 1f);
			int furnitureRes = (int)((m_furniture_resolutionSlider != null) ? m_furniture_resolutionSlider.Value : 8f);
			FitFurnitureToBudget(_srcW, _srcH, useFurniture: true, ref scaleStep, ref furnitureRes, out var _, out var _);
			m_image2DWidget.ConfigureFilter(quantize16: true, Math.Max(1, scaleStep), 0, palette);
			_lastFilterKey = $"q1_s{Math.Max(1, scaleStep)}_r0_o0";
		}
	}

	private void UpdateControls(int scaledW, int scaledH, int tilesX, int tilesY, bool canBuild, string reason)
	{
		if (m_resizeSlider != null)
		{
			string text = (type_cr ? "предметов" : "блоков");
			m_resizeSlider.Text = $"Сжатие: ×{(int)m_resizeSlider.Value}  →  {scaledW}×{scaledH} {text}";
		}
		bool flag = type_cr;
		if (m_furnResTitleLabel != null)
		{
			m_furnResTitleLabel.IsVisible = flag;
		}
		if (m_furniture_resolutionSlider != null)
		{
			m_furniture_resolutionSlider.IsVisible = flag;
			m_furniture_resolutionSlider.IsEnabled = flag;
			if (flag)
			{
				m_furniture_resolutionSlider.Text = $"{(int)m_furniture_resolutionSlider.Value} px / предмет  →  {tilesX}×{tilesY} предметов";
			}
			else
			{
				m_furniture_resolutionSlider.Text = string.Empty;
			}
		}
		if (m_furnResPlus != null)
		{
			m_furnResPlus.IsVisible = flag;
		}
		if (m_furnResMinus != null)
		{
			m_furnResMinus.IsVisible = flag;
		}
		if (m_furnitureCountLabel != null)
		{
			m_furnitureCountLabel.IsVisible = flag;
			m_furnitureCountLabel.Text = (flag ? (tilesX * tilesY).ToString() : string.Empty);
		}
		if (m_blocksCountLabel != null)
		{
			if (type_cr)
			{
				m_blocksCountLabel.IsVisible = false;
				m_blocksCountLabel.Text = string.Empty;
			}
			else
			{
				m_blocksCountLabel.IsVisible = true;
				m_blocksCountLabel.Text = (scaledW * scaledH).ToString();
			}
		}
		if (m_type_creatingLabel != null)
		{
			m_type_creatingLabel.Text = t_c_txt ?? "Furniture";
		}
		if (m_posLabel != null)
		{
			m_posLabel.Text = pos_txt ?? "Horizontally";
		}
		if (m_rotLabel != null)
		{
			m_rotLabel.Text = rot_txt ?? "Front";
		}
		if (m_okButton != null)
		{
			m_okButton.IsEnabled = true;
		}
	}

	private static int CeilDiv(int a, int b)
	{
		return (a + b - 1) / b;
	}

	private int CountFreeFurnitureSlots()
	{
		int num = 0;
		SubsystemFurnitureBlockBehavior subsystemFurnitureBlockBehavior = m_subsystemTerrain?.SubsystemFurnitureBlockBehavior;
		if (subsystemFurnitureBlockBehavior == null)
		{
			return 0;
		}
		for (int i = 0; i < 65535; i++)
		{
			if (subsystemFurnitureBlockBehavior.GetDesign(i) != null)
			{
				num++;
			}
		}
		return Math.Max(0, 65535 - num);
	}

	private void BuildPreviewFromImage(int scaleStep, bool horizontally, bool rotateFront, bool mirrorIgnored)
	{
		if (m_previewDesign == null || m_previewWidget == null || m_subsystemTerrain == null)
		{
			return;
		}
		Image face;
		try
		{
			face = Image.Load(path_img);
		}
		catch
		{
			m_previewDesign.SetValues(2, new int[8]);
			return;
		}
		if (_rot90 < 0)
		{
			Image image = new Image(face.Height, face.Width);
			for (int i = 0; i < image.Width; i++)
			{
				for (int j = 0; j < image.Height; j++)
				{
					image.SetPixel(i, j, face.GetPixel(face.Width - 1 - j, i));
				}
			}
			face = image;
		}
		else if (_rot90 > 0)
		{
			Image image2 = new Image(face.Height, face.Width);
			for (int k = 0; k < image2.Width; k++)
			{
				for (int l = 0; l < image2.Height; l++)
				{
					image2.SetPixel(k, l, face.GetPixel(l, face.Height - 1 - k));
				}
			}
			face = image2;
		}
		if (horizontally)
		{
			Image image3 = new Image(face.Height, face.Width);
			for (int m = 0; m < image3.Width; m++)
			{
				for (int n = 0; n < image3.Height; n++)
				{
					image3.SetPixel(m, image3.Height - 1 - n, face.GetPixel(n, m));
				}
			}
			face = image3;
		}
		if (scaleStep < 1)
		{
			scaleStep = 1;
		}
		int num = Math.Max(1, face.Width / scaleStep);
		int num2 = Math.Max(1, face.Height / scaleStep);
		int num3;
		if (_forcePreview16)
		{
			num3 = 16;
		}
		else
		{
			num3 = ((MathUtils.Max(num, num2) <= 8) ? 8 : 16);
		}
		int[] array = new int[16]
		{
			16456, 49224, 81992, 114760, 147528, 180296, 213064, 245832, 278600, 311368,
			344136, 376904, 409672, 442440, 475208, 507976
		};
		Color[] colors = m_subsystemTerrain.SubsystemPalette.m_colors;
		int num4 = ofst_color & 0xF;
		int num5 = Math.Min(colors.Length, array.Length);
		int[] array2 = new int[num3 * num3 * num3];
		for (int num6 = 0; num6 < num3; num6++)
		{
			int num7 = MathUtils.Clamp(num6 * num2 / num3, 0, num2 - 1);
			int y = Math.Min(face.Height - 1, num7 * scaleStep);
			for (int num8 = 0; num8 < num3; num8++)
			{
				int num9 = MathUtils.Clamp(num8 * num / num3, 0, num - 1);
				int x = Math.Min(face.Width - 1, num9 * scaleStep);
				Color pixel = face.GetPixel(x, y);
				if (pixel.A == 0)
				{
					continue;
				}
				pixel.A = byte.MaxValue;
				int num10 = 0;
				int num11 = int.MaxValue;
				for (int num12 = 0; num12 < num5; num12++)
				{
					int num13 = colors[num12].A - pixel.A;
					int num14 = colors[num12].R - pixel.R;
					int num15 = colors[num12].G - pixel.G;
					int num16 = colors[num12].B - pixel.B;
					int num17 = num13 * num13 + num14 * num14 + num15 * num15 + num16 * num16;
					if (num17 < num11)
					{
						num11 = num17;
						num10 = num12;
					}
				}
				int x2 = (num10 + num4) % num5;
				int num18 = array[MathUtils.Clamp(x2, 0, array.Length - 1)];
				int num19;
				int num20;
				int num21;
				if (horizontally)
				{
					if (rotateFront)
					{
						num19 = 0;
						num20 = num8;
						num21 = num6;
					}
					else
					{
						num19 = num6;
						num20 = num8;
						num21 = 0;
					}
				}
				else
				{
					num19 = num6;
					num20 = 0;
					num21 = num8;
				}
				int num22 = num19 + num20 * num3 + num21 * num3 * num3;
				if (num22 >= 0 && num22 < array2.Length)
				{
					array2[num22] = num18;
				}
			}
		}
		m_previewDesign.SetValues(num3, array2);
		Point3 location = m_previewDesign.Box.Location;
		Point3 point = new Point3(m_previewDesign.Resolution) - (m_previewDesign.Box.Location + m_previewDesign.Box.Size);
		Point3 delta = new Point3((point.X - location.X) / 2, -location.Y, (point.Z - location.Z) / 2);
		m_previewDesign.Shift(delta);
	}

	private void FitFurnitureToBudget(int srcW, int srcH, bool useFurniture, ref int scaleStep, ref int furnitureRes, out int tilesX, out int tilesY)
	{
		scaleStep = Math.Max(1, scaleStep);
		furnitureRes = MathUtils.Clamp(furnitureRes, 2, 16);
		if (!useFurniture)
		{
			tilesX = Math.Max(1, srcW / scaleStep);
			tilesY = Math.Max(1, srcH / scaleStep);
			return;
		}
		int num = ((_freeSlotsCached >= 0) ? _freeSlotsCached : CountFreeFurnitureSlots());
		if (num <= 0)
		{
			num = 1;
		}
		for (int i = 0; i < 128; i++)
		{
			int num2 = Math.Max(1, srcW / scaleStep);
			int num3 = Math.Max(1, srcH / scaleStep);
			tilesX = CeilDiv(num2, furnitureRes);
			tilesY = CeilDiv(num3, furnitureRes);
			long num4 = (long)tilesX * (long)tilesY;
			if (num4 <= num)
			{
				return;
			}
			if (furnitureRes < 16)
			{
				furnitureRes = Math.Min(16, furnitureRes * 2);
				continue;
			}
			double num5 = Math.Sqrt((double)num4 / (double)num);
			int num6 = Math.Max(1, (int)Math.Floor((double)num2 / num5));
			int num7 = Math.Max(1, (int)Math.Floor((double)num3 / num5));
			int num8 = Math.Max(scaleStep + 1, Math.Max((int)Math.Ceiling((double)srcW / (double)num6), (int)Math.Ceiling((double)srcH / (double)num7)));
			scaleStep = num8;
		}
		int a = Math.Max(1, srcW / scaleStep);
		int a2 = Math.Max(1, srcH / scaleStep);
		tilesX = CeilDiv(a, furnitureRes);
		tilesY = CeilDiv(a2, furnitureRes);
	}

	private void UpdatePreview2D()
	{
		if (m_image2DWidget != null && !string.IsNullOrEmpty(path_img))
		{
			Color[] palette = ((m_subsystemTerrain != null && m_subsystemTerrain.SubsystemPalette != null) ? m_subsystemTerrain.SubsystemPalette.m_colors : null);
			int num = Math.Max(1, _previewSampleStep);
			int rot = _rot90;
			int num2 = ofst_color & 0xF;
			string text = $"q1_s{num}_r{rot}_o{num2}";
			if (!string.Equals(m_image2DWidget.SourcePath, path_img, StringComparison.OrdinalIgnoreCase))
			{
				m_image2DWidget.SetSource(path_img);
				_lastFilterKey = null;
			}
			if (text != _lastFilterKey)
			{
				m_image2DWidget.ConfigureFilter(quantize16: true, num, rot, palette, num2);
				_lastFilterKey = text;
			}
		}
	}
}
