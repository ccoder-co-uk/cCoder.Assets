// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using System.Text.Json;

namespace cCoder.Packer.Models.Reports;

internal sealed record AssetReportAsset(
    string RelativePath,
    string Source,
    string Key,
    string Type,
    bool IsCommonCache,
    bool FirstTimeSetup,
    JsonElement Value);