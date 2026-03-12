namespace FileExplorer.Forms;

public class TextViewerForm : Form
{
    private readonly string _filePath;
    private RichTextBox _textBox = null!;
    private ToolStripStatusLabel _lblInfo = null!;

    public TextViewerForm(string filePath)
    {
        _filePath = filePath;
        InitializeComponent();
        LoadFile();
    }

    private void InitializeComponent()
    {
        Text = $"Texto — {Path.GetFileName(_filePath)}";
        Size = new Size(900, 680);
        MinimumSize = new Size(500, 400);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(28, 28, 30);

        var toolbar = new ToolStrip
        {
            GripStyle = ToolStripGripStyle.Hidden,
            BackColor = Color.FromArgb(44, 44, 46)
        };

        // Sin PlaceholderText: no disponible en ToolStripTextBox de WinForms
        var txtSearch = new ToolStripTextBox { Width = 150 };
        var btnSearch = new ToolStripButton("Buscar");
        var btnWrap = new ToolStripButton("Ajuste de línea");

        btnSearch.Click += (_, _) => HighlightText(txtSearch.Text);
        btnWrap.Click += (_, _) => { _textBox.WordWrap = !_textBox.WordWrap; };

        toolbar.Items.AddRange(new ToolStripItem[]
        {
            new ToolStripLabel("Buscar:"),
            txtSearch, btnSearch,
            new ToolStripSeparator(),
            btnWrap
        });

        _textBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            WordWrap = false,
            ScrollBars = RichTextBoxScrollBars.Both,
            BackColor = Color.FromArgb(28, 28, 30),
            ForeColor = Color.FromArgb(220, 220, 200),
            Font = new Font("Consolas", 10f),
            BorderStyle = BorderStyle.None
        };

        var status = new StatusStrip { BackColor = Color.FromArgb(36, 36, 38) };
        _lblInfo = new ToolStripStatusLabel
        {
            Spring = true,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(174, 174, 178)
        };
        status.Items.Add(_lblInfo);

        Controls.AddRange(new Control[] { _textBox, toolbar, status });
    }

    private void LoadFile()
    {
        try
        {
            string content = File.ReadAllText(_filePath);
            _textBox.Text = content;

            string ext = Path.GetExtension(_filePath).ToLowerInvariant();

            // RichTextBox no tiene BeginUpdate/EndUpdate, usamos SuspendLayout
            SuspendLayout();
            switch (ext)
            {
                case ".json":
                    ColorizeJson();
                    break;
                case ".xml":
                case ".html":
                case ".htm":
                    ColorizeXml();
                    break;
                case ".cs":
                case ".js":
                case ".py":
                    ColorizeCode();
                    break;
            }
            ResumeLayout();

            _lblInfo.Text = $"Líneas: {_textBox.Lines.Length}   Caracteres: {_textBox.TextLength}";
        }
        catch (Exception ex)
        {
            _textBox.Text = $"Error al cargar el archivo:\n{ex.Message}";
            _textBox.ForeColor = Color.FromArgb(255, 69, 58);
        }
    }

    private void ColorizeJson()
    {
        HighlightPattern("\"[^\"\\\\]*(?:\\\\.[^\"\\\\]*)*\"",
            Color.FromArgb(206, 145, 120));
        HighlightPattern(@"\b\d+\.?\d*\b",
            Color.FromArgb(181, 206, 168));
        HighlightPattern(@"\b(true|false|null)\b",
            Color.FromArgb(86, 156, 214));
    }

    private void ColorizeXml()
    {
        HighlightPattern(@"<[^>]+>", Color.FromArgb(78, 201, 176));
        HighlightPattern("\"[^\"]*\"", Color.FromArgb(206, 145, 120));
        HighlightPattern(@"<!--.*?-->", Color.FromArgb(106, 153, 85));
    }

    private void ColorizeCode()
    {
        string[] keywords =
        {
            "using","namespace","class","public","private","protected",
            "static","void","int","string","bool","return","var","if","else",
            "for","foreach","while","new","null","true","false","async","await"
        };

        HighlightPattern(@"//.*$", Color.FromArgb(106, 153, 85), multiline: true);
        HighlightPattern("\"[^\"]*\"", Color.FromArgb(206, 145, 120));

        foreach (var kw in keywords)
            HighlightPattern($@"\b{kw}\b", Color.FromArgb(86, 156, 214));

        HighlightPattern(@"\b\d+\.?\d*\b", Color.FromArgb(181, 206, 168));
    }

    private void HighlightPattern(string pattern, Color color, bool multiline = false)
    {
        var opts = multiline
            ? System.Text.RegularExpressions.RegexOptions.Multiline
            : System.Text.RegularExpressions.RegexOptions.None;

        var regex = new System.Text.RegularExpressions.Regex(pattern, opts);

        foreach (System.Text.RegularExpressions.Match m in regex.Matches(_textBox.Text))
        {
            _textBox.Select(m.Index, m.Length);
            _textBox.SelectionColor = color;
        }
        _textBox.Select(0, 0);
    }

    private void HighlightText(string term)
    {
        if (string.IsNullOrWhiteSpace(term)) return;
        string text = _textBox.Text;
        int idx = 0;
        while (true)
        {
            idx = text.IndexOf(term, idx, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) break;
            _textBox.Select(idx, term.Length);
            _textBox.SelectionBackColor = Color.FromArgb(255, 200, 0);
            idx += term.Length;
        }
        _textBox.Select(0, 0);
    }
}