using Microsoft.EntityFrameworkCore;
using YMI_PMT_PayrollManagement_API.Models;
using YMI_PMT_PayrollManagement_API.Interfaces.Repository;
using YMI_PMT_PayrollManagement_API.Data;

namespace YMI_PMT_PayrollManagement_API.Repository
{
    public class VendorMasterRepository : IVendorMasterRepository
    {
        private readonly AppDbContext _context;

        public VendorMasterRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<VendorMaster>> GetAllAsync()
        {
            return await _context.VendorMasters
                .AsNoTracking()
                .OrderBy(v => v.VendorName)
                .ToListAsync();
        }

        public async Task<VendorMaster?> GetByIdAsync(int id)
        {
            return await _context.VendorMasters
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task<int> CreateAsync(VendorMaster vendor)
        {
            if (string.IsNullOrEmpty(vendor.VendorId))
                throw new ArgumentException("Vendor Id is required");

            if (string.IsNullOrEmpty(vendor.VendorName))
                throw new ArgumentException("Vendor Name is required");

            if (string.IsNullOrEmpty(vendor.Status))
                vendor.Status = "1";

            vendor.CreatedOn = DateTime.Now;
            if (vendor.CreatedBy == null)
                vendor.CreatedBy = "SYSTEM";

            _context.VendorMasters.Add(vendor);
            await _context.SaveChangesAsync();
            return vendor.Id;
        }

        public async Task<bool> UpdateAsync(VendorMaster vendor)
        {
            var existing = await _context.VendorMasters.FirstOrDefaultAsync(v => v.Id == vendor.Id);
            if (existing == null)
                return false;

            existing.VendorName = vendor.VendorName ?? existing.VendorName;
            existing.VendorEmail = vendor.VendorEmail ?? existing.VendorEmail;
            existing.VendorGSTIN = vendor.VendorGSTIN ?? existing.VendorGSTIN;
            existing.VendorPAN = vendor.VendorPAN ?? existing.VendorPAN;
            existing.VendorContact = vendor.VendorContact ?? existing.VendorContact;
            existing.VendorAddress = vendor.VendorAddress;
            existing.VendorESIC = vendor.VendorESIC;
            existing.VendorPF = vendor.VendorPF;
            existing.VendorLicense = vendor.VendorLicense; // now allowed to be set to null
            existing.Status = vendor.Status ?? "1";
            existing.ModifiedOn = DateTime.Now;
            existing.ModifiedBy = vendor.ModifiedBy ?? "SYSTEM";

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _context.VendorMasters.FirstOrDefaultAsync(v => v.Id == id);
            if (existing == null)
                return false;

            _context.VendorMasters.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> VendorIdExistsAsync(string vendorId, int excludeId = 0)
        {
            return await _context.VendorMasters
                .AnyAsync(v => v.VendorId == vendorId && v.Id != excludeId);
        }

        public async Task<List<string>> GetDistinctVendorNamesAsync()
        {
            return await _context.VendorMasters
                .AsNoTracking()
                .Select(v => v.VendorName)
                .Distinct()
                .OrderBy(n => n)
                .ToListAsync();
        }
    }
}