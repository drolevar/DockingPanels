using Xunit;

namespace Digi21.WinUI.Docking.Tests;

public class SplitSizePlannerTests
{
    private const double Splitter = 6;

    [Fact]
    public void PanesShareWhatIsLeftAfterTheSplitters_ByRelativeSize()
    {
        var sizes = SplitSizePlanner.Compute(1000, Splitter, 0, [1, 3]);

        Assert.Equal(994 / 4.0, sizes[0], 6);
        Assert.Equal(994 * 3 / 4.0, sizes[1], 6);
    }

    [Fact]
    public void AReservedLengthIsNotTheirsToShare()
    {
        // The terminal column: the documents and a 28-pixel strip under them.
        var sizes = SplitSizePlanner.Compute(600, Splitter, 28, [1]);

        Assert.Equal(new[] { 572.0 }, sizes);
    }

    [Fact]
    public void PanesAndSplittersAndTheReservedLengthFillTheSplit()
    {
        var sizes = SplitSizePlanner.Compute(700, Splitter, 56, [1, 2, 5]);

        Assert.Equal(700, sizes.Sum() + 2 * Splitter + 56, 6);
    }

    [Fact]
    public void WithNothingReserved_TheResultIsWhatASplitAlwaysGave()
    {
        var sizes = SplitSizePlanner.Compute(500, Splitter, 0, [1, 1, 1]);

        Assert.All(sizes, size => Assert.Equal(488 / 3.0, size, 6));
    }

    [Fact]
    public void ASplitSmallerThanItsReservedLength_GivesThePanesNothing()
    {
        var sizes = SplitSizePlanner.Compute(20, Splitter, 28, [1, 1]);

        Assert.Equal(new[] { 0.0, 0.0 }, sizes);
    }

    [Fact]
    public void NoPanes_HaveNoSizes()
    {
        Assert.Empty(SplitSizePlanner.Compute(500, Splitter, 28, []));
    }
}
