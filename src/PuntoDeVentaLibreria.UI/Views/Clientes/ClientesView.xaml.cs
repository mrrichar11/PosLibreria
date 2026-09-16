using System.Windows;
using System.Windows.Controls;
using PuntoDeVentaLibreria.Application.Services;
using PuntoDeVentaLibreria.UI.ViewModels;

namespace PuntoDeVentaLibreria.UI.Views.Clientes;

public partial class ClientesView : UserControl
{
    private readonly IClienteService _clienteService;
    private readonly ICajaService _cajaService;
    private readonly IConfiguracionService _configuracionService;
    private readonly ITicketPrinterService _ticketPrinterService;
    public ClientesViewModel ViewModel { get; }

    public ClientesView(
        ClientesViewModel viewModel, 
        IClienteService clienteService, 
        ICajaService cajaService,
        IConfiguracionService configuracionService,
        ITicketPrinterService ticketPrinterService)
    {
        InitializeComponent();
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _clienteService = clienteService ?? throw new ArgumentNullException(nameof(clienteService));
        _cajaService = cajaService ?? throw new ArgumentNullException(nameof(cajaService));
        _configuracionService = configuracionService ?? throw new ArgumentNullException(nameof(configuracionService));
        _ticketPrinterService = ticketPrinterService ?? throw new ArgumentNullException(nameof(ticketPrinterService));
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
            var modal = new CobroDeudaModalWindow(c, _clienteService, _cajaService, _configuracionService, _ticketPrinterService)
            {
                Owner = Window.GetWindow(this)
            };

            var res = modal.ShowDialog();
            return Task.FromResult(res == true && modal.CobradoExitoso);
        };

        ViewModel.SolicitarHistorialCliente = (c) =>
        {
            var modal = new HistorialClienteModalWindow(c, _clienteService)
            {
                Owner = Window.GetWindow(this)
            };

            modal.ShowDialog();
            return Task.CompletedTask;
        };

        Loaded += async (s, e) =>
        {
            await ViewModel.CargarDatosAsync();
        };
    }
}
