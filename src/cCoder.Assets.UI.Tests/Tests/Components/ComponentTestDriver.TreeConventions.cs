// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components;

internal sealed partial class ComponentTestDriver
{
    internal static async Task AssertTreeConventionsAsync(
        IPage page,
        string componentName)
    {
        ILocator trees = page.Locator(
            selector: $".component[name='{componentName}'] "
                + ".k-treeview:visible");

        int treeCount = await trees.CountAsync();

        for (int index = 0; index < treeCount; index++)
        {
            ILocator tree = trees.Nth(index: index);

            string[] failures = await tree.EvaluateAsync<string[]>(
                expression: "element => {"
                    + "const widget = window.jQuery(element).data('kendoTreeView');"
                    + "if (!widget) return ['Kendo TreeView was not initialized'];"
                    + "const failures = [];"
                    + "const nodes = [...element.querySelectorAll('[role=treeitem]')];"
                    + "nodes.forEach((node, index) => {"
                    + "const content = node.querySelector('.k-treeview-leaf, "
                    + ".k-in');"
                    + "if (content && !content.querySelector('.k-icon, .k-svg-icon, "
                    + "img, svg')) failures.push('node ' + (index + 1) "
                    + "+ ' has no icon'); });"
                    + "if (widget.options.dragAndDrop "
                    + "&& typeof widget.options.drop !== 'function') "
                    + "failures.push('drag and drop has no drop handler');"
                    + "return failures; }");

            Assert.True(
                condition: failures.Length == 0,
                userMessage: $"{componentName} tree {index + 1}: "
                    + string.Join(separator: "; ", value: failures));

            ILocator expand = tree.Locator(
                selectorOrLocator: ".k-treeview-toggle:visible, "
                    + ".k-icon.k-i-expand:visible, "
                    + "[aria-expanded='false']:visible")
                .First;

            if (await expand.CountAsync() > 0)
            {
                await expand.ClickAsync();

                await Assertions.Expect(locator: tree)
                    .ToHaveAttributeAsync(
                        name: "class",
                        value: new Regex(pattern: "k-treeview"));
            }
        }
    }
}