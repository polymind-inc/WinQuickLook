using System;
using System.Runtime.InteropServices;

using Moq;

using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell.PropertiesSystem;

using WinQuickLook.Extensions;

using Xunit;

namespace WinQuickLook.Tests.Extensions;

public class PropertyStoreExtensionsTests
{
    [Fact]
    public void GetString_NumericVariant_DoesNotDereferenceNumericValue()
    {
        var value = new PROPVARIANT { vt = 19, pwszVal = (nint)1 };
        var store = CreateStore(value);

        Assert.Null(store.GetString(default));
    }

    [Fact]
    public void GetString_StringVariant_ReturnsText()
    {
        var value = new PROPVARIANT { vt = 31, pwszVal = Marshal.StringToCoTaskMemUni("日本語のタイトル") };
        var store = CreateStore(value);

        Assert.Equal("日本語のタイトル", store.GetString(default));
    }

    [Fact]
    public void GetStringArray_SingleString_DoesNotInterpretPointerAsVector()
    {
        var value = new PROPVARIANT { vt = 31, pwszVal = Marshal.StringToCoTaskMemUni("artist") };
        var store = CreateStore(value);

        Assert.Empty(store.GetStringArray(default));
    }

    [Fact]
    public void GetStringArray_Vector_ReturnsAllStrings()
    {
        var pointers = Marshal.AllocCoTaskMem(2 * IntPtr.Size);
        Marshal.WriteIntPtr(pointers, Marshal.StringToCoTaskMemUni("first"));
        Marshal.WriteIntPtr(pointers, IntPtr.Size, Marshal.StringToCoTaskMemUni("second"));
        var value = new PROPVARIANT { vt = 0x101f, calpwstr = new CALPWSTR { cElems = 2, pElems = pointers } };
        var store = CreateStore(value);

        Assert.Equal(new[] { "first", "second" }, store.GetStringArray(default));
    }

    [Fact]
    public void GetString_Failure_DoesNotReadReturnedValue()
    {
        var value = new PROPVARIANT { vt = 31, pwszVal = Marshal.StringToCoTaskMemUni("unavailable") };
        var store = CreateStore(value, new HRESULT(unchecked((int)0x80004005)));

        Assert.Null(store.GetString(default));
    }

    private static IPropertyStore CreateStore(PROPVARIANT value, HRESULT hr = default)
    {
        var store = new Mock<IPropertyStore>();
        store.Setup(x => x.GetValue(ref It.Ref<PROPERTYKEY>.IsAny, out value)).Returns(hr);

        return store.Object;
    }
}
