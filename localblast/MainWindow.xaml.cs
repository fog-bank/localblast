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
            OnCloseAllTab(null, new RoutedEventArgs());

            base.OnClosing(e);
        }

        private void OnCloseAllTab(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null && ViewModel.Tabs != null)
            {
                ViewModel.SelectedTabIndex = 0;

                var cmds = new List<DelegateCommand>(ViewModel.Tabs.Count);

                foreach (var page in ViewModel.Tabs)
                {
                    if (page.CloseCommand.CanExecute())
                        cmds.Add(page.CloseCommand);
                }

                foreach (var cmd in cmds)
                    cmd.Execute();
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
