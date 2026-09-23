using YMI_PMT_PayrollManagement_API.DTOs.VendorMaster;
using YMI_PMT_PayrollManagement_API.Interfaces.Repository;
using YMI_PMT_PayrollManagement_API.Interfaces.Services;
using YMI_PMT_PayrollManagement_API.Models;

namespace YMI_PMT_PayrollManagement_API.Services
{
    public class VendorMasterService : IVendorMasterService
    {
        private readonly IVendorMasterRepository _repository;

        public VendorMasterService(IVendorMasterRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<VendorMasterDTO>> GetAllAsync()
        {
            var list = await _repository.GetAllAsync();
            return list.Select(MapToDto).ToList();
        }

        public async Task<VendorMasterDTO?> GetByIdAsync(int id)
        {
            var vendor = await _repository.GetByIdAsync(id);
            return vendor == null ? null : MapToDto(vendor);
        }

        public async Task<List<string>> GetVendorNamesAsync()
        {
            return await _repository.GetDistinctVendorNamesAsync();
        }

        public async Task<(bool Success, string Message, int Id)> CreateAsync(CreateVendorMasterDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.VendorId))
                return (false, "Vendor Id is required", 0);

            if (await _repository.VendorIdExistsAsync(dto.VendorId))
                return (false, "Vendor Id already exists", 0);

            if (string.IsNullOrWhiteSpace(dto.VendorName))
                return (false, "Vendor Name is required", 0);

            if (string.IsNullOrWhiteSpace(dto.VendorEmail))
                return (false, "Vendor Email is required", 0);

            if (string.IsNullOrWhiteSpace(dto.VendorGSTIN))
                return (false, "Vendor GSTIN is required", 0);

            if (string.IsNullOrWhiteSpace(dto.VendorPAN))
                return (false, "Vendor PAN is required", 0);

            if (string.IsNullOrWhiteSpace(dto.VendorContact))
                return (false, "Vendor Contact is required", 0);

            var entity = new VendorMaster
            {
                VendorId = dto.VendorId.Trim(),
                VendorName = dto.VendorName.Trim(),
                VendorEmail = dto.VendorEmail.Trim(),
                VendorGSTIN = dto.VendorGSTIN.Trim(),
                VendorPAN = dto.VendorPAN.Trim(),
                VendorContact = dto.VendorContact.Trim(),
                VendorAddress = string.IsNullOrWhiteSpace(dto.VendorAddress) ? null : dto.VendorAddress.Trim(),
                VendorESIC = string.IsNullOrWhiteSpace(dto.VendorESIC) ? null : dto.VendorESIC.Trim(),
                VendorPF = string.IsNullOrWhiteSpace(dto.VendorPF) ? null : dto.VendorPF.Trim(),
                VendorLicense = string.IsNullOrWhiteSpace(dto.VendorLicense) ? null : dto.VendorLicense.Trim(),
                Status = dto.IsActive ? "1" : "0",
                CreatedOn = DateTime.Now,
                CreatedBy = string.IsNullOrWhiteSpace(dto.ModifiedBy) ? "SYSTEM" : dto.ModifiedBy,
            };

            var id = await _repository.CreateAsync(entity);
            return (true, "Vendor Saved Successfully", id);
        }

        public async Task<(bool Success, string Message)> UpdateAsync(int id, CreateVendorMasterDTO dto)
        {
            if (await _repository.VendorIdExistsAsync(dto.VendorId, id))
                return (false, "Vendor Id already exists");

            var entity = new VendorMaster
            {
                Id = id,
                VendorId = dto.VendorId.Trim(),
                VendorName = dto.VendorName.Trim(),
                VendorEmail = dto.VendorEmail.Trim(),
                VendorGSTIN = dto.VendorGSTIN.Trim(),
                VendorPAN = dto.VendorPAN.Trim(),
                VendorContact = dto.VendorContact.Trim(),
                VendorAddress = string.IsNullOrWhiteSpace(dto.VendorAddress) ? null : dto.VendorAddress.Trim(),
                VendorESIC = string.IsNullOrWhiteSpace(dto.VendorESIC) ? null : dto.VendorESIC.Trim(),
                VendorPF = string.IsNullOrWhiteSpace(dto.VendorPF) ? null : dto.VendorPF.Trim(),
                VendorLicense = string.IsNullOrWhiteSpace(dto.VendorLicense) ? null : dto.VendorLicense.Trim(),
                Status = dto.IsActive ? "1" : "0",
                ModifiedBy = string.IsNullOrWhiteSpace(dto.ModifiedBy) ? "SYSTEM" : dto.ModifiedBy,
            };

            var result = await _repository.UpdateAsync(entity);
            return result ? (true, "Vendor Updated Successfully") : (false, "Vendor not found");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(int id)
        {
            var result = await _repository.DeleteAsync(id);
            return result ? (true, "Deleted Successfully") : (false, "Vendor not found");
        }

        private static VendorMasterDTO MapToDto(VendorMaster v) => new VendorMasterDTO
        {
            Id = v.Id,
            VendorId = v.VendorId ?? string.Empty,
            VendorName = v.VendorName ?? string.Empty,
            VendorEmail = v.VendorEmail ?? string.Empty,
            VendorGSTIN = v.VendorGSTIN ?? string.Empty,
            VendorPAN = v.VendorPAN ?? string.Empty,
            VendorContact = v.VendorContact ?? string.Empty,
            VendorAddress = v.VendorAddress ?? string.Empty,
            VendorESIC = v.VendorESIC ?? string.Empty,
            VendorPF = v.VendorPF ?? string.Empty,
            VendorLicense = v.VendorLicense ?? string.Empty,
            Status = v.Status ?? string.Empty,
        };
    }
}