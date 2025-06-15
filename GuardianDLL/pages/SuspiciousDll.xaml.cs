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

namespace GuardianDLL.pages
{
    public partial class SuspiciousDllView : UserControl
    {
        private HashSet<string> knownHijackableDlls = new HashSet<string>
        {
            "version.dll", "winmm.dll", "dwmapi.dll", "dbghelp.dll", "msvcrt.dll", "userenv.dll", "profapi.dll"
        };

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

            // Start actual scan in background (progress is now linked to work)
            await Task.Run(() => ScanSuspiciousDllsWithProgress());

            // Complete the scan
            CompleteScan();
        }

        // No need for timer-based progress anymore, so disable timer
        private void ProgressTimer_Tick(object sender, EventArgs e) { }

        private void CompleteScan()
        {
            _isScanning = false;
            _progressTimer.Stop();

            // Update UI for completed state
            ScanButton.Content = "Start DLL Scan";
            ScanButton.IsEnabled = true;
            ProgressSection.Visibility = Visibility.Collapsed;
            ResultsSection.Visibility = Visibility.Visible;
        }

        private void ScanSuspiciousDllsWithProgress()
        {
            var results = new List<ScanResult>();
            var allDlls = new List<string>(GetAllDllFiles(@"C:\Program Files\Microsoft SDKs"));
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
                        hasCert = IsSigned(dllPath); // Optionally keep this to distinguish unsigned from invalid
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Skip this DLL and continue
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

                    if (!isTrusted || isHeuristic || isHijackProne)
                    {
                        var fileInfo = new FileInfo(dllPath);
                        string size = $"{fileInfo.Length / 1024 / 1024.0:F1} MB";

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
                    // Skip this DLL and continue
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

            Dispatcher.Invoke(() =>
            {
                ResultsCount.Text = $"{results.Count} suspicious DLLs found";
            });
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
                files.AddRange(Directory.EnumerateFiles(root, "*.dll", SearchOption.TopDirectoryOnly));

                foreach (var dir in Directory.EnumerateDirectories(root))
                {
                    try
                    {
                        files.AddRange(GetAllDllFiles(dir));
                    }
                    catch { /* skip inaccessible subdirectory */ }
                }
            }
            catch { /* skip inaccessible root */ }
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

        // The following methods are unchanged and keep the UI result item/colours as before
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