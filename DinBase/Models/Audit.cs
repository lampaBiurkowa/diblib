using DibBase.ModelBase;

namespace DibBase.Models;

public class Audit : Entity
{
    public required string ChangedType { get; set; }
    public required long ChangedId { get; set; }
    public required string ChangedField { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
}
