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

                // Show holidays for admin (they can see company holidays)
                HolidaysButtonGrid.Visibility = Visibility.Visible;

                // Hide employee-specific buttons (My Training, My Pay, Leave, Grievance)
                MyTrainingButtonGrid.Visibility = Visibility.Collapsed;
                MyPayButtonGrid.Visibility = Visibility.Collapsed;
                LeaveButtonGrid.Visibility = Visibility.Collapsed;
                GrievanceButtonGrid.Visibility = Visibility.Collapsed;
            }
            // WORKPLACE TRAINER - Training and department management access
            else if (_currentUser.Role == "Workplace Trainer")
            {
                // Show: Dashboard, Account, Employee Management, Departments, Holidays, About
                // Hide: Leave Management, Payroll, Grievances, My Training, My Pay, Leave, Grievance

                // Show admin and trainer menu items (Employee Management, Departments)
                AdminTrainerMenuPanel.Visibility = Visibility.Visible;

                // Hide admin-only items (Payroll, Leave Management, Grievances)
                AdminOnlyMenuPanel.Visibility = Visibility.Collapsed;

                // Show holidays for trainers
                HolidaysButtonGrid.Visibility = Visibility.Visible;

                // Hide employee-specific buttons (My Training, My Pay, Leave, Grievance)
                MyTrainingButtonGrid.Visibility = Visibility.Collapsed;
                MyPayButtonGrid.Visibility = Visibility.Collapsed;
                LeaveButtonGrid.Visibility = Visibility.Collapsed;
                GrievanceButtonGrid.Visibility = Visibility.Collapsed;
            }
            // EMPLOYEE - Basic access
            else // Default to Employee role
            {
                // Show: Dashboard, Account, My Training, My Pay, Holidays, Leave, Grievance, About
                // Hide: Employee Management, Departments, Payroll, Leave Management, Grievances

                // Hide admin and trainer sections
                AdminTrainerMenuPanel.Visibility = Visibility.Collapsed;
                AdminOnlyMenuPanel.Visibility = Visibility.Collapsed;

                // Show employee-specific buttons
                MyTrainingButtonGrid.Visibility = Visibility.Visible;
                MyPayButtonGrid.Visibility = Visibility.Visible;
                HolidaysButtonGrid.Visibility = Visibility.Visible;
                LeaveButtonGrid.Visibility = Visibility.Visible;
                GrievanceButtonGrid.Visibility = Visibility.Visible;
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
            if (LeaveButtonGrid.Visibility == Visibility.Visible)
            {
                LeaveButtonExpanded.Tag = null;
                LeaveButtonCollapsed.Tag = null;
            }
            if (GrievanceButtonGrid.Visibility == Visibility.Visible)
            {
                GrievanceButtonExpanded.Tag = null;
                GrievanceButtonCollapsed.Tag = null;
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

            // Set the active button based on the current page
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
                case "AboutContact":
                    AboutContactButtonExpanded.Tag = "Active";
                    AboutContactButtonCollapsed.Tag = "Active";
                    break;
                case "MyTraining":
                    MyTrainingButtonExpanded.Tag = "Active";
                    MyTrainingButtonCollapsed.Tag = "Active";
                    break;
                case "MyPay":
                    MyPayButtonExpanded.Tag = "Active";
                    MyPayButtonCollapsed.Tag = "Active";
                    break;
                case "Holidays":
                    HolidaysButtonExpanded.Tag = "Active";
                    HolidaysButtonCollapsed.Tag = "Active";
                    break;
                case "Leave":
                    LeaveButtonExpanded.Tag = "Active";
                    LeaveButtonCollapsed.Tag = "Active";
                    break;
                case "Grievance":
                    GrievanceButtonExpanded.Tag = "Active";
                    GrievanceButtonCollapsed.Tag = "Active";
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

        #region Hamburger Menu

        /// <summary>
        /// Handles the hamburger button click to toggle menu collapse/expand
        /// </summary>
        private void HamburgerButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the storyboard resources for animation
            var collapseStoryboard = (Storyboard)this.Resources["CollapseMenu"];
            var expandStoryboard = (Storyboard)this.Resources["ExpandMenu"];

            // Toggle the collapse state
            if (_isMenuCollapsed)
            {
                // Expand the menu
                expandStoryboard.Begin();

                // Show expanded buttons, hide collapsed buttons
                ShowExpandedButtons();

                // Update state
                _isMenuCollapsed = false;
            }
            else
            {
                // Collapse the menu
                collapseStoryboard.Begin();

                // Show collapsed buttons, hide expanded buttons
                ShowCollapsedButtons();

                // Update state
                _isMenuCollapsed = true;
            }
        }

        /// <summary>
        /// Shows expanded menu buttons and hides collapsed versions
        /// </summary>
        private void ShowExpandedButtons()
        {
            // Dashboard button
            DashboardButtonExpanded.Visibility = Visibility.Visible;
            DashboardButtonCollapsed.Visibility = Visibility.Collapsed;

            // Account button
            AccountButtonExpanded.Visibility = Visibility.Visible;
            AccountButtonCollapsed.Visibility = Visibility.Collapsed;

            // About/Contact button
            AboutContactButtonExpanded.Visibility = Visibility.Visible;
            AboutContactButtonCollapsed.Visibility = Visibility.Collapsed;

            // Employee-specific buttons
            if (MyTrainingButtonGrid.Visibility == Visibility.Visible)
            {
                MyTrainingButtonExpanded.Visibility = Visibility.Visible;
                MyTrainingButtonCollapsed.Visibility = Visibility.Collapsed;
            }

            if (MyPayButtonGrid.Visibility == Visibility.Visible)
            {
                MyPayButtonExpanded.Visibility = Visibility.Visible;
                MyPayButtonCollapsed.Visibility = Visibility.Collapsed;
            }

            if (HolidaysButtonGrid.Visibility == Visibility.Visible)
            {
                HolidaysButtonExpanded.Visibility = Visibility.Visible;
                HolidaysButtonCollapsed.Visibility = Visibility.Collapsed;
            }

            if (LeaveButtonGrid.Visibility == Visibility.Visible)
            {
                LeaveButtonExpanded.Visibility = Visibility.Visible;
                LeaveButtonCollapsed.Visibility = Visibility.Collapsed;
            }

            if (GrievanceButtonGrid.Visibility == Visibility.Visible)
            {
                GrievanceButtonExpanded.Visibility = Visibility.Visible;
                GrievanceButtonCollapsed.Visibility = Visibility.Collapsed;
            }

            // Admin/Trainer buttons
            if (AdminTrainerMenuPanel.Visibility == Visibility.Visible)
            {
                EmployeeManagementButtonExpanded.Visibility = Visibility.Visible;
                EmployeeManagementButtonCollapsed.Visibility = Visibility.Collapsed;

                DepartmentsButtonExpanded.Visibility = Visibility.Visible;
                DepartmentsButtonCollapsed.Visibility = Visibility.Collapsed;
            }

            // Admin-only buttons
            if (AdminOnlyMenuPanel.Visibility == Visibility.Visible)
            {
                PayrollButtonExpanded.Visibility = Visibility.Visible;
                PayrollButtonCollapsed.Visibility = Visibility.Collapsed;

                LeaveManagementButtonExpanded.Visibility = Visibility.Visible;
                LeaveManagementButtonCollapsed.Visibility = Visibility.Collapsed;

                GrievancesButtonExpanded.Visibility = Visibility.Visible;
                GrievancesButtonCollapsed.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Shows collapsed menu buttons and hides expanded versions
        /// </summary>
        private void ShowCollapsedButtons()
        {
            // Dashboard button
            DashboardButtonExpanded.Visibility = Visibility.Collapsed;
            DashboardButtonCollapsed.Visibility = Visibility.Visible;

            // Account button
            AccountButtonExpanded.Visibility = Visibility.Collapsed;
            AccountButtonCollapsed.Visibility = Visibility.Visible;

            // About/Contact button
            AboutContactButtonExpanded.Visibility = Visibility.Collapsed;
            AboutContactButtonCollapsed.Visibility = Visibility.Visible;

            // Employee-specific buttons
            if (MyTrainingButtonGrid.Visibility == Visibility.Visible)
            {
                MyTrainingButtonExpanded.Visibility = Visibility.Collapsed;
                MyTrainingButtonCollapsed.Visibility = Visibility.Visible;
            }

            if (MyPayButtonGrid.Visibility == Visibility.Visible)
            {
                MyPayButtonExpanded.Visibility = Visibility.Collapsed;
                MyPayButtonCollapsed.Visibility = Visibility.Visible;
            }

            if (HolidaysButtonGrid.Visibility == Visibility.Visible)
            {
                HolidaysButtonExpanded.Visibility = Visibility.Collapsed;
                HolidaysButtonCollapsed.Visibility = Visibility.Visible;
            }

            if (LeaveButtonGrid.Visibility == Visibility.Visible)
            {
                LeaveButtonExpanded.Visibility = Visibility.Collapsed;
                LeaveButtonCollapsed.Visibility = Visibility.Visible;
            }

            if (GrievanceButtonGrid.Visibility == Visibility.Visible)
            {
                GrievanceButtonExpanded.Visibility = Visibility.Collapsed;
                GrievanceButtonCollapsed.Visibility = Visibility.Visible;
            }

            // Admin/Trainer buttons
            if (AdminTrainerMenuPanel.Visibility == Visibility.Visible)
            {
                EmployeeManagementButtonExpanded.Visibility = Visibility.Collapsed;
                EmployeeManagementButtonCollapsed.Visibility = Visibility.Visible;

                DepartmentsButtonExpanded.Visibility = Visibility.Collapsed;
                DepartmentsButtonCollapsed.Visibility = Visibility.Visible;
            }

            // Admin-only buttons
            if (AdminOnlyMenuPanel.Visibility == Visibility.Visible)
            {
                PayrollButtonExpanded.Visibility = Visibility.Collapsed;
                PayrollButtonCollapsed.Visibility = Visibility.Visible;

                LeaveManagementButtonExpanded.Visibility = Visibility.Collapsed;
                LeaveManagementButtonCollapsed.Visibility = Visibility.Visible;

                GrievancesButtonExpanded.Visibility = Visibility.Collapsed;
                GrievancesButtonCollapsed.Visibility = Visibility.Visible;
            }
        }

        #endregion

        #region Navigation Methods

        /// <summary>
        /// Navigates to Dashboard home page
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
        /// Navigates to Profile/Account page
        /// </summary>
        private void Account_Click(object sender, RoutedEventArgs e)
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
        /// Available to all roles
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
        /// Navigates to Leave page (employee-only)
        /// Shows employee's leave requests and allows submitting new requests
        /// </summary>
        private void Leave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ContentFrame.Navigate(new LeavePage(_currentUser));
                PageTitleText.Text = "Leave";
                SetActiveButton("Leave");
            }
            catch
            {
                MessageBox.Show("Error loading Leave page", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Navigates to Grievance page (employee-only)
        /// Shows employee's grievances and allows submitting new grievances
        /// </summary>
        private void Grievance_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ContentFrame.Navigate(new GrievancesPage(_currentUser));
                PageTitleText.Text = "Grievance";
                SetActiveButton("Grievance");
            }
            catch
            {
                MessageBox.Show("Error loading Grievance page", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Shows About/Contact information popup
        /// Available to all users
        /// </summary>
        private void AboutContact_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ContentFrame.Navigate(new Contact_AboutUsPage(_currentUser));
                PageTitleText.Text = "About/Contact";
                SetActiveButton("AboutContact");
            }
            catch
            {
                MessageBox.Show(
                    "Awaiting creation",
                    "About/Contact",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
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

        #region Public Navigation Methods for Quick Links

        /// <summary>
        /// Public method to navigate to Leave Management
        /// Called by DashboardHomePage quick links
        /// </summary>
        public void NavigateToLeaveManagement()
        {
            LeaveManagement_Click(this, new RoutedEventArgs());
        }

        /// <summary>
        /// Public method to navigate to Leave
        /// Called by DashboardHomePage quick links for employees
        /// </summary>
        public void NavigateToLeave()
        {
            Leave_Click(this, new RoutedEventArgs());
        }

        /// <summary>
        /// Public method to navigate to Grievance
        /// Called by DashboardHomePage quick links for employees
        /// </summary>
        public void NavigateToGrievance()
        {
            Grievance_Click(this, new RoutedEventArgs());
        }

        /// <summary>
        /// Public method to navigate to Payroll
        /// Called by DashboardHomePage quick links
        /// </summary>
        public void NavigateToPayroll()
        {
            Payroll_Click(this, new RoutedEventArgs());
        }

        /// <summary>
        /// Public method to navigate to Departments
        /// Called by DashboardHomePage quick links
        /// </summary>
        public void NavigateToDepartments()
        {
            Departments_Click(this, new RoutedEventArgs());
        }

        /// <summary>
        /// Public method to navigate to My Pay
        /// Called by DashboardHomePage quick links for employees
        /// </summary>
        public void NavigateToMyPay()
        {
            MyPay_Click(this, new RoutedEventArgs());
        }

        /// <summary>
        /// Public method to navigate to My Training
        /// Called by DashboardHomePage quick links for employees
        /// </summary>
        public void NavigateToMyTraining()
        {
            MyTraining_Click(this, new RoutedEventArgs());
        }

        /// <summary>
        /// Public method to navigate to Employee Management
        /// Called by DashboardHomePage quick links for trainers
        /// </summary>
        public void NavigateToEmployeeManagement()
        {
            ManageEmployees_Click(this, new RoutedEventArgs());
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