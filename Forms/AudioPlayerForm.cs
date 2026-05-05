using System.IO;
using System.Drawing;
using System.Windows.Forms;
using LibVLCSharp.Shared;
using TagFile = TagLib.File;

namespace FileExplorer.Forms;

public class AudioPlayerForm : Form
{
    // ─── LibVLC ───────────────────────────────────────────────────────────────
    private readonly LibVLC _libVlc;
    private readonly MediaPlayer _mediaPlayer;

    // ─── Estado ───────────────────────────────────────────────────────────────
    private readonly List<string> _playlist = new();
    private readonly List<string> _shuffled = new();
    private int _currentIndex = -1;
    private bool _isShuffled = false;
    private bool _repeat = false;
    private bool _isDragging = false;
    private string? _currentFolder = null;   // carpeta de la canción actual
    private readonly System.Windows.Forms.Timer _ticker = new() { Interval = 500 };

    // ─── Colores lista ────────────────────────────────────────────────────────
    private static readonly Color ColorPlaying = Color.FromArgb(80, 130, 200);
    private static readonly Color ColorNormal = Color.FromArgb(32, 32, 44);

    // ─── Controles ────────────────────────────────────────────────────────────
    private PictureBox _cover = null!;
    private Label _lblTitle = null!;
    private Label _lblArtist = null!;
    private Label _lblAlbum = null!;
    private Label _lblTime = null!;
    private TrackBar _trackPos = null!;
    private TrackBar _trackVol = null!;
    private Button _btnPrev = null!;
    private Button _btnPlay = null!;
    private Button _btnNext = null!;
    private Button _btnShuffle = null!;
    private Button _btnRepeat = null!;
    private ListBox _lstPlaylist = null!;

    // ─── Constructor ──────────────────────────────────────────────────────────
    public AudioPlayerForm(IEnumerable<string>? initialFiles = null)
    {
        Core.Initialize();
        _libVlc = new LibVLC();
        _mediaPlayer = new MediaPlayer(_libVlc);

        _mediaPlayer.EndReached += (_, _) => BeginInvoke(OnTrackEnded);
        _mediaPlayer.TimeChanged += (_, e) => BeginInvoke(() => OnTimeChanged(e.Time));

        BuildUI();
        _ticker.Tick += OnTick;

        if (initialFiles != null)
            AddFiles(initialFiles);
    }

    // ─── UI ───────────────────────────────────────────────────────────────────

    private void BuildUI()
    {
        Text = "🎵 Reproductor";
        Size = new Size(700, 620);
        MinimumSize = new Size(700, 620);
        MaximumSize = new Size(700, 620);
        BackColor = Color.FromArgb(24, 24, 32);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9.5f);
        FormBorderStyle = FormBorderStyle.FixedSingle;

        // ── Portada  (16,16)  180×180 ────────────────────────────────────────
        _cover = new PictureBox
        {
            Location = new Point(16, 16),
            Size = new Size(180, 180),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(40, 40, 55)
        };
        _cover.Image = DefaultCover();

        // ── Metadata  columna derecha desde X=210 ────────────────────────────
        _lblTitle = MakeLabel("Sin canción", 14, FontStyle.Bold, new Point(210, 16));
        _lblArtist = MakeLabel("—", 11, FontStyle.Regular, new Point(210, 46));
        _lblAlbum = MakeLabel("—", 10, FontStyle.Italic, new Point(210, 70));
        _lblTime = MakeLabel("0:00 / 0:00", 9, FontStyle.Regular, new Point(210, 94));

        // ── Barra de progreso  Y=120  (debajo del tiempo, sin solapar) ────────
        _trackPos = new TrackBar
        {
            Location = new Point(206, 112),
            Size = new Size(474, 30),
            Minimum = 0,
            Maximum = 1000,
            TickStyle = TickStyle.None,
            BackColor = Color.FromArgb(24, 24, 32)
        };
        _trackPos.MouseDown += (_, _) => _isDragging = true;
        _trackPos.MouseUp += (_, _) =>
        {
            _isDragging = false;
            if (_mediaPlayer.Length > 0)
                _mediaPlayer.Time = (long)(_trackPos.Value / 1000.0 * _mediaPlayer.Length);
        };

