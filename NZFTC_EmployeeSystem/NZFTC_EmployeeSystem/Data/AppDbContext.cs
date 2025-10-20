// This section controls the SQLite database connection.
// [DB HOOK] Add DbSet<T> properties for more tables.

using Microsoft.EntityFrameworkCore;
using NZFTC_EmployeeSystem.Models;
using System.IO;

namespace NZFTC_EmployeeSystem.Data
{
    public class AppDbContext : DbContext
    {
        // [DB HOOK] Each DbSet<T> becomes a table.
        public DbSet<Employee> Employees => Set<Employee>();

        public AppDbContext() { }

        // Store the DB in %LOCALAPPDATA%\NZFTC_EmployeeSystem\employee.db
        private static string GetDatabasePath()
        {
            var appData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            var dir = Path.Combine(appData, "NZFTC_EmployeeSystem");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "employee.db");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite($"Data Source={GetDatabasePath()}");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // [DB HOOK] Fluent configuration (indexes, constraints, seeds) goes here later.
            base.OnModelCreating(modelBuilder);
        }
    }
}
