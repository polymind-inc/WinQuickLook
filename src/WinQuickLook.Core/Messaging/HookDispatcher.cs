using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

namespace WinQuickLook.Messaging;

internal sealed class HookDispatcher
{
    private const uint DispatchMessage = 0x8001;
    private readonly ConcurrentQueue<Action> _actions = new();
    private readonly uint _threadId;

    public HookDispatcher()
    {
        Thread = Thread.CurrentThread;
        _threadId = PInvoke.GetCurrentThreadId();
        // Create the native message queue before publishing this dispatcher.
        PInvoke.PeekMessage(out _, default, 0, 0, PEEK_MESSAGE_REMOVE_TYPE.PM_NOREMOVE);
    }

    public Thread Thread { get; }

    public void Invoke(Action action)
    {
        if (Thread.CurrentThread == Thread)
        {
            action();
            return;
        }
        InvokeAsync(() => { action(); return true; }).GetAwaiter().GetResult();
    }

    public Task<T> InvokeAsync<T>(Func<T> action)
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        _actions.Enqueue(() =>
        {
            try { completion.SetResult(action()); }
            catch (Exception exception) { completion.SetException(exception); }
        });

        if (!PInvoke.PostThreadMessage(_threadId, DispatchMessage, default, default))
        {
            completion.TrySetException(new Win32Exception(Marshal.GetLastPInvokeError()));
        }
        return completion.Task;
    }

    public void Shutdown() => PInvoke.PostThreadMessage(_threadId, PInvoke.WM_QUIT, default, default);

    public void Run()
    {
        int result;
        while ((result = PInvoke.GetMessage(out var message, default, 0, 0)) > 0)
        {
            if (message.message == DispatchMessage)
            {
                while (_actions.TryDequeue(out var action))
                {
                    action();
                }
            }
            else
            {
                PInvoke.TranslateMessage(message);
                PInvoke.DispatchMessage(message);
            }
        }
        if (result < 0)
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        }
    }
}
