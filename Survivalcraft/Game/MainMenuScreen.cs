using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using Engine;
using Engine.Input;
using Game.NetWork;

namespace Game;

public class MainMenuScreen : Screen
{
	public enum UpdateState
	{
		Idle,
		Checking,
		UpdateAvailable,
		Error,
		UpToDate
	}

	public int m_tapsCount;

	private static bool isVersionChecked;

	private static bool isVersionOutdated;

	private bool isVersionCheckInProgress;

	private StackPanelWidget _leftMenu;

	private StackPanelWidget _rightBottom;

	private Vector2 _leftMenuBaseMargin = new Vector2(20f, 250f);

	private Vector2 _rightBottomBaseMargin = new Vector2(10f, 10f);

	private RectangleWidget _logo;

	private Vector2 _logoBaseSize;

	private Vector2 _baseScreenSize;

	private CanvasWidget _updateContainer;

	private LabelWidget _updateLabel;

	public static UpdateState CurrentUpdateState;

	private static string LastCheckResult;

	private double _dotsTimer;

	private static bool DialogShownOnce;

	private LabelWidget _modVersionLabel;

	private RectangleWidget _updateClickArea;

	private RectangleWidget _logClickArea;

	private Vector2 _lastScreenSize;

	private string _lastLogoKey;

	private ButtonWidget _wikiButton;

	private StackPanelWidget _iconsMidRow;

	public MainMenuScreen()
	{
		XElement node = ContentManager.Get<XElement>("Screens/MainMenuScreen");
		LoadContents(this, node);
		_leftMenu = Children.Find<StackPanelWidget>("LeftMenu");
		_rightBottom = Children.Find<StackPanelWidget>("RightBottom");
		_logo = Children.Find<RectangleWidget>("Logo");
		_updateContainer = Children.Find<CanvasWidget>("UpdateButtonContainer");
		_updateLabel = Children.Find<LabelWidget>("UpdateLabel");
		_updateClickArea = Children.Find<RectangleWidget>("UpdateClickArea");
		_logClickArea = Children.Find<RectangleWidget>("LogClickArea");
		_modVersionLabel = Children.Find<LabelWidget>("ModVersion");
		_wikiButton = Children.Find<ButtonWidget>("WikiButton");
		_iconsMidRow = Children.Find<StackPanelWidget>("IconsMidRow");
		if (_leftMenu != null)
		{
			_leftMenuBaseMargin = _leftMenu.Margin;
		}
		if (_rightBottom != null)
		{
			_rightBottomBaseMargin = _rightBottom.Margin;
		}
		if (_logo != null)
		{
			_logoBaseSize = _logo.Size;
		}
	}

	public override void Enter(object[] parameters)
	{
		_lastScreenSize = Vector2.Zero;
		_lastLogoKey = null;
		UpdateLayoutAndLogo();
		if (SettingsManager.IsolatedStorageMigrationCounter < 3)
		{
			SettingsManager.IsolatedStorageMigrationCounter++;
			VersionConverter126To127.MigrateDataFromIsolatedStorageWithDialog();
		}
		if (CommonLib.Net.CurrentStage == NetNode.Stage.Connected)
		{
			CommonLib.Net.Stop();
		}
		UpdateVersionUI();
		if (CurrentUpdateState == UpdateState.UpdateAvailable && !DialogShownOnce)
		{
			DialogsManager.ShowDialog(null, new VersionInfoDialog());
			DialogShownOnce = true;
		}
	}

	public override void Leave()
	{
		Keyboard.BackButtonQuitsApp = false;
	}

