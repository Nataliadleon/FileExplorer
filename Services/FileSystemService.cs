// REEMPLAZA todo el archivo:
using FileExplorer.Models;
using System.Security.AccessControl;
using System.Security.Principal;

namespace FileExplorer.Services;

/// <summary>
/// Servicio responsable de todas las operaciones del sistema de archivos.
/// </summary>
public class FileSystemService
{
    // ─── Lectura de directorio ───────────────────────────────────────────────

    public List<FileSystemItem> GetItems(string path, string filterExtension = "*", bool showHidden = false)
    {
        var items = new List<FileSystemItem>();
        if (!Directory.Exists(path)) return items;

        items.AddRange(GetDirectories(path, showHidden));
        items.AddRange(GetFiles(path, filterExtension, showHidden));
        return items;
    }

    private IEnumerable<FileSystemItem> GetDirectories(string path, bool showHidden)
    {
        foreach (var dir in SafeGetDirectories(path))
        {
            var info = new DirectoryInfo(dir);
            if (!showHidden && (info.Attributes & FileAttributes.Hidden) != 0) continue;

            var (files, folders) = SafeGetChildCounts(dir);
            yield return new FileSystemItem
            {
                Name = info.Name,
                FullPath = info.FullName,
                IsDirectory = true,
                CreatedDate = info.CreationTime,
                ModifiedDate = info.LastWriteTime,
                LastAccessDate = info.LastAccessTime,
                Attributes = info.Attributes,
                IsHidden = (info.Attributes & FileAttributes.Hidden) != 0,
                IsSystem = (info.Attributes & FileAttributes.System) != 0,
                IsReadOnly = (info.Attributes & FileAttributes.ReadOnly) != 0,
                Owner = GetOwner(info.FullName),
                FileType = "Carpeta de archivos",
                Extension = string.Empty,
                Size = 0,
                ChildFileCount = files,
                ChildFolderCount = folders
            };
        }
    }

    private IEnumerable<FileSystemItem> GetFiles(string path, string filterExtension, bool showHidden)
    {
        string pattern = filterExtension == "*" ? "*.*" : $"*.{filterExtension}";

        foreach (var file in SafeGetFiles(path, pattern))
        {
            var info = new FileInfo(file);
            if (!showHidden && (info.Attributes & FileAttributes.Hidden) != 0) continue;

            yield return new FileSystemItem
            {
                Name = info.Name,
                FullPath = info.FullName,
                IsDirectory = false,
                CreatedDate = info.CreationTime,
                ModifiedDate = info.LastWriteTime,
                LastAccessDate = info.LastAccessTime,
                Attributes = info.Attributes,
                IsReadOnly = info.IsReadOnly,
                IsHidden = (info.Attributes & FileAttributes.Hidden) != 0,
                IsSystem = (info.Attributes & FileAttributes.System) != 0,
                Owner = GetOwner(info.FullName),
                FileType = GetFileType(info.Extension),
                Extension = info.Extension.ToLowerInvariant(),
                Size = info.Length
            };
        }
    }

    // ─── Ordenamiento ────────────────────────────────────────────────────────

    public List<FileSystemItem> SortItems(List<FileSystemItem> items, string sortBy, bool ascending)
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

    // ─── Disco ───────────────────────────────────────────────────────────────

    public List<DriveInfo> GetDrives() =>
        DriveInfo.GetDrives().Where(d => d.IsReady).ToList();

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
                    string ext = string.IsNullOrEmpty(info.Extension)
                        ? "Sin extensión"
                        : info.Extension.ToUpperInvariant();
                    dist[ext] = dist.GetValueOrDefault(ext) + info.Length;
                }
                catch { }
            }
        }
        catch { }
        return dist;
    }

    // ─── CRUD ────────────────────────────────────────────────────────────────

    public bool CreateFolder(string parentPath, string folderName)
    {
        try { Directory.CreateDirectory(Path.Combine(parentPath, folderName)); return true; }
        catch { return false; }
    }

    public bool DeleteItem(string path)
    {
        try
        {
            if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
            else if (File.Exists(path)) File.Delete(path);
            return true;
        }
        catch { return false; }
    }

    public bool RenameItem(string path, string newName)
    {
        try
        {
            string dir = Path.GetDirectoryName(path)!;
            string newPath = Path.Combine(dir, newName);
            if (Directory.Exists(path)) Directory.Move(path, newPath);
            else File.Move(path, newPath);
            return true;
        }
        catch { return false; }
    }

    // ─── Helpers privados ─────────────────────────────────────────────────────

    private static string GetOwner(string path)
    {
        try
        {
            var acl = new FileInfo(path).GetAccessControl();
            var owner = acl.GetOwner(typeof(NTAccount));
            return owner?.ToString() ?? string.Empty;
        }
        catch { return string.Empty; }
    }

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
        try { return (Directory.GetFiles(path).Length, Directory.GetDirectories(path).Length); }
        catch { return (0, 0); }
    }

    private static string GetFileType(string extension) =>
        extension.ToLowerInvariant() switch
        {
            ".txt" => "Archivo de texto",
            ".pdf" => "Documento PDF",
            ".jpg" or ".jpeg" => "Imagen JPEG",
            ".png" => "Imagen PNG",
            ".gif" => "Imagen GIF",
            ".bmp" => "Mapa de bits",
            ".tif" or ".tiff" => "Imagen TIFF",
            ".webp" => "Imagen WebP",
            ".heic" or ".heif" => "Imagen HEIC",
            ".raw" or ".cr2" or ".nef" => "Foto RAW",
            ".svg" => "Gráfico vectorial",
            ".ico" => "Ícono",
            ".mp3" => "Audio MP3",
            ".wav" => "Audio WAV",
            ".ogg" => "Audio OGG",
            ".flac" => "Audio FLAC",
            ".aac" => "Audio AAC",
            ".m4a" => "Audio M4A",
            ".mp4" => "Video MP4",
            ".avi" => "Video AVI",
            ".mkv" => "Video MKV",
            ".mov" => "Video MOV",
            ".wmv" => "Video WMV",
            ".webm" => "Video WebM",
            ".json" => "Archivo JSON",
            ".xml" => "Archivo XML",
            ".html" or ".htm" => "Página Web",
            ".css" => "Hoja de estilos",
            ".cs" => "Código C#",
            ".js" or ".ts" => "JavaScript/TypeScript",
            ".py" => "Script Python",
            ".java" => "Código Java",
            ".cpp" or ".c" or ".h" => "Código C/C++",
            ".zip" or ".rar" or ".7z" => "Archivo comprimido",
            ".tar" or ".gz" => "Archivo tar/gz",
            ".exe" => "Aplicación ejecutable",
            ".dll" => "Biblioteca dinámica",
            ".msi" => "Instalador Windows",
            ".doc" or ".docx" => "Documento Word",
            ".xls" or ".xlsx" => "Hoja de cálculo Excel",
            ".ppt" or ".pptx" => "Presentación PowerPoint",
            ".md" => "Markdown",
            ".log" => "Archivo de registro",
            ".db" or ".sqlite" => "Base de datos",
            ".iso" => "Imagen de disco",
            ".ttf" or ".otf" or ".woff" => "Fuente tipográfica",
            var e when !string.IsNullOrEmpty(e) => $"Archivo {e.ToUpperInvariant()}",
            _ => "Archivo"
        };
}