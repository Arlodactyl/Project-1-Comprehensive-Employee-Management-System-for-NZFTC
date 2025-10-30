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
        }
    }
}