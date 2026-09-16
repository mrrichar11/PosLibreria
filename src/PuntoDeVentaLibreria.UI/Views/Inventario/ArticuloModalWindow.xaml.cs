using System.Windows;
using System.Windows.Controls;
using PuntoDeVentaLibreria.Application.DTOs.Inventario;
using PuntoDeVentaLibreria.Application.Services;
using PuntoDeVentaLibreria.Domain.Entities.Catalogo;

namespace PuntoDeVentaLibreria.UI.Views.Inventario;

public partial class ArticuloModalWindow : Window
{
    private readonly IInventarioService _inventarioService;
    private IReadOnlyList<ArticuloDto> _articulosDisponibles = new List<ArticuloDto>();
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

        SincronizarTipoUI();
        CargarArticulosDisponibles();
        TxtNombre.Focus();
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
            // Filtrar para no agregar el mismo artículo a sí mismo ni otros combos anidados
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
            MessageBox.Show($"Error al guardar artículo: {ex.Message}", "MR SYS Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
