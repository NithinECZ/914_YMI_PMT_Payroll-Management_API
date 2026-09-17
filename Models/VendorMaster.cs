using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YMI_PMT_PayrollManagement_API.Models
{
    [Table("YMT_VENDOR_MASTER")]
    public class VendorMaster
    {
        [Key]
        public int Id { get; set; }

        [Column("Vnd_Id")]
        public string VendorId { get; set; } = string.Empty;

        [Column("Vnd_Nm")]
        public string VendorName { get; set; } = string.Empty;

        [Column("Vnd_Em")]
        public string VendorEmail { get; set; } = string.Empty;

        [Column("Vnd_Gs")]
        public string VendorGSTIN { get; set; } = string.Empty;

        [Column("Vnd_Pn")]
        public string VendorPAN { get; set; } = string.Empty;

        [Column("Vnd_Ct")]
        public string VendorContact { get; set; } = string.Empty;

        [Column("Vnd_Ad")]
        public string? VendorAddress { get; set; }

        [Column("Vnd_Es")]
        public string? VendorESIC { get; set; }

        [Column("Vnd_Pf")]
        public string? VendorPF { get; set; }

        [Column("Vnd_Lc")]
        public string VendorLicense { get; set; } = string.Empty;

        [Column("Status")]
        public string? Status { get; set; }

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