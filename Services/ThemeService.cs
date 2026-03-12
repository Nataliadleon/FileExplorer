namespace FileExplorer.Services;

public class ThemeService
{
    public static readonly Color DarkBackground = Color.FromArgb(28, 28, 30);
    public static readonly Color DarkSurface = Color.FromArgb(44, 44, 46);
    public static readonly Color DarkBorder = Color.FromArgb(58, 58, 60);
    public static readonly Color DarkForeground = Color.FromArgb(235, 235, 245);
    public static readonly Color DarkSecondary = Color.FromArgb(174, 174, 178);
    public static readonly Color DarkAccent = Color.FromArgb(10, 132, 255);
    public static readonly Color DarkToolbar = Color.FromArgb(36, 36, 38);

    public static readonly Color LightBackground = Color.FromArgb(246, 246, 248);
    public static readonly Color LightSurface = Color.FromArgb(255, 255, 255);
    public static readonly Color LightBorder = Color.FromArgb(210, 210, 215);
    public static readonly Color LightForeground = Color.FromArgb(29, 29, 31);
    public static readonly Color LightSecondary = Color.FromArgb(100, 100, 105);
    public static readonly Color LightAccent = Color.FromArgb(0, 120, 215);
    public static readonly Color LightToolbar = Color.FromArgb(240, 240, 242);

    public Color Background { get; private set; }
    public Color Surface { get; private set; }
    public Color Border { get; private set; }
    public Color Foreground { get; private set; }
    public Color Secondary { get; private set; }
    public Color Accent { get; private set; }
    public Color Toolbar { get; private set; }
    public bool IsDarkMode { get; private set; }

    public ThemeService(bool darkMode = false) => Apply(darkMode);

    public void Apply(bool darkMode)
    {
        IsDarkMode = darkMode;
        Background = darkMode ? DarkBackground : LightBackground;
        Surface = darkMode ? DarkSurface : LightSurface;
        Border = darkMode ? DarkBorder : LightBorder;
        Foreground = darkMode ? DarkForeground : LightForeground;
        Secondary = darkMode ? DarkSecondary : LightSecondary;
        Accent = darkMode ? DarkAccent : LightAccent;
        Toolbar = darkMode ? DarkToolbar : LightToolbar;
    }

    public void ApplyToForm(Form form)
    {
        form.BackColor = Background;
        form.ForeColor = Foreground;
        ApplyToControls(form.Controls);
    }

    public void ApplyToControls(Control.ControlCollection controls)
    {
        foreach (Control ctrl in controls)
        {
            StyleControl(ctrl);
            if (ctrl.HasChildren)
                ApplyToControls(ctrl.Controls);
        }
    }

    // Usando if/else en lugar de switch para evitar error CS8120
    private void StyleControl(Control ctrl)
    {
        ctrl.ForeColor = Foreground;

        if (ctrl is MenuStrip ms)
        {
            ms.BackColor = Toolbar;
            StyleMenuItems(ms.Items);
        }
        else if (ctrl is StatusStrip ss)
        {
            ss.BackColor = Surface;
            StyleToolStripItems(ss.Items);
        }
        else if (ctrl is ToolStrip ts)
        {
            ts.BackColor = Toolbar;
            StyleToolStripItems(ts.Items);
        }
        else if (ctrl is TreeView tv)
        {
            tv.BackColor = Surface;
            tv.LineColor = Border;
        }
        else if (ctrl is ListView lv)
        {
            lv.BackColor = Surface;
        }
        else if (ctrl is RichTextBox rtb)
        {
            rtb.BackColor = Surface;
        }
        else if (ctrl is TextBox tb)
        {
            tb.BackColor = Surface;
        }
        else if (ctrl is Button btn)
        {
            btn.BackColor = Surface;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderColor = Border;
        }
        else if (ctrl is ComboBox cb)
        {
            cb.BackColor = Surface;
        }
        else if (ctrl is Label lbl)
        {
            lbl.BackColor = Color.Transparent;
        }
        else
        {
            ctrl.BackColor = Background;
        }
    }

    private void StyleMenuItems(ToolStripItemCollection items)
    {
        foreach (ToolStripItem item in items)
        {
            item.BackColor = Toolbar;
            item.ForeColor = Foreground;
            if (item is ToolStripMenuItem mi)
                StyleMenuItems(mi.DropDownItems);
        }
    }

    private void StyleToolStripItems(ToolStripItemCollection items)
    {
        foreach (ToolStripItem item in items)
        {
            item.BackColor = Toolbar;
            item.ForeColor = Foreground;
        }
    }
}