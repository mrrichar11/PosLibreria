using System.Windows;
using System.Windows.Input;
using PuntoDeVentaLibreria.UI.ViewModels;
using PuntoDeVentaLibreria.UI.Views;

namespace PuntoDeVentaLibreria.UI;

public partial class MainWindow : Window
{
    public PosViewModel ViewModel { get; }

    public MainWindow(PosViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = ViewModel;

        // Conexión del diálogo modal de cobro
        ViewModel.SolicitarCobroDialogo = (cobroVm) =>
        {
            var modal = new CobroModalWindow(cobroVm)
            {
                Owner = this
            };

            var dialogResult = modal.ShowDialog();
            TxtCodigoBarras.Focus();
            return Task.FromResult(dialogResult == true);
        };

        Loaded += async (s, e) =>
        {
            await ViewModel.InicializarAsync();
            TxtCodigoBarras.Focus();
        };

        // Manejo de Enter en la barra de escaneo
        TxtCodigoBarras.KeyDown += async (s, e) =>
        {
            if (e.Key == Key.Enter)
            {
                if (ViewModel.EscanearCodigoCommand.CanExecute(null))
                {
                    await ViewModel.EscanearCodigoCommand.ExecuteAsync(null);
                    TxtCodigoBarras.Focus();
                    e.Handled = true;
                }
            }
        };

        // Atajos de teclado globales en el mostrador
        KeyDown += async (s, e) =>
        {
            if (e.Key == Key.F2)
            {
                if (ViewModel.AbrirCobroCommand.CanExecute(null))
                {
                    await ViewModel.AbrirCobroCommand.ExecuteAsync(null);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.F4)
            {
                if (ViewModel.AgregarVentaManualCommand.CanExecute(null))
                {
                    ViewModel.AgregarVentaManualCommand.Execute(null);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.F5)
            {
                if (ViewModel.LimpiarTicketCommand.CanExecute(null))
                {
                    ViewModel.LimpiarTicketCommand.Execute(null);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.F12 || e.Key == Key.Escape)
            {
                TxtCodigoBarras.Focus();
                TxtCodigoBarras.SelectAll();
                e.Handled = true;
            }
        };
    }
}