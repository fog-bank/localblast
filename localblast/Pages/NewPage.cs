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
            dlg.Filter = "blastn sequence file (*.nsq)|*.nsq";

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

        public bool CanOpenBlastn(object parameter) => File.Exists(Path.Combine(Owner.BlastBinDir, "blastn.exe")) && File.Exists(BlastnDbPath);

        public void OpenAlignBlastn(object parameter)
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
                                var page = new BlastnPage(Owner);
                                page.ExePath = Path.Combine(Owner.BlastBinDir, "blastn.exe");
                                page.DbPath = BlastnDbPath.Substring(0, BlastnDbPath.Length - 4);

                                page.JobTitle = name;
                                page.Query = string.Join(Environment.NewLine, lines);

                                if (page.CanRun(null))
                                {
                                    if (++concurrents > ncpu)
                                    {
                                        if (MessageBox.Show("The alignment contains " + concurrents + "+ sequences. Continue?",
                                            "Local BLAST", MessageBoxButton.YesNo) == MessageBoxResult.No)
                                        {
                                            break;
                                        }
                                    }

                                    page.Run(null);
                                }

                                Owner.Tabs.Add(page);

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

        public void BrowseBlastpDb(object parameter)
		{
			var dlg = new OpenFileDialog();
			dlg.Filter = "blastp sequence file (*.psq)|*.psq";

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
								var blastp = new BlastpPage(Owner);
								blastp.ExePath = Path.Combine(Owner.BlastBinDir, "blastp.exe");
								blastp.DbPath = BlastpDbPath.Substring(0, BlastpDbPath.Length - 4);

								blastp.JobTitle = name;
								blastp.Query = string.Join(Environment.NewLine, lines);

								if (blastp.CanRun(null))
                                {
                                    if (++concurrents > ncpu)
                                    {
                                        if (MessageBox.Show("The alignment contains " + concurrents + "+ sequences. Continue?", 
                                            "Local BLAST", MessageBoxButton.YesNo) == MessageBoxResult.No)
                                        {
                                            break;
                                        }
                                    }

                                    blastp.Run(null);
                                }

								Owner.Tabs.Add(blastp);

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

		public bool CanOpenBlastp(object parameter) => File.Exists(Path.Combine(Owner.BlastBinDir, "blastp.exe")) && File.Exists(BlastpDbPath);
	}
}