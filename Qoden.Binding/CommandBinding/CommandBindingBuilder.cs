using System;

namespace Qoden.Binding
{
	public static class CommandBindingBuilder
	{

		public struct SourceBuilder<T>
			where T: ICommand
		{
			public readonly ICommandBinding Binding;

			internal SourceBuilder (T command) : this (new CommandBinding {
					Source = command
				})
			{
			}

			internal SourceBuilder (ICommandBinding binding)
			{
				if (binding == null)
					throw new ArgumentNullException ("binding");
				Binding = binding;
			}

			public SourceBuilder<T> BeforeUpdateTarget (CommandBindingAction<T> action)
			{
				var old = Binding.UpdateTargetAction;
				Binding.UpdateTargetAction = (t, s) => {
					action (t, (T)s);
					old (t, s);
				};
				return this;
			}

			public SourceBuilder<T> AfterUpdateTarget (CommandBindingAction<T> action)
			{
				var old = Binding.UpdateTargetAction;
				Binding.UpdateTargetAction = (t, s) => {
					old (t, s);
					action (t, (T)s);
				};
				return this;
			}

			public SourceBuilder<T> UpdateTarget (CommandBindingAction<T> action)
			{
				Binding.UpdateTargetAction = action != null ? new CommandBindingAction ((t, s) => action (t, (T)s)) : null;
				return this;
			}

			public SourceBuilder<T> BeforeExecute (CommandBindingAction<T> action)
			{
				Binding.BeforeExecuteAction = (t, s) => action (t, (T)s);
				return this;
			}

			public SourceBuilder<T> AfterExecute (CommandBindingAction<T> action)
			{
				Binding.AfterExecuteAction = (t, s) => action (t, (T)s);
				return this;
			}

			public SourceBuilder<T> WhenStarted (CommandBindingAction<T> action)
			{
				var b = (IAsyncCommandBinding)Binding;
				b.CommandStarted = (t, s) => action (t, (T)s);
				return this;
			}

			public SourceBuilder<T> WhenFinished (CommandBindingAction<T> action)
			{
				var b = (IAsyncCommandBinding)Binding;
				b.CommandFinished = (t, s) => action (t, (T)s);
				return this;
			}

			public SourceBuilder<T> Disabled ()
			{
				Binding.Enabled = false;
				return this;
			}

			public void To (IEventSource source, object parameter = null)
			{
				Binding.Target = source;
				Binding.Parameter = parameter;
			}

			public TargetBuilder<Target, T> To<Target> (IEventSource<Target> source, object parameter = null)
			{
				Binding.Target = source;
				Binding.Parameter = parameter;
				return new TargetBuilder<Target, T> (Binding);
			}

            public SourceBuilder<T> ParameterConverter (Func<object, object> parameterConverter)
            {
                Binding.ParameterConverter = parameterConverter;
                return this;
            }
        }

		public struct TargetBuilder<T, S>
			where S : ICommand
		{
			public readonly ICommandBinding Binding;

			internal TargetBuilder (ICommandBinding binding)
			{
				Binding = binding;
			}

			public TargetBuilder<T, S> BeforeUpdateTarget (CommandBindingAction<T, S> action)
			{
				var old = Binding.UpdateTargetAction;
				Binding.UpdateTargetAction = (t, s) => {
					action ((IEventSource<T>)t, (S)s);
					old (t, s);
				};
				return this;
			}

			public TargetBuilder<T, S> AfterUpdateTarget (CommandBindingAction<T, S> action)
			{
				var old = Binding.UpdateTargetAction;
				Binding.UpdateTargetAction = (t, s) => {
					old (t, s);
					action ((IEventSource<T>)t, (S)s);
				};
				return this;
			}

			public TargetBuilder<T, S> UpdateTarget (CommandBindingAction<T,S> action)
			{
				Binding.UpdateTargetAction = (t, s) => action ((IEventSource<T>)t, (S)s);
				return this;
			}

			public TargetBuilder<T, S> BeforeExecute (CommandBindingAction<T, S> action)
			{
				Binding.BeforeExecuteAction = (t, s) => action ((IEventSource<T>)t, (S)s);
				return this;
			}

			public TargetBuilder<T, S> AfterExecute (CommandBindingAction<T, S> action)
			{
				Binding.AfterExecuteAction = (t, s) => action ((IEventSource<T>)t, (S)s);
				return this;
			}

			public TargetBuilder<T, S> WhenStarted (CommandBindingAction<T, S> action)
			{
				var b = (IAsyncCommandBinding)Binding;
				b.CommandStarted = (t, s) => action ((IEventSource<T>)t, (S)s);
				return this;
			}

			public TargetBuilder<T, S> WhenFinished (CommandBindingAction<T, S> action)
			{
				var b = (IAsyncCommandBinding)Binding;
				b.CommandFinished = (t, s) => action ((IEventSource<T>)t, (S)s);
				return this;
			}

            public TargetBuilder<T, S> ParameterConverter (Func<object, object> parameterConverter)
            {
                Binding.ParameterConverter = parameterConverter;
                return this;
            }
        }

		public static SourceBuilder<T> Command<T> (this BindingList list, T command)
			where T : ICommand
		{
			var builder = new SourceBuilder<T> (command);
			list.Add (builder.Binding);
			return builder;
		}


	}

}
