using FileExplorer.Models;

namespace FileExplorer.Services;

/// <summary>
/// Servicio que determina cómo abrir cada tipo de archivo.
/// Abre visualizadores integrados o delega al sistema operativo según el tipo.
/// </summary>
public class FileOpenerService
{
    // Extensiones manejadas con visualizadores integrados
    private static readonly HashSet<string> ImageExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };

    private static readonly HashSet<string> TextExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".txt", ".json", ".xml", ".csv", ".log", ".md", ".cs", ".js", ".html", ".htm", ".py" };

    private static readonly HashSet<string> AudioExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".mp3", ".wav", ".ogg", ".flac" };

    private static readonly HashSet<string> VideoExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".mp4", ".avi", ".mkv", ".mov", ".wmv" };

    // ─── API pública ─────────────────────────────────────────────────────────

    /// <summary>
    /// Abre el archivo en el visor apropiado según su tipo.
    /// Retorna true si el archivo fue manejado internamente.
    /// </summary>
    public bool OpenFile(string filePath, Form owner)
    {
        if (!File.Exists(filePath)) return false;

        string ext = Path.GetExtension(filePath);

        if (ImageExtensions.Contains(ext))
            return OpenImage(filePath, owner);

        if (TextExtensions.Contains(ext))
            return OpenTextFile(filePath, owner);

        if (AudioExtensions.Contains(ext))
            return OpenAudio(filePath, owner);

        if (VideoExtensions.Contains(ext))
            return OpenVideo(filePath, owner);

        return OpenWithSystem(filePath);
    }

    /// <summary>Determina la categoría de un archivo según su extensión.</summary>
    public FileCategory GetCategory(string extension) =>
        extension.ToLowerInvariant() switch
        {
            var e when ImageExtensions.Contains(e) => FileCategory.Image,
            var e when TextExtensions.Contains(e)  => FileCategory.Text,
            var e when AudioExtensions.Contains(e) => FileCategory.Audio,
            var e when VideoExtensions.Contains(e) => FileCategory.Video,
            ".pdf"                                  => FileCategory.Pdf,
            _                                       => FileCategory.Other
        };

    // ─── Métodos de apertura ─────────────────────────────────────────────────

    private static bool OpenImage(string path, Form owner)
    {
        try
        {
            var form = new Forms.ImageViewerForm(path);
            form.Show(owner);
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al abrir imagen:\n{ex.Message}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private static bool OpenTextFile(string path, Form owner)
    {
        try
        {
            var form = new Forms.TextViewerForm(path);
            form.Show(owner);
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al abrir archivo de texto:\n{ex.Message}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private static bool OpenAudio(string path, Form owner)
    {
        try
        {
            var form = new Forms.AudioPlayerForm(path);
            form.Show(owner);
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al abrir audio:\n{ex.Message}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private static bool OpenVideo(string path, Form owner)
    {
        // Para video usamos el reproductor del sistema operativo,
        // ya que no dependemos de un reproductor de video nativo integrado.
        return OpenWithSystem(path);
    }

    private static bool OpenWithSystem(string path)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudo abrir el archivo:\n{ex.Message}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
    }
}

/// <summary>Categorías de archivos reconocidas por el explorador.</summary>
public enum FileCategory
{
    Image, Text, Audio, Video, Pdf, Other
}
