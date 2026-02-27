namespace ExtendedPropertiesExporter.App.Settings;

public sealed class AppSettings
{
    public required string ConnectionStringTemplate { get; init; }
    public required string ColumnPropertyTemplate { get; init; }
    public required string TablePropertyTemplate { get; init; }
    public required string GetAllExtendedPropertiesQuery { get; init; }

    public static AppSettings Load(string basePath)
    {
        var settingsJson = File.ReadAllText(Path.Combine(basePath, "appsettings.json"));
        var settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(settingsJson)
            ?? throw new InvalidOperationException("Falha ao desserializar appsettings.json.");

        var templateJson = File.ReadAllText(Path.Combine(basePath, "Templates", "template.json"));
        var template = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(templateJson)
            ?? throw new InvalidOperationException("Falha ao desserializar template.json.");

        return new AppSettings
        {
            ConnectionStringTemplate = settings["ConnectionString"],
            ColumnPropertyTemplate = template["columnQuery"],
            TablePropertyTemplate = template["tableQuery"],
            GetAllExtendedPropertiesQuery = File.ReadAllText(Path.Combine(basePath, "Queries", "GetAllExtendedProperties.sql")),
        };
    }
}
