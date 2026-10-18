using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Engine;
using Engine.Graphics;
using Engine.Media;

namespace Game;

public class LoomWidget : CanvasWidget
{
	private ComponentLoom m_componentLoom;

	private ClothingPreviewWidget m_preview;

	private BevelledButtonWidget m_viewModeBtn;

	private int m_lastSlotValue;

	private Color m_editColor;

	private ButtonWidget m_skinButton;

	private ButtonWidget m_genderButton;

	private ButtonWidget m_bodyButton;

	private CharacterSkinsCache m_skinsCache;

	private string m_currentSkinName;

	private PlayerClass m_currentClass;

	private PlayerData.PlayerTag m_currentTag;

	private CheckboxWidget m_gridCheckbox;

	private ButtonWidget m_zoomInBtn;

	private ButtonWidget m_zoomOutBtn;

	private ButtonWidget m_panUpBtn;

	private ButtonWidget m_panDownBtn;

	private ButtonWidget m_panLeftBtn;

	private ButtonWidget m_panRightBtn;

	private ButtonWidget m_undoBtn;

	private ButtonWidget m_redoBtn;

	private bool m_isView2D;

	private BevelledButtonWidget m_rotatePaintBtn;

	private CheckboxWidget m_showSkinCheckbox;

	private SliderWidget m_lightSlider;

	private RectangleWidget m_partHead;

	private RectangleWidget m_partBody;

	private RectangleWidget m_partHand1;

	private RectangleWidget m_partHand2;

	private RectangleWidget m_partLeg1;

	private RectangleWidget m_partLeg2;

	private bool m_wasPainting;

	private RectangleWidget m_hitboxHead;

	private RectangleWidget m_hitboxBody;

	private RectangleWidget m_hitboxHand1;

	private RectangleWidget m_hitboxHand2;

	private RectangleWidget m_hitboxLeg1;

	private RectangleWidget m_hitboxLeg2;

	private CheckboxWidget m_noShadowsCheckbox;

	private int m_currentBrushSize = 1;

	private BevelledButtonWidget m_brushBtn;

	private BevelledButtonWidget m_eraserBtn;

	private BevelledButtonWidget m_pipetteBtn;

	private ImageWidget m_design2d;

	private ImageWidget m_skinPreview2D;

	private ButtonWidget m_saveButton;

	private ButtonWidget m_loadButton;

	private InventorySlotWidget m_loomSlotWidget;

	private BevelledButtonWidget m_applyButton;

