using Microsoft.EntityFrameworkCore;
using NZFTC_EmployeeSystem.Data;
using NZFTC_EmployeeSystem.Models;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace NZFTC_EmployeeSystem.Views
{
    /// <summary>
    /// Employee Management Page with training records
    /// Allows admins to manage employees and their training
    /// </summary>
    public partial class EmployeeManagementPage : Page
    {
        // Store the current logged-in user
        private readonly User _currentUser;

        // Store the currently selected employee for training view
        private Employee? _selectedEmployee = null;

        public EmployeeManagementPage(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;

            // Security check: Only admins and Workplace Trainers can access this page
            if (_currentUser.Role != "Admin" && _currentUser.Role != "Workplace Trainer")
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

            // Load initial data
            LoadEmployees();
            RoleComboBox.SelectedIndex = 0;
            LoadDepartments();

            // Hide Complete Training button if user is not Admin or Workplace Trainer
            if (_currentUser.Role != "Admin" && _currentUser.Role != "Workplace Trainer")
            {
                CompleteTrainingButton.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Loads all employees ordered by ID
        /// Includes Department navigation property for display
        /// </summary>
        private void LoadEmployees()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    // Load employees ordered by ID (ascending)
                    var employees = db.Employees
                        .Include(e => e.Department)
                        .OrderBy(e => e.Id) // Order by ID as requested
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
        /// Loads departments for the dropdown when creating employees
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
        /// Search employees by name as user types
        /// </summary>
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchTextBox.Text.ToLower().Trim();

            try
            {
                using (var db = new AppDbContext())
                {
                    var employees = db.Employees
                        .Include(e => e.Department)
                        .Where(e => e.FirstName.ToLower().Contains(searchText) ||
                                    e.LastName.ToLower().Contains(searchText) ||
                                    (e.FirstName + " " + e.LastName).ToLower().Contains(searchText))
                        .OrderBy(e => e.Id) // Keep ID ordering
                        .ToList();

                    EmployeesGrid.ItemsSource = employees;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error searching employees: {ex.Message}",
                    "Search Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// Export employee list to CSV file
        /// </summary>
        private void ExportEmployees_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var employees = db.Employees
                        .Include(e => e.Department)
                        .OrderBy(e => e.Id)
                        .ToList();

                    // Create CSV content
                    var csv = new StringBuilder();
                    csv.AppendLine("ID,First Name,Last Name,Email,Phone,Job Title,Department,Hire Date,Salary,Active");

                    foreach (var emp in employees)
                    {
                        csv.AppendLine($"{emp.Id}," +
                                      $"\"{emp.FirstName}\"," +
                                      $"\"{emp.LastName}\"," +
                                      $"\"{emp.Email}\"," +
                                      $"\"{emp.PhoneNumber ?? ""}\"," +
                                      $"\"{emp.JobTitle ?? ""}\"," +
                                      $"\"{emp.Department?.Name ?? ""}\"," +
                                      $"{emp.HireDate:dd/MM/yyyy}," +
                                      $"{emp.Salary}," +
                                      $"{emp.IsActive}");
                    }

                    // Save to Downloads folder
                    string downloadsPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        "Downloads");
                    string fileName = $"Employees_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                    string fullPath = Path.Combine(downloadsPath, fileName);

                    File.WriteAllText(fullPath, csv.ToString());

                    MessageBox.Show(
                        $"Employee list exported successfully!\n\nSaved to: {fullPath}",
                        "Export Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error exporting employees: {ex.Message}",
                    "Export Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// Toggle employee active status
        /// </summary>
        private void ToggleActive_Click(object sender, RoutedEventArgs e)
        {
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
                using (var db = new AppDbContext())
                {
                    var employee = db.Employees.Find(selected.Id);
                    if (employee != null)
                    {
                        employee.IsActive = !employee.IsActive;
                        db.SaveChanges();
                    }
                }

                MessageBox.Show(
                    $"Employee {selected.FullName} status changed to {(selected.IsActive ? "Inactive" : "Active")}.",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                LoadEmployees();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error toggling employee status: {ex.Message}",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// View training records for selected employee
        /// </summary>
        private void ViewTraining_Click(object sender, RoutedEventArgs e)
        {
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

            _selectedEmployee = selected;
            SelectedEmployeeText.Text = $"Training Records for: {selected.FullName} (ID: {selected.Id})";
            LoadTrainingRecords(selected.Id);

            // Switch to training tab (index 1)
            var tabControl = this.FindName("TrainingTab") as TabControl;
        }

        /// <summary>
        /// Load training records for an employee
        /// </summary>
        private void LoadTrainingRecords(int employeeId)
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var trainings = db.Trainings
                        .Include(t => t.SignedOffByUser)
                        .Where(t => t.EmployeeId == employeeId)
                        .OrderBy(t => t.Id)
                        .ToList();

                    TrainingGrid.ItemsSource = trainings;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error loading training records: {ex.Message}",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// Show add training panel
        /// </summary>
        private void AddTraining_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedEmployee == null)
            {
                MessageBox.Show(
                    "Please select an employee first by clicking 'View Training'.",
                    "No Employee Selected",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                return;
            }

            AddTrainingPanel.Visibility = Visibility.Visible;
            TrainingTypeComboBox.SelectedIndex = 0;
            TrainingNotesTextBox.Clear();
        }

        /// <summary>
        /// Cancel adding training
        /// </summary>
        private void CancelAddTraining_Click(object sender, RoutedEventArgs e)
        {
            AddTrainingPanel.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Save new training record
        /// </summary>
        private void SaveTraining_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedEmployee == null)
            {
                MessageBox.Show(
                    "No employee selected.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return;
            }

            if (TrainingTypeComboBox.SelectedItem == null)
            {
                MessageBox.Show(
                    "Please select a training type.",
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
                    string trainingType = ((ComboBoxItem)TrainingTypeComboBox.SelectedItem).Content.ToString() ?? "";

                    var training = new Training
                    {
                        EmployeeId = _selectedEmployee.Id,
                        TrainingType = trainingType,
                        Status = "Not Started",
                        Notes = TrainingNotesTextBox.Text.Trim()
                    };

                    db.Trainings.Add(training);
                    db.SaveChanges();
                }

                MessageBox.Show(
                    "Training record added successfully!",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                AddTrainingPanel.Visibility = Visibility.Collapsed;
                LoadTrainingRecords(_selectedEmployee.Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error adding training: {ex.Message}",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// Mark selected training as completed
        /// ONLY ADMIN AND WORKPLACE TRAINER CAN SIGN OFF
        /// </summary>
        private void CompleteTraining_Click(object sender, RoutedEventArgs e)
        {
            // SECURITY CHECK: Only Admin and Workplace Trainer can sign off training
            if (_currentUser.Role != "Admin" && _currentUser.Role != "Workplace Trainer")
            {
                MessageBox.Show(
                    "Only Admin and Workplace Trainer accounts can sign off training.\n\n" +
                    $"Your current role: {_currentUser.Role}",
                    "Access Denied",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            var selected = TrainingGrid.SelectedItem as Training;
            if (selected == null)
            {
                MessageBox.Show(
                    "Please select a training record from the list.",
                    "No Selection",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                return;
            }

            // Check if already completed
            if (selected.Status == "Completed")
            {
                MessageBox.Show(
                    "This training is already marked as completed.",
                    "Already Completed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                return;
            }

            try
            {
                using (var db = new AppDbContext())
                {
                    var training = db.Trainings.Find(selected.Id);
                    if (training != null)
                    {
                        training.Status = "Completed";
                        training.CompletedDate = DateTime.Now;
                        training.SignedOffByUserId = _currentUser.Id;
                        db.SaveChanges();
                    }
                }

                MessageBox.Show(
                    $"Training marked as completed and signed off by {_currentUser.Username}!",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                if (_selectedEmployee != null)
                {
                    LoadTrainingRecords(_selectedEmployee.Id);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error completing training: {ex.Message}",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// Export training records to CSV
        /// </summary>
        private void ExportTraining_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedEmployee == null)
            {
                MessageBox.Show(
                    "Please select an employee first by clicking 'View Training'.",
                    "No Employee Selected",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                return;
            }

            try
            {
                using (var db = new AppDbContext())
                {
                    var trainings = db.Trainings
                        .Include(t => t.SignedOffByUser)
                        .Where(t => t.EmployeeId == _selectedEmployee.Id)
                        .OrderBy(t => t.Id)
                        .ToList();

                    // Create CSV content
                    var csv = new StringBuilder();
                    csv.AppendLine($"Training Records for: {_selectedEmployee.FullName} (ID: {_selectedEmployee.Id})");
                    csv.AppendLine($"Generated: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                    csv.AppendLine("");
                    csv.AppendLine("ID,Training Type,Status,Completed Date,Signed Off By,Notes");

                    foreach (var t in trainings)
                    {
                        csv.AppendLine($"{t.Id}," +
                                      $"\"{t.TrainingType}\"," +
                                      $"\"{t.Status}\"," +
                                      $"{(t.CompletedDate.HasValue ? t.CompletedDate.Value.ToString("dd/MM/yyyy") : "")}," +
                                      $"\"{t.SignedOffByUser?.Username ?? ""}\"," +
                                      $"\"{t.Notes ?? ""}\"");
                    }

                    // Save to Downloads folder
                    string downloadsPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        "Downloads");
                    string fileName = $"Training_{_selectedEmployee.FirstName}_{_selectedEmployee.LastName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                    string fullPath = Path.Combine(downloadsPath, fileName);

                    File.WriteAllText(fullPath, csv.ToString());

                    MessageBox.Show(
                        $"Training records exported successfully!\n\nSaved to: {fullPath}",
                        "Export Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error exporting training: {ex.Message}",
                    "Export Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// Create new employee (existing code - kept the same)
        /// </summary>
        private void CreateEmployee_Click(object sender, RoutedEventArgs e)
        {
            // Validate required fields
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

            // Validate salary
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

            // Validate tax rate
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

            // Validate department
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

            // Validate email format
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
                var hireDate = HireDatePicker.SelectedDate ?? DateTime.Now;

                using (var db = new AppDbContext())
                {
                    // Check username exists
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

                    // Check email exists
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

                    int departmentId = (int)DepartmentComboBox.SelectedValue;

                    // Create employee
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
                        AnnualLeaveBalance = 20,
                        SickLeaveBalance = 10,
                        IsActive = true
                    };

                    db.Employees.Add(employee);
                    db.SaveChanges();

                    string selectedRoleName = ((ComboBoxItem)RoleComboBox.SelectedItem)?.Content?.ToString() ?? "Employee";

                    // Create user account
                    var user = new User
                    {
                        Username = UsernameTextBox.Text.Trim(),
                        Password = PasswordBox.Password,
                        Role = selectedRoleName,
                        EmployeeId = employee.Id,
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    };

                    db.Users.Add(user);
                    db.SaveChanges();

                    // Link to role
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

                MessageBox.Show(
                    $"Employee {FirstNameTextBox.Text} {LastNameTextBox.Text} created successfully!\n\n" +
                    $"Username: {UsernameTextBox.Text}\n" +
                    $"Password: {PasswordBox.Password}",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                // Clear form
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