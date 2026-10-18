using System.Xml.Linq;
using Engine;

namespace Game;

public class UndoRedoSettingsDialog : Dialog
{
	private readonly ComponentWorldEditIntegration _we;

	private LabelWidget _title;

	private SliderWidget _slider;

	private ButtonWidget _plus;

	private ButtonWidget _minus;

	private ButtonWidget _ok;

	private ButtonWidget _cancel;

	public UndoRedoSettingsDialog(ComponentWorldEditIntegration we)
	{
		_we = we;
		LoadContents(this, ContentManager.Get<XElement>("WE/DialogsWE/UndoRedoDialog"));
		_title = Children.Find<LabelWidget>("Dialog.Title");
		_slider = Children.Find<SliderWidget>("Dialog.Slider");
		_plus = Children.Find<ButtonWidget>("Dialog.Plus");
		_minus = Children.Find<ButtonWidget>("Dialog.Minus");
		_ok = Children.Find<ButtonWidget>("Dialog.OK");
		_cancel = Children.Find<ButtonWidget>("Dialog.Cancel");
		_title.Text = "Undo/Redo Settings";
		_slider.MinValue = 1f;
		_slider.MaxValue = 100f;
		_slider.Value = MathUtils.Clamp(_we.HistoryCapacity, 1, 100);
	}

	public override void Update()
	{
		if (_plus.IsClicked)
		{
			_slider.Value = MathUtils.Min(_slider.Value + 1f, _slider.MaxValue);
		}
		if (_minus.IsClicked)
		{
			_slider.Value = MathUtils.Max(_slider.Value - 1f, _slider.MinValue);
		}
		_slider.Text = ((int)_slider.Value).ToString();
		if (_ok.IsClicked)
		{
			_we.HistoryCapacity = (int)_slider.Value;
			DialogsManager.HideDialog(this);
		}
		if (_cancel.IsClicked)
		{
			DialogsManager.HideDialog(this);
		}
	}
}
