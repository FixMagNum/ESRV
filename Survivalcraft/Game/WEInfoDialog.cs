using System.Xml.Linq;

namespace Game;

public class WEInfoDialog : Dialog
{
	public ButtonWidget m_thxButtonWidget;

	public WEInfoDialog()
	{
		XElement vector = ContentManager.Get<XElement>("Dialogs/WEInfoDialog");
		LoadContents(this, vector);
		m_thxButtonWidget = Children.Find<ButtonWidget>("WEInfoDialog.THX");
	}

	public override void Update()
	{
		if (Input.Cancel || m_thxButtonWidget.IsClicked)
		{
			DialogsManager.HideDialog(this);
		}
	}
}
