using Engine;
using Engine.Graphics;

namespace Game;

public abstract class BasePerspectiveCamera : Camera
{
	public Vector3 m_viewPosition;

	public Vector3 m_viewDirection;

	public Vector3 m_viewUp;

	public Vector3 m_viewRight;

	public Matrix? m_viewMatrix;

	public Matrix? m_invertedViewMatrix;

	public Matrix? m_projectionMatrix;

	public Matrix? m_invertedProjectionMatrix;

	public Matrix? m_screenProjectionMatrix;

	public Matrix? m_viewProjectionMatrix;

	public Vector2? m_viewportSize;

	public Matrix? m_viewportMatrix;

	public BoundingFrustum m_viewFrustum;

	public bool m_viewFrustumValid;

	public override Vector3 ViewPosition => m_viewPosition;

	public override Vector3 ViewDirection => m_viewDirection;

	public override Vector3 ViewUp => m_viewUp;

	public override Vector3 ViewRight => m_viewRight;

	public override Matrix ViewMatrix
	{
		get
		{
			if (!m_viewMatrix.HasValue)
			{
				if (!Eye.HasValue)
				{
					m_viewMatrix = Matrix.CreateLookAt(m_viewPosition, m_viewPosition + m_viewDirection, m_viewUp);
				}
				else
				{
					Matrix arg = VrManager.GetEyeToHeadTransform(Eye.Value);
					m_viewMatrix = Matrix.CreateLookAt(m_viewPosition, m_viewPosition + m_viewDirection, m_viewUp) * Matrix.Invert(arg);
				}
			}
			return m_viewMatrix.Value;
		}
	}

	public override Matrix InvertedViewMatrix
	{
		get
		{
			if (!m_invertedViewMatrix.HasValue)
			{
				m_invertedViewMatrix = Matrix.Invert(ViewMatrix);
			}
			return m_invertedViewMatrix.Value;
		}
	}

	public override Matrix ProjectionMatrix
	{
		get
		{
			if (!m_projectionMatrix.HasValue)
			{
				m_projectionMatrix = CalculateBaseProjectionMatrix();
				ViewWidget text = GameWidget.ViewWidget;
				if (!text.ScalingRenderTargetSize.HasValue && !Eye.HasValue)
				{
					m_projectionMatrix *= MatrixUtils.CreateScaleTranslation(0.5f * text.ActualSize.X, -0.5f * text.ActualSize.Y, text.ActualSize.X / 2f, text.ActualSize.Y / 2f) * text.GlobalTransform * MatrixUtils.CreateScaleTranslation(2f / (float)Display.Viewport.Width, -2f / (float)Display.Viewport.Height, -1f, 1f);
				}
			}
			return m_projectionMatrix.Value;
		}
	}

	public override Matrix ScreenProjectionMatrix
	{
		get
		{
			if (!m_screenProjectionMatrix.HasValue)
			{
				if (!Eye.HasValue)
				{
					Point2 size = Window.Size;
					ViewWidget viewWidget = GameWidget.ViewWidget;
					m_screenProjectionMatrix = CalculateBaseProjectionMatrix() * MatrixUtils.CreateScaleTranslation(0.5f * viewWidget.ActualSize.X, -0.5f * viewWidget.ActualSize.Y, viewWidget.ActualSize.X / 2f, viewWidget.ActualSize.Y / 2f) * viewWidget.GlobalTransform * MatrixUtils.CreateScaleTranslation(2f / (float)size.X, -2f / (float)size.Y, -1f, 1f);
				}
				else
				{
					m_screenProjectionMatrix = CalculateBaseProjectionMatrix();
				}
			}
			return m_screenProjectionMatrix.Value;
		}
	}

	public override Matrix InvertedProjectionMatrix
	{
		get
		{
			if (!m_invertedProjectionMatrix.HasValue)
			{
				m_invertedProjectionMatrix = Matrix.Invert(ProjectionMatrix);
			}
			return m_invertedProjectionMatrix.Value;
		}
	}

	public override Matrix ViewProjectionMatrix
	{
		get
		{
			if (!m_viewProjectionMatrix.HasValue)
			{
				m_viewProjectionMatrix = ViewMatrix * ProjectionMatrix;
			}
			return m_viewProjectionMatrix.Value;
		}
	}

