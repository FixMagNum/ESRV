using Engine;
using Engine.Input;

namespace Game;

public class TouchInputWidget : Widget
{
	public int? m_touchId;

	public Vector2 m_touchLastPosition;

	public Vector2 m_touchOrigin;

	public Vector2 m_touchOriginLimited;

	public bool m_touchMoved;

	public double m_touchTime;

	public int m_touchFrameIndex;

	public TouchInput? m_touchInput;

	public float m_radius = 30f;

	public float Radius
	{
		get
		{
			return m_radius;
		}
		set
		{
			m_radius = MathUtils.Max(value, 1f);
		}
	}

	public TouchInput? TouchInput
	{
		get
		{
			if (IsEnabledGlobal && IsVisibleGlobal)
			{
				return m_touchInput;
			}
			return null;
		}
	}

	public override void Update()
	{
		m_touchInput = null;
		double frameStartTime = Time.FrameStartTime;
		int i = Time.FrameIndex;
		foreach (TouchLocation k in Input.TouchLocations)
		{
			if (k.State == TouchLocationState.Pressed)
			{
				if (HitTestGlobal(k.Position) == this)
				{
					m_touchId = k.Id;
					m_touchLastPosition = k.Position;
					m_touchOrigin = k.Position;
					m_touchOriginLimited = k.Position;
					m_touchTime = frameStartTime;
					m_touchFrameIndex = i;
					m_touchMoved = false;
				}
			}
			else if (k.State == TouchLocationState.Moved)
			{
				if (m_touchId.HasValue && k.Id == m_touchId.Value)
				{
					m_touchMoved |= Vector2.Distance(k.Position, m_touchOrigin) > SettingsManager.MinimumDragDistance * GlobalScale;
					TouchInput l = new TouchInput
					{
						InputType = ((!m_touchMoved) ? TouchInputType.Hold : TouchInputType.Move),
						Duration = (float)(frameStartTime - m_touchTime),
						DurationFrames = i - m_touchFrameIndex,
						Position = k.Position,
						Move = k.Position - m_touchLastPosition,
						TotalMove = k.Position - m_touchOrigin,
						TotalMoveLimited = k.Position - m_touchOriginLimited
					};
					if (MathUtils.Abs(l.TotalMoveLimited.X) > m_radius)
					{
						m_touchOriginLimited.X = k.Position.X - MathUtils.Sign(l.TotalMoveLimited.X) * m_radius;
					}
					if (MathUtils.Abs(l.TotalMoveLimited.Y) > m_radius)
					{
						m_touchOriginLimited.Y = k.Position.Y - MathUtils.Sign(l.TotalMoveLimited.Y) * m_radius;
					}
					m_touchInput = l;
					m_touchLastPosition = k.Position;
				}
			}
			else if (k.State == TouchLocationState.Released && m_touchId.HasValue && k.Id == m_touchId.Value)
			{
				if (frameStartTime - m_touchTime <= (double)SettingsManager.MinimumHoldDuration && Vector2.Distance(k.Position, m_touchOrigin) < SettingsManager.MinimumDragDistance * GlobalScale)
				{
					m_touchInput = new TouchInput
					{
						InputType = TouchInputType.Tap,
						Duration = (float)(frameStartTime - m_touchTime),
						DurationFrames = i - m_touchFrameIndex,
						Position = k.Position
					};
				}
				m_touchId = null;
			}
		}
	}
}
