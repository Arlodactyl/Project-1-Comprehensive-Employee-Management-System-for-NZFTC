using NZFTC_EmployeeSystem.Data;
using NZFTC_EmployeeSystem.Models;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace NZFTC_EmployeeSystem.Views
{
    /// <summary>
    /// Interaction logic for DepartmentsPage.xaml
    /// Provides functionality for administrators to view, add and delete departments.
    /// </summary>
    public partial class DepartmentsPage : Page
    {
        private readonly User _currentUser;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="currentUser">The logged in user</param>
        public DepartmentsPage(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            // Only admins should access this page, but check just in case
            if (_currentUser.Role != "Admin")
            {
                MessageBox.Show("You do not have permission to access Department Management.", "Access Denied");
                // Navigate back to previous page if not admin
                this.NavigationService?.GoBack();
                return;
            }
            LoadDepartmentData();
        }

        /// <summary>
        /// Loads all departments from the database and binds them to the grid.
        /// </summary>
        private void LoadDepartmentData()
        {
            using (var db = new AppDbContext())
            {
                var departments = db.Departments
                    .OrderBy(d => d.Name)
                    .ToList();
                DepartmentGrid.ItemsSource = departments;
            }
        }

        /// <summary>
        /// Handles the AddDepartment button click.
        /// Validates the input and saves the new department to the database.
        /// </summary>
        private void AddDepartment_Click(object sender, RoutedEventArgs e)
        {
            var deptName = DepartmentNameTextBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(deptName))
            {
                MessageBox.Show("Please enter a department name.", "Validation Error");
                return;
            }

            using (var db = new AppDbContext())
            {
                // Prevent adding duplicates (case insensitive)
                if (db.Departments.Any(d => d.Name.ToLower() == deptName.ToLower()))
                {
                    MessageBox.Show("Department already exists.", "Duplicate Department");
                    return;
                }

                var department = new Department
                {
                    Name = deptName
                };
                db.Departments.Add(department);
                db.SaveChanges();
            }

            MessageBox.Show("Department added successfully!", "Success");
            DepartmentNameTextBox.Clear();
            LoadDepartmentData();
        }

        /// <summary>
        /// Handles the Delete button click on a department row.
        /// Confirms deletion and ensures there are no employees assigned to the department.
        /// </summary>
        private void DeleteDepartment_Click(object sender, RoutedEventArgs e)
        {
            // The Tag property contains the department's Id
            if (sender is Button button && button.Tag is int deptId)
            {
                // Confirm deletion
                var result = MessageBox.Show(
                    "Are you sure you want to delete this department?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;

                using (var db = new AppDbContext())
                {
                    var department = db.Departments.Find(deptId);
                    if (department == null)
                    {
                        MessageBox.Show("Department not found.", "Error");
                        return;
                    }

                    // Check if any employees are associated with this department
                    bool hasEmployees = db.Employees.Any(e => e.DepartmentId == deptId);
                    if (hasEmployees)
                    {
                        MessageBox.Show("Cannot delete department with assigned employees. Reassign or remove employees first.", "Delete Not Allowed");
                        return;
                    }

                    db.Departments.Remove(department);
                    db.SaveChanges();
                }

                MessageBox.Show("Department deleted.", "Success");
                LoadDepartmentData();
            }
        }
    }
}