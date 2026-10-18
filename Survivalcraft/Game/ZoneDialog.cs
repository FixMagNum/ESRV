using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using Engine;

namespace Game;

public class ZoneDialog : Dialog
{
	private ListPanelWidget list_build;

	private ButtonWidget AddButton;

	private ButtonWidget MoreButton;

	private ButtonWidget Cancel;

	private List<Category> m_categories = new List<Category>();

	private List<string> names_item = new List<string>();

	private string selected_item;

	private string name_file;

	private ComponentPlayer _player;

	private SubsystemTerrain _terrain;

	private TerrainRaycastResult _p1;

	private TerrainRaycastResult _p2;

	private TerrainRaycastResult _p3;

	private CheckboxWidget SaveFurnitureCheckbox;

	private FurnitureDesignWidget m_previewWidget;

	private FurnitureDesign m_previewDesign;

	private string _lastPreviewed;

	private string _lastSelectedForPreview;

	private bool _lastIncludeFurniture;

	public static string Path_mod
	{
		get
		{
			FieldInfo field2 = typeof(ModsManager).GetField("ExternelPath", BindingFlags.Static | BindingFlags.Public);
			string text = ((field2 == null) ? "app:" : ((string)field2.GetValue(null)));
			return Storage.CombinePaths(text, "WorldEdit");
		}
	}

	private ComponentPlayer player => _player;

	private TerrainRaycastResult Point1 => _p1;

	private TerrainRaycastResult Point2 => _p2;

	private TerrainRaycastResult Point3 => _p3;

	private SubsystemTerrain subsystemTerrain => _terrain;

	public ZoneDialog(ComponentPlayer player, TerrainRaycastResult Point1, TerrainRaycastResult Point2, TerrainRaycastResult Point3, SubsystemTerrain subsystemTerrain)
	{
		LoadContents(this, ContentManager.Get<XElement>("WE/DialogsWE/ZoneWidget"));
		if (player == null)
		{
			throw new ArgumentNullException("player");
		}
		_player = player;
		if (subsystemTerrain == null)
		{
			throw new ArgumentNullException("subsystemTerrain");
		}
		_terrain = subsystemTerrain;
		_p1 = Point1;
		_p2 = Point2;
		_p3 = Point3;
		list_build = Children.Find<ListPanelWidget>("ListView");
		AddButton = Children.Find<ButtonWidget>("AddButton");
		MoreButton = Children.Find<ButtonWidget>("MoreButton");
		Cancel = Children.Find<ButtonWidget>("Cancel");
		SaveFurnitureCheckbox = Children.Find<CheckboxWidget>("SaveFurnitureCheckbox");
		if (SaveFurnitureCheckbox != null)
		{
			SaveFurnitureCheckbox.IsChecked = true;
		}
		names_item.Clear();
		names_item.Add("Delete");
		names_item.Add("Rename");
		names_item.Add("Paste");
		m_categories.Clear();
		foreach (string item in names_item)
		{
			m_categories.Add(new Category
			{
				Name = item
			});
		}
		MoreButton.IsEnabled = false;
		m_previewWidget = Children.Find<FurnitureDesignWidget>("ZoneDialog.Preview3D");
		m_previewDesign = new FurnitureDesign(_terrain);
		m_previewWidget.Design = m_previewDesign;
		m_previewWidget.Mode = FurnitureDesignWidget.ViewMode.Perspective;
		_lastIncludeFurniture = SaveFurnitureCheckbox == null || SaveFurnitureCheckbox.IsChecked;
		_lastSelectedForPreview = null;
		update_builds();
		RebuildPreviewSelected();
	}

	private void update_builds()
	{
		try
		{
			list_build.ClearItems();
			Storage.CreateDirectory(Path_mod);
			foreach (string item in Storage.ListFileNames(Path_mod))
			{
				if (Path.GetExtension(item) == ".scbuild")
				{
					list_build.AddItem(Path.GetFileName(Storage.CombinePaths(Path_mod, item)));
				}
			}
		}
		catch (Exception message)
		{
			Log.Information("World Edit Path: " + Path_mod);
			Log.Error(message);
		}
	}

