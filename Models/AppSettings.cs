// REEMPLAZA todo el archivo:
namespace FileExplorer.Models;

/// <summary>
/// Configuración global de la aplicación.
/// </summary>
public class AppSettings
{
    public bool IsDarkMode { get; set; } = false;
    public bool IsGridView { get; set; } = false;
    public string LastPath { get; set; } =
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    public string SortBy { get; set; } = "Name";
    public bool SortAscending { get; set; } = true;
    public bool ShowPreview { get; set; } = true;
    public string FilterExtension { get; set; } = "*";
    public bool ShowHiddenFiles { get; set; } = false;
}