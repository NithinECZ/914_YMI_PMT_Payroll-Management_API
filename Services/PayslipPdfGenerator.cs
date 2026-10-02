using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using YMI_PMT_PayrollManagement_API.DTOs.Payslip;

namespace YMI_PMT_PayrollManagement_API.Services
{
    /// <summary>
    /// Generates standalone standard PDF 1.4 payslip documents in pure C#
    /// embedding the official Yamaha branded background template (PAYSLIP.png)
    /// without any external DLL or NuGet dependencies.
    /// </summary>
    public static class PayslipPdfGenerator
    {
        private static PngHelper.DecodedPng? _cachedBg = null;
        private static bool _bgLoadAttempted = false;
        private static readonly object _bgLock = new object();

        private static PngHelper.DecodedPng? GetBackgroundPng()
        {
            if (_bgLoadAttempted) return _cachedBg;
            lock (_bgLock)
            {
                if (_bgLoadAttempted) return _cachedBg;
                _bgLoadAttempted = true;

                string[] paths = new[]
                {
                    @"D:\914_YMI_PMT_PayrollManagementTool\YMI_PMT_PayrollManagement_Web\src\assets\images\PAYSLIP.png",
                    Path.Combine(AppContext.BaseDirectory, "Resources", "PAYSLIP.png"),
                    Path.Combine(AppContext.BaseDirectory, "PAYSLIP.png"),
                    Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "YMI_PMT_PayrollManagement_Web", "src", "assets", "images", "PAYSLIP.png"),
                    Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Resources", "PAYSLIP.png"),
                };

                foreach (var p in paths)
                {
                    try
                    {
                        if (File.Exists(p))
                        {
                            _cachedBg = PngHelper.TryLoadAndDecode(p);
                            if (_cachedBg != null) break;
                        }
                    }
                    catch
                    {
                        // continue checking fallback paths
                    }
                }
                return _cachedBg;
            }
        }

        public static byte[] GeneratePayslipPdf(PayslipEmployeeDTO emp, string category, string fromDate, string toDate)
        {
            return GenerateCombinedPayslipsPdf(new List<PayslipEmployeeDTO> { emp }, category, fromDate, toDate);
        }

