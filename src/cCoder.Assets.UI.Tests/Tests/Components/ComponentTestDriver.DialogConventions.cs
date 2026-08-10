// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components;

internal sealed partial class ComponentTestDriver
{
    internal static async Task AssertVisibleDialogConventionsAsync(
        IPage page,
        string componentName)
    {
        ILocator dialogs = page.Locator(
            selector: ".k-window:visible, .modal.show:visible, "
                + "[role='dialog']:visible");

        int dialogCount = await dialogs.CountAsync();

        for (int index = 0; index < dialogCount; index++)
        {
            ILocator dialog = dialogs.Nth(index: index);

            string[] failures = await dialog.EvaluateAsync<string[]>(
                expression: "element => {"
                    + "const failures = [];"
                    + "const bounds = element.getBoundingClientRect();"
                    + "if (bounds.width < 300) failures.push('width is below 300px');"
                    + "if (bounds.height <= 50) failures.push('height is 50px or less');"
                    + "if (element.scrollWidth > element.clientWidth + 2) "
                    + "failures.push('unexpected horizontal scrollbar');"
                    + "const fields = [...element.querySelectorAll('input:not([type=hidden]), "
                    + "select, textarea')];"
                    + "fields.forEach(field => {"
                    + "const group = field.closest('.input-group, .row, .form-group');"
                    + "const label = group?.querySelector('label, .input-group-text');"
                    + "if (!label) failures.push((field.name || field.type) "
                    + "+ ' has no label');"
                    + "else if (label.getBoundingClientRect().left "
                    + "> field.getBoundingClientRect().left) "
                    + "failures.push((field.name || field.type) "
                    + "+ ' label is not left of its field'); });"
                    + "return failures; }");

            Assert.True(
                condition: failures.Length == 0,
                userMessage: $"{componentName} dialog {index + 1}: "
                    + string.Join(separator: "; ", value: failures));
        }
    }
}