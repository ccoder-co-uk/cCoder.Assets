// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using System.Text.Json;
using cCoder.Packer.Models.Exports;

namespace cCoder.Packer.Dependencies;

public sealed class ExportWriterDependency(string dataPath)
{
    public async Task<IReadOnlyList<string>> WriteAsync(
        IEnumerable<ExportRecord> records,
        CancellationToken cancellationToken = default)
    {
        List<string> writtenFiles = [];

        foreach (IGrouping<(string Domain, string Category, string Name), ExportRecord> group
            in records
                .OrderBy(keySelector: record => record.Domain, comparer: StringComparer.OrdinalIgnoreCase)
                .ThenBy(keySelector: record => record.Category, comparer: StringComparer.OrdinalIgnoreCase)
                .ThenBy(keySelector: record => record.Name, comparer: StringComparer.OrdinalIgnoreCase)
                .GroupBy(keySelector: record => (
                    record.Domain,
                    record.Category,
                    record.Name)))
        {
            string directory = Path.Combine(
paths: new[] { dataPath, SafeSegment(value: group.Key.Domain) }
                    .Concat(second: SafePath(value: group.Key.Category))
                    .ToArray());

            Directory.CreateDirectory(path: directory);

            ExportRecord[] values = group.ToArray();

            if (values.Length == 1 || values.All(predicate: value => value.CombineValues))
            {
                string file = Path.Combine(
path1: directory,
path2: $"{SafeSegment(value: group.Key.Name)}.json");

                object content = values.Length == 1
                    ? values[0].Value
                    : values
                        .Select(selector: record => record.Value)
                        .ToArray();

                await WriteFileAsync(file: file, content: content, cancellationToken: cancellationToken);
                writtenFiles.Add(item: file);
                continue;
            }

            for (int index = 0; index < values.Length; index++)
            {
                string file = Path.Combine(
path1: directory,
path2: $"{SafeSegment(value: group.Key.Name)}-{index + 1}.json");

                await WriteFileAsync(
file: file,
content: values[index].Value,
cancellationToken: cancellationToken);

                writtenFiles.Add(item: file);
            }
        }

        return writtenFiles;
    }

    private static async Task WriteFileAsync(
        string file,
        object content,
        CancellationToken cancellationToken)
    {
        await using FileStream stream = File.Create(path: file);

        await JsonSerializer.SerializeAsync(
utf8Json: stream,
value: content,
options: JsonDefaultsDependency.Options,
cancellationToken: cancellationToken);
    }

    private static IEnumerable<string> SafePath(string value) =>
        value.Split(
separator: [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
options: StringSplitOptions.RemoveEmptyEntries)
            .Select(selector: SafeSegment);

    private static string SafeSegment(string value)
    {
        char[] invalidCharacters = Path.GetInvalidFileNameChars();

        string result = new(value: value
            .Trim()
            .Select(selector: character =>
                invalidCharacters.Contains(value: character) ? '_' : character)
            .ToArray());

        result = result.TrimEnd(trimChars: ['.', ' ']);
        return string.IsNullOrWhiteSpace(value: result) ? "Unnamed" : result;
    }
}