        public static byte[] GenerateCombinedPayslipsPdf(List<PayslipEmployeeDTO> employees, string category, string fromDate, string toDate)
        {
            if (employees == null || employees.Count == 0)
                return Array.Empty<byte>();

            double width = 595.28;  // A4 width in pt
            double height = 841.89; // A4 height in pt
            int pageCount = employees.Count;

            var bg = GetBackgroundPng();
            bool hasBg = (bg != null && bg.CompressedRgb.Length > 0);

            // Object ID Layout:
            // 1: Catalog
            // 2: Pages
            // If hasBg:
            //   3: Background Image XObject
            //   Page i (0..N-1): pageObjId = 4 + 2*i, contentObjId = 5 + 2*i
            //   Font F1: 4 + 2*N
            //   Font F2: 5 + 2*N
            // Else:
            //   Page i (0..N-1): pageObjId = 3 + 2*i, contentObjId = 4 + 2*i
            //   Font F1: 3 + 2*N
            //   Font F2: 4 + 2*N

            int bgObjId = hasBg ? 3 : 0;
            int pageStartObjId = hasBg ? 4 : 3;
            int f1ObjId = pageStartObjId + 2 * pageCount;
            int f2ObjId = pageStartObjId + 2 * pageCount + 1;
            int totalObjects = f2ObjId;

            var ms = new MemoryStream();
            var offsets = new List<long>();

            void WritePdfObject(string headerText, byte[]? binaryStream = null, string? trailerText = null)
            {
                offsets.Add(ms.Position);
                var headBytes = Encoding.ASCII.GetBytes(headerText);
                ms.Write(headBytes, 0, headBytes.Length);
                if (binaryStream != null && binaryStream.Length > 0)
                {
                    ms.Write(binaryStream, 0, binaryStream.Length);
                }
                if (!string.IsNullOrEmpty(trailerText))
                {
                    var trailBytes = Encoding.ASCII.GetBytes(trailerText);
                    ms.Write(trailBytes, 0, trailBytes.Length);
                }
            }

            // PDF 1.4 Header
            var pdfHeader = Encoding.ASCII.GetBytes("%PDF-1.4\n%\xE2\xE3\xCF\xD3\n");
            ms.Write(pdfHeader, 0, pdfHeader.Length);

            // 1. Catalog Object
            WritePdfObject("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

            // Build Kids list for Pages object
            var kidsSb = new StringBuilder();
            for (int i = 0; i < pageCount; i++)
            {
                int pId = pageStartObjId + 2 * i;
                kidsSb.Append($"{pId} 0 R ");
            }

            // 2. Pages Object
            WritePdfObject($"2 0 obj\n<< /Type /Pages /Kids [{kidsSb.ToString().Trim()}] /Count {pageCount} >>\nendobj\n");

            // 3. Background Image XObject (if available)
            if (hasBg && bg != null)
            {
                string imgHeader = string.Format(CultureInfo.InvariantCulture,
                    "{0} 0 obj\n<< /Type /XObject /Subtype /Image /Width {1} /Height {2} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /FlateDecode /Length {3} >>\nstream\n",
                    bgObjId, bg.Width, bg.Height, bg.CompressedRgb.Length);
                string imgFooter = "\nendstream\nendobj\n";
                WritePdfObject(imgHeader, bg.CompressedRgb, imgFooter);
            }

            // Generate Pages and Content objects for each employee
            for (int i = 0; i < pageCount; i++)
            {
                int pId = pageStartObjId + 2 * i;
                int cId = pageStartObjId + 2 * i + 1;

                string pageStream = BuildSinglePayslipStream(employees[i], category, fromDate, toDate, i + 1, pageCount, hasBg, width, height);
                byte[] contentBytes = Encoding.ASCII.GetBytes(pageStream);

                string resourceDict = hasBg
                    ? $"/Font << /F1 {f1ObjId} 0 R /F2 {f2ObjId} 0 R >> /XObject << /BgImg {bgObjId} 0 R >>"
                    : $"/Font << /F1 {f1ObjId} 0 R /F2 {f2ObjId} 0 R >>";

                // Page Object
                string pageObjStr = string.Format(CultureInfo.InvariantCulture,
                    "{0} 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {1:0.##} {2:0.##}] /Contents {3} 0 R /Resources << {4} >> >>\nendobj\n",
                    pId, width, height, cId, resourceDict);
                WritePdfObject(pageObjStr);

                // Contents Object
                string contentHeader = $"{cId} 0 obj\n<< /Length {contentBytes.Length} >>\nstream\n{pageStream}endstream\nendobj\n";
                WritePdfObject(contentHeader);
            }

            // Fonts
            WritePdfObject($"{f1ObjId} 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n");
            WritePdfObject($"{f2ObjId} 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>\nendobj\n");

            // Cross-Reference Table
            long xrefOffset = ms.Position;
            var xrefSb = new StringBuilder();
            xrefSb.Append("xref\n");
            xrefSb.Append($"0 {totalObjects + 1}\n");
            xrefSb.Append("0000000000 65535 f \r\n");
            foreach (var off in offsets)
            {
                xrefSb.Append(string.Format(CultureInfo.InvariantCulture, "{0:D10} 00000 n \r\n", off));
            }

            xrefSb.Append("trailer\n");
            xrefSb.Append($"<< /Size {totalObjects + 1} /Root 1 0 R >>\n");
            xrefSb.Append("startxref\n");
            xrefSb.Append($"{xrefOffset}\n");
            xrefSb.Append("%%EOF\n");

            var xrefBytes = Encoding.ASCII.GetBytes(xrefSb.ToString());
            ms.Write(xrefBytes, 0, xrefBytes.Length);

            return ms.ToArray();
        }

        private static string BuildSinglePayslipStream(
            PayslipEmployeeDTO emp,
            string category,
            string fromDate,
            string toDate,
            int pageNum,
            int totalPages,
            bool hasBg,
            double width,
            double height)
        {
            var stream = new StringBuilder();

            void Rect(double x, double y, double w, double h, double r, double g, double b, bool fill = true)
            {
                if (fill)
                {
                    stream.Append(string.Format(CultureInfo.InvariantCulture, "{0:0.##} {1:0.##} {2:0.##} rg\n", r, g, b));
                    stream.Append(string.Format(CultureInfo.InvariantCulture, "{0:0.##} {1:0.##} {2:0.##} {3:0.##} re f\n", x, y, w, h));
                }
                else
                {
                    stream.Append(string.Format(CultureInfo.InvariantCulture, "{0:0.##} {1:0.##} {2:0.##} RG\n", r, g, b));
                    stream.Append(string.Format(CultureInfo.InvariantCulture, "{0:0.##} {1:0.##} {2:0.##} {3:0.##} re S\n", x, y, w, h));
                }
            }

            void RoundedRect(double x, double y, double w, double h, double radius, double r, double g, double b, bool fill = true, double strokeWidth = 1.0)
            {
                if (fill)
                {
                    stream.Append(string.Format(CultureInfo.InvariantCulture, "{0:0.##} {1:0.##} {2:0.##} rg\n", r, g, b));
                }
                else
                {
                    stream.Append(string.Format(CultureInfo.InvariantCulture, "{0:0.##} w\n", strokeWidth));
                    stream.Append(string.Format(CultureInfo.InvariantCulture, "{0:0.##} {1:0.##} {2:0.##} RG\n", r, g, b));
                }
                double k = radius * 0.55228475;
                double x0 = x + radius, x1 = x + w - radius;
                double y0 = y + radius, y1 = y + h - radius;
                stream.Append(string.Format(CultureInfo.InvariantCulture,
                    "{0:0.##} {1:0.##} m " +
                    "{2:0.##} {1:0.##} l {3:0.##} {1:0.##} {4:0.##} {5:0.##} {4:0.##} {6:0.##} c " +
                    "{4:0.##} {7:0.##} l {4:0.##} {8:0.##} {3:0.##} {9:0.##} {2:0.##} {9:0.##} c " +
                    "{0:0.##} {9:0.##} l {10:0.##} {9:0.##} {11:0.##} {8:0.##} {11:0.##} {7:0.##} c " +
                    "{11:0.##} {6:0.##} l {11:0.##} {5:0.##} {10:0.##} {1:0.##} {0:0.##} {1:0.##} c {12}\n",
                    x0, y, x1, x + w - radius + k, x + w, y + radius - k, y0,
                    y1, y + h - radius + k, y + h, x + radius - k, x,
                    fill ? "f" : "S"));
            }

            void Circle(double cx, double cy, double radius, double r, double g, double b, bool fill = true)
            {
                if (fill)
                {
                    stream.Append(string.Format(CultureInfo.InvariantCulture, "{0:0.##} {1:0.##} {2:0.##} rg\n", r, g, b));
                }
                else
                {
                    stream.Append(string.Format(CultureInfo.InvariantCulture, "{0:0.##} RG\n", r, g, b));
                }
                double k = radius * 0.55228475;
                stream.Append(string.Format(CultureInfo.InvariantCulture,
                    "{0:0.##} {1:0.##} m " +
                    "{2:0.##} {1:0.##} {3:0.##} {4:0.##} {3:0.##} {5:0.##} c " +
                    "{3:0.##} {6:0.##} {2:0.##} {7:0.##} {0:0.##} {7:0.##} c " +
                    "{8:0.##} {7:0.##} {9:0.##} {6:0.##} {9:0.##} {5:0.##} c " +
                    "{9:0.##} {4:0.##} {8:0.##} {1:0.##} {0:0.##} {1:0.##} c {10}\n",
                    cx, cy - radius,
                    cx + k, cx + radius, cy - k, cy,
                    cy + k, cy + radius,
                    cx - k, cx - radius,
                    fill ? "f" : "S"));
            }

            void Line(double x1, double y1, double x2, double y2, double r = 0.8, double g = 0.8, double b = 0.8, double lw = 0.5)
            {
                stream.Append(string.Format(CultureInfo.InvariantCulture, "{0:0.##} w\n", lw));
                stream.Append(string.Format(CultureInfo.InvariantCulture, "{0:0.##} {1:0.##} {2:0.##} RG\n", r, g, b));
                stream.Append(string.Format(CultureInfo.InvariantCulture, "{0:0.##} {1:0.##} m {2:0.##} {3:0.##} l S\n", x1, y1, x2, y2));
            }

            void Text(string text, double x, double y, string font = "/F1", double size = 10, double r = 0, double g = 0, double b = 0)
            {
                if (string.IsNullOrEmpty(text)) return;
                var safe = text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
                stream.Append(string.Format(CultureInfo.InvariantCulture, "{0:0.##} {1:0.##} {2:0.##} rg\n", r, g, b));
                stream.Append(string.Format(CultureInfo.InvariantCulture, "BT {0} {1:0.##} Tf {2:0.##} {3:0.##} Td ({4}) Tj ET\n", font, size, x, y, safe));
            }

            void RightText(string text, double rightX, double y, string font = "/F1", double size = 10, double r = 0, double g = 0, double b = 0)
            {
                if (string.IsNullOrEmpty(text)) return;
                bool isBold = font.Contains("F2");
                double w = MeasureHelvetica(text, size, isBold);
                Text(text, rightX - w, y, font, size, r, g, b);
            }

            void CenterText(string text, double centerX, double y, string font = "/F1", double size = 10, double r = 0, double g = 0, double b = 0)
            {
                if (string.IsNullOrEmpty(text)) return;
                bool isBold = font.Contains("F2");
                double w = MeasureHelvetica(text, size, isBold);
                Text(text, centerX - (w / 2.0), y, font, size, r, g, b);
            }

            // 1. Paint Yamaha Background Template (PAYSLIP.png) across entire page
            if (hasBg)
            {
                stream.Append(string.Format(CultureInfo.InvariantCulture,
                    "q {0:0.##} 0 0 {1:0.##} 0 0 cm /BgImg Do Q\n",
                    width, height));
            }
            else
            {
                // Fallback background border and header banner
                Rect(20, 20, 555.28, 801.89, 0.98, 0.98, 0.99, fill: true);
                Rect(20, 20, 555.28, 801.89, 0.8, 0.83, 0.88, fill: false);
                Rect(20, 755, 555.28, 66.89, 0.08, 0.22, 0.45, fill: true);
                Text("INDIA YAMAHA MOTOR PVT. LTD.", 36, 792, "/F2", 18, 1, 1, 1);
                Text("Official Payroll Statement / Payslip", 36, 775, "/F1", 11, 0.85, 0.9, 1);
            }

            // 2. Month Label Resolution (e.g. "AUGUST 2026")
            string monthYearStr = GetMonthYearLabel(emp.FromDate, emp.ToDate, fromDate, toDate);

            // 3. Top Header Right: "PAYSLIP" Title & "FOR THE MONTH OF [MONTH YEAR]" Badge
            // Positioned cleanly in the white space below the piano curve (matching View Slip preview)
            RightText("PAYSLIP", 567.28, 724, "/F2", 26, 0.04, 0.15, 0.25);
            RightText("FOR THE MONTH OF", 466, 705, "/F2", 7.5, 0.20, 0.25, 0.35);

            // Dark Navy Badge with Month Year
            RoundedRect(472, 700, 95.28, 16, 3, 0.04, 0.15, 0.25, fill: true);
            CenterText(monthYearStr, 472 + (95.28 / 2.0), 704.5, "/F2", 8, 1, 1, 1);

            // 4. EMPLOYEE DETAILS Box (Matches View Slip modal exactly)
            // Dimensions: X=28, Width=539.28, Y=581, Height=94
            double empBoxY = 581;
            double empBoxH = 94;
            double contentW = 539.28;
            double leftMargin = 28;
            double rightMargin = leftMargin + contentW; // 567.28

            // Outer Card Background and Border
            RoundedRect(leftMargin, empBoxY, contentW, empBoxH, 4, 1, 1, 1, fill: true);
            RoundedRect(leftMargin, empBoxY, contentW, empBoxH, 4, 0.12, 0.23, 0.54, fill: false, strokeWidth: 1.2);

            // Header Bar (#1E3A8A Dark Navy Blue)
            Rect(leftMargin + 0.5, empBoxY + empBoxH - 18, contentW - 1, 18, 0.12, 0.23, 0.54, fill: true);
            Text("EMPLOYEE DETAILS", leftMargin + 14, empBoxY + empBoxH - 13, "/F2", 9, 1, 1, 1);

            // 2-Column Details formatting
            string dojStr = emp.DateOfJoining.HasValue ? emp.DateOfJoining.Value.ToString("dd MMM yyyy") : "-";
            string pDaysStr = (emp.PeriodDays ?? 0).ToString(CultureInfo.InvariantCulture);
            string wDaysStr = emp.WorkingDays.HasValue
                ? ((emp.WorkingDays.Value % 1 == 0)
                    ? ((long)emp.WorkingDays.Value).ToString(CultureInfo.InvariantCulture)
                    : emp.WorkingDays.Value.ToString("0.#", CultureInfo.InvariantCulture))
                : "0";
            string desigStr = !string.IsNullOrWhiteSpace(emp.Qualification)
                ? emp.Qualification
                : (!string.IsNullOrWhiteSpace(emp.Designation) ? emp.Designation : "-");

            void DetailRow(double yPos, string l1, string v1, string l2, string v2, bool isWorkedDays = false)
            {
                // Left Column
                Text(l1, leftMargin + 14, yPos, "/F2", 8, 0.28, 0.33, 0.41);
                Text(":", leftMargin + 118, yPos, "/F2", 8, 0.28, 0.33, 0.41);
                Text(v1, leftMargin + 128, yPos, "/F2", 8, 0.06, 0.09, 0.16);

                // Right Column
                Text(l2, leftMargin + 270, yPos, "/F2", 8, 0.28, 0.33, 0.41);
                Text(":", leftMargin + 355, yPos, "/F2", 8, 0.28, 0.33, 0.41);
                if (isWorkedDays)
                {
                    // Bold Green for Worked Days
                    Text(v2, leftMargin + 365, yPos, "/F2", 8.5, 0.09, 0.64, 0.29);
                }
                else
                {
                    Text(v2, leftMargin + 365, yPos, "/F2", 8, 0.06, 0.09, 0.16);
                }
            }

            DetailRow(empBoxY + 62, "Employee Code", emp.EmployeeId ?? "-", "PF No", emp.PfNo ?? "-");
            DetailRow(empBoxY + 48, "Employee Name", emp.EmployeeName ?? "-", "ESI No", emp.EsiNo ?? "-");
            DetailRow(empBoxY + 34, "Designation", desigStr, "PAN No", emp.PanNo ?? "-");
            DetailRow(empBoxY + 20, "Date of Joining (DOJ)", dojStr, "UAN No", emp.UanNo ?? "-");
            DetailRow(empBoxY + 6,  "Days in Month", pDaysStr, "Worked Days", wDaysStr, isWorkedDays: true);

            // 5. Side-by-Side Tables: EARNINGS & DEDUCTIONS
            double tablesTopY = 572;
            double colW = 264;
            double leftX = leftMargin;
            double rightX = leftMargin + colW + 11.28; // 303.28

            bool isNaps = string.Equals(category, "NAPS", StringComparison.OrdinalIgnoreCase);

            List<(string name, decimal amount)> ernList;
            List<(string name, decimal amount)> dedList;

            if (isNaps)
            {
                decimal earnedWages = GetDictOr(emp.Earnings, "earnedWages", 0);
                decimal otEarning = GetDictOr(emp.Earnings, "otEarning", 0);
                decimal handlingCharge = GetDictOr(emp.Earnings, "handlingCharge", 0);

                ernList = new List<(string name, decimal amount)>
                {
                    ("Earned Wages", earnedWages),
                    ("OT Earning", otEarning),
                    ("Handling Charge", handlingCharge),
                };

                decimal hostelDed = GetDictOr(emp.Deductions, "hostelDed",
                    GetDictOr(emp.Deductions, "hostelFee",
                    GetDictOr(emp.Deductions, "hostelDeduction", 0)));

                dedList = new List<(string name, decimal amount)>
                {
                    ("Hostel Deduction", hostelDed),
                };
            }
            else
            {
                decimal attnBonus = GetDictOr(emp.Earnings, "attnBonus", 0);
                decimal earnedBasicDa = GetDictOr(emp.Earnings, "earnedBasicDa", 0);
                decimal earnedHra = GetDictOr(emp.Earnings, "earnedHra", 0);
                decimal otAmount = GetDictOr(emp.Earnings, "otAmount", 0);
                decimal epf = GetDictOr(emp.Earnings, "pf", GetDictOr(emp.Earnings, "epf", 0));
                decimal esi = GetDictOr(emp.Earnings, "esi", 0);
                decimal bonus = GetDictOr(emp.Earnings, "bonus", GetDictOr(emp.Earnings, "bonusDed", 0));
                decimal serChar = GetDictOr(emp.Earnings, "serChar", 0);
                decimal lwf = GetDictOr(emp.Earnings, "lwf", 0);

                ernList = new List<(string name, decimal amount)>
                {
                    ("Attendance Bonus", attnBonus),
                    ("Basic + DA", earnedBasicDa),
                    ("HRA", earnedHra),
                    ("OT Amount", otAmount),
                    ("EPF", epf),
                    ("ESI", esi),
                    ("Bonus", bonus),
                    ("Service Charge", serChar),
                    ("LWF", lwf),
                };

                decimal pfDed = GetDictOr(emp.Deductions, "pf", 0);
                decimal esiDed = GetDictOr(emp.Deductions, "esi", 0);
                decimal hostelDed = GetDictOr(emp.Deductions, "hostelDeduction", GetDictOr(emp.Deductions, "hostelDed", 0));

                dedList = new List<(string name, decimal amount)>
                {
                    ("PF", pfDed),
                    ("ESI", esiDed),
                    ("Hostel Deduction", hostelDed),
                };
            }

            int targetRowCount = Math.Max(ernList.Count, Math.Max(dedList.Count, 9));
            double rowH = 12.5;
            double dataH = targetRowCount * rowH;
            double headerH = 17;
            double subHeaderH = 14;
            double footerH = 17;
            double totalTableH = headerH + subHeaderH + dataH + footerH; // 17 + 14 + 112.5 + 17 = 160.5
            double tableBottomY = tablesTopY - totalTableH; // 572 - 160.5 = 411.5

            // Left Table (EARNINGS) Outer Box
            RoundedRect(leftX, tableBottomY, colW, totalTableH, 4, 1, 1, 1, fill: true);
            RoundedRect(leftX, tableBottomY, colW, totalTableH, 4, 0.12, 0.23, 0.54, fill: false, strokeWidth: 1.2);

            // Left Header Bar (#1E3A8A)
            Rect(leftX + 0.5, tablesTopY - headerH, colW - 1, headerH, 0.12, 0.23, 0.54, fill: true);
            Text("EARNINGS", leftX + 12, tablesTopY - 12.5, "/F2", 9, 1, 1, 1);

            // Left Subheader Bar (#E0F2FE)
            double subY = tablesTopY - headerH - subHeaderH;
            Rect(leftX + 0.5, subY, colW - 1, subHeaderH, 0.88, 0.95, 0.99, fill: true);
            Line(leftX, subY, leftX + colW, subY, 0.58, 0.77, 0.99, lw: 0.5);
            Text("Components", leftX + 10, subY + 4, "/F2", 8, 0.12, 0.23, 0.54);
            RightText("Amount (Rs.)", leftX + colW - 10, subY + 4, "/F2", 8, 0.12, 0.23, 0.54);

            // Right Table (DEDUCTIONS) Outer Box
            RoundedRect(rightX, tableBottomY, colW, totalTableH, 4, 1, 1, 1, fill: true);
            RoundedRect(rightX, tableBottomY, colW, totalTableH, 4, 0.12, 0.23, 0.54, fill: false, strokeWidth: 1.2);

            // Right Header Bar (#1E3A8A)
            Rect(rightX + 0.5, tablesTopY - headerH, colW - 1, headerH, 0.12, 0.23, 0.54, fill: true);
            Text("DEDUCTIONS", rightX + 12, tablesTopY - 12.5, "/F2", 9, 1, 1, 1);

            // Right Subheader Bar (#E0F2FE)
            Rect(rightX + 0.5, subY, colW - 1, subHeaderH, 0.88, 0.95, 0.99, fill: true);
            Line(rightX, subY, rightX + colW, subY, 0.58, 0.77, 0.99, lw: 0.5);
            Text("Components", rightX + 10, subY + 4, "/F2", 8, 0.12, 0.23, 0.54);
            RightText("Amount (Rs.)", rightX + colW - 10, subY + 4, "/F2", 8, 0.12, 0.23, 0.54);

            // Render Table Rows (Alternating zebra & padded with '-')
            for (int i = 0; i < targetRowCount; i++)
            {
                double rY = subY - (i + 1) * rowH;
                bool isAlt = (i % 2 == 1);

                if (isAlt)
                {
                    Rect(leftX + 0.5, rY, colW - 1, rowH, 0.97, 0.98, 0.99, fill: true);
                    Rect(rightX + 0.5, rY, colW - 1, rowH, 0.97, 0.98, 0.99, fill: true);
                }

                Line(leftX, rY, leftX + colW, rY, 0.88, 0.91, 0.94, lw: 0.5);
                Line(rightX, rY, rightX + colW, rY, 0.88, 0.91, 0.94, lw: 0.5);

                double textY = rY + 3.5;

                // Left row item
                if (i < ernList.Count)
                {
                    var (nm, amt) = ernList[i];
                    Text(nm, leftX + 10, textY, "/F1", 7.5, 0.20, 0.25, 0.33);
                    RightText(amt.ToString("N2", CultureInfo.InvariantCulture), leftX + colW - 10, textY, "/F2", 7.5, 0.06, 0.09, 0.16);
                }
                else
                {
                    Text("-", leftX + 10, textY, "/F1", 7.5, 0.75, 0.80, 0.85);
                    RightText("-", leftX + colW - 10, textY, "/F1", 7.5, 0.75, 0.80, 0.85);
                }

                // Right row item
                if (i < dedList.Count)
                {
                    var (nm, amt) = dedList[i];
                    Text(nm, rightX + 10, textY, "/F1", 7.5, 0.20, 0.25, 0.33);
                    RightText(amt.ToString("N2", CultureInfo.InvariantCulture), rightX + colW - 10, textY, "/F2", 7.5, 0.06, 0.09, 0.16);
                }
                else
                {
                    Text("-", rightX + 10, textY, "/F1", 7.5, 0.75, 0.80, 0.85);
                    RightText("-", rightX + colW - 10, textY, "/F1", 7.5, 0.75, 0.80, 0.85);
                }
            }

            // Table Footers
            double footY = tableBottomY;
            decimal sumErn = 0;
            foreach (var item in ernList) sumErn += item.amount;
            decimal totalEarnings;
            if (isNaps)
            {
                totalEarnings = sumErn > 0 ? sumErn : (emp.Gross ?? 0);
            }
            else
            {
                if (emp.TotalDed.HasValue && emp.TotalDed.Value > 0)
                    totalEarnings = emp.TotalDed.Value;
                else if (emp.Gross.HasValue && emp.Gross.Value > 0)
                    totalEarnings = emp.Gross.Value;
                else
                    totalEarnings = sumErn;
            }

            decimal sumDed = 0;
            foreach (var item in dedList) sumDed += item.amount;
            decimal totalDeductions = (emp.TotalDeductions.HasValue && emp.TotalDeductions.Value > 0)
                ? emp.TotalDeductions.Value
                : sumDed;

            // Left Table Footer
            Rect(leftX + 0.5, footY + 0.5, colW - 1, footerH - 1, 0.95, 0.96, 0.98, fill: true);
            Line(leftX, footY + footerH, leftX + colW, footY + footerH, 0.12, 0.23, 0.54, lw: 1.2);
            Text("Total Earnings", leftX + 10, footY + 5, "/F2", 8.5, 0.06, 0.09, 0.16);
            RightText(totalEarnings.ToString("N2", CultureInfo.InvariantCulture), leftX + colW - 10, footY + 5, "/F2", 8.5, 0.06, 0.09, 0.16);

            // Right Table Footer
            Rect(rightX + 0.5, footY + 0.5, colW - 1, footerH - 1, 0.95, 0.96, 0.98, fill: true);
            Line(rightX, footY + footerH, rightX + colW, footY + footerH, 0.12, 0.23, 0.54, lw: 1.2);
            Text("Total Deductions", rightX + 10, footY + 5, "/F2", 8.5, 0.06, 0.09, 0.16);
            RightText(totalDeductions.ToString("N2", CultureInfo.InvariantCulture), rightX + colW - 10, footY + 5, "/F2", 8.5, 0.06, 0.09, 0.16);

            // 6. PROVISIONS / VARIABLE INPUTS Box (2-Line Card matching View Slip)
            decimal refB = (emp.RefBon.HasValue && emp.RefBon.Value > 0) ? emp.RefBon.Value : GetDictOr(emp.Provisions, "referalBonus", 0);
            decimal splA = (emp.SplAllw.HasValue && emp.SplAllw.Value > 0) ? emp.SplAllw.Value : GetDictOr(emp.Provisions, "splAllow", 0);
            decimal kzn = (emp.Kaizen.HasValue && emp.Kaizen.Value > 0) ? emp.Kaizen.Value : GetDictOr(emp.Provisions, "kaizen", 0);
            decimal gGmc = (emp.GpaGmc.HasValue && emp.GpaGmc.Value > 0) ? emp.GpaGmc.Value : GetDictOr(emp.Provisions, "gpaGmc", 0);
            decimal varTotal = (emp.VariableTotal.HasValue && emp.VariableTotal.Value > 0) ? emp.VariableTotal.Value : (refB + splA + kzn + gGmc);

            double provBoxTop = tableBottomY - 9; // 411.5 - 9 = 402.5
            double provH = 31;
            double provBoxY = provBoxTop - provH; // 371.5

            RoundedRect(leftMargin, provBoxY, contentW, provH, 4, 0.996, 0.988, 0.91, fill: true);
            RoundedRect(leftMargin, provBoxY, contentW, provH, 4, 0.99, 0.88, 0.28, fill: false, strokeWidth: 1.2);

            // Top Line: Bullet + "PROVISIONS / VARIABLE INPUTS :" on left, Total on right
            Circle(leftMargin + 15, provBoxY + 21, 2.2, 0.79, 0.54, 0.02, fill: true);
            Text("PROVISIONS / VARIABLE INPUTS :", leftMargin + 22, provBoxY + 18.5, "/F2", 8.5, 0.52, 0.30, 0.05);
            RightText($"Rs. {varTotal:N2}", rightMargin - 14, provBoxY + 18, "/F2", 10.5, 0.52, 0.30, 0.05);

            // Bottom Line: Component breakdown
            string provDetailStr;
            if (isNaps && splA == 0 && gGmc == 0)
            {
                provDetailStr = $"Ref Bonus: Rs. {refB:N2}    Kaizen: Rs. {kzn:N2}";
            }
            else
            {
                provDetailStr = $"Ref Bonus: Rs. {refB:N2}    Spl Allow: Rs. {splA:N2}    Kaizen: Rs. {kzn:N2}    GPA/GMC: Rs. {gGmc:N2}";
            }
            Text(provDetailStr, leftMargin + 14, provBoxY + 7, "/F2", 7.5, 0.44, 0.25, 0.07);

            // 7. NET PAY FOR THE MONTH Box (Prominent Card matching View Slip)
            double netBoxTop = provBoxY - 9; // 371.5 - 9 = 362.5
            double netBoxH = 44;
            double netBoxY = netBoxTop - netBoxH; // 318.5

            RoundedRect(leftMargin, netBoxY, contentW, netBoxH, 4, 1, 1, 1, fill: true);
            RoundedRect(leftMargin, netBoxY, contentW, netBoxH, 4, 0.75, 0.86, 0.99, fill: false, strokeWidth: 1.2);

            decimal net = emp.NetPayable ?? (totalEarnings + varTotal - totalDeductions);
            string words = NumberToWords(net);

            // Left Side: Soft blue icon badge + Titles
            RoundedRect(leftMargin + 10, netBoxY + 8, 28, 28, 4, 0.88, 0.95, 0.99, fill: true);
            CenterText("Rs", leftMargin + 24, netBoxY + 17, "/F2", 10, 0.04, 0.15, 0.25);

            Text("Net Pay for the Month", leftMargin + 46, netBoxY + 24, "/F2", 12.5, 0.06, 0.09, 0.16);
            Text("(Rupees in Words)", leftMargin + 46, netBoxY + 12, "/F1", 7.5, 0.39, 0.45, 0.55);

            // Vertical Divider Line
            Line(leftMargin + 225, netBoxY + 6, leftMargin + 225, netBoxY + netBoxH - 6, 0.80, 0.84, 0.88, lw: 0.75);

            // Right Side: Large Bold Amount & Words Below
            RightText($"Rs. {net:N2}", rightMargin - 14, netBoxY + 23, "/F2", 16, 0.06, 0.09, 0.16);
            RightText($"({words})", rightMargin - 14, netBoxY + 11, "/F1", 7, 0.20, 0.25, 0.35);

            // The remaining bottom area from Y=0 to 318 is completely clear of any opaque overlay
            // so the original Yamaha Music India template footer ("Thank you...", music notes,
            // "Music Connects People", and bottom strip) displays natively from PAYSLIP.png!

            return stream.ToString();
        }

        private static double MeasureHelvetica(string text, double fontSize, bool isBold)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            double total = 0;
            foreach (char c in text)
            {
                double charW;
                if (c >= '0' && c <= '9')
                    charW = isBold ? 0.58 : 0.556;
                else if (c == '.' || c == ',' || c == ':' || c == ';')
                    charW = 0.278;
                else if (c == ' ' || c == '-' || c == '/' || c == '(' || c == ')')
                    charW = 0.333;
                else if (char.IsUpper(c))
                    charW = isBold ? 0.72 : 0.667;
                else
                    charW = isBold ? 0.55 : 0.500;

                total += charW * fontSize;
            }
            return total;
        }

