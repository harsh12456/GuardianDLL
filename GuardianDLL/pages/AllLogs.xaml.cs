using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Threading;

namespace GuardianDLL
{
    public partial class AllLogsView : System.Windows.Controls.UserControl
    {
        private FileSystemWatcher _watcher;
        public ObservableCollection<string> Logs { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> SuspiciousLogs { get; set; } = new ObservableCollection<string>();
        private DllLoadEtwMonitor _etwMonitor;

        public AllLogsView()
        {
            InitializeComponent();
            DataContext = this;
            StartWatcher("C:\\");
            LoadLogsToUI();

            // Start ETW-based DLL load monitoring
            _etwMonitor = new DllLoadEtwMonitor(IsSuspiciousHeuristic, HasValidSignature);
            _etwMonitor.OnDllLoaded += EtwMonitor_OnDllLoaded;
            _etwMonitor.Start();

            this.Unloaded += AllLogsView_Unloaded;
        }

        private void AllLogsView_Unloaded(object sender, RoutedEventArgs e)
        {
            _etwMonitor?.Dispose();
        }

        private void EtwMonitor_OnDllLoaded(string dllPath, string processName, int processId, bool isSuspicious, bool isMalicious)
        {
            string status = isMalicious ? "malicious" : isSuspicious ? "suspicious" : "normal";
            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] DLL Loaded: {dllPath} by {processName} (PID: {processId}) [{status.ToUpper()}]";

            Dispatcher.Invoke(() =>
            {
                Logs.Add(logEntry);
                if (isMalicious || isSuspicious)
                {
                    string suspiciousEntry = $"[{status.ToUpper()}] DLL Loaded: {dllPath} by {processName} (PID: {processId})";
                    SuspiciousLogs.Add(suspiciousEntry);
                }
                AddLogToUI(logEntry, isMalicious || isSuspicious);
                TrimLogs();
            });
        }

        private void StartWatcher(string path)
        {
            _watcher = new FileSystemWatcher
            {
                Path = path,
                Filter = "*.dll",
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime
            };

            _watcher.Created += OnChanged;
            _watcher.Changed += OnChanged;
            _watcher.Deleted += OnChanged;
            _watcher.Renamed += OnRenamed;

            _watcher.EnableRaisingEvents = true;
            Logs.Add($"[INFO] Watching path: {path} for DLL changes");
        }

        private bool HasValidSignature(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return false;
                var cert = X509Certificate.CreateFromSignedFile(filePath);
                return cert != null;
            }
            catch
            {
                return false;
            }
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            string fileName = Path.GetFileName(e.FullPath).ToLower();

            bool isSuspicious = false;

            // Example heuristics
            if (fileName.Contains("temp") || fileName.Contains("cloudflare") || fileName.EndsWith(".bat") || fileName.Contains("xworm"))
            {
                isSuspicious = true;
            }
            if (Path.GetExtension(fileName) == ".dll" && !HasValidSignature(e.FullPath))
            {
                isSuspicious = true;
            }

            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {e.ChangeType}: {e.FullPath}";
            Dispatcher.Invoke(() =>
            {
                Logs.Add(logEntry);
                if (isSuspicious)
                {
                    string suspiciousEntry = $"[SUSPICIOUS] {e.ChangeType}: {e.FullPath}";
                    SuspiciousLogs.Add(suspiciousEntry);
                }
                AddLogToUI(logEntry, isSuspicious);
                TrimLogs();
            });
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [Renamed] {e.OldFullPath} -> {e.FullPath}";
            Dispatcher.Invoke(() =>
            {
                Logs.Add(logEntry);
                string suspiciousEntry = $"[SUSPICIOUS] [Renamed] {e.OldFullPath} -> {e.FullPath}";
                SuspiciousLogs.Add(suspiciousEntry);
                AddLogToUI(logEntry, true);
                TrimLogs();
            });
        }

        private void LoadLogsToUI()
        {
            DummyLogsPanel.Children.Clear();
            foreach (var log in Logs)
            {
                AddLogToUI(log, log.Contains("[SUSPICIOUS]"));
            }
        }

        private void AddLogToUI(string log, bool isSuspicious)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 31, 31)),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(51, 51, 51)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 10)
            };

            var stack = new StackPanel();

            var topRow = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
            var timestamp = log.Length > 21 && log[0] == '[' ? log.Substring(1, 19) : DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            topRow.Children.Add(new TextBlock
            {
                Text = timestamp,
                Foreground = System.Windows.Media.Brushes.LightGray,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 12
            });

            var badge = new TextBlock
            {
                Text = isSuspicious ? "suspicious" : "normal",
                Foreground = isSuspicious ? System.Windows.Media.Brushes.White : System.Windows.Media.Brushes.Black,
                Background = isSuspicious ? System.Windows.Media.Brushes.OrangeRed : System.Windows.Media.Brushes.LightGray,
                FontSize = 10,
                Margin = new Thickness(10, 0, 0, 0),
                Padding = new Thickness(4, 1, 4, 1),
                FontWeight = FontWeights.Bold
            };
            topRow.Children.Add(badge);

            stack.Children.Add(topRow);
            stack.Children.Add(new TextBlock
            {
                Text = log,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 14
            });

            border.Child = stack;
            DummyLogsPanel.Children.Add(border);
        }

        private bool IsSuspiciousHeuristic(string path)
        {
            string lowerPath = path.ToLower();
            string fileName = Path.GetFileName(path).ToLower();

            return lowerPath.Contains("temp") ||
                   lowerPath.Contains("appdata\\roaming") ||
                   lowerPath.Contains("updater") ||
                   fileName.Contains("update") ||
                   fileName.Contains("cloudflare") ||
                   fileName.Contains("xworm") ||
                   fileName.Contains("inject") ||
                   fileName.Contains("patch") ||
                   (fileName.StartsWith("sys") && fileName.Contains("helper"));
        }

        private void TrimLogs()
        {
            const int maxLogs = 1000;
            while (Logs.Count > maxLogs) Logs.RemoveAt(0);
            while (SuspiciousLogs.Count > maxLogs) SuspiciousLogs.RemoveAt(0);
        }
    }
}