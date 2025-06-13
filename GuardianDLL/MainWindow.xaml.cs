// GuardianDLL: Basic C# WPF Application to Monitor .dll File Events

using System;
using System.IO;
using System.Windows;
using System.Collections.ObjectModel;

namespace GuardianDLL
{
    public partial class MainWindow : Window
    {
        private FileSystemWatcher _watcher;
        public ObservableCollection<string> Logs { get; set; } = new ObservableCollection<string>();

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

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            Dispatcher.Invoke(() =>
                Logs.Add($"[{e.ChangeType}] {e.FullPath} at {DateTime.Now}")
            );
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            Dispatcher.Invoke(() =>
                Logs.Add($"[Renamed] {e.OldFullPath} -> {e.FullPath} at {DateTime.Now}")
            );
        }

        private void OnDelete(object sender, FileSystemEventArgs e)
        {
            Dispatcher.Invoke(() =>
                Logs.Add($"[Deleted] {e.FullPath} at {DateTime.Now}")
            );
        }
    }
}
