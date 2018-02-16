namespace LocalBlast
{
    public class BlastpPage : BlastPage
    {
        private static int index = 1;

        public BlastpPage(MainViewModel owner)
            : base(owner)
        {
            JobTitle = "blastp #" + index++;
        }

        public string QueryPaneHeight
        {
            get => Settings.Default.BlastpQueryPaneHeight;
            set
            {
                Settings.Default.BlastpQueryPaneHeight = value;
                OnPropertyChanged();
            }
        }

        public string ResultPaneHeight
        {
            get => Settings.Default.BlastpResultPaneHeight;
            set
            {
                Settings.Default.BlastpResultPaneHeight = value;
                OnPropertyChanged();
            }
        }

        public string DescPaneHeight
        {
            get => Settings.Default.BlastpDescPaneHeight;
            set
            {
                Settings.Default.BlastpDescPaneHeight = value;
                OnPropertyChanged();
            }
        }
    }
}