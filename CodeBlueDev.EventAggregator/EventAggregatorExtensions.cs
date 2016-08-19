// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EventAggregatorExtensions.cs" company="CodeBlueDev">
//   All rights reserved.
// </copyright>
// <summary>
//   Defines the EventAggregatorExtensions type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace CodeBlueDev.EventAggregator
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// EventAggregator extensions to make publishing and subscribing easier.
    /// </summary>
    public static class EventAggregatorExtensions
    {
        public static Task<Task[]> Publish<TEvent>(this object sender, Task<TEvent> eventDataTask)
        {
            return EventAggregator.Publish(sender, eventDataTask);
        }

        public static Task Subscribe<TEvent>(this object sender, Func<Task<TEvent>, Task> eventHandlerTaskFactory)
        {
            return EventAggregator.Subscribe(sender, eventHandlerTaskFactory);
        }

        public static Task Unsubscribe<TEvent>(this object sender)
        {
            return EventAggregator.Unsubscribe<TEvent>(sender);
        }
    }
}
