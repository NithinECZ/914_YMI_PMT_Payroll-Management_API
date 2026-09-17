using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YMI_PMT_PayrollManagement_API.Models
{
    [Table("YMT_USER_MASTER")]
    public class UserMaster
    {
        [Key]
        public int Id { get; set; }

        [Column("User_Id")]
        public string UserId { get; set; } = string.Empty;  // Required field, never NULL

        [Column("User_Nm")]
        public string UserName { get; set; } = string.Empty;  // Required field, never NULL

        [Column("Dept_Nm")]
        public string? DepartmentName { get; set; }  // 👈 FIXED: Nullable (can be NULL in DB)

        // MAKER | CHECKER | APPROVER | SUPER ADMIN
        [Column("User_Type")]
        public string? UserType { get; set; }  // 👈 FIXED: Nullable (can be NULL in DB)

        // Stores combined "salt:hash" string (salt + hash together, colon separated)
        // No separate salt column used - everything goes into this single column
        [Column("Pass_Wd")]
        public string? Password { get; set; }  // 👈 FIXED: Nullable (can be NULL in DB)

        [Column("Status")]  // 👈 ADDED: Explicit column mapping
        public string? Status { get; set; }  // 👈 FIXED: Nullable (can be NULL in DB)

        [Column("User_St")]
        public string? UserState { get; set; }  // 👈 FIXED: Nullable (can be NULL in DB)

        [Column("Time_St")]
        public DateTime? TimeStamp { get; set; }

        [Column("Crtd_On")]
        public DateTime CreatedOn { get; set; }

        [Column("Crtd_By")]
        public string? CreatedBy { get; set; }  // 👈 FIXED: Nullable (can be NULL in DB)

        [Column("Mdfd_On")]
        public DateTime? ModifiedOn { get; set; }

        [Column("Mdfd_By")]
        public string? ModifiedBy { get; set; }
    }
}