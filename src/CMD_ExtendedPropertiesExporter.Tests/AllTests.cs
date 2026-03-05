using CMD_ExtendedPropertiesExporter.App.Models;
using CMD_ExtendedPropertiesExporter.App.Services;
using CMD_ExtendedPropertiesExporter.App.Settings;
using Xunit;

namespace CMD_ExtendedPropertiesExporter.Tests;

// ════════════════════════════════════════════════════════════════════════════
// ExtendedProperty
// ════════════════════════════════════════════════════════════════════════════

public sealed class ExtendedPropertyTests
{
    [Fact]
    public void IsTableLevel_ReturnsTrue_WhenColumnNameIsNull()
    {
        var property = new ExtendedProperty
        {
            SchemaName = "dbo",
            ObjectName = "Orders",
            ColumnName = null,
            Name = "MS_Description",
            Value = "Order table"
        };

        Assert.True(property.IsTableLevel);
    }

    [Fact]
    public void IsTableLevel_ReturnsFalse_WhenColumnNameIsSet()
    {
        var property = new ExtendedProperty
        {
            SchemaName = "dbo",
            ObjectName = "Orders",
            ColumnName = "Id",
            Name = "MS_Description",
            Value = "Primary key"
        };

        Assert.False(property.IsTableLevel);
    }

    [Fact]
    public void IsTableLevel_ReturnsFalse_WhenColumnNameIsEmptyString()
    {
        var property = new ExtendedProperty
        {
            SchemaName = "dbo",
            ObjectName = "Orders",
            ColumnName = string.Empty,
            Name = "MS_Description",
            Value = "Some col"
        };

        Assert.False(property.IsTableLevel);
    }

    [Fact]
    public void Properties_AreAssignedCorrectly()
    {
        var property = new ExtendedProperty
        {
            SchemaName = "sales",
            ObjectName = "Invoices",
            ColumnName = "Amount",
            Name = "Caption",
            Value = "Invoice Amount"
        };

        Assert.Equal("sales", property.SchemaName);
        Assert.Equal("Invoices", property.ObjectName);
        Assert.Equal("Amount", property.ColumnName);
        Assert.Equal("Caption", property.Name);
        Assert.Equal("Invoice Amount", property.Value);
    }
}

// ════════════════════════════════════════════════════════════════════════════
// TableInfo
// ════════════════════════════════════════════════════════════════════════════

public sealed class TableInfoTests
{
    [Fact]
    public void ExtendedProperties_DefaultsToEmptyList()
    {
        var table = new TableInfo { SchemaName = "dbo", Name = "MyTable" };

        Assert.NotNull(table.ExtendedProperties);
        Assert.Empty(table.ExtendedProperties);
    }

    [Fact]
    public void Properties_AreAssignedCorrectly()
    {
        var props = new List<ExtendedProperty>
        {
            new() { SchemaName = "dbo", ObjectName = "MyTable", Name = "Desc", Value = "Test" }
        };

        var table = new TableInfo
        {
            SchemaName = "dbo",
            Name = "MyTable",
            ExtendedProperties = props
        };

        Assert.Equal("dbo", table.SchemaName);
        Assert.Equal("MyTable", table.Name);
        Assert.Single(table.ExtendedProperties);
    }

    [Fact]
    public void ExtendedProperties_CanBeModified()
    {
        var table = new TableInfo { SchemaName = "dbo", Name = "MyTable" };

        table.ExtendedProperties.Add(new ExtendedProperty
        {
            SchemaName = "dbo",
            ObjectName = "MyTable",
            Name = "MS_Description",
            Value = "A table"
        });

        Assert.Single(table.ExtendedProperties);
    }
}

// ════════════════════════════════════════════════════════════════════════════
// AppSettings
// ════════════════════════════════════════════════════════════════════════════

public sealed class AppSettingsTests : IDisposable
{
    private readonly string _basePath;

