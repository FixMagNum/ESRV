using Engine;

namespace Game;

public class OrbitCamera : BasePerspectiveCamera
{
	public Vector3 m_position;

	public Vector2 m_angles = new Vector2(0f, MathUtils.DegToRad(30f));

	public float m_distance = 6f;

	public override bool UsesMovementControls => true;

	public override bool IsEntityControlEnabled => true;

	public OrbitCamera(GameWidget gameWidget)
		: base(gameWidget)
	{
	}

	public override void Activate(Camera previousCamera)
	{
		SetupPerspectiveCamera(previousCamera.ViewPosition, previousCamera.ViewDirection, previousCamera.ViewUp);
	}

	public override void Update(float dt)
	{
		ComponentPlayer value = GameWidget.PlayerData.ComponentPlayer;
		if (value == null || GameWidget.Target == null)
		{
			return;
		}
		ComponentInput componentInput = value.ComponentInput;
		Vector3 cameraSneakMove = componentInput.PlayerInput.CameraSneakMove;
		Vector2 cameraLook = componentInput.PlayerInput.CameraLook;
		m_angles.X = MathUtils.NormalizeAngle(m_angles.X + 4f * cameraLook.X * dt + 0.5f * cameraSneakMove.X * dt);
		m_angles.Y = MathUtils.Clamp(MathUtils.NormalizeAngle(m_angles.Y + 4f * cameraLook.Y * dt), MathUtils.DegToRad(-20f), MathUtils.DegToRad(70f));
		m_distance = MathUtils.Clamp(m_distance - 10f * cameraSneakMove.Z * dt, 2f, 16f);
		Vector3 vector = Vector3.Transform(new Vector3(m_distance, 0f, 0f), Matrix.CreateFromYawPitchRoll(m_angles.X, 0f, m_angles.Y));
		Vector3 vector2 = GameWidget.Target.ComponentBody.BoundingBox.Center();
		Vector3 vector3 = vector2 + vector;
		if (Vector3.Distance(vector3, m_position) < 10f)
		{
			Vector3 vector4 = vector3 - m_position;
			float num = MathUtils.Saturate(10f * dt);
			m_position += num * vector4;
		}
		else
		{
			m_position = vector3;
		}
		Vector3 vector5 = m_position - vector2;
		float? num2 = null;
		Vector3 vector6 = Vector3.Normalize(Vector3.Cross(vector5, Vector3.UnitY));
		Vector3 vector7 = Vector3.Normalize(Vector3.Cross(vector5, vector6));
		for (int i = 0; i <= 0; i++)
		{
			for (int j = 0; j <= 0; j++)
			{
				Vector3 vector8 = 0.5f * (vector6 * i + vector7 * j);
				Vector3 vector9 = vector2 + vector8;
				Vector3 end = vector9 + vector5 + Vector3.Normalize(vector5) * 0.5f;
				TerrainRaycastResult? terrainRaycastResult = GameWidget.SubsystemGameWidgets.SubsystemTerrain.Raycast(vector9, end, useInteractionBoxes: false, skipAirBlocks: true, (int value2, float distance) => !BlocksManager.Blocks[Terrain.ExtractContents(value2)].IsTransparent);
				if (terrainRaycastResult.HasValue)
				{
					num2 = (num2.HasValue ? MathUtils.Min(num2.Value, terrainRaycastResult.Value.Distance) : terrainRaycastResult.Value.Distance);
				}
			}
		}
		Vector3 vector10 = ((!num2.HasValue) ? (vector2 + vector5) : (vector2 + Vector3.Normalize(vector5) * MathUtils.Max(num2.Value - 0.5f, 0.2f)));
		SetupPerspectiveCamera(vector10, vector2 - vector10, Vector3.UnitY);
	}
}
