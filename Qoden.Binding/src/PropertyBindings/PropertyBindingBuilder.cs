using System;
using System.ComponentModel;
using System.Linq.Expressions;
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
                this.binding = binding ?? throw new ArgumentNullException(nameof(binding));
			}

			public IPropertyBinding Binding { get { return binding; } }

			public void UpdateTarget (PropertyBindingAction action)
			{
                binding.UpdateTargetAction = action;
			}

			public SourceBinding<S>
			BeforeTargetUpdate (PropertyBindingAction action)
			{
				var oldAction = binding.UpdateTargetAction;
				binding.UpdateTargetAction = new PropertyBindingAction ((binding, source) => {
					action (binding, source);
					oldAction (binding, source);
				});
				return this;
			}

			public SourceBinding<S>
			AfterTargetUpdate (PropertyBindingAction action)
			{
				var oldAction = binding.UpdateTargetAction;
				binding.UpdateTargetAction = new PropertyBindingAction((binding, change) => {				
					oldAction (binding, change);
                    action (binding, change);
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

			public TargetBinding<T> 
			UpdateTarget (PropertyBindingAction action)
			{
				binding.UpdateTargetAction = action;
				return this;
			}

			public TargetBinding<T>
			UpdateSource (PropertyBindingAction action)
			{
				binding.UpdateSourceAction = action;
				return this;
			}

			PropertyBindingAction
			Before (PropertyBindingAction d, PropertyBindingAction action)
			{
				return new PropertyBindingAction ((b, c) => {
                    action (b, c);
					d (b, c);
				});
			}

			public TargetBinding<T>
			BeforeTargetUpdate (PropertyBindingAction action)
			{			
				binding.UpdateTargetAction = Before (binding.UpdateTargetAction, action);
				return this;
			}

			public TargetBinding<T>
			BeforeSourceUpdate (PropertyBindingAction action)
			{
				binding.UpdateSourceAction = Before (binding.UpdateSourceAction, action);
				return this;
			}

			PropertyBindingAction
			After (PropertyBindingAction d, PropertyBindingAction action)
			{
				return new PropertyBindingAction ((t, s) => {
					d (t, s);
					action (t, s);
				});
			}

			public TargetBinding<T>
			AfterTargetUpdate (PropertyBindingAction action)
			{
				binding.UpdateTargetAction = After (binding.UpdateTargetAction, action);
				return this;
			}

			public TargetBinding<T>
			AfterSourceUpdate (PropertyBindingAction action)
			{
				binding.UpdateSourceAction = After (binding.UpdateSourceAction, action);
				return this;
			}

            public TargetBinding<T>
            AfterUpdate(PropertyBindingAction action)
            {
                AfterSourceUpdate(action);
                AfterTargetUpdate(action);
                return this;
            }

            public TargetBinding<T>
            BeforeUpdate(PropertyBindingAction action)
            {
                BeforeSourceUpdate(action);
                BeforeTargetUpdate(action);
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
				PropertyBindingAction initTargetAndStopUpdating = (t, s) => {
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
