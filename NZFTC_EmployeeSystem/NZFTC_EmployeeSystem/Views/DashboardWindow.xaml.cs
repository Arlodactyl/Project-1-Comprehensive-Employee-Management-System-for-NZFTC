using System.Windows;
using System.Windows.Controls;
using NZFTC_EmployeeSystem.Models;
using NZFTC_EmployeeSystem.Views;

namespace NZFTC_EmployeeSystem.Views
{
    /// <summary>
    /// DashboardWindow - This is the main window that shows after login
    /// It displays the navigation menu and content area
    /// </summary>
    public partial class DashboardWindow : Window
    {
        // Private field to store the currently logged-in user
        // 'readonly' means it can only be set in the constructor
        // The underscore (_) is a naming convention for private fields
        private readonly User _currentUser;

        /// <summary>
        /// Constructor - This runs when a new DashboardWindow is created
        /// It receives the logged-in user as a parameter
        /// </summary>
        /// <param name="currentUser">The user who just logged in</param>
        public DashboardWindow(User currentUser)
        {
            // Initialize all XAML components (buttons, text boxes, etc.)
            // This MUST be called first before accessing any controls
            InitializeComponent();

            // Store the current user so we can use it throughout the class
            _currentUser = currentUser;

            // Set the welcome message at the top of the window
            // The ?? operator means "if Employee.FullName is null, use Username instead"
            // The $ before the string allows us to insert variables using {}
            WelcomeText.Text = $"Welcome, {_currentUser.Employee?.FullName ?? _currentUser.Username}";

            // Check if the user is an Admin
            // If they are, show the Admin menu section
            if (_currentUser.Role == "Admin")
            {
                // Make the admin menu visible
                // By default it's collapsed (hidden)
                AdminMenuPanel.Visibility = Visibility.Visible;
            }
            // If not admin, the menu stays collapsed (default)
        }

        /// <summary>
        /// Dashboard button clicked - navigates to the dashboard summary page
        /// Instead of clearing the frame, this loads a summary page
        /// showing key metrics like employee count, pending leaves, etc.
        /// </summary>
        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            // Check if DashboardHomePage exists, if not show a message
            try
            {
                ContentFrame.Navigate(new DashboardHomePage(_currentUser));
            }
            catch
            {
                MessageBox.Show(
                    "Dashboard home page is under construction.",
                    "Coming Soon",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
        }

        /// <summary>
        /// My Profile button clicked - shows the profile page
        /// </summary>
        private void MyProfile_Click(object sender, RoutedEventArgs e)
        {
            // Try to navigate to ProfilePage, show message if it doesn't exist yet
            try
            {
                ContentFrame.Navigate(new ProfilePage(_currentUser));
            }
            catch
            {
                MessageBox.Show(
                    "Profile page - To be implemented",
                    "Coming Soon",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
        }

        /// <summary>
        /// Leave Management button clicked - navigates to the leave page
        /// This is a working page that shows leave requests
        /// </summary>
        private void LeaveManagement_Click(object sender, RoutedEventArgs e)
        {
            // Try to navigate to LeavePage
            try
            {
                ContentFrame.Navigate(new LeavePage(_currentUser));
            }
            catch
            {
                MessageBox.Show(
                    "Leave management page - To be implemented",
                    "Coming Soon",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
        }

        /// <summary>
        /// Payroll button clicked - navigates to payroll page
        /// </summary>
        private void Payroll_Click(object sender, RoutedEventArgs e)
        {
            // Try to navigate to PayrollPage
            try
            {
                ContentFrame.Navigate(new PayrollPage(_currentUser));
            }
            catch
            {
                MessageBox.Show(
                    "Payroll page - To be implemented",
                    "Coming Soon",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
        }

        /// <summary>
        /// Holidays button clicked - navigates to holidays page
        /// </summary>
        private void Holidays_Click(object sender, RoutedEventArgs e)
        {
            // Try to navigate to HolidaysPage
            try
            {
                ContentFrame.Navigate(new HolidaysPage(_currentUser));
            }
            catch
            {
                MessageBox.Show(
                    "Holidays page - To be implemented",
                    "Coming Soon",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
        }

        /// <summary>
        /// Grievances button clicked - navigates to grievances page
        /// </summary>
        private void Grievances_Click(object sender, RoutedEventArgs e)
        {
            // Try to navigate to GrievancesPage
            try
            {
                ContentFrame.Navigate(new GrievancesPage(_currentUser));
            }
            catch
            {
                MessageBox.Show(
                    "Grievances page - To be implemented",
                    "Coming Soon",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
        }

        /// <summary>
        /// Manage Employees button clicked (ADMIN ONLY)
        /// Navigates to employee management page
        /// </summary>
        private void ManageEmployees_Click(object sender, RoutedEventArgs e)
        {
            // Try to navigate to EmployeeManagementPage
            try
            {
                ContentFrame.Navigate(new EmployeeManagementPage(_currentUser));
            }
            catch
            {
                MessageBox.Show(
                    "Employee Management page - To be implemented",
                    "Coming Soon",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
        }

        /// <summary>
        /// Departments button clicked (ADMIN ONLY)
        /// Navigates to the departments management page.
        /// </summary>
        private void Departments_Click(object sender, RoutedEventArgs e)
        {
            // Try to navigate to DepartmentsPage
            try
            {
                ContentFrame.Navigate(new DepartmentsPage(_currentUser));
            }
            catch
            {
                MessageBox.Show(
                    "Departments page - To be implemented",
                    "Coming Soon",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
        }

        /// <summary>
        /// Logout button clicked - returns to the login window
        /// This closes the dashboard and shows the login screen
        /// </summary>
        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            // Create a new instance of the LoginWindow
            var loginWindow = new LoginWindow();

            // Show the login window
            loginWindow.Show();

            // Close this dashboard window
            // 'this' refers to the current DashboardWindow
            this.Close();
        }
    }
}