namespace MedicalCenter.Application.Features.Imports;

public interface IXlsxRowReader
{
    /// <summary>
    /// Reads an xlsx stream and yields one dictionary per data row.
    /// Keys are normalized header names; values are raw cell text or null.
    /// The first sheet is used. The first non-empty row is treated as headers.
    /// </summary>
    IEnumerable<IReadOnlyDictionary<string, string?>> Read(Stream xlsxStream);
}
