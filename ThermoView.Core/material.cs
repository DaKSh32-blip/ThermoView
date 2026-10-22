namespace ThermoView.Core;

// A material is just a name + 3 real physical properties.
// "record" = a simple class whose job is to hold data.
public record Material(
    string Name,
    double Conductivity,   // how well it passes heat (W/m·K)
    double Density,        // how heavy it is per volume (kg/m³)
    double SpecificHeat)   // how much energy it takes to warm it up (J/kg·K)
{
    // Diffusivity = how FAST heat spreads through this material.
    // Formula: conductivity / (density × specific heat)
    public double Diffusivity => Conductivity / (Density * SpecificHeat);

    // Real-world values for the materials on a circuit board
    public static readonly Material Copper = new("Copper", 401, 8960, 385);
    public static readonly Material Aluminum = new("Aluminum", 237, 2700, 897);
    public static readonly Material FR4 = new("FR4 Board", 0.3, 1850, 1100);
}