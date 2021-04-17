using System;

namespace Eocron.Algorithms.Disposing
{
    /// <summary>
    /// Disposable struct. You can specify dispose action and return this object for later dispose.
    /// Usefull for writing resource allocation where you need to perform some release action.
    /// For example: aquire/release lock, open/close stream, etc.
    /// </summary>
    public struct Disposable : IDisposable
    {
        private readonly Action _onDispose;
        public Disposable(Action onDispose)
        {
            if (onDispose == null)
                throw new ArgumentNullException(nameof(onDispose));
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            try{}
            finally
            {
                _onDispose();//this way dispose will not be interrupted by Thread.Abort() if called not in finally;
            }
        }
    }
}
