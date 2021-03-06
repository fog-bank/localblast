﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            CloseCommand = new DelegateCommand(Close);
        }

        public DelegateCommand RunCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public DelegateCommand LoadSequenceCommand { get; }
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

            var psi = new ProcessStartInfo(ExePath);
            psi.Arguments = "-db \"" + DbPath + "\" -query \"" + queryPath + "\" -out \"" + outPath + "\" -outfmt 16";
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