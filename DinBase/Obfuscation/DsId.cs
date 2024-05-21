public class DsId
{
    public Guid Guid { get; set; }
}

[System.AttributeUsage(System.AttributeTargets.Property)]
public class DsIdAttribute(string navigationProperty) : System.Attribute
{
    public string NaviagationProperty { get; set; } = navigationProperty;
}