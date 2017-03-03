using System;
using Qoden.Reflection;
using Qoden.Validation;

namespace Qoden.Binding
{
	public interface IPropertyBindingStrategy
	{		
		/// <summary>
		/// Subscribes action to property change event and return subscription object.
		/// Caller can dispose returned object to unsubscribe action from event.
		/// </summary>
		IDisposable SubscribeToPropertyChange (IProperty property, Action<IProperty> action);
	}

	public abstract class EventPropertyBindingStrategyBase : IPropertyBindingStrategy
	{
		public IDisposable SubscribeToPropertyChange (IProperty property, Action<IProperty> action)
		{
			Assert.Argument (property, "property").NotNull ();
			Assert.Argument (action, "action").NotNull ();
			return new EventSubscription (GetEvent(), AdaptAction(property, action), property.Owner);
		}

		protected abstract RuntimeEvent GetEvent();
		protected abstract Delegate AdaptAction(IProperty property, Action<IProperty> action);
	}

	public abstract class EventHandlerBindingStrategyBase : EventPropertyBindingStrategyBase
	{
		readonly RuntimeEvent @event;
		public EventHandlerBindingStrategyBase(RuntimeEvent @event)
		{
			Assert.Argument(@event, "event").NotNull();
			this.@event = @event;
		}

		protected sealed override RuntimeEvent GetEvent()
		{
			return @event;
		}
	}

	public class EventHandlerBindingStrategy : EventHandlerBindingStrategyBase
	{		
		public EventHandlerBindingStrategy (RuntimeEvent @event) : base(@event)
		{
		}

		protected override Delegate AdaptAction(IProperty property, Action<IProperty> action)
		{
			return new EventHandler ((s, e) => action(property));
		}
	}

	public class EventHandlerBindingStrategy<T> : EventHandlerBindingStrategyBase where T : EventArgs
	{
		public EventHandlerBindingStrategy(RuntimeEvent @event) : base(@event)
		{
		}

		protected override Delegate AdaptAction(IProperty property, Action<IProperty> action)
		{
			return new EventHandler<T>((s, e) => action(property));
		}
	}


}