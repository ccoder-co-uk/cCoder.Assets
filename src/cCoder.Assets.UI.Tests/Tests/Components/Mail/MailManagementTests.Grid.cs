// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using Microsoft.Playwright;
using System.Text.Json;

namespace cCoder.Assets.UI.Tests.Tests.Components.Mail;

public sealed partial class MailManagementTests
{
    private static async Task ArrangeVisibleMailRowsAsync(IPage page)
    {
        (string Pattern, string Body)[] routes =
        [
            ("**/Api/Mail/QueuedEmail**", JsonSerializer.Serialize(value: new
            {
                value = new[]
                {
                    new {
                    Id = Guid.NewGuid(), AppId = 1,
                    Subject = "Acceptance queued email",
                    SentByUserId = "AssetsAcceptanceAdmin",
                    To = "recipient@localhost", Content = "<p>Queued</p>",
                    FailedSends = Array.Empty<object>(), type = "Mail/QueuedEmail" }
                }
            })),
            ("**/Api/Mail/SentEmail**", JsonSerializer.Serialize(value: new
            {
                value = new[]
                {
                    new {
                    Id = Guid.NewGuid(), AppId = 1,
                    Subject = "Acceptance sent email", SentOn = DateTime.UtcNow,
                    Content = "<p>Sent</p>", type = "Mail/SentEmail" }
                }
            })),
            ("**/Api/Mail/MailSender**", JsonSerializer.Serialize(value: new
            {
                value = new[]
                {
                    new {
                    Id = Guid.NewGuid(), AppId = 1, Name = "Acceptance SMTP",
                    ProviderName = "SMTP", Host = "localhost", Port = 25,
                    EnableSSL = false, type = "Mail/MailSender" }
                }
            })),
            ("**/Api/Mail/MailReceiver**", JsonSerializer.Serialize(value: new
            {
                value = new[]
                {
                    new {
                    Id = Guid.NewGuid(), AppId = 1, Name = "Acceptance receiver",
                    ProviderName = "POP3", Host = "localhost", Port = 110,
                    EnableSSL = false, IsEnabled = true,
                    LastReceivedOn = DateTime.UtcNow, type = "Mail/MailReceiver" }
                }
            }))
        ];

        foreach ((string Pattern, string Body) routeData in routes)
        {
            await page.RouteAsync(
                url: routeData.Pattern,
                handler: route => route.FulfillAsync(
                    options: new()
                    {
                        Status = 200,
                        ContentType = "application/json",
                        Body = routeData.Body
                    }));
        }

        await page.EvaluateAsync(
            expression: "async () => MailManagement.init("
                + "session.app, $('.component[name=MailManagement]'))");
    }
}