	public LoomWidget(IInventory playerInventory, ComponentLoom loom)
	{
		m_skinsCache = new CharacterSkinsCache();
		m_currentTag = PlayerData.PlayerTag.Tall;
		m_lastSlotValue = -1;
		m_editColor = Color.Red;
		m_componentLoom = loom;
		XElement node = ContentManager.Get<XElement>("Widgets/LoomWidget");
		LoadContents(this, node);
		m_noShadowsCheckbox = Children.Find<CheckboxWidget>("NoShadowsCheckbox");
		m_rotatePaintBtn = Children.Find<BevelledButtonWidget>("RotatePaintButton");
		m_preview = Children.Find<ClothingPreviewWidget>("ClothingPreview");
		m_lightSlider = Children.Find<SliderWidget>("LightSlider");
		m_brushBtn = Children.Find<BevelledButtonWidget>("BrushButton");
		m_eraserBtn = Children.Find<BevelledButtonWidget>("EraserButton");
		m_pipetteBtn = Children.Find<BevelledButtonWidget>("PipetteButton");
		m_viewModeBtn = Children.Find<BevelledButtonWidget>("ViewModeButton");
		GridPanelWidget gridPanelWidget = Children.Find<GridPanelWidget>("InventoryGrid");
		m_skinButton = Children.Find<ButtonWidget>("SkinButton");
		m_genderButton = Children.Find<ButtonWidget>("GenderButton");
		m_bodyButton = Children.Find<ButtonWidget>("BodyButton");
		m_design2d = Children.Find<ImageWidget>("Design2D");
		m_skinPreview2D = Children.Find<ImageWidget>("SkinPreview2D");
		m_gridCheckbox = Children.Find<CheckboxWidget>("ShowGridCheckbox");
		m_zoomInBtn = Children.Find<ButtonWidget>("ZoomInButton");
		m_zoomOutBtn = Children.Find<ButtonWidget>("ZoomOutButton");
		m_panUpBtn = Children.Find<ButtonWidget>("PanUpButton");
		m_panDownBtn = Children.Find<ButtonWidget>("PanDownButton");
		m_panLeftBtn = Children.Find<ButtonWidget>("PanLeftButton");
		m_panRightBtn = Children.Find<ButtonWidget>("PanRightButton");
		m_undoBtn = Children.Find<ButtonWidget>("UndoButton");
		m_redoBtn = Children.Find<ButtonWidget>("RedoButton");
		m_saveButton = Children.Find<ButtonWidget>("SaveButton");
		m_loadButton = Children.Find<ButtonWidget>("LoadButton");
		m_showSkinCheckbox = Children.Find<CheckboxWidget>("ShowSkinCheckbox");
		m_partHead = Children.Find<RectangleWidget>("Part_Head");
		m_partBody = Children.Find<RectangleWidget>("Part_Body");
		m_partHand1 = Children.Find<RectangleWidget>("Part_Hand1");
		m_partHand2 = Children.Find<RectangleWidget>("Part_Hand2");
		m_partLeg1 = Children.Find<RectangleWidget>("Part_Leg1");
		m_partLeg2 = Children.Find<RectangleWidget>("Part_Leg2");
		m_applyButton = Children.Find<BevelledButtonWidget>("CraftButton");
		m_hitboxHead = Children.Find<RectangleWidget>("Hitbox_Head");
		m_hitboxBody = Children.Find<RectangleWidget>("Hitbox_Body");
		m_hitboxHand1 = Children.Find<RectangleWidget>("Hitbox_Hand1");
		m_hitboxHand2 = Children.Find<RectangleWidget>("Hitbox_Hand2");
		m_hitboxLeg1 = Children.Find<RectangleWidget>("Hitbox_Leg1");
		m_hitboxLeg2 = Children.Find<RectangleWidget>("Hitbox_Leg2");
		m_viewModeBtn.Text = "View: 3D";
		int num = 10;
		for (int i = 0; i < gridPanelWidget.RowsCount; i++)
		{
			for (int j = 0; j < gridPanelWidget.ColumnsCount; j++)
			{
				InventorySlotWidget inventorySlotWidget = new InventorySlotWidget();
				inventorySlotWidget.AssignInventorySlot(playerInventory, num++);
				gridPanelWidget.Children.Add(inventorySlotWidget);
				gridPanelWidget.SetWidgetCell(inventorySlotWidget, new Point2(j, i));
			}
		}
		GridPanelWidget gridPanelWidget2 = Children.Find<GridPanelWidget>("LoomGrid");
		InventorySlotWidget inventorySlotWidget2 = new InventorySlotWidget();
		inventorySlotWidget2.AssignInventorySlot(loom, 0);
		m_loomSlotWidget = inventorySlotWidget2;
		gridPanelWidget2.Children.Add(inventorySlotWidget2);
		gridPanelWidget2.SetWidgetCell(inventorySlotWidget2, new Point2(0, 0));
		UpdateToolButtons();
		if (m_componentLoom.SavedTextureData != null)
		{
			m_preview.SetRawTextureData(m_componentLoom.SavedTextureData, m_componentLoom.SavedTextureWidth, m_componentLoom.SavedTextureHeight);
			m_design2d.Texture = m_preview.WorkingTexture;
		}
		m_editColor = m_componentLoom.SavedEditColor;
		m_currentBrushSize = m_componentLoom.SavedBrushSize;
		if (m_noShadowsCheckbox != null)
		{
			m_noShadowsCheckbox.IsChecked = m_componentLoom.SavedNoShadows;
		}
		if (m_lightSlider != null)
		{
			m_lightSlider.Value = m_componentLoom.SavedLightRotation;
		}
		m_preview.IsPaintMode = m_componentLoom.SavedIsPaintMode;
		m_preview.ActiveTool = ClothingPreviewWidget.ToolType.Brush;
		m_preview.CameraZoom = m_componentLoom.SavedCameraZoom;
		m_preview.CameraPan = m_componentLoom.SavedCameraPan;
		m_preview.CameraRotation = m_componentLoom.SavedCameraRotation;
		m_preview.CameraRotationVertical = m_componentLoom.SavedCameraRotationVertical;
		m_currentSkinName = m_componentLoom.SavedSkinName;
		m_preview.AlwaysShowSkin = m_componentLoom.SavedShowSkin;
		if (m_showSkinCheckbox != null)
		{
			m_showSkinCheckbox.IsChecked = m_componentLoom.SavedShowSkin;
		}
		m_skinButton.Text = ((m_currentSkinName == null) ? "Skin" : CharacterSkinsManager.GetDisplayName(m_currentSkinName));
		if (!string.IsNullOrEmpty(m_currentSkinName) || m_componentLoom.SavedShowSkin)
		{
			UpdateModelSettings();
		}
		UpdateModeButtonText();
	}

