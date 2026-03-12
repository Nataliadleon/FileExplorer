using NAudio.Wave;

namespace FileExplorer.Forms;

public class AudioPlayerForm : Form
{
    private readonly string _filePath;
    private WaveOutEvent? _waveOut;
    private AudioFileReader? _audioReader;
    private bool _isPlaying;
    private bool _isDragging;

    private TrackBar _progressBar = null!;
    private Label _lblCurrent = null!;
    private Label _lblDuration = null!;
    private Button _btnPlay = null!;
    private TrackBar _volumeBar = null!;
    private System.Windows.Forms.Timer _timer = null!;

    public AudioPlayerForm(string filePath)
    {
        _filePath = filePath;
        InitializeComponent();
        InitializePlayer();
    }

    private void InitializeComponent()
    {
        Text = $"Audio — {Path.GetFileName(_filePath)}";
        Size = new Size(420, 480);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(28, 28, 30);

        var artPanel = new Panel
        {
            Size = new Size(200, 200),
            Location = new Point(110, 16),
            BackColor = Color.FromArgb(50, 50, 55)
        };
        DrawAlbumArt(artPanel);

        var lblTitle = new Label
        {
            Text = Path.GetFileNameWithoutExtension(_filePath),
            AutoSize = false,
            Width = 380,
            Height = 28,
            Location = new Point(20, 228),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            BackColor = Color.Transparent
        };

        _lblCurrent = new Label
        {
            Text = "0:00",
            Location = new Point(20, 272),
            Width = 45,
            ForeColor = Color.FromArgb(174, 174, 178),
            Font = new Font("Segoe UI", 8.5f),
            BackColor = Color.Transparent
        };

        _progressBar = new TrackBar
        {
            Location = new Point(65, 267),
            Width = 275,
            Minimum = 0,
            Maximum = 1000,
            TickStyle = TickStyle.None,
            BackColor = Color.FromArgb(28, 28, 30)
        };
        _progressBar.MouseDown += (_, _) => _isDragging = true;
        _progressBar.MouseUp += (_, _) => { _isDragging = false; SeekToProgress(); };

        _lblDuration = new Label
        {
            Text = "0:00",
            Location = new Point(344, 272),
            Width = 45,
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = Color.FromArgb(174, 174, 178),
            Font = new Font("Segoe UI", 8.5f),
            BackColor = Color.Transparent
        };

        var btnRewind = CreateBtn("⏮", new Point(100, 315));
        _btnPlay = CreateBtn("▶", new Point(173, 310));
        _btnPlay.Size = new Size(64, 64);
        _btnPlay.Font = new Font("Segoe UI Emoji", 18f);
        _btnPlay.BackColor = Color.FromArgb(0, 122, 255);
        var btnStop = CreateBtn("⏹", new Point(250, 315));

        btnRewind.Click += (_, _) => SeekRelative(-10);
        _btnPlay.Click += BtnPlay_Click;
        btnStop.Click += BtnStop_Click;

        var lblVol = new Label
        {
            Text = "🔊",
            Location = new Point(20, 400),
            Width = 30,
            Font = new Font("Segoe UI Emoji", 11f),
            BackColor = Color.Transparent,
            ForeColor = Color.White
        };

        _volumeBar = new TrackBar
        {
            Location = new Point(55, 394),
            Width = 290,
            Minimum = 0,
            Maximum = 100,
            Value = 80,
            TickStyle = TickStyle.None,
            BackColor = Color.FromArgb(28, 28, 30)
        };
        _volumeBar.ValueChanged += (_, _) =>
        {
            if (_audioReader != null)
                _audioReader.Volume = _volumeBar.Value / 100f;
        };

        _timer = new System.Windows.Forms.Timer { Interval = 250 };
        _timer.Tick += Timer_Tick;

        Controls.AddRange(new Control[]
        {
            artPanel, lblTitle, _lblCurrent, _progressBar,
            _lblDuration, btnRewind, _btnPlay, btnStop, lblVol, _volumeBar
        });
    }

