using System;
using System.Collections.Generic;
using System.Globalization;

namespace LocalBlast
{
    public class BlastxPage : BlastPage
    {
        private static int index = 1;
        private int maxTargetSeqs = Settings.Default.BlastxMaxTargetSeqs;

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

        /// <summary>
        /// Gets or sets the maximum number of aligned sequences to keep.
        /// </summary>
        public int MaxTargetSequences
        {
            get => maxTargetSeqs;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                maxTargetSeqs = value;
                OnPropertyChanged();
            }
        }

        public override void Close(object parameter)
        {
            base.Close(parameter);

            Settings.Default.BlastxMaxTargetSeqs = MaxTargetSequences;
        }

        protected override void SetArgument(Dictionary<string, string> arglist)
        {
            base.SetArgument(arglist);

            var culture = CultureInfo.InvariantCulture;

            arglist["max_target_seqs"] = MaxTargetSequences.ToString(culture);
        }
    }
}