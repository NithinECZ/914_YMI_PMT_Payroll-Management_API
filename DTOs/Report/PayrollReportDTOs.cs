using System;
using System.Collections.Generic;

namespace YMI_PMT_PayrollManagement_API.DTOs.Report
{
    public class PayrollReportFilterDTO
    {
        public string Category { get; set; } = "CL";
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? EmployeeId { get; set; }
        public string? SkillCategory { get; set; }
        public string? Vendor { get; set; }
    }

    public class PayrollReportRowDTO
    {
        public int Id { get; set; }
        public string EmployeeId { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string CategoryCode { get; set; } = string.Empty;
        public string SkillCategory { get; set; } = string.Empty;
        public string Vendor { get; set; } = string.Empty;
        public string VendorId { get; set; } = string.Empty;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string Designation { get; set; } = string.Empty;
        public DateTime? DateOfJoining { get; set; }
        public DateTime? DateOfLeaving { get; set; }

        // Statutory & Identification
        public string PfNo { get; set; } = string.Empty;
        public string EsiNo { get; set; } = string.Empty;
        public string PanNo { get; set; } = string.Empty;
        public string UanNo { get; set; } = string.Empty;

        // Attendance & Working Days
        public int PeriodDays { get; set; }
        public decimal PayDaysFull { get; set; }
        public decimal PayDaysHalf { get; set; }
        public decimal WorkedDays { get; set; }
        public decimal Twd { get; set; }
        public decimal OtHrs { get; set; }

        // Earnings Components (CL)
        public decimal AttnBonus { get; set; }
        public decimal EarnedBasicDa { get; set; }
        public decimal EarnedHra { get; set; }
        public decimal OtAmount { get; set; }
        public decimal Epf { get; set; }
        public decimal Esi { get; set; }
        public decimal BonusDed { get; set; }
        public decimal SerChar { get; set; }
        public decimal Lwf { get; set; }
        public decimal Gross { get; set; }

        // Earnings Components (NAPS)
        public decimal EarnedWages { get; set; }
        public decimal OtEarning { get; set; }
        public decimal HandlingCharge { get; set; }
        public decimal NapsGross { get; set; }

        // Deductions
        public decimal PfDed { get; set; }
        public decimal EsiDed { get; set; }
        public decimal HostelDed { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal TotalDed { get; set; }

        // Provisions / Variable Inputs
        public decimal RefBon { get; set; }
        public decimal SplAllw { get; set; }
        public decimal Kaizen { get; set; }
        public decimal GpaGmc { get; set; }
        public decimal VariableTotal { get; set; }

        // Financial Net
        public decimal NetPayable { get; set; }

        // Status & Approval Dates
        public string Status { get; set; } = "Pending";
        public string CheckerStatus { get; set; } = "Pending";
        public string? CheckerBy { get; set; }
        public DateTime? CheckerOn { get; set; }
        public string ApproverStatus { get; set; } = "Pending";
        public string? ApproverBy { get; set; }
        public DateTime? ApproverOn { get; set; } // User specifically requested Approver Date

        public string? Remarks { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string Version { get; set; } = "V1";
        public DateTime? CreatedOn { get; set; }
    }

    public class EmployeeDropdownOption
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty; // e.g. "CIELCL0002 | Rishiba D"
    }

    public class ReportPeriodOption
    {
        public string FromDate { get; set; } = string.Empty;
        public string ToDate { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty; // "2026-07-25|2026-08-26"
        public string Label { get; set; } = string.Empty;
    }

    public class PayrollReportFilterOptionsDTO
    {
        public List<EmployeeDropdownOption> Employees { get; set; } = new();
        public List<string> SkillCategories { get; set; } = new();
        public List<string> Vendors { get; set; } = new();
        public List<ReportPeriodOption> Periods { get; set; } = new();
    }
}
