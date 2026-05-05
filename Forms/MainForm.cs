using FileExplorer.Helpers;
using FileExplorer.Models;
using FileExplorer.Services;

namespace FileExplorer.Forms;

public class MainForm : Form
{
    // ─── Servicios ────────────────────────────────────────────────────────────
    private readonly FileSystemService _fsService = new();
    private readonly FileOpenerService _openerService = new();
    private readonly SearchService _searchService = new();
    private readonly ThemeService _themeService = new();
    private readonly AppSettings _settings = new();

    // ─── Navegación ───────────────────────────────────────────────────────────
    private readonly Stack<string> _backHistory = new();
    private readonly Stack<string> _forwardHistory = new();
    private string _currentPath = string.Empty;
    private ListViewItem? _hoveredItem;

    // ─── ImageLists ───────────────────────────────────────────────────────────
    private ImageList _smallIcons = null!;
    private ImageList _largeIcons = null!;

    // ─── Controles principales ────────────────────────────────────────────────
    private MenuStrip _menuStrip = null!;
    private ToolStrip _toolStrip = null!;
    private StatusStrip _statusStrip = null!;
    private SplitContainer _mainSplit = null!;
    private SplitContainer _rightSplit = null!;
    private TreeView _treeView = null!;
    private ListView _listView = null!;
    private Panel _previewPanel = null!;
    private ToolTip _toolTip = null!;

    // ─── Toolbar ─────────────────────────────────────────────────────────────
    private ToolStripButton _btnBack = null!;
    private ToolStripButton _btnForward = null!;
    private ToolStripButton _btnUp = null!;
    private ToolStripTextBox _txtAddress = null!;
    private ToolStripTextBox _txtSearch = null!;
    private ToolStripButton _btnGrid = null!;
    private ToolStripButton _btnList = null!;
    private ToolStripComboBox _cmbSort = null!;
    private ToolStripComboBox _cmbFilter = null!;

    // ─── Status ───────────────────────────────────────────────────────────────
    private ToolStripStatusLabel _lblStatus = null!;
    private ToolStripStatusLabel _lblSelected = null!;
    private ToolStripStatusLabel _lblFreeSpace = null!;

    // ─── Preview ─────────────────────────────────────────────────────────────
    private PictureBox _previewImage = null!;
    private RichTextBox _previewText = null!;
    private Label _previewLabel = null!;

    // ─── Constructor ─────────────────────────────────────────────────────────

    public MainForm(string? startPath = null)
    {
        _smallIcons = IconHelper.BuildSmallImageList();
        _largeIcons = IconHelper.BuildLargeImageList();
        InitializeComponent();
        InitializeTreeView();

        // Arrancar en la ruta del usuario actual
        string path = startPath
            ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!Directory.Exists(path))
            path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        NavigateTo(path);

