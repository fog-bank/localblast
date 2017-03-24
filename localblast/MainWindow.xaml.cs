using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shapes;

namespace LocalBlast
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		public MainViewModel ViewModel => DataContext as MainViewModel;

		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);

			if (ViewModel != null && ViewModel.Tabs != null)
			{
				var cmds = new List<DelegateCommand>(ViewModel.Tabs.Count);

				foreach (var page in ViewModel.Tabs)
				{
					if (page.CloseCommand.CanExecute(null))
						cmds.Add(page.CloseCommand);
				}

				foreach (var cmd in cmds)
					cmd.Execute(null);
			}
		}

		private void SegmentOnMouseEnter(object sender, MouseEventArgs e)
		{
			var rect = sender as Rectangle;
			var segment = rect.DataContext as SegmentPair;
			(segment.Parent.Parent as BlastpPage).SelectedSegment = segment;

		}
	}
}
