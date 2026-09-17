using System.Windows;
using System.Windows.Controls;
using PuntoDeVentaLibreria.Application.Services;
using PuntoDeVentaLibreria.UI.ViewModels;

namespace PuntoDeVentaLibreria.UI.Views.Inventario;

public partial class InventarioView : UserControl
{
    private readonly IInventarioService _inventarioService;
    private readonly IConfiguracionService _configuracionService;
    private readonly IProveedorService _proveedorService;
    public InventarioViewModel ViewModel { get; }

    public InventarioView(InventarioViewModel viewModel, IInventarioService inventarioService, IConfiguracionService configuracionService, IProveedorService proveedorService)
    {
        InitializeComponent();
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _inventarioService = inventarioService ?? throw new ArgumentNullException(nameof(inventarioService));
        _configuracionService = configuracionService ?? throw new ArgumentNullException(nameof(configuracionService));
        _proveedorService = proveedorService ?? throw new ArgumentNullException(nameof(proveedorService));
        DataContext = ViewModel;

        ViewModel.SolicitarEditorArticulo = (art) =>
        {
            var modal = new ArticuloModalWindow(art, _inventarioService, _configuracionService, _proveedorService)
            {
                Owner = Window.GetWindow(this)
            };

            var res = modal.ShowDialog();
            return Task.FromResult(res == true && modal.GuardadoExitoso);
        };

        Loaded += async (s, e) =>
        {
            await ViewModel.CargarDatosAsync();
        };
    }

    private async void BtnProveedores_Click(object sender, RoutedEventArgs e)
    {
        var modal = new ProveedoresModalWindow(_proveedorService)
        {
            Owner = Window.GetWindow(this)
        };
        modal.ShowDialog();
        await ViewModel.CargarDatosAsync();
    }

    private async void BtnMigrarAlmaLibre_Click(object sender, RoutedEventArgs e)
    {
        var modal = new MigracionAlmaLibreModalWindow(_inventarioService, _proveedorService)
        {
            Owner = Window.GetWindow(this)
        };

        if (modal.ShowDialog() == true && modal.MigracionRealizada)
        {
            await ViewModel.CargarDatosAsync();
        }
    }

    private async void BtnActualizarPrecios_Click(object sender, RoutedEventArgs e)
    {
        var modal = new ActualizarPreciosProveedorModalWindow(_inventarioService, _proveedorService)
        {
            Owner = Window.GetWindow(this)
        };

        if (modal.ShowDialog() == true && modal.PreciosActualizados)
        {
            await ViewModel.CargarDatosAsync();
        }
    }
}
