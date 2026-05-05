// REEMPLAZA todo el archivo:
using FileExplorer.Forms;
using Microsoft.Win32;

namespace FileExplorer;

internal static class Program
{
    private const string AppName = "FileExplorer";

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        ApplicationConfiguration.Initialize();

        // Arrancar con el usuario actual (ruta del perfil del usuario)
        string startPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        Application.Run(new MainForm(startPath));
    }

    /// <summary>
    /// Registra o elimina la app del arranque automático de Windows
    /// para el usuario actual (no requiere permisos de administrador).
    /// </summary>
    public static void SetAutoStart(bool enable)
    {
        const string keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        using var key = Registry.CurrentUser.OpenSubKey(keyPath, writable: true);
        if (key is null) return;

        if (enable)
            key.SetValue(AppName, $"\"{Application.ExecutablePath}\"");
        else
            key.DeleteValue(AppName, throwOnMissingValue: false);
    }

    public static bool IsAutoStartEnabled()
    {
        const string keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        using var key = Registry.CurrentUser.OpenSubKey(keyPath);
        return key?.GetValue(AppName) != null;
    }
}