using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace LocalBlast
{
    public class MakeBlastDbPage : TabPage
    {
        private string path;
        private string input;
        private string output;
        private MakeBlastDbType[] dbTypes = { MakeBlastDbType.Nucleotide, MakeBlastDbType.Protein };
        private MakeBlastDbType dbType = MakeBlastDbType.Nucleotide;

        private bool running;
        private CancellationTokenSource cts;
        private ObservableCollection<string> stdout = new ObservableCollection<string>();

        public MakeBlastDbPage(MainViewModel owner)
            : base(owner)
        {
            Header = "makeblastdb";
            State = PageState.New;

            RunCommand = new DelegateCommand(Run, CanRun);
            CancelCommand = new DelegateCommand(Cancel, CanCancel);
            BrowseInputFileCommand = new DelegateCommand(BrowseInputFile);
            BrowseOutputFileCommand = new DelegateCommand(BrowseOutputFile);
            CloseCommand = new DelegateCommand(Close);
        }

        public DelegateCommand RunCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public DelegateCommand BrowseInputFileCommand { get; }
        public DelegateCommand BrowseOutputFileCommand { get; }
        public override DelegateCommand CloseCommand { get; }

        public string MakeBlastDbPath
        {
            get { return path; }
            set
            {
                path = value;
                OnPropertyChanged();
            }
        }

        public string Input
        {
            get { return input; }
            set
            {
                input = value;
                OnPropertyChanged();

                if (string.IsNullOrEmpty(Output))
                    Output = Path.Combine(Path.GetDirectoryName(value), Path.GetFileNameWithoutExtension(value));

                RunCommand.OnCanExecuteChanged();
            }
        }

        public string Output
        {
            get { return output; }
            set
            {
                output = value;
                OnPropertyChanged();
            }
        }

        public MakeBlastDbType[] DbTypes => dbTypes;

        public MakeBlastDbType DbType
        {
            get { return dbType; }
            set
            {
                dbType = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> StdOut => stdout;

        public void BrowseInputFile(object parameter)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "All Files|*.*";

            if (dlg.ShowDialog() == true)
            {
                Input = dlg.FileName;
            }
        }

        public void BrowseOutputFile(object parameter)
        {
            var dlg = new SaveFileDialog();
            dlg.Filter = "All Files|*";

            if (!string.IsNullOrWhiteSpace(Output))
                dlg.FileName = Output;

            if (dlg.ShowDialog() == true)
            {
                Output = dlg.FileName;
            }
        }

        public async void Run(object parameter)
        {
            State = PageState.Running;
            running = true;
            RunCommand.OnCanExecuteChanged();
            StdOut.Clear();

            string workdir = Path.GetDirectoryName(Output);

            if (!Directory.Exists(workdir))
                Directory.CreateDirectory(workdir);

            var psi = new ProcessStartInfo(MakeBlastDbPath);
            psi.Arguments = "-dbtype \"" + (DbType == MakeBlastDbType.Protein ? "prot" : "nucl") + "\" -in \"" + Input + "\" -out \"" + Output + "\"";
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.CreateNoWindow = true;

            using (cts = new CancellationTokenSource())
            {
                CancelCommand.OnCanExecuteChanged();

                var prog = new Progress<string>(msg => StdOut.Add(msg));

                var task = RunMakeBlastDb(psi, cts.Token, prog);
                try
                {
                    await task;
                }
                catch
                { }

                if (cts != null && !cts.IsCancellationRequested && task.IsCompleted && task.Result == 0 && 
                    File.Exists(Output + (DbType == MakeBlastDbType.Protein ? ".psq" : ".nsq")))
                {
                    State = PageState.Completed;
                }
                else
                    State = PageState.Error;
            }
            cts = null;
            CancelCommand.OnCanExecuteChanged();

            running = false;
            RunCommand.OnCanExecuteChanged();
        }

        private Task<int> RunMakeBlastDb(ProcessStartInfo psi, CancellationToken ct, IProgress<string> progress)
        {
            var tcs = new TaskCompletionSource<int>();
            var proc = new Process();
            try
            {
                ct.Register(() =>
                {
                    if (proc != null && !proc.HasExited)
                        proc.Kill();
                });

                proc.EnableRaisingEvents = true;
                proc.StartInfo = psi;

                proc.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                        progress.Report(e.Data);
                };

                proc.Exited += (sender, e) =>
                {
                    tcs.TrySetResult(proc.ExitCode);
                    proc.Dispose();
                };

                proc.Start();
                proc.BeginOutputReadLine();
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
            return tcs.Task;
        }

        public bool CanRun(object parameter) => !running && File.Exists(input);

        public void Cancel(object parameter)
        {
            cts?.Cancel();
            State = PageState.Error;
        }

        public bool CanCancel(object parameter) => cts != null && !cts.IsCancellationRequested;

        public void Close(object parameter)
        {
            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
                cts = null;
            }
            Owner.Tabs.Remove(this);
        }
    }

    public enum MakeBlastDbType
    {
        Nucleotide,
        Protein
    }
}
