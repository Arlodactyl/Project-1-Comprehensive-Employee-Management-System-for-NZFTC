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
using System.Windows.Media;
using System.Windows.Shapes;
using Path = System.Windows.Shapes.Path;

namespace NZFTC_EmployeeSystem.Views
{
    public partial class EmployeeManagementPage : Page
    {
        private readonly User _currentUser;
        private Employee _selectedEmployee = null;
        private Employee _editingEmployee = null;

        // Below initializes page and loads data
        public EmployeeManagementPage(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;

            // Check permissions
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

            // Load all data
            LoadEmployees();
            RoleComboBox.SelectedIndex = 0;
            LoadDepartments();
            LoadAllTrainingRecords();
            LoadEmployeesForTraining();

            AddTrainingPanel.Visibility = Visibility.Collapsed;
            TrainingTypeComboBox.SelectedIndex = 0;

            // Set button visibility based on role
            if (_currentUser.Role == "Admin" || _currentUser.Role == "Workplace Trainer")
            {
                CompleteTrainingButton.Visibility = Visibility.Visible;
            }
            else
            {
                CompleteTrainingButton.Visibility = Visibility.Collapsed;
            }

            LoadDepartmentsForEdit();
        }

        #region Employee Tab Actions

        // Below shows selected employee details
        private void ViewEmployeeDetails_Click(object sender, RoutedEventArgs e)
        {
            _selectedEmployee = EmployeesGrid.SelectedItem as Employee;

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

            ShowEmployeeDetails(_selectedEmployee, null);
        }

        // Below loads employee data for editing
        private void EditEmployee_Click(object sender, RoutedEventArgs e)
        {
            _selectedEmployee = EmployeesGrid.SelectedItem as Employee;

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

            LoadEmployeeForEditing(_selectedEmployee);
        }

        // Below shows training records for selected employee
        private void ViewEmployeeTraining_Click(object sender, RoutedEventArgs e)
        {
            _selectedEmployee = EmployeesGrid.SelectedItem as Employee;

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

            NavigateToTrainingRecords(_selectedEmployee);
        }

        #endregion

        private void EmployeesGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            _selectedEmployee = EmployeesGrid.SelectedItem as Employee;

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

            ShowEmployeeActionPopup(_selectedEmployee);
        }

