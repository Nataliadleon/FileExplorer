// REEMPLAZA todo el archivo:
namespace FileExplorer.Models;

/// <summary>
/// Representa un ítem del sistema de archivos (archivo o carpeta).
/// Encapsula toda la metadata relevante para mostrar en el explorador.
/// </summary>
public class FileSystemItem
{
    // ─── Datos básicos ───────────────────────────────────────────────────────
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public bool IsDirectory { get; set; }
    public long Size { get; set; }
    public string FileType { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;

    // ─── Fechas ──────────────────────────────────────────────────────────────
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public DateTime LastAccessDate { get; set; }

    // ─── Atributos del sistema ───────────────────────────────────────────────
    public FileAttributes Attributes { get; set; }
    public bool IsReadOnly { get; set; }
    public bool IsHidden { get; set; }
    public bool IsSystem { get; set; }
    public string Owner { get; set; } = string.Empty;

    // ─── Directorio ──────────────────────────────────────────────────────────
    public int ChildFileCount { get; set; }
    public int ChildFolderCount { get; set; }

    // ─── Imagen (EXIF / GPS) ─────────────────────────────────────────────────
    public int? ImageWidth { get; set; }
    public int? ImageHeight { get; set; }
    public string? CameraMake { get; set; }
    public string? CameraModel { get; set; }
    public string? DateTaken { get; set; }
    public double? GpsLatitude { get; set; }
    public double? GpsLongitude { get; set; }
    public double? GpsAltitude { get; set; }
    public string? ColorSpace { get; set; }
    public string? ExposureTime { get; set; }
    public string? FNumber { get; set; }
    public string? IsoSpeed { get; set; }
    public string? Flash { get; set; }

    // ─── Icono ───────────────────────────────────────────────────────────────
    public string IconKey { get; set; } = string.Empty;
    public int ImageIndex { get; set; }
}