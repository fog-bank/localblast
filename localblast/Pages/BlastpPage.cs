using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LocalBlast
{
    public class BlastpPage : TabPage
    {
        private string path;
        private string dbPath;
        private string query;
        private string jobTitle;
        private string jobId = Guid.NewGuid().ToString();
        private bool cleanup = true;

        private bool running;
        private CancellationTokenSource cts;

        private int queryLength;
        private string message;
        private List<Hit> hits = new List<Hit>();
        private Hit selectedHit;
        private SegmentPair selectedSegment;

        public BlastpPage(MainViewModel owner)
            : base(owner)
        {
            Header = "blastp";
            State = PageState.New;

            RunCommand = new DelegateCommand(Run, CanRun);
            CancelCommand = new DelegateCommand(Cancel, CanCancel);
            CloseCommand = new DelegateCommand(Close);
        }

        public DelegateCommand RunCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public override DelegateCommand CloseCommand { get; }

        public string BlastpPath
        {
            get { return path; }
            set
            {
                path = value;
                OnPropertyChanged();
            }
        }

        public string BlastpDbPath
        {
            get { return dbPath; }
            set
            {
                dbPath = value;
                OnPropertyChanged();
            }
        }

        public string Query
        {
            get { return query; }
            set
            {
                if (value != null && value.StartsWith(">"))
                {
                    int index = value.IndexOf('\n');

                    if (index >= 2)
                    {
                        JobTitle = value.Substring(1, index).Trim();
                        value = value.Substring(index + 1);
                    }
                }
                query = value;
                OnPropertyChanged();
                RunCommand.OnCanExecuteChanged();
            }
        }

        public string JobTitle
        {
            get { return jobTitle; }
            set
            {
                jobTitle = value;
                OnPropertyChanged();

                Header = JobTitle;
            }
        }

        public string JobID
        {
            get { return jobId; }
            set
            {
                jobId = value;
                OnPropertyChanged();
            }
        }

        public bool EnableCleanup
        {
            get { return cleanup; }
            set
            {
                cleanup = value;
                OnPropertyChanged();
            }
        }

        public int QueryLength
        {
            get { return queryLength; }
            set
            {
                queryLength = value;
                OnPropertyChanged();
            }
        }

        public string Message
        {
            get { return message; }
            set
            {
                message = value;
                OnPropertyChanged();
            }
        }

        public List<Hit> Hits
        {
            get { return hits; }
            set
            {
                hits = value;
                OnPropertyChanged();
            }
        }

        public Hit SelectedHit
        {
            get { return selectedHit; }
            set
            {
                if (selectedHit != value)
                {
                    selectedHit = value;
                    selectedSegment = value?.Segments?.FirstOrDefault();
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SelectedSegment));
                }
            }
        }

        public SegmentPair SelectedSegment
        {
            get { return selectedSegment; }
            set
            {
                if (selectedSegment != value)
                {
                    selectedSegment = value;
                    selectedHit = value?.Parent;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SelectedHit));
                }
            }
        }

        public string QueryPaneHeight
        {
            get { return Settings.Default.BlastpQueryPaneHeight; }
            set
            {
                Settings.Default.BlastpQueryPaneHeight = value;
                OnPropertyChanged();
            }
        }

        public string ResultPaneHeight
        {
            get { return Settings.Default.BlastpResultPaneHeight; }
            set
            {
                Settings.Default.BlastpResultPaneHeight = value;
                OnPropertyChanged();
            }
        }

        public string DescPaneHeight
        {
            get { return Settings.Default.BlastpDescPaneHeight; }
            set
            {
                Settings.Default.BlastpDescPaneHeight = value;
                OnPropertyChanged();
            }
        }

        public async void Run(object parameter)
        {
            State = PageState.Running;
            running = true;
            RunCommand.OnCanExecuteChanged();

            SelectedHit = null;
            Hits = null;

            Owner.EnsureWorkingDirectory();

            string queryPath = Path.Combine(Owner.WorkingDirectory, JobID + ".fas");
            string outPath = Path.Combine(Owner.WorkingDirectory, JobID + ".xml");

            File.WriteAllText(queryPath, ">" + JobTitle + Environment.NewLine + Query);

            var psi = new ProcessStartInfo(BlastpPath);
            psi.Arguments = "-db \"" + BlastpDbPath + "\" -query \"" + queryPath + "\" -out \"" + outPath + "\" -outfmt 16";
            psi.WindowStyle = ProcessWindowStyle.Hidden;

            using (cts = new CancellationTokenSource())
            {
                CancelCommand.OnCanExecuteChanged();

                var task = RunBlast(psi, cts.Token);
                try
                {
                    await task;
                }
                catch
                { }

                if (cts != null && !cts.IsCancellationRequested && task.IsCompleted && task.Result == 0 && File.Exists(outPath))
                {
                    Hits = await Task.Run(() =>
                    {
                        var hits = new List<Hit>();

                        var xml = XDocument.Load(outPath);
                        //var xml = XDocument.Load(testOutPath);
                        var ns = XNamespace.Get("http://www.ncbi.nlm.nih.gov");

                        var search = xml.Descendants(ns + "Search").First();
                        int queryLength = (int)search.Element(ns + "query-len");
                        string message = (string)search.Element(ns + "message");

                        App.Current.Dispatcher.Invoke(() =>
                        {
                            QueryLength = queryLength;
                            Message = string.IsNullOrWhiteSpace(message) ? null : message;
                        });

                        foreach (var hit in search.Descendants(ns + "Hit"))
                            hits.Add(new Hit(this, hit, ns));

                        return hits;
                    });

                    State = PageState.Completed;
                }
                else
                    State = PageState.Error;
            }
            cts = null;
            CancelCommand.OnCanExecuteChanged();

            if (EnableCleanup)
            {
                File.Delete(queryPath);
                File.Delete(outPath);
            }

            SelectedHit = Hits?.FirstOrDefault();

            running = false;
            RunCommand.OnCanExecuteChanged();
        }

        private Task<int> RunBlast(ProcessStartInfo psi, CancellationToken ct)
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

                proc.Exited += (sender, e) =>
                {
                    tcs.TrySetResult(proc.ExitCode);
                    proc.Dispose();
                };

                proc.Start();
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
            return tcs.Task;
        }

        public bool CanRun(object parameter) => !running && !string.IsNullOrWhiteSpace(Query);

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

            SelectedHit = null;
            Hits = null;
        }
    }
}