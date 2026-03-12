namespace FileExplorer.Helpers;

/// <summary>
/// Ayudante para la gestión de íconos del explorador.
/// Extrae íconos del sistema y genera íconos predeterminados por tipo de archivo.
/// Usa colores distintivos para identificar visualmente cada tipo de archivo.
/// </summary>
public static class IconHelper
{
    // ─── Claves de ícono por categoría ───────────────────────────────────────
    public const string KeyFolder   = "folder";
    public const string KeyDrive    = "drive";
    public const string KeyImage    = "image";
    public const string KeyText     = "text";
    public const string KeyAudio    = "audio";
    public const string KeyVideo    = "video";
    public const string KeyPdf      = "pdf";
    public const string KeyCode     = "code";
    public const string KeyArchive  = "archive";
    public const string KeyExe      = "exe";
    public const string KeyDefault  = "default";

    // ─── Inicialización de ImageLists ────────────────────────────────────────

    /// <summary>
    /// Crea y llena un ImageList con íconos de 16×16 píxeles para vista lista.
    /// </summary>
    public static ImageList BuildSmallImageList()
    {
        var il = new ImageList { ImageSize = new Size(16, 16), ColorDepth = ColorDepth.Depth32Bit };
        AddAllIcons(il);
        return il;
    }

    /// <summary>
    /// Crea y llena un ImageList con íconos de 48×48 píxeles para vista cuadrícula.
    /// </summary>
    public static ImageList BuildLargeImageList()
    {
        var il = new ImageList { ImageSize = new Size(48, 48), ColorDepth = ColorDepth.Depth32Bit };
        AddAllIcons(il);
        return il;
    }

    /// <summary>Devuelve la clave de ícono correspondiente a la extensión dada.</summary>
    public static string GetIconKey(string extension, bool isDirectory)
    {
        if (isDirectory) return KeyFolder;

        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or ".webp" => KeyImage,
            ".txt" or ".log" or ".md"                                   => KeyText,
            ".json" or ".xml" or ".html" or ".htm" or ".css"           => KeyCode,
            ".cs" or ".js" or ".py" or ".ts" or ".java" or ".cpp"      => KeyCode,
            ".mp3" or ".wav" or ".ogg" or ".flac" or ".aac"            => KeyAudio,
            ".mp4" or ".avi" or ".mkv" or ".mov" or ".wmv"             => KeyVideo,
            ".pdf"                                                       => KeyPdf,
            ".zip" or ".rar" or ".7z" or ".tar" or ".gz"               => KeyArchive,
            ".exe" or ".msi"                                            => KeyExe,
            _                                                            => KeyDefault
        };
    }

    // ─── Generación de íconos con GDI+ ───────────────────────────────────────

    private static void AddAllIcons(ImageList il)
    {
        int size = il.ImageSize.Width;
        il.Images.Add(KeyFolder,  DrawFolderIcon(size));
        il.Images.Add(KeyDrive,   DrawDriveIcon(size));
        il.Images.Add(KeyImage,   DrawColoredIcon(size, Color.FromArgb(52, 199, 89),   "🖼", "IMG"));
        il.Images.Add(KeyText,    DrawColoredIcon(size, Color.FromArgb(90, 90, 100),    "📄", "TXT"));
        il.Images.Add(KeyAudio,   DrawColoredIcon(size, Color.FromArgb(255, 159, 10),   "🎵", "MP3"));
        il.Images.Add(KeyVideo,   DrawColoredIcon(size, Color.FromArgb(191, 90, 242),   "🎬", "MP4"));
        il.Images.Add(KeyPdf,     DrawColoredIcon(size, Color.FromArgb(255, 59, 48),    "📕", "PDF"));
        il.Images.Add(KeyCode,    DrawColoredIcon(size, Color.FromArgb(48, 176, 199),   "💻", "COD"));
        il.Images.Add(KeyArchive, DrawColoredIcon(size, Color.FromArgb(162, 132, 94),   "📦", "ZIP"));
        il.Images.Add(KeyExe,     DrawColoredIcon(size, Color.FromArgb(0, 122, 255),    "⚙", "EXE"));
        il.Images.Add(KeyDefault, DrawColoredIcon(size, Color.FromArgb(142, 142, 147),  "📎", "???"));
    }

    /// <summary>Dibuja un ícono de carpeta estilizado con perspectiva.</summary>
    private static Bitmap DrawFolderIcon(int size)
    {
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        Color folderBack   = Color.FromArgb(255, 204, 0);
        Color folderFront  = Color.FromArgb(255, 214, 40);
        Color folderTab    = Color.FromArgb(230, 184, 0);

        int m = Math.Max(1, size / 16);
        // Cuerpo de la carpeta
        using var backBrush  = new SolidBrush(folderBack);
        using var frontBrush = new SolidBrush(folderFront);
        using var tabBrush   = new SolidBrush(folderTab);

        // Cuerpo principal
        g.FillRectangle(backBrush,
            new Rectangle(m, size / 4, size - m * 2, size * 3 / 4 - m));

        // Solapa superior
        var tabPath = new System.Drawing.Drawing2D.GraphicsPath();
        tabPath.AddRoundedRectangle(new Rectangle(m, size / 4 - size / 6, size / 2, size / 5), 2);
        g.FillPath(tabBrush, tabPath);

        // Brillo frontal
        g.FillRectangle(frontBrush,
            new Rectangle(m, size / 3, size - m * 2, size / 6));

        return bmp;
    }

    /// <summary>Dibuja un ícono de disco/unidad.</summary>
    private static Bitmap DrawDriveIcon(int size)
    {
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        int m = Math.Max(1, size / 8);
        using var body = new SolidBrush(Color.FromArgb(100, 100, 110));
        using var top  = new SolidBrush(Color.FromArgb(140, 140, 150));
        using var led  = new SolidBrush(Color.FromArgb(52, 199, 89));

        g.FillRectangle(body, new Rectangle(m, m * 2, size - m * 2, size - m * 4));
        g.FillRectangle(top,  new Rectangle(m, m * 2, size - m * 2, m * 2));
        g.FillEllipse(led,    new Rectangle(size - m * 3, m * 3, m, m));
        return bmp;
    }

    /// <summary>Dibuja un ícono de color sólido con etiqueta de texto.</summary>
    private static Bitmap DrawColoredIcon(int size, Color baseColor, string emoji, string label)
    {
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        g.Clear(Color.Transparent);

        int r = Math.Max(2, size / 8);
        var rect = new Rectangle(0, 0, size - 1, size - 1);

        // Fondo redondeado
        using var fillBrush = new SolidBrush(baseColor);
        FillRoundRect(g, fillBrush, rect, r);

        // Texto pequeño centrado
        if (size >= 32)
        {
            float fontSize = size / 5.5f;
            using var font = new Font("Segoe UI", fontSize, FontStyle.Bold);
            using var textBrush = new SolidBrush(Color.White);
            var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            g.DrawString(label, font, textBrush, new RectangleF(0, 0, size, size), sf);
        }

        return bmp;
    }

    private static void FillRoundRect(Graphics g, Brush brush, Rectangle rect, int radius)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        path.AddRoundedRectangle(rect, radius);
        g.FillPath(brush, path);
    }
}

/// <summary>Extensión para dibujar rectángulos redondeados con GraphicsPath.</summary>
internal static class GraphicsPathExtensions
{
    public static void AddRoundedRectangle(this System.Drawing.Drawing2D.GraphicsPath path,
        Rectangle rect, int radius)
    {
        int d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
    }
}
