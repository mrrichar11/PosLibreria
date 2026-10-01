using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using PuntoDeVentaLibreria.Application.Services;

namespace PuntoDeVentaLibreria.UI.Views.Inventario;

public partial class ConsultaListaAnteriorModalWindow : Window
{
    private readonly IInventarioService _inventarioService;

    public ConsultaListaAnteriorModalWindow(IInventarioService inventarioService)
    {
        InitializeComponent();
        _inventarioService = inventarioService ?? throw new ArgumentNullException(nameof(inventarioService));

        Loaded += ConsultaListaAnteriorModalWindow_Loaded;
        PreviewKeyDown += (s, e) =>
        {
            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
        };
    }

    public void PreestablecerBusqueda(string query)
    {
        TxtBuscar.Text = query;
    }

    private async void ConsultaListaAnteriorModalWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await ActualizarContadorYBuscarAsync();
        TxtBuscar.Focus();
        if (!string.IsNullOrWhiteSpace(TxtBuscar.Text))
        {
            TxtBuscar.SelectAll();
        }
    }

    private async Task ActualizarContadorYBuscarAsync()
    {
        try
        {
            int total = await _inventarioService.ObtenerTotalRegistrosListaAnteriorAsync();
            TxtTotalRegistros.Text = $"{total} productos";

            // Si está vacía y existe el archivo descargado por defecto en la máquina, ofrecer cargarlo
            if (total == 0)
            {
                string defaultPath = @"C:\Users\Fliac\Downloads\PRECIOS LISTA SISTEMA.xlsx";
                if (File.Exists(defaultPath))
                {
                    var res = MessageBox.Show(
                        $"Se detectó la lista original exportada en:\n'{defaultPath}'\n\n¿Desea importarla al repositorio de resguardo de solo lectura ahora?",
                        "Cargar Lista Histórica Automáticamente",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (res == MessageBoxResult.Yes)
                    {
                        await ImportarArchivoDesdeRutaAsync(defaultPath);
                        return;
                    }
                }
            }

            await EjecutarBusquedaAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar la lista histórica: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task EjecutarBusquedaAsync()
    {
        try
        {
            Mouse.OverrideCursor = Cursors.Wait;
            var query = TxtBuscar.Text?.Trim() ?? string.Empty;
            var items = await _inventarioService.BuscarEnListaSistemaAnteriorAsync(query);
            GridHistorico.ItemsSource = items;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error en la búsqueda: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private async void BtnBuscar_Click(object sender, RoutedEventArgs e)
    {
        await EjecutarBusquedaAsync();
    }

    private async void TxtBuscar_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            await EjecutarBusquedaAsync();
            e.Handled = true;
        }
    }

    private async void TxtBuscar_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        // Si borra el texto, refrescar los primeros 100
        if (string.IsNullOrWhiteSpace(TxtBuscar.Text))
        {
            await EjecutarBusquedaAsync();
        }
    }

    private async void BtnCargarExcel_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Seleccionar Excel Exportado del Sistema Anterior",
            Filter = "Archivos Excel (*.xlsx)|*.xlsx|Todos los archivos (*.*)|*.*",
            InitialDirectory = @"C:\Users\Fliac\Downloads"
        };

        if (dialog.ShowDialog() == true)
        {
            await ImportarArchivoDesdeRutaAsync(dialog.FileName);
        }
    }

    private async Task ImportarArchivoDesdeRutaAsync(string filePath)
    {
        try
        {
            Mouse.OverrideCursor = Cursors.Wait;
            using var fileStream = File.OpenRead(filePath);
            string fileName = Path.GetFileName(filePath);
            int importados = await _inventarioService.ImportarListaSistemaAnteriorAsync(fileStream, fileName);

            int total = await _inventarioService.ObtenerTotalRegistrosListaAnteriorAsync();
            TxtTotalRegistros.Text = $"{total} productos";

            MessageBox.Show(
                $"Se importaron exitosamente {importados} productos desde '{fileName}' al repositorio histórico de solo lectura.",
                "Importación Exitosa",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            await EjecutarBusquedaAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al importar el archivo Excel: {ex.Message}", "Error de Importación", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private void BtnCerrar_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
