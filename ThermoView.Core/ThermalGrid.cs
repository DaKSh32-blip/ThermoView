namespace ThermoView.Core;

public class ThermalGrid
{
    public int Width { get; }
    public int Height { get; }
    public double CellSize { get; }   // size of one square, in meters

    public double[,] Temperature { get; private set; }  // temp of every square (°C)
    public Material[,] Materials { get; }               // what every square is made of

    public ThermalGrid(int width, int height, double cellSize,
                       Material baseMaterial, double startTemp)
    {
        Width = width;
        Height = height;
        CellSize = cellSize;
        Temperature = new double[width, height];
        Materials = new Material[width, height];

        // fill every square with the starting temp and base material
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                Temperature[x, y] = startTemp;
                Materials[x, y] = baseMaterial;
            }
    }

    // The biggest time jump we can take without the math exploding.
    // Too big a step = temperatures shoot to millions of degrees.
    // Safe limit: step ≤ cellSize² / (4 × fastest diffusivity)
    public double MaxStableTimeStep()
    {
        double fastest = 0;
        foreach (var m in Materials)
            fastest = Math.Max(fastest, m.Diffusivity);

        return 0.9 * CellSize * CellSize / (4 * fastest);  // 0.9 = safety margin
    }

    // Move the simulation forward by dt seconds.
    public void Step(double dt)
    {
        // Work on a COPY so every square uses the OLD temperatures.
        var next = (double[,])Temperature.Clone();
        double dx2 = CellSize * CellSize;

        // Skip the outer edge for now: edges stay fixed,
        // like the board is clamped to a cold table.
        for (int x = 1; x < Width - 1; x++)
            for (int y = 1; y < Height - 1; y++)
            {
                double me = Temperature[x, y];
                double neighbors = Temperature[x + 1, y] + Temperature[x - 1, y]
                                 + Temperature[x, y + 1] + Temperature[x, y - 1];
                double speed = Materials[x, y].Diffusivity * dt / dx2;

                next[x, y] = me + speed * (neighbors - 4 * me);
            }

        Temperature = next;
    }
}