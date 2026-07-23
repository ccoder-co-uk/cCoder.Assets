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
                new[] { dataPath, SafeSegment(group.Key.Domain) }
                    .Concat(SafePath(group.Key.Category))
                    .ToArray());

            Directory.CreateDirectory(directory);

            ExportRecord[] values = group.ToArray();
            if (values.Length == 1 || values.All(value => value.CombineValues))
            {
                string file = Path.Combine(
                    directory,
                    $"{SafeSegment(group.Key.Name)}.json");

                object content = values.Length == 1
                    ? values[0].Value
                    : values.Select(record => record.Value).ToArray();

                await WriteFileAsync(file, content, cancellationToken);
                writtenFiles.Add(file);
                continue;
            }

            for (int index = 0; index < values.Length; index++)
            {
                string file = Path.Combine(
                    directory,
                    $"{SafeSegment(group.Key.Name)}-{index + 1}.json");

                await WriteFileAsync(
                    file,
                    values[index].Value,
                    cancellationToken);

                writtenFiles.Add(file);
            }
        }

        return writtenFiles;
    }

    private static async Task WriteFileAsync(
        string file,
        object content,
        CancellationToken cancellationToken)
    {
        await using FileStream stream = File.Create(file);
        await JsonSerializer.SerializeAsync(
            stream,
            content,
            JsonDefaults.Options,
            cancellationToken);
    }

    private static IEnumerable<string> SafePath(string value) =>
        value.Split(
                [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                StringSplitOptions.RemoveEmptyEntries)
            .Select(SafeSegment);

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
