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
        /// Dashboard button clicked - clears the content frame
        /// </summary>
        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            // Clear any page that's currently showing in the content frame
            // This shows a blank dashboard area
            ContentFrame.Content = null;
        }

        /// <summary>
        /// My Profile button clicked - shows a "coming soon" message
        /// TODO: Create a ProfilePage.xaml and navigate to it
        /// </summary>
        private void MyProfile_Click(object sender, RoutedEventArgs e)
        {
            // Show a popup message box
            // MessageBoxButton.OK means it only has an OK button
            // MessageBoxImage.Information shows the blue "i" icon
            MessageBox.Show(
                "Profile page - To be implemented",
                "Coming Soon",
                MessageBoxButton.OK,
                MessageBoxImage.Information
     
                    ContentFrame.Navigate(new ProfilePage(_currentUser)));
        }

        /// <summary>
        /// Leave Management button clicked - navigates to the leave page
        /// This is a working page that shows leave requests
        /// </summary>
        private void LeaveManagement_Click(object sender, RoutedEventArgs e)
        {
            // Navigate to the LeavePage
            // We pass the current user so the page knows who's logged in
            ContentFrame.Navigate(new LeavePage(_currentUser));
        }

        /// <summary>
        /// Payroll button clicked - shows a "coming soon" message
        /// TODO: Create a PayrollPage.xaml and navigate to it
        /// </summary>
        private void Payroll_Click(object sender, RoutedEventArgs e)
        {
            // Show a popup message box
            MessageBox.Show(
                "Payroll page - To be implemented",
                "Coming Soon",
                MessageBoxButton.OK,
                MessageBoxImage.Information
           
                    ContentFrame.Navigate(new PayrollPage(_cuentUser)););
        }

        /// <summary>
        /// Holidays button clicked - shows a "coming soon" message
        /// TODO: Create a HolidaysPage.xaml and navigate to it
        /// </summary>
        private void Holidays_Click(object sender, RoutedEventArgs e)
        {
            // Show a popup message box
                    ContentFrame.Navigate(new HolidaysPage(_currentUser));

            MessageBox.Show(
                "Holidays page - To be implemented",
                "Coming Soon",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
        

        /// <summary>
        /// Grievances button clicked - shows a "coming soon" message
        /// TODO: Create a GrievancesPage.xaml and navigate to it
        /// </summary>
        private void Grievances_Click(object sender, RoutedEventArgs e)
        {
            // Show a popup message box
            MessageBox.Show(
                "Grievances page - To be implemented",
                "Coming Soon",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
                    ContentFrame.Navigate(new GrievancesPage(_currentUser));

        }

        /// <summary>
        /// Manage Employees button clicked (ADMIN ONLY)
        /// Shows a "coming soon" message
        /// TODO: Create an EmployeeManagementPage.xaml and navigate to it
        /// </summary>
        private void ManageEmployees_Click(object sender, RoutedEventArgs e)
        {
            // Show a popup message box
            MessageBox.Show(
                "Employee Management page - To be implemented",
                "Coming Soon",
                MessageBoxButton.OK,
                        ContentFrame.Navigate(new EmployeeManagementPage(_currentUser));

                MessageBoxImage.Information
            );
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
