using System.IO;
using System.Windows;
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
    public bool PreciosActualizados { get; private set; }

    public ActualizarPreciosProveedorModalWindow(IInventarioService inventarioService, IProveedorService proveedorService)
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
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lista de precio proveedor El Once_2026-09-17 (Con Cod.Barra).xls"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "lista de precio proveedor El Once_2026-09-17 (Con Cod.Barra).xls"),
            @"C:\Proyectos\PuntoDeVentaLibreria\lista de precio proveedor El Once_2026-09-17 (Con Cod.Barra).xls",
            @"C:\Proyectos\PuntoDeVentaLibreria\lista de precio proveedor El Once_2026-09-17 (Con Cod.Producto).xls"
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
                new ProveedorDto { Id = Guid.Empty, Nombre = "(Todos los proveedores)" }
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
            Filter = "Listas de Precios (*.xls;*.xlsx)|*.xls;*.xlsx|Todos los archivos (*.*)|*.*",
            Title = "Seleccione la lista de precios descargada del proveedor (ej. El Once)"
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

        Guid? proveedorId = null;
        if (CmbProveedor.SelectedItem is ProveedorDto p && p.Id != Guid.Empty)
        {
            proveedorId = p.Id;
        }

        try
        {
            using var stream = File.OpenRead(_rutaArchivo);
            var resumen = await _inventarioService.PrevisualizarActualizacionPreciosProveedorAsync(stream, proveedorId);

            _itemsComparados = resumen.ItemsParaActualizar;
            GridComparativa.ItemsSource = _itemsComparados;
            TxtEstadoVacio.Visibility = _itemsComparados.Count > 0 ? Visibility.Collapsed : Visibility.Visible;

            TxtCoincidencias.Text = $"Artículos Coincidentes: {resumen.CoincidenciasEncontradas:N0}";
            TxtConCambio.Text = $"Con Variación de Precio: {resumen.CoincidenciasConCambioDePrecio:N0}";
            TxtNoEncontrados.Text = $"No Encontrados en Local: {resumen.NoEncontradosEnCatalogo:N0}";
            PnlMetricas.Visibility = Visibility.Visible;

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

    private void BtnMarcarTodos_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in _itemsComparados)
        {
            item.Aplicar = Math.Abs(item.CostoNuevo - item.CostoAnterior) > 0.01m;
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

        var res = MessageBox.Show(
            $"¿Confirma actualizar los precios de costo y venta de {seleccionados.Count:N0} artículos en su catálogo?",
            "Confirmar Actualización de Precios", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (res != MessageBoxResult.Yes) return;

        PbProgreso.Visibility = Visibility.Visible;
        BtnAplicarAumento.IsEnabled = false;
        BtnComparar.IsEnabled = false;

        try
        {
            var resultado = await _inventarioService.AplicarActualizacionPreciosAsync(seleccionados);
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
