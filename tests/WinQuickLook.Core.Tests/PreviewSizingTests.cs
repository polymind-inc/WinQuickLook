using Windows.Foundation;

using Xunit;

namespace WinQuickLook.Tests;

public class PreviewSizingTests
{
    [Theory]
    [InlineData(128, 64, 1, 360, 182)]
    [InlineData(800, 600, 2, 432, 418)]
    [InlineData(123, 97, 1.25, 360, 196)]
    [InlineData(4000, 1000, 1, 1536, 494)]
    [InlineData(1000, 4000, 1, 360, 864)]
    [InlineData(3840, 2160, 1, 1359, 864)]
    public void GetClientSize_FitsPixelsAndChromeWithinWorkArea(double width, double height, double dpiScale,
        double expectedWidth, double expectedHeight)
    {
        var actual = PreviewSizing.GetClientSize(new Size(width, height), new Size(1920, 1080),
            new Size(32, 118), new Size(360, 180), dpiScale);

        Assert.Equal(expectedWidth, actual.Width);
        Assert.Equal(expectedHeight, actual.Height);
    }

    [Fact]
    public void GetClientSize_SmallWorkArea_TakesPriorityOverMinimumWindowSize()
    {
        var actual = PreviewSizing.GetClientSize(new Size(1920, 1080), new Size(320, 240),
            new Size(32, 118), new Size(360, 180));

        Assert.True(actual.Width <= 320 * 0.8);
        Assert.True(actual.Height <= 240 * 0.8);
    }

    [Fact]
    public void GetClientSize_SmallVideo_IsNotUpscaledToMinimumWindowWidth()
    {
        var actual = PreviewSizing.GetClientSize(new Size(320, 240), new Size(1920, 1080),
            new Size(0, 86), new Size(360, 180));

        Assert.Equal(360, actual.Width);
        Assert.Equal(326, actual.Height);
    }
}
