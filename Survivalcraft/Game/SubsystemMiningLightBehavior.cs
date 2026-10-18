using System;
using System.Collections.Generic;
using Engine;
using Engine.Graphics;
using TemplatesDatabase;

namespace Game;

public class SubsystemMiningLightBehavior : SubsystemBlockBehavior, IUpdateable, IDrawable
{
	private struct EmbraceMarker
	{
		public Vector3 Position;

		public Vector3 Right;

		public Vector3 Up;
	}

	public struct MiningLightData
	{
		public Vector3 Position;

		public float Intensity;

		public Color Color;
	}

	private class LightSmoother
	{
		public Vector3 CurrentPosition;

		public float CurrentIntensity;
	}

	private SubsystemPlayers m_subsystemPlayers;

	private SubsystemTerrain m_subsystemTerrain;

	private SubsystemGameInfo m_subsystemGameInfo;

	private SubsystemGlow m_subsystemGlow;

	private readonly Dictionary<Guid, GlowPoint> m_helmetGlowPoints;

	private SubsystemGameWidgets m_subsystemGameWidgets;

	public static int[] m_drawOrders;

	private readonly Dictionary<Guid, EmbraceMarker> m_embraceMarkers;

	private PrimitivesRenderer3D m_primitivesRenderer;

	private TexturedBatch3D m_embraceBatch;

	public Dictionary<Guid, MiningLightData> ActiveLights;

	public Dictionary<Guid, MiningLightData> ActiveHeadLights;

	private Dictionary<Guid, LightSmoother> m_smoothedLights;

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override int[] HandledBlocks => new int[0];

