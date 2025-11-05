using System;

namespace NZFTC_EmployeeSystem.Models
{
    public class Payslip
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public DateTime PayPeriodStart { get; set; }
        public DateTime PayPeriodEnd { get; set; }
        public decimal GrossSalary { get; set; }
        public decimal TaxDeduction { get; set; }
        public decimal NetSalary { get; set; }
        public DateTime GeneratedDate { get; set; }
        public int GeneratedByUserId { get; set; }

        // Navigation properties
        public Employee Employee { get; set; }
        public User GeneratedByUser { get; set; }
    }
}
