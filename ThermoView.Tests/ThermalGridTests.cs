using ThermoView.Core;
using Xunit;

public class ThermalGridTests
{
    [Fact]
    public void HotSpot_SpreadsToNeighbors()
    {
        // 5x5 copper plate, 1mm squares, everything at 20°C
        var grid = new ThermalGrid(5, 5, 0.001, Material.Copper, 20);
        grid.Temperature[2, 2] = 100;   // heat up the center square

        grid.Step(grid.MaxStableTimeStep());

        Assert.True(grid.Temperature[2, 1] > 20);   // neighbor got warmer
        Assert.True(grid.Temperature[2, 2] < 100);  // center got cooler
    }

    [Fact]
    public void Temperatures_NeverGoOutsideStartingRange()
    {
        // Run 1000 steps. If the time step were unsafe,
        // temps would explode and this fails.
        var grid = new ThermalGrid(10, 10, 0.001, Material.Copper, 20);
        grid.Temperature[5, 5] = 100;
        double dt = grid.MaxStableTimeStep();

        for (int i = 0; i < 1000; i++)
            grid.Step(dt);

        foreach (var t in grid.Temperature)
            Assert.InRange(t, 20, 100);
    }

    [Fact]
    public void UniformGrid_StaysUniform()
    {
        // Everything at 50°C. No differences = no heat flow.
        var grid = new ThermalGrid(8, 8, 0.001, Material.FR4, 50);
        double dt = grid.MaxStableTimeStep();

        for (int i = 0; i < 100; i++)
            grid.Step(dt);

        foreach (var t in grid.Temperature)
            Assert.Equal(50, t, precision: 6);
    }
}