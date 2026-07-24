// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using System.Text;

using System.Text.Json;

using cCoder.Packer.Models.Reports;

using cCoder.Packer.Services.Processings.Reports;

namespace cCoder.Packer.Services.Processings.Reports;


internal sealed partial class AssetReportProcessingService
    : IAssetReportProcessingService
{
    public Task<string> WriteAsync(
        string dataPath,
        CancellationToken cancellationToken = default) =>
        TryCatch(operation: async () =>
        {
            Validate(inputs: [dataPath, cancellationToken]);

            return await WriteAssetReportAsync(
                dataPath: dataPath,
                cancellationToken: cancellationToken);
        });

    private static async Task<string> WriteAssetReportAsync(
        string dataPath,
        CancellationToken cancellationToken)
    {
        string fullDataPath = Path.GetFullPath(path: dataPath);

        if (!Directory.Exists(path: fullDataPath))
        {
            throw new DirectoryNotFoundException(
                message: $"Data directory '{fullDataPath}' does not exist.");
        }

        List<AssetReportAsset> assets = await LoadAssetsAsync(
            dataPath: fullDataPath,
            cancellationToken: cancellationToken);

        AssetReportGraphProcessingService graph = new(assets: assets);

        string report = graph.Build();

        string reportDirectory = Path.Combine(
            path1: Directory.GetParent(path: fullDataPath)?.FullName
                ?? fullDataPath,
            path2: "reports");

        string reportPath = Path.Combine(
            path1: reportDirectory,
            path2: "asset-usage-report.md");

        Directory.CreateDirectory(path: reportDirectory);

        await File.WriteAllTextAsync(
            path: reportPath,
            contents: report,
            encoding: Encoding.UTF8,
            cancellationToken: cancellationToken);

        return reportPath;
    }

    private static async Task<List<AssetReportAsset>> LoadAssetsAsync(
        string dataPath,
        CancellationToken cancellationToken)
    {
        string[] files = Directory.GetFiles(
            path: dataPath,
            searchPattern: "*.json",
            searchOption: SearchOption.AllDirectories);

        List<AssetReportAsset> assets = new(capacity: files.Length);

        foreach (string file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using JsonDocument document = JsonDocument.Parse(
                json: await File.ReadAllTextAsync(
                    path: file,
                    cancellationToken: cancellationToken));

            string relativePath = Path.GetRelativePath(
                    relativeTo: dataPath,
                    path: file)
                .Replace(
                    oldChar: Path.DirectorySeparatorChar,
                    newChar: '/');

            string[] segments = relativePath.Split(separator: '/');

            assets.Add(item: new AssetReportAsset(
                RelativePath: relativePath,
                Source: segments[0],
                Type: segments.Length > 1 ? segments[^2] : string.Empty,
                IsCommonCache: string.Equals(
                    a: segments[0],
                    b: "Common Cache",
                    comparisonType: StringComparison.OrdinalIgnoreCase),
                Value: document.RootElement.Clone()));
        }

        return assets;
    }

    internal static AssetReportReferences GetReferences(
        JsonElement value,
        ISet<string> componentNames)
    {
        string text = string.Join(
            separator: "\n",
            values: GetStrings(value: value));

        List<string> components = GetTaggedValues(
            text: text,
            tagPrefix: "[component[");

        List<string> resources = GetResourceValues(text: text);

        List<string> scripts = GetTaggedValues(
            text: text,
            tagPrefix: "[script[");

        components.AddRange(collection:
            GetAttributeValues(
                text: text,
                attributeName: "data-component"));

        foreach (string call in GetLoadComponentCalls(text: text))
        {
            foreach (string candidate in GetQuotedValues(text: call))
            {
                if (componentNames.Contains(item: candidate))
                {
                    components.Add(item: candidate);
                }
            }
        }

        return new AssetReportReferences(
            Components: Distinct(values: components),
            Resources: Distinct(values: resources),
            Scripts: Distinct(values: scripts));
    }

    private static IEnumerable<string> GetStrings(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.String)
        {
            yield return value.GetString() ?? string.Empty;

            yield break;
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in value.EnumerateArray())
            {
                foreach (string text in GetStrings(value: item))
                {
                    yield return text;
                }
            }
        }
        else if (value.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in value.EnumerateObject())
            {
                foreach (string text in GetStrings(value: property.Value))
                {
                    yield return text;
                }
            }
        }
    }

    private static List<string> GetTaggedValues(
        string text,
        string tagPrefix)
    {
        List<string> values = [];

        int searchIndex = 0;

        while ((searchIndex = text.IndexOf(
            value: tagPrefix,
            startIndex: searchIndex,
            comparisonType: StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            int valueStart = searchIndex + tagPrefix.Length;

            int valueEnd = text.IndexOf(
                value: ']',
                startIndex: valueStart);

            if (valueEnd < 0)
            {
                break;
            }

            values.Add(item: text[valueStart..valueEnd]);

            searchIndex = valueEnd + 1;
        }

        return values;
    }

    private static List<string> GetResourceValues(string text)
    {
        List<string> values = [];

        int searchIndex = 0;

        while ((searchIndex = text.IndexOf(
            value: "[resource",
            startIndex: searchIndex,
            comparisonType: StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            int valueStart = text.IndexOf(
                value: '[',
                startIndex: searchIndex + 1);

            int valueEnd = valueStart < 0
                ? -1
                : text.IndexOf(value: ']', startIndex: valueStart + 1);

            if (valueStart < 0 || valueEnd < 0)
            {
                break;
            }

            values.Add(item: text[(valueStart + 1)..valueEnd]);

            searchIndex = valueEnd + 1;
        }

        return values;
    }

    private static List<string> GetAttributeValues(
        string text,
        string attributeName)
    {
        List<string> values = [];

        int searchIndex = 0;

        while ((searchIndex = text.IndexOf(
            value: attributeName,
            startIndex: searchIndex,
            comparisonType: StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            int equalsIndex = text.IndexOf(
                value: '=',
                startIndex: searchIndex + attributeName.Length);

            if (equalsIndex < 0)
            {
                break;
            }

            int quoteIndex = equalsIndex + 1;

            while (quoteIndex < text.Length
                && char.IsWhiteSpace(c: text[quoteIndex]))
            {
                quoteIndex++;
            }

            if (quoteIndex >= text.Length
                || text[quoteIndex] is not ('\'' or '"'))
            {
                searchIndex = equalsIndex + 1;

                continue;
            }

            char quote = text[quoteIndex];

            int valueEnd = text.IndexOf(
                value: quote,
                startIndex: quoteIndex + 1);

            if (valueEnd < 0)
            {
                break;
            }

            values.Add(item: text[(quoteIndex + 1)..valueEnd]);

            searchIndex = valueEnd + 1;
        }

        return values;
    }

    private static IEnumerable<string> GetQuotedValues(string text)
    {
        int searchIndex = 0;

        while (searchIndex < text.Length)
        {
            int singleQuote = text.IndexOf(
                value: '\'',
                startIndex: searchIndex);

            int doubleQuote = text.IndexOf(
                value: '"',
                startIndex: searchIndex);

            int backtick = text.IndexOf(
                value: '`',
                startIndex: searchIndex);

            int quoteIndex = new[] { singleQuote, doubleQuote, backtick }
                .Where(predicate: index => index >= 0)
                .DefaultIfEmpty(defaultValue: -1)
                .Min();

            if (quoteIndex < 0)
            {
                yield break;
            }

            char quote = text[quoteIndex];

            int valueEnd = text.IndexOf(
                value: quote,
                startIndex: quoteIndex + 1);

            if (valueEnd < 0)
            {
                yield break;
            }

            yield return text[(quoteIndex + 1)..valueEnd];

            searchIndex = valueEnd + 1;
        }
    }

    private static IReadOnlyList<string> Distinct(
        IEnumerable<string> values) =>
        [.. values
            .Where(predicate: value =>
                !string.IsNullOrWhiteSpace(value: value))
            .Distinct(comparer: StringComparer.OrdinalIgnoreCase)];

    private static IEnumerable<string> GetLoadComponentCalls(string text)
    {
        const string name = "loadComponent";

        int searchIndex = 0;

        while ((searchIndex = text.IndexOf(
            value: name,
            startIndex: searchIndex,
            comparisonType: StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            int openingParenthesis = text.IndexOf(
                value: '(',
                startIndex: searchIndex + name.Length);

            if (openingParenthesis < 0)
            {
                yield break;
            }

            int end = FindCallEnd(
                text: text,
                openingParenthesis: openingParenthesis);

            if (end < 0)
            {
                searchIndex = openingParenthesis + 1;

                continue;
            }

            yield return text[searchIndex..end];

            searchIndex = end;
        }
    }

    private static int FindCallEnd(string text, int openingParenthesis)
    {
        int depth = 1;

        char quote = '\0';

        bool escaped = false;

        for (int index = openingParenthesis + 1;

            index < text.Length;

            index++)
        {
            char character = text[index];

            if (quote != '\0')
            {
                if (escaped)
                {
                    escaped = false;
                }
                else if (character == '\\')
                {
                    escaped = true;
                }
                else if (character == quote)
                {
                    quote = '\0';
                }

                continue;
            }

            if (character is '\'' or '"' or '`')
            {
                quote = character;
            }
            else if (character is '(' or '[' or '{')
            {
                depth++;
            }
            else if (character is ')' or ']' or '}')
            {
                depth--;

                if (depth == 0)
                {
                    return index + 1;
                }
            }
        }

        return -1;
    }

    internal static string? GetString(JsonElement value, string name) =>
        value.ValueKind == JsonValueKind.Object
        && value.TryGetProperty(propertyName: name, value: out JsonElement item)
        && item.ValueKind == JsonValueKind.String
            ? item.GetString()
            : null;
}