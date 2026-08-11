// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using Xunit;

namespace cCoder.Assets.UI.Tests.Infrastructure;

[CollectionDefinition(name: "Published Core UI")]
public sealed class PublishedCoreCollection :
    ICollectionFixture<PublishedCoreFixture>;