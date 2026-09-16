using System.Windows;
using PuntoDeVentaLibreria.Application.Services;

namespace PuntoDeVentaLibreria.UI.Views.Caja;

public partial class GastoRetiroModalWindow : Window
{
    private readonly string _tipoMovimiento;
    private readonly ICajaService _cajaService;
    public bool RegistradoExitoso { get; private set; }

    public GastoRetiroModalWindow(string tipoMovimiento, ICajaService cajaService)
    {
        InitializeComponent();
        _tipoMovimiento = tipoMovimiento;
        _cajaService = cajaService ?? throw new ArgumentNullException(nameof(cajaService));

        if (_tipoMovimiento == "RetiroDueño")
        {
            TxtTitulo.Text = "RETIRO DE DUEÑO / SOCIO";
            BtnConfirmar.Content = "Confirmar Retiro Dueño";
            TxtConcepto.Text = "Retiro de fondos por titular";
        }
        else
        {
            TxtTitulo.Text = "REGISTRAR GASTO OPERATIVO";
            BtnConfirmar.Content = "Registrar Gasto";
            TxtConcepto.Text = "Compra de insumos / limpieza";
        }

        TxtMonto.Focus();
    }

    private async void BtnConfirmar_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(TxtMonto.Text, out var monto) || monto <= 0)
        {
            MessageBox.Show("Ingrese un monto numérico positivo.", "MR SYS", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtMonto.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(TxtConcepto.Text))
        {
            MessageBox.Show("Debe especificar el concepto del movimiento.", "MR SYS", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtConcepto.Focus();
            return;
        }

        try
        {
            if (_tipoMovimiento == "RetiroDueño")
            {
                await _cajaService.RegistrarRetiroDueñoAsync(monto, TxtConcepto.Text, "Cajero");
            }
            else
            {
                await _cajaService.RegistrarGastoOperativoAsync(monto, TxtConcepto.Text, "Cajero");
            }

            RegistradoExitoso = true;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al registrar movimiento: {ex.Message}", "MR SYS Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
