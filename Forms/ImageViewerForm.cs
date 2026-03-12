namespace FileExplorer.Forms;

public class ImageViewerForm : Form
{
    private readonly string _filePath;
    private Image? _image;
    private float _zoomFactor = 1.0f;
    private Point _panStart;
    private Point _panOffset;
    private bool _isPanning;
    private Panel _canvas = null!;
    private Label _lblInfo = null!;

    public ImageViewerForm(string filePath)
    {
        _filePath = filePath;
        InitializeComponent();
        LoadImage();
    }

    private void InitializeComponent()
    {
        Text = $"Imagen — {Path.GetFileName(_filePath)}";
        Size = new Size(900, 680);
        MinimumSize = new Size(400, 300);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(28, 28, 30);

        var toolbar = new ToolStrip
        {
            GripStyle = ToolStripGripStyle.Hidden,
            BackColor = Color.FromArgb(44, 44, 46)
        };

        var btnZoomIn = new ToolStripButton("Acercar (+)");
        var btnZoomOut = new ToolStripButton("Alejar (−)");
        var btnFit = new ToolStripButton("Ajustar");
        var btnOrig = new ToolStripButton("100%");

        btnZoomIn.Click += (_, _) => Zoom(1.25f);
        btnZoomOut.Click += (_, _) => Zoom(0.80f);
        btnFit.Click += (_, _) => FitToWindow();
        btnOrig.Click += (_, _) =>
        {
            _zoomFactor = 1f;
            _panOffset = Point.Empty;
            _canvas.Invalidate();
        };

        toolbar.Items.AddRange(new ToolStripItem[]
        {
            btnZoomIn, btnZoomOut,
            new ToolStripSeparator(),
            btnFit, btnOrig
        });

        _canvas = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(18, 18, 20),
            Cursor = Cursors.Hand
        };

        _canvas.Paint += Canvas_Paint;
        _canvas.MouseWheel += (_, e) => Zoom(e.Delta > 0 ? 1.15f : 0.87f);
        _canvas.MouseDown += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                _isPanning = true;
                _panStart = new Point(e.X - _panOffset.X, e.Y - _panOffset.Y);
            }
        };
        _canvas.MouseMove += (_, e) =>
        {
            if (_isPanning)
            {
                _panOffset = new Point(e.X - _panStart.X, e.Y - _panStart.Y);
                _canvas.Invalidate();
            }
        };
        _canvas.MouseUp += (_, _) => _isPanning = false;

        _lblInfo = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 24,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(174, 174, 178),
            BackColor = Color.FromArgb(36, 36, 38),
            Font = new Font("Segoe UI", 8.5f)
        };

        Controls.AddRange(new Control[] { _canvas, toolbar, _lblInfo });

        // Atajos — Keys.Fit no existe, se usa Keys.Home
        KeyPreview = true;
        KeyDown += (_, e) =>
        {
            if (!e.Control) return;
            if (e.KeyCode == Keys.Oemplus || e.KeyCode == Keys.Add) Zoom(1.25f);
            if (e.KeyCode == Keys.OemMinus || e.KeyCode == Keys.Subtract) Zoom(0.8f);
            if (e.KeyCode == Keys.D0) { _zoomFactor = 1f; _panOffset = Point.Empty; _canvas.Invalidate(); }
            if (e.KeyCode == Keys.Home) FitToWindow();
        };
    }

    private void LoadImage()
    {
        try
        {
            _image = Image.FromFile(_filePath);
            FitToWindow();
            UpdateInfo();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudo cargar la imagen:\n{ex.Message}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Close();
        }
    }

    private void Canvas_Paint(object? sender, PaintEventArgs e)
    {
        if (_image is null) return;
        e.Graphics.Clear(_canvas.BackColor);
        e.Graphics.InterpolationMode =
            System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

        int w = (int)(_image.Width * _zoomFactor);
        int h = (int)(_image.Height * _zoomFactor);
        int x = (_canvas.Width - w) / 2 + _panOffset.X;
        int y = (_canvas.Height - h) / 2 + _panOffset.Y;

        e.Graphics.DrawImage(_image, x, y, w, h);
    }

    private void Zoom(float factor)
    {
        _zoomFactor = Math.Clamp(_zoomFactor * factor, 0.05f, 20f);
        UpdateInfo();
        _canvas.Invalidate();
    }

    private void FitToWindow()
    {
        if (_image is null) return;
        float sx = (float)_canvas.Width / _image.Width;
        float sy = (float)_canvas.Height / _image.Height;
        _zoomFactor = Math.Min(sx, sy) * 0.95f;
        _panOffset = Point.Empty;
        UpdateInfo();
        _canvas.Invalidate();
    }

    private void UpdateInfo()
    {
        if (_image is null) return;
        _lblInfo.Text = $"{_image.Width} × {_image.Height} px  |  Zoom: {_zoomFactor * 100:F0}%";
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _image?.Dispose();
        base.OnFormClosed(e);
    }
}