	public override void Update()
	{
		if (CurrentUpdateState == UpdateState.UpdateAvailable && !DialogShownOnce)
		{
			DialogsManager.ShowDialog(null, new VersionInfoDialog());
			DialogShownOnce = true;
		}
		UpdateLayoutAndLogo();
		UpdateVersionUI();
		if (_updateClickArea != null && Input.Tap.HasValue && _updateClickArea.HitTest(Input.Tap.Value))
		{
			if (CurrentUpdateState == UpdateState.UpdateAvailable)
			{
				DialogsManager.ShowDialog(null, new VersionInfoDialog());
			}
			else if (CurrentUpdateState != UpdateState.Checking)
			{
				StartVersionCheck(isSilent: false);
			}
		}
		if (_logClickArea != null && Input.Tap.HasValue && _logClickArea.HitTest(Input.Tap.Value))
		{
			DialogsManager.ShowDialog(null, new ViewGameLogDialog());
		}
		if (Children.Find<ButtonWidget>("FocalorsInfoButton").IsClicked)
		{
			DialogsManager.ShowDialog(null, new FocalorsInfoDialog());
		}
		if (Children.Find<ButtonWidget>("WEInfoButton").IsClicked)
		{
			DialogsManager.ShowDialog(null, new WEInfoDialog());
		}
		if (Children.Find<ButtonWidget>("SinglePlayer").IsClicked)
		{
			ScreensManager.SwitchScreen("Play");
		}
		if (Children.Find<ButtonWidget>("Online").IsClicked)
		{
			ScreensManager.SwitchScreen("NetPlay");
		}
		if (Children.Find<ButtonWidget>("Help").IsClicked)
		{
			ScreensManager.SwitchScreen("Help");
		}
		if (Children.Find<ButtonWidget>("Content").IsClicked)
		{
			ScreensManager.SwitchScreen("Content");
		}
		if (Children.Find<ButtonWidget>("Settings").IsClicked)
		{
			ScreensManager.SwitchScreen("Settings");
		}
		if (Children.Find<ButtonWidget>("PlayerID").IsClicked)
		{
			DialogsManager.ShowDialog(null, new ManipulateOfflineTokenDialog());
		}
		if (Children.Find<ButtonWidget>("KnowHowInfo").IsClicked)
		{
			DialogsManager.ShowDialog(null, new KnowHowInfoDialog());
		}
		if (Children.Find<ButtonWidget>("LanguageMainButton").IsClicked)
		{
			ShowLanguageDialog();
		}
		if (_wikiButton.IsClicked)
		{
			string largeMessage = LanguageControl.Get("MainMenuScreen", "Wiki_Title") ?? "Wiki";
			string smallMessage = LanguageControl.Get("MainMenuScreen", "Wiki_Confirm") ?? "Вы хотите перейти на внешнюю страницу Википедии?";
			DialogsManager.ShowDialog(null, new MessageDialog(largeMessage, smallMessage, LanguageControl.Yes, LanguageControl.No, (MessageDialogButton button) =>
			{
				if (button == MessageDialogButton.Button1)
				{
					WebBrowserManager.LaunchBrowser("https://echoellihuma.miraheze.org/wiki/Main_Page");
				}
			}));
		}
		if ((Input.Back && !Keyboard.BackButtonQuitsApp) || Input.IsKeyDownOnce(Key.Escape) || Children.Find<ButtonWidget>("Exit").IsClicked)
		{
			Window.Close();
		}
	}

	private async Task<string> DownloadVersionFromGoogleDriveAsync()
	{
		string num3;
		try
		{
			string v = "https://drive.google.com/uc?export=download&id=1HxbTd5VdkSa298TS_DI1sCJog5cg-Ei6&random=" + Guid.NewGuid();
			string num2 = "https://gitee.com/clear-coagulation_1_0/echo-mod/releases/download/MOD/version.txt?random=" + Guid.NewGuid();
			if (SettingsManager.TranslationProviderState != 0)
			{
				num3 = ((SettingsManager.TranslationProviderState != 1) ? (await DownloadVersionFromLink(num2)) : (await DownloadVersionFromLink(v)));
			}
			else
			{
				Task<string> googleTask = DownloadVersionFromLink(v);
				Task<string> giteeTask = DownloadVersionFromLink(num2);
				Task<string> num4 = await Task.WhenAny<string>(googleTask, giteeTask);
				if (num4.Status == TaskStatus.RanToCompletion && num4.Result != null)
				{
					SettingsManager.TranslationProviderState = ((num4 == googleTask) ? 1 : 2);
					num3 = num4.Result;
				}
				else
				{
					Task<string> otherTask = ((num4 == googleTask) ? giteeTask : googleTask);
					string text = await otherTask;
					if (text != null)
					{
						SettingsManager.TranslationProviderState = ((otherTask == googleTask) ? 1 : 2);
					}
					num3 = text;
				}
			}
		}
		catch
		{
			num3 = null;
		}
		return num3;
	}

	private async Task<string> DownloadVersionFromLink(string uri)
	{
		string ray;
		try
		{
			using HttpClient client = new HttpClient();
			client.Timeout = TimeSpan.FromSeconds(5.0);
			HttpResponseMessage httpResponseMessage = await client.GetAsync(uri);
			httpResponseMessage.EnsureSuccessStatusCode();
			ray = (await httpResponseMessage.Content.ReadAsStringAsync()).Trim();
		}
		catch
		{
			ray = null;
		}
		return ray;
	}

	private void EnsureLogoInside(Vector2 actualSize)
	{
		if (_logo == null)
		{
			return;
		}
		if (_baseScreenSize.X <= 0f || _baseScreenSize.Y <= 0f)
		{
			_baseScreenSize = actualSize;
		}
		float x = actualSize.X / _baseScreenSize.X;
		float x2 = actualSize.Y / _baseScreenSize.Y;
		float x3 = MathUtils.Min(x, x2);
		x3 = MathUtils.Clamp(x3, 0.5f, 2f);
		if (_leftMenu != null)
		{
			float y = _logo.Margin.Y;
			float num = _logoBaseSize.Y * x3;
			float num2 = y + num;
			float num3 = _leftMenu.Margin.Y - 10f;
			if (num2 > num3)
			{
				float x4 = MathUtils.Max(num3 - y, 10f) / _logoBaseSize.Y;
				x3 = MathUtils.Min(x3, x4);
			}
		}
		_logo.Size = _logoBaseSize * x3;
	}

