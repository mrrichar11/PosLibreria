using System.Windows;
using PuntoDeVentaLibreria.Application.DTOs.Inventario;
using PuntoDeVentaLibreria.Application.Services;

namespace PuntoDeVentaLibreria.UI.Views.Inventario;

public partial class ArticuloModalWindow : Window
{
    private readonly IInventarioService _inventarioService;
    public ArticuloDto Articulo { get; }
    public bool GuardadoExitoso { get; private set; }

    public ArticuloModalWindow(ArticuloDto articulo, IInventarioService inventarioService)
    {
        InitializeComponent();
        Articulo = articulo ?? throw new ArgumentNullException(nameof(articulo));
        _inventarioService = inventarioService ?? throw new ArgumentNullException(nameof(inventarioService));
        DataContext = Articulo;

        if (string.IsNullOrEmpty(Articulo.ColorBoton))
        {
            Articulo.ColorBoton = "#3B82F6";
        }

        TxtNombre.Focus();
    }

    private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Articulo.Nombre))
        {
            MessageBox.Show("El nombre del artículo es obligatorio.", "MR SYS", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtNombre.Focus();
            return;
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
            MessageBox.Show($"Error al guardar artículo: {ex.Message}", "MR SYS Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
