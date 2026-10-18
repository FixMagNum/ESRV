using System.Xml.Linq;

namespace Game;

public class KnowHowInfoDialog : Dialog
{
	public ButtonWidget m_okButtonWidget;

	public ButtonWidget m_changelogButtonWidget;

	public KnowHowInfoDialog()
	{
		XElement node = ContentManager.Get<XElement>("Dialogs/KnowHowInfoDialog");
		LoadContents(this, node);
		m_okButtonWidget = Children.Find<ButtonWidget>("KnowHowInfoDialog.OK");
		m_changelogButtonWidget = Children.Find<ButtonWidget>("KnowHowInfoDialog.Changelog");
	}

	public override void Update()
	{
		if (Input.Cancel || m_okButtonWidget.IsClicked)
		{
			DialogsManager.HideDialog(this);
		}
		if (m_changelogButtonWidget.IsClicked)
		{
			DialogsManager.ShowDialog(null, new ChangelogDialog());
		}
	}
}
