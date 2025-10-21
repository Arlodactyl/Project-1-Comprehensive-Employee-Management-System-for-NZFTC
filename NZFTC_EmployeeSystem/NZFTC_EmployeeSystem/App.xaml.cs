using System.Windows;
using Microsoft.EntityFrameworkCore;
using NZFTC_EmployeeSystem.Data;

namespace NZFTC_EmployeeSystem
{
    /// <summary>
    /// This is the application entry point
    /// It runs before any windows are shown
    /// We use it to initialize the database
    /// </summary>
    public partial class App : Application
    {
        // ========================================
        // STARTUP METHOD - Runs when app starts
        // ========================================
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialize the database
            // This creates the database file and tables if they don't exist
            InitializeDatabase();
        }

        // ========================================
        // DATABASE INITIALIZATION
        // This creates the database and seeds initial data
        // ========================================
        private void InitializeDatabase()
        {
            try
            {
                // Create a database context
                // 'using' ensures it's properly disposed after we're done
                using (var db = new AppDbContext())
                {
                    // Check if database exists, if not create it
                    // This also creates all tables defined in AppDbContext
                   //  db.Database.EnsureCreated();
                  // Apply pending migrations to update the database schemachem
                  db.Database.Migrate();
                    // MessageBox.Show(
                    //     "Database initialized successfully!",
                    //     "Database Status",
                    //     MessageBoxButton.OK,
                    //     MessageBoxImage.Information
                    // );
                }
            }
            catch (System.Exception ex)
            {
                // If something goes wrong, show an error message
                MessageBox.Show(
                    $"Failed to initialize database:\n{ex.Message}",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                // Exit the application since we can't run without a database
                Application.Current.Shutdown();
            }
        }
    }
}
