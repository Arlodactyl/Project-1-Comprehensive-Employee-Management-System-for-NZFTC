using System.Windows;
using Microsoft.EntityFrameworkCore;          // <-- needed for Database.EnsureCreated()
using NZFTC_EmployeeSystem.Data;

namespace NZFTC_EmployeeSystem
{
    /// <summary>
    /// App bootstrap.
    /// - Controls startup & theming (App.xaml)
    /// - [DB HOOK] Initializes SQLite DB on first run
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Ensure the local SQLite DB and tables exist
            using var db = new AppDbContext();   // DbContext implements IDisposable
            db.Database.EnsureCreated();

            // [DB HOOK] Optional: seed demo data once
            // if (!db.Employees.Any())
            // {
            //     db.Employees.Add(new Models.Employee { FirstName = "Demo", LastName = "User", Role = "Admin" });
            //     db.SaveChanges();
            // }
        }
    }
}
