using System;

namespace Linter.Services;

internal readonly record struct LayoutCandidate(IntPtr Layout, string Text)
{
    public bool IsEmpty => string.IsNullOrEmpty(Text);
}
