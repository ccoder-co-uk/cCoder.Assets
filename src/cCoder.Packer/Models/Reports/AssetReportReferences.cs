// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

namespace cCoder.Packer.Models.Reports;

internal sealed record AssetReportReferences(
    IReadOnlyList<string> Components,
    IReadOnlyList<string> Resources,
    IReadOnlyList<string> Scripts,
    IReadOnlyList<string> Styles);