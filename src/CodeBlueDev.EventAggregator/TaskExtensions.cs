// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TaskExtensions.cs" company="CodeBlueDev">
//   All rights reserved.
// </copyright>
// <summary>
//   Defines the TaskExtensions type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace CodeBlueDev.EventAggregator
{
    using System.Threading.Tasks;

    /// <summary>
    /// The task extensions.
    /// </summary>
    public static class TaskExtensions
    {
        public static Task<T> AsTask<T>(this T value)
        {
            TaskCompletionSource<T> taskCompletionSource = new TaskCompletionSource<T>();
            taskCompletionSource.SetResult(value);
            return taskCompletionSource.Task;
        }
    }
}