	public int[] DrawOrders => m_drawOrders;

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemGameWidgets = Project.FindSubsystem<SubsystemGameWidgets>(throwOnError: true);
		m_subsystemPlayers.PlayerRemoved += OnPlayerRemoved;
		m_subsystemGlow = Project.FindSubsystem<SubsystemGlow>(throwOnError: true);
		Texture2D block = ContentManager.Get<Texture2D>("Textures/Gui/FoodBar_Embrace");
		m_embraceBatch = m_primitivesRenderer.TexturedBatch(block, useAlphaTest: false, 0, DepthStencilState.DepthRead, RasterizerState.CullCounterClockwiseScissor, BlendState.AlphaBlend, SamplerState.LinearClamp);
	}

	public void Update(float dt)
	{
		Camera camera = null;
		if (m_subsystemGameWidgets != null)
		{
			foreach (GameWidget gameWidget in m_subsystemGameWidgets.GameWidgets)
			{
				if (gameWidget.ActiveCamera != null)
				{
					camera = gameWidget.ActiveCamera;
					break;
				}
			}
		}
		MiningLightData value3 = default;
		MiningLightData value4 = default;
		MiningLightData value5 = default;
		EmbraceMarker value7 = default;
		foreach (ComponentPlayer componentPlayer in m_subsystemPlayers.ComponentPlayers)
		{
			ComponentMiner componentMiner = componentPlayer.Entity.FindComponent<ComponentMiner>();
			if (componentMiner == null)
			{
				continue;
			}
			Guid playerGUID = componentPlayer.PlayerData.PlayerGUID;
			bool flag = false;
			int itemValue = 0;
			bool flag2 = false;
			ComponentClothing componentClothing = componentPlayer.Entity.FindComponent<ComponentClothing>();
			if (componentClothing != null)
			{
				foreach (ClothingSlot value8 in Enum.GetValues(typeof(ClothingSlot)))
				{
					foreach (int item in componentClothing.GetClothes(value8))
					{
						int num = Terrain.ExtractContents(item);
						int value = Terrain.ExtractData(item);
						if (BlocksManager.Blocks[num].GetClothingData(value).EmitsLight)
						{
							flag = true;
							itemValue = item;
							if (value8 == ClothingSlot.Head)
							{
								flag2 = true;
							}
							break;
						}
					}
					if (flag)
					{
						break;
					}
				}
			}
			ComponentCreatureModel componentCreatureModel = componentMiner.ComponentCreature.ComponentCreatureModel;
			Vector3 eyePosition = componentCreatureModel.EyePosition;
			Quaternion eyeRotation = componentCreatureModel.EyeRotation;
			Vector3 forward = Vector3.Normalize(Vector3.Transform(new Vector3(0f, 0f, -1f), eyeRotation));
			Vector3 vector = Vector3.Normalize(Vector3.Transform(new Vector3(0f, 0f, -1f), eyeRotation));
			Vector3 up = Vector3.Normalize(Vector3.Transform(new Vector3(0f, 1f, 0f), eyeRotation));
			if (!m_smoothedLights.TryGetValue(playerGUID, out var value2))
			{
				value2 = new LightSmoother
				{
					CurrentPosition = eyePosition + vector,
					CurrentIntensity = 0f
				};
				m_smoothedLights[playerGUID] = value2;
			}
			if (flag)
			{
				int lightStrengthForItem = GetLightStrengthForItem(itemValue);
				Ray3 ray = new Ray3(eyePosition, vector);
				Vector3 v = eyePosition;
				float distance = 0f;
				int num2 = 30;
				for (int i = 1; i <= num2; i++)
				{
					Vector3 vector2 = ray.Position + ray.Direction * i;
					int x = (int)Math.Floor(vector2.X);
					int y = (int)Math.Floor(vector2.Y);
					int z = (int)Math.Floor(vector2.Z);
					int num3 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(x, y, z));
					Block block = BlocksManager.Blocks[num3];
					v = vector2;
					distance = i;
					if (block.IsCollidable && !block.IsTransparent)
					{
						break;
					}
				}
				float x2 = ApplyDistancePenalty(lightStrengthForItem, distance);
				float f = 18f * dt;
				value2.CurrentPosition = Vector3.Lerp(value2.CurrentPosition, v, f);
				value2.CurrentIntensity = MathUtils.Lerp(value2.CurrentIntensity, x2, f);
				if (value2.CurrentIntensity > 0.1f)
				{
					value3.Position = value2.CurrentPosition;
					value3.Intensity = value2.CurrentIntensity;
					value3.Color = Color.White;
					ActiveLights[playerGUID] = value3;
				}
				else
				{
					ActiveLights.Remove(playerGUID);
				}
				value4.Position = eyePosition + vector * 0.25f;
				value4.Intensity = (float)lightStrengthForItem * 0.65f;
				value4.Color = Color.White;
				ActiveHeadLights[playerGUID] = value4;
			}
			else
			{
				value2.CurrentIntensity = MathUtils.Lerp(value2.CurrentIntensity, 0f, 10f * dt);
				if (value2.CurrentIntensity > 0.1f)
				{
					value5.Position = value2.CurrentPosition;
					value5.Intensity = value2.CurrentIntensity;
					value5.Color = Color.White;
					ActiveLights[playerGUID] = value5;
				}
				else
				{
					ActiveLights.Remove(playerGUID);
					m_smoothedLights.Remove(playerGUID);
				}
				ActiveHeadLights.Remove(playerGUID);
			}
			GlowPoint value6;
			if (flag2)
			{
				if (!m_helmetGlowPoints.TryGetValue(playerGUID, out value6))
				{
					value6 = m_subsystemGlow.AddGlowPoint();
					value6.Size = 0.04f;
					value6.FarSize = 0.16f;
					value6.FarDistance = 10f;
					m_helmetGlowPoints[playerGUID] = value6;
				}
				ModelBone modelBone = ((componentCreatureModel.Model != null) ? componentCreatureModel.Model.FindBone("Head", throwIfNotFound: false) : null);
				Matrix matrix = ((modelBone == null || camera == null) ? Matrix.CreateWorld(eyePosition, vector, up) : (componentCreatureModel.AbsoluteBoneTransformsForCamera[modelBone.Index] * camera.InvertedViewMatrix));
				Vector3 right = Vector3.Normalize(matrix.Right);
				Vector3 vector3 = Vector3.Normalize(matrix.Up);
				Vector3 vector4 = -Vector3.Normalize(matrix.Forward);
				value6.Right = right;
				value6.Up = vector4;
				value6.Forward = vector3;
				float num4 = 0.22f;
				float num5 = 0.24f;
				switch (componentPlayer.PlayerData.Tag)
				{
				case PlayerData.PlayerTag.Short:
					num4 *= 0.8f;
					num5 *= 0.8f;
					break;
				case PlayerData.PlayerTag.Fat:
					num5 *= 1.4f;
					break;
				case PlayerData.PlayerTag.Dwarf:
					num4 *= 0.8f;
					break;
				}
				Vector3 vector5 = (value6.Position = matrix.Translation + vector4 * num4 + vector3 * num5);
				int cellLight = m_subsystemTerrain.Terrain.GetCellLight(Terrain.ToCell(vector5.X), Terrain.ToCell(vector5.Y), Terrain.ToCell(vector5.Z));
				float num6 = LightingManager.LightIntensityByLightValue[cellLight];
				float num7 = MathUtils.Saturate(0.6f + 0.4f * (1f - num6));
				float f2 = MathUtils.Saturate(((float)GetLightStrengthForItem(itemValue) - 8f) / 5f);
				float num8 = MathUtils.Lerp(0.25f, 1f, f2);
				value6.Color = new Color(255, 252, 220) * (num7 * num8);
			}
			else if (m_helmetGlowPoints.TryGetValue(playerGUID, out value6))
			{
				m_subsystemGlow.RemoveGlowPoint(value6);
				m_helmetGlowPoints.Remove(playerGUID);
			}
			if (m_subsystemGameInfo != null && m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace)
			{
				ModelBone modelBone2 = ((componentCreatureModel.Model != null) ? componentCreatureModel.Model.FindBone("Head", throwIfNotFound: false) : null);
				Matrix matrix2 = ((modelBone2 == null || camera == null) ? Matrix.CreateWorld(eyePosition, forward, up) : (componentCreatureModel.AbsoluteBoneTransformsForCamera[modelBone2.Index] * camera.InvertedViewMatrix));
				Vector3 vector6 = Vector3.Normalize(matrix2.Up);
				Vector3 vector7 = -Vector3.Normalize(matrix2.Forward);
				Vector3 right2 = Vector3.Normalize(matrix2.Right);
				float num9 = 1f;
				switch (componentPlayer.PlayerData.Tag)
				{
				case PlayerData.PlayerTag.Short:
					num9 = 0.85f;
					break;
				case PlayerData.PlayerTag.Fat:
					num9 = 1f;
					break;
				case PlayerData.PlayerTag.Tall:
					num9 = 1f;
					break;
				case PlayerData.PlayerTag.Dwarf:
					num9 = 0.7f;
					break;
				}
				Vector3 position = matrix2.Translation + vector7 * (0.22f * num9) + vector6 * (0.24f * num9);
				value7.Position = position;
				value7.Right = right2;
				value7.Up = vector7;
				m_embraceMarkers[playerGUID] = value7;
			}
			else
			{
				m_embraceMarkers.Remove(playerGUID);
			}
		}
	}

	public SubsystemMiningLightBehavior()
	{
		ActiveLights = new Dictionary<Guid, MiningLightData>();
		ActiveHeadLights = new Dictionary<Guid, MiningLightData>();
		m_smoothedLights = new Dictionary<Guid, LightSmoother>();
		m_helmetGlowPoints = new Dictionary<Guid, GlowPoint>();
		m_embraceMarkers = new Dictionary<Guid, EmbraceMarker>();
		m_primitivesRenderer = new PrimitivesRenderer3D();
	}

	private void OnPlayerRemoved(PlayerData pd)
	{
		Guid playerGUID = pd.PlayerGUID;
		if (ActiveLights.ContainsKey(playerGUID))
		{
			ActiveLights.Remove(playerGUID);
		}
		if (ActiveHeadLights.ContainsKey(playerGUID))
		{
			ActiveHeadLights.Remove(playerGUID);
		}
		if (m_helmetGlowPoints.TryGetValue(playerGUID, out var value))
		{
			m_subsystemGlow.RemoveGlowPoint(value);
			m_helmetGlowPoints.Remove(playerGUID);
		}
		m_embraceMarkers.Remove(playerGUID);
	}

	private int GetLightStrengthForItem(int itemValue)
	{
		int flag = BlocksManager.Blocks[Terrain.ExtractContents(itemValue)].GetDamage(itemValue);
		float componentCreature = 1f - (float)flag / 15f;
		if (componentCreature >= 0.71f)
		{
			return 13;
		}
		if (componentCreature >= 0.51f)
		{
			return 11;
		}
		if (componentCreature >= 0.31f)
		{
			return 10;
		}
		return 8;
	}

	private int ApplyDistancePenalty(int baseStrength, float distance)
	{
		float num = MathUtils.Clamp(distance, 0f, 30f);
		float x;
		if (num <= 10f)
		{
			x = 0f;
		}
		else
		{
			x = ((!(num <= 20f)) ? (3f + (num - 20f) / 10f * 2f) : ((num - 10f) / 10f * 3f));
		}
		int num2 = baseStrength - (int)MathUtils.Round(x);
		if (num2 < 0)
		{
			num2 = 0;
		}
		if (num2 > 15)
		{
			num2 = 15;
		}
		return num2;
	}

	public void Draw(Camera camera, int drawOrder)
	{
		if (m_embraceBatch == null || m_embraceMarkers.Count == 0)
		{
			return;
		}
		foreach (EmbraceMarker value in m_embraceMarkers.Values)
		{
			Vector3 vector = value.Position - camera.ViewPosition;
			float num = Vector3.Dot(vector, camera.ViewDirection);
			if (num > 0.01f)
			{
				float num2 = vector.Length();
				if (num2 > 0.01f)
				{
					float num3 = 0.18f;
					Vector3 vector2 = (0f - (0.01f + 0.02f * num)) / num2 * vector;
					Vector3 p = value.Position + num3 * (-value.Right - value.Up) + vector2;
					Vector3 p2 = value.Position + num3 * (value.Right - value.Up) + vector2;
					Vector3 p3 = value.Position + num3 * (value.Right + value.Up) + vector2;
					Vector3 p4 = value.Position + num3 * (-value.Right + value.Up) + vector2;
					m_embraceBatch.QueueQuad(p, p2, p3, p4, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f), Color.White);
				}
			}
		}
		m_primitivesRenderer.Flush(camera.ViewProjectionMatrix);
	}

	static SubsystemMiningLightBehavior()
	{
		m_drawOrders = new int[1] { 111 };
	}
}
