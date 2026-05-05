// Forms/PropertiesForm.cs  — ARCHIVO NUEVO
using FileExplorer.Helpers;
using FileExplorer.Models;

namespace FileExplorer.Forms;

/// <summary>
/// Ventana de propiedades detalladas de un archivo o carpeta.
/// Muestra más de 10 datos incluyendo coordenadas GPS para fotos.
/// </summary>
public partial class PropertiesForm : Form
{
    private readonly FileSystemItem _item;

    public PropertiesForm(FileSystemItem item)
    {
        _item = item;

        // Cargar EXIF si es imagen (puede no estar cargado aún)
        ExifHelper.PopulateExif(_item);

        Text = $"Propiedades — {item.Name}";
        Size = new Size(520, 640);
        MinimumSize = new Size(420, 500);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        BuildUI();
    }

    private void BuildUI()
    {
        var tabs = new TabControl { Dock = DockStyle.Fill };

        tabs.TabPages.Add(BuildGeneralTab());
        tabs.TabPages.Add(BuildDetailsTab());

        if (_item.GpsLatitude.HasValue && _item.GpsLongitude.HasValue)
            tabs.TabPages.Add(BuildGpsTab());

        var btnClose = new Button
        {
            Text = "Cerrar",
            DialogResult = DialogResult.OK,
            Dock = DockStyle.Bottom,
            Height = 34
        };

        Controls.Add(tabs);
        Controls.Add(btnClose);
    }

    // ─── Pestaña General ────────────────────────────────────────────────────

    private TabPage BuildGeneralTab()
    {
        var page = new TabPage("General");
        var lv = CreateListView();

        AddRow(lv, "Nombre", _item.Name);
        AddRow(lv, "Ruta completa", _item.FullPath);
        AddRow(lv, "Tipo", _item.IsDirectory ? "Carpeta de archivos" : _item.FileType);
        AddRow(lv, "Extensión", string.IsNullOrEmpty(_item.Extension) ? "(ninguna)" : _item.Extension.ToUpperInvariant());
        AddRow(lv, "Tamaño", _item.IsDirectory ? "—" : FormatBytes(_item.Size));
        AddRow(lv, "Tamaño (bytes)", _item.IsDirectory ? "—" : $"{_item.Size:N0} bytes");
        AddRow(lv, "Creado", _item.CreatedDate.ToString("dd/MM/yyyy  HH:mm:ss"));
        AddRow(lv, "Modificado", _item.ModifiedDate.ToString("dd/MM/yyyy  HH:mm:ss"));
        AddRow(lv, "Último acceso", _item.LastAccessDate.ToString("dd/MM/yyyy  HH:mm:ss"));
        AddRow(lv, "Sólo lectura", _item.IsReadOnly ? "Sí" : "No");
        AddRow(lv, "Oculto", _item.IsHidden ? "Sí" : "No");
        AddRow(lv, "Sistema", _item.IsSystem ? "Sí" : "No");
        AddRow(lv, "Propietario", string.IsNullOrEmpty(_item.Owner) ? "Desconocido" : _item.Owner);

        if (_item.IsDirectory)
        {
            AddRow(lv, "Archivos directos", _item.ChildFileCount.ToString());
            AddRow(lv, "Subcarpetas directas", _item.ChildFolderCount.ToString());
        }

        page.Controls.Add(lv);
        return page;
    }

    // ─── Pestaña Detalles (EXIF) ─────────────────────────────────────────────

