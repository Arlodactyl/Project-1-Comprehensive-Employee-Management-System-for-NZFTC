using Microsoft.EntityFrameworkCore;
using NZFTC_EmployeeSystem.Data;
using NZFTC_EmployeeSystem.Models;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace NZFTC_EmployeeSystem.Views
{
    
    /// Interaction logic for DepartmentsPage.xaml
    /// This page allows administrators and trainers to manage departments within the organization.
    /// Admins/Trainers can view all departments, add new ones, delete departments that
    /// have no employees assigned to them, and view which employees are in each department.
    
    public partial class DepartmentsPage : Page
    {
        // Store the current logged-in user
        private readonly User _currentUser;

   
        /// <param name="currentUser">The logged-in user accessing this page</param>
        public DepartmentsPage(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;

            // Security check: Only admins and trainers should access this page
            if (_currentUser.Role != "Admin" && _currentUser.Role != "Trainer")
            {
                MessageBox.Show(
                    "You do not have permission to access Department Management.",
                    "Access Denied",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                // Try to navigate back to the previous page
                this.NavigationService?.GoBack();
                return;
            }

            // Load all departments from the database
            LoadDepartmentData();
        }

       
        private void LoadDepartmentData()
        {
            try
            {
                // 'using' ensures the database connection is properly closed after use
                using (var db = new AppDbContext())
                {
                    // Query the Departments table, order alphabetically by name
                    var departments = db.Departments
                        .OrderBy(d => d.Name)
                        .ToList();

                    // Bind the departments to the DataGrid so they appear on screen
                    DepartmentGrid.ItemsSource = departments;
                }
            }
            catch (Exception ex)
            {
                // If something goes wrong, show an error message
                MessageBox.Show(
                    $"Error loading departments: {ex.Message}\n\nPlease ensure the database exists.",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

       
        private void ViewEmployees_Click(object sender, RoutedEventArgs e)
        {
            // Step 1: Get the department ID from the button's Tag property
            if (sender is Button button && button.Tag is int deptId)
            {
                try
                {
                    using (var db = new AppDbContext())
                    {
                        // Step 2: Get the department name
                        var department = db.Departments.Find(deptId);
                        if (department == null)
                        {
                            MessageBox.Show(
                                "Department not found.",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error
                            );
                            return;
                        }

                        // Step 3: Load all employees in this department
                        var employees = db.Employees
                            .Where(e => e.DepartmentId == deptId)
                            .OrderBy(e => e.LastName)
                            .ToList();

                        // Step 4: Update the right side panel with employee information
                        DepartmentEmployeesTitle.Text = $"Employees in {department.Name} Department";
                        EmployeeCountText.Text = $"Total: {employees.Count} employee(s)";

                        // Step 5: Bind the employees to the grid
                        EmployeesGrid.ItemsSource = employees;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error loading employees: {ex.Message}",
                        "Database Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            }
        }

     
        /// Handles the Add Department button click.
        /// This method validates the department name, checks for duplicates,
        /// and saves the new department to the database.
     
        private void AddDepartment_Click(object sender, RoutedEventArgs e)
        {
            // Step 1: Get the department name from the text box and remove extra spaces
            var deptName = DepartmentNameTextBox.Text?.Trim();

            // Step 2: Validate that the department name is not empty
            if (string.IsNullOrWhiteSpace(deptName))
            {
                MessageBox.Show(
                    "Please enter a department name.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return; // Stop here if validation fails
            }

            // Step 3: Check if department name is too short
            if (deptName.Length < 2)
            {
                MessageBox.Show(
                    "Department name must be at least 2 characters long.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            // Step 4: Validate that the department name contains only letters and spaces
            // This regex pattern matches only letters (uppercase and lowercase) and spaces
            if (!Regex.IsMatch(deptName, @"^[a-zA-Z\s]+$"))
            {
                MessageBox.Show(
                    "Department name can only contain letters and spaces.\nSpecial characters and numbers are not allowed.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            try
            {
                // Step 5: Check for duplicates and add the department
                using (var db = new AppDbContext())
                {
                    // Check if a department with this name already exists (case insensitive)
                    // ToLower() makes the comparison case-insensitive
                    // Example: "IT" and "it" would be considered duplicates
                    if (db.Departments.Any(d => d.Name.ToLower() == deptName.ToLower()))
                    {
                        MessageBox.Show(
                            "A department with this name already exists.",
                            "Duplicate Department",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                        return; // Stop here if duplicate found
                    }

                    // Step 6: Create a new Department object
                    var department = new Department
                    {
                        Name = deptName
                    };

                    // Step 7: Add the department to the database
                    db.Departments.Add(department);

                    // Step 8: Save changes to the database (this actually writes to the database file)
                    db.SaveChanges();
                }

                // Step 9: Show success message
                MessageBox.Show(
                    $"Department '{deptName}' added successfully!",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                // Step 10: Clear the text box so admin can add another department
                DepartmentNameTextBox.Clear();

                // Step 11: Reload the departments grid to show the newly added department
                LoadDepartmentData();
            }
            catch (Exception ex)
            {
                // If something goes wrong, show an error message
                MessageBox.Show(
                    $"Error adding department: {ex.Message}",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

     
        private void DeleteDepartment_Click(object sender, RoutedEventArgs e)
        {
            // Step 1: Get the department ID from the button's Tag property
            // The Tag property was set in the XAML: Tag="{Binding Id}"
            if (sender is Button button && button.Tag is int deptId)
            {
                try
                {
                    // Step 2: Ask the user to confirm deletion
                    var result = MessageBox.Show(
                        "Are you sure you want to delete this department?\n\nThis action cannot be undone.",
                        "Confirm Delete",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning
                    );

                    // If user clicked "No", stop here
                    if (result != MessageBoxResult.Yes)
                        return;

                    // Step 3: Check if department has employees and delete if safe
                    using (var db = new AppDbContext())
                    {
                        // Find the department in the database
                        var department = db.Departments.Find(deptId);

                        // Check if department exists
                        if (department == null)
                        {
                            MessageBox.Show(
                                "Department not found in the database.",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error
                            );
                            return;
                        }

                        // Step 4: Check if any employees are assigned to this department
                        // We cannot delete a department that has employees
                        bool hasEmployees = db.Employees.Any(e => e.DepartmentId == deptId);

                        if (hasEmployees)
                        {
                            // Count how many employees are in this department
                            int employeeCount = db.Employees.Count(e => e.DepartmentId == deptId);

                            // Get the names of employees in this department
                            var employeeNames = db.Employees
                                .Where(e => e.DepartmentId == deptId)
                                .Select(e => e.FullName)
                                .Take(5) // Show up to 5 names
                                .ToList();

                            string employeeList = string.Join(", ", employeeNames);
                            if (employeeCount > 5)
                            {
                                employeeList += $", and {employeeCount - 5} more...";
                            }

                            MessageBox.Show(
                                $"Cannot delete '{department.Name}' department because it has {employeeCount} employee(s) assigned:\n\n" +
                                $"{employeeList}\n\n" +
                                "Please reassign or remove these employees first before deleting the department.",
                                "Delete Not Allowed",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning
                            );
                            return; // Stop here - don't delete
                        }

                        // Step 5: Safe to delete - remove the department
                        db.Departments.Remove(department);

                        // Step 6: Save changes to the database
                        db.SaveChanges();
                    }

                    // Step 7: Show success message
                    MessageBox.Show(
                        "Department deleted successfully.",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );

                    // Step 8: Clear the employee list on the right side
                    DepartmentEmployeesTitle.Text = "Select a department to view employees";
                    EmployeeCountText.Text = "";
                    EmployeesGrid.ItemsSource = null;

                    // Step 9: Reload the departments grid to reflect the deletion
                    LoadDepartmentData();
                }
                catch (Exception ex)
                {
                    // If something goes wrong, show an error message
                    MessageBox.Show(
                        $"Error deleting department: {ex.Message}",
                        "Database Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            }
        }

      
        /// Shows help information for using the departments page
     
        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            string helpMessage = "Departments Page Help\n\n" +
                "View Departments:\n" +
                "- See all company departments listed on the left\n" +
                "- Click 'View' to see employees in that department\n" +
                "- Click 'Delete' to remove empty departments\n\n" +
                "Add Department:\n" +
                "- Enter a department name (letters and spaces only)\n" +
                "- Name must be at least 2 characters\n" +
                "- Cannot use numbers or special characters like @, #, etc.\n" +
                "- Click 'Add Department' to save\n\n" +
                "Delete Department:\n" +
                "- Can only delete departments with no employees\n" +
                "- Reassign employees first before deleting\n" +
                "- You will be asked to confirm deletion";

            MessageBox.Show(
                helpMessage,
                "Departments Help",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
    }
}