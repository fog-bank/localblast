namespace LocalBlast
{
    public class BlastnPage : BlastPage
    {
        private static int index = 1;

        public BlastnPage(MainViewModel owner)
            : base(owner)
        {
            JobTitle = "blastn #" + index++;
        }

        public string QueryPaneHeight
        {
            get { return Settings.Default.BlastnQueryPaneHeight; }
            set
            {
                Settings.Default.BlastnQueryPaneHeight = value;
                OnPropertyChanged();
            }
        }

        public string ResultPaneHeight
        {
            get { return Settings.Default.BlastnResultPaneHeight; }
            set
            {
                Settings.Default.BlastnResultPaneHeight = value;
                OnPropertyChanged();
            }
        }

        public string DescPaneHeight
        {
            get { return Settings.Default.BlastnDescPaneHeight; }
            set
            {
                Settings.Default.BlastnDescPaneHeight = value;
                OnPropertyChanged();
            }
        }
    }
}