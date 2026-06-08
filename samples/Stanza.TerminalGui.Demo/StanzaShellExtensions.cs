using Terminal.Gui.ViewBase;

public static class StanzaShellExtensions
{
    public static void ConfigureShell(this View parent, Action<ShellConfig> config)
    {
        var shell = new ShellConfig(parent);
        config(shell);
        shell.Apply();
    }

    public class ShellConfig
    {
        private readonly View _parent;
        // We now store Functions (Lambdas) instead of fixed ints
        private View? _h, _f, _s, _c;
        private Func<int> _hH = () => 0, _fH = () => 0, _sW = () => 0;

        public ShellConfig(View parent) => _parent = parent;

        public void Header(View view, Func<int> height) { _h = view; _hH = height; }
        public void Footer(View view, Func<int> height) { _f = view; _fH = height; }
        public void Sidebar(View view, Func<int> width) { _s = view; _sW = width; }
        public void Content(View view) { _c = view; }

        public void Apply()
        {
            // HEADER
            if (_h != null)
            {
                _h.X = 0; _h.Y = 0; _h.Width = Dim.Fill();
                _h.Height = Dim.Func(_ => _hH());
                _parent.Add(_h);
            }

            // FOOTER
            if (_f != null)
            {
                _f.X = 0;
                _f.Y = Pos.AnchorEnd() - Pos.Func(_ => _fH());
                _f.Width = Dim.Fill();
                _f.Height = Dim.Func(_ => _fH());
                _parent.Add(_f);
            }

            // SIDEBAR
            if (_s != null)
            {
                _s.X = 0;
                _s.Y = _h != null ? Pos.Bottom(_h) : 0;
                _s.Width = Dim.Func(_ => {
                    var w = _sW();
                    _s.Visible = w > 0; // Reactive Visibility
                    return w;
                });
                _s.Height = Dim.Fill(Dim.Func(_ => _fH()));
                _parent.Add(_s);
            }

            // CONTENT
            if (_c != null)
            {
                _c.X = _s != null ? Pos.Right(_s) : 0;
                _c.Y = _h != null ? Pos.Bottom(_h) : 0;
                _c.Width = Dim.Fill();
                _c.Height = Dim.Fill(Dim.Func(_ => _fH()));
                _parent.Add(_c);
            }
        }
    }
}

