// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using System.Text.Json;

namespace cCoder.Packer.Models.Exports;

public sealed record ExportRecord(
    string Domain,
    string Category,
    string Name,
    JsonElement Value,
    bool CombineValues = false);