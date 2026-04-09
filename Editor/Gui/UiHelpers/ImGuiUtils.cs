#nullable enable
using ImGuiNET;

namespace T3.Editor.Gui.UiHelpers;

/// <summary>
/// Small helpers for working around ImGui quirks.
/// </summary>
internal static class ImGuiUtils
{
    /// <summary>
    /// Resets the cursor to <see cref="ImGui.GetCursorStartPos"/> so the
    /// "SetCursorPos extends parent boundaries" assertion (added in ImGui 1.91)
    /// passes at the next <c>End*</c> call.
    /// <para>
    /// Why it's needed: any scope that calls <c>SetCursorPos*</c> sets an
    /// internal <c>IsSetPos</c> flag which is checked at <c>End/EndChild/EndGroup</c>.
    /// Because <c>ItemSize</c> always advances the cursor by an extra
    /// <c>ItemSpacing.y</c> beyond <c>CursorMaxPos</c>, the check fails after
    /// any normal item submission. Resetting the cursor to the content start
    /// position (which is by definition <see cref="ImGui.GetCursorStartPos"/>)
    /// is always within bounds and silences the assert.
    /// </para>
    /// <para>
    /// Call this immediately before <c>End/EndChild/EndGroup</c> in any scope
    /// that uses <c>SetCursorPos*</c>.
    /// </para>
    /// </summary>
    public static void ResetCursorForExtentCheck()
    {
        ImGui.SetCursorPos(ImGui.GetCursorStartPos());
    }
}
