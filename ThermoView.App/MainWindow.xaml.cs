using System.Windows;
using ThermoView.App.ViewModels;

namespace ThermoView.App;

// In MVVM the window's code-behind does almost nothing.
// It just hands the window its ViewModel. Everything else
// is connected through {Binding ...} in the XAML.
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
