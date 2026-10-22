using ThermoView.Core;
using Xunit;

public class ChipPlacementTests
{
    private static ThermalGrid Board() => new(20, 20, 0.001, Material.FR4, 25);

    [Fact]
    public void ValidChip_IsAdded()
    {
        var grid = Board();
        Assert.True(grid.TryAddChip(new Chip("A", 2, 2, 4, 4, 0.5, 85)));
        Assert.Single(grid.Chips);
    }

    [Fact]
    public void ChipHangingOffBoard_IsRejected()
    {
        var grid = Board();
        Assert.False(grid.TryAddChip(new Chip("A", 18, 18, 4, 4, 0.5, 85)));  // goes past square 19
        Assert.Empty(grid.Chips);
    }

    [Fact]
    public void OverlappingChip_IsRejected()
    {
        var grid = Board();
        grid.TryAddChip(new Chip("A", 2, 2, 4, 4, 0.5, 85));
        Assert.False(grid.TryAddChip(new Chip("B", 4, 4, 4, 4, 0.5, 85)));   // shares squares with A
        Assert.Single(grid.Chips);
    }

    [Fact]
    public void ChipsSideBySide_AreAllowed()
    {
        // B starts exactly where A ends: touching, not overlapping
        var grid = Board();
        grid.TryAddChip(new Chip("A", 2, 2, 4, 4, 0.5, 85));
        Assert.True(grid.TryAddChip(new Chip("B", 6, 2, 4, 4, 0.5, 85)));
    }

    [Fact]
    public void ChipTemperature_IsHottestSquareUnderChip()
    {
        var grid = Board();
        var chip = new Chip("A", 8, 8, 4, 4, 0.5, 85);
        grid.TryAddChip(chip);
        double dt = grid.MaxStableTimeStep();
        for (int i = 0; i < 200; i++) grid.Step(dt);

        double expected = double.MinValue;
        for (int x = 8; x < 12; x++)
            for (int y = 8; y < 12; y++)
                expected = Math.Max(expected, grid.Temperature[x, y]);

        Assert.Equal(expected, grid.ChipTemperature(chip));
        Assert.True(grid.ChipTemperature(chip) > 25);
    }

    [Fact]
    public void IsOverheating_ComparesAgainstLimit()
    {
        var chip = new Chip("A", 0, 0, 2, 2, 0.5, MaxTemp: 85);
        Assert.False(chip.IsOverheating(84.9));
        Assert.True(chip.IsOverheating(85.1));
    }
}
