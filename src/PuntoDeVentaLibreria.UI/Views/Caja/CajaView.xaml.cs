using System.Windows;
using System.Windows.Controls;
using PuntoDeVentaLibreria.Application.Services;
using PuntoDeVentaLibreria.UI.ViewModels;

namespace PuntoDeVentaLibreria.UI.Views.Caja;

public partial class CajaView : UserControl
{
    private readonly ICajaService _cajaService;
    private readonly IConfiguracionService _configuracionService;
    private readonly ITicketPrinterService _ticketPrinterService;
    public CajaViewModel ViewModel { get; }

    public CajaView(
        CajaViewModel viewModel,
        ICajaService cajaService,
        IConfiguracionService configuracionService,
        ITicketPrinterService ticketPrinterService)
    {
        InitializeComponent();
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _cajaService = cajaService ?? throw new ArgumentNullException(nameof(cajaService));
        _configuracionService = configuracionService ?? throw new ArgumentNullException(nameof(configuracionService));
        _ticketPrinterService = ticketPrinterService ?? throw new ArgumentNullException(nameof(ticketPrinterService));
        DataContext = ViewModel;

        ViewModel.SolicitarCierreCajaDialogo = (turno) =>
        {
            var modal = new CierreCajaModalWindow(turno, _cajaService, _configuracionService, _ticketPrinterService)
            {
                Owner = Window.GetWindow(this)
            };

            var res = modal.ShowDialog();
            return Task.FromResult(res == true && modal.CerradoExitoso);
        };

        ViewModel.SolicitarGastoRetiroDialogo = (tipo) =>
        {
            var modal = new GastoRetiroModalWindow(tipo, _cajaService)
            {
                Owner = Window.GetWindow(this)
            };

            var res = modal.ShowDialog();
            return Task.FromResult(res == true && modal.RegistradoExitoso);
        };

        Loaded += async (s, e) =>
        {
            await ViewModel.CargarDatosAsync();
        };
    }
}
