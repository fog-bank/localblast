﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace LocalBlast
{
	public class NewPage : TabPage
	{
        public NewPage(MainViewModel owner)
			: base(owner)
		{
			Header = "New job";

            BrowseBlastBinDirCommand = new DelegateCommand(BrowseBlastBinDir);
            BrowseWorkingDirCommand = new DelegateCommand(BrowseWorkingDir);

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
        public DelegateCommand BrowseWorkingDirCommand { get; }

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
            get => Settings.Default.BlastnDbPath;
            set
            {
                Settings.Default.BlastnDbPath = value;
                OnPropertyChanged();
            }
        }

        public string BlastpDbPath
        {
            get => Settings.Default.BlastpDbPath;
            set
            {
                Settings.Default.BlastpDbPath = value;
                OnPropertyChanged();
            }
        }

        public string BlastxDbPath
        {
            get => Settings.Default.BlastxDbPath;
            set
            {
                Settings.Default.BlastxDbPath = value;
                OnPropertyChanged();
            }
        }

        public static int ProcessorCount => Environment.ProcessorCount;

        public void BrowseBlastBinDir(object? parameter)
        {
            using (var dlg = new CommonOpenFileDialog())
            {
                dlg.IsFolderPicker = true;

                if (parameter is string defaultDir && Directory.Exists(defaultDir))
                    dlg.InitialDirectory = defaultDir;

                if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    Owner.BlastBinDir = dlg.FileName;

                    OpenMakeBlastDbCommand.OnCanExecuteChanged();
                    OpenBlastpCommand.OnCanExecuteChanged();
                    OpenAlginBlastpCommand.OnCanExecuteChanged();
                }
            }
        }

        public void BrowseWorkingDir(object? parameter)
        {
            using (var dlg = new CommonOpenFileDialog())
            {
                dlg.IsFolderPicker = true;

                if (parameter is string defaultDir && Directory.Exists(defaultDir))
                    dlg.InitialDirectory = defaultDir;

                if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                    Owner.WorkingDirectory = dlg.FileName;
            }
        }

        public void OpenMakeBlastDb(object? parameter)
        {
            var page = new MakeBlastDbPage(Owner)
            {
                MakeBlastDbPath = Path.Combine(Owner.BlastBinDir, "makeblastdb.exe")
            };

            Owner.Tabs.Add(page);
            Owner.SelectedTabIndex = Owner.Tabs.Count - 1;
        }

        public bool CanOpenMakeBlastDb(object? parameter) => File.Exists(Path.Combine(Owner.BlastBinDir, "makeblastdb.exe"));

        public void BrowseBlastnDb(object? parameter)
        {
            var dlg = new OpenFileDialog
            {
                Filter = BlastnPage.DbFileTypeFilter
            };

            if (File.Exists(BlastnDbPath))
                dlg.InitialDirectory = Path.GetDirectoryName(BlastnDbPath);

            if (dlg.ShowDialog() == true)
            {
                BlastnDbPath = dlg.FileName;
                OpenBlastnCommand.OnCanExecuteChanged();
                OpenAlginBlastnCommand.OnCanExecuteChanged();
            }
        }

        public void OpenBlastn(object? parameter)
        {
            var page = new BlastnPage(Owner)
            {
                ExePath = Path.Combine(Owner.BlastBinDir, "blastn.exe"),
                DbPath = BlastnDbPath[0..^4]
            };

            Owner.Tabs.Add(page);
            Owner.SelectedTabIndex = Owner.Tabs.Count - 1;
        }

        public void OpenAlignBlastn(object? parameter)
        {
            var owner = Owner;
            string exePath = Path.Combine(Owner.BlastBinDir, "blastn.exe");
            string dbPath = BlastnDbPath[0..^4];

            OpenWithAlignment(() => new BlastnPage(owner)
            {
                ExePath = exePath,
                DbPath = dbPath
            });
        }

        public bool CanOpenBlastn(object? parameter) => File.Exists(Path.Combine(Owner.BlastBinDir, "blastn.exe")) && File.Exists(BlastnDbPath);

        public void BrowseBlastpDb(object? parameter)
		{
            var dlg = new OpenFileDialog
            {
                Filter = "Protein sequence file (*.psq;*.pal)|*.psq;*.pal"
            };

            if (File.Exists(BlastpDbPath))
                dlg.InitialDirectory = Path.GetDirectoryName(BlastpDbPath);

            if (dlg.ShowDialog() == true)
			{
				BlastpDbPath = dlg.FileName;
				OpenBlastpCommand.OnCanExecuteChanged();
                OpenAlginBlastpCommand.OnCanExecuteChanged();
            }
		}

		public void OpenBlastp(object? parameter)
		{
            var page = new BlastpPage(Owner)
            {
                ExePath = Path.Combine(Owner.BlastBinDir, "blastp.exe"),
                DbPath = BlastpDbPath[0..^4]
            };

            Owner.Tabs.Add(page);
			Owner.SelectedTabIndex = Owner.Tabs.Count - 1;
		}

		public void OpenAlignBlastp(object? parameter)
        {
            var owner = Owner;
            string exePath = Path.Combine(Owner.BlastBinDir, "blastp.exe");
            string dbPath = BlastpDbPath[0..^4];

            OpenWithAlignment(() => new BlastpPage(owner)
            {
                ExePath = exePath,
                DbPath = dbPath
            });
		}

		public bool CanOpenBlastp(object? parameter) => File.Exists(Path.Combine(Owner.BlastBinDir, "blastp.exe")) && File.Exists(BlastpDbPath);

        public void BrowseBlastxDb(object? parameter)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Protein sequence file (*.psq;*.pal)|*.psq;*.pal"
            };

            if (File.Exists(BlastxDbPath))
                dlg.InitialDirectory = Path.GetDirectoryName(BlastxDbPath);

            if (dlg.ShowDialog() == true)
            {
                BlastxDbPath = dlg.FileName;
                OpenBlastxCommand.OnCanExecuteChanged();
                OpenAlginBlastxCommand.OnCanExecuteChanged();
            }
        }

        public void OpenBlastx(object? parameter)
        {
            var page = new BlastxPage(Owner)
            {
                ExePath = Path.Combine(Owner.BlastBinDir, "blastx.exe"),
                DbPath = BlastxDbPath[0..^4]
            };

            Owner.Tabs.Add(page);
            Owner.SelectedTabIndex = Owner.Tabs.Count - 1;
        }

        public void OpenAlignBlastx(object? parameter)
        {
            var owner = Owner;
            string exePath = Path.Combine(Owner.BlastBinDir, "blastx.exe");
            string dbPath = BlastxDbPath[0..^4];

            OpenWithAlignment(() => new BlastxPage(owner)
            {
                ExePath = exePath,
                DbPath = dbPath
            });
        }

        public bool CanOpenBlastx(object? parameter) => File.Exists(Path.Combine(Owner.BlastBinDir, "blastx.exe")) && File.Exists(BlastxDbPath);

        private void OpenWithAlignment(Func<BlastPage> pageInitializer)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Alignment file(*.fa*)|*.fa*"
            };

            if (Directory.Exists(Settings.Default.SeqFileDir))
                dlg.InitialDirectory = Settings.Default.SeqFileDir;

            if (dlg.ShowDialog() == true)
            {
                string? name = null;
                var lines = new List<string>();
                int ncpu = Environment.ProcessorCount;
                int concurrents = 0;

                Settings.Default.SeqFileDir = Path.GetDirectoryName(dlg.FileName);

                using (var sr = File.OpenText(dlg.FileName))
                {
                    string? line = null;
                    bool added = false;
                    do
                    {
                        line = sr.ReadLine();

                        if (line == null || line.StartsWith('>'))
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
                            name = line?[1..];
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