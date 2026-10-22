using ThermoView.Core;
using Xunit;

public class HeatSourceTests
{
    // helper: a 10x10mm FR4 board at 25°C with one 0.2W chip in the middle
    private static ThermalGrid MakeBoard(double fanStrength)
    {
        var grid = new ThermalGrid(10, 10, 0.001, Material.FR4, 25);
        grid.ConvectionCoefficient = fanStrength;
        grid.Chips.Add(new Chip("CPU", 4, 4, 2, 2, PowerWatts: 0.2, MaxTemp: 85));
        return grid;
    }

    // helper: run the simulation for a number of steps
    private static void Run(ThermalGrid grid, int steps)
    {
        double dt = grid.MaxStableTimeStep();
        for (int i = 0; i < steps; i++)
            grid.Step(dt);
    }

    [Fact]
    public void Chip_HeatsUpTheBoard()
    {
        var grid = MakeBoard(fanStrength: 10);
        Run(grid, 100);

        Assert.True(grid.Temperature[4, 4] > 25);   // under the chip got hotter
    }

    [Fact]
    public void Fan_KeepsBoardCooler()
    {
        var stillAir = MakeBoard(fanStrength: 10);
        var withFan = MakeBoard(fanStrength: 100);
        Run(stillAir, 5000);
        Run(withFan, 5000);

        Assert.True(withFan.MaxTemperature() < stillAir.MaxTemperature());
    }

    [Fact]
    public void SteadyState_HeatOutEqualsHeatIn()
    {
        // The bathtub test: after a long time, the drain (air)
        // must remove exactly what the faucet (chip) adds: 0.2 W.
        var grid = MakeBoard(fanStrength: 50);
        Run(grid, 30000);

        double cellArea = grid.CellSize * grid.CellSize;
        double heatOut = 0;
        foreach (var t in grid.Temperature)
            heatOut += grid.ConvectionCoefficient * cellArea * (t - grid.AmbientTemp);

        Assert.InRange(heatOut, 0.198, 0.202);   // 0.2 W, within 1%
    }
}