using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace WinQuickLook.Messaging;

public abstract class WindowsHook(WINDOWS_HOOK_ID idHook) : IDisposable
{
    private readonly object _sync = new();
    private HookThread? _worker;
    private bool _disposed;

    internal Dispatcher? HookDispatcher
    {
        get
        {
            lock (_sync)
            {
                return _worker?.WaitForStart();
            }
        }
    }

    public void Start()
    {
        HookThread worker;

        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_worker is not { IsAlive: true })
            {
                var newWorker = new HookThread(idHook, InvokeHookProc);

                newWorker.Start();
                _worker = newWorker;
            }

            worker = _worker;
        }

        try
        {
            worker.WaitForStart();
        }
        catch
        {
            lock (_sync)
            {
                if (ReferenceEquals(_worker, worker))
                {
                    _worker = null;
                }
            }

            worker.Stop();

            throw;
        }
    }

    public void Stop()
    {
        HookThread? worker;

        lock (_sync)
        {
            worker = _worker;
            _worker = null;
        }

        worker?.Stop();
    }

    protected virtual LRESULT HookProc(int code, WPARAM wParam, LPARAM lParam) => PInvoke.CallNextHookEx(default(HHOOK), code, wParam, lParam);

    internal LRESULT InvokeHookProc(int code, WPARAM wParam, LPARAM lParam)
    {
        try
        {
            return HookProc(code, wParam, lParam);
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);

            return PInvoke.CallNextHookEx(default(HHOOK), code, wParam, lParam);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        Stop();
    }

    private sealed class HookThread
    {
        public HookThread(WINDOWS_HOOK_ID idHook, HOOKPROC callback)
        {
            _idHook = idHook;
            _callback = callback;
            _thread = new Thread(Run) { IsBackground = true, Name = $"WinQuickLook {idHook}" };
            _thread.SetApartmentState(ApartmentState.STA);
        }

        private readonly WINDOWS_HOOK_ID _idHook;
        private readonly HOOKPROC _callback;
        private readonly Thread _thread;
        private readonly TaskCompletionSource<Dispatcher> _started = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool IsAlive => _thread.IsAlive;

        public void Start() => _thread.Start();

        public Dispatcher WaitForStart() => _started.Task.GetAwaiter().GetResult();

        public void Stop()
        {
            try
            {
                var dispatcher = WaitForStart();

                if (!dispatcher.HasShutdownStarted)
                {
                    dispatcher.BeginInvokeShutdown(DispatcherPriority.Send);
                }
            }
            catch (Exception) when (_started.Task.IsFaulted)
            {
                // Startup failures are reported by Start; the thread still needs joining.
            }
            finally
            {
                if (Thread.CurrentThread != _thread)
                {
                    _thread.Join();
                }
            }
        }

        private void Run()
        {
            try
            {
                using var module = PInvoke.GetModuleHandle((string)null!);
                using var hook = PInvoke.SetWindowsHookEx(_idHook, _callback, module, 0);

                if (hook.IsInvalid)
                {
                    throw new Win32Exception(Marshal.GetLastPInvokeError());
                }

                var dispatcher = Dispatcher.CurrentDispatcher;

                _started.SetResult(dispatcher);
                Dispatcher.Run();
            }
            catch (Exception exception)
            {
                _started.TrySetException(exception);
                Debug.WriteLine(exception);
            }
            finally
            {
                GC.KeepAlive(_callback);
            }
        }
    }
}
