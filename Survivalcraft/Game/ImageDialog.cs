using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;
using Engine;
using Engine.Media;

namespace Game;

public class ImageDialog : Dialog
{
	private ListPanelWidget list_build;

	private ButtonWidget AddButton;

	private ButtonWidget MoreButton;

	private ButtonWidget Cancel;

	private new LabelWidget Title;

	public static readonly string Path_img;

	private string selected_item;

	private List<Category> m_categories;

	private List<string> names_item;

	private ComponentWorldEditIntegration we;

	private ComponentPlayer player;

	private SubsystemTerrain m_terrain;

	private FurnitureDesignWidget m_previewWidget;

	private FurnitureDesign m_previewDesign;

	private string _lastSelected;

	public ImageWidget m_imageWidget;

	public Action<string> OnPickProfilePhoto;

	private void update_builds()
	{
		list_build.ClearItems();
		Storage.CreateDirectory(Path_img);
		foreach (string item in Storage.ListFileNames(Path_img))
		{
			switch (Path.GetExtension(item).ToLowerInvariant())
			{
			case ".jpg":
			case ".jpeg":
			case ".png":
				list_build.AddItem(Path.GetFileName(item));
				break;
			}
		}
	}

	public override void Update()
	{
		selected_item = list_build.SelectedItem as string;
		if (Cancel.IsClicked)
		{
			DialogsManager.HideDialog(this);
			return;
		}
		MoreButton.IsEnabled = list_build.SelectedItem != null;
		if (MoreButton.IsClicked)
		{
			List<string> num4 = new List<string>();
			if (OnPickProfilePhoto != null)
			{
				num4.Add("Use");
			}
			else
			{
				num4.Add("Build");
			}
			num4.Add("Rename");
			num4.Add("Delete");
			DialogsManager.ShowDialog(null, new ListSelectionDialog(string.Empty, num4, 56f, (object c) => new LabelWidget
			{
				Text = c.ToString(),
				Color = Engine.Color.White,
				HorizontalAlignment = WidgetAlignment.Center,
				VerticalAlignment = WidgetAlignment.Center
			}, (object c) =>
			{
				if (c != null && !string.IsNullOrEmpty(selected_item))
				{
					string text = c.ToString();
					if (text == "Delete")
					{
						DialogsManager.ShowDialog(null, new MessageDialog("Warning", "Image will be deleted", "Delete", "Cancel", (MessageDialogButton b) =>
						{
							if (b == MessageDialogButton.Button1)
							{
								Storage.DeleteFile(Storage.CombinePaths(Path_img, selected_item));
								update_builds();
								_lastSelected = null;
								RebuildPreviewSelected();
							}
						}));
					}
					else if (text == "Rename")
					{
						string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(selected_item);
						DialogsManager.ShowDialog(null, new TextBoxDialog("Rename file", fileNameWithoutExtension, 128, (string newBase) =>
						{
							if (!string.IsNullOrWhiteSpace(newBase))
							{
								if (TryRenameFileInFolder(Path_img, selected_item, newBase.Trim(), out var newFileName))
								{
									update_builds();
									list_build.SelectedItem = newFileName;
									_lastSelected = null;
									RebuildPreviewSelected();
								}
								else
								{
									DialogsManager.Alert("Rename failed.");
								}
							}
						}));
					}
					else
					{
						string text2 = Storage.CombinePaths(Path_img, selected_item);
						if (text == "Use" && OnPickProfilePhoto != null)
						{
							OnPickProfilePhoto(text2);
							DialogsManager.HideDialog(this);
						}
						else if (text == "Build" && we != null && m_terrain != null)
						{
							DialogsManager.ShowDialog(null, new Img(player, we, m_terrain, text2));
							DialogsManager.HideDialog(this);
						}
					}
				}
			}));
		}
		if (AddButton.IsClicked)
		{
			ShowAddSourceDialog();
		}
		if (selected_item != _lastSelected)
		{
			_lastSelected = selected_item;
			RebuildPreviewSelected();
		}
	}

	public static string UnclutterLink(string address)
	{
		try
		{
			string text = address;
			int num = text.IndexOf('&');
			if (num > 0)
			{
				text = text.Remove(num);
			}
			int num2 = text.IndexOf('?');
			if (num2 > 0)
			{
				text = text.Remove(num2);
			}
			return Uri.UnescapeDataString(text);
		}
		catch (Exception)
		{
			return string.Empty;
		}
	}

