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
    /// Shows collapsible sidebar menu, user profile, and content area
    /// </summary>
    public partial class DashboardWindow : Window
    {
        private readonly User _currentUser;
        private bool _isMenuCollapsed = false;

        public DashboardWindow(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;

            // Set welcome message with user's full name or username
            WelcomeText.Text = $"Welcome, {_currentUser.Employee?.FullName ?? _currentUser.Username}";

            // Load user's profile picture if available
            LoadUserProfilePicture();

            // Show admin menu options if user is admin
            if (_currentUser.Role == "Admin")
            {
                AdminMenuPanel.Visibility = Visibility.Visible;
            }

            // Automatically load Dashboard page on startup
            ContentFrame.Navigate(new DashboardHomePage(_currentUser));
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
            LeaveManagementButtonExpanded.Tag = null;
            LeaveManagementButtonCollapsed.Tag = null;
            HolidaysButtonExpanded.Tag = null;
            HolidaysButtonCollapsed.Tag = null;

            // Clear admin button states if they exist
            if (_currentUser.Role == "Admin")
            {
                EmployeeManagementButtonExpanded.Tag = null;
                EmployeeManagementButtonCollapsed.Tag = null;
                DepartmentsButtonExpanded.Tag = null;
                DepartmentsButtonCollapsed.Tag = null;
                PayrollButtonExpanded.Tag = null;
                PayrollButtonCollapsed.Tag = null;
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
                case "LeaveManagement":
                    LeaveManagementButtonExpanded.Tag = "Active";
                    LeaveManagementButtonCollapsed.Tag = "Active";
                    break;
                case "Holidays":
                    HolidaysButtonExpanded.Tag = "Active";
                    HolidaysButtonCollapsed.Tag = "Active";
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

            // Show expanded buttons with full text
            DashboardButtonExpanded.Visibility = Visibility.Visible;
            DashboardButtonExpanded.IsEnabled = true;
            AccountButtonExpanded.Visibility = Visibility.Visible;
            AccountButtonExpanded.IsEnabled = true;
            LeaveManagementButtonExpanded.Visibility = Visibility.Visible;
            LeaveManagementButtonExpanded.IsEnabled = true;
            HolidaysButtonExpanded.Visibility = Visibility.Visible;
            HolidaysButtonExpanded.IsEnabled = true;

            // Hide collapsed buttons (abbreviated text)
            DashboardButtonCollapsed.Visibility = Visibility.Collapsed;
            DashboardButtonCollapsed.IsEnabled = false;
            AccountButtonCollapsed.Visibility = Visibility.Collapsed;
            AccountButtonCollapsed.IsEnabled = false;
            LeaveManagementButtonCollapsed.Visibility = Visibility.Collapsed;
            LeaveManagementButtonCollapsed.IsEnabled = false;
            HolidaysButtonCollapsed.Visibility = Visibility.Collapsed;
            HolidaysButtonCollapsed.IsEnabled = false;

            // Handle admin buttons if user is admin
            if (AdminMenuPanel.Visibility == Visibility.Visible)
            {
                EmployeeManagementButtonExpanded.Visibility = Visibility.Visible;
                EmployeeManagementButtonExpanded.IsEnabled = true;
                EmployeeManagementButtonCollapsed.Visibility = Visibility.Collapsed;
                EmployeeManagementButtonCollapsed.IsEnabled = false;

                DepartmentsButtonExpanded.Visibility = Visibility.Visible;
                DepartmentsButtonExpanded.IsEnabled = true;
                DepartmentsButtonCollapsed.Visibility = Visibility.Collapsed;
                DepartmentsButtonCollapsed.IsEnabled = false;

                PayrollButtonExpanded.Visibility = Visibility.Visible;
                PayrollButtonExpanded.IsEnabled = true;
                PayrollButtonCollapsed.Visibility = Visibility.Collapsed;
                PayrollButtonCollapsed.IsEnabled = false;

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
            LeaveManagementButtonExpanded.Visibility = Visibility.Collapsed;
            LeaveManagementButtonExpanded.IsEnabled = false;
            HolidaysButtonExpanded.Visibility = Visibility.Collapsed;
            HolidaysButtonExpanded.IsEnabled = false;

            // Show collapsed buttons (abbreviated versions)
            DashboardButtonCollapsed.Visibility = Visibility.Visible;
            DashboardButtonCollapsed.IsEnabled = true;
            AccountButtonCollapsed.Visibility = Visibility.Visible;
            AccountButtonCollapsed.IsEnabled = true;
            LeaveManagementButtonCollapsed.Visibility = Visibility.Visible;
            LeaveManagementButtonCollapsed.IsEnabled = true;
            HolidaysButtonCollapsed.Visibility = Visibility.Visible;
            HolidaysButtonCollapsed.IsEnabled = true;

            // Handle admin buttons if user is admin
            if (AdminMenuPanel.Visibility == Visibility.Visible)
            {
                EmployeeManagementButtonExpanded.Visibility = Visibility.Collapsed;
                EmployeeManagementButtonExpanded.IsEnabled = false;
                EmployeeManagementButtonCollapsed.Visibility = Visibility.Visible;
                EmployeeManagementButtonCollapsed.IsEnabled = true;

                DepartmentsButtonExpanded.Visibility = Visibility.Collapsed;
                DepartmentsButtonExpanded.IsEnabled = false;
                DepartmentsButtonCollapsed.Visibility = Visibility.Visible;
                DepartmentsButtonCollapsed.IsEnabled = true;

                PayrollButtonExpanded.Visibility = Visibility.Collapsed;
                PayrollButtonExpanded.IsEnabled = false;
                PayrollButtonCollapsed.Visibility = Visibility.Visible;
                PayrollButtonCollapsed.IsEnabled = true;

                GrievancesButtonExpanded.Visibility = Visibility.Collapsed;
                GrievancesButtonExpanded.IsEnabled = false;
                GrievancesButtonCollapsed.Visibility = Visibility.Visible;
                GrievancesButtonCollapsed.IsEnabled = true;
            }

            // Hide logo when collapsed
            LogoPanel.Visibility = Visibility.Collapsed;
        }

        // Navigation button click handlers - load different pages when buttons are clicked

        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ContentFrame.Navigate(new DashboardHomePage(_currentUser));
                SetActiveButton("Dashboard");
            }
            catch { MessageBox.Show("Dashboard page coming soon", "Info"); }
        }

        private void MyProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ContentFrame.Navigate(new ProfilePage(_currentUser));
                SetActiveButton("Account");
            }
            catch { MessageBox.Show("Profile page coming soon", "Info"); }
        }

        private void LeaveManagement_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ContentFrame.Navigate(new LeavePage(_currentUser));
                SetActiveButton("LeaveManagement");
            }
            catch { MessageBox.Show("Leave page coming soon", "Info"); }
        }

        private void Payroll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ContentFrame.Navigate(new PayrollPage(_currentUser));
                SetActiveButton("Payroll");
            }
            catch { MessageBox.Show("Payroll page coming soon", "Info"); }
        }

        private void Holidays_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ContentFrame.Navigate(new HolidaysPage(_currentUser));
                SetActiveButton("Holidays");
            }
            catch { MessageBox.Show("Holidays page coming soon", "Info"); }
        }

        private void Grievances_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ContentFrame.Navigate(new GrievancesPage(_currentUser));
                SetActiveButton("Grievances");
            }
            catch { MessageBox.Show("Grievances page coming soon", "Info"); }
        }

        private void ManageEmployees_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ContentFrame.Navigate(new EmployeeManagementPage(_currentUser));
                SetActiveButton("EmployeeManagement");
            }
            catch { MessageBox.Show("Employee Management page coming soon", "Info"); }
        }

        private void Departments_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ContentFrame.Navigate(new DepartmentsPage(_currentUser));
                SetActiveButton("Departments");
            }
            catch { MessageBox.Show("Departments page coming soon", "Info"); }
        }

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