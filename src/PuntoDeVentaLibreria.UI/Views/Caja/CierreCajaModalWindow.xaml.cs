using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PuntoDeVentaLibreria.Application.DTOs.Peripherals;
using PuntoDeVentaLibreria.Application.Services;
using PuntoDeVentaLibreria.Domain.Entities.Finanzas;
using PuntoDeVentaLibreria.UI.Views.Tickets;

namespace PuntoDeVentaLibreria.UI.Views.Caja;

public partial class CierreCajaModalWindow : Window
{
    private readonly TurnoCaja _turno;
    private readonly ICajaService _cajaService;
    private readonly IConfiguracionService? _configuracionService;
    private readonly ITicketPrinterService? _ticketPrinterService;
    private decimal _efectivoSistema;
    private bool _conteoIniciadoPorBilletes = false;

    public bool CerradoExitoso { get; private set; }

    public CierreCajaModalWindow(
        TurnoCaja turno,
        ICajaService cajaService,
        IConfiguracionService? configuracionService = null,
        ITicketPrinterService? ticketPrinterService = null)
    {
        InitializeComponent();
        _turno = turno ?? throw new ArgumentNullException(nameof(turno));
        _cajaService = cajaService ?? throw new ArgumentNullException(nameof(cajaService));
        _configuracionService = configuracionService;
        _ticketPrinterService = ticketPrinterService;

        CalcularEfectivoSistema();
        CargarBotonesBilletes();
        TxtEfectivoReal.Focus();
        TxtEfectivoReal.SelectAll();
    }

    private void CalcularEfectivoSistema()
    {
        var ventasEf = _turno.Movimientos.Where(m => m.MetodoPago == "Efectivo" && m.Tipo == TipoMovimientoCaja.IngresoVenta).Sum(m => m.Monto);
        var cobrosCta = _turno.Movimientos.Where(m => m.MetodoPago == "Efectivo" && m.Tipo == TipoMovimientoCaja.CobroCuentaCorriente).Sum(m => m.Monto);
        var egresos = _turno.Movimientos.Where(m => m.MetodoPago == "Efectivo" && (m.Tipo == TipoMovimientoCaja.GastoOperativo || m.Tipo == TipoMovimientoCaja.RetiroDueño)).Sum(m => m.Monto);
        
        _efectivoSistema = _turno.MontoInicialEfectivo + ventasEf + cobrosCta - egresos;

        TxtEfectivoSistema.Text = $"${_efectivoSistema:N2}";
        TxtDetalleMovimientos.Text = $"Inicio: ${_turno.MontoInicialEfectivo:N2} | Entradas: ${ventasEf + cobrosCta:N2} | Salidas: ${egresos:N2}";
        TxtEfectivoReal.Text = _efectivoSistema.ToString("N2");
        ActualizarDiferencia();
    }

