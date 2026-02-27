using System.Text.RegularExpressions;
using ExtendedPropertiesExporter.App.Models;

namespace ExtendedPropertiesExporter.App.Services;

public enum UpdateResult
{
    Updated,
    NotFound,
    NoTableDefinition
}

public sealed partial class SqlFileUpdater
{
    private const string BatchSeparator = "GO";
    private const string ExtendedPropertyMarker = "sp_addextendedproperty";

    private readonly string _extendedPropertyTemplate;
    private readonly Dictionary<string, List<string>> _sqlFileIndex;

    public SqlFileUpdater(string rootPath, string extendedPropertyTemplate)
    {
        _extendedPropertyTemplate = extendedPropertyTemplate;
        _sqlFileIndex = BuildFileIndex(rootPath);
    }

    public int IndexedFileCount => _sqlFileIndex.Count;

    public List<(string FilePath, UpdateResult Result)> UpdateTable(TableInfo table)
    {
        var results = new List<(string FilePath, UpdateResult Result)>();

        if (!_sqlFileIndex.TryGetValue(table.Name, out var matchingFiles))
        {
            results.Add((string.Empty, UpdateResult.NotFound));
            return results;
        }

        foreach (var filePath in matchingFiles)
        {
            var content = File.ReadAllText(filePath);

            if (!CreateTableRegex().IsMatch(content))
            {
                results.Add((filePath, UpdateResult.NoTableDefinition));
                continue;
            }

            var lines = File.ReadAllLines(filePath);
            var cleanedLines = RemoveExtendedPropertyBatches(lines);

            File.WriteAllLines(filePath, cleanedLines);

            if (table.ExtendedProperties.Count > 0)
            {
                AppendExtendedProperties(filePath, table.ExtendedProperties);
            }

            results.Add((filePath, UpdateResult.Updated));
        }

        return results;
    }

    private static Dictionary<string, List<string>> BuildFileIndex(string rootPath)
    {
        return Directory
            .EnumerateFiles(rootPath, "*.sql", SearchOption.AllDirectories)
            .GroupBy(
                f => Path.GetFileNameWithoutExtension(f),
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.ToList(),
                StringComparer.OrdinalIgnoreCase);
    }

    private static List<string> RemoveExtendedPropertyBatches(string[] lines)
    {
        var batches = SplitIntoBatches(lines);

        var kept = batches
            .Where(batch => !batch.Any(line =>
                line.Contains(ExtendedPropertyMarker, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        // Remove trailing empty batches
        while (kept.Count > 0 && kept[^1].All(string.IsNullOrWhiteSpace))
        {
            kept.RemoveAt(kept.Count - 1);
        }

        return RebuildFromBatches(kept);
    }

    private static List<List<string>> SplitIntoBatches(string[] lines)
    {
        var batches = new List<List<string>>();
        var currentBatch = new List<string>();

        foreach (var line in lines)
        {
            if (line.Trim().Equals(BatchSeparator, StringComparison.OrdinalIgnoreCase))
            {
                batches.Add(currentBatch);
                currentBatch = [];
            }
            else
            {
                currentBatch.Add(line);
            }
        }

        batches.Add(currentBatch);
        return batches;
    }

    private static List<string> RebuildFromBatches(List<List<string>> batches)
    {
        var output = new List<string>();

        for (var i = 0; i < batches.Count; i++)
        {
            output.AddRange(batches[i]);

            if (i < batches.Count - 1)
            {
                output.Add(BatchSeparator);
            }
        }

        // Trim trailing blank lines
        while (output.Count > 0 && string.IsNullOrWhiteSpace(output[^1]))
        {
            output.RemoveAt(output.Count - 1);
        }

        return output;
    }

    private void AppendExtendedProperties(string filePath, List<ExtendedProperty> properties)
    {
        using var stream = File.AppendText(filePath);

        foreach (var property in properties)
        {
            var statement = _extendedPropertyTemplate
                .Replace("{{Extendend_Name}}", property.Name)
                .Replace("{{Extended_Value}}", property.Value)
                .Replace("{{Table_Name}}", property.ObjectName)
                .Replace("{{Column_Name}}", property.ColumnName)
                .Trim();

            stream.WriteLine();
            stream.WriteLine(BatchSeparator);
            stream.WriteLine(statement);
        }
    }

    [GeneratedRegex(@"CREATE\s+TABLE", RegexOptions.IgnoreCase)]
    private static partial Regex CreateTableRegex();
}
