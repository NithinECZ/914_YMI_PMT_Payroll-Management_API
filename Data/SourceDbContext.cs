using Microsoft.EntityFrameworkCore;
using YMI_PMT_PayrollManagement_API.Models;

namespace YMI_PMT_PayrollManagement_API.Data
{
    public class SourceDbContext : DbContext
    {
        public SourceDbContext(DbContextOptions<SourceDbContext> options) : base(options) { }

        public DbSet<MxVewDailyAttendance> MxVewDailyAttendance { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<MxVewDailyAttendance>().HasNoKey().ToView(null);
        }
    }
}