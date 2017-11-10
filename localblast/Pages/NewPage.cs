using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace LocalBlast
{
	public class NewPage : TabPage
	{
        private string blastnDatabasePath = Settings.Default.BlastnDbPath;
        private string blastpDatabasePath = Settings.Default.BlastpDbPath;
        private string blastxDatabasePath = Settings.Default.BlastxDbPath;

        public NewPage(MainViewModel owner)
			: base(owner)
		{
			Header = "New job";

            BrowseBlastBinDirCommand = new DelegateCommand(BrowseBlastBinDir);

            OpenMakeBlastDbCommand = new DelegateCommand(OpenMakeBlastDb, CanOpenMakeBlastDb);

            BrowseBlastnDbCommand = new DelegateCommand(BrowseBlastnDb);
            OpenBlastnCommand = new DelegateCommand(OpenBlastn, CanOpenBlastn);
            OpenAlginBlastnCommand = new DelegateCommand(OpenAlignBlastn, CanOpenBlastn);

            BrowseBlastpDbCommand = new DelegateCommand(BrowseBlastpDb);
			OpenBlastpCommand = new DelegateCommand(OpenBlastp, CanOpenBlastp);
			OpenAlginBlastpCommand = new DelegateCommand(OpenAlignBlastp, CanOpenBlastp);

            BrowseBlastxDbCommand = new DelegateCommand(BrowseBlastxDb);
            OpenBlastxCommand = new DelegateCommand(OpenBlastx, CanOpenBlastx);
            OpenAlginBlastxCommand = new DelegateCommand(OpenAlignBlastx, CanOpenBlastx);

            CloseCommand = new DelegateCommand(null, _ => false);
		}

        public DelegateCommand BrowseBlastBinDirCommand { get; }
        public DelegateCommand OpenMakeBlastDbCommand { get; }

        public DelegateCommand BrowseBlastnDbCommand { get; }
        public DelegateCommand OpenBlastnCommand { get; }
        public DelegateCommand OpenAlginBlastnCommand { get; }

        public DelegateCommand BrowseBlastpDbCommand { get; }
		public DelegateCommand OpenBlastpCommand { get; }
		public DelegateCommand OpenAlginBlastpCommand { get; }

        public DelegateCommand BrowseBlastxDbCommand { get; }
        public DelegateCommand OpenBlastxCommand { get; }
        public DelegateCommand OpenAlginBlastxCommand { get; }

        public override DelegateCommand CloseCommand { get; }

        public string BlastnDbPath
        {
            get { return blastnDatabasePath; }
            set
            {
                blastnDatabasePath = value;
                Settings.Default.BlastnDbPath = value;
                OnPropertyChanged();
            }
        }

        public string BlastpDbPath
		{
			get { return blastpDatabasePath; }
			set
			{
				blastpDatabasePath = value;
				Settings.Default.BlastpDbPath = value;
				OnPropertyChanged();
			}
        }

        public string BlastxDbPath
        {
            get { return blastxDatabasePath; }
            set
            {
                blastxDatabasePath = value;
                Settings.Default.BlastxDbPath = value;
                OnPropertyChanged();
            }
        }

        public void BrowseBlastBinDir(object parameter)
        {
            using (var dlg = new CommonOpenFileDialog())
            {
                dlg.IsFolderPicker = true;

                if (parameter is string defaultDir && Directory.Exists(defaultDir))
                    dlg.DefaultDirectory = defaultDir;

                if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    Owner.BlastBinDir = dlg.FileName;

                    OpenMakeBlastDbCommand.OnCanExecuteChanged();
                    OpenBlastpCommand.OnCanExecuteChanged();
                    OpenAlginBlastpCommand.OnCanExecuteChanged();
                }
            }
        }

        public void OpenMakeBlastDb(object parameter)
        {
            var page = new MakeBlastDbPage(Owner);
            page.MakeBlastDbPath = Path.Combine(Owner.BlastBinDir, "makeblastdb.exe");

            Owner.Tabs.Add(page);
            Owner.SelectedTabIndex = Owner.Tabs.Count - 1;
        }

        public bool CanOpenMakeBlastDb(object parameter) => File.Exists(Path.Combine(Owner.BlastBinDir, "makeblastdb.exe"));

        public void BrowseBlastnDb(object parameter)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "Nucleotide sequence file (*.nsq;*.nal)|*.nsq;*.nal";

            if (dlg.ShowDialog() == true)
            {
                BlastnDbPath = dlg.FileName;
                OpenBlastnCommand.OnCanExecuteChanged();
                OpenAlginBlastnCommand.OnCanExecuteChanged();
            }
        }

        public void OpenBlastn(object parameter)
        {
            var page = new BlastnPage(Owner);
			page.ExePath = Path.Combine(Owner.BlastBinDir, "blastn.exe");
            page.DbPath = BlastnDbPath.Substring(0, BlastnDbPath.Length - 4);

            Owner.Tabs.Add(page);
            Owner.SelectedTabIndex = Owner.Tabs.Count - 1;
        }

        public void OpenAlignBlastn(object parameter)
        {
            var owner = Owner;
            string exePath = Path.Combine(Owner.BlastBinDir, "blastn.exe");
            string dbPath = BlastnDbPath.Substring(0, BlastnDbPath.Length - 4);

            OpenWithAlignment(() => new BlastnPage(owner)
            {
                ExePath = exePath,
                DbPath = dbPath
            });
        }

        public bool CanOpenBlastn(object parameter) => File.Exists(Path.Combine(Owner.BlastBinDir, "blastn.exe")) && File.Exists(BlastnDbPath);

        public void BrowseBlastpDb(object parameter)
		{
			var dlg = new OpenFileDialog();
            dlg.Filter = "Protein sequence file (*.psq;*.pal)|*.psq;*.pal";

            if (dlg.ShowDialog() == true)
			{
				BlastpDbPath = dlg.FileName;
				OpenBlastpCommand.OnCanExecuteChanged();
                OpenAlginBlastpCommand.OnCanExecuteChanged();
            }
		}

		public void OpenBlastp(object parameter)
		{
			var page = new BlastpPage(Owner);
			page.ExePath = Path.Combine(Owner.BlastBinDir, "blastp.exe");
			page.DbPath = BlastpDbPath.Substring(0, BlastpDbPath.Length - 4);

			Owner.Tabs.Add(page);
			Owner.SelectedTabIndex = Owner.Tabs.Count - 1;
		}

		public void OpenAlignBlastp(object parameter)
        {
            var owner = Owner;
            string exePath = Path.Combine(Owner.BlastBinDir, "blastp.exe");
            string dbPath = BlastpDbPath.Substring(0, BlastpDbPath.Length - 4);

            OpenWithAlignment(() => new BlastpPage(owner)
            {
                ExePath = exePath,
                DbPath = dbPath
            });
		}

		public bool CanOpenBlastp(object parameter) => File.Exists(Path.Combine(Owner.BlastBinDir, "blastp.exe")) && File.Exists(BlastpDbPath);

        public void BrowseBlastxDb(object parameter)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "Protein sequence file (*.psq;*.pal)|*.psq;*.pal";

            if (dlg.ShowDialog() == true)
            {
                BlastxDbPath = dlg.FileName;
                OpenBlastxCommand.OnCanExecuteChanged();
                OpenAlginBlastxCommand.OnCanExecuteChanged();
            }
        }

        public void OpenBlastx(object parameter)
        {
            var page = new BlastxPage(Owner);
            page.ExePath = Path.Combine(Owner.BlastBinDir, "blastx.exe");
            page.DbPath = BlastxDbPath.Substring(0, BlastxDbPath.Length - 4);

            Owner.Tabs.Add(page);
            Owner.SelectedTabIndex = Owner.Tabs.Count - 1;
        }

        public void OpenAlignBlastx(object parameter)
        {
            var owner = Owner;
            string exePath = Path.Combine(Owner.BlastBinDir, "blastx.exe");
            string dbPath = BlastxDbPath.Substring(0, BlastxDbPath.Length - 4);

            OpenWithAlignment(() => new BlastxPage(owner)
            {
                ExePath = exePath,
                DbPath = dbPath
            });
        }

        public bool CanOpenBlastx(object parameter) => File.Exists(Path.Combine(Owner.BlastBinDir, "blastx.exe")) && File.Exists(BlastxDbPath);

        private void OpenWithAlignment(Func<BlastPage> pageInitializer)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "Alignment file(*.fa*)|*.fa*";

            if (dlg.ShowDialog() == true)
            {
                string name = null;
                var lines = new List<string>();
                int ncpu = Environment.ProcessorCount;
                int concurrents = 0;

                using (var sr = File.OpenText(dlg.FileName))
                {
                    string line = null;
                    bool added = false;
                    do
                    {
                        line = sr.ReadLine();

                        if (line == null || line.StartsWith(">"))
                        {
                            if (name != null)
                            {
                                var blast = pageInitializer();
                                blast.JobTitle = name;
                                blast.Query = string.Join(Environment.NewLine, lines);

                                if (blast.CanRun(null))
                                {
                                    if (++concurrents > ncpu)
                                    {
                                        if (MessageBox.Show("The alignment contains " + concurrents + "+ sequences. Continue?",
                                            "Local BLAST", MessageBoxButton.YesNo) == MessageBoxResult.No)
                                        {
                                            break;
                                        }
                                    }

                                    blast.Run(null);
                                }

                                Owner.Tabs.Add(blast);

                                if (!added)
                                {
                                    Owner.SelectedTabIndex = Owner.Tabs.Count - 1;
                                    added = true;
                                }
                            }
                            name = line?.Substring(1);
                            lines.Clear();
                        }
                        else
                            lines.Add(line);
                    }
                    while (line != null);
                }
            }
        }
    }
}