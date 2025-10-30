using System.Windows;
using Microsoft.EntityFrameworkCore;
using NZFTC_EmployeeSystem.Data;

namespace NZFTC_EmployeeSystem
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    // CHANGED: Use EnsureCreated instead of Migrate
                    // This creates all tables and seeds the data
                    db.Database.EnsureCreated();

                    MessageBox.Show(
                        "Database initialized successfully!\n\nAdmin login:\nUsername: admin\nPassword: admin123",
                        "Database Ready",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(
                    $"Failed to initialize database:\n{ex.Message}",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                Application.Current.Shutdown();
            }
        }
    }
}