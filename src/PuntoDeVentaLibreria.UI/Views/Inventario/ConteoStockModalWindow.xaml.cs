using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using PuntoDeVentaLibreria.Application.DTOs.Inventario;
using PuntoDeVentaLibreria.Application.Services;
using PuntoDeVentaLibreria.UI.Views.Pos;

namespace PuntoDeVentaLibreria.UI.Views.Inventario;

public class ItemAuditoriaSesionModel
{
    public string HoraTexto { get; set; } = string.Empty;
    public string Rubro { get; set; } = "Librería";
    public string SKU { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal StockAnterior { get; set; }
    public decimal NuevoStock { get; set; }
    public decimal Variacion => NuevoStock - StockAnterior;
    public string VariacionTexto => Variacion > 0 ? $"+{Variacion:N0}" : $"{Variacion:N0}";
}

public partial class ConteoStockModalWindow : Window
{
    private readonly IInventarioService _inventarioService;
    private ArticuloDto? _articuloSeleccionado;
    public ObservableCollection<ItemAuditoriaSesionModel> HistorialSesion { get; } = new();

    public ConteoStockModalWindow(IInventarioService inventarioService)
    {
        _inventarioService = inventarioService ?? throw new ArgumentNullException(nameof(inventarioService));
        InitializeComponent();

        GridSesion.ItemsSource = HistorialSesion;

        Loaded += async (_, _) =>
        {
            await CargarMetricasProgresoAsync();
            TxtEscaneo.Focus();
        };

        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        };
    }

    private async Task CargarMetricasProgresoAsync()
    {
        try
        {
            var progreso = await _inventarioService.ObtenerProgresoAuditoriaAsync();
            TxtTotalArticulos.Text = $"{progreso.TotalArticulos:N0}";
            TxtAuditados.Text = $"{progreso.ArticulosAuditados:N0}";
            TxtPendientes.Text = $"{progreso.ArticulosPendientes:N0}";

            PbProgreso.Value = progreso.PorcentajeCompletado;
            TxtPorcentajeProgreso.Text = $"{progreso.PorcentajeCompletado:N1}% auditado ({progreso.ArticulosAuditados} de {progreso.TotalArticulos})";
        }
        catch { }
    }

    private async void TxtEscaneo_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await ProcesarEscaneoAsync();
        }
    }

    private async void BtnBuscar_Click(object sender, RoutedEventArgs e)
    {
        await ProcesarEscaneoAsync();
    }

    private async Task ProcesarEscaneoAsync()
    {
        var query = TxtEscaneo.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(query))
        {
            TxtEscaneo.Focus();
            return;
        }

        // 1. Buscar por código de barras exacto o secundario
        var art = await _inventarioService.BuscarPorCodigoBarrasAsync(query);

        if (art == null)
        {
            // 2. Buscar por SKU o nombre
            var coincidencias = await _inventarioService.BuscarArticulosAsync(query);
            if (coincidencias != null && coincidencias.Count == 1)
            {
                art = coincidencias[0];
            }
            else if (coincidencias != null && coincidencias.Count > 1)
            {
                var selector = new SeleccionarArticuloModalWindow(coincidencias, query)
                {
                    Owner = this
                };
                if (selector.ShowDialog() == true)
                {
                    art = selector.ArticuloSeleccionado;
                }
            }
        }

        if (art == null)
        {
            MessageBox.Show($"No se encontró ningún artículo para: '{query}'", "No encontrado", MessageBoxButton.OK, MessageBoxImage.Information);
            TxtEscaneo.Focus();
            TxtEscaneo.SelectAll();
            return;
        }

        _articuloSeleccionado = art;

        // Si está en Modo Continuo (+1 por escaneo)
        if (RbModoContinuo.IsChecked == true)
        {
            decimal nuevoStock = art.StockActual + 1;
            await AplicarAjusteAsync(art, nuevoStock);
            TxtEscaneo.Text = string.Empty;
            TxtEscaneo.Focus();
            return;
        }

        // Modo Manual: Mostrar Ficha y enfocar cantidad
        MostrarFichaArticulo(art);
    }

    private void MostrarFichaArticulo(ArticuloDto art)
    {
        TxtArticuloNombre.Text = art.Nombre;
        TxtArticuloRubro.Text = art.Rubro ?? "Librería";

        if (string.Equals(art.Rubro, "Regalería", StringComparison.OrdinalIgnoreCase))
        {
            BadgeRubro.Background = new SolidColorBrush(Color.FromRgb(243, 232, 255));
            TxtArticuloRubro.Foreground = new SolidColorBrush(Color.FromRgb(109, 40, 217));
        }
        else
        {
            BadgeRubro.Background = new SolidColorBrush(Color.FromRgb(224, 231, 255));
            TxtArticuloRubro.Foreground = new SolidColorBrush(Color.FromRgb(55, 48, 163));
        }

        TxtArticuloCodigos.Text = $"SKU: {art.SKU} | Barras: {art.CodigoBarras ?? "Sin código"}";
        TxtArticuloPrecio.Text = $"Precio Venta: ${art.PrecioVenta:N2}";
        TxtStockSistema.Text = $"{art.StockActual:N0}";

        TxtNuevoStock.Text = $"{art.StockActual:N0}";
        BrdFichaArticulo.Visibility = Visibility.Visible;

        TxtNuevoStock.Focus();
        TxtNuevoStock.SelectAll();
    }

    private async void TxtNuevoStock_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await ConfirmarAjusteManualAsync();
        }
    }

    private async void BtnConfirmarStock_Click(object sender, RoutedEventArgs e)
    {
        await ConfirmarAjusteManualAsync();
    }

    private async Task ConfirmarAjusteManualAsync()
    {
        if (_articuloSeleccionado == null) return;

        var txt = TxtNuevoStock.Text?.Trim() ?? "0";
        if (!decimal.TryParse(txt, NumberStyles.Any, CultureInfo.CurrentCulture, out var nuevoStock) &&
            !decimal.TryParse(txt, NumberStyles.Any, CultureInfo.InvariantCulture, out nuevoStock))
        {
            MessageBox.Show("Por favor, ingrese una cantidad numérica válida.", "Valor inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtNuevoStock.Focus();
            return;
        }

        if (nuevoStock < 0)
        {
            MessageBox.Show("El stock físico contado no puede ser negativo.", "Cantidad inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtNuevoStock.Focus();
            return;
        }

        await AplicarAjusteAsync(_articuloSeleccionado, nuevoStock);

        BrdFichaArticulo.Visibility = Visibility.Collapsed;
        _articuloSeleccionado = null;
        TxtEscaneo.Text = string.Empty;
        TxtEscaneo.Focus();
    }

    private async Task AplicarAjusteAsync(ArticuloDto art, decimal nuevoStock)
    {
        decimal previo = art.StockActual;

        try
        {
            var actualizado = await _inventarioService.AjustarStockRapidoAsync(
                art.Id,
                nuevoStock,
                "Auditoría de Stock por Góndola",
                "Operador");

            HistorialSesion.Insert(0, new ItemAuditoriaSesionModel
            {
                HoraTexto = DateTime.Now.ToString("HH:mm:ss"),
                Rubro = actualizado.Rubro,
                SKU = actualizado.SKU,
                Nombre = actualizado.Nombre,
                StockAnterior = previo,
                NuevoStock = nuevoStock
            });

            TxtConteoSession.Text = $"({HistorialSesion.Count} productos)";
            await CargarMetricasProgresoAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al actualizar el stock: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnCerrar_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
