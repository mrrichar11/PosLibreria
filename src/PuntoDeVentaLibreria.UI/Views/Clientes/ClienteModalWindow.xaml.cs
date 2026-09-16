using System.Windows;
using PuntoDeVentaLibreria.Application.DTOs.Clientes;
using PuntoDeVentaLibreria.Application.Services;

namespace PuntoDeVentaLibreria.UI.Views.Clientes;

public partial class ClienteModalWindow : Window
{
    private readonly IClienteService _clienteService;
    public ClienteDto Cliente { get; }
    public bool GuardadoExitoso { get; private set; }

    public ClienteModalWindow(ClienteDto cliente, IClienteService clienteService)
    {
        InitializeComponent();
        Cliente = cliente ?? throw new ArgumentNullException(nameof(cliente));
        _clienteService = clienteService ?? throw new ArgumentNullException(nameof(clienteService));
        DataContext = Cliente;

        TxtNombre.Focus();
    }

    private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Cliente.NombreCompleto))
        {
            MessageBox.Show("El nombre del cliente es obligatorio.", "MR SYS", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtNombre.Focus();
            return;
        }

        try
        {
            await _clienteService.GuardarClienteAsync(Cliente);
            GuardadoExitoso = true;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al guardar cliente: {ex.Message}", "MR SYS Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
