using ExtendedPropertiesExporter.App.Models;

namespace ExtendedPropertiesExporter.App.Services;

public sealed class SqlFileUpdater
{
    private const string BatchSeparator = "GO";
    private const string ExtendedPropertyMarker = "sp_addextendedproperty";

    private readonly string _projectTablesPath;
    private readonly string _extendedPropertyTemplate;

    public SqlFileUpdater(string projectTablesPath, string extendedPropertyTemplate)
    {
        _projectTablesPath = projectTablesPath;
        _extendedPropertyTemplate = extendedPropertyTemplate;
    }

    public void UpdateTable(TableInfo table)
    {
        var filePath = Path.Combine(_projectTablesPath, $"{table.Name}.sql");

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"SQL file not found for table '{table.Name}'.", filePath);
        }

        var lines = File.ReadAllLines(filePath);
        var cleanedLines = RemoveExtendedPropertyBatches(lines);

        File.WriteAllLines(filePath, cleanedLines);

        if (table.ExtendedProperties.Count > 0)
        {
            AppendExtendedProperties(filePath, table.ExtendedProperties);
        }
    }

    /// <summary>
    /// Splits the file into GO-delimited batches, removes any batch
    /// containing sp_addextendedproperty, and rebuilds the file.
    /// Handles all format variants: EXEC/EXECUTE, sys. prefix,
    /// mixed-case Go/GO, blank lines, semicolons, etc.
    /// </summary>
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
}
