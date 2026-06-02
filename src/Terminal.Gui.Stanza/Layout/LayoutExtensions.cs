using Terminal.Gui;
using Terminal.Gui.ViewBase;

namespace Terminal.Gui.Stanza;

/// <summary>
/// C# 14 extension members for declarative layout DSL.
/// These properties are intercepted by the generator and wired into InitializeComponent().
/// </summary>
public static class LayoutExtensions
{
    extension(View view)
    {
        public int PositionX
        {
            get => 0; // Placeholder; wired by generator
            set { } // Placeholder; wired by generator
        }

        public int PositionY
        {
            get => 0; // Placeholder
            set { } // Placeholder
        }

        public int Width
        {
            get => 0; // Placeholder
            set { } // Placeholder
        }

        public int Height
        {
            get => 0; // Placeholder
            set { } // Placeholder
        }

        public string Below
        {
            get => string.Empty;
            set { }
        }
        public string RightOf
        {
            get => string.Empty;
            set { }
        }

        /// <summary>
        /// Align this view's right edge to another view's right edge.
        /// Creates a layout dependency: other → this.
        /// </summary>
        public View AlignRight(View other)
        {
            // Placeholder; wired by generator as view.X = Pos.Right(other)
            return view;
        }

        /// <summary>
        /// Position this view below another view.
        /// Creates a layout dependency: other → this.
        /// </summary>
        public View PositionBelow(View other)
        {
            // Placeholder; wired by generator as view.Y = Pos.Bottom(other)
            return view;
        }
    }
}
