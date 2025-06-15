using GuardianDLL.pages; // Make sure this matches your folder structure
using System.Windows;
using System.Windows.Controls;

namespace GuardianDLL
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainContent.Content = new AllLogsView(); // Load AllLogs page by default
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            // You can create a HomePage.xaml UserControl and replace this when ready
            MainContent.Content = new TextBlock
            {
                Text = "Home Page (coming soon)",
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 20,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
        }

        private void AllLogsButton_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new AllLogsView();
        }

        private void SuspiciousDllsButton_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new TextBlock
            {
                Text = "Suspicious DLLs View (coming soon)",
                Foreground = System.Windows.Media.Brushes.Orange,
                FontSize = 20,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
        }

        private void SuspiciousActivitiesButton_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new TextBlock
            {
                Text = "Suspicious Activities View (coming soon)",
                Foreground = System.Windows.Media.Brushes.OrangeRed,
                FontSize = 20,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
        }
    }
}
