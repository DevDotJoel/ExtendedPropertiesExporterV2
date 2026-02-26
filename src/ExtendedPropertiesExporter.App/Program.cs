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
            var (serverName, databaseName, projectTablesPath) = ReadUserInput();

            if (!Directory.Exists(projectTablesPath))
            {
                Console.WriteLine($"Error: Path not found: {projectTablesPath}");
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

                var fileUpdater = new SqlFileUpdater(projectTablesPath, appSettings.ExtendedPropertyTemplate);

                Console.WriteLine("Updating project files...");
                foreach (var table in tables)
                {
                    try
                    {
                        fileUpdater.UpdateTable(table);
                        Console.WriteLine($"  Updated {table.Name}.sql ({table.ExtendedProperties.Count} properties)");
                    }
                    catch (FileNotFoundException)
                    {
                        Console.WriteLine($"  Skipped {table.Name}.sql — file not found.");
                    }
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

    private static (string ServerName, string DatabaseName, string ProjectTablesPath) ReadUserInput()
    {
        while (true)
        {
            Console.Write("Server Name: ");
            var serverName = Console.ReadLine()?.Trim();

            Console.Write("Database Name: ");
            var databaseName = Console.ReadLine()?.Trim();

            Console.Write("Project Tables Path: ");
            var projectTablesPath = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(serverName) || string.IsNullOrEmpty(databaseName))
            {
                Console.WriteLine("Server name and database name are required.\n");
                continue;
            }

            if (string.IsNullOrEmpty(projectTablesPath))
            {
                Console.WriteLine("Project tables path is required.\n");
                continue;
            }

            return (serverName, databaseName, projectTablesPath);
        }
    }
}
