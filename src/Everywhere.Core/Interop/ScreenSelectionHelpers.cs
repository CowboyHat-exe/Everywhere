using Avalonia;

namespace Everywhere.Interop;

/// <summary>
/// Shared helper methods for screen selection sessions.
/// Eliminates duplicated logic across Windows, Mac, and Linux platform implementations.
/// </summary>
public static class ScreenSelectionHelpers
{
    /// <summary>
    /// Formats a size info string from a bounding rectangle for tooltip display.
    /// </summary>
    public static string FormatSizeInfo(PixelRect rect)
    {
        return $"{rect.Width} x {rect.Height}";
    }

    /// <summary>
    /// Cycles to the next selection mode based on mouse wheel direction.
    /// </summary>
    /// <param name="allowedModes">The list of allowed modes to cycle through.</param>
    /// <param name="currentMode">The current selection mode.</param>
    /// <param name="delta">Positive for scroll up (previous), negative for scroll down (next).</param>
    /// <param name="wrap">If true, wraps around at boundaries; if false, clamps to first/last mode.</param>
    /// <returns>The new selection mode after cycling.</returns>
    public static ScreenSelectionMode CycleMode(
        IReadOnlyList<ScreenSelectionMode> allowedModes,
        ScreenSelectionMode currentMode,
        int delta,
        bool wrap = true)
    {
        var currentIndex = -1;
        for (var i = 0; i < allowedModes.Count; i++)
        {
            if (allowedModes[i] == currentMode)
            {
                currentIndex = i;
                break;
            }
        }

        var newIndex = currentIndex + (delta > 0 ? -1 : 1);
        if (wrap)
        {
            if (newIndex < 0) newIndex = allowedModes.Count - 1;
            else if (newIndex >= allowedModes.Count) newIndex = 0;
        }
        else
        {
            newIndex = Math.Clamp(newIndex, 0, allowedModes.Count - 1);
        }
        return allowedModes[newIndex];
    }

    /// <summary>
    /// Calculates the tooltip window position relative to a pointer point,
    /// ensuring it stays within screen bounds.
    /// </summary>
    /// <param name="pointerX">Pointer X coordinate in screen pixels.</param>
    /// <param name="pointerY">Pointer Y coordinate in screen pixels.</param>
    /// <param name="tooltipWidth">Width of the tooltip.</param>
    /// <param name="tooltipHeight">Height of the tooltip.</param>
    /// <param name="screenRight">Right edge of the screen.</param>
    /// <param name="screenTop">Top edge of the screen (typically 0 for top-left origin).</param>
    /// <param name="margin">Margin between pointer and tooltip (default: 16).</param>
    /// <returns>The calculated (x, y) position for the tooltip.</returns>
    public static (int x, int y) CalculateTooltipPosition(
        int pointerX,
        int pointerY,
        double tooltipWidth,
        double tooltipHeight,
        int screenRight,
        int screenTop = 0,
        int margin = 16)
    {
        var x = (double)pointerX;
        var y = pointerY - margin - tooltipHeight;

        // Check if there is enough space above the pointer
        if (y < screenTop)
        {
            y = pointerY + margin; // place below the pointer
        }

        // Check if there is enough space to the right of the pointer
        if (x + tooltipWidth > screenRight)
        {
            x = pointerX - tooltipWidth; // place to the left of the pointer
        }

        return ((int)x, (int)y);
    }

    /// <summary>
    /// Calculates a drag rectangle from start and current points.
    /// </summary>
    /// <param name="startX">The X coordinate where the drag started.</param>
    /// <param name="startY">The Y coordinate where the drag started.</param>
    /// <param name="currentX">The current X coordinate.</param>
    /// <param name="currentY">The current Y coordinate.</param>
    /// <returns>A normalized PixelRect representing the drag area.</returns>
    public static PixelRect CalculateDragRect(int startX, int startY, int currentX, int currentY)
    {
        var minX = Math.Min(startX, currentX);
        var minY = Math.Min(startY, currentY);
        var maxX = Math.Max(startX, currentX);
        var maxY = Math.Max(startY, currentY);
        return new PixelRect(minX, minY, maxX - minX, maxY - minY);
    }
}
