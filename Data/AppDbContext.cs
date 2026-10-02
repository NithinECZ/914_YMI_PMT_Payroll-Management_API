using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using YMI_PMT_PayrollManagement_API.Models;

namespace YMI_PMT_PayrollManagement_API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Support for FromSqlInterpolated (used in UserRepository)
            base.OnConfiguring(optionsBuilder);
        }

        public DbSet<UserMaster> UserMasters { get; set; }
        public DbSet<UserPrivilege> UserPrivileges { get; set; }
        public DbSet<MailConfig> MailConfigs { get; set; }
        public DbSet<EmployeeMaster> EmployeeMasters { get; set; }
        public DbSet<VendorMaster> VendorMasters { get; set; }
        public DbSet<CalendarMaster> CalendarMasters { get; set; }
        public DbSet<SalaryStructure> SalaryStructures { get; set; }
        public DbSet<SalaryStructureComponent> SalaryStructureComponents { get; set; }
        public DbSet<SalaryFormula> SalaryFormulas { get; set; }

        // Result set of SP_GET_PAYROLL_EMPLOYEES (keyless, not a table)
        public DbSet<PayrollEmployee> PayrollEmployees { get; set; }

        // Real table (YMT_PAYROLL_PROVISIONS) behind the "Upload Excel"
        // / "Reupload Excel" flow on the Payroll screen.
        public DbSet<PayrollProvision> PayrollProvisions { get; set; }

        // Real table (YMT_PAYROLL_CALCULATION) written by SP_CALCULATE_PAYROLL
        // ("Calculate Payroll" button) and read back via SP_GET_PAYROLL_CALCULATIONS.
        public DbSet<PayrollCalculation> PayrollCalculations { get; set; }
        public DbSet<PayrollCheckerRow> PayrollCheckerRows { get; set; }

        // 👈 NEW: Attendance archive table
        public DbSet<PayrollAttendanceArchive> PayrollAttendanceArchives { get; set; }

        public DbSet<YMI_PMT_PayrollManagement_API.Models.EmailQueue> EmailQueues { get; set; }
        public DbSet<YmtAttendance> YmtAttendances { get; set; }
        public DbSet<PayrollStageRow> PayrollStageRows { get; set; }
        public DbSet<PayslipEmailLog> PayslipEmailLogs { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CalendarMaster>()
                .HasIndex(c => new { c.Month, c.Year, c.Day })
                .IsUnique()
                .HasDatabaseName("IX_CalendarMaster_MonthYearDay");

            modelBuilder.Entity<SalaryStructureComponent>()
                .HasOne(c => c.Structure)
                .WithMany(s => s.Components)
                .HasForeignKey(c => c.StructureId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SalaryStructureComponent>()
                .Property(c => c.AmountOrPercentage)
                .HasColumnType("decimal(18,2)");

            // Keyless entity mapped to the stored procedure result (no table / no migration)
            modelBuilder.Entity<PayrollEmployee>()
                .HasNoKey()
                .ToView(null);
            modelBuilder.Entity<EmpLeavingRow>().HasNoKey();
            modelBuilder.Entity<PayrollPeriodRow>().HasNoKey();
            modelBuilder.Entity<YmtAttendance>().ToTable("YMT_ATTENDANCE");
            modelBuilder.Entity<PayrollStatusSummary>().HasNoKey();
            // YMT_PAYROLL_PROVISIONS and YMT_PAYROLL_CALCULATION are real tables
            // with an Id identity column, so the [Table]/[Column] attributes on
            // PayrollProvision.cs / PayrollCalculation.cs are enough — nothing
            // extra to configure here.
        }
    }
}