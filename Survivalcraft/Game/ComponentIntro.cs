using System;
using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentIntro : Component, IUpdateable
{
	public SubsystemTime m_subsystemTime;

	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemGameInfo m_subsystemGameInfo;

	public ComponentPlayer m_componentPlayer;

	public bool m_playIntro;

	public static string fName;

	public StateMachine m_stateMachine = new StateMachine();

	private SubsystemAmbientMusic m_music;

	private Random m_random;

	private Vector3 m_playerPos;

	private Vector2 m_fwd;

	private Vector2 m_right;

	private int m_cut;

	private double m_cutStart;

	private float m_flipStartYaw;

	private int m_panIndex;

	private double m_panStart;

	private Vector3 m_dropStart;

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public static Vector2 FindOceanDirection(ITerrainContentsGenerator generator, Vector2 position)
	{
		float text = float.MaxValue;
		Vector2 text2 = Vector2.Zero;
		for (int i = 0; i < 36; i++)
		{
			Vector2 vector = Vector2.CreateFromAngle((float)i / 36f * 2f * (float)Math.PI);
			Vector2 vector2 = position + 50f * vector;
			float num = generator.CalculateOceanShoreDistance(vector2.X, vector2.Y);
			if (num < text)
			{
				text2 = vector;
				text = num;
			}
		}
		return text2;
	}

	public void Update(float dt)
	{
		if (m_playIntro)
		{
			m_playIntro = false;
			switch (m_subsystemGameInfo.WorldSettings.GameMode)
			{
			case GameMode.Embrace:
				m_stateMachine.TransitionTo("EmbraceIntro");
				break;
			case GameMode.Creative:
				m_stateMachine.TransitionTo("CreativeIntro");
				break;
			case GameMode.Cruel:
				m_stateMachine.TransitionTo("CruelIntro");
				break;
			default:
				m_stateMachine.TransitionTo("SurvivalIntro");
				break;
			}
		}
		m_stateMachine.Update();
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_componentPlayer = Entity.FindComponent<ComponentPlayer>(throwOnError: true);
		m_music = Project.FindSubsystem<SubsystemAmbientMusic>(throwOnError: false);
		m_random = new Random();
		m_playIntro = valuesDictionary.GetValue<bool>("PlayIntro");
		m_stateMachine.AddState("CreativeIntro", CreativeIntro_Enter, CreativeIntro_Update, null);
		m_stateMachine.AddState("SurvivalIntro", SurvivalIntro_Enter, SurvivalIntro_Update, null);
		m_stateMachine.AddState("EmbraceIntro", EmbraceIntro_Enter, EmbraceIntro_Update, null);
		m_stateMachine.AddState("CruelIntro", CruelIntro_Enter, CruelIntro_Update, null);
	}

	public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
	{
		valuesDictionary.SetValue("PlayIntro", m_playIntro);
	}

	public float ScoreShipPosition(Vector2 playerPosition, int x, int z)
	{
		float num = 0f;
		float num2 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(x, z);
		if (num2 > -8f)
		{
			num -= 100f;
		}
		num -= 0.25f * num2;
		float num3 = Vector2.Distance(playerPosition, new Vector2(x, z));
		num -= MathUtils.Abs(num3 - 20f);
		int num4 = 0;
		TerrainChunk chunkAtCell = m_subsystemTerrain.Terrain.GetChunkAtCell(x, z);
		if (chunkAtCell != null && chunkAtCell.State >= TerrainChunkState.InvalidLight)
		{
			int oceanLevel = m_subsystemTerrain.TerrainContentsGenerator.OceanLevel;
			int num5 = oceanLevel;
			while (num5 >= oceanLevel - 5 && num5 >= 0)
			{
				int cellContentsFast = chunkAtCell.GetCellContentsFast(x & 0xF, num5, z & 0xF);
				if (cellContentsFast != 18 && cellContentsFast != 92)
				{
					break;
				}
				num5--;
				num4++;
			}
		}
		else
		{
			num4 = 2;
		}
		if (num4 < 2)
		{
			num -= 100f;
		}
		return num + 2f * (float)num4;
	}

	static ComponentIntro()
	{
		fName = "ComponentIntro";
	}

	public void CreativeIntro_Enter()
	{
		IntroCamera introCamera = m_componentPlayer.GameWidget.FindCamera<IntroCamera>();
		m_componentPlayer.GameWidget.ActiveCamera = introCamera;
		ComponentBody componentBody = m_componentPlayer.Entity.FindComponent<ComponentBody>(throwOnError: true);
		m_playerPos = componentBody.Position;
		float x = componentBody.Rotation.ToYawPitchRoll().X;
		m_fwd = new Vector2(MathUtils.Sin(x), MathUtils.Cos(x));
		if (m_fwd.LengthSquared() < 1E-06f)
		{
			m_fwd = Vector2.UnitY;
		}
		m_right = new Vector2(m_fwd.Y, 0f - m_fwd.X);
		Vector3 eyePosition = m_componentPlayer.ComponentCreatureModel.EyePosition;
		Vector2 vector = Vector2.Normalize(-m_fwd + m_right);
		introCamera.CameraPosition = eyePosition + new Vector3(vector.X, 0f, vector.Y) * 42f + new Vector3(0f, 18f, 0f);
		introCamera.TargetPosition = eyePosition;
		introCamera.TargetCameraPosition = introCamera.CameraPosition;
		introCamera.Speed = 0f;
		m_cut = 0;
		m_cutStart = m_subsystemGameInfo.TotalElapsedGameTime;
		StartRandomMusic();
		m_subsystemTime.QueueGameTimeDelayedExecution(1.0, () =>
		{
			m_componentPlayer.ComponentGui.DisplayLargeMessage(null, LanguageControl.Get("ComponentIntroCreative", "1"), 3f, 0f);
		});
		m_subsystemTime.QueueGameTimeDelayedExecution(4.6, () =>
		{
			m_componentPlayer.ComponentGui.DisplayLargeMessage(null, LanguageControl.Get("ComponentIntroCreative", "2"), 3f, 0f);
		});
		m_subsystemTime.QueueGameTimeDelayedExecution(7.9, () =>
		{
			m_componentPlayer.ComponentGui.DisplayLargeMessage(null, LanguageControl.Get("ComponentIntroCreative", "3"), 3f, 0f);
		});
		m_subsystemTime.QueueGameTimeDelayedExecution(10.5, () =>
		{
			m_componentPlayer.ComponentGui.DisplayLargeMessage(null, LanguageControl.Get("ComponentIntroCreative", "4"), 2.6f, 0f);
		});
		m_subsystemTime.QueueGameTimeDelayedExecution(12.2, () =>
		{
			m_componentPlayer.ComponentGui.DisplayLargeMessage(null, LanguageControl.Get("ComponentIntroCreative", "5"), 1.7f, 0f);
		});
	}

	public void CreativeIntro_Update()
	{
		double num = m_subsystemGameInfo.TotalElapsedGameTime - m_cutStart;
		IntroCamera introCamera = m_componentPlayer.GameWidget.FindCamera<IntroCamera>();
		Vector3 eyePosition = m_componentPlayer.ComponentCreatureModel.EyePosition;
		Vector2 vector = Vector2.Normalize(-m_fwd + m_right);
		Vector2 vector2 = Vector2.Normalize(m_fwd + m_right);
		Vector2 v = Vector2.Normalize(m_fwd - m_right);
		Vector2 vector3 = Vector2.Normalize(-m_fwd - m_right);
		if (m_cut == 0)
		{
			float num2 = 4f;
			float f = MathUtils.SmoothStep(0f, 1f, MathUtils.Saturate((float)(num / (double)num2)));
			Vector3 v2 = eyePosition + new Vector3(vector.X, 0f, vector.Y) * 42f + new Vector3(0f, 18f, 0f);
			Vector3 v3 = eyePosition + new Vector3(vector.X, 0f, vector.Y) * 28f + new Vector3(0f, 14.5f, 0f);
			introCamera.CameraPosition = Vector3.Lerp(v2, v3, f);
			introCamera.TargetPosition = eyePosition;
			if (num >= (double)num2)
			{
				m_cut = 1;
				m_cutStart = m_subsystemGameInfo.TotalElapsedGameTime;
			}
		}
		else if (m_cut == 1)
		{
			float num3 = 4f;
			float f2 = MathUtils.SmoothStep(0f, 1f, MathUtils.Saturate((float)(num / (double)num3)));
			Vector3 v4 = eyePosition + new Vector3(vector2.X, 0f, vector2.Y) * 52f + new Vector3(0f, 22f, 0f);
			Vector3 v5 = eyePosition + new Vector3(vector2.X, 0f, vector2.Y) * 30f + new Vector3(0f, 15f, 0f);
			introCamera.CameraPosition = Vector3.Lerp(v4, v5, f2);
			introCamera.TargetPosition = eyePosition;
			if (num >= (double)num3)
			{
				m_cut = 2;
				m_cutStart = m_subsystemGameInfo.TotalElapsedGameTime;
			}
		}
		else if (m_cut == 2)
		{
			float num4 = 3.5f;
			float x = MathUtils.Saturate((float)(num / (double)num4));
			float f3 = MathUtils.SmoothStep(0f, 1f, x);
			Vector2 v6 = Vector2.Normalize(vector3 + 0.25f * -m_right);
			Vector2 vector4 = Vector2.Normalize(Vector2.Lerp(v, v6, f3));
			float num5 = MathUtils.Lerp(48f, 32f, f3);
			float y = MathUtils.Lerp(20f, 15f, f3);
			introCamera.CameraPosition = eyePosition + new Vector3(vector4.X, 0f, vector4.Y) * num5 + new Vector3(0f, y, 0f);
			introCamera.TargetPosition = eyePosition;
			if (num >= (double)num4)
			{
				m_cut = 3;
				m_cutStart = m_subsystemGameInfo.TotalElapsedGameTime;
			}
		}
		else if (m_cut == 3)
		{
			float num6 = 2.6f;
			float f4 = MathUtils.SmoothStep(0f, 1f, MathUtils.Saturate((float)(num / (double)num6)));
			Vector3 v7 = eyePosition - new Vector3(m_fwd.X, 0f, m_fwd.Y) * 14f + new Vector3(0f, 8f, 0f);
			Vector3 v8 = eyePosition - new Vector3(m_fwd.X, 0f, m_fwd.Y) * 0.35f + new Vector3(0f, 1.64f, 0f);
			introCamera.CameraPosition = Vector3.Lerp(v7, v8, f4);
			introCamera.TargetPosition = eyePosition;
			if (num >= (double)num6)
			{
				m_componentPlayer.GameWidget.ActiveCamera = m_componentPlayer.GameWidget.FindCamera<FppCamera>();
				m_stateMachine.TransitionTo(null);
			}
		}
	}

	public void SurvivalIntro_Enter()
	{
		IntroCamera introCamera = m_componentPlayer.GameWidget.FindCamera<IntroCamera>();
		m_componentPlayer.GameWidget.ActiveCamera = introCamera;
		ComponentBody componentBody = m_componentPlayer.Entity.FindComponent<ComponentBody>(throwOnError: true);
		m_playerPos = componentBody.Position;
		float x = componentBody.Rotation.ToYawPitchRoll().X;
		m_fwd = new Vector2(MathUtils.Sin(x), MathUtils.Cos(x));
		if (m_fwd.LengthSquared() < 1E-06f)
		{
			m_fwd = Vector2.UnitY;
		}
		m_right = new Vector2(m_fwd.Y, 0f - m_fwd.X);
		Vector3 eyePosition = m_componentPlayer.ComponentCreatureModel.EyePosition;
		SpawnIntroShipAndPlaceCamera(introCamera, eyePosition);
		m_cut = 0;
		m_cutStart = m_subsystemGameInfo.TotalElapsedGameTime;
		StartRandomMusic();
		m_subsystemTime.QueueGameTimeDelayedExecution(0.6, () =>
		{
			m_componentPlayer.ComponentGui.DisplayLargeMessage(null, LanguageControl.Get("ComponentIntroSurvival", "1"), 4.5f, 0f);
		});
		m_subsystemTime.QueueGameTimeDelayedExecution(6.5, () =>
		{
			m_componentPlayer.ComponentGui.DisplayLargeMessage(null, LanguageControl.Get("ComponentIntroSurvival", "2"), 4.5f, 0f);
		});
		m_subsystemTime.QueueGameTimeDelayedExecution(10.7, () =>
		{
			m_componentPlayer.ComponentGui.DisplayLargeMessage(null, LanguageControl.Get("ComponentIntroSurvival", "3"), 4.5f, 0f);
		});
		m_subsystemTime.QueueGameTimeDelayedExecution(14.3, () =>
		{
			m_componentPlayer.ComponentGui.DisplayLargeMessage(null, LanguageControl.Get("ComponentIntroSurvival", "4"), 4.5f, 0f);
		});
		m_subsystemTime.QueueGameTimeDelayedExecution(17.0, () =>
		{
			m_componentPlayer.ComponentGui.DisplayLargeMessage(null, LanguageControl.Get("ComponentIntroSurvival", "5"), 4.5f, 0f);
		});
	}

	public void SurvivalIntro_Update()
	{
		double stream = m_subsystemGameInfo.TotalElapsedGameTime - m_cutStart;
		IntroCamera introCamera = m_componentPlayer.GameWidget.FindCamera<IntroCamera>();
		Vector3 eyePosition = m_componentPlayer.ComponentCreatureModel.EyePosition;
		Vector2 vector = Vector2.Normalize(m_fwd + m_right);
		Vector2 v = Vector2.Normalize(m_fwd - m_right);
		Vector2 vector2 = Vector2.Normalize(-m_fwd - m_right);
		if (m_cut == 0)
		{
			introCamera.TargetPosition = eyePosition;
			introCamera.TargetCameraPosition = eyePosition;
			introCamera.Speed = MathUtils.Lerp(0f, 8f, MathUtils.Saturate(((float)m_subsystemGameInfo.TotalElapsedGameTime - 6f) / 3f));
			if (stream >= 6.0)
			{
				m_cut = 1;
				m_cutStart = m_subsystemGameInfo.TotalElapsedGameTime;
			}
		}
		else if (m_cut == 1)
		{
			float num = 4f;
			float f = MathUtils.SmoothStep(0f, 1f, MathUtils.Saturate((float)(stream / (double)num)));
			Vector3 v2 = eyePosition + new Vector3(vector.X, 0f, vector.Y) * 24f + new Vector3(0f, 7.5f, 0f);
			Vector3 v3 = eyePosition + new Vector3(vector.X, 0f, vector.Y) * 12f + new Vector3(0f, 6.2f, 0f);
			introCamera.CameraPosition = Vector3.Lerp(v2, v3, f);
			introCamera.TargetPosition = eyePosition;
			if (stream >= (double)num)
			{
				m_cut = 2;
				m_cutStart = m_subsystemGameInfo.TotalElapsedGameTime;
			}
		}
		else if (m_cut == 2)
		{
			float num2 = 3.5f;
			float x = MathUtils.Saturate((float)(stream / (double)num2));
			float f2 = MathUtils.SmoothStep(0f, 1f, x);
			Vector2 v4 = Vector2.Normalize(vector2 + 0.2f * -m_right);
			Vector2 vector3 = Vector2.Normalize(Vector2.Lerp(v, v4, f2));
			float num3 = MathUtils.Lerp(22f, 15f, f2);
			float y = MathUtils.Lerp(7f, 6f, f2);
			introCamera.CameraPosition = eyePosition + new Vector3(vector3.X, 0f, vector3.Y) * num3 + new Vector3(0f, y, 0f);
			introCamera.TargetPosition = eyePosition;
			if (stream >= (double)num2)
			{
				m_cut = 3;
				m_cutStart = m_subsystemGameInfo.TotalElapsedGameTime;
			}
		}
		else if (m_cut == 3)
		{
			float num4 = 3f;
			float f3 = MathUtils.SmoothStep(0f, 1f, MathUtils.Saturate((float)(stream / (double)num4)));
			Vector3 v5 = eyePosition + new Vector3(m_fwd.X, 0f, m_fwd.Y) * 4.2f + new Vector3(0f, 1.75f, 0f);
			Vector3 v6 = eyePosition + new Vector3(m_fwd.X, 0f, m_fwd.Y) * 3f + new Vector3(0f, 1.68f, 0f);
			introCamera.CameraPosition = Vector3.Lerp(v5, v6, f3);
			introCamera.TargetPosition = eyePosition;
			if (stream >= (double)num4)
			{
				m_cut = 4;
				m_cutStart = m_subsystemGameInfo.TotalElapsedGameTime;
			}
		}
		else if (m_cut == 4)
		{
			Vector2 vector4 = -m_fwd;
			float num5 = 2.2f;
			float f4 = MathUtils.SmoothStep(0f, 1f, MathUtils.Saturate((float)(stream / (double)num5)));
			Vector3 v7 = eyePosition + new Vector3(vector4.X, 0f, vector4.Y) * 9f + new Vector3(0f, 6.5f, 0f);
			Vector3 v8 = eyePosition + new Vector3(vector4.X, 0f, vector4.Y) * 0.4f + new Vector3(0f, 1.64f, 0f);
			introCamera.CameraPosition = Vector3.Lerp(v7, v8, f4);
			introCamera.TargetPosition = eyePosition;
			if (stream >= (double)num5)
			{
				m_componentPlayer.GameWidget.ActiveCamera = m_componentPlayer.GameWidget.FindCamera<FppCamera>();
				m_stateMachine.TransitionTo(null);
			}
		}
	}

	public void EmbraceIntro_Enter()
	{
		IntroCamera num = m_componentPlayer.GameWidget.FindCamera<IntroCamera>();
		m_componentPlayer.GameWidget.ActiveCamera = num;
		ComponentBody num2 = m_componentPlayer.Entity.FindComponent<ComponentBody>(throwOnError: true);
		m_playerPos = num2.Position;
		float message = num2.Rotation.ToYawPitchRoll().X;
		m_fwd = new Vector2(MathUtils.Sin(message), MathUtils.Cos(message));
		if (m_fwd.LengthSquared() < 1E-06f)
		{
			m_fwd = Vector2.UnitY;
		}
		m_right = new Vector2(m_fwd.Y, 0f - m_fwd.X);
		Vector3 eyePosition = m_componentPlayer.ComponentCreatureModel.EyePosition;
		num.CameraPosition = eyePosition + new Vector3(m_fwd.X, 0f, m_fwd.Y) * 6f + new Vector3(0f, 2.2f, 0f);
		num.TargetPosition = eyePosition;
		num.TargetCameraPosition = num.CameraPosition;
		num.Speed = 0f;
		m_cut = 0;
		m_cutStart = m_subsystemGameInfo.TotalElapsedGameTime;
		StartRandomMusic();
	}

	public void EmbraceIntro_Update()
	{
		double num = m_subsystemGameInfo.TotalElapsedGameTime - m_cutStart;
		IntroCamera num2 = m_componentPlayer.GameWidget.FindCamera<IntroCamera>();
		Vector3 text = m_componentPlayer.ComponentCreatureModel.EyePosition;
		if (m_cut == 0)
		{
			float num3 = 8f;
			float item = MathUtils.Saturate((float)(num / (double)num3));
			float f = MathUtils.SmoothStep(0f, 1f, item);
			float x = (float)(num * 0.7);
			Vector2 vector = new Vector2(MathUtils.Cos(x), MathUtils.Sin(x));
			float num4 = MathUtils.Lerp(6f, 2.8f, f);
			float y = MathUtils.Lerp(2.2f, 1.95f, f);
			num2.CameraPosition = text + new Vector3(vector.X, 0f, vector.Y) * num4 + new Vector3(0f, y, 0f);
			num2.TargetPosition = text;
			if (num >= (double)num3)
			{
				m_cut = 1;
				m_cutStart = m_subsystemGameInfo.TotalElapsedGameTime;
			}
		}
		else if (m_cut == 1)
		{
			float num5 = 2.2f;
			int num6 = (int)MathUtils.Clamp((float)Math.Floor(num / (double)num5), 0f, 2f);
			float num7 = (float)(num - (double)((float)num6 * num5));
			float f2 = MathUtils.SmoothStep(0f, 1f, MathUtils.Saturate(num7 / num5));
			Vector3[] array = new Vector3[3];
			Vector3[] array2 = new Vector3[3];
			array[0] = m_playerPos + new Vector3(0f, 80f, 0f);
			array2[0] = m_playerPos + new Vector3((m_fwd.X + m_right.X) * 20f, 80f, (m_fwd.Y + m_right.Y) * 20f);
			array[1] = m_playerPos + new Vector3(m_right.X, 0f, m_right.Y) * 50f + new Vector3(0f, 86f, 0f);
			array2[1] = m_playerPos + new Vector3((m_right.X - m_fwd.X) * 22f, 86f, (m_right.Y - m_fwd.Y) * 22f);
			array[2] = m_playerPos - new Vector3(m_right.X, 0f, m_right.Y) * 50f + new Vector3(0f, 78f, 0f);
			array2[2] = m_playerPos + new Vector3((0f - m_right.X + m_fwd.X) * 22f, 78f, (0f - m_right.Y + m_fwd.Y) * 22f);
			Vector3 cameraPosition = Vector3.Lerp(array[num6], array2[num6], f2);
			Vector3 vector2 = num6 switch
			{
				0 => new Vector3(m_fwd.X, 0f, m_fwd.Y), 
				1 => new Vector3(m_right.X, 0f, m_right.Y), 
				_ => new Vector3(0f - m_right.X, 0f, 0f - m_right.Y), 
			};
			Vector3 targetPosition = m_playerPos + vector2 * 200f;
			targetPosition.Y = cameraPosition.Y;
			num2.CameraPosition = cameraPosition;
			num2.TargetPosition = targetPosition;
			if (num >= (double)(num5 * 3f))
			{
				m_cut = 2;
				m_cutStart = m_subsystemGameInfo.TotalElapsedGameTime;
			}
		}
		else if (m_cut == 2)
		{
			float num8 = 3f;
			float f3 = MathUtils.SmoothStep(0f, 1f, MathUtils.Saturate((float)(num / (double)num8)));
			Vector3 cameraPosition2 = num2.CameraPosition;
			Vector3 v = text + new Vector3(0f, 1f, 0f);
			num2.CameraPosition = Vector3.Lerp(cameraPosition2, v, f3);
			num2.TargetPosition = text;
			if (num >= (double)num8)
			{
				m_cut = 3;
				m_cutStart = m_subsystemGameInfo.TotalElapsedGameTime;
			}
		}
		else if (m_cut == 3)
		{
			float num9 = 2f;
			float f4 = MathUtils.SmoothStep(0f, 1f, MathUtils.Saturate((float)(num / (double)num9)));
			Vector3 v2 = text + new Vector3(m_fwd.X, 0f, m_fwd.Y) * 1.2f + new Vector3(0f, 1.62f, 0f);
			Vector3 v3 = text + new Vector3(m_fwd.X, 0f, m_fwd.Y) * 0.3f + new Vector3(0f, 1.64f, 0f);
			num2.CameraPosition = Vector3.Lerp(v2, v3, f4);
			num2.TargetPosition = text;
			if (num >= (double)num9)
			{
				m_componentPlayer.GameWidget.ActiveCamera = m_componentPlayer.GameWidget.FindCamera<FppCamera>();
				m_stateMachine.TransitionTo(null);
			}
		}
	}

	private void StartRandomMusic()
	{
		if (m_music != null && !(SettingsManager.MusicVolume <= 0f))
		{
			string text;
			if (m_subsystemGameInfo != null && m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace)
			{
				string[] ex = new string[10] { "Music/Embrace/Embrace slow (1)", "Music/Embrace/Embrace slow (2)", "Music/Embrace/Embrace slow (3)", "Music/Embrace/Embrace slow (4)", "Music/Embrace/Embrace slow (5)", "Music/Embrace/Embrace slow (6)", "Music/Embrace/Embrace slow (7)", "Music/Embrace/Embrace slow (8)", "Music/Embrace/Embrace slow (9)", "Music/Embrace/Embrace slow (10)" };
				text = ex[MathUtils.Min(m_random.Int(0, ex.Length), ex.Length - 1)];
			}
			else
			{
				string[] array = new string[5] { "Music/NativeAmericanFluteSpirit", "Music/AloneForever", "Music/NativeAmerican", "Music/NativeAmericanHeart", "Music/NativeAmericanPeaceFlute" };
				text = array[MathUtils.Min(m_random.Int(0, array.Length), array.Length - 1)];
			}
			m_music.StopMusic();
			m_music.PlayTrack(text, 0.0);
		}
	}

	private void SpawnIntroShipAndPlaceCamera(IntroCamera cam, Vector3 eye)
	{
		ComponentBody componentBody = m_componentPlayer.Entity.FindComponent<ComponentBody>(throwOnError: true);
		Vector2 vector = FindOceanDirection(m_subsystemTerrain.TerrainContentsGenerator, componentBody.Position.XZ);
		Vector2 vector2 = componentBody.Position.XZ + 25f * vector;
		Vector2 vector3 = vector2;
		float num = float.MinValue;
		for (int i = Terrain.ToCell(vector2.Y) - 15; i < Terrain.ToCell(vector2.Y) + 15; i++)
		{
			for (int j = Terrain.ToCell(vector2.X) - 15; j < Terrain.ToCell(vector2.X) + 15; j++)
			{
				float num2 = ScoreShipPosition(componentBody.Position.XZ, j, i);
				if (num2 > num)
				{
					num = num2;
					vector3 = new Vector2(j, i);
				}
			}
		}
		int oceanLevel = m_subsystemTerrain.TerrainContentsGenerator.OceanLevel;
		int num3 = 32;
		Vector2 vector4 = Vector2.Normalize(vector);
		if (vector4.X == 0f && vector4.Y == 0f)
		{
			vector4 = Vector2.UnitY;
		}
		for (int k = 0; k < num3; k++)
		{
			int x = Terrain.ToCell(vector3.X);
			int z = Terrain.ToCell(vector3.Y);
			int cellContentsFast = m_subsystemTerrain.Terrain.GetCellContentsFast(x, oceanLevel, z);
			if (cellContentsFast == 18 || cellContentsFast == 92)
			{
				break;
			}
			vector3 += vector4;
		}
		DatabaseObject databaseObject = Project.GameDatabase.Database.FindDatabaseObject("IntroShip", Project.GameDatabase.EntityTemplateType, throwIfNotFound: true);
		ValuesDictionary valuesDictionary = new ValuesDictionary();
		valuesDictionary.PopulateFromDatabaseObject(databaseObject);
		Entity entity = Project.CreateEntity(valuesDictionary);
		Vector3 vector5 = new Vector3(vector3.X, (float)oceanLevel + 0.5f, vector3.Y);
		entity.FindComponent<ComponentFrame>(throwOnError: true).Position = vector5;
		entity.FindComponent<ComponentIntroShip>(throwOnError: true).Heading = Vector2.Angle(vector, -Vector2.UnitY);
		Project.AddEntity(entity);
		cam.CameraPosition = vector5 + new Vector3(12f * vector.X, 8f, 12f * vector.Y) + new Vector3(-5f * vector.Y, 0f, 5f * vector.X);
		cam.TargetPosition = eye;
		cam.TargetCameraPosition = eye;
		cam.Speed = 0f;
	}

	public void CruelIntro_Enter()
	{
		IntroCamera arg = m_componentPlayer.GameWidget.FindCamera<IntroCamera>();
		m_componentPlayer.GameWidget.ActiveCamera = arg;
		ComponentBody componentBody = m_componentPlayer.Entity.FindComponent<ComponentBody>(throwOnError: true);
		m_playerPos = componentBody.Position;
		float x = componentBody.Rotation.ToYawPitchRoll().X;
		m_fwd = new Vector2(MathUtils.Sin(x), MathUtils.Cos(x));
		if (m_fwd.LengthSquared() < 1E-06f)
		{
			m_fwd = Vector2.UnitY;
		}
		m_right = new Vector2(m_fwd.Y, 0f - m_fwd.X);
		Vector3 eyePosition = m_componentPlayer.ComponentCreatureModel.EyePosition;
		if (m_music != null)
		{
			m_music.StopMusic();
		}
		arg.CameraPosition = eyePosition + new Vector3(m_fwd.X, 0f, m_fwd.Y) * 0.9f + new Vector3(0f, 1.62f, 0f);
		arg.TargetPosition = eyePosition;
		arg.TargetCameraPosition = arg.CameraPosition;
		arg.Speed = 0f;
		m_cut = 0;
		m_cutStart = m_subsystemGameInfo.TotalElapsedGameTime;
		m_subsystemTime.QueueGameTimeDelayedExecution(4.0, () =>
		{
			m_componentPlayer.ComponentGui.DisplayLargeMessage(null, LanguageControl.Get("ComponentIntroCruel", "1"), 1.4f, 0f);
		});
		m_subsystemTime.QueueGameTimeDelayedExecution(6.0, () =>
		{
			m_componentPlayer.ComponentGui.DisplayLargeMessage(null, LanguageControl.Get("ComponentIntroCruel", "2"), 1.4f, 0f);
		});
		m_subsystemTime.QueueGameTimeDelayedExecution(8.0, () =>
		{
			m_componentPlayer.ComponentGui.DisplayLargeMessage(null, LanguageControl.Get("ComponentIntroCruel", "3"), 1.4f, 0f);
		});
		m_subsystemTime.QueueGameTimeDelayedExecution(10.0, () =>
		{
			m_componentPlayer.ComponentGui.DisplayLargeMessage(null, LanguageControl.Get("ComponentIntroCruel", "4"), 1.4f, 0f);
		});
	}

	public void CruelIntro_Update()
	{
		double num = m_subsystemGameInfo.TotalElapsedGameTime - m_cutStart;
		IntroCamera introCamera = m_componentPlayer.GameWidget.FindCamera<IntroCamera>();
		Vector3 eyePosition = m_componentPlayer.ComponentCreatureModel.EyePosition;
		if (m_cut == 0)
		{
			float num2 = 4.6f;
			float f = MathUtils.SmoothStep(0f, 1f, MathUtils.Saturate((float)(num / (double)num2)));
			Vector3 v = eyePosition + new Vector3(m_fwd.X, 0f, m_fwd.Y) * 0.9f + new Vector3(0f, 1.62f, 0f);
			Vector3 v2 = eyePosition + new Vector3(m_fwd.X, 0f, m_fwd.Y) * 12f + new Vector3(0f, 7.2f, 0f);
			introCamera.CameraPosition = Vector3.Lerp(v, v2, f);
			introCamera.TargetPosition = eyePosition;
			if (num >= (double)num2)
			{
				m_cut = 1;
				m_cutStart = m_subsystemGameInfo.TotalElapsedGameTime;
			}
		}
		else if (m_cut == 1)
		{
			float num3 = 1.4f;
			float num4 = MathUtils.Saturate((float)(num / (double)num3));
			num4 = num4 * num4 * (3f - 2f * num4);
			Vector3 cameraPosition = introCamera.CameraPosition;
			Vector3 v3 = eyePosition + new Vector3(m_fwd.X, 0f, m_fwd.Y) * 0.35f + new Vector3(0f, 1.64f, 0f);
			introCamera.CameraPosition = Vector3.Lerp(cameraPosition, v3, num4);
			introCamera.TargetPosition = eyePosition;
			if (num >= (double)num3)
			{
				m_componentPlayer.GameWidget.ActiveCamera = m_componentPlayer.GameWidget.FindCamera<FppCamera>();
				m_stateMachine.TransitionTo(null);
			}
		}
	}
}
