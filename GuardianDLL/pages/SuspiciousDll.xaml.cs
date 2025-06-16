using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;

namespace GuardianDLL.pages
{
    public partial class SuspiciousDllView : System.Windows.Controls.UserControl
    {
        private readonly SafeDllBackupManager _backupManager = new SafeDllBackupManager();

        private HashSet<string> knownHijackableDlls = new HashSet<string>
        {
            "version.dll", "winmm.dll", "dwmapi.dll", "dbghelp.dll", "msvcrt.dll", "userenv.dll", "profapi.dll"
        };

        private bool _isScanning = false;
        private DispatcherTimer _progressTimer;
        private int _currentProgress = 0;
        private string _selectedPath = "";
        private List<ScanResult> _scanResults = new List<ScanResult>();

        public SuspiciousDllView()
        {
            InitializeComponent();
            SetupProgressTimer();
            InitializeUI();
        }

        private void InitializeUI()
        {
            // Initially hide the selected path indicator and show warning
            SelectedPathIndicator.Visibility = Visibility.Collapsed;
            NoPathWarning.Visibility = Visibility.Visible;
            ScanButton.IsEnabled = false;
        }

        private void SetupProgressTimer()
        {
            _progressTimer = new DispatcherTimer();
            _progressTimer.Interval = TimeSpan.FromMilliseconds(300);
            _progressTimer.Tick += ProgressTimer_Tick;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select any folder or file to scan (including entire drives like C:\\)",
                Filter = "All Files (*.*)|*.*|DLL Files (*.dll)|*.dll|Executable Files (*.exe)|*.exe",
                CheckFileExists = false,
                CheckPathExists = false,
                ValidateNames = false,
                Multiselect = false,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                FileName = "Folder Selection"
            };

            if (dialog.ShowDialog() == true)
            {
                string selectedPath = dialog.FileName;

                // Handle different selection scenarios
                if (selectedPath == "Folder Selection" || string.IsNullOrEmpty(Path.GetFileName(selectedPath)))
                {
                    // User navigated to a folder but didn't select a file
                    _selectedPath = Path.GetDirectoryName(selectedPath) ?? selectedPath;
                }
                else if (File.Exists(selectedPath))
                {
                    // User selected an actual file
                    _selectedPath = selectedPath;
                }
                else if (Directory.Exists(selectedPath))
                {
                    // User selected a directory
                    _selectedPath = selectedPath;
                }
                else
                {
                    // Try to get the directory from the path
                    string directoryPath = Path.GetDirectoryName(selectedPath);
                    if (Directory.Exists(directoryPath))
                    {
                        _selectedPath = directoryPath;
                    }
                    else
                    {
                        // Handle drive selection (like C:\)
                        if (selectedPath.Length >= 2 && selectedPath[1] == ':')
                        {
                            string drivePath = selectedPath.Substring(0, 3); // Get "C:\" format
                            if (Directory.Exists(drivePath))
                            {
                                _selectedPath = drivePath;
                            }
                            else
                            {
                                _selectedPath = selectedPath;
                            }
                        }
                        else
                        {
                            _selectedPath = selectedPath;
                        }
                    }
                }

                UpdateSelectedPath(_selectedPath);
            }
        }

