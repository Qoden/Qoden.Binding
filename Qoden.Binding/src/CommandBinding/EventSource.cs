using System;
using System.Collections.Generic;
using System.Reflection;
using Qoden.Reflection;
using Qoden.Util;

#if __ANDROID__
using Android.Text;
#endif

namespace Qoden.Binding
{
	/// <summary>
	/// Source of events which trigget commands.
	/// </summary>
	public interface IEventSource
	{
		/// <summary>
		/// Owner of event
		/// </summary>
		object Owner { get; }

		/// <summary>
		/// Proxy event handler to susbcribe/unsubscribe from event this source represents.
		/// </summary>
		event EventHandler Handler;

		/// <summary>
		/// Enable/Disable event owner so it start/stop generate events.
		/// </summary>
		void SetEnabled (bool enabled);

        /// <summary>
        /// Func to extract Parameter from Target EventArgs
        /// </summary>
        Func<object, EventArgs, object> ParameterExtractor { get; set; }
    }

	public interface IEventSource<T> : IEventSource
	{
		new T Owner { get; }
	}

	/// <summary>
	/// IEventSource adapter for .NET events
	/// </summary>
    public class EventHandlerSource : IEventSource
    {
        readonly RuntimeEvent @event;
        readonly object owner;
        readonly Delegate eventHandler;
        static readonly MethodInfo HandleOwnerEventMethodInfo = typeof(EventHandlerSource)
            .GetMethod("HandleOwnerEvent", BindingFlags.NonPublic | BindingFlags.Instance);

        event EventHandler handler;
        int subscriptions = 0;

        public EventHandlerSource(RuntimeEvent @event, object owner)
        {            
            this.@event = @event;
            this.owner = owner;

            eventHandler = @event.CreateDelegate(this, HandleOwnerEventMethodInfo);
        }

        [Preserve]
        void HandleOwnerEvent(object sender, EventArgs args)
        {
            handler?.Invoke(sender, args);
        }

        public void SetEnabled(bool enabled)
        {
            SetEnabledAction?.Invoke(Owner, enabled);
        }

        public Func<object, EventArgs, object> ParameterExtractor { get; set; }

        public object Owner { get { return owner; } }

        public Action<object, bool> SetEnabledAction { get; set; }

        public event EventHandler Handler
        {
            add
            {
                handler += value;
                if (subscriptions == 0)
                    this.@event.AddEventHandler(Owner, eventHandler);
                subscriptions++;
            }
            remove
            {
                handler -= value;
                subscriptions--;
                if (subscriptions == 0)
                    this.@event.RemoveEventHandler(Owner, eventHandler);
            }
        }
    }

	/// <summary>
	/// IEventSource adapter for .NET events with typed Owner
	/// </summary>
	public class EventHandlerSource<T> : EventHandlerSource, IEventSource<T>
	{
		public EventHandlerSource (RuntimeEvent @event, T owner) : base (@event, owner)
		{
		}

		public new T Owner {
			get { return (T)base.Owner; }
		}

		public new Action<T, bool> SetEnabledAction { 
			get { 
				return (o, b) => SetEnabledAction (o, b);
			}
			set { 
				base.SetEnabledAction = (o, b) => value ((T)o, b);
			}
		}
	}

    /// <summary>
    /// EventListSource allows to subscribe to an event of multiple objects.
    /// T is the type of object with the event
    /// </summary>
    public class EventListSource<T> : IEventSource
    {
        readonly RuntimeEvent @event;
        readonly List<T> owners = new List<T>();
        readonly List<object> replacementSenders = new List<object>();
        readonly Delegate eventHandler;
        static readonly MethodInfo HandleOwnerEventMethodInfo = typeof(EventListSource<T>)
            .GetMethod("HandleOwnerEvent", BindingFlags.NonPublic | BindingFlags.Instance);

        event EventHandler handler;
        int subscriptions = 0;

        public EventListSource(RuntimeEvent @event)
        {
            this.@event = @event;

            eventHandler = @event.CreateDelegate(this, HandleOwnerEventMethodInfo);
        }

        /// <summary>
        /// Listen to the event on this object
        /// </summary>
        /// <param name="owner">Object to subscribe to</param>
        /// <param name="replacementSender">Replace sender in the callback with this object if it is not null</param>
        public void Listen(T owner, object replacementSender = null)
        {
            Owners.Add(owner);
            replacementSenders.Add(replacementSender);
            if (subscriptions > 0)
            {
                @event.AddEventHandler(owner, eventHandler);
            }
        }

        void HandleOwnerEvent(object sender, EventArgs args)
        {
            if (handler != null)
            {
                var index = Owners.IndexOf((T)sender);
                var replacementSender = replacementSenders[index] ?? sender;
                handler(replacementSender, args);
            }
        }

        public void SetEnabled(bool enabled)
        {
            if (SetEnabledAction != null)
            {
                foreach(var owner in Owners)
                {
                    SetEnabledAction(owner, enabled);
                }
            }
        }

        public Func<object, EventArgs, object> ParameterExtractor { get; set; }

        public object Owner { get { return Owners; } }

        public List<T> Owners { get { return owners; } }

        public Action<object, bool> SetEnabledAction { get; set; }

        public event EventHandler Handler
        {
            add
            {
                handler += value;
                if (subscriptions == 0)
                {
                    foreach (var owner in Owners)
                    {
                        @event.AddEventHandler(owner, eventHandler);
					}
                }
                subscriptions++;
            }
            remove
            {
                handler -= value;
                subscriptions--;
                if (subscriptions == 0)
                {
                    foreach (var owner in Owners)
                    {
                        @event.RemoveEventHandler(owner, eventHandler);
                    }
                }
            }
        }
    }

}