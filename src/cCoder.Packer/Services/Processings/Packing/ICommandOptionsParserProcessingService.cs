// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Packer.Models.Commands;

namespace cCoder.Packer.Services.Processings.Packing;

internal interface ICommandOptionsParserProcessingService
{
    CommandOptions Parse(string[] args);
}