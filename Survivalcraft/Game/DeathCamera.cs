using System;
using Engine;

namespace Game;

public class DeathCamera : BasePerspectiveCamera
{
	public Vector3 m_position;

	public Vector3? m_bestPosition;

	public float m_vrDeltaYaw;

	public override bool UsesMovementControls => false;

	public override bool IsEntityControlEnabled => false;

	public DeathCamera(GameWidget gameWidget)
		: base(gameWidget)
	{
	}

	public override void Activate(Camera previousCamera)
	{
		m_position = previousCamera.ViewPosition;
		Vector3 vector = GameWidget.Target?.ComponentBody.BoundingBox.Center() ?? m_position;
		m_bestPosition = FindBestCameraPosition(vector, 6f);
		SetupPerspectiveCamera(m_position, vector - m_position, Vector3.UnitY);
		if (GameWidget.Target is ComponentPlayer componentPlayer && componentPlayer.ComponentInput.IsControlledByVr && m_bestPosition.HasValue)
		{
			m_vrDeltaYaw = Matrix.CreateWorld(Vector3.Zero, vector - m_bestPosition.Value, Vector3.UnitY).ToYawPitchRoll().X - VrManager.HmdMatrixYpr.X;
		}
	}

	public override void Update(float dt)
	{
		Vector3 vector = GameWidget.Target?.ComponentBody.BoundingBox.Center() ?? m_position;
		if (m_bestPosition.HasValue)
		{
			if (Vector3.Distance(m_bestPosition.Value, m_position) > 20f)
			{
				m_position = m_bestPosition.Value;
			}
			m_position += 1.5f * dt * (m_bestPosition.Value - m_position);
		}
		if (!Eye.HasValue)
		{
			SetupPerspectiveCamera(m_position, vector - m_position, Vector3.UnitY);
			return;
		}
		Matrix identity = Matrix.Identity;
		identity.Translation = m_position;
		identity.OrientationMatrix = VrManager.HmdMatrix * Matrix.CreateRotationY(m_vrDeltaYaw);
		SetupPerspectiveCamera(identity.Translation, identity.Forward, identity.Up);
	}

	public Vector3 FindBestCameraPosition(Vector3 targetPosition, float distance)
	{
		Vector3? vector = null;
		for (int i = 0; i < 36; i++)
		{
			float x = 1f + (float)Math.PI * 2f * (float)i / 36f;
			Vector3 vector2 = Vector3.Normalize(new Vector3(MathUtils.Sin(x), 0.5f, MathUtils.Cos(x)));
			Vector3 vector3 = targetPosition + vector2 * distance;
			TerrainRaycastResult? terrainRaycastResult = GameWidget.SubsystemGameWidgets.SubsystemTerrain.Raycast(targetPosition, vector3, useInteractionBoxes: false, skipAirBlocks: true, (int v, float d) => !BlocksManager.Blocks[Terrain.ExtractContents(v)].IsTransparent);
			Vector3 zero = Vector3.Zero;
			if (terrainRaycastResult.HasValue)
			{
				CellFace cellFace = terrainRaycastResult.Value.CellFace;
				zero = new Vector3((float)cellFace.X + 0.5f, (float)cellFace.Y + 0.5f, (float)cellFace.Z + 0.5f) - 1f * vector2;
			}
			else
			{
				zero = vector3;
			}
			if (!vector.HasValue || Vector3.Distance(zero, targetPosition) > Vector3.Distance(vector.Value, targetPosition))
			{
				vector = zero;
			}
		}
		if (vector.HasValue)
		{
			return vector.Value;
		}
		return targetPosition;
	}
}
