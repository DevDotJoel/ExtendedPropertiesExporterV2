namespace ExtendedPropertiesExporter.App.Models;

public sealed class TableInfo
{
    public required string SchemaName { get; set; }
    public required string Name { get; set; }
    public List<ExtendedProperty> ExtendedProperties { get; set; } = [];
}
