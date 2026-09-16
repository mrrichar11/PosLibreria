using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PuntoDeVentaLibreria.UI.ViewModels;
using PuntoDeVentaLibreria.UI.Views;

namespace PuntoDeVentaLibreria.UI.Views.Pos;

public partial class PosView : UserControl
{
    public PosViewModel ViewModel { get; }

    public event Action? OnCajaModificada;

    public PosView(PosViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = ViewModel;

        ViewModel.SolicitarCobroDialogo = (cobroVm) =>
        {
            var modal = new CobroModalWindow(cobroVm)
            {
                Owner = Window.GetWindow(this)
            };

            var dialogResult = modal.ShowDialog();
            TxtCodigoBarras.Focus();
            OnCajaModificada?.Invoke();
            return Task.FromResult(dialogResult == true);
        };

        ViewModel.SolicitarSeleccionArticulo = (opciones, query) =>
        {
            var modal = new SeleccionarArticuloModalWindow(opciones, query)
            {
                Owner = Window.GetWindow(this)
            };

            var dialogResult = modal.ShowDialog();
            TxtCodigoBarras.Focus();
            return Task.FromResult(dialogResult == true ? modal.ArticuloSeleccionado : null);
        };

        ViewModel.SolicitarVentaManualDialogo = () =>
        {
            var modal = new VentaManualModalWindow
            {
                Owner = Window.GetWindow(this)
            };

            var dialogResult = modal.ShowDialog();
            TxtCodigoBarras.Focus();
            if (dialogResult == true && modal.Confirmado)
            {
                return Task.FromResult<(string descripcion, decimal precio, decimal cantidad)?>((modal.Descripcion, modal.PrecioUnitario, modal.Cantidad));
            }
            return Task.FromResult<(string descripcion, decimal precio, decimal cantidad)?>(null);
        };

        Loaded += async (s, e) =>
        {
            await ViewModel.InicializarAsync();
            TxtCodigoBarras.Focus();
        };

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
                    await ViewModel.AgregarVentaManualCommand.ExecuteAsync(null);
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

    public async Task VerificarCajaAsync()
    {
        await ViewModel.RecargarCajaAsync();
    }
}
