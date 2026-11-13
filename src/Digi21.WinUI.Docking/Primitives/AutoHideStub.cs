using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Digi21.WinUI.Docking.Primitives;

// The strip of tabs of a collapsed panel, kept inside the layout instead of along an edge of the
// dock site (AutoHideStripPlacement.InPlace).
//
// It is a child of the split container the panel came out of, and not one of its panes: like a
// splitter it is part of how the split is drawn, so everything that walks the layout tree by its
// panes (locating a node, saving the layout, choosing a drop target) never meets it. The split gives
// it the room its strip needs along the split's axis and shares what remains among its panes.
internal sealed partial class AutoHideStub : Panel
{
    internal AutoHideStub(DockSide side)
    {
        Side = side;

        // The tabs of a strip along an edge are placed near where their panel was along that edge.
        // Here the strip is exactly as long as the area the panel came out of, so they start at its
        // beginning.
        Strip = new AutoHideTabStrip { Edge = side, IgnoreOffsets = true };
        Children.Add(Strip);
    }

    // Gets the side of the split's panes the strip is on.
    internal DockSide Side { get; }

    // Gets or sets the split container that holds the strip, as the layout manager put it there: the
    // layout is answered from its own tree, not the visual one, which lags behind a move.
    internal SplitContainer? Owner { get; set; }

    // Gets the strip that lists the collapsed panels.
    internal AutoHideTabStrip Strip { get; }

    // Tells whether the strip is at an end of a split that runs in the given direction. A split
    // that was turned the other way after the strip was added has no end for it.
    internal bool BelongsTo(Orientation orientation) => AutoHideStubPlanner.AxisOf(Side) == orientation;

    // Tells whether the strip comes before the panes of its split rather than after them.
    internal bool IsLeading => Side is DockSide.Left or DockSide.Top;

    protected override Size MeasureOverride(Size availableSize)
    {
        Strip.Measure(availableSize);
        return Strip.DesiredSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        Strip.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
        return finalSize;
    }
}
