using System;
using Engine;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentAimingSights : Component, IUpdateable, IDrawable
{
	public ComponentPlayer m_componentPlayer;

	public readonly PrimitivesRenderer2D m_primitivesRenderer2D;

	public readonly PrimitivesRenderer3D m_primitivesRenderer3D;

	public Vector3 m_sightsPosition;

	public Vector3 m_sightsDirection;

	public static int[] m_drawOrders;

	public float SightsSizeFactor;

	public Color CurrentColor;

	public Color TargetColor;

	public float ColorLerpSpeed;

	public bool IsTargetSightsVisible;

	public Vector3 m_targetSightsPosition;

	public Vector3 m_targetSightsDirection;

	public float TargetSightsSizeFactor = 1f;

	public Color TargetSightsTint;

	public ComponentCreature m_componentCreature;

	public SubsystemFishingFloats m_subsystemFishingFloats;

	public SubsystemTime m_subsystemTime;

	private bool m_mgInited;

	private FishingFloat m_mgFloat;

	private float m_mgTargetAngle;

	private float m_mgTargetAngularSpeed;

	private float m_mgTargetInnerRad;

	private float m_mgTargetNextRadReseed;

	public bool IsSightsVisible { get; set; }

	public UpdateOrder UpdateOrder => UpdateOrder.Reset;

	public int[] DrawOrders => m_drawOrders;

	public void ShowAimingSights(Vector3 position, Vector3 direction)
	{
		IsSightsVisible = true;
		m_sightsPosition = position;
		m_sightsDirection = direction;
		if (SightsSizeFactor <= 0f)
		{
			SightsSizeFactor = 0.0001f;
		}
	}

	public void Update(float dt)
	{
		float num = MathUtils.Saturate(ColorLerpSpeed * dt);
		if (num > 0f)
		{
			CurrentColor = new Color((byte)((float)(int)CurrentColor.R + (float)(TargetColor.R - CurrentColor.R) * num), (byte)((float)(int)CurrentColor.G + (float)(TargetColor.G - CurrentColor.G) * num), (byte)((float)(int)CurrentColor.B + (float)(TargetColor.B - CurrentColor.B) * num), byte.MaxValue);
		}
		IsSightsVisible = false;
		IsTargetSightsVisible = false;
		UpdateFishingMiniGame(dt);
	}

	public void Draw(Camera camera, int drawOrder)
	{
		if (camera.GameWidget != m_componentPlayer.GameWidget)
		{
			return;
		}
		float num = MathUtils.Saturate(1f - SettingsManager.UITransparent);
		if (num <= 0f)
		{
			m_primitivesRenderer2D.Flush();
			m_primitivesRenderer3D.Flush(camera.ProjectionMatrix);
			return;
		}
		if (m_componentPlayer.ComponentHealth.Health > 0f && m_componentPlayer.ComponentGui.ControlsContainerWidget.IsVisible)
		{
			if (IsSightsVisible)
			{
				Texture2D texture = ContentManager.Get<Texture2D>("Textures/Gui/Sights");
				float num2 = ((!camera.Eye.HasValue) ? 8f : 2.5f) * SightsSizeFactor;
				Vector3 vector = m_sightsPosition + m_sightsDirection * 50f;
				Vector3 vector2 = Vector3.Normalize(Vector3.Cross(m_sightsDirection, Vector3.UnitY));
				Vector3 vector3 = Vector3.Normalize(Vector3.Cross(m_sightsDirection, vector2));
				Vector3 p = vector + num2 * (-vector2 - vector3);
				Vector3 p2 = vector + num2 * (vector2 - vector3);
				Vector3 p3 = vector + num2 * (vector2 + vector3);
				Vector3 p4 = vector + num2 * (-vector2 + vector3);
				byte a = (byte)MathUtils.Clamp((int)(128f * num), 0, 255);
				byte r = (byte)((float)(int)CurrentColor.R * num);
				byte g = (byte)((float)(int)CurrentColor.G * num);
				byte b = (byte)((float)(int)CurrentColor.B * num);
				TexturedBatch3D texturedBatch3D = m_primitivesRenderer3D.TexturedBatch(texture, useAlphaTest: false, 0, DepthStencilState.None, RasterizerState.CullNoneScissor, BlendState.AlphaBlend, SamplerState.LinearClamp);
				int count = texturedBatch3D.TriangleVertices.Count;
				texturedBatch3D.QueueQuad(p, p2, p3, p4, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Color(r, g, b, a));
				texturedBatch3D.TransformTriangles(camera.ViewMatrix, count);
			}
			if (IsTargetSightsVisible)
			{
				Texture2D texture2 = ContentManager.Get<Texture2D>("Textures/Gui/Sights_Target");
				float num3 = ((!camera.Eye.HasValue) ? 8f : 2.5f) * TargetSightsSizeFactor;
				Vector3 vector4 = m_targetSightsPosition + m_targetSightsDirection * 50f;
				Vector3 vector5 = Vector3.Normalize(Vector3.Cross(m_targetSightsDirection, Vector3.UnitY));
				Vector3 vector6 = Vector3.Normalize(Vector3.Cross(m_targetSightsDirection, vector5));
				Vector3 p5 = vector4 + num3 * (-vector5 - vector6);
				Vector3 p6 = vector4 + num3 * (vector5 - vector6);
				Vector3 p7 = vector4 + num3 * (vector5 + vector6);
				Vector3 p8 = vector4 + num3 * (-vector5 + vector6);
				byte a2 = (byte)MathUtils.Clamp((int)(128f * num), 0, 255);
				byte r2 = (byte)((float)(int)TargetSightsTint.R * num);
				byte g2 = (byte)((float)(int)TargetSightsTint.G * num);
				byte b2 = (byte)((float)(int)TargetSightsTint.B * num);
				TexturedBatch3D texturedBatch3D2 = m_primitivesRenderer3D.TexturedBatch(texture2, useAlphaTest: false, 0, DepthStencilState.None, RasterizerState.CullNoneScissor, BlendState.AlphaBlend, SamplerState.LinearClamp);
				int count2 = texturedBatch3D2.TriangleVertices.Count;
				texturedBatch3D2.QueueQuad(p5, p6, p7, p8, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Color(r2, g2, b2, a2));
				texturedBatch3D2.TransformTriangles(camera.ViewMatrix, count2);
			}
			if (!camera.Eye.HasValue && !camera.UsesMovementControls && !IsSightsVisible && (SettingsManager.LookControlMode == LookControlMode.SplitTouch || !m_componentPlayer.ComponentInput.IsControlledByTouch))
			{
				Subtexture subtexture = ContentManager.Get<Subtexture>("Textures/Atlas/Crosshair");
				float num4 = 1.25f;
				Vector3 vector7 = camera.ViewPosition + camera.ViewDirection * 50f;
				Vector3 vector8 = Vector3.Normalize(Vector3.Cross(camera.ViewDirection, Vector3.UnitY));
				Vector3 vector9 = Vector3.Normalize(Vector3.Cross(camera.ViewDirection, vector8));
				Vector3 p9 = vector7 + num4 * (-vector8 - vector9);
				Vector3 p10 = vector7 + num4 * (vector8 - vector9);
				Vector3 p11 = vector7 + num4 * (vector8 + vector9);
				Vector3 p12 = vector7 + num4 * (-vector8 + vector9);
				byte a3 = (byte)MathUtils.Clamp((int)(128f * num), 0, 255);
				byte b3 = (byte)(255f * num);
				TexturedBatch3D texturedBatch3D3 = m_primitivesRenderer3D.TexturedBatch(subtexture.Texture, useAlphaTest: false, 0, DepthStencilState.None, RasterizerState.CullNoneScissor, BlendState.AlphaBlend, SamplerState.LinearClamp);
				int count3 = texturedBatch3D3.TriangleVertices.Count;
				texturedBatch3D3.QueueQuad(p9, p10, p11, p12, new Vector2(subtexture.TopLeft.X, subtexture.TopLeft.Y), new Vector2(subtexture.BottomRight.X, subtexture.TopLeft.Y), new Vector2(subtexture.BottomRight.X, subtexture.BottomRight.Y), new Vector2(subtexture.TopLeft.X, subtexture.BottomRight.Y), new Color(b3, b3, b3, a3));
				texturedBatch3D3.TransformTriangles(camera.ViewMatrix, count3);
			}
		}
		m_primitivesRenderer2D.Flush();
		m_primitivesRenderer3D.Flush(camera.ProjectionMatrix);
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		m_componentPlayer = Entity.FindComponent<ComponentPlayer>(throwOnError: true);
		m_componentCreature = Entity.FindComponent<ComponentCreature>(throwOnError: true);
		m_subsystemFishingFloats = Entity.Project.FindSubsystem<SubsystemFishingFloats>(throwOnError: true);
		m_subsystemTime = Entity.Project.FindSubsystem<SubsystemTime>(throwOnError: true);
	}

	public ComponentAimingSights()
	{
		SightsSizeFactor = 1f;
		CurrentColor = Color.White;
		TargetColor = Color.White;
		TargetSightsTint = Color.White;
		ColorLerpSpeed = 6f;
		m_primitivesRenderer2D = new PrimitivesRenderer2D();
		m_primitivesRenderer3D = new PrimitivesRenderer3D();
	}

	static ComponentAimingSights()
	{
		m_drawOrders = new int[1] { 2000 };
	}

	public void SetTargetSightsColor(Color c)
	{
		TargetColor = c;
	}

	public void SetInstantSightsColor(Color c)
	{
		TargetColor = c;
		CurrentColor = c;
	}

	public void SetSightsSizeFactor(float f)
	{
		SightsSizeFactor = ((f <= 0f) ? 0.0001f : f);
	}

	public void ShowTargetSights(Vector3 position, Vector3 direction)
	{
		IsTargetSightsVisible = true;
		m_targetSightsPosition = position;
		m_targetSightsDirection = direction;
		if (TargetSightsSizeFactor <= 0f)
		{
			TargetSightsSizeFactor = 0.0001f;
		}
	}

	public void SetTargetSightsSizeFactor(float f)
	{
		TargetSightsSizeFactor = ((f <= 0f) ? 0.0001f : f);
	}

	public void SetTargetSightsTintColor(Color c)
	{
		TargetSightsTint = c;
	}

	public bool ProcessReelAttempt()
	{
		if (m_subsystemFishingFloats == null || m_componentCreature == null)
		{
			return false;
		}
		ComponentCreature num = m_componentCreature;
		return m_subsystemFishingFloats.ProcessReelAttempt(num);
	}

	private void UpdateFishingMiniGame(float dt)
	{
		if (m_componentPlayer == null || m_subsystemFishingFloats == null || m_subsystemTime == null || m_componentCreature == null)
		{
			return;
		}
		ComponentCreature componentCreature = m_componentCreature;
		if (componentCreature == null)
		{
			ClearMiniGameLocal();
			return;
		}
		FishingFloat fishingFloat = m_subsystemFishingFloats.FindFloat(componentCreature);
		if (fishingFloat == null || fishingFloat.StuckBody == null)
		{
			ClearMiniGameLocal();
			return;
		}
		InitMiniGameLocal(fishingFloat);
		float mass = fishingFloat.StuckBody.Mass;
		float num = (float)(m_subsystemTime.GameTime - fishingFloat.CreationTime);
		Vector3 eyePosition = componentCreature.ComponentCreatureModel.EyePosition;
		Vector3 vector = fishingFloat.Position - eyePosition;
		float num2 = vector.Length();
		if (num2 < 0.1f)
		{
			num2 = 0.1f;
		}
		Vector3 vector2 = vector / num2;
		Vector3 v = Vector3.Cross(Vector3.UnitY, vector2);
		if (v.LengthSquared() < 1E-06f)
		{
			v = Vector3.Cross(Vector3.UnitX, vector2);
		}
		v = Vector3.Normalize(v);
		Vector3 vector3 = Vector3.Normalize(Vector3.Cross(vector2, v));
		SubsystemFishingFloats subsystemFishingFloats = m_subsystemFishingFloats;
		float x = MathUtils.Atan(ComputeBlockAreaRadiusLocal(mass, subsystemFishingFloats) * subsystemFishingFloats.AreaScale / MathUtils.Max(0.1f, 6f));
		x = MathUtils.Clamp(x, 0.01f, 1.4f);
		float num3 = MathUtils.Saturate(mass / subsystemFishingFloats.NearImpossibleMass);
		float num4 = MathUtils.Lerp(subsystemFishingFloats.GreenSizeFracSmall, subsystemFishingFloats.GreenSizeFracHeavy, num3);
		float num5 = MathUtils.Lerp(subsystemFishingFloats.YellowOuterFracSmall, subsystemFishingFloats.YellowOuterFracHeavy, num3);
		if (num5 < num4)
		{
			num5 = num4;
		}
		if (mass >= subsystemFishingFloats.ImpossibleMass)
		{
			num4 = 0f;
			num5 = 0f;
		}
		float num6 = MathUtils.Max(subsystemFishingFloats.GreenAngleMin, x * num4);
		float mgTargetAngle = m_mgTargetAngle;
		float mgTargetAngularSpeed = m_mgTargetAngularSpeed;
		float x2 = mgTargetAngle + num * mgTargetAngularSpeed;
		float mgTargetInnerRad = m_mgTargetInnerRad;
		float mgTargetNextRadReseed = m_mgTargetNextRadReseed;
		float x3 = MathUtils.Clamp(mgTargetInnerRad + 0.12f * MathUtils.Sin(num * mgTargetNextRadReseed + mgTargetAngle * 0.37f), 0.15f, 0.95f) * (x * 0.95f);
		float num7 = MathUtils.Sin(x3);
		Vector3 v2 = MathUtils.Cos(x3) * vector2 + num7 * (MathUtils.Cos(x2) * v + MathUtils.Sin(x2) * vector3);
		v2 = Vector3.Normalize(v2);
		Camera camera = ((m_componentPlayer.GameWidget != null) ? m_componentPlayer.GameWidget.ActiveCamera : null);
		Vector3 v3 = ((camera == null || !(camera.ViewDirection.LengthSquared() > 1E-06f)) ? Vector3.Normalize(componentCreature.ComponentBody.Matrix.Forward) : Vector3.Normalize(camera.ViewDirection));
		bool flag = MathUtils.Acos(MathUtils.Clamp(Vector3.Dot(v3, vector2), -1f, 1f)) > x;
		float num8 = MathUtils.Acos(MathUtils.Clamp(Vector3.Dot(v3, v2), -1f, 1f));
		Color targetSightsColor;
		if (flag)
		{
			targetSightsColor = new Color(160, 160, 160, 255);
		}
		else if (mass >= subsystemFishingFloats.ImpossibleMass)
		{
			targetSightsColor = new Color(255, 0, 0, 255);
		}
		else if (num8 <= num6)
		{
			targetSightsColor = new Color(0, 255, 0, 255);
		}
		else
		{
			targetSightsColor = ((!(num8 <= x * num5)) ? new Color(255, 0, 0, 255) : new Color(255, 255, 0, 255));
		}
		Color targetSightsTintColor;
		if (flag)
		{
			targetSightsTintColor = new Color(160, 160, 160, 255);
		}
		else if (mass >= subsystemFishingFloats.ImpossibleMass)
		{
			targetSightsTintColor = Color.Black;
		}
		else if (num3 < 0.33f)
		{
			targetSightsTintColor = new Color(0, 80, 0, 255);
		}
		else
		{
			targetSightsTintColor = ((!(num3 < 0.66f)) ? new Color(110, 0, 25, 255) : new Color(200, 100, 0, 255));
		}
		SetTargetSightsSizeFactor(MathUtils.Lerp(0.48f, 0.08f, num3));
		SetTargetSightsColor(targetSightsColor);
		SetTargetSightsTintColor(targetSightsTintColor);
		ShowTargetSights(eyePosition, v2);
	}

	private void InitMiniGameLocal(FishingFloat f)
	{
		if (f != m_mgFloat || !m_mgInited)
		{
			m_mgFloat = f;
			m_mgInited = true;
			SubsystemFishingFloats subsystemFishingFloats = m_subsystemFishingFloats;
			float f2 = MathUtils.Saturate(((f.StuckBody != null) ? f.StuckBody.Mass : 0f) / subsystemFishingFloats.NearImpossibleMass);
			int mGSeedLocal = GetMGSeedLocal(f);
			m_mgTargetAngle = Hash01Local(mGSeedLocal, 1) * ((float)Math.PI * 2f);
			m_mgTargetAngularSpeed = MathUtils.Lerp(subsystemFishingFloats.TargetOrbitSpeedMin, subsystemFishingFloats.TargetOrbitSpeedMax, f2) * MathUtils.Lerp(0.85f, 1.25f, Hash01Local(mGSeedLocal, 2));
			m_mgTargetInnerRad = MathUtils.Lerp(0.2f, 0.9f, Hash01Local(mGSeedLocal, 3));
			m_mgTargetNextRadReseed = MathUtils.Lerp(0.6f, 1.6f, Hash01Local(mGSeedLocal, 4));
		}
	}

	private void ClearMiniGameLocal()
	{
		m_mgInited = false;
		m_mgFloat = null;
	}

	private float ComputeBlockAreaRadiusLocal(float massKg, SubsystemFishingFloats S)
	{
		if (massKg <= S.LightMassKg)
		{
			massKg = S.LightMassKg;
		}
		if (massKg < S.MidMassThreshold)
		{
			float value2 = (massKg - S.LightMassKg) / Math.Max(1f, S.MidMassThreshold - S.LightMassKg);
			return MathUtils.Lerp(S.AreaBlocksSmall, S.AreaBlocksMidMax, MathUtils.Saturate(value2));
		}
		if (massKg < S.NearImpossibleMass)
		{
			float x = (massKg - S.MidMassThreshold) / Math.Max(1f, S.NearImpossibleMass - S.MidMassThreshold);
			return MathUtils.Lerp(S.AreaBlocksMidMax, S.AreaBlocksHeavy, MathUtils.Saturate(x));
		}
		if (massKg < S.ImpossibleMass)
		{
			float x2 = (massKg - S.NearImpossibleMass) / Math.Max(1f, S.ImpossibleMass - S.NearImpossibleMass);
			return MathUtils.Lerp(S.AreaBlocksHeavy, S.AreaBlocksNearImpossible, MathUtils.Saturate(x2));
		}
		return S.AreaBlocksNearImpossible;
	}

	private int GetMGSeedLocal(FishingFloat f)
	{
		int num = Terrain.ToCell(f.Position.X);
		int humidity = Terrain.ToCell(f.Position.Y);
		int radians = Terrain.ToCell(f.Position.Z);
		int matrix = (int)MathUtils.Round(((f.StuckBody != null) ? f.StuckBody.Mass : 0f) * 10f);
		int value2 = 0;
		if (f.BobberValue != 0)
		{
			value2 = BobberBlock.GetBait(Terrain.ExtractData(f.BobberValue));
		}
		return HashIntsLocal(num, humidity, radians, (matrix << 1) ^ (value2 * 1315423911));
	}

	private static float Hash01Local(int h)
	{
		int num = (h ^ (h >>> 16)) * 2146121005;
		int num2 = (num ^ (num >>> 15)) * -2073254261;
		return (float)((num2 ^ (num2 >>> 16)) & 0x7FFFFFFF) / 2.1474836E+09f;
	}

	private float Hash01Local(int seed, int salt)
	{
		return Hash01Local(HashIntsLocal(seed, salt, -1640531527, -2048144777));
	}

	private int HashIntsLocal(int a, int b, int c, int d)
	{
		return (((((((-2128831035 ^ a) * 16777619) ^ b) * 16777619) ^ c) * 16777619) ^ d) * 16777619;
	}
}
