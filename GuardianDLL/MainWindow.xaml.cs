
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
            SetActiveButton(AllLogsButton); // Set AllLogs as active by default
        }

        private void SetActiveButton(Button activeButton)
        {
            // Reset all buttons to normal style
            DashboardButton.Style = (Style)Resources["SidebarButtonStyle"];
            AllLogsButton.Style = (Style)Resources["SidebarButtonStyle"];
            SuspiciousDllsButton.Style = (Style)Resources["SidebarButtonStyle"];
            ThreatActivitiesButton.Style = (Style)Resources["SidebarButtonStyle"];

            // Set the clicked button to active style
            activeButton.Style = (Style)Resources["ActiveSidebarButtonStyle"];
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(DashboardButton);

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
            SetActiveButton(AllLogsButton);
            MainContent.Content = new AllLogsView();
        }

        private void SuspiciousDllsButton_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(SuspiciousDllsButton);

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
            SetActiveButton(ThreatActivitiesButton);

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