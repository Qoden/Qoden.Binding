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
                Binding = binding ?? throw new ArgumentNullException("binding");
			}

			public SourceBuilder<T> BeforeExecute (CommandBindingAction action)
			{
				Binding.BeforeExecuteAction = action;
				return this;
			}

			public SourceBuilder<T> AfterExecute (CommandBindingAction action)
			{
				Binding.AfterExecuteAction = action;
				return this;
			}

			public SourceBuilder<T> WhenStarted (CommandBindingAction action)
			{
				var b = (IAsyncCommandBinding)Binding;
				b.CommandStarted = action;
				return this;
			}

			public SourceBuilder<T> WhenFinished (CommandBindingAction action)
			{
				var b = (IAsyncCommandBinding)Binding;
				b.CommandFinished = action;
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
		
            public SourceBuilder<T> ParameterConverter (Func<object, object> parameterConverter)
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
