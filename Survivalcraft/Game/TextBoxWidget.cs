using System;
using Engine;
using Engine.Graphics;
using Engine.Input;
using Engine.Media;
using Game.NetWork;

namespace Game;

public class TextBoxWidget : Widget
{
	public BitmapFont m_font;

	public string m_text = string.Empty;

	public int m_maximumLength = 512;

	public bool m_hasFocus;

	public int m_caretPosition;

	public double m_focusStartTime;

	public float m_scroll;

	public Vector2? m_size;

	public bool MoveNextFlag;

	public Vector2 Size
	{
		get
		{
			if (!m_size.HasValue)
			{
				return Vector2.Zero;
			}
			return m_size.Value;
		}
		set
		{
			m_size = value;
		}
	}

	public string Description { get; set; }

	public string Text
	{
		get
		{
			return m_text;
		}
		set
		{
			string text = ((value.Length > MaximumLength) ? value.Substring(0, MaximumLength) : value);
			if (text != m_text)
			{
				m_text = text;
				CaretPosition = CaretPosition;
				TextChanged?.Invoke(this);
			}
		}
	}

	public int MaximumLength
	{
		get
		{
			return m_maximumLength;
		}
		set
		{
			m_maximumLength = MathUtils.Max(value, 0);
			if (Text.Length > m_maximumLength)
			{
				Text = Text.Substring(0, m_maximumLength);
			}
		}
	}

	public bool OverwriteMode { get; set; }

	public bool HasFocus
	{
		get
		{
			return m_hasFocus;
		}
		set
		{
			if (value == m_hasFocus)
			{
				return;
			}
			m_hasFocus = value;
			if (value)
			{
				if (Text == string.Empty)
				{
					KeyboardInput.GetInput();
				}
				CaretPosition = m_text.Length;
				Keyboard.ShowKeyboard(Title, Description, Text, passwordMode: false, (string text) =>
				{
					Text = text;
				}, null);
			}
			else
			{
				FocusLost?.Invoke(this);
			}
		}
	}

	public BitmapFont Font
	{
		get
		{
			return m_font;
		}
		set
		{
			m_font = value;
		}
	}

	public float FontScale { get; set; }

	public Vector2 FontSpacing { get; set; }

	public Color Color { get; set; }

	public bool TextureLinearFilter { get; set; }

	public int CaretPosition
	{
		get
		{
			return m_caretPosition;
		}
		set
		{
			m_caretPosition = MathUtils.Clamp(value, 0, Text.Length);
			m_focusStartTime = Time.RealTime;
		}
	}

	public event Action<TextBoxWidget> TextChanged;

	public event Action<TextBoxWidget> Enter;

	public event Action<TextBoxWidget> Escape;

	public event Action<TextBoxWidget> FocusLost;

	public TextBoxWidget()
	{
		ClampToBounds = true;
		Color = Color.White;
		TextureLinearFilter = true;
		Font = ContentManager.Get<BitmapFont>("Fonts/Pericles");
		FontScale = 1f;
		Title = string.Empty;
		Description = string.Empty;
	}

	public override void Update()
	{
		if (HasFocus)
		{
			if (KeyboardInput.DeletePressed)
			{
				CaretPosition--;
				CaretPosition = Math.Max(0, CaretPosition);
				if (Text.Length > 0)
				{
					Text = Text.Remove(CaretPosition, 1);
				}
				float num = Font.CalculateCharacterPosition(Text, 0, new Vector2(FontScale), FontSpacing);
				m_scroll = num - ActualSize.X;
				m_scroll = MathUtils.Max(0f, m_scroll);
			}
			string input = KeyboardInput.GetInput();
			if (!string.IsNullOrEmpty(input))
			{
				EnterText(input);
			}
		}
		if (Input.Click.HasValue)
		{
			HasFocus = HitTestGlobal(Input.Click.Value.Start) == this && HitTestGlobal(Input.Click.Value.End) == this;
			if (GameManager.Project != null)
			{
				if (CommonLib.WorkType == WorkType.Local)
				{
					foreach (ComponentPlayer componentPlayer in GameManager.Project.FindSubsystem<SubsystemPlayers>().ComponentPlayers)
					{
						componentPlayer.ComponentInput.AllowHandleInput = !HasFocus;
					}
				}
				else
				{
					ComponentPlayer mainPlayer = GameManager.Project.FindSubsystem<SubsystemPlayers>().MainPlayer;
					if (mainPlayer != null)
					{
						mainPlayer.ComponentInput.AllowHandleInput = !HasFocus;
					}
				}
			}
		}
		if (!HasFocus)
		{
			return;
		}
		if (Input.IsKeyDown(Key.Control))
		{
			if (Input.IsKeyDownOnce(Key.V))
			{
				Text += ClipboardManager.ClipboardString;
			}
			else if (Input.IsKeyDownOnce(Key.C))
			{
				ClipboardManager.ClipboardString = Text;
			}
			else if (Input.IsKeyDownOnce(Key.X))
			{
				ClipboardManager.ClipboardString = Text;
				Text = string.Empty;
			}
		}
		if (Input.IsKeyDownOnce(Key.Tab))
		{
			MoveNext(ScreensManager.CurrentScreen.Children);
		}
		if (Input.IsKeyDownRepeat(Key.LeftArrow))
		{
			CaretPosition = MathUtils.Max(0, --CaretPosition);
		}
		if (Input.IsKeyDownRepeat(Key.RightArrow))
		{
			CaretPosition = MathUtils.Max(Text.Length, ++CaretPosition);
		}
		if (Input.IsKeyDownRepeat(Key.UpArrow))
		{
			CaretPosition = 0;
		}
		if (Input.IsKeyDownRepeat(Key.DownArrow))
		{
			CaretPosition = Text.Length;
		}
	}

