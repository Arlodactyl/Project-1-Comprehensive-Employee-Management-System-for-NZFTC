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
        private readonly User _currentUser;


        /// <param name="currentUser">The logged-in user accessing this page</param>
        public DepartmentsPage(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;

            if (_currentUser.Role != "Admin" && _currentUser.Role != "Workplace Trainer")
            {
                MessageBox.Show(
                    "You do not have permission to access Department Management.",
                    "Access Denied",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                this.NavigationService?.GoBack();
                return;
            }

            LoadDepartmentData();
        }


        private void LoadDepartmentData()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var departments = db.Departments
                        .OrderBy(d => d.Name)
                        .ToList();

                    DepartmentGrid.ItemsSource = departments;
                }
            }
            catch (Exception ex)
            {
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
            if (sender is Button button && button.Tag is int deptId)
            {
                try
                {
                    using (var db = new AppDbContext())
                    {
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

                        var employees = db.Employees
                            .Where(e => e.DepartmentId == deptId)
                            .OrderBy(e => e.LastName)
                            .ToList();

                        DepartmentEmployeesTitle.Text = $"Employees in {department.Name} Department";
                        EmployeeCountText.Text = $"Total: {employees.Count} employee(s)";

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
            var deptName = DepartmentNameTextBox.Text?.Trim();

            if (string.IsNullOrWhiteSpace(deptName))
            {
                MessageBox.Show(
                    "Please enter a department name.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

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
                using (var db = new AppDbContext())
                {
                    if (db.Departments.Any(d => d.Name.ToLower() == deptName.ToLower()))
                    {
                        MessageBox.Show(
                            "A department with this name already exists.",
                            "Duplicate Department",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                        return;
                    }

                    var department = new Department
                    {
                        Name = deptName
                    };

                    db.Departments.Add(department);

                    db.SaveChanges();
                }

                MessageBox.Show(
                    $"Department '{deptName}' added successfully!",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                DepartmentNameTextBox.Clear();

                LoadDepartmentData();
            }
            catch (Exception ex)
            {
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
            if (sender is Button button && button.Tag is int deptId)
            {
                try
                {
                    var result = MessageBox.Show(
                        "Are you sure you want to delete this department?\n\nThis action cannot be undone.",
                        "Confirm Delete",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning
                    );

                    if (result != MessageBoxResult.Yes)
                        return;

                    using (var db = new AppDbContext())
                    {
                        var department = db.Departments.Find(deptId);

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

                        bool hasEmployees = db.Employees.Any(e => e.DepartmentId == deptId);

                        if (hasEmployees)
                        {
                            int employeeCount = db.Employees.Count(e => e.DepartmentId == deptId);

                            var employeeNames = db.Employees
                                .Where(e => e.DepartmentId == deptId)
                                .Select(e => e.FullName)
                                .Take(5)
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
                            return;
                        }

                        db.Departments.Remove(department);

                        db.SaveChanges();
                    }

                    MessageBox.Show(
                        "Department deleted successfully.",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );

                    DepartmentEmployeesTitle.Text = "Select a department to view employees";
                    EmployeeCountText.Text = "";
                    EmployeesGrid.ItemsSource = null;

                    LoadDepartmentData();
                }
                catch (Exception ex)
                {
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

        private void DepartmentNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
