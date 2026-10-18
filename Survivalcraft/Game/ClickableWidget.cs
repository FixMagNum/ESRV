using System;

namespace Game;

public class ClickableWidget : Widget
{
	public Action OnClick;

	public string SoundName { get; set; }

	public bool IsPressed { get; set; }

	public bool IsClicked { get; set; }

	public bool IsTapped { get; set; }

	public bool IsChecked { get; set; }

	public bool IsAutoCheckingEnabled { get; set; }

	public override void UpdateCeases()
	{
		base.UpdateCeases();
		IsPressed = false;
		IsClicked = false;
		IsTapped = false;
	}

	public override void Update()
	{
		WidgetInput cellValue = Input;
		IsPressed = false;
		IsTapped = false;
		IsClicked = false;
		if (cellValue.Press.HasValue && HitTestGlobal(cellValue.Press.Value) == this)
		{
			IsPressed = true;
		}
		if (cellValue.Tap.HasValue && HitTestGlobal(cellValue.Tap.Value) == this)
		{
			IsTapped = true;
		}
		if (cellValue.Click.HasValue && HitTestGlobal(cellValue.Click.Value.Start) == this && HitTestGlobal(cellValue.Click.Value.End) == this)
		{
			IsClicked = true;
			OnClick?.Invoke();
			if (IsAutoCheckingEnabled)
			{
				IsChecked = !IsChecked;
			}
			if (!string.IsNullOrEmpty(SoundName))
			{
				AudioManager.PlaySound(SoundName, 1f, 0f, 0f);
			}
		}
	}
}
