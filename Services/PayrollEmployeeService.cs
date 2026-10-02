using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using YMI_PMT_PayrollManagement_API.DTOs.PayrollEmployee;
using YMI_PMT_PayrollManagement_API.Interfaces.Repository;
using YMI_PMT_PayrollManagement_API.Interfaces.Services;
using YMI_PMT_PayrollManagement_API.Models;

namespace YMI_PMT_PayrollManagement_API.Services
{
    public class PayrollEmployeeService : IPayrollEmployeeService
    {
        private const string DateFormat = "yyyy-MM-dd";
        private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
        private const int OtMinGapMinutes = 120;

        private readonly IPayrollEmployeeRepository _repository;

        public PayrollEmployeeService(IPayrollEmployeeRepository repository)
        {
            _repository = repository;
        }

        // UI -> DB Checker_Status
        private static readonly Dictionary<string, string> CheckerStatusMap =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["rejected"] = "CHECKER-REJECT",
                ["hold"] = "CHECKER-HOLD",
                ["verified"] = "CHECKER-VERIFIED",
                ["approved"] = "CHECKER-APPROVED",
            };

        // DB -> UI
        private static string MapCheckerStatusToUiStatus(string? checkerStatus)
        {
            var s = (checkerStatus ?? string.Empty).Trim().ToUpperInvariant();
            return s switch
            {
                "CHECKER-HOLD" => "hold",
                "CHECKER-REJECT" => "rejected",
                "CHECKER-VERIFIED" or "CHECKER-VERIFY" => "verified",
                "CHECKER-APPROVED" or "CHECKER-APPROVE" => "approved",
                "HOLD" => "hold",
                "REJECT" => "rejected",
                "VERIFIED" => "verified",
                "APPROVED" or "APPROVE" => "approved",
                _ => "pending",
            };
        }

        // ===================== MAKER =====================
        public async Task<PayrollDataDTO> GetPayrollEmployeesAsync(string category)
        {
            var rows = await _repository.GetPayrollEmployeesAsync(category);
            return await BuildMakerDataAsync(rows, category);
        }

        // 👈 CHANGED: added stage param
        public async Task<PayrollDataDTO> GetPayrollEmployeesByStatusAsync(string category, string status, string? stage = null)
        {
            var st = (status ?? "ALL").Trim().ToUpperInvariant();
            var stg = string.IsNullOrWhiteSpace(stage) ? null : stage.Trim().ToUpperInvariant();
            var rows = await _repository.GetPayrollEmployeesByStatusAsync(category, st, stg);
            return await BuildMakerDataAsync(rows, category);
        }

