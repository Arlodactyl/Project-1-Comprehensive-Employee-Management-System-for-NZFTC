using Microsoft.EntityFrameworkCore;
using NZFTC_EmployeeSystem.Data;
using NZFTC_EmployeeSystem.Models;
using NZFTC_EmployeeSystem.Views;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace NZFTC_EmployeeSystem.Views
{
    /// <summary>
    /// Dashboard window - main navigation interface after login
    /// Shows collapsible sidebar menu with role-based buttons
    /// Customized navigation for Admin, Workplace Trainer, and Employee roles
    /// </summary>
    public partial class DashboardWindow : Window
    {
        // The currently logged-in user
        private readonly User _currentUser;

        // Tracks whether the sidebar menu is collapsed (true) or expanded (false)
        private bool _isMenuCollapsed = false;

        /// <summary>
        /// Constructor - initializes the dashboard with user-specific navigation
        /// </summary>
        /// <param name="currentUser">The logged-in user</param>
        public DashboardWindow(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;

            // Set welcome message with user's full name or username
            WelcomeText.Text = $"Welcome, {_currentUser.Employee?.FullName ?? _currentUser.Username}";

            // Load user's profile picture if available
            LoadUserProfilePicture();

            // Customize menu based on user role
            CustomizeMenuForRole();

            // Automatically load Dashboard page on startup
            ContentFrame.Navigate(new DashboardHomePage(_currentUser));
        }

        /// <summary>
        /// Customizes the navigation menu based on user's role
        /// Each role sees different buttons based on their permissions
        /// </summary>
        private void CustomizeMenuForRole()
        {
            // ADMIN - Full access to all features
            if (_currentUser.Role == "Admin")
            {
                // Show admin and trainer menu items
                AdminTrainerMenuPanel.Visibility = Visibility.Visible;

                // Show admin-only items
                AdminOnlyMenuPanel.Visibility = Visibility.Visible;

                // Hide employee-specific buttons
                MyTrainingButtonGrid.Visibility = Visibility.Collapsed;
                MyPayButtonGrid.Visibility = Visibility.Collapsed;
                HolidaysButtonGrid.Visibility = Visibility.Collapsed;
            }
            // WORKPLACE TRAINER - Training and department management access
            else if (_currentUser.Role == "Workplace Trainer")
            {
                // Show: Dashboard, Account, Employee Management, Departments, About
                // Hide: Holidays, Leave Management, Payroll, Grievances

                // Show admin and trainer menu items (Employee Management, Departments)
                // FIXED: Trainers now have full access to Departments
                AdminTrainerMenuPanel.Visibility = Visibility.Visible;

                // Hide admin-only items (Payroll, Leave Management, Grievances)
                AdminOnlyMenuPanel.Visibility = Visibility.Collapsed;

                // Hide employee-specific buttons
                MyTrainingButtonGrid.Visibility = Visibility.Collapsed;
                MyPayButtonGrid.Visibility = Visibility.Collapsed;
                HolidaysButtonGrid.Visibility = Visibility.Collapsed;
            }
            // EMPLOYEE - Basic access
            else // Default to Employee role
            {
                // Show: Dashboard, Account, My Training, My Pay, Holidays, About
                // Hide: Employee Management, Departments, Payroll, Leave Management, Grievances

                // Hide admin and trainer sections
                AdminTrainerMenuPanel.Visibility = Visibility.Collapsed;
                AdminOnlyMenuPanel.Visibility = Visibility.Collapsed;

                // Show employee-specific buttons
                MyTrainingButtonGrid.Visibility = Visibility.Visible;
                MyPayButtonGrid.Visibility = Visibility.Visible;
                HolidaysButtonGrid.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Loads and displays the user's profile picture from the ProfilePictures folder
        /// Shows default red circle if no picture is available
        /// </summary>
        private void LoadUserProfilePicture()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    // Get employee record for current user
                    var employee = db.Employees.FirstOrDefault(e => e.Id == _currentUser.EmployeeId);

                    // Check if employee has a profile picture path set
                    if (employee != null && !string.IsNullOrEmpty(employee.ProfilePicturePath))
                    {
                        string fullPath = GetProfilePicturePath(employee.ProfilePicturePath);

                        // Verify file exists before trying to load it
                        if (File.Exists(fullPath))
                        {
                            try
                            {
                                // Load image file into bitmap
                                var bitmap = new BitmapImage();
                                bitmap.BeginInit();
                                bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                bitmap.EndInit();

                                // Display profile picture and hide default avatar
                                UserProfilePicture.Source = bitmap;
                                UserProfilePicture.Visibility = Visibility.Visible;
                                DefaultUserAvatar.Visibility = Visibility.Collapsed;
                            }
                            catch
                            {
                                // If image fails to load, show default avatar
                                UserProfilePicture.Visibility = Visibility.Collapsed;
                                DefaultUserAvatar.Visibility = Visibility.Visible;
                            }
                        }
                    }
                }
            }
            catch
            {
                // If any error occurs, show default avatar
                UserProfilePicture.Visibility = Visibility.Collapsed;
                DefaultUserAvatar.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Constructs the full file path to the profile picture
        /// Looks in the ProfilePictures folder in the project root
        /// </summary>
        private string GetProfilePicturePath(string fileName)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var projectRoot = Directory.GetParent(baseDir)?.Parent?.Parent?.Parent?.FullName ?? baseDir;
            var picturesFolder = Path.Combine(projectRoot, "ProfilePictures");
            return Path.Combine(picturesFolder, fileName);
        }

        /// <summary>
        /// Updates all menu buttons to show which page is currently active
        /// Sets Tag="Active" on the current page button to apply grey background
        /// </summary>
        private void SetActiveButton(string buttonName)
        {
            // Clear all button active states first
            DashboardButtonExpanded.Tag = null;
            DashboardButtonCollapsed.Tag = null;
            AccountButtonExpanded.Tag = null;
            AccountButtonCollapsed.Tag = null;
            AboutContactButtonExpanded.Tag = null;
            AboutContactButtonCollapsed.Tag = null;

            // Clear employee-specific buttons if visible
            if (MyTrainingButtonGrid.Visibility == Visibility.Visible)
            {
                MyTrainingButtonExpanded.Tag = null;
                MyTrainingButtonCollapsed.Tag = null;
            }
            if (MyPayButtonGrid.Visibility == Visibility.Visible)
            {
                MyPayButtonExpanded.Tag = null;
                MyPayButtonCollapsed.Tag = null;
            }
            if (HolidaysButtonGrid.Visibility == Visibility.Visible)
            {
                HolidaysButtonExpanded.Tag = null;
                HolidaysButtonCollapsed.Tag = null;
            }

            // Clear admin/trainer buttons if visible
            if (AdminTrainerMenuPanel.Visibility == Visibility.Visible)
            {
                EmployeeManagementButtonExpanded.Tag = null;
                EmployeeManagementButtonCollapsed.Tag = null;
                DepartmentsButtonExpanded.Tag = null;
                DepartmentsButtonCollapsed.Tag = null;
            }

            // Clear admin-only buttons if visible
            if (AdminOnlyMenuPanel.Visibility == Visibility.Visible)
            {
                PayrollButtonExpanded.Tag = null;
                PayrollButtonCollapsed.Tag = null;
                LeaveManagementButtonExpanded.Tag = null;
                LeaveManagementButtonCollapsed.Tag = null;
                GrievancesButtonExpanded.Tag = null;
                GrievancesButtonCollapsed.Tag = null;
            }

            // Set the active button based on which page is loaded
            switch (buttonName)
            {
                case "Dashboard":
                    DashboardButtonExpanded.Tag = "Active";
                    DashboardButtonCollapsed.Tag = "Active";
                    break;
                case "Account":
                    AccountButtonExpanded.Tag = "Active";
                    AccountButtonCollapsed.Tag = "Active";
                    break;
                case "MyTraining":
                    if (MyTrainingButtonGrid.Visibility == Visibility.Visible)
                    {
                        MyTrainingButtonExpanded.Tag = "Active";
                        MyTrainingButtonCollapsed.Tag = "Active";
                    }
                    break;
                case "MyPay":
                    if (MyPayButtonGrid.Visibility == Visibility.Visible)
                    {
                        MyPayButtonExpanded.Tag = "Active";
                        MyPayButtonCollapsed.Tag = "Active";
                    }
                    break;
                case "Holidays":
                    if (HolidaysButtonGrid.Visibility == Visibility.Visible)
                    {
                        HolidaysButtonExpanded.Tag = "Active";
                        HolidaysButtonCollapsed.Tag = "Active";
                    }
                    break;
                case "AboutContact":
                    AboutContactButtonExpanded.Tag = "Active";
                    AboutContactButtonCollapsed.Tag = "Active";
                    break;
                case "EmployeeManagement":
                    EmployeeManagementButtonExpanded.Tag = "Active";
                    EmployeeManagementButtonCollapsed.Tag = "Active";
                    break;
                case "Departments":
                    DepartmentsButtonExpanded.Tag = "Active";
                    DepartmentsButtonCollapsed.Tag = "Active";
                    break;
                case "Payroll":
                    PayrollButtonExpanded.Tag = "Active";
                    PayrollButtonCollapsed.Tag = "Active";
                    break;
                case "LeaveManagement":
                    LeaveManagementButtonExpanded.Tag = "Active";
                    LeaveManagementButtonCollapsed.Tag = "Active";
                    break;
                case "Grievances":
                    GrievancesButtonExpanded.Tag = "Active";
                    GrievancesButtonCollapsed.Tag = "Active";
                    break;
            }
        }

        /// <summary>
        /// Toggles the sidebar menu between expanded and collapsed states
        /// </summary>
        private void MenuToggle_Click(object sender, RoutedEventArgs e)
        {
            if (_isMenuCollapsed)
            {
                ExpandMenu();
            }
            else
            {
                CollapseMenu();
            }

            _isMenuCollapsed = !_isMenuCollapsed;
        }

        /// <summary>
        /// Expands the sidebar menu to show full button text
        /// Uses animation defined in XAML resources
        /// </summary>
        private void ExpandMenu()
        {
            // Start expand animation
            var storyboard = (Storyboard)FindResource("ExpandMenu");
            storyboard.Begin();

            // Show expanded buttons (full text versions)
            DashboardButtonExpanded.Visibility = Visibility.Visible;
            DashboardButtonExpanded.IsEnabled = true;
            AccountButtonExpanded.Visibility = Visibility.Visible;
            AccountButtonExpanded.IsEnabled = true;
            AboutContactButtonExpanded.Visibility = Visibility.Visible;
            AboutContactButtonExpanded.IsEnabled = true;

            // Hide collapsed buttons (abbreviated versions)
            DashboardButtonCollapsed.Visibility = Visibility.Collapsed;
            DashboardButtonCollapsed.IsEnabled = false;
            AccountButtonCollapsed.Visibility = Visibility.Collapsed;
            AccountButtonCollapsed.IsEnabled = false;
            AboutContactButtonCollapsed.Visibility = Visibility.Collapsed;
            AboutContactButtonCollapsed.IsEnabled = false;

            // Handle employee-specific buttons
            if (MyTrainingButtonGrid.Visibility == Visibility.Visible)
            {
                MyTrainingButtonExpanded.Visibility = Visibility.Visible;
                MyTrainingButtonExpanded.IsEnabled = true;
                MyTrainingButtonCollapsed.Visibility = Visibility.Collapsed;
                MyTrainingButtonCollapsed.IsEnabled = false;
            }
            if (MyPayButtonGrid.Visibility == Visibility.Visible)
            {
                MyPayButtonExpanded.Visibility = Visibility.Visible;
                MyPayButtonExpanded.IsEnabled = true;
                MyPayButtonCollapsed.Visibility = Visibility.Collapsed;
                MyPayButtonCollapsed.IsEnabled = false;
            }
            if (HolidaysButtonGrid.Visibility == Visibility.Visible)
            {
                HolidaysButtonExpanded.Visibility = Visibility.Visible;
                HolidaysButtonExpanded.IsEnabled = true;
                HolidaysButtonCollapsed.Visibility = Visibility.Collapsed;
                HolidaysButtonCollapsed.IsEnabled = false;
            }

            // Handle admin/trainer buttons
            if (AdminTrainerMenuPanel.Visibility == Visibility.Visible)
            {
                EmployeeManagementButtonExpanded.Visibility = Visibility.Visible;
                EmployeeManagementButtonExpanded.IsEnabled = true;
                EmployeeManagementButtonCollapsed.Visibility = Visibility.Collapsed;
                EmployeeManagementButtonCollapsed.IsEnabled = false;

                DepartmentsButtonExpanded.Visibility = Visibility.Visible;
                DepartmentsButtonExpanded.IsEnabled = true;
                DepartmentsButtonCollapsed.Visibility = Visibility.Collapsed;
                DepartmentsButtonCollapsed.IsEnabled = false;
            }

            // Handle admin-only buttons
            if (AdminOnlyMenuPanel.Visibility == Visibility.Visible)
            {
                PayrollButtonExpanded.Visibility = Visibility.Visible;
                PayrollButtonExpanded.IsEnabled = true;
                PayrollButtonCollapsed.Visibility = Visibility.Collapsed;
                PayrollButtonCollapsed.IsEnabled = false;

                LeaveManagementButtonExpanded.Visibility = Visibility.Visible;
                LeaveManagementButtonExpanded.IsEnabled = true;
                LeaveManagementButtonCollapsed.Visibility = Visibility.Collapsed;
                LeaveManagementButtonCollapsed.IsEnabled = false;

                GrievancesButtonExpanded.Visibility = Visibility.Visible;
                GrievancesButtonExpanded.IsEnabled = true;
                GrievancesButtonCollapsed.Visibility = Visibility.Collapsed;
                GrievancesButtonCollapsed.IsEnabled = false;
            }

            // Show logo at bottom
            LogoPanel.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Collapses the sidebar menu to show only icons/abbreviations
        /// Uses animation defined in XAML resources
        /// </summary>
        private void CollapseMenu()
        {
            // Start collapse animation
            var storyboard = (Storyboard)FindResource("CollapseMenu");
            storyboard.Begin();

            // Hide expanded buttons (full text versions)
            DashboardButtonExpanded.Visibility = Visibility.Collapsed;
            DashboardButtonExpanded.IsEnabled = false;
            AccountButtonExpanded.Visibility = Visibility.Collapsed;
            AccountButtonExpanded.IsEnabled = false;
            AboutContactButtonExpanded.Visibility = Visibility.Collapsed;
            AboutContactButtonExpanded.IsEnabled = false;

            // Show collapsed buttons (abbreviated versions)
            DashboardButtonCollapsed.Visibility = Visibility.Visible;
            DashboardButtonCollapsed.IsEnabled = true;
            AccountButtonCollapsed.Visibility = Visibility.Visible;
            AccountButtonCollapsed.IsEnabled = true;
            AboutContactButtonCollapsed.Visibility = Visibility.Visible;
            AboutContactButtonCollapsed.IsEnabled = true;

            // Handle employee-specific buttons
            if (MyTrainingButtonGrid.Visibility == Visibility.Visible)
            {
                MyTrainingButtonExpanded.Visibility = Visibility.Collapsed;
                MyTrainingButtonExpanded.IsEnabled = false;
                MyTrainingButtonCollapsed.Visibility = Visibility.Visible;
                MyTrainingButtonCollapsed.IsEnabled = true;
            }
            if (MyPayButtonGrid.Visibility == Visibility.Visible)
            {
                MyPayButtonExpanded.Visibility = Visibility.Collapsed;
                MyPayButtonExpanded.IsEnabled = false;
                MyPayButtonCollapsed.Visibility = Visibility.Visible;
                MyPayButtonCollapsed.IsEnabled = true;
            }
            if (HolidaysButtonGrid.Visibility == Visibility.Visible)
            {
                HolidaysButtonExpanded.Visibility = Visibility.Collapsed;
                HolidaysButtonExpanded.IsEnabled = false;
                HolidaysButtonCollapsed.Visibility = Visibility.Visible;
                HolidaysButtonCollapsed.IsEnabled = true;
            }

            // Handle admin/trainer buttons
            if (AdminTrainerMenuPanel.Visibility == Visibility.Visible)
            {
                EmployeeManagementButtonExpanded.Visibility = Visibility.Collapsed;
                EmployeeManagementButtonExpanded.IsEnabled = false;
                EmployeeManagementButtonCollapsed.Visibility = Visibility.Visible;
                EmployeeManagementButtonCollapsed.IsEnabled = true;

                DepartmentsButtonExpanded.Visibility = Visibility.Collapsed;
                DepartmentsButtonExpanded.IsEnabled = false;
                DepartmentsButtonCollapsed.Visibility = Visibility.Visible;
                DepartmentsButtonCollapsed.IsEnabled = true;
            }

            // Handle admin-only buttons
            if (AdminOnlyMenuPanel.Visibility == Visibility.Visible)
            {
                PayrollButtonExpanded.Visibility = Visibility.Collapsed;
                PayrollButtonExpanded.IsEnabled = false;
                PayrollButtonCollapsed.Visibility = Visibility.Visible;
                PayrollButtonCollapsed.IsEnabled = true;

                LeaveManagementButtonExpanded.Visibility = Visibility.Collapsed;
                LeaveManagementButtonExpanded.IsEnabled = false;
                LeaveManagementButtonCollapsed.Visibility = Visibility.Visible;
                LeaveManagementButtonCollapsed.IsEnabled = true;

                GrievancesButtonExpanded.Visibility = Visibility.Collapsed;
                GrievancesButtonExpanded.IsEnabled = false;
                GrievancesButtonCollapsed.Visibility = Visibility.Visible;
                GrievancesButtonCollapsed.IsEnabled = true;
            }

            // Hide logo when collapsed
            LogoPanel.Visibility = Visibility.Collapsed;
        }

        #region Navigation Button Click Handlers

        /// <summary>
        /// Navigates to Dashboard home page
        /// Shows summary statistics for the company
        /// </summary>
        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ContentFrame.Navigate(new DashboardHomePage(_currentUser));
                PageTitleText.Text = "Dashboard";
                SetActiveButton("Dashboard");
            }
            catch
            {
                MessageBox.Show("Error loading Dashboard page", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Navigates to My Account/Profile page
        /// Shows employee's personal information and settings
        /// </summary>
        private void MyProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ContentFrame.Navigate(new ProfilePage(_currentUser));
                PageTitleText.Text = "My Account";
                SetActiveButton("Account");
            }
            catch
            {
                MessageBox.Show("Error loading Profile page", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// NEW: Navigates to My Training page
        /// Shows ONLY the logged-in user's training records
        /// Available to Employees only
        /// </summary>
        private void MyTraining_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Navigate to the new MyTrainingPage that shows only current user's training
                ContentFrame.Navigate(new MyTrainingPage(_currentUser));
                PageTitleText.Text = "My Training";
                SetActiveButton("MyTraining");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error loading My Training page: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// NEW: Navigates to My Pay page (employee-only)
        /// Shows ONLY the logged-in employee's payslips
        /// </summary>
        private void MyPay_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Navigate to payroll page (it will show only this employee's payslips)
                ContentFrame.Navigate(new PayrollPage(_currentUser));
                PageTitleText.Text = "My Pay";
                SetActiveButton("MyPay");
            }
            catch
            {
                MessageBox.Show("Error loading My Pay page", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Navigates to Holidays page
        /// Shows company holidays and countdown
        /// Available to Employees only
        /// </summary>
        private void Holidays_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ContentFrame.Navigate(new HolidaysPage(_currentUser));
                PageTitleText.Text = "Holidays";
                SetActiveButton("Holidays");
            }
            catch
            {
                MessageBox.Show("Error loading Holidays page", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Shows About/Contact information popup
        /// Available to all users
        /// </summary>
        private void AboutContact_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Awaiting creation",
                "About/Contact",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
            PageTitleText.Text = "About/Contact";
            SetActiveButton("AboutContact");
        }

        /// <summary>
        /// Navigates to Employee Management page
        /// Manages all employees and their training
        /// Available to Admin and Workplace Trainer
        /// </summary>
        private void ManageEmployees_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ContentFrame.Navigate(new EmployeeManagementPage(_currentUser));
                PageTitleText.Text = "Employee Management";
                SetActiveButton("EmployeeManagement");
            }
            catch
            {
                MessageBox.Show("Error loading Employee Management page", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Navigates to Departments page
        /// Manages company departments
        /// Available to Admin and Workplace Trainer
        /// </summary>
        private void Departments_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ContentFrame.Navigate(new DepartmentsPage(_currentUser));
                PageTitleText.Text = "Departments";
                SetActiveButton("Departments");
            }
            catch
            {
                MessageBox.Show("Error loading Departments page", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Navigates to Payroll page (full admin access)
        /// Manages all employee payslips
        /// Available to Admin only
        /// </summary>
        private void Payroll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ContentFrame.Navigate(new PayrollPage(_currentUser));
                PageTitleText.Text = "Payroll";
                SetActiveButton("Payroll");
            }
            catch
            {
                MessageBox.Show("Error loading Payroll page", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Navigates to Leave Management page
        /// Manages all employee leave requests
        /// Available to Admin only
        /// </summary>
        private void LeaveManagement_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ContentFrame.Navigate(new LeavePage(_currentUser));
                PageTitleText.Text = "Leave Management";
                SetActiveButton("LeaveManagement");
            }
            catch
            {
                MessageBox.Show("Error loading Leave Management page", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Navigates to Grievances page
        /// Manages employee grievances
        /// Available to Admin only
        /// </summary>
        private void Grievances_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ContentFrame.Navigate(new GrievancesPage(_currentUser));
                PageTitleText.Text = "Grievances";
                SetActiveButton("Grievances");
            }
            catch
            {
                MessageBox.Show("Error loading Grievances page", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        /// <summary>
        /// Logs out the current user and returns to login screen
        /// </summary>
        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }
    }
}