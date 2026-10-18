using System.Xml.Linq;

namespace Game;

public class ChangelogDialog : Dialog
{
	public ButtonWidget m_okButtonWidget;

	public ChangelogDialog()
	{
		XElement node = ContentManager.Get<XElement>("Dialogs/ChangelogDialog");
		LoadContents(this, node);
		m_okButtonWidget = Children.Find<ButtonWidget>("ChangelogDialog.OK");
	}

	public override void Update()
	{
		if (Input.Cancel || m_okButtonWidget.IsClicked)
		{
			DialogsManager.HideDialog(this);
		}
	}
}