    private TabPage BuildDetailsTab()
    {
        var page = new TabPage("Detalles");
        var lv = CreateListView();

        if (_item.ImageWidth.HasValue && _item.ImageHeight.HasValue)
        {
            AddRow(lv, "Dimensiones", $"{_item.ImageWidth} × {_item.ImageHeight} px");
            AddRow(lv, "Ancho", $"{_item.ImageWidth} px");
            AddRow(lv, "Alto", $"{_item.ImageHeight} px");
        }

        if (!string.IsNullOrEmpty(_item.CameraMake)) AddRow(lv, "Fabricante cámara", _item.CameraMake!);
        if (!string.IsNullOrEmpty(_item.CameraModel)) AddRow(lv, "Modelo cámara", _item.CameraModel!);
        if (!string.IsNullOrEmpty(_item.DateTaken)) AddRow(lv, "Fecha de captura", _item.DateTaken!);
        if (!string.IsNullOrEmpty(_item.ExposureTime)) AddRow(lv, "Tiempo exposición", _item.ExposureTime!);
        if (!string.IsNullOrEmpty(_item.FNumber)) AddRow(lv, "Apertura (f/)", _item.FNumber!);
        if (!string.IsNullOrEmpty(_item.IsoSpeed)) AddRow(lv, "ISO", _item.IsoSpeed!);
        if (!string.IsNullOrEmpty(_item.Flash)) AddRow(lv, "Flash", _item.Flash!);
        if (!string.IsNullOrEmpty(_item.ColorSpace)) AddRow(lv, "Espacio de color", _item.ColorSpace!);

        if (_item.GpsLatitude.HasValue)
        {
            AddRow(lv, "GPS Latitud", FormatCoord(_item.GpsLatitude.Value, "N", "S"));
            AddRow(lv, "GPS Longitud", FormatCoord(_item.GpsLongitude!.Value, "E", "O"));
        }
        if (_item.GpsAltitude.HasValue)
            AddRow(lv, "Altitud GPS", $"{_item.GpsAltitude.Value:F1} m");

        if (lv.Items.Count == 0)
            AddRow(lv, "Info", "No hay datos EXIF disponibles");

        page.Controls.Add(lv);
        return page;
    }

    // ─── Pestaña GPS ─────────────────────────────────────────────────────────

    private TabPage BuildGpsTab()
    {
        var page = new TabPage("Ubicación GPS");

        var lbl = new Label
        {
            Dock = DockStyle.Top,
            Height = 80,
            Padding = new Padding(8),
            Font = new Font("Segoe UI", 9.5f),
            Text = $"📍 Coordenadas GPS\n\n" +
                   $"Latitud:  {_item.GpsLatitude:F6}° {(_item.GpsLatitude >= 0 ? "N" : "S")}\n" +
                   $"Longitud: {_item.GpsLongitude:F6}° {(_item.GpsLongitude >= 0 ? "E" : "O")}\n" +
                   (_item.GpsAltitude.HasValue ? $"Altitud:  {_item.GpsAltitude:F1} m" : "")
        };

        var btnMap = new Button
        {
            Text = "🗺  Abrir en Google Maps",
            Height = 36,
            Dock = DockStyle.Top,
            Margin = new Padding(8)
        };
        btnMap.Click += (_, _) =>
        {
            string url = $"https://maps.google.com/?q={_item.GpsLatitude:F6},{_item.GpsLongitude:F6}";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        };

        page.Controls.Add(btnMap);
        page.Controls.Add(lbl);
        return page;
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static ListView CreateListView()
    {
        var lv = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            HeaderStyle = ColumnHeaderStyle.Nonclickable
        };
        lv.Columns.Add("Propiedad", 170);
        lv.Columns.Add("Valor", 300);
        return lv;
    }

    private static void AddRow(ListView lv, string key, string value)
    {
        var item = new ListViewItem(key);
        item.SubItems.Add(value);
        lv.Items.Add(item);
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes >= 1_073_741_824) return $"{bytes / 1_073_741_824.0:F2} GB";
        if (bytes >= 1_048_576) return $"{bytes / 1_048_576.0:F2} MB";
        if (bytes >= 1_024) return $"{bytes / 1_024.0:F1} KB";
        return $"{bytes} bytes";
    }

    private static string FormatCoord(double val, string pos, string neg) =>
        $"{Math.Abs(val):F6}° {(val >= 0 ? pos : neg)}";

    private void PropertiesForm_Load(object sender, EventArgs e)
    {

    }
}