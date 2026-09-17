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
        }
    }
}