namespace DibBase.ModelBase;

public interface ITimeStamped
{
    DateTimeOffset CreatedAt { get; set; }
    DateTimeOffset UpdatedAt { get; set; }
}
