namespace ExtendedPropertiesExporter.App.Settings;

public sealed class AppSettings
{
    public required string ConnectionStringTemplate { get; init; }
    public required string ExtendedPropertyTemplate { get; init; }
    public required string GetTablesQuery { get; init; }
    public required string GetColumnsQuery { get; init; }
    public required string GetExtendedPropertiesQuery { get; init; }

    public static AppSettings Load(string basePath)
    {
        var settingsJson = File.ReadAllText(Path.Combine(basePath, "appsettings.json"));
        var settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(settingsJson)
            ?? throw new InvalidOperationException("Failed to deserialize appsettings.json.");

        var templateJson = File.ReadAllText(Path.Combine(basePath, "Templates", "template.json"));
        var template = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(templateJson)
            ?? throw new InvalidOperationException("Failed to deserialize template.json.");

        return new AppSettings
        {
            ConnectionStringTemplate = settings["ConnectionString"],
            ExtendedPropertyTemplate = template["query"],
            GetTablesQuery = File.ReadAllText(Path.Combine(basePath, "Queries", "GetTables.sql")),
            GetColumnsQuery = File.ReadAllText(Path.Combine(basePath, "Queries", "GetColumns.sql")),
            GetExtendedPropertiesQuery = File.ReadAllText(Path.Combine(basePath, "Queries", "GetExtendedProperties.sql")),
        };
    }
}
