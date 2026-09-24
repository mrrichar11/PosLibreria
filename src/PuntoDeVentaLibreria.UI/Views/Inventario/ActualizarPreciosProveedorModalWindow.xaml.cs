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
    private string _rutaArchivo = string.Empty;
    private List<ArticuloAumentoPrecioItemDto> _itemsComparados = new();
    private int _totalCatalogo;
    private int _noEncontrados;

    public bool PreciosActualizados { get; private set; }

    public ActualizarPreciosProveedorModalWindow(IInventarioService inventarioService, IProveedorService proveedorService)
    {
        InitializeComponent();
        _inventarioService = inventarioService ?? throw new ArgumentNullException(nameof(inventarioService));
        _proveedorService = proveedorService ?? throw new ArgumentNullException(nameof(proveedorService));

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

            GridComparativa.ItemsSource = _itemsComparados;
            TxtEstadoVacio.Visibility = _itemsComparados.Count > 0 ? Visibility.Collapsed : Visibility.Visible;

            PnlMetricas.Visibility = Visibility.Visible;
            ActualizarContadoresMetricas();

            RbFiltroTodos.IsChecked = true;
            AplicarFiltroVista("Todos");

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
        int total = _itemsComparados.Count;
        int conCambio = _itemsComparados.Count(i => !i.EsAlertaVariacionExtrema && Math.Abs(i.CostoNuevo - i.CostoAnterior) > 0.01m);
        int alertas = _itemsComparados.Count(i => i.EsAlertaVariacionExtrema);
        int sinCambio = _itemsComparados.Count(i => Math.Abs(i.CostoNuevo - i.CostoAnterior) <= 0.01m);

        TxtTotalCatalogo.Text = $"Catálogo Local: {_totalCatalogo:N0}";
        TxtCoincidencias.Text = $"Coincidentes: {total:N0}";
        TxtConCambio.Text = $"Con Variación: {conCambio:N0}";
        TxtConAlerta.Text = $"⚠️ Posibles Bultos/Packs: {alertas:N0}";
        TxtNoEncontrados.Text = $"Filas sin asociar en local: {_noEncontrados:N0}";

        BorderAlertas.Visibility = alertas > 0 ? Visibility.Visible : Visibility.Collapsed;

        // Textos de los filtros de radio
        RbFiltroTodos.Content = $"Mostrar Todos ({total:N0})";
        RbFiltroAlertas.Content = $"⚠️ Alertas / Posibles Packs ({alertas:N0})";
        RbFiltroConCambio.Content = $"📈 Variación Normal ({conCambio:N0})";
        RbFiltroSinCambio.Content = $"⏸️ Sin Cambios ({sinCambio:N0})";

        // Botón masivo para auto-aplicar sugerencias
        int sugeridosPendientes = _itemsComparados.Count(i => i.FactorSugerido.HasValue && i.MostrarBotonSugerido);
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

    private void FiltroRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.IsChecked == true)
        {
            if (rb == RbFiltroTodos) AplicarFiltroVista("Todos");
            else if (rb == RbFiltroAlertas) AplicarFiltroVista("Alertas");
            else if (rb == RbFiltroConCambio) AplicarFiltroVista("ConCambio");
            else if (rb == RbFiltroSinCambio) AplicarFiltroVista("SinCambio");
        }
    }

    private void AplicarFiltroVista(string tipoFiltro)
    {
        var view = CollectionViewSource.GetDefaultView(GridComparativa.ItemsSource);
        if (view == null) return;

        view.Filter = obj =>
        {
            if (obj is not ArticuloAumentoPrecioItemDto item) return false;

            return tipoFiltro switch
            {
                "Alertas" => item.EsAlertaVariacionExtrema,
                "ConCambio" => !item.EsAlertaVariacionExtrema && Math.Abs(item.CostoNuevo - item.CostoAnterior) > 0.01m,
                "SinCambio" => Math.Abs(item.CostoNuevo - item.CostoAnterior) <= 0.01m,
                _ => true
            };
        };
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

    private void BtnMarcarTodos_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in _itemsComparados)
        {
            // Solo marcar automáticamente los que tienen cambios y NO son alertas extremas
            if (!item.EsAlertaVariacionExtrema && Math.Abs(item.CostoNuevo - item.CostoAnterior) > 0.01m)
            {
                item.Aplicar = true;
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

        var mensajeConfirmacion = $"¿Confirma actualizar los precios de costo y venta de {seleccionados.Count:N0} artículos?";
        if (asignarProveedorId.HasValue)
        {
            mensajeConfirmacion += $"\n\nAdemás, se vinculará el proveedor '{nombreProveedor}' a los artículos actualizados.";
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
            var resultado = await _inventarioService.AplicarActualizacionPreciosAsync(seleccionados, asignarProveedorId);
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
