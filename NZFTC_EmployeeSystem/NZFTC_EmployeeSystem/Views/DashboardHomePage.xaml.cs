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
    /// <summary>
    /// Dashboard home page - displays company overview statistics
    /// Shows total employees, pending requests, and open grievances
    /// This is the first page users see when they log into the system
    /// </summary>
    public partial class DashboardHomePage : Page
    {
        // Database context for querying employee data
        private readonly AppDbContext _dbContext;

        // Current logged-in user
        private readonly User _currentUser;

        /// <summary>
        /// Constructor - runs when the dashboard page is first loaded
        /// Accepts the current user as a parameter to personalize the dashboard
        /// </summary>
        /// <param name="currentUser">The user who is currently logged in</param>
        public DashboardHomePage(User currentUser)
        {
            // Initialize the XAML components (loads the visual design)
            InitializeComponent();

            // Store the current user for later use
            _currentUser = currentUser;

            // Create database connection
            _dbContext = new AppDbContext();

            // Load the building image with multiple fallback attempts
            LoadBuildingImage();

            // Load all the statistics from the database
            LoadSummary();

            // Customize quick links based on user role
            CustomizeQuickLinks();

            // Subscribe to the Unloaded event to clean up resources
            // This ensures the database connection is closed when the page is removed
            this.Unloaded += DashboardHomePage_Unloaded;
        }

        /// <summary>
        /// Customizes quick links based on the user's role
        /// Admin sees Leave Management, Payroll, Departments
        /// Employees see My Leave, My Pay, My Training
        /// </summary>
        private void CustomizeQuickLinks()
        {
            // Admin role - show admin-specific buttons
            if (_currentUser.Role == "Admin")
            {
                LeaveManagementButton.Content = "Leave Management";
                LeaveManagementButton.Click += LeaveManagement_Click;

                PayrollButton.Content = "Payroll";
                PayrollButton.Click += Payroll_Click;

                DepartmentsButton.Content = "Departments";
                DepartmentsButton.Click += Departments_Click;
            }
            // Employee role - show employee-specific buttons
            else if (_currentUser.Role == "Employee")
            {
                LeaveManagementButton.Content = "My Leave";
                LeaveManagementButton.Click += MyLeave_Click;

                PayrollButton.Content = "My Pay";
                PayrollButton.Click += MyPay_Click;

                DepartmentsButton.Content = "My Training";
                DepartmentsButton.Click += MyTraining_Click;
            }
            // Workplace Trainer role - show trainer-specific buttons
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

        /// <summary>
        /// Navigates to Leave Management page for Admin
        /// </summary>
        private void LeaveManagement_Click(object sender, RoutedEventArgs e)
        {
            var dashboardWindow = Window.GetWindow(this) as DashboardWindow;
            if (dashboardWindow != null)
            {
                dashboardWindow.NavigateToLeaveManagement();
            }
        }

        /// <summary>
        /// Navigates to Payroll page for Admin
        /// </summary>
        private void Payroll_Click(object sender, RoutedEventArgs e)
        {
            var dashboardWindow = Window.GetWindow(this) as DashboardWindow;
            if (dashboardWindow != null)
            {
                dashboardWindow.NavigateToPayroll();
            }
        }

        /// <summary>
        /// Navigates to Departments page for Admin and Workplace Trainer
        /// </summary>
        private void Departments_Click(object sender, RoutedEventArgs e)
        {
            var dashboardWindow = Window.GetWindow(this) as DashboardWindow;
            if (dashboardWindow != null)
            {
                dashboardWindow.NavigateToDepartments();
            }
        }

        /// <summary>
        /// Navigates to My Leave page for Employee
        /// </summary>
        private void MyLeave_Click(object sender, RoutedEventArgs e)
        {
            var dashboardWindow = Window.GetWindow(this) as DashboardWindow;
            if (dashboardWindow != null)
            {
                dashboardWindow.NavigateToLeaveManagement();
            }
        }

        /// <summary>
        /// Navigates to My Pay page for Employee
        /// </summary>
        private void MyPay_Click(object sender, RoutedEventArgs e)
        {
            var dashboardWindow = Window.GetWindow(this) as DashboardWindow;
            if (dashboardWindow != null)
            {
                dashboardWindow.NavigateToMyPay();
            }
        }

        /// <summary>
        /// Navigates to My Training page for Employee
        /// </summary>
        private void MyTraining_Click(object sender, RoutedEventArgs e)
        {
            var dashboardWindow = Window.GetWindow(this) as DashboardWindow;
            if (dashboardWindow != null)
            {
                dashboardWindow.NavigateToMyTraining();
            }
        }

        /// <summary>
        /// Navigates to Employee Management page for Workplace Trainer
        /// </summary>
        private void EmployeeManagement_Click(object sender, RoutedEventArgs e)
        {
            var dashboardWindow = Window.GetWindow(this) as DashboardWindow;
            if (dashboardWindow != null)
            {
                dashboardWindow.NavigateToEmployeeManagement();
            }
        }

        /// <summary>
        /// Navigates to Training Records page for Workplace Trainer
        /// </summary>
        private void TrainingRecords_Click(object sender, RoutedEventArgs e)
        {
            var dashboardWindow = Window.GetWindow(this) as DashboardWindow;
            if (dashboardWindow != null)
            {
                dashboardWindow.NavigateToEmployeeManagement();
            }
        }

        /// <summary>
        /// Attempts to load the building image from multiple possible locations
        /// Tries various path formats until one succeeds
        /// Shows placeholder text if all attempts fail
        /// </summary>
        private void LoadBuildingImage()
        {
            try
            {
                // Get the application base directory
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;

                // Calculate the project root directory
                // Typically goes up from bin/Debug/net8.0 to project root
                var projectRoot = Directory.GetParent(baseDir)?.Parent?.Parent?.Parent?.FullName;

                // List of possible image paths to try, in order of preference
                string[] possiblePaths = new string[]
                {
                    // Path 1: Project root Images folder (most common)
                    projectRoot != null ? Path.Combine(projectRoot, "Images", "building.png") : null,
                    
                    // Path 2: Relative to base directory
                    Path.Combine(baseDir, "Images", "building.png"),
                    
                    // Path 3: One level up from base directory
                    Path.Combine(Directory.GetParent(baseDir)?.FullName ?? baseDir, "Images", "building.png"),
                    
                    // Path 4: In the base directory directly
                    Path.Combine(baseDir, "building.png"),
                    
                    // Path 5: NZFTC_EmployeeSystem folder explicitly
                    projectRoot != null ? Path.Combine(projectRoot, "NZFTC_EmployeeSystem", "Images", "building.png") : null
                };

                // Try each path until we find one that exists
                foreach (var path in possiblePaths)
                {
                    // Skip null paths (can happen if projectRoot is null)
                    if (path == null)
                        continue;

                    // Check if the file exists at this path
                    if (File.Exists(path))
                    {
                        try
                        {
                            // Create a new bitmap image
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();

                            // Set the image source to the file path
                            bitmap.UriSource = new Uri(path, UriKind.Absolute);

                            // Cache on load to avoid file locking issues
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();

                            // Set the image source
                            BuildingImage.Source = bitmap;

                            // Hide the placeholder text since image loaded successfully
                            ImagePlaceholder.Visibility = Visibility.Collapsed;

                            // Success - exit the method
                            return;
                        }
                        catch
                        {
                            // If loading this path failed, continue to next path
                            continue;
                        }
                    }
                }

                // If we get here, no paths worked - show placeholder
                ShowImagePlaceholder();
            }
            catch (Exception ex)
            {
                // If any error occurs during the whole process, show placeholder
                ShowImagePlaceholder();

                // Optionally log the error for debugging
                System.Diagnostics.Debug.WriteLine($"Error loading building image: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows the placeholder text when the image cannot be loaded
        /// </summary>
        private void ShowImagePlaceholder()
        {
            // Make the placeholder text visible
            ImagePlaceholder.Visibility = Visibility.Visible;

            // Clear the image source
            BuildingImage.Source = null;
        }

        /// <summary>
        /// Loads summary statistics from the database and displays them on the dashboard
        /// This method queries different tables to count records and updates the UI
        /// </summary>
        private void LoadSummary()
        {
            // Count total number of employees in the system
            // Uses LINQ to query the Employees table and count all records
            TotalEmployeesText.Text = _dbContext.Employees.Count().ToString();

            // Count leave requests that are still pending approval
            // Filters LeaveRequests table where Status equals "Pending"
            PendingLeaveText.Text = _dbContext.LeaveRequests
                .Count(l => l.Status == "Pending")
                .ToString();

            // Count grievances that are currently open (not resolved)
            // Filters Grievances table where Status equals "Open"
            OpenGrievanceText.Text = _dbContext.Grievances
                .Count(g => g.Status == "Open")
                .ToString();
        }

        /// <summary>
        /// Event handler for when the page is unloaded from memory
        /// Ensures database connection is properly closed to prevent memory leaks
        /// </summary>
        private void DashboardHomePage_Unloaded(object sender, RoutedEventArgs e)
        {
            // Dispose the database context to free up resources
            // The question mark checks if _dbContext is not null before calling Dispose
            _dbContext?.Dispose();
        }

        /// <summary>
        /// Shows help information for using the dashboard
        /// </summary>
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