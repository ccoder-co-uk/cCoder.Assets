// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

namespace cCoder.Packer.Models.Commands;

public sealed record CommandOptions(
    string Target,
    Uri Source,
    string? User,
    string? Password,
    int? AppId);