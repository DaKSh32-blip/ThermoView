using System.Windows.Input;

namespace ThermoView.App.ViewModels;

// A button in XAML can't call a method directly in MVVM.
// Instead it's bound to an ICommand. RelayCommand is a tiny wrapper
// that turns any method into a command a button can trigger.
public class RelayCommand : ICommand
{
    private readonly Action _execute;

    public RelayCommand(Action execute) => _execute = execute;

    // Our buttons are always clickable, so these are simple
    public event EventHandler? CanExecuteChanged { add { } remove { } }
    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => _execute();
}