        private void ShowEmployeeActionPopup(Employee employee)
        {
            var actionWindow = new Window
            {
                Title = $"Employee Actions - {employee.FullName}",
                Width = 450,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };

            var stackPanel = new StackPanel
            {
                Margin = new Thickness(30),
                VerticalAlignment = VerticalAlignment.Center
            };

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
                ShowEmployeeDetails(employee, () => ShowEmployeeActionPopup(employee));
            };
            stackPanel.Children.Add(viewButton);

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
                LoadEmployeeForEditing(employee);
            };
            stackPanel.Children.Add(editButton);

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
                NavigateToTrainingRecords(employee);
            };
            stackPanel.Children.Add(trainingButton);

            actionWindow.Content = stackPanel;
            actionWindow.ShowDialog();
        }

        private void NavigateToTrainingRecords(Employee employee)
        {
            try
            {
                LoadTrainingRecords(employee.Id);

                Dispatcher.BeginInvoke(new Action(() =>
                {
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

        private void ShowEmployeeDetails(Employee employee, Action backAction)
        {
            if (employee == null) return;

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

            var detailsWindow = new Window
            {
                Title = $"Employee Details - {employee.FullName}",
                Width = 550,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            var stackPanel = new StackPanel
            {
                Margin = new Thickness(30)
            };

            var header = new TextBlock
            {
                Text = "Employee Information",
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20)
            };
            stackPanel.Children.Add(header);

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

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 25, 0, 0)
            };

            if (backAction != null)
            {
                var backButton = new Button
                {
                    Content = "Back",
                    Width = 100,
                    Height = 35,
                    Margin = new Thickness(5, 0, 5, 0),
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(243, 156, 18)),
                    Foreground = System.Windows.Media.Brushes.White,
                    BorderThickness = new Thickness(0),
                    FontWeight = FontWeights.SemiBold,
                    Cursor = Cursors.Hand
                };
                backButton.Click += (s, args) =>
                {
                    detailsWindow.Close();
                    backAction?.Invoke();
                };
                buttonPanel.Children.Add(backButton);
            }

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

            scrollViewer.Content = stackPanel;
            detailsWindow.Content = scrollViewer;
            detailsWindow.ShowDialog();
        }

        private void LoadEmployeeForEditing(Employee employee)
        {
            try
            {
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

                    if (_editingEmployee.Department != null)
                    {
                        EditDepartmentComboBox.SelectedValue = _editingEmployee.DepartmentId;
                    }

                    EditEmployeeTab.Visibility = Visibility.Visible;

                    Dispatcher.BeginInvoke(new Action(() =>
                    {
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

                    db.SaveChanges();
                }

                MessageBox.Show(
                    $"Employee {EditFirstNameTextBox.Text} {EditLastNameTextBox.Text} updated successfully!",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                LoadEmployees();
                EditEmployeeTab.Visibility = Visibility.Collapsed;
                MainTabControl.SelectedIndex = 0;
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

        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to cancel? Any unsaved changes will be lost.",
                "Confirm Cancel",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                EditEmployeeTab.Visibility = Visibility.Collapsed;
                MainTabControl.SelectedIndex = 0;
                _editingEmployee = null;
            }
        }

        private void TrainingGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedTraining = TrainingGrid.SelectedItem as Training;
            if (selectedTraining == null) return;

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

        // Below loads all employees from database
        private void LoadEmployees()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var employees = db.Employees
                        .Include(e => e.Department)
                        .OrderBy(e => e.Id)
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

        // Below loads departments for dropdown
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
                        .OrderBy(emp => emp.Id)
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

        private void RefreshEmployees_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Clear();
            LoadEmployees();
        }

        private void ClearForm_Click(object sender, RoutedEventArgs e)
        {
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
        }

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

                    string downloadsPath = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        "Downloads");
                    string fileName = $"Employees_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                    string fullPath = System.IO.Path.Combine(downloadsPath, fileName);

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

                    if (trainings.Count == 0)
                    {
                        EmptyTrainingMessage.Visibility = Visibility.Visible;
                        EmptyMessageText.Text = "No training records exist in the system yet.\n\nClick 'Add Training Records' below to create the first training record.";
                    }
                    else
                    {
                        EmptyTrainingMessage.Visibility = Visibility.Collapsed;
                    }
                }

                UpdateTrainingPieChart();
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

        private void UpdateTrainingPieChart()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var allTraining = db.Trainings.ToList();

                    if (_currentUser.Role == "Employee" && _currentUser.EmployeeId > 0)
                    {
                        allTraining = allTraining.Where(t => t.EmployeeId == _currentUser.EmployeeId).ToList();
                    }

                    int completed = allTraining.Count(t => t.Status == "Completed");
                    int pending = allTraining.Count(t => t.Status == "Pending");
                    int notCompleted = allTraining.Count(t => t.Status == "Not Completed");
                    int total = allTraining.Count;

                    if (total == 0)
                    {
                        PieChartCanvas.Children.Clear();

                        var emptyCircle = new Ellipse
                        {
                            Width = 180,
                            Height = 180,
                            Fill = new SolidColorBrush(Color.FromRgb(189, 195, 199)),
                            Stroke = new SolidColorBrush(Color.FromRgb(149, 165, 166)),
                            StrokeThickness = 1
                        };
                        Canvas.SetLeft(emptyCircle, 0);
                        Canvas.SetTop(emptyCircle, 0);
                        PieChartCanvas.Children.Add(emptyCircle);

                        var emptyText = new TextBlock
                        {
                            Text = "No Data",
                            FontSize = 14,
                            FontWeight = FontWeights.Bold,
                            Foreground = new SolidColorBrush(Color.FromRgb(127, 140, 141))
                        };
                        Canvas.SetLeft(emptyText, 90 - (emptyText.Text.Length * 3.5));
                        Canvas.SetTop(emptyText, 85);
                        PieChartCanvas.Children.Add(emptyText);

                        CompletedLegend.Text = "Completed: 0 (0%)";
                        PendingLegend.Text = "Pending: 0 (0%)";
                        NotCompletedLegend.Text = "Not Completed: 0 (0%)";
                        return;
                    }

                    double completedPercent = (double)completed / total * 100;
                    double pendingPercent = (double)pending / total * 100;
                    double notCompletedPercent = (double)notCompleted / total * 100;

                    CompletedLegend.Text = $"Completed: {completed} ({completedPercent:F1}%)";
                    PendingLegend.Text = $"Pending: {pending} ({pendingPercent:F1}%)";
                    NotCompletedLegend.Text = $"Not Completed: {notCompleted} ({notCompletedPercent:F1}%)";

                    PieChartCanvas.Children.Clear();

                    double centerX = 90;
                    double centerY = 90;
                    double radius = 85;
                    double startAngle = -90;

                    if (completed > 0)
                    {
                        double sweepAngle = (double)completed / total * 360;
                        AddPieSlice(centerX, centerY, radius, startAngle, sweepAngle, Color.FromRgb(39, 174, 96));
                        startAngle += sweepAngle;
                    }

                    if (pending > 0)
                    {
                        double sweepAngle = (double)pending / total * 360;
                        AddPieSlice(centerX, centerY, radius, startAngle, sweepAngle, Color.FromRgb(243, 156, 18));
                        startAngle += sweepAngle;
                    }

                    if (notCompleted > 0)
                    {
                        double sweepAngle = (double)notCompleted / total * 360;
                        AddPieSlice(centerX, centerY, radius, startAngle, sweepAngle, Color.FromRgb(231, 76, 60));
                    }

                    var centerCircle = new Ellipse
                    {
                        Width = 50,
                        Height = 50,
                        Fill = Brushes.White
                    };
                    Canvas.SetLeft(centerCircle, centerX - 25);
                    Canvas.SetTop(centerCircle, centerY - 25);
                    PieChartCanvas.Children.Add(centerCircle);

                    var totalText = new TextBlock
                    {
                        Text = total.ToString(),
                        FontSize = 20,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80))
                    };
                    totalText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    Canvas.SetLeft(totalText, centerX - (totalText.DesiredSize.Width / 2));
                    Canvas.SetTop(totalText, centerY - 10);
                    PieChartCanvas.Children.Add(totalText);

                    var totalLabel = new TextBlock
                    {
                        Text = "Total",
                        FontSize = 10,
                        Foreground = new SolidColorBrush(Color.FromRgb(127, 140, 141))
                    };
                    totalLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    Canvas.SetLeft(totalLabel, centerX - (totalLabel.DesiredSize.Width / 2));
                    Canvas.SetTop(totalLabel, centerY + 5);
                    PieChartCanvas.Children.Add(totalLabel);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error updating pie chart: {ex.Message}",
                    "Chart Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void AddPieSlice(double centerX, double centerY, double radius, double startAngle, double sweepAngle, Color color)
        {
            if (sweepAngle <= 0) return;

            var path = new Path
            {
                Fill = new SolidColorBrush(color),
                Stroke = Brushes.White,
                StrokeThickness = 2
            };

            var geometry = new PathGeometry();
            var figure = new PathFigure
            {
                StartPoint = new Point(centerX, centerY),
                IsClosed = true
            };

            double startAngleRad = startAngle * Math.PI / 180.0;
            double endAngleRad = (startAngle + sweepAngle) * Math.PI / 180.0;

            var startPoint = new Point(
                centerX + radius * Math.Cos(startAngleRad),
                centerY + radius * Math.Sin(startAngleRad)
            );

            figure.Segments.Add(new LineSegment(startPoint, true));

            var endPoint = new Point(
                centerX + radius * Math.Cos(endAngleRad),
                centerY + radius * Math.Sin(endAngleRad)
            );

            var arcSegment = new ArcSegment
            {
                Point = endPoint,
                Size = new Size(radius, radius),
                IsLargeArc = sweepAngle > 180,
                SweepDirection = SweepDirection.Clockwise
            };

            figure.Segments.Add(arcSegment);
            figure.Segments.Add(new LineSegment(new Point(centerX, centerY), true));

            geometry.Figures.Add(figure);
            path.Data = geometry;

            PieChartCanvas.Children.Add(path);
        }

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

                    var employee = db.Employees.Find(employeeId);
                    string employeeName = employee?.FullName ?? "this employee";

                    if (trainings.Count == 0)
                    {
                        EmptyTrainingMessage.Visibility = Visibility.Visible;
                        EmptyMessageText.Text = $"No training records found for {employeeName}.\n\nClick 'Add Training Records' below to create their first training record.";
                    }
                    else
                    {
                        EmptyTrainingMessage.Visibility = Visibility.Collapsed;
                    }
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

                    TrainingEmployeeComboBox.ItemsSource = null;
                    TrainingEmployeeComboBox.ItemsSource = employees;
                    TrainingEmployeeComboBox.Items.Refresh();
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

        private void AddTraining_Click(object sender, RoutedEventArgs e)
        {
            TrainingTypeComboBox.SelectedIndex = 0;
            TrainingNotesTextBox.Clear();
            TrainingEmployeeComboBox.SelectedIndex = -1;

            LoadEmployeesForTraining();

            AddTrainingPanel.Visibility = Visibility.Visible;
        }

        private void CancelAddTraining_Click(object sender, RoutedEventArgs e)
        {
            AddTrainingPanel.Visibility = Visibility.Collapsed;
        }

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

        private void CompleteTraining_Click(object sender, RoutedEventArgs e)
        {
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

                    string downloadsPath = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        "Downloads");
                    string fileName = $"All_Training_Records_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                    string fullPath = System.IO.Path.Combine(downloadsPath, fileName);

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

                    if (trainings.Count == 0)
                    {
                        EmptyTrainingMessage.Visibility = Visibility.Visible;
                        EmptyMessageText.Text = $"No training records match '{searchText}'.\n\nTry a different search term or clear the search to see all records.";
                    }
                    else
                    {
                        EmptyTrainingMessage.Visibility = Visibility.Collapsed;
                    }
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

        private void RefreshTraining_Click(object sender, RoutedEventArgs e)
        {
            TrainingSearchTextBox.Clear();
            LoadAllTrainingRecords();
        }

        private void InfoButton_Click(object sender, RoutedEventArgs e)
        {
            var infoMessage = @"Employee Management Help

EMPLOYEES TAB

View All Employees:
- Search employees by name, job title, email, or phone
- Double-click any employee for quick actions menu
- Use action buttons below grid for: View Details, Edit, View Training

Quick Actions Menu (Double-click employee):
> View Details: See complete employee information
> Edit Employee: Modify employee details
> View Training: Jump to their training records

EDIT EMPLOYEE TAB

Create New Employees:
- Fill in all required fields (marked with *)
- Select appropriate role (Employee/Admin/Trainer)
- System creates both employee record and user account
- Username and password are shown after creation

Required Fields:
- First & Last Name
- Email (must be unique)
- Username (must be unique)
- Password
- Department
- Salary & Tax Rate

TRAINING RECORDS TAB

View Training:
- Search by employee name or training type
- Double-click record to view detailed information
- Filter shows training for specific employees

Add Training:
- Click 'Add Training' button
- Select employee from dropdown
- Choose training type
- Add optional notes
- Training starts as 'Not Started' status

Complete Training:
- Select a training record
- Click 'Complete Training' button
- Only Admin and Workplace Trainer can sign off
- System records completion date and who signed off

Training Types Available:
- Ethics Training
- Induction
- Health and Safety
- Fire Safety
- First Aid
- Data Privacy
- Workplace Harassment
- Other

Export Training:
- Click 'Export Training Records' to save as CSV file
- Includes all visible/filtered records
- Opens file location after export

WORKFLOW TIPS

Finding Employees:
1. Use search box to filter by name
2. Grid shows real-time filtered results
3. Click Refresh to reload all data

Adding Training Quickly:
1. Find employee in Employees tab
2. Click 'View Training' button
3. Click 'Add Training' (employee pre-selected)
4. Fill in training type and save

Completing Training:
1. Go to Training Records tab
2. Find the training record
3. Select it and click 'Complete Training'
4. System marks as completed with your signature

Exporting Data:
- Use Export button on Training tab
- CSV format compatible with Excel
- Includes all current filters

ACCESS LEVELS

Admin:
- Full access to all features
- Can create/edit employees
- Can add and sign off training

Workplace Trainer:
- View all employees
- Add training records
- Sign off completed training
- Export training records

Employee:
- No access to this page
(Employees use Employee Dashboard)

TROUBLESHOOTING

Q: Employee not showing in training dropdown?
A: Click Refresh or switch tabs to reload the list

Q: Can't save training record?
A: Ensure both employee and training type are selected

Q: Complete Training button doesn't work?
A: Check you're Admin or Workplace Trainer role

Q: Empty training grid?
A: Select an employee first, or add new training records

For additional help, contact your system administrator.";

            MessageBox.Show(
                infoMessage,
                "Employee Management Help",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void CreateEmployee_Click(object sender, RoutedEventArgs e)
        {
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

                ClearForm_Click(sender, e);
                LoadEmployees();
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