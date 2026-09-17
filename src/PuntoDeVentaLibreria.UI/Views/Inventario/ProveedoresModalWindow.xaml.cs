using System.Windows;
using System.Windows.Controls;
using PuntoDeVentaLibreria.Application.DTOs.Proveedores;
using PuntoDeVentaLibreria.Application.Services;

namespace PuntoDeVentaLibreria.UI.Views.Inventario;

public partial class ProveedoresModalWindow : Window
{
    private readonly IProveedorService _proveedorService;
    private ProveedorDto? _proveedorSeleccionado;

    public ProveedoresModalWindow(IProveedorService proveedorService)
    {
        InitializeComponent();
        _proveedorService = proveedorService ?? throw new ArgumentNullException(nameof(proveedorService));
        Loaded += async (s, e) => await CargarProveedoresAsync();
    }

    private async Task CargarProveedoresAsync()
    {
        try
        {
            var lista = await _proveedorService.ObtenerTodosAsync();
            GridProveedores.ItemsSource = lista;

            if (_proveedorSeleccionado != null)
            {
                var match = lista.FirstOrDefault(p => p.Id == _proveedorSeleccionado.Id);
                GridProveedores.SelectedItem = match;
            }
            else if (lista.Count > 0)
            {
                GridProveedores.SelectedIndex = 0;
            }
            else
            {
                LimpiarFormulario();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar proveedores: {ex.Message}", "MR SYS Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void GridProveedores_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GridProveedores.SelectedItem is ProveedorDto p)
        {
            _proveedorSeleccionado = p;
            TxtTituloForm.Text = $"EDITAR: {p.Nombre.ToUpper()}";
            TxtNombre.Text = p.Nombre;
            TxtRazonSocial.Text = p.RazonSocial ?? "";
            TxtCuit.Text = p.Cuit ?? "";
            TxtContacto.Text = p.Contacto ?? "";
            TxtTelefono.Text = p.Telefono ?? "";
            TxtEmail.Text = p.Email ?? "";
            TxtDiasVisita.Text = p.DiasVisitaOEntrega ?? "";
            TxtDireccion.Text = p.Direccion ?? "";
            TxtNotas.Text = p.Notas ?? "";
            BtnEliminar.Visibility = Visibility.Visible;
        }
    }

    private void BtnNuevo_Click(object sender, RoutedEventArgs e)
    {
        LimpiarFormulario();
    }

    private void LimpiarFormulario()
    {
        _proveedorSeleccionado = null;
        GridProveedores.SelectedItem = null;
        TxtTituloForm.Text = "NUEVO PROVEEDOR";
        TxtNombre.Clear();
        TxtRazonSocial.Clear();
        TxtCuit.Clear();
        TxtContacto.Clear();
        TxtTelefono.Clear();
        TxtEmail.Clear();
        TxtDiasVisita.Clear();
        TxtDireccion.Clear();
        TxtNotas.Clear();
        BtnEliminar.Visibility = Visibility.Collapsed;
        TxtNombre.Focus();
    }

    private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
    {
        var nombre = TxtNombre.Text.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
        {
            MessageBox.Show("El nombre comercial del proveedor es obligatorio.", "MR SYS", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtNombre.Focus();
            return;
        }

        var dto = _proveedorSeleccionado ?? new ProveedorDto();
        dto.Nombre = nombre;
        dto.RazonSocial = TxtRazonSocial.Text.Trim();
        dto.Cuit = TxtCuit.Text.Trim();
        dto.Contacto = TxtContacto.Text.Trim();
        dto.Telefono = TxtTelefono.Text.Trim();
        dto.Email = TxtEmail.Text.Trim();
        dto.DiasVisitaOEntrega = TxtDiasVisita.Text.Trim();
        dto.Direccion = TxtDireccion.Text.Trim();
        dto.Notas = TxtNotas.Text.Trim();

        try
        {
            await _proveedorService.GuardarAsync(dto);
            MessageBox.Show("Proveedor guardado correctamente.", "MR SYS", MessageBoxButton.OK, MessageBoxImage.Information);
            _proveedorSeleccionado = dto;
            await CargarProveedoresAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al guardar: {ex.Message}", "MR SYS Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnEliminar_Click(object sender, RoutedEventArgs e)
    {
        if (_proveedorSeleccionado == null) return;

        var res = MessageBox.Show(
            $"¿Está seguro de eliminar el proveedor '{_proveedorSeleccionado.Nombre}'?\n(Los artículos asociados conservarán su información pero quedarán desvinculados).",
            "Confirmar Eliminación", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (res == MessageBoxResult.Yes)
        {
            try
            {
                await _proveedorService.EliminarAsync(_proveedorSeleccionado.Id);
                LimpiarFormulario();
                await CargarProveedoresAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar: {ex.Message}", "MR SYS Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void BtnCerrar_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
