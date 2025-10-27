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
    /// Interaction logic for GrievancesPage.xaml
    /// Handles submission and management of employee grievances.
    /// </summary>
    public partial class GrievancesPage : Page
    {
        private readonly User _currentUser;

        public GrievancesPage(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            // Hide admin tab if not admin
            if (_currentUser.Role != "Admin")
            {
                AdminGrievancesTab.Visibility = Visibility.Collapsed;
            }
            LoadGrievances();
        }

        // Load grievances into the grids
        private void LoadGrievances()
        {
            using (var db = new AppDbContext())
            {
                // Load current user's grievances
                var myGrievances = db.Grievances
                    .Where(g => g.EmployeeId == _currentUser.EmployeeId)
                    .OrderByDescending(g => g.SubmittedDate)
                    .ToList();
                MyGrievancesGrid.ItemsSource = myGrievances;

                // Load all grievances if admin
                if (_currentUser.Role == "Admin")
                {
                    var allGrievances = db.Grievances
                        .Include(g => g.Employee)
                        .OrderByDescending(g => g.SubmittedDate)
                        .ToList();
                    AllGrievancesGrid.ItemsSource = allGrievances;
                }
            }
        }

        // Handles submission of a new grievance
        private void SubmitGrievance_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(GrievanceTitleTextBox.Text) || string.IsNullOrWhiteSpace(GrievanceDescriptionTextBox.Text))
            {
                MessageBox.Show("Please enter a title and description", "Validation Error");
                return;
            }

            using (var db = new AppDbContext())
            {
                var grievance = new Grievance
                {
                    EmployeeId = _currentUser.EmployeeId,
                    Title = GrievanceTitleTextBox.Text.Trim(),
                    Description = GrievanceDescriptionTextBox.Text.Trim(),
                    Status = "Open",
                    SubmittedDate = DateTime.Now
                };
                db.Grievances.Add(grievance);
                db.SaveChanges();
            }

            MessageBox.Show("Grievance submitted successfully!", "Success");
            GrievanceTitleTextBox.Clear();
            GrievanceDescriptionTextBox.Clear();
            LoadGrievances();
        }

        // Handles responding to a grievance (admin)
        private void Respond_Click(object sender, RoutedEventArgs e)
        {
            var selected = AllGrievancesGrid.SelectedItem as Grievance;
            if (selected == null)
            {
                MessageBox.Show("Please select a grievance to respond", "No Selection");
                return;
            }
            if (string.IsNullOrWhiteSpace(AdminResponseTextBox.Text))
            {
                MessageBox.Show("Please enter a response", "Validation Error");
                return;
            }

            using (var db = new AppDbContext())
            {
                var grievance = db.Grievances.Find(selected.Id);
                if (grievance != null)
                {
                    grievance.AdminResponse = AdminResponseTextBox.Text.Trim();
                    grievance.Status = "InProgress";
                    grievance.HandledByUserId = _currentUser.Id;
                    db.SaveChanges();
                }
            }

            MessageBox.Show("Response saved!", "Success");
            AdminResponseTextBox.Clear();
            LoadGrievances();
        }

        // Handles closing a grievance (admin)
        private void CloseGrievance_Click(object sender, RoutedEventArgs e)
        {
            var selected = AllGrievancesGrid.SelectedItem as Grievance;
            if (selected == null)
            {
                MessageBox.Show("Please select a grievance to close", "No Selection");
                return;
            }

            using (var db = new AppDbContext())
            {
                var grievance = db.Grievances.Find(selected.Id);
                if (grievance != null)
                {
                    grievance.Status = "Closed";
                    grievance.HandledByUserId = _currentUser.Id;
                    grievance.ResolvedDate = DateTime.Now;
                    db.SaveChanges();
                }
            }

            MessageBox.Show("Grievance closed", "Success");
            AdminResponseTextBox.Clear();
            LoadGrievances();
        }
    }
}
