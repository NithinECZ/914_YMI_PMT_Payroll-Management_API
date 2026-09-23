using YMI_PMT_PayrollManagement_API.DTOs.PayrollEmployee;
using YMI_PMT_PayrollManagement_API.Interfaces.Repository;
using YMI_PMT_PayrollManagement_API.Interfaces.Services;
using YMI_PMT_PayrollManagement_API.Models;

namespace YMI_PMT_PayrollManagement_API.Services
{
    public class PayrollEmployeeService : IPayrollEmployeeService
    {
        private const string DateFormat = "yyyy-MM-dd";

        private readonly IPayrollEmployeeRepository _repository;

        public PayrollEmployeeService(IPayrollEmployeeRepository repository)
        {
            _repository = repository;
        }

        public async Task<PayrollDataDTO> GetPayrollEmployeesAsync(string category)
        {
            var rows = await _repository.GetPayrollEmployeesAsync(category);

            var first = rows.FirstOrDefault();
            PayrollPeriodDTO? period = null;
            if (first?.FromDate != null && first.ToDate != null)
            {
                period = new PayrollPeriodDTO
                {
                    TrId = first.TrId ?? string.Empty,
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

                    var latestShiftRow = g
                        .Where(r => !string.IsNullOrWhiteSpace(r.ShiftStart) && !string.IsNullOrWhiteSpace(r.ShiftEnd))
                        .OrderByDescending(r => r.AttendanceDate)
                        .FirstOrDefault();

                    return new PayrollEmployeeDTO
                    {
                        Id = head.Id!.Value,
                        EmployeeId = head.EmployeeId ?? string.Empty,
                        EmployeeName = head.EmployeeName ?? string.Empty,
                        SkillCategory = head.SkillCategory ?? string.Empty,
                        ShiftStart = latestShiftRow?.ShiftStart ?? string.Empty,
                        ShiftEnd = latestShiftRow?.ShiftEnd ?? string.Empty,
                        Attendance = g
                            .Where(r => r.AttendanceDate.HasValue)
                            .Select(MapDay)
                            .ToList(),
                    };
                })
                .ToList();

            return new PayrollDataDTO { Period = period, Employees = employees };
        }

        public async Task<PayrollEmployeeUploadResultDTO> UploadPayrollDataAsync(IFormFile file, string category)
        {
            var result = new PayrollEmployeeUploadResultDTO();

            try
            {
                var rows = await ParsePayrollFile(file, category);

                if (rows.Count == 0)
                {
                    result.Success = false;
                    result.Message = "No valid data rows found in the file";
                    return result;
                }

                var saved = await _repository.SavePayrollProvisionsAsync(rows, category);

                result.Success = true;
                result.RowsProcessed = saved;
                result.RowsFailed = rows.Count - saved;
                result.Message = $"Successfully uploaded {saved} employee records";

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Error during upload: {ex.Message}";
                return result;
            }
        }

        private async Task<List<PayrollProvisionUploadRowDTO>> ParsePayrollFile(IFormFile file, string category)
        {
            var rows = new List<PayrollProvisionUploadRowDTO>();

            if (file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                rows = ParseCsv(file, category);
            }
            else if (file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ||
                     file.FileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
            {
                rows = await ParseExcel(file, category);
            }

            return rows;
        }

        private List<PayrollProvisionUploadRowDTO> ParseCsv(IFormFile file, string category)
        {
            var rows = new List<PayrollProvisionUploadRowDTO>();
            var provisionCols = GetProvisionColumns(category);

            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                var firstLine = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(firstLine))
                    return rows;

                var headers = firstLine.TrimStart('\uFEFF').Split(',')
                    .Select(h => h.Trim(' ', '"'))
                    .ToList();

                int empIdIdx = headers.FindIndex(h => h.Equals("Emp ID", StringComparison.OrdinalIgnoreCase));
                int nameIdx = headers.FindIndex(h => h.Equals("Name", StringComparison.OrdinalIgnoreCase));

                if (empIdIdx < 0 || nameIdx < 0)
                    throw new Exception("CSV must have 'Emp ID' and 'Name' columns");

                var provisionIndices = new Dictionary<string, int>();
                foreach (var prov in provisionCols)
                {
                    int idx = headers.FindIndex(h => h.Equals(prov, StringComparison.OrdinalIgnoreCase));
                    if (idx >= 0)
                        provisionIndices[prov] = idx;
                }

                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var cells = ParseCsvLine(line);
                    if (cells.Count <= empIdIdx || cells.Count <= nameIdx)
                        continue;

                    var empId = cells[empIdIdx].Trim();
                    var empName = cells[nameIdx].Trim();

                    if (string.IsNullOrWhiteSpace(empId) || string.IsNullOrWhiteSpace(empName))
                        continue;

                    var provData = new Dictionary<string, decimal>();
                    foreach (var prov in provisionIndices)
                    {
                        if (prov.Value < cells.Count && decimal.TryParse(cells[prov.Value].Trim(), out var val))
                        {
                            provData[prov.Key] = val;
                        }
                        else
                        {
                            provData[prov.Key] = 0;
                        }
                    }

                    rows.Add(new PayrollProvisionUploadRowDTO
                    {
                        EmployeeId = empId,
                        EmployeeName = empName,
                        Provisions = provData,
                    });
                }
            }

            return rows;
        }

        private async Task<List<PayrollProvisionUploadRowDTO>> ParseExcel(IFormFile file, string category)
        {
            var rows = new List<PayrollProvisionUploadRowDTO>();
            var provisionCols = GetProvisionColumns(category);

            try
            {
                throw new NotImplementedException("Excel parsing requires ExcelDataReader NuGet package. Use CSV for now.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error parsing Excel file: {ex.Message}", ex);
            }
        }

        private List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            var current = string.Empty;
            var inQuotes = false;

            foreach (var c in line)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current);
                    current = string.Empty;
                }
                else
                {
                    current += c;
                }
            }

            result.Add(current);
            return result;
        }

        private List<string> GetProvisionColumns(string category)
        {
            return category.ToUpperInvariant() switch
            {
                "CL" => new List<string>
                {
                    "Referal Bonus", "Spl Allow", "Kaizen", "GPA & GMC"
                },
                "NAPS" => new List<string>
                {
                    "Referal Bonus", "Kaizen"
                },
                _ => new List<string>()
            };
        }

        private static PayrollAttendanceDayDTO MapDay(PayrollEmployee r) => new PayrollAttendanceDayDTO
        {
            Date = r.AttendanceDate!.Value.ToString(DateFormat),
            Status = r.DayStatus ?? string.Empty,
            ShiftStart = r.ShiftStart ?? string.Empty,
            ShiftEnd = r.ShiftEnd ?? string.Empty,
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
    }
}