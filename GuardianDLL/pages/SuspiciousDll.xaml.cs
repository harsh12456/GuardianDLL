using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace GuardianDLL.pages
{
    public partial class SuspiciousDllView : UserControl
    {
        private bool _isScanning = false;
        private DispatcherTimer _progressTimer;
        private int _currentProgress = 0;

        public SuspiciousDllView()
        {
            InitializeComponent();
            SetupProgressTimer();
        }

        private void SetupProgressTimer()
        {
            _progressTimer = new DispatcherTimer();
            _progressTimer.Interval = TimeSpan.FromMilliseconds(300);
            _progressTimer.Tick += ProgressTimer_Tick;
        }

        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isScanning) return;

            await StartScan();
        }

        private async Task StartScan()
        {
            _isScanning = true;
            _currentProgress = 0;

            // Update UI for scanning state
            ScanButton.Content = "Scanning...";
            ScanButton.IsEnabled = false;
            ProgressSection.Visibility = Visibility.Visible;
            ResultsSection.Visibility = Visibility.Collapsed;

            // Reset progress
            ScanProgress.Value = 0;
            ProgressText.Text = "0%";

            // Start progress animation
            _progressTimer.Start();

            // Simulate scanning time (3 seconds)
            await Task.Delay(3000);

            // Complete the scan
            CompleteScan();
        }

        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            _currentProgress += 10;
            ScanProgress.Value = _currentProgress;
            ProgressText.Text = $"{_currentProgress}%";

            if (_currentProgress >= 100)
            {
                _progressTimer.Stop();
            }
        }

        private void CompleteScan()
        {
            _isScanning = false;
            _progressTimer.Stop();

            // Update UI for completed state
            ScanButton.Content = "Start DLL Scan";
            ScanButton.IsEnabled = true;
            ProgressSection.Visibility = Visibility.Collapsed;
            ResultsSection.Visibility = Visibility.Visible;

            // Load dummy results
            LoadScanResults();
        }

        private void LoadScanResults()
        {
            var results = GetDummyResults();
            ResultsCount.Text = $"{results.Count} files scanned";

            // Clear previous results
            ResultsList.Items.Clear();

            // Add new results
            foreach (var result in results)
            {
                var item = CreateResultItem(result);
                ResultsList.Items.Add(item);
            }
        }

        private UIElement CreateResultItem(ScanResult result)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x25, 0x25, 0x26)),
                CornerRadius = new CornerRadius(6),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x40, 0x40, 0x40)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var mainStack = new StackPanel();

            // File info row
            var fileInfoStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 8)
            };

            // Status icon
            var statusIcon = new TextBlock
            {
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };

            // File path
            var filePath = new TextBlock
            {
                Text = result.File,
                FontWeight = FontWeights.Medium,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 12, 0)
            };

            // Status badge
            var statusBadge = new Border
            {
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(8, 4, 8, 4)
            };

            var statusText = new TextBlock
            {
                FontSize = 12,
                FontWeight = FontWeights.SemiBold
            };

            // Set status-specific styling
            SetStatusStyling(result.Status, statusIcon, statusBadge, statusText);

            statusBadge.Child = statusText;

            fileInfoStack.Children.Add(statusIcon);
            fileInfoStack.Children.Add(filePath);
            fileInfoStack.Children.Add(statusBadge);

            // Details row
            var detailsStack = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            var sizeText = new TextBlock
            {
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(0xB0, 0xB0, 0xB0)),
                Margin = new Thickness(0, 0, 16, 0)
            };
            sizeText.Inlines.Add(new Run("Size: "));
            sizeText.Inlines.Add(new Run(result.Size));

            var threatText = new TextBlock
            {
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(0xB0, 0xB0, 0xB0))
            };
            threatText.Inlines.Add(new Run("Threat: "));
            threatText.Inlines.Add(new Run(result.Threat));

            detailsStack.Children.Add(sizeText);
            detailsStack.Children.Add(threatText);

            mainStack.Children.Add(fileInfoStack);
            mainStack.Children.Add(detailsStack);

            border.Child = mainStack;
            return border;
        }

        private void SetStatusStyling(string status, TextBlock icon, Border badge, TextBlock text)
        {
            switch (status.ToLower())
            {
                case "clean":
                    icon.Text = "\uE930"; // CheckCircle icon
                    icon.Foreground = new SolidColorBrush(Color.FromRgb(0x22, 0xc5, 0x5e));
                    badge.Background = new SolidColorBrush(Color.FromRgb(0xdc, 0xfc, 0xe7));
                    text.Text = "Clean";
                    text.Foreground = new SolidColorBrush(Color.FromRgb(0x16, 0x6c, 0x34));
                    break;

                case "suspicious":
                    icon.Text = "\uE7BA"; // Warning icon
                    icon.Foreground = new SolidColorBrush(Color.FromRgb(0xf5, 0x9e, 0x0b));
                    badge.Background = new SolidColorBrush(Color.FromRgb(0xfe, 0xf3, 0xc7));
                    text.Text = "Suspicious";
                    text.Foreground = new SolidColorBrush(Color.FromRgb(0x92, 0x40, 0x0e));
                    break;

                case "malicious":
                    icon.Text = "\uE7BA"; // Warning icon
                    icon.Foreground = new SolidColorBrush(Color.FromRgb(0xef, 0x44, 0x44));
                    badge.Background = new SolidColorBrush(Color.FromRgb(0xfe, 0xca, 0xca));
                    text.Text = "Malicious";
                    text.Foreground = new SolidColorBrush(Color.FromRgb(0x99, 0x1b, 0x1b));
                    break;
            }
        }

        private List<ScanResult> GetDummyResults()
        {
            return new List<ScanResult>
            {
                new ScanResult
                {
                    File = @"C:\Windows\System32\kernel32.dll",
                    Status = "clean",
                    Threat = "No threats detected",
                    Size = "1.2 MB"
                },
                new ScanResult
                {
                    File = @"C:\Program Files\App\suspicious.dll",
                    Status = "suspicious",
                    Threat = "Potential malware signature",
                    Size = "856 KB"
                },
                new ScanResult
                {
                    File = @"C:\Temp\malware.dll",
                    Status = "malicious",
                    Threat = "Trojan.Generic.KD.12345",
                    Size = "2.1 MB"
                },
                new ScanResult
                {
                    File = @"C:\Windows\System32\user32.dll",
                    Status = "clean",
                    Threat = "No threats detected",
                    Size = "1.8 MB"
                },
                new ScanResult
                {
                    File = @"C:\Program Files\Browser\addon.dll",
                    Status = "suspicious",
                    Threat = "Adware.Generic.X12",
                    Size = "445 KB"
                },
                new ScanResult
                {
                    File = @"C:\Windows\System32\ntdll.dll",
                    Status = "clean",
                    Threat = "No threats detected",
                    Size = "2.3 MB"
                }
            };
        }
    }

    public class ScanResult
    {
        public string File { get; set; }
        public string Status { get; set; }
        public string Threat { get; set; }
        public string Size { get; set; }
    }
}