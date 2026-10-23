using System;
using System.Diagnostics;

using Microsoft.UI.Dispatching;
using Microsoft.UI.Reactor;

using Windows.Graphics;
using Windows.UI.ViewManagement;

namespace WinQuickLook.App;

internal sealed class PreviewWindowAnimator : IDisposable
{
    private readonly ReactorWindow _window;
    private readonly DispatcherQueueTimer _timer;
    private readonly UISettings _settings = new();
    private readonly Stopwatch _elapsed = new();
    private RectInt32 _start;
    private RectInt32 _target;

    public PreviewWindowAnimator(ReactorWindow window)
    {
        _window = window;
        _timer = window.NativeWindow.DispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(16);
        _timer.Tick += OnTick;
    }

    public void Resize(RectInt32 target, bool animate)
    {
        Stop();
        var position = _window.AppWindow.Position;
        var size = _window.AppWindow.Size;
        _start = new RectInt32(position.X, position.Y, size.Width, size.Height);
        _target = target;
        if (!animate || !_settings.AnimationsEnabled || _start.Equals(target))
        {
            _window.AppWindow.MoveAndResize(target);
            return;
        }

        // Retarget from the current bounds, even when the previous transition was interrupted.
        _elapsed.Restart();
        _timer.Start();
    }

    private void OnTick(DispatcherQueueTimer sender, object args)
    {
        if (!_timer.IsRunning)
        {
            return;
        }
        var progress = Math.Min(_elapsed.Elapsed.TotalMilliseconds / 200, 1);
        var eased = 1 - Math.Pow(1 - progress, 3);
        int Interpolate(int from, int to) => (int)Math.Round(from + (to - from) * eased);

        _window.AppWindow.MoveAndResize(new RectInt32(
            Interpolate(_start.X, _target.X), Interpolate(_start.Y, _target.Y),
            Interpolate(_start.Width, _target.Width), Interpolate(_start.Height, _target.Height)));
        if (progress >= 1)
        {
            Stop();
        }
    }

    public void Stop()
    {
        _timer.Stop();
        _elapsed.Reset();
    }

    public void Dispose()
    {
        Stop();
        _timer.Tick -= OnTick;
    }
}
