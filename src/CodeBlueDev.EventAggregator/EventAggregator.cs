// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EventAggregator.cs" company="CodeBlueDev">
//   All rights reserved.
// </copyright>
// <summary>
//   Defines the EventAggregator type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace CodeBlueDev.EventAggregator
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading.Tasks;

    using CodeBlueDev.EventAggregator.Properties;

    /// <summary>
    /// The event aggregator responsible for keeping track of subscriptions and publishing messages.
    /// </summary>
    public static class EventAggregator
    {
        private static readonly TaskFactory _taskFactory;

        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<object, ConcurrentBag<object>>> _events;

        /// <summary>
        /// Initializes static members of the <see cref="EventAggregator"/> class.
        /// </summary>
        static EventAggregator()
        {
            _taskFactory = Task.Factory;
            _events = new ConcurrentDictionary<Type, ConcurrentDictionary<object, ConcurrentBag<object>>>();
        }

        /// <summary>
        /// Publishes an event to all subscribers.
        /// </summary>
        /// <param name="sender">
        /// The sender publishing the task.
        /// </param>
        /// <param name="eventDataTask">
        /// The event being published.
        /// </param>
        /// <typeparam name="TEvent">
        /// The type of event being published.
        /// </typeparam>
        /// <returns>
        /// The <see cref="Task"/> result of the publishing request.
        /// </returns>
        public static Task<Task[]> Publish<TEvent>(object sender, Task<TEvent> eventDataTask)
        {
            TaskCompletionSource<Task[]> taskCompletionSouce = new TaskCompletionSource<Task[]>();

            _taskFactory.StartNew(
                () =>
                    {
                        ConcurrentDictionary<object, ConcurrentBag<object>> subscribers;

                        Type eventType = typeof(TEvent);

                        if (_events.TryGetValue(eventType, out subscribers))
                        {
                            if (subscribers.Count == 0)
                            {
                                taskCompletionSouce.SetException(new Exception(ExceptionMessages.SubscribersNotFound));
                            }
                            else
                            {
                                _taskFactory.ContinueWhenAll(
                                    new ConcurrentBag<Task>(
                                        new ConcurrentBag<object>(subscribers.Keys)
                                            .Where(subscriber => subscriber != sender)
                                            .Select(
                                                subscriber =>
                                                    {
                                                        ConcurrentBag<object> eventHandlerTaskFactories;
                                                        bool hasValue = subscribers.TryGetValue(
                                                            subscriber,
                                                            out eventHandlerTaskFactories);
                                                        return
                                                            new
                                                                {
                                                                    HasValue = hasValue,
                                                                    EventHandlerTaskFactories =
                                                                        eventHandlerTaskFactories
                                                                };
                                                    })
                                            .SelectMany(
                                                subscriber =>
                                                    {
                                                        if (!subscriber.HasValue)
                                                        {
                                                            TaskCompletionSource<Task> innerTaskCompletionSource =
                                                                new TaskCompletionSource<Task>();
                                                            innerTaskCompletionSource.SetException(new Exception(ExceptionMessages.FailedToGetEventHandlerTaskFactories));
                                                            return
                                                                new ConcurrentBag<Task>(
                                                                    new[] { innerTaskCompletionSource.Task });
                                                        }

                                                        return new ConcurrentBag<Task>(subscriber.EventHandlerTaskFactories
                                                            .Select(
                                                                eventHandlerTaskFactory =>
                                                                    {
                                                                        try
                                                                        {
                                                                            return
                                                                                ((Func<Task<TEvent>, Task>)
                                                                                 eventHandlerTaskFactory)(eventDataTask);
                                                                        }
                                                                        catch (Exception exception)
                                                                        {
                                                                            TaskCompletionSource<object> innerTaskCompletionSource = new TaskCompletionSource<object>(_taskFactory.CreationOptions);
                                                                            innerTaskCompletionSource.SetException(exception);
                                                                            return innerTaskCompletionSource.Task;
                                                                        }
                                                                    }));
                                                    }))
                                            .ToArray(),
                                    taskCompletionSouce.SetResult);
                            }
                        }
                        else
                        {
                            taskCompletionSouce.SetException(new Exception(ExceptionMessages.EventTypeNotFound));
                        }
                    });

            return taskCompletionSouce.Task;
        }

        /// <summary>
        /// Subscribes to an event.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="eventHandlerTaskFactory">
        /// The event handler task factory.
        /// </param>
        /// <typeparam name="TEvent">
        /// </typeparam>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public static Task Subscribe<TEvent>(object sender, Func<Task<TEvent>, Task> eventHandlerTaskFactory)
        {
            TaskCompletionSource<object> taskCompletionSource = new TaskCompletionSource<object>();

            _taskFactory.StartNew(
                () =>
                    {
                        AddSubscription<TEvent>(sender, eventHandlerTaskFactory, taskCompletionSource);
                    });

            return taskCompletionSource.Task;
        }

        public static Task Unsubscribe<TEvent>(object sender)
        {
            TaskCompletionSource<object> taskCompletionSource = new TaskCompletionSource<object>();

            _taskFactory.StartNew(
                () =>
                    {
                        RemoveSubscription<TEvent>(sender, taskCompletionSource);
                    });

            return taskCompletionSource.Task;
        }

        private static void AddSubscription<TEvent>(
            object sender,
            Func<Task<TEvent>, Task> eventHandlerTaskFactory,
            TaskCompletionSource<object> taskCompletionSource)
        {
            ConcurrentDictionary<object, ConcurrentBag<object>> subscribers;

            Type eventType = typeof(TEvent);

            if (_events.TryGetValue(eventType, out subscribers))
            {
                ConcurrentBag<object> eventHandlerTaskFactories;

                if (subscribers.TryGetValue(sender, out eventHandlerTaskFactories))
                {
                    eventHandlerTaskFactories.Add(eventHandlerTaskFactory);
                    taskCompletionSource.SetResult(null);
                }
                else
                {
                    AddEventHandlerTaskFactory(sender, eventHandlerTaskFactory, taskCompletionSource, subscribers);
                }
            }
            else
            {
                subscribers = new ConcurrentDictionary<object, ConcurrentBag<object>>();

                if (_events.TryAdd(eventType, subscribers))
                {
                    AddEventHandlerTaskFactory(sender, eventHandlerTaskFactory, taskCompletionSource, subscribers);
                }
                else
                {
                    taskCompletionSource.SetException(new Exception(ExceptionMessages.FailedToAddSubscribers));
                }
            }
        }

        private static void AddEventHandlerTaskFactory<TEvent>(
            object sender,
            Func<Task<TEvent>, Task> eventHandlerTaskFactory,
            TaskCompletionSource<object> taskCompletionSource,
            ConcurrentDictionary<object, ConcurrentBag<object>> subscribers)
        {
            ConcurrentBag<object> eventHandlerTaskFactories = new ConcurrentBag<object>();

            if (subscribers.TryAdd(sender, eventHandlerTaskFactories))
            {
                eventHandlerTaskFactories.Add(eventHandlerTaskFactory);
                taskCompletionSource.SetResult(null);
            }
            else
            {
                taskCompletionSource.SetException(new Exception(ExceptionMessages.FailedToAddEventHandlerTaskFactories));
            }
        }

        private static void RemoveSubscription<TEvent>(object sender, TaskCompletionSource<object> taskCompletionSource)
        {
            ConcurrentDictionary<object, ConcurrentBag<object>> subscribers;

            Type eventType = typeof(TEvent);

            if (_events.TryGetValue(eventType, out subscribers))
            {
                if (subscribers == null)
                {
                    taskCompletionSource.SetException(new Exception(ExceptionMessages.FailedToGetSubscribers));
                }
                else
                {
                    ConcurrentBag<object> eventHandlerTaskFactories;
                    if (subscribers.TryRemove(sender, out eventHandlerTaskFactories))
                    {
                        taskCompletionSource.SetResult(null);
                    }
                    else
                    {
                        taskCompletionSource.SetException(
                            new Exception(ExceptionMessages.FailedToRemoveEventHandlerTaskFactories));
                    }
                }
            }
            else
            {
                taskCompletionSource.SetException(new Exception(ExceptionMessages.FailedToGetSubscribers));
            }
        }
    }
}
