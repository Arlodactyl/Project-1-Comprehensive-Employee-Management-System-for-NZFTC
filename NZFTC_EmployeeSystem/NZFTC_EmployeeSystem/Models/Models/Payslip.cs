using System;

namespace NZFTC_EmployeeSystem.Models
{
    // This class represents a payslip
    // Demonstrates ENCAPSULATION and calculation logic
    public class Payslip
    {
        // Primary key
        public int Id { get; set; }

        // Foreign key to Employee
        public int EmployeeId { get; set; }

        // Navigation property
        public Employee? Employee { get; set; }

        // Pay period
        public DateTime PayPeriodStart { get; set; }
        public DateTime PayPeriodEnd { get; set; }

        // Salary calculations
        public decimal GrossSalary { get; set; }
        public decimal TaxDeduction { get; set; }
        public decimal NetSalary { get; set; }

        // Metadata
        public DateTime GeneratedDate { get; set; } = DateTime.Now;
        public int GeneratedByUserId { get; set; }
    }
}