	public static string GetNameFromLink(string address)
	{
		try
		{
			string fileNameWithoutExtension = Storage.GetFileNameWithoutExtension(UnclutterLink(address));
			if (!string.IsNullOrEmpty(fileNameWithoutExtension) && fileNameWithoutExtension.IndexOfAny(Path.GetInvalidFileNameChars()) < 0)
			{
				return fileNameWithoutExtension;
			}
			return "";
		}
		catch (Exception)
		{
			return "";
		}
	}

	static ImageDialog()
	{
		Path_img = Storage.CombinePaths(ZoneDialog.Path_mod, "img");
	}

	public ImageDialog(ComponentPlayer player, ComponentWorldEditIntegration we)
	{
		this.player = player;
		this.we = we;
		LoadContents(this, ContentManager.Get<XElement>("WE/DialogsWE/ImageZoneWidget"));
		m_imageWidget = Children.Find<ImageWidget>("ImageDialog.Preview2D", throwIfNotFound: false);
		if (m_imageWidget != null)
		{
			m_imageWidget.ConfigureFilter(quantize16: false, 1, 0, null);
		}
		list_build = Children.Find<ListPanelWidget>("ListView");
		AddButton = Children.Find<ButtonWidget>("AddButton");
		MoreButton = Children.Find<ButtonWidget>("MoreButton");
		Cancel = Children.Find<ButtonWidget>("Cancel");
		Title = Children.Find<LabelWidget>("Dialog.Title");
		Title.Text = "Image in SC";
		names_item = new List<string> { "Delete", "Rename", "Build" };
		m_categories = new List<Category>();
		foreach (string item in names_item)
		{
			m_categories.Add(new Category
			{
				Name = item
			});
		}
		MoreButton.IsEnabled = false;
		update_builds();
	}

	public ImageDialog(ComponentPlayer player, ComponentWorldEditIntegration we, SubsystemTerrain terrain)
	{
		this.player = player;
		this.we = we;
		m_terrain = terrain;
		LoadContents(this, ContentManager.Get<XElement>("WE/DialogsWE/ImageZoneWidget"));
		list_build = Children.Find<ListPanelWidget>("ListView");
		AddButton = Children.Find<ButtonWidget>("AddButton");
		MoreButton = Children.Find<ButtonWidget>("MoreButton");
		Cancel = Children.Find<ButtonWidget>("Cancel");
		Title = Children.Find<LabelWidget>("Dialog.Title", throwIfNotFound: false);
		if (Title != null)
		{
			Title.Text = "Image in SC";
		}
		m_imageWidget = Children.Find<ImageWidget>("ImageDialog.Preview2D");
		if (m_imageWidget != null)
		{
			m_imageWidget.ConfigureFilter(quantize16: false, 1, 0, null);
		}
		names_item = new List<string> { "Delete", "Rename", "Build" };
		m_categories = new List<Category>();
		foreach (string item in names_item)
		{
			m_categories.Add(new Category
			{
				Name = item
			});
		}
		MoreButton.IsEnabled = false;
		_lastSelected = null;
		update_builds();
		RebuildPreviewSelected();
	}

