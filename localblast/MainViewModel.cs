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
        private string wd = Path.Combine(Environment.CurrentDirectory, "Temp\\");

        public MainViewModel()
        {
            tabs.Add(new NewPage(this));
        }

        public double WindowWidth
        {
            get { return Settings.Default.WindowWidth; }
            set
            {
                Settings.Default.WindowWidth = value;
                OnPropertyChanged();
            }
        }

        public double WindowHeight
        {
            get { return Settings.Default.WindowHeight; }
            set
            {
                Settings.Default.WindowHeight = value;
                OnPropertyChanged();
            }
        }

        public double WindowTop
        {
            get { return Settings.Default.WindowTop; }
            set
            {
                Settings.Default.WindowTop = value;
                OnPropertyChanged();
            }
        }

        public double WindowLeft
        {
            get { return Settings.Default.WindowLeft; }
            set
            {
                Settings.Default.WindowLeft = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<TabPage> Tabs => tabs;

        public int SelectedTabIndex
        {
            get { return tabIndex; }
            set
            {
                tabIndex = value;
                OnPropertyChanged();
            }
        }

        public string BlastBinDir
        {
            get { return blastBinDir; }
            set
            {
                blastBinDir = value;
                Settings.Default.BlastBinDir = value;
                OnPropertyChanged();
            }
        }

        public string WorkingDirectory
        {
            get { return wd; }
            set
            {
                wd = value;
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