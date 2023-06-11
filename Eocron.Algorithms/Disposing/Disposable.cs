using System;
using System.Collections.Generic;

namespace Eocron.Algorithms.Disposing
{
    /// <summary>
    ///     Disposable struct. You can specify dispose action/other disposables and return this object for later dispose.
    ///     Usefull for writing resource allocation where you need to perform some release action.
    ///     For example: aquire/release lock, open/close stream, open connection then transaction then user scope and return,
    ///     etc.
    /// </summary>
    public struct Disposable : IDisposable
    {
        private readonly Action _onDispose;
        private readonly IDisposable[] _other;
        private bool _disposed;

        /// <summary>
        ///     Takes dispose action.
        /// </summary>
        /// <param name="onDispose"></param>
        public Disposable(Action onDispose)
        {
            if (onDispose == null)
                throw new ArgumentNullException(nameof(onDispose));
            _onDispose = onDispose;
            _other = null;
            _disposed = false;
        }

        /// <summary>
        ///     Takes other disposables to dispose.
        ///     They will be disposed in exact same order they were passed in constructor.
        ///     Disposing will be performed regardles if exception risen in one of them. All exceptions will be thrown as
        ///     AggregateException.
        /// </summary>
        /// <param name="other">Disposables to dispose in order.</param>
        public Disposable(params IDisposable[] other)
        {
            _onDispose = null;
            _other = other;
            _disposed = false;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            try
            {
            } //this way dispose will not be interrupted by Thread.Abort() if called not in finally;
            finally
            {
                if (_other != null)
                {
                    var exceptions = new List<Exception>();
                    foreach (var o in _other)
                        try
                        {
                            o?.Dispose();
                        }
                        catch (Exception e)
                        {
                            exceptions.Add(e);
                        }

                    if (exceptions.Count > 0)
                        throw new AggregateException(exceptions);
                }

                _onDispose?.Invoke();
                _disposed = true;
            }
        }
    }
}