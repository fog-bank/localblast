using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

namespace LocalBlast
{
	public class NewPage : TabPage
	{
		private int index = 1;

		private string blastpPath = Settings.Default.BlastpExePath;
		private string blastpDatabasePath = Settings.Default.BlastpDbPath;

		public NewPage(MainViewModel owner)
			: base(owner)
		{
			Header = "New job";

			BrowseBlastpCommand = new DelegateCommand(BrowseBlastp);
			BrowseBlastpDbCommand = new DelegateCommand(BrowseBlastpDb);
			OpenBlastpCommand = new DelegateCommand(OpenBlastp, CanOpenBlastp);
			OpenAlginBlastpCommand = new DelegateCommand(OpenAlignBlastp, CanOpenBlastp);

			CloseCommand = new DelegateCommand(null, _ => false);
		}

		public DelegateCommand BrowseBlastpCommand { get; }
		public DelegateCommand BrowseBlastpDbCommand { get; }
		public DelegateCommand OpenBlastpCommand { get; }
		public DelegateCommand OpenAlginBlastpCommand { get; }

		public string BlastpPath
		{
			get { return blastpPath; }
			set
			{
				blastpPath = value;
				Settings.Default.BlastpExePath = value;
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

		public override DelegateCommand CloseCommand { get; }

		public void BrowseBlastp(object parameter)
		{
			var dlg = new OpenFileDialog();
			dlg.Filter = "blastp program|blastp.exe";

			if (dlg.ShowDialog() == true)
			{
				BlastpPath = dlg.FileName;
				OpenBlastpCommand.OnCanExecuteChanged();
                OpenAlginBlastpCommand.OnCanExecuteChanged();
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
			var blastp = new BlastpPage(Owner);
			blastp.BlastpPath = BlastpPath;
			blastp.BlastpDbPath = BlastpDbPath.Substring(0, BlastpDbPath.Length - 4);
			blastp.JobTitle = "blastp #" + index++;

			Owner.Tabs.Add(blastp);
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
								blastp.BlastpPath = BlastpPath;
								blastp.BlastpDbPath = BlastpDbPath.Substring(0, BlastpDbPath.Length - 4);
								blastp.JobTitle = "blastp #" + index++;

								blastp.JobTitle = name;
								blastp.Query = string.Join(Environment.NewLine, lines);

								if (blastp.CanRun(null))
									blastp.Run(null);

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

		public bool CanOpenBlastp(object parameter) => File.Exists(BlastpPath) && File.Exists(BlastpDbPath);
	}
}