using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PuntoDeVentaLibreria.Application.DTOs.Clientes;
using PuntoDeVentaLibreria.Application.Services;

namespace PuntoDeVentaLibreria.UI.ViewModels;

public partial class ClientesViewModel : ObservableObject
{
    private readonly IClienteService _clienteService;

    [ObservableProperty]
    private string _criterioBusqueda = string.Empty;

    [ObservableProperty]
    private ClienteDto? _clienteSeleccionado;

    [ObservableProperty]
    private int _totalClientes;

    [ObservableProperty]
    private decimal _totalDeudaFiados;

    public ObservableCollection<ClienteDto> Clientes { get; } = new();

    public Func<ClienteDto, Task<bool>>? SolicitarEditorCliente { get; set; }
    public Func<ClienteDto, Task<bool>>? SolicitarCobroDeuda { get; set; }

    public ClientesViewModel(IClienteService clienteService)
    {
        _clienteService = clienteService ?? throw new ArgumentNullException(nameof(clienteService));
    }

    public async Task CargarDatosAsync()
    {
        var lista = await _clienteService.BuscarClientesAsync(CriterioBusqueda);
        Clientes.Clear();
        decimal totalDeuda = 0;
        foreach (var c in lista)
        {
            Clientes.Add(c);
            totalDeuda += c.SaldoDeudorActual;
        }

        TotalClientes = Clientes.Count;
        TotalDeudaFiados = totalDeuda;
    }

    [RelayCommand]
    private async Task BuscarAsync()
    {
        await CargarDatosAsync();
    }

    [RelayCommand]
    private async Task NuevoClienteAsync()
    {
        var nuevo = new ClienteDto
        {
            NombreCompleto = string.Empty,
            LimiteCredito = 50000m,
            PermiteFiado = true
        };

        if (SolicitarEditorCliente != null)
        {
            var guardado = await SolicitarEditorCliente(nuevo);
            if (guardado)
            {
                await CargarDatosAsync();
            }
        }
    }

    [RelayCommand]
    private async Task EditarClienteAsync(ClienteDto? cliente)
    {
        var c = cliente ?? ClienteSeleccionado;
        if (c == null) return;

        if (SolicitarEditorCliente != null)
        {
            var guardado = await SolicitarEditorCliente(c);
            if (guardado)
            {
                await CargarDatosAsync();
            }
        }
    }

    [RelayCommand]
    private async Task CobrarDeudaAsync(ClienteDto? cliente)
    {
        var c = cliente ?? ClienteSeleccionado;
        if (c == null || c.SaldoDeudorActual <= 0) return;

        if (SolicitarCobroDeuda != null)
        {
            var cobrado = await SolicitarCobroDeuda(c);
            if (cobrado)
            {
                await CargarDatosAsync();
            }
        }
    }
}