	public override void Update()
	{
		if (m_brushBtn.IsClicked)
		{
			if (m_preview.ActiveTool == ClothingPreviewWidget.ToolType.Brush)
			{
				DialogsManager.ShowDialog(null, new AdvancedColorDialog(m_editColor, m_currentBrushSize, (Color? result, int size) =>
				{
					if (result.HasValue)
					{
						m_editColor = result.Value;
						m_currentBrushSize = size;
						m_preview.BrushSize = m_currentBrushSize;
						UpdateToolButtons();
					}
				}));
			}
			else
			{
				m_preview.ActiveTool = ClothingPreviewWidget.ToolType.Brush;
				m_preview.IsPaintMode = true;
				UpdateToolButtons();
				UpdateModeButtonText();
			}
		}
		if (m_eraserBtn.IsClicked)
		{
			m_preview.ActiveTool = ClothingPreviewWidget.ToolType.Eraser;
			m_preview.IsPaintMode = true;
			UpdateToolButtons();
			UpdateModeButtonText();
		}
		if (m_pipetteBtn.IsClicked)
		{
			m_preview.ActiveTool = ClothingPreviewWidget.ToolType.Pipette;
			m_preview.IsPaintMode = false;
			UpdateToolButtons();
		}
		if (m_loomSlotWidget != null)
		{
			m_loomSlotWidget.IsEnabled = !m_preview.IsModified;
		}
		if (m_saveButton.IsClicked)
		{
			int slotValueToSave = m_componentLoom.GetSlotValue(0);
			if (Terrain.ExtractContents(slotValueToSave) == 0)
			{
				DialogsManager.ShowDialog(null, new MessageDialog("Error", "Slot is empty!", "OK", null, null));
			}
			else
			{
				DialogsManager.ShowDialog(null, new TextBoxDialog("Enter Design Name", "MyDesign", 32, (string name) =>
				{
					if (!string.IsNullOrEmpty(name))
					{
						try
						{
							string text = "app:/LoomDesigns";
							if (!Storage.DirectoryExists(text))
							{
								Storage.CreateDirectory(text);
							}
							Image image = m_preview.GetImage();
							if (image != null)
							{
								string text2 = name + ".png";
								using (Stream stream = Storage.OpenFile(Storage.CombinePaths(text, text2), OpenFileMode.Create))
								{
									Image.Save(image, stream, ImageFileFormat.Png, saveAlpha: true);
								}
								using (Stream output = Storage.OpenFile(Storage.CombinePaths(text, name + ".dat"), OpenFileMode.Create))
								{
									using BinaryWriter binaryWriter = new BinaryWriter(output);
									binaryWriter.Write("LoomDesignData");
									binaryWriter.Write(1);
									binaryWriter.Write(text2);
									int value = Terrain.ExtractData(slotValueToSave);
									if (BlocksManager.Blocks[Terrain.ExtractContents(slotValueToSave)] is ClothingBlock clothingBlock)
									{
										ClothingData clothingData = clothingBlock.GetClothingData(value);
										binaryWriter.Write(clothingData.Index);
										binaryWriter.Write(clothingData.DisplayName);
										binaryWriter.Write((int)clothingData.Slot);
										binaryWriter.Write(clothingData.IsOuter);
										binaryWriter.Write(clothingData.Texture.Width);
										binaryWriter.Write(clothingData.Texture.Height);
									}
									else
									{
										binaryWriter.Write(-1);
										binaryWriter.Write("Unknown");
										binaryWriter.Write(0);
										binaryWriter.Write(value: false);
										binaryWriter.Write(0);
										binaryWriter.Write(0);
									}
								}
								SubsystemCustomClothing subsystemCustomClothing = m_componentLoom.Project.FindSubsystem<SubsystemCustomClothing>(throwOnError: true);
								int slotValue3 = m_componentLoom.GetSlotValue(0);
								int num7 = Terrain.ExtractContents(slotValue3);
								int value2 = Terrain.ExtractData(slotValue3);
								if (BlocksManager.Blocks[num7] is ClothingBlock clothingBlock2)
								{
									ClothingData clothingData2 = clothingBlock2.GetClothingData(value2);
									Color[] rawTextureData = m_preview.GetRawTextureData(out var w, out var h);
									subsystemCustomClothing.AddCustomClothing(new CustomClothingData
									{
										Id = name,
										ClothingIndex = clothingData2.Index,
										Width = w,
										Height = h,
										TextureData = rawTextureData
									});
								}
								DialogsManager.ShowDialog(null, new MessageDialog("Success", "Design saved to files and world!", "OK", null, null));
								m_preview.IsModified = false;
							}
						}
						catch (Exception ex)
						{
							DialogsManager.ShowDialog(null, new MessageDialog("Error", "Failed to save: " + ex.Message, "OK", null, null));
						}
					}
				}));
			}
		}
		if (m_loadButton.IsClicked)
		{
			int slotValue = m_componentLoom.GetSlotValue(0);
			int currentContents = Terrain.ExtractContents(slotValue);
			int currentData = Terrain.ExtractData(slotValue);
			if (m_preview.IsModified)
			{
				DialogsManager.ShowDialog(null, new MessageDialog("Warning", "Clear the canvas before loading (use Eraser or re-insert item).", "OK", null, null));
			}
			else if (currentContents == 0)
			{
				DialogsManager.ShowDialog(null, new MessageDialog("Error", "Insert clothing into the loom first!", "OK", null, null));
			}
			else
			{
				string path = "app:/LoomDesigns";
				if (!Storage.DirectoryExists(path))
				{
					Storage.CreateDirectory(path);
				}
				List<string> list = new List<string>();
				foreach (string item in Storage.ListFileNames(path))
				{
					if (Path.GetExtension(item) == ".dat")
					{
						list.Add(Path.GetFileNameWithoutExtension(item));
					}
				}
				DialogsManager.ShowDialog(null, new ListSelectionDialog("Select Design", list, 60f, (object item) => new LabelWidget
				{
					Text = (string)item,
					HorizontalAlignment = WidgetAlignment.Center,
					VerticalAlignment = WidgetAlignment.Center
				}, (object item) =>
				{
					string text = (string)item;
					try
					{
						string path2 = Storage.CombinePaths(path, text + ".dat");
						string text2 = "";
						int num7 = -1;
						using (Stream input = Storage.OpenFile(path2, OpenFileMode.Read))
						{
							using BinaryReader binaryReader = new BinaryReader(input);
							string text3 = binaryReader.ReadString();
							if (text3 == "LoomDesignData")
							{
								binaryReader.ReadInt32();
								text2 = binaryReader.ReadString();
								num7 = binaryReader.ReadInt32();
								binaryReader.ReadString();
								binaryReader.ReadInt32();
								binaryReader.ReadBoolean();
							}
							else
							{
								text2 = text3;
							}
						}
						if (num7 != -1)
						{
							Block obj = BlocksManager.Blocks[currentContents];
							int num8 = -1;
							if (obj is ClothingBlock clothingBlock)
							{
								num8 = clothingBlock.GetClothingData(currentData).Index;
							}
							if (num7 != num8)
							{
								DialogsManager.ShowDialog(null, new MessageDialog("Mismatch", $"This design is for a different item!\nFile Item Index: {num7}\nSlot Item Index: {num8}", "OK", null, null));
								return;
							}
						}
						using Stream stream = Storage.OpenFile(Storage.CombinePaths(path, text2), OpenFileMode.Read);
						Image image = Image.Load(stream, ImageFileFormat.Png);
						m_preview.SetImage(image);
						UpdateModelSettings();
					}
					catch (Exception ex)
					{
						DialogsManager.ShowDialog(null, new MessageDialog("Error", "Corrupt file: " + ex.Message, "OK", null, null));
					}
				}));
			}
		}
		if (m_applyButton != null && m_applyButton.IsClicked)
		{
			int slotVal = m_componentLoom.GetSlotValue(0);
			int contents = Terrain.ExtractContents(slotVal);
			if (contents != 0)
			{
				DialogsManager.ShowDialog(null, new TextBoxDialog("Apply Design", "MySkin", 32, (string name) =>
				{
					if (!string.IsNullOrEmpty(name))
					{
						try
						{
							Log.Information("[Loom] Applying design via file: " + name + ".png");
							string text = "app:/LoomDesigns";
							if (!Storage.DirectoryExists(text))
							{
								Storage.CreateDirectory(text);
							}
							Image image = m_preview.GetImage();
							string text2 = name + ".png";
							using (Stream stream = Storage.OpenFile(Storage.CombinePaths(text, text2), OpenFileMode.Create))
							{
								Image.Save(image, stream, ImageFileFormat.Png, saveAlpha: true);
							}
							SubsystemCustomClothing subsystemCustomClothing = m_componentLoom.Project.FindSubsystem<SubsystemCustomClothing>(throwOnError: true);
							int value = Terrain.ExtractData(slotVal);
							if (BlocksManager.Blocks[contents] is ClothingBlock clothingBlock)
							{
								ClothingData clothingData = clothingBlock.GetClothingData(value);
								subsystemCustomClothing.AddCustomClothing(new CustomClothingData
								{
									Id = name,
									ClothingIndex = clothingData.Index,
									TextureName = text2
								});
								int designIndex = subsystemCustomClothing.GetDesignIndex(name);
								int data = ClothingBlock.SetClothingIndex(0, clothingData.Index);
								data = ClothingBlock.SetDesignId(data, designIndex);
								data = ClothingBlock.SetCustom(data, isCustom: true);
								int num7 = Terrain.MakeBlockValue(203, 0, data);
								m_componentLoom.AddSlotItem(0, num7, 1);
								Log.Information($"[Loom] SUCCESS: Item linked to file {text2}. Final Value: {num7}");
								DialogsManager.ShowDialog(null, new MessageDialog("Applied", "Design linked to PNG file and applied!", "OK", null, null));
								m_preview.IsModified = false;
							}
						}
						catch (Exception ex)
						{
							Log.Error("[Loom] Apply Error: " + ex.Message);
						}
					}
				}));
			}
		}
		bool flag = Input.Press.HasValue || Input.Tap.HasValue;
		Vector2 vector = Input.Press ?? Input.Tap ?? Vector2.Zero;
		bool flag2 = false;
		if (flag)
		{
			if (m_isView2D)
			{
				if (m_design2d.IsVisible && m_design2d.GlobalBounds.Contains(vector))
				{
					flag2 = true;
				}
			}
			else if (m_preview.HitTestGlobal(vector) == m_preview)
			{
				flag2 = true;
			}
		}
		if (m_preview.IsPaintMode || m_preview.ActiveTool == ClothingPreviewWidget.ToolType.Eraser)
		{
			if (flag)
			{
				if (!m_wasPainting & flag2)
				{
					m_preview.BeginStroke();
					m_wasPainting = true;
				}
			}
			else if (m_wasPainting)
			{
				m_preview.EndStroke();
				m_wasPainting = false;
			}
		}
		else
		{
			m_wasPainting = false;
		}
		if (flag & flag2)
		{
			if (m_preview.ActiveTool == ClothingPreviewWidget.ToolType.Pipette)
			{
				Color? color = null;
				if (m_isView2D)
				{
					BoundingRectangle globalBounds = m_design2d.GlobalBounds;
					float num = globalBounds.Max.X - globalBounds.Min.X;
					float num2 = globalBounds.Max.Y - globalBounds.Min.Y;
					if (num > 0f && num2 > 0f)
					{
						float x = (vector.X - globalBounds.Min.X) / num;
						float y = (vector.Y - globalBounds.Min.Y) / num2;
						color = m_preview.PickColor(new Vector2(x, y));
					}
				}
				else
				{
					color = m_preview.Pick3D(vector);
				}
				if (color.HasValue && color.Value != Color.Transparent)
				{
					m_editColor = color.Value;
					UpdateToolButtons();
				}
			}
			else if (m_wasPainting)
			{
				if (m_isView2D)
				{
					BoundingRectangle globalBounds2 = m_design2d.GlobalBounds;
					float num3 = globalBounds2.Max.X - globalBounds2.Min.X;
					float num4 = globalBounds2.Max.Y - globalBounds2.Min.Y;
					if (num3 > 0f && num4 > 0f)
					{
						float x2 = (vector.X - globalBounds2.Min.X) / num3;
						float y2 = (vector.Y - globalBounds2.Min.Y) / num4;
						m_preview.PaintTextureUV(new Vector2(x2, y2), m_editColor);
						m_preview.FlushTexture();
					}
				}
				else
				{
					m_preview.BrushSize = m_currentBrushSize;
					m_preview.Paint3D(vector, m_editColor);
				}
			}
		}
		if (m_componentLoom.Entity == null)
		{
			ParentWidget.Children.Remove(this);
			return;
		}
		if (m_lightSlider != null)
		{
			bool noShadows = m_noShadowsCheckbox != null && m_noShadowsCheckbox.IsChecked;
			m_preview.NoShadows = noShadows;
			m_lightSlider.IsEnabled = true;
			m_preview.LightRotation = m_lightSlider.Value;
		}
		if (m_rotatePaintBtn.IsClicked)
		{
			m_preview.IsPaintMode = !m_preview.IsPaintMode;
			if (m_preview.IsPaintMode)
			{
				m_preview.ActiveTool = ClothingPreviewWidget.ToolType.Brush;
				UpdateToolButtons();
			}
			UpdateModeButtonText();
		}
		if (m_viewModeBtn.IsClicked)
		{
			if (SettingsManager.DebugMode)
			{
				Log.Information("[LOOM DEBUG] Switch View Clicked. Current: " + (m_isView2D ? "2D" : "3D") + ". Preview.WorkingTexture is " + ((m_preview.WorkingTexture == null) ? "NULL" : "EXISTS"));
			}
			m_preview.FlushTexture();
			m_isView2D = !m_isView2D;
			m_preview.IsVisible = !m_isView2D;
			m_design2d.IsVisible = m_isView2D;
			m_viewModeBtn.Text = (m_isView2D ? "View: 2D" : "View: 3D");
			if (m_preview.WorkingTexture != null)
			{
				if (SettingsManager.DebugMode)
				{
					Log.Information("[LOOM DEBUG] Assigning WorkingTexture to Design2D.");
				}
				m_design2d.Texture = m_preview.WorkingTexture;
				m_design2d.ColorTransForm = m_preview.ClothingColor;
				m_design2d.ForcePointFilter = true;
			}
			else if (SettingsManager.DebugMode)
			{
				Log.Warning("[LOOM DEBUG] WorkingTexture is NULL during switch! Design2D might be empty.");
			}
			Update2DLayersVisibility();
		}
		if (m_showSkinCheckbox != null)
		{
			m_preview.AlwaysShowSkin = m_showSkinCheckbox.IsChecked;
			if (m_isView2D && m_showSkinCheckbox.IsClicked)
			{
				if (SettingsManager.DebugMode)
				{
					Log.Information($"[LOOM DEBUG] Skin Checkbox Clicked. State: {m_showSkinCheckbox.IsChecked}. IsView2D: {m_isView2D}");
				}
				Update2DLayersVisibility();
			}
		}
		float num5 = 0.1f;
		float num6 = 0.1f;
		if (m_zoomInBtn.IsClicked)
		{
			m_preview.Zoom(num6);
		}
		if (m_zoomOutBtn.IsClicked)
		{
			m_preview.Zoom(0f - num6);
		}
		if (m_panUpBtn.IsClicked)
		{
			m_preview.Pan(new Vector2(0f, num5));
		}
		if (m_panDownBtn.IsClicked)
		{
			m_preview.Pan(new Vector2(0f, 0f - num5));
		}
		if (m_panLeftBtn.IsClicked)
		{
			m_preview.Pan(new Vector2(0f - num5, 0f));
		}
		if (m_panRightBtn.IsClicked)
		{
			m_preview.Pan(new Vector2(num5, 0f));
		}
		if (m_gridCheckbox.IsClicked)
		{
			m_preview.ShowGrid = m_gridCheckbox.IsChecked;
		}
		HandleBodyPartClick(m_hitboxHead, m_partHead, "Head");
		HandleBodyPartClick(m_hitboxBody, m_partBody, "Body");
		HandleBodyPartClick(m_hitboxHand1, m_partHand1, "Hand2");
		HandleBodyPartClick(m_hitboxHand2, m_partHand2, "Hand1");
		HandleBodyPartClick(m_hitboxLeg1, m_partLeg1, "Leg2");
		HandleBodyPartClick(m_hitboxLeg2, m_partLeg2, "Leg1");
		if (m_undoBtn.IsClicked)
		{
			m_preview.Undo();
		}
		if (m_redoBtn.IsClicked)
		{
			m_preview.Redo();
		}
		if (m_skinButton.IsClicked)
		{
			CharacterSkinsManager.UpdateCharacterSkinsList();
			List<string> list2 = new List<string> { "None" };
			list2.AddRange(CharacterSkinsManager.CharacterSkinsNames);
			DialogsManager.ShowDialog(null, new ListSelectionDialog("Select Skin", list2, 64f, (object item) =>
			{
				string text = (string)item;
				XElement node = ContentManager.Get<XElement>("Widgets/CharacterSkinItem");
				ContainerWidget containerWidget = (ContainerWidget)Widget.LoadWidget(this, node, null);
				if (text == "None")
				{
					containerWidget.Children.Find<LabelWidget>("CharacterSkinItem.Text").Text = "None";
					return containerWidget;
				}
				Texture2D texture = m_skinsCache.GetTexture(text);
				containerWidget.Children.Find<LabelWidget>("CharacterSkinItem.Text").Text = CharacterSkinsManager.GetDisplayName(text);
				containerWidget.Children.Find<PlayerModelWidget>("CharacterSkinItem.Model").CharacterSkinTexture = texture;
				return containerWidget;
			}, (object item) =>
			{
				string text = (string)item;
				m_currentSkinName = ((text == "None") ? null : text);
				m_skinButton.Text = ((m_currentSkinName == null) ? "Skin" : CharacterSkinsManager.GetDisplayName(m_currentSkinName));
				UpdateModelSettings();
			}));
		}
		if (m_genderButton.IsClicked)
		{
			m_currentClass = ((m_currentClass == PlayerClass.Male) ? PlayerClass.Female : PlayerClass.Male);
			m_genderButton.Text = m_currentClass.ToString();
			UpdateModelSettings();
		}
		if (m_bodyButton.IsClicked)
		{
			int length = Enum.GetValues(typeof(PlayerData.PlayerTag)).Length;
			int currentTag = (int)(m_currentTag + 1) % length;
			m_currentTag = (PlayerData.PlayerTag)currentTag;
			m_bodyButton.Text = m_currentTag.ToString();
			UpdateModelSettings();
		}
		int slotValue2 = m_componentLoom.GetSlotValue(0);
		if (slotValue2 != m_lastSlotValue)
		{
			if (SettingsManager.DebugMode)
			{
				Log.Information($"[LOOM DEBUG] Slot Changed! Old: {m_lastSlotValue}, New: {slotValue2}. Calling UpdatePreview...");
			}
			m_lastSlotValue = slotValue2;
			UpdatePreview(slotValue2);
			if (Terrain.ExtractContents(slotValue2) == 0)
			{
				m_componentLoom.SavedTextureData = null;
				m_componentLoom.SavedTextureWidth = 0;
				m_componentLoom.SavedTextureHeight = 0;
				m_preview.IsModified = false;
			}
		}
	}