        private static string GetMonthYearLabel(DateTime? fDate, DateTime? tDate, string? fallbackF, string? fallbackT)
        {
            DateTime? dt = tDate ?? fDate;
            if (!dt.HasValue && !string.IsNullOrEmpty(fallbackT) && DateTime.TryParse(fallbackT, out var pT))
            {
                dt = pT;
            }
            if (!dt.HasValue && !string.IsNullOrEmpty(fallbackF) && DateTime.TryParse(fallbackF, out var pF))
            {
                dt = pF;
            }
            return dt.HasValue ? dt.Value.ToString("MMMM yyyy").ToUpperInvariant() : "CURRENT PERIOD";
        }

        private static decimal GetDictOr(Dictionary<string, decimal>? dict, string key, decimal def = 0)
        {
            if (dict == null) return def;
            return dict.TryGetValue(key, out var val) ? val : def;
        }

        public static string NumberToWords(decimal number)
        {
            if (number == 0) return "Zero Rupees Only";
            if (number < 0) return "Minus " + NumberToWords(Math.Abs(number));

            long intVal = (long)Math.Truncate(number);
            long decimalVal = (long)Math.Round((number - intVal) * 100);

            string words = ConvertWholeNumberToWords(intVal) + " Rupees";
            if (decimalVal > 0)
            {
                words += " and " + ConvertWholeNumberToWords(decimalVal) + " Paise";
            }

            return words + " Only";
        }

