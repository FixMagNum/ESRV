using System.Xml.Linq;

namespace Game;

public class FocalorsInfoDialog : Dialog
{
	public ButtonWidget m_thxButtonWidget;

	public FocalorsInfoDialog()
	{
		XElement node = ContentManager.Get<XElement>("Dialogs/FocalorsInfoDialog");
		LoadContents(this, node);
		m_thxButtonWidget = Children.Find<ButtonWidget>("FocalorsInfoDialog.THX");
	}

	public override void Update()
	{
		if (Input.Cancel || m_thxButtonWidget.IsClicked)
		{
			DialogsManager.HideDialog(this);
		}
	}
}
