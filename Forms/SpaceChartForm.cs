using FileExplorer.Services;
using FileExplorer.Helpers;

namespace FileExplorer.Forms;

/// <summary>
/// Muestra una gráfica de pastel (pie chart) del uso de espacio en disco
/// en el directorio seleccionado, agrupado por tipo/extensión de archivo.
/// Renderizado completamente con GDI+.
/// </summary>
public class SpaceChartForm : Form
{
    private readonly string _path;
    private readonly FileSystemService _fsService;

    // ─── Datos ────────────────────────────────────────────────────────────────
    private List<(string Label, long Bytes, Color Color)> _segments = [];
    private int _hoveredIndex = -1;

    // ─── Controles ────────────────────────────────────────────────────────────
    private Panel   _chartPanel  = null!;
    private Panel   _legendPanel = null!;
    private Label   _lblTitle    = null!;
    private Label   _lblTotal    = null!;
    private Label   _lblStatus   = null!;

    // Paleta de colores para los segmentos
    private static readonly Color[] Palette =
    {
        Color.FromArgb(0,  122, 255),
        Color.FromArgb(52, 199, 89),
        Color.FromArgb(255, 159, 10),
        Color.FromArgb(255, 59,  48),
        Color.FromArgb(191, 90, 242),
        Color.FromArgb(48, 176, 199),
        Color.FromArgb(255, 204, 0),
        Color.FromArgb(100, 210, 255),
        Color.FromArgb(254, 114, 138),
        Color.FromArgb(162, 132, 94)
    };

    public SpaceChartForm(string path)
    {
        _path      = path;
        _fsService = new FileSystemService();
        InitializeComponent();
        _ = LoadDataAsync();
    }

    // ─── UI ──────────────────────────────────────────────────────────────────

    private void InitializeComponent()
    {
        Text          = $"Uso de espacio — {Path.GetFileName(_path)}";
        Size          = new Size(800, 580);
        MinimumSize   = new Size(600, 450);
        StartPosition = FormStartPosition.CenterParent;
        BackColor     = Color.FromArgb(28, 28, 30);

        _lblTitle = new Label
        {
            Text      = $"📊  Distribución de espacio en: {_path}",
            Dock      = DockStyle.Top,
            Height    = 44,
            ForeColor = Color.White,
            Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(12, 0, 0, 0),
            BackColor = Color.FromArgb(44, 44, 46)
        };

        _lblStatus = new Label
        {
            Text      = "Calculando uso de espacio...",
            Dock      = DockStyle.Top,
            Height    = 24,
            ForeColor = Color.FromArgb(174, 174, 178),
            Font      = new Font("Segoe UI", 8.5f),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(12, 0, 0, 0),
            BackColor = Color.Transparent
        };

        // Panel de la gráfica
        _chartPanel = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.Transparent
        };
        _chartPanel.Paint     += ChartPanel_Paint;
        _chartPanel.MouseMove += ChartPanel_MouseMove;
        _chartPanel.MouseLeave += (_, _) => { _hoveredIndex = -1; _chartPanel.Invalidate(); };

        // Panel de leyenda (derecha)
        _legendPanel = new Panel
        {
            Dock      = DockStyle.Right,
            Width     = 220,
            BackColor = Color.FromArgb(38, 38, 40),
            Padding   = new Padding(10)
        };

        _lblTotal = new Label
        {
            Dock      = DockStyle.Bottom,
            Height    = 36,
            ForeColor = Color.FromArgb(174, 174, 178),
            Font      = new Font("Segoe UI", 9f),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.FromArgb(36, 36, 38)
        };

