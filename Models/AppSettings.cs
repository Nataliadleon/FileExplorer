namespace FileExplorer.Models;

/// <summary>
/// Configuración global de la aplicación.
/// Mantiene el estado de preferencias del usuario durante la sesión.
/// </summary>
public class AppSettings
{
    /// <summary>Activa el modo oscuro de la interfaz.</summary>
    public bool IsDarkMode { get; set; } = false;

    /// <summary>Muestra los archivos en vista cuadrícula (true) o lista (false).</summary>
    public bool IsGridView { get; set; } = false;

    /// <summary>Última ruta visitada.</summary>
    public string LastPath { get; set; } =
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

    /// <summary>Campo por el que se ordena: "Name", "Size", "Date".</summary>
    public string SortBy { get; set; } = "Name";

    /// <summary>Orden ascendente (true) o descendente (false).</summary>
    public bool SortAscending { get; set; } = true;

    /// <summary>Muestra el panel de vista previa lateral.</summary>
    public bool ShowPreview { get; set; } = true;

    /// <summary>
    /// Extensión de filtro activa. "*" = todos los archivos.
    /// Ejemplo: "jpg", "txt", "mp3".
    /// </summary>
    public string FilterExtension { get; set; } = "*";
}
