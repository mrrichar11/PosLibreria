using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PuntoDeVentaLibreria.Application.DTOs.Inventario;
using PuntoDeVentaLibreria.Application.Services;

namespace PuntoDeVentaLibreria.UI.ViewModels;

public partial class InventarioViewModel : ObservableObject
{
    private readonly IInventarioService _inventarioService;
    private readonly IConfiguracionService _configuracionService;

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

    [ObservableProperty]
    private int _paginaActual = 1;

    [ObservableProperty]
    private int _totalPaginas = 1;

    [ObservableProperty]
    private int _cantidadPorPagina = 50;

    [ObservableProperty]
    private string _rubroSeleccionado = "Todos";

    [ObservableProperty]
    private string _textoPaginacion = "Cargando artículos...";

    [ObservableProperty]
    private bool _puedeIrAnterior;

    [ObservableProperty]
    private bool _puedeIrSiguiente;

    [ObservableProperty]
    private int _totalRegistrosFiltrados;

    public ObservableCollection<ArticuloDto> Articulos { get; } = new();

    public Func<ArticuloDto, Task<bool>>? SolicitarEditorArticulo { get; set; }

    public InventarioViewModel(IInventarioService inventarioService, IConfiguracionService configuracionService)
    {
        _inventarioService = inventarioService ?? throw new ArgumentNullException(nameof(inventarioService));
        _configuracionService = configuracionService ?? throw new ArgumentNullException(nameof(configuracionService));
    }

    public async Task CargarDatosAsync()
    {
        EstaCargando = true;
        try
        {
            var consulta = new ConsultaInventarioPaginadaDto
            {
                CriterioBusqueda = CriterioBusqueda,
                Rubro = RubroSeleccionado,
                Pagina = PaginaActual,
                CantidadPorPagina = CantidadPorPagina
            };

            var resultado = await _inventarioService.ObtenerArticulosPaginadosAsync(consulta);

            Articulos.Clear();
            foreach (var item in resultado.Items)
            {
                Articulos.Add(item);
            }

            PaginaActual = resultado.PaginaActual;
            TotalPaginas = resultado.TotalPaginas;
            TotalArticulos = resultado.TotalArticulosGlobal;
            TotalRegistrosFiltrados = resultado.TotalRegistros;
            ValorTotalStock = resultado.ValorTotalStockGlobal;

            PuedeIrAnterior = PaginaActual > 1;
            PuedeIrSiguiente = PaginaActual < TotalPaginas;

            if (resultado.TotalRegistros == 0)
            {
                TextoPaginacion = "No se encontraron artículos.";
            }
            else if (CantidadPorPagina <= 0)
            {
                TextoPaginacion = $"Mostrando todos los {resultado.TotalRegistros:N0} artículos filtrados";
            }
            else
            {
                TextoPaginacion = $"Mostrando {resultado.RegistroDesde:N0} - {resultado.RegistroHasta:N0} de {resultado.TotalRegistros:N0} artículos";
            }
        }
        finally
        {
            EstaCargando = false;
        }
    }

    [RelayCommand]
    private async Task BuscarAsync()
    {
        PaginaActual = 1;
        await CargarDatosAsync();
    }

    [RelayCommand]
    private async Task PrimeraPaginaAsync()
    {
        if (PaginaActual <= 1) return;
        PaginaActual = 1;
        await CargarDatosAsync();
    }

    [RelayCommand]
    private async Task PaginaAnteriorAsync()
    {
        if (PaginaActual <= 1) return;
        PaginaActual--;
        await CargarDatosAsync();
    }

    [RelayCommand]
    private async Task PaginaSiguienteAsync()
    {
        if (PaginaActual >= TotalPaginas) return;
        PaginaActual++;
        await CargarDatosAsync();
    }

    [RelayCommand]
    private async Task UltimaPaginaAsync()
    {
        if (PaginaActual >= TotalPaginas) return;
        PaginaActual = TotalPaginas;
        await CargarDatosAsync();
    }

    [RelayCommand]
    private async Task CambiarCantidadPorPaginaAsync(string cantidadStr)
    {
        if (int.TryParse(cantidadStr, out int cant))
        {
            CantidadPorPagina = cant;
            PaginaActual = 1;
            await CargarDatosAsync();
        }
    }

    [RelayCommand]
    private async Task CambiarRubroAsync(string rubro)
    {
        RubroSeleccionado = rubro;
        PaginaActual = 1;
        await CargarDatosAsync();
    }

    [RelayCommand]
    private async Task NuevoArticuloAsync()
    {
        var config = await _configuracionService.ObtenerConfiguracionAsync();
        var margen = config.MargenGananciaSugerido > 0 ? config.MargenGananciaSugerido : 40m;
        var sku = await _inventarioService.GenerarSkuSugeridoAsync();
        var codigoBarras = await _inventarioService.GenerarCodigoBarrasSugeridoAsync();

        var costo = 1000m;
        var venta = Math.Round(costo * (1 + (margen / 100m)), 2);

        var nuevo = new ArticuloDto
        {
            Nombre = string.Empty,
            SKU = sku,
            CodigoBarras = codigoBarras,
            PrecioCosto = costo,
            PorcentajeGanancia = margen,
            PrecioVenta = venta,
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

    [RelayCommand]
    private async Task EliminarArticuloAsync(ArticuloDto? item)
    {
        var art = item ?? ArticuloSeleccionado;
        if (art == null || art.Id == Guid.Empty) return;

        var confirm = System.Windows.MessageBox.Show(
            $"¿Está seguro de que desea eliminar el artículo '{art.Nombre}' (SKU: {art.SKU})?\n\nEsta acción lo quitará del inventario activo y del catálogo de ventas.",
            "Confirmar Eliminación",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (confirm == System.Windows.MessageBoxResult.Yes)
        {
            var res = await _inventarioService.EliminarArticuloAsync(art.Id);
            if (res)
            {
                await CargarDatosAsync();
            }
            else
            {
                System.Windows.MessageBox.Show("No se pudo eliminar el artículo seleccionado.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    private async Task ExportarCsvAsync()
    {
        try
        {
            var sfd = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"Catalogo_Libreria_{DateTime.Now:yyyyMMdd_HHmm}.csv",
                Filter = "Archivo CSV (*.csv)|*.csv|Todos los archivos (*.*)|*.*",
                DefaultExt = ".csv"
            };

            if (sfd.ShowDialog() == true)
            {
                var csv = await _inventarioService.ExportarCatalogoCsvAsync();
                await System.IO.File.WriteAllTextAsync(sfd.FileName, csv, System.Text.Encoding.UTF8);
                System.Windows.MessageBox.Show("Catálogo exportado exitosamente a CSV.", "Exportación Completa", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error al exportar: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task ImportarCsvAsync()
    {
        try
        {
            var ofd = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Archivo CSV (*.csv)|*.csv|Todos los archivos (*.*)|*.*",
                DefaultExt = ".csv"
            };

            if (ofd.ShowDialog() == true)
            {
                var csv = await System.IO.File.ReadAllTextAsync(ofd.FileName, System.Text.Encoding.UTF8);
                var (creados, actualizados, errores) = await _inventarioService.ImportarCatalogoCsvAsync(csv);
                await CargarDatosAsync();

                System.Windows.MessageBox.Show(
                    $"Importación finalizada:\n\n" +
                    $"• Artículos nuevos creados: {creados}\n" +
                    $"• Artículos existentes actualizados: {actualizados}\n" +
                    $"• Filas omitidas / errores: {errores}",
                    "Resultado de Importación",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error al importar catálogo: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
}
