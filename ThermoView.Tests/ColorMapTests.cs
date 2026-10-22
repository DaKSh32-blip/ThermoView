using ThermoView.Core;
using Xunit;

public class ColorMapTests
{
    [Fact]
    public void Coldest_IsBlue() =>
        Assert.Equal(((byte)0, (byte)0, (byte)255), ColorMap.ToColor(25, 25, 100));

    [Fact]
    public void Hottest_IsRed() =>
        Assert.Equal(((byte)255, (byte)0, (byte)0), ColorMap.ToColor(100, 25, 100));

    [Fact]
    public void Middle_IsGreen() =>
        Assert.Equal(((byte)0, (byte)255, (byte)0), ColorMap.ToColor(62.5, 25, 100));

    [Fact]
    public void OutOfRange_IsClamped() =>
        Assert.Equal(((byte)0, (byte)0, (byte)255), ColorMap.ToColor(-50, 25, 100));
}
