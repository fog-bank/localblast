using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using Microsoft.Win32;

namespace LocalBlast
{
    public abstract class BlastPage : TabPage
    {
        private string exePath;
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

        protected BlastPage(MainViewModel owner)
            : base(owner)
        {
            State = PageState.New;

            RunCommand = new DelegateCommand(Run, CanRun);
            CancelCommand = new DelegateCommand(Cancel, CanCancel);
            LoadSequenceCommand = new DelegateCommand(LoadSequence, CanLoadSequence);
            PreviousHitCommand = new DelegateCommand(PreviousHit, CanPreviousHit);
            NextHitCommand = new DelegateCommand(NextHit, CanNextHit);
            PreviousSegmentPairCommand = new DelegateCommand(PreviousSegmentPair, CanPreviousSegmentPair);
            NextSegmentPairCommand = new DelegateCommand(NextSegmentPair, CanNextSegmentPair);
            CopyAlignmentCommand = new DelegateCommand(CopyAlignment);
            CloseCommand = new DelegateCommand(Close);
        }

        public DelegateCommand RunCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public DelegateCommand LoadSequenceCommand { get; }
        public DelegateCommand PreviousHitCommand { get; }
        public DelegateCommand NextHitCommand { get; }
        public DelegateCommand PreviousSegmentPairCommand { get; }
        public DelegateCommand NextSegmentPairCommand { get; }
        public DelegateCommand CopyAlignmentCommand { get; }
        public override DelegateCommand CloseCommand { get; }

        public string ExePath
        {
            get { return exePath; }
            set
            {
                exePath = value;
                OnPropertyChanged();
            }
        }

        public string DbPath
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
                    PreviousHitCommand.OnCanExecuteChanged();
                    NextHitCommand.OnCanExecuteChanged();
                    PreviousSegmentPairCommand.OnCanExecuteChanged();
                    NextSegmentPairCommand.OnCanExecuteChanged();
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
                    PreviousHitCommand.OnCanExecuteChanged();
                    NextHitCommand.OnCanExecuteChanged();
                    PreviousSegmentPairCommand.OnCanExecuteChanged();
                    NextSegmentPairCommand.OnCanExecuteChanged();
                }
            }
        }

        public void LoadSequence(object parameter)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "Alignment file(*.fa*)|*.fa*";

            if (dlg.ShowDialog() == true)
            {
                string name = null;
                var lines = new List<string>();

                using (var sr = File.OpenText(dlg.FileName))
                {
                    for (string line = sr.ReadLine(); line != null; line = sr.ReadLine())
                    {
                        if (line.StartsWith(">"))
                        {
                            if (name != null)
                                break;

                            name = line.Substring(1);
                        }
                        else
                            lines.Add(line);
                    }
                }
                JobTitle = name;
                Query = string.Join(Environment.NewLine, lines);
                Run(null);
            }
        }

        public bool CanLoadSequence(object parameter) => !running;

        public async void Run(object parameter)
        {
            State = PageState.Running;
            running = true;
            RunCommand.OnCanExecuteChanged();
            LoadSequenceCommand.OnCanExecuteChanged();

            SelectedHit = null;
            Hits = null;
            Message = null;

            Owner.EnsureWorkingDirectory();

            string queryPath = Path.Combine(Owner.WorkingDirectory, JobID + ".fas");
            string outPath = Path.Combine(Owner.WorkingDirectory, JobID + ".xml");

            File.WriteAllText(queryPath, ">" + JobTitle + Environment.NewLine + Query);

            var arglist = new Dictionary<string, string>();
            arglist["db"] = "\"" + DbPath + "\"";
            arglist["query"] = "\"" + queryPath + "\"";
            arglist["out"] = "\"" + outPath + "\"";
            arglist["outfmt"] = "16";

            SetArgument(arglist);

            var sb = new StringBuilder();

            foreach (var pair in arglist)
                sb.Append("-").Append(pair.Key).Append(" ").Append(pair.Value).Append(" ");

            sb.Length--;

            var psi = new ProcessStartInfo(ExePath);
            psi.Arguments = sb.ToString();
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
                            hits.Add(new Hit(this, ns, hit, queryLength));

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

            if (SelectedHit == null)
                Message = "Query length:  " + QueryLength + Environment.NewLine + Environment.NewLine + Message; 

            running = false;
            RunCommand.OnCanExecuteChanged();
            LoadSequenceCommand.OnCanExecuteChanged();
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

        public bool CanRun(object parameter)
        {
            return !running && !string.IsNullOrWhiteSpace(Query);
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

        public void PreviousHit(object parameter)
        {
            SelectedHit = Hits[SelectedHit.Index - 2];
        }

        public bool CanPreviousHit(object parameter)
        {
            return SelectedHit != null && SelectedHit.Index >= 2 && SelectedHit.Index <= Hits.Count;
        }

        public void NextHit(object parameter)
        {
            SelectedHit = Hits[SelectedHit.Index];
        }

        public bool CanNextHit(object parameter)
        {
            return SelectedHit != null && SelectedHit.Index >= 1 && SelectedHit.Index <= Hits.Count - 1;
        }

        public void PreviousSegmentPair(object parameter)
        {
            SelectedSegment = SelectedHit.Segments[SelectedSegment.Index - 2];
        }

        public bool CanPreviousSegmentPair(object parameter)
        {
            return SelectedHit != null && SelectedSegment != null && SelectedSegment.Index >= 2 && SelectedSegment.Index <= SelectedHit.Segments.Count;
        }

        public void NextSegmentPair(object parameter)
        {
            SelectedSegment = SelectedHit.Segments[SelectedSegment.Index];
        }

        public bool CanNextSegmentPair(object parameter)
        {
            return SelectedHit != null && SelectedSegment != null && SelectedSegment.Index >= 1 && SelectedSegment.Index <= SelectedHit.Segments.Count - 1;
        }

        public void CopyAlignment(object parameter)
        {
            if (SelectedSegment == null)
                return;

            var seg = SelectedSegment;

            var sb = new StringBuilder();
            string queryName = sb.Append("Query:").Append(seg.QueryFrom).Append("..").Append(seg.QueryTo).ToString();
            sb.Clear();

            string hitName = sb.Append("Hit:").Append(seg.HitFrom).Append("..").Append(seg.HitTo).ToString();
            sb.Clear();

            sb.Capacity = queryName.Length + seg.QuerySeq.Length + hitName.Length + seg.HitSeq.Length + 5;

            sb.Append(">").AppendLine(queryName).AppendLine(seg.QuerySeq).Append(">").AppendLine(hitName).Append(seg.HitSeq);
            Clipboard.SetText(sb.ToString());
        }

        public virtual void Close(object parameter)
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

        protected virtual void SetArgument(Dictionary<string, string> arglist)
        {
        }
    }
}