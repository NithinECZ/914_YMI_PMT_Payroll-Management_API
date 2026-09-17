using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YMI_PMT_PayrollManagement_API.Models
{
    [Table("YMT_USER_PRIVILEGES")]
    public class UserPrivilege
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        public string MenuName { get; set; } = string.Empty;

        public bool CanView { get; set; }

        public bool CanEdit { get; set; }

        public bool CanDelete { get; set; }
    }
}