// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using System.Threading.Tasks;
using cCoder.Assets.UI.Tests.Infrastructure;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.ContentManagement;

[Collection(name: "Published Core UI")]
public sealed partial class MetadataMenuTests(PublishedCoreFixture fixture)
{
    private readonly ComponentTestDriver driver = new(fixture: fixture);

    [Fact]
    public Task Rendering_ShouldInitialize() =>
        driver.AssertComponentRendersAsync(
            pagePath: "Admin/Metadata",
            componentName: "MetadataMenu");
}