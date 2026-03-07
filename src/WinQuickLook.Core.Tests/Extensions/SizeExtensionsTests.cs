using System.Windows;

using WinQuickLook.Extensions;

using Xunit;

namespace WinQuickLook.Tests.Extensions;

public class SizeExtensionsTests
{
    [Theory]
    [InlineData(100, 100, 100, 100)]
    [InlineData(3000, 2000, 1200, 800)]
    [InlineData(2000, 3000, 800, 1200)]
    [InlineData(1280, 1024, 1200, 960)]
    [InlineData(1024, 1280, 960, 1200)]
    public void FitTo(int width, int height, int fitWidth, int fitHeight)
    {
        var size = new Size(width, height);

        var actual = size.FitTo(1200);

        Assert.Equal(fitWidth, (int)actual.Width);
        Assert.Equal(fitHeight, (int)actual.Height);
    }
}
