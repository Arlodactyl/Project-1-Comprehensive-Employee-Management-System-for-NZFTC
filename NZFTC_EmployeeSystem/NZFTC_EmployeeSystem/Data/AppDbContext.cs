using Microsoft.EntityFrameworkCore;
using NZFTC_EmployeeSystem.Models;
using System;
using System.IO;
using System.Linq;

namespace NZFTC_EmployeeSystem.Data
{
    /// <summary>
    /// This class manages the SQLite database connection
    /// It defines all the tables and handles database operations
    /// Think of this as the "bridge" between your C# code and the SQLite database file
    /// </summary>
    public class AppDbContext : DbContext
    {
        // ========================================
        // DATABASE TABLES
        // Each DbSet<T> below creates a table in the database
        // ========================================

        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<User> Users => Set<User>();
        public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
        public DbSet<Payslip> Payslips => Set<Payslip>();
        public DbSet<Holiday> Holidays => Set<Holiday>();
        public DbSet<Grievance> Grievances => Set<Grievance>();
        // Added tables for normalization and role management
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();

        // Empty constructor required by Entity Framework
        public AppDbContext() { }

        // ========================================
        // DATABASE FILE LOCATION
        // This method determines WHERE the database file is saved
        // ========================================
        private static string GetDatabasePath()
        {
            // Get the user's AppData folder (e.g., C:\Users\YourName\AppData\Local)
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            // Create a subfolder for our app
            var dir = Path.Combine(appData, "NZFTC_EmployeeSystem");
            Directory.CreateDirectory(dir); // Create folder if it doesn't exist

            // Return the full path to the database file
            // Final path will be something like:
            // C:\Users\YourName\AppData\Local\NZFTC_EmployeeSystem\employee.db
            return Path.Combine(dir, "employee.db");
        }

