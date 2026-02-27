using ExtendedPropertiesExporter.App.Models;
using ExtendedPropertiesExporter.App.Services;
using ExtendedPropertiesExporter.App.Settings;

namespace ExtendedPropertiesExporter.App;

public static class Program
{
    public static void Main()
    {
        Console.WriteLine("=== Extended Properties Exporter ===");

        var appSettings = AppSettings.Load(AppContext.BaseDirectory);

        while (true)
        {
            var (serverName, databaseName, rootPath) = ReadUserInput();

            if (!Directory.Exists(rootPath))
            {
                Console.WriteLine($"Error: Path not found: {rootPath}");
                Console.WriteLine("Please try again.\n");
                continue;
            }

            var connectionString = appSettings.ConnectionStringTemplate
                .Replace("{{ServerName}}", serverName)
                .Replace("{{DatabaseName}}", databaseName);

            try
            {
                using var dbService = new DatabaseService(connectionString, appSettings);
                Console.WriteLine($"Connected to {databaseName} on {serverName}.");

                var tables = dbService.GetTablesWithExtendedProperties();
                Console.WriteLine($"Found {tables.Count} tables.");

                var fileUpdater = new SqlFileUpdater(
                    rootPath,
                    appSettings.ColumnPropertyTemplate,
                    appSettings.TablePropertyTemplate);
                Console.WriteLine($"Indexed {fileUpdater.IndexedFileCount} .sql files.");

                Console.WriteLine("Updating project files...");
                foreach (var table in tables)
                {
                    var results = fileUpdater.UpdateTable(table);
                    LogUpdateResults(table, results);
                }

                Console.WriteLine("Done.");
                break;
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                Console.WriteLine($"Could not connect: {ex.Message}");
                Console.WriteLine("Please try again.\n");
            }
        }
    }

    private static void LogUpdateResults(TableInfo table, List<(string FilePath, UpdateResult Result)> results)
    {
        foreach (var (filePath, result) in results)
        {
            switch (result)
            {
                case UpdateResult.Updated:
                    Console.WriteLine($"  Updated {filePath} ({table.ExtendedProperties.Count} properties)");
                    break;
                case UpdateResult.NotFound:
                    Console.WriteLine($"  Skipped {table.Name} — no matching .sql file.");
                    break;
                case UpdateResult.NoTableDefinition:
                    Console.WriteLine($"  Skipped {filePath} — not a CREATE TABLE file.");
                    break;
            }
        }
    }

    private static (string ServerName, string DatabaseName, string RootPath) ReadUserInput()
    {
        while (true)
        {
            Console.Write("Server Name: ");
            var serverName = Console.ReadLine()?.Trim();

            Console.Write("Database Name: ");
            var databaseName = Console.ReadLine()?.Trim();

            Console.Write("Root Path: ");
            var rootPath = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(serverName) || string.IsNullOrEmpty(databaseName))
            {
                Console.WriteLine("Server name and database name are required.\n");
                continue;
            }

            if (string.IsNullOrEmpty(rootPath))
            {
                Console.WriteLine("Root path is required.\n");
                continue;
            }

            return (serverName, databaseName, rootPath);
        }
    }
}