        private async Task<PayrollDataDTO> BuildMakerDataAsync(List<PayrollEmployee> rows, string category)
        {
            List<PayrollProvision> provisions = new();
            try
            {
                provisions = await _repository.GetPayrollProvisionsAsync(category);
            }
            catch (Exception exProv)
            {
                Console.WriteLine($"[BuildMakerDataAsync] Warning: GetPayrollProvisionsAsync failed: {exProv.Message}");
            }

            var provisionsByEmp = provisions
                .Where(p => !string.IsNullOrWhiteSpace(p.EmployeeId))
                .GroupBy(p => p.EmployeeId.Trim().ToUpperInvariant())
                .ToDictionary(g => g.Key, g => g.ToList());

            List<PayrollCalculation> calcs = new();
            try
            {
                calcs = await _repository.GetPayrollCalculationsAsync(category);
            }
            catch (Exception exCalc)
            {
                Console.WriteLine($"[BuildMakerDataAsync] Warning: GetPayrollCalculationsAsync failed: {exCalc.Message}");
            }

            var calcLookup = calcs
                .Where(c => !string.IsNullOrWhiteSpace(c.EmployeeId))
                .GroupBy(c => c.EmployeeId.Trim().ToUpperInvariant())
                .ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.ModifiedOn ?? c.CreatedOn).First());

            List<EmpLeavingRow> leaving = new();
            try
            {
                leaving = await _repository.GetEmployeeLeavingDatesAsync();
            }
            catch (Exception exLeaving)
            {
                Console.WriteLine($"[BuildMakerDataAsync] Warning: GetEmployeeLeavingDatesAsync failed: {exLeaving.Message}");
            }

            var leavingLookup = leaving
                .Where(l => !string.IsNullOrWhiteSpace(l.EmployeeId) && l.LeavingDate.HasValue)
                .GroupBy(l => l.EmployeeId.Trim().ToUpperInvariant())
                .ToDictionary(g => g.Key, g => g.First().LeavingDate!.Value);

            var first = rows.FirstOrDefault();
            PayrollPeriodDTO? period = null;
            if (first?.FromDate != null && first.ToDate != null)
            {
                period = new PayrollPeriodDTO
                {
                    FromDate = first.FromDate.Value.ToString(DateFormat),
                    ToDate = first.ToDate.Value.ToString(DateFormat),
                };
            }

            var employees = rows
                .Where(r => r.Id.HasValue)
                .GroupBy(r => r.Id!.Value)
                .Select(g =>
                {
                    var head = g.First();
                    var dto = new PayrollEmployeeDTO
                    {
                        Id = head.Id!.Value,
                        EmployeeId = head.EmployeeId ?? string.Empty,
                        EmployeeName = head.EmployeeName ?? string.Empty,
                        SkillCategory = head.SkillCategory ?? string.Empty,
                        VendorName = head.Vendor ?? string.Empty,
                        Attendance = g
                            .Where(r => r.AttendanceDate.HasValue)
                            .Select(MapDay)
                            .ToList(),
                    };

                    var key = dto.EmployeeId.Trim().ToUpperInvariant();

                    if (leavingLookup.TryGetValue(key, out var dol))
                        dto.LeavingDate = dol.ToString(DateFormat);

                    PayrollCalculation? calc = null;
                    calcLookup.TryGetValue(key, out calc);

                    PayrollProvision? prov = null;
                    if (provisionsByEmp.TryGetValue(key, out var empProvs) && empProvs.Count > 0)
                    {
                        // 1) Match by Calculation's TransactionId and Version == Provision's Attr1 and Attr2
                        if (calc != null && !string.IsNullOrWhiteSpace(calc.TransactionId) && !string.IsNullOrWhiteSpace(calc.Version))
                        {
                            prov = empProvs.FirstOrDefault(p =>
                                string.Equals((p.Attr1 ?? string.Empty).Trim(), calc.TransactionId.Trim(), StringComparison.OrdinalIgnoreCase) &&
                                string.Equals((p.Attr2 ?? string.Empty).Trim(), calc.Version.Trim(), StringComparison.OrdinalIgnoreCase));
                        }

                        // 2) Fallback to latest non-rejected provision
                        if (prov == null)
                        {
                            prov = empProvs
                                .Where(p => !((p.Status ?? string.Empty).Trim().ToUpperInvariant().Contains("REJECT")))
                                .OrderByDescending(p => p.Id)
                                .FirstOrDefault()
                                ?? empProvs.OrderByDescending(p => p.Id).First();
                        }
                    }

                    if (prov != null)
                    {
                        var provStatus = (prov.Status ?? string.Empty).Trim().ToUpperInvariant();
                        bool isProvRejected = provStatus.Contains("REJECT");

                        if (!isProvRejected)
                        {
                            dto.Provisions = MapToProvisions(prov, category);
                            dto.Status =
                                string.IsNullOrWhiteSpace(prov.Status) ||
                                prov.Status.Trim().Equals("Uploaded", StringComparison.OrdinalIgnoreCase)
                                    ? "Open"
                                    : prov.Status;
                        }
                    }
                    if (calc != null)
                    {
                        dto.Earnings = MapToEarnings(calc, category);
                        dto.Deductions = MapToDeductions(calc, category);
                        dto.NetPayable = calc.NetPayable;
                        dto.Status = calc.Status;
                        dto.CheckerStatus = calc.Status ?? string.Empty;
                        dto.Remarks = calc.Remarks ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(calc.Vendor))
                            dto.VendorName = calc.Vendor;
                    }
                    return dto;
                })
                .ToList();

            return new PayrollDataDTO { Period = period, Employees = employees };
        }

        // ---------------------------------------------------------------- UPLOAD
        public async Task<PayrollEmployeeUploadResultDTO> UploadPayrollDataAsync(IFormFile file, string category, string? createdBy = null)
        {
            var result = new PayrollEmployeeUploadResultDTO();
            var issues = new List<PayrollUploadIssueDTO>();

            List<PayrollUploadIssueDTO> SortedIssues() => issues
                .OrderBy(i => i.RowNumber == 0 ? int.MaxValue : i.RowNumber)
                .ThenBy(i => i.EmployeeId)
                .ToList();

            try
            {
                var parsedRows = await ParsePayrollFile(file, category, issues);
                if (parsedRows.Count == 0)
                {
                    result.Success = false;
                    result.Issues = SortedIssues();
                    result.Message = issues.Count > 0
                        ? "No rows could be uploaded — see details below"
                        : "No valid data rows found in the file";
                    return result;
                }

                var existingRows = await _repository.GetPayrollEmployeesAsync(category);
                var validExisting = existingRows
                    .Where(r => !string.IsNullOrWhiteSpace(r.EmployeeId) && !string.IsNullOrWhiteSpace(r.EmployeeName))
                    .ToList();

                var existingLookup = validExisting
                    .Select(r => (EmpId: r.EmployeeId!.Trim(), EmpName: r.EmployeeName!.Trim()))
                    .Distinct()
                    .ToDictionary(
                        k => (k.EmpId.ToUpperInvariant(), k.EmpName.ToUpperInvariant()),
                        _ => true);

                var idToName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var r in validExisting)
                    idToName.TryAdd(r.EmployeeId!.Trim(), r.EmployeeName!.Trim());

                var periodRow = existingRows.FirstOrDefault(r => r.FromDate != null);

                var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var r in parsedRows) seenIds.Add(r.EmployeeId.Trim());
                foreach (var i in issues)
                    if (!string.IsNullOrWhiteSpace(i.EmployeeId)) seenIds.Add(i.EmployeeId.Trim());

                var beforeProvisions = await _repository.GetPayrollProvisionsAsync(category);
                var hadInputs = new HashSet<string>(
                    beforeProvisions
                        .Where(p => !string.IsNullOrWhiteSpace(p.EmployeeId) &&
                                    !((p.Status ?? string.Empty).Trim().ToUpperInvariant().Contains("REJECT")))
                        .Select(p => p.EmployeeId.Trim()),
                    StringComparer.OrdinalIgnoreCase);

                foreach (var kv in idToName)
                {
                    if (seenIds.Contains(kv.Key) || hadInputs.Contains(kv.Key)) continue;
                    issues.Add(new PayrollUploadIssueDTO
                    {
                        RowNumber = 0,
                        EmployeeId = kv.Key,
                        EmployeeName = kv.Value,
                        Reason = "Not in the uploaded Excel file — employee is in the payroll list but has no row in your file",
                    });
                }

                var matched = new List<(int RowNumber, PayrollProvisionSaveRowDTO Save)>();

                foreach (var row in parsedRows)
                {
                    var key = (row.EmployeeId.Trim().ToUpperInvariant(), row.EmployeeName.Trim().ToUpperInvariant());
                    if (!existingLookup.ContainsKey(key))
                    {
                        var reason = idToName.TryGetValue(row.EmployeeId.Trim(), out var expectedName)
                            ? $"Emp ID found, but Name does not match (system name: '{expectedName}')"
                            : $"Emp ID not found in {category} payroll list for this period (wrong category or no attendance)";
                        issues.Add(new PayrollUploadIssueDTO
                        {
                            RowNumber = row.RowNumber,
                            EmployeeId = row.EmployeeId,
                            EmployeeName = row.EmployeeName,
                            Reason = reason,
                        });
                        continue;
                    }
                    matched.Add((row.RowNumber, BuildSaveRow(row)));
                }

                var finalRows = new List<(int RowNumber, PayrollProvisionSaveRowDTO Save)>();
                foreach (var g in matched.GroupBy(m => m.Save.EmployeeId.Trim().ToUpperInvariant()))
                {
                    var list = g.ToList();
                    var last = list[list.Count - 1];
                    foreach (var dup in list.Take(list.Count - 1))
                    {
                        issues.Add(new PayrollUploadIssueDTO
                        {
                            RowNumber = dup.RowNumber,
                            EmployeeId = dup.Save.EmployeeId,
                            EmployeeName = dup.Save.EmployeeName,
                            Reason = $"Duplicate Emp ID in file — row {last.RowNumber} was used instead",
                        });
                    }
                    finalRows.Add(last);
                }

                if (finalRows.Count == 0)
                {
                    result.Success = false;
                    result.Issues = SortedIssues();
                    result.Message = "None of the rows matched an existing Emp ID + Name for this category";
                    return result;
                }

                await _repository.SavePayrollProvisionsAsync(
                    finalRows.Select(f => f.Save).ToList(), category, periodRow?.FromDate, periodRow?.ToDate, createdBy);

                var savedCount = finalRows.Count;
                try
                {
                    var dbProvisions = await _repository.GetPayrollProvisionsAsync(category);
                    var dbLatest = dbProvisions
                        .Where(p => !string.IsNullOrWhiteSpace(p.EmployeeId))
                        .GroupBy(p => p.EmployeeId.Trim().ToUpperInvariant())
                        .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.Id).First());

                    var provCols = GetProvisionColumns(category);

                    foreach (var f in finalRows)
                    {
                        var key = f.Save.EmployeeId.Trim().ToUpperInvariant();

                        if (!dbLatest.TryGetValue(key, out var stored))
                        {
                            savedCount--;
                            issues.Add(new PayrollUploadIssueDTO
                            {
                                RowNumber = f.RowNumber,
                                EmployeeId = f.Save.EmployeeId,
                                EmployeeName = f.Save.EmployeeName,
                                Reason = "Not saved — record not found in database after upload",
                            });
                            continue;
                        }

                        var diffs = provCols
                            .Select(c => (Col: c, Excel: UploadedValue(f.Save, c) ?? 0m, Db: StoredValue(stored, c) ?? 0m))
                            .Where(x => Math.Abs(x.Excel - x.Db) > 0.01m)
                            .Select(x => $"{x.Col} (Excel {x.Excel}, saved {x.Db})")
                            .ToList();

                        if (diffs.Count > 0)
                        {
                            savedCount--;
                            issues.Add(new PayrollUploadIssueDTO
                            {
                                RowNumber = f.RowNumber,
                                EmployeeId = f.Save.EmployeeId,
                                EmployeeName = f.Save.EmployeeName,
                                Reason = $"Not saved correctly — database value differs from Excel: {string.Join("; ", diffs)}",
                            });
                        }
                    }
                }
                catch (Exception exVerify)
                {
                    Console.WriteLine($"Upload verification warning: {exVerify.Message}");
                }

                result.Success = savedCount > 0;
                result.RowsProcessed = savedCount;
                result.RowsFailed = issues.Count;
                result.Issues = SortedIssues();
                result.Message = issues.Count > 0
                    ? $"Uploaded {savedCount} employee record(s); {issues.Count} employee(s) NOT uploaded — see details"
                    : $"Successfully uploaded {savedCount} employee records";
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Issues = SortedIssues();
                result.Message = $"Error during upload: {ex.Message}";
                return result;
            }
        }

        private static decimal? UploadedValue(PayrollProvisionSaveRowDTO r, string col) => col switch
        {
            "Referal Bonus" => r.ReferalBonus,
            "Spl Allow" => r.SplAllow,
            "Kaizen" => r.Kaizen,
            "GPA & GMC" => r.GpaGmc,
            _ => null,
        };

        private static decimal? StoredValue(PayrollProvision p, string col) => col switch
        {
            "Referal Bonus" => p.ReferalBonus,
            "Spl Allow" => p.SplAllow,
            "Kaizen" => p.Kaizen,
            "GPA & GMC" => p.GpaGmc,
            _ => null,
        };

        private async Task<List<PayrollProvisionUploadRowDTO>> ParsePayrollFile(IFormFile file, string category, List<PayrollUploadIssueDTO> issues)
            => await ParseExcel(file, category, issues);

        private async Task<List<PayrollProvisionUploadRowDTO>> ParseExcel(IFormFile file, string category, List<PayrollUploadIssueDTO> issues)
        {
            var rows = new List<PayrollProvisionUploadRowDTO>();
            var provisionCols = GetProvisionColumns(category);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            try
            {
                using var ms = new MemoryStream();
                await file.OpenReadStream().CopyToAsync(ms);
                ms.Position = 0;
                using var excelReader = ExcelReaderFactory.CreateReader(ms);
                var dataSet = excelReader.AsDataSet(new ExcelDataSetConfiguration
                {
                    ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true },
                });
                if (dataSet.Tables.Count == 0) return rows;
                var table = dataSet.Tables[0];
                var headers = table.Columns
                    .Cast<DataColumn>()
                    .Select(c => (c.ColumnName ?? string.Empty).Trim())
                    .ToList();
                int empIdIdx = headers.FindIndex(h => h.Equals("Emp ID", StringComparison.OrdinalIgnoreCase));
                int nameIdx = headers.FindIndex(h => h.Equals("Name", StringComparison.OrdinalIgnoreCase));
                if (empIdIdx < 0 || nameIdx < 0)
                    throw new Exception("Excel file must have 'Emp ID' and 'Name' columns");

                var provisionIndices = new Dictionary<string, int>();
                var missingCols = new List<string>();
                foreach (var prov in provisionCols)
                {
                    int idx = headers.FindIndex(h => h.Equals(prov, StringComparison.OrdinalIgnoreCase));
                    if (idx >= 0) provisionIndices[prov] = idx;
                    else missingCols.Add(prov);
                }
                if (missingCols.Count > 0)
                    throw new Exception($"Missing column(s) in Excel: {string.Join(", ", missingCols)}");

                for (int i = 0; i < table.Rows.Count; i++)
                {
                    var row = table.Rows[i];
                    var excelRowNo = i + 2;

                    var empId = row[empIdIdx]?.ToString()?.Trim() ?? string.Empty;
                    var empName = row[nameIdx]?.ToString()?.Trim() ?? string.Empty;

                    var allEmpty = row.ItemArray.All(c => string.IsNullOrWhiteSpace(c?.ToString()));
                    if (allEmpty) continue;

                    if (string.IsNullOrWhiteSpace(empId) || string.IsNullOrWhiteSpace(empName))
                    {
                        issues.Add(new PayrollUploadIssueDTO
                        {
                            RowNumber = excelRowNo,
                            EmployeeId = empId,
                            EmployeeName = empName,
                            Reason = string.IsNullOrWhiteSpace(empId) && string.IsNullOrWhiteSpace(empName)
                                ? "Emp ID and Name are both blank"
                                : string.IsNullOrWhiteSpace(empId) ? "Emp ID is blank" : "Name is blank",
                        });
                        continue;
                    }

                    var provData = new Dictionary<string, decimal>();
                    var badValues = new List<string>();
                    foreach (var prov in provisionIndices)
                    {
                        var cellVal = row[prov.Value]?.ToString()?.Trim();
                        if (string.IsNullOrEmpty(cellVal)) provData[prov.Key] = 0;
                        else if (decimal.TryParse(cellVal, out var val)) provData[prov.Key] = val;
                        else badValues.Add($"{prov.Key} = '{cellVal}'");
                    }

                    if (badValues.Count > 0)
                    {
                        issues.Add(new PayrollUploadIssueDTO
                        {
                            RowNumber = excelRowNo,
                            EmployeeId = empId,
                            EmployeeName = empName,
                            Reason = $"Invalid number in: {string.Join("; ", badValues)}",
                        });
                        continue;
                    }

                    rows.Add(new PayrollProvisionUploadRowDTO
                    {
                        RowNumber = excelRowNo,
                        EmployeeId = empId,
                        EmployeeName = empName,
                        Provisions = provData,
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error parsing Excel file: {ex.Message}", ex);
            }
            return rows;
        }

        // ---------------------------------------------------------------- CALCULATE
        // ---------------------------------------------------------------- CALCULATE
        public async Task<PayrollEmployeeUploadResultDTO> CalculatePayrollAsync(string category, string? user = null)
        {
            var result = new PayrollEmployeeUploadResultDTO();
            try
            {
                var (processed, noStructure) = await _repository.CalculatePayrollAsync(category, user);
                if (processed == 0)
                {
                    result.Success = false;
                    result.Message = "No employees found to calculate payroll for";
                    return result;
                }
                result.Success = true;
                result.RowsProcessed = processed - noStructure;
                result.RowsFailed = noStructure;
                result.Message = noStructure > 0
                    ? $"Payroll calculated for {processed - noStructure} employee(s) — status set to Pending; {noStructure} have no active salary structure matching their group + skill category"
                    : $"Payroll calculated for {processed} employee(s) — status set to Pending";
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Error calculating payroll: {ex.Message}";
                return result;
            }
        }

        // ---------------------------------------------------------------- SUBMIT
        public async Task<PayrollEmployeeUploadResultDTO> SubmitPayrollAsync(string category, string? user = null)
        {
            var result = new PayrollEmployeeUploadResultDTO();
            try
            {
                var submitted = await _repository.SubmitPayrollAsync(category, user);
                if (submitted == 0)
                {
                    result.Success = false;
                    result.Message = "No pending records found to submit";
                    return result;
                }

                try
                {
                    var attSaved = await _repository.SavePayrollAttendanceAsync(category, user);
                    result.Success = true;
                    result.RowsProcessed = submitted;
                    result.Message = $"Payroll submitted for {submitted} employee(s) — status set to Open; {attSaved} attendance record(s) archived";
                }
                catch (Exception attEx)
                {
                    Console.WriteLine($"Attendance archive warning: {attEx.Message}");
                    result.Success = true;
                    result.RowsProcessed = submitted;
                    result.Message = $"Payroll submitted for {submitted} employee(s) — status set to Open (attendance archive pending)";
                }

                try
                {
                    var calcs = await _repository.GetPayrollCalculationsAsync(category);
                    var openOnes = calcs
                        .Where(c => !string.IsNullOrWhiteSpace(c.TransactionId)
                                 && (c.Status ?? "").Trim().Equals("Open", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (openOnes.Count > 0)
                    {
                        var txn = openOnes
                            .GroupBy(c => c.TransactionId!.Trim())
                            .OrderByDescending(g => g.Count())
                            .First().Key;

                        var versions = openOnes
                            .Where(c => c.TransactionId == txn)
                            .Select(c => (c.Version ?? "V1").Trim())
                            .Distinct()
                            .ToList();

                        result.TransactionId = txn;
                        result.Version = versions.Count == 1
                            ? versions[0]
                            : versions.OrderByDescending(v => int.TryParse(v.TrimStart('V', 'v'), out var n) ? n : 0).First();
                    }
                }
                catch (Exception exTxn)
                {
                    Console.WriteLine($"Txn/Version lookup warning: {exTxn.Message}");
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Error submitting payroll: {ex.Message}";
                return result;
            }
        }

        // ===================== CHECKER =====================
        // 👈 CHANGED: period lookup from List<PayrollStageRow>
        public async Task<PayrollCheckerDataDTO> GetPayrollCheckerDataAsync(string category)
        {
            var cat = (category ?? "CL").Trim().ToUpperInvariant();
            var rows = await _repository.GetPayrollCheckerDataAsync(cat);

            PayrollPeriodDTO? period = null;
            try
            {
                var stageRows = await _repository.GetPayrollStatusSummaryAsync(cat);
                var first = stageRows?.FirstOrDefault();
                if (first?.FromDate != null && first?.ToDate != null)
                {
                    period = new PayrollPeriodDTO
                    {
                        FromDate = first.FromDate.Value.ToString(DateFormat),
                        ToDate = first.ToDate.Value.ToString(DateFormat),
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Checker period lookup warning: {ex.Message}");
            }

            return new PayrollCheckerDataDTO
            {
                Period = period,
                Employees = rows.Select(r => MapCheckerRow(r, cat)).ToList()
            };
        }

        public async Task<PayrollEmployeeUploadResultDTO> UpdateCheckerStatusAsync(PayrollCheckerStatusUpdateDTO dto)
        {
            var result = new PayrollEmployeeUploadResultDTO();
            if (dto == null || !CheckerStatusMap.TryGetValue(dto.Status ?? string.Empty, out var checkerStatus))
            {
                result.Success = false;
                result.Message = "Status must be one of: rejected, hold, verified, approved";
                return result;
            }
            if (string.IsNullOrWhiteSpace(dto.EmployeeId) ||
                string.IsNullOrWhiteSpace(dto.TransactionId) ||
                string.IsNullOrWhiteSpace(dto.Version))
            {
                result.Success = false;
                result.Message = "EmployeeId, TransactionId and Version are required";
                return result;
            }
            var needsRemark = dto.Status.Equals("rejected", StringComparison.OrdinalIgnoreCase);
            if (needsRemark && string.IsNullOrWhiteSpace(dto.Remarks))
            {
                result.Success = false;
                result.Message = "Remarks are required for Reject";
                return result;
            }
            try
            {
                var cat = (dto.Category ?? "CL").Trim().ToUpperInvariant();
                var ok = await _repository.UpdateCheckerStatusAsync(
                    dto.EmployeeId,
                    dto.TransactionId,
                    dto.Version,
                    cat,
                    checkerStatus,
                    dto.Remarks,
                    dto.CheckedBy);

                if (!ok)
                {
                    result.Success = false;
                    result.Message = "Employee payroll record not found for this Emp ID / Transaction ID / Version";
                    return result;
                }

                if (dto.Status.Equals("rejected", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var checkerRows = await _repository.GetPayrollCheckerDataAsync(cat);
                        var matched = checkerRows.FirstOrDefault(r => r.EmployeeId == dto.EmployeeId);
                        await _repository.QueueRejectionEmailAsync(
                            dto.EmployeeId,
                            matched?.EmployeeName,
                            cat,
                            matched?.Vendor,
                            dto.Remarks);
                    }
                    catch (Exception exQueue)
                    {
                        Console.WriteLine($"Email queue insert failed for {dto.EmployeeId}: {exQueue.Message}");
                    }
                }

                result.Success = true;
                result.RowsProcessed = 1;
                result.Message = $"Status updated to {checkerStatus}";
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Error updating checker status: {ex.Message}";
                return result;
            }
        }

        // ===================== DETAIL =====================
        public async Task<PayrollCheckerDetailDTO?> GetPayrollCheckerDetailAsync(int id, string employeeId, string category)
        {
            var cat = (category ?? "CL").Trim().ToUpperInvariant();
            var allCheckerData = await _repository.GetPayrollCheckerDataAsync(cat);
            var checkerRow = allCheckerData.FirstOrDefault(r =>
                string.Equals(r.EmployeeId?.Trim(), employeeId?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (checkerRow == null) return null;

            var attendanceRows = await _repository.GetPayrollCheckerAttendanceAsync(checkerRow.EmployeeId, cat);

            var detail = new PayrollCheckerDetailDTO
            {
                Id = checkerRow.Id,
                EmployeeId = checkerRow.EmployeeId,
                EmployeeName = checkerRow.EmployeeName,
                SkillCategory = checkerRow.SkillCategory ?? string.Empty,
                Vendor = checkerRow.Vendor ?? string.Empty,
                FromDate = checkerRow.FromDate,
                ToDate = checkerRow.ToDate,
                LeavingDate = checkerRow.LeavingDate?.ToString(DateFormat),
                NetPayable = checkerRow.NetPayable,
                Status = MapCheckerStatusToUiStatus(checkerRow.CheckerStatus),
                TransactionId = checkerRow.TransactionId ?? string.Empty,
                Version = checkerRow.Version ?? string.Empty,
                Remarks = string.Empty,

                PayDaysFull = checkerRow.PayDaysFull ?? 0,
                PayDaysHalf = checkerRow.PayDaysHalf ?? 0,
                PaidDays = (checkerRow.PayDaysFull ?? 0) + ((checkerRow.PayDaysHalf ?? 0) * 0.5m),
                TotalWorkingDays = (int)(checkerRow.Twd ?? 0),
                OtHours = checkerRow.OtHrs ?? 0,
            };

            MapCheckerProvisions(detail, checkerRow, cat);
            MapCheckerEarnings(detail, checkerRow, cat);
            MapCheckerDeductions(detail, checkerRow, cat);

            int present = 0, half = 0, absent = 0, wo = 0, holiday = 0, resigned = 0;
            var daysDict = new Dictionary<string, PayrollCheckerAttendanceDayDTO>();

            foreach (var att in attendanceRows.Where(a => a.AttendanceDate.HasValue))
            {
                var attDate = att.AttendanceDate!.Value;
                var status = att.DayStatus ?? string.Empty;

                var dayDto = new PayrollCheckerAttendanceDayDTO
                {
                    AttendanceDate = attDate,
                    Day = attDate.Day,
                    Status = status,
                    ShiftStart = att.ShiftStart ?? string.Empty,
                    ShiftEnd = att.ShiftEnd ?? string.Empty,
                    Punch1 = att.Punch1,
                    Punch2 = att.Punch2,
                    WorkedMinutes = att.WorkedMinutes,
                    WorkedHours = FormatWorkedHours(att.WorkedMinutes),
                    OtHours = Math.Round(CalcDayOt(status, att.ShiftStart, att.ShiftEnd, att.Punch1, att.Punch2), 2),
                    HolidayName = att.HolidayName ?? string.Empty,
                };
                daysDict[attDate.ToString("yyyy-MM-dd")] = dayDto;

                if (status == "P") present++;
                else if (status == "HP") half++;
                else if (status == "A" || status == "PL") absent++;
                else if (status == "WO") wo++;
                else if (status == "PH" || status == "PM") holiday++;
                else if (status == "RS") resigned++;
            }

            detail.AttendanceDetails = daysDict.Values.OrderBy(d => d.AttendanceDate).ToList();
            detail.PresentDays = present;
            detail.HalfDays = half;
            detail.AbsentDays = absent;
            detail.WeeklyOff = wo;
            detail.Holidays = holiday;
            detail.ResignedDays = resigned;
            detail.Lop = absent > 0 ? absent - 1 : 0;

            return detail;
        }

        private static TimeSpan? ParseShiftTime(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            if (TimeSpan.TryParse(s.Trim(), out var ts)) return ts;
            if (DateTime.TryParse(s.Trim(), out var dt)) return dt.TimeOfDay;
            return null;
        }

        private static decimal CalcDayOt(string? status, string? shiftStart, string? shiftEnd,
                                         DateTime? punch1, DateTime? punch2)
        {
            if (status == "RS") return 0;
            if (!punch1.HasValue || !punch2.HasValue) return 0;

            var st = ParseShiftTime(shiftStart);
            var en = ParseShiftTime(shiftEnd);
            if (!st.HasValue || !en.HasValue) return 0;

            var sDt = punch1.Value.Date + st.Value;
            var eDt = punch1.Value.Date + en.Value;
            if (eDt <= sDt) eDt = eDt.AddDays(1);

            if (status == "PH" || status == "PM" || status == "WO")
                return (decimal)(eDt - sDt).TotalHours;

            if ((punch2.Value - punch1.Value) < (eDt - sDt)) return 0;
            if (punch2.Value < eDt.AddMinutes(OtMinGapMinutes)) return 0;

            return (decimal)(punch2.Value - eDt).TotalHours;
        }

        // ===================== MAPPERS =====================
        private static PayrollCheckerEmployeeDTO MapCheckerRow(PayrollCheckerRow r, string category)
        {
            var provisions = new Dictionary<string, decimal>();
            var earnings = new Dictionary<string, decimal>();
            var deductions = new Dictionary<string, decimal>();

            static void Add(Dictionary<string, decimal> dict, string key, decimal? value)
            {
                if (value.HasValue) dict[key] = value.Value;
            }

            if (category.Equals("NAPS", StringComparison.OrdinalIgnoreCase))
            {
                Add(provisions, "referalBonus", r.RefBon);
                Add(provisions, "kaizen", r.Kaizen);

                Add(earnings, "payDaysFull", r.PayDaysFull);
                Add(earnings, "payDaysHalf", r.PayDaysHalf);
                Add(earnings, "attnBonus", r.AttnBonus);
                Add(earnings, "earnedWages", r.EarnedWages);
                Add(earnings, "otEarning", r.OtEarning);
                Add(earnings, "handlingCharge", r.HandlingCharge);

                Add(deductions, "hostelFee", r.HostelDed);
            }
            else
            {
                Add(provisions, "referalBonus", r.RefBon);
                Add(provisions, "splAllow", r.SplAllw);
                Add(provisions, "kaizen", r.Kaizen);
                Add(provisions, "gpaGmc", r.GpaGmc);

                Add(earnings, "payDaysFull", r.PayDaysFull);
                Add(earnings, "payDaysHalf", r.PayDaysHalf);
                Add(earnings, "attnBonus", r.AttnBonus);
                Add(earnings, "earnedBasicDa", r.EarnedBasicDa);
                Add(earnings, "earnedHra", r.EarnedHra);
                Add(earnings, "otAmount", r.OtAmount);
                Add(earnings, "gross", r.Gross);
                Add(earnings, "pf", r.Epf);
                Add(earnings, "esi", r.Esi);
                Add(earnings, "bonus", r.BonusDed);
                Add(earnings, "serChar", r.SerChar);
                Add(earnings, "lwf", r.Lwf);
                Add(earnings, "total", r.TotalDed);

                Add(deductions, "hostelDeduction", r.HostelDed);
                Add(deductions, "pf", r.PfDed);
                Add(deductions, "esi", r.EsiDed);
            }

            return new PayrollCheckerEmployeeDTO
            {
                Id = r.Id,
                EmployeeId = r.EmployeeId ?? string.Empty,
                EmployeeName = r.EmployeeName ?? string.Empty,
                SkillCategory = r.SkillCategory ?? string.Empty,
                Vendor = r.Vendor ?? string.Empty,
                FromDate = r.FromDate,
                ToDate = r.ToDate,
                LeavingDate = r.LeavingDate?.ToString(DateFormat),
                Provisions = provisions,
                Earnings = earnings,
                Deductions = deductions,
                NetPayable = r.NetPayable,
                Status = MapCheckerStatusToUiStatus(r.CheckerStatus),
                TransactionId = r.TransactionId ?? string.Empty,
                Version = r.Version ?? string.Empty,
            };
        }

        private void MapCheckerProvisions(PayrollCheckerDetailDTO detail, PayrollCheckerRow row, string cat)
        {
            static void Add(Dictionary<string, decimal> dict, string key, decimal? val)
            {
                if (val.HasValue) dict[key] = val.Value;
            }

            if (cat == "NAPS")
            {
                Add(detail.Provisions, "referalBonus", row.RefBon);
                Add(detail.Provisions, "kaizen", row.Kaizen);
            }
            else
            {
                Add(detail.Provisions, "referalBonus", row.RefBon);
                Add(detail.Provisions, "splAllow", row.SplAllw);
                Add(detail.Provisions, "kaizen", row.Kaizen);
                Add(detail.Provisions, "gpaGmc", row.GpaGmc);
            }
        }

        private void MapCheckerEarnings(PayrollCheckerDetailDTO detail, PayrollCheckerRow row, string cat)
        {
            static void Add(Dictionary<string, decimal> dict, string key, decimal? val)
            {
                if (val.HasValue) dict[key] = val.Value;
            }

            if (cat == "NAPS")
            {
                Add(detail.Earnings, "attnBonus", row.AttnBonus);
                Add(detail.Earnings, "earnedWages", row.EarnedWages);
                Add(detail.Earnings, "otEarning", row.OtEarning);
                Add(detail.Earnings, "handlingCharge", row.HandlingCharge);
            }
            else
            {
                Add(detail.Earnings, "attnBonus", row.AttnBonus);
                Add(detail.Earnings, "earnedBasicDa", row.EarnedBasicDa);
                Add(detail.Earnings, "earnedHra", row.EarnedHra);
                Add(detail.Earnings, "otAmount", row.OtAmount);
                Add(detail.Earnings, "gross", row.Gross);
                Add(detail.Earnings, "pf", row.Epf);
                Add(detail.Earnings, "esi", row.Esi);
                Add(detail.Earnings, "bonus", row.BonusDed);
                Add(detail.Earnings, "serChar", row.SerChar);
                Add(detail.Earnings, "lwf", row.Lwf);
                Add(detail.Earnings, "total", row.TotalDed);
            }
        }

        private void MapCheckerDeductions(PayrollCheckerDetailDTO detail, PayrollCheckerRow row, string cat)
        {
            static void Add(Dictionary<string, decimal> dict, string key, decimal? val)
            {
                if (val.HasValue) dict[key] = val.Value;
            }

            if (cat == "NAPS")
            {
                Add(detail.Deductions, "hostelFee", row.HostelDed);
            }
            else
            {
                Add(detail.Deductions, "hostelDeduction", row.HostelDed);
                Add(detail.Deductions, "pf", row.PfDed);
                Add(detail.Deductions, "esi", row.EsiDed);
            }
        }

        // ===================== HELPERS =====================
        private List<string> GetProvisionColumns(string category) =>
            category.ToUpperInvariant() switch
            {
                "CL" => new List<string> { "Referal Bonus", "Spl Allow", "Kaizen", "GPA & GMC" },
                "NAPS" => new List<string> { "Referal Bonus", "Kaizen" },
                _ => new List<string>()
            };

        private static readonly Dictionary<string, string> ProvisionApiKeys =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Referal Bonus"] = "referalBonus",
                ["Spl Allow"] = "splAllow",
                ["Kaizen"] = "kaizen",
                ["GPA & GMC"] = "gpaGmc",
            };

        private static PayrollProvisionSaveRowDTO BuildSaveRow(PayrollProvisionUploadRowDTO row)
        {
            decimal? Get(string key) => row.Provisions.TryGetValue(key, out var v) ? v : (decimal?)null;
            return new PayrollProvisionSaveRowDTO
            {
                EmployeeId = row.EmployeeId,
                EmployeeName = row.EmployeeName,
                ReferalBonus = Get("Referal Bonus"),
                SplAllow = Get("Spl Allow"),
                Kaizen = Get("Kaizen"),
                GpaGmc = Get("GPA & GMC"),
            };
        }

        private Dictionary<string, decimal> MapToProvisions(PayrollProvision prov, string category)
        {
            var relevantColumns = GetProvisionColumns(category);
            var named = new Dictionary<string, decimal?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Referal Bonus"] = prov.ReferalBonus,
                ["Spl Allow"] = prov.SplAllow,
                ["Kaizen"] = prov.Kaizen,
                ["GPA & GMC"] = prov.GpaGmc,
            };
            var result = new Dictionary<string, decimal>();
            foreach (var col in relevantColumns)
            {
                if (named.TryGetValue(col, out var val) && val.HasValue && ProvisionApiKeys.TryGetValue(col, out var apiKey))
                    result[apiKey] = val.Value;
            }
            return result;
        }

        private static Dictionary<string, decimal> MapToEarnings(PayrollCalculation c, string category)
        {
            var d = new Dictionary<string, decimal>();
            void Add(string k, decimal? v) { if (v.HasValue) d[k] = v.Value; }

            if (category.Equals("NAPS", StringComparison.OrdinalIgnoreCase))
            {
                Add("payDaysFull", c.PayDaysFull);
                Add("payDaysHalf", c.PayDaysHalf);
                Add("attnBonus", c.AttnBonus);
                Add("earnedWages", c.EarnedWages);
                Add("otEarning", c.OtEarning);
                Add("handlingCharge", c.HandlingCharge);
                return d;
            }
            Add("payDaysFull", c.PayDaysFull);
            Add("payDaysHalf", c.PayDaysHalf);
            Add("attnBonus", c.AttnBonus);
            Add("earnedBasicDa", c.EarnedBasicDa);
            Add("earnedHra", c.EarnedHra);
            Add("otAmount", c.OtAmount);
            Add("gross", c.Gross);
            Add("pf", c.Epf);
            Add("esi", c.Esi);
            Add("bonus", c.BonusDed);
            Add("serChar", c.SerChar);
            Add("lwf", c.Lwf);
            Add("total", c.TotalDed);
            return d;
        }

        private static Dictionary<string, decimal> MapToDeductions(PayrollCalculation c, string category)
        {
            var d = new Dictionary<string, decimal>();
            void Add(string k, decimal? v) { if (v.HasValue) d[k] = v.Value; }

            if (category.Equals("NAPS", StringComparison.OrdinalIgnoreCase))
            {
                Add("hostelFee", c.HostelDed);
                return d;
            }
            Add("hostelDeduction", c.HostelDed);
            Add("pf", c.PfDed);
            Add("esi", c.EsiDed);
            return d;
        }

        private static PayrollAttendanceDayDTO MapDay(PayrollEmployee r) => new PayrollAttendanceDayDTO
        {
            Date = r.AttendanceDate!.Value.ToString(DateFormat),
            Status = r.DayStatus ?? string.Empty,
            ShiftStart = r.ShiftStart ?? string.Empty,
            ShiftEnd = r.ShiftEnd ?? string.Empty,
            Punch1 = r.Punch1?.ToString(DateTimeFormat) ?? string.Empty,
            Punch2 = r.Punch2?.ToString(DateTimeFormat) ?? string.Empty,
            WorkedMinutes = r.WorkedMinutes,
            WorkedHours = FormatWorkedHours(r.WorkedMinutes),
            HolidayName = r.HolidayName ?? string.Empty,
        };

        private static string FormatWorkedHours(int? minutes)
        {
            if (!minutes.HasValue || minutes.Value < 0) return string.Empty;
            var h = minutes.Value / 60;
            var m = minutes.Value % 60;
            return $"{h}:{m:D2}";
        }

        // ===================== ANALYSIS =====================
        public async Task<PayrollDataDTO> GetPayrollAnalysisAsync(
            string category, string? search, string? skill, string? vendor, DateTime? from, DateTime? to)
        {
            var rows = await _repository.GetPayrollAnalysisAsync(category, search, skill, vendor, from, to);
            var calcs = await _repository.GetPayrollAnalysisCalculationsAsync(category, search, skill, vendor, from, to);

            var calcLookup = calcs
                .Where(c => !string.IsNullOrWhiteSpace(c.EmployeeId))
                .GroupBy(c => c.EmployeeId.Trim().ToUpperInvariant())
                .ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.ModifiedOn ?? c.CreatedOn).First());

            var first = rows.FirstOrDefault();
            PayrollPeriodDTO? period = null;
            if (first?.FromDate != null && first.ToDate != null)
            {
                period = new PayrollPeriodDTO
                {
                    FromDate = first.FromDate.Value.ToString(DateFormat),
                    ToDate = first.ToDate.Value.ToString(DateFormat),
                };
            }

            var employees = rows
                .Where(r => r.Id.HasValue)
                .GroupBy(r => r.Id!.Value)
                .Select(g =>
                {
                    var head = g.First();
                    var dto = new PayrollEmployeeDTO
                    {
                        Id = head.Id!.Value,
                        EmployeeId = head.EmployeeId ?? string.Empty,
                        EmployeeName = head.EmployeeName ?? string.Empty,
                        SkillCategory = head.SkillCategory ?? string.Empty,
                        VendorName = head.Vendor ?? string.Empty,
                        Attendance = g
                            .Where(r => r.AttendanceDate.HasValue)
                            .Select(MapDay)
                            .ToList(),
                    };

                    var key = dto.EmployeeId.Trim().ToUpperInvariant();
                    if (calcLookup.TryGetValue(key, out var calc))
                    {
                        dto.Earnings = MapToEarnings(calc, category);
                        dto.Deductions = MapToDeductions(calc, category);
                        dto.NetPayable = calc.NetPayable;
                        dto.Status = calc.Status;
                        dto.Remarks = calc.Remarks ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(calc.Vendor))
                            dto.VendorName = calc.Vendor;
                    }
                    return dto;
                })
                .ToList();

            return new PayrollDataDTO { Period = period, Employees = employees };
        }

        // ===================== RESIGN QUERY =====================
        public async Task<PayrollEmployeeUploadResultDTO> RaiseResignQueryAsync(PayrollResignRequestDTO dto)
        {
            var result = new PayrollEmployeeUploadResultDTO();
            if (dto == null || string.IsNullOrWhiteSpace(dto.EmployeeId))
            {
                result.Success = false;
                result.Message = "EmployeeId is required";
                return result;
            }
            try
            {
                var cat = (dto.Category ?? "CL").Trim().ToUpperInvariant();

                var dateText = string.Empty;
                if (!string.IsNullOrWhiteSpace(dto.ResignDate) &&
                    DateTime.TryParseExact(dto.ResignDate.Trim(), DateFormat,
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var clickedDate))
                {
                    dateText = clickedDate.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture);
                }

                var userRemarks = dto.Remarks?.Trim();
                string? finalRemarks;
                if (!string.IsNullOrEmpty(userRemarks) && dateText.Length > 0)
                    finalRemarks = $"{userRemarks} | Date: {dateText}";
                else if (dateText.Length > 0)
                    finalRemarks = $"Date: {dateText}";
                else
                    finalRemarks = string.IsNullOrEmpty(userRemarks) ? null : userRemarks;

                await _repository.QueueAdminResignEmailAsync(
                    dto.EmployeeId.Trim(),
                    dto.EmployeeName?.Trim(),
                    cat,
                    dto.Vendor?.Trim(),
                    finalRemarks);

                result.Success = true;
                result.RowsProcessed = 1;
                result.Message = "Resignation query sent to Admin";
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Error sending query: {ex.Message}";
                return result;
            }
        }

        // ===================== PERIOD CARDS + HISTORY =====================
        public async Task<List<PayrollPeriodSummaryDTO>> GetPayrollPeriodsAsync(string category)
        {
            var rows = await _repository.GetPayrollPeriodsAsync(category);
            return rows.Select(r => new PayrollPeriodSummaryDTO
            {
                FromDate = r.FromDate.ToString(DateFormat),
                ToDate = r.ToDate.ToString(DateFormat),
                EmployeeCount = r.EmployeeCount,
                NetTotal = r.NetTotal,
                TransactionId = r.TransactionId,
            }).ToList();
        }

        public async Task<PayrollDataDTO> GetPayrollHistoryAsync(string category, DateTime from, DateTime to)
        {
            var rows = await _repository.GetPayrollHistoryAsync(category, from, to);
            var provisions = await _repository.GetPayrollHistoryProvisionsAsync(category, from, to);
            var calcs = await _repository.GetPayrollHistoryCalculationsAsync(category, from, to);
            var leaving = await _repository.GetEmployeeLeavingDatesAsync();

            var provisionsByEmp = provisions
                .Where(p => !string.IsNullOrWhiteSpace(p.EmployeeId))
                .GroupBy(p => p.EmployeeId.Trim().ToUpperInvariant())
                .ToDictionary(g => g.Key, g => g.ToList());

            var calcLookup = calcs
                .Where(c => !string.IsNullOrWhiteSpace(c.EmployeeId))
                .GroupBy(c => c.EmployeeId.Trim().ToUpperInvariant())
                .ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.ModifiedOn ?? c.CreatedOn).First());

            var leavingLookup = leaving
                .Where(l => !string.IsNullOrWhiteSpace(l.EmployeeId) && l.LeavingDate.HasValue)
                .GroupBy(l => l.EmployeeId.Trim().ToUpperInvariant())
                .ToDictionary(g => g.Key, g => g.First().LeavingDate!.Value);

            var period = new PayrollPeriodDTO
            {
                FromDate = from.ToString(DateFormat),
                ToDate = to.ToString(DateFormat),
            };

            var employees = rows
                .Where(r => r.Id.HasValue)
                .GroupBy(r => r.Id!.Value)
                .Select(g =>
                {
                    var head = g.First();
                    var dto = new PayrollEmployeeDTO
                    {
                        Id = head.Id!.Value,
                        EmployeeId = head.EmployeeId ?? string.Empty,
                        EmployeeName = head.EmployeeName ?? string.Empty,
                        SkillCategory = head.SkillCategory ?? string.Empty,
                        VendorName = head.Vendor ?? string.Empty,
                        Attendance = g
                            .Where(r => r.AttendanceDate.HasValue)
                            .Select(MapDay)
                            .ToList(),
                    };

                    var key = dto.EmployeeId.Trim().ToUpperInvariant();

                    if (leavingLookup.TryGetValue(key, out var dol))
                        dto.LeavingDate = dol.ToString(DateFormat);

                    PayrollCalculation? calc = null;
                    calcLookup.TryGetValue(key, out calc);

                    PayrollProvision? prov = null;
                    if (provisionsByEmp.TryGetValue(key, out var empProvs) && empProvs.Count > 0)
                    {
                        // 1) Match by Calculation's TransactionId and Version == Provision's Attr1 and Attr2
                        if (calc != null && !string.IsNullOrWhiteSpace(calc.TransactionId) && !string.IsNullOrWhiteSpace(calc.Version))
                        {
                            prov = empProvs.FirstOrDefault(p =>
                                string.Equals((p.Attr1 ?? string.Empty).Trim(), calc.TransactionId.Trim(), StringComparison.OrdinalIgnoreCase) &&
                                string.Equals((p.Attr2 ?? string.Empty).Trim(), calc.Version.Trim(), StringComparison.OrdinalIgnoreCase));
                        }

                        // 2) Fallback to latest non-rejected provision
                        if (prov == null)
                        {
                            prov = empProvs
                                .Where(p => !((p.Status ?? string.Empty).Trim().ToUpperInvariant().Contains("REJECT")))
                                .OrderByDescending(p => p.ModifiedOn ?? p.CreatedOn)
                                .ThenByDescending(p => p.Id)
                                .FirstOrDefault()
                                ?? empProvs.OrderByDescending(p => p.Id).First();
                        }
                    }

                    if (prov != null)
                        dto.Provisions = MapToProvisions(prov, category);

                    if (calc != null)
                    {
                        dto.Earnings = MapToEarnings(calc, category);
                        dto.Deductions = MapToDeductions(calc, category);
                        dto.NetPayable = calc.NetPayable;
                        dto.Status = calc.Status;
                        dto.Remarks = calc.Remarks ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(calc.Vendor))
                            dto.VendorName = calc.Vendor;
                    }
                    return dto;
                })
                .ToList();

            return new PayrollDataDTO { Period = period, Employees = employees };
        }

        public async Task<PayrollDataDTO> GetPayrollLiveAttendanceAsync(string category)
        {
            var rows = await _repository.GetPayrollLiveAttendanceAsync(category);
            var leaving = await _repository.GetEmployeeLeavingDatesAsync();

            var leavingLookup = leaving
                .Where(l => !string.IsNullOrWhiteSpace(l.EmployeeId) && l.LeavingDate.HasValue)
                .GroupBy(l => l.EmployeeId.Trim().ToUpperInvariant())
                .ToDictionary(g => g.Key, g => g.First().LeavingDate!.Value);

            var first = rows.FirstOrDefault(r => r.FromDate != null && r.ToDate != null);
            PayrollPeriodDTO? period = null;
            if (first != null)
            {
                period = new PayrollPeriodDTO
                {
                    FromDate = first.FromDate!.Value.ToString(DateFormat),
                    ToDate = first.ToDate!.Value.ToString(DateFormat),
                };
            }

            var employees = rows
                .Where(r => r.Id.HasValue)
                .GroupBy(r => r.Id!.Value)
                .Select(g =>
                {
                    var head = g.First();
                    var dto = new PayrollEmployeeDTO
                    {
                        Id = head.Id!.Value,
                        EmployeeId = head.EmployeeId ?? string.Empty,
                        EmployeeName = head.EmployeeName ?? string.Empty,
                        SkillCategory = head.SkillCategory ?? string.Empty,
                        VendorName = head.Vendor ?? string.Empty,
                        Attendance = g
                            .Where(r => r.AttendanceDate.HasValue)
                            .Select(MapDay)
                            .ToList(),
                    };

                    var key = dto.EmployeeId.Trim().ToUpperInvariant();
                    if (leavingLookup.TryGetValue(key, out var dol))
                        dto.LeavingDate = dol.ToString(DateFormat);

                    return dto;
                })
                .ToList();

            return new PayrollDataDTO { Period = period, Employees = employees };
        }

        // 👈 CHANGED: builds DTO from List<PayrollStageRow> (multi-stage)
        public async Task<PayrollStatusSummaryDTO> GetPayrollStatusSummaryAsync(string category)
        {
            var cat = (category ?? "CL").Trim().ToUpperInvariant();
            var rows = await _repository.GetPayrollStatusSummaryAsync(cat);

            // SP always returns at least one row (with 0 counts + From/To) so this rarely triggers.
            if (rows == null || rows.Count == 0)
            {
                return new PayrollStatusSummaryDTO
                {
                    Stage = "PENDING",
                    Stages = new List<PayrollStageSummaryDTO>()
                };
            }

            var dto = new PayrollStatusSummaryDTO
            {
                TotalEmployees = rows.Sum(r => r.Total),
                PendingCount = rows.Where(r => r.Stage == "PENDING").Sum(r => r.Total),
                VerifyCount = rows.Sum(r => r.VerifyCount),
                HoldCount = rows.Sum(r => r.HoldCount),
                RejectCount = rows.Sum(r => r.RejectCount),
                ApproveCount = rows.Sum(r => r.ApproveCount),
                FromDate = rows[0].FromDate?.ToString("yyyy-MM-dd"),
                ToDate = rows[0].ToDate?.ToString("yyyy-MM-dd"),
                Stages = rows
                    .OrderBy(r => StageOrder(r.Stage))
                    .Select(r => new PayrollStageSummaryDTO
                    {
                        Stage = r.Stage,
                        Total = r.Total,
                        Verify = r.VerifyCount,
                        Hold = r.HoldCount,
                        Reject = r.RejectCount,
                        Approve = r.ApproveCount,
                    })
                    .ToList()
            };

            var names = dto.Stages.Select(s => s.Stage).ToHashSet();
            dto.Stage = names.Contains("APPROVING") ? "APPROVING"
                      : names.Contains("APPROVE_PENDING") ? "APPROVE_PENDING"
                      : names.Contains("VERIFYING") ? "VERIFYING"
                      : "PENDING";

            return dto;
        }

        private static int StageOrder(string s) => s switch
        {
            "PENDING" => 1,
            "VERIFYING" => 2,
            "APPROVE_PENDING" => 3,
            "APPROVING" => 4,
            _ => 99,
        };
    }
}