using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

using WinQuickLook.Messaging;

using Xunit;

namespace WinQuickLook.Tests.Messaging;

public class WindowsHookTests
{
    [Fact]
    public async Task Start_InstallsOnDedicatedResponsiveThread()
    {
        using var hook = new LowLevelKeyboardHook();

        hook.Start();

        var dispatcher = Assert.IsType<System.Windows.Threading.Dispatcher>(hook.HookDispatcher);

        Assert.NotEqual(Thread.CurrentThread.ManagedThreadId, dispatcher.Thread.ManagedThreadId);

        var operation = dispatcher.InvokeAsync(() => Thread.CurrentThread.ManagedThreadId);

        var threadId = await operation.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.Equal(dispatcher.Thread.ManagedThreadId, threadId);

        hook.Start();

        Assert.Same(dispatcher, hook.HookDispatcher);
    }

    [Fact]
    public void Stop_UnhooksAndSupportsRestart()
    {
        using var hook = new LowLevelKeyboardHook();
        hook.Start();
        var previous = hook.HookDispatcher!;

        hook.Stop();

        Assert.False(previous.Thread.IsAlive);
        Assert.Null(hook.HookDispatcher);

        hook.Start();

        Assert.NotSame(previous, hook.HookDispatcher);
    }

    [Fact]
    public void Stop_FromHookThread_DoesNotDeadlock()
    {
        using var hook = new LowLevelKeyboardHook();
        hook.Start();
        var dispatcher = hook.HookDispatcher!;

        dispatcher.Invoke(hook.Stop);

        Assert.True(dispatcher.Thread.Join(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public void Dispose_StopsWorkerAndRejectsRestart()
    {
        var hook = new LowLevelKeyboardHook();
        hook.Start();
        var dispatcher = hook.HookDispatcher!;

        hook.Dispose();
        hook.Dispose();

        Assert.False(dispatcher.Thread.IsAlive);
        Assert.Throws<ObjectDisposedException>(hook.Start);
    }

    [Fact]
    public void Start_NativeFailure_ReportsErrorAndCleansUp()
    {
        using var hook = new InvalidHook();

        Assert.Throws<Win32Exception>(hook.Start);
        Assert.Null(hook.HookDispatcher);
    }

    [Fact]
    public void Callback_Exception_DoesNotCrossNativeBoundary()
    {
        using var hook = new ThrowingHook();

        hook.InvokeHookProc(-1, default, default);
    }

    private sealed class InvalidHook() : WindowsHook((WINDOWS_HOOK_ID)int.MaxValue);

    private sealed class ThrowingHook() : WindowsHook(WINDOWS_HOOK_ID.WH_KEYBOARD_LL)
    {
        protected override LRESULT HookProc(int code, WPARAM wParam, LPARAM lParam) => throw new InvalidOperationException();
    }
}
