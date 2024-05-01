namespace DibBase.ModelBase;

public interface IAudited
{
    List<string> GetFieldsToAudit();
}
