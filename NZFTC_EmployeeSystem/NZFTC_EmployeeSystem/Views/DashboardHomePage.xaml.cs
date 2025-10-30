using System;
using System.Linq;
using System.Windows.Controls;
using NZFTC_EmployeeSystem.Data;

namespace NZFTC_EmployeeSystem.Views
{
    public partial class DashboardHomePage : Page
    {
        private readonly AppDbContext _dbContext;

        public DashboardHomePage()
        {
            InitializeComponent();
            _dbContext = new AppDbContext();
            LoadSummary();
        }

        private void LoadSummary()
        {
            TotalEmployeesText.Text = _dbContext.Employees.Count().ToString();
       PendingLeaveText.Text = _dbContext.LeaveRequests.Count(l => l.Status == "Pending").ToString();
         OpenGrievanceText.Text = _dbContext.Grievances.Count(g => g.Status == "Open").ToString();
            UpcomingHolidayText.Text = _dbContext.Holidays.Count(h => h.Date >= DateTime.Today).ToString();
        }
    }
}
