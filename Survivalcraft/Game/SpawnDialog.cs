using System.Xml.Linq;
using Engine;

namespace Game;

public class SpawnDialog : Dialog
{
	public LabelWidget m_largeLabelWidget;

	public LabelWidget m_smallLabelWidget;

	public BlockLoadingWidget m_progressWidget;

	public string LargeMessage
	{
		get
		{
			return m_largeLabelWidget.Text;
		}
		set
		{
			m_largeLabelWidget.Text = value;
		}
	}

	public string SmallMessage
	{
		get
		{
			return m_smallLabelWidget.Text;
		}
		set
		{
			m_smallLabelWidget.Text = value;
		}
	}

	public float Progress
	{
		get
		{
			return m_progressWidget.Value;
		}
		set
		{
			m_progressWidget.Value = value;
		}
	}

	public SpawnDialog()
	{
		XElement num = ContentManager.Get<XElement>("Dialogs/SpawnDialog");
		LoadContents(this, num);
		m_largeLabelWidget = Children.Find<LabelWidget>("SpawnDialog.LargeLabel");
		m_smallLabelWidget = Children.Find<LabelWidget>("SpawnDialog.SmallLabel");
		m_progressWidget = Children.Find<BlockLoadingWidget>("SpawnDialog.Progress");
		if (m_smallLabelWidget != null)
		{
			m_smallLabelWidget.RenderTransform = Matrix.CreateTranslation(0f, -100f, 0f);
		}
	}
}