	private void BuildPreviewFromImage(string path)
	{
		Engine.Media.Image image = null;
		try
		{
			image = Engine.Media.Image.Load(path);
		}
		catch
		{
			m_previewDesign.SetValues(2, new int[8]);
			return;
		}
		int num = Math.Max(1, image.Width / 2);
		int num2 = Math.Max(1, image.Height / 2);
		int num3 = ((MathUtils.Max(num, num2) <= 8) ? 8 : 16);
		int[] array = new int[16]
		{
			16456, 49224, 81992, 114760, 147528, 180296, 213064, 245832, 278600, 311368,
			344136, 376904, 409672, 442440, 475208, 507976
		};
		Engine.Color[] colors = m_terrain.SubsystemPalette.m_colors;
		int[] array2 = new int[num3 * num3 * num3];
		for (int i = 0; i < num3; i++)
		{
			int num4 = MathUtils.Clamp(i * num2 / num3, 0, num2 - 1);
			int y = Math.Min(image.Height - 1, num4 * 2);
			for (int j = 0; j < num3; j++)
			{
				int num5 = MathUtils.Clamp(j * num / num3, 0, num - 1);
				int x = Math.Min(image.Width - 1, num5 * 2);
				Engine.Color pixel = image.GetPixel(x, y);
				if (pixel.A == 0)
				{
					continue;
				}
				int x2 = 0;
				int num6 = int.MaxValue;
				for (int k = 0; k < colors.Length && k < array.Length; k++)
				{
					byte b = (byte)(colors[k].R - pixel.R);
					int num7 = colors[k].G - pixel.G;
					int num8 = colors[k].B - pixel.B;
					int num9 = colors[k].A - pixel.A;
					int num10 = b * b + num7 * num7 + num8 * num8 + num9 * num9;
					if (num10 < num6)
					{
						num6 = num10;
						x2 = k;
					}
				}
				int num11 = array[MathUtils.Clamp(x2, 0, array.Length - 1)];
				int num12 = j + i * num3 * num3;
				array2[num12] = num11;
			}
		}
		m_previewDesign.SetValues(num3, array2);
		Point3 location = m_previewDesign.Box.Location;
		Point3 point = new Point3(m_previewDesign.Resolution) - (m_previewDesign.Box.Location + m_previewDesign.Box.Size);
		Point3 delta = new Point3((point.X - location.X) / 2, -location.Y, (point.Z - location.Z) / 2);
		m_previewDesign.Shift(delta);
	}

	private void RebuildPreviewSelected()
	{
		try
		{
			if (!string.IsNullOrEmpty(selected_item))
			{
				string text = Storage.CombinePaths(Path_img, selected_item);
				if (m_imageWidget != null)
				{
					if (!string.Equals(m_imageWidget.SourcePath, text, StringComparison.OrdinalIgnoreCase))
					{
						m_imageWidget.SetSource(text);
					}
					m_imageWidget.ConfigureFilter(quantize16: false, 1, 0, null);
				}
			}
			else if (m_imageWidget != null)
			{
				m_imageWidget.SetSource(null);
				m_imageWidget.ConfigureFilter(quantize16: false, 1, 0, null);
			}
		}
		catch (Exception message)
		{
			Log.Error(message);
			if (m_imageWidget != null)
			{
				m_imageWidget.SetSource(null);
				m_imageWidget.ConfigureFilter(quantize16: false, 1, 0, null);
			}
		}
	}

	public void ShowAddSourceDialog()
	{
		ContainerWidget owner = ((player != null) ? player.GameWidget : null);
		List<string> items = new List<string> { "URL", "External Content", "Paste from Clipboard" };
		Func<object, Widget> itemWidgetFactory = (object c) => new LabelWidget
		{
			Text = c.ToString(),
			Color = Engine.Color.White,
			HorizontalAlignment = WidgetAlignment.Center,
			VerticalAlignment = WidgetAlignment.Center
		};
		Action<object> selectionHandler = (object c) =>
		{
			if (c != null)
			{
				switch (c.ToString())
				{
				case "URL":
					DialogsManager.ShowDialog(owner, new TextBoxDialog("Enter image URL or data: URI", "", 262144, (string url) =>
					{
						if (string.IsNullOrWhiteSpace(url))
						{
							return;
						}
						try
						{
							if (TryImportFromUrl(url.Trim(), out var savedFileName3))
							{
								update_builds();
								list_build.SelectedItem = savedFileName3;
								RebuildPreviewSelected();
							}
							else
							{
								DialogsManager.Alert("Download failed");
							}
						}
						catch (Exception ex3)
						{
							DialogsManager.Alert("Error: " + ex3.Message);
						}
					}));
					break;
				case "External Content":
					try
					{
						if (TryImportFromExternalAny(out var savedFileName2))
						{
							update_builds();
							list_build.SelectedItem = savedFileName2;
							RebuildPreviewSelected();
						}
						else
						{
							DialogsManager.Alert("Not supported on this platform.");
						}
						break;
					}
					catch (Exception ex2)
					{
						DialogsManager.Alert("Open failed: " + ex2.Message);
						break;
					}
				case "Paste from Clipboard":
					try
					{
						if (TryImportFromClipboard(out var savedFileName))
						{
							update_builds();
							list_build.SelectedItem = savedFileName;
							RebuildPreviewSelected();
						}
						else
						{
							DialogsManager.Alert("Clipboard does not contain image or image URL/data URI.");
						}
						break;
					}
					catch (Exception ex)
					{
						DialogsManager.Alert("Paste failed: " + ex.Message);
						break;
					}
				}
			}
		};
		DialogsManager.ShowDialog(owner, new ListSelectionDialog(string.Empty, items, 56f, itemWidgetFactory, selectionHandler));
	}

