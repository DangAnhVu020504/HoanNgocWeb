using ClosedXML.Excel;

namespace TechPos.Reporting;

/// <summary>
/// Xuất bảng dữ liệu ra .xlsx (ClosedXML) — Phase 6 (ROADMAP Phase 6).
/// Header bold + freeze hàng 1 + cột tiền/số định dạng #,##0. Không phụ thuộc DB.
/// </summary>
public static class ExcelExporter
{
    /// <summary>Trích giá trị 1 cột từ 1 dòng dữ liệu.</summary>
    public sealed record Column(string Title, Func<object, object?> Get);

    public static byte[] Build(
        string sheetName,
        IReadOnlyList<Column> columns,
        IReadOnlyList<object> rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(string.IsNullOrWhiteSpace(sheetName) ? "Report" : sheetName);

        for (var c = 0; c < columns.Count; c++)
        {
            var cell = ws.Cell(1, c + 1);
            cell.Value = columns[c].Title;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#f0f0f0");
        }
        ws.Row(1).Height = 20;

        for (var r = 0; r < rows.Count; r++)
        {
            for (var c = 0; c < columns.Count; c++)
            {
                var raw = columns[c].Get(rows[r]);
                var cell = ws.Cell(r + 2, c + 1);
                switch (raw)
                {
                    case decimal d:
                        cell.Value = d;
                        cell.Style.NumberFormat.Format = "#,##0.####";
                        break;
                    case int i:
                        cell.Value = i;
                        break;
                    case long l:
                        cell.Value = l;
                        break;
                    case DateTime dt:
                        cell.Value = dt;
                        cell.Style.DateFormat.Format = "dd/MM/yyyy";
                        break;
                    case bool b:
                        cell.Value = b ? "X" : "";
                        break;
                    default:
                        cell.Value = raw?.ToString() ?? "";
                        break;
                }
            }
        }

        ws.Columns().AdjustToContents(1, Math.Min(rows.Count + 1, 200));
        ws.SheetView.FreezeRows(1);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
