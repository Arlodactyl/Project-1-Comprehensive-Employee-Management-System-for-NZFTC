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
    /// Enables admins to view, activate/deactivate and create employees.
    /// </summary>
    public partial class EmployeeManagementPage : Page
    {
        private readonly User _currentUser;

        public EmployeeManagementPage(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            LoadEmployees();
            RoleComboBox.SelectedIndex = 0; // default to Employee role
        }

        // Loads employees into the data grid
        private void LoadEmployees()
        {
            using (var db = new AppDbContext())
            {
                var employees = db.Employees
                    .OrderBy(e => e.LastName)
                    .ToList();
                EmployeesGrid.ItemsSource = employees;
            }
        }

        // Toggles the selected employee's active status
        private void ToggleActive_Click(object sender, RoutedEventArgs e)
        {
            var selected = EmployeesGrid.SelectedItem as Employee;
            if (selected == null)
            {
                MessageBox.Show("Please select an employee", "No Selection");
                return;
            }

            using (var db = new AppDbContext())
            {
                var employee = db.Employees.Find(selected.Id);
                if (employee != null)
                {
                    employee.IsActive = !employee.IsActive;
                    db.SaveChanges();

                    // Also update the associated user status
                    var user = db.Users.FirstOrDefault(u => u.EmployeeId == employee.Id);
                    if (user != null)
                    {
                        user.IsActive = employee.IsActive;
                        db.SaveChanges();
                    }
                }
            }

            MessageBox.Show("Employee status updated", "Success");
            LoadEmployees();
        }

        // Creates a new employee and corresponding user account
        private void CreateEmployee_Click(object sender, RoutedEventArgs e)
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text) ||
                string.IsNullOrWhiteSpace(LastNameTextBox.Text) ||
                string.IsNullOrWhiteSpace(EmailTextBox.Text) ||
                string.IsNullOrWhiteSpace(UsernameTextBox.Text) ||
                string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                MessageBox.Show("Please fill in required fields (First Name, Last Name, Email, Username, Password)", "Validation Error");
                return;
            }

            if (!decimal.TryParse(SalaryTextBox.Text, out decimal salary))
            {
                MessageBox.Show("Invalid salary", "Validation Error");
                return;
            }

            if (!decimal.TryParse(TaxRateTextBox.Text, out decimal taxRate))
            {
                MessageBox.Show("Invalid tax rate", "Validation Error");
                return;
            }

            var hireDate = HireDatePicker.SelectedDate ?? DateTime.Now;

            using (var db = new AppDbContext())
            {
                // Create or find the department
                string departmentName = DepartmentTextBox.Text.Trim();
                var department = db.Departments.FirstOrDefault(d => d.Name == departmentName);
                if (department == null)
                {
                    department = new Department { Name = departmentName };
                    db.Departments.Add(department);
                    db.SaveChanges();
                }

                // Create employee record
                var employee = new Employee
                {
                    FirstName = FirstNameTextBox.Text.Trim(),
                    LastName = LastNameTextBox.Text.Trim(),
                    Email = EmailTextBox.Text.Trim(),
                    PhoneNumber = PhoneTextBox.Text.Trim(),
                    JobTitle = JobTitleTextBox.Text.Trim(),
                    DepartmentId = department.Id,
                    HireDate = hireDate,
                    Salary = salary,
                    TaxRate = taxRate,
                    AnnualLeaveBalance = 20,
                    SickLeaveBalance = 10,
                    IsActive = true
                };

                db.Employees.Add(employee);
                db.SaveChanges();

                // Determine selected role name
                string selectedRoleName = ((ComboBoxItem)RoleComboBox.SelectedItem)?.Content?.ToString() ?? "Employee";

                

                // Create linked user
                var user = new User
                {
                    Username = UsernameTextBox.Text.Trim(),
                    
                    Role = selectedRoleName,
                    EmployeeId = employee.Id,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                db.Users.Add(user);
                db.SaveChanges();

                // Link user to their role in UserRoles table
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

            MessageBox.Show("Employee created successfully!", "Success");

            // Reset form fields
            FirstNameTextBox.Clear();
            LastNameTextBox.Clear();
            EmailTextBox.Clear();
            PhoneTextBox.Clear();
            JobTitleTextBox.Clear();
            DepartmentTextBox.Clear();
            HireDatePicker.SelectedDate = null;
            SalaryTextBox.Clear();
            TaxRateTextBox.Clear();
            UsernameTextBox.Clear();
            PasswordBox.Clear();
            RoleComboBox.SelectedIndex = 0;

            LoadEmployees();
        }
    }
}
