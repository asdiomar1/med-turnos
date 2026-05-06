using System.Globalization;
using System.Text.Json;
using MedicalCenter.Api.Extensions;

namespace MedicalCenter.UnitTests.Api.Extensions;

public sealed class TimeOnlyJsonConverterTests
{
    [Fact]
    public void Read_Parses_HH_mm_WithInvariantCulture()
    {
        var converter = new TimeOnlyJsonConverter();
        var reader = new Utf8JsonReader("\"09:30\""u8.ToArray());
        reader.Read();

        var result = converter.Read(ref reader, typeof(TimeOnly), new JsonSerializerOptions());

        Assert.Equal(new TimeOnly(9, 30), result);
    }

    [Fact]
    public void Read_IsIndependentFromCurrentCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("es-AR");
            CultureInfo.CurrentUICulture = new CultureInfo("es-AR");
            var converter = new TimeOnlyJsonConverter();
            var reader = new Utf8JsonReader("\"09:30:15\""u8.ToArray());
            reader.Read();

            var result = converter.Read(ref reader, typeof(TimeOnly), new JsonSerializerOptions());

            Assert.Equal(new TimeOnly(9, 30, 15), result);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }
}
