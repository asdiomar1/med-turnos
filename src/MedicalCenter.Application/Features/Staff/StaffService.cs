using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Exceptions;

namespace MedicalCenter.Application.Features.Staff;

public sealed class StaffService(IRbacAdminRepository rbacAdminRepository) : IStaffService
{
    public async Task<StaffProfileSummary> UpdateMyDataAsync(Guid userId, string nombre, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ValidationException("nombre es obligatorio");
        }

        return await rbacAdminRepository.UpdateMyDataAsync(userId, nombre, cancellationToken);
    }
}
