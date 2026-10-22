using System.Windows;
using System.Windows.Input;
using ThermoView.App.ViewModels;

namespace ThermoView.App;

// In MVVM the window's code-behind stays tiny.
// The only extra job here: turn a mouse click into "which square was clicked",
// then hand that to the ViewModel. All the real logic lives there.
public partial class MainWindow : Window
{
    private readonly MainViewModel _vm = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;
    }

    // The board is measured in grid squares, so the mouse position
    // relative to it IS the square number (just round down).
    private void Board_LeftClick(object sender, MouseButtonEventArgs e)
    {
        var p = e.GetPosition((IInputElement)sender);
        _vm.PlaceChipAt((int)p.X, (int)p.Y);
    }

    private void Board_RightClick(object sender, MouseButtonEventArgs e)
    {
        var p = e.GetPosition((IInputElement)sender);
        _vm.RemoveChipAt((int)p.X, (int)p.Y);
    }
}
