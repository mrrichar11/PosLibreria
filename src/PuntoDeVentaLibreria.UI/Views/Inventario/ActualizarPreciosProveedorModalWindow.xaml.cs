using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
using PuntoDeVentaLibreria.Application.DTOs.Inventario;
using PuntoDeVentaLibreria.Application.DTOs.Proveedores;
using PuntoDeVentaLibreria.Application.Services;

namespace PuntoDeVentaLibreria.UI.Views.Inventario;

public partial class ActualizarPreciosProveedorModalWindow : Window
{
    private readonly IInventarioService _inventarioService;
    private readonly IProveedorService _proveedorService;
    private readonly IConfiguracionService? _configuracionService;
    private string _rutaArchivo = string.Empty;
    private List<ArticuloAumentoPrecioItemDto> _itemsComparados = new();
    private int _totalCatalogo;
    private int _noEncontrados;
    private string _filtroTipoActual = "Todos";
    private string _filtroTexto = string.Empty;

    public bool PreciosActualizados { get; private set; }

    public ActualizarPreciosProveedorModalWindow(IInventarioService inventarioService, IProveedorService proveedorService, IConfiguracionService? configuracionService = null)
    {
        InitializeComponent();
        _inventarioService = inventarioService ?? throw new ArgumentNullException(nameof(inventarioService));
        _proveedorService = proveedorService ?? throw new ArgumentNullException(nameof(proveedorService));
        _configuracionService = configuracionService;

        Loaded += async (s, e) =>
        {
            await CargarProveedoresAsync();
        };
    }

    private async Task CargarProveedoresAsync()
    {
        try
        {
            var proveedores = await _proveedorService.ObtenerTodosAsync();
            var lista = new List<ProveedorDto>
            {
                new ProveedorDto { Id = Guid.Empty, Nombre = "(Todos los proveedores)" }
            };
            lista.AddRange(proveedores);

            CmbProveedor.ItemsSource = lista;
            CmbProveedor.SelectedIndex = 0;
        }
        catch { }
    }

