using System;
using System.Threading.Tasks;
using System.Threading;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq.Expressions;
using Qoden.Validation;
using Qoden.Reflection;

namespace Qoden.Binding
{
	public interface ICommand
	{
		event EventHandler CanExecuteChanged;

		bool CanExecute(object parameter);

		void Execute(object parameter);
	}

	public static class CommandExtensions
	{
		public static void Execute(this ICommand command)
		{
			command.Execute(null);
		}

		public static bool CanExecute(this ICommand command)
		{
			return command.CanExecute(null);
		}
	}

	public abstract class CommandBase : ICommand
	{
		public event EventHandler CanExecuteChanged;

		public void RaiseCanExecuteChanged()
		{
			if (CanExecuteChanged != null)
			{
				CanExecuteChanged(this, EventArgs.Empty);
			}
		}

		bool ICommand.CanExecute(object parameter)
		{
			return CheckCanExecute(parameter);
		}

		protected abstract bool CheckCanExecute(object parameter);

		public void Execute(object parameter)
		{
			if (CheckCanExecute(parameter))
			{
				DoExecute(parameter);
			}
		}

		protected abstract void DoExecute(object parameter);
	}

	public class Command : CommandBase
	{
		public Command()
		{
		}

		public Command(Action action)
		{
			Action = _ => action();
		}

		public Command(Action action, Func<bool> canExecute) : this(action)
		{
			CanExecute = _ => canExecute();
		}

		public Action<object> Action { get; set; }

		public Func<object, bool> CanExecute { get; set; }

		protected override bool CheckCanExecute(object parameter)
		{
			return Action != null && (CanExecute == null || CanExecute(parameter));
		}

		protected override void DoExecute(object parameter)
		{
			Action(parameter);
		}
	}

	public interface IAsyncCommand : ICommand, INotifyPropertyChanged
	{
		bool IsRunning { get; }
		Exception Error { get; }
		Task ExecuteAsync(object parameter);
		IAsyncCommand CancelCommand { get; }
		bool CanCancel { get; }
	}

	public static class AsyncCommandExtensions
	{
		public static Task ExecuteAsync(this IAsyncCommand command)
		{
			return command.ExecuteAsync(null);
		}
	}

	public static class AsyncCommandProperties
	{
		public static IProperty<bool> IsRunningProperty<T>(this T command)
			where T : IAsyncCommand
		{
			return command.Property<T, bool>("IsRunning");
		}

	}

