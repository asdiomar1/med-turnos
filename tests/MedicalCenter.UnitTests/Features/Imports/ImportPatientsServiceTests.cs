using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.Features.Imports;
using MedicalCenter.Domain.Entities;
using NSubstitute;

namespace MedicalCenter.UnitTests.Features.Imports;

public sealed class ImportPatientsServiceTests
{
    private readonly IPatientRepository _patientRepository = Substitute.For<IPatientRepository>();
    private readonly ICondicionIvaRepository _condicionIvaRepository = Substitute.For<ICondicionIvaRepository>();
    private readonly IObraSocialRepository _obraSocialRepository = Substitute.For<IObraSocialRepository>();
    private readonly IClinicalHistoryRepository _clinicalHistoryRepository = Substitute.For<IClinicalHistoryRepository>();
    private readonly IXlsxRowReader _xlsxRowReader = Substitute.For<IXlsxRowReader>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task ImportAsync_WhenPortalEnabledAndLoginIdentifierIsMissing_UsesNormalizedDocumentoAsLoginIdentifier()
    {
        Patient? capturedPatient = null;
        _condicionIvaRepository
            .GetAllAsync(includeInactive: true, Arg.Any<CancellationToken>())
            .Returns([new CondicionIva(1, "Responsable Inscripto")]);
        _obraSocialRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);
        _patientRepository.GetByDocumentoAsync("12.345.678", Arg.Any<CancellationToken>()).Returns((Patient?)null);
        _xlsxRowReader.Read(Arg.Any<Stream>()).Returns(
        [
            new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["nombre"] = "Paciente Demo",
                ["telefono"] = "123456789",
                ["documento_identidad"] = "12.345.678",
                ["condicion_iva_id"] = "1",
                ["portal_habilitado"] = "true"
            }
        ]);
        _patientRepository
            .When(repository => repository.AddAsync(Arg.Any<Patient>(), Arg.Any<CancellationToken>()))
            .Do(call => capturedPatient = call.Arg<Patient>());

        var sut = CreateService();

        var result = await sut.ImportAsync(new MemoryStream(), CancellationToken.None);

        Assert.Equal(1, result.CreatedRows);
        Assert.Equal(0, result.UpdatedRows);
        Assert.Equal(0, result.SkippedRows);
        Assert.Equal("12345678", capturedPatient?.LoginIdentifier);
    }

    private ImportPatientsService CreateService()
    {
        return new ImportPatientsService(
            _patientRepository,
            _condicionIvaRepository,
            _obraSocialRepository,
            _clinicalHistoryRepository,
            _xlsxRowReader,
            _unitOfWork);
    }
}
