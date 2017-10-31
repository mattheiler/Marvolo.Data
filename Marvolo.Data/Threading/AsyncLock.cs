using System;
using System.Threading;
using System.Threading.Tasks;

namespace Marvolo.Data.Threading
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class AsyncLock
    {
        private readonly IDisposable _releaser;

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// 
        /// </summary>
        public AsyncLock()
        {
            _releaser = new Releaser(this);
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsEntered => _semaphore.CurrentCount == 0;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<IDisposable> LockAsync()
        {
            await _semaphore.WaitAsync();
            return _releaser;
        }

        private void Release()
        {
            _semaphore.Release();
        }

        private sealed class Releaser : IDisposable
        {
            private readonly AsyncLock _lock;

            internal Releaser(AsyncLock @lock)
            {
                _lock = @lock;
            }

            public void Dispose()
            {
                _lock.Release();
            }
        }
    }
}