    private async void CargarBotonesBilletes()
    {
        string billetesRaw = "100,200,500,1000,2000,10000,20000";
        string simbolo = "$";

        if (_configuracionService != null)
        {
            try
            {
                var cfg = await _configuracionService.ObtenerConfiguracionAsync();
                billetesRaw = cfg.BilletesHabilitados;
                simbolo = cfg.SimboloMoneda;
            }
            catch { }
        }

        PanelBilletesConteo.Children.Clear();
        var lista = billetesRaw.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var bStr in lista)
        {
            if (decimal.TryParse(bStr.Trim(), out var valor))
            {
                var btn = new Button
                {
                    Content = $"+{simbolo}{valor:N0}",
                    Margin = new Thickness(0, 0, 6, 6),
                    Padding = new Thickness(10, 5, 10, 5),
                    FontWeight = FontWeights.Bold,
                    FontSize = 11,
                    Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                    BorderThickness = new Thickness(1),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Tag = valor
                };

                btn.Click += (s, e) =>
                {
                    decimal actual = 0;
                    if (_conteoIniciadoPorBilletes && decimal.TryParse(TxtEfectivoReal.Text, out var val))
                    {
                        actual = val;
                    }
                    _conteoIniciadoPorBilletes = true;
                    var nuevo = actual + valor;
                    TxtEfectivoReal.Text = nuevo.ToString("N2");
                };

                PanelBilletesConteo.Children.Add(btn);
            }
        }
    }

    private void BtnBorrarConteo_Click(object sender, RoutedEventArgs e)
    {
        _conteoIniciadoPorBilletes = true;
        TxtEfectivoReal.Text = "0.00";
        TxtEfectivoReal.Focus();
    }

    private void TxtEfectivoReal_TextChanged(object sender, TextChangedEventArgs e)
    {
        ActualizarDiferencia();
    }

    private void ActualizarDiferencia()
    {
        if (decimal.TryParse(TxtEfectivoReal.Text, out var real))
        {
            var dif = real - _efectivoSistema;
            if (dif == 0)
            {
                LblTipoDiferencia.Text = "Caja Balanceada (Exacta):";
                TxtDiferenciaMonto.Text = "$0.00";
                TxtDiferenciaMonto.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                BrdDiferencia.Background = new SolidColorBrush(Color.FromRgb(240, 253, 244));
                BrdDiferencia.BorderBrush = new SolidColorBrush(Color.FromRgb(187, 247, 208));
            }
            else if (dif > 0)
            {
                LblTipoDiferencia.Text = "Sobrante de Efectivo:";
                TxtDiferenciaMonto.Text = $"+${dif:N2}";
                TxtDiferenciaMonto.Foreground = new SolidColorBrush(Color.FromRgb(37, 99, 235));
                BrdDiferencia.Background = new SolidColorBrush(Color.FromRgb(239, 246, 255));
                BrdDiferencia.BorderBrush = new SolidColorBrush(Color.FromRgb(191, 219, 254));
            }
            else
            {
                LblTipoDiferencia.Text = "Faltante de Efectivo:";
                TxtDiferenciaMonto.Text = $"-${Math.Abs(dif):N2}";
                TxtDiferenciaMonto.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38));
                BrdDiferencia.Background = new SolidColorBrush(Color.FromRgb(254, 242, 242));
                BrdDiferencia.BorderBrush = new SolidColorBrush(Color.FromRgb(254, 202, 202));
            }
        }
    }

    private async void BtnConfirmar_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(TxtEfectivoReal.Text, out var real))
        {
            MessageBox.Show("Por favor ingrese un monto de efectivo válido.", "MR SYS", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            await _cajaService.CerrarTurnoAsync(real, "Cajero", TxtObservaciones.Text);
            CerradoExitoso = true;

            // Imprimir o previsualizar ticket de cierre si el checkbox está activo
            if (ChkImprimirTicketCierre.IsChecked == true && _ticketPrinterService != null)
            {
                try
                {
                    var resumen = await _cajaService.ObtenerResumenTurnoAsync(_turno.Id);
                    ConfiguracionTicketDto configTicket = new();
                    if (_configuracionService != null)
                    {
                        var cfg = await _configuracionService.ObtenerConfiguracionAsync();
                        configTicket = new ConfiguracionTicketDto
                        {
                            NombreComercio = cfg.NombreComercio,
                            Direccion = cfg.Direccion,
                            Telefono = cfg.Telefono,
                            Cuit = cfg.Cuit,
                            AnchoPapelMm = cfg.AnchoPapelMm,
                            ImpresoraNombre = cfg.ImpresoraTickets,
                            MensajePie = cfg.MensajePieTicket
                        };
                    }

                    var textoTicket = await _ticketPrinterService.GenerarTicketCierreCajaAsync(resumen, configTicket);
                    var preview = new TicketPreviewWindow(textoTicket, $"Cierre_Turno_{_turno.Id.ToString().Substring(0, 8)}", configTicket.AnchoPapelMm, configTicket.ImpresoraNombre)
                    {
                        Owner = this.Owner
                    };
                    preview.ShowDialog();
                }
                catch { }
            }

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cerrar turno: {ex.Message}", "MR SYS Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
