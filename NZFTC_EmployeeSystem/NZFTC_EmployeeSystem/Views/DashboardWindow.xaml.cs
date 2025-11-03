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
    /// Dashboard window - shows after login with navigation menu and user's profile picture
    /// </summary>
    public partial class DashboardWindow : Window
    {
        private readonly User _currentUser;
        private bool _isMenuCollapsed = false;

        public DashboardWindow(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;

            WelcomeText.Text = $"Welcome, {_currentUser.Employee?.FullName ?? _currentUser.Username}";
            LoadUserProfilePicture();

            if (_currentUser.Role == "Admin")
            {
                AdminMenuPanel.Visibility = Visibility.Visible;
            }
        }

        // Load user's profile picture from ProfilePictures folder
        private void LoadUserProfilePicture()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var employee = db.Employees.FirstOrDefault(e => e.Id == _currentUser.EmployeeId);

                    if (employee != null && !string.IsNullOrEmpty(employee.ProfilePicturePath))
                    {
                        string fullPath = GetProfilePicturePath(employee.ProfilePicturePath);

                        if (File.Exists(fullPath))
                        {
                            try
                            {
                                var bitmap = new BitmapImage();
                                bitmap.BeginInit();
                                bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                bitmap.EndInit();

                                UserProfilePicture.Source = bitmap;
                                UserProfilePicture.Visibility = Visibility.Visible;
                                DefaultUserAvatar.Visibility = Visibility.Collapsed;
                            }
                            catch
                            {
                                UserProfilePicture.Visibility = Visibility.Collapsed;
                                DefaultUserAvatar.Visibility = Visibility.Visible;
                            }
                        }
                    }
                }
            }
            catch
            {
                UserProfilePicture.Visibility = Visibility.Collapsed;
                DefaultUserAvatar.Visibility = Visibility.Visible;
            }
        }

        // Get full path to profile picture
        private string GetProfilePicturePath(string fileName)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var projectRoot = Directory.GetParent(baseDir)?.Parent?.Parent?.Parent?.FullName ?? baseDir;
            var picturesFolder = Path.Combine(projectRoot, "ProfilePictures");
            return Path.Combine(picturesFolder, fileName);
        }

        // Toggle menu collapse/expand
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

        // Expand the menu
        private void ExpandMenu()
        {
            var storyboard = (Storyboard)FindResource("ExpandMenu");
            storyboard.Begin();

            // Show expanded buttons and enable them
            DashboardButtonExpanded.Visibility = Visibility.Visible;
            DashboardButtonExpanded.IsEnabled = true;
            AccountButtonExpanded.Visibility = Visibility.Visible;
            AccountButtonExpanded.IsEnabled = true;
            LeaveManagementButtonExpanded.Visibility = Visibility.Visible;
            LeaveManagementButtonExpanded.IsEnabled = true;
            HolidaysButtonExpanded.Visibility = Visibility.Visible;
            HolidaysButtonExpanded.IsEnabled = true;

            // Hide collapsed buttons
            DashboardButtonCollapsed.Visibility = Visibility.Collapsed;
            DashboardButtonCollapsed.IsEnabled = false;
            AccountButtonCollapsed.Visibility = Visibility.Collapsed;
            AccountButtonCollapsed.IsEnabled = false;
            LeaveManagementButtonCollapsed.Visibility = Visibility.Collapsed;
            LeaveManagementButtonCollapsed.IsEnabled = false;
            HolidaysButtonCollapsed.Visibility = Visibility.Collapsed;
            HolidaysButtonCollapsed.IsEnabled = false;

            // Handle admin buttons if visible
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

                // Show admin header text
                AdminHeaderText.Visibility = Visibility.Visible;
            }

            // Show headers and logo
            MainHeaderText.Visibility = Visibility.Visible;
            EmployeeHeaderText.Visibility = Visibility.Visible;
            LogoPanel.Visibility = Visibility.Visible;
        }

        // Collapse the menu
        private void CollapseMenu()
        {
            var storyboard = (Storyboard)FindResource("CollapseMenu");
            storyboard.Begin();

            // Hide expanded buttons completely (not clickable)
            DashboardButtonExpanded.Visibility = Visibility.Collapsed;
            DashboardButtonExpanded.IsEnabled = false;
            AccountButtonExpanded.Visibility = Visibility.Collapsed;
            AccountButtonExpanded.IsEnabled = false;
            LeaveManagementButtonExpanded.Visibility = Visibility.Collapsed;
            LeaveManagementButtonExpanded.IsEnabled = false;
            HolidaysButtonExpanded.Visibility = Visibility.Collapsed;
            HolidaysButtonExpanded.IsEnabled = false;

            // Show collapsed buttons
            DashboardButtonCollapsed.Visibility = Visibility.Visible;
            DashboardButtonCollapsed.IsEnabled = true;
            AccountButtonCollapsed.Visibility = Visibility.Visible;
            AccountButtonCollapsed.IsEnabled = true;
            LeaveManagementButtonCollapsed.Visibility = Visibility.Visible;
            LeaveManagementButtonCollapsed.IsEnabled = true;
            HolidaysButtonCollapsed.Visibility = Visibility.Visible;
            HolidaysButtonCollapsed.IsEnabled = true;

            // Handle admin buttons if visible
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

                // Hide admin header text
                AdminHeaderText.Visibility = Visibility.Collapsed;
            }

            // Hide all headers and logo
            MainHeaderText.Visibility = Visibility.Collapsed;
            EmployeeHeaderText.Visibility = Visibility.Collapsed;
            LogoPanel.Visibility = Visibility.Collapsed;
        }

        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            try { ContentFrame.Navigate(new DashboardHomePage(_currentUser)); }
            catch { MessageBox.Show("Dashboard page coming soon", "Info"); }
        }

        private void MyProfile_Click(object sender, RoutedEventArgs e)
        {
            try { ContentFrame.Navigate(new ProfilePage(_currentUser)); }
            catch { MessageBox.Show("Profile page coming soon", "Info"); }
        }

        private void LeaveManagement_Click(object sender, RoutedEventArgs e)
        {
            try { ContentFrame.Navigate(new LeavePage(_currentUser)); }
            catch { MessageBox.Show("Leave page coming soon", "Info"); }
        }

        private void Payroll_Click(object sender, RoutedEventArgs e)
        {
            try { ContentFrame.Navigate(new PayrollPage(_currentUser)); }
            catch { MessageBox.Show("Payroll page coming soon", "Info"); }
        }

        private void Holidays_Click(object sender, RoutedEventArgs e)
        {
            try { ContentFrame.Navigate(new HolidaysPage(_currentUser)); }
            catch { MessageBox.Show("Holidays page coming soon", "Info"); }
        }

        private void Grievances_Click(object sender, RoutedEventArgs e)
        {
            try { ContentFrame.Navigate(new GrievancesPage(_currentUser)); }
            catch { MessageBox.Show("Grievances page coming soon", "Info"); }
        }

        private void ManageEmployees_Click(object sender, RoutedEventArgs e)
        {
            try { ContentFrame.Navigate(new EmployeeManagementPage(_currentUser)); }
            catch { MessageBox.Show("Employee Management page coming soon", "Info"); }
        }

        private void Departments_Click(object sender, RoutedEventArgs e)
        {
            try { ContentFrame.Navigate(new DepartmentsPage(_currentUser)); }
            catch { MessageBox.Show("Departments page coming soon", "Info"); }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }
    }
}