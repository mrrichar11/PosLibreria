using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PuntoDeVentaLibreria.UI.ViewModels;
using PuntoDeVentaLibreria.UI.Views;
using PuntoDeVentaLibreria.UI.Views.Tickets;

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

        ViewModel.SolicitarSeleccionVariante = (articulo) =>
        {
            var modal = new SeleccionarVarianteModalWindow(articulo)
            {
                Owner = Window.GetWindow(this)
            };

            var dialogResult = modal.ShowDialog();
            TxtCodigoBarras.Focus();
            return Task.FromResult(dialogResult == true ? modal.VarianteSeleccionada : null);
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

        ViewModel.SolicitarConfirmacionDialogo = (mensaje, titulo) =>
        {
            var ventana = Window.GetWindow(this);
            var res = MessageBox.Show(ventana, mensaje, titulo, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return Task.FromResult(res == MessageBoxResult.Yes);
        };

        ViewModel.SolicitarVistaPreviaTicket = (texto, comprobante, ancho, impresora) =>
        {
            var modal = new TicketPreviewWindow(texto, comprobante, ancho, impresora)
            {
                Owner = Window.GetWindow(this)
            };
            modal.ShowDialog();
            TxtCodigoBarras.Focus();
            return Task.CompletedTask;
        };

        ViewModel.SolicitarAltaRapidaArticulo = (codigoInicial) =>
        {
            var modal = new ArticuloRapidoModalWindow(ViewModel.InventarioService, codigoInicial)
            {
                Owner = Window.GetWindow(this)
            };

            var dialogResult = modal.ShowDialog();
            TxtCodigoBarras.Focus();
            return Task.FromResult(dialogResult == true ? modal.ArticuloCreado : null);
        };

        ViewModel.SolicitarConfirmacionAltaRapida = (codigo) =>
        {
            var ventana = Window.GetWindow(this);
            var res = MessageBox.Show(ventana,
                $"El producto con código '{codigo}' no está registrado en el sistema.\n\n¿Desea darlo de alta rápidamente ahora para agregarlo al ticket?",
                "Producto No Registrado",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            return Task.FromResult(res == MessageBoxResult.Yes);
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

        PreviewKeyDown += async (s, e) =>
        {
            if (e.Key == Key.F1)
            {
                TxtCodigoBarras.Focus();
                TxtCodigoBarras.SelectAll();
                e.Handled = true;
            }
            else if (e.Key == Key.F2)
            {
                if (ViewModel.AbrirCobroCommand.CanExecute(null))
                {
                    await ViewModel.AbrirCobroCommand.ExecuteAsync(null);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.F3)
            {
                if (ViewModel.AbrirAltaRapidaCommand.CanExecute(null))
                {
                    await ViewModel.AbrirAltaRapidaCommand.ExecuteAsync(null);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.F4)
            {
                if (ViewModel.PausarVentaActualCommand.CanExecute(null))
                {
                    ViewModel.PausarVentaActualCommand.Execute(null);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.F6)
            {
                if (ViewModel.AgregarVentaManualCommand.CanExecute(null))
                {
                    await ViewModel.AgregarVentaManualCommand.ExecuteAsync(null);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.F9)
            {
                if (ViewModel.ReimprimirUltimoTicketCommand.CanExecute(null))
                {
                    await ViewModel.ReimprimirUltimoTicketCommand.ExecuteAsync(null);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.F12)
            {
                if (ViewModel.LimpiarTicketConConfirmacionCommand.CanExecute(null))
                {
                    await ViewModel.LimpiarTicketConConfirmacionCommand.ExecuteAsync(null);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Delete)
            {
                if (GridCarrito.IsFocused || GridCarrito.IsKeyboardFocusWithin)
                {
                    if (GridCarrito.SelectedItem is PosItemModel item)
                    {
                        ViewModel.EliminarItemCommand.Execute(item);
                        e.Handled = true;
                    }
                }
            }
            else if (e.Key == Key.Escape)
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
