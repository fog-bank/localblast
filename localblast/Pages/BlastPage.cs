﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
        private string? exePath;
        private string? dbPath;
        private string? query;
        private string? jobTitle;
        private string jobId = Guid.NewGuid().ToString();
        private bool cleanup = true;

        private bool running;
        private CancellationTokenSource? cts;

        private int queryLength;
        private string? message;
        private List<Hit>? hits;
        private ObservableHeadCollection<Hit>? hitsView;
        private Hit? selectedHit;
        private SegmentPair? selectedSegment;
        private double zoomLevel = 1;

        protected BlastPage(MainViewModel owner)
            : base(owner)
        {
            State = PageState.New;

            RunCommand = new DelegateCommand(Run, CanRun);
            CancelCommand = new DelegateCommand(Cancel, CanCancel);
            LoadSequenceCommand = new DelegateCommand(LoadSequence, CanLoadSequence);
            ViewMoreHitsCommand = new DelegateCommand(ViewMoreHits, CanViewMoreHits);
            ViewAllHitsCommand = new DelegateCommand(ViewAllHits, CanViewMoreHits);
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
        public DelegateCommand ViewMoreHitsCommand { get; }
        public DelegateCommand ViewAllHitsCommand { get; }
        public DelegateCommand PreviousHitCommand { get; }
        public DelegateCommand NextHitCommand { get; }
        public DelegateCommand PreviousSegmentPairCommand { get; }
        public DelegateCommand NextSegmentPairCommand { get; }
        public DelegateCommand CopyAlignmentCommand { get; }
        public override DelegateCommand CloseCommand { get; }

        public override string HeaderTooltip => string.Join(Environment.NewLine, 
            Header,
            "Program: " + Path.GetFileNameWithoutExtension(ExePath),
            "Database: " + Path.GetFileNameWithoutExtension(DbPath));

        public string? ExePath
        {
            get => exePath;
            set
            {
                exePath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HeaderTooltip));
            }
        }

        public string? DbPath
        {
            get => dbPath;
            set
            {
                dbPath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HeaderTooltip));
            }
        }

        public string? Query
        {
            get => query;
            set
            {
                if (value != null)
                {
                    if (value.StartsWith(">"))
                    {
                        int index = value.IndexOf('\n');

                        if (index >= 2)
                            JobTitle = value.Substring(1, index).Trim();

                        value = value[(index + 1)..];
                    }
                    else if (value.StartsWith("#"))
                    {
                        int index = value.IndexOf('\n');

                        if (index >= 2)
                            JobTitle = value.Substring(1, index).Trim();

                        // DNA
                        int index2 = value.IndexOf("1\r\n", index + 1);

                        if (index2 != -1)
                            value = value[(index2 + 3)..];
                        else
                        {
                            // Protein
                            index2 = value.IndexOf("0\r\n", index + 1);

                            if (index2 != -1)
                                value = value[(index2 + 3)..];
                        }
                    }
                }
                query = value;
                OnPropertyChanged();
                RunCommand.OnCanExecuteChanged();
            }
        }

        public string? JobTitle
        {
            get => jobTitle;
            set
            {
                jobTitle = value;
                OnPropertyChanged();

                Header = JobTitle?.Split(' ')[0] ?? string.Empty;
            }
        }

        public string JobID
        {
            get => jobId;
            set
            {
                jobId = value;
                OnPropertyChanged();
            }
        }

        public bool EnableCleanup
        {
            get => cleanup;
            set
            {
                cleanup = value;
                OnPropertyChanged();
            }
        }

        public int QueryLength
        {
            get => queryLength;
            set
            {
                queryLength = value;
                OnPropertyChanged();
            }
        }

        public string? Message
        {
            get => message;
            set
            {
                message = value;
                OnPropertyChanged();
            }
        }

        public int TotalHits => hits == null ? 0 : hits.Count;

        public ObservableHeadCollection<Hit>? Hits
        {
            get => hitsView;
            set
            {
                hitsView = value;

                if (value == null)
                    hits = null;

                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalHits));
                ViewMoreHitsCommand.OnCanExecuteChanged();
                ViewAllHitsCommand.OnCanExecuteChanged();
            }
        }

        public Hit? SelectedHit
        {
            get => selectedHit;
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

        public SegmentPair? SelectedSegment
        {
            get => selectedSegment;
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

        public double ZoomLevel
        {
            get => zoomLevel;
            set
            {
                if (value <= 0 || value > 1)
                    throw new ArgumentOutOfRangeException(nameof(value));

                zoomLevel = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ZoomedThickness));
            }
        }

        public Thickness ZoomedThickness => new(1 / ZoomLevel, 1, 1 / ZoomLevel, 1);

        public void LoadSequence(object? parameter)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Alignment file(*.fa*)|*.fa*"
            };

            if (Directory.Exists(Settings.Default.SeqFileDir))
                dlg.InitialDirectory = Settings.Default.SeqFileDir;

            if (dlg.ShowDialog() == true)
            {
                Settings.Default.SeqFileDir = Path.GetDirectoryName(dlg.FileName);

                string? name = null;
                var lines = new List<string>();

                using (var sr = File.OpenText(dlg.FileName))
                {
                    for (string? line = sr.ReadLine(); line != null; line = sr.ReadLine())
                    {
                        if (line.StartsWith(">"))
                        {
                            if (name != null)
                                break;

                            name = line[1..];
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

        public bool CanLoadSequence(object? parameter) => !running;

        public async void Run(object? parameter)
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

            var arglist = new Dictionary<string, string>
            {
                ["db"] = "\"" + DbPath + "\"",
                ["query"] = "\"" + queryPath + "\"",
                ["out"] = "\"" + outPath + "\"",
                ["outfmt"] = "16",
                ["num_threads"] = Owner.NumberOfThreads.ToString(CultureInfo.InvariantCulture)
            };

            SetArgument(arglist);

            var sb = new StringBuilder();

            foreach (var pair in arglist)
                sb.Append('-').Append(pair.Key).Append(' ').Append(pair.Value).Append(' ');

            sb.Length--;

            var psi = new ProcessStartInfo(ExePath)
            {
                Arguments = sb.ToString(),
                UseShellExecute = false,
                CreateNoWindow = true
            };

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
                    hits = await Task.Run(() =>
                    {
                        var hits = new List<Hit>();

                        var xml = XDocument.Load(outPath);
                        //var xml = XDocument.Load(testOutPath);
                        var ns = XNamespace.Get("http://www.ncbi.nlm.nih.gov");

                        var search = xml.Descendants(ns + "Search").First();
                        int queryLength = (int?)search.Element(ns + "query-len") ?? 0;
                        string? message = (string?)search.Element(ns + "message");

                        App.Current.Dispatcher.Invoke(() =>
                        {
                            QueryLength = queryLength;
                            Message = string.IsNullOrWhiteSpace(message) ? null : message;
                        });

                        foreach (var hit in search.Descendants(ns + "Hit"))
                            hits.Add(new Hit(this, ns, hit, queryLength));

                        return hits;
                    });

                    Hits = new ObservableHeadCollection<Hit>(hits, Math.Min(TotalHits, Owner.InitialBlastHitsView));
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

        private static Task<int> RunBlast(ProcessStartInfo psi, CancellationToken ct)
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

        public bool CanRun(object? parameter)
        {
            return !running && !string.IsNullOrWhiteSpace(Query);
        }

        public void Cancel(object? parameter)
        {
            cts?.Cancel();
            State = PageState.Error;
        }

        public bool CanCancel(object? parameter)
        {
            return cts != null && !cts.IsCancellationRequested;
        }

        public void ViewMoreHits(object? parameter)
        {
            Hits!.ViewMoreItems(Owner.InitialBlastHitsView);
            ViewMoreHitsCommand.OnCanExecuteChanged();
            ViewAllHitsCommand.OnCanExecuteChanged();
            NextHitCommand.OnCanExecuteChanged();
        }

        public bool CanViewMoreHits(object? parameter)
        {
            return Hits != null && Hits.Count < TotalHits;
        }

        public void ViewAllHits(object? parameter)
        {
            Hits!.ViewAllItems();
            ViewMoreHitsCommand.OnCanExecuteChanged();
            ViewAllHitsCommand.OnCanExecuteChanged();
            NextHitCommand.OnCanExecuteChanged();
        }

        public void PreviousHit(object? parameter)
        {
            SelectedHit = Hits![SelectedHit!.Index - 2];
        }

        public bool CanPreviousHit(object? parameter)
        {
            return Hits != null && SelectedHit != null && SelectedHit.Index >= 2 && SelectedHit.Index <= Hits.Count;
        }

        public void NextHit(object? parameter)
        {
            SelectedHit = Hits![SelectedHit!.Index];
        }

        public bool CanNextHit(object? parameter)
        {
            return Hits != null && SelectedHit != null && SelectedHit.Index >= 1 && SelectedHit.Index <= Hits.Count - 1;
        }

        public void PreviousSegmentPair(object? parameter)
        {
            SelectedSegment = SelectedHit!.Segments[SelectedSegment!.Index - 2];
        }

        public bool CanPreviousSegmentPair(object? parameter)
        {
            return SelectedHit != null && SelectedSegment != null && SelectedSegment.Index >= 2 && SelectedSegment.Index <= SelectedHit.Segments.Count;
        }

        public void NextSegmentPair(object? parameter)
        {
            SelectedSegment = SelectedHit!.Segments[SelectedSegment!.Index];
        }

        public bool CanNextSegmentPair(object? parameter)
        {
            return SelectedHit != null && SelectedSegment != null && SelectedSegment.Index >= 1 && SelectedSegment.Index <= SelectedHit.Segments.Count - 1;
        }

        public void CopyAlignment(object? parameter)
        {
            var seg = SelectedSegment;

            if (seg == null || seg.QuerySeq == null || seg.HitSeq == null)
                return;

            var sb = new StringBuilder();
            string queryName = sb.Append("Query:").Append(seg.QueryFrom).Append("..").Append(seg.QueryTo).ToString();
            sb.Clear();

            string hitName = sb.Append("Hit:").Append(seg.HitFrom).Append("..").Append(seg.HitTo).ToString();
            sb.Clear();

            sb.Capacity = queryName.Length + seg.QuerySeq.Length + hitName.Length + seg.HitSeq.Length + 5;

            sb.Append('>').AppendLine(queryName).AppendLine(seg.QuerySeq).Append('>').AppendLine(hitName).Append(seg.HitSeq);
            Clipboard.SetText(sb.ToString());
        }

        public virtual void Close(object? parameter)
        {
            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
                cts = null;
            }
            Owner.Tabs.Remove(this);
        }

        protected virtual void SetArgument(Dictionary<string, string> arglist)
        {
        }
    }
}