	public override void Update()
	{
		selected_item = list_build.SelectedItem as string;
		if (_player == null)
		{
			Log.Error("ZoneDialog._player is null!");
		}
		if (Cancel.IsClicked)
		{
			DialogsManager.HideDialog(this);
		}
		if (MoreButton.IsClicked && _player != null)
		{
			DialogsManager.ShowDialog(_player.GameWidget, new ListSelectionDialog(string.Empty, m_categories, 56f, (object c) => new LabelWidget
			{
				Text = ((Category)c).Name,
				Color = Color.White,
				HorizontalAlignment = WidgetAlignment.Center,
				VerticalAlignment = WidgetAlignment.Center
			}, (object c) =>
			{
				try
				{
					if (c != null)
					{
						string name = ((Category)c).Name;
						if (name == "Delete" && selected_item != null)
						{
							DialogsManager.ShowDialog(_player.GameWidget, new MessageDialog("Warning", "The build will be deleted", "Delete", "Cancel", (MessageDialogButton b) =>
							{
								if (b == MessageDialogButton.Button1)
								{
									Storage.DeleteFile(Path.Combine(Path_mod, selected_item));
									update_builds();
									_lastSelectedForPreview = null;
									RebuildPreviewSelected();
								}
							}));
						}
						else if (name == "Rename" && selected_item != null)
						{
							string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(selected_item);
							DialogsManager.ShowDialog(_player.GameWidget, new TextBoxDialog("Rename build", fileNameWithoutExtension, 128, (string newBase) =>
							{
								if (!string.IsNullOrWhiteSpace(newBase))
								{
									if (TryRenameScbuildFile(selected_item, newBase.Trim(), out var newFileName))
									{
										update_builds();
										list_build.SelectedItem = newFileName;
										_lastSelectedForPreview = null;
										RebuildPreviewSelected();
									}
									else
									{
										DialogsManager.Alert("Rename failed.");
									}
								}
							}));
						}
						else if (name == "Paste")
						{
							if (object.Equals(Point3, default(TerrainRaycastResult)))
							{
								DialogsManager.HideDialog(this);
								_player.ComponentGui.DisplaySmallMessage("You have not selected point 3", Color.White, blinking: false, playNotificationSound: false);
							}
							else
							{
								bool includeFurniture = SaveFurnitureCheckbox != null && SaveFurnitureCheckbox.IsChecked;
								_player.Entity.FindComponent<ComponentWorldEditIntegration>(throwOnError: true).Paste_zone(new Point3(Point3.CellFace.X, Point3.CellFace.Y, Point3.CellFace.Z), subsystemTerrain, _player, Path.Combine(Path_mod, selected_item), includeFurniture);
								DialogsManager.HideDialog(this);
							}
						}
					}
				}
				catch (Exception message)
				{
					Log.Error(message);
				}
			}));
		}
		MoreButton.IsEnabled = list_build.SelectedItem != null;
		if (AddButton.IsClicked)
		{
			if (object.Equals(Point1, default(TerrainRaycastResult)) || object.Equals(Point2, default(TerrainRaycastResult)))
			{
				_player.ComponentGui.DisplaySmallMessage("You have not selected points 1,2", Color.White, blinking: false, playNotificationSound: false);
				DialogsManager.HideDialog(this);
				return;
			}
			DialogsManager.ShowDialog(ParentWidget ?? _player.GameWidget, new TextBoxDialog("Enter build name", "New build", 20, (string s) =>
			{
				try
				{
					if (!string.IsNullOrWhiteSpace(s))
					{
						name_file = s.Trim();
						Point3 point = new Point3(Point1.CellFace.X, Point1.CellFace.Y, Point1.CellFace.Z);
						Point3 point2 = new Point3(Point2.CellFace.X, Point2.CellFace.Y, Point2.CellFace.Z);
						string path = Storage.CombinePaths(Path_mod, name_file + ".scbuild");
						bool includeFurniture = SaveFurnitureCheckbox != null && SaveFurnitureCheckbox.IsChecked;
						_player.Entity.FindComponent<ComponentWorldEditIntegration>(throwOnError: true).Coppy_zone(path, point, point2, subsystemTerrain, _player, includeFurniture);
						update_builds();
						list_build.SelectedItem = name_file + ".scbuild";
						_lastSelectedForPreview = null;
						RebuildPreviewSelected();
					}
				}
				catch (Exception ex)
				{
					DialogsManager.Alert("Save failed: " + ex.Message);
				}
			}));
		}
		bool flag = SaveFurnitureCheckbox == null || SaveFurnitureCheckbox.IsChecked;
		if (selected_item != _lastSelectedForPreview || flag != _lastIncludeFurniture)
		{
			_lastSelectedForPreview = selected_item;
			_lastIncludeFurniture = flag;
			RebuildPreviewSelected();
		}
	}

