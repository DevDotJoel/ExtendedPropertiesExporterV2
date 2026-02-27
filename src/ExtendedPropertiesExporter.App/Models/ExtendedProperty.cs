namespace ExtendedPropertiesExporter.App.Models;

public sealed class ExtendedProperty
{
    public required string SchemaName { get; set; }
    public required string ObjectName { get; set; }
    public string? ColumnName { get; set; }
    public required string Name { get; set; }
    public required string Value { get; set; }

    public bool IsTableLevel => ColumnName is null;
}
