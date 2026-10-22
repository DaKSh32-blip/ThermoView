using ThermoView.Core;

namespace ThermoView.App.ViewModels;

// One chip as the SCREEN sees it: the chip itself plus its live temperature.
// The same item feeds both the outline drawn on the board and the list on the right.
public class ChipItem : ViewModelBase
{
    public ChipItem(Chip chip, double startTemp)
    {
        Chip = chip;
        _temperature = startTemp;
    }

    public Chip Chip { get; }

    // position/size, used to draw the outline on the board
    public int X => Chip.X;
    public int Y => Chip.Y;
    public int Width => Chip.Width;
    public int Height => Chip.Height;

    private double _temperature;
    public double Temperature
    {
        get => _temperature;
        set
        {
            if (SetField(ref _temperature, value))
            {
                // these depend on Temperature, so they change too
                OnPropertyChanged(nameof(IsOverheating));
                OnPropertyChanged(nameof(Summary));
            }
        }
    }

    public bool IsOverheating => Chip.IsOverheating(Temperature);

    public string Summary =>
        $"{Chip.Name} ({Chip.PowerWatts:0.0} W):  {Temperature:F1} °C  /  limit {Chip.MaxTemp:F0} °C";
}
