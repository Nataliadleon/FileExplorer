namespace FileExplorer.Models;

/// <summary>
/// Representa un ítem del sistema de archivos (archivo o carpeta).
/// Encapsula toda la metadata relevante para mostrar en el explorador.
/// </summary>
public class FileSystemItem
{
    /// <summary>Nombre del archivo o carpeta (sin ruta).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Ruta completa en el sistema de archivos.</summary>
    public string FullPath { get; set; } = string.Empty;

    /// <summary>Indica si el ítem es un directorio.</summary>
    public bool IsDirectory { get; set; }

    /// <summary>Tamaño en bytes. 0 para carpetas.</summary>
    public long Size { get; set; }

    /// <summary>Descripción legible del tipo de archivo.</summary>
    public string FileType { get; set; } = string.Empty;

    /// <summary>Extensión del archivo en minúsculas (ej: ".txt").</summary>
    public string Extension { get; set; } = string.Empty;

    /// <summary>Fecha y hora de creación.</summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>Fecha y hora de última modificación.</summary>
    public DateTime ModifiedDate { get; set; }

    /// <summary>Número de archivos directos dentro de la carpeta.</summary>
    public int ChildFileCount { get; set; }

    /// <summary>Número de subcarpetas directas dentro de la carpeta.</summary>
    public int ChildFolderCount { get; set; }

    /// <summary>Key del ícono en el ImageList del ListView.</summary>
    public string IconKey { get; set; } = string.Empty;

    /// <summary>Índice del ícono en el ImageList del ListView.</summary>
    public int ImageIndex { get; set; }
}
