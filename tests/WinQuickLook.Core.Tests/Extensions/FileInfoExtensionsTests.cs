using System.IO;

using WinQuickLook.Extensions;

using Xunit;

namespace WinQuickLook.Tests.Extensions;

public class FileInfoExtensionsTests
{
    [Fact]
    public void OpenReadNoLock_AllowsRenameAndDeleteWhileReading()
    {
        var path = Path.GetTempFileName();
        var renamed = path + ".renamed";

        try
        {
            File.WriteAllBytes(path, [1, 2]);

            using var stream = new FileInfo(path).OpenReadNoLock();

            File.Move(path, renamed);
            File.Delete(renamed);

            Assert.Equal(1, stream.ReadByte());
            Assert.Equal(2, stream.ReadByte());
        }
        finally
        {
            File.Delete(path);
            File.Delete(renamed);
        }
    }
}