	public abstract class AsyncCommandBase : CommandBase, IAsyncCommand
	{
	    protected sealed override void DoExecute(object parameter)
		{
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            ExecuteAsyncImpl(parameter);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

		public async Task ExecuteAsync(object parameter)
		{
			if (!CheckCanExecute(parameter))
			{
				return;
			}
			await ExecuteAsyncImpl(parameter);
			if (Error != null)
			{
				throw Error;
			}
		}

		async Task ExecuteAsyncImpl(object parameter)
		{
			Error = null;
            IsRunningCount++;
			try
			{
				await DoExecuteAsync(parameter);
			}
			catch (Exception e)
			{
				Error = e;
			}
			finally
			{
                IsRunningCount--;
				if (_cancelCommand != null && _cancelCommand.IsRunning)
				{
					_cancelCommand.IsRunning = false;
				}
				_cts = null;
			}
		}

		protected abstract Task DoExecuteAsync(object parameter);

		CancelAsyncCommand _cancelCommand;

		public IAsyncCommand CancelCommand
		{
			get
			{
				if (_cancelCommand == null)
				{
					_cancelCommand = new CancelAsyncCommand(this);
				}
				return _cancelCommand;
			}
		}

		const string IsRunningErrorMessage = "Cannot change {Key} while command is running";
		public bool CanCancel => true;

	    CancellationTokenSource _cts;

		public CancellationTokenSource CancellationTokenSource
		{
			get
			{
				if (_cts == null)
					_cts = new CancellationTokenSource();
				return _cts;
			}
			set
			{
				Assert.Argument(value, "value").NotNull();
				Assert.State(IsRunning).IsFalse(IsRunningErrorMessage);
				_cts = value;
			}
		}

		public CancellationToken Token => CancellationTokenSource.Token;

	    public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(PropertyChangedEventArgs key)
        {
            PropertyChanged?.Invoke(this, key);
        }

        protected void RaisePropertyChanged([CallerMemberName] string key = null)
        {
            RaisePropertyChanged(new PropertyChangedEventArgs(key));
        }

        protected void RaisePropertyChanged<T>(Expression<Func<T>> key)
        {
            RaisePropertyChanged(new PropertyChangedEventArgs(PropertySupport.ExtractPropertyName(key)));
        }

	    private int _isRunningCount;
        protected int IsRunningCount
        {
            get { return _isRunningCount; }
            set
            {
                _isRunningCount = value;
                IsRunning = _isRunningCount > 0;
            }
        }

	    private bool _isRunning;

		public bool IsRunning
		{
			get { return _isRunning; }
			private set
			{
			    if (_isRunning == value) return;
			    _isRunning = value;
			    RaisePropertyChanged();
			    RaiseCanExecuteChanged();
			}
		}

	    private void Cancel()
		{
		    if (_cts == null || _cts.IsCancellationRequested || !IsRunning) return;
		    _cts.Cancel();
		    //if still running after call to Cancel then cancelCommand isRunning
		    if (IsRunning)
		    {
		        _cancelCommand.IsRunning = true;
		    }
		}

		public bool IsCancelRunning => _cancelCommand != null && _cancelCommand.IsRunning;

	    private Exception _error;
		public Exception Error
		{
			get { return _error; }
			set
			{
				if (_error != value)
				{
					_error = value;
                    RaisePropertyChanged();
				}
			}
		}

	    private class CancelAsyncCommand : CommandBase, IAsyncCommand
		{
			readonly AsyncCommandBase _owner;

			public CancelAsyncCommand(AsyncCommandBase owner)
			{
				_owner = owner;
				owner.PropertyChanged += Owner_IsRunningChanged;
			}

			protected override bool CheckCanExecute(object parameter)
			{
				return !IsRunning && _owner.IsRunning;
			}

			protected override void DoExecute(object parameter)
			{
                ExecuteAsyncImpl();
			}

			public async Task ExecuteAsync(object parameter)
			{
				if (!CheckCanExecute(parameter))
				{
					return;
				}
				await ExecuteAsyncImpl();
			}

			TaskCompletionSource<object> _completionSource;

			Task ExecuteAsyncImpl()
			{
				try
				{
					IsRunning = true;
					_owner.Cancel();
				}
				catch (Exception e)
				{
					Error = e;
					IsRunning = false;
				}
				return IsRunning ? _completionSource.Task : Task.FromResult<object>(null);
			}

			void Owner_IsRunningChanged(object sender, PropertyChangedEventArgs e)
			{
				if (e.PropertyName == "IsRunning")
				{
					if (!_owner.IsRunning && IsRunning)
					{
						IsRunning = false;
					}
				}
			}

			public event PropertyChangedEventHandler PropertyChanged;

			public bool IsRunning
			{
				get { return _completionSource != null; }
				set
				{
					if (value != IsRunning)
					{
						if (value)
						{
							_completionSource = new TaskCompletionSource<object>();
						}
						else {
							_completionSource.TrySetResult(null);
							_completionSource = null;
						}
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsRunning"));
                        RaiseCanExecuteChanged();
					}
				}
			}

		    private Exception _error;
			public Exception Error
			{
				get { return _error; }
				private set
				{
					if (value != _error)
					{
						_error = value;
						if (PropertyChanged != null)
						{
							PropertyChanged(this, new PropertyChangedEventArgs("Error"));
						}
					}
				}
			}

			public IAsyncCommand CancelCommand
			{
				get { return null; }
			}

			public bool CanCancel
			{
				get { return false; }
			}
		}
	}

	public sealed class AsyncCommand : AsyncCommandBase
	{
		CancellationTokenSource _delayToken = new CancellationTokenSource();

		public AsyncCommand()
		{ }

		public AsyncCommand(Func<Task> action) : this(action, null)
		{
		}

		public AsyncCommand(Func<Task> action, Func<bool> canExecute)
		{
			if (action != null)
				Action = _ => action();
			if (canExecute != null)
				CanExecute = _ => canExecute();
			Delay = TimeSpan.Zero;
		}

		protected override async Task DoExecuteAsync(object parameter)
		{
			if (Action != null)
			{
				try
				{
					if (_delayToken != null && !_delayToken.IsCancellationRequested)
						_delayToken.Cancel();
					if (Delay > TimeSpan.Zero)
					{
						_delayToken = new CancellationTokenSource();
						await Task.Delay(Delay, _delayToken.Token);
						//check if action still can be executed after delay
						if (!CheckCanExecute(parameter)) return;
					}

					var task = Action(parameter);
					if (task != null)
					{
						await task;
					}
				}
				catch (OperationCanceledException)
				{
				}
			}
		}

		protected override bool CheckCanExecute(object parameter)
		{
			return Action != null && (CanExecute == null || CanExecute(parameter));
		}

		public Func<object, Task> Action { get; set; }

		public Func<object, bool> CanExecute { get; set; }

		public TimeSpan Delay { get; set; }

		public async Task ExecuteWithoutDelay(object param)
		{
			var oldDelay = Delay;
			try
			{
				Delay = TimeSpan.Zero;
				await ExecuteAsync(param);
			}
			finally
			{
				Delay = oldDelay;
			}
		}
	}
}
