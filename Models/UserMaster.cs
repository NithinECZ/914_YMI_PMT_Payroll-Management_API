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
        public string UserId { get; set; } = string.Empty;

        [Column("User_Nm")]
        public string UserName { get; set; } = string.Empty;

        [Column("Dept_Nm")]
        public string? DepartmentName { get; set; }

        [Column("User_Type")]
        public string? UserType { get; set; }

        [Column("Pass_Wd")]
        public string? Password { get; set; }

        // NEW
        [Column("Email_Id")]
        public string? EmailId { get; set; }

        [Column("Status")]
        public string? Status { get; set; }

        [Column("User_St")]
        public string? UserState { get; set; }

        [Column("Time_St")]
        public DateTime? TimeStamp { get; set; }

        [Column("Crtd_On")]
        public DateTime CreatedOn { get; set; }

        [Column("Crtd_By")]
        public string? CreatedBy { get; set; }

        [Column("Mdfd_On")]
        public DateTime? ModifiedOn { get; set; }

        [Column("Mdfd_By")]
        public string? ModifiedBy { get; set; }
    }
}