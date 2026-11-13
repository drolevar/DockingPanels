namespace Digi21.WinUI.Docking.Interaction;

// A rectangle in the dock site's coordinates. The placement below is plain arithmetic, so it uses
// its own type and can be tested without a XAML runtime.
internal readonly record struct PlacementRect(double X, double Y, double Width, double Height)
{
    internal double Right => X + Width;

    internal double Bottom => Y + Height;
}

// Where an auto-hide flyout goes: the area it may cover, and the rectangle it takes within it.
internal static class AutoHideFlyoutPlacement
{
    // The area a panel that slides out of a strip may cover, when the strip is inside a layout
    // instead of along an edge of the dock site: what the split holding the strip has, less the
    // strip itself and whatever lies beyond it. A panel from a strip at the bottom of the terminal
    // column covers that column down to the strip, and nothing of the panels beside it.
    //
    // edge: The side of the split the strip is on.
    //
    // split: The bounds of the split that holds the strip.
    //
    // strip: The bounds of the strip.
    internal static PlacementRect AreaBesideStrip(DockSide edge, PlacementRect split, PlacementRect strip)
    {
        return edge switch
        {
            DockSide.Bottom => new PlacementRect(split.X, split.Y, split.Width, Math.Max(0, strip.Y - split.Y)),
            DockSide.Top => new PlacementRect(split.X, strip.Bottom, split.Width, Math.Max(0, split.Bottom - strip.Bottom)),
            DockSide.Right => new PlacementRect(split.X, split.Y, Math.Max(0, strip.X - split.X), split.Height),
            _ => new PlacementRect(strip.Right, split.Y, Math.Max(0, split.Right - strip.Right), split.Height),
        };
    }

    // The rectangle a panel takes inside the area it may cover: the whole length of the side it
    // comes out of, as deep as its size allows (see AutoHideResizePolicy.Clamp), against that side.
    internal static PlacementRect Place(DockSide edge, PlacementRect area, double size)
    {
        var sideEdge = edge is DockSide.Left or DockSide.Right;
        var depth = AutoHideResizePolicy.Clamp(size, sideEdge ? area.Width : area.Height);

        return sideEdge
            ? new PlacementRect(area.X + (edge == DockSide.Right ? area.Width - depth : 0), area.Y, depth, area.Height)
            : new PlacementRect(area.X, area.Y + (edge == DockSide.Bottom ? area.Height - depth : 0), area.Width, depth);
    }
}
