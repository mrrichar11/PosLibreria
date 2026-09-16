using System.Windows;
using PuntoDeVentaLibreria.Application.DTOs.Clientes;
using PuntoDeVentaLibreria.Application.Services;

namespace PuntoDeVentaLibreria.UI.Views.Clientes;

public partial class HistorialClienteModalWindow : Window
{
    private readonly ClienteDto _cliente;
    private readonly IClienteService _clienteService;

    public HistorialClienteModalWindow(ClienteDto cliente, IClienteService clienteService)
    {
        InitializeComponent();
        _cliente = cliente ?? throw new ArgumentNullException(nameof(cliente));
        _clienteService = clienteService ?? throw new ArgumentNullException(nameof(clienteService));

        TxtTitulo.Text = $"HISTORIAL: {_cliente.NombreCompleto.ToUpperInvariant()}";
        TxtSubtitulo.Text = $"DNI/CUIT: {_cliente.DniOCuit ?? "-"} | Tel: {_cliente.Telefono ?? "-"} | Inst: {_cliente.ColegioOInstitucion ?? "-"}";
        TxtLimite.Text = $"${_cliente.LimiteCredito:N2}";
        TxtSaldoDeudor.Text = $"${_cliente.SaldoDeudorActual:N2}";

        Loaded += async (s, e) => await CargarHistorialAsync();
    }

    private async Task CargarHistorialAsync()
    {
        try
        {
            var movimientos = await _clienteService.ObtenerHistorialClienteAsync(_cliente.Id);
            GridMovimientos.ItemsSource = movimientos;
            TxtCantidadMovimientos.Text = $"{movimientos.Count} movimiento(s) registrado(s) en cuenta";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar historial: {ex.Message}", "MR SYS Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnCerrar_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
