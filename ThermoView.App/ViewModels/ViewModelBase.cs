using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ThermoView.App.ViewModels;

// The "doorbell" every ViewModel shares.
// When a property changes, we ring PropertyChanged, and WPF
// automatically refreshes any part of the screen bound to it.
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    // [CallerMemberName] fills in the property's name for us automatically
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    // Set a field, and ring the doorbell only if the value actually changed.
    // Returns true if it changed, so the caller can react.
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }
}
