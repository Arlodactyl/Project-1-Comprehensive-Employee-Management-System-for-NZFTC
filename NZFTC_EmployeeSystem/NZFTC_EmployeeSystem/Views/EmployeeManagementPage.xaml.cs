using Microsoft.EntityFrameworkCore;
using NZFTC_EmployeeSystem.Data;
using NZFTC_EmployeeSystem.Models;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NZFTC_EmployeeSystem.Views
{
    /// <summary>
    /// Employee Management Page with training records
    /// Allows admins to manage employees and their training
    /// Now includes View Details and Edit Employee functionality
    /// </summary>
    public partial class EmployeeManagementPage : Page
    {
        // Store the current logged-in user
        private readonly User _currentUser;

        // Store the currently selected employee for various actions
        private Employee _selectedEmployee = null;

        // Store the employee being edited
        private Employee _editingEmployee = null;

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
            LoadAllTrainingRecords();

            // Show Add Training panel by default and load employees
            LoadEmployeesForTraining();
            AddTrainingPanel.Visibility = Visibility.Visible;
            TrainingTypeComboBox.SelectedIndex = 0;

            // Hide Complete Training button if user is not Admin or Workplace Trainer
            if (_currentUser.Role != "Admin" && _currentUser.Role != "Workplace Trainer")
            {
                CompleteTrainingButton.Visibility = Visibility.Collapsed;
            }

            // Load departments for edit dropdown
            LoadDepartmentsForEdit();
        }

        /// <summary>
        /// Handles single-click on employee row
        /// Shows the action buttons (View Details, Edit, etc.)
        /// </summary>
        private void EmployeesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Get the selected employee from the grid
            _selectedEmployee = EmployeesGrid.SelectedItem as Employee;

            // If an employee is selected, show the action panel
            if (_selectedEmployee != null)
            {
                EmployeeActionPanel.Visibility = Visibility.Visible;
                SelectedEmployeeText.Text = $"Selected: {_selectedEmployee.FullName}";
            }
            else
            {
                // No employee selected, hide the action panel
                EmployeeActionPanel.Visibility = Visibility.Collapsed;
                SelectedEmployeeText.Text = "Selected: None";
            }
        }

        /// <summary>
        /// Handles double-click on employee row
        /// Quick shortcut to open edit tab
        /// </summary>
        private void EmployeesGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Check if an employee is selected
            if (_selectedEmployee != null)
            {
                // Double-click is a shortcut to edit
                LoadEmployeeForEditing(_selectedEmployee);
            }
        }

        /// <summary>
        /// View Details button click
        /// Shows a popup window with full employee details
        /// </summary>
        private void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedEmployee == null)
            {
                MessageBox.Show(
                    "Please select an employee first.",
                    "No Selection",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                return;
            }

            // Create a popup window to show employee details
            var detailsWindow = new Window
            {
                Title = $"Employee Details - {_selectedEmployee.FullName}",
                Width = 500,
                Height = 550,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };

            // Create scrollviewer for the content
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            // Create stack panel for details
            var stackPanel = new StackPanel
            {
                Margin = new Thickness(25)
            };

            // Add header
            var header = new TextBlock
            {
                Text = "Employee Information",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20)
            };
            stackPanel.Children.Add(header);

            // Helper method to add detail rows
            void AddDetailRow(string label, string value)
            {
                var labelBlock = new TextBlock
                {
                    Text = label,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 13,
                    Margin = new Thickness(0, 8, 0, 3)
                };
                stackPanel.Children.Add(labelBlock);

                var valueBlock = new TextBlock
                {
                    Text = value ?? "N/A",
                    FontSize = 13,
                    Margin = new Thickness(0, 0, 0, 5)
                };
                stackPanel.Children.Add(valueBlock);
            }

            // Add all employee details
            AddDetailRow("Employee ID:", _selectedEmployee.Id.ToString());
            AddDetailRow("First Name:", _selectedEmployee.FirstName);
            AddDetailRow("Last Name:", _selectedEmployee.LastName);
            AddDetailRow("Full Name:", _selectedEmployee.FullName);
            AddDetailRow("Email Address:", _selectedEmployee.Email);
            AddDetailRow("Phone Number:", _selectedEmployee.PhoneNumber);
            AddDetailRow("Job Title:", _selectedEmployee.JobTitle);
            AddDetailRow("Department:", _selectedEmployee.Department?.Name);
            AddDetailRow("Hire Date:", _selectedEmployee.HireDate.ToString("dd/MM/yyyy"));
            AddDetailRow("Salary:", $"${_selectedEmployee.Salary:N2}");
            AddDetailRow("Tax Rate:", $"{_selectedEmployee.TaxRate}%");
            AddDetailRow("Annual Leave Balance:", $"{_selectedEmployee.AnnualLeaveBalance} days");
            AddDetailRow("Sick Leave Balance:", $"{_selectedEmployee.SickLeaveBalance} days");
            AddDetailRow("Status:", _selectedEmployee.IsActive ? "Active" : "Inactive");

            // Add close button
            var closeButton = new Button
            {
                Content = "Close",
                Width = 100,
                Height = 35,
                Margin = new Thickness(0, 20, 0, 0),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 152, 219)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand
            };
            closeButton.Click += (s, args) => detailsWindow.Close();
            stackPanel.Children.Add(closeButton);

            // Set content and show window
            scrollViewer.Content = stackPanel;
            detailsWindow.Content = scrollViewer;
            detailsWindow.ShowDialog();
        }

        /// <summary>
        /// Edit Employee button click
        /// Loads employee data into edit tab
        /// </summary>
        private void EditEmployee_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedEmployee == null)
            {
                MessageBox.Show(
                    "Please select an employee first.",
                    "No Selection",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                return;
            }

            LoadEmployeeForEditing(_selectedEmployee);
        }

        /// <summary>
        /// Loads employee data into the edit form and switches to edit tab
        /// </summary>
        private void LoadEmployeeForEditing(Employee employee)
        {
            try
            {
                // Reload employee from database to get fresh data with relationships
                using (var db = new AppDbContext())
                {
                    _editingEmployee = db.Employees
                        .Include(e => e.Department)
                        .FirstOrDefault(e => e.Id == employee.Id);

                    if (_editingEmployee == null)
                    {
                        MessageBox.Show(
                            "Could not load employee data.",
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error
                        );
                        return;
                    }

                    // Fill in all the edit form fields
                    EditEmployeeNameText.Text = $"Editing: {_editingEmployee.FullName} (ID: {_editingEmployee.Id})";
                    EditFirstNameTextBox.Text = _editingEmployee.FirstName;
                    EditLastNameTextBox.Text = _editingEmployee.LastName;
                    EditEmailTextBox.Text = _editingEmployee.Email;
                    EditPhoneTextBox.Text = _editingEmployee.PhoneNumber ?? "";
                    EditJobTitleTextBox.Text = _editingEmployee.JobTitle ?? "";
                    EditHireDatePicker.SelectedDate = _editingEmployee.HireDate;
                    EditSalaryTextBox.Text = _editingEmployee.Salary.ToString();
                    EditTaxRateTextBox.Text = _editingEmployee.TaxRate.ToString();
                    EditAnnualLeaveTextBox.Text = _editingEmployee.AnnualLeaveBalance.ToString();
                    EditSickLeaveTextBox.Text = _editingEmployee.SickLeaveBalance.ToString();
                    EditIsActiveCheckBox.IsChecked = _editingEmployee.IsActive;

                    // Set department dropdown
                    if (_editingEmployee.Department != null)
                    {
                        EditDepartmentComboBox.SelectedValue = _editingEmployee.DepartmentId;
                    }

                    // Show the edit tab and switch to it
                    EditEmployeeTab.Visibility = Visibility.Visible;
                    MainTabControl.SelectedItem = EditEmployeeTab;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error loading employee for editing: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// Load departments for the edit form dropdown
        /// </summary>
        private void LoadDepartmentsForEdit()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var departments = db.Departments
                        .OrderBy(d => d.Name)
                        .ToList();

                    EditDepartmentComboBox.ItemsSource = departments;
                    EditDepartmentComboBox.DisplayMemberPath = "Name";
                    EditDepartmentComboBox.SelectedValuePath = "Id";
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
        /// Save Changes button click in edit tab
        /// Updates the employee in the database
        /// </summary>
        private void SaveEmployeeChanges_Click(object sender, RoutedEventArgs e)
        {
            if (_editingEmployee == null)
            {
                MessageBox.Show(
                    "No employee is currently being edited.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(EditFirstNameTextBox.Text) ||
                string.IsNullOrWhiteSpace(EditLastNameTextBox.Text) ||
                string.IsNullOrWhiteSpace(EditEmailTextBox.Text))
            {
                MessageBox.Show(
                    "Please fill in all required fields:\n- First Name\n- Last Name\n- Email",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            // Validate salary
            if (!decimal.TryParse(EditSalaryTextBox.Text, out decimal salary) || salary <= 0)
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
            if (!decimal.TryParse(EditTaxRateTextBox.Text, out decimal taxRate) || taxRate < 0 || taxRate > 100)
            {
                MessageBox.Show(
                    "Please enter a valid tax rate (between 0 and 100).",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            // Validate leave balances
            if (!int.TryParse(EditAnnualLeaveTextBox.Text, out int annualLeave) || annualLeave < 0)
            {
                MessageBox.Show(
                    "Please enter a valid annual leave balance (must be 0 or greater).",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            if (!int.TryParse(EditSickLeaveTextBox.Text, out int sickLeave) || sickLeave < 0)
            {
                MessageBox.Show(
                    "Please enter a valid sick leave balance (must be 0 or greater).",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            // Validate department selected
            if (EditDepartmentComboBox.SelectedValue == null)
            {
                MessageBox.Show(
                    "Please select a department.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            // Validate email format
            if (!EditEmailTextBox.Text.Contains("@"))
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
                using (var db = new AppDbContext())
                {
                    // Get the employee from database
                    var employee = db.Employees.Find(_editingEmployee.Id);
                    if (employee == null)
                    {
                        MessageBox.Show(
                            "Employee not found in database.",
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error
                        );
                        return;
                    }

                    // Check if email is already used by another employee
                    var emailExists = db.Employees.Any(e =>
                        e.Email.ToLower() == EditEmailTextBox.Text.Trim().ToLower() &&
                        e.Id != employee.Id);

                    if (emailExists)
                    {
                        MessageBox.Show(
                            "This email is already used by another employee.",
                            "Duplicate Email",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                        return;
                    }

                    // Update all employee fields
                    employee.FirstName = EditFirstNameTextBox.Text.Trim();
                    employee.LastName = EditLastNameTextBox.Text.Trim();
                    employee.Email = EditEmailTextBox.Text.Trim();
                    employee.PhoneNumber = EditPhoneTextBox.Text.Trim();
                    employee.JobTitle = EditJobTitleTextBox.Text.Trim();
                    employee.DepartmentId = (int)EditDepartmentComboBox.SelectedValue;
                    employee.HireDate = EditHireDatePicker.SelectedDate ?? employee.HireDate;
                    employee.Salary = salary;
                    employee.TaxRate = taxRate;
                    employee.AnnualLeaveBalance = annualLeave;
                    employee.SickLeaveBalance = sickLeave;
                    employee.IsActive = EditIsActiveCheckBox.IsChecked ?? true;

                    // Save changes to database
                    db.SaveChanges();
                }

                MessageBox.Show(
                    $"Employee {EditFirstNameTextBox.Text} {EditLastNameTextBox.Text} updated successfully!",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                // Reload employees list and return to employees tab
                LoadEmployees();
                EditEmployeeTab.Visibility = Visibility.Collapsed;
                MainTabControl.SelectedIndex = 0; // Go back to Employees tab
                _editingEmployee = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error saving employee changes: {ex.Message}",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// Cancel Edit button click
        /// Returns to employees tab without saving
        /// </summary>
        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            // Ask for confirmation before canceling
            var result = MessageBox.Show(
                "Are you sure you want to cancel? Any unsaved changes will be lost.",
                "Confirm Cancel",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                EditEmployeeTab.Visibility = Visibility.Collapsed;
                MainTabControl.SelectedIndex = 0; // Go back to Employees tab
                _editingEmployee = null;
            }
        }

        /// <summary>
        /// Double-click handler for training grid
        /// Quick way to view training details
        /// </summary>
        private void TrainingGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedTraining = TrainingGrid.SelectedItem as Training;
            if (selectedTraining == null) return;

            // Show training details popup
            var detailsMessage = $"Training Details\n\n" +
                               $"Employee: {selectedTraining.Employee?.FullName ?? "Unknown"}\n" +
                               $"Training Type: {selectedTraining.TrainingType}\n" +
                               $"Status: {selectedTraining.Status}\n" +
                               $"Completed Date: {(selectedTraining.CompletedDate.HasValue ? selectedTraining.CompletedDate.Value.ToString("dd/MM/yyyy") : "Not completed")}\n" +
                               $"Signed Off By: {selectedTraining.SignedOffByUser?.Username ?? "Not signed off"}\n" +
                               $"Notes: {selectedTraining.Notes ?? "None"}";

            MessageBox.Show(
                detailsMessage,
                "Training Record Details",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
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
                        .Include(emp => emp.Department)
                        .Where(emp => emp.FirstName.ToLower().Contains(searchText) ||
                                    emp.LastName.ToLower().Contains(searchText) ||
                                    (emp.FirstName + " " + emp.LastName).ToLower().Contains(searchText))
                        .OrderBy(emp => emp.Id) // Keep ID ordering
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
                        .Include(emp => emp.Department)
                        .OrderBy(emp => emp.Id)
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
            if (_selectedEmployee == null)
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
                    var employee = db.Employees.Find(_selectedEmployee.Id);
                    if (employee != null)
                    {
                        employee.IsActive = !employee.IsActive;
                        db.SaveChanges();
                    }
                }

                MessageBox.Show(
                    $"Employee {_selectedEmployee.FullName} status changed to {(!_selectedEmployee.IsActive ? "Inactive" : "Active")}.",
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
            if (_selectedEmployee == null)
            {
                MessageBox.Show(
                    "Please select an employee from the list first.",
                    "No Selection",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                return;
            }

            LoadTrainingRecords(_selectedEmployee.Id);
            MainTabControl.SelectedIndex = 2; // Switch to training records tab
        }

        /// <summary>
        /// Load all training records for all employees
        /// </summary>
        private void LoadAllTrainingRecords()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var trainings = db.Trainings
                        .Include(t => t.Employee)
                        .Include(t => t.SignedOffByUser)
                        .OrderByDescending(t => t.CompletedDate)
                        .ThenBy(t => t.Employee.FirstName)
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
        /// Load training records for a specific employee
        /// </summary>
        private void LoadTrainingRecords(int employeeId)
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var trainings = db.Trainings
                        .Include(t => t.Employee)
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
        /// Load employees for training dropdown
        /// </summary>
        private void LoadEmployeesForTraining()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var employees = db.Employees
                        .Where(emp => emp.IsActive)
                        .OrderBy(emp => emp.LastName)
                        .ThenBy(emp => emp.FirstName)
                        .ToList();

                    TrainingEmployeeComboBox.ItemsSource = employees;
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
        /// Show add training panel
        /// </summary>
        private void AddTraining_Click(object sender, RoutedEventArgs e)
        {
            AddTrainingPanel.Visibility = Visibility.Visible;
            TrainingEmployeeComboBox.SelectedIndex = -1;
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
            if (TrainingEmployeeComboBox.SelectedValue == null)
            {
                MessageBox.Show(
                    "Please select an employee.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
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
                    int employeeId = (int)TrainingEmployeeComboBox.SelectedValue;

                    var training = new Training
                    {
                        EmployeeId = employeeId,
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
                LoadAllTrainingRecords();
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

                LoadAllTrainingRecords();
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
            try
            {
                using (var db = new AppDbContext())
                {
                    var trainings = db.Trainings
                        .Include(t => t.Employee)
                        .Include(t => t.SignedOffByUser)
                        .OrderBy(t => t.Employee.LastName)
                        .ThenBy(t => t.Employee.FirstName)
                        .ThenBy(t => t.Id)
                        .ToList();

                    // Create CSV content
                    var csv = new StringBuilder();
                    csv.AppendLine("All Training Records");
                    csv.AppendLine($"Generated: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                    csv.AppendLine("");
                    csv.AppendLine("ID,Employee Name,Training Type,Status,Completed Date,Signed Off By,Notes");

                    foreach (var t in trainings)
                    {
                        csv.AppendLine($"{t.Id}," +
                                      $"\"{t.Employee?.FullName ?? "Unknown"}\"," +
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
                    string fileName = $"All_Training_Records_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
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
        /// Search training records by employee name or training type
        /// </summary>
        private void TrainingSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = TrainingSearchTextBox.Text.ToLower().Trim();

            try
            {
                using (var db = new AppDbContext())
                {
                    var trainings = db.Trainings
                        .Include(t => t.Employee)
                        .Include(t => t.SignedOffByUser)
                        .Where(t => t.Employee.FirstName.ToLower().Contains(searchText) ||
                                    t.Employee.LastName.ToLower().Contains(searchText) ||
                                    (t.Employee.FirstName + " " + t.Employee.LastName).ToLower().Contains(searchText) ||
                                    t.TrainingType.ToLower().Contains(searchText))
                        .OrderByDescending(t => t.CompletedDate)
                        .ThenBy(t => t.Employee.FirstName)
                        .ToList();

                    TrainingGrid.ItemsSource = trainings;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error searching training: {ex.Message}",
                    "Search Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// Refresh all training records
        /// </summary>
        private void RefreshTraining_Click(object sender, RoutedEventArgs e)
        {
            TrainingSearchTextBox.Clear();
            LoadAllTrainingRecords();
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