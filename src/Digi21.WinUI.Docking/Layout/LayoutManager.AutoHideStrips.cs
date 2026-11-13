using Digi21.WinUI.Docking.Primitives;
using Microsoft.UI.Xaml.Controls;

namespace Digi21.WinUI.Docking;

// The strips of collapsed panels that stay inside the layout (AutoHideStripPlacement.InPlace).
//
// A strip is a child of a split container, next to its panes and not one of them (see
// AutoHideStub), so what the layout looks like to everything that walks its panes is the same
// with or without it. What this file keeps is where strips go, and that a split that holds one is
// not collapsed while a single pane is left in it.
internal static partial class LayoutManager
{
    // Finds the strip for a container that is about to be collapsed, and makes it. It is made before
    // the container leaves its split, so the split is still there to hold it: taking the pane out
    // afterwards leaves a single pane and the strip, which is a split that is not collapsed.
    // Returns null when the pane has no place for a strip, which sends the strip to an edge.
    private static AutoHideStub? StubForCollapsedContainer(DockSite site, DockingWindowContainer container, DockSide edge)
    {
        if (site.AutoHideStripPlacement != AutoHideStripPlacement.InPlace
            || LayoutTree.Locate(site, container) is not { Split: { } split } location
            || !ReferenceEquals(location.Host, site))
        {
            return null;
        }

        var panes = split.GetPanes();
        var plan = AutoHideStubPlanner.Plan(split.Orientation, panes.IndexOf(container), panes.Count, edge, canWrap: false);

        return plan == AutoHideStubPlan.InParent ? StubOf(site, split, edge) : null;
    }

    // Gives the groups whose strip has not been placed a place for it. That is every group of a layout
    // that has just been loaded, which remembers where the panels were by the neighbours they sat
    // beside, and every group when the dock site is switched to keep its strips in place.
    internal static void PlaceAutoHideStubs(DockSite site)
    {
        foreach (var group in site.AutoHideGroups.ToList())
        {
            if (group.IsStripPlaced)
            {
                continue;
            }

            group.IsStripPlaced = true;

            // A layout that says where the tabs were is believed; one that does not leaves it to the
            // dock site.
            var placement = group.RequestedPlacement ?? site.AutoHideStripPlacement;
            if (placement == AutoHideStripPlacement.InPlace && group.Windows.Count > 0)
            {
                group.Stub = StubFromHint(site, group);
            }
        }

        site.RefreshAutoHideStrips();
    }

    // Puts every strip back at an edge, or in the layout if the dock site now keeps them there: the
    // dock site's placement has changed.
    internal static void ReplaceAutoHideStubs(DockSite site)
    {
        site.HideAutoHideFlyout();

        var stubs = site.AutoHideGroups.Select(group => group.Stub).OfType<AutoHideStub>().Distinct().ToList();
        // Changing the setting is asking for the strips to be placed by it, whatever a layout loaded
        // earlier said about the groups.
        foreach (var group in site.AutoHideGroups)
        {
            group.Stub = null;
            group.RequestedPlacement = null;
            group.IsStripPlaced = false;
        }

        foreach (var stub in stubs)
        {
            ReleaseStub(site, stub);
        }

        PlaceAutoHideStubs(site);
    }

    // Works out from where a group came from whether it has a place for its strip in the layout as it
    // stands, and makes it.
    private static AutoHideStub? StubFromHint(DockSite site, AutoHideGroup group)
    {
        if (group.RestoreHint is not { Sibling: { } sibling } hint || hint.Side != group.Edge)
        {
            return null;
        }

        // The container the group left is still in the layout: it kept other windows docked, and the
        // group came from one tab of it. There is nothing to put a strip beside.
        if (hint.Container is { } container && LayoutTree.Locate(site, container) is not null)
        {
            return null;
        }

        if (LayoutTree.Locate(site, sibling) is not { } location || !ReferenceEquals(location.Host, site))
        {
            return null;
        }

        var parent = location.Split;
        var panes = parent?.GetPanes();
        var plan = AutoHideStubPlanner.Plan(
            parent?.Orientation,
            panes?.IndexOf(sibling) ?? 0,
            panes?.Count ?? 1,
            group.Edge,
            canWrap: true);

        switch (plan)
        {
            case AutoHideStubPlan.InParent:
                return StubOf(site, parent!, group.Edge);

            case AutoHideStubPlan.WrapNode:
                // The neighbour is in a split that runs the other way, or in none: it gets one of its
                // own, which takes its share of the space and holds it and the strip.
                var wrapper = new SplitContainer { Orientation = AutoHideStubPlanner.AxisOf(group.Edge) };
                DockSite.SetRelativeSize(wrapper, SplitContainer.GetEffectiveRelativeSize(sibling));
                ReplaceInParent(site, sibling, wrapper);
                DockSite.SetRelativeSize(sibling, 1.0);
                InsertChild(site, wrapper, 0, sibling);
                return StubOf(site, wrapper, group.Edge);

            default:
                return null;
        }
    }

    // Gets the strip a split holds on a side, making it when there is none: panels collapsed from the
    // same end of a split are listed by the same strip, as they are along an edge.
    private static AutoHideStub StubOf(DockSite site, SplitContainer split, DockSide side)
    {
        if (split.FindStub(side) is { } existing)
        {
            return existing;
        }

        var stub = new AutoHideStub(side) { Owner = split };

        // A panel that is out is placed against its strip, which can move without the strip's own size
        // changing; the dock site finds out when it does.
        stub.SizeChanged += (_, _) => site.NotifyAutoHideStubResized(stub);
        split.Children.Add(stub);
        return stub;
    }

    // Takes a strip out of its split, once no group is listed by it, and collapses the split if it
    // only held on for the strip.
    internal static void ReleaseStub(DockSite site, AutoHideStub stub)
    {
        if (stub.Owner is not { } split)
        {
            return;
        }

        stub.Owner = null;
        split.Children.Remove(stub);
        CollapseSplit(site, split);
    }

    // Takes a strip out of its split without looking at what is left of it. For a layout that is
    // about to be rebuilt anyway.
    internal static void DiscardStub(AutoHideStub stub)
    {
        if (stub.Owner is { } split)
        {
            split.Children.Remove(stub);
        }

        stub.Owner = null;
    }

    // The last pane of a split that holds strips has gone: they have nowhere to be, so the groups they
    // listed go to the edges they were collapsed to.
    private static void ReturnStubsToEdges(DockSite site, SplitContainer split)
    {
        var stubs = split.GetStubs();
        foreach (var stub in stubs)
        {
            foreach (var group in site.AutoHideGroups.Where(group => ReferenceEquals(group.Stub, stub)))
            {
                group.Stub = null;
            }

            DiscardStub(stub);
        }

        site.HideAutoHideFlyout();
        site.RefreshAutoHideStrips();
    }
}
