using System;

namespace Linter.Services;

/// <summary>
/// Everything needed to undo a conversion the tool just performed: what is
/// now on screen (<see cref="ToText"/> under <see cref="ToLayout"/>, plus
/// the untouched <see cref="Trailing"/>) and what to restore it to
/// (<see cref="FromText"/> under <see cref="FromLayout"/>).
/// </summary>
internal readonly record struct ConversionResult(
    IntPtr FromLayout,
    string FromText,
    IntPtr ToLayout,
    string ToText,
    string Trailing)
{
    /// <summary>Characters currently on screen that an undo must delete.</summary>
    public int VisibleLength => ToText.Length + Trailing.Length;
}
