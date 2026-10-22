namespace ThermoView.Core;

// Turns a temperature into a color, like a thermal camera.
// coldest = blue -> cyan -> green -> yellow -> red = hottest
public static class ColorMap
{
    public static (byte R, byte G, byte B) ToColor(double temp, double min, double max)
    {
        // Step 1: squash the temp into a 0-to-1 "fraction" of the range.
        // 0 = coldest on screen, 1 = hottest on screen.
        double f = max > min ? (temp - min) / (max - min) : 0;
        f = Math.Clamp(f, 0, 1);

        // Step 2: split 0-1 into 4 equal slices, each slice blends two colors.
        double r, g, b;
        if (f < 0.25)      { r = 0;                 g = f / 0.25;              b = 1; }                    // blue  -> cyan
        else if (f < 0.50) { r = 0;                 g = 1;                     b = 1 - (f - 0.25) / 0.25; } // cyan  -> green
        else if (f < 0.75) { r = (f - 0.50) / 0.25; g = 1;                     b = 0; }                    // green -> yellow
        else               { r = 1;                 g = 1 - (f - 0.75) / 0.25; b = 0; }                    // yellow-> red

        // Step 3: screens use 0-255 for each color channel
        return ((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
    }
}
