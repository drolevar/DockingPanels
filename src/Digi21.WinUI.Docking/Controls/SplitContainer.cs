using Digi21.WinUI.Docking.Primitives;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Digi21.WinUI.Docking;

/// <summary>
/// A panel that arranges its children side by side (or stacked) with draggable splitters
/// between them. The share of space each child receives is controlled by the
/// <see cref="DockSite.RelativeSizeProperty"/> attached property.
/// </summary>
public partial class SplitContainer : Panel
{
    internal const double MinPaneSize = 48.0;

    private static double? splitterThickness;

    // How much room a splitter takes between two panes. A splitter is sized by the panel that
    // arranges it rather than by a template of its own, so the value is read from the resources
    // (DockingSplitterThickness) instead of being fixed here. It is resolved once, on the first
    // layout pass, which is long after the application's resources are in place.
    internal static double SplitterThickness =>
        splitterThickness ??= DockingThemeResources.Value("DockingSplitterThickness", 6.0);

    /// <summary>Identifies the <see cref="Orientation"/> dependency property.</summary>
    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
        nameof(Orientation),
        typeof(Orientation),
        typeof(SplitContainer),
        new PropertyMetadata(Orientation.Horizontal, (d, _) => ((SplitContainer)d).OnOrientationChanged()));

    /// <summary>
    /// Gets or sets the direction in which children are laid out: <see cref="Orientation.Horizontal"/>
    /// places them side by side, <see cref="Orientation.Vertical"/> stacks them top to bottom.
    /// </summary>
    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    // Gets the children that take part in the split layout, excluding the generated splitters and
    // the strips of collapsed panels, which are part of how the split is drawn and not panes of it.
    internal List<UIElement> GetPanes()
    {
        var panes = new List<UIElement>(Children.Count);
        foreach (var child in Children)
        {
            if (!IsDecoration(child))
            {
                panes.Add(child);
            }
        }

        return panes;
    }

    private static bool IsDecoration(UIElement child) => child is DockSplitter or AutoHideStub;

    // Gets the strips of collapsed panels this split holds.
    internal List<AutoHideStub> GetStubs() => [.. Children.OfType<AutoHideStub>()];

    // Tells whether the split holds the strip of a collapsed panel. Such a split stays even when a
    // single pane is left in it, because the strip needs it.
    internal bool HasStubs => Children.OfType<AutoHideStub>().Any();

    // Gets the strip this split holds on the given side, if any.
    internal AutoHideStub? FindStub(DockSide side) => Children.OfType<AutoHideStub>().FirstOrDefault(stub => stub.Side == side);

    internal static double GetEffectiveRelativeSize(UIElement element)
    {
        var value = DockSite.GetRelativeSize(element);
        return double.IsFinite(value) && value > 0 ? value : 1.0;
    }

    private void OnOrientationChanged()
    {
        foreach (var child in Children)
        {
            if (child is DockSplitter splitter)
            {
                splitter.Orientation = Orientation;
            }
        }

        InvalidateMeasure();
    }

    // Keeps exactly one generated DockSplitter per pair of adjacent panes. Splitters are appended
    // at the end of the children collection (so they stay on top) and are positioned between panes
    // during arrange.
    private void ReconcileSplitters()
    {
        var paneCount = 0;
        foreach (var child in Children)
        {
            if (!IsDecoration(child))
            {
                paneCount++;
            }
        }

        var desired = Math.Max(0, paneCount - 1);
        var splitters = Children.OfType<DockSplitter>().ToList();

        for (var i = splitters.Count - 1; i >= desired; i--)
        {
            Children.Remove(splitters[i]);
        }

        for (var i = splitters.Count; i < desired; i++)
        {
            Children.Add(new DockSplitter());
        }

        var index = 0;
        foreach (var child in Children)
        {
            if (child is DockSplitter splitter)
            {
                splitter.Owner = this;
                splitter.Index = index++;
                splitter.Orientation = Orientation;
            }
        }
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        ReconcileSplitters();

        var panes = GetPanes();
        if (panes.Count == 0)
        {
            return new Size(0, 0);
        }

        var horizontal = Orientation == Orientation.Horizontal;
        var splitterCount = panes.Count - 1;
        var main = horizontal ? availableSize.Width : availableSize.Height;
        var cross = horizontal ? availableSize.Height : availableSize.Width;

        foreach (var child in Children)
        {
            if (child is DockSplitter splitter)
            {
                splitter.Measure(horizontal
                    ? new Size(SplitterThickness, double.IsInfinity(cross) ? 0 : cross)
                    : new Size(double.IsInfinity(cross) ? 0 : cross, SplitterThickness));
            }
        }

        // The strips of collapsed panels have a thickness of their own, which the panes do not share.
        var reserved = MeasureStubs(horizontal, cross);

        double desiredMain;
        double maxChildCross = 0;

        if (double.IsInfinity(main))
        {
            // Unconstrained along the split axis: fall back to the sum of desired sizes.
            double sum = 0;
            foreach (var pane in panes)
            {
                pane.Measure(horizontal ? new Size(double.PositiveInfinity, cross) : new Size(cross, double.PositiveInfinity));
                sum += horizontal ? pane.DesiredSize.Width : pane.DesiredSize.Height;
                maxChildCross = Math.Max(maxChildCross, horizontal ? pane.DesiredSize.Height : pane.DesiredSize.Width);
            }

            desiredMain = sum + splitterCount * SplitterThickness + reserved;
        }
        else
        {
            var sizes = ComputePaneSizes(panes, main, reserved);
            for (var i = 0; i < panes.Count; i++)
            {
                panes[i].Measure(horizontal ? new Size(sizes[i], cross) : new Size(cross, sizes[i]));
                maxChildCross = Math.Max(maxChildCross, horizontal ? panes[i].DesiredSize.Height : panes[i].DesiredSize.Width);
            }

            desiredMain = main;
        }

        var desiredCross = double.IsInfinity(cross) ? maxChildCross : cross;
        return horizontal ? new Size(desiredMain, desiredCross) : new Size(desiredCross, desiredMain);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        var panes = GetPanes();
        if (panes.Count == 0)
        {
            return finalSize;
        }

        var horizontal = Orientation == Orientation.Horizontal;
        var main = horizontal ? finalSize.Width : finalSize.Height;
        var cross = horizontal ? finalSize.Height : finalSize.Width;
        var stubs = GetStubs();
        var reserved = stubs.Where(stub => stub.BelongsTo(Orientation)).Sum(stub => Thickness(stub, horizontal));
        var sizes = ComputePaneSizes(panes, main, reserved);
        var splitters = Children.OfType<DockSplitter>().ToList();

        double offset = 0;

        // A strip at the start of the split comes before the panes, one at the end after them.
        foreach (var stub in stubs)
        {
            if (!stub.BelongsTo(Orientation))
            {
                stub.Arrange(new Rect(0, 0, 0, 0));
            }
            else if (stub.IsLeading)
            {
                var thickness = Thickness(stub, horizontal);
                stub.Arrange(horizontal ? new Rect(offset, 0, thickness, cross) : new Rect(0, offset, cross, thickness));
                offset += thickness;
            }
        }

        for (var i = 0; i < panes.Count; i++)
        {
            panes[i].Arrange(horizontal
                ? new Rect(offset, 0, sizes[i], cross)
                : new Rect(0, offset, cross, sizes[i]));
            offset += sizes[i];

            if (i < splitters.Count)
            {
                splitters[i].Arrange(horizontal
                    ? new Rect(offset, 0, SplitterThickness, cross)
                    : new Rect(0, offset, cross, SplitterThickness));
                offset += SplitterThickness;
            }
        }

        foreach (var stub in stubs)
        {
            if (stub.BelongsTo(Orientation) && !stub.IsLeading)
            {
                var thickness = Thickness(stub, horizontal);
                stub.Arrange(horizontal ? new Rect(offset, 0, thickness, cross) : new Rect(0, offset, cross, thickness));
                offset += thickness;
            }
        }

        return finalSize;
    }

    // Measures the strips of collapsed panels and returns how much of the split's length they take.
    // A strip is as thick as its tabs ask along the axis and as long as the split across it.
    private double MeasureStubs(bool horizontal, double cross)
    {
        double reserved = 0;
        foreach (var stub in GetStubs())
        {
            if (!stub.BelongsTo(Orientation))
            {
                stub.Measure(new Size(0, 0));
                continue;
            }

            stub.Measure(horizontal ? new Size(double.PositiveInfinity, cross) : new Size(cross, double.PositiveInfinity));
            reserved += Thickness(stub, horizontal);
        }

        return reserved;
    }

    private static double Thickness(AutoHideStub stub, bool horizontal)
    {
        return horizontal ? stub.DesiredSize.Width : stub.DesiredSize.Height;
    }

    // Distributes the available main-axis size among panes proportionally to their relative sizes,
    // less the room the strips of collapsed panels take.
    private static double[] ComputePaneSizes(List<UIElement> panes, double mainSize, double reserved)
    {
        var relativeSizes = new double[panes.Count];
        for (var i = 0; i < panes.Count; i++)
        {
            relativeSizes[i] = GetEffectiveRelativeSize(panes[i]);
        }

        return SplitSizePlanner.Compute(mainSize, SplitterThickness, reserved, relativeSizes);
    }
}
