namespace DibBase.Obfuscation;

[AttributeUsage(AttributeTargets.Property)]
public class DsGuidAttribute(string navigationProperty) : Attribute
{
    public string NaviagationProperty { get; set; } = navigationProperty;
}

[AttributeUsage(AttributeTargets.Property)]
public class DsLongAttribute() : Attribute
{
}
