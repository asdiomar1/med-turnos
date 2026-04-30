using MedicalCenter.Domain.Common;

namespace MedicalCenter.Domain.Entities;

public sealed class Block : Entity<Guid>
{
    private readonly List<Guid> _appointmentIds = [];

    private Block() { }

    public Block(Guid id, string code)
    {
        Id = id;
        Code = code;
    }

    public string Code { get; private set; } = string.Empty;
    public IReadOnlyCollection<Guid> AppointmentIds => _appointmentIds.AsReadOnly();
}
