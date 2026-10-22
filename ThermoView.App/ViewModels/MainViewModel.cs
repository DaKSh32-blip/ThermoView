using System.Collections.ObjectModel;
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
    private const double RoomTemp = 25;

    private ThermalGrid _grid = null!;
    private double _dt;
    private double _simTime;
    private int[] _pixels = Array.Empty<int>();
    private int _chipCounter = 1;
    private readonly DispatcherTimer _timer;

    public MainViewModel()
    {
        PlayPauseCommand = new RelayCommand(TogglePlay);
        ResetCommand = new RelayCommand(RebuildGrid);
        ClearChipsCommand = new RelayCommand(ClearChips);
        LoadDemoCommand = new RelayCommand(LoadDemo);

        _selectedPreset = ChipPresets[1];

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1 / FramesPerSecond) };
        _timer.Tick += (_, _) => Advance();

        LoadDemo();
        TogglePlay();   // start running right away
    }

    // ---------- board size (the overlay uses these as its coordinate system) ----------

    public int BoardWidth => 60;
    public int BoardHeight => 40;

    // ---------- simulation settings ----------

    public IReadOnlyList<Material> Materials { get; } =
        new[] { Material.FR4, Material.Aluminum, Material.Copper };

    private Material _selectedMaterial = Material.FR4;
    public Material SelectedMaterial
    {
        get => _selectedMaterial;
        set { if (SetField(ref _selectedMaterial, value)) RebuildGrid(); }   // same chips, new board
    }

    private double _fanStrength = 20;
    public double FanStrength
    {
        get => _fanStrength;
        set { if (SetField(ref _fanStrength, value)) _grid.ConvectionCoefficient = value; }   // live
    }

    private double _simSpeed = 100;   // simulated seconds per real second
    public double SimSpeed
    {
        get => _simSpeed;
        set => SetField(ref _simSpeed, value);
    }

    // ---------- chips ----------

    // ObservableCollection = a list that tells the screen when items are added/removed
    public ObservableCollection<ChipItem> ChipItems { get; } = new();

    public IReadOnlyList<ChipPreset> ChipPresets { get; } = new[]
    {
        new ChipPreset("Small chip  (4x4 mm, 0.3 W)",      "Small",  4, 0.3,  85),
        new ChipPreset("Medium chip  (6x6 mm, 0.8 W)",     "Medium", 6, 0.8,  85),
        new ChipPreset("Big CPU  (8x8 mm, 1.5 W)",         "CPU",    8, 1.5,  95),
        new ChipPreset("Power regulator  (5x5 mm, 0.6 W)", "Reg",    5, 0.6, 105),
    };

    private ChipPreset _selectedPreset;
    public ChipPreset SelectedPreset
    {
        get => _selectedPreset;
        set => SetField(ref _selectedPreset, value);
    }

    private string _placementNote = "";
    public string PlacementNote { get => _placementNote; private set => SetField(ref _placementNote, value); }

    // ---------- display ----------

    private WriteableBitmap _heatMap = null!;
    public WriteableBitmap HeatMap { get => _heatMap; private set => SetField(ref _heatMap, value); }

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

    private string _warningText = "";
    public string WarningText { get => _warningText; private set => SetField(ref _warningText, value); }

    private bool _hasWarning;
    public bool HasWarning { get => _hasWarning; private set => SetField(ref _hasWarning, value); }

    // ---------- commands (what buttons trigger) ----------

    public ICommand PlayPauseCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand ClearChipsCommand { get; }
    public ICommand LoadDemoCommand { get; }

    // ---------- actions ----------

    private void TogglePlay()
    {
        if (_timer.IsEnabled) _timer.Stop();
        else _timer.Start();

        OnPropertyChanged(nameof(IsRunning));
        OnPropertyChanged(nameof(PlayPauseText));
    }

    // Fresh board at room temp, but KEEP the current chip layout.
    private void RebuildGrid()
    {
        _grid = new ThermalGrid(BoardWidth, BoardHeight, 0.001, SelectedMaterial, RoomTemp);
        _grid.ConvectionCoefficient = FanStrength;

        foreach (var item in ChipItems)
        {
            _grid.Chips.Add(item.Chip);
            item.Temperature = RoomTemp;
        }

        _dt = _grid.MaxStableTimeStep();
        _simTime = 0;
        _pixels = new int[_grid.Width * _grid.Height];
        HeatMap = new WriteableBitmap(_grid.Width, _grid.Height, 96, 96, PixelFormats.Bgr32, null);
        SpeedNote = "";
        Draw();
    }

    private void LoadDemo()
    {
        ChipItems.Clear();
        _chipCounter = 1;
        RebuildGrid();
        //            name               x   y   w   h  watts  max°C
        AddChip(new Chip("CPU",             10, 12,  8, 8,  1.2,   85));
        AddChip(new Chip("Power Regulator", 40,  8,  5, 5,  0.6,  105));
        AddChip(new Chip("Memory",          38, 26, 10, 5,  0.5,   85));
        PlacementNote = "";
        Draw();
    }

    private void ClearChips()
    {
        ChipItems.Clear();
        _grid.Chips.Clear();
        PlacementNote = "";
        Draw();
    }

    private bool AddChip(Chip chip)
    {
        if (!_grid.TryAddChip(chip)) return false;
        ChipItems.Add(new ChipItem(chip, _grid.ChipTemperature(chip)));
        return true;
    }

    // Called by the window when the board is left-clicked.
    // (x, y) is the square that was clicked.
    public void PlaceChipAt(int x, int y)
    {
        var p = SelectedPreset;

        // center the chip on the click, but keep it fully on the board
        int left = Math.Clamp(x - p.Size / 2, 0, BoardWidth - p.Size);
        int top  = Math.Clamp(y - p.Size / 2, 0, BoardHeight - p.Size);

        var chip = new Chip($"{p.ShortName} {_chipCounter}", left, top, p.Size, p.Size, p.Watts, p.MaxTemp);

        if (AddChip(chip))
        {
            _chipCounter++;
            PlacementNote = $"Placed {chip.Name}.";
        }
        else
        {
            PlacementNote = "Can't place there: it would overlap another chip.";
        }
        Draw();
    }

    // Called by the window when the board is right-clicked.
    public void RemoveChipAt(int x, int y)
    {
        var item = ChipItems.FirstOrDefault(c => c.Chip.Covers(x, y));
        if (item == null) return;

        _grid.Chips.Remove(item.Chip);
        ChipItems.Remove(item);
        PlacementNote = $"Removed {item.Chip.Name}.";
        Draw();
    }

    // Called ~30 times a second by the timer
    private void Advance()
    {
        double target = SimSpeed / FramesPerSecond;

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
        // 1) paint the heat map
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

        // 2) update every chip's live temperature
        foreach (var item in ChipItems)
            item.Temperature = _grid.ChipTemperature(item.Chip);

        // 3) warning banner
        var overheating = ChipItems.Where(c => c.IsOverheating).ToList();
        if (ChipItems.Count == 0)
        {
            HasWarning = false;
            WarningText = "No chips on the board. Left-click to place one.";
        }
        else if (overheating.Count == 0)
        {
            HasWarning = false;
            WarningText = "All chips within their temperature limits";
        }
        else
        {
            // name the worst offender: the one furthest over its limit
            var worst = overheating.MaxBy(c => c.Temperature - c.Chip.MaxTemp)!;
            string who = overheating.Count == 1 ? "" : $"{overheating.Count} chips overheating. Worst: ";
            HasWarning = true;
            WarningText = $"WARNING  {who}{worst.Chip.Name} is at {worst.Temperature:F1} °C (limit {worst.Chip.MaxTemp:F0} °C)";
        }

        StatusText = $"Time: {_simTime:F0} s     Hottest point: {hottest:F1} °C";
        ScaleMinText = $"{min:F0} °C";
        ScaleMaxText = $"{max:F0} °C";
    }
}
