using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PuntoDeVentaLibreria.Application.DTOs.Inventario;
using PuntoDeVentaLibreria.Application.Services;

namespace PuntoDeVentaLibreria.UI.ViewModels;

public partial class InventarioViewModel : ObservableObject
{
    private readonly IInventarioService _inventarioService;

    [ObservableProperty]
    private string _criterioBusqueda = string.Empty;

    [ObservableProperty]
    private ArticuloDto? _articuloSeleccionado;

    [ObservableProperty]
    private bool _estaCargando;

    [ObservableProperty]
    private int _totalArticulos;

    [ObservableProperty]
    private decimal _valorTotalStock;

    public ObservableCollection<ArticuloDto> Articulos { get; } = new();

    public Func<ArticuloDto, Task<bool>>? SolicitarEditorArticulo { get; set; }

    public InventarioViewModel(IInventarioService inventarioService)
    {
        _inventarioService = inventarioService ?? throw new ArgumentNullException(nameof(inventarioService));
    }

    public async Task CargarDatosAsync()
    {
        EstaCargando = true;
        try
        {
            var lista = await _inventarioService.BuscarArticulosAsync(CriterioBusqueda);
            Articulos.Clear();
            decimal totalValor = 0;
            foreach (var item in lista)
            {
                Articulos.Add(item);
                totalValor += item.PrecioVenta * item.StockActual;
            }
            TotalArticulos = Articulos.Count;
            ValorTotalStock = totalValor;
        }
        finally
        {
            EstaCargando = false;
        }
    }

    [RelayCommand]
    private async Task BuscarAsync()
    {
        await CargarDatosAsync();
    }

    [RelayCommand]
    private async Task NuevoArticuloAsync()
    {
        var nuevo = new ArticuloDto
        {
            Nombre = string.Empty,
            SKU = $"LIB-{DateTime.Now:fffss}",
            PrecioCosto = 1000m,
            PorcentajeGanancia = 65m,
            PrecioVenta = 1650m,
            StockActual = 10,
            StockMinimo = 5,
            Ubicacion = "Estante A"
        };

        if (SolicitarEditorArticulo != null)
        {
            var guardado = await SolicitarEditorArticulo(nuevo);
            if (guardado)
            {
                await CargarDatosAsync();
            }
        }
    }

    [RelayCommand]
    private async Task EditarArticuloAsync(ArticuloDto? item)
    {
        var art = item ?? ArticuloSeleccionado;
        if (art == null) return;

        if (SolicitarEditorArticulo != null)
        {
            var guardado = await SolicitarEditorArticulo(art);
            if (guardado)
            {
                await CargarDatosAsync();
            }
        }
    }
}
