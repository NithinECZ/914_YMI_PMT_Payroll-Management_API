using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using YMI_PMT_PayrollManagement_API.DTOs.Payslip;

namespace YMI_PMT_PayrollManagement_API.Services
{
    /// <summary>
    /// Generates standard Microsoft Excel OpenXML (.xlsx) workbooks for vendor employee payslips
    /// in pure .NET without any external NuGet or third-party dependencies.
    /// </summary>
    public static class PayslipExcelGenerator
    {
        public static byte[] GeneratePayslipsExcel(
            List<PayslipEmployeeDTO> employees,
            string category,
            string fromDate,
            string toDate,
            string vendorName,
            string vendorId)
        {
            if (employees == null) employees = new List<PayslipEmployeeDTO>();
            bool isNaps = string.Equals(category, "NAPS", StringComparison.OrdinalIgnoreCase);

            using var ms = new MemoryStream();
            using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
            {
                // 1. [Content_Types].xml
                WriteArchiveFile(archive, "[Content_Types].xml", GetContentTypesXml());

                // 2. _rels/.rels
                WriteArchiveFile(archive, "_rels/.rels", GetRelsXml());

                // 3. xl/_rels/workbook.xml.rels
                WriteArchiveFile(archive, "xl/_rels/workbook.xml.rels", GetWorkbookRelsXml());

                // 4. xl/workbook.xml
                WriteArchiveFile(archive, "xl/workbook.xml", GetWorkbookXml());

                // 5. xl/styles.xml
                WriteArchiveFile(archive, "xl/styles.xml", GetStylesXml());

                // 6. xl/worksheets/sheet1.xml
                WriteArchiveFile(archive, "xl/worksheets/sheet1.xml", BuildWorksheetXml(employees, category, fromDate, toDate, vendorName, vendorId, isNaps));
            }

            return ms.ToArray();
        }

        private static void WriteArchiveFile(ZipArchive archive, string path, string content)
        {
            var entry = archive.CreateEntry(path, CompressionLevel.Fastest);
            using var entryStream = entry.Open();
            using var writer = new StreamWriter(entryStream, Encoding.UTF8);
            writer.Write(content);
        }

        private static string GetContentTypesXml()
        {
            return @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">
  <Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/>
  <Default Extension=""xml"" ContentType=""application/xml""/>
  <Override PartName=""/xl/workbook.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml""/>
  <Override PartName=""/xl/worksheets/sheet1.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml""/>
  <Override PartName=""/xl/styles.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml""/>
</Types>";
        }

        private static string GetRelsXml()
        {
            return @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""xl/workbook.xml""/>
</Relationships>";
        }

        private static string GetWorkbookRelsXml()
        {
            return @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"" Target=""worksheets/sheet1.xml""/>
  <Relationship Id=""rId2"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"" Target=""styles.xml""/>
</Relationships>";
        }

        private static string GetWorkbookXml()
        {
            return @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">
  <sheets>
    <sheet name=""Employee Payslips"" sheetId=""1"" r:id=""rId1""/>
  </sheets>
</workbook>";
        }