	private void ShowLanguageDialog()
	{
		List<string> languageTypes = LanguageControl.LanguageTypes;
		DialogsManager.ShowDialog(null, new ListSelectionDialog(LanguageControl.Get("Language", "Name"), languageTypes, 60f, (object item) =>
		{
			string text = item.ToString();
			try
			{
				CultureInfo cultureInfo = new CultureInfo(text.Replace("_", "-"));
				return cultureInfo.TextInfo.ToTitleCase(cultureInfo.NativeName) + " (" + text + ")";
			}
			catch
			{
				return text;
			}
		}, (object item) =>
		{
			LanguageControl.Initialize(item.ToString());
			foreach (ModEntity mod in ModsManager.ModList)
			{
				mod.LoadLauguage();
			}
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			foreach (KeyValuePair<string, Screen> screen in ScreensManager.m_screens)
			{
				object value = Activator.CreateInstance(screen.Value.GetType());
				dictionary.Add(screen.Key, value);
			}
			foreach (KeyValuePair<string, object> dictItem in dictionary)
			{
				ScreensManager.m_screens[dictItem.Key] = dictItem.Value as Screen;
			}
			CraftingRecipesManager.Initialize();
			ScreensManager.SwitchScreen("MainMenu");
		}));
	}

	private void UpdateVersionUI()
	{
		if (_updateLabel != null && _updateContainer != null)
		{
			string text = "";
			Color color = Color.White;
			Color color2 = new Color(135, 206, 235);
			float a = 1f;
			switch (CurrentUpdateState)
			{
			case UpdateState.Idle:
				text = LanguageControl.Get("MainMenuScreen", "11");
				color = Color.White;
				break;
			case UpdateState.Checking:
			{
				_dotsTimer += Time.FrameDuration;
				int count = (int)(_dotsTimer * 3.0) % 4;
				string text2 = new string('.', count);
				text = LanguageControl.Get("MainMenuScreen", "12") + text2;
				color = Color.Gray;
				a = 0.5f;
				break;
			}
			case UpdateState.UpdateAvailable:
				text = LanguageControl.Get("MainMenuScreen", "13");
				color = new Color(152, 255, 152);
				color2 = new Color(255, 100, 100);
				break;
			case UpdateState.Error:
				text = LanguageControl.Get("MainMenuScreen", "14");
				color = Color.Red;
				break;
			case UpdateState.UpToDate:
				text = LanguageControl.Get("MainMenuScreen", "15");
				color = Color.White;
				break;
			}
			_updateLabel.Text = text;
			_updateLabel.Color = color;
			_updateContainer.ColorTransform = new Color(1f, 1f, 1f, a);
			if (_modVersionLabel != null)
			{
				_modVersionLabel.Color = color2;
				_modVersionLabel.Text = LanguageControl.Get("MainMenuScreen", "9") + SettingsManager.CurrentModVersion;
			}
		}
	}

	public static async void StartVersionCheck(bool isSilent)
	{
		if (CurrentUpdateState == UpdateState.Checking)
		{
			return;
		}
		CurrentUpdateState = UpdateState.Checking;
		try
		{
			string requiredVersion = SettingsManager.CurrentModVersion;
			string text = await new MainMenuScreen().DownloadVersionFromGoogleDriveAsync();
			if (string.IsNullOrEmpty(text))
			{
				CurrentUpdateState = UpdateState.Error;
				if (!isSilent)
				{
					DialogsManager.ShowDialog(null, new MessageDialog("Error", "Check internet connection", "OK", null, null));
				}
				else
				{
					CurrentUpdateState = UpdateState.Idle;
				}
			}
			else if (text != requiredVersion)
			{
				CurrentUpdateState = UpdateState.UpdateAvailable;
				if (!isSilent)
				{
					DialogsManager.ShowDialog(null, new VersionInfoDialog());
				}
			}
			else
			{
				CurrentUpdateState = UpdateState.UpToDate;
			}
		}
		catch
		{
			CurrentUpdateState = UpdateState.Error;
			if (isSilent)
			{
				CurrentUpdateState = UpdateState.Idle;
			}
		}
	}

	static MainMenuScreen()
	{
		CurrentUpdateState = UpdateState.Idle;
		LastCheckResult = "";
		DialogShownOnce = false;
	}

