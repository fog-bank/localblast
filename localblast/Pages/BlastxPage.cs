namespace LocalBlast
{
    public class BlastxPage : BlastPage
    {
        private static int index = 1;

        public BlastxPage(MainViewModel owner)
            : base(owner)
        {
            JobTitle = "blastx #" + index++;
        }

        public string QueryPaneHeight
        {
            get { return Settings.Default.BlastxQueryPaneHeight; }
            set
            {
                Settings.Default.BlastxQueryPaneHeight = value;
                OnPropertyChanged();
            }
        }

        public string ResultPaneHeight
        {
            get { return Settings.Default.BlastxResultPaneHeight; }
            set
            {
                Settings.Default.BlastxResultPaneHeight = value;
                OnPropertyChanged();
            }
        }

        public string DescPaneHeight
        {
            get { return Settings.Default.BlastxDescPaneHeight; }
            set
            {
                Settings.Default.BlastxDescPaneHeight = value;
                OnPropertyChanged();
            }
        }
    }
}