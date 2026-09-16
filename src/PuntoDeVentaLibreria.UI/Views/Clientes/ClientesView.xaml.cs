using System.Windows;
using System.Windows.Controls;
using PuntoDeVentaLibreria.Application.Services;
using PuntoDeVentaLibreria.UI.ViewModels;

namespace PuntoDeVentaLibreria.UI.Views.Clientes;

public partial class ClientesView : UserControl
{
    private readonly IClienteService _clienteService;
    private readonly ICajaService _cajaService;
    public ClientesViewModel ViewModel { get; }

    public ClientesView(ClientesViewModel viewModel, IClienteService clienteService, ICajaService cajaService)
    {
        InitializeComponent();
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _clienteService = clienteService ?? throw new ArgumentNullException(nameof(clienteService));
        _cajaService = cajaService ?? throw new ArgumentNullException(nameof(cajaService));
        DataContext = ViewModel;

        ViewModel.SolicitarEditorCliente = (c) =>
        {
            var modal = new ClienteModalWindow(c, _clienteService)
            {
                Owner = Window.GetWindow(this)
            };

            var res = modal.ShowDialog();
            return Task.FromResult(res == true && modal.GuardadoExitoso);
        };

        ViewModel.SolicitarCobroDeuda = (c) =>
        {
            var modal = new CobroDeudaModalWindow(c, _clienteService, _cajaService)
            {
                Owner = Window.GetWindow(this)
            };

            var res = modal.ShowDialog();
            return Task.FromResult(res == true && modal.CobradoExitoso);
        };

        Loaded += async (s, e) =>
        {
            await ViewModel.CargarDatosAsync();
        };
    }
}
