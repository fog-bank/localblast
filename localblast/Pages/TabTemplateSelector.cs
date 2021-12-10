using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LocalBlast
{
	public class TabTemplateSelector : DataTemplateSelector
	{
		public TabTemplateSelector()
		{ }

		public string DefaultKey { get; set; } = "Default";

		public string? KeySuffix { get; set; }

		public override DataTemplate? SelectTemplate(object item, DependencyObject container)
		{
			var parent = FindParent<TabControl>(container);

			if (parent != null && parent.Resources != null)
			{
				for (var type = item.GetType(); type != null; type = type.BaseType)
				{
					string key = type.Name + KeySuffix;

					if (parent.Resources.Contains(key))
						return parent.Resources[key] as DataTemplate;
				}

				string defaultKey = DefaultKey + KeySuffix;

				if (parent.Resources.Contains(defaultKey))
					return parent.Resources[defaultKey] as DataTemplate;
			}
			return null;
		}

		private static TVisual? FindParent<TVisual>(DependencyObject visual) where TVisual : Visual
		{
			var obj = VisualTreeHelper.GetParent(visual);

			if (obj == null)
				return null;

            return obj is TVisual parent ? parent : FindParent<TVisual>(obj);
        }
	}
}