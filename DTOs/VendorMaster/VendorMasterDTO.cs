namespace YMI_PMT_PayrollManagement_API.DTOs.VendorMaster
{
    // Used when returning vendor data to frontend
    public class VendorMasterDTO
    {
        public int Id { get; set; }
        public string VendorId { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public string VendorEmail { get; set; } = string.Empty;
        public string VendorGSTIN { get; set; } = string.Empty;
        public string VendorPAN { get; set; } = string.Empty;
        public string VendorContact { get; set; } = string.Empty;
        public string? VendorAddress { get; set; }
        public string? VendorESIC { get; set; }
        public string? VendorPF { get; set; }
        public string? VendorLicense { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    // Used when creating or updating a vendor
    public class CreateVendorMasterDTO
    {
        public string VendorId { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public string VendorEmail { get; set; } = string.Empty;
        public string VendorGSTIN { get; set; } = string.Empty;
        public string VendorPAN { get; set; } = string.Empty;
        public string VendorContact { get; set; } = string.Empty;
        public string? VendorAddress { get; set; }
        public string? VendorESIC { get; set; }
        public string? VendorPF { get; set; }
        public string? VendorLicense { get; set; }
        public string ModifiedBy { get; set; } = "SYSTEM";
        public bool IsActive { get; set; } = true;
    }
}