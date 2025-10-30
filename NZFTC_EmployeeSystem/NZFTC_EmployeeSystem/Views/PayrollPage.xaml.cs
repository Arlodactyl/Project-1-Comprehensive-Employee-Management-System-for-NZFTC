using Microsoft.EntityFrameworkCore;
using NZFTC_EmployeeSystem.Data;
using NZFTC_EmployeeSystem.Models;
using System;  // ADD THIS
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace NZFTC_EmployeeSystem.Views
{
    /// <summary>
    /// Interaction logic for PayrollPage.xaml
    /// Displays payslips for the current user and, if an admin,
    /// all payslips across employees.
    /// </summary>
    public partial class PayrollPage : Page
    {
        private readonly User _currentUser;

        public PayrollPage(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            // Hide admin tab if user is not an admin
            if (_currentUser.Role != "Admin")
            {
                AdminPayslipsTab.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Show the admin payslip creation panel and load employee list
                AdminPayslipPanel.Visibility = Visibility.Visible;
                LoadEmployeeList();
            }
            // Load payslips for the current user (and all if admin)
            LoadPayslipData();
        }

        // Loads payslip data from the database into the grids
        private void LoadPayslipData()
        {
            using (var db = new AppDbContext())
            {
                // Load payslips for the current user
                var myPayslips = db.Payslips
                    .Where(p => p.EmployeeId == _currentUser.EmployeeId)
                    .OrderByDescending(p => p.PayPeriodEnd)
                    .ToList();
                MyPayslipsGrid.ItemsSource = myPayslips;

                // Load all payslips if admin
                if (_currentUser.Role == "Admin")
                {
                    var allPayslips = db.Payslips
                        .Include(p => p.Employee)
                        .OrderByDescending(p => p.PayPeriodEnd)
                        .ToList();
                    AllPayslipsGrid.ItemsSource = allPayslips;
                }
            }
        }

        /// <summary>
        /// Loads the list of active employees into the EmployeeComboBox
        /// This is used by administrators when generating a new payslip.
        /// </summary>
        private void LoadEmployeeList()
        {
            using (var db = new AppDbContext())
            {
                // Only include active employees so we don't generate payslips for inactive ones
                var employees = db.Employees
                    .Where(e => e.IsActive)
                    .OrderBy(e => e.LastName)
                    .ToList();
                EmployeeComboBox.ItemsSource = employees;
                // Display full name and store the Id as the selected value
                EmployeeComboBox.DisplayMemberPath = nameof(Employee.FullName);
                EmployeeComboBox.SelectedValuePath = nameof(Employee.Id);
            }
        }

        /// <summary>
        /// Handles the Generate Payslip button click for administrators.
        /// Validates the input fields, calculates tax and net salary using the
        /// employee's tax rate, saves the payslip to the database and refreshes
        /// the payslip grids.
        /// </summary>
        private void GeneratePayslip_Click(object sender, RoutedEventArgs e)
        {
            // Make sure an employee is selected
            if (EmployeeComboBox.SelectedItem is not Employee selectedEmployee)
            {
                MessageBox.Show("Please select an employee.", "Validation Error");
                return;
            }

            // Validate dates
            if (!PayStartDatePicker.SelectedDate.HasValue || !PayEndDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Please select both pay period start and end dates.", "Validation Error");
                return;
            }
            var startDate = PayStartDatePicker.SelectedDate.Value;
            var endDate = PayEndDatePicker.SelectedDate.Value;
            if (endDate < startDate)
            {
                MessageBox.Show("The end date must be on or after the start date.", "Validation Error");
                return;
            }

            // Validate gross salary input
            if (!decimal.TryParse(GrossSalaryTextBox.Text, out decimal grossSalary) || grossSalary <= 0)
            {
                MessageBox.Show("Please enter a valid gross salary.", "Validation Error");
                return;
            }

            using (var db = new AppDbContext())
            {
                // Retrieve the employee again to get an up-to-date tax rate
                var employee = db.Employees.Find(selectedEmployee.Id);
                if (employee == null)
                {
                    MessageBox.Show("Selected employee could not be found in the database.", "Error");
                    return;
                }

                // Calculate tax deduction and net salary based on the employee's tax rate
                decimal taxDeduction = grossSalary * (employee.TaxRate / 100);
                decimal netSalary = grossSalary - taxDeduction;

                // Create and save the new payslip
                var payslip = new Payslip
                {
                    EmployeeId = employee.Id,
                    PayPeriodStart = startDate,
                    PayPeriodEnd = endDate,
                    GrossSalary = grossSalary,
                    TaxDeduction = taxDeduction,
                    NetSalary = netSalary,
                    GeneratedDate = DateTime.Now,
                    GeneratedByUserId = _currentUser.Id
                };
                db.Payslips.Add(payslip);
                db.SaveChanges();
            }

            // Show confirmation
            MessageBox.Show("Payslip generated successfully!", "Success");

            // Reset form fields
            EmployeeComboBox.SelectedIndex = -1;
            PayStartDatePicker.SelectedDate = null;
            PayEndDatePicker.SelectedDate = null;
            GrossSalaryTextBox.Clear();

            // Reload payslip grids so the new entry appears
            LoadPayslipData();
        }
    }
}