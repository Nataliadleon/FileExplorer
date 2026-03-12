namespace FileExplorer.Helpers;

/// <summary>
/// Ayudante para formatear tamaños de archivo y fechas de forma legible.
/// </summary>
public static class FileSizeHelper
{
    private static readonly string[] SizeSuffixes =
        { "B", "KB", "MB", "GB", "TB", "PB" };

    // ─── Formateo de tamaño ──────────────────────────────────────────────────

    /// <summary>
    /// Convierte un tamaño en bytes a una cadena legible (ej: "1.5 MB", "320 KB").
    /// </summary>
    public static string FormatSize(long bytes)
    {
        if (bytes < 0) return "N/A";
        if (bytes == 0) return "0 B";

        int magnitude = 0;
        double value = bytes;

        while (value >= 1024 && magnitude < SizeSuffixes.Length - 1)
        {
            value /= 1024.0;
            magnitude++;
        }

        return magnitude == 0
            ? $"{value:F0} {SizeSuffixes[magnitude]}"
            : $"{value:F1} {SizeSuffixes[magnitude]}";
    }

    /// <summary>
    /// Formatea bytes a GB para mostrar espacio en disco.
    /// </summary>
    public static string FormatSizeGb(long bytes) =>
        $"{bytes / 1_073_741_824.0:F1} GB";

    // ─── Formateo de fechas ──────────────────────────────────────────────────

    /// <summary>
    /// Formatea una fecha de manera relativa si es reciente, o absoluta si es antigua.
    /// </summary>
    public static string FormatDate(DateTime date)
    {
        var diff = DateTime.Now - date;

        if (diff.TotalMinutes < 1)  return "Hace un momento";
        if (diff.TotalHours   < 1)  return $"Hace {(int)diff.TotalMinutes} min";
        if (diff.TotalDays    < 1)  return $"Hace {(int)diff.TotalHours} h";
        if (diff.TotalDays    < 7)  return $"Hace {(int)diff.TotalDays} días";

        return date.ToString("dd/MM/yyyy HH:mm");
    }

    /// <summary>Retorna la fecha en formato largo estándar.</summary>
    public static string FormatDateLong(DateTime date) =>
        date.ToString("dddd, dd 'de' MMMM 'de' yyyy HH:mm",
            new System.Globalization.CultureInfo("es-MX"));

    // ─── Cálculo de porcentajes ──────────────────────────────────────────────

    /// <summary>
    /// Calcula qué porcentaje representa <paramref name="part"/> del <paramref name="total"/>.
    /// </summary>
    public static double Percentage(long part, long total) =>
        total == 0 ? 0.0 : (double)part / total * 100.0;
}
