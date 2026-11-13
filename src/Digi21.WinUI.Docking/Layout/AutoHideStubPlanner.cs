using Microsoft.UI.Xaml.Controls;

namespace Digi21.WinUI.Docking;

// What it takes to give an auto-hidden panel's strip a place in the layout (see
// AutoHideStripPlacement.InPlace), as a decision that needs no controls.
internal enum AutoHideStubPlan
{
    // There is no place for it: the strip goes to an edge of the dock site, as it always did.
    Edge,

    // The strip is added to the split the node is in, at the end the side names.
    InParent,

    // The node is wrapped in a new split of its own that holds the strip.
    WrapNode,
}

internal static class AutoHideStubPlanner
{
    // The direction a split has to run for a strip on the given side to sit at one of its ends:
    // a strip at the bottom is at the end of a stack, one at the right at the end of a row.
    internal static Orientation AxisOf(DockSide side)
    {
        return side is DockSide.Top or DockSide.Bottom ? Orientation.Vertical : Orientation.Horizontal;
    }

    // Decides where the strip of a collapsed panel goes, given the node it goes next to.
    //
    // The node is the pane that has just been collapsed, while it is still in its split, or, when a
    // layout is being loaded and the pane is long gone, the neighbour it sat beside. In both cases
    // the strip goes where the pane was seen from that node: on the given side of it.
    //
    // A strip is only ever at an end of its split. One between two panes would be a rail in the
    // middle of the layout, which is not where anyone looks for the tabs of a panel that came out
    // of an edge, so a node that is not at that end sends the strip to an edge instead.
    //
    // parentOrientation: The direction of the split that holds the node, or null when it is not in
    // one.
    //
    // index: The position of the node among the panes of that split.
    //
    // count: How many panes the split holds, the node included.
    //
    // side: The side of the node the strip goes on.
    //
    // canWrap: Whether the node may be wrapped in a split of its own to make room for the strip.
    // That is for a neighbour that stays in the layout; the pane being collapsed is leaving it,
    // and wrapping what is about to be taken out would only move it twice.
    internal static AutoHideStubPlan Plan(Orientation? parentOrientation, int index, int count, DockSide side, bool canWrap)
    {
        if (count <= 0 || index < 0 || index >= count)
        {
            return AutoHideStubPlan.Edge;
        }

        if (parentOrientation is { } orientation && orientation == AxisOf(side))
        {
            var leading = side is DockSide.Left or DockSide.Top;
            var atEnd = leading ? index == 0 : index == count - 1;
            if (!atEnd)
            {
                return AutoHideStubPlan.Edge;
            }

            // The pane being collapsed, alone in its split, leaves the strip with nothing beside it.
            return canWrap || count >= 2 ? AutoHideStubPlan.InParent : AutoHideStubPlan.Edge;
        }

        return canWrap ? AutoHideStubPlan.WrapNode : AutoHideStubPlan.Edge;
    }
}
