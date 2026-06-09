using System.Diagnostics;
using Terminal.Gui.App;

namespace Stanza.TerminalGui.Layout;

public enum ScreenSize
{
    S,
    M,
    L,
    XL,
}

public record Breakpoint(int Columns, int Rows);
