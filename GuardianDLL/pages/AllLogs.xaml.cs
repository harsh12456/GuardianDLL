using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

namespace GuardianDLL
{
    public partial class AllLogsView : UserControl
    {
        public AllLogsView()
        {
            InitializeComponent();
            LoadDummyLogs();
        }

        private void LoadDummyLogs()
        {
            var dummyData = new[]
            {
                new { Timestamp = "2024-01-15 10:30:15", File = @"C:\Windows\System32\kernel32.dll", Action = "File modified", Status = "normal" },
                new { Timestamp = "2024-01-15 10:31:22", File = @"C:\Windows\System32\suspicious.dll", Action = "Unauthorized access attempt", Status = "suspicious" },
                new { Timestamp = "2024-01-15 10:32:45", File = @"C:\Program Files\App\malware.dll", Action = "File created without permission", Status = "suspicious" },
                new { Timestamp = "2024-01-15 10:33:12", File = @"C:\Windows\System32\user32.dll", Action = "File accessed", Status = "normal" },
                new { Timestamp = "2024-01-15 10:34:33", File = @"C:\Temp\unknown.dll", Action = "Suspicious file execution", Status = "suspicious" }
            };

            foreach (var log in dummyData)
            {
                var border = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(31, 31, 31)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(10),
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var stack = new StackPanel();

                var topRow = new StackPanel { Orientation = Orientation.Horizontal };
                topRow.Children.Add(new TextBlock
                {
                    Text = log.Timestamp,
                    Foreground = Brushes.LightGray,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 12
                });

                var badge = new TextBlock
                {
                    Text = log.Status,
                    Foreground = log.Status == "suspicious" ? Brushes.White : Brushes.Black,
                    Background = log.Status == "suspicious" ? Brushes.OrangeRed : Brushes.LightGray,
                    FontSize = 10,
                    Margin = new Thickness(10, 0, 0, 0),
                    Padding = new Thickness(4, 1, 4, 1),
                    FontWeight = FontWeights.Bold
                };
                topRow.Children.Add(badge);

                stack.Children.Add(topRow);
                stack.Children.Add(new TextBlock { Text = log.File, FontWeight = FontWeights.Bold, Foreground = Brushes.White, FontSize = 14 });
                stack.Children.Add(new TextBlock { Text = log.Action, Foreground = Brushes.Gray, FontSize = 12 });

                border.Child = stack;
                DummyLogsPanel.Children.Add(border);
            }
        }
    }
}
