using System.Text.Json;

namespace cCoder.Packer;

public sealed record ExportRecord(
    string Domain,
    string Category,
    string Name,
    JsonElement Value,
    bool CombineValues = false);
