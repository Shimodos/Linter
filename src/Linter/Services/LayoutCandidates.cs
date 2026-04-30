using System;
using System.Collections.Generic;
using System.Text;
using Linter.Keyboard;
using Linter.Text;

namespace Linter.Services;

/// <summary>
/// Replays a typed word under every installed keyboard layout other than
/// the one it was originally typed under. Each result is what the user
/// would have seen if that layout had been active when they were typing.
/// </summary>
internal static class LayoutCandidates
{
    public static IEnumerable<LayoutCandidate> ForOtherLayouts(WordSnapshot snapshot, IntPtr currentLayout)
    {
        if (snapshot.IsEmpty) yield break;

        foreach (var layout in InstalledLayouts.Enumerate())
        {
            if (layout == currentLayout) continue;
            var rendered = RenderUnder(snapshot, layout);
            if (rendered.Length > 0)
                yield return new LayoutCandidate(layout, rendered);
        }
    }

    private static string RenderUnder(WordSnapshot snapshot, IntPtr layout)
    {
        var result = new StringBuilder(snapshot.Text.Length);
        foreach (var stroke in snapshot.Strokes)
        {
            var rendered = LayoutRenderer.Render(stroke.VirtualKey, stroke.ScanCode, stroke.Modifiers, layout);
            result.Append(rendered);
        }
        return result.ToString();
    }
}
