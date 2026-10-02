using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using YMI_PMT_PayrollManagement_API.Data;
using YMI_PMT_PayrollManagement_API.DTOs.Report;
using YMI_PMT_PayrollManagement_API.Interfaces.Repository;

namespace YMI_PMT_PayrollManagement_API.Repository
{
    public class PayrollReportRepository : IPayrollReportRepository
    {
        private readonly AppDbContext _context;

        public PayrollReportRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<PayrollReportRowDTO>> GetPayrollReportAsync(PayrollReportFilterDTO filter)
        {
            var list = new List<PayrollReportRowDTO>();
            var cat = (filter?.Category ?? "CL").Trim().ToUpperInvariant();

            var conn = _context.Database.GetDbConnection();
            bool wasOpen = conn.State == ConnectionState.Open;
            if (!wasOpen) await conn.OpenAsync();

            try
            {
                // 1. Try to call the dedicated Stored Procedure: dbo.SP_GET_PAYROLL_REPORT
                bool spSucceeded = false;
                try
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "dbo.SP_GET_PAYROLL_REPORT";
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandTimeout = 60;

                        var pCat = cmd.CreateParameter();
                        pCat.ParameterName = "@Category";
                        pCat.Value = string.IsNullOrWhiteSpace(cat) ? (object)DBNull.Value : cat;
                        cmd.Parameters.Add(pCat);

                        var pFrom = cmd.CreateParameter();
                        pFrom.ParameterName = "@FromDate";
                        pFrom.Value = filter?.FromDate.HasValue == true ? (object)filter.FromDate.Value.Date : DBNull.Value;
                        cmd.Parameters.Add(pFrom);

                        var pTo = cmd.CreateParameter();
                        pTo.ParameterName = "@ToDate";
                        pTo.Value = filter?.ToDate.HasValue == true ? (object)filter.ToDate.Value.Date : DBNull.Value;
                        cmd.Parameters.Add(pTo);

                        var pEmp = cmd.CreateParameter();
                        pEmp.ParameterName = "@EmpId";
                        pEmp.Value = !string.IsNullOrWhiteSpace(filter?.EmployeeId) ? (object)filter.EmployeeId.Trim() : DBNull.Value;
                        cmd.Parameters.Add(pEmp);

                        var pSkill = cmd.CreateParameter();
                        pSkill.ParameterName = "@SkillCat";
                        pSkill.Value = !string.IsNullOrWhiteSpace(filter?.SkillCategory) ? (object)filter.SkillCategory.Trim() : DBNull.Value;
                        cmd.Parameters.Add(pSkill);

                        var pVendor = cmd.CreateParameter();
                        pVendor.ParameterName = "@Vendor";
                        pVendor.Value = !string.IsNullOrWhiteSpace(filter?.Vendor) ? (object)filter.Vendor.Trim() : DBNull.Value;
                        cmd.Parameters.Add(pVendor);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                list.Add(ReadReportRow(reader));
                            }
                        }
                        spSucceeded = true;
                    }
                }
                catch
                {
                    // SP doesn't exist yet or had a syntax mismatch, will execute direct resilient query
                    spSucceeded = false;
                }

                // 2. Direct ADO.NET Fallback Query if SP is not yet deployed
                if (!spSucceeded)
                {
                    list.Clear();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                            SELECT 
                                c.Id,
                                c.Emp_Id AS EmployeeId,
                                ISNULL(NULLIF(LTRIM(RTRIM(c.Emp_Nm)), ''), ISNULL(NULLIF(LTRIM(RTRIM(m.Emp_Nm)), ''), '-')) AS EmployeeName,
                                c.Ctg_Code AS CategoryCode,
                                ISNULL(NULLIF(LTRIM(RTRIM(m.Skill_Cat)), ''), ISNULL(NULLIF(LTRIM(RTRIM(c.Ctg_Code)), ''), '-')) AS SkillCategory,
                                ISNULL(NULLIF(LTRIM(RTRIM(c.Vendor)), ''), ISNULL(NULLIF(LTRIM(RTRIM(m.Vendor)), ''), '-')) AS Vendor,
                                ISNULL(NULLIF(LTRIM(RTRIM(m.Vendor_Id)), ''), ISNULL(NULLIF(LTRIM(RTRIM(c.Vendor)), ''), '-')) AS VendorId,
                                c.From_Dt AS FromDate,
                                c.To_Dt AS ToDate,
                                ISNULL(NULLIF(LTRIM(RTRIM(m.Qualify)), ''), ISNULL(NULLIF(LTRIM(RTRIM(m.Dept_Nm)), ''), '-')) AS Designation,
                                m.DOJ AS DateOfJoining,
                                m.DOL AS DateOfLeaving,
                                ISNULL(NULLIF(LTRIM(RTRIM(m.PF_No)), ''), '-') AS PfNo,
                                ISNULL(NULLIF(LTRIM(RTRIM(m.ESI_No)), ''), '-') AS EsiNo,
                                ISNULL(NULLIF(LTRIM(RTRIM(m.PAN_No)), ''), '-') AS PanNo,
                                ISNULL(NULLIF(LTRIM(RTRIM(m.UAN_No)), ''), '-') AS UanNo,
                                ISNULL(c.Period_Days, DATEDIFF(DAY, c.From_Dt, c.To_Dt) + 1) AS PeriodDays,
                                ISNULL(c.Pay_Days_Full, 0) AS PayDaysFull,
                                ISNULL(c.Pay_Days_Half, 0) AS PayDaysHalf,
                                ISNULL(c.Pay_Days_Full, 0) + ISNULL(c.Pay_Days_Half, 0) AS WorkedDays,
                                ISNULL(c.TWD, 0) AS Twd,
                                ISNULL(c.OT_Hrs, 0) AS OtHrs,

                                -- Earnings (CL)
                                ISNULL(c.Attn_Bonus, 0) AS AttnBonus,
                                ISNULL(c.Earned_Basic_DA, 0) AS EarnedBasicDa,
                                ISNULL(c.Earned_HRA, 0) AS EarnedHra,
                                ISNULL(c.OT_Amount, 0) AS OtAmount,
                                ISNULL(c.EPF, 0) AS Epf,
                                ISNULL(c.ESI, 0) AS Esi,
                                ISNULL(c.Bonus_Ded, 0) AS BonusDed,
                                ISNULL(c.Ser_Char, 0) AS SerChar,
                                ISNULL(c.LWF, 0) AS Lwf,
                                ISNULL(c.Gross, 0) AS Gross,

                                -- Earnings (NAPS)
                                ISNULL(c.Earned_Wages, 0) AS EarnedWages,
                                ISNULL(c.OT_Earning, 0) AS OtEarning,
                                ISNULL(c.Handling_Charge, 0) AS HandlingCharge,
                                CASE 
                                    WHEN UPPER(LTRIM(RTRIM(ISNULL(c.Ctg_Code, '')))) = 'NAPS' 
                                    THEN ISNULL(c.Earned_Wages, 0) + ISNULL(c.OT_Earning, 0) + ISNULL(c.Handling_Charge, 0)
                                    ELSE ISNULL(c.Gross, 0)
                                END AS NapsGross,

                                -- Deductions
                                ISNULL(c.PF_Ded, 0) AS PfDed,
                                ISNULL(c.ESI_Ded, 0) AS EsiDed,
                                ISNULL(c.Hostel_Ded, 0) AS HostelDed,
                                CASE 
                                    WHEN UPPER(LTRIM(RTRIM(ISNULL(c.Ctg_Code, '')))) = 'NAPS' 
                                    THEN ISNULL(c.Hostel_Ded, 0)
                                    ELSE ISNULL(c.PF_Ded, 0) + ISNULL(c.ESI_Ded, 0) + ISNULL(c.Hostel_Ded, 0)
                                END AS TotalDeductions,
                                ISNULL(c.Total_Ded, 0) AS TotalDed,

                                -- Provisions (Attr1 = Transaction_ID & Attr2 = Version)
                                ISNULL(prv.Ref_Bon, 0) AS RefBon,
                                ISNULL(prv.Spl_Allw, 0) AS SplAllw,
                                ISNULL(prv.Kaizen, 0) AS Kaizen,
                                ISNULL(prv.GPA_GMC, 0) AS GpaGmc,
                                ISNULL(prv.Ref_Bon, 0) + ISNULL(prv.Spl_Allw, 0) + ISNULL(prv.Kaizen, 0) + ISNULL(prv.GPA_GMC, 0) AS VariableTotal,

                                -- Net Payable
                                ISNULL(c.Net_Payable, 0) AS NetPayable,

                                -- Status & Approval Dates
                                ISNULL(c.Status, 'Pending') AS Status,
                                ISNULL(c.Checker_Status, 'Pending') AS CheckerStatus,
                                c.Checker_By AS CheckerBy,
                                c.Checker_On AS CheckerOn,
                                ISNULL(c.Approver_Status, 'Pending') AS ApproverStatus,
                                c.Approver_By AS ApproverBy,
                                c.Approver_On AS ApproverOn,
                                c.Remarks,
                                c.Transaction_ID AS TransactionId,
                                c.Version,
                                c.Crtd_On AS CreatedOn
                            FROM dbo.YMT_PAYROLL_CALCULATION c
                            LEFT JOIN dbo.YMT_EMP_MASTER m 
                                ON UPPER(LTRIM(RTRIM(c.Emp_Id))) = UPPER(LTRIM(RTRIM(m.Emp_Id)))
                            OUTER APPLY (
                                SELECT TOP (1) p.*
                                FROM dbo.YMT_PAYROLL_PROVISIONS p
                                WHERE UPPER(LTRIM(RTRIM(p.Emp_Id))) = UPPER(LTRIM(RTRIM(c.Emp_Id)))
                                  AND (
                                      -- 1) Exact match on Transaction_ID & Version (saved in Attr1 & Attr2)
                                      (p.Attr1 IS NOT NULL AND LTRIM(RTRIM(p.Attr1)) = LTRIM(RTRIM(c.Transaction_ID))
                                       AND (LTRIM(RTRIM(p.Attr2)) = LTRIM(RTRIM(c.Version)) OR c.Version IS NULL))
                                      -- 2) Fallback to date range & category if Attr1 is null
                                      OR (
                                          (p.Attr1 IS NULL OR LTRIM(RTRIM(p.Attr1)) = '')
                                          AND (p.Ctg_Code IS NULL OR UPPER(LTRIM(RTRIM(p.Ctg_Code))) = UPPER(LTRIM(RTRIM(c.Ctg_Code))))
                                          AND (
                                              (CAST(p.From_Dt AS DATE) = CAST(c.From_Dt AS DATE) AND CAST(p.To_Dt AS DATE) = CAST(c.To_Dt AS DATE))
                                              OR (YEAR(p.From_Dt) = YEAR(c.From_Dt) AND MONTH(p.From_Dt) = MONTH(c.From_Dt))
                                          )
                                      )
                                  )
                                ORDER BY 
                                    CASE 
                                        WHEN p.Attr1 IS NOT NULL AND LTRIM(RTRIM(p.Attr1)) = LTRIM(RTRIM(c.Transaction_ID)) 
                                             AND LTRIM(RTRIM(p.Attr2)) = LTRIM(RTRIM(c.Version)) THEN 1
                                        WHEN p.Attr1 IS NOT NULL AND LTRIM(RTRIM(p.Attr1)) = LTRIM(RTRIM(c.Transaction_ID)) THEN 2
                                        ELSE 3 
                                    END,
                                    ISNULL(p.Mdfd_On, p.Crtd_On) DESC,
                                    p.Id DESC
                            ) prv
                            WHERE (@Category IS NULL OR @Category = '' OR UPPER(LTRIM(RTRIM(ISNULL(c.Ctg_Code, '')))) = UPPER(LTRIM(RTRIM(@Category))))
                              AND (@FromDate IS NULL OR CAST(c.From_Dt AS DATE) >= @FromDate)
                              AND (@ToDate IS NULL OR CAST(c.To_Dt AS DATE) <= @ToDate)
                              AND (@EmpId IS NULL OR @EmpId = '' OR UPPER(LTRIM(RTRIM(c.Emp_Id))) = UPPER(LTRIM(RTRIM(@EmpId))))
                              AND (@SkillCat IS NULL OR @SkillCat = '' OR UPPER(LTRIM(RTRIM(ISNULL(m.Skill_Cat, '')))) = UPPER(LTRIM(RTRIM(@SkillCat))))
                              AND (@Vendor IS NULL OR @Vendor = '' OR UPPER(LTRIM(RTRIM(ISNULL(c.Vendor, ISNULL(m.Vendor, ''))))) = UPPER(LTRIM(RTRIM(@Vendor))))
                            ORDER BY 
                                c.From_Dt DESC, 
                                c.Emp_Id ASC, 
                                TRY_CAST(REPLACE(UPPER(c.Version), 'V', '') AS INT) DESC,
                                c.Version DESC,
                                c.Id DESC";

                        cmd.CommandType = CommandType.Text;
                        cmd.CommandTimeout = 60;

                        var pCat = cmd.CreateParameter();
                        pCat.ParameterName = "@Category";
                        pCat.Value = string.IsNullOrWhiteSpace(cat) ? (object)DBNull.Value : cat;
                        cmd.Parameters.Add(pCat);

                        var pFrom = cmd.CreateParameter();
                        pFrom.ParameterName = "@FromDate";
                        pFrom.Value = filter?.FromDate.HasValue == true ? (object)filter.FromDate.Value.Date : DBNull.Value;
                        cmd.Parameters.Add(pFrom);

                        var pTo = cmd.CreateParameter();
                        pTo.ParameterName = "@ToDate";
                        pTo.Value = filter?.ToDate.HasValue == true ? (object)filter.ToDate.Value.Date : DBNull.Value;
                        cmd.Parameters.Add(pTo);

                        var pEmp = cmd.CreateParameter();
                        pEmp.ParameterName = "@EmpId";
                        pEmp.Value = !string.IsNullOrWhiteSpace(filter?.EmployeeId) ? (object)filter.EmployeeId.Trim() : DBNull.Value;
                        cmd.Parameters.Add(pEmp);

                        var pSkill = cmd.CreateParameter();
                        pSkill.ParameterName = "@SkillCat";
                        pSkill.Value = !string.IsNullOrWhiteSpace(filter?.SkillCategory) ? (object)filter.SkillCategory.Trim() : DBNull.Value;
                        cmd.Parameters.Add(pSkill);

                        var pVendor = cmd.CreateParameter();
                        pVendor.ParameterName = "@Vendor";
                        pVendor.Value = !string.IsNullOrWhiteSpace(filter?.Vendor) ? (object)filter.Vendor.Trim() : DBNull.Value;
                        cmd.Parameters.Add(pVendor);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                list.Add(ReadReportRow(reader));
                            }
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

        public async Task<PayrollReportFilterOptionsDTO> GetFilterOptionsAsync(string category)
        {
            var res = new PayrollReportFilterOptionsDTO();
            var cat = (category ?? "CL").Trim().ToUpperInvariant();

            var conn = _context.Database.GetDbConnection();
            bool wasOpen = conn.State == ConnectionState.Open;
            if (!wasOpen) await conn.OpenAsync();

            try
            {
                // 1. Periods
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT DISTINCT 
                            CAST(From_Dt AS DATE) AS FromDate, 
                            CAST(To_Dt AS DATE) AS ToDate
                        FROM dbo.YMT_PAYROLL_CALCULATION
                        WHERE From_Dt IS NOT NULL AND To_Dt IS NOT NULL
                          AND (@Cat = '' OR UPPER(LTRIM(RTRIM(Ctg_Code))) = @Cat)
                        ORDER BY FromDate DESC";
                    cmd.CommandType = CommandType.Text;

                    var p = cmd.CreateParameter();
                    p.ParameterName = "@Cat";
                    p.Value = cat;
                    cmd.Parameters.Add(p);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var f = GetDateTime(reader, "FromDate");
                            var t = GetDateTime(reader, "ToDate");
                            if (f.HasValue && t.HasValue)
                            {
                                string fStr = f.Value.ToString("yyyy-MM-dd");
                                string tStr = t.Value.ToString("yyyy-MM-dd");
                                res.Periods.Add(new ReportPeriodOption
                                {
                                    FromDate = fStr,
                                    ToDate = tStr,
                                    Key = $"{fStr}|{tStr}",
                                    Label = $"{f.Value:dd-MMM-yyyy} to {t.Value:dd-MMM-yyyy}"
                                });
                            }
                        }
                    }
                }

                // 2. Employees (EmpId & EmpNm)
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT DISTINCT 
                            LTRIM(RTRIM(c.Emp_Id)) AS EmpId,
                            ISNULL(NULLIF(LTRIM(RTRIM(c.Emp_Nm)), ''), ISNULL(NULLIF(LTRIM(RTRIM(m.Emp_Nm)), ''), '-')) AS EmpNm
                        FROM dbo.YMT_PAYROLL_CALCULATION c
                        LEFT JOIN dbo.YMT_EMP_MASTER m ON UPPER(LTRIM(RTRIM(c.Emp_Id))) = UPPER(LTRIM(RTRIM(m.Emp_Id)))
                        WHERE (@Cat = '' OR UPPER(LTRIM(RTRIM(c.Ctg_Code))) = @Cat)
                          AND c.Emp_Id IS NOT NULL AND LTRIM(RTRIM(c.Emp_Id)) <> ''
                        ORDER BY EmpId ASC";
                    cmd.CommandType = CommandType.Text;

                    var p = cmd.CreateParameter();
                    p.ParameterName = "@Cat";
                    p.Value = cat;
                    cmd.Parameters.Add(p);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string eId = reader["EmpId"]?.ToString() ?? "";
                            string eNm = reader["EmpNm"]?.ToString() ?? "";
                            if (!string.IsNullOrEmpty(eId))
                            {
                                res.Employees.Add(new EmployeeDropdownOption
                                {
                                    EmployeeId = eId,
                                    EmployeeName = eNm,
                                    Label = $"{eId} | {eNm}"
                                });
                            }
                        }
                    }
                }

                // 3. Skill Categories
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT DISTINCT LTRIM(RTRIM(Skill_Cat)) AS SkillCat
                        FROM dbo.YMT_EMP_MASTER
                        WHERE Skill_Cat IS NOT NULL AND LTRIM(RTRIM(Skill_Cat)) <> ''
                          AND UPPER(LTRIM(RTRIM(Skill_Cat))) NOT IN ('CL', 'NAPS', '-')
                        ORDER BY SkillCat ASC";
                    cmd.CommandType = CommandType.Text;

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string s = reader["SkillCat"]?.ToString() ?? "";
                            if (!string.IsNullOrEmpty(s)) res.SkillCategories.Add(s);
                        }
                    }
                }

                // 4. Vendors
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT DISTINCT LTRIM(RTRIM(Vendor)) AS VendorName
                        FROM dbo.YMT_PAYROLL_CALCULATION
                        WHERE Vendor IS NOT NULL AND LTRIM(RTRIM(Vendor)) <> '' AND Vendor <> '-'
                        UNION
                        SELECT DISTINCT LTRIM(RTRIM(Vnd_Nm)) AS VendorName
                        FROM dbo.YMT_VENDOR_MASTER
                        WHERE Vnd_Nm IS NOT NULL AND LTRIM(RTRIM(Vnd_Nm)) <> ''
                        ORDER BY VendorName ASC";
                    cmd.CommandType = CommandType.Text;

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string v = reader["VendorName"]?.ToString() ?? "";
                            if (!string.IsNullOrEmpty(v)) res.Vendors.Add(v);
                        }
                    }
                }
            }
            finally
            {
                if (!wasOpen && conn.State == ConnectionState.Open)
                    await conn.CloseAsync();
            }

            return res;
        }

        private static PayrollReportRowDTO ReadReportRow(DbDataReader r)
        {
            var dto = new PayrollReportRowDTO();

            dto.Id = GetInt(r, "Id") ?? 0;
            dto.EmployeeId = GetString(r, "EmployeeId") ?? GetString(r, "Emp_Id") ?? "";
            dto.EmployeeName = GetString(r, "EmployeeName") ?? GetString(r, "Emp_Nm") ?? "";
            dto.CategoryCode = GetString(r, "CategoryCode") ?? GetString(r, "Ctg_Code") ?? "";
            dto.SkillCategory = GetString(r, "SkillCategory") ?? GetString(r, "Skill_Cat") ?? "-";
            dto.Vendor = GetString(r, "Vendor") ?? "-";
            dto.VendorId = GetString(r, "VendorId") ?? GetString(r, "Vendor_Id") ?? "-";
            dto.FromDate = GetDateTime(r, "FromDate") ?? GetDateTime(r, "From_Dt");
            dto.ToDate = GetDateTime(r, "ToDate") ?? GetDateTime(r, "To_Dt");
            dto.Designation = GetString(r, "Designation") ?? GetString(r, "Qualify") ?? "-";
            dto.DateOfJoining = GetDateTime(r, "DateOfJoining") ?? GetDateTime(r, "DOJ");
            dto.DateOfLeaving = GetDateTime(r, "DateOfLeaving") ?? GetDateTime(r, "DOL");

            dto.PfNo = GetString(r, "PfNo") ?? GetString(r, "PF_No") ?? "-";
            dto.EsiNo = GetString(r, "EsiNo") ?? GetString(r, "ESI_No") ?? "-";
            dto.PanNo = GetString(r, "PanNo") ?? GetString(r, "PAN_No") ?? "-";
            dto.UanNo = GetString(r, "UanNo") ?? GetString(r, "UAN_No") ?? "-";

            dto.PeriodDays = GetInt(r, "PeriodDays") ?? GetInt(r, "Period_Days") ?? 0;
            dto.PayDaysFull = GetDecimal(r, "PayDaysFull") ?? GetDecimal(r, "Pay_Days_Full") ?? 0;
            dto.PayDaysHalf = GetDecimal(r, "PayDaysHalf") ?? GetDecimal(r, "Pay_Days_Half") ?? 0;
            dto.WorkedDays = GetDecimal(r, "WorkedDays") ?? (dto.PayDaysFull + dto.PayDaysHalf);
            dto.Twd = GetDecimal(r, "Twd") ?? GetDecimal(r, "TWD") ?? 0;
            dto.OtHrs = GetDecimal(r, "OtHrs") ?? GetDecimal(r, "OT_Hrs") ?? 0;

            dto.AttnBonus = GetDecimal(r, "AttnBonus") ?? GetDecimal(r, "Attn_Bonus") ?? 0;
            dto.EarnedBasicDa = GetDecimal(r, "EarnedBasicDa") ?? GetDecimal(r, "Earned_Basic_DA") ?? 0;
            dto.EarnedHra = GetDecimal(r, "EarnedHra") ?? GetDecimal(r, "Earned_HRA") ?? 0;
            dto.OtAmount = GetDecimal(r, "OtAmount") ?? GetDecimal(r, "OT_Amount") ?? 0;
            dto.Epf = GetDecimal(r, "Epf") ?? GetDecimal(r, "EPF") ?? 0;
            dto.Esi = GetDecimal(r, "Esi") ?? GetDecimal(r, "ESI") ?? 0;
            dto.BonusDed = GetDecimal(r, "BonusDed") ?? GetDecimal(r, "Bonus_Ded") ?? 0;
            dto.SerChar = GetDecimal(r, "SerChar") ?? GetDecimal(r, "Ser_Char") ?? 0;
            dto.Lwf = GetDecimal(r, "Lwf") ?? GetDecimal(r, "LWF") ?? 0;
            dto.Gross = GetDecimal(r, "Gross") ?? 0;

            dto.EarnedWages = GetDecimal(r, "EarnedWages") ?? GetDecimal(r, "Earned_Wages") ?? 0;
            dto.OtEarning = GetDecimal(r, "OtEarning") ?? GetDecimal(r, "OT_Earning") ?? 0;
            dto.HandlingCharge = GetDecimal(r, "HandlingCharge") ?? GetDecimal(r, "Handling_Charge") ?? 0;
            dto.NapsGross = GetDecimal(r, "NapsGross") ?? (dto.EarnedWages + dto.OtEarning + dto.HandlingCharge);

            dto.PfDed = GetDecimal(r, "PfDed") ?? GetDecimal(r, "PF_Ded") ?? 0;
            dto.EsiDed = GetDecimal(r, "EsiDed") ?? GetDecimal(r, "ESI_Ded") ?? 0;
            dto.HostelDed = GetDecimal(r, "HostelDed") ?? GetDecimal(r, "Hostel_Ded") ?? 0;
            dto.TotalDeductions = GetDecimal(r, "TotalDeductions") ?? (dto.PfDed + dto.EsiDed + dto.HostelDed);
            dto.TotalDed = GetDecimal(r, "TotalDed") ?? GetDecimal(r, "Total_Ded") ?? 0;

            dto.RefBon = GetDecimal(r, "RefBon") ?? GetDecimal(r, "Ref_Bon") ?? 0;
            dto.SplAllw = GetDecimal(r, "SplAllw") ?? GetDecimal(r, "Spl_Allw") ?? 0;
            dto.Kaizen = GetDecimal(r, "Kaizen") ?? 0;
            dto.GpaGmc = GetDecimal(r, "GpaGmc") ?? GetDecimal(r, "GPA_GMC") ?? 0;
            dto.VariableTotal = GetDecimal(r, "VariableTotal") ?? (dto.RefBon + dto.SplAllw + dto.Kaizen + dto.GpaGmc);

            dto.NetPayable = GetDecimal(r, "NetPayable") ?? GetDecimal(r, "Net_Payable") ?? 0;

            dto.Status = GetString(r, "Status") ?? "Pending";
            dto.CheckerStatus = GetString(r, "CheckerStatus") ?? GetString(r, "Checker_Status") ?? "Pending";
            dto.CheckerBy = GetString(r, "CheckerBy") ?? GetString(r, "Checker_By");
            dto.CheckerOn = GetDateTime(r, "CheckerOn") ?? GetDateTime(r, "Checker_On");

            dto.ApproverStatus = GetString(r, "ApproverStatus") ?? GetString(r, "Approver_Status") ?? "Pending";
            dto.ApproverBy = GetString(r, "ApproverBy") ?? GetString(r, "Approver_By");
            dto.ApproverOn = GetDateTime(r, "ApproverOn") ?? GetDateTime(r, "Approver_On");

            dto.Remarks = GetString(r, "Remarks");
            dto.TransactionId = GetString(r, "TransactionId") ?? GetString(r, "Transaction_ID") ?? "";
            dto.Version = GetString(r, "Version") ?? "V1";
            dto.CreatedOn = GetDateTime(r, "CreatedOn") ?? GetDateTime(r, "Crtd_On");

            return dto;
        }

        private static string? GetString(DbDataReader r, string col)
        {
            try
            {
                int ord = r.GetOrdinal(col);
                return r.IsDBNull(ord) ? null : r.GetValue(ord).ToString()?.Trim();
            }
            catch { return null; }
        }

        private static decimal? GetDecimal(DbDataReader r, string col)
        {
            try
            {
                int ord = r.GetOrdinal(col);
                if (r.IsDBNull(ord)) return null;
                var val = r.GetValue(ord);
                if (val is decimal d) return d;
                return decimal.TryParse(val.ToString(), out var parsed) ? parsed : null;
            }
            catch { return null; }
        }

        private static int? GetInt(DbDataReader r, string col)
        {
            try
            {
                int ord = r.GetOrdinal(col);
                if (r.IsDBNull(ord)) return null;
                var val = r.GetValue(ord);
                if (val is int i) return i;
                return int.TryParse(val.ToString(), out var parsed) ? parsed : null;
            }
            catch { return null; }
        }

        private static DateTime? GetDateTime(DbDataReader r, string col)
        {
            try
            {
                int ord = r.GetOrdinal(col);
                if (r.IsDBNull(ord)) return null;
                var val = r.GetValue(ord);
                if (val is DateTime dt) return dt;
                return DateTime.TryParse(val.ToString(), out var parsed) ? parsed : null;
            }
            catch { return null; }
        }
    }
}
