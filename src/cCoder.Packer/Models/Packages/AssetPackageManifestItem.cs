// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

namespace cCoder.Packer.Models.Packages;

public sealed record AssetPackageManifestItem(
    string Path,
    string Sha256,
    bool FirstTimeSetup,
    string Source,
    string Category,
    string[] ItemTypes);