	private void UpdatePreview(int value)
	{
		int num = Terrain.ExtractContents(value);
		Block block = BlocksManager.Blocks[num];
		if (!(block is ClothingBlock))
		{
			m_preview.Setup(isOuter: false, (ClothingSlot)(-1), null, Color.White);
			m_design2d.Texture = null;
			return;
		}
		int num2 = Terrain.ExtractData(value);
		ClothingData clothingData = ((ClothingBlock)block).GetClothingData(num2);
		int clothingColor = ClothingBlock.GetClothingColor(num2);
		Color fabricColor = SubsystemPalette.GetFabricColor(m_componentLoom.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true), clothingColor);
		m_preview.Setup(clothingData.IsOuter, clothingData.Slot, clothingData.Texture, fabricColor);
		if (m_componentLoom.SavedTextureData != null && m_componentLoom.SavedTextureWidth == clothingData.Texture.Width && m_componentLoom.SavedTextureHeight == clothingData.Texture.Height)
		{
			m_preview.SetRawTextureData(m_componentLoom.SavedTextureData, m_componentLoom.SavedTextureWidth, m_componentLoom.SavedTextureHeight);
		}
		Texture2D workingTexture = m_preview.WorkingTexture;
		if (workingTexture != null)
		{
			m_design2d.Texture = workingTexture;
			m_design2d.ColorTransForm = fabricColor;
			m_design2d.ForcePointFilter = true;
		}
	}

	private void UpdateModelSettings()
	{
		m_preview.PlayerClass = m_currentClass;
		m_preview.BodyTag = m_currentTag;
		Texture2D texture2D = ((m_currentSkinName != null) ? m_skinsCache.GetTexture(m_currentSkinName) : null);
		m_preview.SkinTexture = texture2D;
		if (m_skinPreview2D != null)
		{
			if (texture2D != null)
			{
				m_skinPreview2D.Texture = texture2D;
				m_skinPreview2D.ForcePointFilter = true;
			}
			else
			{
				m_skinPreview2D.Texture = null;
			}
		}
		if (m_preview.WorkingTexture != null)
		{
			m_design2d.Texture = m_preview.WorkingTexture;
		}
	}

	public override void UpdateCeases()
	{
		if (m_componentLoom != null)
		{
			m_componentLoom.SavedBrushSize = m_currentBrushSize;
			m_componentLoom.SavedEditColor = m_editColor;
			m_componentLoom.SavedIsPaintMode = m_preview.IsPaintMode;
			if (m_lightSlider != null)
			{
				m_componentLoom.SavedLightRotation = m_lightSlider.Value;
			}
			if (m_noShadowsCheckbox != null)
			{
				m_componentLoom.SavedNoShadows = m_noShadowsCheckbox.IsChecked;
			}
			m_componentLoom.SavedCameraZoom = m_preview.CameraZoom;
			m_componentLoom.SavedCameraPan = m_preview.CameraPan;
			m_componentLoom.SavedCameraRotation = m_preview.CameraRotation;
			m_componentLoom.SavedCameraRotationVertical = m_preview.CameraRotationVertical;
			m_componentLoom.SavedShowSkin = m_showSkinCheckbox.IsChecked;
			m_componentLoom.SavedSkinName = m_currentSkinName;
			Color[] rawTextureData = m_preview.GetRawTextureData(out var w, out var h);
			if (rawTextureData != null)
			{
				m_componentLoom.SavedTextureData = rawTextureData;
				m_componentLoom.SavedTextureWidth = w;
				m_componentLoom.SavedTextureHeight = h;
			}
		}
		base.UpdateCeases();
		m_preview.DisposeTexture();
		m_skinsCache.Clear();
	}

	private void UpdateModeButtonText()
	{
		if (m_preview.IsPaintMode)
		{
			m_rotatePaintBtn.Text = "Mode: Paint";
			m_rotatePaintBtn.Color = Color.LightGreen;
		}
		else
		{
			m_rotatePaintBtn.Text = "Mode: Rotate";
			m_rotatePaintBtn.Color = Color.White;
		}
	}

	private void Update2DLayersVisibility()
	{
		m_design2d.IsVisible = m_isView2D;
		if (m_isView2D && m_skinPreview2D != null)
		{
			m_skinPreview2D.IsVisible = m_showSkinCheckbox.IsChecked;
		}
		else if (m_skinPreview2D != null)
		{
			m_skinPreview2D.IsVisible = false;
		}
	}

	private void HandleBodyPartClick(RectangleWidget widget, string partName)
	{
		if (widget != null && widget.Input.Tap.HasValue && widget.HitTest(widget.Input.Tap.Value))
		{
			if (m_preview.HiddenParts.Contains(partName))
			{
				m_preview.HiddenParts.Remove(partName);
				widget.ColorTransform = Color.White;
			}
			else
			{
				m_preview.HiddenParts.Add(partName);
				widget.ColorTransform = Color.Red;
			}
		}
	}

	private void HandleBodyPartClick(RectangleWidget hitbox, RectangleWidget visual, string partName)
	{
		if (hitbox == null || !hitbox.Input.Tap.HasValue || !hitbox.HitTest(hitbox.Input.Tap.Value))
		{
			return;
		}
		if (m_preview.HiddenParts.Contains(partName))
		{
			m_preview.HiddenParts.Remove(partName);
			if (visual != null)
			{
				visual.ColorTransform = Color.White;
			}
		}
		else
		{
			m_preview.HiddenParts.Add(partName);
			if (visual != null)
			{
				visual.ColorTransform = Color.Red;
			}
		}
	}

	private void UpdateToolButtons()
	{
		m_brushBtn.CenterColor = Color.White;
		m_eraserBtn.CenterColor = Color.White;
		m_pipetteBtn.CenterColor = Color.White;
		m_brushBtn.CenterColor = m_editColor;
		if (m_preview.ActiveTool == ClothingPreviewWidget.ToolType.Brush)
		{
			m_eraserBtn.Color = new Color(128, 128, 128);
			m_pipetteBtn.Color = new Color(128, 128, 128);
			m_brushBtn.Color = Color.White;
		}
		else if (m_preview.ActiveTool == ClothingPreviewWidget.ToolType.Eraser)
		{
			m_eraserBtn.CenterColor = Color.LightGreen;
			m_eraserBtn.Color = Color.White;
			m_brushBtn.Color = new Color(128, 128, 128);
			m_pipetteBtn.Color = new Color(128, 128, 128);
		}
		else if (m_preview.ActiveTool == ClothingPreviewWidget.ToolType.Pipette)
		{
			m_pipetteBtn.CenterColor = Color.LightGreen;
			m_pipetteBtn.Color = Color.White;
			m_brushBtn.Color = new Color(128, 128, 128);
			m_eraserBtn.Color = new Color(128, 128, 128);
		}
	}
}
