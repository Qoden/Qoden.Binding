using System;
using System.ComponentModel;
using System.Linq.Expressions;
using Qoden.Reflection;

namespace Qoden.Binding
{	
	public static class NotifyPropertyChangedProperties 
	{
		public static IProperty Property(this INotifyPropertyChanged owner, string key)			
		{
			return Property<INotifyPropertyChanged, object> (owner, key);
		}

		public static IProperty<T> Property<OwnerT, T>(this OwnerT owner, Expression<Func<OwnerT, T>> key)
			where OwnerT : INotifyPropertyChanged
		{
			return Property<OwnerT, T> (owner, PropertySupport.ExtractPropertyName (key));
		}

		public static IProperty<T> Property<OwnerT, T>(this OwnerT owner, string key)			
			where OwnerT : INotifyPropertyChanged
		{
			return new Property<T> (owner, key, BindingStrategy);
		}

		public static readonly IPropertyBindingStrategy BindingStrategy = new NotifyPropertyChangedBindingStrategy ();
		public static readonly RuntimeEvent PropertyChangedEvent = new RuntimeEvent (typeof(INotifyPropertyChanged), "PropertyChanged");

		class NotifyPropertyChangedBindingStrategy : EventPropertyBindingStrategyBase
		{
			protected override RuntimeEvent GetEvent ()
			{
				return PropertyChangedEvent;
			}

			protected override Delegate AdaptAction (IProperty property, Action<IProperty> action)
			{
				return new PropertyChangedEventHandler ((s, e) => {
					if (e.PropertyName == property.Key) 
						action(property);
				});
			}
		}
	}
}
