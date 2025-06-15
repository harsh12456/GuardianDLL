// GuardianDLL: Basic C# WPF Application to Monitor .dll File Events

using System;
using System.IO;
using System.Windows;
using System.Collections.ObjectModel;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Threading;


namespace GuardianDLL
{
    public partial class MainWindow : Window
    {
        private FileSystemWatcher _watcher;
        public ObservableCollection<string> Logs { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> SuspiciousLogs { get; set; } = new ObservableCollection<string>();


        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            StartWatcher("C:\\"); // Change path as needed
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

        private bool IsSuspicious(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return false;

                var cert = System.Security.Cryptography.X509Certificates.X509Certificate.CreateFromSignedFile(filePath);
                return cert == null;
            }
            catch
            {
                // If unable to verify, assume suspicious
                return true;
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

            if (isSuspicious)
            {
                string suspiciousEntry = $"[SUSPICIOUS] {e.ChangeType}: {e.FullPath}";
                SuspiciousLogs.Add(suspiciousEntry);
            }

        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            Dispatcher.Invoke(() =>
                Logs.Add($"[Renamed] {e.OldFullPath} -> {e.FullPath} at {DateTime.Now}")
            );
        }

        private void OpenSuspiciousWindow(object sender, RoutedEventArgs e)
        {
            SuspiciousWindow sw = new SuspiciousWindow(SuspiciousLogs);
            sw.Show();
        }
        private bool HasValidSignature(string filePath)
        {
            try
            {
                X509Certificate cert = X509Certificate.CreateFromSignedFile(filePath);
                X509Certificate2 cert2 = new X509Certificate2(cert);

                // Optional: You can do deeper checks here, like expiry or issuer.
                return !string.IsNullOrEmpty(cert2.Issuer);
            }
            catch
            {
                // Unsigned or invalid
                return false;
            }
        }
        private void ScanSuspiciousDlls(object sender, RoutedEventArgs e)
        {
            SuspiciousDllList.Items.Clear();

            Task.Run(() =>
            {
                Thread.Sleep(1000);

                Dispatcher.Invoke(() =>
                {
                    SuspiciousDllList.Items.Add("🛑 [UNSIGNED] C:\\Users\\Public\\AppData\\Local\\Temp\\cloudflare_update.dll");
                    SuspiciousDllList.Items.Add("⚠️ [INVALID CERT] C:\\ProgramData\\NVIDIA\\drivers\\nvTelemetry64.dll");
                    SuspiciousDllList.Items.Add("🧪 [SUSPICIOUS NAME] C:\\Users\\Sanskar\\AppData\\Roaming\\SystemCache\\xworm_helper.dll");
                    SuspiciousDllList.Items.Add("🛑 [UNSIGNED] C:\\Windows\\System32\\drivers\\hid_comms.dll");
                    SuspiciousDllList.Items.Add("⚠️ [TAMPERED CERT] C:\\Program Files (x86)\\Common Files\\Updater\\taskrunner.dll");
                    SuspiciousDllList.Items.Add("🧪 [HEURISTIC MATCH] C:\\Users\\Sanskar\\Downloads\\gputool_patch.dll");
                });
            });
        }



    }
}
