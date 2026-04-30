namespace MedicalCenter.Domain.Constants;

public static class MagicBytes
{
    /// <summary>
    /// XLSX (Office Open XML Spreadsheet) magic bytes: PK (0x50, 0x4B) followed by [03][04]
    /// </summary>
    public static readonly byte[] Xlsx = [0x50, 0x4B, 0x03, 0x04];

    /// <summary>
    /// XLS (Excel 97-2003) magic bytes
    /// </summary>
    public static readonly byte[] Xls = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

    /// <summary>
    /// PNG image magic bytes
    /// </summary>
    public static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>
    /// JPEG image magic bytes
    /// </summary>
    public static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF];

    /// <summary>
    /// PDF document magic bytes
    /// </summary>
    public static readonly byte[] Pdf = [0x25, 0x50, 0x44, 0x46];

    public static bool IsMatch(byte[] fileBytes, byte[] magicBytes)
    {
        if (fileBytes.Length < magicBytes.Length)
            return false;

        for (int i = 0; i < magicBytes.Length; i++)
        {
            if (fileBytes[i] != magicBytes[i])
                return false;
        }

        return true;
    }

    public static bool IsXlsx(byte[] fileBytes) => IsMatch(fileBytes, Xlsx);
    public static bool IsXls(byte[] fileBytes) => IsMatch(fileBytes, Xls);
    public static bool IsPng(byte[] fileBytes) => IsMatch(fileBytes, Png);
    public static bool IsJpeg(byte[] fileBytes) => IsMatch(fileBytes, Jpeg);
    public static bool IsPdf(byte[] fileBytes) => IsMatch(fileBytes, Pdf);
}