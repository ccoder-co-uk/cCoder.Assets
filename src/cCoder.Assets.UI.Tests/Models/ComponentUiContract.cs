// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

namespace cCoder.Assets.UI.Tests.Models;

internal sealed record ComponentUiContract(
    string Name,
    string Route,
    string ReadySelector,
    bool AuthenticationRequired,
    string[] RequiredApiResponses);