using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using PuntoDeVentaLibreria.Application.DTOs.Inventario;
using PuntoDeVentaLibreria.Application.DTOs.Proveedores;
using PuntoDeVentaLibreria.Application.Services;

namespace PuntoDeVentaLibreria.UI.Views.Inventario;

public partial class MigracionAlmaLibreModalWindow : Window
{
    private readonly IInventarioService _inventarioService;
    private readonly IProveedorService _proveedorService;
    private string _rutaArchivo = string.Empty;
    private List<ItemPrevisualizacionAlmaLibreDto> _itemsCompletos = new();
    private List<ItemPrevisualizacionAlmaLibreDto> _itemsFiltrados = new();
    public bool MigracionRealizada { get; private set; }

    public MigracionAlmaLibreModalWindow(IInventarioService inventarioService, IProveedorService proveedorService)
    {
        InitializeComponent();
        _inventarioService = inventarioService ?? throw new ArgumentNullException(nameof(inventarioService));
        _proveedorService = proveedorService ?? throw new ArgumentNullException(nameof(proveedorService));

        Loaded += async (s, e) =>
        {
            await CargarProveedoresAsync();
            DetectarArchivoPorDefecto();
        };
    }

    private void DetectarArchivoPorDefecto()
    {
        var candidatos = new[]
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Lista de precios ALMA LIBRE.xlsx"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Lista de precios ALMA LIBRE.xlsx"),
            @"C:\Proyectos\PuntoDeVentaLibreria\Lista de precios ALMA LIBRE.xlsx"
        };

        foreach (var c in candidatos)
        {
            if (File.Exists(c))
            {
                _rutaArchivo = Path.GetFullPath(c);
                TxtRutaArchivo.Text = _rutaArchivo;
                break;
            }
        }
    }

    private async Task CargarProveedoresAsync()
    {
        try
        {
            var proveedores = await _proveedorService.ObtenerTodosAsync();
            var lista = new List<ProveedorDto>
            {
                new ProveedorDto { Id = Guid.Empty, Nombre = "(Auto-asignar o Ninguno)" }
            };
            lista.AddRange(proveedores);

            CmbProveedor.ItemsSource = lista;
            CmbProveedor.SelectedIndex = 0;
        }
        catch { }
    }

    private void BtnExaminar_Click(object sender, RoutedEventArgs e)
    {
        var ofd = new OpenFileDialog
        {
            Filter = "Archivos de Excel (*.xlsx;*.xls)|*.xlsx;*.xls|Todos los archivos (*.*)|*.*",
            Title = "Seleccione la lista de precios anterior (ej. ALMA LIBRE.xlsx)"
        };

        if (ofd.ShowDialog() == true)
        {
            _rutaArchivo = ofd.FileName;
            TxtRutaArchivo.Text = _rutaArchivo;
            BtnAnalizar_Click(sender, e);
        }
    }

    private async void BtnAnalizar_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_rutaArchivo) || !File.Exists(_rutaArchivo))
        {
            MessageBox.Show("Por favor seleccione un archivo Excel válido primero.", "MR SYS", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        PbProgreso.Visibility = Visibility.Visible;
        BtnAnalizar.IsEnabled = false;
        BtnImportar.IsEnabled = false;
        TxtEstadoVacio.Text = "Analizando archivo Excel, cruzando con Mayorista El Once y verificando base de datos...";

        try
        {
            using var stream = File.OpenRead(_rutaArchivo);
            var items = await _inventarioService.PrevisualizarCatalogoAlmaLibreAsync(stream);

            _itemsCompletos = items.ToList();
            AplicarFiltros();

            TxtEstadoVacio.Visibility = _itemsCompletos.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
            PnlMetricas.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al analizar el archivo Excel:\n\n{ex.Message}", "MR SYS Error", MessageBoxButton.OK, MessageBoxImage.Error);
            TxtEstadoVacio.Text = "Ocurrió un error al procesar el archivo.";
        }
        finally
        {
            PbProgreso.Visibility = Visibility.Collapsed;
            BtnAnalizar.IsEnabled = true;
        }
    }

    private void Filtros_Changed(object sender, RoutedEventArgs e)
    {
        if (_itemsCompletos == null || _itemsCompletos.Count == 0) return;
        AplicarFiltros();
    }

    private void AplicarFiltros()
    {
        if (_itemsCompletos == null) return;

        var q = _itemsCompletos.AsEnumerable();

        // 1. Filtro de Texto
        var texto = TxtFiltroTexto?.Text?.Trim();
        if (!string.IsNullOrWhiteSpace(texto))
        {
            q = q.Where(i =>
                (i.Nombre != null && i.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase)) ||
                (i.SKU != null && i.SKU.Contains(texto, StringComparison.OrdinalIgnoreCase)) ||
                (i.CodigoProveedor != null && i.CodigoProveedor.Contains(texto, StringComparison.OrdinalIgnoreCase)) ||
                (i.CodigoBarras != null && i.CodigoBarras.Contains(texto, StringComparison.OrdinalIgnoreCase)) ||
                (i.CategoriaRubro != null && i.CategoriaRubro.Contains(texto, StringComparison.OrdinalIgnoreCase)));
        }

        // 2. Filtro Fecha de Carga (fchalta)
        if (DpFechaAltaDesde?.SelectedDate.HasValue == true)
        {
            var fechaMin = DpFechaAltaDesde.SelectedDate.Value.Date;
            q = q.Where(i => i.FechaAlta.HasValue && i.FechaAlta.Value.Date >= fechaMin);
        }

        // 3. Filtro Última Actualización de Precio (fchultpre)
        if (DpFechaPrecioDesde?.SelectedDate.HasValue == true)
        {
            var fechaMin = DpFechaPrecioDesde.SelectedDate.Value.Date;
            q = q.Where(i => i.FechaUltimaActualizacionPrecio.HasValue && i.FechaUltimaActualizacionPrecio.Value.Date >= fechaMin);
        }

        // 4. Filtro Proveedor / Once / Estado
        if (CmbFiltroProveedor != null && CmbFiltroProveedor.SelectedIndex > 0)
        {
            switch (CmbFiltroProveedor.SelectedIndex)
            {
                case 1: // Solo Mayorista El Once
                    q = q.Where(i => i.EsDeMayoristaElOnce);
                    break;
                case 2: // Otros artículos (No Once)
                    q = q.Where(i => !i.EsDeMayoristaElOnce);
                    break;
                case 3: // Solo Nuevos (No importados)
                    q = q.Where(i => !i.YaExisteEnSistema);
                    break;
                case 4: // Solo Ya Importados
                    q = q.Where(i => i.YaExisteEnSistema);
                    break;
            }
        }

        _itemsFiltrados = q.ToList();
        GridPreview.ItemsSource = _itemsFiltrados;

        ActualizarMetricas();
    }

    private void ActualizarMetricas()
    {
        int total = _itemsCompletos.Count;
        int filtrados = _itemsFiltrados.Count;
        int onceTotal = _itemsCompletos.Count(i => i.EsDeMayoristaElOnce);
        int seleccionados = _itemsCompletos.Count(i => i.Seleccionado);
        decimal stockTotal = _itemsCompletos.Where(i => i.Seleccionado).Sum(i => i.StockImportar);

        TxtTotalFilas.Text = $"Filtrados: {filtrados:N0} / {total:N0}";
        TxtOnceCount.Text = $"🏷️ El Once: {onceTotal:N0}";
        TxtSeleccionadosCount.Text = $"Seleccionados: {seleccionados:N0} (Stock: {stockTotal:N0})";

        BtnImportar.IsEnabled = seleccionados > 0;
        BtnImportar.Content = $"🚀 Comenzar Importación Seleccionados ({seleccionados:N0} artículos)";
    }

    private void BtnLimpiarFiltros_Click(object sender, RoutedEventArgs e)
    {
        if (TxtFiltroTexto != null) TxtFiltroTexto.Text = string.Empty;
        if (DpFechaAltaDesde != null) DpFechaAltaDesde.SelectedDate = null;
        if (DpFechaPrecioDesde != null) DpFechaPrecioDesde.SelectedDate = null;
        if (CmbFiltroProveedor != null) CmbFiltroProveedor.SelectedIndex = 0;
        AplicarFiltros();
    }

    private void ChkSeleccionarTodoHeader_Click(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox chk)
        {
            bool isChecked = chk.IsChecked == true;
            foreach (var item in _itemsFiltrados)
            {
                item.Seleccionado = isChecked;
            }
            GridPreview.Items.Refresh();
            ActualizarMetricas();
        }
    }

    private void BtnSeleccionarTodos_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in _itemsFiltrados)
        {
            item.Seleccionado = true;
        }
        if (ChkSeleccionarTodoHeader != null) ChkSeleccionarTodoHeader.IsChecked = true;
        GridPreview.Items.Refresh();
        ActualizarMetricas();
    }

    private void BtnDeseleccionarTodos_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in _itemsFiltrados)
        {
            item.Seleccionado = false;
        }
        if (ChkSeleccionarTodoHeader != null) ChkSeleccionarTodoHeader.IsChecked = false;
        GridPreview.Items.Refresh();
        ActualizarMetricas();
    }

    private void BtnAplicarStockMasivo_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(TxtStockMasivo.Text.Trim().Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var stockVal) || stockVal < 0)
        {
            MessageBox.Show("Ingrese una cantidad de stock válida (número positivo).", "MR SYS", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var seleccionados = _itemsFiltrados.Where(i => i.Seleccionado).ToList();
        if (seleccionados.Count == 0)
        {
            MessageBox.Show("No hay artículos seleccionados en la vista actual.", "MR SYS", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        foreach (var item in seleccionados)
        {
            item.StockImportar = stockVal;
        }

        GridPreview.Items.Refresh();
        ActualizarMetricas();
        MessageBox.Show($"Se asignó stock de {stockVal:G29} unidades a {seleccionados.Count:N0} artículos seleccionados.", "MR SYS", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ChkItem_Checked(object sender, RoutedEventArgs e)
    {
        ActualizarMetricas();
    }

    private void ChkItem_Unchecked(object sender, RoutedEventArgs e)
    {
        ActualizarMetricas();
    }

    private async void BtnImportar_Click(object sender, RoutedEventArgs e)
    {
        var seleccionados = _itemsCompletos.Where(i => i.Seleccionado).ToList();
        if (seleccionados.Count == 0)
        {
            MessageBox.Show("No ha seleccionado ningún artículo para importar.", "MR SYS", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        decimal totalStock = seleccionados.Sum(i => i.StockImportar);
        var res = MessageBox.Show(
            $"¿Confirma la importación de {seleccionados.Count:N0} artículos seleccionados?\n\n" +
            $"• Total Stock inicial a ingresar: {totalStock:N0} unidades\n" +
            $"• Los artículos del Mayorista El Once se asociarán automáticamente.\n" +
            $"• Los existentes se actualizarán con los costos, precios y stock ingresado.",
            "Confirmar Importación Selectiva", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (res != MessageBoxResult.Yes) return;

        PbProgreso.Visibility = Visibility.Visible;
        BtnImportar.IsEnabled = false;
        BtnAnalizar.IsEnabled = false;

        Guid? proveedorId = null;
        if (CmbProveedor.SelectedItem is ProveedorDto p && p.Id != Guid.Empty)
        {
            proveedorId = p.Id;
        }

        try
        {
            var resultado = await _inventarioService.ImportarCatalogoSeleccionadoAsync(seleccionados, proveedorId);

            MigracionRealizada = true;
            TxtMensajeResultado.Text = $"✅ Creados: {resultado.ArticulosCreados:N0} | Actualizados: {resultado.ArticulosActualizados:N0} | Stock cargado: {resultado.TotalStockIngresado:N0} un.";
            TxtMensajeResultado.Foreground = System.Windows.Media.Brushes.Green;

            GridPreview.Items.Refresh();
            ActualizarMetricas();

            MessageBox.Show(
                $"¡Importación completada con éxito!\n\n" +
                $"• Artículos seleccionados procesados: {resultado.TotalFilasProcesadas:N0}\n" +
                $"• Nuevos artículos cargados: {resultado.ArticulosCreados:N0}\n" +
                $"• Artículos actualizados: {resultado.ArticulosActualizados:N0}\n" +
                $"• Total Stock inicial ingresado: {resultado.TotalStockIngresado:N0} unidades\n" +
                $"• Errores / omitidos: {resultado.Errores:N0}",
                "MR SYS - Migración Exitosa",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error durante la importación:\n\n{ex.Message}", "MR SYS Error", MessageBoxButton.OK, MessageBoxImage.Error);
            BtnImportar.IsEnabled = true;
        }
        finally
        {
            PbProgreso.Visibility = Visibility.Collapsed;
            BtnAnalizar.IsEnabled = true;
        }
    }

    private void BtnCerrar_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
