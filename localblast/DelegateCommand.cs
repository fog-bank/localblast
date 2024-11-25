using System;
using System.Windows.Input;

namespace LocalBlast;

public class DelegateCommand(Action<object?>? execute, Func<object?, bool>? canExecute = null) : ICommand
{
    public void Execute(object? parameter = null) => execute?.Invoke(parameter);

    public bool CanExecute(object? parameter = null) => canExecute == null || canExecute(parameter);

    public void TryExecute(object? exeParameter = null, object? canParameter = null)
    {
        if (CanExecute(canParameter))
            Execute(exeParameter);
    }

    public void OnCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    public event EventHandler? CanExecuteChanged;
}
