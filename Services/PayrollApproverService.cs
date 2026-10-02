using YMI_PMT_PayrollManagement_API.DTOs.PayrollEmployee;
using YMI_PMT_PayrollManagement_API.Interfaces.Repository;
using YMI_PMT_PayrollManagement_API.Interfaces.Services;
using YMI_PMT_PayrollManagement_API.Models;

namespace YMI_PMT_PayrollManagement_API.Services
{
    public class PayrollApproverService : IPayrollApproverService
    {
        private const string DateFormat = "yyyy-MM-dd";
        private const int OtMinGapMinutes = 120;   // OT counts only when punch-out >= shift end + 2 hrs (same as Maker / Checker)

        private readonly IPayrollApproverRepository _repository;

        public PayrollApproverService(IPayrollApproverRepository repository)
        {
            _repository = repository;
        }

        // UI -> DB Approver_Status
        private static readonly Dictionary<string, string> ApproverStatusMap =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["rejected"] = "APPROVER-REJECT",
                ["hold"] = "APPROVER-HOLD",
                ["verified"] = "APPROVER-VERIFIED",
                ["approved"] = "APPROVER-APPROVED",
            };

        // DB -> UI
        private static string MapApproverStatusToUiStatus(string? approverStatus)
        {
            var s = (approverStatus ?? string.Empty).Trim().ToUpperInvariant();
            return s switch
            {
                "APPROVER-HOLD" => "hold",
                "APPROVER-REJECT" => "rejected",
                "APPROVER-VERIFIED" => "verified",
                "APPROVER-APPROVED" or "APPROVER-APPROVE" => "approved",
                _ => "pending",   // Open / Pending / blank
            };
        }

        // ===================== GRID (latest version per employee – decided in the SP) =====================
        public async Task<PayrollApproverDataDTO> GetPayrollApproverDataAsync(string category)
        {
            var cat = (category ?? "CL").Trim().ToUpperInvariant();
            var rows = await _repository.GetPayrollApproverDataAsync(cat);
            return new PayrollApproverDataDTO
            {
                Employees = rows.Select(r => MapApproverRow(r, cat)).ToList()
            };
        }