	public void MoveNext(WidgetsList widgets)
	{
		foreach (Widget num2 in widgets)
		{
			if (num2 is TextBoxWidget)
			{
				if (!MoveNextFlag && num2 == this)
				{
					MoveNextFlag = true;
				}
				else if (MoveNextFlag)
				{
					(num2 as TextBoxWidget).HasFocus = true;
					HasFocus = false;
					MoveNextFlag = false;
				}
			}
			if (num2 is ContainerWidget)
			{
				ContainerWidget containerWidget = num2 as ContainerWidget;
				MoveNext(containerWidget.Children);
			}
		}
	}

	public override void MeasureOverride(Vector2 parentAvailableSize)
	{
		IsDrawRequired = true;
		if (m_size.HasValue)
		{
			DesiredSize = m_size.Value;
			return;
		}
		if (Text.Length == 0)
		{
			DesiredSize = Font.MeasureText(" ", new Vector2(FontScale), FontSpacing);
		}
		else
		{
			DesiredSize = Font.MeasureText(Text, new Vector2(FontScale), FontSpacing);
		}
		DesiredSize += new Vector2(1f * FontScale * Font.Scale, 0f);
	}

	public override void Draw(DrawContext dc)
	{
		Color result = Color * GlobalColorTransform;
		if (!string.IsNullOrEmpty(m_text))
		{
			Vector2 position = new Vector2(0f - m_scroll, ActualSize.Y / 2f);
			SamplerState samplerState = (TextureLinearFilter ? SamplerState.LinearClamp : SamplerState.PointClamp);
			FontBatch2D fontBatch2D = dc.PrimitivesRenderer2D.FontBatch(Font, 1, DepthStencilState.None, null, null, samplerState);
			int count = fontBatch2D.TriangleVertices.Count;
			fontBatch2D.QueueText(Text, position, 0f, result, TextAnchor.VerticalCenter, new Vector2(FontScale), FontSpacing);
			fontBatch2D.TransformTriangles(GlobalTransform, count);
		}
		if (!m_hasFocus || !(MathUtils.Remainder(Time.RealTime - m_focusStartTime, 0.5) < 0.25))
		{
			return;
		}
		float num = Font.CalculateCharacterPosition(Text, CaretPosition, new Vector2(FontScale), FontSpacing);
		Vector2 vector = new Vector2(0f, ActualSize.Y / 2f) + new Vector2(num - m_scroll, 0f);
		if (m_hasFocus)
		{
			if (vector.X < 0f)
			{
				m_scroll = MathUtils.Max(m_scroll + vector.X, 0f);
			}
			if (vector.X > ActualSize.X)
			{
				m_scroll += vector.X - ActualSize.X + 1f;
			}
		}
		FlatBatch2D flatBatch2D = dc.PrimitivesRenderer2D.FlatBatch(1, DepthStencilState.None);
		int terrainChunk = flatBatch2D.TriangleVertices.Count;
		flatBatch2D.QueueQuad(vector - new Vector2(0f, Font.GlyphHeight / 2f * FontScale * Font.Scale), vector + new Vector2(1f, Font.GlyphHeight / 2f * FontScale * Font.Scale), 0f, result);
		flatBatch2D.TransformTriangles(GlobalTransform, terrainChunk);
	}

	public void EnterText(string s)
	{
		if (OverwriteMode)
		{
			if (CaretPosition + s.Length <= MaximumLength)
			{
				if (CaretPosition < m_text.Length)
				{
					string text = Text;
					text = text.Remove(CaretPosition, s.Length);
					string text2 = (Text = text.Insert(CaretPosition, s));
					text = text2;
				}
				else
				{
					Text = m_text + s;
				}
				CaretPosition += s.Length;
			}
		}
		else if (m_text.Length + s.Length <= MaximumLength)
		{
			if (CaretPosition < m_text.Length)
			{
				Text = Text.Insert(CaretPosition, s);
			}
			else
			{
				Text = m_text + s;
			}
			CaretPosition += s.Length;
		}
	}
}
