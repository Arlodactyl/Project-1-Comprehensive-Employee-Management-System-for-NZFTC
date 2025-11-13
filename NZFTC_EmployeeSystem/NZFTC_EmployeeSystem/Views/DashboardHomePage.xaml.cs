using System;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using NZFTC_EmployeeSystem.Data;
using NZFTC_EmployeeSystem.Models;

namespace NZFTC_EmployeeSystem.Views
{
    public partial class DashboardHomePage : Page
    {
        private readonly AppDbContext _dbContext;
        private readonly User _currentUser;

        public DashboardHomePage(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            _dbContext = new AppDbContext();

            LoadBuildingImage();
            LoadSummary();
            CustomizeQuickLinks();

            this.Unloaded += DashboardHomePage_Unloaded;
        }

        // Below customizes quick links based on user role
        private void CustomizeQuickLinks()
        {
            if (_currentUser.Role == "Admin")
            {
                LeaveManagementButton.Content = "Leave Management";
                LeaveManagementButton.Click += LeaveManagement_Click;

                PayrollButton.Content = "Payroll";
                PayrollButton.Click += Payroll_Click;

                DepartmentsButton.Content = "Departments";
                DepartmentsButton.Click += Departments_Click;
            }
            else if (_currentUser.Role == "Employee")
            {
                LeaveManagementButton.Content = "My Leave";
                LeaveManagementButton.Click += MyLeave_Click;

                PayrollButton.Content = "My Pay";
                PayrollButton.Click += MyPay_Click;

                DepartmentsButton.Content = "My Training";
                DepartmentsButton.Click += MyTraining_Click;
            }
            else if (_currentUser.Role == "Workplace Trainer")
            {
                LeaveManagementButton.Content = "Employee Management";
                LeaveManagementButton.Click += EmployeeManagement_Click;

                PayrollButton.Content = "Training Records";
                PayrollButton.Click += TrainingRecords_Click;

                DepartmentsButton.Content = "Departments";
                DepartmentsButton.Click += Departments_Click;
            }
        }

        // Navigation methods
        private void LeaveManagement_Click(object sender, RoutedEventArgs e)
        {
            var dashboardWindow = Window.GetWindow(this) as DashboardWindow;
            if (dashboardWindow != null)
            {
                dashboardWindow.NavigateToLeaveManagement();
            }
        }

        private void Payroll_Click(object sender, RoutedEventArgs e)
        {
            var dashboardWindow = Window.GetWindow(this) as DashboardWindow;
            if (dashboardWindow != null)
            {
                dashboardWindow.NavigateToPayroll();
            }
        }

        private void Departments_Click(object sender, RoutedEventArgs e)
        {
            var dashboardWindow = Window.GetWindow(this) as DashboardWindow;
            if (dashboardWindow != null)
            {
                dashboardWindow.NavigateToDepartments();
            }
        }

        private void MyLeave_Click(object sender, RoutedEventArgs e)
        {
            var dashboardWindow = Window.GetWindow(this) as DashboardWindow;
            if (dashboardWindow != null)
            {
                dashboardWindow.NavigateToLeaveManagement();
            }
        }

        private void MyPay_Click(object sender, RoutedEventArgs e)
        {
            var dashboardWindow = Window.GetWindow(this) as DashboardWindow;
            if (dashboardWindow != null)
            {
                dashboardWindow.NavigateToMyPay();
            }
        }

        private void MyTraining_Click(object sender, RoutedEventArgs e)
        {
            var dashboardWindow = Window.GetWindow(this) as DashboardWindow;
            if (dashboardWindow != null)
            {
                dashboardWindow.NavigateToMyTraining();
            }
        }

        private void EmployeeManagement_Click(object sender, RoutedEventArgs e)
        {
            var dashboardWindow = Window.GetWindow(this) as DashboardWindow;
            if (dashboardWindow != null)
            {
                dashboardWindow.NavigateToEmployeeManagement();
            }
        }

        private void TrainingRecords_Click(object sender, RoutedEventArgs e)
        {
            var dashboardWindow = Window.GetWindow(this) as DashboardWindow;
            if (dashboardWindow != null)
            {
                dashboardWindow.NavigateToEmployeeManagement();
            }
        }

        // Below tries multiple paths to load building image
        private void LoadBuildingImage()
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var projectRoot = Directory.GetParent(baseDir)?.Parent?.Parent?.Parent?.FullName;

                string[] possiblePaths = new string[]
                {
                    projectRoot != null ? Path.Combine(projectRoot, "Images", "building.png") : null,
                    Path.Combine(baseDir, "Images", "building.png"),
                    Path.Combine(Directory.GetParent(baseDir)?.FullName ?? baseDir, "Images", "building.png"),
                    Path.Combine(baseDir, "building.png"),
                    projectRoot != null ? Path.Combine(projectRoot, "NZFTC_EmployeeSystem", "Images", "building.png") : null
                };

                foreach (var path in possiblePaths)
                {
                    if (path == null)
                        continue;

                    if (File.Exists(path))
                    {
                        try
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.UriSource = new Uri(path, UriKind.Absolute);
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();

                            BuildingImage.Source = bitmap;
                            ImagePlaceholder.Visibility = Visibility.Collapsed;
                            return;
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }

                ShowImagePlaceholder();
            }
            catch (Exception ex)
            {
                ShowImagePlaceholder();
                System.Diagnostics.Debug.WriteLine($"Error loading building image: {ex.Message}");
            }
        }

        private void ShowImagePlaceholder()
        {
            ImagePlaceholder.Visibility = Visibility.Visible;
            BuildingImage.Source = null;
        }

        // Below loads statistics from database
        private void LoadSummary()
        {
            TotalEmployeesText.Text = _dbContext.Employees.Count().ToString();
            PendingLeaveText.Text = _dbContext.LeaveRequests.Count(l => l.Status == "Pending").ToString();
            OpenGrievanceText.Text = _dbContext.Grievances.Count(g => g.Status == "Open").ToString();
        }

        // Below closes database connection when page unloads
        private void DashboardHomePage_Unloaded(object sender, RoutedEventArgs e)
        {
            _dbContext?.Dispose();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            string helpMessage = "Dashboard Overview Help\n\n" +
                "This dashboard provides a quick overview of company statistics and shortcuts to common tasks.\n\n" +
                "Statistics Cards:\n" +
                "- Total Employees: Shows the current number of active employees\n" +
                "- Pending Leave Requests: Leave requests awaiting approval\n" +
                "- Open Grievances: Unresolved employee grievances\n\n" +
                "Quick Links:\n" +
                "- Use the buttons on the right to quickly navigate to frequently used pages\n" +
                "- Button labels change based on your role (Admin, Employee, or Trainer)\n\n" +
                "Company News:\n" +
                "- View important company announcements and updates";

            MessageBox.Show(
                helpMessage,
                "Dashboard Help",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
    }
}