	public override Vector2 ViewportSize
	{
		get
		{
			if (!m_viewportSize.HasValue)
			{
				ViewWidget viewWidget = GameWidget.ViewWidget;
				if (viewWidget.ScalingRenderTargetSize.HasValue)
				{
					m_viewportSize = new Vector2(viewWidget.ScalingRenderTargetSize.Value);
				}
				else
				{
					m_viewportSize = ((!Eye.HasValue) ? new Vector2?(new Vector2(viewWidget.ActualSize.X * viewWidget.GlobalTransform.Right.Length(), viewWidget.ActualSize.Y * viewWidget.GlobalTransform.Up.Length())) : new Vector2?(new Vector2(VrManager.VrRenderTarget.Width, VrManager.VrRenderTarget.Height)));
				}
			}
			return m_viewportSize.Value;
		}
	}

	public override Matrix ViewportMatrix
	{
		get
		{
			if (!m_viewportMatrix.HasValue)
			{
				if (!Eye.HasValue)
				{
					ViewWidget viewWidget = GameWidget.ViewWidget;
					if (viewWidget.ScalingRenderTargetSize.HasValue)
					{
						m_viewportMatrix = Matrix.Identity;
					}
					else
					{
						Matrix identity = Matrix.Identity;
						identity.Right = Vector3.Normalize(viewWidget.GlobalTransform.Right);
						identity.Up = Vector3.Normalize(viewWidget.GlobalTransform.Up);
						identity.Forward = viewWidget.GlobalTransform.Forward;
						identity.Translation = viewWidget.GlobalTransform.Translation;
						m_viewportMatrix = identity;
					}
				}
				else
				{
					m_viewportMatrix = Matrix.Identity;
				}
			}
			return m_viewportMatrix.Value;
		}
	}

	public override BoundingFrustum ViewFrustum
	{
		get
		{
			if (!m_viewFrustumValid)
			{
				if (m_viewFrustum == null)
				{
					m_viewFrustum = new BoundingFrustum(ViewProjectionMatrix);
				}
				else
				{
					m_viewFrustum.Matrix = ViewProjectionMatrix;
				}
				m_viewFrustumValid = true;
			}
			return m_viewFrustum;
		}
	}

	public override void PrepareForDrawing(VrEye? eye)
	{
		base.PrepareForDrawing(eye);
		m_viewMatrix = null;
		m_invertedViewMatrix = null;
		m_projectionMatrix = null;
		m_invertedProjectionMatrix = null;
		m_screenProjectionMatrix = null;
		m_viewProjectionMatrix = null;
		m_viewportSize = null;
		m_viewportMatrix = null;
		m_viewFrustumValid = false;
	}

	public BasePerspectiveCamera(GameWidget gameWidget)
		: base(gameWidget)
	{
	}

	public void SetupPerspectiveCamera(Vector3 position, Vector3 direction, Vector3 up)
	{
		m_viewPosition = position;
		m_viewDirection = Vector3.Normalize(direction);
		m_viewUp = Vector3.Normalize(up);
		m_viewRight = Vector3.Normalize(Vector3.Cross(m_viewDirection, m_viewUp));
	}

	public Matrix CalculateBaseProjectionMatrix()
	{
		if (!Eye.HasValue)
		{
			float num = 90f;
			float num2 = 1f;
			if (SettingsManager.ViewAngleMode == ViewAngleMode.Narrow)
			{
				num2 = 0.8f;
			}
			else if (SettingsManager.ViewAngleMode == ViewAngleMode.Normal)
			{
				num2 = 0.9f;
			}
			ViewWidget viewWidget = GameWidget.ViewWidget;
			float num3 = viewWidget.ActualSize.X / viewWidget.ActualSize.Y;
			float num4 = MathUtils.Min(num * num3, num);
			float num5 = num4 * num3;
			if (num5 < 90f)
			{
				num4 *= 90f / num5;
			}
			else if (num5 > 175f)
			{
				num4 *= 175f / num5;
			}
			float x = 2048f;
			if (SettingsManager.DistantChunksRange > 0)
			{
				x = (float)SettingsManager.VisibilityRange + (float)SettingsManager.DistantChunksRange + 512f;
			}
			x = MathUtils.Clamp(x, 2048f, 12000f);
			return Matrix.CreatePerspectiveFieldOfView(MathUtils.DegToRad(num4 * num2), num3, 0.1f, x);
		}
		float far = 2048f;
		if (SettingsManager.DistantChunksRange > 0)
		{
			far = MathUtils.Clamp((float)SettingsManager.VisibilityRange + (float)SettingsManager.DistantChunksRange + 512f, 2048f, 12000f);
		}
		return VrManager.GetProjectionMatrix(Eye.Value, 0.1f, far);
	}
}
