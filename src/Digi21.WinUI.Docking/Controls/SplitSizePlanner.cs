namespace Digi21.WinUI.Docking;

// How a split container shares its length among its panes, as arithmetic that needs no controls.
internal static class SplitSizePlanner
{
    // Gives each pane its share of the length along the axis, by relative size.
    //
    // main: The length the split has along its axis.
    //
    // splitter: How much room a splitter takes between two panes.
    //
    // reserved: Length that is not the panes' to share: the strips of collapsed panels the split
    // holds, which have a size of their own.
    //
    // relativeSizes: The relative size of each pane, in order. Each is positive.
    internal static double[] Compute(double main, double splitter, double reserved, IReadOnlyList<double> relativeSizes)
    {
        var sizes = new double[relativeSizes.Count];
        if (sizes.Length == 0)
        {
            return sizes;
        }

        var available = Math.Max(0, main - reserved - (sizes.Length - 1) * splitter);

        double total = 0;
        foreach (var relative in relativeSizes)
        {
            total += relative;
        }

        for (var i = 0; i < sizes.Length; i++)
        {
            sizes[i] = available * relativeSizes[i] / total;
        }

        return sizes;
    }
}
