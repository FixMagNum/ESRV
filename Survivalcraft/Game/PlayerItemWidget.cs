using Engine;
using Game.NetWork;
using LiteNetLib.Utils;

namespace Game;

public class PlayerItemWidget : CanvasWidget
{
	private static Subtexture Plus;

	private static Subtexture Reticle;

	private static Subtexture Sleep;

	private static Subtexture manageUser;

	private static Subtexture Arrow;

	private PlayerData Player;

	private PlayerData Main;

	private LabelWidget labelWidget;

	private BitmapButtonWidget addFirendBtn;

	private BitmapButtonWidget manageUserBtn;

	private LabelWidget DistanceText;

	private RectangleWidget RectangleWhite;

	private RectangleWidget RectangleRed;

	private StackPanelWidget Container;

	private double LastSendTime;

	private int addFirendPeriod;

	private NetPlayerListWidget listWidget;

	private Dialog dialog;

	public static string fName;

	private PlayerItemImageWidget PlayerDirectionImage;

	private PlayerItemImageWidget SleepImage;

	public PlayerItemWidget(PlayerData playerData, PlayerData main, NetPlayerListWidget listWidget)
	{
		labelWidget = new LabelWidget
		{
			FontScale = 0.7f
		};
		addFirendBtn = new BitmapButtonWidget
		{
			Size = new Vector2(16f),
			HorizontalAlignment = WidgetAlignment.Center,
			VerticalAlignment = WidgetAlignment.Center
		};
		manageUserBtn = new BitmapButtonWidget
		{
			Size = new Vector2(14f),
			Margin = new Vector2(5f, 0f),
			HorizontalAlignment = WidgetAlignment.Center,
			VerticalAlignment = WidgetAlignment.Center
		};
		DistanceText = new LabelWidget
		{
			FontScale = 0.7f
		};
		PlayerDirectionImage = new PlayerItemImageWidget
		{
			HorizontalAlignment = WidgetAlignment.Center,
			VerticalAlignment = WidgetAlignment.Center,
			Margin = new Vector2(5f, 0f)
		};
		SleepImage = new PlayerItemImageWidget
		{
			HorizontalAlignment = WidgetAlignment.Center,
			VerticalAlignment = WidgetAlignment.Center,
			Margin = new Vector2(5f, 0f)
		};
		RectangleWhite = new RectangleWidget
		{
			FillColor = Color.White,
			OutlineThickness = 0f,
			VerticalAlignment = WidgetAlignment.Far
		};
		RectangleRed = new RectangleWidget
		{
			FillColor = Color.Red,
			OutlineThickness = 0f,
			VerticalAlignment = WidgetAlignment.Far
		};
		Container = new StackPanelWidget
		{
			Direction = LayoutDirection.Horizontal,
			HorizontalAlignment = WidgetAlignment.Far
		};
		addFirendPeriod = 200;
		this.listWidget = listWidget;
		Size = new Vector2(220f, 30f);
		Player = playerData;
		Main = main;
		RectangleWhite.Size = new Vector2(220f, 1f);
		RectangleRed.Size = new Vector2(220f, 1f);
		PlayerDirectionImage.SubTexture = Arrow;
		PlayerDirectionImage.Size = new Vector2(12f, 14f);
		PlayerDirectionImage.ColorTransForm = Color.LightGreen;
		SleepImage.Size = new Vector2(14f);
		SleepImage.SubTexture = Sleep;
		addFirendBtn.NormalSubtexture = Plus;
		addFirendBtn.ClickedSubtexture = addFirendBtn.NormalSubtexture;
		Children.Add(labelWidget);
		Children.Add(Container);
		Container.Children.Add(DistanceText);
		Container.Children.Add(PlayerDirectionImage);
		Container.Children.Add(SleepImage);
		Container.Children.Add(addFirendBtn);
		if (main.ServerManager)
		{
			manageUserBtn.NormalSubtexture = manageUser;
			manageUserBtn.ClickedSubtexture = manageUser;
			Container.Children.Add(manageUserBtn);
		}
		Children.Add(RectangleWhite);
		Children.Add(RectangleRed);
		DistanceText.Color = Color.Yellow;
	}

	public float GetAngle(Vector3 direction, Vector3 start, Vector3 desc)
	{
		Vector2 x = new Vector2(desc.X, desc.Z) - new Vector2(start.X, start.Z);
		return Vector2.Angle(new Vector2(direction.X, direction.Z), x);
	}