        // ── Volumen  Y=148  (debajo del trackbar) ────────────────────────────
        var lblVol = MakeLabel("🔊", 9, FontStyle.Regular, new Point(210, 150));
        _trackVol = new TrackBar
        {
            Location = new Point(235, 144),
            Size = new Size(160, 28),
            Minimum = 0,
            Maximum = 100,
            Value = 80,
            TickStyle = TickStyle.None,
            BackColor = Color.FromArgb(24, 24, 32)
        };
        _mediaPlayer.Volume = 80;
        _trackVol.Scroll += (_, _) => _mediaPlayer.Volume = _trackVol.Value;

        // ── Botones transporte  Y=180  (debajo del volumen, sin solapar) ──────
        // La portada termina en Y=196 (16+180), los botones van a Y=200
        // para que queden debajo tanto de la portada como del volumen
        int btnY = 180;
        _btnPrev = MakeBtn("⏮", new Point(210, btnY));
        _btnPlay = MakeBtn("▶", new Point(262, btnY));
        _btnNext = MakeBtn("⏭", new Point(314, btnY));
        _btnShuffle = MakeBtn("🔀", new Point(380, btnY));
        _btnRepeat = MakeBtn("🔁", new Point(432, btnY));

        _btnPrev.Click += (_, _) => PlayPrevious();
        _btnPlay.Click += (_, _) => TogglePlay();
        _btnNext.Click += (_, _) => PlayNext();
        _btnShuffle.Click += (_, _) => ToggleShuffle();
        _btnRepeat.Click += (_, _) => ToggleRepeat();

        // ── Separador  Y=218 ──────────────────────────────────────────────────
        var sep = new Panel
        {
            Location = new Point(0, 218),
            Size = new Size(700, 1),
            BackColor = Color.FromArgb(60, 60, 80)
        };

        // ── Header playlist ───────────────────────────────────────────────────
        var lblList = MakeLabel("Lista de reproducción", 9, FontStyle.Bold, new Point(16, 226));

        // ── ListBox  Y=248  altura=278 → llega a Y=526 ────────────────────────
        _lstPlaylist = new ListBox
        {
            Location = new Point(16, 248),
            Size = new Size(662, 278),
            BackColor = ColorNormal,
            ForeColor = Color.White,
            BorderStyle = BorderStyle.None,
            SelectionMode = SelectionMode.MultiExtended,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 22
        };
        _lstPlaylist.DrawItem += OnDrawPlaylistItem;
        _lstPlaylist.DoubleClick += (_, _) =>
        {
            if (_lstPlaylist.SelectedIndex >= 0)
                PlayAt(_lstPlaylist.SelectedIndex);
        };

        // ── Botones  Y=534  (248+278+8) ───────────────────────────────────────
        var btnAdd = MakeSmallBtn("➕ Agregar canciones", new Point(16, 534));
        var btnImport = MakeSmallBtn("📋 Importar .m3u", new Point(185, 534));
        var btnRemove = MakeSmallBtn("✕ Quitar", new Point(354, 534));
        var btnExport = MakeSmallBtn("💾 Exportar .m3u", new Point(523, 534));

        btnAdd.Click += (_, _) => AddSongs();
        btnImport.Click += (_, _) => ImportM3u();
        btnRemove.Click += (_, _) => RemoveSelected();
        btnExport.Click += (_, _) => ExportM3u();

