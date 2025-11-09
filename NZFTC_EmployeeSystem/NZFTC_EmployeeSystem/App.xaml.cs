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

            // Initialize database silently in the background
            InitializeDatabase();
        }

        /// <summary>
        /// Initialize the database when the application starts
        /// Creates tables and seeds data if database doesn't exist
        /// Runs silently without showing any messages
        /// </summary>
        private void InitializeDatabase()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    // Create database and seed data if it doesn't exist
                    // This runs silently - no console output or message boxes
                    db.Database.EnsureCreated();
                }
            }
            catch (System.Exception ex)
            {
                // Only show error if database initialization fails
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