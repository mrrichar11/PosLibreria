using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PuntoDeVentaLibreria.Application.DTOs.Inventario;
using PuntoDeVentaLibreria.Application.Services;
using PuntoDeVentaLibreria.Domain.Entities.Catalogo;

namespace PuntoDeVentaLibreria.UI.Views.Inventario;

public partial class ArticuloModalWindow : Window
{
    private readonly IInventarioService _inventarioService;
    private readonly IConfiguracionService? _configuracionService;
    private IReadOnlyList<ArticuloDto> _articulosDisponibles = new List<ArticuloDto>();
    private decimal _margenConfigurado = 40m;
    private bool _isCalculating;

    public ArticuloDto Articulo { get; }
    public bool GuardadoExitoso { get; private set; }

    public ArticuloModalWindow(ArticuloDto articulo, IInventarioService inventarioService, IConfiguracionService? configuracionService = null)
    {
        InitializeComponent();
        Articulo = articulo ?? throw new ArgumentNullException(nameof(articulo));
        _inventarioService = inventarioService ?? throw new ArgumentNullException(nameof(inventarioService));
        _configuracionService = configuracionService;
        DataContext = Articulo;

        if (string.IsNullOrEmpty(Articulo.ColorBoton))
        {
            Articulo.ColorBoton = "#3B82F6";
        }

        // Inicializar campos de precio en UI de forma robusta e invariable
        _isCalculating = true;
        try
        {
            TxtCosto.Text = Articulo.PrecioCosto > 0 ? Articulo.PrecioCosto.ToString("0.##", CultureInfo.InvariantCulture) : "0";
            TxtMargen.Text = Articulo.PorcentajeGanancia > 0 ? Articulo.PorcentajeGanancia.ToString("0.#", CultureInfo.InvariantCulture) : "40";
            TxtVenta.Text = Articulo.PrecioVenta > 0 ? Articulo.PrecioVenta.ToString("0.##", CultureInfo.InvariantCulture) : "0";
        }
        finally
        {
            _isCalculating = false;
        }

        SincronizarTipoUI();
        CargarArticulosDisponibles();
        CargarConfiguracionNegocioAsync();

        TxtNombre.Focus();
    }

    private async void CargarConfiguracionNegocioAsync()
    {
        try
        {
            if (_configuracionService != null)
            {
                var cfg = await _configuracionService.ObtenerConfiguracionAsync();
                if (cfg.MargenGananciaSugerido > 0)
                {
                    _margenConfigurado = cfg.MargenGananciaSugerido;
                }
            }
        }
        catch { }

        BtnAplicarMargenConfig.Content = $"🎯 Aplicar Margen del Comercio ({_margenConfigurado:0.#}%)";

        // Si es un artículo nuevo o no tiene margen configurado
        if (Articulo.PorcentajeGanancia <= 0)
        {
            _isCalculating = true;
            try
            {
                Articulo.PorcentajeGanancia = _margenConfigurado;
                TxtMargen.Text = _margenConfigurado.ToString("0.#", CultureInfo.InvariantCulture);
            }
            finally
            {
                _isCalculating = false;
            }
            RecalcularPrecioVentaDesdeCostoYMargen();
        }
    }

    private void SincronizarTipoUI()
    {
        foreach (ComboBoxItem item in CmbTipo.Items)
        {
            if (item.Tag is string tag && tag == Articulo.Tipo.ToString())
            {
                CmbTipo.SelectedItem = item;
                break;
            }
        }
        ActualizarVisibilidadCombo();
    }

    private async void CargarArticulosDisponibles()
    {
        try
        {
            _articulosDisponibles = await _inventarioService.BuscarArticulosAsync(string.Empty);
            var articulosFisicos = _articulosDisponibles
                .Where(a => a.Id != Articulo.Id && a.Tipo != TipoArticulo.ComboKit)
                .OrderBy(a => a.Nombre)
                .ToList();

            CmbArticuloParaCombo.ItemsSource = articulosFisicos;
            if (articulosFisicos.Count > 0)
            {
                CmbArticuloParaCombo.SelectedIndex = 0;
            }
        }
        catch { }
    }

