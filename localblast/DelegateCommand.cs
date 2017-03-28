using System;
using System.Windows.Input;

namespace LocalBlast
{
	public class DelegateCommand : ICommand
	{
		private readonly Action<object> execute;
		private readonly Func<object, bool> canExecute;

		public DelegateCommand(Action<object> execute)
			: this(execute, null)
		{
		}

		public DelegateCommand(Action<object> execute, Func<object, bool> canExecute)
		{
			this.execute = execute;
			this.canExecute = canExecute;
		}

		public void Execute(object parameter = null)
		{
			execute(parameter);
		}

		public bool CanExecute(object parameter = null)
		{
			return canExecute == null || canExecute(parameter);
		}

        public void TryExecute(object exeParameter = null, object canParameter = null)
        {
            if (CanExecute(canParameter))
                Execute(exeParameter);
        }

		public void OnCanExecuteChanged()
		{
			CanExecuteChanged?.Invoke(this, EventArgs.Empty);
		}

		public event EventHandler CanExecuteChanged;
	}
}