    private void CmbProveedor_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ChkAsignarProveedor == null || ChkSoloArticulosDelProveedor == null) return;

        if (CmbProveedor.SelectedItem is ProveedorDto p && p.Id != Guid.Empty)
        {
            ChkAsignarProveedor.IsEnabled = true;
            ChkAsignarProveedor.IsChecked = true;
            ChkAsignarProveedor.Content = $"Asignar '{p.Nombre}' a los artículos actualizados que no lo tengan";

            ChkSoloArticulosDelProveedor.IsEnabled = true;
            ChkSoloArticulosDelProveedor.Content = $"Buscar únicamente en artículos que ya tienen '{p.Nombre}' asignado";
        }
        else
        {
            ChkAsignarProveedor.IsEnabled = false;
            ChkAsignarProveedor.IsChecked = false;
            ChkAsignarProveedor.Content = "Asignar proveedor a los artículos actualizados";

            ChkSoloArticulosDelProveedor.IsEnabled = false;
            ChkSoloArticulosDelProveedor.IsChecked = false;
            ChkSoloArticulosDelProveedor.Content = "Buscar únicamente en artículos que ya tienen este proveedor asignado";
        }
    }

    private void BtnExaminar_Click(object sender, RoutedEventArgs e)
    {
        var ofd = new OpenFileDialog
        {
            Filter = "Listas de Precios (*.xls;*.xlsx)|*.xls;*.xlsx|Todos los archivos (*.*)|*.*",
            Title = "Seleccione la lista de precios del proveedor"
        };

        if (ofd.ShowDialog() == true)
        {
            _rutaArchivo = ofd.FileName;
            TxtRutaArchivo.Text = _rutaArchivo;
            BtnComparar_Click(sender, e);
        }
    }

    private async void BtnComparar_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_rutaArchivo) || !File.Exists(_rutaArchivo))
        {
            MessageBox.Show("Por favor seleccione un archivo de proveedor primero.", "MR SYS", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        PbProgreso.Visibility = Visibility.Visible;
        BtnComparar.IsEnabled = false;
        BtnAplicarAumento.IsEnabled = false;
        TxtEstadoVacio.Text = "Cruzando catálogo de artículos con lista de precios del proveedor...";

        Guid? proveedorIdFiltro = null;
        if (ChkSoloArticulosDelProveedor.IsChecked == true && CmbProveedor.SelectedItem is ProveedorDto p && p.Id != Guid.Empty)
        {
            proveedorIdFiltro = p.Id;
        }

        try
        {
            using var stream = File.OpenRead(_rutaArchivo);
            var resumen = await _inventarioService.PrevisualizarActualizacionPreciosProveedorAsync(stream, proveedorIdFiltro);

            _itemsComparados = resumen.ItemsParaActualizar;
            _totalCatalogo = resumen.TotalArticulosCatalogo;
            _noEncontrados = resumen.NoEncontradosEnCatalogo;

            var reglaActual = ObtenerReglaRedondeoSeleccionada();
            foreach (var item in _itemsComparados)
            {
                item.ReglaRedondeo = reglaActual;
                item.Recalcular();
            }

            GridComparativa.ItemsSource = _itemsComparados;
            TxtEstadoVacio.Visibility = _itemsComparados.Count > 0 ? Visibility.Collapsed : Visibility.Visible;

            PnlMetricas.Visibility = Visibility.Visible;
            ActualizarContadoresMetricas();

            RbFiltroTodos.IsChecked = true;
            _filtroTipoActual = "Todos";
            AplicarFiltroVista();

            BtnAplicarAumento.IsEnabled = _itemsComparados.Any(i => i.Aplicar);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al comparar precios:\n\n{ex.Message}", "MR SYS Error", MessageBoxButton.OK, MessageBoxImage.Error);
            TxtEstadoVacio.Text = "Ocurrió un error al procesar el archivo.";
        }
        finally
        {
            PbProgreso.Visibility = Visibility.Collapsed;
            BtnComparar.IsEnabled = true;
        }
    }

    private void ActualizarContadoresMetricas()
    {
        if (TxtTotalCatalogo == null || TxtCoincidencias == null || TxtConAumento == null || 
            TxtConAlerta == null || TxtNoEncontrados == null || BorderAlertas == null) return;

        int total = _itemsComparados.Count;
        int aumentos = _itemsComparados.Count(i => !i.EsAlertaVariacionExtrema && !i.EsAlertaCodigoReutilizado && i.CostoNuevo > i.CostoAnterior + 0.01m);
        int bajas = _itemsComparados.Count(i => i.EsBajaDePrecio);
        int alertas = _itemsComparados.Count(i => i.EsAlertaVariacionExtrema);
        int reutilizados = _itemsComparados.Count(i => i.EsAlertaCodigoReutilizado);
        int sinCambio = _itemsComparados.Count(i => Math.Abs(i.CostoNuevo - i.CostoAnterior) <= 0.01m);

        TxtTotalCatalogo.Text = $"Catálogo Local: {_totalCatalogo:N0}";
        TxtCoincidencias.Text = $"Coincidentes: {total:N0}";
        TxtConAumento.Text = $"📈 Aumentos: {aumentos:N0}";
        if (TxtBajasDePrecio != null) TxtBajasDePrecio.Text = $"📉 Bajas a Revisar: {bajas:N0}";
        TxtConAlerta.Text = $"⚠️ Posibles Packs: {alertas:N0}";
        if (TxtCodigosReutilizados != null) TxtCodigosReutilizados.Text = $"⚠️ Cód. Reutilizados: {reutilizados:N0}";
        TxtNoEncontrados.Text = $"Filas sin asociar en local: {_noEncontrados:N0}";

        BorderAlertas.Visibility = alertas > 0 ? Visibility.Visible : Visibility.Collapsed;
        if (BorderBajas != null) BorderBajas.Visibility = bajas > 0 ? Visibility.Visible : Visibility.Collapsed;
        if (BorderReutilizados != null) BorderReutilizados.Visibility = reutilizados > 0 ? Visibility.Visible : Visibility.Collapsed;

        // Textos de los filtros de radio
        if (RbFiltroTodos != null) RbFiltroTodos.Content = $"Mostrar Todos ({total:N0})";
        if (RbFiltroConCambio != null) RbFiltroConCambio.Content = $"📈 Aumentos ({aumentos:N0})";
        if (RbFiltroBajas != null) RbFiltroBajas.Content = $"📉 Bajas a Revisar ({bajas:N0})";
        if (RbFiltroAlertas != null) RbFiltroAlertas.Content = $"⚠️ Posibles Packs ({alertas:N0})";
        if (RbFiltroReutilizados != null) RbFiltroReutilizados.Content = $"⚠️ Cód. Reutilizados ({reutilizados:N0})";
        if (RbFiltroSinCambio != null) RbFiltroSinCambio.Content = $"⏸️ Sin Cambios ({sinCambio:N0})";

        // Botón masivo para auto-aplicar sugerencias
        int sugeridosPendientes = _itemsComparados.Count(i => i.FactorSugerido.HasValue && i.MostrarBotonSugerido);
        if (BtnAutoAplicarDivisores != null)
        {
            if (sugeridosPendientes > 0)
            {
                BtnAutoAplicarDivisores.Visibility = Visibility.Visible;
                BtnAutoAplicarDivisores.Content = $"✨ Auto-aplicar divisores sugeridos ({sugeridosPendientes})";
            }
            else
            {
                BtnAutoAplicarDivisores.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void TxtFiltroBusqueda_TextChanged(object sender, TextChangedEventArgs e)
    {
        _filtroTexto = TxtFiltroBusqueda.Text.Trim();
        AplicarFiltroVista();
    }

    private void FiltroRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        if (sender is RadioButton rb && rb.IsChecked == true)
        {
            if (rb == RbFiltroTodos) _filtroTipoActual = "Todos";
            else if (rb == RbFiltroConCambio) _filtroTipoActual = "Aumentos";
            else if (rb == RbFiltroBajas) _filtroTipoActual = "Bajas";
            else if (rb == RbFiltroAlertas) _filtroTipoActual = "Alertas";
            else if (rb == RbFiltroReutilizados) _filtroTipoActual = "Reutilizados";
            else if (rb == RbFiltroSinCambio) _filtroTipoActual = "SinCambio";

            AplicarFiltroVista();
        }
    }

    private void AplicarFiltroVista()
    {
        if (GridComparativa == null || GridComparativa.ItemsSource == null) return;
        var view = CollectionViewSource.GetDefaultView(GridComparativa.ItemsSource);
        if (view == null) return;

        view.Filter = obj =>
        {
            if (obj is not ArticuloAumentoPrecioItemDto item) return false;

            // 1. Filtro por tipo/estado
            bool cumpleTipo = _filtroTipoActual switch
            {
                "Aumentos" => !item.EsAlertaVariacionExtrema && !item.EsAlertaCodigoReutilizado && item.CostoNuevo > item.CostoAnterior + 0.01m,
                "Bajas" => item.EsBajaDePrecio,
                "Alertas" => item.EsAlertaVariacionExtrema,
                "Reutilizados" => item.EsAlertaCodigoReutilizado,
                "SinCambio" => Math.Abs(item.CostoNuevo - item.CostoAnterior) <= 0.01m,
                _ => true
            };

            if (!cumpleTipo) return false;

            // 2. Filtro por texto de búsqueda rápida (nombre, sku, código proveedor, barras)
            if (!string.IsNullOrWhiteSpace(_filtroTexto))
            {
                var q = _filtroTexto.ToLowerInvariant();
                bool match = (item.Nombre?.ToLowerInvariant().Contains(q) == true)
                    || (item.SKU?.ToLowerInvariant().Contains(q) == true)
                    || (item.CodigoProveedor?.ToLowerInvariant().Contains(q) == true)
                    || (item.CodigoBarras?.ToLowerInvariant().Contains(q) == true)
                    || (item.DescripcionProveedor?.ToLowerInvariant().Contains(q) == true);

                if (!match) return false;
            }

            return true;
        };
    }

    private async void BtnEditarArticuloFila_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is ArticuloAumentoPrecioItemDto item)
        {
            await AbrirEdicionArticuloAsync(item);
        }
    }

    private async void GridComparativa_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (GridComparativa.SelectedItem is ArticuloAumentoPrecioItemDto item)
        {
            await AbrirEdicionArticuloAsync(item);
        }
    }

    private async Task AbrirEdicionArticuloAsync(ArticuloAumentoPrecioItemDto item)
    {
        try
        {
            var dto = await _inventarioService.ObtenerPorIdAsync(item.ArticuloId);
            if (dto == null)
            {
                MessageBox.Show("No se encontró el artículo en la base de datos.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var modal = new ArticuloModalWindow(dto, _inventarioService, _configuracionService, _proveedorService)
            {
                Owner = this
            };

            if (modal.ShowDialog() == true && modal.GuardadoExitoso)
            {
                var actualizado = await _inventarioService.ObtenerPorIdAsync(item.ArticuloId);
                if (actualizado != null)
                {
                    item.Nombre = actualizado.Nombre;
                    item.SKU = actualizado.SKU;
                    item.CodigoProveedor = actualizado.CodigoProveedor;
                    item.CodigoBarras = actualizado.CodigoBarras;
                    item.CostoAnterior = actualizado.PrecioCosto;
                    item.VentaAnterior = actualizado.PrecioVenta;
                    item.PorcentajeGanancia = actualizado.PorcentajeGanancia;
                    item.IvaPorcentaje = actualizado.IvaPorcentaje;
                    item.Recalcular();
                    GridComparativa.Items.Refresh();
                    ActualizarContadoresMetricas();
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al editar el artículo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnEliminarArticuloFila_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is ArticuloAumentoPrecioItemDto item)
        {
            var confirm = MessageBox.Show(
                $"¿Está seguro de eliminar el artículo '{item.Nombre}' (SKU: {item.SKU}) del catálogo?\n\nEsta acción quitará el producto de las listas y del punto de venta.",
                "Confirmar Eliminación", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Warning);

            if (confirm == MessageBoxResult.Yes)
            {
                try
                {
                    var ok = await _inventarioService.EliminarArticuloAsync(item.ArticuloId);
                    if (ok)
                    {
                        _itemsComparados.Remove(item);
                        GridComparativa.Items.Refresh();
                        ActualizarContadoresMetricas();
                        BtnAplicarAumento.IsEnabled = _itemsComparados.Any(i => i.Aplicar);
                        MessageBox.Show("Artículo eliminado con éxito del catálogo.", "MR SYS", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al eliminar el artículo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private void BtnOmitirFila_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is ArticuloAumentoPrecioItemDto item)
        {
            item.Aplicar = false;
            GridComparativa.Items.Refresh();
            BtnAplicarAumento.IsEnabled = _itemsComparados.Any(i => i.Aplicar);
        }
    }

    private void BtnAplicarFactorFila_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is ArticuloAumentoPrecioItemDto item && item.FactorSugerido.HasValue)
        {
            item.FactorConversion = item.FactorSugerido.Value;
            item.Aplicar = true;
            ActualizarContadoresMetricas();
            BtnAplicarAumento.IsEnabled = _itemsComparados.Any(i => i.Aplicar);
        }
    }

    private void BtnAutoAplicarDivisores_Click(object sender, RoutedEventArgs e)
    {
        int aplicados = 0;
        foreach (var item in _itemsComparados.Where(i => i.FactorSugerido.HasValue && i.MostrarBotonSugerido))
        {
            if (item.FactorSugerido.HasValue)
            {
                item.FactorConversion = item.FactorSugerido.Value;
                item.Aplicar = true;
                aplicados++;
            }
        }

        ActualizarContadoresMetricas();
        GridComparativa.Items.Refresh();
        BtnAplicarAumento.IsEnabled = _itemsComparados.Any(i => i.Aplicar);

        MessageBox.Show($"Se aplicaron automáticamente los factores divisores a {aplicados:N0} artículos en base a su presentación.", 
            "MR SYS - Conciliación de Packs", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnMarcarSoloAumentos_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in _itemsComparados)
        {
            if (!item.EsAlertaVariacionExtrema && !item.EsAlertaCodigoReutilizado && !item.EsBajaDePrecio && item.CostoNuevo > item.CostoAnterior + 0.01m)
            {
                item.Aplicar = true;
            }
            else
            {
                item.Aplicar = false;
            }
        }
        GridComparativa.Items.Refresh();
        BtnAplicarAumento.IsEnabled = _itemsComparados.Any(i => i.Aplicar);
    }

    private void BtnMarcarTodos_Click(object sender, RoutedEventArgs e)
    {
        var view = CollectionViewSource.GetDefaultView(GridComparativa.ItemsSource);
        foreach (var item in _itemsComparados)
        {
            if (view?.Filter == null || view.Filter(item))
            {
                if (!item.EsAlertaVariacionExtrema && !item.EsAlertaCodigoReutilizado)
                {
                    item.Aplicar = true;
                }
            }
        }
        GridComparativa.Items.Refresh();
        BtnAplicarAumento.IsEnabled = _itemsComparados.Any(i => i.Aplicar);
    }

    private void BtnDesmarcarTodos_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in _itemsComparados)
        {
            item.Aplicar = false;
        }
        GridComparativa.Items.Refresh();
        BtnAplicarAumento.IsEnabled = false;
    }

    private void CmbReglaRedondeo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded || _itemsComparados == null || _itemsComparados.Count == 0) return;
        var regla = ObtenerReglaRedondeoSeleccionada();
        foreach (var item in _itemsComparados)
        {
            item.ReglaRedondeo = regla;
            item.Recalcular();
        }
        GridComparativa?.Items?.Refresh();
    }

    private PuntoDeVentaLibreria.Application.Common.ReglaRedondeoPrecio ObtenerReglaRedondeoSeleccionada()
    {
        if (CmbReglaRedondeo?.SelectedItem is ComboBoxItem cbi && cbi.Tag is string tag)
        {
            return tag switch
            {
                "CentenaSuperior" => PuntoDeVentaLibreria.Application.Common.ReglaRedondeoPrecio.CentenaSuperior,
                "CincuentaCercano" => PuntoDeVentaLibreria.Application.Common.ReglaRedondeoPrecio.CincuentaCercano,
                "SinRedondeo" => PuntoDeVentaLibreria.Application.Common.ReglaRedondeoPrecio.SinRedondeo,
                _ => PuntoDeVentaLibreria.Application.Common.ReglaRedondeoPrecio.CentenaCercana
            };
        }
        return PuntoDeVentaLibreria.Application.Common.ReglaRedondeoPrecio.CentenaCercana;
    }

    private async void BtnAplicarAumento_Click(object sender, RoutedEventArgs e)
    {
        var seleccionados = _itemsComparados.Where(i => i.Aplicar).ToList();
        if (!seleccionados.Any())
        {
            MessageBox.Show("No ha seleccionado ningún artículo para actualizar.", "MR SYS", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Validación de seguridad para artículos con variaciones extremas
        var itemsConAlertaExtrema = seleccionados.Where(i => i.EsAlertaVariacionExtrema).ToList();
        if (itemsConAlertaExtrema.Any())
        {
            var adv = MessageBox.Show(
                $"¡Atención! Hay {itemsConAlertaExtrema.Count} artículo(s) seleccionado(s) con una variación de costo extrema (> +80% o < -50%), que probablemente sean bultos o packs mayoristas sin dividir.\n\n" +
                "¿Está completamente seguro de continuar y actualizar estos precios con los valores actuales?",
                "Alerta de Variación Extrema de Precios",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (adv != MessageBoxResult.Yes) return;
        }

        Guid? asignarProveedorId = null;
        string nombreProveedor = string.Empty;
        if (ChkAsignarProveedor.IsChecked == true && CmbProveedor.SelectedItem is ProveedorDto p && p.Id != Guid.Empty)
        {
            asignarProveedorId = p.Id;
            nombreProveedor = p.Nombre;
        }

        string? asignarRubro = null;
        if (ChkAsignarRubro?.IsChecked == true && CmbAsignarRubro?.SelectedItem is ComboBoxItem cbiRubro)
        {
            asignarRubro = cbiRubro.Content?.ToString();
        }

        bool actualizarNombres = ChkActualizarNombreConDescripcionProveedor?.IsChecked == true;
        bool sincronizarSueltos = ChkSincronizarSueltos?.IsChecked == true;

        var mensajeConfirmacion = $"¿Confirma actualizar los precios de costo y venta de {seleccionados.Count:N0} artículos?";
        if (asignarProveedorId.HasValue)
        {
            mensajeConfirmacion += $"\n\n• Se vinculará el proveedor '{nombreProveedor}'.";
        }
        if (!string.IsNullOrWhiteSpace(asignarRubro))
        {
            mensajeConfirmacion += $"\n• Se asignará el rubro '{asignarRubro}'.";
        }
        if (actualizarNombres)
        {
            mensajeConfirmacion += "\n• Se reemplazarán los nombres locales por las descripciones del proveedor.";
        }
        if (sincronizarSueltos)
        {
            mensajeConfirmacion += "\n• Se sincronizarán automáticamente los artículos sueltos vinculados (-1) con el costo unitario del pack.";
        }

        var res = MessageBox.Show(
            mensajeConfirmacion,
            "Confirmar Actualización de Precios", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (res != MessageBoxResult.Yes) return;

        PbProgreso.Visibility = Visibility.Visible;
        BtnAplicarAumento.IsEnabled = false;
        BtnComparar.IsEnabled = false;

        try
        {
            var resultado = await _inventarioService.AplicarActualizacionPreciosAsync(seleccionados, asignarProveedorId, asignarRubro, actualizarNombres, sincronizarSueltos);
            PreciosActualizados = true;

            TxtMensajeResultado.Text = $"✅ {resultado.Mensaje}";
            TxtMensajeResultado.Foreground = System.Windows.Media.Brushes.DarkViolet;

            MessageBox.Show(
                resultado.Mensaje,
                "MR SYS - Precios Actualizados",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al aplicar precios: {ex.Message}", "MR SYS Error", MessageBoxButton.OK, MessageBoxImage.Error);
            BtnAplicarAumento.IsEnabled = true;
        }
        finally
        {
            PbProgreso.Visibility = Visibility.Collapsed;
            BtnComparar.IsEnabled = true;
        }
    }

    private void BtnCerrar_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
