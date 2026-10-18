using System;
using System.Linq;
using System.Xml.Linq;
using Engine;

namespace Game;

public class SelectExternalContentProviderDialog : ListSelectionDialog
{
	public SelectExternalContentProviderDialog(string title, bool listingSupportRequired, Action<IExternalContentProvider> selectionHandler)
		: base(title, ExternalContentManager.Providers.Where((IExternalContentProvider p) => !listingSupportRequired || p.SupportsListing), 100f, (object item) =>
		{
			IExternalContentProvider externalContentProvider = (IExternalContentProvider)item;
			XElement node = ContentManager.Get<XElement>("Widgets/SelectExternalContentProviderItem");
			ContainerWidget containerWidget = (ContainerWidget)Widget.LoadWidget(null, node, null);
			containerWidget.Children.Find<LabelWidget>("SelectExternalContentProvider.Text").Text = externalContentProvider.DisplayName;
			containerWidget.Children.Find<LabelWidget>("SelectExternalContentProvider.Details").Text = externalContentProvider.Description;
			return containerWidget;
		}, (object item) =>
		{
			selectionHandler((IExternalContentProvider)item);
		})
	{
		ContentSize = new Vector2(700f, ContentSize.Y);
	}
}
