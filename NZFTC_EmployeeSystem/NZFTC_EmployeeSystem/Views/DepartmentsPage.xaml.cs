using NZFTC_EmployeeSystem.Data;
using NZFTC_EmployeeSystem.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace NZFTC_EmployeeSystem.Views
{
    /// <summary>
    /// Interaction logic for DepartmentsPage.xaml
    /// This page allows administrators to manage departments within the organization.
    /// Admins can view all departments, add new ones, and delete departments that
    /// have no employees assigned to them.
    /// </summary>
    public partial class DepartmentsPage : Page
    {
        // Store the current logged-in user
        private readonly User _currentUser;

        /// <summary>
        /// Constructor - runs when the page is first created
        /// </summary>
        /// <param name="currentUser">The logged-in user accessing this page</param>
        public DepartmentsPage(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;

            // Security check: Only admins should access this page
            if (_currentUser.Role != "Admin")
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

        /// <summary>
        /// Loads all departments from the database and displays them in the grid.
        /// This method connects to the database, retrieves all departments,
        /// sorts them alphabetically by name, and binds them to the DataGrid.
        /// </summary>
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

        /// <summary>
        /// Handles the Add Department button click.
        /// This method validates the department name, checks for duplicates,
        /// and saves the new department to the database.
        /// </summary>
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

            try
            {
                // Step 4: Check for duplicates and add the department
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

                    // Step 5: Create a new Department object
                    var department = new Department
                    {
                        Name = deptName
                    };

                    // Step 6: Add the department to the database
                    db.Departments.Add(department);

                    // Step 7: Save changes to the database (this actually writes to the database file)
                    db.SaveChanges();
                }

                // Step 8: Show success message
                MessageBox.Show(
                    $"Department '{deptName}' added successfully!",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                // Step 9: Clear the text box so admin can add another department
                DepartmentNameTextBox.Clear();

                // Step 10: Reload the departments grid to show the newly added department
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

        /// <summary>
        /// Handles the Delete button click on a department row.
        /// This method confirms deletion with the user and ensures there are no
        /// employees assigned to the department before allowing deletion.
        /// </summary>
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

                            MessageBox.Show(
                                $"Cannot delete this department because it has {employeeCount} employee(s) assigned to it.\n\n" +
                                "Please reassign or remove the employees first before deleting the department.",
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

                    // Step 8: Reload the departments grid to reflect the deletion
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
    }
}