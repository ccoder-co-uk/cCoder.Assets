using System.Text.Json;

namespace cCoder.Packer;

public sealed class ExportWriter(string dataPath)
{
    public async Task<IReadOnlyList<string>> WriteAsync(
        IEnumerable<ExportRecord> records,
        CancellationToken cancellationToken = default)
    {
        List<string> writtenFiles = [];

        foreach (IGrouping<(string Domain, string Category, string Name), ExportRecord> group
            in records
                .OrderBy(record => record.Domain, StringComparer.OrdinalIgnoreCase)
                .ThenBy(record => record.Category, StringComparer.OrdinalIgnoreCase)
                .ThenBy(record => record.Name, StringComparer.OrdinalIgnoreCase)
                .GroupBy(record => (
                    record.Domain,
                    record.Category,
                    record.Name)))
        {
            string directory = Path.Combine(
                dataPath,
                SafeSegment(group.Key.Domain),
                SafeSegment(group.Key.Category));

            Directory.CreateDirectory(directory);

            string file = Path.Combine(
                directory,
                $"{SafeSegment(group.Key.Name)}.json");

            ExportRecord[] values = group.ToArray();
            object content = values.Length == 1
                ? values[0].Value
                : values.Select(record => record.Value).ToArray();

            await using FileStream stream = File.Create(file);
            await JsonSerializer.SerializeAsync(
                stream,
                content,
                JsonDefaults.Options,
                cancellationToken);

            writtenFiles.Add(file);
        }

        return writtenFiles;
    }

    private static string SafeSegment(string value)
    {
        char[] invalidCharacters = Path.GetInvalidFileNameChars();
        string result = new(value
            .Trim()
            .Select(character =>
                invalidCharacters.Contains(character) ? '_' : character)
            .ToArray());

        result = result.TrimEnd('.', ' ');
        return string.IsNullOrWhiteSpace(result) ? "Unnamed" : result;
    }
}