        private static string ConvertWholeNumberToWords(long n)
        {
            if (n == 0) return "Zero";

            var units = new[] { "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
            var tens = new[] { "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

            string WordsUnderThousand(long num)
            {
                var s = "";
                if (num >= 100)
                {
                    s += units[num / 100] + " Hundred ";
                    num %= 100;
                }
                if (num >= 20)
                {
                    s += tens[num / 10] + " ";
                    num %= 10;
                }
                if (num > 0)
                {
                    s += units[num] + " ";
                }
                return s.Trim();
            }

            var parts = new List<string>();

            // Crores (10,000,000)
            if (n >= 10000000)
            {
                long crore = n / 10000000;
                parts.Add(WordsUnderThousand(crore) + " Crore");
                n %= 10000000;
            }

            // Lakhs (100,000)
            if (n >= 100000)
            {
                long lakh = n / 100000;
                parts.Add(WordsUnderThousand(lakh) + " Lakh");
                n %= 100000;
            }

            // Thousands (1,000)
            if (n >= 1000)
            {
                long thousand = n / 1000;
                parts.Add(WordsUnderThousand(thousand) + " Thousand");
                n %= 1000;
            }

            // Hundreds and remaining
            if (n > 0)
            {
                parts.Add(WordsUnderThousand(n));
            }

            return string.Join(" ", parts).Trim();
        }
    }
}
