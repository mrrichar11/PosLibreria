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
    private List<string> _columnasDetectadas = new();
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
            Filter = "Planillas y Catálogos (*.xlsx;*.xls;*.csv)|*.xlsx;*.xls;*.csv|Todos los archivos (*.*)|*.*",
            Title = "Seleccione el archivo de catálogo o lista de precios a importar"
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
        await ProcesarAnalisisAsync(null);
    }

    private async void BtnReanalizarConMapeo_Click(object sender, RoutedEventArgs e)
    {
        var customMapeo = new MapeoColumnasExcelDto
        {
            ColumnaCodigoBarras = ObtenerColumnaSeleccionada(CmbColCodigoBarras),
            ColumnaSku = ObtenerColumnaSeleccionada(CmbColSku),
            ColumnaDescripcion = ObtenerColumnaSeleccionada(CmbColDescripcion),
            ColumnaPrecioVenta = ObtenerColumnaSeleccionada(CmbColPrecioVenta),
            ColumnaPrecioCosto = ObtenerColumnaSeleccionada(CmbColPrecioCosto),
            ColumnaPrecioTarjeta = ObtenerColumnaSeleccionada(CmbColPrecioTarjeta),
            ColumnaStock = ObtenerColumnaSeleccionada(CmbColStock),
            ColumnaCategoria = ObtenerColumnaSeleccionada(CmbColCategoria),
            ColumnaMarca = ObtenerColumnaSeleccionada(CmbColMarca),
            ColumnaCodigoProveedor = ObtenerColumnaSeleccionada(CmbColCodProv),
            ColumnaFechaAlta = ObtenerColumnaSeleccionada(CmbColFechaAlta)
        };

        await ProcesarAnalisisAsync(customMapeo);
    }

    private async Task ProcesarAnalisisAsync(MapeoColumnasExcelDto? mapeoPersonalizado)
    {
        if (string.IsNullOrWhiteSpace(_rutaArchivo) || !File.Exists(_rutaArchivo))
        {
            MessageBox.Show("Por favor seleccione un archivo Excel válido primero.", "MR SYS", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        PbProgreso.Visibility = Visibility.Visible;
        BtnAnalizar.IsEnabled = false;
        BtnImportar.IsEnabled = false;
        TxtEstadoVacio.Text = "Analizando columnas y registros del archivo...";

        try
        {
            using var stream = File.OpenRead(_rutaArchivo);
            var analisis = await _inventarioService.AnalizarExcelGenericoAsync(stream, mapeoPersonalizado);

            _columnasDetectadas = analisis.ColumnasDetectadas;
            _itemsCompletos = analisis.Items;

            // Actualizar combos de mapeo
            PoblarCombosMapeo(_columnasDetectadas, analisis.MapeoSugerido);

            AplicarFiltros();

            TxtEstadoVacio.Visibility = _itemsCompletos.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
            PnlMetricas.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al analizar el archivo:\n\n{ex.Message}", "MR SYS Error", MessageBoxButton.OK, MessageBoxImage.Error);
            TxtEstadoVacio.Text = "Ocurrió un error al procesar el archivo.";
        }
        finally
        {
            PbProgreso.Visibility = Visibility.Collapsed;
            BtnAnalizar.IsEnabled = true;
        }
    }

    private void PoblarCombosMapeo(List<string> columnas, MapeoColumnasExcelDto mapeo)
    {
        var opciones = new List<string> { "(Ninguna / No usar)" };
        opciones.AddRange(columnas);

        ConfigurarCombo(CmbColCodigoBarras, opciones, mapeo.ColumnaCodigoBarras);
        ConfigurarCombo(CmbColSku, opciones, mapeo.ColumnaSku);
        ConfigurarCombo(CmbColDescripcion, opciones, mapeo.ColumnaDescripcion);
        ConfigurarCombo(CmbColPrecioVenta, opciones, mapeo.ColumnaPrecioVenta);
        ConfigurarCombo(CmbColPrecioCosto, opciones, mapeo.ColumnaPrecioCosto);
        ConfigurarCombo(CmbColPrecioTarjeta, opciones, mapeo.ColumnaPrecioTarjeta);
        ConfigurarCombo(CmbColStock, opciones, mapeo.ColumnaStock);
        ConfigurarCombo(CmbColCategoria, opciones, mapeo.ColumnaCategoria);
        ConfigurarCombo(CmbColMarca, opciones, mapeo.ColumnaMarca);
        ConfigurarCombo(CmbColCodProv, opciones, mapeo.ColumnaCodigoProveedor);
        ConfigurarCombo(CmbColFechaAlta, opciones, mapeo.ColumnaFechaAlta);

        ExpMapeo.Header = $"⚙️ Mapeo Inteligente de Columnas ({columnas.Count} columnas detectadas en el archivo)";
    }

    private static void ConfigurarCombo(ComboBox combo, List<string> opciones, string? seleccionado)
    {
        combo.ItemsSource = opciones;
        if (!string.IsNullOrEmpty(seleccionado) && opciones.Contains(seleccionado))
        {
            combo.SelectedItem = seleccionado;
        }
        else
        {
            combo.SelectedIndex = 0;
        }
    }

    private static string? ObtenerColumnaSeleccionada(ComboBox combo)
    {
        var sel = combo.SelectedItem as string;
        if (string.IsNullOrWhiteSpace(sel) || sel.StartsWith("(")) return null;
        return sel;
    }

    private async void BtnDescargarPlantilla_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var sfd = new SaveFileDialog
            {
                Title = "Guardar Plantilla Modelo de Catálogo Excel",
                Filter = "Archivo CSV compatible con Excel (*.csv)|*.csv|Todos los archivos (*.*)|*.*",
                FileName = "Plantilla_Catalogo_MR_SYS.csv"
            };

            if (sfd.ShowDialog() == true)
            {
                var bytes = await _inventarioService.GenerarPlantillaExcelModeloAsync();
                await File.WriteAllBytesAsync(sfd.FileName, bytes);

                var r = MessageBox.Show(
                    $"¡Plantilla descargada con éxito en:\n{sfd.FileName}\n\n¿Desea abrir la carpeta contenedora?",
                    "Plantilla Generada",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (r == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = Path.GetDirectoryName(sfd.FileName)!,
                        UseShellExecute = true
                    });
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al generar plantilla: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Filtros_Changed(object sender, RoutedEventArgs e)
    {
        AplicarFiltros();
    }

    private void AplicarFiltros()
    {
        if (_itemsCompletos == null || _itemsCompletos.Count == 0) return;

        var criterio = TxtFiltroTexto?.Text?.Trim().ToLowerInvariant() ?? "";
        var fechaAltaDesde = DpFechaAltaDesde?.SelectedDate;
        var fechaPrecioDesde = DpFechaPrecioDesde?.SelectedDate;
        var filtroProvIdx = CmbFiltroProveedor?.SelectedIndex ?? 0;

        _itemsFiltrados = _itemsCompletos.Where(i =>
        {
            // 1. Filtro de Texto
            if (!string.IsNullOrEmpty(criterio))
            {
                bool coincide = (i.Nombre?.ToLowerInvariant().Contains(criterio) == true) ||
                                (i.SKU?.ToLowerInvariant().Contains(criterio) == true) ||
                                (i.CodigoProveedor?.ToLowerInvariant().Contains(criterio) == true) ||
                                (i.CodigoBarras?.ToLowerInvariant().Contains(criterio) == true) ||
                                (i.CategoriaRubro?.ToLowerInvariant().Contains(criterio) == true);
                if (!coincide) return false;
            }

            // 2. Filtro Fecha de Carga
            if (fechaAltaDesde.HasValue)
            {
                if (!i.FechaAlta.HasValue || i.FechaAlta.Value.Date < fechaAltaDesde.Value.Date)
                    return false;
            }

            // 3. Filtro Fecha Último Precio
            if (fechaPrecioDesde.HasValue)
            {
                if (!i.FechaUltimaActualizacionPrecio.HasValue || i.FechaUltimaActualizacionPrecio.Value.Date < fechaPrecioDesde.Value.Date)
                    return false;
            }

            // 4. Filtro Proveedor / Origen
            switch (filtroProvIdx)
            {
                case 1: // Solo El Once
                    if (!i.EsDeMayoristaElOnce) return false;
                    break;
                case 2: // Otros
                    if (i.EsDeMayoristaElOnce) return false;
                    break;
                case 3: // Solo Nuevos
                    if (i.EsYaImportado) return false;
                    break;
                case 4: // Solo Ya Importados
                    if (!i.EsYaImportado) return false;
                    break;
            }

            return true;
        }).ToList();

        DgPrevisualizacion.ItemsSource = null;
        DgPrevisualizacion.ItemsSource = _itemsFiltrados;

        ActualizarMetricas();
    }

    private void ActualizarMetricas()
    {
        var total = _itemsCompletos.Count;
        var filtrados = _itemsFiltrados.Count;
        var seleccionados = _itemsFiltrados.Count(i => i.Seleccionado);
        var once = _itemsFiltrados.Count(i => i.EsDeMayoristaElOnce);

        TxtTotalFilas.Text = $"Filtrados: {filtrados:N0} / {total:N0}";
        TxtOnceCount.Text = $"🏷️ El Once: {once:N0}";
        TxtSeleccionadosCount.Text = $"Seleccionados: {seleccionados:N0}";

        BtnImportar.IsEnabled = seleccionados > 0;
        BtnImportar.Content = $"🚀 Importar {seleccionados:N0} Artículos Seleccionados al Inventario";
    }

    private void BtnSeleccionarTodos_Click(object sender, RoutedEventArgs e)
    {
        foreach (var i in _itemsFiltrados) i.Seleccionado = true;
        DgPrevisualizacion.Items.Refresh();
        ActualizarMetricas();
    }

    private void BtnDeseleccionarTodos_Click(object sender, RoutedEventArgs e)
    {
        foreach (var i in _itemsFiltrados) i.Seleccionado = false;
        DgPrevisualizacion.Items.Refresh();
        ActualizarMetricas();
    }

    private void BtnAplicarStockMasivo_Click(object sender, RoutedEventArgs e)
    {
        if (decimal.TryParse(TxtStockMasivo.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var stock))
        {
            int aplicados = 0;
            foreach (var i in _itemsFiltrados.Where(x => x.Seleccionado))
            {
                i.StockImportar = stock;
                aplicados++;
            }
            DgPrevisualizacion.Items.Refresh();
            MessageBox.Show($"Se aplicó un stock inicial de {stock:N0} unidades a los {aplicados} artículos seleccionados.", "Stock Aplicado", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show("Por favor ingrese un valor numérico válido para el stock.", "Valor Inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BtnLimpiarFiltros_Click(object sender, RoutedEventArgs e)
    {
        TxtFiltroTexto.Text = string.Empty;
        DpFechaAltaDesde.SelectedDate = null;
        DpFechaPrecioDesde.SelectedDate = null;
        CmbFiltroProveedor.SelectedIndex = 0;
        AplicarFiltros();
    }

    private async void BtnImportar_Click(object sender, RoutedEventArgs e)
    {
        var seleccionados = _itemsCompletos.Where(i => i.Seleccionado).ToList();
        if (seleccionados.Count == 0)
        {
            MessageBox.Show("No hay ningún artículo seleccionado para importar.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            $"¿Confirma la importación de {seleccionados.Count:N0} artículos al inventario?\n\nLos artículos existentes se actualizarán con los precios y stock cargados, y los nuevos se registrarán con su rubro y código correspondiente.",
            "Confirmar Importación Masiva",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        PbProgreso.Visibility = Visibility.Visible;
        BtnImportar.IsEnabled = false;
        BtnAnalizar.IsEnabled = false;

        try
        {
            Guid? provId = null;
            if (CmbProveedor.SelectedValue is Guid id && id != Guid.Empty)
            {
                provId = id;
            }

            var resultado = await _inventarioService.ImportarCatalogoSeleccionadoAsync(seleccionados, provId);

            MessageBox.Show(
                $"¡IMPORTACIÓN COMPLETADA CON ÉXITO!\n\n" +
                $"• Total Artículos Nuevos Creados: {resultado.ArticulosCreados:N0}\n" +
                $"• Artículos Existentes Actualizados: {resultado.ArticulosActualizados:N0}\n" +
                $"• Rubros / Categorías Creadas: {resultado.CategoriasCreadas:N0}\n\n" +
                $"Todos los productos ya están disponibles en el mostrador para venta y control de stock.",
                "Importación Exitosa",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            MigracionRealizada = true;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error durante la importación:\n\n{ex.Message}", "MR SYS Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            PbProgreso.Visibility = Visibility.Collapsed;
            BtnImportar.IsEnabled = true;
            BtnAnalizar.IsEnabled = true;
        }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
