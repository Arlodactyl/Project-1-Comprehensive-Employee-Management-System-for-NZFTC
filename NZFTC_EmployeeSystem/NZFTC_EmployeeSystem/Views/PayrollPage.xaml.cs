using Microsoft.EntityFrameworkCore;
using NZFTC_EmployeeSystem.Data;
using NZFTC_EmployeeSystem.Models;
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
    }
}
