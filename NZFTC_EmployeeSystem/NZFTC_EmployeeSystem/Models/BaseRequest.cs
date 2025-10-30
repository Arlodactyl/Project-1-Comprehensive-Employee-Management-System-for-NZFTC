using System;

namespace NZFTC_EmployeeSystem.Models
{
    /// <summary>
    /// BASE CLASS for all request types (Leave, Grievance, etc.)
    /// This demonstrates INHERITANCE and POLYMORPHISM
    /// </summary>
    public abstract class BaseRequest
    {
        // Common properties for all requests
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }
        public string Status { get; set; }
        public DateTime RequestDate { get; set; }

        // ========================================
        // POLYMORPHISM EXAMPLE 1: Virtual Method
        // Virtual methods CAN be overridden by child classes
        // ========================================
        /// <summary>
        /// Checks if the request can be approved
        /// This method can be overridden by derived classes to add custom logic
        /// </summary>
        public virtual bool CanBeApproved()
        {
            // Default behavior: can only approve pending requests
            return Status == "Pending";
        }

        // ========================================
        // POLYMORPHISM EXAMPLE 2: Abstract Method
        // Abstract methods MUST be implemented by child classes
        // ========================================
        /// <summary>
        /// Gets a summary of the request
        /// Each request type will provide its own implementation
        /// </summary>
        public abstract string GetRequestSummary();

        // ========================================
        // POLYMORPHISM EXAMPLE 3: Virtual Method
        // Another virtual method that can be customized
        // ========================================
        /// <summary>
        /// Gets the priority level of the request
        /// Default is "Normal", but child classes can override
        /// </summary>
        public virtual string GetPriorityLevel()
        {
            return "Normal";
        }

        // ========================================
        // POLYMORPHISM EXAMPLE 4: Virtual Method
        // Gets request type name - can be overridden
        // ========================================
        /// <summary>
        /// Returns the type of request
        /// Each child class can provide its own implementation
        /// </summary>
        public virtual string GetRequestType()
        {
            return "General Request";
        }
    }
}