	public override void Update()
	{
		labelWidget.Text = Player.Name;
		SubsystemPlayers subsystemPlayer = Main.Project.FindSubsystem<SubsystemPlayers>();
		if (manageUserBtn.IsClicked)
		{
			if (subsystemPlayer.BlackPlayerGuidList.Contains(Player.PlayerGUID.ToString()))
			{
				DialogsManager.Confirm(string.Format(LanguageControl.Get(fName, 1), Player.Name), (MessageDialogButton btn) =>
				{
					DialogsManager.HideDialog(dialog);
					if (btn == MessageDialogButton.Button1)
					{
						subsystemPlayer.BlackPlayerGuidList.Remove(Player.PlayerGUID.ToString());
						listWidget.SortPlayerList();
						Log.Information(Main.Name + "Are you sure?" + Player.Name + "Remove from blacklist");
					}
				});
			}
			else
			{
				DialogsManager.Confirm(string.Format(LanguageControl.Get(fName, 2), Player.Name), (MessageDialogButton btn) =>
				{
					DialogsManager.HideDialog(dialog);
					if (btn == MessageDialogButton.Button1)
					{
						subsystemPlayer.AddBlackList(Player);
						Log.Information(Main.Name + "Are you sure?" + Player.Name + "Add to blacklist");
					}
				});
			}
		}
		if (subsystemPlayer.BlackPlayerGuidList.Contains(Player.PlayerGUID.ToString()))
		{
			IsVisible = false;
			IsEnabled = false;
			return;
		}
		IsVisible = true;
		IsEnabled = true;
		if (Player.ComponentPlayer == null || Main.ComponentPlayer == null)
		{
			labelWidget.Color = Color.DarkGray;
			return;
		}
		if (addFirendBtn.IsClicked)
		{
			bool IsInGroup = Main.Friends.Contains(Player.PlayerGUID);
			DialogsManager.Confirm(string.Format(LanguageControl.Get(fName, IsInGroup ? 4 : 3), Player.Name), (MessageDialogButton btn) =>
			{
				if (btn == MessageDialogButton.Button1)
				{
					int num2 = (int)(Time.RealTime - LastSendTime);
					if (Time.RealTime >= (double)addFirendPeriod && num2 <= addFirendPeriod)
					{
						DialogsManager.Alert(string.Format(LanguageControl.Get(fName, 5), (addFirendPeriod - num2).ToString()));
					}
					else
					{
						LastSendTime = Time.RealTime;
						CommonLib.Net.SendPackage(PackageFlag.AddFriendReuqest, (NetDataWriter w) =>
						{
							w.Put(IsInGroup);
							w.Put(Main.ClientId.Value);
							w.Put(Player.ClientId.Value);
							LastSendTime = Time.RealTime;
						});
						DialogsManager.Alert(string.Format(LanguageControl.Get(fName, IsInGroup ? 7 : 6), Player.Name));
					}
				}
			});
		}
		if (Player.ComponentPlayer.ComponentSleep.IsSleeping)
		{
			addFirendBtn.IsVisible = false;
			SleepImage.Padding = new Vector2(2f);
			SleepImage.IsVisible = true;
			SleepImage.Background = Color.White;
			DistanceText.IsVisible = false;
			PlayerDirectionImage.IsVisible = false;
			return;
		}
		SleepImage.IsVisible = false;
		if (!Main.Friends.Contains(Player.PlayerGUID))
		{
			RectangleRed.IsVisible = false;
			addFirendBtn.NormalSubtexture = Plus;
			labelWidget.Color = Color.White;
			DistanceText.IsVisible = false;
			PlayerDirectionImage.IsVisible = false;
			return;
		}
		RectangleRed.IsVisible = true;
		RectangleRed.Size = new Vector2(220f * Player.ComponentPlayer.ComponentHealth.Health, 1f);
		addFirendBtn.NormalSubtexture = Reticle;
		labelWidget.Color = Color.Green;
		PlayerDirectionImage.IsVisible = true;
		float j = Vector2.Distance(Main.ComponentPlayer.ComponentBody.Position.XZ, Player.ComponentPlayer.ComponentBody.Position.XZ);
		if (j > 1000f)
		{
			PlayerDirectionImage.IsVisible = false;
			DistanceText.IsVisible = false;
		}
		else if (j > 750f)
		{
			PlayerDirectionImage.ColorTransForm = Color.LightGray;
			PlayerDirectionImage.IsVisible = true;
			DistanceText.IsVisible = true;
			DistanceText.Text = $"{j:0.0}m";
		}
		else
		{
			PlayerDirectionImage.ColorTransForm = Color.LightGreen;
			PlayerDirectionImage.IsVisible = true;
			float num = GetAngle(Main.ComponentPlayer.GameWidget.ActiveCamera.ViewDirection, Main.ComponentPlayer.ComponentBody.Position, Player.ComponentPlayer.ComponentBody.Position);
			PlayerDirectionImage.RotateAngle = num;
			DistanceText.Text = $"{j:0.0}m";
			DistanceText.IsVisible = true;
		}
	}

	static PlayerItemWidget()
	{
		Plus = TextureAtlasManager.GetSubtexture("Textures/Atlas/Plus");
		Reticle = TextureAtlasManager.GetSubtexture("Textures/Gui/exit_group");
		Sleep = TextureAtlasManager.GetSubtexture("Textures/Atlas/Sleep");
		manageUser = TextureAtlasManager.GetSubtexture("Textures/Gui/manageUser");
		Arrow = TextureAtlasManager.GetSubtexture("Textures/Gui/Arrow");
		fName = "PlayerItemWidget";
	}
}