	private bool TryImportFromUrl(string url, out string savedFileName)
	{
		savedFileName = null;
		url = url.Trim();
		if (url.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
		{
			return TryImportFromDataUri(url, out savedFileName);
		}
		string text = GetNameFromLink(url);
		if (string.IsNullOrEmpty(text))
		{
			text = "img_" + DateTime.UtcNow.Ticks;
		}
		string text2 = Path.GetExtension(url);
		if (string.IsNullOrEmpty(text2))
		{
			text2 = ".png";
		}
		text2 = text2.ToLowerInvariant();
		if (text2 != ".png" && text2 != ".jpg" && text2 != ".jpeg")
		{
			text2 = ".png";
		}
		string text3 = text + text2;
		string path = Storage.CombinePaths(Path_img, text3);
		using (WebClient webClient = new WebClient())
		{
			byte[] array = webClient.DownloadData(url);
			using Stream stream = Storage.OpenFile(path, OpenFileMode.Create);
			stream.Write(array, 0, array.Length);
		}
		savedFileName = text3;
		return true;
	}

	private bool TryImportFromDataUri(string dataUri, out string savedFileName)
	{
		savedFileName = null;
		int num = dataUri.IndexOf(',');
		if (num < 0)
		{
			return false;
		}
		string text = dataUri.Substring(0, num);
		string s = dataUri.Substring(num + 1);
		string text2 = ".png";
		if (text.IndexOf("image/jpeg", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			text2 = ".jpg";
		}
		byte[] array = Convert.FromBase64String(s);
		string text3 = "img_" + DateTime.UtcNow.Ticks + text2;
		using (Stream stream = Storage.OpenFile(Storage.CombinePaths(Path_img, text3), OpenFileMode.Create))
		{
			stream.Write(array, 0, array.Length);
		}
		savedFileName = text3;
		return true;
	}

	private bool TryImportFromExternalWindows(out string savedFileName)
	{
		savedFileName = null;
		OpenFileDialog openFileDialog = new OpenFileDialog();
		openFileDialog.Filter = "Images|*.png;*.jpg;*.jpeg|All|*.*";
		openFileDialog.Multiselect = false;
		if (openFileDialog.ShowDialog() != DialogResult.OK)
		{
			return false;
		}
		string fileName = openFileDialog.FileName;
		string text = Path.GetExtension(fileName).ToLowerInvariant();
		if (text != ".png" && text != ".jpg" && text != ".jpeg")
		{
			return false;
		}
		string text2 = Path.GetFileNameWithoutExtension(fileName);
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = "img_" + DateTime.UtcNow.Ticks;
		}
		string text3 = text2 + text;
		string path = Storage.CombinePaths(Path_img, text3);
		int num = 1;
		while (Storage.FileExists(path))
		{
			text3 = text2 + "_" + num + text;
			path = Storage.CombinePaths(Path_img, text3);
			num++;
		}
		using (Stream stream = File.OpenRead(fileName))
		{
			using Stream destination = Storage.OpenFile(path, OpenFileMode.Create);
			stream.CopyTo(destination);
		}
		savedFileName = text3;
		return true;
	}

	private bool TryImportFromClipboard(out string savedFileName)
	{
		savedFileName = null;
		try
		{
			string clipboardString = ClipboardManager.ClipboardString;
			if (!string.IsNullOrWhiteSpace(clipboardString))
			{
				clipboardString = clipboardString.Trim();
				if (clipboardString.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || clipboardString.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
				{
					return TryImportFromUrl(clipboardString, out savedFileName);
				}
				if (clipboardString.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
				{
					return TryImportFromDataUri(clipboardString, out savedFileName);
				}
			}
		}
		catch
		{
		}
		try
		{
			if (TryImportFromClipboardImageWindows(out savedFileName))
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private bool TryImportFromClipboardImageWindows(out string savedFileName)
	{
		savedFileName = null;
		System.Drawing.Image grabbed = null;
		bool hasImage = false;
		Thread thread = new Thread(() =>
		{
			try
			{
				if (Clipboard.ContainsImage())
				{
					System.Drawing.Image image = Clipboard.GetImage();
					if (image != null)
					{
						grabbed = (System.Drawing.Image)image.Clone();
						hasImage = true;
					}
				}
			}
			catch
			{
			}
		});
		thread.IsBackground = true;
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		thread.Join();
		if (!hasImage || grabbed == null)
		{
			return false;
		}
		try
		{
			string text = "img_" + DateTime.UtcNow.Ticks + ".png";
			string path = Storage.CombinePaths(Path_img, text);
			using (MemoryStream memoryStream = new MemoryStream())
			{
				grabbed.Save(memoryStream, ImageFormat.Png);
				byte[] array = memoryStream.ToArray();
				using Stream stream = Storage.OpenFile(path, OpenFileMode.Create);
				stream.Write(array, 0, array.Length);
			}
			savedFileName = text;
			return true;
		}
		catch
		{
			return false;
		}
		finally
		{
			try
			{
				if (grabbed != null)
				{
					grabbed.Dispose();
				}
			}
			catch
			{
			}
		}
	}

	private bool TryImportFromExternalAny(out string savedFileName)
	{
		savedFileName = null;
		try
		{
			if (TryImportFromExternalWindows_Reflect(out savedFileName))
			{
				return true;
			}
		}
		catch
		{
		}
		return TryImportFromExternalByBrowsing(out savedFileName);
	}

	private bool TryImportFromExternalByBrowsing(out string savedFileName)
	{
		savedFileName = null;
		string text = null;
		try
		{
			FieldInfo field = typeof(ModsManager).GetField("ExternelPath", BindingFlags.Static | BindingFlags.Public);
			if (field != null)
			{
				text = field.GetValue(null) as string;
			}
		}
		catch
		{
		}
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		List<string> list = new List<string>();
		CollectImagesRecursiveSafe(text, list, 0, 4);
		if (list.Count == 0)
		{
			return false;
		}
		string text2 = null;
		DateTime dateTime = DateTime.MinValue;
		foreach (string item in list)
		{
			try
			{
				DateTime lastWriteTimeUtc = File.GetLastWriteTimeUtc(item);
				if (lastWriteTimeUtc > dateTime)
				{
					dateTime = lastWriteTimeUtc;
					text2 = item;
				}
			}
			catch
			{
			}
		}
		if (string.IsNullOrEmpty(text2))
		{
			return false;
		}
		string text3 = Path.GetExtension(text2).ToLowerInvariant();
		if (text3 != ".png" && text3 != ".jpg" && text3 != ".jpeg")
		{
			return false;
		}
		string text4 = Path.GetFileNameWithoutExtension(text2);
		if (string.IsNullOrWhiteSpace(text4))
		{
			text4 = "img_" + DateTime.UtcNow.Ticks;
		}
		string text5 = text4 + text3;
		string path = Storage.CombinePaths(Path_img, text5);
		int num = 1;
		while (Storage.FileExists(path))
		{
			text5 = text4 + "_" + num + text3;
			path = Storage.CombinePaths(Path_img, text5);
			num++;
		}
		try
		{
			using (Stream stream = File.OpenRead(text2))
			{
				using Stream destination = Storage.OpenFile(path, OpenFileMode.Create);
				stream.CopyTo(destination);
			}
			savedFileName = text5;
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static void CollectImagesRecursiveSafe(string root, List<string> files, int depth, int maxDepth)
	{
		if (depth > maxDepth)
		{
			return;
		}
		try
		{
			string[] files2 = Directory.GetFiles(root);
			foreach (string text in files2)
			{
				switch (Path.GetExtension(text).ToLowerInvariant())
				{
				case ".png":
				case ".jpg":
				case ".jpeg":
					files.Add(text);
					break;
				}
			}
		}
		catch
		{
		}
		try
		{
			string[] directories = Directory.GetDirectories(root);
			for (int j = 0; j < directories.Length; j++)
			{
				CollectImagesRecursiveSafe(directories[j], files, depth + 1, maxDepth);
			}
		}
		catch
		{
		}
	}

	private bool TryRenameFileInFolder(string folder, string oldName, string newBaseName, out string newFileName)
	{
		newFileName = null;
		try
		{
			char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
			foreach (char oldChar in invalidFileNameChars)
			{
				newBaseName = newBaseName.Replace(oldChar, '_');
			}
			string text = Path.GetExtension(oldName);
			if (string.IsNullOrEmpty(text))
			{
				text = ".png";
			}
			string path = Storage.CombinePaths(folder, oldName);
			string text2 = newBaseName + text;
			string path2 = Storage.CombinePaths(folder, text2);
			int num = 1;
			while (Storage.FileExists(path2))
			{
				text2 = newBaseName + "_" + num + text;
				path2 = Storage.CombinePaths(folder, text2);
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

	private bool TryImportFromExternalWindows_Reflect(out string savedFileName)
	{
		savedFileName = null;
		PlatformID platform = Environment.OSVersion.Platform;
		if (platform != PlatformID.Win32NT && platform != PlatformID.Win32Windows && platform != PlatformID.Win32S && platform != PlatformID.WinCE)
		{
			return false;
		}
		try
		{
			Assembly assembly = null;
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (Assembly assembly2 in assemblies)
			{
				if (assembly2.GetName().Name == "System.Windows.Forms")
				{
					assembly = assembly2;
					break;
				}
			}
			if (assembly == null)
			{
				try
				{
					assembly = Assembly.Load("System.Windows.Forms");
				}
				catch
				{
				}
			}
			if (assembly == null)
			{
				return false;
			}
			Type tOpenFileDialog = assembly.GetType("System.Windows.Forms.OpenFileDialog", throwOnError: false);
			Type type = assembly.GetType("System.Windows.Forms.DialogResult", throwOnError: false);
			Type tApplication = assembly.GetType("System.Windows.Forms.Application", throwOnError: false);
			if (tOpenFileDialog == null || type == null)
			{
				return false;
			}
			string pickedFile = null;
			object drOK = Enum.Parse(type, "OK");
			Thread thread = new Thread(() =>
			{
				try
				{
					try
					{
						if (tApplication != null)
						{
							MethodInfo method = tApplication.GetMethod("EnableVisualStyles", BindingFlags.Static | BindingFlags.Public);
							if (method != null)
							{
								method.Invoke(null, null);
							}
						}
					}
					catch
					{
					}
					object obj4 = Activator.CreateInstance(tOpenFileDialog);
					PropertyInfo property = tOpenFileDialog.GetProperty("Filter");
					if (property != null)
					{
						property.SetValue(obj4, "Images|*.png;*.jpg;*.jpeg|All|*.*", null);
					}
					PropertyInfo property2 = tOpenFileDialog.GetProperty("Multiselect");
					if (property2 != null)
					{
						property2.SetValue(obj4, false, null);
					}
					try
					{
						string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
						if (string.IsNullOrEmpty(folderPath))
						{
							folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
						}
						PropertyInfo property3 = tOpenFileDialog.GetProperty("InitialDirectory");
						if (property3 != null && !string.IsNullOrEmpty(folderPath))
						{
							property3.SetValue(obj4, folderPath, null);
						}
					}
					catch
					{
					}
					MethodInfo method2 = tOpenFileDialog.GetMethod("ShowDialog", Type.EmptyTypes);
					if (object.Equals((method2 != null) ? method2.Invoke(obj4, null) : null, drOK))
					{
						PropertyInfo property4 = tOpenFileDialog.GetProperty("FileName");
						pickedFile = (string)((property4 != null) ? property4.GetValue(obj4, null) : null);
					}
				}
				catch
				{
				}
			});
			thread.IsBackground = true;
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
			thread.Join();
			if (string.IsNullOrEmpty(pickedFile))
			{
				return false;
			}
			string text = Path.GetExtension(pickedFile).ToLowerInvariant();
			if (text != ".png" && text != ".jpg" && text != ".jpeg")
			{
				return false;
			}
			string text2 = Path.GetFileNameWithoutExtension(pickedFile);
			if (string.IsNullOrWhiteSpace(text2))
			{
				text2 = "img_" + DateTime.UtcNow.Ticks;
			}
			string text3 = text2 + text;
			string path = Storage.CombinePaths(Path_img, text3);
			int num = 1;
			while (Storage.FileExists(path))
			{
				text3 = text2 + "_" + num + text;
				path = Storage.CombinePaths(Path_img, text3);
				num++;
			}
			using (Stream stream = File.OpenRead(pickedFile))
			{
				using Stream destination = Storage.OpenFile(path, OpenFileMode.Create);
				stream.CopyTo(destination);
			}
			savedFileName = text3;
			return true;
		}
		catch
		{
			return false;
		}
	}
}