	private void UpdateLayoutAndLogo()
	{
		Vector2 actualSize = ActualSize;
		bool flag = actualSize != _lastScreenSize;
		string currentLogoKey = PanoramaWidget.GetCurrentLogoKey();
		bool flag2 = currentLogoKey != _lastLogoKey;
		if (flag)
		{
			EnsureMenusInside(actualSize);
			_lastScreenSize = actualSize;
		}
		if (!(flag2 | flag))
		{
			return;
		}
		RectangleWidget rectangleWidget = _logo ?? Children.Find<RectangleWidget>("Logo");
		if (rectangleWidget == null)
		{
			return;
		}
		if (flag2)
		{
			Subtexture subtexture;
			try
			{
				subtexture = ContentManager.Get<Subtexture>("Textures/Gui/Logos/" + currentLogoKey);
			}
			catch
			{
				subtexture = ContentManager.Get<Subtexture>("Textures/Gui/Logos/LogoDefault");
			}
			rectangleWidget.Subtexture = subtexture;
			bool flag3 = SettingsManager.ThemeMode == ThemeMode.Light || SettingsManager.ThemeMode == ThemeMode.LightTransparent;
			rectangleWidget.ColorTransform = (flag3 ? Color.White : new Color(0.7f, 0.7f, 0.7f, 1f));
			_lastLogoKey = currentLogoKey;
			if (_logo == null)
			{
				_logo = rectangleWidget;
			}
			if ((_logoBaseSize.X <= 0f || _logoBaseSize.Y <= 0f) && rectangleWidget.ActualSize.X > 0f)
			{
				_logoBaseSize = rectangleWidget.ActualSize;
			}
		}
		if (flag && _logoBaseSize.X > 0f)
		{
			EnsureLogoInside(actualSize);
		}
	}

	private void EnsureMenusInside(Vector2 actualSize)
	{
		if (actualSize.X <= 0f || actualSize.Y <= 0f)
		{
			return;
		}
		if (_baseScreenSize == Vector2.Zero)
		{
			_baseScreenSize = actualSize;
		}
		if (_leftMenu != null)
		{
			float position = ((!float.IsPositiveInfinity(_leftMenu.ParentDesiredSize.X) && _leftMenu.ParentDesiredSize.X > 0f) ? _leftMenu.ParentDesiredSize.X : _leftMenu.ActualSize.X);
			float result = ((!float.IsPositiveInfinity(_leftMenu.ParentDesiredSize.Y) && _leftMenu.ParentDesiredSize.Y > 0f) ? _leftMenu.ParentDesiredSize.Y : _leftMenu.ActualSize.Y);
			float num = MathUtils.Max(actualSize.X - position - 10f, 10f);
			float i = MathUtils.Max(actualSize.Y - result - 10f, 10f);
			Vector2 componentCreature = _leftMenuBaseMargin;
			componentCreature.X = MathUtils.Clamp(componentCreature.X, 10f, num);
			componentCreature.Y = MathUtils.Clamp(componentCreature.Y, 10f, i);
			_leftMenu.Margin = componentCreature;
			_leftMenu.HorizontalAlignment = WidgetAlignment.Near;
			_leftMenu.VerticalAlignment = WidgetAlignment.Near;
		}
		if (_rightBottom != null)
		{
			float num2 = ((!float.IsPositiveInfinity(_rightBottom.ParentDesiredSize.X) && _rightBottom.ParentDesiredSize.X > 0f) ? _rightBottom.ParentDesiredSize.X : _rightBottom.ActualSize.X);
			float num3 = ((!float.IsPositiveInfinity(_rightBottom.ParentDesiredSize.Y) && _rightBottom.ParentDesiredSize.Y > 0f) ? _rightBottom.ParentDesiredSize.Y : _rightBottom.ActualSize.Y);
			float max = MathUtils.Max(actualSize.X - num2 - 10f, 10f);
			Vector2 rightBottomBaseMargin = _rightBottomBaseMargin;
			rightBottomBaseMargin.X = MathUtils.Clamp(rightBottomBaseMargin.X, 10f, max);
			bool flag = actualSize.Y < 600f;
			if (_iconsMidRow != null)
			{
				_iconsMidRow.Direction = ((!flag) ? LayoutDirection.Vertical : LayoutDirection.Horizontal);
				_iconsMidRow.Margin = (flag ? new Vector2(0f, 0f) : new Vector2(0f, 0f));
			}
			if (num3 + 20f > actualSize.Y)
			{
				_rightBottom.VerticalAlignment = WidgetAlignment.Near;
				rightBottomBaseMargin.Y = 10f;
			}
			else
			{
				_rightBottom.VerticalAlignment = WidgetAlignment.Far;
				rightBottomBaseMargin.Y = MathUtils.Max(rightBottomBaseMargin.Y, 10f);
			}
			_rightBottom.Margin = rightBottomBaseMargin;
			_rightBottom.HorizontalAlignment = WidgetAlignment.Far;
		}
	}
}
