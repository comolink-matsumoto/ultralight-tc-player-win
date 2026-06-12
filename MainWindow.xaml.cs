using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace UltraLightTCPlayer;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _timer;
    private string? _sourcePath;
    private TimeSpan? _duration;
    private TimeSpan? _inPoint;
    private TimeSpan? _outPoint;
    private MarkerSelection _selectedMarker = MarkerSelection.None;
    private double _fps = 30.0;
    private bool _isPlaying;
    private bool _isSeeking;
    private bool _isMuted;
    private int _mediaLoadVersion;
    private double _lastVolume = 0.8;

    public MainWindow()
    {
        InitializeComponent();

        Player.Volume = VolumeSlider.Value;

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(80)
        };
        _timer.Tick += (_, _) => UpdatePlaybackUi();
        _timer.Start();
    }

    private void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Video files|*.mp4;*.mov;*.m4v|All files|*.*"
        };

        if (dialog.ShowDialog(this) == true)
        {
            OpenVideo(dialog.FileName);
        }
    }

    private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
    {
        TogglePlayPause();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.O when Keyboard.Modifiers.HasFlag(ModifierKeys.Control):
                OpenButton_Click(sender, e);
                e.Handled = true;
                break;
            case Key.Space:
                TogglePlayPause();
                e.Handled = true;
                break;
            case Key.Right:
                StepFrame(1);
                e.Handled = true;
                break;
            case Key.Left:
                StepFrame(-1);
                e.Handled = true;
                break;
            case Key.I:
                SetInPoint();
                e.Handled = true;
                break;
            case Key.O:
                SetOutPoint();
                e.Handled = true;
                break;
            case Key.Escape:
                ClearSelectedMarker();
                e.Handled = true;
                break;
            case Key.Delete:
                ClearAllMarkers();
                e.Handled = true;
                break;
        }
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
        {
            OpenVideo(files[0]);
        }
    }

    private void Player_MediaOpened(object sender, RoutedEventArgs e)
    {
        if (Player.NaturalDuration.HasTimeSpan)
        {
            _duration = Player.NaturalDuration.TimeSpan;
            SeekSlider.Maximum = Math.Max(0.001, _duration.Value.TotalSeconds);
        }

        _fps = SnapFrameRate(TryReadFrameRate(_sourcePath) ?? 30.0);
        FpsText.Text = $"FPS {_fps:0.###}";
        DurationTimecodeText.Text = _duration.HasValue
            ? FormatTimecode(_duration.Value)
            : "--:--:--:--";
        DropHintText.Visibility = Visibility.Collapsed;
        UpdatePlaybackUi();
    }

    private void Player_MediaEnded(object sender, RoutedEventArgs e)
    {
        Pause();
    }

    private void Player_MediaFailed(object sender, ExceptionRoutedEventArgs e)
    {
        Pause();
        MessageBox.Show(this, e.ErrorException.Message, "Media open failed", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void SeekSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isSeeking = true;

        if (FindVisualAncestor<Thumb>(e.OriginalSource as DependencyObject) is not null)
        {
            return;
        }

        if (_duration.HasValue && SeekSlider.ActualWidth > 0)
        {
            var clickPosition = e.GetPosition(SeekSlider);
            var ratio = Math.Clamp(clickPosition.X / SeekSlider.ActualWidth, 0, 1);
            SeekSlider.Value = ratio * SeekSlider.Maximum;
            SeekTo(TimeSpan.FromSeconds(SeekSlider.Value));
            e.Handled = true;
        }
    }

    private void SeekSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isSeeking = false;
        SeekTo(TimeSpan.FromSeconds(SeekSlider.Value));
    }

    private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (Player is null)
        {
            return;
        }

        Player.Volume = e.NewValue;
        if (e.NewValue > 0)
        {
            _lastVolume = e.NewValue;
            _isMuted = false;
            MuteButton.Content = "Mute";
        }
    }

    private void MuteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isMuted)
        {
            VolumeSlider.Value = _lastVolume <= 0 ? 0.8 : _lastVolume;
            _isMuted = false;
            MuteButton.Content = "Mute";
        }
        else
        {
            _lastVolume = VolumeSlider.Value;
            VolumeSlider.Value = 0;
            _isMuted = true;
            MuteButton.Content = "Muted";
        }
    }

    private void ClearInButton_Click(object sender, RoutedEventArgs e)
    {
        _inPoint = null;
        if (_selectedMarker == MarkerSelection.In)
        {
            _selectedMarker = MarkerSelection.None;
        }
        UpdateMarkerUi();
    }

    private void ClearOutButton_Click(object sender, RoutedEventArgs e)
    {
        _outPoint = null;
        if (_selectedMarker == MarkerSelection.Out)
        {
            _selectedMarker = MarkerSelection.None;
        }
        UpdateMarkerUi();
    }

    private void ClearAllButton_Click(object sender, RoutedEventArgs e)
    {
        ClearAllMarkers();
    }

    private void CopyCommandButton_Click(object sender, RoutedEventArgs e)
    {
        if (CopyCommandButton.IsEnabled && !string.IsNullOrWhiteSpace(FfmpegCommandText.Text))
        {
            Clipboard.SetText(FfmpegCommandText.Text);
        }
    }

    private void OpenVideo(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        _sourcePath = path;
        _duration = null;
        _inPoint = null;
        _outPoint = null;
        _selectedMarker = MarkerSelection.None;
        _isPlaying = false;
        var loadVersion = ++_mediaLoadVersion;
        SeekSlider.Value = 0;
        DurationTimecodeText.Text = "--:--:--:--";
        PlayPauseButton.Content = "Play";
        Title = $"UltraLight TC Player - {Path.GetFileName(path)}";
        Player.Source = new Uri(path);
        UpdateMarkerUi();

        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
        {
            if (loadVersion == _mediaLoadVersion)
            {
                Play();
            }
        });
    }

    private void TogglePlayPause()
    {
        if (_sourcePath is null)
        {
            return;
        }

        if (_isPlaying)
        {
            Pause();
        }
        else
        {
            Play();
        }
    }

    private void Play()
    {
        Player.Play();
        _isPlaying = true;
        PlayPauseButton.Content = "Pause";
    }

    private void Pause()
    {
        Player.Pause();
        _isPlaying = false;
        PlayPauseButton.Content = "Play";
    }

    private void StepFrame(int direction)
    {
        if (_sourcePath is null)
        {
            return;
        }

        Pause();
        var next = Player.Position + TimeSpan.FromSeconds(direction / _fps);
        SeekTo(ClampPosition(next));
        UpdatePlaybackUi();
    }

    private void SeekTo(TimeSpan position)
    {
        Player.Position = ClampPosition(position);
        UpdatePlaybackUi();
    }

    private TimeSpan ClampPosition(TimeSpan position)
    {
        if (position < TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        if (_duration.HasValue && position > _duration.Value)
        {
            return _duration.Value;
        }

        return position;
    }

    private void SetInPoint()
    {
        if (_sourcePath is null)
        {
            return;
        }

        _inPoint = Player.Position;
        _selectedMarker = MarkerSelection.In;
        UpdateMarkerUi();
    }

    private void SetOutPoint()
    {
        if (_sourcePath is null)
        {
            return;
        }

        _outPoint = Player.Position;
        _selectedMarker = MarkerSelection.Out;
        UpdateMarkerUi();
    }

    private void ClearSelectedMarker()
    {
        if (_selectedMarker == MarkerSelection.In)
        {
            _inPoint = null;
        }
        else if (_selectedMarker == MarkerSelection.Out)
        {
            _outPoint = null;
        }

        _selectedMarker = MarkerSelection.None;
        UpdateMarkerUi();
    }

    private void ClearAllMarkers()
    {
        _inPoint = null;
        _outPoint = null;
        _selectedMarker = MarkerSelection.None;
        UpdateMarkerUi();
    }

    private void UpdatePlaybackUi()
    {
        if (_sourcePath is null)
        {
            return;
        }

        var position = Player.Position;
        CurrentTimecodeText.Text = FormatTimecode(position);

        if (!_isSeeking)
        {
            SeekSlider.Value = Math.Min(SeekSlider.Maximum, Math.Max(SeekSlider.Minimum, position.TotalSeconds));
        }
    }

    private void UpdateMarkerUi()
    {
        InTimecodeText.Text = _inPoint.HasValue ? FormatTimecode(_inPoint.Value) : "--:--:--:--";
        OutTimecodeText.Text = _outPoint.HasValue ? FormatTimecode(_outPoint.Value) : "--:--:--:--";
        InTimecodeText.Foreground = _selectedMarker == MarkerSelection.In ? System.Windows.Media.Brushes.White : System.Windows.Media.Brushes.LightGray;
        OutTimecodeText.Foreground = _selectedMarker == MarkerSelection.Out ? System.Windows.Media.Brushes.White : System.Windows.Media.Brushes.LightGray;

        var command = BuildFfmpegCommand();
        FfmpegCommandText.Text = command.message;
        CopyCommandButton.IsEnabled = command.canCopy;
    }

    private (string message, bool canCopy) BuildFfmpegCommand()
    {
        if (_sourcePath is null)
        {
            return ("動画ファイルを開いてください", false);
        }

        if (!_inPoint.HasValue || !_outPoint.HasValue)
        {
            return ("IN / OUTを設定するとffmpegコマンドを生成します", false);
        }

        if (_outPoint.Value <= _inPoint.Value)
        {
            return ("OUTはINより後ろに設定してください", false);
        }

        var directory = Path.GetDirectoryName(_sourcePath) ?? "";
        var name = Path.GetFileNameWithoutExtension(_sourcePath);
        var extension = Path.GetExtension(_sourcePath);
        var outputPath = Path.Combine(directory, $"{name}-cut{extension}");

        var command = $"ffmpeg -ss {FormatFfmpegTime(_inPoint.Value)} -to {FormatFfmpegTime(_outPoint.Value)} -i \"{EscapePath(_sourcePath)}\" -c copy \"{EscapePath(outputPath)}\"";
        return (command, true);
    }

    private string FormatTimecode(TimeSpan position)
    {
        var frameBase = Math.Max(1, (int)Math.Round(_fps));
        var totalFrames = Math.Max(0, (long)Math.Round(position.TotalSeconds * _fps));
        var framesPerHour = frameBase * 60 * 60;
        var framesPerMinute = frameBase * 60;

        var hours = totalFrames / framesPerHour;
        var minutes = totalFrames % framesPerHour / framesPerMinute;
        var seconds = totalFrames % framesPerMinute / frameBase;
        var frames = totalFrames % frameBase;

        return $"{hours:00}:{minutes:00}:{seconds:00}:{frames:00}";
    }

    private static string FormatFfmpegTime(TimeSpan position)
    {
        var totalHours = (int)Math.Floor(position.TotalHours);
        return $"{totalHours:00}:{position.Minutes:00}:{position.Seconds:00}.{position.Milliseconds:000}";
    }

    private static string EscapePath(string path)
    {
        return path.Replace("\"", "\\\"");
    }

    private static T? FindVisualAncestor<T>(DependencyObject? element) where T : DependencyObject
    {
        while (element is not null)
        {
            if (element is T match)
            {
                return match;
            }

            element = VisualTreeHelper.GetParent(element);
        }

        return null;
    }

    private static double? TryReadFrameRate(string? path)
    {
        if (path is null)
        {
            return null;
        }

        try
        {
            var shellType = Type.GetTypeFromProgID("Shell.Application");
            if (shellType is null)
            {
                return null;
            }

            dynamic shell = Activator.CreateInstance(shellType)!;
            var folder = shell.NameSpace(Path.GetDirectoryName(path));
            var item = folder?.ParseName(Path.GetFileName(path));
            var rawValue = item?.ExtendedProperty("System.Video.FrameRate");

            if (rawValue is null)
            {
                return null;
            }

            string rawText = rawValue.ToString();
            if (double.TryParse(rawText, out double value) && value > 0)
            {
                return value > 1000 ? value / 1000.0 : value;
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static double SnapFrameRate(double value)
    {
        double[] commonRates = [23.976, 24.0, 25.0, 29.97, 30.0, 59.94, 60.0];
        return commonRates
            .OrderBy(rate => Math.Abs(rate - value))
            .FirstOrDefault(value);
    }

    private enum MarkerSelection
    {
        None,
        In,
        Out
    }
}