	private void BuildPreviewFromScbuild(string path, bool includeFurniture)
	{
		List<BlockMem> list = ReadScbuildNormalized(path, includeFurniture);
		if (list.Count == 0)
		{
			m_previewDesign.SetValues(2, new int[8]);
			return;
		}
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].x > num)
			{
				num = list[i].x;
			}
			if (list[i].y > num2)
			{
				num2 = list[i].y;
			}
			if (list[i].z > num3)
			{
				num3 = list[i].z;
			}
		}
		int x = num + 1;
		int x2 = num2 + 1;
		int x3 = num3 + 1;
		int num4 = MathUtils.Max(x, MathUtils.Max(x2, x3));
		int num5 = MathUtils.Max(1, (num4 + 15) / 16);
		int num6 = MathUtils.Clamp((num4 + num5 - 1) / num5, 8, 16);
		int num7 = num6 * num6 * num6;
		int[] array = new int[num7];
		int[] array2 = new int[num7];
		int[] array3 = new int[num7];
		for (int j = 0; j < list.Count; j++)
		{
			int id = list[j].id;
			if (id == 0)
			{
				continue;
			}
			int value = PreviewSanitizeValue(id);
			int num8 = MathUtils.Clamp(list[j].x / num5, 0, num6 - 1);
			int num9 = MathUtils.Clamp(list[j].y / num5, 0, num6 - 1);
			int num10 = MathUtils.Clamp(list[j].z / num5, 0, num6 - 1);
			int num11 = num8 + num9 * num6 + num10 * num6 * num6;
			int num12 = Terrain.ExtractContents(value);
			if (num12 == array2[num11])
			{
				array3[num11]++;
				continue;
			}
			if (array3[num11] == 0)
			{
				array2[num11] = num12;
				array3[num11] = 1;
				continue;
			}
			array3[num11]--;
			if (array3[num11] <= 0)
			{
				array2[num11] = num12;
				array3[num11] = 1;
			}
		}
		for (int k = 0; k < num7; k++)
		{
			int num13 = array2[k];
			array[k] = ((num13 != 0) ? Terrain.MakeBlockValue(num13) : 0);
		}
		m_previewDesign.SetValues(num6, array);
		Point3 location = m_previewDesign.Box.Location;
		Point3 point = new Point3(m_previewDesign.Resolution) - (m_previewDesign.Box.Location + m_previewDesign.Box.Size);
		Point3 delta = new Point3((point.X - location.X) / 2, -location.Y, (point.Z - location.Z) / 2);
		m_previewDesign.Shift(delta);
	}

	private void RebuildPreviewSelected()
	{
		try
		{
			bool includeFurniture = SaveFurnitureCheckbox == null || SaveFurnitureCheckbox.IsChecked;
			if (!string.IsNullOrEmpty(selected_item))
			{
				string path = Storage.CombinePaths(Path_mod, selected_item);
				BuildPreviewFromScbuild(path, includeFurniture);
				return;
			}
			bool flag = !object.Equals(Point1, default(TerrainRaycastResult));
			bool flag2 = !object.Equals(Point2, default(TerrainRaycastResult));
			if (flag & flag2)
			{
				BuildPreviewFromTerrainBox(Point1, Point2, includeFurniture);
			}
			else
			{
				m_previewDesign.SetValues(2, new int[8]);
			}
		}
		catch (Exception message)
		{
			Log.Error(message);
			m_previewDesign.SetValues(2, new int[8]);
		}
	}

	private static List<BlockMem> ReadScbuildNormalized(string path, bool includeFurniture)
	{
		List<BlockMem> list = new List<BlockMem>(4096);
		using (Stream input = Storage.OpenFile(path, OpenFileMode.Read))
		{
			using BinaryReader binaryReader = new BinaryReader(input);
			binaryReader.BaseStream.Position = 7L;
			int num = binaryReader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				int x = binaryReader.ReadInt32();
				int y = binaryReader.ReadInt32();
				int z = binaryReader.ReadInt32();
				int num2 = binaryReader.ReadInt32();
				int num3 = Terrain.ExtractContents(num2);
				if ((includeFurniture || (num3 != 227 && num3 != 366)) && num3 != 0)
				{
					list.Add(new BlockMem
					{
						x = x,
						y = y,
						z = z,
						id = num2
					});
				}
			}
		}
		if (list.Count == 0)
		{
			return list;
		}
		int num4 = int.MaxValue;
		int num5 = int.MaxValue;
		int num6 = int.MaxValue;
		for (int j = 0; j < list.Count; j++)
		{
			if (list[j].x < num4)
			{
				num4 = list[j].x;
			}
			if (list[j].y < num5)
			{
				num5 = list[j].y;
			}
			if (list[j].z < num6)
			{
				num6 = list[j].z;
			}
		}
		for (int k = 0; k < list.Count; k++)
		{
			BlockMem blockMem = list[k];
			blockMem.x -= num4;
			blockMem.y -= num5;
			blockMem.z -= num6;
			list[k] = blockMem;
		}
		return list;
	}

	private static int PreviewSanitizeValue(int terrainValue)
	{
		int num = Terrain.ExtractContents(terrainValue);
		if (num == 0)
		{
			return 0;
		}
		return Terrain.MakeBlockValue(num);
	}

	private void BuildPreviewFromTerrainBox(TerrainRaycastResult p1, TerrainRaycastResult p2, bool includeFurniture)
	{
		int num = MathUtils.Min(p1.CellFace.X, p2.CellFace.X);
		int num2 = MathUtils.Max(p1.CellFace.X, p2.CellFace.X);
		int num3 = MathUtils.Min(p1.CellFace.Y, p2.CellFace.Y);
		int num4 = MathUtils.Max(p1.CellFace.Y, p2.CellFace.Y);
		int num5 = MathUtils.Min(p1.CellFace.Z, p2.CellFace.Z);
		int num6 = MathUtils.Max(p1.CellFace.Z, p2.CellFace.Z);
		int num7 = num2 - num + 1;
		int num8 = num4 - num3 + 1;
		int num9 = num6 - num5 + 1;
		if (num7 <= 0 || num8 <= 0 || num9 <= 0)
		{
			m_previewDesign.SetValues(2, new int[8]);
			return;
		}
		List<BlockMem> list = new List<BlockMem>(num7 * num8 * num9);
		for (int i = num; i <= num2; i++)
		{
			for (int j = num3; j <= num4; j++)
			{
				for (int k = num5; k <= num6; k++)
				{
					int num10 = Terrain.ExtractContents(subsystemTerrain.Terrain.GetCellValueFast(i, j, k));
					if (num10 != 0 && (includeFurniture || (num10 != 227 && num10 != 366)))
					{
						int id = Terrain.MakeBlockValue(num10);
						list.Add(new BlockMem
						{
							x = i - num,
							y = j - num3,
							z = k - num5,
							id = id
						});
					}
				}
			}
		}
		if (list.Count == 0)
		{
			m_previewDesign.SetValues(2, new int[8]);
			return;
		}
		int num11 = MathUtils.Max(num7, MathUtils.Max(num8, num9));
		int num12 = MathUtils.Max(1, (num11 + 15) / 16);
		int num13 = MathUtils.Clamp((num11 + num12 - 1) / num12, 8, 16);
		int num14 = num13 * num13 * num13;
		int[] array = new int[num14];
		int[] array2 = new int[num14];
		int[] array3 = new int[num14];
		for (int l = 0; l < list.Count; l++)
		{
			BlockMem blockMem = list[l];
			int num15 = MathUtils.Clamp(blockMem.x / num12, 0, num13 - 1);
			int num16 = MathUtils.Clamp(blockMem.y / num12, 0, num13 - 1);
			int num17 = MathUtils.Clamp(blockMem.z / num12, 0, num13 - 1);
			int num18 = num15 + num16 * num13 + num17 * num13 * num13;
			int num19 = Terrain.ExtractContents(blockMem.id);
			if (num19 == array2[num18])
			{
				array3[num18]++;
				continue;
			}
			if (array3[num18] == 0)
			{
				array2[num18] = num19;
				array3[num18] = 1;
				continue;
			}
			array3[num18]--;
			if (array3[num18] <= 0)
			{
				array2[num18] = num19;
				array3[num18] = 1;
			}
		}
		for (int m = 0; m < num14; m++)
		{
			int num20 = array2[m];
			array[m] = ((num20 != 0) ? Terrain.MakeBlockValue(num20) : 0);
		}
		m_previewDesign.SetValues(num13, array);
		Point3 location = m_previewDesign.Box.Location;
		Point3 point = new Point3(m_previewDesign.Resolution) - (m_previewDesign.Box.Location + m_previewDesign.Box.Size);
		Point3 delta = new Point3((point.X - location.X) / 2, -location.Y, (point.Z - location.Z) / 2);
		m_previewDesign.Shift(delta);
	}

	private bool TryRenameScbuildFile(string oldName, string newBaseName, out string newFileName)
	{
		newFileName = null;
		try
		{
			char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
			foreach (char oldChar in invalidFileNameChars)
			{
				newBaseName = newBaseName.Replace(oldChar, '_');
			}
			string text = ".scbuild";
			string path = Storage.CombinePaths(Path_mod, oldName);
			string text2 = newBaseName + text;
			string path2 = Storage.CombinePaths(Path_mod, text2);
			int num = 1;
			while (Storage.FileExists(path2))
			{
				text2 = newBaseName + "_" + num + text;
				path2 = Storage.CombinePaths(Path_mod, text2);
				num++;
			}
			using (Stream stream = Storage.OpenFile(path, OpenFileMode.Read))
			{
				using Stream destination = Storage.OpenFile(path2, OpenFileMode.Create);
				stream.CopyTo(destination);
			}
			Storage.DeleteFile(path);
			newFileName = text2;
			return true;
		}
		catch
		{
			return false;
		}
	}
}
