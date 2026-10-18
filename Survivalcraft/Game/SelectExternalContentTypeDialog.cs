using System;
using System.Linq;
using System.Xml.Linq;

namespace Game;

public class SelectExternalContentTypeDialog : ListSelectionDialog
{
	public SelectExternalContentTypeDialog(string title, Action<ExternalContentType> selectionHandler)
		: base(title, from v in EnumUtils.GetEnumValues(typeof(ExternalContentType))
			where ExternalContentManager.IsEntryTypeDownloadSupported((ExternalContentType)v)
			select v, 64f, (object item) =>
		{
			ExternalContentType type = (ExternalContentType)item;
			XElement node = ContentManager.Get<XElement>("Widgets/SelectExternalContentTypeItem");
			ContainerWidget containerWidget = (ContainerWidget)Widget.LoadWidget(null, node, null);
			containerWidget.Children.Find<RectangleWidget>("SelectExternalContentType.Icon").Subtexture = ExternalContentManager.GetEntryTypeIcon(type);
			containerWidget.Children.Find<LabelWidget>("SelectExternalContentType.Text").Text = ExternalContentManager.GetEntryTypeDescription(type);
			return containerWidget;
		}, (object item) =>
		{
			selectionHandler((ExternalContentType)item);
		})
	{
	}
}
