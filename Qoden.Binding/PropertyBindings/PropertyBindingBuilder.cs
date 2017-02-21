using System;
using System.ComponentModel;
using System.Linq.Expressions;
using Qoden.Reflection;
using Qoden.Validation;

namespace Qoden.Binding
{
	public static class PropertyBindingBuilder
	{
		public static SourceBinding<T> Create<S, T> (S source, Expression<Func<S,T>> property)
			where S : INotifyPropertyChanged
		{
			Assert.Argument (source, "source").NotNull ();
			Assert.Argument (property, "property").NotNull ();

			var sourceProperty = source.Property (property);
			return Create (sourceProperty);
		}

		public static SourceBinding<T> Create<T> (IProperty<T> source)
		{
			Assert.Argument (source, "source").NotNull ();
			return _Create<T> (new PropertyBinding { 
				Source = source
			});
		}

		public static SourceBinding<T> Create<T> (IPropertyBinding binding)
		{
			Assert.Argument (binding, "binding").NotNull ();
			return _Create<T>(binding);
		}

		static SourceBinding<T> _Create<T> (IPropertyBinding binding)
		{			
			return new SourceBinding<T> (binding);
		}

		public struct SourceBinding<S>
		{
			readonly IPropertyBinding binding;

			public SourceBinding (IPropertyBinding binding)
			{
				if (binding == null)
					throw new ArgumentNullException ("binding");
				this.binding = binding;
			}

			public IPropertyBinding Binding { get { return binding; } }

			public void UpdateTarget (SourcePropertyAction<S> action)
			{
				binding.UpdateTargetAction = (t, s) => action ((IProperty<S>)s);
			}

			public SourceBinding<S>
			BeforeTargetUpdate (SourcePropertyAction<S> action)
			{
				var oldAction = binding.UpdateTargetAction;
				binding.UpdateTargetAction = new BindingAction ((t, s) => {
					action ((IProperty<S>)s);
					oldAction (t, s);
				});
				return this;
			}

			public SourceBinding<S>
			AfterTargetUpdate (SourcePropertyAction<S> action)
			{
				var oldAction = binding.UpdateTargetAction;
				binding.UpdateTargetAction = new BindingAction((t, s) => {				
					oldAction (t, s);
					action ((IProperty<S>)s);
				});
				return this;
			}

			public SourceBinding<NewT>
			Convert<NewT> (Func<S, NewT> to, Func<NewT, S> from = null)
			{
				var sp = (IProperty<S>)binding.Source;
				binding.Source = sp.Convert (to, from);	
				return new SourceBinding<NewT> (binding);
			}

			public TargetBinding<S> 
			To (IProperty<S> targetProperty)
			{
				binding.Target = targetProperty;
				return new TargetBinding<S> (binding);
			}
		}

		public struct TargetBinding<T>
		{
			readonly IPropertyBinding binding;

			public TargetBinding (IPropertyBinding binding)
			{
				this.binding = binding;
			}

			BindingAction Cast (BindingAction<T> action)
			{
				return (t, s) => action ((IProperty<T>)t, (IProperty<T>)s);
			}

			public TargetBinding<T> 
			UpdateTarget (BindingAction<T> action)
			{
				binding.UpdateTargetAction = Cast (action);
				return this;
			}

			public TargetBinding<T>
			UpdateSource (BindingAction<T> action)
			{
				binding.UpdateSourceAction = Cast (action);
				return this;
			}

			BindingAction<T>
			Before (BindingAction d, BindingAction<T> action)
			{
				return new BindingAction<T> ((t, s) => {
					action (t, s);
					d (t, s);
				});
			}

			public TargetBinding<T>
			BeforeTargetUpdate (BindingAction<T> action)
			{			
				binding.UpdateTargetAction = Cast (Before (binding.UpdateTargetAction, action));
				return this;
			}

			public TargetBinding<T>
			BeforeSourceUpdate (BindingAction<T> action)
			{
				binding.UpdateSourceAction = Cast (Before (binding.UpdateSourceAction, action));
				return this;
			}

			BindingAction<T>
			After (BindingAction d, BindingAction<T> action)
			{
				return new BindingAction<T> ((t, s) => {
					d (t, s);
					action (t, s);
				});
			}

			public TargetBinding<T>
			AfterTargetUpdate (BindingAction<T> action)
			{
				binding.UpdateTargetAction = Cast (After (binding.UpdateTargetAction, action));
				return this;
			}

			public TargetBinding<T>
			AfterSourceUpdate (BindingAction<T> action)
			{
				binding.UpdateSourceAction = Cast (After (binding.UpdateSourceAction, action));
				return this;
			}

			public TargetBinding<T> OneWay ()
			{
				binding.DontUpdateSource ();
				return this;
			}

			public TargetBinding<T> OneWayToSource ()
			{
				var old = binding.UpdateSourceAction;
				var b = binding;
				BindingAction initTargetAndStopUpdating = (t, s) => {
					old (t, s);
					b.DontUpdateSource ();
				};
				binding.UpdateSourceAction = initTargetAndStopUpdating;
				return this;
			}
		}
	}

	public static class PropertyBindingBuilderExtensions
	{
		public static PropertyBindingBuilder.SourceBinding<T> Property<OwnerT, T> (this BindingList list, OwnerT source, Expression<Func<OwnerT,T>> property)
			where OwnerT : INotifyPropertyChanged
		{
			var builder = PropertyBindingBuilder.Create (source, property);
			list.Add (builder.Binding);
			return builder;
		}

		public static PropertyBindingBuilder.SourceBinding<T> Property<T> (this BindingList list, IProperty<T> source)
		{
			var builder = PropertyBindingBuilder.Create (source);
			list.Add (builder.Binding);
			return builder;
		}

		public static PropertyBindingBuilder.SourceBinding<T> Property<T> (this BindingList list, IPropertyBinding binding)
		{
			var builder = PropertyBindingBuilder.Create<T> (binding);
			list.Add (builder.Binding);
			return builder;
		}
	}
	
}
