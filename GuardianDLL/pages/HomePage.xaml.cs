using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GuardianDLL.pages
{
    public partial class HomePage : System.Windows.Controls.UserControl
    {
        // Delegate to communicate with MainWindow
        public delegate void NavigateToPageDelegate(string pageName);
        public event NavigateToPageDelegate NavigateToPage;

        public HomePage()
        {
            InitializeComponent();
        }

        // Card click handlers - these navigate to respective modules
        private void AllLogsCard_Click(object sender, MouseButtonEventArgs e)
        {
            NavigateToPage?.Invoke("all-logs");
        }

        private void SuspiciousDllCard_Click(object sender, MouseButtonEventArgs e)
        {
            NavigateToPage?.Invoke("suspicious-dll");
        }

        private void SuspiciousActivitiesCard_Click(object sender, MouseButtonEventArgs e)
        {
            // Change navigation to "all-logs" instead of "suspicious-activities"
            NavigateToPage?.Invoke("all-logs");
        }

        private void SystemOverviewCard_Click(object sender, MouseButtonEventArgs e)
        {
            NavigateToPage?.Invoke("home");
        }

        // Quick action button handlers
        private void QuickScanButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage?.Invoke("suspicious-dll");
        }

        private void ViewRecentLogsButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage?.Invoke("all-logs");
        }

        private void CheckThreatsButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage?.Invoke("suspicious-activities");
        }
    }
}