        private void UpdateSelectedPath(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                SelectedPath.Text = path;

                // Show different text based on whether it's a file or directory
                if (File.Exists(path))
                {
                    SelectedPathText.Text = $"Selected File: {Path.GetFileName(path)}";
                }
                else if (Directory.Exists(path))
                {
                    SelectedPathText.Text = $"Selected Folder: {path}";
                }
                else
                {
                    SelectedPathText.Text = $"Selected: {path}";
                }

                SelectedPathIndicator.Visibility = Visibility.Visible;
                NoPathWarning.Visibility = Visibility.Collapsed;
                ScanButton.IsEnabled = true;
            }
        }

        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isScanning || string.IsNullOrEmpty(_selectedPath)) return;

            await StartScan();
        }

        private async Task StartScan()
        {
            _isScanning = true;
            _currentProgress = 0;
            _scanResults.Clear();

            // Update UI for scanning state
            ScanButton.Content = "Scanning...";
            ScanButton.IsEnabled = false;
            ProgressSection.Visibility = Visibility.Visible;
            ResultsSection.Visibility = Visibility.Collapsed;

            // Show appropriate scanning text
            if (File.Exists(_selectedPath))
            {
                ScanningText.Text = $"Scanning file: {Path.GetFileName(_selectedPath)}";
            }
            else if (Directory.Exists(_selectedPath))
            {
                ScanningText.Text = $"Scanning folder: {_selectedPath}";
            }
            else
            {
                ScanningText.Text = $"Scanning: {_selectedPath}";
            }

            // Reset progress
            ScanProgress.Value = 0;
            ProgressText.Text = "0%";

            // Clear previous results
            ResultsList.Items.Clear();

            // Start actual scan in background
            await Task.Run(() => ScanSuspiciousDllsWithProgress());

            // Complete the scan
            CompleteScan();
        }

        private void ProgressTimer_Tick(object sender, EventArgs e) { }

        private void CompleteScan()
        {
            _isScanning = false;
            _progressTimer.Stop();

            // Update UI for completed state
            ScanButton.Content = "Start DLL Threat Scan";
            ScanButton.IsEnabled = true;
            ProgressSection.Visibility = Visibility.Collapsed;
            ResultsSection.Visibility = Visibility.Visible;

            // Show appropriate results section
            if (_scanResults.Count == 0)
            {
                NoThreatsSection.Visibility = Visibility.Visible;
                ResultsScrollViewer.Visibility = Visibility.Collapsed;
                ActionRequiredBadge.Visibility = Visibility.Collapsed;
            }
            else
            {
                NoThreatsSection.Visibility = Visibility.Collapsed;
                ResultsScrollViewer.Visibility = Visibility.Visible;
                ActionRequiredBadge.Visibility = Visibility.Visible;
            }
        }

        private void ScanSuspiciousDllsWithProgress()
        {
            var results = new List<ScanResult>();
            var allDlls = new List<string>();

            try
            {
                // Check if selected path is a file or directory
                if (File.Exists(_selectedPath))
                {
                    // If it's a DLL file, add it directly
                    if (Path.GetExtension(_selectedPath).ToLower() == ".dll")
                    {
                        allDlls.Add(_selectedPath);
                    }
                    // If it's any other file, we can still check if it's suspicious based on path/name
                    else
                    {
                        // Only scan executable files for potential threats
                        string ext = Path.GetExtension(_selectedPath).ToLower();
                        if (ext == ".exe" || ext == ".dll" || ext == ".sys" || ext == ".ocx")
                        {
                            allDlls.Add(_selectedPath);
                        }
                    }
                }
                else if (Directory.Exists(_selectedPath))
                {
                    allDlls.AddRange(GetAllDllFiles(_selectedPath));
                }

                int total = allDlls.Count;
                int processed = 0;

                Dispatcher.Invoke(() =>
                {
                    ScanProgress.Maximum = total > 0 ? total : 1;
                    ScanProgress.Value = 0;
                    ProgressText.Text = "0%";
                });

                foreach (var dllPath in allDlls)
                {
                    try
                    {
                        string fileName = Path.GetFileName(dllPath).ToLower();
                        bool isTrusted = false;
                        bool hasCert = false;
                        bool isHeuristic = false;
                        bool isHijackProne = false;

                        try
                        {
                            isTrusted = AuthenticodeTools.IsAuthenticodeSignedAndTrusted(dllPath);
                            hasCert = IsSigned(dllPath);
                        }
                        catch (UnauthorizedAccessException)
                        {
                            processed++;
                            UpdateProgress(processed, total);
                            continue;
                        }
                        catch
                        {
                            isTrusted = false;
                            hasCert = false;
                        }

                        isHeuristic = IsSuspiciousHeuristic(dllPath);
                        isHijackProne = IsHijackProne(fileName);

                        string status = isHeuristic ? "malicious" : "suspicious";
                        string threat = "";
                        if (!isTrusted)
                        {
                            if (!hasCert)
                                threat += "[UNSIGNED] ";
                            else
                                threat += "[INVALID CERT] ";
                        }
                        if (isHeuristic)
                            threat += "[HEURISTIC MATCH] ";
                        if (isHijackProne)
                            threat += "[HIJACK-PRONE DLL] ";

                        if (isTrusted && !isHeuristic && !isHijackProne)
                        {
                            try
                            {
                                _backupManager.BackupDll(dllPath);
                            }
                            catch (Exception ex)
                            {
                                // Optionally log or show error
                                // System.Windows.MessageBox.Show($"Failed to backup DLL: {ex.Message}");
                            }
                        }

                        if (!isTrusted || isHeuristic || isHijackProne)
                        {
                            var fileInfo = new FileInfo(dllPath);
                            string size = fileInfo.Length < 1024 ? $"{fileInfo.Length} B" :
                                         fileInfo.Length < 1024 * 1024 ? $"{fileInfo.Length / 1024.0:F1} KB" :
                                         $"{fileInfo.Length / 1024 / 1024.0:F1} MB";

                            var result = new ScanResult
                            {
                                File = dllPath,
                                Status = status,
                                Threat = threat.Trim(),
                                Size = size
                            };

                            results.Add(result);

                            // Add result to UI as soon as found
                            Dispatcher.Invoke(() =>
                            {
                                var item = CreateResultItem(result);
                                ResultsList.Items.Add(item);
                            });
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Skip this file and continue
                    }
                    catch
                    {
                        // Skip files that cause other issues
                    }
                    processed++;
                    if (processed % 10 == 0 || processed == total)
                    {
                        UpdateProgress(processed, total);
                    }
                }

                _scanResults = results;
                Dispatcher.Invoke(() =>
                {
                    ResultsCount.Text = $"{results.Count} threats found";
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show($"Error during scan: {ex.Message}", "Scan Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
            }
        }

        private void UpdateProgress(int processed, int total)
        {
            Dispatcher.Invoke(() =>
            {
                ScanProgress.Value = processed;
                int percent = total > 0 ? (int)((processed * 100.0) / total) : 100;
                ProgressText.Text = $"{percent}%";
            });
        }

        private IEnumerable<string> GetAllDllFiles(string root)
        {
            var files = new List<string>();
            try
            {
                // Scan for multiple file types, not just DLLs
                string[] patterns = { "*.dll", "*.exe", "*.sys", "*.ocx" };

                foreach (string pattern in patterns)
                {
                    try
                    {
                        files.AddRange(Directory.EnumerateFiles(root, pattern, SearchOption.TopDirectoryOnly));
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Skip files we can't access
                        continue;
                    }
                    catch (DirectoryNotFoundException)
                    {
                        // Skip if directory doesn't exist
                        continue;
                    }
                    catch
                    {
                        // Skip other errors
                        continue;
                    }
                }

                // Recursively scan subdirectories
                try
                {
                    foreach (var dir in Directory.EnumerateDirectories(root))
                    {
                        try
                        {
                            files.AddRange(GetAllDllFiles(dir));
                        }
                        catch (UnauthorizedAccessException)
                        {
                            // Skip inaccessible subdirectories
                            continue;
                        }
                        catch
                        {
                            // Skip other errors in subdirectories
                            continue;
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // Can't enumerate subdirectories
                }
                catch
                {
                    // Other errors enumerating subdirectories
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Can't access the root directory
            }
            catch
            {
                // Other errors accessing root
            }
            return files;
        }

        private bool IsSigned(string filePath)
        {
            try
            {
                X509Certificate cert = X509Certificate.CreateFromSignedFile(filePath);
                return cert != null;
            }
            catch (CryptographicException)
            {
                throw;
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch
            {
                return false;
            }
        }

        private bool IsCertValid(string filePath)
        {
            try
            {
                var cert2 = new X509Certificate2(X509Certificate.CreateFromSignedFile(filePath));
                return cert2.NotBefore <= DateTime.Now && cert2.NotAfter >= DateTime.Now;
            }
            catch (CryptographicException)
            {
                throw;
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch
            {
                return false;
            }
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
                   fileName.StartsWith("sys") && fileName.Contains("helper");
        }

        private bool IsHijackProne(string dllName)
        {
            return knownHijackableDlls.Contains(dllName.ToLower());
        }

        private void QuarantineFile_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            var filePath = button?.Tag as string;

            if (!string.IsNullOrEmpty(filePath))
            {
                try
                {
                    // Create quarantine directory if it doesn't exist
                    string quarantineDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GuardianDLL", "Quarantine");
                    Directory.CreateDirectory(quarantineDir);

                    // Move file to quarantine with timestamp to avoid conflicts
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string quarantinedPath = Path.Combine(quarantineDir, $"{Path.GetFileName(filePath)}_{timestamp}.quarantined");
                    File.Move(filePath, quarantinedPath);

                    // Update UI to show quarantined status
                    UpdateFileStatus(filePath, "quarantined");

                    System.Windows.MessageBox.Show($"File quarantined successfully!\nLocation: {quarantinedPath}", "Quarantine Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to quarantine file: {ex.Message}", "Quarantine Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RestoreFile_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            var originalPath = button?.Tag as string;

            if (!string.IsNullOrEmpty(originalPath))
            {
                try
                {
                    if (_backupManager.HasBackup(originalPath))
                    {
                        if (_backupManager.RestoreDll(originalPath))
                        {
                            UpdateFileStatus(originalPath, "restored");
                            System.Windows.MessageBox.Show("DLL restored from backup.", "Restore Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            System.Windows.MessageBox.Show("Backup file not found!", "Restore Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("No backup exists for this DLL.", "Restore Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to restore DLL: {ex.Message}", "Restore Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void UpdateFileStatus(string filePath, string newStatus)
        {
            // Find and update the result item in the UI
            foreach (Border item in ResultsList.Items)
            {
                var mainStack = item.Child as StackPanel;
                var fileInfoStack = mainStack?.Children[0] as StackPanel;
                var pathTextBlock = fileInfoStack?.Children[1] as TextBlock;

                if (pathTextBlock?.Text == filePath)
                {
                    var statusBadge = fileInfoStack.Children[2] as Border;
                    var statusText = statusBadge?.Child as TextBlock;

                    if (statusText != null)
                    {
                        statusText.Text = newStatus.Substring(0, 1).ToUpper() + newStatus.Substring(1);

                        // Update colors based on status
                        if (newStatus == "quarantined")
                        {
                            statusBadge.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xfe, 0xd7, 0xaa));
                            statusText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x92, 0x40, 0x0e));
                        }
                        else if (newStatus == "restored")
                        {
                            statusBadge.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xbb, 0xf7, 0xd0));
                            statusText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x16, 0x65, 0x34));
                        }
                    }
                    break;
                }
            }
        }

        private UIElement CreateResultItem(ScanResult result)   
        {
            var border = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x25, 0x25, 0x26)),
                CornerRadius = new CornerRadius(6),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x40, 0x40, 0x40)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var mainStack = new StackPanel();

            // File info row
            var fileInfoStack = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 8)
            };

            // Status icon
            var statusIcon = new TextBlock
            {
                FontFamily = new System.Windows.Media.FontFamily("Segoe MDL2 Assets"),
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };

            // File path
            var filePath = new TextBlock
            {
                Text = result.File,
                FontWeight = FontWeights.Medium,
                Foreground = System.Windows.Media.Brushes.White,
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
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 12)
            };

            var sizeText = new TextBlock
            {
                FontSize = 12,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xB0, 0xB0, 0xB0)),
                Margin = new Thickness(0, 0, 16, 0)
            };
            sizeText.Inlines.Add(new Run("Size: "));
            sizeText.Inlines.Add(new Run(result.Size));

            var threatText = new TextBlock
            {
                FontSize = 12,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xB0, 0xB0, 0xB0))
            };
            threatText.Inlines.Add(new Run("Threat: "));
            threatText.Inlines.Add(new Run(result.Threat));

            detailsStack.Children.Add(sizeText);
            detailsStack.Children.Add(threatText);

            // Action buttons row
            var actionStack = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal
            };

            var quarantineButton = new System.Windows.Controls.Button
            {
                Content = "Quarantine",
                Tag = result.File,
                Style = (Style)FindResource("QuarantineButtonStyle"),
                Margin = new Thickness(0, 0, 8, 0)
            };
            quarantineButton.Click += QuarantineFile_Click;

            var restoreButton = new System.Windows.Controls.Button
            {
                Content = "Restore",
                Tag = result.File,
                Style = (Style)FindResource("RestoreButtonStyle"),
                IsEnabled = _backupManager.HasBackup(result.File)
            };
            restoreButton.Click += RestoreFile_Click;

            actionStack.Children.Add(quarantineButton);
            actionStack.Children.Add(restoreButton);

            mainStack.Children.Add(fileInfoStack);
            mainStack.Children.Add(detailsStack);
            mainStack.Children.Add(actionStack);

            border.Child = mainStack;
            return border;
        }

        private void SetStatusStyling(string status, TextBlock icon, Border badge, TextBlock text)
        {
            switch (status.ToLower())
            {
                case "clean":
                    icon.Text = "\uE930"; // CheckCircle icon
                    icon.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x22, 0xc5, 0x5e));
                    badge.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xdc, 0xfc, 0xe7));
                    text.Text = "Clean";
                    text.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x16, 0x6c, 0x34));
                    break;

                case "suspicious":
                    icon.Text = "\uE7BA"; // Warning icon
                    icon.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xf5, 0x9e, 0x0b));
                    badge.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xfe, 0xf3, 0xc7));
                    text.Text = "Suspicious";
                    text.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x92, 0x40, 0x0e));
                    break;

                case "malicious":
                    icon.Text = "\uE7BA"; // Warning icon
                    icon.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xef, 0x44, 0x44));
                    badge.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xfe, 0xca, 0xca));
                    text.Text = "Malicious";
                    text.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x99, 0x1b, 0x1b));
                    break;
            }
        }
    }

    public static class AuthenticodeTools
    {
        private static readonly Guid WINTRUST_ACTION_GENERIC_VERIFY_V2 = new Guid("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");

        [DllImport("wintrust.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint WinVerifyTrust(
            IntPtr hwnd,
            [MarshalAs(UnmanagedType.LPStruct)] Guid pgActionID,
            ref WINTRUST_DATA pWVTData
        );

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WINTRUST_FILE_INFO
        {
            public uint cbStruct;
            public string pcwszFilePath;
            public IntPtr hFile;
            public IntPtr pgKnownSubject;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WINTRUST_DATA
        {
            public uint cbStruct;
            public IntPtr pPolicyCallbackData;
            public IntPtr pSIPClientData;
            public uint dwUIChoice;
            public uint fdwRevocationChecks;
            public uint dwUnionChoice;
            public IntPtr pFile;
            public uint dwStateAction;
            public IntPtr hWVTStateData;
            public string pwszURLReference;
            public uint dwProvFlags;
            public uint dwUIContext;
            public IntPtr pSignatureSettings;
        }

        public static bool IsAuthenticodeSignedAndTrusted(string filePath)
        {
            WINTRUST_FILE_INFO fileInfo = new WINTRUST_FILE_INFO
            {
                cbStruct = (uint)Marshal.SizeOf(typeof(WINTRUST_FILE_INFO)),
                pcwszFilePath = filePath,
                hFile = IntPtr.Zero,
                pgKnownSubject = IntPtr.Zero
            };

            WINTRUST_DATA data = new WINTRUST_DATA
            {
                cbStruct = (uint)Marshal.SizeOf(typeof(WINTRUST_DATA)),
                pPolicyCallbackData = IntPtr.Zero,
                pSIPClientData = IntPtr.Zero,
                dwUIChoice = 2, // WTD_UI_NONE
                fdwRevocationChecks = 0, // WTD_REVOKE_NONE
                dwUnionChoice = 1, // WTD_CHOICE_FILE
                pFile = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WINTRUST_FILE_INFO))),
                dwStateAction = 0,
                hWVTStateData = IntPtr.Zero,
                pwszURLReference = null,
                dwProvFlags = 0x00000010, // WTD_SAFER_FLAG
                dwUIContext = 0,
                pSignatureSettings = IntPtr.Zero
            };

            Marshal.StructureToPtr(fileInfo, data.pFile, false);

            uint result = WinVerifyTrust(IntPtr.Zero, WINTRUST_ACTION_GENERIC_VERIFY_V2, ref data);

            Marshal.FreeHGlobal(data.pFile);

            // 0 == trusted, any other value is not trusted
            return result == 0;
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