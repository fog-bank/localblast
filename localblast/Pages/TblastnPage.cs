using System;
using System.Collections.Generic;
using System.Globalization;

namespace LocalBlast
{
    public class TblastnPage : BlastPage
    {
        private static int index = 1;

        public TblastnPage(MainViewModel owner)
            : base(owner)
        {
            JobTitle = "tblastn #" + index++;
        }

        public string QueryPaneHeight
        {
            get => Settings.Default.TblastnQueryPaneHeight;
            set
            {
                Settings.Default.TblastnQueryPaneHeight = value;
                OnPropertyChanged();
            }
        }

        public string ResultPaneHeight
        {
            get => Settings.Default.TblastnResultPaneHeight;
            set
            {
                Settings.Default.TblastnResultPaneHeight = value;
                OnPropertyChanged();
            }
        }

        public string DescPaneHeight
        {
            get => Settings.Default.TblastnDescPaneHeight;
            set
            {
                Settings.Default.TblastnDescPaneHeight = value;
                OnPropertyChanged();
            }
        }
    }
}
