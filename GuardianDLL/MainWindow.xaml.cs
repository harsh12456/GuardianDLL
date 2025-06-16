using GuardianDLL.pages; // Ensure your folder structure matches this
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GuardianDLL
{
    public partial class MainWindow : Window
    {
        private AllLogsView _allLogsView;
        private SuspiciousDllView _suspiciousDllView;
        private HomePage homePage;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize views
            _allLogsView = new AllLogsView();
            _suspiciousDllView = new SuspiciousDllView();

            // Initialize HomePage
            homePage = new HomePage();
            homePage.NavigateToPage += HomePage_NavigateToPage;

            MainContent.Content = homePage; // Load HomePage by default
            SetActiveButton(DashboardButton); // Set Dashboard as active by default
        }

        // Fix: Add string parameter to match delegate signature
        private void HomePage_NavigateToPage(string pageName)
        {
            // Example navigation logic based on pageName
            switch (pageName)
            {
                case "all-logs":
                    SetActiveButton(AllLogsButton);
                    MainContent.Content = _allLogsView;
                    break;
                case "suspicious-dll":
                    SetActiveButton(SuspiciousDllsButton);
                    MainContent.Content = _suspiciousDllView;
                    break;
              
                    MainContent.Content = new TextBlock
                    {
                        Text = "Suspicious Activities View (coming soon)",
                        Foreground = System.Windows.Media.Brushes.OrangeRed,
                        FontSize = 20,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center
                    };
                    break;
                case "home":
                default:
                    SetActiveButton(DashboardButton);
                    MainContent.Content = homePage;
                    break;
            }
        }

        private void SetActiveButton(System.Windows.Controls.Button activeButton)
        {
            // Reset all buttons to normal style
            DashboardButton.Style = (Style)Resources["SidebarButtonStyle"];
            AllLogsButton.Style = (Style)Resources["SidebarButtonStyle"];
            SuspiciousDllsButton.Style = (Style)Resources["SidebarButtonStyle"];

            // Fix: Ensure ThreatActivitiesButton is properly defined and styled
            if (FindName("ThreatActivitiesButton") is System.Windows.Controls.Button threatActivitiesButton)
            {
                threatActivitiesButton.Style = (Style)Resources["SidebarButtonStyle"];
            }

            // Set the clicked button to active style
            activeButton.Style = (Style)Resources["ActiveSidebarButtonStyle"];
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(DashboardButton);

            // Show the actual HomePage under Dashboard navigation
            MainContent.Content = homePage;
        }

        private void AllLogsButton_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(AllLogsButton);
            MainContent.Content = _allLogsView;
        }

        private void SuspiciousDllsButton_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(SuspiciousDllsButton);
            MainContent.Content = _suspiciousDllView; // ? Load the actual view
        }

        private void SuspiciousActivitiesButton_Click(object sender, RoutedEventArgs e)
        {
            // Fix: Use FindName to locate the ThreatActivitiesButton dynamically
            if (FindName("ThreatActivitiesButton") is System.Windows.Controls.Button threatActivitiesButton)
            {
                SetActiveButton(threatActivitiesButton);
            }

            // Placeholder for future implementation
            MainContent.Content = new TextBlock
            {
                Text = "Suspicious Activities View (coming soon)",
                Foreground = System.Windows.Media.Brushes.OrangeRed,
                FontSize = 20,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
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