        // ===================== DETAIL =====================
        // Pay days, TWD, OT hours, Net = DB values (Maker calculated). Attendance = saved archive first / live fallback (SP).
        public async Task<PayrollApproverDetailDTO?> GetPayrollApproverDetailAsync(int id, string employeeId, string category)
        {
            var cat = (category ?? "CL").Trim().ToUpperInvariant();
            var allApproverData = await _repository.GetPayrollApproverDataAsync(cat);
            var row = allApproverData.FirstOrDefault(r =>
                string.Equals(r.EmployeeId?.Trim(), employeeId?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (row == null) return null;

            var attendanceRows = await _repository.GetPayrollApproverAttendanceAsync(row.EmployeeId, cat);

            var detail = new PayrollApproverDetailDTO
            {
                Id = row.Id,
                EmployeeId = row.EmployeeId,
                EmployeeName = row.EmployeeName,
                SkillCategory = row.SkillCategory ?? string.Empty,
                Vendor = row.Vendor ?? string.Empty,
                FromDate = row.FromDate,
                ToDate = row.ToDate,
                LeavingDate = row.LeavingDate?.ToString(DateFormat),
                NetPayable = row.NetPayable,
                Status = MapApproverStatusToUiStatus(row.ApproverStatus),
                TransactionId = row.TransactionId ?? string.Empty,
                Version = row.Version ?? string.Empty,
                Remarks = string.Empty,

                // DIRECT FROM DB – NO RECALCULATION
                PayDaysFull = row.PayDaysFull ?? 0,
                PayDaysHalf = row.PayDaysHalf ?? 0,
                PaidDays = (row.PayDaysFull ?? 0) + ((row.PayDaysHalf ?? 0) * 0.5m),
                TotalWorkingDays = (int)(row.Twd ?? 0),
                OtHours = row.OtHrs ?? 0,
            };

            MapApproverProvisions(detail, row, cat);
            MapApproverEarnings(detail, row, cat);
            MapApproverDeductions(detail, row, cat);

            int present = 0, half = 0, absent = 0, wo = 0, holiday = 0, resigned = 0;
            var daysDict = new Dictionary<string, PayrollApproverAttendanceDayDTO>();

            foreach (var att in attendanceRows.Where(a => a.AttendanceDate.HasValue))
            {
                var attDate = att.AttendanceDate!.Value;
                var status = att.DayStatus ?? string.Empty;

                var dayDto = new PayrollApproverAttendanceDayDTO
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
                daysDict[attDate.ToString(DateFormat)] = dayDto;

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

            // NEVER overwrite PaidDays, TotalWorkingDays or OtHours from attendance
            return detail;
        }

        // ===================== STATUS UPDATE (single or bulk – UI calls once per record) =====================
        public async Task<PayrollEmployeeUploadResultDTO> UpdateApproverStatusAsync(PayrollApproverStatusUpdateDTO dto)
        {
            var result = new PayrollEmployeeUploadResultDTO();

            if (dto == null || !ApproverStatusMap.TryGetValue(dto.Status ?? string.Empty, out var approverStatus))
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
                var ok = await _repository.UpdateApproverStatusAsync(
                    dto.EmployeeId,
                    dto.TransactionId,
                    dto.Version,
                    cat,
                    approverStatus,
                    dto.Remarks,
                    dto.ApprovedBy);

                if (!ok)
                {
                    result.Success = false;
                    result.Message = "Record not found, or a newer version already exists for this Emp ID / Transaction ID / Version";
                    return result;
                }

                if (dto.Status.Equals("rejected", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var approverRows = await _repository.GetPayrollApproverDataAsync(cat);
                        var matched = approverRows.FirstOrDefault(r =>
                            string.Equals(r.EmployeeId?.Trim(), dto.EmployeeId?.Trim(), StringComparison.OrdinalIgnoreCase));
                        await _repository.QueueApproverRejectionEmailAsync(
                            dto.EmployeeId,
                            matched?.EmployeeName,
                            cat,
                            matched?.Vendor,
                            dto.Remarks);
                    }
                    catch (Exception exQueue)
                    {
                        Console.WriteLine($"Approver rejection email queue insert failed for {dto.EmployeeId}: {exQueue.Message}");
                    }
                }

                result.Success = true;
                result.RowsProcessed = 1;
                result.Message = $"Status updated to {approverStatus}";
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Error updating approver status: {ex.Message}";
                return result;
            }
        }

        // ===================== OT / TIME HELPERS (same rule as Maker / Checker) =====================
        private static TimeSpan? ParseShiftTime(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            if (TimeSpan.TryParse(s.Trim(), out var ts)) return ts;
            if (DateTime.TryParse(s.Trim(), out var dt)) return dt.TimeOfDay;
            return null;
        }

        //   - RS / missing punches  -> 0
        //   - PH / PM / WO worked   -> full shift hours
        //   - worked < shift        -> 0
        //   - punch-out < shift end + 2 hrs -> 0, else (punch-out - shift end) hours
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
            if (eDt <= sDt) eDt = eDt.AddDays(1); // Night Shift crossing midnight

            // 1. Holiday / Weekly Off working
            if (status == "PH" || status == "PM" || status == "WO")
                return (decimal)(eDt - sDt).TotalHours;

            // 2. Early Punch-In is IGNORED: Effective start time is strictly sDt (Shift Start)!
            var effectiveStart = punch1.Value < sDt ? sDt : punch1.Value;
            if ((punch2.Value - effectiveStart) < (eDt - sDt)) return 0;

            // 3. Minimum 120 minutes (2 hrs) rule from Shift End
            if (punch2.Value < eDt.AddMinutes(OtMinGapMinutes)) return 0;

            // 4. OT is calculated STRICTLY from Shift End (eDt) to Punch-Out (punch2) in minutes!
            // Example: 00:40 to 02:54:54 = 134 minutes = 2.23 Hours (2:14 HH:MM)
            var otMinutes = (int)(punch2.Value - eDt).TotalMinutes;
            return Math.Round((decimal)otMinutes / 60m, 2);
        }

        private static string FormatWorkedHours(int? minutes)
        {
            if (!minutes.HasValue || minutes.Value < 0) return string.Empty;
            var h = minutes.Value / 60;
            var m = minutes.Value % 60;
            return $"{h}:{m:D2}";
        }

        // ===================== MAPPERS =====================
        private static PayrollApproverEmployeeDTO MapApproverRow(PayrollCheckerRow r, string category)
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

            return new PayrollApproverEmployeeDTO
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
                Status = MapApproverStatusToUiStatus(r.ApproverStatus),
                TransactionId = r.TransactionId ?? string.Empty,
                Version = r.Version ?? string.Empty,
            };
        }

        private static void Add(Dictionary<string, decimal> dict, string key, decimal? val)
        {
            if (val.HasValue) dict[key] = val.Value;
        }

        private void MapApproverProvisions(PayrollApproverDetailDTO detail, PayrollCheckerRow row, string cat)
        {
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

        private void MapApproverEarnings(PayrollApproverDetailDTO detail, PayrollCheckerRow row, string cat)
        {
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

        private void MapApproverDeductions(PayrollApproverDetailDTO detail, PayrollCheckerRow row, string cat)
        {
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
    }
}