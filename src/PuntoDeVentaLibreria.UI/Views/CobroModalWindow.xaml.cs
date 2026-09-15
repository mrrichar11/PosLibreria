using System.Windows;
using System.Windows.Input;
using PuntoDeVentaLibreria.UI.ViewModels;

namespace PuntoDeVentaLibreria.UI.Views;

public partial class CobroModalWindow : Window
{
    public CobroModalViewModel ViewModel { get; }

    public CobroModalWindow(CobroModalViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = ViewModel;

        ViewModel.OnCerrar += () =>
        {
            DialogResult = ViewModel.VentaConfirmada;
            Close();
        };

        Loaded += (s, e) =>
        {
            TxtMontoEntregado.Focus();
            TxtMontoEntregado.SelectAll();
        };

        KeyDown += (s, e) =>
        {
            if (e.Key == Key.Escape)
            {
                ViewModel.CancelarCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key == Key.Enter || e.Key == Key.F2)
            {
                if (ViewModel.ConfirmarCommand.CanExecute(null))
                {
                    ViewModel.ConfirmarCommand.Execute(null);
                    e.Handled = true;
                }
            }
        };
    }
}
