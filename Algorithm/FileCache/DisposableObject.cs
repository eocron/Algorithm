using System;

namespace Algorithm.FileCache
{
    public struct DisposableObject : IDisposable
    {
        private readonly Action _dispose;

        public DisposableObject(Action dispose)
        {
            _dispose = dispose;
        }

        public void Dispose()
        {
            _dispose();
        }
    }
}