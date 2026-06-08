using System.Diagnostics;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;

namespace Stanza.TerminalGui.Layout;

public enum ScreenSize
{
    S,
    M,
    L,
    XL,
}

public record Breakpoint(int Columns, int Rows);

public class ScreenConfiguration
{
    private readonly View _container;

    public ScreenConfiguration(View container) => _container = container;

    public ScreenSize CurrentSize => _container.Viewport.Width switch
    {
        < 60 => ScreenSize.S,
        < 100 => ScreenSize.M,
        < 140 => ScreenSize.L,
        _ => ScreenSize.XL,
    };

    // Helper for "Mobile-first" logic
    public bool AtLeast(ScreenSize size) => CurrentSize >= size;
}