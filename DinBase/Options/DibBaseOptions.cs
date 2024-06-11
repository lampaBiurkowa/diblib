namespace DibBase.Options;

public class DsDbLibOptions
{
    public const string SECTION = "DsDbLib";

    public required string ObfuscationKey { get; set; }
    public required string DatabaseName { get; set; }
    public required string Password { get; set; }
    public required string User { get; set; }
    public required string Host { get; set; }
}
