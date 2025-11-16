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
                // Admin account not found - creating silently without console output

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

                // Create admin user with HASHED password
                // Password is securely hashed using BCrypt
                adminUser = new User
                {
                    Username = "admin",
                    Password = PasswordHasher.HashPassword("admin123"), // BCrypt hashed
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

            // Create a default admin user account with HASHED password
            // Password is securely hashed using BCrypt - see documentation for credentials
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Username = "admin",
                    Password = PasswordHasher.HashPassword("admin123"), // BCrypt hashed
                    Role = "Admin",
                    EmployeeId = 1,
                    IsActive = true,
                    CreatedDate = new DateTime(2024, 1, 1)
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


            // Seed essential employees (trainer only - admin is created via EnsureAdminExists)
            // Additional employees can be added through the application's employee management interface
            modelBuilder.Entity<Employee>().HasData(
                new Employee { Id = 12, FirstName = "Sarah", LastName = "Wilson", Email = "sarah.wilson@nzftc.co.nz", PhoneNumber = "022-345-6790", JobTitle = "Workplace Trainer", DepartmentId = 2, HireDate = new DateTime(2021, 3, 15), Salary = 72000, TaxRate = 17.5m, AnnualLeaveBalance = 20, SickLeaveBalance = 10, IsActive = true }
            );

            // Seed user accounts for test employees with HASHED passwords

            // Seed trainer user account (admin is created via EnsureAdminExists)
            // Password is securely hashed using BCrypt - see documentation for credentials
            modelBuilder.Entity<User>().HasData(
                new User { Id = 12, Username = "trainer", Password = PasswordHasher.HashPassword("password123"), Role = "Workplace Trainer", EmployeeId = 12, IsActive = true, CreatedDate = new DateTime(2021, 3, 15) }
            );


            // Assign Workplace Trainer role to trainer user
            modelBuilder.Entity<UserRole>().HasData(
                new UserRole { UserId = 12, RoleId = 3 }
            );


            // Seed training records for admin only
            // Additional training records can be added through the application
            modelBuilder.Entity<Training>().HasData(
                new Training { Id = 1, EmployeeId = 1, TrainingType = "Ethics Training", Status = "Completed", CompletedDate = new DateTime(2024, 1, 15), SignedOffByUserId = 1, Notes = "Initial admin training" },
                new Training { Id = 2, EmployeeId = 1, TrainingType = "Induction", Status = "Completed", CompletedDate = new DateTime(2024, 1, 10), SignedOffByUserId = 1, Notes = "Company induction completed" }
            );


            // Seed sample payslips for admin only
            // Additional payslips will be generated through the payroll system
            modelBuilder.Entity<Payslip>().HasData(
                new Payslip { Id = 1, EmployeeId = 1, PayPeriodStart = new DateTime(2025, 10, 27), PayPeriodEnd = new DateTime(2025, 11, 2), GrossSalary = 1538.46m, TaxDeduction = 461.54m, NetSalary = 1076.92m, GeneratedDate = new DateTime(2025, 11, 3), GeneratedByUserId = 1 },
                new Payslip { Id = 2, EmployeeId = 1, PayPeriodStart = new DateTime(2025, 10, 20), PayPeriodEnd = new DateTime(2025, 10, 26), GrossSalary = 1538.46m, TaxDeduction = 461.54m, NetSalary = 1076.92m, GeneratedDate = new DateTime(2025, 10, 27), GeneratedByUserId = 1 }
            );
        }
    }
}