        Controls.AddRange(new Control[]
        {
            _cover,
            _lblTitle, _lblArtist, _lblAlbum, _lblTime,
            _trackPos, lblVol, _trackVol,
            _btnPrev, _btnPlay, _btnNext, _btnShuffle, _btnRepeat,
            sep, lblList, _lstPlaylist,
            btnAdd, btnImport, btnRemove, btnExport
        });
    }

    // ─── DrawItem — colores de la lista ───────────────────────────────────────

    private void OnDrawPlaylistItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _lstPlaylist.Items.Count) return;

        bool isActive = e.Index == (_isShuffled
            ? _shuffled.IndexOf(_currentIndex >= 0 && _currentIndex < CurrentList.Count
                ? CurrentList[_currentIndex] : "")
            : _currentIndex);

        Color bg = isActive
            ? ColorPlaying
            : (e.State & DrawItemState.Selected) != 0
                ? Color.FromArgb(55, 55, 75)
                : ColorNormal;

        Color fg = Color.White;

        e.Graphics.FillRectangle(new SolidBrush(bg), e.Bounds);

        string text = _lstPlaylist.Items[e.Index]?.ToString() ?? "";
        if (isActive) text = "▶  " + text;

        e.Graphics.DrawString(text, e.Font ?? Font, new SolidBrush(fg),
            e.Bounds.X + 4, e.Bounds.Y + 3);
    }

    // ─── Reproducción ─────────────────────────────────────────────────────────

    public void PlayAt(int index)
    {
        if (index < 0 || index >= CurrentList.Count) return;
        _currentIndex = index;
        string path = CurrentList[_currentIndex];

        _currentFolder = Path.GetDirectoryName(path);

        _mediaPlayer.Stop();
        LoadMetadata(path);

        using var media = new Media(_libVlc, path, FromType.FromPath);
        _mediaPlayer.Play(media);

        _btnPlay.Text = "⏸";
        _ticker.Start();

        // Resaltar ítem activo
        int listIdx = _isShuffled ? _shuffled.IndexOf(path) : _currentIndex;
        if (listIdx >= 0 && listIdx < _lstPlaylist.Items.Count)
            _lstPlaylist.SelectedIndex = listIdx;

        _lstPlaylist.Invalidate();   // fuerza repintado de colores
    }

    private void TogglePlay()
    {
        if (_mediaPlayer.IsPlaying)
        {
            _mediaPlayer.Pause();
            _btnPlay.Text = "▶";
            _ticker.Stop();
        }
        else if (_currentIndex >= 0)
        {
            _mediaPlayer.Play();
            _btnPlay.Text = "⏸";
            _ticker.Start();
        }
        else if (_playlist.Count > 0)
        {
            PlayAt(0);
        }
    }

    private void PlayNext()
    {
        // Si hay playlist, avanzar en ella
        if (CurrentList.Count > 0)
        {
            PlayAt((_currentIndex + 1) % CurrentList.Count);
            return;
        }

        // Sin playlist: buscar siguiente archivo en la carpeta actual
        if (_currentFolder == null) return;
        var siblings = GetAudioFilesInFolder(_currentFolder);
        if (siblings.Count == 0) return;

        // Encontrar el índice del archivo actual en la carpeta
        string? currentFile = _currentIndex >= 0 && _currentIndex < _playlist.Count
            ? _playlist[_currentIndex] : null;

        int sibIdx = currentFile != null ? siblings.IndexOf(currentFile) : -1;
        int nextIdx = (sibIdx + 1) % siblings.Count;

        // Agregar a playlist y reproducir
        AddFiles(new[] { siblings[nextIdx] });
        PlayAt(_playlist.Count - 1);
    }

    private void PlayPrevious()
    {
        if (CurrentList.Count > 0)
        {
            if (_mediaPlayer.Time > 3000) { PlayAt(_currentIndex); return; }
            PlayAt((_currentIndex - 1 + CurrentList.Count) % CurrentList.Count);
            return;
        }

        if (_currentFolder == null) return;
        var siblings = GetAudioFilesInFolder(_currentFolder);
        if (siblings.Count == 0) return;

        string? currentFile = _currentIndex >= 0 && _currentIndex < _playlist.Count
            ? _playlist[_currentIndex] : null;

        int sibIdx = currentFile != null ? siblings.IndexOf(currentFile) : 0;
        int prevIdx = (_mediaPlayer.Time > 3000)
            ? sibIdx
            : (sibIdx - 1 + siblings.Count) % siblings.Count;

        AddFiles(new[] { siblings[prevIdx] });
        PlayAt(_playlist.IndexOf(siblings[prevIdx]));
    }

    private void OnTrackEnded()
    {
        if (_repeat) PlayAt(_currentIndex);
        else PlayNext();
    }

    // ─── Tiempo ───────────────────────────────────────────────────────────────

    private void OnTimeChanged(long ms)
    {
        if (_isDragging) return;
        long total = _mediaPlayer.Length;
        if (total <= 0) return;

        int cur = (int)(ms / 1000);
        int dur = (int)(total / 1000);
        _lblTime.Text = $"{cur / 60}:{cur % 60:D2} / {dur / 60}:{dur % 60:D2}";

        int pos = (int)((double)ms / total * 1000);
        if (pos >= 0 && pos <= 1000) _trackPos.Value = pos;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (!_mediaPlayer.IsPlaying) return;
        long ms = _mediaPlayer.Time;
        long total = _mediaPlayer.Length;
        if (ms <= 0 || total <= 0) return;
        int cur = (int)(ms / 1000);
        int dur = (int)(total / 1000);
        _lblTime.Text = $"{cur / 60}:{cur % 60:D2} / {dur / 60}:{dur % 60:D2}";
    }

    // ─── Shuffle / Repeat ─────────────────────────────────────────────────────

    private void ToggleShuffle()
    {
        _isShuffled = !_isShuffled;
        _btnShuffle.BackColor = _isShuffled
            ? Color.FromArgb(80, 130, 200)
            : Color.FromArgb(50, 50, 70);

        if (_isShuffled)
        {
            string? current = _currentIndex >= 0 && _currentIndex < _playlist.Count
                ? _playlist[_currentIndex] : null;
            _shuffled.Clear();
            _shuffled.AddRange(_playlist.OrderBy(_ => Guid.NewGuid()));
            _currentIndex = current != null ? _shuffled.IndexOf(current) : 0;
        }

        _lstPlaylist.Invalidate();
    }

    private void ToggleRepeat()
    {
        _repeat = !_repeat;
        _btnRepeat.BackColor = _repeat
            ? Color.FromArgb(80, 130, 200)
            : Color.FromArgb(50, 50, 70);
    }

    private List<string> CurrentList => _isShuffled ? _shuffled : _playlist;

    // ─── Metadata ─────────────────────────────────────────────────────────────

    private void LoadMetadata(string path)
    {
        _lblTitle.Text = Path.GetFileNameWithoutExtension(path);
        _lblArtist.Text = "—";
        _lblAlbum.Text = "—";
        _cover.Image = DefaultCover();

        try
        {
            using var tag = TagFile.Create(path);
            if (!string.IsNullOrWhiteSpace(tag.Tag.Title))
                _lblTitle.Text = tag.Tag.Title;
            if (tag.Tag.Performers?.Length > 0)
                _lblArtist.Text = string.Join(", ", tag.Tag.Performers);
            if (!string.IsNullOrWhiteSpace(tag.Tag.Album))
                _lblAlbum.Text = tag.Tag.Album;

            var pic = tag.Tag.Pictures?.FirstOrDefault();
            if (pic != null)
            {
                using var ms = new MemoryStream(pic.Data.Data);
                _cover.Image = Image.FromStream(ms);
            }
        }
        catch { }

        Text = $"🎵 {_lblTitle.Text}  —  {_lblArtist.Text}";
    }

    // ─── Playlist ─────────────────────────────────────────────────────────────

    public void AddFiles(IEnumerable<string> files)
    {
        var audio = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { ".mp3", ".flac", ".ogg", ".wav", ".aac", ".m4a", ".wma", ".opus" };

        foreach (var f in files.Where(f => audio.Contains(Path.GetExtension(f))))
        {
            if (_playlist.Contains(f)) continue;
            _playlist.Add(f);

            // NUEVO: leer título y artista de los tags
            string displayName = GetDisplayName(f);
            _lstPlaylist.Items.Add(displayName);
        }

        if (_isShuffled)
        {
            _shuffled.Clear();
            _shuffled.AddRange(_playlist.OrderBy(_ => Guid.NewGuid()));
        }

        _lstPlaylist.Invalidate();
    }

    /// <summary>
    /// Devuelve "Artista — Título" si tiene tags, o el nombre de archivo limpio si no.
    /// </summary>
    private static string GetDisplayName(string path)
    {
        try
        {
            using var tag = TagFile.Create(path);

            string title = string.IsNullOrWhiteSpace(tag.Tag.Title)
                ? Path.GetFileNameWithoutExtension(path)
                : tag.Tag.Title;

            string artist = tag.Tag.Performers?.Length > 0
                ? tag.Tag.Performers[0]
                : string.Empty;

            return string.IsNullOrWhiteSpace(artist)
                ? title
                : $"{artist}  —  {title}";
        }
        catch
        {
            // Sin tags: limpiar el nombre de archivo quitando extensión
            return Path.GetFileNameWithoutExtension(path);
        }
    }

    /// <summary>
    /// Abre un selector de archivos dentro del propio programa (sin explorador de Windows).
    /// Muestra los archivos de la carpeta actual del explorador.
    /// </summary>
    private void AddSongs()
    {
        // Obtener la carpeta actual del MainForm padre
        string startFolder = _currentFolder
            ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        using var dlg = new SongPickerForm(startFolder);
        if (dlg.ShowDialog(this) == DialogResult.OK && dlg.SelectedFiles.Count > 0)
            AddFiles(dlg.SelectedFiles);
    }

    private void RemoveSelected()
    {
        foreach (int i in _lstPlaylist.SelectedIndices
                     .Cast<int>().OrderByDescending(x => x))
        {
            _playlist.RemoveAt(i);
            _lstPlaylist.Items.RemoveAt(i);
        }
        if (_isShuffled)
        {
            _shuffled.Clear();
            _shuffled.AddRange(_playlist.OrderBy(_ => Guid.NewGuid()));
        }
        _lstPlaylist.Invalidate();
    }

    private void ImportM3u()
    {
        using var dlg = new OpenFileDialog
        {
            Filter = "Playlist M3U|*.m3u;*.m3u8|Todos|*.*",
            Title = "Importar playlist"
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        string baseDir = Path.GetDirectoryName(dlg.FileName)!;
        var resolved = System.IO.File.ReadAllLines(dlg.FileName)
            .Where(l => !l.StartsWith('#') && !string.IsNullOrWhiteSpace(l))
            .Select(l => Path.IsPathRooted(l) ? l : Path.Combine(baseDir, l))
            .Where(System.IO.File.Exists);

        AddFiles(resolved);
    }

    private void ExportM3u()
    {
        using var dlg = new SaveFileDialog
        {
            Filter = "Playlist M3U|*.m3u",
            FileName = "playlist.m3u"
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        var lines = new List<string> { "#EXTM3U" };
        lines.AddRange(_playlist);
        System.IO.File.WriteAllLines(dlg.FileName, lines);
        MessageBox.Show("Playlist exportada.", "Listo",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static List<string> GetAudioFilesInFolder(string folder)
    {
        var audio = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".mp3", ".flac", ".ogg", ".wav", ".aac", ".m4a", ".wma", ".opus" };
        try
        {
            return Directory.GetFiles(folder)
                .Where(f => audio.Contains(Path.GetExtension(f)))
                .OrderBy(f => f)
                .ToList();
        }
        catch { return new List<string>(); }
    }

    private Label MakeLabel(string text, float size, FontStyle style, Point loc) => new()
    {
        Text = text,
        Font = new Font("Segoe UI", size, style),
        ForeColor = Color.White,
        BackColor = Color.Transparent,
        Location = loc,
        AutoSize = true
    };

    private Button MakeBtn(string text, Point loc) => new()
    {
        Text = text,
        Location = loc,
        Size = new Size(48, 34),
        FlatStyle = FlatStyle.Flat,
        BackColor = Color.FromArgb(50, 50, 70),
        ForeColor = Color.White,
        Font = new Font("Segoe UI Emoji", 12f),
        Cursor = Cursors.Hand
    };

    private Button MakeSmallBtn(string text, Point loc) => new()
    {
        Text = text,
        Location = loc,
        Size = new Size(160, 28),
        FlatStyle = FlatStyle.Flat,
        BackColor = Color.FromArgb(50, 50, 70),
        ForeColor = Color.White,
        Font = new Font("Segoe UI", 9f),
        Cursor = Cursors.Hand
    };

    private static Image DefaultCover()
    {
        var bmp = new Bitmap(180, 180);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.FromArgb(40, 40, 55));
        using var f = new Font("Segoe UI Emoji", 48f);
        g.DrawString("🎵", f, Brushes.Gray, new PointF(40, 55));
        return bmp;
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _ticker.Dispose();
        _mediaPlayer.Stop();
        _mediaPlayer.Dispose();
        _libVlc.Dispose();
        base.OnFormClosed(e);
    }
}