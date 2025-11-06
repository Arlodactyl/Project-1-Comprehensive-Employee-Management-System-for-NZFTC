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
        public DbSet<Training> Trainings => Set<Training>();

        // Empty constructor required by Entity Framework
        public AppDbContext() { }

        // ========================================
        // DATABASE FILE LOCATION
        // This method determines WHERE the database file is saved
        // Now saves to project root directory for easy submission
        // ========================================
        private static string GetDatabasePath()
        {
            // Get the directory where the application .exe is running from
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // Go up to the project root (assuming typical bin/Debug/net8.0 structure)
            // This navigates: bin/Debug/net8.0 -> bin/Debug -> bin -> project root
            var projectRoot = Directory.GetParent(baseDir)?.Parent?.Parent?.Parent?.FullName;

            // If we can find the project root, use it; otherwise use the exe location
            var dbDir = projectRoot ?? baseDir;

            // Return the full path to the database file in the project directory
            // Final path will be something like:
            // C:\Users\YourName\Documents\YourProject\employee.db
            return Path.Combine(dbDir, "employee.db");
        }

        // ========================================
        // DATABASE CONNECTION CONFIGURATION
        // This tells Entity Framework to use SQLite
        // ========================================
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var dbPath = GetDatabasePath();

                // Optional: Print database location to console for verification
                Console.WriteLine($"Database location: {dbPath}");

                // Configure to use SQLite database
                // UseSqlite is the method that says "use SQLite, not SQL Server or MySQL"
                optionsBuilder.UseSqlite($"Data Source={dbPath}");
            }
        }

        // ========================================
        // ENSURE ADMIN EXISTS
        // This guarantees admin can always log in
        // Call this when app starts or before login
        // ========================================
        /// <summary>
        /// Ensures the default admin account exists in the database
        /// Call this method when the application starts or before login
        /// </summary>
        public void EnsureAdminExists()
        {
            // Check if admin user exists
            var adminUser = Users.FirstOrDefault(u => u.Username == "admin");

            if (adminUser == null)
            {
                Console.WriteLine("⚠ Admin account not found. Creating...");

                // First ensure the admin employee exists
                var adminEmployee = Employees.FirstOrDefault(e => e.Id == 1);
                if (adminEmployee == null)
                {
                    // Ensure IT department exists
                    var itDept = Departments.FirstOrDefault(d => d.Id == 1);
                    if (itDept == null)
                    {
                        Departments.Add(new Department { Id = 1, Name = "IT" });
                        SaveChanges();
                    }

                    adminEmployee = new Employee
                    {
                        Id = 1,
                        FirstName = "Admin",
                        LastName = "User",
                        Email = "admin@nzftc.com",
                        PhoneNumber = "021-123-4567",
                        JobTitle = "System Administrator",
                        DepartmentId = 1,
                        HireDate = new DateTime(2020, 1, 1),
                        Salary = 80000m,
                        TaxRate = 30m,
                        AnnualLeaveBalance = 20,
                        SickLeaveBalance = 10,
                        IsActive = true
                    };
                    Employees.Add(adminEmployee);
                    SaveChanges();
                }

                // Create admin user
                adminUser = new User
                {
                    Username = "admin",
                    Password = "admin123",
                    Role = "Admin",
                    EmployeeId = 1,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };
                Users.Add(adminUser);
                SaveChanges();

                // Ensure Admin role exists and is assigned
                var adminRole = Roles.FirstOrDefault(r => r.Id == 1);
                if (adminRole == null)
                {
                    adminRole = new Role { Id = 1, Name = "Admin" };
                    Roles.Add(adminRole);
                    SaveChanges();
                }

                var userRole = UserRoles.FirstOrDefault(ur => ur.UserId == adminUser.Id && ur.RoleId == 1);
                if (userRole == null)
                {
                    UserRoles.Add(new UserRole { UserId = adminUser.Id, RoleId = 1 });
                    SaveChanges();
                }

                Console.WriteLine("✓ Admin account created successfully");
                Console.WriteLine("  Username: admin");
                Console.WriteLine("  Password: admin123");
            }
            else
            {
                Console.WriteLine("✓ Admin account exists and is ready");
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

            // Configure Training.EmployeeId -> Employee relationship
            modelBuilder.Entity<Training>()
                .HasOne(t => t.Employee)
                .WithMany()
                .HasForeignKey(t => t.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Training.SignedOffByUserId -> User relationship
            modelBuilder.Entity<Training>()
                .HasOne(t => t.SignedOffByUser)
                .WithMany()
                .HasForeignKey(t => t.SignedOffByUserId)
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
                new Role { Id = 2, Name = "Employee" },
                new Role { Id = 3, Name = "Workplace Trainer" }
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

            // Create a default admin user account - FIXED: Use fixed date
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Username = "admin",
                    Password = "admin123", // Stored in plain text (for demonstration)
                    Role = "Admin",
                    EmployeeId = 1,
                    IsActive = true,
                    CreatedDate = new DateTime(2024, 1, 1) // FIXED: Use fixed date instead of DateTime.Now
                }
            );

            // Assign the Admin role to the default admin user via the join table
            modelBuilder.Entity<UserRole>().HasData(
                new UserRole { UserId = 1, RoleId = 1 }
            );

            // Seed comprehensive New Zealand public holidays for 2025 and 2026
            // This provides a full calendar of holidays including national public holidays,
            // regional anniversary days, and company-specific holidays
            modelBuilder.Entity<Holiday>().HasData(
                // ========================================
                // 2025 NEW ZEALAND PUBLIC HOLIDAYS
                // ========================================

                // New Year's Day - January 1, 2025
                new Holiday
                {
                    Id = 1,
                    Name = "New Year's Day",
                    Date = new DateTime(2025, 1, 1),
                    Type = "Public",
                    Description = "First day of the new year - a nationwide public holiday in New Zealand",
                    IsRecurring = true,
                    CreatedDate = new DateTime(2024, 1, 1),
                    CreatedByUserId = 1
                },

                // Day after New Year's Day - January 2, 2025
                new Holiday
                {
                    Id = 2,
                    Name = "Day after New Year's Day",
                    Date = new DateTime(2025, 1, 2),
                    Type = "Public",
                    Description = "Second day of new year celebrations - public holiday in New Zealand",
                    IsRecurring = true,
                    CreatedDate = new DateTime(2024, 1, 1),
                    CreatedByUserId = 1
                },

                // Waitangi Day - February 6, 2025
                new Holiday
                {
                    Id = 3,
                    Name = "Waitangi Day",
                    Date = new DateTime(2025, 2, 6),
                    Type = "Public",
                    Description = "New Zealand's national day commemorating the signing of the Treaty of Waitangi in 1840",
                    IsRecurring = true,
                    CreatedDate = new DateTime(2024, 1, 1),
                    CreatedByUserId = 1
                },

                // Good Friday - April 18, 2025
                new Holiday
                {
                    Id = 4,
                    Name = "Good Friday",
                    Date = new DateTime(2025, 4, 18),
                    Type = "Public",
                    Description = "Christian holiday commemorating the crucifixion of Jesus Christ - public holiday",
                    IsRecurring = false,
                    CreatedDate = new DateTime(2024, 1, 1),
                    CreatedByUserId = 1
                },

                // Easter Monday - April 21, 2025
                new Holiday
                {
                    Id = 5,
                    Name = "Easter Monday",
                    Date = new DateTime(2025, 4, 21),
                    Type = "Public",
                    Description = "Day after Easter Sunday - recognized as a public holiday in New Zealand",
                    IsRecurring = false,
                    CreatedDate = new DateTime(2024, 1, 1),
                    CreatedByUserId = 1
                },

                // ANZAC Day - April 25, 2025
                new Holiday
                {
                    Id = 6,
                    Name = "ANZAC Day",
                    Date = new DateTime(2025, 4, 25),
                    Type = "Public",
                    Description = "Remembrance day for Australian and New Zealand Army Corps members who served in wars and conflicts",
                    IsRecurring = true,
                    CreatedDate = new DateTime(2024, 1, 1),
                    CreatedByUserId = 1
                },

                // Queen's Birthday - June 2, 2025
                new Holiday
                {
                    Id = 7,
                    Name = "Queen's Birthday",
                    Date = new DateTime(2025, 6, 2),
                    Type = "Public",
                    Description = "Official celebration of the monarch's birthday - observed on first Monday in June",
                    IsRecurring = false,
                    CreatedDate = new DateTime(2024, 1, 1),
                    CreatedByUserId = 1
                },

                // Matariki - June 20, 2025
                new Holiday
                {
                    Id = 8,
                    Name = "Matariki",
                    Date = new DateTime(2025, 6, 20),
                    Type = "Public",
                    Description = "Maori New Year celebrating the rise of the Matariki star cluster - newest public holiday in NZ",
                    IsRecurring = false,
                    CreatedDate = new DateTime(2024, 1, 1),
                    CreatedByUserId = 1
                },

                // Labour Day - October 27, 2025
                new Holiday
                {
                    Id = 9,
                    Name = "Labour Day",
                    Date = new DateTime(2025, 10, 27),
                    Type = "Public",
                    Description = "Celebrates workers' rights and the eight-hour working day - fourth Monday in October",
                    IsRecurring = false,
                    CreatedDate = new DateTime(2024, 1, 1),
                    CreatedByUserId = 1
                },

                // Christmas Day - December 25, 2025
                new Holiday
                {
                    Id = 10,
                    Name = "Christmas Day",
                    Date = new DateTime(2025, 12, 25),
                    Type = "Public",
                    Description = "Christian celebration of the birth of Jesus Christ - major public holiday in New Zealand",
                    IsRecurring = true,
                    CreatedDate = new DateTime(2024, 1, 1),
                    CreatedByUserId = 1
                },

                // Boxing Day - December 26, 2025
                new Holiday
                {
                    Id = 11,
                    Name = "Boxing Day",
                    Date = new DateTime(2025, 12, 26),
                    Type = "Public",
                    Description = "Day after Christmas - traditionally for giving to those in need, now a shopping holiday",
                    IsRecurring = true,
                    CreatedDate = new DateTime(2024, 1, 1),
                    CreatedByUserId = 1
                },

                // ========================================
                // 2025 REGIONAL ANNIVERSARY DAYS
                // These are observed in specific regions of New Zealand
                // ========================================

                // Wellington Anniversary Day - January 27, 2025
                new Holiday
                {
                    Id = 12,
                    Name = "Wellington Anniversary Day",
                    Date = new DateTime(2025, 1, 27),
                    Type = "Public",
                    Description = "Regional holiday for Wellington province - commemorates founding of the city",
                    IsRecurring = false,
                    CreatedDate = new DateTime(2024, 1, 1),
                    CreatedByUserId = 1
                },

                // Auckland Anniversary Day - January 27, 2025
                new Holiday
                {
                    Id = 13,
                    Name = "Auckland Anniversary Day",
                    Date = new DateTime(2025, 1, 27),
                    Type = "Public",
                    Description = "Regional holiday for Auckland province - celebrates the arrival of first Governor",
                    IsRecurring = false,
                    CreatedDate = new DateTime(2024, 1, 1),
                    CreatedByUserId = 1
                },

                // Canterbury Anniversary Day - November 14, 2025
                new Holiday
                {
                    Id = 14,
                    Name = "Canterbury Anniversary Day",
                    Date = new DateTime(2025, 11, 14),
                    Type = "Public",
                    Description = "Regional holiday for Canterbury - celebrates the arrival of the first Canterbury settlers",
                    IsRecurring = false,
                    CreatedDate = new DateTime(2024, 1, 1),
                    CreatedByUserId = 1
                },

                // ========================================
                // 2025 COMPANY HOLIDAYS
                // These are specific to NZFTC company
                // ========================================

                // Company Founding Day - March 15, 2025
                new Holiday
                {
                    Id = 15,
                    Name = "NZFTC Founding Day",
                    Date = new DateTime(2025, 3, 15),
                    Type = "Company",
                    Description = "Celebrates the founding anniversary of New Zealand Freight Transport Company",
                    IsRecurring = true,
                    CreatedDate = new DateTime(2024, 1, 1),
                    CreatedByUserId = 1
                },

                // Summer Shutdown Start - December 22, 2025
                new Holiday
                {
                    Id = 16,
                    Name = "Summer Shutdown - Week 1",
                    Date = new DateTime(2025, 12, 22),
                    Type = "Company",
                    Description = "Company closes for summer break - first week of annual shutdown period",
                    IsRecurring = false,
                    CreatedDate = new DateTime(2024, 1, 1),
                    CreatedByUserId = 1
                },

                // Summer Shutdown Continued - December 29, 2025
                new Holiday
                {
                    Id = 17,
                    Name = "Summer Shutdown - Week 2",
                    Date = new DateTime(2025, 12, 29),
                    Type = "Company",
                    Description = "Company remains closed for summer break - second week of annual shutdown period",
                    IsRecurring = false,
                    CreatedDate = new DateTime(2024, 1, 1),
                    CreatedByUserId = 1
                },

                // ========================================
                // 2026 NEW ZEALAND PUBLIC HOLIDAYS
                // Planning ahead for next year's calendar
                // ========================================

                // New Year's Day - January 1, 2026
                new Holiday
                {
                    Id = 18,
                    Name = "New Year's Day",
                    Date = new DateTime(2026, 1, 1),
                    Type = "Public",
                    Description = "First day of the new year - a nationwide public holiday in New Zealand",
                    IsRecurring = true,
                    CreatedDate = new DateTime(2024, 1, 1),
                    CreatedByUserId = 1
                },

                // Day after New Year's Day - January 2, 2026
                new Holiday
                {
                    Id = 19,
                    Name = "Day after New Year's Day",
                    Date = new DateTime(2026, 1, 2),
                    Type = "Public",
                    Description = "Second day of new year celebrations - public holiday in New Zealand",
                    IsRecurring = true,
                    CreatedDate = new DateTime(2024, 1, 1),
                    CreatedByUserId = 1
                },

                // Waitangi Day - February 6, 2026
                new Holiday
                {
                    Id = 20,
                    Name = "Waitangi Day",
                    Date = new DateTime(2026, 2, 6),
                    Type = "Public",
                    Description = "New Zealand's national day commemorating the signing of the Treaty of Waitangi in 1840",
                    IsRecurring = true,
                    CreatedDate = new DateTime(2024, 1, 1),
                    CreatedByUserId = 1
                },

                // Good Friday - April 3, 2026
                new Holiday
                {
                    Id = 21,
                    Name = "Good Friday",
                    Date = new DateTime(2026, 4, 3),
                    Type = "Public",
                    Description = "Christian holiday commemorating the crucifixion of Jesus Christ - public holiday",
                    IsRecurring = false,
                    CreatedDate = new DateTime(2024, 1, 1),
                    CreatedByUserId = 1
                },

                // Easter Monday - April 6, 2026
                new Holiday
                {
                    Id = 22,
                    Name = "Easter Monday",
                    Date = new DateTime(2026, 4, 6),
                    Type = "Public",
                    Description = "Day after Easter Sunday - recognized as a public holiday in New Zealand",
                    IsRecurring = false,
                    CreatedDate = new DateTime(2024, 1, 1),
                    CreatedByUserId = 1
                },

                // ANZAC Day - April 25, 2026
                new Holiday
                {
                    Id = 23,
                    Name = "ANZAC Day",
                    Date = new DateTime(2026, 4, 25),
                    Type = "Public",
                    Description = "Remembrance day for Australian and New Zealand Army Corps members who served in wars and conflicts",
                    IsRecurring = true,
                    CreatedDate = new DateTime(2024, 1, 1),
                    CreatedByUserId = 1
                },

                // Queen's Birthday - June 1, 2026
                new Holiday
                {
                    Id = 24,
                    Name = "Queen's Birthday",
                    Date = new DateTime(2026, 6, 1),
                    Type = "Public",
                    Description = "Official celebration of the monarch's birthday - observed on first Monday in June",
                    IsRecurring = false,
                    CreatedDate = new DateTime(2024, 1, 1),
                    CreatedByUserId = 1
                },

                // Matariki - July 10, 2026 (estimated)
                new Holiday
                {
                    Id = 25,
                    Name = "Matariki",
                    Date = new DateTime(2026, 7, 10),
                    Type = "Public",
                    Description = "Maori New Year celebrating the rise of the Matariki star cluster - newest public holiday in NZ",
                    IsRecurring = false,
                    CreatedDate = new DateTime(2024, 1, 1),
                    CreatedByUserId = 1
                },

                // Labour Day - October 26, 2026
                new Holiday
                {
                    Id = 26,
                    Name = "Labour Day",
                    Date = new DateTime(2026, 10, 26),
                    Type = "Public",
                    Description = "Celebrates workers' rights and the eight-hour working day - fourth Monday in October",
                    IsRecurring = false,
                    CreatedDate = new DateTime(2024, 1, 1),
                    CreatedByUserId = 1
                },

                // Christmas Day - December 25, 2026
                new Holiday
                {
                    Id = 27,
                    Name = "Christmas Day",
                    Date = new DateTime(2026, 12, 25),
                    Type = "Public",
                    Description = "Christian celebration of the birth of Jesus Christ - major public holiday in New Zealand",
                    IsRecurring = true,
                    CreatedDate = new DateTime(2024, 1, 1),
                    CreatedByUserId = 1
                },

                // Boxing Day - December 26, 2026
                new Holiday
                {
                    Id = 28,
                    Name = "Boxing Day",
                    Date = new DateTime(2026, 12, 26),
                    Type = "Public",
                    Description = "Day after Christmas - traditionally for giving to those in need, now a shopping holiday",
                    IsRecurring = true,
                    CreatedDate = new DateTime(2024, 1, 1),
                    CreatedByUserId = 1
                }
            );

            // Seed random test employees
            modelBuilder.Entity<Employee>().HasData(
                new Employee { Id = 2, FirstName = "James", LastName = "Smith", Email = "james.smith@nzftc.co.nz", PhoneNumber = "022-345-6789", JobTitle = "Developer", DepartmentId = 1, HireDate = new DateTime(2023, 6, 15), Salary = 75000, TaxRate = 17.5m, AnnualLeaveBalance = 20, SickLeaveBalance = 10, IsActive = true },
                new Employee { Id = 3, FirstName = "Emma", LastName = "Johnson", Email = "emma.johnson@nzftc.co.nz", PhoneNumber = "022-456-7890", JobTitle = "Manager", DepartmentId = 2, HireDate = new DateTime(2022, 9, 20), Salary = 85000, TaxRate = 17.5m, AnnualLeaveBalance = 20, SickLeaveBalance = 10, IsActive = true },
                new Employee { Id = 4, FirstName = "Oliver", LastName = "Williams", Email = "oliver.williams@nzftc.co.nz", PhoneNumber = "022-567-8901", JobTitle = "Analyst", DepartmentId = 3, HireDate = new DateTime(2024, 1, 10), Salary = 65000, TaxRate = 17.5m, AnnualLeaveBalance = 20, SickLeaveBalance = 10, IsActive = true },
                new Employee { Id = 5, FirstName = "Sophia", LastName = "Brown", Email = "sophia.brown@nzftc.co.nz", PhoneNumber = "022-678-9012", JobTitle = "Coordinator", DepartmentId = 1, HireDate = new DateTime(2023, 4, 5), Salary = 55000, TaxRate = 17.5m, AnnualLeaveBalance = 20, SickLeaveBalance = 10, IsActive = true },
                new Employee { Id = 6, FirstName = "William", LastName = "Jones", Email = "william.jones@nzftc.co.nz", PhoneNumber = "022-789-0123", JobTitle = "Specialist", DepartmentId = 2, HireDate = new DateTime(2023, 7, 12), Salary = 70000, TaxRate = 17.5m, AnnualLeaveBalance = 20, SickLeaveBalance = 10, IsActive = true },
                new Employee { Id = 7, FirstName = "Ava", LastName = "Garcia", Email = "ava.garcia@nzftc.co.nz", PhoneNumber = "022-890-1234", JobTitle = "Developer", DepartmentId = 1, HireDate = new DateTime(2024, 3, 1), Salary = 72000, TaxRate = 17.5m, AnnualLeaveBalance = 20, SickLeaveBalance = 10, IsActive = true },
                new Employee { Id = 8, FirstName = "Lucas", LastName = "Miller", Email = "lucas.miller@nzftc.co.nz", PhoneNumber = "022-901-2345", JobTitle = "Associate", DepartmentId = 3, HireDate = new DateTime(2022, 12, 8), Salary = 60000, TaxRate = 17.5m, AnnualLeaveBalance = 20, SickLeaveBalance = 10, IsActive = true },
                new Employee { Id = 9, FirstName = "Isabella", LastName = "Davis", Email = "isabella.davis@nzftc.co.nz", PhoneNumber = "022-012-3456", JobTitle = "Consultant", DepartmentId = 2, HireDate = new DateTime(2023, 8, 25), Salary = 78000, TaxRate = 17.5m, AnnualLeaveBalance = 20, SickLeaveBalance = 10, IsActive = true },
                new Employee { Id = 10, FirstName = "Mason", LastName = "Rodriguez", Email = "mason.rodriguez@nzftc.co.nz", PhoneNumber = "022-123-4567", JobTitle = "Manager", DepartmentId = 1, HireDate = new DateTime(2023, 5, 18), Salary = 88000, TaxRate = 17.5m, AnnualLeaveBalance = 20, SickLeaveBalance = 10, IsActive = true },
                new Employee { Id = 11, FirstName = "Mia", LastName = "Martinez", Email = "mia.martinez@nzftc.co.nz", PhoneNumber = "022-234-5678", JobTitle = "Analyst", DepartmentId = 3, HireDate = new DateTime(2023, 11, 30), Salary = 67000, TaxRate = 17.5m, AnnualLeaveBalance = 20, SickLeaveBalance = 10, IsActive = true },
                new Employee { Id = 12, FirstName = "Sarah", LastName = "Wilson", Email = "sarah.wilson@nzftc.co.nz", PhoneNumber = "022-345-6790", JobTitle = "Workplace Trainer", DepartmentId = 2, HireDate = new DateTime(2021, 3, 15), Salary = 72000, TaxRate = 17.5m, AnnualLeaveBalance = 20, SickLeaveBalance = 10, IsActive = true }
            );

            // Seed user accounts for test employees
            modelBuilder.Entity<User>().HasData(
                new User { Id = 2, Username = "james.smith", Password = "password123", Role = "Employee", EmployeeId = 2, IsActive = true, CreatedDate = new DateTime(2023, 6, 15) },
                new User { Id = 3, Username = "emma.johnson", Password = "password123", Role = "Employee", EmployeeId = 3, IsActive = true, CreatedDate = new DateTime(2022, 9, 20) },
                new User { Id = 4, Username = "oliver.williams", Password = "password123", Role = "Employee", EmployeeId = 4, IsActive = true, CreatedDate = new DateTime(2024, 1, 10) },
                new User { Id = 5, Username = "sophia.brown", Password = "password123", Role = "Employee", EmployeeId = 5, IsActive = true, CreatedDate = new DateTime(2023, 4, 5) },
                new User { Id = 6, Username = "william.jones", Password = "password123", Role = "Employee", EmployeeId = 6, IsActive = true, CreatedDate = new DateTime(2023, 7, 12) },
                new User { Id = 7, Username = "ava.garcia", Password = "password123", Role = "Employee", EmployeeId = 7, IsActive = true, CreatedDate = new DateTime(2024, 3, 1) },
                new User { Id = 8, Username = "lucas.miller", Password = "password123", Role = "Employee", EmployeeId = 8, IsActive = true, CreatedDate = new DateTime(2022, 12, 8) },
                new User { Id = 9, Username = "isabella.davis", Password = "password123", Role = "Employee", EmployeeId = 9, IsActive = true, CreatedDate = new DateTime(2023, 8, 25) },
                new User { Id = 10, Username = "mason.rodriguez", Password = "password123", Role = "Employee", EmployeeId = 10, IsActive = true, CreatedDate = new DateTime(2023, 5, 18) },
                new User { Id = 11, Username = "mia.martinez", Password = "password123", Role = "Employee", EmployeeId = 11, IsActive = true, CreatedDate = new DateTime(2023, 11, 30) },
                new User { Id = 12, Username = "trainer", Password = "password123", Role = "Workplace Trainer", EmployeeId = 12, IsActive = true, CreatedDate = new DateTime(2021, 3, 15) }
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
                new UserRole { UserId = 11, RoleId = 2 },
                new UserRole { UserId = 12, RoleId = 3 }
            );

            // Seed training records for testing
            modelBuilder.Entity<Training>().HasData(
                new Training { Id = 1, EmployeeId = 1, TrainingType = "Ethics Training", Status = "Completed", CompletedDate = new DateTime(2024, 1, 15), SignedOffByUserId = 1, Notes = "Initial admin training" },
                new Training { Id = 2, EmployeeId = 1, TrainingType = "Induction", Status = "Completed", CompletedDate = new DateTime(2024, 1, 10), SignedOffByUserId = 1, Notes = "Company induction completed" },
                new Training { Id = 3, EmployeeId = 2, TrainingType = "Induction", Status = "Completed", CompletedDate = new DateTime(2023, 6, 16), SignedOffByUserId = 1, Notes = "New hire induction" },
                new Training { Id = 4, EmployeeId = 2, TrainingType = "Health and Safety", Status = "Completed", CompletedDate = new DateTime(2023, 6, 20), SignedOffByUserId = 1, Notes = "Workplace safety training" },
                new Training { Id = 5, EmployeeId = 2, TrainingType = "Fire Safety", Status = "In Progress", Notes = "Scheduled for next week" },
                new Training { Id = 6, EmployeeId = 3, TrainingType = "Induction", Status = "Completed", CompletedDate = new DateTime(2022, 9, 21), SignedOffByUserId = 1 },
                new Training { Id = 7, EmployeeId = 3, TrainingType = "Ethics Training", Status = "Completed", CompletedDate = new DateTime(2022, 10, 5), SignedOffByUserId = 1 },
                new Training { Id = 8, EmployeeId = 3, TrainingType = "Data Privacy", Status = "Not Started", Notes = "To be scheduled" },
                new Training { Id = 9, EmployeeId = 4, TrainingType = "Induction", Status = "Completed", CompletedDate = new DateTime(2024, 1, 11), SignedOffByUserId = 1 },
                new Training { Id = 10, EmployeeId = 4, TrainingType = "First Aid", Status = "In Progress", Notes = "Attending course next month" },
                new Training { Id = 11, EmployeeId = 5, TrainingType = "Induction", Status = "Completed", CompletedDate = new DateTime(2023, 4, 6), SignedOffByUserId = 1 },
                new Training { Id = 12, EmployeeId = 5, TrainingType = "Workplace Harassment", Status = "Completed", CompletedDate = new DateTime(2023, 5, 15), SignedOffByUserId = 1 },
                new Training { Id = 13, EmployeeId = 5, TrainingType = "Ethics Training", Status = "Not Started" },
                new Training { Id = 14, EmployeeId = 6, TrainingType = "Induction", Status = "Completed", CompletedDate = new DateTime(2023, 7, 13), SignedOffByUserId = 1 },
                new Training { Id = 15, EmployeeId = 6, TrainingType = "Health and Safety", Status = "Completed", CompletedDate = new DateTime(2023, 7, 20), SignedOffByUserId = 1 },
                new Training { Id = 16, EmployeeId = 7, TrainingType = "Induction", Status = "Not Started", Notes = "New employee - schedule induction" },
                new Training { Id = 17, EmployeeId = 8, TrainingType = "Induction", Status = "Completed", CompletedDate = new DateTime(2022, 12, 9), SignedOffByUserId = 1 },
                new Training { Id = 18, EmployeeId = 8, TrainingType = "Ethics Training", Status = "Completed", CompletedDate = new DateTime(2023, 1, 20), SignedOffByUserId = 1 },
                new Training { Id = 19, EmployeeId = 8, TrainingType = "Fire Safety", Status = "Completed", CompletedDate = new DateTime(2023, 2, 10), SignedOffByUserId = 1 },
                new Training { Id = 20, EmployeeId = 9, TrainingType = "Induction", Status = "Completed", CompletedDate = new DateTime(2023, 8, 26), SignedOffByUserId = 1 },
                new Training { Id = 21, EmployeeId = 9, TrainingType = "Data Privacy", Status = "In Progress", Notes = "Online course in progress" },
                new Training { Id = 22, EmployeeId = 10, TrainingType = "Induction", Status = "Completed", CompletedDate = new DateTime(2023, 5, 19), SignedOffByUserId = 1 },
                new Training { Id = 23, EmployeeId = 10, TrainingType = "Health and Safety", Status = "Completed", CompletedDate = new DateTime(2023, 6, 1), SignedOffByUserId = 1 },
                new Training { Id = 24, EmployeeId = 10, TrainingType = "First Aid", Status = "Completed", CompletedDate = new DateTime(2023, 9, 15), SignedOffByUserId = 12, Notes = "Signed off by workplace trainer" },
                new Training { Id = 25, EmployeeId = 11, TrainingType = "Induction", Status = "Completed", CompletedDate = new DateTime(2023, 12, 1), SignedOffByUserId = 1 },
                new Training { Id = 26, EmployeeId = 11, TrainingType = "Ethics Training", Status = "Not Started", Notes = "Scheduled for Q1 2025" }
            );

            // Seed sample payslips for recent weeks
            modelBuilder.Entity<Payslip>().HasData(
                new Payslip { Id = 1, EmployeeId = 1, PayPeriodStart = new DateTime(2025, 10, 27), PayPeriodEnd = new DateTime(2025, 11, 2), GrossSalary = 1538.46m, TaxDeduction = 461.54m, NetSalary = 1076.92m, GeneratedDate = new DateTime(2025, 11, 3), GeneratedByUserId = 1 },
                new Payslip { Id = 2, EmployeeId = 2, PayPeriodStart = new DateTime(2025, 10, 27), PayPeriodEnd = new DateTime(2025, 11, 2), GrossSalary = 1442.31m, TaxDeduction = 252.40m, NetSalary = 1189.91m, GeneratedDate = new DateTime(2025, 11, 3), GeneratedByUserId = 1 },
                new Payslip { Id = 3, EmployeeId = 3, PayPeriodStart = new DateTime(2025, 10, 27), PayPeriodEnd = new DateTime(2025, 11, 2), GrossSalary = 1634.62m, TaxDeduction = 286.06m, NetSalary = 1348.56m, GeneratedDate = new DateTime(2025, 11, 3), GeneratedByUserId = 1 },
                new Payslip { Id = 4, EmployeeId = 4, PayPeriodStart = new DateTime(2025, 10, 27), PayPeriodEnd = new DateTime(2025, 11, 2), GrossSalary = 1230.77m, TaxDeduction = 215.38m, NetSalary = 1015.39m, GeneratedDate = new DateTime(2025, 11, 3), GeneratedByUserId = 1 },
                new Payslip { Id = 5, EmployeeId = 5, PayPeriodStart = new DateTime(2025, 10, 27), PayPeriodEnd = new DateTime(2025, 11, 2), GrossSalary = 1057.69m, TaxDeduction = 185.10m, NetSalary = 872.59m, GeneratedDate = new DateTime(2025, 11, 3), GeneratedByUserId = 1 },
                new Payslip { Id = 6, EmployeeId = 1, PayPeriodStart = new DateTime(2025, 10, 20), PayPeriodEnd = new DateTime(2025, 10, 26), GrossSalary = 1538.46m, TaxDeduction = 461.54m, NetSalary = 1076.92m, GeneratedDate = new DateTime(2025, 10, 27), GeneratedByUserId = 1 },
                new Payslip { Id = 7, EmployeeId = 2, PayPeriodStart = new DateTime(2025, 10, 20), PayPeriodEnd = new DateTime(2025, 10, 26), GrossSalary = 1442.31m, TaxDeduction = 252.40m, NetSalary = 1189.91m, GeneratedDate = new DateTime(2025, 10, 27), GeneratedByUserId = 1 },
                new Payslip { Id = 8, EmployeeId = 3, PayPeriodStart = new DateTime(2025, 10, 20), PayPeriodEnd = new DateTime(2025, 10, 26), GrossSalary = 1716.35m, TaxDeduction = 300.36m, NetSalary = 1415.99m, GeneratedDate = new DateTime(2025, 10, 27), GeneratedByUserId = 1 },
                new Payslip { Id = 9, EmployeeId = 6, PayPeriodStart = new DateTime(2025, 10, 27), PayPeriodEnd = new DateTime(2025, 11, 2), GrossSalary = 1346.15m, TaxDeduction = 235.58m, NetSalary = 1110.57m, GeneratedDate = new DateTime(2025, 11, 3), GeneratedByUserId = 1 },
                new Payslip { Id = 10, EmployeeId = 7, PayPeriodStart = new DateTime(2025, 10, 27), PayPeriodEnd = new DateTime(2025, 11, 2), GrossSalary = 1384.62m, TaxDeduction = 242.31m, NetSalary = 1142.31m, GeneratedDate = new DateTime(2025, 11, 3), GeneratedByUserId = 1 }
            );
        }
    }
}