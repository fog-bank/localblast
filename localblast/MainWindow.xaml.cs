using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace LocalBlast
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public MainViewModel ViewModel => (MainViewModel)DataContext;

        protected override void OnClosing(CancelEventArgs e)
        {
            OnCloseAllTab(null, new RoutedEventArgs());

            base.OnClosing(e);
        }

        private void OnCloseAllTab(object? sender, RoutedEventArgs e)
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

        private void AlignViewOnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Right:
                    if (sender is FrameworkElement element && element.DataContext is BlastPage page && page.CanNextSegmentPair(null))
                        page.NextSegmentPair(null);
                    break;

                case Key.Left:
                    if (sender is FrameworkElement element2 && element2.DataContext is BlastPage page2 && page2.CanPreviousSegmentPair(null))
                        page2.PreviousSegmentPair(null);
                    break;
            }
        }

        private void SegmentOnMouseEnter(object? sender, MouseEventArgs e)
        {
            var element = sender as FrameworkElement;
            var segment = element?.DataContext as SegmentPair;

            if (segment?.Parent?.Parent is BlastPage page)
                page.SelectedSegment = segment;
        }
    }
}
