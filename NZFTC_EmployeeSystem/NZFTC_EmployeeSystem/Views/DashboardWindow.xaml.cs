using System.Windows;
using System.Windows.Controls;
using NZFTC_EmployeeSystem.Models;

namespace NZFTC_EmployeeSystem.Views
{
    /// <summary>
    /// DashboardWindow - Main application window after login
    /// 
    /// OOP CONCEPTS DEMONSTRATED:
    /// 1. ENCAPSULATION - Private field _currentUser protects user data
    /// 2. POLYMORPHISM - Different behavior/UI for Admin vs Employee role
    /// 3. ABSTRACTION - Navigation methods hide complex page loading logic
    /// </summary>
    public partial class DashboardWindow : Window
    {
        // ENCAPSULATION: Private field to store logged-in user
        // Only this class can directly access this field
        private readonly User _currentUser;

        /// <summary>
        /// Constructor - Called when creating new DashboardWindow
        /// Accepts the logged-in user and initializes the window
        /// </summary>
        /// <param name="currentUser">The user who just logged in</param>
        public DashboardWindow(User currentUser)
        {
            // Initialize all XAML components (buttons, text, etc.)
            InitializeComponent();

            // ENCAPSULATION: Store user in private field
            _currentUser = currentUser;

            // Set the welcome message with user's name
            // Uses null-coalescing operator (??) for safety
            // If Employee or FullName is null, use Username instead
            WelcomeText.Text = $"Welcome, {_currentUser.Employee?.FullName ?? _currentUser.Username}";

            // POLYMORPHISM: Show admin menu only for admin users
            // This demonstrates different behavior based on user role
            if (_currentUser.Role == "Admin")
            {
                // Make admin menu visible
                AdminMenuPanel.Visibility = Visibility.Visible;
            }
            // If not admin, menu stays collapsed (hidden)
        }

        // ========================================
        // NAVIGATION METHODS
        // These methods handle button clicks and navigate to different pages
        // ========================================

        /// <summary>
        /// Dashboard button clicked - Navigate to main dashboard
        /// </summary>
        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            // Clear content frame to show blank dashboard
            // TODO: Create a DashboardPage to show statistics
            ContentFrame.Content = null;
        }

        /// <summary>
        /// My Profile button clicked - Show user's profile
        /// </summary>
        private void MyProfile_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Create ProfilePage.xaml and ProfilePage.xaml.cs
            // For now, show a message box
            MessageBox.Show("Profile page - To be implemented",
                          "Coming Soon",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);
        }

        /// <summary>
        /// Leave Management button clicked - Navigate to leave page
        /// </summary>
        private void LeaveManagement_Click(object sender, RoutedEventArgs e)
        {
            // Navigate to LeavePage and pass current user
            // LeavePage will show different options based on user role
            ContentFrame.Navigate(new LeavePage(_currentUser));
        }

        /// <summary>
        /// Payroll button clicked - Navigate to payroll page
        /// </summary>
        private void Payroll_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Create PayrollPage.xaml and PayrollPage.xaml.cs
            MessageBox.Show("Payroll page - To be implemented",
                          "Coming Soon",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);
        }

        /// <summary>
        /// Holidays button clicked - Navigate to holidays page
        /// </summary>
        private void Holidays_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Create HolidaysPage.xaml and HolidaysPage.xaml.cs
            MessageBox.Show("Holidays page - To be implemented",
                          "Coming Soon",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);
        }

        /// <summary>
        /// Grievances button clicked - Navigate to grievances page
        /// </summary>
        private void Grievances_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Create GrievancesPage.xaml and GrievancesPage.xaml.cs
            MessageBox.Show("Grievances page - To be implemented",
                          "Coming Soon",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);
        }

        /// <summary>
        /// Manage Employees button clicked - Navigate to employee management
        /// ADMIN ONLY FEATURE - demonstrates POLYMORPHISM
        /// </summary>
        private void ManageEmployees_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Create EmployeeManagementPage.xaml and EmployeeManagementPage.xaml.cs
            MessageBox.Show("Employee Management page - To be implemented",
                          "Coming Soon",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);
        }

        /// <summary>
        /// Logout button clicked - Return to login screen
        /// </summary>
        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            // Create new login window
            var loginWindow = new LoginWindow();

            // Show login window
            loginWindow.Show();

            // Close current dashboard window
            this.Close();
        }
    }
}