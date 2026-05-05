
namespace FileExplorer.Forms;


/// <summary>
/// Selector de archivos de audio interno al programa.
/// Navega carpetas con un TreeView y selecciona canciones con un ListView.
/// </summary>
public partial class SongPickerForm : Form
{
    public List<string> SelectedFiles { get; } = new();

    private readonly TreeView _tree = new();
    private readonly ListView _files = new();
    private readonly TextBox _txtPath = new();
    private string _currentPath;

    private static readonly HashSet<string> AudioExts =
        new(StringComparer.OrdinalIgnoreCase)
        { ".mp3", ".flac", ".ogg", ".wav", ".aac", ".m4a", ".wma", ".opus" };

    public SongPickerForm(string startFolder)
    {
        _currentPath = startFolder;
        Text = "Seleccionar canciones";
        Size = new Size(700, 520);
        MinimumSize = new Size(600, 400);
        BackColor = Color.FromArgb(24, 24, 32);
        ForeColor = Color.White;
        FormBorderStyle = FormBorderStyle.Sizable;
        StartPosition = FormStartPosition.CenterParent;

        BuildUI();
        LoadTree();
        LoadFiles(_currentPath);
    }

    private void BuildUI()
    {
        // ── Barra de ruta ─────────────────────────────────────────────────────
        _txtPath.Location = new Point(8, 8);
        _txtPath.Size = new Size(580, 24);
        _txtPath.BackColor = Color.FromArgb(32, 32, 44);
        _txtPath.ForeColor = Color.White;
        _txtPath.BorderStyle = BorderStyle.FixedSingle;
        _txtPath.Text = _currentPath;
        _txtPath.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter && Directory.Exists(_txtPath.Text))
                LoadFiles(_txtPath.Text);
        };

        var btnGo = new Button
        {
            Text = "→",
            Location = new Point(596, 8),
            Size = new Size(32, 24),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(50, 50, 70),
            ForeColor = Color.White
        };
        btnGo.Click += (_, _) =>
        {
            if (Directory.Exists(_txtPath.Text)) LoadFiles(_txtPath.Text);
        };

        var btnUp = new Button
        {
            Text = "▲",
            Location = new Point(636, 8),
            Size = new Size(32, 24),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(50, 50, 70),
            ForeColor = Color.White
        };
        btnUp.Click += (_, _) =>
        {
            string? parent = Directory.GetParent(_currentPath)?.FullName;
            if (parent != null) LoadFiles(parent);
        };

        // ── Tree de carpetas ──────────────────────────────────────────────────
        _tree.Location = new Point(8, 40);
        _tree.Size = new Size(200, 380);
        _tree.BackColor = Color.FromArgb(32, 32, 44);
        _tree.ForeColor = Color.White;
        _tree.BorderStyle = BorderStyle.None;
        _tree.AfterSelect += (_, e) =>
        {
            if (e.Node?.Tag is string path) LoadFiles(path);
        };

        // ── Lista de archivos ─────────────────────────────────────────────────
        _files.Location = new Point(216, 40);
        _files.Size = new Size(464, 380);
        _files.View = View.Details;
        _files.FullRowSelect = true;
        _files.BackColor = Color.FromArgb(32, 32, 44);
        _files.ForeColor = Color.White;
        _files.BorderStyle = BorderStyle.None;
        _files.CheckBoxes = true;
        _files.Columns.Add("Nombre", 300);
        _files.Columns.Add("Tipo", 80);
        _files.Columns.Add("Tamaño", 70);
        _files.DoubleClick += (_, _) =>
        {
            // Doble clic en carpeta: navegar
            if (_files.SelectedItems.Count == 0) return;
            string? tag = _files.SelectedItems[0].Tag as string;
            if (tag != null && Directory.Exists(tag)) LoadFiles(tag);
        };

        // ── Botones ───────────────────────────────────────────────────────────
        var btnSelectAll = new Button
        {
            Text = "✔ Seleccionar todo",
            Location = new Point(8, 428),
            Size = new Size(160, 28),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(50, 50, 70),
            ForeColor = Color.White
        };
        btnSelectAll.Click += (_, _) =>
        {
            foreach (ListViewItem item in _files.Items)
                if (item.Tag is string p && System.IO.File.Exists(p))
                    item.Checked = true;
        };

        var btnOk = new Button
        {
            Text = "✅ Agregar seleccionadas",
            Location = new Point(390, 428),
            Size = new Size(180, 28),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(60, 120, 60),
            ForeColor = Color.White,
            DialogResult = DialogResult.OK
        };
        btnOk.Click += (_, _) =>
        {
            SelectedFiles.AddRange(
                _files.CheckedItems.Cast<ListViewItem>()
                    .Select(i => i.Tag as string)
                    .Where(p => p != null && System.IO.File.Exists(p))
                    .Select(p => p!));
            Close();
        };

        var btnCancel = new Button
        {
            Text = "Cancelar",
            Location = new Point(580, 428),
            Size = new Size(96, 28),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(80, 40, 40),
            ForeColor = Color.White,
            DialogResult = DialogResult.Cancel
        };

        Controls.AddRange(new Control[]
        {
            _txtPath, btnGo, btnUp,
            _tree, _files,
            btnSelectAll, btnOk, btnCancel
        });
    }

    private void LoadTree()
    {
        _tree.Nodes.Clear();
        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
        {
            var node = new TreeNode($"💾 {drive.Name}") { Tag = drive.RootDirectory.FullName };
            node.Nodes.Add(new TreeNode("...")); // lazy load
            _tree.Nodes.Add(node);
        }
        _tree.BeforeExpand += (_, e) =>
      
        {
            if (e.Node.Nodes.Count == 1 && e.Node.Nodes[0].Text == "...")
            {
                e.Node.Nodes.Clear();
                string nodePath = (e.Node.Tag as string) ?? string.Empty;
                if (string.IsNullOrEmpty(nodePath)) return;
                try
                {
                    foreach (var dir in Directory.GetDirectories(nodePath))
                    {
                        var child = new TreeNode("📁 " + Path.GetFileName(dir)) { Tag = dir };
                        child.Nodes.Add(new TreeNode("..."));
                        e.Node.Nodes.Add(child);
                    }
                }
                catch { }
            }
        };
    }

    private void LoadFiles(string path)
    {
        if (!Directory.Exists(path)) return;
        _currentPath = path;
        _txtPath.Text = path;
        _files.Items.Clear();

        // Subcarpetas primero
        try
        {
            foreach (var dir in Directory.GetDirectories(path))
            {
                var item = new ListViewItem("📁 " + Path.GetFileName(dir));
                item.SubItems.Add("Carpeta");
                item.SubItems.Add("—");
                item.Tag = dir;
                item.ForeColor = Color.FromArgb(180, 180, 220);
                _files.Items.Add(item);
            }
        }
        catch { }

        // Archivos de audio
        try
        {
            foreach (var file in Directory.GetFiles(path)
                         .Where(f => AudioExts.Contains(Path.GetExtension(f)))
                         .OrderBy(f => f))
            {
                var info = new FileInfo(file);
                var item = new ListViewItem("🎵 " + info.Name);
                item.SubItems.Add(info.Extension.ToUpperInvariant().TrimStart('.'));
                item.SubItems.Add(FormatBytes(info.Length));
                item.Tag = file;
                _files.Items.Add(item);
            }
        }
        catch { }
    }

    private static string FormatBytes(long b) =>
        b >= 1_048_576 ? $"{b / 1_048_576.0:F1} MB" : $"{b / 1024} KB";
}