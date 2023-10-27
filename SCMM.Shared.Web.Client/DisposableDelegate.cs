﻿namespace SCMM.Shared.Web.Client
{
    public class DisposableDelegate : IDisposable
    {
        private readonly Action _onDisposed;

        public DisposableDelegate(Action onDisposed)
        {
            _onDisposed = onDisposed;
        }

        public void Dispose()
        {
            _onDisposed?.Invoke();
        }
    }
}
