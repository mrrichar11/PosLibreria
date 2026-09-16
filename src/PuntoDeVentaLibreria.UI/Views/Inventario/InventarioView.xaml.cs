using System.Windows;
using System.Windows.Controls;
using PuntoDeVentaLibreria.Application.Services;
using PuntoDeVentaLibreria.UI.ViewModels;

namespace PuntoDeVentaLibreria.UI.Views.Inventario;

public partial class InventarioView : UserControl
{
    private readonly IInventarioService _inventarioService;
    private readonly IConfiguracionService _configuracionService;
    public InventarioViewModel ViewModel { get; }

    public InventarioView(InventarioViewModel viewModel, IInventarioService inventarioService, IConfiguracionService configuracionService)
    {
        InitializeComponent();
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _inventarioService = inventarioService ?? throw new ArgumentNullException(nameof(inventarioService));
        _configuracionService = configuracionService ?? throw new ArgumentNullException(nameof(configuracionService));
        DataContext = ViewModel;

        ViewModel.SolicitarEditorArticulo = (art) =>
        {
            var modal = new ArticuloModalWindow(art, _inventarioService, _configuracionService)
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
}
