using Terminal.Gui.ViewBase;

namespace Stanza.TerminalGui.Layout;

public class ScreenConfiguration
{
    private readonly View _container;

    public ScreenConfiguration(View container) => _container = container;

    public ScreenSize CurrentSize
    {
        get
        {
            var newSize = _container.Viewport.Width switch
            {
                < 60 => ScreenSize.S,
                < 100 => ScreenSize.M,
                < 140 => ScreenSize.L,
                _ => ScreenSize.XL,
            };

            return newSize;
        }
    }

    // Helper for "Mobile-first" logic
    public bool AtLeast(ScreenSize size) => CurrentSize >= size;
}
