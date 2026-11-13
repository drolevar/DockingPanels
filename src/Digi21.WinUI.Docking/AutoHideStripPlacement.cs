namespace Digi21.WinUI.Docking;

/// <summary>
/// Where the strip of tabs of an auto-hidden panel goes.
/// </summary>
public enum AutoHideStripPlacement
{
    /// <summary>
    /// The strip runs along an edge of the dock site, under (or beside) everything the layout
    /// holds, at the edge the panel was nearest to when it was collapsed. This is the default.
    /// </summary>
    Edge,

    /// <summary>
    /// The strip stays where the panel was: it takes its room from the area the panel came out of
    /// and from nothing else, so the panels beside that area keep their full length, and the panel
    /// slides out over that area only.
    /// </summary>
    /// <remarks>
    /// A panel that has no such place — it is in the middle of a split, or it is the only pane of
    /// its host — goes to an edge as with <see cref="Edge"/>, and so does one collapsed from a
    /// container that keeps other windows docked.
    /// </remarks>
    InPlace,
}
