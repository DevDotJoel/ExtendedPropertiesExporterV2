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
                Console.WriteLine($"Erro: Caminho não encontrado: {rootPath}");
                Console.WriteLine("Tente novamente.\n");
                continue;
            }

            var connectionString = appSettings.ConnectionStringTemplate
                .Replace("{{ServerName}}", serverName)
                .Replace("{{DatabaseName}}", databaseName);

            try
            {
                using var dbService = new DatabaseService(connectionString, appSettings);
                Console.WriteLine($"Conectado a {databaseName} em {serverName}.");

                var tables = dbService.GetTablesWithExtendedProperties();
                Console.WriteLine($"Encontradas {tables.Count} tabelas.");

                var fileUpdater = new SqlFileUpdater(
                    rootPath,
                    appSettings.ColumnPropertyTemplate,
                    appSettings.TablePropertyTemplate);
                Console.WriteLine($"Indexados {fileUpdater.IndexedFileCount} ficheiros .sql.");

                Console.WriteLine("A atualizar ficheiros do projeto...");
                foreach (var table in tables)
                {
                    var results = fileUpdater.UpdateTable(table);
                    LogUpdateResults(table, results);
                }

                Console.WriteLine("Concluído.");
                break;
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                Console.WriteLine($"Não foi possível ligar: {ex.Message}");
                Console.WriteLine("Tente novamente.\n");
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
                    Console.WriteLine($"  Atualizado {filePath} ({table.ExtendedProperties.Count} propriedades)");
                    break;
                case UpdateResult.NotFound:
                    Console.WriteLine($"  Ignorado {table.Name} — ficheiro .sql não encontrado.");
                    break;
                case UpdateResult.NoTableDefinition:
                    Console.WriteLine($"  Ignorado {filePath} — não contém CREATE TABLE."); // caso seja sp's views functions etc
                    break;
            }
        }
    }

    private static (string ServerName, string DatabaseName, string RootPath) ReadUserInput()
    {
        while (true)
        {
            Console.Write("Nome Servidor: ");
            var serverName = Console.ReadLine()?.Trim();

            Console.Write("Nome Base Dados: ");
            var databaseName = Console.ReadLine()?.Trim();

            Console.Write("Caminho raiz do projeto: ");
            var rootPath = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(serverName) || string.IsNullOrEmpty(databaseName))
            {
                Console.WriteLine("O nome do servidor e da base de dados são obrigatórios.\n");
                continue;
            }

            if (string.IsNullOrEmpty(rootPath))
            {
                Console.WriteLine("O caminho raiz é obrigatório.\n");
                continue;
            }

            return (serverName, databaseName, rootPath);
        }
    }
}
