using NZFTC_EmployeeSystem.Data;
using NZFTC_EmployeeSystem.Models;
using System;
using System.Windows.Controls;

namespace NZFTC_EmployeeSystem.Views
{
    /// <summary>
    /// Interaction logic for ProfilePage.xaml
    /// Displays the employee details for the logged in user.
    /// </summary>
    public partial class ProfilePage : Page
    {
        private readonly User _currentUser;

        public ProfilePage(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            LoadProfile();
        }

        // Loads employee details from the database and populates the UI
        private void LoadProfile()
        {
            using (var db = new AppDbContext())
            {
                var employee = db.Employees.Find(_currentUser.EmployeeId);
                if (employee != null)
                {
                    FullNameText.Text = employee.FullName;
                    EmailText.Text = employee.Email;
                    PhoneText.Text = employee.PhoneNumber;
                    JobTitleText.Text = employee.JobTitle;
                    // Display the name of the department by accessing the Department navigation property
                    DepartmentText.Text = employee.Department?.Name ?? string.Empty;
                    HireDateText.Text = employee.HireDate.ToString("dd/MM/yyyy");
                    SalaryText.Text = employee.Salary.ToString("C");
                    AnnualLeaveText.Text = employee.AnnualLeaveBalance.ToString();
                    SickLeaveText.Text = employee.SickLeaveBalance.ToString();
                }
            }
        }
    }
}