        // Mostrar usuario en el título
        string userName = Environment.UserName;
        Text = $"Explorador — {userName}";
    }

    // ─── Inicialización ───────────────────────────────────────────────────────

    private void InitializeComponent()
    {
        SuspendLayout();
        Text = "Explorador de Archivos Avanzado";
        Size = new Size(1300, 800);
        MinimumSize = new Size(900, 600);
        StartPosition = FormStartPosition.CenterScreen;

        _toolTip = new ToolTip { AutoPopDelay = 8000, InitialDelay = 600 };

        BuildMenuStrip();
        BuildToolStrip();

        // SplitterDistance se quita del constructor para evitar InvalidOperationException
        _mainSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterWidth = 4,
            BackColor = Color.FromArgb(58, 58, 60)
        };

        BuildTreeView();

        _rightSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 4,
            BackColor = Color.FromArgb(58, 58, 60)
        };

        BuildListView();
        BuildPreviewPanel();
        BuildStatusStrip();

        _mainSplit.Panel1.Controls.Add(_treeView);
        _rightSplit.Panel1.Controls.Add(_listView);
        _rightSplit.Panel2.Controls.Add(_previewPanel);
        _mainSplit.Panel2.Controls.Add(_rightSplit);

        Controls.AddRange(new Control[] { _mainSplit, _toolStrip, _menuStrip, _statusStrip });
        MainMenuStrip = _menuStrip;
        ApplyTheme();

        // Se asignan después de que el form tiene tamaño real
        Load += (_, _) =>
        {
            _mainSplit.Panel1MinSize = 160;
            _mainSplit.Panel2MinSize = 200;
            _mainSplit.SplitterDistance = 230;

            _rightSplit.Panel1MinSize = 200;
            _rightSplit.Panel2MinSize = 180;
            _rightSplit.SplitterDistance = 550;
        };

        ResumeLayout(true);

        ResumeLayout(true);
    }

    // ─── MenuStrip ────────────────────────────────────────────────────────────

    private void BuildMenuStrip()
    {
        _menuStrip = new MenuStrip { RenderMode = ToolStripRenderMode.System };

        var mnuArchivo = new ToolStripMenuItem("Archivo");
        AddMenuItem(mnuArchivo, "📁  Nueva carpeta", Keys.Control | Keys.N, (_, _) => CreateNewFolder());
        mnuArchivo.DropDownItems.Add(new ToolStripSeparator());
        AddMenuItem(mnuArchivo, "🗑  Eliminar", Keys.Delete, (_, _) => DeleteSelected());
        AddMenuItem(mnuArchivo, "✏  Renombrar", Keys.F2, (_, _) => RenameSelected());
        mnuArchivo.DropDownItems.Add(new ToolStripSeparator());
        AddMenuItem(mnuArchivo, "💾  Propiedades disco", (_, _) => ShowDriveProperties());
        mnuArchivo.DropDownItems.Add(new ToolStripSeparator());
        AddMenuItem(mnuArchivo, "✕  Salir", Keys.Alt | Keys.F4, (_, _) => Close());

        var mnuVer = new ToolStripMenuItem("Ver");
        AddMenuItem(mnuVer, "☰  Vista lista", (_, _) => SetListView());
        AddMenuItem(mnuVer, "⊞  Vista cuadrícula", (_, _) => SetGridView());
        mnuVer.DropDownItems.Add(new ToolStripSeparator());
        AddMenuItem(mnuVer, "👁  Panel de vista previa", (_, _) => TogglePreview());
        mnuVer.DropDownItems.Add(new ToolStripSeparator());
        AddMenuItem(mnuVer, "🌙  Tema oscuro / claro", (_, _) => ToggleTheme());

        // ─── Archivos ocultos (NUEVO) ────────────────────────────────────────────
        mnuVer.DropDownItems.Add(new ToolStripSeparator());
        var mnuHidden = new ToolStripMenuItem("👁  Mostrar archivos ocultos");
        mnuHidden.Click += (_, _) =>
        {
            _settings.ShowHiddenFiles = !_settings.ShowHiddenFiles;
            mnuHidden.Checked = _settings.ShowHiddenFiles;
            RefreshDirectory();
        };
        mnuVer.DropDownItems.Add(mnuHidden);

        var mnuHerr = new ToolStripMenuItem("Herramientas");
        AddMenuItem(mnuHerr, "📊  Gráfica de espacio", (_, _) => ShowSpaceChart());

        // ─── Auto-inicio (NUEVO) — va aquí, ANTES del AddRange ───────────────────
        var mnuAutoStart = new ToolStripMenuItem("🚀  Iniciar con Windows")
        {
            Checked = Program.IsAutoStartEnabled()
        };
        mnuAutoStart.Click += (_, _) =>
        {
            bool enable = !Program.IsAutoStartEnabled();
            Program.SetAutoStart(enable);
            mnuAutoStart.Checked = enable;
        };
        mnuHerr.DropDownItems.Add(new ToolStripSeparator());
        mnuHerr.DropDownItems.Add(mnuAutoStart);

        var mnuAyuda = new ToolStripMenuItem("Ayuda");
        AddMenuItem(mnuAyuda, "ℹ  Acerca de", (_, _) => ShowAbout());

        // AddRange al final cuando todas las variables ya existen
        _menuStrip.Items.AddRange(new ToolStripItem[]
        {
        mnuArchivo, mnuVer, mnuHerr, mnuAyuda
        });
        Controls.Add(_menuStrip);
    }
    private static void AddMenuItem(ToolStripMenuItem parent, string text, EventHandler handler)
    {
        var m = new ToolStripMenuItem(text);
        m.Click += handler;
        parent.DropDownItems.Add(m);
    }

    private static void AddMenuItem(ToolStripMenuItem parent, string text, Keys keys, EventHandler handler)
    {
        var m = new ToolStripMenuItem(text) { ShortcutKeys = keys };
        m.Click += handler;
        parent.DropDownItems.Add(m);
    }


    // ─── ToolStrip ────────────────────────────────────────────────────────────

    private void BuildToolStrip()
    {
        _toolStrip = new ToolStrip
        {
            GripStyle = ToolStripGripStyle.Hidden,
            RenderMode = ToolStripRenderMode.System,
            Padding = new Padding(4, 2, 4, 2)
        };

        _btnBack = new ToolStripButton("◀") { ToolTipText = "Atrás (Alt+←)", Enabled = false };
        _btnForward = new ToolStripButton("▶") { ToolTipText = "Adelante (Alt+→)", Enabled = false };
        _btnUp = new ToolStripButton("▲") { ToolTipText = "Subir (Alt+↑)" };

        _btnBack.Click += (_, _) => GoBack();
        _btnForward.Click += (_, _) => GoForward();
        _btnUp.Click += (_, _) => GoUp();

        _txtAddress = new ToolStripTextBox { Width = 420, Font = new Font("Segoe UI", 9.5f) };
        _txtAddress.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                NavigateTo(_txtAddress.Text.Trim());
                e.SuppressKeyPress = true;
            }
        };

        var btnNew = new ToolStripButton("📁+") { ToolTipText = "Nueva carpeta" };
        var btnDel = new ToolStripButton("🗑") { ToolTipText = "Eliminar" };
        var btnRename = new ToolStripButton("✏") { ToolTipText = "Renombrar" };
        btnNew.Click += (_, _) => CreateNewFolder();
        btnDel.Click += (_, _) => DeleteSelected();
        btnRename.Click += (_, _) => RenameSelected();

        _btnList = new ToolStripButton("☰") { ToolTipText = "Vista lista" };
        _btnGrid = new ToolStripButton("⊞") { ToolTipText = "Vista cuadrícula" };
        _btnList.Click += (_, _) => SetListView();
        _btnGrid.Click += (_, _) => SetGridView();

        _cmbSort = new ToolStripComboBox { Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbSort.Items.AddRange(new object[]
        {
            "Nombre ↑", "Nombre ↓", "Tamaño ↑", "Tamaño ↓", "Fecha ↑", "Fecha ↓", "Tipo"
        });
        _cmbSort.SelectedIndex = 0;
        _cmbSort.SelectedIndexChanged += (_, _) => RefreshDirectory();

        _cmbFilter = new ToolStripComboBox { Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbFilter.Items.AddRange(new object[]
        {
            "Todos", "Imágenes", "Documentos", "Audio", "Video", "Código"
        });
        _cmbFilter.SelectedIndex = 0;
        _cmbFilter.SelectedIndexChanged += (_, _) => RefreshDirectory();

        _txtSearch = new ToolStripTextBox { Width = 160, Font = new Font("Segoe UI", 9.5f) };
        var btnSearch = new ToolStripButton("🔍");
        btnSearch.Click += (_, _) => PerformSearch();
        _txtSearch.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { PerformSearch(); e.SuppressKeyPress = true; }
            if (e.KeyCode == Keys.Escape) { _txtSearch.Text = string.Empty; RefreshDirectory(); e.SuppressKeyPress = true; }
        };

        _toolStrip.Items.AddRange(new ToolStripItem[]
        {
            _btnBack, _btnForward, _btnUp,
            new ToolStripSeparator(), _txtAddress,
            new ToolStripSeparator(), btnNew, btnDel, btnRename,
            new ToolStripSeparator(), _btnList, _btnGrid,
            new ToolStripSeparator(),
            new ToolStripLabel("Orden:"), _cmbSort,
            new ToolStripLabel(" Filtro:"), _cmbFilter,
            new ToolStripSeparator(), _txtSearch, btnSearch
        });
    }

    // ─── TreeView ────────────────────────────────────────────────────────────

    private void BuildTreeView()
    {
        _treeView = new TreeView
        {
            Dock = DockStyle.Fill,
            HideSelection = false,
            ShowLines = true,
            ShowPlusMinus = true,
            ShowRootLines = true,
            ImageList = _smallIcons,
            Font = new Font("Segoe UI", 9f),
            BorderStyle = BorderStyle.None,
            Indent = 16
        };
        _treeView.BeforeExpand += TreeView_BeforeExpand;
        _treeView.AfterSelect += (_, e) =>
        {
            if (e.Node?.Tag is string path && path != "__dummy__")
                NavigateTo(path);
        };
    }

    private void InitializeTreeView()
    {
        _treeView.Nodes.Clear();

        var desktop = new TreeNode("Escritorio")
        {
            Tag = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            ImageKey = IconHelper.KeyFolder,
            SelectedImageKey = IconHelper.KeyFolder
        };
        AddDummyIfChildren(desktop);
        _treeView.Nodes.Add(desktop);

        foreach (var drive in _fsService.GetDrives())
        {
            string lbl = string.IsNullOrWhiteSpace(drive.VolumeLabel)
                ? drive.Name
                : $"{drive.Name} ({drive.VolumeLabel})";

            var node = new TreeNode(lbl)
            {
                Tag = drive.RootDirectory.FullName,
                ImageKey = IconHelper.KeyDrive,
                SelectedImageKey = IconHelper.KeyDrive
            };
            AddDummyIfChildren(node);
            _treeView.Nodes.Add(node);
        }
    }

    private void AddDummyIfChildren(TreeNode node)
    {
        try
        {
            if (Directory.GetDirectories(node.Tag!.ToString()!).Length > 0)
                node.Nodes.Add(new TreeNode("...") { Tag = "__dummy__" });
        }
        catch { }
    }

    private void TreeView_BeforeExpand(object? sender, TreeViewCancelEventArgs e)
    {
        if (e.Node is null) return;
        if (e.Node.Nodes.Count == 1 && e.Node.Nodes[0].Tag?.ToString() == "__dummy__")
        {
            e.Node.Nodes.Clear();
            try
            {
                foreach (var dir in Directory.GetDirectories(e.Node.Tag!.ToString()!))
                {
                    var info = new DirectoryInfo(dir);
                    var child = new TreeNode(info.Name)
                    {
                        Tag = info.FullName,
                        ImageKey = IconHelper.KeyFolder,
                        SelectedImageKey = IconHelper.KeyFolder
                    };
                    AddDummyIfChildren(child);
                    e.Node.Nodes.Add(child);
                }
            }
            catch { }
        }
    }

    // ─── ListView ────────────────────────────────────────────────────────────

    private void BuildListView()
    {
        _listView = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = false,
            MultiSelect = true,
            SmallImageList = _smallIcons,
            LargeImageList = _largeIcons,
            Font = new Font("Segoe UI", 9f),
            BorderStyle = BorderStyle.None,
            HideSelection = false
        };

        _listView.Columns.AddRange(new[]
        {
            new ColumnHeader { Text = "Nombre",     Width = 280 },
            new ColumnHeader { Text = "Tamaño",     Width = 90, TextAlign = HorizontalAlignment.Right },
            new ColumnHeader { Text = "Tipo",       Width = 140 },
            new ColumnHeader { Text = "Modificado", Width = 130 },
            new ColumnHeader { Text = "Creado",     Width = 130 }
        });

        _listView.ItemActivate += (_, _) => OpenSelected();
        _listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
        _listView.MouseMove += ListView_MouseMove;
        _listView.KeyDown += ListView_KeyDown;
        _listView.ColumnClick += (_, e) =>
        {
            _cmbSort.SelectedIndex = e.Column switch { 1 => 2, 2 => 6, 3 => 4, _ => 0 };
        };

        var ctx = new ContextMenuStrip();
        AddCtxItem(ctx, "Abrir", (_, _) => OpenSelected());
        ctx.Items.Add(new ToolStripSeparator());
        AddCtxItem(ctx, "📋 Copiar ruta", (_, _) => CopyPath());
        ctx.Items.Add(new ToolStripSeparator());
        AddCtxItem(ctx, "✏ Renombrar", (_, _) => RenameSelected());
        AddCtxItem(ctx, "🗑 Eliminar", (_, _) => DeleteSelected());
        ctx.Items.Add(new ToolStripSeparator());
        AddCtxItem(ctx, "📊 Uso de espacio", (_, _) => ShowSpaceChart());
        AddCtxItem(ctx, "ℹ Propiedades", (_, _) => ShowItemProperties());
        _listView.ContextMenuStrip = ctx;

        // ─── Drag & Drop ──────────────────────────────────────────────────────
        _listView.AllowDrop = true;
        _listView.ItemDrag += ListView_ItemDrag;
        _listView.DragEnter += ListView_DragEnter;
        _listView.DragDrop += ListView_DragDrop;
    }
    // ─── Drag & Drop ──────────────────────────────────────────────────────────

    private void ListView_ItemDrag(object? sender, ItemDragEventArgs e)
    {
        var paths = _listView.SelectedItems
            .Cast<ListViewItem>()
            .Select(i => (i.Tag as FileSystemItem)?.FullPath)
            .Where(p => !string.IsNullOrEmpty(p))
            .Select(p => p!)
            .ToArray();

        if (paths.Length == 0) return;

        var data = new DataObject();
        data.SetData(DataFormats.FileDrop, paths);
        _listView.DoDragDrop(data, DragDropEffects.Move | DragDropEffects.Copy);
    }

    private void ListView_DragEnter(object? sender, DragEventArgs e)
    {
        e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true
            ? DragDropEffects.Move
            : DragDropEffects.None;
    }

    private void ListView_DragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] paths) return;

        var pt = _listView.PointToClient(new Point(e.X, e.Y));
        var hit = _listView.HitTest(pt);

        string destDir = (hit.Item?.Tag is FileSystemItem fi && fi.IsDirectory)
            ? fi.FullPath
            : _currentPath;

        foreach (var src in paths)
        {
            try
            {
                if (src == destDir) continue;
                string dest = Path.Combine(destDir, Path.GetFileName(src));

                if (dest.Equals(src, StringComparison.OrdinalIgnoreCase)) continue;

                if (Directory.Exists(src))
                {
                    if (dest.StartsWith(src + Path.DirectorySeparatorChar))
                    {
                        MessageBox.Show("No puedes mover una carpeta dentro de sí misma.",
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        continue;
                    }
                    Directory.Move(src, dest);
                }
                else if (File.Exists(src))
                {
                    if (File.Exists(dest))
                    {
                        var r = MessageBox.Show(
                            $"'{Path.GetFileName(src)}' ya existe. ¿Reemplazar?",
                            "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (r != DialogResult.Yes) continue;
                    }
                    File.Move(src, dest, overwrite: true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al mover '{Path.GetFileName(src)}':\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        RefreshDirectory();
    }

    private static void AddCtxItem(ContextMenuStrip ctx, string text, EventHandler h)
    {
        var m = new ToolStripMenuItem(text);
        m.Click += h;
        ctx.Items.Add(m);
    }

    // ─── Preview Panel ────────────────────────────────────────────────────────

    private void BuildPreviewPanel()
    {
        _previewPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };

        var hdr = new Label
        {
            Text = "Vista previa",
            Dock = DockStyle.Top,
            Height = 32,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(6, 0, 0, 0)
        };

        _previewImage = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            Visible = false
        };

        _previewText = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            Font = new Font("Consolas", 8.5f),
            BorderStyle = BorderStyle.None,
            Visible = false
        };

        _previewLabel = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Emoji", 28f),
            Text = "📂",
            Visible = true
        };

        _previewPanel.Controls.AddRange(
            new Control[] { _previewLabel, _previewText, _previewImage, hdr });
    }

    // ─── StatusStrip ─────────────────────────────────────────────────────────

    private void BuildStatusStrip()
    {
        _statusStrip = new StatusStrip { SizingGrip = true };
        _lblStatus = new ToolStripStatusLabel { AutoSize = true };
        _lblSelected = new ToolStripStatusLabel
        {
            Spring = true,
            TextAlign = ContentAlignment.MiddleCenter
        };
        _lblFreeSpace = new ToolStripStatusLabel
        {
            AutoSize = true,
            Padding = new Padding(0, 0, 6, 0)
        };
        _statusStrip.Items.AddRange(
            new ToolStripItem[] { _lblStatus, _lblSelected, _lblFreeSpace });
    }

    // ─── Navegación ───────────────────────────────────────────────────────────

    public void NavigateTo(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            MessageBox.Show($"La ruta no existe:\n{path}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!string.IsNullOrEmpty(_currentPath) && _currentPath != path)
        {
            _backHistory.Push(_currentPath);
            _forwardHistory.Clear();
        }

        _currentPath = path;
        _txtAddress.Text = path;

        LoadDirectory(path);
        UpdateNavButtons();
        UpdateFreeSpace(path);
        ClearPreview();
    }

    private void GoBack()
    {
        if (_backHistory.Count == 0) return;
        _forwardHistory.Push(_currentPath);
        string p = _backHistory.Pop();
        _currentPath = p; _txtAddress.Text = p;
        LoadDirectory(p); UpdateNavButtons();
    }

    private void GoForward()
    {
        if (_forwardHistory.Count == 0) return;
        _backHistory.Push(_currentPath);
        string p = _forwardHistory.Pop();
        _currentPath = p; _txtAddress.Text = p;
        LoadDirectory(p); UpdateNavButtons();
    }

    private void GoUp()
    {
        string? parent = Directory.GetParent(_currentPath)?.FullName;
        if (parent != null) NavigateTo(parent);
    }

    // ─── Directorio ───────────────────────────────────────────────────────────

    private void LoadDirectory(string path)
    {
        _listView.BeginUpdate();
        _listView.Items.Clear();

        string filter = _cmbFilter.SelectedIndex switch
        {
            1 => "jpg",
            2 => "pdf",
            3 => "mp3",
            4 => "mp4",
            5 => "cs",
            _ => "*"
        };

        var items = _fsService.GetItems(path, filter, _settings.ShowHiddenFiles);
        items = SortItems(items);

        foreach (var item in items)
        {
            string key = IconHelper.GetIconKey(item.Extension, item.IsDirectory);
            var lvi = new ListViewItem(item.Name, imageKey: key) { Tag = item };
            lvi.SubItems.Add(item.IsDirectory ? "" : FileSizeHelper.FormatSize(item.Size));
            lvi.SubItems.Add(item.FileType);
            lvi.SubItems.Add(FileSizeHelper.FormatDate(item.ModifiedDate));
            lvi.SubItems.Add(FileSizeHelper.FormatDate(item.CreatedDate));
            if (item.IsDirectory) lvi.ForeColor = _themeService.Accent;
            _listView.Items.Add(lvi);
        }

        _listView.EndUpdate();

        int fc = items.Count(i => !i.IsDirectory);
        int dc = items.Count(i => i.IsDirectory);
        _lblStatus.Text = $"📁 {dc} carpetas   📄 {fc} archivos";
        _settings.LastPath = path;
        Text = $"Explorador — {path}";
    }

    private void RefreshDirectory()
    {
        if (!string.IsNullOrEmpty(_currentPath))
            LoadDirectory(_currentPath);
    }

    private List<FileSystemItem> SortItems(List<FileSystemItem> items)
    {
        (string f, bool asc) = _cmbSort.SelectedIndex switch
        {
            1 => ("Name", false),
            2 => ("Size", true),
            3 => ("Size", false),
            4 => ("Date", true),
            5 => ("Date", false),
            6 => ("Type", true),
            _ => ("Name", true)
        };
        return _fsService.SortItems(items, f, asc);
    }

    // ─── Búsqueda ─────────────────────────────────────────────────────────────

    private async void PerformSearch()
    {
        string term = _txtSearch.Text.Trim();
        if (string.IsNullOrWhiteSpace(term)) return;

        _listView.Items.Clear();
        _lblStatus.Text = $"🔍 Buscando '{term}'...";

        var progress = new Progress<FileSystemItem>(item =>
        {
            string key = IconHelper.GetIconKey(item.Extension, item.IsDirectory);
            var lvi = new ListViewItem(item.Name, imageKey: key) { Tag = item };
            lvi.SubItems.Add(item.IsDirectory ? "" : FileSizeHelper.FormatSize(item.Size));
            lvi.SubItems.Add(item.FileType);
            lvi.SubItems.Add(item.FullPath);
            lvi.SubItems.Add(FileSizeHelper.FormatDate(item.ModifiedDate));
            if (_listView.IsHandleCreated)
                _listView.Invoke(() => _listView.Items.Add(lvi));
        });

        await _searchService.SearchAsync(_currentPath, term, recursive: true, progress);
        _lblStatus.Text = $"🔍 {_listView.Items.Count} resultados para '{term}'";
    }

    // ─── Eventos ListView ─────────────────────────────────────────────────────

    private void OpenSelected()
    {
        if (_listView.SelectedItems.Count == 0) return;
        var item = _listView.SelectedItems[0].Tag as FileSystemItem;
        if (item is null) return;
        if (item.IsDirectory) NavigateTo(item.FullPath);
        else _openerService.OpenFile(item.FullPath, this);
    }

    private void ListView_SelectedIndexChanged(object? sender, EventArgs e)
    {
        int n = _listView.SelectedItems.Count;
        if (n == 0) { _lblSelected.Text = string.Empty; ClearPreview(); return; }

        if (n == 1)
        {
            var item = _listView.SelectedItems[0].Tag as FileSystemItem;
            if (item != null)
            {
                _lblSelected.Text = item.IsDirectory
                    ? $"📁 {item.Name}  —  {item.ChildFileCount} archivos, {item.ChildFolderCount} carpetas"
                    : $"📄 {item.Name}  —  {FileSizeHelper.FormatSize(item.Size)}";
                ShowPreview(item);
            }
        }
        else
        {
            long sz = _listView.SelectedItems
                .Cast<ListViewItem>()
                .Select(i => i.Tag as FileSystemItem)
                .Where(i => i != null && !i.IsDirectory)
                .Sum(i => i!.Size);
            _lblSelected.Text = $"✓ {n} seleccionados  —  {FileSizeHelper.FormatSize(sz)}";
        }
    }

    private void ListView_MouseMove(object? sender, MouseEventArgs e)
    {
        var info = _listView.HitTest(e.X, e.Y);
        if (info.Item == _hoveredItem) return;
        _hoveredItem = info.Item;

        if (_hoveredItem?.Tag is FileSystemItem item)
        {
            string tip = item.IsDirectory
                ? $"📁 {item.Name}\nArchivos: {item.ChildFileCount}  |  Subcarpetas: {item.ChildFolderCount}\nModificado: {FileSizeHelper.FormatDateLong(item.ModifiedDate)}"
                : $"📄 {item.Name}\nTipo: {item.FileType}\nTamaño: {FileSizeHelper.FormatSize(item.Size)}\nModificado: {FileSizeHelper.FormatDateLong(item.ModifiedDate)}";
            _toolTip.SetToolTip(_listView, tip);
        }
        else _toolTip.SetToolTip(_listView, string.Empty);
    }

    private void ListView_KeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Delete: DeleteSelected(); break;
            case Keys.F2: RenameSelected(); break;
            case Keys.Enter: OpenSelected(); break;
            case Keys.Back: GoUp(); break;
        }
    }

    // ─── Vista previa ─────────────────────────────────────────────────────────

    private void ShowPreview(FileSystemItem item)
    {
        _previewImage.Visible = false;
        _previewText.Visible = false;
        _previewLabel.Visible = false;

        try
        {
            var cat = _openerService.GetCategory(item.Extension);

            if (cat == FileCategory.Image && item.Size < 10_000_000)
            {
                _previewImage.Image = Image.FromFile(item.FullPath);
                _previewImage.Visible = true;
            }
            else if (cat == FileCategory.Text && item.Size < 500_000)
            {
                _previewText.Text = File.ReadAllText(item.FullPath);
                _previewText.Visible = true;
            }
            else
            {
                _previewLabel.Text = cat switch
                {
                    FileCategory.Audio => "🎵",
                    FileCategory.Video => "🎬",
                    FileCategory.Pdf => "📕",
                    _ => item.IsDirectory ? "📁" : "📄"
                };
                _previewLabel.Visible = true;
            }
        }
        catch { _previewLabel.Text = "⚠️"; _previewLabel.Visible = true; }
    }

    private void ClearPreview()
    {
        _previewImage.Image = null;
        _previewImage.Visible = false;
        _previewText.Text = string.Empty;
        _previewText.Visible = false;
        _previewLabel.Text = "📂";
        _previewLabel.Visible = true;
    }

    // ─── Operaciones de archivo ───────────────────────────────────────────────

    private void CreateNewFolder()
    {
        using var dlg = new InputDialog("Nueva carpeta", "Nombre:", "Nueva carpeta");
        if (dlg.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(dlg.InputText))
        {
            if (!_fsService.CreateFolder(_currentPath, dlg.InputText))
                MessageBox.Show("No se pudo crear la carpeta.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            RefreshDirectory();
        }
    }

    private void DeleteSelected()
    {
        if (_listView.SelectedItems.Count == 0) return;
        int n = _listView.SelectedItems.Count;
        string msg = n == 1
            ? $"¿Eliminar '{_listView.SelectedItems[0].Text}'?"
            : $"¿Eliminar {n} elementos?";

        if (MessageBox.Show(msg, "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        foreach (ListViewItem lvi in _listView.SelectedItems)
            if (lvi.Tag is FileSystemItem fi)
                _fsService.DeleteItem(fi.FullPath);

        RefreshDirectory();
    }

    private void RenameSelected()
    {
        if (_listView.SelectedItems.Count != 1) return;
        var item = _listView.SelectedItems[0].Tag as FileSystemItem;
        if (item is null) return;

        using var dlg = new InputDialog("Renombrar", "Nuevo nombre:", item.Name);
        if (dlg.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(dlg.InputText))
        {
            if (!_fsService.RenameItem(item.FullPath, dlg.InputText))
                MessageBox.Show("No se pudo renombrar.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            RefreshDirectory();
        }
    }

    private void CopyPath()
    {
        if (_listView.SelectedItems.Count > 0 &&
            _listView.SelectedItems[0].Tag is FileSystemItem fi)
            Clipboard.SetText(fi.FullPath);
    }

    private void ShowItemProperties()
    {
        if (_listView.SelectedItems.Count == 0) return;
        var item = _listView.SelectedItems[0].Tag as FileSystemItem;
        if (item is null) return;

        using var form = new PropertiesForm(item);
        form.ShowDialog(this);
    }

    private void ShowSpaceChart()
    {
        string path = _listView.SelectedItems.Count == 1 &&
                      (_listView.SelectedItems[0].Tag as FileSystemItem)?.IsDirectory == true
            ? (_listView.SelectedItems[0].Tag as FileSystemItem)!.FullPath
            : _currentPath;
        new SpaceChartForm(path).Show(this);
    }

    private void ShowDriveProperties()
    {
        try
        {
            var d = new DriveInfo(Path.GetPathRoot(_currentPath) ?? "C:");
            MessageBox.Show(
                $"Unidad: {d.Name}\nVolumen: {d.VolumeLabel}\nSistema: {d.DriveFormat}\n" +
                $"Total: {FileSizeHelper.FormatSizeGb(d.TotalSize)}\n" +
                $"Libre: {FileSizeHelper.FormatSizeGb(d.AvailableFreeSpace)}\n" +
                $"Usado: {FileSizeHelper.FormatSizeGb(d.TotalSize - d.AvailableFreeSpace)}",
                "Propiedades del disco", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch { }
    }

    // ─── Vista ───────────────────────────────────────────────────────────────

    private void SetListView()
    {
        _listView.View = View.Details;
        _btnList.BackColor = _themeService.Accent;
        _btnGrid.BackColor = Color.Transparent;
    }

    private void SetGridView()
    {
        _listView.View = View.LargeIcon;
        _btnGrid.BackColor = _themeService.Accent;
        _btnList.BackColor = Color.Transparent;
    }

    private void TogglePreview()
    {
        _settings.ShowPreview = !_settings.ShowPreview;
        _rightSplit.Panel2Collapsed = !_settings.ShowPreview;
    }

    // ─── Tema ─────────────────────────────────────────────────────────────────

    private void ToggleTheme()
    {
        _settings.IsDarkMode = !_settings.IsDarkMode;
        _themeService.Apply(_settings.IsDarkMode);
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        _themeService.ApplyToForm(this);
        _listView.BackColor = _themeService.Surface;
        _listView.ForeColor = _themeService.Foreground;
        _previewText.BackColor = _themeService.Surface;
        _previewText.ForeColor = _themeService.Foreground;
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private void UpdateNavButtons()
    {
        _btnBack.Enabled = _backHistory.Count > 0;
        _btnForward.Enabled = _forwardHistory.Count > 0;
        _btnUp.Enabled = Directory.GetParent(_currentPath) != null;
    }

    private void UpdateFreeSpace(string path)
    {
        try
        {
            var d = new DriveInfo(Path.GetPathRoot(path) ?? path);
            _lblFreeSpace.Text =
                $"💾 Libre: {FileSizeHelper.FormatSizeGb(d.AvailableFreeSpace)} / " +
                $"{FileSizeHelper.FormatSizeGb(d.TotalSize)}";
        }
        catch { _lblFreeSpace.Text = string.Empty; }
    }

    private static void ShowAbout() =>
        MessageBox.Show(
            "Explorador de Archivos Avanzado\n.NET 8 / Windows Forms\n\n" +
            "• Navegación completa\n• Reproductor MP3 (NAudio)\n" +
            "• Visor de imágenes con zoom\n• Coloreado de sintaxis\n" +
            "• Gráfica de espacio en disco\n• Tema claro / oscuro",
            "Acerca de", MessageBoxButtons.OK, MessageBoxIcon.Information);

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.Alt | Keys.Left: GoBack(); return true;
            case Keys.Alt | Keys.Right: GoForward(); return true;
            case Keys.Alt | Keys.Up: GoUp(); return true;
            case Keys.F5: RefreshDirectory(); return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }
  
    private void OpenAudioPlayer(string? specificFile = null)
    {
        // Recopilar todos los archivos de audio de la carpeta actual
        var audioExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { ".mp3", ".flac", ".ogg", ".wav", ".aac", ".m4a", ".wma", ".opus" };

        var filesInFolder = Directory.GetFiles(_currentPath)
            .Where(f => audioExts.Contains(Path.GetExtension(f)))
            .OrderBy(f => f)
            .ToList();

        var player = new AudioPlayerForm(filesInFolder);

        // Si se abrió desde un archivo específico, saltar a él
        if (specificFile != null)
        {
            int idx = filesInFolder.IndexOf(specificFile);
            if (idx >= 0) player.PlayAt(idx);  // necesitas hacer PlayAt público
        }

        player.Show(this);
    }
}