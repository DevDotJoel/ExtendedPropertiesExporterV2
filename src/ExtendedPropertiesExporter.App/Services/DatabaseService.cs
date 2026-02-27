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
            .GroupBy(p => p.ObjectName, StringComparer.OrdinalIgnoreCase)
            .Select(g => new TableInfo
            {
                Name = g.Key,
                ExtendedProperties = g.ToList()
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
                ObjectName = reader.GetString(0),
                ColumnName = reader.GetString(1),
                Name = reader.GetString(2),
                Value = reader.GetString(3),
            });
        }

        return properties;
    }

    private List<TableInfo> GetTableNames()
    {
        var tables = new List<TableInfo>();

        using var command = new SqlCommand(_settings.GetTablesQuery, _connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            tables.Add(new TableInfo { Name = reader.GetString(0) });
        }

        return tables;
    }

    private List<string> GetColumnNames(string tableName)
    {
        var columns = new List<string>();

        var query = _settings.GetColumnsQuery.Replace("{{TableName}}", tableName);

        using var command = new SqlCommand(query, _connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            columns.Add(reader.GetString(0));
        }

        return columns;
    }

    private List<ExtendedProperty> GetExtendedProperties(string tableName, string columnName)
    {
        var properties = new List<ExtendedProperty>();

        var query = _settings.GetExtendedPropertiesQuery
            .Replace("{{TableName}}", tableName)
            .Replace("{{ColumnName}}", columnName);

        using var command = new SqlCommand(query, _connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            properties.Add(new ExtendedProperty
            {
                ObjectName = reader.GetString(0),
                ColumnName = reader.GetString(1),
                Name = reader.GetString(2),
                Value = reader.GetString(3),
            });
        }

        return properties;
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
