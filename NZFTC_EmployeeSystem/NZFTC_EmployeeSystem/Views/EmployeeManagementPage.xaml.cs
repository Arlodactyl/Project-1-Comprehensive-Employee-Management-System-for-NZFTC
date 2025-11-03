using Microsoft.EntityFrameworkCore;
using NZFTC_EmployeeSystem.Data;
using NZFTC_EmployeeSystem.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace NZFTC_EmployeeSystem.Views
{
    /// <summary>
    /// Interaction logic for EmployeeManagementPage.xaml
    /// This page allows administrators to view all employees, create new employees,
    /// and toggle employee active status. Each employee is linked to a department
    /// and has a corresponding user account for login.
    /// </summary>
    public partial class EmployeeManagementPage : Page
    {
        // Store the current logged-in user
        private readonly User _currentUser;

        /// <summary>
        /// Constructor - runs when the page is first created
        /// </summary>
        public EmployeeManagementPage(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;

            // Security check: Only admins should access this page
            if (_currentUser.Role != "Admin")
            {
                MessageBox.Show(
                    "You do not have permission to access Employee Management.",
                    "Access Denied",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                this.NavigationService?.GoBack();
                return;
            }

            // Load all employees from the database
            LoadEmployees();

            // Set default role to "Employee" when creating new employees
            RoleComboBox.SelectedIndex = 0;

            // Load departments into the department dropdown
            LoadDepartments();
        }

        /// <summary>
        /// Loads all employees from the database and displays them in the grid.
        /// IMPORTANT: Uses Include() to load the Department navigation property
        /// so we can see which department each employee belongs to.
        /// </summary>
        private void LoadEmployees()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    // FIXED: Added Include(e => e.Department) to load department data
                    // This is called "eager loading" - it loads related data at the same time
                    var employees = db.Employees
                        .Include(e => e.Department) // This loads the Department for each Employee
                        .OrderBy(e => e.LastName)
                        .ToList();

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

        /// <summary>
        /// Loads all departments into a ComboBox for easy selection when creating employees.
        /// This replaces the text box with a dropdown for better user experience.
        /// </summary>
        private void LoadDepartments()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var departments = db.Departments
                        .OrderBy(d => d.Name)
                        .ToList();

                    // Store departments for later use
                    DepartmentComboBox.ItemsSource = departments;
                    DepartmentComboBox.DisplayMemberPath = "Name";
                    DepartmentComboBox.SelectedValuePath = "Id";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error loading departments: {ex.Message}",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// Toggles the selected employee's active status between active and inactive.
        /// When an employee is deactivated, their corresponding user account is also
        /// deactivated, preventing them from logging in.
        /// </summary>
        private void ToggleActive_Click(object sender, RoutedEventArgs e)
        {
            // Step 1: Get the selected employee from the grid
            var selected = EmployeesGrid.SelectedItem as Employee;
            if (selected == null)
            {
                MessageBox.Show(
                    "Please select an employee from the list first.",
                    "No Selection",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                return;
            }

            try
            {
                // Step 2: Update the employee's active status in the database
                using (var db = new AppDbContext())
                {
                    var employee = db.Employees.Find(selected.Id);
                    if (employee != null)
                    {
                        // Toggle the status (true becomes false, false becomes true)
                        employee.IsActive = !employee.IsActive;
                        db.SaveChanges();

                        // Step 3: Also update the associated user account status
                        // This prevents inactive employees from logging in
                        var user = db.Users.FirstOrDefault(u => u.EmployeeId == employee.Id);
                        if (user != null)
                        {
                            user.IsActive = employee.IsActive;
                            db.SaveChanges();
                        }
                    }
                }

                // Step 4: Show success message with the new status
                string status = selected.IsActive ? "deactivated" : "activated";
                MessageBox.Show(
                    $"Employee {selected.FullName} has been {status}.",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                // Step 5: Reload the employee list to show the updated status
                LoadEmployees();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error updating employee status: {ex.Message}",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// Creates a new employee and corresponding user account.
        /// This method validates all inputs, creates or finds the department,
        /// creates the employee record, creates the user account, and links
        /// the user to their role in the UserRoles table.
        /// </summary>
        private void CreateEmployee_Click(object sender, RoutedEventArgs e)
        {
            // Step 1: Validate required fields
            if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text) ||
                string.IsNullOrWhiteSpace(LastNameTextBox.Text) ||
                string.IsNullOrWhiteSpace(EmailTextBox.Text) ||
                string.IsNullOrWhiteSpace(UsernameTextBox.Text) ||
                string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                MessageBox.Show(
                    "Please fill in all required fields:\n- First Name\n- Last Name\n- Email\n- Username\n- Password\n- Salary\n- Tax Rate",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            // Step 2: Validate salary is a valid number
            if (!decimal.TryParse(SalaryTextBox.Text, out decimal salary) || salary <= 0)
            {
                MessageBox.Show(
                    "Please enter a valid salary (must be a positive number).",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            // Step 3: Validate tax rate is a valid number
            if (!decimal.TryParse(TaxRateTextBox.Text, out decimal taxRate) || taxRate < 0 || taxRate > 100)
            {
                MessageBox.Show(
                    "Please enter a valid tax rate (between 0 and 100).",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            // Step 4: Validate department is selected
            if (DepartmentComboBox.SelectedValue == null)
            {
                MessageBox.Show(
                    "Please select a department for the employee.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            // Step 5: Validate email format (basic check)
            if (!EmailTextBox.Text.Contains("@"))
            {
                MessageBox.Show(
                    "Please enter a valid email address.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            try
            {
                // Step 6: Get hire date or use today's date if not selected
                var hireDate = HireDatePicker.SelectedDate ?? DateTime.Now;

                using (var db = new AppDbContext())
                {
                    // Step 7: Check if username already exists
                    if (db.Users.Any(u => u.Username.ToLower() == UsernameTextBox.Text.Trim().ToLower()))
                    {
                        MessageBox.Show(
                            "This username is already taken. Please choose a different username.",
                            "Duplicate Username",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                        return;
                    }

                    // Step 8: Check if email already exists
                    if (db.Employees.Any(e => e.Email.ToLower() == EmailTextBox.Text.Trim().ToLower()))
                    {
                        MessageBox.Show(
                            "This email is already registered. Please use a different email.",
                            "Duplicate Email",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                        return;
                    }

                    // Step 9: Get the selected department ID
                    int departmentId = (int)DepartmentComboBox.SelectedValue;

                    // Step 10: Create the employee record
                    var employee = new Employee
                    {
                        FirstName = FirstNameTextBox.Text.Trim(),
                        LastName = LastNameTextBox.Text.Trim(),
                        Email = EmailTextBox.Text.Trim(),
                        PhoneNumber = PhoneTextBox.Text.Trim(),
                        JobTitle = JobTitleTextBox.Text.Trim(),
                        DepartmentId = departmentId,
                        HireDate = hireDate,
                        Salary = salary,
                        TaxRate = taxRate,
                        AnnualLeaveBalance = 20, // Default 20 days
                        SickLeaveBalance = 10,   // Default 10 days
                        IsActive = true
                    };

                    db.Employees.Add(employee);
                    db.SaveChanges(); // Save to get the employee ID

                    // Step 11: Determine the selected role
                    string selectedRoleName = ((ComboBoxItem)RoleComboBox.SelectedItem)?.Content?.ToString() ?? "Employee";

                    // Step 12: Create the linked user account
                    var user = new User
                    {
                        Username = UsernameTextBox.Text.Trim(),
                        Password = PasswordBox.Password, // Note: In production, this should be hashed
                        Role = selectedRoleName,
                        EmployeeId = employee.Id,
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    };

                    db.Users.Add(user);
                    db.SaveChanges(); // Save to get the user ID

                    // Step 13: Link the user to their role in the UserRoles table
                    var roleEntity = db.Roles.FirstOrDefault(r => r.Name == selectedRoleName);
                    if (roleEntity != null)
                    {
                        var userRole = new UserRole
                        {
                            UserId = user.Id,
                            RoleId = roleEntity.Id
                        };
                        db.UserRoles.Add(userRole);
                        db.SaveChanges();
                    }
                }

                // Step 14: Show success message
                MessageBox.Show(
                    $"Employee {FirstNameTextBox.Text} {LastNameTextBox.Text} created successfully!\n\n" +
                    $"Username: {UsernameTextBox.Text}\n" +
                    $"Password: {PasswordBox.Password}",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                // Step 15: Clear the form so admin can create another employee
                FirstNameTextBox.Clear();
                LastNameTextBox.Clear();
                EmailTextBox.Clear();
                PhoneTextBox.Clear();
                JobTitleTextBox.Clear();
                DepartmentComboBox.SelectedIndex = -1;
                HireDatePicker.SelectedDate = null;
                SalaryTextBox.Clear();
                TaxRateTextBox.Clear();
                UsernameTextBox.Clear();
                PasswordBox.Clear();
                RoleComboBox.SelectedIndex = 0;

                // Step 16: Reload the employee list to show the new employee
                LoadEmployees();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error creating employee: {ex.Message}",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }
    }
}