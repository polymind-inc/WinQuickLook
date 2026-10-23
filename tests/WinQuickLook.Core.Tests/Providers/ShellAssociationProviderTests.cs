using System.IO;
using System.Runtime.InteropServices;

using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;

using WinQuickLook.Providers;

using Xunit;

namespace WinQuickLook.Tests.Providers;

public class ShellAssociationProviderTests
{
    [Fact]
    public void GetRecommends_PartialFinalBatch_ReturnsHandlersWithoutExtraFetch()
    {
        var enumerator = new EnumeratorStub();

        var entries = ShellAssociationProvider.GetRecommends(enumerator);

        Assert.Single(entries);
        Assert.Equal("テスト用アプリ", entries[0].Name);
        Assert.Null(entries[0].Icon);
        Assert.Equal(1, enumerator.Calls);
    }

    [Fact]
    public void GetRecommends_FailedName_SkipsHandler()
    {
        var enumerator = new EnumeratorStub { NameResult = new HRESULT(unchecked((int)0x80004005)) };

        Assert.Empty(ShellAssociationProvider.GetRecommends(enumerator));
    }

    [Fact]
    public void TryGetDefault_Name_DoesNotContainTerminatingNull()
    {
        var provider = new ShellAssociationProvider();

        if (provider.TryGetDefault(new FileInfo("sample.txt"), out var entry))
        {
            Assert.NotEmpty(entry.Name);
            Assert.DoesNotContain('\0', entry.Name);
        }
    }

    private unsafe sealed class EnumeratorStub : IEnumAssocHandlers
    {
        public int Calls { get; private set; }
        public HRESULT NameResult { get; set; }

        public HRESULT Next(uint count, IAssocHandler[] handlers, uint* fetched)
        {
            Calls++;
            *fetched = 1;
            handlers[0] = new HandlerStub { NameResult = NameResult };

            return new HRESULT(1);
        }
    }

    private unsafe sealed class HandlerStub : IAssocHandler
    {
        public HRESULT NameResult { get; set; }

        public HRESULT GetUIName(PWSTR* name)
        {
            *name = new PWSTR(Marshal.StringToCoTaskMemUni("テスト用アプリ"));

            return NameResult;
        }

        public HRESULT GetIconLocation(PWSTR* location, out int index)
        {
            *location = new PWSTR(Marshal.StringToCoTaskMemUni("@missing-icon"));
            index = 0;

            return new HRESULT(unchecked((int)0x80004005));
        }

        public HRESULT GetName(PWSTR* name) { *name = default; return default; }
        public HRESULT IsRecommended() => default;
        public HRESULT MakeDefault(PCWSTR description) => default;
        public HRESULT Invoke(Windows.Win32.System.Com.IDataObject data) => default;
        public HRESULT CreateInvoker(Windows.Win32.System.Com.IDataObject data, out IAssocHandlerInvoker invoker) { invoker = null!; return default; }
    }
}
