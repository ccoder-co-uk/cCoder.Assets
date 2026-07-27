// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

namespace cCoder.Packer.Models.Commands;

public sealed record CommandOptions(
    string Name,
    string? Target,
    Uri? Source,
    string? User,
    string? Password,
    string? AppName,
    string? TenantId,
    string? BaselinePath,
    int? AppId,
    string? DataPath,
    string? PackagesPath,
    string? DestinationPath,
    string? PackageName,
    string? Category);