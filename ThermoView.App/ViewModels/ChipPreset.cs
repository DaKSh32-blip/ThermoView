namespace ThermoView.App.ViewModels;

// A "stamp" the user picks before clicking the board.
public record ChipPreset(string Label, string ShortName, int Size, double Watts, double MaxTemp);
