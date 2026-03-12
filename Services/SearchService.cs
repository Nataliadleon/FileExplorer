using FileExplorer.Models;

namespace FileExplorer.Services;

/// <summary>
/// Servicio de búsqueda de archivos y carpetas.
/// Soporta búsqueda por nombre con opción recursiva.
/// </summary>
public class SearchService
{
    private CancellationTokenSource? _cancellationSource;

    // ─── API pública ─────────────────────────────────────────────────────────

    /// <summary>
    /// Realiza una búsqueda asíncrona de archivos que coincidan con el término dado.
    /// </summary>
    /// <param name="rootPath">Directorio raíz donde iniciar la búsqueda.</param>
    /// <param name="searchTerm">Término de búsqueda (acepta wildcards: *).</param>
    /// <param name="recursive">Si true, busca en todos los subdirectorios.</param>
    /// <param name="progress">Callback para reportar resultados encontrados en tiempo real.</param>
    public async Task SearchAsync(
        string rootPath,
        string searchTerm,
        bool recursive,
        IProgress<FileSystemItem> progress)
    {
        CancelPendingSearch();
        _cancellationSource = new CancellationTokenSource();
        var token = _cancellationSource.Token;

        await Task.Run(() =>
        {
            try
            {
                var searchOption = recursive
                    ? SearchOption.AllDirectories
                    : SearchOption.TopDirectoryOnly;

                string pattern = searchTerm.Contains('*')
                    ? searchTerm
                    : $"*{searchTerm}*";

                // Buscar archivos
                foreach (var file in SafeEnumerateFiles(rootPath, pattern, searchOption))
                {
                    if (token.IsCancellationRequested) break;
                    var item = BuildFileItem(file);
                    progress.Report(item);
                }

                // Buscar carpetas
                foreach (var dir in SafeEnumerateDirectories(rootPath, pattern, searchOption))
                {
                    if (token.IsCancellationRequested) break;
                    var item = BuildDirectoryItem(dir);
                    progress.Report(item);
                }
            }
            catch (OperationCanceledException) { }
        }, token);
    }

    /// <summary>
    /// Realiza una búsqueda sincrónica simple y devuelve todos los resultados.
    /// Adecuada para búsquedas locales de pocos archivos.
    /// </summary>
    public List<FileSystemItem> SearchSync(string rootPath, string searchTerm)
    {
        var results = new List<FileSystemItem>();
        string pattern = searchTerm.Contains('*') ? searchTerm : $"*{searchTerm}*";

        try
        {
            foreach (var file in Directory.GetFiles(rootPath, pattern, SearchOption.TopDirectoryOnly))
                results.Add(BuildFileItem(file));

            foreach (var dir in Directory.GetDirectories(rootPath, pattern, SearchOption.TopDirectoryOnly))
                results.Add(BuildDirectoryItem(dir));
        }
        catch { }

        return results;
    }

    /// <summary>Cancela cualquier búsqueda asíncrona en curso.</summary>
    public void CancelPendingSearch()
    {
        _cancellationSource?.Cancel();
        _cancellationSource?.Dispose();
        _cancellationSource = null;
    }

    // ─── Métodos de construcción de ítems ───────────────────────────────────

    private static FileSystemItem BuildFileItem(string filePath)
    {
        var info = new FileInfo(filePath);
        return new FileSystemItem
        {
            Name         = info.Name,
            FullPath     = info.FullName,
            IsDirectory  = false,
            Size         = info.Length,
            Extension    = info.Extension.ToLowerInvariant(),
            FileType     = $"Archivo {info.Extension.ToUpperInvariant()}",
            CreatedDate  = info.CreationTime,
            ModifiedDate = info.LastWriteTime
        };
    }

    private static FileSystemItem BuildDirectoryItem(string dirPath)
    {
        var info = new DirectoryInfo(dirPath);
        return new FileSystemItem
        {
            Name         = info.Name,
            FullPath     = info.FullName,
            IsDirectory  = true,
            FileType     = "Carpeta de archivos",
            CreatedDate  = info.CreationTime,
            ModifiedDate = info.LastWriteTime
        };
    }

    // ─── Enumeración segura (evita UnauthorizedAccessException) ─────────────

    private static IEnumerable<string> SafeEnumerateFiles(
        string path, string pattern, SearchOption option)
    {
        var dirs = new Queue<string>();
        dirs.Enqueue(path);

        while (dirs.Count > 0)
        {
            string current = dirs.Dequeue();

            string[] files = Array.Empty<string>();
            try { files = Directory.GetFiles(current, pattern); } catch { }

            foreach (var f in files)
                yield return f;

            if (option == SearchOption.AllDirectories)
            {
                string[] subs = Array.Empty<string>();
                try { subs = Directory.GetDirectories(current); } catch { }
                foreach (var d in subs) dirs.Enqueue(d);
            }
        }
    }

    private static IEnumerable<string> SafeEnumerateDirectories(
        string path, string pattern, SearchOption option)
    {
        var dirs = new Queue<string>();
        dirs.Enqueue(path);

        while (dirs.Count > 0)
        {
            string current = dirs.Dequeue();

            string[] subs = Array.Empty<string>();
            try { subs = Directory.GetDirectories(current, pattern); } catch { }

            foreach (var d in subs)
                yield return d;

            if (option == SearchOption.AllDirectories)
            {
                string[] allSubs = Array.Empty<string>();
                try { allSubs = Directory.GetDirectories(current); } catch { }
                foreach (var d in allSubs) dirs.Enqueue(d);
            }
        }
    }
}
