using System.Collections.ObjectModel;
using System.Windows;

namespace GuardianDLL
{
    public partial class SuspiciousWindow : Window
    {
        public SuspiciousWindow(ObservableCollection<string> suspiciousLogs)
        {
            InitializeComponent();
            SuspiciousList.ItemsSource = suspiciousLogs;
        }
    }
}