        // ========================================
        // DATABASE CONNECTION CONFIGURATION
        // This tells Entity Framework to use SQLite
        // ========================================
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Configure to use SQLite database
                // UseSqlite is the method that says "use SQLite, not SQL Server or MySQL"
                optionsBuilder.UseSqlite($"Data Source={GetDatabasePath()}");
            }
        }

        // ========================================
        // DATABASE SCHEMA CONFIGURATION
        // This method sets up relationships and rules between tables
        // ========================================
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User -> Employee relationship
            // One User has one Employee
            // If an Employee is deleted, also delete their User account
            modelBuilder.Entity<User>()
                .HasOne(u => u.Employee)
                .WithMany()
                .HasForeignKey(u => u.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure LeaveRequest -> Employee relationship
            modelBuilder.Entity<LeaveRequest>()
                .HasOne(l => l.Employee)
                .WithMany()
                .HasForeignKey(l => l.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Payslip -> Employee relationship
            modelBuilder.Entity<Payslip>()
                .HasOne(p => p.Employee)
                .WithMany()
                .HasForeignKey(p => p.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Grievance -> Employee relationship
            modelBuilder.Entity<Grievance>()
                .HasOne(g => g.Employee)
                .WithMany()
                .HasForeignKey(g => g.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            // ========================================
            // ADDITIONAL RELATIONSHIPS
            // These relationships handle the new Department and Role tables
            // as well as linking optional user actions like approvals.
            // ========================================

            // Configure Employee -> Department relationship
            // One Department can have many Employees. When a Department
            // is deleted, we restrict deletion to avoid orphaning employees.
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure many-to-many relationship between User and Role
            // using the UserRole join entity. A composite key is used
            // so each user-role pair is unique.
            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);

            // Configure LeaveRequest.ApprovedByUserId -> User relationship
            modelBuilder.Entity<LeaveRequest>()
                .HasOne(l => l.ApprovedByUser)
                .WithMany()
                .HasForeignKey(l => l.ApprovedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Payslip.GeneratedByUserId -> User relationship
            modelBuilder.Entity<Payslip>()
                .HasOne(p => p.GeneratedByUser)
                .WithMany()
                .HasForeignKey(p => p.GeneratedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Grievance.HandledByUserId -> User relationship
            modelBuilder.Entity<Grievance>()
                .HasOne(g => g.HandledByUser)
                .WithMany()
                .HasForeignKey(g => g.HandledByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Holiday.CreatedByUserId -> User relationship
            modelBuilder.Entity<Holiday>()
                .HasOne(h => h.CreatedByUser)
                .WithMany()
                .HasForeignKey(h => h.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed initial data (create default admin account)
            SeedData(modelBuilder);
        }

        // ========================================
        // SEED DATA - Creates default admin account
        // This runs when the database is first created
        // ========================================
        private void SeedData(ModelBuilder modelBuilder)
        {
            // Create some default departments
            modelBuilder.Entity<Department>().HasData(
                new Department { Id = 1, Name = "IT" },
                new Department { Id = 2, Name = "HR" },
                new Department { Id = 3, Name = "Finance" }
            );

            // Create some default roles
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Admin" },
                new Role { Id = 2, Name = "Employee" }
            );

            // Create a default admin employee
            modelBuilder.Entity<Employee>().HasData(
                new Employee
                {
                    Id = 1,
                    FirstName = "Admin",
                    LastName = "User",
                    Email = "admin@nzftc.com",
                    PhoneNumber = "021-123-4567",
                    JobTitle = "System Administrator",
                    DepartmentId = 1, // Link to IT department
                    HireDate = new DateTime(2020, 1, 1),
                    Salary = 80000m,
                    TaxRate = 30m,
                    AnnualLeaveBalance = 20,
                    SickLeaveBalance = 10,
                    IsActive = true
                }
            );

            // Create a default admin user account
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Username = "admin",
                    Password = "admin123", // Stored in plain text (for demonstration)
                    Role = "Admin",
                    EmployeeId = 1,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                }
            );

            // Assign the Admin role to the default admin user via the join table
            modelBuilder.Entity<UserRole>().HasData(
                new UserRole { UserId = 1, RoleId = 1 }
            );

            // Add some sample holidays
            modelBuilder.Entity<Holiday>().HasData(
                new Holiday
                {
                    Id = 1,
                    Name = "New Year's Day",
                    Date = new DateTime(DateTime.Now.Year, 1, 1),
                    Type = "Public",
                    Description = "First day of the new year",
                    IsRecurring = true,
                    CreatedDate = DateTime.Now,
                    CreatedByUserId = 1
                },
                new Holiday
                {
                    Id = 2,
                    Name = "Waitangi Day",
                    Date = new DateTime(DateTime.Now.Year, 2, 6),
                    Type = "Public",
                    Description = "New Zealand's national day",
                    IsRecurring = true,
                    CreatedDate = DateTime.Now,
                    CreatedByUserId = 1
                },
                new Holiday
                {
                    Id = 3,
                    Name = "ANZAC Day",
                    Date = new DateTime(DateTime.Now.Year, 4, 25),
                    Type = "Public",
                    Description = "Remembrance of soldiers",
                    IsRecurring = true,
                    CreatedDate = DateTime.Now,
                    CreatedByUserId = 1
                },
                new Holiday
                {
                    Id = 4,
                    Name = "Christmas Day",
                    Date = new DateTime(DateTime.Now.Year, 12, 25),
                    Type = "Public",
                    Description = "Christmas celebration",
                    IsRecurring = true,
                    CreatedDate = DateTime.Now,
                    CreatedByUserId = 1
                }
            );

            // Seed random test employees
            modelBuilder.Entity<Employee>().HasData(
                new Employee { Id = 2, FirstName = "James", LastName = "Smith", Email = "james.smith@nzftc.co.nz", PhoneNumber = "022-345-6789", JobTitle = "Developer", DepartmentId = 1, HireDate = DateTime.Now.AddDays(-500), Salary = 75000, TaxRate = 17.5m, AnnualLeaveBalance = 20, SickLeaveBalance = 10, IsActive = true },
                new Employee { Id = 3, FirstName = "Emma", LastName = "Johnson", Email = "emma.johnson@nzftc.co.nz", PhoneNumber = "022-456-7890", JobTitle = "Manager", DepartmentId = 2, HireDate = DateTime.Now.AddDays(-800), Salary = 85000, TaxRate = 17.5m, AnnualLeaveBalance = 20, SickLeaveBalance = 10, IsActive = true },
                new Employee { Id = 4, FirstName = "Oliver", LastName = "Williams", Email = "oliver.williams@nzftc.co.nz", PhoneNumber = "022-567-8901", JobTitle = "Analyst", DepartmentId = 3, HireDate = DateTime.Now.AddDays(-300), Salary = 65000, TaxRate = 17.5m, AnnualLeaveBalance = 20, SickLeaveBalance = 10, IsActive = true },
                new Employee { Id = 5, FirstName = "Sophia", LastName = "Brown", Email = "sophia.brown@nzftc.co.nz", PhoneNumber = "022-678-9012", JobTitle = "Coordinator", DepartmentId = 1, HireDate = DateTime.Now.AddDays(-600), Salary = 55000, TaxRate = 17.5m, AnnualLeaveBalance = 20, SickLeaveBalance = 10, IsActive = true },
                new Employee { Id = 6, FirstName = "William", LastName = "Jones", Email = "william.jones@nzftc.co.nz", PhoneNumber = "022-789-0123", JobTitle = "Specialist", DepartmentId = 2, HireDate = DateTime.Now.AddDays(-450), Salary = 70000, TaxRate = 17.5m, AnnualLeaveBalance = 20, SickLeaveBalance = 10, IsActive = true },
                new Employee { Id = 7, FirstName = "Ava", LastName = "Garcia", Email = "ava.garcia@nzftc.co.nz", PhoneNumber = "022-890-1234", JobTitle = "Developer", DepartmentId = 1, HireDate = DateTime.Now.AddDays(-250), Salary = 72000, TaxRate = 17.5m, AnnualLeaveBalance = 20, SickLeaveBalance = 10, IsActive = true },
                new Employee { Id = 8, FirstName = "Lucas", LastName = "Miller", Email = "lucas.miller@nzftc.co.nz", PhoneNumber = "022-901-2345", JobTitle = "Associate", DepartmentId = 3, HireDate = DateTime.Now.AddDays(-700), Salary = 60000, TaxRate = 17.5m, AnnualLeaveBalance = 20, SickLeaveBalance = 10, IsActive = true },
                new Employee { Id = 9, FirstName = "Isabella", LastName = "Davis", Email = "isabella.davis@nzftc.co.nz", PhoneNumber = "022-012-3456", JobTitle = "Consultant", DepartmentId = 2, HireDate = DateTime.Now.AddDays(-400), Salary = 78000, TaxRate = 17.5m, AnnualLeaveBalance = 20, SickLeaveBalance = 10, IsActive = true },
                new Employee { Id = 10, FirstName = "Mason", LastName = "Rodriguez", Email = "mason.rodriguez@nzftc.co.nz", PhoneNumber = "022-123-4567", JobTitle = "Manager", DepartmentId = 1, HireDate = DateTime.Now.AddDays(-550), Salary = 88000, TaxRate = 17.5m, AnnualLeaveBalance = 20, SickLeaveBalance = 10, IsActive = true },
                new Employee { Id = 11, FirstName = "Mia", LastName = "Martinez", Email = "mia.martinez@nzftc.co.nz", PhoneNumber = "022-234-5678", JobTitle = "Analyst", DepartmentId = 3, HireDate = DateTime.Now.AddDays(-350), Salary = 67000, TaxRate = 17.5m, AnnualLeaveBalance = 20, SickLeaveBalance = 10, IsActive = true }
            );

            // Seed user accounts for test employees
            modelBuilder.Entity<User>().HasData(
                new User { Id = 2, Username = "james.smith", Password = "password123", Role = "Employee", EmployeeId = 2, IsActive = true, CreatedDate = DateTime.Now },
                new User { Id = 3, Username = "emma.johnson", Password = "password123", Role = "Employee", EmployeeId = 3, IsActive = true, CreatedDate = DateTime.Now },
                new User { Id = 4, Username = "oliver.williams", Password = "password123", Role = "Employee", EmployeeId = 4, IsActive = true, CreatedDate = DateTime.Now },
                new User { Id = 5, Username = "sophia.brown", Password = "password123", Role = "Employee", EmployeeId = 5, IsActive = true, CreatedDate = DateTime.Now },
                new User { Id = 6, Username = "william.jones", Password = "password123", Role = "Employee", EmployeeId = 6, IsActive = true, CreatedDate = DateTime.Now },
                new User { Id = 7, Username = "ava.garcia", Password = "password123", Role = "Employee", EmployeeId = 7, IsActive = true, CreatedDate = DateTime.Now },
                new User { Id = 8, Username = "lucas.miller", Password = "password123", Role = "Employee", EmployeeId = 8, IsActive = true, CreatedDate = DateTime.Now },
                new User { Id = 9, Username = "isabella.davis", Password = "password123", Role = "Employee", EmployeeId = 9, IsActive = true, CreatedDate = DateTime.Now },
                new User { Id = 10, Username = "mason.rodriguez", Password = "password123", Role = "Employee", EmployeeId = 10, IsActive = true, CreatedDate = DateTime.Now },
                new User { Id = 11, Username = "mia.martinez", Password = "password123", Role = "Employee", EmployeeId = 11, IsActive = true, CreatedDate = DateTime.Now }
            );

            // Assign Employee role to test users
            modelBuilder.Entity<UserRole>().HasData(
                new UserRole { UserId = 2, RoleId = 2 },
                new UserRole { UserId = 3, RoleId = 2 },
                new UserRole { UserId = 4, RoleId = 2 },
                new UserRole { UserId = 5, RoleId = 2 },
                new UserRole { UserId = 6, RoleId = 2 },
                new UserRole { UserId = 7, RoleId = 2 },
                new UserRole { UserId = 8, RoleId = 2 },
                new UserRole { UserId = 9, RoleId = 2 },
                new UserRole { UserId = 10, RoleId = 2 },
                new UserRole { UserId = 11, RoleId = 2 }
            );
        }
    }
}