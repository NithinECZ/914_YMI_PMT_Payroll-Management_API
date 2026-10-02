using System.Data;
using System.Data.Common;
using System.Net.Mail;
using System.Text;
using Microsoft.EntityFrameworkCore;
using YMI_PMT_PayrollManagement_API.Data;
using YMI_PMT_PayrollManagement_API.DTOs.Payslip;
using YMI_PMT_PayrollManagement_API.Interfaces.Repository;
using YMI_PMT_PayrollManagement_API.Models;
using YMI_PMT_PayrollManagement_API.Services;

namespace YMI_PMT_PayrollManagement_API.Repository
{
    public class PayslipRepository : IPayslipRepository
    {
        private readonly AppDbContext _context;

        public PayslipRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<PayslipEmployeeDTO>> GetApprovedPayslipDataAsync(string category, DateTime? fromDate, DateTime? toDate)
        {
            var cat = (category ?? "CL").Trim().ToUpperInvariant();
            var list = new List<PayslipEmployeeDTO>();

            var conn = _context.Database.GetDbConnection();
            bool wasOpen = conn.State == ConnectionState.Open;
            if (!wasOpen) await conn.OpenAsync();

            try
            {
                // 1. Try stored procedure SP_GET_PAYSLIP_APPROVED_DATA
                try
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "dbo.SP_GET_PAYSLIP_APPROVED_DATA";
                        cmd.CommandType = CommandType.StoredProcedure;

                        var pCat = cmd.CreateParameter();
                        pCat.ParameterName = "@Category";
                        pCat.Value = cat;
                        cmd.Parameters.Add(pCat);

                        var pFrom = cmd.CreateParameter();
                        pFrom.ParameterName = "@FromDate";
                        pFrom.Value = fromDate.HasValue ? (object)fromDate.Value.Date : DBNull.Value;
                        cmd.Parameters.Add(pFrom);

                        var pTo = cmd.CreateParameter();
                        pTo.ParameterName = "@ToDate";
                        pTo.Value = toDate.HasValue ? (object)toDate.Value.Date : DBNull.Value;
                        cmd.Parameters.Add(pTo);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                list.Add(ReadPayslipEmployeeDTO(reader, cat));
                            }
                        }
                    }
                }
                catch
                {
                    // Stored procedure failed or does not exist, will fall back to direct query
                }

                // 2. Direct fallback on YMT_PAYROLL_CALCULATION if SP returned 0 rows or data for wrong period
                if (list.Count == 0 || (fromDate.HasValue && toDate.HasValue && !list.Any(x => x.FromDate?.Date == fromDate.Value.Date && x.ToDate?.Date == toDate.Value.Date)))
                {
                    list.Clear();
                    try
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = @"
                                WITH RankedCalc AS (
                                    SELECT 
                                        c.*,
                                        ROW_NUMBER() OVER (
                                            PARTITION BY c.Emp_Id, CAST(c.From_Dt AS DATE), CAST(c.To_Dt AS DATE)
                                            ORDER BY 
                                                CASE WHEN UPPER(LTRIM(RTRIM(ISNULL(c.Approver_Status, '')))) IN ('APPROVER-APPROVED', 'APPROVED') THEN 1 ELSE 2 END,
                                                c.Version DESC,
                                                c.Id DESC
                                        ) AS rn
                                    FROM dbo.YMT_PAYROLL_CALCULATION c
                                    WHERE UPPER(LTRIM(RTRIM(ISNULL(c.Ctg_Code, '')))) = @Category
                                      AND (
                                            UPPER(LTRIM(RTRIM(ISNULL(c.Approver_Status, '')))) IN ('APPROVER-APPROVED', 'APPROVED')
                                         OR UPPER(LTRIM(RTRIM(ISNULL(c.Status, '')))) IN ('APPROVER-APPROVED', 'APPROVED')
                                      )
                                      AND (@FromDate IS NULL OR CAST(c.From_Dt AS DATE) = CAST(@FromDate AS DATE))
                                      AND (@ToDate IS NULL OR CAST(c.To_Dt AS DATE) = CAST(@ToDate AS DATE))
                                )
                                SELECT 
                                    r.Id,
                                    r.Emp_Id,
                                    r.Emp_Nm,
                                    r.Ctg_Code,
                                    r.Vendor,
                                    r.From_Dt,
                                    r.To_Dt,
                                    r.Period_Days,
                                    r.Pay_Days_Full,
                                    r.Pay_Days_Half,
                                    r.TWD,
                                    r.OT_Hrs,
                                    r.Attn_Bonus,
                                    r.Earned_Basic_DA,
                                    r.Earned_HRA,
                                    r.OT_Amount,
                                    r.Gross,
                                    r.EPF,
                                    r.ESI,
                                    r.Bonus_Ded,
                                    r.Ser_Char,
                                    r.LWF,
                                    r.Total_Ded,
                                    r.Earned_Wages,
                                    r.OT_Earning,
                                    r.Handling_Charge,
                                    r.Hostel_Ded,
                                    r.PF_Ded,
                                    r.ESI_Ded,
                                    r.Net_Payable,
                                    r.Status,
                                    r.Approver_Status,
                                    r.Checker_Status,
                                    r.Transaction_ID,
                                    r.Version,
                                    r.Remarks,
                                    r.Crtd_On,
                                    p.Ref_Bon,
                                    p.Spl_Allw,
                                    p.Kaizen,
                                    p.GPA_GMC
                                FROM RankedCalc r
                                LEFT JOIN dbo.YMT_PAYROLL_PROVISIONS p 
                                    ON p.Emp_Id = r.Emp_Id 
                                   AND CAST(p.From_Dt AS DATE) = CAST(r.From_Dt AS DATE)
                                   AND CAST(p.To_Dt AS DATE) = CAST(r.To_Dt AS DATE)
                                WHERE r.rn = 1
                                ORDER BY r.Emp_Id ASC";
                            cmd.CommandType = CommandType.Text;

                            var pCat = cmd.CreateParameter();
                            pCat.ParameterName = "@Category";
                            pCat.Value = cat;
                            cmd.Parameters.Add(pCat);

                            var pFrom = cmd.CreateParameter();
                            pFrom.ParameterName = "@FromDate";
                            pFrom.Value = fromDate.HasValue ? (object)fromDate.Value.Date : DBNull.Value;
                            cmd.Parameters.Add(pFrom);

                            var pTo = cmd.CreateParameter();
                            pTo.ParameterName = "@ToDate";
                            pTo.Value = toDate.HasValue ? (object)toDate.Value.Date : DBNull.Value;
                            cmd.Parameters.Add(pTo);

                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    list.Add(ReadPayslipEmployeeDTO(reader, cat));
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Direct calculation query error handling
                    }
                }
            }
            finally
            {
                if (!wasOpen && conn.State == ConnectionState.Open)
                    await conn.CloseAsync();
            }

            // Enrich with EmployeeMaster (YMT_EMP_MASTER) for Skill Category, Qualification, Statutory numbers, etc.
            if (list.Count > 0)
            {
                try
                {
                    var empIds = list
                        .Select(x => (x.EmployeeId ?? "").Trim())
                        .Where(x => !string.IsNullOrEmpty(x))
                        .Distinct()
                        .ToList();

                    if (empIds.Count > 0)
                    {
                        var masters = await _context.EmployeeMasters
                            .Where(m => empIds.Contains(m.EmpId))
                            .AsNoTracking()
                            .ToListAsync();

                        var masterDict = masters
                            .Where(m => !string.IsNullOrWhiteSpace(m.EmpId))
                            .ToDictionary(m => m.EmpId.Trim().ToUpperInvariant(), m => m);

                        foreach (var item in list)
                        {
                            var key = (item.EmployeeId ?? "").Trim().ToUpperInvariant();
                            if (masterDict.TryGetValue(key, out var m))
                            {
                                if (string.IsNullOrWhiteSpace(item.SkillCategory))
                                    item.SkillCategory = m.SkillCat ?? "";
                                if (string.IsNullOrWhiteSpace(item.Qualification))
                                    item.Qualification = m.Qualify ?? "";
                                if (string.IsNullOrWhiteSpace(item.Designation) || item.Designation == "-")
                                    item.Designation = !string.IsNullOrWhiteSpace(m.Qualify) ? m.Qualify : "-";
                                if (string.IsNullOrWhiteSpace(item.Vendor))
                                    item.Vendor = m.Vendor ?? "";
                                if (string.IsNullOrWhiteSpace(item.VendorId))
                                    item.VendorId = m.VendorId ?? "";
                                if (string.IsNullOrWhiteSpace(item.PfNo))
                                    item.PfNo = m.PfNo ?? "";
                                if (string.IsNullOrWhiteSpace(item.EsiNo))
                                    item.EsiNo = m.EsiNo ?? "";
                                if (string.IsNullOrWhiteSpace(item.PanNo))
                                    item.PanNo = m.PanNo ?? "";
                                if (string.IsNullOrWhiteSpace(item.UanNo))
                                    item.UanNo = m.UanNo ?? "";
                                if (!item.DateOfJoining.HasValue)
                                    item.DateOfJoining = m.Doj;
                                if (string.IsNullOrWhiteSpace(item.LeavingDate) && m.Dol.HasValue)
                                    item.LeavingDate = m.Dol.Value.ToString("yyyy-MM-dd");
                            }
                        }
                    }
                }
                catch
                {
                    // Non-blocking: safe fallback to calculation data
                }
            }

            // Enrich with PayrollProvisions (YMT_PAYROLL_PROVISIONS) for all records using raw SQL ADO.NET
            if (list.Count > 0)
            {
                try
                {
                    var allProvs = await GetProvisionsAsync(cat);

                    if (allProvs.Count > 0)
                    {
                        foreach (var item in list)
                        {
                            var empKey = (item.EmployeeId ?? "").Trim().ToUpperInvariant();

                            // Match by EmployeeId with scoring:
                            // 100 = Exact From & To date match
                            // 50  = Either From or To date matches
                            // 25  = Matching Month & Year
                            // 1   = Latest record for this employee
                            var matched = allProvs
                                .Where(p => (p.EmployeeId ?? "").Trim().ToUpperInvariant() == empKey)
                                .OrderByDescending(p =>
                                    (p.FromDate.HasValue && item.FromDate.HasValue && p.FromDate.Value.Date == item.FromDate.Value.Date &&
                                     p.ToDate.HasValue && item.ToDate.HasValue && p.ToDate.Value.Date == item.ToDate.Value.Date) ? 100 :
                                    ((p.FromDate.HasValue && item.FromDate.HasValue && p.FromDate.Value.Date == item.FromDate.Value.Date) ||
                                     (p.ToDate.HasValue && item.ToDate.HasValue && p.ToDate.Value.Date == item.ToDate.Value.Date)) ? 50 :
                                    ((p.ToDate.HasValue && item.ToDate.HasValue && p.ToDate.Value.Year == item.ToDate.Value.Year && p.ToDate.Value.Month == item.ToDate.Value.Month) ||
                                     (p.FromDate.HasValue && item.FromDate.HasValue && p.FromDate.Value.Year == item.FromDate.Value.Year && p.FromDate.Value.Month == item.FromDate.Value.Month)) ? 25 : 1
                                )
                                .ThenByDescending(p => p.Id)
                                .FirstOrDefault();

                            if (matched != null)
                            {
                                var rBon = matched.ReferalBonus ?? 0;
                                var sAllw = matched.SplAllow ?? 0;
                                var kzn = matched.Kaizen ?? 0;
                                var gGmc = matched.GpaGmc ?? 0;
                                var provTotal = rBon + sAllw + kzn + gGmc;

                                item.RefBon = rBon;
                                item.SplAllw = sAllw;
                                item.Kaizen = kzn;
                                item.GpaGmc = gGmc;
                                item.VariableTotal = provTotal;

                                item.Provisions["referalBonus"] = rBon;
                                item.Provisions["splAllow"] = sAllw;
                                item.Provisions["kaizen"] = kzn;
                                item.Provisions["gpaGmc"] = gGmc;

                                // Recalculate NetPayable to accurately include provisions
                                item.NetPayable = (item.Gross ?? 0) + provTotal - (item.TotalDeductions ?? 0);
                            }
                        }
                    }
                }
                catch
                {
                    // Non-blocking provision enrichment
                }
            }

            return list;
        }

        public async Task<List<PayrollPeriodRow>> GetApprovedPeriodsAsync(string category)
        {
            var cat = (category ?? "CL").Trim().ToUpperInvariant();
            var list = new List<PayrollPeriodRow>();

            var conn = _context.Database.GetDbConnection();
            bool wasOpen = conn.State == ConnectionState.Open;
            if (!wasOpen) await conn.OpenAsync();

            try
            {
                // 1. Direct query from YMT_PAYROLL_CALCULATION where Approver_Status or Status is approved
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT 
                            CAST(From_Dt AS DATE) AS From_Dt,
                            CAST(To_Dt AS DATE) AS To_Dt,
                            COUNT(DISTINCT Emp_Id) AS Emp_Count,
                            ISNULL(SUM(ISNULL(Net_Payable, 0)), 0) AS Net_Total,
                            MAX(Transaction_ID) AS Transaction_ID
                        FROM dbo.YMT_PAYROLL_CALCULATION
                        WHERE UPPER(LTRIM(RTRIM(ISNULL(Ctg_Code, '')))) = @Category
                          AND (
                                UPPER(LTRIM(RTRIM(ISNULL(Approver_Status, '')))) IN ('APPROVER-APPROVED', 'APPROVED')
                             OR UPPER(LTRIM(RTRIM(ISNULL(Status, '')))) IN ('APPROVER-APPROVED', 'APPROVED')
                          )
                          AND From_Dt IS NOT NULL
                          AND To_Dt IS NOT NULL
                        GROUP BY CAST(From_Dt AS DATE), CAST(To_Dt AS DATE)
                        ORDER BY CAST(From_Dt AS DATE) DESC";
                    cmd.CommandType = CommandType.Text;

                    var pCat = cmd.CreateParameter();
                    pCat.ParameterName = "@Category";
                    pCat.Value = cat;
                    cmd.Parameters.Add(pCat);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var row = new PayrollPeriodRow();
                            var f = GetDateTime(reader, "From_Dt") ?? GetDateTime(reader, "FromDate");
                            var t = GetDateTime(reader, "To_Dt") ?? GetDateTime(reader, "ToDate");
                            if (f.HasValue) row.FromDate = f.Value;
                            if (t.HasValue) row.ToDate = t.Value;
                            row.EmployeeCount = GetInt(reader, "Emp_Count") ?? GetInt(reader, "EmployeeCount") ?? 0;
                            row.NetTotal = GetDecimal(reader, "Net_Total") ?? GetDecimal(reader, "NetTotal") ?? 0;
                            row.TransactionId = GetStringAny(reader, "Transaction_ID", "TransactionId");
                            list.Add(row);
                        }
                    }
                }

                // 2. If no approved rows found, check all calculation periods for that category
                if (list.Count == 0)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                            SELECT 
                                CAST(From_Dt AS DATE) AS From_Dt,
                                CAST(To_Dt AS DATE) AS To_Dt,
                                COUNT(DISTINCT Emp_Id) AS Emp_Count,
                                ISNULL(SUM(ISNULL(Net_Payable, 0)), 0) AS Net_Total,
                                MAX(Transaction_ID) AS Transaction_ID
                            FROM dbo.YMT_PAYROLL_CALCULATION
                            WHERE UPPER(LTRIM(RTRIM(ISNULL(Ctg_Code, '')))) = @Category
                              AND From_Dt IS NOT NULL
                              AND To_Dt IS NOT NULL
                            GROUP BY CAST(From_Dt AS DATE), CAST(To_Dt AS DATE)
                            ORDER BY CAST(From_Dt AS DATE) DESC";
                        cmd.CommandType = CommandType.Text;

                        var pCat = cmd.CreateParameter();
                        pCat.ParameterName = "@Category";
                        pCat.Value = cat;
                        cmd.Parameters.Add(pCat);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var row = new PayrollPeriodRow();
                                var f = GetDateTime(reader, "From_Dt") ?? GetDateTime(reader, "FromDate");
                                var t = GetDateTime(reader, "To_Dt") ?? GetDateTime(reader, "ToDate");
                                if (f.HasValue) row.FromDate = f.Value;
                                if (t.HasValue) row.ToDate = t.Value;
                                row.EmployeeCount = GetInt(reader, "Emp_Count") ?? GetInt(reader, "EmployeeCount") ?? 0;
                                row.NetTotal = GetDecimal(reader, "Net_Total") ?? GetDecimal(reader, "NetTotal") ?? 0;
                                row.TransactionId = GetStringAny(reader, "Transaction_ID", "TransactionId");
                                list.Add(row);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Fallback: try stored procedure if table query errors
                try
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "dbo.SP_GET_PAYSLIP_PERIODS";
                        cmd.CommandType = CommandType.StoredProcedure;

                        var pCat = cmd.CreateParameter();
                        pCat.ParameterName = "@Category";
                        pCat.Value = cat;
                        cmd.Parameters.Add(pCat);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var row = new PayrollPeriodRow();
                                var f = GetDateTime(reader, "From_Dt") ?? GetDateTime(reader, "FromDate");
                                var t = GetDateTime(reader, "To_Dt") ?? GetDateTime(reader, "ToDate");
                                if (f.HasValue) row.FromDate = f.Value;
                                if (t.HasValue) row.ToDate = t.Value;
                                row.EmployeeCount = GetInt(reader, "Emp_Count") ?? GetInt(reader, "EmployeeCount") ?? 0;
                                row.NetTotal = GetDecimal(reader, "Net_Total") ?? GetDecimal(reader, "NetTotal") ?? 0;
                                row.TransactionId = GetStringAny(reader, "Transaction_ID", "TransactionId");
                                list.Add(row);
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore fallback error
                }
            }
            finally
            {
                if (!wasOpen && conn.State == ConnectionState.Open)
                    await conn.CloseAsync();
            }

            return list;
        }

        public async Task<List<PayrollProvision>> GetProvisionsAsync(string category)
        {
            var cat = (category ?? "CL").Trim().ToUpperInvariant();
            var list = new List<PayrollProvision>();

            var conn = _context.Database.GetDbConnection();
            bool wasOpen = conn.State == ConnectionState.Open;
            if (!wasOpen) await conn.OpenAsync();

            try
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT 
                            Id,
                            Emp_Id,
                            Emp_Nm,
                            Ctg_Code,
                            CAST(From_Dt AS DATE) AS From_Dt,
                            CAST(To_Dt AS DATE) AS To_Dt,
                            ISNULL(Ref_Bon, 0) AS Ref_Bon,
                            ISNULL(Spl_Allw, 0) AS Spl_Allw,
                            ISNULL(Kaizen, 0) AS Kaizen,
                            ISNULL(GPA_GMC, 0) AS GPA_GMC,
                            ISNULL(Status, 'Open') AS Status,
                            Crtd_By,
                            Crtd_On
                        FROM dbo.YMT_PAYROLL_PROVISIONS
                        WHERE UPPER(LTRIM(RTRIM(ISNULL(Ctg_Code, '')))) = @Category
                        ORDER BY Id DESC";
                    cmd.CommandType = CommandType.Text;

                    var pCat = cmd.CreateParameter();
                    pCat.ParameterName = "@Category";
                    pCat.Value = cat;
                    cmd.Parameters.Add(pCat);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(new PayrollProvision
                            {
                                Id = GetInt(reader, "Id") ?? 0,
                                EmployeeId = GetStringAny(reader, "Emp_Id", "EmployeeId") ?? "",
                                EmployeeName = GetStringAny(reader, "Emp_Nm", "EmployeeName") ?? "",
                                CategoryCode = GetStringAny(reader, "Ctg_Code", "CategoryCode") ?? "",
                                FromDate = GetDateTime(reader, "From_Dt"),
                                ToDate = GetDateTime(reader, "To_Dt"),
                                ReferalBonus = GetDecimal(reader, "Ref_Bon") ?? 0,
                                SplAllow = GetDecimal(reader, "Spl_Allw") ?? 0,
                                Kaizen = GetDecimal(reader, "Kaizen") ?? 0,
                                GpaGmc = GetDecimal(reader, "GPA_GMC") ?? 0,
                                Status = GetStringAny(reader, "Status") ?? "Open",
                                CreatedBy = GetStringAny(reader, "Crtd_By"),
                                CreatedOn = GetDateTime(reader, "Crtd_On"),
                            });
                        }
                    }
                }
            }
            finally
            {
                if (!wasOpen && conn.State == ConnectionState.Open)
                    await conn.CloseAsync();
            }

            return list;
        }

        private static PayslipEmployeeDTO ReadPayslipEmployeeDTO(DbDataReader reader, string cat)
        {
            var dto = new PayslipEmployeeDTO();

            dto.Id = GetInt(reader, "Id") ?? 0;
            dto.EmployeeId = GetStringAny(reader, "EmployeeId", "Emp_Id", "EmpId", "Employee_Id");
            dto.EmployeeName = GetStringAny(reader, "EmployeeName", "Emp_Nm", "EmpNm", "Employee_Nm", "Name");
            dto.SkillCategory = GetStringAny(reader, "SkillCategory", "Skill_Cat", "SkillCat", "Skill");
            dto.Qualification = GetStringAny(reader, "Qualification", "Qualify", "Degree", "Education");

            dto.Designation = GetStringAny(reader, "Designation", "Desigination");
            if (!string.IsNullOrWhiteSpace(dto.Qualification))
            {
                dto.Designation = dto.Qualification;
            }
            else if (string.IsNullOrWhiteSpace(dto.Designation))
            {
                dto.Designation = "-";
            }

            dto.Vendor = GetStringAny(reader, "Vendor", "Vendor_Nm", "VendorName", "Vendor_Name");
            dto.VendorId = GetStringAny(reader, "VendorId", "Vendor_Id", "Vendor_ID");
            dto.FromDate = GetDateTime(reader, "FromDate") ?? GetDateTime(reader, "From_Dt");
            dto.ToDate = GetDateTime(reader, "ToDate") ?? GetDateTime(reader, "To_Dt");
            dto.DateOfJoining = GetDateTime(reader, "DateOfJoining") ?? GetDateTime(reader, "DOJ") ?? GetDateTime(reader, "Doj");
            dto.LeavingDate = (GetDateTime(reader, "LeavingDate") ?? GetDateTime(reader, "DOL") ?? GetDateTime(reader, "Dol"))?.ToString("yyyy-MM-dd");

            // Statutory numbers
            dto.PfNo = GetStringAny(reader, "PfNo", "PF_No", "Pf_No", "EpfNo");
            dto.EsiNo = GetStringAny(reader, "EsiNo", "ESI_No", "Esi_No");
            dto.PanNo = GetStringAny(reader, "PanNo", "PAN_No", "Pan_No");
            dto.UanNo = GetStringAny(reader, "UanNo", "UAN_No", "Uan_No");

            // Days
            dto.PeriodDays = GetInt(reader, "PeriodDays") ?? GetInt(reader, "Period_Days");
            dto.PayDaysFull = GetDecimalAny(reader, "PayDaysFull", "Pay_Days_Full");
            dto.PayDaysHalf = GetDecimalAny(reader, "PayDaysHalf", "Pay_Days_Half");
            dto.WorkingDays = (dto.PayDaysFull ?? 0) + (dto.PayDaysHalf ?? 0);
            dto.OtHrs = GetDecimalAny(reader, "OtHrs", "OT_Hrs");

            // Provisions / Variable Total
            decimal? refBon = GetDecimalAny(reader, "RefBon", "Ref_Bon", "ReferalBonus");
            decimal? splAllw = GetDecimalAny(reader, "SplAllw", "Spl_Allw", "SplAllow");
            decimal? kaizen = GetDecimalAny(reader, "Kaizen");
            decimal? gpaGmc = GetDecimalAny(reader, "GpaGmc", "GPA_GMC");
            decimal calcVarTotal = (refBon ?? 0) + (splAllw ?? 0) + (kaizen ?? 0) + (gpaGmc ?? 0);
            dto.VariableTotal = GetDecimal(reader, "VariableTotal") ?? (calcVarTotal > 0 ? calcVarTotal : 0);
            dto.RefBon = refBon ?? 0;
            dto.SplAllw = splAllw ?? 0;
            dto.Kaizen = kaizen ?? 0;
            dto.GpaGmc = gpaGmc ?? 0;

            // Statutory deductions
            decimal? pfDed = GetDecimalAny(reader, "PfDed", "PF_Ded");
            decimal? esiDed = GetDecimalAny(reader, "EsiDed", "ESI_Ded");
            decimal? hostelDed = GetDecimalAny(reader, "HostelDed", "Hostel_Ded");

            // NAPS Specific Earnings Components
            decimal earnedWages = GetDecimalAny(reader, "EarnedWages", "Earned_Wages") ?? 0;
            decimal otEarning = GetDecimalAny(reader, "OtEarning", "OT_Earning") ?? 0;
            decimal handlingCharge = GetDecimalAny(reader, "HandlingCharge", "Handling_Charge") ?? 0;

            if (cat.Equals("NAPS", StringComparison.OrdinalIgnoreCase))
            {
                // User Requirement for NAPS:
                // Gross = Earned Wages + OT Earning + Handling Charge
                decimal napsGross = earnedWages + otEarning + handlingCharge;
                dto.Gross = napsGross > 0 ? napsGross : (GetDecimal(reader, "Gross") ?? 0);

                // Deductions = Only Hostel Deduction
                dto.TotalDeductions = hostelDed ?? 0;
                dto.TotalDed = dto.TotalDeductions;
            }
            else
            {
                dto.Gross = GetDecimal(reader, "Gross") ?? 0;
                dto.TotalDed = GetDecimalAny(reader, "TotalDed", "Total_Ded");
                dto.TotalDeductions = (pfDed ?? 0) + (esiDed ?? 0) + (hostelDed ?? 0);
            }

            // Net Payable
            dto.NetPayable = GetDecimalAny(reader, "NetPayable", "Net_Payable");
            if (!dto.NetPayable.HasValue || dto.NetPayable == 0)
            {
                dto.NetPayable = (dto.Gross ?? 0) + (dto.VariableTotal ?? 0) - (dto.TotalDeductions ?? 0);
            }

            // Build breakdown dictionaries
            var provisions = new Dictionary<string, decimal>();
            var earnings = new Dictionary<string, decimal>();
            var deductions = new Dictionary<string, decimal>();

            // Always add provisions
            provisions["referalBonus"] = refBon ?? 0;
            provisions["splAllow"] = splAllw ?? 0;
            provisions["kaizen"] = kaizen ?? 0;
            provisions["gpaGmc"] = gpaGmc ?? 0;

            if (cat.Equals("NAPS", StringComparison.OrdinalIgnoreCase))
            {
                earnings["earnedWages"] = earnedWages;
                earnings["otEarning"] = otEarning;
                earnings["handlingCharge"] = handlingCharge;
                earnings["total"] = dto.Gross ?? 0;

                deductions["hostelFee"] = hostelDed ?? 0;
                deductions["hostelDed"] = hostelDed ?? 0;
                deductions["hostelDeduction"] = hostelDed ?? 0;
                deductions["total"] = dto.TotalDeductions ?? 0;
            }
            else
            {
                void AddVal(Dictionary<string, decimal> dict, string key, decimal? val)
                {
                    if (val.HasValue) dict[key] = val.Value;
                }

                AddVal(earnings, "payDaysFull", dto.PayDaysFull);
                AddVal(earnings, "payDaysHalf", dto.PayDaysHalf);
                AddVal(earnings, "attnBonus", GetDecimalAny(reader, "AttnBonus", "Attn_Bonus"));
                AddVal(earnings, "earnedBasicDa", GetDecimalAny(reader, "EarnedBasicDa", "Earned_Basic_DA"));
                AddVal(earnings, "earnedHra", GetDecimalAny(reader, "EarnedHra", "Earned_HRA"));
                AddVal(earnings, "otAmount", GetDecimalAny(reader, "OtAmount", "OT_Amount"));
                AddVal(earnings, "gross", dto.Gross);
                AddVal(earnings, "pf", GetDecimalAny(reader, "Epf", "EPF"));
                AddVal(earnings, "esi", GetDecimalAny(reader, "Esi", "ESI"));
                AddVal(earnings, "bonus", GetDecimalAny(reader, "BonusDed", "Bonus_Ded"));
                AddVal(earnings, "serChar", GetDecimalAny(reader, "SerChar", "Ser_Char"));
                AddVal(earnings, "lwf", GetDecimalAny(reader, "Lwf", "LWF"));
                AddVal(earnings, "total", dto.TotalDed);

                AddVal(deductions, "pf", pfDed);
                AddVal(deductions, "esi", esiDed);
                AddVal(deductions, "hostelDeduction", hostelDed);
                AddVal(deductions, "total", dto.TotalDeductions);
            }

            dto.Provisions = provisions;
            dto.Earnings = earnings;
            dto.Deductions = deductions;

            dto.Status = "approved";
            dto.ApproverStatus = GetStringAny(reader, "ApproverStatus", "Approver_Status");
            if (string.IsNullOrWhiteSpace(dto.ApproverStatus)) dto.ApproverStatus = "APPROVER-APPROVED";

            dto.CheckerStatus = GetStringAny(reader, "CheckerStatus", "Checker_Status");
            dto.TransactionId = GetStringAny(reader, "TransactionId", "Transaction_ID", "TransactionID", "TrId");
            dto.Version = GetStringAny(reader, "Version", "Ver");
            dto.CreatedOn = GetDateTime(reader, "CreatedOn") ?? GetDateTime(reader, "Crtd_On");

            return dto;
        }

        public async Task<bool> RecordPayslipGenerationAsync(
            string employeeId,
            string transactionId,
            string version,
            string category,
            string generatedBy)
        {
            await Task.CompletedTask;
            return true;
        }

        public async Task<List<PayslipEmailStatusDTO>> GetPayslipEmailStatusAsync(string category, DateTime? fromDate, DateTime? toDate)
        {
            var cat = (category ?? "CL").Trim().ToUpperInvariant();
            var list = new List<PayslipEmailStatusDTO>();

            var conn = _context.Database.GetDbConnection();
            bool wasOpen = conn.State == ConnectionState.Open;
            if (!wasOpen) await conn.OpenAsync();

            try
            {
                await EnsureEmailLogTableExistsAsync(conn);

                // 1. Try stored procedure SP_GET_PAYSLIP_EMAIL_STATUS
                try
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "dbo.SP_GET_PAYSLIP_EMAIL_STATUS";
                        cmd.CommandType = CommandType.StoredProcedure;

                        var pCat = cmd.CreateParameter();
                        pCat.ParameterName = "@Category";
                        pCat.Value = cat;
                        cmd.Parameters.Add(pCat);

                        var pFrom = cmd.CreateParameter();
                        pFrom.ParameterName = "@FromDate";
                        pFrom.Value = fromDate.HasValue ? (object)fromDate.Value.Date : DBNull.Value;
                        cmd.Parameters.Add(pFrom);

                        var pTo = cmd.CreateParameter();
                        pTo.ParameterName = "@ToDate";
                        pTo.Value = toDate.HasValue ? (object)toDate.Value.Date : DBNull.Value;
                        cmd.Parameters.Add(pTo);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var item = new PayslipEmailStatusDTO
                                {
                                    EmployeeId = GetStringAny(reader, "Emp_Id", "EmployeeId"),
                                    EmployeeName = GetStringAny(reader, "Emp_Nm", "EmployeeName"),
                                    SkillCategory = GetStringAny(reader, "Skill_Cat", "SkillCategory"),
                                    CategoryCode = GetStringAny(reader, "Ctg_Code", "CategoryCode"),
                                    VendorId = GetStringAny(reader, "Vendor_Id", "VendorId"),
                                    VendorName = GetStringAny(reader, "Vendor_Nm", "VendorName"),
                                    VendorEmail = GetStringAny(reader, "Vendor_Email", "VendorEmail"),
                                    FromDate = GetDateTime(reader, "From_Dt") ?? GetDateTime(reader, "FromDate"),
                                    ToDate = GetDateTime(reader, "To_Dt") ?? GetDateTime(reader, "ToDate"),
                                    MailSentStatus = GetStringAny(reader, "Mail_Sent_Status", "MailSentStatus"),
                                    MailSentOn = GetDateTime(reader, "Mail_Sent_On") ?? GetDateTime(reader, "MailSentOn"),
                                    Remarks = GetString(reader, "Remarks")
                                };
                                list.Add(item);
                            }
                        }
                    }
                }
                catch
                {
                    // Fallback to direct query from table if SP fails
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                            SELECT Emp_Id, Emp_Nm, Skill_Cat, Ctg_Code, Vendor_Id, Vendor_Nm, Vendor_Email,
                                   From_Dt, To_Dt, Mail_Sent_Status, Mail_Sent_On, Remarks
                            FROM dbo.YMT_PAYSLIP_EMAIL_LOG
                            WHERE UPPER(LTRIM(RTRIM(ISNULL(Ctg_Code, '')))) = @Category
                              AND (@FromDate IS NULL OR CAST(From_Dt AS DATE) = CAST(@FromDate AS DATE))
                              AND (@ToDate IS NULL OR CAST(To_Dt AS DATE) = CAST(@ToDate AS DATE))";
                        cmd.CommandType = CommandType.Text;

                        var pCat = cmd.CreateParameter();
                        pCat.ParameterName = "@Category";
                        pCat.Value = cat;
                        cmd.Parameters.Add(pCat);

                        var pFrom = cmd.CreateParameter();
                        pFrom.ParameterName = "@FromDate";
                        pFrom.Value = fromDate.HasValue ? (object)fromDate.Value.Date : DBNull.Value;
                        cmd.Parameters.Add(pFrom);

                        var pTo = cmd.CreateParameter();
                        pTo.ParameterName = "@ToDate";
                        pTo.Value = toDate.HasValue ? (object)toDate.Value.Date : DBNull.Value;
                        cmd.Parameters.Add(pTo);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var item = new PayslipEmailStatusDTO
                                {
                                    EmployeeId = GetStringAny(reader, "Emp_Id", "EmployeeId"),
                                    EmployeeName = GetStringAny(reader, "Emp_Nm", "EmployeeName"),
                                    SkillCategory = GetStringAny(reader, "Skill_Cat", "SkillCategory"),
                                    CategoryCode = GetStringAny(reader, "Ctg_Code", "CategoryCode"),
                                    VendorId = GetStringAny(reader, "Vendor_Id", "VendorId"),
                                    VendorName = GetStringAny(reader, "Vendor_Nm", "VendorName"),
                                    VendorEmail = GetStringAny(reader, "Vendor_Email", "VendorEmail"),
                                    FromDate = GetDateTime(reader, "From_Dt") ?? GetDateTime(reader, "FromDate"),
                                    ToDate = GetDateTime(reader, "To_Dt") ?? GetDateTime(reader, "ToDate"),
                                    MailSentStatus = GetStringAny(reader, "Mail_Sent_Status", "MailSentStatus"),
                                    MailSentOn = GetDateTime(reader, "Mail_Sent_On") ?? GetDateTime(reader, "MailSentOn"),
                                    Remarks = GetString(reader, "Remarks")
                                };
                                list.Add(item);
                            }
                        }
                    }
                }
            }
            catch
            {
                // return empty on non-blocking failure
            }
            finally
            {
                if (!wasOpen && conn.State == ConnectionState.Open)
                    await conn.CloseAsync();
            }

            return list;
        }

        public async Task<SendVendorEmailResultDTO> SendVendorEmailsAsync(SendVendorEmailRequestDTO dto)
        {
            var result = new SendVendorEmailResultDTO();
            if (dto == null)
            {
                result.Success = false;
                result.Message = "Request cannot be null";
                return result;
            }

            var cat = (dto.Category ?? "CL").Trim().ToUpperInvariant();

            // 1. Fetch Mail Configuration from YMT_MAIL_CONFIG (Top 1 with Status = '1')
            var mailConfig = await _context.MailConfigs
                .Where(m => m.Status == "1" || m.Status == "Active")
                .OrderByDescending(m => m.Id)
                .FirstOrDefaultAsync();

            if (mailConfig == null)
            {
                result.Success = false;
                result.Message = "Active email configuration not found in YMT_MAIL_CONFIG (Status = 1). Please configure SMTP settings.";
                return result;
            }

            if (string.IsNullOrWhiteSpace(mailConfig.EmailServer) || string.IsNullOrWhiteSpace(mailConfig.EmailFrom))
            {
                result.Success = false;
                result.Message = "Mail server (Mail_Srv) or From email (Mail_Frm) is not configured in YMT_MAIL_CONFIG.";
                return result;
            }

            int port = 587;
            if (!string.IsNullOrWhiteSpace(mailConfig.EmailPort) && int.TryParse(mailConfig.EmailPort.Trim(), out int parsedPort))
            {
                port = parsedPort;
            }

            // 2. Fetch Vendor Masters from YMT_VENDOR_MASTER
            var vendorMasters = await _context.VendorMasters.AsNoTracking().ToListAsync();
            var vendorByIdMap = new Dictionary<string, VendorMaster>(StringComparer.OrdinalIgnoreCase);
            var vendorByNameMap = new Dictionary<string, VendorMaster>(StringComparer.OrdinalIgnoreCase);

            foreach (var v in vendorMasters)
            {
                if (!string.IsNullOrWhiteSpace(v.VendorId))
                    vendorByIdMap[v.VendorId.Trim()] = v;
                if (!string.IsNullOrWhiteSpace(v.VendorName))
                    vendorByNameMap[v.VendorName.Trim()] = v;
            }

            // 3. Resolve Target Employees
            List<PayslipEmployeeDTO> targetEmployees = dto.Employees ?? new List<PayslipEmployeeDTO>();
            if (targetEmployees.Count == 0)
            {
                var allApproved = await GetApprovedPayslipDataAsync(cat, dto.FromDate, dto.ToDate);
                if (dto.EmployeeIds != null && dto.EmployeeIds.Count > 0)
                {
                    var idSet = new HashSet<string>(dto.EmployeeIds.Select(x => x.Trim().ToUpperInvariant()));
                    targetEmployees = allApproved.Where(x => idSet.Contains(x.EmployeeId.Trim().ToUpperInvariant())).ToList();
                }
                else
                {
                    targetEmployees = allApproved;
                }
            }

            if (targetEmployees.Count == 0)
            {
                result.Success = false;
                result.Message = "No approved employees selected for email delivery.";
                return result;
            }

            // 4. Group Employees by Vendor
            var groups = targetEmployees.GroupBy(e =>
            {
                var vId = (e.VendorId ?? "").Trim();
                if (!string.IsNullOrEmpty(vId)) return vId;
                var vNm = (e.Vendor ?? "").Trim();
                if (!string.IsNullOrEmpty(vNm)) return vNm;
                return "UNKNOWN";
            }).ToList();

            result.TotalVendors = groups.Count;
            result.TotalEmployees = targetEmployees.Count;

            var conn = _context.Database.GetDbConnection();
            bool wasOpen = conn.State == ConnectionState.Open;
            if (!wasOpen) await conn.OpenAsync();

            try
            {
                await EnsureEmailLogTableExistsAsync(conn);

                string fDateStr = dto.FromDate?.ToString("yyyy-MM-dd") ?? targetEmployees.FirstOrDefault(x => x.FromDate.HasValue)?.FromDate?.ToString("yyyy-MM-dd") ?? "";
                string tDateStr = dto.ToDate?.ToString("yyyy-MM-dd") ?? targetEmployees.FirstOrDefault(x => x.ToDate.HasValue)?.ToDate?.ToString("yyyy-MM-dd") ?? "";
                string periodLabel = (!string.IsNullOrEmpty(fDateStr) && !string.IsNullOrEmpty(tDateStr)) ? $"{fDateStr} to {tDateStr}" : "Current Period";

                foreach (var grp in groups)
                {
                    var vendorKey = grp.Key;
                    var empList = grp.ToList();

                    // Resolve Vendor Name and Vendor Email from YMT_VENDOR_MASTER
                    string vendorName = empList.FirstOrDefault()?.Vendor ?? vendorKey;
                    string vendorId = empList.FirstOrDefault()?.VendorId ?? vendorKey;
                    string vendorEmail = "";

                    if (vendorByIdMap.TryGetValue(vendorKey, out var vm1))
                    {
                        vendorEmail = vm1.VendorEmail?.Trim() ?? "";
                        if (!string.IsNullOrWhiteSpace(vm1.VendorName)) vendorName = vm1.VendorName.Trim();
                        if (!string.IsNullOrWhiteSpace(vm1.VendorId)) vendorId = vm1.VendorId.Trim();
                    }
                    else if (vendorByNameMap.TryGetValue(vendorName, out var vm2))
                    {
                        vendorEmail = vm2.VendorEmail?.Trim() ?? "";
                        if (!string.IsNullOrWhiteSpace(vm2.VendorId)) vendorId = vm2.VendorId.Trim();
                    }

                    if (string.IsNullOrWhiteSpace(vendorEmail))
                    {
                        string err = $"Vendor '{vendorName}' ({vendorId}) has no email address configured in YMT_VENDOR_MASTER.";
                        result.Errors.Add(err);

                        foreach (var emp in empList)
                        {
                            await SaveEmailLogAsync(conn, emp, cat, vendorId, vendorName, "", "FAILED", err, dto.SentBy);
                        }
                        continue;
                    }

                    try
                    {
                        bool isExcel = string.Equals(dto.FileFormat, "EXCEL", StringComparison.OrdinalIgnoreCase) ||
                                       string.Equals(dto.FileFormat, "XLSX", StringComparison.OrdinalIgnoreCase);

                        byte[] fileBytes;
                        string fileName;
                        string mimeType;
                        string formatLabel;

                        string cleanVnd = (vendorName ?? vendorKey).Replace(" ", "_").Replace("/", "_").Replace("\\", "_");

                        if (isExcel)
                        {
                            fileBytes = PayslipExcelGenerator.GeneratePayslipsExcel(empList, cat, fDateStr, tDateStr, vendorName, vendorId);
                            fileName = $"Payslips_{cleanVnd}_{fDateStr}.xlsx";
                            mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                            formatLabel = "Excel Spreadsheet";
                        }
                        else
                        {
                            // Doc / PDF format: all employee payslips combined into one consolidated multi-page PDF document
                            fileBytes = PayslipPdfGenerator.GenerateCombinedPayslipsPdf(empList, cat, fDateStr, tDateStr);
                            fileName = $"Payslips_{cleanVnd}_{fDateStr}.pdf";
                            mimeType = "application/pdf";
                            formatLabel = "Consolidated PDF Document";
                        }

                        // Send Email via SmtpClient
                        using (var client = new SmtpClient(mailConfig.EmailServer, port))
                        {
                            client.EnableSsl = (port == 587 || port == 465 || port == 25);
                            client.UseDefaultCredentials = false;
                            client.Credentials = new System.Net.NetworkCredential(mailConfig.EmailFrom.Trim(), mailConfig.EmailPassword?.Trim() ?? "");
                            client.DeliveryMethod = SmtpDeliveryMethod.Network;
                            client.Timeout = 45000;

                            using (var message = new MailMessage())
                            {
                                message.From = new MailAddress(mailConfig.EmailFrom.Trim(), "India Yamaha Motor - Payroll");
                                message.To.Add(vendorEmail);
                                message.Subject = $"India Yamaha Motor - Official Employee Payslips ({periodLabel}) - {vendorName} [{formatLabel}]";
                                message.IsBodyHtml = true;

                                // Build Email Body with table summary
                                var bodySb = new StringBuilder();
                                bodySb.Append("<div style='font-family: Arial, sans-serif; color: #1e293b; max-width: 680px; margin: 0 auto; border: 1px solid #e2e8f0; border-radius: 8px; overflow: hidden;'>");
                                bodySb.Append("<div style='background-color: #1e3a8a; color: #ffffff; padding: 18px 24px;'>");
                                bodySb.Append("<h2 style='margin: 0; font-size: 20px;'>INDIA YAMAHA MOTOR PVT. LTD.</h2>");
                                bodySb.Append($"<p style='margin: 4px 0 0 0; font-size: 13px; opacity: 0.9;'>Official Employee Payslips for <strong>{vendorName}</strong> | Period: {periodLabel}</p>");
                                bodySb.Append("</div>");

                                bodySb.Append("<div style='padding: 24px;'>");
                                bodySb.Append($"<p>Dear <strong>{vendorName}</strong>,</p>");
                                if (isExcel)
                                {
                                    bodySb.Append($"<p>Please find attached the official <strong>Excel Spreadsheet ({fileName})</strong> containing complete payslip and payroll calculations for all <strong>{empList.Count}</strong> employee(s) under category <strong>{cat}</strong> for the period <strong>{periodLabel}</strong>.</p>");
                                }
                                else
                                {
                                    bodySb.Append($"<p>Please find attached the official <strong>Consolidated PDF Document ({fileName})</strong> containing individual payslip pages for all <strong>{empList.Count}</strong> employee(s) under category <strong>{cat}</strong> for the period <strong>{periodLabel}</strong>.</p>");
                                }

                                bodySb.Append("<table style='width: 100%; border-collapse: collapse; margin-top: 16px; font-size: 12px;'>");
                                bodySb.Append("<tr style='background-color: #f1f5f9; text-align: left;'>");
                                bodySb.Append("<th style='padding: 8px; border: 1px solid #cbd5e1;'>Emp ID</th>");
                                bodySb.Append("<th style='padding: 8px; border: 1px solid #cbd5e1;'>Name</th>");
                                bodySb.Append("<th style='padding: 8px; border: 1px solid #cbd5e1;'>Skill</th>");
                                bodySb.Append("<th style='padding: 8px; border: 1px solid #cbd5e1; text-align: right;'>Working Days</th>");
                                bodySb.Append("<th style='padding: 8px; border: 1px solid #cbd5e1; text-align: right;'>Net Payable (Rs.)</th>");
                                bodySb.Append("</tr>");

                                foreach (var emp in empList)
                                {
                                    bodySb.Append("<tr>");
                                    bodySb.Append($"<td style='padding: 8px; border: 1px solid #e2e8f0; font-weight: bold;'>{emp.EmployeeId}</td>");
                                    bodySb.Append($"<td style='padding: 8px; border: 1px solid #e2e8f0;'>{emp.EmployeeName}</td>");
                                    bodySb.Append($"<td style='padding: 8px; border: 1px solid #e2e8f0;'>{emp.SkillCategory}</td>");
                                    bodySb.Append($"<td style='padding: 8px; border: 1px solid #e2e8f0; text-align: right;'>{(emp.WorkingDays ?? 0):F1}</td>");
                                    bodySb.Append($"<td style='padding: 8px; border: 1px solid #e2e8f0; text-align: right; font-weight: bold;'>Rs. {(emp.NetPayable ?? 0):N2}</td>");
                                    bodySb.Append("</tr>");
                                }
                                bodySb.Append("</table>");

                                bodySb.Append($"<p style='margin-top: 20px; font-size: 12px; color: #64748b;'>Attachment: <strong>{fileName}</strong> ({formatLabel}). For any queries, please reach out to the Payroll Department.</p>");
                                bodySb.Append("<p style='font-size: 12px; color: #64748b;'>Best regards,<br/><strong>India Yamaha Motor Pvt. Ltd.</strong></p>");
                                bodySb.Append("</div>");
                                bodySb.Append("</div>");

                                message.Body = bodySb.ToString();

                                using (var ms = new MemoryStream(fileBytes))
                                {
                                    message.Attachments.Add(new Attachment(ms, fileName, mimeType));
                                    await client.SendMailAsync(message);
                                }
                            }
                        }

                        // Success logging
                        result.SuccessfulVendors++;
                        result.SuccessfulEmployees += empList.Count;
                        string okMsg = $"Sent {empList.Count} payslip(s) via {formatLabel} to {vendorName} ({vendorEmail})";
                        result.Details.Add(okMsg);

                        foreach (var emp in empList)
                        {
                            await SaveEmailLogAsync(conn, emp, cat, vendorId, vendorName, vendorEmail, "SENT", okMsg, dto.SentBy);
                        }
                    }
                    catch (Exception ex)
                    {
                        string err = $"Failed to send email to {vendorName} ({vendorEmail}): {ex.Message}";
                        result.Errors.Add(err);

                        foreach (var emp in empList)
                        {
                            await SaveEmailLogAsync(conn, emp, cat, vendorId, vendorName, vendorEmail, "FAILED", ex.Message, dto.SentBy);
                        }
                    }
                }
            }
            finally
            {
                if (!wasOpen && conn.State == ConnectionState.Open)
                    await conn.CloseAsync();
            }

            result.Success = result.SuccessfulEmployees > 0;
            result.Message = result.Success
                ? $"Emails successfully sent to {result.SuccessfulVendors} vendor(s) for {result.SuccessfulEmployees} employee(s)!"
                : $"Failed to send vendor emails: {string.Join("; ", result.Errors)}";

            return result;
        }

        private static async Task EnsureEmailLogTableExistsAsync(DbConnection conn)
        {
            try
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        IF OBJECT_ID('dbo.YMT_PAYSLIP_EMAIL_LOG', 'U') IS NULL
                        BEGIN
                            CREATE TABLE [dbo].[YMT_PAYSLIP_EMAIL_LOG] (
                                [Id] INT IDENTITY(1,1) PRIMARY KEY,
                                [Emp_Id] VARCHAR(50) NOT NULL,
                                [Emp_Nm] NVARCHAR(150) NULL,
                                [Skill_Cat] NVARCHAR(100) NULL,
                                [Ctg_Code] VARCHAR(20) NOT NULL,
                                [Vendor_Id] VARCHAR(50) NULL,
                                [Vendor_Nm] NVARCHAR(150) NULL,
                                [Vendor_Email] NVARCHAR(150) NULL,
                                [From_Dt] DATE NULL,
                                [To_Dt] DATE NULL,
                                [Mail_Sent_Status] VARCHAR(20) NOT NULL DEFAULT 'FAILED',
                                [Mail_Sent_On] DATETIME NULL,
                                [Remarks] NVARCHAR(500) NULL,
                                [Crtd_By] NVARCHAR(100) NULL,
                                [Crtd_On] DATETIME DEFAULT GETDATE(),
                                [Mdfd_On] DATETIME NULL
                            );
                            CREATE INDEX IX_PAYSLIP_EMAIL_LOG_EMP ON [dbo].[YMT_PAYSLIP_EMAIL_LOG] (Emp_Id, Ctg_Code, From_Dt, To_Dt);
                        END";
                    cmd.CommandType = CommandType.Text;
                    await cmd.ExecuteNonQueryAsync();
                }

                // Ensure Stored Procedures exist
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        IF OBJECT_ID('dbo.SP_SAVE_PAYSLIP_EMAIL_LOG', 'P') IS NULL
                        BEGIN
                            EXEC('
                            CREATE PROCEDURE [dbo].[SP_SAVE_PAYSLIP_EMAIL_LOG]
                                @Emp_Id VARCHAR(50),
                                @Emp_Nm NVARCHAR(150) = NULL,
                                @Skill_Cat NVARCHAR(100) = NULL,
                                @Ctg_Code VARCHAR(20),
                                @Vendor_Id VARCHAR(50) = NULL,
                                @Vendor_Nm NVARCHAR(150) = NULL,
                                @Vendor_Email NVARCHAR(150) = NULL,
                                @From_Dt DATE = NULL,
                                @To_Dt DATE = NULL,
                                @Mail_Sent_Status VARCHAR(20) = ''SENT'',
                                @Remarks NVARCHAR(500) = NULL,
                                @Crtd_By NVARCHAR(100) = ''System''
                            AS
                            BEGIN
                                SET NOCOUNT ON;
                                IF EXISTS (
                                    SELECT 1 FROM [dbo].[YMT_PAYSLIP_EMAIL_LOG]
                                    WHERE Emp_Id = @Emp_Id AND Ctg_Code = @Ctg_Code
                                      AND ((@From_Dt IS NULL AND From_Dt IS NULL) OR From_Dt = @From_Dt)
                                      AND ((@To_Dt IS NULL AND To_Dt IS NULL) OR To_Dt = @To_Dt)
                                )
                                BEGIN
                                    UPDATE [dbo].[YMT_PAYSLIP_EMAIL_LOG]
                                    SET Emp_Nm = ISNULL(@Emp_Nm, Emp_Nm),
                                        Skill_Cat = ISNULL(@Skill_Cat, Skill_Cat),
                                        Vendor_Id = ISNULL(@Vendor_Id, Vendor_Id),
                                        Vendor_Nm = ISNULL(@Vendor_Nm, Vendor_Nm),
                                        Vendor_Email = ISNULL(@Vendor_Email, Vendor_Email),
                                        Mail_Sent_Status = @Mail_Sent_Status,
                                        Mail_Sent_On = CASE WHEN @Mail_Sent_Status = ''SENT'' THEN GETDATE() ELSE Mail_Sent_On END,
                                        Remarks = @Remarks,
                                        Mdfd_On = GETDATE()
                                    WHERE Emp_Id = @Emp_Id AND Ctg_Code = @Ctg_Code
                                      AND ((@From_Dt IS NULL AND From_Dt IS NULL) OR From_Dt = @From_Dt)
                                      AND ((@To_Dt IS NULL AND To_Dt IS NULL) OR To_Dt = @To_Dt);
                                END
                                ELSE
                                BEGIN
                                    INSERT INTO [dbo].[YMT_PAYSLIP_EMAIL_LOG] (
                                        Emp_Id, Emp_Nm, Skill_Cat, Ctg_Code, Vendor_Id, Vendor_Nm, Vendor_Email,
                                        From_Dt, To_Dt, Mail_Sent_Status, Mail_Sent_On, Remarks, Crtd_By, Crtd_On
                                    )
                                    VALUES (
                                        @Emp_Id, @Emp_Nm, @Skill_Cat, @Ctg_Code, @Vendor_Id, @Vendor_Nm, @Vendor_Email,
                                        @From_Dt, @To_Dt, @Mail_Sent_Status, 
                                        CASE WHEN @Mail_Sent_Status = ''SENT'' THEN GETDATE() ELSE NULL END, 
                                        @Remarks, @Crtd_By, GETDATE()
                                    );
                                END
                            END');
                        END";
                    cmd.CommandType = CommandType.Text;
                    await cmd.ExecuteNonQueryAsync();
                }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        IF OBJECT_ID('dbo.SP_GET_PAYSLIP_EMAIL_STATUS', 'P') IS NULL
                        BEGIN
                            EXEC('
                            CREATE PROCEDURE [dbo].[SP_GET_PAYSLIP_EMAIL_STATUS]
                                @Category VARCHAR(20) = ''CL'',
                                @FromDate DATE = NULL,
                                @ToDate DATE = NULL
                            AS
                            BEGIN
                                SET NOCOUNT ON;
                                SELECT Emp_Id, Emp_Nm, Skill_Cat, Ctg_Code, Vendor_Id, Vendor_Nm, Vendor_Email,
                                       From_Dt, To_Dt, Mail_Sent_Status, Mail_Sent_On, Remarks
                                FROM [dbo].[YMT_PAYSLIP_EMAIL_LOG]
                                WHERE UPPER(LTRIM(RTRIM(Ctg_Code))) = UPPER(LTRIM(RTRIM(@Category)))
                                  AND (@FromDate IS NULL OR From_Dt = @FromDate)
                                  AND (@ToDate IS NULL OR To_Dt = @ToDate);
                            END');
                        END";
                    cmd.CommandType = CommandType.Text;
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch
            {
                // Non-blocking
            }
        }

        private static async Task SaveEmailLogAsync(
            DbConnection conn,
            PayslipEmployeeDTO emp,
            string category,
            string vendorId,
            string vendorName,
            string vendorEmail,
            string status,
            string remarks,
            string? sentBy)
        {
            try
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "dbo.SP_SAVE_PAYSLIP_EMAIL_LOG";
                    cmd.CommandType = CommandType.StoredProcedure;

                    void AddParam(string name, object? val)
                    {
                        var p = cmd.CreateParameter();
                        p.ParameterName = name;
                        p.Value = val ?? DBNull.Value;
                        cmd.Parameters.Add(p);
                    }

                    AddParam("@Emp_Id", emp.EmployeeId);
                    AddParam("@Emp_Nm", emp.EmployeeName);
                    AddParam("@Skill_Cat", emp.SkillCategory);
                    AddParam("@Ctg_Code", category);
                    AddParam("@Vendor_Id", string.IsNullOrWhiteSpace(emp.VendorId) ? vendorId : emp.VendorId);
                    AddParam("@Vendor_Nm", string.IsNullOrWhiteSpace(emp.Vendor) ? vendorName : emp.Vendor);
                    AddParam("@Vendor_Email", vendorEmail);
                    AddParam("@From_Dt", emp.FromDate.HasValue ? (object)emp.FromDate.Value.Date : DBNull.Value);
                    AddParam("@To_Dt", emp.ToDate.HasValue ? (object)emp.ToDate.Value.Date : DBNull.Value);
                    AddParam("@Mail_Sent_Status", status);
                    AddParam("@Remarks", remarks);
                    AddParam("@Crtd_By", sentBy ?? "System");

                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch
            {
                // direct fallback upsert
                try
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                            IF EXISTS (
                                SELECT 1 FROM dbo.YMT_PAYSLIP_EMAIL_LOG
                                WHERE Emp_Id = @Emp_Id AND Ctg_Code = @Ctg_Code
                                  AND ((@From_Dt IS NULL AND From_Dt IS NULL) OR From_Dt = @From_Dt)
                                  AND ((@To_Dt IS NULL AND To_Dt IS NULL) OR To_Dt = @To_Dt)
                            )
                            BEGIN
                                UPDATE dbo.YMT_PAYSLIP_EMAIL_LOG
                                SET Mail_Sent_Status = @Status,
                                    Mail_Sent_On = CASE WHEN @Status = 'SENT' THEN GETDATE() ELSE Mail_Sent_On END,
                                    Vendor_Email = @Vendor_Email,
                                    Remarks = @Remarks,
                                    Mdfd_On = GETDATE()
                                WHERE Emp_Id = @Emp_Id AND Ctg_Code = @Ctg_Code
                                  AND ((@From_Dt IS NULL AND From_Dt IS NULL) OR From_Dt = @From_Dt)
                                  AND ((@To_Dt IS NULL AND To_Dt IS NULL) OR To_Dt = @To_Dt);
                            END
                            ELSE
                            BEGIN
                                INSERT INTO dbo.YMT_PAYSLIP_EMAIL_LOG
                                    (Emp_Id, Emp_Nm, Skill_Cat, Ctg_Code, Vendor_Id, Vendor_Nm, Vendor_Email, From_Dt, To_Dt, Mail_Sent_Status, Mail_Sent_On, Remarks, Crtd_By, Crtd_On)
                                VALUES
                                    (@Emp_Id, @Emp_Nm, @Skill_Cat, @Ctg_Code, @Vendor_Id, @Vendor_Nm, @Vendor_Email, @From_Dt, @To_Dt, @Status, CASE WHEN @Status = 'SENT' THEN GETDATE() ELSE NULL END, @Remarks, @Crtd_By, GETDATE());
                            END";
                        cmd.CommandType = CommandType.Text;

                        void AddParam(string name, object? val)
                        {
                            var p = cmd.CreateParameter();
                            p.ParameterName = name;
                            p.Value = val ?? DBNull.Value;
                            cmd.Parameters.Add(p);
                        }

                        AddParam("@Emp_Id", emp.EmployeeId);
                        AddParam("@Emp_Nm", emp.EmployeeName);
                        AddParam("@Skill_Cat", emp.SkillCategory);
                        AddParam("@Ctg_Code", category);
                        AddParam("@Vendor_Id", string.IsNullOrWhiteSpace(emp.VendorId) ? vendorId : emp.VendorId);
                        AddParam("@Vendor_Nm", string.IsNullOrWhiteSpace(emp.Vendor) ? vendorName : emp.Vendor);
                        AddParam("@Vendor_Email", vendorEmail);
                        AddParam("@From_Dt", emp.FromDate.HasValue ? (object)emp.FromDate.Value.Date : DBNull.Value);
                        AddParam("@To_Dt", emp.ToDate.HasValue ? (object)emp.ToDate.Value.Date : DBNull.Value);
                        AddParam("@Status", status);
                        AddParam("@Remarks", remarks);
                        AddParam("@Crtd_By", sentBy ?? "System");

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                catch
                {
                    // non-blocking
                }
            }
        }

        #region Safe Reader Helpers
        private static string GetString(DbDataReader reader, string col)
        {
            try
            {
                int ord = reader.GetOrdinal(col);
                return reader.IsDBNull(ord) ? string.Empty : reader.GetValue(ord).ToString()?.Trim() ?? string.Empty;
            }
            catch { return string.Empty; }
        }

        private static string GetStringAny(DbDataReader reader, params string[] cols)
        {
            foreach (var col in cols)
            {
                var val = GetString(reader, col);
                if (!string.IsNullOrWhiteSpace(val)) return val;
            }
            return string.Empty;
        }

        private static decimal? GetDecimal(DbDataReader reader, string col)
        {
            try
            {
                int ord = reader.GetOrdinal(col);
                if (reader.IsDBNull(ord)) return null;
                return Convert.ToDecimal(reader.GetValue(ord));
            }
            catch { return null; }
        }

        private static decimal? GetDecimalAny(DbDataReader reader, params string[] cols)
        {
            foreach (var col in cols)
            {
                var val = GetDecimal(reader, col);
                if (val.HasValue) return val;
            }
            return null;
        }

        private static DateTime? GetDateTime(DbDataReader reader, string col)
        {
            try
            {
                int ord = reader.GetOrdinal(col);
                if (reader.IsDBNull(ord)) return null;
                var val = reader.GetValue(ord);
                if (val is DateTime dt) return dt;
                if (DateTime.TryParse(val.ToString(), out var parsed)) return parsed;
                return null;
            }
            catch { return null; }
        }

        private static int? GetInt(DbDataReader reader, string col)
        {
            try
            {
                int ord = reader.GetOrdinal(col);
                if (reader.IsDBNull(ord)) return null;
                return Convert.ToInt32(reader.GetValue(ord));
            }
            catch { return null; }
        }
        #endregion
    }
}
