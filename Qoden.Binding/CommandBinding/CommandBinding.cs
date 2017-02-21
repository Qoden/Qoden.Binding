using System;
using Qoden.Reflection;
using Qoden.Validation;

namespace Qoden.Binding
{
	public delegate void CommandBindingAction (IEventSource target, ICommand command);

	public delegate void CommandBindingAction<T> (IEventSource target, T command)
		where T : ICommand;

	public delegate void CommandBindingAction<Target, Source> (IEventSource<Target> target, Source command)
		where Source : ICommand;

	public interface ICommandBinding : IBinding
	{
		/// <summary>
		/// Event source which executes Source command
		/// </summary>
		IEventSource Target { get; set; }

		/// <summary>
		/// Command to execute
		/// </summary>
		ICommand Source { get; set; }

		/// <summary>
		/// Action to update Target enabled/disabled status when command CanExecute changes
		/// </summary>
		CommandBindingAction UpdateTargetAction { get; set; }

		/// <summary>
		/// Action which is called right before Command Execute method is called
		/// </summary>
		CommandBindingAction BeforeExecuteAction { get; set; }

		/// <summary>
		/// Action which is called right after Command Execute method is called. 
		/// It does not wait until async command execution finish. For this see IAsyncCommandBinding interface.
		/// </summary>
		CommandBindingAction AfterExecuteAction { get; set; }

		/// <summary>
		/// Gets or sets the parameter to be passed to command Execute method.
		/// </summary>
		object Parameter { get; set; }

        /// <summary>
        /// Func to convert Parameter from Target
        /// </summary>
        Func<object, object> ParameterConverter { get; set; }
    }

	public interface IAsyncCommandBinding : ICommandBinding
	{
		/// <summary>
		/// Action to execute when command is started as defined by IsRunning property
		/// </summary>
		CommandBindingAction CommandStarted { get; set; }
		/// <summary>
		/// Action to execute when command is finished as defined by IsRunning property
		/// </summary>
		CommandBindingAction CommandFinished { get; set; }
	}

	public class CommandBinding : IAsyncCommandBinding
	{
		public CommandBinding ()
		{
			UpdateTargetAction = DefaultUpdateTargetAction;
			Enabled = true;
		}

		public CommandBindingAction UpdateTargetAction { get; set; }

		public CommandBindingAction BeforeExecuteAction { get; set; }

		public CommandBindingAction AfterExecuteAction { get; set; }

		public CommandBindingAction CommandStarted { get; set; }

		public CommandBindingAction CommandFinished { get; set; }

		public object Parameter { get; set; }

        public Func<object, object> ParameterConverter { get; set; }

		void DefaultUpdateTargetAction (IEventSource t, ICommand command)
		{
			t.SetEnabled (command.CanExecute (Parameter));
		}

		public void Bind ()
		{
			if (!Bound) {
				OnBind ();
				Bound = true;
			}
		}

		protected virtual void OnBind ()
		{
			Source.CanExecuteChanged += Source_CanExecuteChanged;
			Target.Handler += Target_ExecuteCommand;
			var asyncCommand = Source as IAsyncCommand;
			if (asyncCommand != null) {
				asyncCommand.PropertyChanged += AsyncCommand_PropertyChanged;
			}
		}

		public void Unbind ()
		{
			if (Bound) {
				OnUnbind ();
				Bound = false;
			}
		}

		protected virtual void OnUnbind ()
		{
			Source.CanExecuteChanged -= Source_CanExecuteChanged;
			Target.Handler -= Target_ExecuteCommand;
			var asyncCommand = Source as IAsyncCommand;
			if (asyncCommand != null) {
				asyncCommand.PropertyChanged -= AsyncCommand_PropertyChanged;
			}
		}

		void Source_CanExecuteChanged (object sender, EventArgs e)
		{
			UpdateTarget ();
		}

		void Target_ExecuteCommand (object sender, EventArgs e)
		{
			if (Enabled) {
                var isAsyncCommand = Source is IAsyncCommand;

                if (BeforeExecuteAction != null)
					BeforeExecuteAction (Target, Source);
                if (!isAsyncCommand && CommandStarted != null)
                    CommandStarted(Target, Source);
                if (Target.ParameterExtractor != null) {
					var extractedParameter = Target.ParameterExtractor(sender, e);
                    if (ParameterConverter != null) {
                        extractedParameter = ParameterConverter(extractedParameter);
                    }
                    Parameter = extractedParameter;
                }
                try
                {
                    Source.Execute (Parameter);
                }
                finally
                {
                    if (AfterExecuteAction != null)
                        AfterExecuteAction (Target, Source);
                    if (!isAsyncCommand && CommandFinished != null)
                        CommandFinished(Target, Source);
                }
            }
		}

		void AsyncCommand_PropertyChanged (object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			var command = (IAsyncCommand)sender;
			if (e.PropertyName == "IsRunning") {
				if (command.IsRunning) {
					if (CommandStarted != null)
						CommandStarted (Target, Source);
				} else {
					if (CommandFinished != null)
						CommandFinished (Target, Source);
				}
			}
		}

		public void UpdateTarget ()
		{
			if (Enabled && UpdateTargetAction != null) {
				UpdateTargetAction (Target, Source);
			}
		}

		public void UpdateSource ()
		{
			throw new NotSupportedException();
		}

		IEventSource target;

		public IEventSource Target {
			get { return target; }
			set {
				Assert.State (Bound, "Bound").IsFalse ();
				target = value;
			}
		}

		ICommand source;

		public ICommand Source {
			get { return source; }
			set {
				Assert.State (Bound, "Bound").IsFalse ();
				source = value;
			}
		}

		public bool Enabled { get; set; }

		public bool Bound { get; private set; }
	}

}