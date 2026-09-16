using System.Windows;
using System.Windows.Controls;
using PuntoDeVentaLibreria.Application.DTOs.Clientes;
using PuntoDeVentaLibreria.Application.Services;

namespace PuntoDeVentaLibreria.UI.Views.Clientes;

public partial class CobroDeudaModalWindow : Window
{
    private readonly ClienteDto _cliente;
    private readonly IClienteService _clienteService;
    private readonly ICajaService _cajaService;
    public bool CobradoExitoso { get; private set; }

    public CobroDeudaModalWindow(ClienteDto cliente, IClienteService clienteService, ICajaService cajaService)
    {
        InitializeComponent();
        _cliente = cliente ?? throw new ArgumentNullException(nameof(cliente));
        _clienteService = clienteService ?? throw new ArgumentNullException(nameof(clienteService));
        _cajaService = cajaService ?? throw new ArgumentNullException(nameof(cajaService));

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
            await _clienteService.CobrarSaldoCuentaCorrienteAsync(new RegistrarEntregaCuentaCorrienteDto
            {
                ClienteId = _cliente.Id,
                TurnoCajaId = turno.Id,
                MontoEntrega = monto,
                MetodoPago = metodo,
                Observaciones = TxtObservaciones.Text,
                UsuarioNombre = "Cajero"
            });

            CobradoExitoso = true;
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