        Controls.AddRange(new Control[] { _chartPanel, _legendPanel, _lblStatus, _lblTitle, _lblTotal });
    }

    // ─── Carga de datos ───────────────────────────────────────────────────────

    private async Task LoadDataAsync()
    {
        _lblStatus.Text = "⏳  Analizando archivos...";

        var distribution = await Task.Run(() => _fsService.GetSpaceDistribution(_path));

        if (distribution.Count == 0)
        {
            _lblStatus.Text = "ℹ️  No se encontraron archivos en la carpeta.";
            return;
        }

        // Ordenar por tamaño descendente y tomar top 9 + "Otros"
        var sorted = distribution
            .OrderByDescending(kv => kv.Value)
            .ToList();

        _segments = [];
        long total  = sorted.Sum(kv => kv.Value);
        long others = 0;

        for (int i = 0; i < sorted.Count; i++)
        {
            if (i < Palette.Length - 1)
            {
                _segments.Add((
                    sorted[i].Key,
                    sorted[i].Value,
                    Palette[i % Palette.Length]));
            }
            else
            {
                others += sorted[i].Value;
            }
        }

        if (others > 0)
            _segments.Add(("Otros", others, Palette[^1]));

        _lblTotal.Text   = $"Total: {FileSizeHelper.FormatSize(total)}";
        _lblStatus.Text  = $"✓  {sorted.Count} tipos de archivos encontrados";

        BuildLegend(total);
        _chartPanel.Invalidate();
    }

    private void BuildLegend(long total)
    {
        _legendPanel.Controls.Clear();

        var title = new Label
        {
            Text      = "Leyenda",
            Dock      = DockStyle.Top,
            Height    = 32,
            ForeColor = Color.White,
            Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _legendPanel.Controls.Add(title);

        foreach (var (label, bytes, color) in _segments.AsEnumerable().Reverse())
        {
            double pct = total > 0 ? (double)bytes / total * 100 : 0;

            var row = new Panel { Dock = DockStyle.Top, Height = 28 };
            var dot = new Panel
            {
                Size      = new Size(12, 12),
                Location  = new Point(0, 8),
                BackColor = color
            };
            var lbl = new Label
            {
                Text      = $"{label}  ({pct:F1}%)",
                Location  = new Point(18, 0),
                Width     = 190,
                Height    = 28,
                ForeColor = Color.FromArgb(220, 220, 220),
                Font      = new Font("Segoe UI", 8.5f),
                TextAlign = ContentAlignment.MiddleLeft
            };
            row.Controls.AddRange(new Control[] { dot, lbl });
            _legendPanel.Controls.Add(row);
        }
    }

    // ─── Renderizado del pie chart ────────────────────────────────────────────

    private void ChartPanel_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode     = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(_chartPanel.BackColor == Color.Transparent
            ? Color.FromArgb(28, 28, 30) : _chartPanel.BackColor);

        if (_segments.Count == 0) return;

        int size   = Math.Min(_chartPanel.Width, _chartPanel.Height) - 80;
        int x      = (_chartPanel.Width  - size) / 2;
        int y      = (_chartPanel.Height - size) / 2;
        var bounds = new Rectangle(x, y, size, size);
        var center = new PointF(x + size / 2f, y + size / 2f);

        long total       = _segments.Sum(s => s.Bytes);
        float startAngle = -90f;

        for (int i = 0; i < _segments.Count; i++)
        {
            float sweepAngle = (float)((double)_segments[i].Bytes / total * 360f);
            bool  hovered    = i == _hoveredIndex;

            // Segmento resaltado sobresale un poco
            var segBounds = hovered
                ? Explode(bounds, startAngle + sweepAngle / 2f, 10)
                : bounds;

            using var brush = new SolidBrush(_segments[i].Color);
            g.FillPie(brush, segBounds, startAngle, sweepAngle);

            // Borde blanco delgado
            using var pen = new Pen(Color.FromArgb(28, 28, 30), 2f);
            g.DrawPie(pen, segBounds, startAngle, sweepAngle);

            // Etiqueta de porcentaje si el segmento es suficientemente grande
            double pct = (double)_segments[i].Bytes / total * 100;
            if (pct > 5)
            {
                float midAngle  = startAngle + sweepAngle / 2f;
                float labelRadius = size / 2f * 0.65f;
                float lx = center.X + labelRadius * (float)Math.Cos(midAngle * Math.PI / 180f);
                float ly = center.Y + labelRadius * (float)Math.Sin(midAngle * Math.PI / 180f);

                using var font  = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                using var txtBr = new SolidBrush(Color.White);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString($"{pct:F0}%", font, txtBr, lx, ly, sf);
            }

            startAngle += sweepAngle;
        }

        // Círculo central (donut)
        int holeSize = size / 3;
        int hx = x + (size - holeSize) / 2;
        int hy = y + (size - holeSize) / 2;
        using var holeBrush = new SolidBrush(Color.FromArgb(28, 28, 30));
        g.FillEllipse(holeBrush, hx, hy, holeSize, holeSize);

        // Texto central
        using var centerFont  = new Font("Segoe UI", 9f, FontStyle.Bold);
        using var centerBrush = new SolidBrush(Color.White);
        var csf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString($"{_segments.Count}\ntipos", centerFont, centerBrush,
            new RectangleF(hx, hy, holeSize, holeSize), csf);

        // Tooltip en segmento hover
        if (_hoveredIndex >= 0 && _hoveredIndex < _segments.Count)
        {
            var seg  = _segments[_hoveredIndex];
            double p = (double)seg.Bytes / total * 100;
            string tip = $"{seg.Label}\n{FileSizeHelper.FormatSize(seg.Bytes)}  ({p:F1}%)";

            using var tipBrush = new SolidBrush(Color.FromArgb(230, 44, 44, 46));
            using var tipFont  = new Font("Segoe UI", 9f);
            using var tipPen   = new Pen(seg.Color, 1.5f);
            var sz  = g.MeasureString(tip, tipFont);
            var rect = new RectangleF(10, 10, sz.Width + 16, sz.Height + 8);
            g.FillRectangle(tipBrush, rect);
            g.DrawRectangle(tipPen, rect.X, rect.Y, rect.Width, rect.Height);
            g.DrawString(tip, tipFont, centerBrush, rect.X + 8, rect.Y + 4);
        }
    }

    private static Rectangle Explode(Rectangle bounds, float angleDeg, int amount)
    {
        float rad = angleDeg * (float)Math.PI / 180f;
        int dx = (int)(amount * Math.Cos(rad));
        int dy = (int)(amount * Math.Sin(rad));
        return new Rectangle(bounds.X + dx, bounds.Y + dy, bounds.Width, bounds.Height);
    }

    private void ChartPanel_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_segments.Count == 0) return;

        int size   = Math.Min(_chartPanel.Width, _chartPanel.Height) - 80;
        var center = new PointF(
            _chartPanel.Width  / 2f,
            _chartPanel.Height / 2f);

        float dx   = e.X - center.X;
        float dy   = e.Y - center.Y;
        float dist = (float)Math.Sqrt(dx * dx + dy * dy);

        if (dist > size / 2f || dist < size / 6f)
        {
            if (_hoveredIndex != -1) { _hoveredIndex = -1; _chartPanel.Invalidate(); }
            return;
        }

        float angle = (float)(Math.Atan2(dy, dx) * 180 / Math.PI) + 90f;
        if (angle < 0) angle += 360f;

        long total       = _segments.Sum(s => s.Bytes);
        float startAngle = 0f;
        int newHover     = -1;

        for (int i = 0; i < _segments.Count; i++)
        {
            float sweep = (float)((double)_segments[i].Bytes / total * 360f);
            if (angle >= startAngle && angle < startAngle + sweep)
            {
                newHover = i;
                break;
            }
            startAngle += sweep;
        }

        if (newHover != _hoveredIndex)
        {
            _hoveredIndex = newHover;
            _chartPanel.Invalidate();
        }
    }
}
