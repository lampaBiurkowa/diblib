namespace DibBase.Options;

public class DsDbLibOptions
{
    public const string SECTION = "DsDbLib";

    public required string Password { get; set; }
    public required string User { get; set; }
    public required string Host { get; set; }
    public required int Port { get; set; }
    public required bool Migrate { get; set; }
    public int CommandTimeout { get; set; } = 10;

    public string GetConnectionString(string dbName, string schema) =>
        $"User ID={User};Password={Password};Host={Host};Port={Port};Database={dbName};SearchPath={schema};CommandTimeout={CommandTimeout}";
}
