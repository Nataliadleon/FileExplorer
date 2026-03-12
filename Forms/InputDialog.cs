namespace FileExplorer.Forms;

/// <summary>
/// Diálogo modal reutilizable para obtener entrada de texto del usuario.
/// Usado en operaciones de crear carpeta y renombrar.
/// </summary>
public class InputDialog : Form
{
    private readonly TextBox _input = null!;

    /// <summary>Texto ingresado por el usuario al confirmar.</summary>
    public string InputText => _input.Text;

    /// <summary>
    /// Crea el diálogo con el título, etiqueta y valor predeterminado especificados.
    /// </summary>
    public InputDialog(string title, string labelText, string defaultValue = "")
    {
        Text           = title;
        Size           = new Size(400, 160);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox    = false;
        MinimizeBox    = false;
        StartPosition  = FormStartPosition.CenterParent;
        BackColor      = Color.FromArgb(44, 44, 46);

        var lbl = new Label
        {
            Text      = labelText,
            Location  = new Point(16, 16),
            Width     = 360,
            ForeColor = Color.White,
            Font      = new Font("Segoe UI", 9.5f)
        };

        _input = new TextBox
        {
            Text     = defaultValue,
            Location = new Point(16, 40),
            Width    = 355,
            Font     = new Font("Segoe UI", 10f),
            BackColor = Color.FromArgb(58, 58, 60),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        var btnOk = new Button
        {
            Text         = "Aceptar",
            DialogResult = DialogResult.OK,
            Location     = new Point(195, 80),
            Width        = 85,
            Height       = 30,
            BackColor    = Color.FromArgb(0, 122, 255),
            ForeColor    = Color.White,
            FlatStyle    = FlatStyle.Flat,
            Font         = new Font("Segoe UI", 9f)
        };
        btnOk.FlatAppearance.BorderSize = 0;

        var btnCancel = new Button
        {
            Text         = "Cancelar",
            DialogResult = DialogResult.Cancel,
            Location     = new Point(290, 80),
            Width        = 85,
            Height       = 30,
            BackColor    = Color.FromArgb(72, 72, 74),
            ForeColor    = Color.White,
            FlatStyle    = FlatStyle.Flat,
            Font         = new Font("Segoe UI", 9f)
        };
        btnCancel.FlatAppearance.BorderSize = 0;

        AcceptButton = btnOk;
        CancelButton = btnCancel;

        Controls.AddRange(new Control[] { lbl, _input, btnOk, btnCancel });

        Shown += (_, _) =>
        {
            _input.Focus();
            _input.SelectAll();
        };
    }
}
