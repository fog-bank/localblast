using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace LocalBlast
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ObservableCollection<TabPage> tabs = new();
        private int tabIndex = -1;
        private string blastBinDir = Settings.Default.BlastBinDir;
        private string wd = Settings.Default.WorkingDirectory;
        private int numOfThreads = Settings.Default.NumOfThreads;
        private bool limitsBlastHitsView = Settings.Default.LimitsBlastHitsView;
        private int initialBlastHitsView = Settings.Default.InitialBlastHitsView;

        public MainViewModel()
        {
            tabs.Add(new NewPage(this));
            
            if (!Directory.Exists(wd))
                WorkingDirectory = Environment.CurrentDirectory;
        }

        public double WindowWidth
        {
            get => Settings.Default.WindowWidth;
            set
            {
                Settings.Default.WindowWidth = value;
                OnPropertyChanged();
            }
        }

        public double WindowHeight
        {
            get => Settings.Default.WindowHeight;
            set
            {
                Settings.Default.WindowHeight = value;
                OnPropertyChanged();
            }
        }

        public double WindowTop
        {
            get => Settings.Default.WindowTop;
            set
            {
                Settings.Default.WindowTop = value;
                OnPropertyChanged();
            }
        }

        public double WindowLeft
        {
            get => Settings.Default.WindowLeft;
            set
            {
                Settings.Default.WindowLeft = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<TabPage> Tabs => tabs;

        public int SelectedTabIndex
        {
            get => tabIndex;
            set
            {
                tabIndex = value;
                OnPropertyChanged();
            }
        }

        public string BlastBinDir
        {
            get => blastBinDir;
            set
            {
                blastBinDir = value;
                Settings.Default.BlastBinDir = value;
                OnPropertyChanged();
            }
        }

        public string WorkingDirectory
        {
            get => wd;
            set
            {
                wd = value;
                Settings.Default.WorkingDirectory = value;
                OnPropertyChanged();
            }
        }

        public int NumberOfThreads
        {
            get => numOfThreads;
            set
            {
                numOfThreads = value;
                Settings.Default.NumOfThreads = value;
                OnPropertyChanged();
            }
        }

        public bool LimitsBlastHitsView
        {
            get => limitsBlastHitsView;
            set
            {
                limitsBlastHitsView = value;
                Settings.Default.LimitsBlastHitsView = value;
                OnPropertyChanged();
            }
        }

        public int InitialBlastHitsView
        {
            get => initialBlastHitsView;
            set
            {
                initialBlastHitsView = value;
                Settings.Default.InitialBlastHitsView = value;
                OnPropertyChanged();
            }
        }

        public void EnsureWorkingDirectory()
        {
            if (!Directory.Exists(WorkingDirectory))
                Directory.CreateDirectory(WorkingDirectory);
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}