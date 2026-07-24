// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using System.Text.Json;

namespace cCoder.Packer.Models.Packages;

public sealed record PackageSourceItem(
    string Source,
    string Key,
    string Type,
    bool FirstTimeSetup,
    JsonElement Value);