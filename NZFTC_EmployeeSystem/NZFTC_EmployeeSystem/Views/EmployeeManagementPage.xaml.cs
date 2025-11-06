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
    /// Allows admins and workplace trainers to manage employees and their training
    /// Now includes View Details, Edit Employee, and View Training functionality
    /// FIXED: Workplace trainers can now sign off training and have full training rights
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

            // Load employees for training dropdown
            LoadEmployeesForTraining();

            // Hide the add training panel initially
            AddTrainingPanel.Visibility = Visibility.Collapsed;
            TrainingTypeComboBox.SelectedIndex = 0;

            // Show Complete Training button for Admin and Workplace Trainer
            // FIXED: Workplace trainers now have full access to sign off training
            if (_currentUser.Role == "Admin" || _currentUser.Role == "Workplace Trainer")
            {
                CompleteTrainingButton.Visibility = Visibility.Visible;
            }
            else
            {
                CompleteTrainingButton.Visibility = Visibility.Collapsed;
            }

            // Load departments for edit dropdown
            LoadDepartmentsForEdit();
        }

        #region Employee Tab Action Buttons (NEW METHODS)

        /// <summary>
        /// NEW: View Details button click handler - Shows employee details popup
        /// </summary>
        private void ViewEmployeeDetails_Click(object sender, RoutedEventArgs e)
        {
            // Get the selected employee from the grid
            _selectedEmployee = EmployeesGrid.SelectedItem as Employee;

            // Check if an employee is selected
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

            // Show the employee details directly (no back action needed for button click)
            ShowEmployeeDetails(_selectedEmployee, null);
        }

        /// <summary>
        /// NEW: Edit Employee button click handler - Opens edit form
        /// </summary>
        private void EditEmployee_Click(object sender, RoutedEventArgs e)
        {
            // Get the selected employee from the grid
            _selectedEmployee = EmployeesGrid.SelectedItem as Employee;

            // Check if an employee is selected
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

            // Load the employee into the edit form and navigate to edit tab
            LoadEmployeeForEditing(_selectedEmployee);
        }

        /// <summary>
        /// NEW: View Training button click handler - Shows training records
        /// </summary>
        private void ViewEmployeeTraining_Click(object sender, RoutedEventArgs e)
        {
            // Get the selected employee from the grid
            _selectedEmployee = EmployeesGrid.SelectedItem as Employee;

            // Check if an employee is selected
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

            // Navigate to training records tab for this employee
            NavigateToTrainingRecords(_selectedEmployee);
        }

        #endregion

        /// <summary>
        /// Handles double-click on employee row
        /// Shows popup with options: View Details, Edit Employee, View Training
        /// </summary>
        private void EmployeesGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Get the selected employee from the grid
            _selectedEmployee = EmployeesGrid.SelectedItem as Employee;

            // Check if an employee is selected
            if (_selectedEmployee == null)
            {
                MessageBox.Show(
                    "Please select an employee from the list.",
                    "No Selection",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                return;
            }

            // Show the action selection popup
            ShowEmployeeActionPopup(_selectedEmployee);
        }

        /// <summary>
        /// Shows a popup window with action buttons for the selected employee
        /// </summary>
        private void ShowEmployeeActionPopup(Employee employee)
        {
            // Create a popup window with action buttons
            var actionWindow = new Window
            {
                Title = $"Employee Actions - {employee.FullName}",
                Width = 450,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };

            // Create stack panel for the buttons
            var stackPanel = new StackPanel
            {
                Margin = new Thickness(30),
                VerticalAlignment = VerticalAlignment.Center
            };

            // Add header text
            var headerText = new TextBlock
            {
                Text = $"What would you like to do with {employee.FullName}?",
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 25),
                TextAlignment = TextAlignment.Center
            };
            stackPanel.Children.Add(headerText);

            // Create View Details button
            var viewButton = new Button
            {
                Content = "View Employee Details",
                Height = 45,
                Margin = new Thickness(0, 5, 0, 5),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(155, 89, 182)),
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand
            };
            viewButton.Click += (s, args) =>
            {
                actionWindow.Close();
                // Pass the action window callback so we can go back to it
                ShowEmployeeDetails(employee, () => ShowEmployeeActionPopup(employee));
            };
            stackPanel.Children.Add(viewButton);

            // Create Edit Employee button
            var editButton = new Button
            {
                Content = "Edit Employee",
                Height = 45,
                Margin = new Thickness(0, 5, 0, 5),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 152, 219)),
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand
            };
            editButton.Click += (s, args) =>
            {
                actionWindow.Close();
                // Load the employee into the edit form and navigate to edit tab
                LoadEmployeeForEditing(employee);
            };
            stackPanel.Children.Add(editButton);

            // Create View Training button
            var trainingButton = new Button
            {
                Content = "View Training Records",
                Height = 45,
                Margin = new Thickness(0, 5, 0, 5),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 152, 219)),
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand
            };
            trainingButton.Click += (s, args) =>
            {
                actionWindow.Close();
                // Navigate to training records tab for this employee
                NavigateToTrainingRecords(employee);
            };
            stackPanel.Children.Add(trainingButton);

            // Set content and show window
            actionWindow.Content = stackPanel;
            actionWindow.ShowDialog();
        }

        /// <summary>
        /// Navigates to the Training Records tab and loads records for the specified employee
        /// Uses Dispatcher to ensure navigation happens correctly
        /// </summary>
        private void NavigateToTrainingRecords(Employee employee)
        {
            try
            {
                // Load training records for this employee
                LoadTrainingRecords(employee.Id);

                // Use Dispatcher to ensure the tab switch happens after UI updates
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    // Switch to the training records tab (index 2)
                    // Tab 0 = Employees, Tab 1 = Edit Employee, Tab 2 = Training Records
                    MainTabControl.SelectedIndex = 2;
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error navigating to training records: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// Shows a popup window with full employee details
        /// backAction is called when the back button is clicked
        /// </summary>
        private void ShowEmployeeDetails(Employee employee, Action backAction)
        {
            if (employee == null) return;

            // Reload employee from database to get fresh data
            using (var db = new AppDbContext())
            {
                employee = db.Employees
                    .Include(e => e.Department)
                    .FirstOrDefault(e => e.Id == employee.Id);

                if (employee == null)
                {
                    MessageBox.Show(
                        "Could not load employee data.",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                    return;
                }
            }

            // Create a popup window to show employee details
            var detailsWindow = new Window
            {
                Title = $"Employee Details - {employee.FullName}",
                Width = 550,
                Height = 600,
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
                Margin = new Thickness(30)
            };

            // Add header
            var header = new TextBlock
            {
                Text = "Employee Information",
                FontSize = 22,
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
            AddDetailRow("Employee ID:", employee.Id.ToString());
            AddDetailRow("First Name:", employee.FirstName);
            AddDetailRow("Last Name:", employee.LastName);
            AddDetailRow("Full Name:", employee.FullName);
            AddDetailRow("Email Address:", employee.Email);
            AddDetailRow("Phone Number:", employee.PhoneNumber);
            AddDetailRow("Job Title:", employee.JobTitle);
            AddDetailRow("Department:", employee.Department?.Name);
            AddDetailRow("Hire Date:", employee.HireDate.ToString("dd/MM/yyyy"));
            AddDetailRow("Salary:", $"${employee.Salary:N2}");
            AddDetailRow("Tax Rate:", $"{employee.TaxRate}%");
            AddDetailRow("Annual Leave Balance:", $"{employee.AnnualLeaveBalance} days");
            AddDetailRow("Sick Leave Balance:", $"{employee.SickLeaveBalance} days");
            AddDetailRow("Status:", employee.IsActive ? "Active" : "Inactive");

            // Create button panel
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 25, 0, 0)
            };

            // Add back button (YELLOW COLOR) - only if backAction provided
            if (backAction != null)
            {
                var backButton = new Button
                {
                    Content = "Back",
                    Width = 100,
                    Height = 35,
                    Margin = new Thickness(5, 0, 5, 0),
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(243, 156, 18)), // Yellow/Orange color
                    Foreground = System.Windows.Media.Brushes.White,
                    BorderThickness = new Thickness(0),
                    FontWeight = FontWeights.SemiBold,
                    Cursor = Cursors.Hand
                };
                backButton.Click += (s, args) =>
                {
                    detailsWindow.Close();
                    // Call the back action to show the previous menu
                    backAction?.Invoke();
                };
                buttonPanel.Children.Add(backButton);
            }

            // Add close button
            var closeButton = new Button
            {
                Content = "Close",
                Width = 100,
                Height = 35,
                Margin = new Thickness(5, 0, 5, 0),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 152, 219)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand
            };
            closeButton.Click += (s, args) => detailsWindow.Close();
            buttonPanel.Children.Add(closeButton);

            stackPanel.Children.Add(buttonPanel);

            // Set content and show window
            scrollViewer.Content = stackPanel;
            detailsWindow.Content = scrollViewer;
            detailsWindow.ShowDialog();
        }

        /// <summary>
        /// Loads employee data into the edit form and switches to edit tab
        /// Uses Dispatcher to ensure navigation happens correctly
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

                    // Make the edit tab visible
                    EditEmployeeTab.Visibility = Visibility.Visible;

                    // Use Dispatcher to ensure the tab switch happens after UI updates
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        // Switch to the edit tab (index 1)
                        // Tab 0 = Employees, Tab 1 = Edit Employee, Tab 2 = Training Records
                        MainTabControl.SelectedIndex = 1;
                    }), System.Windows.Threading.DispatcherPriority.Background);
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
                                      $"{emp.Salary:F2}," +
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
        /// Load all training records with employee and user information
        /// FIXED: Now properly loads all training records including for workplace trainers
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
        /// FIXED: Now loads ALL active employees for training allocation
        /// </summary>
        private void LoadEmployeesForTraining()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    // Load ALL active employees, including the workplace trainer themselves
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
        /// FIXED: Now properly shows the panel and loads employees
        /// </summary>
        private void AddTraining_Click(object sender, RoutedEventArgs e)
        {
            // Make sure the panel is visible
            AddTrainingPanel.Visibility = Visibility.Visible;

            // Reload employees to make sure list is current
            LoadEmployeesForTraining();

            // Reset form fields
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
        /// FIXED: Now properly validates and saves training records
        /// </summary>
        private void SaveTraining_Click(object sender, RoutedEventArgs e)
        {
            // Validate employee selection
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

            // Validate training type selection
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
                    // Get the selected training type
                    string trainingType = ((ComboBoxItem)TrainingTypeComboBox.SelectedItem).Content.ToString() ?? "";

                    // Get the selected employee ID
                    int employeeId = (int)TrainingEmployeeComboBox.SelectedValue;

                    // Create new training record
                    var training = new Training
                    {
                        EmployeeId = employeeId,
                        TrainingType = trainingType,
                        Status = "Not Started",
                        Notes = TrainingNotesTextBox.Text.Trim()
                    };

                    // Add to database
                    db.Trainings.Add(training);
                    db.SaveChanges();
                }

                MessageBox.Show(
                    "Training record added successfully!",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                // Hide the add panel and reload training records
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
        /// FIXED: Workplace trainers now have full rights to sign off training
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

            // Get the selected training record
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
                    // Find the training record in the database
                    var training = db.Trainings.Find(selected.Id);
                    if (training != null)
                    {
                        // Update training record
                        training.Status = "Completed";
                        training.CompletedDate = DateTime.Now;
                        training.SignedOffByUserId = _currentUser.Id;

                        // Save changes
                        db.SaveChanges();
                    }
                }

                MessageBox.Show(
                    $"Training marked as completed and signed off by {_currentUser.Username}!",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                // Reload training records to show updated information
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
        /// Create new employee
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
                // Also reload the training employee dropdown
                LoadEmployeesForTraining();
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