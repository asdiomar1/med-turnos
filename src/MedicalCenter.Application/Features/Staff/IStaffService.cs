using MedicalCenter.Application.DTOs;

namespace MedicalCenter.Application.Features.Staff;

public interface IStaffService
{
    Task<StaffProfileSummary> UpdateMyDataAsync(Guid userId, string nombre, CancellationToken cancellationToken);
}
