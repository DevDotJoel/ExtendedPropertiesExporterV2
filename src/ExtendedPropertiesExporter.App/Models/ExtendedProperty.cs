namespace ExtendedPropertiesExporter.App.Models;

public sealed class ExtendedProperty
{
    public required string ObjectName { get; set; }
    public required string ColumnName { get; set; }
    public required string Name { get; set; }
    public required string Value { get; set; }
}
