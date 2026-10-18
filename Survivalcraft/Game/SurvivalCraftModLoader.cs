using Engine;
using Engine.Graphics;
using Engine.Media;

namespace Game;

public class SurvivalCraftModLoader : ModLoader
{
	public override void __ModInitialize()
	{
		ModsManager.RegisterHook("OnCameraChange", this);
		ModsManager.RegisterHook("OnPlayerDead", this);
		ModsManager.RegisterHook("OnModelRendererDrawExtra", this);
		ModsManager.RegisterHook("GetMaxInstancesCount", this);
	}

	public override void OnCameraChange(ComponentPlayer m_componentPlayer, ComponentGui componentGui)
	{
		GameWidget gameWidget = m_componentPlayer.GameWidget;
		if (gameWidget.ActiveCamera is FppCamera)
		{
			gameWidget.ActiveCamera = gameWidget.FindCamera<TppCamera>();
			componentGui.DisplaySmallMessage(LanguageControl.Get(ComponentGui.fName, 9), Color.White, blinking: false, playNotificationSound: false);
		}
		else if (gameWidget.ActiveCamera is TppCamera)
		{
			gameWidget.ActiveCamera = gameWidget.FindCamera<OrbitCamera>();
			componentGui.DisplaySmallMessage(LanguageControl.Get(ComponentGui.fName, 10), Color.White, blinking: false, playNotificationSound: false);
		}
		else if (gameWidget.ActiveCamera is OrbitCamera)
		{
			gameWidget.ActiveCamera = gameWidget.FindCamera<FixedCamera>();
			componentGui.DisplaySmallMessage(LanguageControl.Get(ComponentGui.fName, 11), Color.White, blinking: false, playNotificationSound: false);
		}
		else if (componentGui.m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative && gameWidget.ActiveCamera is FixedCamera)
		{
			gameWidget.ActiveCamera = gameWidget.FindCamera<DebugCamera>();
			componentGui.DisplaySmallMessage(LanguageControl.Get(ComponentGui.fName, 19), Color.White, blinking: false, playNotificationSound: false);
		}
		else
		{
			gameWidget.ActiveCamera = gameWidget.FindCamera<FppCamera>();
			componentGui.DisplaySmallMessage(LanguageControl.Get(ComponentGui.fName, 12), Color.White, blinking: false, playNotificationSound: false);
		}
	}

	public override void OnPlayerDead(PlayerData playerData)
	{
		playerData.GameWidget.ActiveCamera = playerData.GameWidget.FindCamera<DeathCamera>();
		if (playerData.ComponentPlayer != null)
		{
			string text = playerData.ComponentPlayer.ComponentHealth.CauseOfDeath;
			if (string.IsNullOrEmpty(text))
			{
				text = LanguageControl.Get(PlayerData.fName, 12);
			}
			string arg = string.Format(LanguageControl.Get(PlayerData.fName, 13), text);
			if (playerData.m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Cruel)
			{
				playerData.ComponentPlayer.ComponentGui.DisplayLargeMessage(LanguageControl.Get(PlayerData.fName, 6), string.Format(LanguageControl.Get(PlayerData.fName, 7), arg, LanguageControl.Get("GameMode", playerData.m_subsystemGameInfo.WorldSettings.GameMode.ToString())), 30f, 1.5f);
			}
			else if (playerData.m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Adventure && !playerData.m_subsystemGameInfo.WorldSettings.IsAdventureRespawnAllowed)
			{
				playerData.ComponentPlayer.ComponentGui.DisplayLargeMessage(LanguageControl.Get(PlayerData.fName, 6), string.Format(LanguageControl.Get(PlayerData.fName, 8), arg), 30f, 1.5f);
			}
			else
			{
				playerData.ComponentPlayer.ComponentGui.DisplayLargeMessage(LanguageControl.Get(PlayerData.fName, 6), string.Format(LanguageControl.Get(PlayerData.fName, 9), arg), 30f, 1.5f);
			}
		}
		playerData.Level = MathUtils.Max(MathUtils.Floor(playerData.Level / 2f), 1f);
	}

	public override void OnModelRendererDrawExtra(SubsystemModelsRenderer modelsRenderer, ComponentModel componentModel, Camera camera, float? alphaThreshold)
	{
		if (componentModel is ComponentHumanModel)
		{
			ComponentPlayer componentPlayer = componentModel.Entity.FindComponent<ComponentPlayer>();
			if (componentPlayer != null && camera.GameWidget.PlayerData != componentPlayer.PlayerData)
			{
				ComponentCreature componentCreature = componentPlayer.ComponentMiner.ComponentCreature;
				Vector3 position = Vector3.Transform(componentCreature.ComponentBody.Position + 1.02f * Vector3.UnitY * componentCreature.ComponentBody.BoxSize.Y, camera.ViewMatrix);
				if (position.Z < 0f)
				{
					Color color = Color.Lerp(Color.White, Color.Transparent, MathUtils.Saturate((position.Length() - 4f) / 3f));
					if (color.A > 8)
					{
						Vector3 right = Vector3.TransformNormal(0.005f * Vector3.Normalize(Vector3.Cross(camera.ViewDirection, Vector3.UnitY)), camera.ViewMatrix);
						Vector3 down = Vector3.TransformNormal(-0.005f * Vector3.UnitY, camera.ViewMatrix);
						BitmapFont bitmapFont = LabelWidget.BitmapFont;
						modelsRenderer.PrimitivesRenderer.FontBatch(bitmapFont, 1, DepthStencilState.DepthRead, RasterizerState.CullNoneScissor, BlendState.AlphaBlend, SamplerState.LinearClamp).QueueText(componentPlayer.PlayerData.Name, position, right, down, color, TextAnchor.HorizontalCenter | TextAnchor.Bottom);
					}
				}
			}
		}
		RadioLabels.Draw(modelsRenderer, camera);
	}

	public override int GetMaxInstancesCount()
	{
		return 7;
	}
}