        private static string GetStylesXml()
        {
            // Fills:
            // 0: none, 1: gray125, 2: Dark Blue Header (#1F4E79), 3: Light Blue Zebra (#F0F4F8), 4: Total row tint (#E2E8F0)
            // Fonts:
            // 0: Normal 10pt Calibri, 1: Bold White 10pt Calibri, 2: Bold Dark 10pt Calibri
            // CellFormats (cellXfs):
            // 0: Normal text (left)
            // 1: Header (fill=2, font=1, center)
            // 2: Normal center
            // 3: Normal currency (#,##0.00 right)
            // 4: Zebra text (left)
            // 5: Zebra currency (right)
            // 6: Zebra center
            // 7: Total label (bold, fill=4, right)
            // 8: Total currency (bold, fill=4, right)
            return @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<styleSheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">
  <fonts count=""3"">
    <font><sz val=""10""/><name val=""Calibri""/></font>
    <font><b/><sz val=""10""/><color rgb=""FFFFFFFF""/><name val=""Calibri""/></font>
    <font><b/><sz val=""10""/><color rgb=""FF0F172A""/><name val=""Calibri""/></font>
  </fonts>
  <fills count=""5"">
    <fill><patternFill patternType=""none""/></fill>
    <fill><patternFill patternType=""gray125""/></fill>
    <fill><patternFill patternType=""solid""><fgColor rgb=""FF1F4E79""/><bgColor indexed=""64""/></patternFill></fill>
    <fill><patternFill patternType=""solid""><fgColor rgb=""FFF8FAFC""/><bgColor indexed=""64""/></patternFill></fill>
    <fill><patternFill patternType=""solid""><fgColor rgb=""FFE2E8F0""/><bgColor indexed=""64""/></patternFill></fill>
  </fills>
  <borders count=""2"">
    <border><left/><right/><top/><bottom/><diagonal/></border>
    <border>
      <left style=""thin""><color rgb=""FFCBD5E1""/></left>
      <right style=""thin""><color rgb=""FFCBD5E1""/></right>
      <top style=""thin""><color rgb=""FFCBD5E1""/></top>
      <bottom style=""thin""><color rgb=""FFCBD5E1""/></bottom>
      <diagonal/>
    </border>
  </borders>
  <cellStyleXfs count=""1"">
    <xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0""/>
  </cellStyleXfs>
  <cellXfs count=""9"">
    <xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""1"" xfId=""0""/>
    <xf numFmtId=""0"" fontId=""1"" fillId=""2"" borderId=""1"" xfId=""0"" applyFont=""1"" applyFill=""1"" applyBorder=""1"">
      <alignment horizontal=""center"" vertical=""center"" wrapText=""1""/>
    </xf>
    <xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""1"" xfId=""0"" applyBorder=""1"">
      <alignment horizontal=""center"" vertical=""center""/>
    </xf>
    <xf numFmtId=""4"" fontId=""0"" fillId=""0"" borderId=""1"" xfId=""0"" applyNumberFormat=""1"" applyBorder=""1"">
      <alignment horizontal=""right"" vertical=""center""/>
    </xf>
    <xf numFmtId=""0"" fontId=""0"" fillId=""3"" borderId=""1"" xfId=""0"" applyFill=""1"" applyBorder=""1""/>
    <xf numFmtId=""4"" fontId=""0"" fillId=""3"" borderId=""1"" xfId=""0"" applyNumberFormat=""1"" applyFill=""1"" applyBorder=""1"">
      <alignment horizontal=""right"" vertical=""center""/>
    </xf>
    <xf numFmtId=""0"" fontId=""0"" fillId=""3"" borderId=""1"" xfId=""0"" applyFill=""1"" applyBorder=""1"">
      <alignment horizontal=""center"" vertical=""center""/>
    </xf>
    <xf numFmtId=""0"" fontId=""2"" fillId=""4"" borderId=""1"" xfId=""0"" applyFont=""1"" applyFill=""1"" applyBorder=""1"">
      <alignment horizontal=""right"" vertical=""center""/>
    </xf>
    <xf numFmtId=""4"" fontId=""2"" fillId=""4"" borderId=""1"" xfId=""0"" applyNumberFormat=""1"" applyFont=""1"" applyFill=""1"" applyBorder=""1"">
      <alignment horizontal=""right"" vertical=""center""/>
    </xf>
  </cellXfs>
</styleSheet>";
        }

