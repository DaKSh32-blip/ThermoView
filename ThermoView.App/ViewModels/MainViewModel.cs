using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ThermoView.Core;

namespace ThermoView.App.ViewModels;

// The "brain of the screen". It holds everything the window shows
// and every action the buttons can do. It never touches a button or
// slider directly: the XAML binds to these properties instead.
public class MainViewModel : ViewModelBase
{
    private const double FramesPerSecond = 30;
    private const int FrameBudgetMs = 25;   // max time we let physics take per frame

    private ThermalGrid _grid = null!;
    private double _dt;
    private double _simTime;
    private int[] _pixels = Array.Empty<int>();
    private readonly DispatcherTimer _timer;

    public MainViewModel()
    {
        PlayPauseCommand = new RelayCommand(TogglePlay);
        ResetCommand = new RelayCommand(Reset);

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1 / FramesPerSecond) };
        _timer.Tick += (_, _) => Advance();

        Reset();
        TogglePlay();   // start running right away
    }

    // ---------- things the screen binds to ----------

    public IReadOnlyList<Material> Materials { get; } =
        new[] { Material.FR4, Material.Aluminum, Material.Copper };

    private Material _selectedMaterial = Material.FR4;
    public Material SelectedMaterial
    {
        get => _selectedMaterial;
        set { if (SetField(ref _selectedMaterial, value)) Reset(); }   // new material = fresh board
    }

    private double _fanStrength = 20;
    public double FanStrength
    {
        get => _fanStrength;
        set { if (SetField(ref _fanStrength, value)) _grid.ConvectionCoefficient = value; }   // live, no reset
    }

    private double _simSpeed = 100;   // simulated seconds per real second
    public double SimSpeed
    {
        get => _simSpeed;
        set => SetField(ref _simSpeed, value);
    }

    private WriteableBitmap _heatMap = null!;
    public WriteableBitmap HeatMap
    {
        get => _heatMap;
        private set => SetField(ref _heatMap, value);
    }

    public bool IsRunning => _timer.IsEnabled;
    public string PlayPauseText => IsRunning ? "Pause" : "Play";

    private string _statusText = "";
    public string StatusText { get => _statusText; private set => SetField(ref _statusText, value); }

    private string _speedNote = "";
    public string SpeedNote { get => _speedNote; private set => SetField(ref _speedNote, value); }

    private string _scaleMinText = "";
    public string ScaleMinText { get => _scaleMinText; private set => SetField(ref _scaleMinText, value); }

    private string _scaleMaxText = "";
    public string ScaleMaxText { get => _scaleMaxText; private set => SetField(ref _scaleMaxText, value); }

    public ICommand PlayPauseCommand { get; }
    public ICommand ResetCommand { get; }

    // ---------- actions ----------

    private void TogglePlay()
    {
        if (_timer.IsEnabled) _timer.Stop();
        else _timer.Start();

        // these two are calculated, so we ring the doorbell for them by hand
        OnPropertyChanged(nameof(IsRunning));
        OnPropertyChanged(nameof(PlayPauseText));
    }

    private void Reset()
    {
        // 60mm x 40mm board made of whatever material is picked
        _grid = new ThermalGrid(60, 40, 0.001, SelectedMaterial, 25);
        _grid.ConvectionCoefficient = FanStrength;

        //                       name               x   y   w   h  watts  max°C
        _grid.Chips.Add(new Chip("CPU",             10, 12,  8, 8,  1.2,   85));
        _grid.Chips.Add(new Chip("Power Regulator", 40,  8,  5, 5,  0.6,  105));
        _grid.Chips.Add(new Chip("Memory",          38, 26, 10, 5,  0.5,   85));

        _dt = _grid.MaxStableTimeStep();
        _simTime = 0;
        _pixels = new int[_grid.Width * _grid.Height];
        HeatMap = new WriteableBitmap(_grid.Width, _grid.Height, 96, 96, PixelFormats.Bgr32, null);
        SpeedNote = "";
        Draw();
    }

    // Called ~30 times a second by the timer
    private void Advance()
    {
        // How much simulated time we WANT to cover this frame
        double target = SimSpeed / FramesPerSecond;

        // Run physics steps until we hit the target OR run out of time.
        // The time limit keeps the window responsive even when a material
        // (like copper) needs thousands of tiny steps.
        var clock = Stopwatch.StartNew();
        double covered = 0;
        while (covered < target && clock.ElapsedMilliseconds < FrameBudgetMs)
        {
            _grid.Step(_dt);
            covered += _dt;
        }
        _simTime += covered;

        SpeedNote = covered < target * 0.95
            ? $"Running at {covered * FramesPerSecond:F0}x (max for {SelectedMaterial.Name}: it needs tiny time steps)"
            : $"Running at {SimSpeed:F0}x real time";

        Draw();
    }

    private void Draw()
    {
        double min = _grid.AmbientTemp;
        double hottest = _grid.MaxTemperature();
        double max = Math.Max(hottest, min + 1);

        for (int y = 0; y < _grid.Height; y++)
            for (int x = 0; x < _grid.Width; x++)
            {
                var (r, g, b) = ColorMap.ToColor(_grid.Temperature[x, y], min, max);
                _pixels[y * _grid.Width + x] = (r << 16) | (g << 8) | b;
            }

        HeatMap.WritePixels(new Int32Rect(0, 0, _grid.Width, _grid.Height),
                            _pixels, _grid.Width * 4, 0);

        StatusText = $"Time: {_simTime:F0} s     Hottest point: {hottest:F1} °C";
        ScaleMinText = $"{min:F0} °C";
        ScaleMaxText = $"{max:F0} °C";
    }
}
