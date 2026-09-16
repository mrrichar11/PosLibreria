using System.Windows;
using System.Windows.Controls;
using PuntoDeVentaLibreria.Application.DTOs.Clientes;
using PuntoDeVentaLibreria.Application.DTOs.Peripherals;
using PuntoDeVentaLibreria.Application.Services;
using PuntoDeVentaLibreria.UI.Views.Tickets;

namespace PuntoDeVentaLibreria.UI.Views.Clientes;

public partial class CobroDeudaModalWindow : Window
{
    private readonly ClienteDto _cliente;
    private readonly IClienteService _clienteService;
    private readonly ICajaService _cajaService;
    private readonly IConfiguracionService _configuracionService;
    private readonly ITicketPrinterService _ticketPrinterService;
    public bool CobradoExitoso { get; private set; }

    public CobroDeudaModalWindow(
        ClienteDto cliente, 
        IClienteService clienteService, 
        ICajaService cajaService,
        IConfiguracionService configuracionService,
        ITicketPrinterService ticketPrinterService)
    {
        InitializeComponent();
        _cliente = cliente ?? throw new ArgumentNullException(nameof(cliente));
        _clienteService = clienteService ?? throw new ArgumentNullException(nameof(clienteService));
        _cajaService = cajaService ?? throw new ArgumentNullException(nameof(cajaService));
        _configuracionService = configuracionService ?? throw new ArgumentNullException(nameof(configuracionService));
        _ticketPrinterService = ticketPrinterService ?? throw new ArgumentNullException(nameof(ticketPrinterService));

        TxtClienteInfo.Text = $"Cliente: {_cliente.NombreCompleto} (DNI: {_cliente.DniOCuit ?? "-"})";
        TxtDeudaTotal.Text = $"${_cliente.SaldoDeudorActual:N2}";
        TxtMontoEntrega.Text = _cliente.SaldoDeudorActual.ToString("N2");

        TxtMontoEntrega.Focus();
        TxtMontoEntrega.SelectAll();
    }

    private async void BtnConfirmar_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(TxtMontoEntrega.Text, out var monto) || monto <= 0)
        {
            MessageBox.Show("Ingrese un monto numérico positivo.", "MR SYS", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtMontoEntrega.Focus();
            return;
        }

        var turno = await _cajaService.ObtenerTurnoActivoAsync();
        if (turno == null)
        {
            MessageBox.Show("Debe existir una caja abierta para registrar el ingreso del cobro.", "MR SYS", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var metodo = (CmbMetodo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Efectivo";

        try
        {
            var recibo = await _clienteService.CobrarSaldoCuentaCorrienteAsync(new RegistrarEntregaCuentaCorrienteDto
            {
                ClienteId = _cliente.Id,
                TurnoCajaId = turno.Id,
                MontoEntrega = monto,
                MetodoPago = metodo,
                Observaciones = TxtObservaciones.Text,
                UsuarioNombre = "Cajero"
            });

            CobradoExitoso = true;

            if (ChkImprimirRecibo.IsChecked == true)
            {
                try
                {
                    var cfg = await _configuracionService.ObtenerConfiguracionAsync();
                    var configTicket = new ConfiguracionTicketDto
                    {
                        NombreComercio = cfg.NombreComercio,
                        Direccion = cfg.Direccion,
                        Telefono = cfg.Telefono,
                        Cuit = cfg.Cuit,
                        AnchoPapelMm = cfg.AnchoPapelMm,
                        ImpresoraNombre = cfg.ImpresoraTickets,
                        MensajePie = cfg.MensajePieTicket
                    };

                    var textoTicket = await _ticketPrinterService.GenerarTicketReciboCtaCteAsync(recibo, configTicket);
                    var preview = new TicketPreviewWindow(textoTicket, recibo.NumeroRecibo, configTicket.AnchoPapelMm, configTicket.ImpresoraNombre)
                    {
                        Owner = this
                    };
                    preview.ShowDialog();
                }
                catch (Exception exTicket)
                {
                    MessageBox.Show($"El cobro fue asentado correctamente, pero no se pudo generar la vista previa del recibo: {exTicket.Message}", 
                        "Aviso Impresión", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al procesar cobro: {ex.Message}", "MR SYS Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
