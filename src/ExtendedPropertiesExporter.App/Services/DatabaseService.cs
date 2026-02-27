using ExtendedPropertiesExporter.App.Models;
using ExtendedPropertiesExporter.App.Settings;
using Microsoft.Data.SqlClient;

namespace ExtendedPropertiesExporter.App.Services;

public sealed class DatabaseService : IDisposable
{
    private readonly SqlConnection _connection;
    private readonly AppSettings _settings;

    public DatabaseService(string connectionString, AppSettings settings)
    {
        _settings = settings;
        _connection = new SqlConnection(connectionString);
        _connection.Open();
    }

    public List<TableInfo> GetTablesWithExtendedProperties()
    {
        var allProperties = GetAllExtendedProperties();

        return allProperties
            .GroupBy(p => new { p.SchemaName, p.ObjectName },
                     (key, props) => new TableInfo
                     {
                         SchemaName = key.SchemaName,
                         Name = key.ObjectName,
                         ExtendedProperties = props.ToList()
                     })
            .ToList();
    }

    private List<ExtendedProperty> GetAllExtendedProperties()
    {
        var properties = new List<ExtendedProperty>();

        using var command = new SqlCommand(_settings.GetAllExtendedPropertiesQuery, _connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            properties.Add(new ExtendedProperty
            {
                SchemaName = reader.GetString(0),
                ObjectName = reader.GetString(1),
                ColumnName = reader.IsDBNull(2) ? null : reader.GetString(2),
                Name = reader.GetString(3),
                Value = reader.GetString(4),
            });
        }

        return properties;
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
