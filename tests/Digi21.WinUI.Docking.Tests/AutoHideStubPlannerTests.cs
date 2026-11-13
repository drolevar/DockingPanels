using Microsoft.UI.Xaml.Controls;
using Xunit;

namespace Digi21.WinUI.Docking.Tests;

public class AutoHideStubPlannerTests
{
    [Theory]
    [InlineData(DockSide.Top, Orientation.Vertical)]
    [InlineData(DockSide.Bottom, Orientation.Vertical)]
    [InlineData(DockSide.Left, Orientation.Horizontal)]
    [InlineData(DockSide.Right, Orientation.Horizontal)]
    public void AStripIsAtTheEndOfASplitThatRunsTheWayItsSideFaces(DockSide side, Orientation axis)
    {
        Assert.Equal(axis, AutoHideStubPlanner.AxisOf(side));
    }

    [Theory]
    [InlineData(Orientation.Vertical, 1, 2, DockSide.Bottom)]
    [InlineData(Orientation.Vertical, 0, 2, DockSide.Top)]
    [InlineData(Orientation.Horizontal, 2, 3, DockSide.Right)]
    [InlineData(Orientation.Horizontal, 0, 3, DockSide.Left)]
    public void ACollapsedPaneAtAnEndOfItsSplit_KeepsItsStripThere(Orientation split, int index, int count, DockSide side)
    {
        Assert.Equal(AutoHideStubPlan.InParent, AutoHideStubPlanner.Plan(split, index, count, side, canWrap: false));
    }

    [Theory]
    [InlineData(Orientation.Vertical, 1, 3, DockSide.Bottom)]
    [InlineData(Orientation.Horizontal, 1, 3, DockSide.Left)]
    [InlineData(Orientation.Horizontal, 1, 3, DockSide.Right)]
    public void ACollapsedPaneInTheMiddleOfItsSplit_GoesToAnEdge(Orientation split, int index, int count, DockSide side)
    {
        // A strip between two panes would be a rail in the middle of the layout.
        Assert.Equal(AutoHideStubPlan.Edge, AutoHideStubPlanner.Plan(split, index, count, side, canWrap: false));
        Assert.Equal(AutoHideStubPlan.Edge, AutoHideStubPlanner.Plan(split, index, count, side, canWrap: true));
    }

    [Fact]
    public void ACollapsedPaneAloneInItsSplit_GoesToAnEdge()
    {
        // Nothing would be left beside the strip.
        Assert.Equal(AutoHideStubPlan.Edge, AutoHideStubPlanner.Plan(Orientation.Vertical, 0, 1, DockSide.Bottom, canWrap: false));
    }

    [Fact]
    public void ACollapsedPaneWithNoSplit_GoesToAnEdge()
    {
        Assert.Equal(AutoHideStubPlan.Edge, AutoHideStubPlanner.Plan(null, 0, 1, DockSide.Bottom, canWrap: false));
    }

    [Fact]
    public void ACollapsedPaneInASplitThatRunsTheOtherWay_GoesToAnEdge()
    {
        // The pane is last in a row, but its strip belongs under it, not at the end of the row; the
        // pane is leaving, so there is no point wrapping it.
        Assert.Equal(AutoHideStubPlan.Edge, AutoHideStubPlanner.Plan(Orientation.Horizontal, 2, 3, DockSide.Bottom, canWrap: false));
    }

    [Fact]
    public void ANeighbourInASplitThatRunsTheOtherWay_IsWrapped()
    {
        // [Sessions | Terminals | Details] with a panel that was below the terminals: the strip
        // goes under them, in a split of their own.
        Assert.Equal(AutoHideStubPlan.WrapNode, AutoHideStubPlanner.Plan(Orientation.Horizontal, 1, 3, DockSide.Bottom, canWrap: true));
    }

    [Fact]
    public void ANeighbourWithNoSplit_IsWrapped()
    {
        Assert.Equal(AutoHideStubPlan.WrapNode, AutoHideStubPlanner.Plan(null, 0, 1, DockSide.Bottom, canWrap: true));
    }

    [Fact]
    public void ANeighbourAloneInASplitThatRunsTheRightWay_TakesTheStripInThatSplit()
    {
        // A split saved with the strip's neighbour as its only pane, which is what a layout that
        // kept its split looks like when it is loaded.
        Assert.Equal(AutoHideStubPlan.InParent, AutoHideStubPlanner.Plan(Orientation.Vertical, 0, 1, DockSide.Bottom, canWrap: true));
    }

    [Theory]
    [InlineData(-1, 2)]
    [InlineData(2, 2)]
    [InlineData(0, 0)]
    public void ANodeThatIsNotAmongThePanes_GoesToAnEdge(int index, int count)
    {
        Assert.Equal(AutoHideStubPlan.Edge, AutoHideStubPlanner.Plan(Orientation.Vertical, index, count, DockSide.Bottom, canWrap: true));
    }
}