    private static Button CreateBtn(string text, Point loc)
    {
        var btn = new Button
        {
            Text = text,
            Location = loc,
            Size = new Size(52, 52),
            Font = new Font("Segoe UI Emoji", 14f),
            BackColor = Color.FromArgb(58, 58, 60),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    // Corregido: sin LinearGradientBrush con PointF que causaba CS1503
    private static void DrawAlbumArt(Panel panel)
    {
        var bmp = new Bitmap(200, 200);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        using var topBrush = new SolidBrush(Color.FromArgb(0, 70, 140));
        using var bottomBrush = new SolidBrush(Color.FromArgb(90, 20, 140));
        g.FillRectangle(topBrush, 0, 0, 200, 100);
        g.FillRectangle(bottomBrush, 0, 100, 200, 100);

        using var font = new Font("Segoe UI Emoji", 64f);
        using var br = new SolidBrush(Color.FromArgb(180, 255, 255, 255));
        var sf = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        g.DrawString("♪", font, br, new RectangleF(0, 0, 200, 200), sf);

        panel.BackgroundImage = bmp;
        panel.BackgroundImageLayout = ImageLayout.Stretch;
    }

    private void InitializePlayer()
    {
        try
        {
            _audioReader = new AudioFileReader(_filePath);
            _waveOut = new WaveOutEvent();
            _waveOut.Init(_audioReader);
            _audioReader.Volume = 0.8f;
            _lblDuration.Text = FormatTime(_audioReader.TotalTime);
            _waveOut.PlaybackStopped += (_, _) =>
            {
                if (IsHandleCreated)
                    Invoke(OnPlaybackStopped);
            };
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar audio:\n{ex.Message}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Close();
        }
    }

    private void BtnPlay_Click(object? sender, EventArgs e)
    {
        if (_waveOut is null) return;
        if (_isPlaying)
        {
            _waveOut.Pause();
            _btnPlay.Text = "▶";
            _isPlaying = false;
            _timer.Stop();
        }
        else
        {
            _waveOut.Play();
            _btnPlay.Text = "⏸";
            _isPlaying = true;
            _timer.Start();
        }
    }

    private void BtnStop_Click(object? sender, EventArgs e)
    {
        _waveOut?.Stop();
        if (_audioReader != null)
            _audioReader.CurrentTime = TimeSpan.Zero;
        OnPlaybackStopped();
    }

    private void OnPlaybackStopped()
    {
        _isPlaying = false;
        _btnPlay.Text = "▶";
        _progressBar.Value = 0;
        _lblCurrent.Text = "0:00";
        _timer.Stop();
    }

    private void SeekRelative(int seconds)
    {
        if (_audioReader is null) return;
        var t = _audioReader.CurrentTime + TimeSpan.FromSeconds(seconds);
        _audioReader.CurrentTime = TimeSpan.FromSeconds(
            Math.Clamp(t.TotalSeconds, 0, _audioReader.TotalTime.TotalSeconds));
    }

    private void SeekToProgress()
    {
        if (_audioReader is null) return;
        double ratio = (double)_progressBar.Value / _progressBar.Maximum;
        _audioReader.CurrentTime = TimeSpan.FromSeconds(
            ratio * _audioReader.TotalTime.TotalSeconds);
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        if (_audioReader is null || _isDragging) return;
        double ratio = _audioReader.CurrentTime.TotalSeconds
                           / _audioReader.TotalTime.TotalSeconds;
        _progressBar.Value = (int)(ratio * _progressBar.Maximum);
        _lblCurrent.Text = FormatTime(_audioReader.CurrentTime);
    }

    private static string FormatTime(TimeSpan t) =>
        t.TotalHours >= 1
            ? $"{(int)t.TotalHours}:{t.Minutes:D2}:{t.Seconds:D2}"
            : $"{t.Minutes}:{t.Seconds:D2}";

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _timer.Stop();
        _waveOut?.Stop();
        _waveOut?.Dispose();
        _audioReader?.Dispose();
        base.OnFormClosed(e);
    }
}