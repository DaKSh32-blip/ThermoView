namespace ThermoView.Core;

public class ThermalGrid
{
    public int Width { get; }
    public int Height { get; }
    public double CellSize { get; }        // size of one square (m)
    public double Thickness { get; }       // NEW: board thickness (m)

    public double[,] Temperature { get; private set; }
    public Material[,] Materials { get; }

    public double AmbientTemp { get; set; }            // NEW: room air temp (°C)
    public double ConvectionCoefficient { get; set; }  // NEW: h, 0 = no air cooling
    public List<Chip> Chips { get; } = new();          // NEW: chips on the board

    public ThermalGrid(int width, int height, double cellSize,
                       Material baseMaterial, double startTemp,
                       double thickness = 0.0016)       // NEW: 1.6mm = standard board
    {
        Width = width;
        Height = height;
        CellSize = cellSize;
        Thickness = thickness;
        AmbientTemp = startTemp;    // room starts at the same temp as the board
        Temperature = new double[width, height];
        Materials = new Material[width, height];

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                Temperature[x, y] = startTemp;
                Materials[x, y] = baseMaterial;
            }
    }

    public double MaxStableTimeStep()
    {
        double fastest = 0;
        foreach (var m in Materials)
            fastest = Math.Max(fastest, m.Diffusivity);

        return 0.9 * CellSize * CellSize / (4 * fastest);
    }

    // NEW: the hottest square on the board (used for warnings later)
    public double MaxTemperature()
    {
        double max = double.MinValue;
        foreach (var t in Temperature)
            max = Math.Max(max, t);
        return max;
    }

    // NEW: insulated edges. If we ask for a square off the board,
    // clamp to the nearest real square. So at the edge, the "missing
    // neighbor" has the same temp as me, meaning no heat flows out the side.
    private double TempAt(int x, int y)
    {
        x = Math.Clamp(x, 0, Width - 1);
        y = Math.Clamp(y, 0, Height - 1);
        return Temperature[x, y];
    }

    // NEW: how many watts are being pumped into square (x, y)?
    // A chip's power is split evenly over all the squares it covers.
    private double PowerAt(int x, int y)
    {
        double power = 0;
        foreach (var chip in Chips)
            if (chip.Covers(x, y))
                power += chip.PowerWatts / chip.CellCount;
        return power;
    }

    public void Step(double dt)
    {
        var next = new double[Width, Height];
        double dx2 = CellSize * CellSize;
        double cellArea = CellSize * CellSize;          // top surface of one square
        double cellVolume = cellArea * Thickness;

        // NEW: loop over EVERY square now, edges included
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                double me = Temperature[x, y];
                var mat = Materials[x, y];

                // 1) spread from neighbors (same as Part 1)
                double neighbors = TempAt(x + 1, y) + TempAt(x - 1, y)
                                 + TempAt(x, y + 1) + TempAt(x, y - 1);
                double spread = mat.Diffusivity * dt / dx2 * (neighbors - 4 * me);

                // 2) energy needed to raise this square by 1°C
                double heatCapacity = mat.Density * mat.SpecificHeat * cellVolume;

                // 3) heat IN from chips, heat OUT to the air
                double powerIn = PowerAt(x, y);
                double powerOut = ConvectionCoefficient * cellArea * (me - AmbientTemp);

                // watts × seconds = joules, joules ÷ heatCapacity = °C change
                next[x, y] = me + spread + (powerIn - powerOut) * dt / heatCapacity;
            }

        Temperature = next;
    }
}