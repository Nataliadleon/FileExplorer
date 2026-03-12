using FileExplorer.Models;

namespace FileExplorer.Services;

/// <summary>
/// Servicio responsable de todas las operaciones del sistema de archivos.
/// Encapsula la lógica de lectura, navegación y manipulación de archivos y carpetas.
/// </summary>
public class FileSystemService
{
    // ─── Lectura de directorio ───────────────────────────────────────────────

    /// <summary>
    /// Obtiene todos los ítems (archivos y carpetas) de un directorio.
    /// Las carpetas se devuelven antes que los archivos.
    /// </summary>
    /// <param name="path">Ruta del directorio a listar.</param>
    /// <param name="filterExtension">"*" para todos, o extensión sin punto ej: "jpg".</param>
    public List<FileSystemItem> GetItems(string path, string filterExtension = "*")
    {
        var items = new List<FileSystemItem>();
        if (!Directory.Exists(path)) return items;

        items.AddRange(GetDirectories(path));
        items.AddRange(GetFiles(path, filterExtension));

        return items;
    }

    /// <summary>Obtiene únicamente las subcarpetas de un directorio.</summary>
    private IEnumerable<FileSystemItem> GetDirectories(string path)
    {
        foreach (var dir in SafeGetDirectories(path))
        {
            var info = new DirectoryInfo(dir);
            var (files, folders) = SafeGetChildCounts(dir);
            yield return new FileSystemItem
            {
                Name         = info.Name,
                FullPath     = info.FullName,
                IsDirectory  = true,
                CreatedDate  = info.CreationTime,
                ModifiedDate = info.LastWriteTime,
                FileType     = "Carpeta de archivos",
                Extension    = string.Empty,
                Size         = 0,
                ChildFileCount   = files,
                ChildFolderCount = folders
            };
        }
    }

    /// <summary>Obtiene únicamente los archivos de un directorio, con filtro opcional.</summary>
    private IEnumerable<FileSystemItem> GetFiles(string path, string filterExtension)
    {
        string pattern = filterExtension == "*" ? "*.*" : $"*.{filterExtension}";

        foreach (var file in SafeGetFiles(path, pattern))
        {
            var info = new FileInfo(file);
            yield return new FileSystemItem
            {
                Name         = info.Name,
                FullPath     = info.FullName,
                IsDirectory  = false,
                CreatedDate  = info.CreationTime,
                ModifiedDate = info.LastWriteTime,
                FileType     = GetFileType(info.Extension),
                Extension    = info.Extension.ToLowerInvariant(),
                Size         = info.Length
            };
        }
    }

    // ─── Ordenamiento ────────────────────────────────────────────────────────

    /// <summary>Ordena una lista de ítems según el criterio y dirección especificados.</summary>
    public List<FileSystemItem> SortItems(
        List<FileSystemItem> items,
        string sortBy,
        bool ascending)
    {
        IOrderedEnumerable<FileSystemItem> sorted = sortBy switch
        {
            "Size" => ascending
                ? items.OrderBy(i => i.IsDirectory).ThenBy(i => i.Size)
                : items.OrderBy(i => i.IsDirectory).ThenByDescending(i => i.Size),
            "Date" => ascending
                ? items.OrderBy(i => i.IsDirectory).ThenBy(i => i.ModifiedDate)
                : items.OrderBy(i => i.IsDirectory).ThenByDescending(i => i.ModifiedDate),
            "Type" => ascending
                ? items.OrderBy(i => i.IsDirectory).ThenBy(i => i.Extension)
                : items.OrderBy(i => i.IsDirectory).ThenByDescending(i => i.Extension),
            _ => ascending
                ? items.OrderBy(i => i.IsDirectory ? 0 : 1).ThenBy(i => i.Name)
                : items.OrderBy(i => i.IsDirectory ? 0 : 1).ThenByDescending(i => i.Name)
        };

        return sorted.ToList();
    }

    // ─── Información de disco ────────────────────────────────────────────────

    /// <summary>Obtiene todas las unidades de disco disponibles y listas.</summary>
    public List<DriveInfo> GetDrives() =>
        DriveInfo.GetDrives().Where(d => d.IsReady).ToList();

