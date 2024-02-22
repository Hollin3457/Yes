using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace API.Utils
{
    public class AsyncAutoResetEvent
    {
        private static readonly UniTask Completed = UniTask.FromResult(true);
        private readonly Queue<UniTaskCompletionSource<bool>> _waits = new Queue<UniTaskCompletionSource<bool>>();
        private bool _signaled;

        public UniTask WaitAsync()
        {
            lock (_waits)
            {
                if (_signaled)
                {
                    _signaled = false;
                    return Completed;
                }

                var tcs = new UniTaskCompletionSource<bool>();
                _waits.Enqueue(tcs);
                return tcs.Task;
            }
        }

        public void Set()
        {
            UniTaskCompletionSource<bool> toRelease = null;

            lock (_waits)
            {
                if (_waits.Count > 0)
                    toRelease = _waits.Dequeue();
                else if (!_signaled)
                    _signaled = true;
            }

            toRelease?.TrySetResult(true);
        }
    }
}