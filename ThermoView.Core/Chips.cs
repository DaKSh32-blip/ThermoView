namespace ThermoView.Core;

// A chip sits on the board as a rectangle of squares and produces heat.
public record Chip(
    string Name,
    int X, int Y,            // top-left square it covers
    int Width, int Height,   // how many squares wide/tall
    double PowerWatts,       // how much heat it makes
    double MaxTemp)          // the temp it's not allowed to go above (°C)
{
    public int CellCount => Width * Height;

    // Is square (x, y) underneath this chip?
    public bool Covers(int x, int y) =>
        x >= X && x < X + Width && y >= Y && y < Y + Height;

    // Do two chips share any square?
    // Two rectangles overlap only if they overlap left-right AND up-down.
    public bool Overlaps(Chip other) =>
        X < other.X + other.Width && other.X < X + Width &&
        Y < other.Y + other.Height && other.Y < Y + Height;

    public bool IsOverheating(double temperature) => temperature > MaxTemp;
}