    /// <summary>
    /// Calcula el tamaño total de todos los archivos dentro de una carpeta
    /// (incluyendo subcarpetas recursivamente).
    /// </summary>
    public long GetFolderSize(string path)
    {
        long total = 0;
        try
        {
            foreach (var f in Directory.GetFiles(path, "*.*", SearchOption.AllDirectories))
                try { total += new FileInfo(f).Length; } catch { }
        }
        catch { }
        return total;
    }

    /// <summary>
    /// Obtiene la distribución de espacio en disco por extensión dentro de una carpeta.
    /// Retorna un diccionario con extensión → bytes totales.
    /// </summary>
    public Dictionary<string, long> GetSpaceDistribution(string path)
    {
        var dist = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var f in Directory.GetFiles(path, "*.*", SearchOption.AllDirectories))
            {
                try
                {
                    var info = new FileInfo(f);
                    string ext = string.IsNullOrEmpty(info.Extension) ? "Sin extensión" : info.Extension.ToUpperInvariant();
                    dist[ext] = dist.GetValueOrDefault(ext) + info.Length;
                }
                catch { }
            }
        }
        catch { }
        return dist;
    }

    // ─── Operaciones CRUD ────────────────────────────────────────────────────

    /// <summary>Crea una nueva carpeta dentro del directorio especificado.</summary>
    public bool CreateFolder(string parentPath, string folderName)
    {
        try
        {
            Directory.CreateDirectory(Path.Combine(parentPath, folderName));
            return true;
        }
        catch { return false; }
    }

    /// <summary>Elimina un archivo o carpeta (carpetas se eliminan recursivamente).</summary>
    public bool DeleteItem(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
            else if (File.Exists(path))
                File.Delete(path);
            return true;
        }
        catch { return false; }
    }

    /// <summary>Renombra un archivo o carpeta en su misma ubicación.</summary>
    public bool RenameItem(string path, string newName)
    {
        try
        {
            string dir = Path.GetDirectoryName(path)!;
            string newPath = Path.Combine(dir, newName);

            if (Directory.Exists(path))
                Directory.Move(path, newPath);
            else
                File.Move(path, newPath);

            return true;
        }
        catch { return false; }
    }

    // ─── Métodos auxiliares seguros ──────────────────────────────────────────

    private IEnumerable<string> SafeGetDirectories(string path)
    {
        try { return Directory.GetDirectories(path); }
        catch { return Array.Empty<string>(); }
    }

    private IEnumerable<string> SafeGetFiles(string path, string pattern)
    {
        try { return Directory.GetFiles(path, pattern); }
        catch { return Array.Empty<string>(); }
    }

    private (int files, int folders) SafeGetChildCounts(string path)
    {
        try
        {
            return (
                Directory.GetFiles(path).Length,
                Directory.GetDirectories(path).Length
            );
        }
        catch { return (0, 0); }
    }

    // ─── Mapeo de tipos de archivo ────────────────────────────────────────────

    /// <summary>Retorna una descripción legible del tipo de archivo según su extensión.</summary>
    private static string GetFileType(string extension) =>
        extension.ToLowerInvariant() switch
        {
            ".txt"               => "Archivo de texto",
            ".pdf"               => "Documento PDF",
            ".jpg" or ".jpeg"    => "Imagen JPEG",
            ".png"               => "Imagen PNG",
            ".gif"               => "Imagen GIF",
            ".bmp"               => "Mapa de bits",
            ".mp3"               => "Audio MP3",
            ".wav"               => "Audio WAV",
            ".mp4"               => "Video MP4",
            ".avi"               => "Video AVI",
            ".mkv"               => "Video MKV",
            ".json"              => "Archivo JSON",
            ".xml"               => "Archivo XML",
            ".html" or ".htm"    => "Página Web",
            ".cs"                => "Código C#",
            ".js"                => "JavaScript",
            ".py"                => "Script Python",
            ".zip" or ".rar"     => "Archivo comprimido",
            ".exe"               => "Aplicación ejecutable",
            ".dll"               => "Biblioteca dinámica",
            var e when !string.IsNullOrEmpty(e) => $"Archivo {e.ToUpperInvariant()}",
            _                    => "Archivo"
        };
}