        private static string BuildWorksheetXml(
            List<PayslipEmployeeDTO> employees,
            string category,
            string fromDate,
            string toDate,
            string vendorName,
            string vendorId,
            bool isNaps)
        {
            var sb = new StringBuilder();
            sb.Append(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<worksheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">");

            // Header columns definition
            List<string> headers = new List<string>
            {
                "S.No",
                "Employee ID",
                "Employee Name",
                "Skill Category",
                "Designation",
                "Date of Joining",
                "Period From",
                "Period To",
                "Days in Month",
                "Worked Days",
                "OT Hours",
                "PF No",
                "ESI No",
                "PAN No",
                "UAN No",
                "Vendor Name",
                "Vendor ID",
                "Category"
            };

            if (isNaps)
            {
                headers.AddRange(new[]
                {
                    "Earned Wages (Rs.)",
                    "OT Earning (Rs.)",
                    "Handling Charge (Rs.)",
                    "Gross Salary (Rs.)",
                    "Hostel Deduction (Rs.)",
                    "Total Deductions (Rs.)",
                    "Ref Bonus (Rs.)",
                    "Spl Allow (Rs.)",
                    "Kaizen (Rs.)",
                    "GPA / GMC (Rs.)",
                    "Total Provisions (Rs.)",
                    "Net Payable (Rs.)"
                });
            }
            else
            {
                headers.AddRange(new[]
                {
                    "Attendance Bonus (Rs.)",
                    "Earned Basic + DA (Rs.)",
                    "Earned HRA (Rs.)",
                    "OT Amount (Rs.)",
                    "Gross Salary (Rs.)",
                    "EPF Deduction (Rs.)",
                    "ESI Deduction (Rs.)",
                    "Bonus Deduction (Rs.)",
                    "Service Charge (Rs.)",
                    "LWF Deduction (Rs.)",
                    "Hostel Deduction (Rs.)",
                    "Total Deductions (Rs.)",
                    "Ref Bonus (Rs.)",
                    "Spl Allow (Rs.)",
                    "Kaizen (Rs.)",
                    "GPA / GMC (Rs.)",
                    "Total Provisions (Rs.)",
                    "Net Payable (Rs.)"
                });
            }

            int colCount = headers.Count;

            // Set column widths
            sb.Append("<cols>");
            for (int c = 1; c <= colCount; c++)
            {
                double width = 14;
                if (c == 1) width = 8;
                else if (c == 2) width = 16;
                else if (c == 3) width = 24;
                else if (c == 4 || c == 5) width = 18;
                else if (c >= 6 && c <= 8) width = 14;
                else if (c >= 12 && c <= 17) width = 18;
                else width = 16;

                sb.Append(string.Format(CultureInfo.InvariantCulture, @"<col min=""{0}"" max=""{0}"" width=""{1:0.##}"" customWidth=""1""/>", c, width));
            }
            sb.Append("</cols>");

            sb.Append("<sheetData>");

            // Row 1: Header Row
            int rowIdx = 1;
            sb.Append(string.Format(CultureInfo.InvariantCulture, @"<row r=""{0}"" ht=""26"" customHeight=""1"">", rowIdx));
            for (int c = 1; c <= colCount; c++)
            {
                string cellRef = GetCellRef(c, rowIdx);
                sb.Append(string.Format(CultureInfo.InvariantCulture,
                    @"<c r=""{0}"" t=""inlineStr"" s=""1""><is><t>{1}</t></is></c>",
                    cellRef, EscapeXml(headers[c - 1])));
            }
            sb.Append("</row>");

            // Data Rows
            decimal sumGross = 0;
            decimal sumDeductions = 0;
            decimal sumProvisions = 0;
            decimal sumNetPayable = 0;

            for (int i = 0; i < employees.Count; i++)
            {
                rowIdx++;
                var emp = employees[i];
                bool isAlt = (i % 2 == 1);
                int textStyle = isAlt ? 4 : 0;
                int centerStyle = isAlt ? 6 : 2;
                int numStyle = isAlt ? 5 : 3;

                decimal refB = (emp.RefBon.HasValue && emp.RefBon.Value > 0) ? emp.RefBon.Value : GetDictOr(emp.Provisions, "referalBonus", 0);
                decimal splA = (emp.SplAllw.HasValue && emp.SplAllw.Value > 0) ? emp.SplAllw.Value : GetDictOr(emp.Provisions, "splAllow", 0);
                decimal kzn = (emp.Kaizen.HasValue && emp.Kaizen.Value > 0) ? emp.Kaizen.Value : GetDictOr(emp.Provisions, "kaizen", 0);
                decimal gGmc = (emp.GpaGmc.HasValue && emp.GpaGmc.Value > 0) ? emp.GpaGmc.Value : GetDictOr(emp.Provisions, "gpaGmc", 0);
                decimal totalProv = (emp.VariableTotal.HasValue && emp.VariableTotal.Value > 0) ? emp.VariableTotal.Value : (refB + splA + kzn + gGmc);

                decimal gross = emp.Gross ?? 0;
                decimal deductions = emp.TotalDeductions ?? (emp.TotalDed ?? 0);
                decimal net = emp.NetPayable ?? (gross + totalProv - deductions);

                sumGross += gross;
                sumDeductions += deductions;
                sumProvisions += totalProv;
                sumNetPayable += net;

                string pFrom = emp.FromDate?.ToString("yyyy-MM-dd") ?? fromDate ?? "";
                string pTo = emp.ToDate?.ToString("yyyy-MM-dd") ?? toDate ?? "";

                sb.Append(string.Format(CultureInfo.InvariantCulture, @"<row r=""{0}"" ht=""20"" customHeight=""1"">", rowIdx));

                // 1. S.No
                WriteCell(sb, 1, rowIdx, (i + 1).ToString(), centerStyle);
                // 2. Emp ID
                WriteCell(sb, 2, rowIdx, emp.EmployeeId, centerStyle);
                // 3. Name
                WriteCell(sb, 3, rowIdx, emp.EmployeeName, textStyle);
                // 4. Skill
                WriteCell(sb, 4, rowIdx, emp.SkillCategory, centerStyle);
                // 5. Designation
                WriteCell(sb, 5, rowIdx, emp.Designation, textStyle);
                // 6. DOJ
                WriteCell(sb, 6, rowIdx, emp.DateOfJoining?.ToString("yyyy-MM-dd") ?? "-", centerStyle);
                // 7. Period From
                WriteCell(sb, 7, rowIdx, pFrom, centerStyle);
                // 8. Period To
                WriteCell(sb, 8, rowIdx, pTo, centerStyle);
                // 9. Period Days
                WriteCell(sb, 9, rowIdx, (emp.PeriodDays ?? 0).ToString(), centerStyle);
                // 10. Worked Days
                WriteNumCell(sb, 10, rowIdx, emp.WorkingDays ?? 0, numStyle);
                // 11. OT Hours
                WriteNumCell(sb, 11, rowIdx, emp.OtHrs ?? 0, numStyle);
                // 12. PF No
                WriteCell(sb, 12, rowIdx, emp.PfNo ?? "-", centerStyle);
                // 13. ESI No
                WriteCell(sb, 13, rowIdx, emp.EsiNo ?? "-", centerStyle);
                // 14. PAN No
                WriteCell(sb, 14, rowIdx, emp.PanNo ?? "-", centerStyle);
                // 15. UAN No
                WriteCell(sb, 15, rowIdx, emp.UanNo ?? "-", centerStyle);
                // 16. Vendor Name
                WriteCell(sb, 16, rowIdx, emp.Vendor ?? vendorName, textStyle);
                // 17. Vendor ID
                WriteCell(sb, 17, rowIdx, emp.VendorId ?? vendorId, centerStyle);
                // 18. Category
                WriteCell(sb, 18, rowIdx, category.ToUpper(), centerStyle);

                if (isNaps)
                {
                    decimal earnedWages = GetDictOr(emp.Earnings, "earnedWages", 0);
                    decimal otEarning = GetDictOr(emp.Earnings, "otEarning", 0);
                    decimal handlingCharge = GetDictOr(emp.Earnings, "handlingCharge", 0);
                    decimal hostelDed = GetDictOr(emp.Deductions, "hostelDed",
                        GetDictOr(emp.Deductions, "hostelFee",
                        GetDictOr(emp.Deductions, "hostelDeduction", 0)));

                    WriteNumCell(sb, 19, rowIdx, earnedWages, numStyle);
                    WriteNumCell(sb, 20, rowIdx, otEarning, numStyle);
                    WriteNumCell(sb, 21, rowIdx, handlingCharge, numStyle);
                    WriteNumCell(sb, 22, rowIdx, gross, numStyle);
                    WriteNumCell(sb, 23, rowIdx, hostelDed, numStyle);
                    WriteNumCell(sb, 24, rowIdx, deductions, numStyle);
                    WriteNumCell(sb, 25, rowIdx, refB, numStyle);
                    WriteNumCell(sb, 26, rowIdx, splA, numStyle);
                    WriteNumCell(sb, 27, rowIdx, kzn, numStyle);
                    WriteNumCell(sb, 28, rowIdx, gGmc, numStyle);
                    WriteNumCell(sb, 29, rowIdx, totalProv, numStyle);
                    WriteNumCell(sb, 30, rowIdx, net, numStyle);
                }
                else
                {
                    decimal attnBonus = GetDictOr(emp.Earnings, "attnBonus", 0);
                    decimal basicDa = GetDictOr(emp.Earnings, "earnedBasicDa", 0);
                    decimal hra = GetDictOr(emp.Earnings, "earnedHra", 0);
                    decimal otAmt = GetDictOr(emp.Earnings, "otAmount", 0);

                    decimal epf = GetDictOr(emp.Earnings, "epf", GetDictOr(emp.Deductions, "pf", 0));
                    decimal esi = GetDictOr(emp.Earnings, "esi", GetDictOr(emp.Deductions, "esi", 0));
                    decimal bonus = GetDictOr(emp.Earnings, "bonus", 0);
                    decimal serChar = GetDictOr(emp.Earnings, "serChar", 0);
                    decimal lwf = GetDictOr(emp.Earnings, "lwf", 0);
                    decimal hostelDed = GetDictOr(emp.Deductions, "hostelDeduction", GetDictOr(emp.Deductions, "hostelDed", 0));

                    WriteNumCell(sb, 19, rowIdx, attnBonus, numStyle);
                    WriteNumCell(sb, 20, rowIdx, basicDa, numStyle);
                    WriteNumCell(sb, 21, rowIdx, hra, numStyle);
                    WriteNumCell(sb, 22, rowIdx, otAmt, numStyle);
                    WriteNumCell(sb, 23, rowIdx, gross, numStyle);
                    WriteNumCell(sb, 24, rowIdx, epf, numStyle);
                    WriteNumCell(sb, 25, rowIdx, esi, numStyle);
                    WriteNumCell(sb, 26, rowIdx, bonus, numStyle);
                    WriteNumCell(sb, 27, rowIdx, serChar, numStyle);
                    WriteNumCell(sb, 28, rowIdx, lwf, numStyle);
                    WriteNumCell(sb, 29, rowIdx, hostelDed, numStyle);
                    WriteNumCell(sb, 30, rowIdx, deductions, numStyle);
                    WriteNumCell(sb, 31, rowIdx, refB, numStyle);
                    WriteNumCell(sb, 32, rowIdx, splA, numStyle);
                    WriteNumCell(sb, 33, rowIdx, kzn, numStyle);
                    WriteNumCell(sb, 34, rowIdx, gGmc, numStyle);
                    WriteNumCell(sb, 35, rowIdx, totalProv, numStyle);
                    WriteNumCell(sb, 36, rowIdx, net, numStyle);
                }

                sb.Append("</row>");
            }

            // Total Summary Row at bottom
            if (employees.Count > 0)
            {
                rowIdx++;
                sb.Append(string.Format(CultureInfo.InvariantCulture, @"<row r=""{0}"" ht=""22"" customHeight=""1"">", rowIdx));
                for (int c = 1; c <= 18; c++)
                {
                    if (c == 3)
                        WriteCell(sb, c, rowIdx, $"TOTAL ({employees.Count} Employees)", 7);
                    else
                        WriteCell(sb, c, rowIdx, "", 7);
                }

                if (isNaps)
                {
                    WriteCell(sb, 19, rowIdx, "", 7);
                    WriteCell(sb, 20, rowIdx, "", 7);
                    WriteCell(sb, 21, rowIdx, "", 7);
                    WriteNumCell(sb, 22, rowIdx, sumGross, 8);
                    WriteCell(sb, 23, rowIdx, "", 7);
                    WriteNumCell(sb, 24, rowIdx, sumDeductions, 8);
                    WriteCell(sb, 25, rowIdx, "", 7);
                    WriteCell(sb, 26, rowIdx, "", 7);
                    WriteCell(sb, 27, rowIdx, "", 7);
                    WriteCell(sb, 28, rowIdx, "", 7);
                    WriteNumCell(sb, 29, rowIdx, sumProvisions, 8);
                    WriteNumCell(sb, 30, rowIdx, sumNetPayable, 8);
                }
                else
                {
                    WriteCell(sb, 19, rowIdx, "", 7);
                    WriteCell(sb, 20, rowIdx, "", 7);
                    WriteCell(sb, 21, rowIdx, "", 7);
                    WriteCell(sb, 22, rowIdx, "", 7);
                    WriteNumCell(sb, 23, rowIdx, sumGross, 8);
                    WriteCell(sb, 24, rowIdx, "", 7);
                    WriteCell(sb, 25, rowIdx, "", 7);
                    WriteCell(sb, 26, rowIdx, "", 7);
                    WriteCell(sb, 27, rowIdx, "", 7);
                    WriteCell(sb, 28, rowIdx, "", 7);
                    WriteCell(sb, 29, rowIdx, "", 7);
                    WriteNumCell(sb, 30, rowIdx, sumDeductions, 8);
                    WriteCell(sb, 31, rowIdx, "", 7);
                    WriteCell(sb, 32, rowIdx, "", 7);
                    WriteCell(sb, 33, rowIdx, "", 7);
                    WriteCell(sb, 34, rowIdx, "", 7);
                    WriteNumCell(sb, 35, rowIdx, sumProvisions, 8);
                    WriteNumCell(sb, 36, rowIdx, sumNetPayable, 8);
                }

                sb.Append("</row>");
            }

            sb.Append("</sheetData>");

            // Add Excel Auto-filter on Row 1
            string lastCol = GetColumnName(colCount);
            sb.Append(string.Format(CultureInfo.InvariantCulture, @"<autoFilter ref=""A1:{0}{1}""/>", lastCol, rowIdx));

            sb.Append("</worksheet>");
            return sb.ToString();
        }

