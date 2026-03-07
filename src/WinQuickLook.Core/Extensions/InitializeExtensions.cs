using System;
using System.ComponentModel;
using System.Windows;

namespace WinQuickLook.Extensions;

internal static class InitializeExtensions
{
    public static IDisposable Initialize(this ISupportInitialize supportInitialize) => new InitializeScope(supportInitialize);

    private class InitializeScope : IDisposable
    {
        public InitializeScope(ISupportInitialize supportInitialize)
        {
            _supportInitialize = supportInitialize;

            _supportInitialize.BeginInit();
        }

        private readonly ISupportInitialize _supportInitialize;

        public void Dispose()
        {
            _supportInitialize.EndInit();

            // ReSharper disable once SuspiciousTypeConversion.Global
            if (_supportInitialize is Freezable freezable)
            {
                freezable.Freeze();
            }
        }
    }
}
