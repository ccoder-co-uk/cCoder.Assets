// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Assets.UI.Tests.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.Mail;

public sealed partial class MailManagementTests
{
    [Fact]
    public async Task RetryFailedSends_ShouldSubmitQueuedEmailRetry()
    {
        // Given
        const string pagePath = "Admin/MailManagement";
        const string queuedEmailId = "34ecf6fa-2f92-44ae-b61f-12b3ad06adbe";
        bool retryRequested = false;

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: pagePath,
            componentName: "MailManagement",
            action: async page =>
            {
                await ArrangeVisibleMailRowsAsync(page: page);

                await page.RouteAsync(
                    url: "**/Api/Mail/QueuedEmail(*)/Retry",
                    handler: async route =>
                    {
                        retryRequested = true;

                        await route.FulfillAsync(
                            options: new RouteFulfillOptions
                            {
                                Status = 204
                            });
                    });

                await page.EvaluateAsync(
                    expression: "args => {"
                        + "const host = $('<div id=retry-test-host></div>')"
                        + ".appendTo(document.body);"
                        + "MailManagement.renderMessageDetails(host, {"
                        + "Id: args.id, type: 'Mail/QueuedEmail', Content: '', FailedSends: [{"
                        + "AttemptedOn: new Date().toISOString(),"
                        + "FailureReason: 'acceptance failure' }] }, [],"
                        + "'QueuedEmail'); }",
                    arg: new { id = queuedEmailId });

                await page.Locator(
                    selector: "#retry-test-host button"
                        + "[data-bs-target*='mail-failures-']")
                    .ClickAsync();

                await page.Locator(
                    selector: "#retry-test-host button[name='retryFailedSends']")
                    .ClickAsync();

                await Assertions.Expect(
                    locator: page.Locator(selector: "#retry-test-host"))
                    .ToContainTextAsync(expected: "Retry queued.");
            });

        // Then
        Assert.True(
            condition: retryRequested,
            userMessage: "The retry action did not call the queued-email retry endpoint.");
    }
}