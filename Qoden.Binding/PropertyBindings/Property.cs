using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Collections;
using System.Linq.Expressions;
using Qoden.Reflection;
using Qoden.Validation;

namespace Qoden.Binding
{
	/// <summary>
	/// A property which can be managed by binding engine
	/// </summary>
	public interface IProperty
	{
		/// <summary>
		/// Object containing this property
		/// </summary>
		object Owner { get; }

		/// <summary>
		/// Property name
		/// </summary>
		string Key { get; }

		/// <summary>
		/// Gets the binding strategy to be user to detect changes in this property.
		/// </summary>
		/// <value>The binding strategy.</value>
		IPropertyBindingStrategy BindingStrategy { get; }

		/// <summary>
		/// Gets or sets underlying property value.
		/// </summary>
		object Value { get; set; }

		/// <summary>
		/// Property type
		/// </summary>
		Type PropertyType { get; }

		/// <summary>
		/// Gets a value indicating whether property is read only.
		/// </summary>
		bool IsReadOnly { get; }

		/// <summary>
		/// Gets a value indicating whether there are any errors related to underlying property.
		/// </summary>
		bool HasErrors { get; }

		/// <summary>
		/// Gets the errors (if any) related to underlying property.
		/// </summary>
		IEnumerable Errors { get; }

		/// <summary>
		/// Subscribes action to a property change event and return subscription object.
		/// Caller can dispose returned object to unsubscribe action from event.
		/// </summary>
		IDisposable OnPropertyChange(Action<IProperty> action);
	}

	public interface IProperty<T> : IProperty
	{
		new T Value { get; set; }
	}

    public static class PropertyExtensions
    {
        public static Property<PropertyType> GetProperty<T, PropertyType>(this T owner, Expression<Func<T, PropertyType>> key, IPropertyBindingStrategy bindingStrategy = null, Func<PropertyType> getter = null, Action<T, PropertyType> setter = null)
        {
            Action<object, PropertyType> mySetter = null;
            if (setter != null) {
                mySetter = (o, v) => setter((T)o, v);
            }
            return new Property<PropertyType>(owner, PropertySupport.ExtractPropertyName(key), bindingStrategy, getter, mySetter);
        }
    }

	public class Property<PropertyType> : IProperty<PropertyType>
	{
        readonly Func<PropertyType> Getter;
		readonly Action<object, PropertyType> Setter;

		public Property(object owner, string key, IPropertyBindingStrategy bindingStrategy = null, Func<PropertyType> getter = null, Action<object, PropertyType> setter = null)
		{
			Owner = owner;
			Key = key;
			Getter = getter;
            Setter = setter;
			BindingStrategy = bindingStrategy;
		}

		public IPropertyBindingStrategy BindingStrategy { get; private set; }

		public object Owner { get; private set; }

		public string Key { get; private set; }

		public PropertyType Value
		{
			get { return Getter != null ? Getter() : GetValue(); }
			set { if (Setter != null) { Setter(Owner, value); } else { SetValue(value); } }
		}

		/// <summary>
		/// Override this method to customize how property value is get from Owner
		/// </summary>
		protected virtual void SetValue(PropertyType value)
		{
			KeyValueCoding.Impl(Owner).Set(Owner, Key, value);
		}

		/// <summary>
		/// Override this method to customize how property value is set.
		/// </summary>
		protected virtual PropertyType GetValue()
		{
			return (PropertyType)KeyValueCoding.Impl(Owner).Get(Owner, Key);
		}


		public bool HasErrors
		{
			get
			{
				var e = (IEnumerable<object>)Errors;
				return e.Any();
			}
		}

		public virtual IEnumerable Errors
		{
			get
			{
				var errorInfo = Owner as INotifyDataErrorInfo;
				if (errorInfo != null)
				{
					return errorInfo.GetErrors(Key);
				}
				else {
					return Enumerable.Empty<object>();
				}
			}
		}

		/// <summary>
		/// Determines whether this instance represent same property as given Expression.
		/// </summary>
		public bool Is<OwnerType>(Expression<Func<OwnerType, PropertyType>> property)
		{
			return PropertySupport.ExtractPropertyName(property) == Key;
		}

		object IProperty.Owner { get { return Owner; } }

		object IProperty.Value
		{
			get { return Value; }
			set { Value = (PropertyType)value; }
		}

		public virtual bool IsReadOnly { get { return KeyValueCoding.Impl(Owner).IsReadonly(Owner, Key); } }

		Type IProperty.PropertyType { get { return typeof(PropertyType); } }

		IDisposable IProperty.OnPropertyChange(Action<IProperty> action)
		{
			if (BindingStrategy == null) 
				throw new InvalidOperationException("Property '" + this.Key + "' does not support change tracking (BindingStrategy is null)");
			return BindingStrategy.SubscribeToPropertyChange(this, action);
		}
	}
}