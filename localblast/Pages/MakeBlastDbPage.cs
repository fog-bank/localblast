using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LocalBlast
{
    public class MakeBlastDbPage : TabPage
    {
        private MakeBlastDbType[] dbTypes = { MakeBlastDbType.Nucleotide, MakeBlastDbType.Protein };
        private MakeBlastDbType dbType = MakeBlastDbType.Nucleotide;
        private string input;
        private string output;

        private bool running;
        private CancellationTokenSource cts;

        public MakeBlastDbPage(MainViewModel owner)
            : base(owner)
        {
            Header = "makeblastdb";
            State = PageState.New;

            RunCommand = new DelegateCommand(Run, CanRun);
            CancelCommand = new DelegateCommand(Cancel, CanCancel);
            CloseCommand = new DelegateCommand(Close);
        }

        public DelegateCommand RunCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public override DelegateCommand CloseCommand { get; }

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

        public string Input
        {
            get { return input; }
            set
            {
                input = value;
                OnPropertyChanged();

                if (string.IsNullOrEmpty(input))
                    Output = Path.Combine(Path.GetDirectoryName(value), Path.GetFileNameWithoutExtension(value));
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

        public async void Run(object parameter)
        {
            State = PageState.Running;
            running = true;
            RunCommand.OnCanExecuteChanged();

            Owner.EnsureWorkingDirectory();

            //string queryPath = Path.Combine(Owner.WorkingDirectory, JobID + ".fas");
            //string outPath = Path.Combine(Owner.WorkingDirectory, JobID + ".xml");

            //File.WriteAllText(queryPath, ">" + JobTitle + Environment.NewLine + Query);

            //var psi = new ProcessStartInfo(BlastpPath);
            //psi.Arguments = "-query \"" + queryPath + "\" -out \"" + outPath + "\" -outfmt 16";
            //psi.WindowStyle = ProcessWindowStyle.Hidden;

            using (cts = new CancellationTokenSource())
            {
                CancelCommand.OnCanExecuteChanged();

                Task<int> task = null;
                try
                {
                    await task;
                }
                catch
                { }

                if (cts != null && !cts.IsCancellationRequested && task.IsCompleted && task.Result == 0 && File.Exists(output))
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

        public bool CanRun(object parameter)
        {
            return !running && File.Exists(input);
        }

        public void Cancel(object parameter)
        {
            cts?.Cancel();
            State = PageState.Error;
        }

        public bool CanCancel(object parameter)
        {
            return cts != null && !cts.IsCancellationRequested;
        }

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
