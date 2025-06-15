using GuardianDLL.pages; // Ensure your folder structure matches this
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

            // Placeholder for Home page
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
            MainContent.Content = new SuspiciousDllView(); // ? Load the actual view
        }

        private void SuspiciousActivitiesButton_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(ThreatActivitiesButton);

            // Placeholder for future implementation
            MainContent.Content = new TextBlock
            {
                Text = "Suspicious Activities View (coming soon)",
                Foreground = System.Windows.Media.Brushes.OrangeRed,
                FontSize = 20,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowState = WindowState.Maximized;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

    }
}
