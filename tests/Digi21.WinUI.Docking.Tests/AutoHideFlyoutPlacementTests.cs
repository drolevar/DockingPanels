using Digi21.WinUI.Docking.Interaction;
using Xunit;

namespace Digi21.WinUI.Docking.Tests;

public class AutoHideFlyoutPlacementTests
{
    // A split of 800 x 600 at (200, 40), with a 28-pixel strip along one of its sides.
    private static readonly PlacementRect Split = new(200, 40, 800, 600);

    [Fact]
    public void PanelFromAStripAtTheBottom_CoversTheSplitDownToTheStrip()
    {
        var strip = new PlacementRect(200, 612, 800, 28);

        var area = AutoHideFlyoutPlacement.AreaBesideStrip(DockSide.Bottom, Split, strip);

        Assert.Equal(new PlacementRect(200, 40, 800, 572), area);
    }

    [Fact]
    public void PanelFromAStripAtTheTop_CoversTheSplitBelowTheStrip()
    {
        var strip = new PlacementRect(200, 40, 800, 28);

        var area = AutoHideFlyoutPlacement.AreaBesideStrip(DockSide.Top, Split, strip);

        Assert.Equal(new PlacementRect(200, 68, 800, 572), area);
    }

    [Fact]
    public void PanelFromAStripAtTheRight_CoversTheSplitLeftOfTheStrip()
    {
        var strip = new PlacementRect(972, 40, 28, 600);

        var area = AutoHideFlyoutPlacement.AreaBesideStrip(DockSide.Right, Split, strip);

        Assert.Equal(new PlacementRect(200, 40, 772, 600), area);
    }

    [Fact]
    public void PanelFromAStripAtTheLeft_CoversTheSplitRightOfTheStrip()
    {
        var strip = new PlacementRect(200, 40, 28, 600);

        var area = AutoHideFlyoutPlacement.AreaBesideStrip(DockSide.Left, Split, strip);

        Assert.Equal(new PlacementRect(228, 40, 772, 600), area);
    }

    [Fact]
    public void AStripLargerThanTheSplit_LeavesNoArea()
    {
        // A split squeezed to nothing must not give a negative size to a flyout.
        var strip = new PlacementRect(200, 20, 800, 28);

        var area = AutoHideFlyoutPlacement.AreaBesideStrip(DockSide.Bottom, Split, strip);

        Assert.Equal(0, area.Height);
    }

    [Theory]
    [InlineData(DockSide.Left, 200, 40, 300, 600)]
    [InlineData(DockSide.Right, 700, 40, 300, 600)]
    [InlineData(DockSide.Top, 200, 40, 800, 300)]
    [InlineData(DockSide.Bottom, 200, 340, 800, 300)]
    public void Place_PutsThePanelAgainstTheSideItComesOutOf(DockSide edge, double x, double y, double width, double height)
    {
        var placed = AutoHideFlyoutPlacement.Place(edge, Split, 300);

        Assert.Equal(new PlacementRect(x, y, width, height), placed);
    }

    [Fact]
    public void Place_KeepsAFifthOfTheAreaVisible()
    {
        // The same ceiling AutoHideResizePolicy gives: a panel never covers all of its area.
        var placed = AutoHideFlyoutPlacement.Place(DockSide.Bottom, Split, 10_000);

        Assert.Equal(480, placed.Height);
        Assert.Equal(160, placed.Y);
    }

    [Fact]
    public void Place_TakesTheAreaItIsGivenNotTheDockSite()
    {
        // The flyout of a panel under the terminal column is as wide as the column.
        var column = new PlacementRect(300, 0, 500, 400);

        var placed = AutoHideFlyoutPlacement.Place(DockSide.Bottom, column, 150);

        Assert.Equal(new PlacementRect(300, 250, 500, 150), placed);
    }
}