    private void CmbTipo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ActualizarVisibilidadCombo();
    }

    private void ActualizarVisibilidadCombo()
    {
        if (BrdComponentesCombo == null || CmbTipo?.SelectedItem is not ComboBoxItem selected) return;
        var esCombo = selected.Tag?.ToString() == "ComboKit";
        BrdComponentesCombo.Visibility = esCombo ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void BtnGenerarCodigoBarras_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var nuevoCodigo = await _inventarioService.GenerarCodigoBarrasSugeridoAsync();
            Articulo.CodigoBarras = nuevoCodigo;
            TxtCodigoBarras.Text = nuevoCodigo;
            TxtCodigoBarras.SelectAll();
        }
        catch { }
    }

    private async void BtnGenerarSku_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var nuevoSku = await _inventarioService.GenerarSkuSugeridoAsync();
            Articulo.SKU = nuevoSku;
            TxtSKU.Text = nuevoSku;
            TxtSKU.SelectAll();
        }
        catch { }
    }

    private void TxtCodigoBarras_GotFocus(object sender, RoutedEventArgs e)
    {
        TxtCodigoBarras.SelectAll();
    }

    private void TxtCodigoBarras_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            TxtSKU.Focus();
            e.Handled = true;
        }
    }

    private void BtnAplicarMargenConfig_Click(object sender, RoutedEventArgs e)
    {
        _isCalculating = true;
        try
        {
            Articulo.PorcentajeGanancia = _margenConfigurado;
            TxtMargen.Text = _margenConfigurado.ToString("0.#", CultureInfo.InvariantCulture);
        }
        finally
        {
            _isCalculating = false;
        }
        RecalcularPrecioVentaDesdeCostoYMargen();
    }

    private void TxtCosto_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isCalculating) return;
        RecalcularPrecioVentaDesdeCostoYMargen();
    }

    private void TxtMargen_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isCalculating) return;
        RecalcularPrecioVentaDesdeCostoYMargen();
    }

    private void TxtVenta_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isCalculating) return;
        RecalcularMargenDesdeVenta();
    }

    private void RecalcularPrecioVentaDesdeCostoYMargen()
    {
        if (_isCalculating) return;
        if (!PuntoDeVentaLibreria.Application.Common.CalculoPreciosUtils.TryParseMonto(TxtCosto?.Text, out var costo)) return;
        if (!PuntoDeVentaLibreria.Application.Common.CalculoPreciosUtils.TryParseMonto(TxtMargen?.Text, out var margen)) return;

        _isCalculating = true;
        try
        {
            var venta = PuntoDeVentaLibreria.Application.Common.CalculoPreciosUtils.CalcularPrecioVenta(costo, margen);
            Articulo.PrecioCosto = costo;
            Articulo.PorcentajeGanancia = margen;
            Articulo.PrecioVenta = venta;
            if (TxtVenta != null && !TxtVenta.IsFocused)
            {
                TxtVenta.Text = venta.ToString("0.00", CultureInfo.InvariantCulture);
            }
        }
        finally
        {
            _isCalculating = false;
        }
    }

    private void RecalcularMargenDesdeVenta()
    {
        if (_isCalculating) return;
        if (!PuntoDeVentaLibreria.Application.Common.CalculoPreciosUtils.TryParseMonto(TxtCosto?.Text, out var costo) || costo <= 0) return;
        if (!PuntoDeVentaLibreria.Application.Common.CalculoPreciosUtils.TryParseMonto(TxtVenta?.Text, out var venta)) return;

        _isCalculating = true;
        try
        {
            var margen = PuntoDeVentaLibreria.Application.Common.CalculoPreciosUtils.CalcularMargenPorcentaje(costo, venta);
            Articulo.PrecioCosto = costo;
            Articulo.PrecioVenta = venta;
            Articulo.PorcentajeGanancia = margen;
            if (TxtMargen != null && !TxtMargen.IsFocused)
            {
                TxtMargen.Text = margen.ToString("0.#", CultureInfo.InvariantCulture);
            }
        }
        finally
        {
            _isCalculating = false;
        }
    }

    public static bool TryParseMonto(string input, out decimal result)
    {
        return PuntoDeVentaLibreria.Application.Common.CalculoPreciosUtils.TryParseMonto(input, out result);
    }

    private void BtnAgregarComponente_Click(object sender, RoutedEventArgs e)
    {
        if (CmbArticuloParaCombo.SelectedItem is not ArticuloDto seleccionado)
        {
            MessageBox.Show("Seleccione un artículo para agregar al combo.", "MR SYS", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!decimal.TryParse(TxtCantidadParaCombo.Text, out var cant) || cant <= 0)
        {
            MessageBox.Show("Ingrese una cantidad válida mayor a 0.", "MR SYS", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var existente = Articulo.ComponentesDelCombo.FirstOrDefault(c => c.ComponenteArticuloId == seleccionado.Id);
        if (existente != null)
        {
            existente.Cantidad += cant;
        }
        else
        {
            Articulo.ComponentesDelCombo.Add(new ComboComponenteDto
            {
                ComponenteArticuloId = seleccionado.Id,
                NombreArticulo = seleccionado.Nombre,
                SKU = seleccionado.SKU,
                Cantidad = cant,
                StockActualDisponible = seleccionado.StockActual
            });
        }

        TxtCantidadParaCombo.Text = "1";
    }

    private void BtnQuitarComponente_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is ComboComponenteDto item)
        {
            Articulo.ComponentesDelCombo.Remove(item);
        }
    }

    private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Articulo.Nombre))
        {
            MessageBox.Show("El nombre del artículo es obligatorio.", "MR SYS", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtNombre.Focus();
            return;
        }

        // Sincronizar montos finales desde los TextBox con TryParseMonto
        if (TryParseMonto(TxtCosto.Text, out var c)) Articulo.PrecioCosto = c;
        if (TryParseMonto(TxtMargen.Text, out var m)) Articulo.PorcentajeGanancia = m;
        if (TryParseMonto(TxtVenta.Text, out var v)) Articulo.PrecioVenta = v;

        // Asegurar que si el SKU quedó vacío se autogenere uno único
        if (string.IsNullOrWhiteSpace(Articulo.SKU))
        {
            Articulo.SKU = await _inventarioService.GenerarSkuSugeridoAsync();
        }

        // Asegurar que si el código de barras quedó vacío se autogenere uno válido
        if (string.IsNullOrWhiteSpace(Articulo.CodigoBarras))
        {
            Articulo.CodigoBarras = await _inventarioService.GenerarCodigoBarrasSugeridoAsync();
        }

        if (CmbTipo.SelectedItem is ComboBoxItem selectedTipo && Enum.TryParse<TipoArticulo>(selectedTipo.Tag?.ToString(), out var tipoEnum))
        {
            Articulo.Tipo = tipoEnum;
        }

        if (Articulo.Tipo == TipoArticulo.ComboKit && Articulo.ComponentesDelCombo.Count == 0)
        {
            var res = MessageBox.Show(
                "Ha seleccionado tipo 'Combo Escolar' pero aún no agregó ningún artículo componente.\n¿Desea guardarlo de todas formas?",
                "MR SYS Confirmación", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res != MessageBoxResult.Yes) return;
        }

        try
        {
            await _inventarioService.GuardarArticuloAsync(Articulo);
            GuardadoExitoso = true;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al guardar el artículo: {ex.Message}", "MR SYS Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