        private static void WriteCell(StringBuilder sb, int col, int row, string text, int style)
        {
            string cellRef = GetCellRef(col, row);
            sb.Append(string.Format(CultureInfo.InvariantCulture,
                @"<c r=""{0}"" t=""inlineStr"" s=""{1}""><is><t>{2}</t></is></c>",
                cellRef, style, EscapeXml(text)));
        }

        private static void WriteNumCell(StringBuilder sb, int col, int row, decimal val, int style)
        {
            string cellRef = GetCellRef(col, row);
            sb.Append(string.Format(CultureInfo.InvariantCulture,
                @"<c r=""{0}"" s=""{1}""><v>{2:0.00}</v></c>",
                cellRef, style, val));
        }

        private static string GetCellRef(int col, int row)
        {
            return GetColumnName(col) + row;
        }

        private static string GetColumnName(int colIndex)
        {
            string col = "";
            while (colIndex > 0)
            {
                int rem = (colIndex - 1) % 26;
                col = (char)('A' + rem) + col;
                colIndex = (colIndex - 1) / 26;
            }
            return col;
        }

        private static string EscapeXml(string? s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("&", "&amp;")
                    .Replace("<", "&lt;")
                    .Replace(">", "&gt;")
                    .Replace("\"", "&quot;")
                    .Replace("'", "&apos;");
        }

        private static decimal GetDictOr(Dictionary<string, decimal>? dict, string key, decimal def = 0)
        {
            if (dict == null) return def;
            return dict.TryGetValue(key, out var val) ? val : def;
        }
    }
}
