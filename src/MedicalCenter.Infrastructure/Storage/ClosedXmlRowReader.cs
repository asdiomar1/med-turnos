using ClosedXML.Excel;
using MedicalCenter.Application.Features.Imports;

namespace MedicalCenter.Infrastructure.Storage;

public sealed class ClosedXmlRowReader : IXlsxRowReader
{
    public IEnumerable<IReadOnlyDictionary<string, string?>> Read(Stream xlsxStream)
    {
        using var workbook = new XLWorkbook(xlsxStream);
        var sheet = workbook.Worksheets.First();

        var rows = sheet.RowsUsed().ToList();
        if (rows.Count == 0)
        {
            yield break;
        }

        // First used row is the header row
        var headerRow = rows[0];
        var headers = headerRow.Cells()
            .Select(c => c.GetString().Trim())
            .ToArray();

        for (var i = 1; i < rows.Count; i++)
        {
            var dataRow = rows[i];
            var cells = dataRow.Cells(1, headers.Length).ToArray();

            var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (var j = 0; j < headers.Length; j++)
            {
                var header = headers[j];
                if (string.IsNullOrWhiteSpace(header))
                {
                    continue;
                }

                var value = j < cells.Length ? GetCellText(cells[j]) : null;
                dict[header] = value;
            }

            yield return dict;
        }
    }

    private static string? GetCellText(IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return null;
        }

        // Preserve original string for data cells; avoid formula evaluation side-effects
        return cell.GetString().Trim() is { Length: > 0 } s ? s : null;
    }
}