    public AppSettingsTests()
    {
        _basePath = Path.Combine(Path.GetTempPath(), "AppSettingsTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_basePath);
        Directory.CreateDirectory(Path.Combine(_basePath, "Templates"));
        Directory.CreateDirectory(Path.Combine(_basePath, "Queries"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_basePath))
            Directory.Delete(_basePath, recursive: true);
    }

    private void WriteSettingsFile(string connectionString = "Server={{ServerName}};Database={{DatabaseName}};")
    {
        var json = $$"""
            {
              "ConnectionString": "{{connectionString}}"
            }
            """;
        File.WriteAllText(Path.Combine(_basePath, "appsettings.json"), json);
    }

    private void WriteTemplateFile(string columnQuery = "EXEC col_proc", string tableQuery = "EXEC tbl_proc")
    {
        var json = $$"""
            {
              "columnQuery": "{{columnQuery}}",
              "tableQuery": "{{tableQuery}}"
            }
            """;
        File.WriteAllText(Path.Combine(_basePath, "Templates", "template.json"), json);
    }

    private void WriteQueryFile(string sql = "SELECT 1")
        => File.WriteAllText(Path.Combine(_basePath, "Queries", "GetAllExtendedProperties.sql"), sql);

    [Fact]
    public void Load_ReturnsCorrectConnectionStringTemplate()
    {
        WriteSettingsFile("Server={{ServerName}};Database={{DatabaseName}};Trusted_Connection=True;");
        WriteTemplateFile();
        WriteQueryFile();

        var settings = AppSettings.Load(_basePath);

        Assert.Equal("Server={{ServerName}};Database={{DatabaseName}};Trusted_Connection=True;",
            settings.ConnectionStringTemplate);
    }

    [Fact]
    public void Load_ReturnsCorrectTemplates()
    {
        WriteSettingsFile();
        WriteTemplateFile("EXEC col_proc {{Property_Name}}", "EXEC tbl_proc {{Property_Name}}");
        WriteQueryFile();

        var settings = AppSettings.Load(_basePath);

        Assert.Equal("EXEC col_proc {{Property_Name}}", settings.ColumnPropertyTemplate);
        Assert.Equal("EXEC tbl_proc {{Property_Name}}", settings.TablePropertyTemplate);
    }

    [Fact]
    public void Load_ReturnsCorrectQuery()
    {
        WriteSettingsFile();
        WriteTemplateFile();
        WriteQueryFile("SELECT schema_name, object_name FROM sys.extended_properties");

        var settings = AppSettings.Load(_basePath);

        Assert.Equal("SELECT schema_name, object_name FROM sys.extended_properties",
            settings.GetAllExtendedPropertiesQuery);
    }

    [Fact]
    public void Load_ThrowsInvalidOperationException_WhenSettingsJsonIsEmpty()
    {
        File.WriteAllText(Path.Combine(_basePath, "appsettings.json"), "null");
        WriteTemplateFile();
        WriteQueryFile();

        Assert.Throws<InvalidOperationException>(() => AppSettings.Load(_basePath));
    }

    [Fact]
    public void Load_ThrowsInvalidOperationException_WhenTemplateJsonIsEmpty()
    {
        WriteSettingsFile();
        File.WriteAllText(Path.Combine(_basePath, "Templates", "template.json"), "null");
        WriteQueryFile();

        Assert.Throws<InvalidOperationException>(() => AppSettings.Load(_basePath));
    }

    [Fact]
    public void Load_ThrowsException_WhenAppsettingsFileMissing()
    {
        WriteTemplateFile();
        WriteQueryFile();

        Assert.Throws<FileNotFoundException>(() => AppSettings.Load(_basePath));
    }
}

// ════════════════════════════════════════════════════════════════════════════
// SqlFileUpdater
// ════════════════════════════════════════════════════════════════════════════

public sealed class SqlFileUpdaterTests : IDisposable
{
    private readonly string _rootPath;

    private const string ColumnTemplate =
        "EXEC sp_addextendedproperty @name = N'{{Property_Name}}', @value = N'{{Property_Value}}', " +
        "@level0type = N'Schema', @level0name = N'{{Schema_Name}}', " +
        "@level1type = N'Table', @level1name = N'{{Table_Name}}', " +
        "@level2type = N'Column', @level2name = N'{{Column_Name}}'";

