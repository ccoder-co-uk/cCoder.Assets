using System.Text.Json;

namespace cCoder.Packer.Models;

internal static class JsonDefaults
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };
}
