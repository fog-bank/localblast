using System;
using System.Collections.Generic;
using System.Globalization;

namespace LocalBlast
{
    public class BlastnPage : BlastPage
    {
        private static int index = 1;
        private BlastnTask task;
        private int maxHsps = Settings.Default.BlastnMaxHsps;
        private int maxTargetSeqs = Settings.Default.BlastnMaxTargetSeqs;
        private bool subranged;
        private int rangeFrom;
        private int rangeTo;

        public BlastnPage(MainViewModel owner)
            : base(owner)
        {
            JobTitle = "blastn #" + index++;

            Enum.TryParse(Settings.Default.BlastnTask, out task);
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

        public BlastnTask[] Tasks { get; } = new[] { BlastnTask.Megablast, BlastnTask.DiscontiguousMegablast, BlastnTask.Blastn, BlastnTask.BlastnShort };

        /// <summary>
        /// Gets or sets the program to execute.
        /// </summary>
        public BlastnTask Task
        {
            get => task;
            set
            {
                task = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of HSPs per subject sequence to save for each query
        /// </summary>
        public int MaxHitSegmentPairs
        {
            get => maxHsps;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                maxHsps = value;
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

        public bool QuerySubranged
        {
            get => subranged;
            set
            {
                subranged = value;
                OnPropertyChanged();

                if (value && QueryRangeFrom == 0 && QueryRangeTo == 0 && Query != null)
                {
                    QueryRangeFrom = 1;
                    QueryRangeTo = Query.Length;
                }
            }
        }

        public int QueryRangeFrom
        {
            get => rangeFrom;
            set
            {
                rangeFrom = value;
                OnPropertyChanged();
            }
        }

        public int QueryRangeTo
        {
            get => rangeTo;
            set
            {
                rangeTo = value;
                OnPropertyChanged();
            }
        }

        public override void Close(object parameter)
        {
            base.Close(parameter);

            Settings.Default.BlastnTask = Task.ToString();
            Settings.Default.BlastnMaxHsps = MaxHitSegmentPairs;
            Settings.Default.BlastnMaxTargetSeqs = MaxTargetSequences;
        }

        protected override void SetArgument(Dictionary<string, string> arglist)
        {
            base.SetArgument(arglist);

            var culture = CultureInfo.InvariantCulture;

            switch (Task)
            {
                case BlastnTask.DiscontiguousMegablast:
                    arglist["task"] = "dc-megablast";
                    break;

                case BlastnTask.Blastn:
                    arglist["task"] = "blastn";
                    break;

                case BlastnTask.BlastnShort:
                    arglist["task"] = "blastn-short";
                    break;

                default:
                    arglist["task"] = "megablast";
                    break;
            }

            arglist["max_hsps"] = MaxHitSegmentPairs.ToString(culture);
            arglist["max_target_seqs"] = MaxTargetSequences.ToString(culture);

            if (QuerySubranged)
                arglist["query_loc"] = QueryRangeFrom + "-" + QueryRangeTo;
        }
    }

    public enum BlastnTask
    {
        /// <summary>
        /// For similar sequences (e.g, sequencing errors).
        /// </summary>
        Megablast,

        /// <summary>
        /// Typically used for inter-species comparisons.
        /// </summary>
        DiscontiguousMegablast,

        /// <summary>
        /// The traditional program used for inter-species comparisons.
        /// </summary>
        Blastn,

        /// <summary>
        /// Optimized for sequences less than 30 nucleotides.
        /// </summary>
        BlastnShort
    }
}