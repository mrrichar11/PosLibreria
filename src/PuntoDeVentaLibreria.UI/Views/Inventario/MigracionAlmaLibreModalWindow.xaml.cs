using System.IO;
using System.Windows;
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
        // Buscar automáticamente si 'Lista de precios ALMA LIBRE.xlsx' existe en el directorio base o workspace
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
                new ProveedorDto { Id = Guid.Empty, Nombre = "(Ninguno / Sin asignar)" }
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
        TxtEstadoVacio.Text = "Analizando archivo Excel y verificando catálogo existente...";

        try
        {
            using var stream = File.OpenRead(_rutaArchivo);
            var items = await _inventarioService.PrevisualizarCatalogoAlmaLibreAsync(stream);

            GridPreview.ItemsSource = items;
            TxtEstadoVacio.Visibility = items.Count > 0 ? Visibility.Collapsed : Visibility.Visible;

            int total = items.Count;
            int existentes = items.Count(i => i.YaExisteEnSistema);
            int nuevos = total - existentes;

            TxtTotalFilas.Text = $"Total Artículos: {total:N0}";
            TxtNuevos.Text = $"Artículos Nuevos: {nuevos:N0}";
            TxtActualizables.Text = $"Coincidentes a Actualizar: {existentes:N0}";
            PnlMetricas.Visibility = Visibility.Visible;

            BtnImportar.IsEnabled = total > 0;
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

    private async void BtnImportar_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_rutaArchivo) || !File.Exists(_rutaArchivo))
        {
            return;
        }

        var res = MessageBox.Show(
            "¿Confirma la importación masiva del catálogo al sistema?\n\nLos artículos nuevos se crearán y los existentes se actualizarán con los costos y precios del Excel.",
            "Confirmar Importación", MessageBoxButton.YesNo, MessageBoxImage.Question);

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
            using var stream = File.OpenRead(_rutaArchivo);
            var resultado = await _inventarioService.ImportarCatalogoAlmaLibreAsync(stream, proveedorId);

            MigracionRealizada = true;
            TxtMensajeResultado.Text = $"✅ ¡Éxito! Creados: {resultado.ArticulosCreados:N0} | Actualizados: {resultado.ArticulosActualizados:N0}";
            TxtMensajeResultado.Foreground = System.Windows.Media.Brushes.Green;

            MessageBox.Show(
                $"¡Importación completada con éxito!\n\n" +
                $"• Total de filas leídas: {resultado.TotalFilasProcesadas:N0}\n" +
                $"• Nuevos artículos cargados: {resultado.ArticulosCreados:N0}\n" +
                $"• Artículos actualizados: {resultado.ArticulosActualizados:N0}\n" +
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
