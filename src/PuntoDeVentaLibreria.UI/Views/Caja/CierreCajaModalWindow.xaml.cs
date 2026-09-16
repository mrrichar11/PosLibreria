using System.Windows;
using System.Windows.Media;
using PuntoDeVentaLibreria.Application.Services;
using PuntoDeVentaLibreria.Domain.Entities.Finanzas;

namespace PuntoDeVentaLibreria.UI.Views.Caja;

public partial class CierreCajaModalWindow : Window
{
    private readonly TurnoCaja _turno;
    private readonly ICajaService _cajaService;
    private decimal _efectivoSistema;
    public bool CerradoExitoso { get; private set; }

    public CierreCajaModalWindow(TurnoCaja turno, ICajaService cajaService)
    {
        InitializeComponent();
        _turno = turno ?? throw new ArgumentNullException(nameof(turno));
        _cajaService = cajaService ?? throw new ArgumentNullException(nameof(cajaService));

        CalcularEfectivoSistema();
        TxtEfectivoReal.Focus();
    }

    private void CalcularEfectivoSistema()
    {
        var ingresos = _turno.Movimientos.Where(m => m.MetodoPago == "Efectivo" && m.Tipo == TipoMovimientoCaja.IngresoVenta).Sum(m => m.Monto);
        var egresos = _turno.Movimientos.Where(m => m.MetodoPago == "Efectivo" && (m.Tipo == TipoMovimientoCaja.GastoOperativo || m.Tipo == TipoMovimientoCaja.RetiroDueño)).Sum(m => m.Monto);
        _efectivoSistema = _turno.MontoInicialEfectivo + ingresos - egresos;

        TxtEfectivoSistema.Text = $"${_efectivoSistema:N2}";
        TxtEfectivoReal.Text = _efectivoSistema.ToString("N2");
        ActualizarDiferencia();
    }

    private void TxtEfectivoReal_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
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
                LblTipoDiferencia.Text = "Caja Perfecta (Sin diferencia):";
                TxtDiferenciaMonto.Text = "$0.00";
                TxtDiferenciaMonto.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
            }
            else if (dif > 0)
            {
                LblTipoDiferencia.Text = "Sobrante de Efectivo:";
                TxtDiferenciaMonto.Text = $"+${dif:N2}";
                TxtDiferenciaMonto.Foreground = new SolidColorBrush(Color.FromRgb(37, 99, 235));
            }
            else
            {
                LblTipoDiferencia.Text = "Faltante de Efectivo:";
                TxtDiferenciaMonto.Text = $"-${Math.Abs(dif):N2}";
                TxtDiferenciaMonto.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38));
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
