using System;
using Unity.Sentis;

namespace Cysharp.Threading.Tasks
{
    public static class SentisAwaitableHelpers
    {
        public static UniTask<T> ReadbackAsync<T>(this T tensor, PlayerLoopTiming timing = PlayerLoopTiming.Update) where T : Tensor
        {
            try
            {
                tensor.ReadbackRequest();
                return new(TensorReadbackCompletionSource<T>.Create(tensor, timing, out var token), token);
            }
            catch (Exception exception)
            {
                return UniTask.FromException<T>(exception);
            }
        }
    }

    sealed class TensorReadbackCompletionSource<T> : IUniTaskSource<T>, IPlayerLoopItem, ITaskPoolNode<TensorReadbackCompletionSource<T>> where T : Tensor
    {
        static TaskPool<TensorReadbackCompletionSource<T>> pool;
        TensorReadbackCompletionSource<T> nextNode;
        T tensor;
        UniTaskCompletionSourceCore<T> core;

        public ref TensorReadbackCompletionSource<T> NextNode => ref nextNode;

        static TensorReadbackCompletionSource() => TaskPool.RegisterSizeGetter(typeof(TensorReadbackCompletionSource<T>), () => pool.Size);


        public static IUniTaskSource<T> Create(T tensor, PlayerLoopTiming timing, out short token)
        {
            if (!pool.TryPop(out var completeSource)) completeSource = new();
            completeSource.tensor = tensor;
            TaskTracker.TrackActiveTask(completeSource, 3);
            PlayerLoopHelper.AddAction(timing, completeSource);
            token = completeSource.core.Version;
            return completeSource;
        }

        TensorReadbackCompletionSource() {}

        public bool MoveNext()
        {
            if (tensor == null) return false;
            if (tensor.IsReadbackRequestDone())
            {
                core.TrySetResult(tensor);
                return false;
            }
            return true;
        }

        public UniTaskStatus GetStatus(short token) =>
            core.GetStatus(token);

        public UniTaskStatus UnsafeGetStatus() =>
            core.UnsafeGetStatus();

        public void OnCompleted(Action<object> continuation, object state, short token) =>
            core.OnCompleted(continuation, state, token);

        void IUniTaskSource.GetResult(short token) =>
            GetResult(token);

        public T GetResult(short token)
        {
            try
            {
                return core.GetResult(token);
            }
            finally
            {
                TryReturn();
            }
        }

        bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            core.Reset();
            tensor = null;
            return pool.TryPush(this);
        }
    }
}