    private const string TableTemplate =
        "EXEC sp_addextendedproperty @name = N'{{Property_Name}}', @value = N'{{Property_Value}}', " +
        "@level0type = N'Schema', @level0name = N'{{Schema_Name}}', " +
        "@level1type = N'Table', @level1name = N'{{Table_Name}}'";

    public SqlFileUpdaterTests()
    {
        _rootPath = Path.Combine(Path.GetTempPath(), "SqlFileUpdaterTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_rootPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
            Directory.Delete(_rootPath, recursive: true);
    }

    private string CreateSqlFile(string tableName, string content)
    {
        var filePath = Path.Combine(_rootPath, $"{tableName}.sql");
        File.WriteAllText(filePath, content);
        return filePath;
    }

    private SqlFileUpdater CreateUpdater() => new(_rootPath, ColumnTemplate, TableTemplate);

    // ── IndexedFileCount ─────────────────────────────────────────────────────

    [Fact]
    public void IndexedFileCount_ReturnsZero_WhenNoSqlFiles()
    {
        Assert.Equal(0, CreateUpdater().IndexedFileCount);
    }

    [Fact]
    public void IndexedFileCount_ReturnsCorrectCount()
    {
        CreateSqlFile("TableA", "-- a");
        CreateSqlFile("TableB", "-- b");
        CreateSqlFile("TableC", "-- c");

        Assert.Equal(3, CreateUpdater().IndexedFileCount);
    }

    // ── NotFound ─────────────────────────────────────────────────────────────

    [Fact]
    public void UpdateTable_ReturnsNotFound_WhenTableNotInIndex()
    {
        CreateSqlFile("OtherTable", "CREATE TABLE OtherTable (Id INT)");

        var results = CreateUpdater().UpdateTable(new TableInfo { SchemaName = "dbo", Name = "MissingTable" });

        Assert.Single(results);
        Assert.Equal(UpdateResult.NotFound, results[0].Result);
    }

    // ── NoTableDefinition ────────────────────────────────────────────────────

    [Fact]
    public void UpdateTable_ReturnsNoTableDefinition_WhenFileHasNoCreateTable()
    {
        CreateSqlFile("MyTable", "-- just a comment, no DDL here");

        var results = CreateUpdater().UpdateTable(new TableInfo { SchemaName = "dbo", Name = "MyTable" });

        Assert.Single(results);
        Assert.Equal(UpdateResult.NoTableDefinition, results[0].Result);
    }

    // ── Updated ──────────────────────────────────────────────────────────────

    [Fact]
    public void UpdateTable_ReturnsUpdated_WhenFileContainsCreateTable()
    {
        CreateSqlFile("MyTable", "CREATE TABLE MyTable (Id INT)");

        var results = CreateUpdater().UpdateTable(new TableInfo { SchemaName = "dbo", Name = "MyTable" });

        Assert.Single(results);
        Assert.Equal(UpdateResult.Updated, results[0].Result);
    }

    [Fact]
    public void UpdateTable_AppendsColumnProperty_WhenColumnLevelPropertyProvided()
    {
        var filePath = CreateSqlFile("Orders", "CREATE TABLE Orders (Id INT)");

        var table = new TableInfo
        {
            SchemaName = "dbo",
            Name = "Orders",
            ExtendedProperties =
            [
                new ExtendedProperty
                {
                    SchemaName = "dbo", ObjectName = "Orders", ColumnName = "Id",
                    Name = "MS_Description", Value = "Primary key"
                }
            ]
        };

        CreateUpdater().UpdateTable(table);

        var written = File.ReadAllText(filePath);
        Assert.Contains("sp_addextendedproperty", written, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("MS_Description", written);
        Assert.Contains("Primary key", written);
        Assert.Contains("@level2type", written);
    }

    [Fact]
    public void UpdateTable_AppendsTableProperty_WhenTableLevelPropertyProvided()
    {
        var filePath = CreateSqlFile("Customers", "CREATE TABLE Customers (Id INT)");

        var table = new TableInfo
        {
            SchemaName = "dbo",
            Name = "Customers",
            ExtendedProperties =
            [
                new ExtendedProperty
                {
                    SchemaName = "dbo", ObjectName = "Customers", ColumnName = null,
                    Name = "MS_Description", Value = "Customer table"
                }
            ]
        };

        CreateUpdater().UpdateTable(table);

        var written = File.ReadAllText(filePath);
        Assert.Contains("sp_addextendedproperty", written, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("MS_Description", written);
        Assert.DoesNotContain("@level2type", written);
    }

    [Fact]
    public void UpdateTable_RemovesExistingExtendedPropertyBatches_BeforeAdding()
    {
        const string originalContent =
            "CREATE TABLE Products (Id INT)\r\n" +
            "GO\r\n" +
            "EXEC sp_addextendedproperty @name = N'OldProp', @value = N'old'\r\n";

        var filePath = CreateSqlFile("Products", originalContent);

        var table = new TableInfo
        {
            SchemaName = "dbo",
            Name = "Products",
            ExtendedProperties =
            [
                new ExtendedProperty { SchemaName = "dbo", ObjectName = "Products", Name = "NewProp", Value = "new value" }
            ]
        };

        CreateUpdater().UpdateTable(table);

        var written = File.ReadAllText(filePath);
        Assert.DoesNotContain("OldProp", written);
        Assert.Contains("NewProp", written);
    }

    [Fact]
    public void UpdateTable_ClearsExtendedPropertyBatches_WhenNoPropertiesProvided()
    {
        const string originalContent =
            "CREATE TABLE Invoices (Id INT)\r\n" +
            "GO\r\n" +
            "EXEC sp_addextendedproperty @name = N'RemoveMe', @value = N'gone'\r\n";

        var filePath = CreateSqlFile("Invoices", originalContent);

        CreateUpdater().UpdateTable(new TableInfo { SchemaName = "dbo", Name = "Invoices", ExtendedProperties = [] });

        var written = File.ReadAllText(filePath);
        Assert.DoesNotContain("sp_addextendedproperty", written, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UpdateTable_IsCaseInsensitive_ForTableName()
    {
        CreateSqlFile("mytable", "CREATE TABLE mytable (Id INT)");

        var results = CreateUpdater().UpdateTable(new TableInfo { SchemaName = "dbo", Name = "MYTABLE" });

        Assert.Single(results);
        Assert.Equal(UpdateResult.Updated, results[0].Result);
    }

    [Fact]
    public void UpdateTable_ReturnsMultipleResults_WhenMultipleFilesMatchTableName()
    {
        var subDir = Path.Combine(_rootPath, "sub");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(_rootPath, "Shared.sql"), "CREATE TABLE Shared (Id INT)");
        File.WriteAllText(Path.Combine(subDir, "Shared.sql"), "CREATE TABLE Shared (Id INT)");

        var results = CreateUpdater().UpdateTable(new TableInfo { SchemaName = "dbo", Name = "Shared" });

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(UpdateResult.Updated, r.Result));
    }

    [Fact]
    public void UpdateTable_EscapesSingleQuotes_InPropertyValues()
    {
        var filePath = CreateSqlFile("EscapeTest", "CREATE TABLE EscapeTest (Id INT)");

        var table = new TableInfo
        {
            SchemaName = "dbo",
            Name = "EscapeTest",
            ExtendedProperties =
            [
                new ExtendedProperty { SchemaName = "dbo", ObjectName = "EscapeTest", Name = "Desc", Value = "It's a test" }
            ]
        };

        CreateUpdater().UpdateTable(table);

        Assert.Contains("It''s a test", File.ReadAllText(filePath));
    }

    [Fact]
    public void UpdateTable_CreateTableRegex_MatchesCaseInsensitive()
    {
        CreateSqlFile("LowerCreate", "create table LowerCreate (Id INT)");

        var results = CreateUpdater().UpdateTable(new TableInfo { SchemaName = "dbo", Name = "LowerCreate" });

        Assert.Single(results);
        Assert.Equal(UpdateResult.Updated, results[0].Result);
    }
}
