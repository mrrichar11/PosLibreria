using System.Windows;
using System.Windows.Media;
using PuntoDeVentaLibreria.UI.ViewModels;
using PuntoDeVentaLibreria.UI.Views.Caja;
using PuntoDeVentaLibreria.UI.Views.Clientes;
using PuntoDeVentaLibreria.UI.Views.Configuracion;
using PuntoDeVentaLibreria.UI.Views.Inventario;
using PuntoDeVentaLibreria.UI.Views.Pos;

namespace PuntoDeVentaLibreria.UI;

public partial class MainWindow : Window
{
    private readonly MainViewModel _mainViewModel;
    private readonly PosView _posView;
    private readonly InventarioView _inventarioView;
    private readonly CajaView _cajaView;
    private readonly ClientesView _clientesView;
    private readonly ConfiguracionView _configuracionView;

    public MainWindow(
        MainViewModel mainViewModel,
        PosView posView,
        InventarioView inventarioView,
        CajaView cajaView,
        ClientesView clientesView,
        ConfiguracionView configuracionView)
    {
        InitializeComponent();

        _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
        _posView = posView ?? throw new ArgumentNullException(nameof(posView));
        _inventarioView = inventarioView ?? throw new ArgumentNullException(nameof(inventarioView));
        _cajaView = cajaView ?? throw new ArgumentNullException(nameof(cajaView));
        _clientesView = clientesView ?? throw new ArgumentNullException(nameof(clientesView));
        _configuracionView = configuracionView ?? throw new ArgumentNullException(nameof(configuracionView));

        DataContext = _mainViewModel;

        _posView.OnCajaModificada += async () =>
        {
            await _mainViewModel.ActualizarEstadoCajaAsync();
        };

        _cajaView.ViewModel.OnCajaModificada += async () =>
        {
            await _mainViewModel.ActualizarEstadoCajaAsync();
            await _posView.VerificarCajaAsync();
        };

        _configuracionView.ViewModel.OnConfiguracionGuardada += async () =>
        {
            await _mainViewModel.ActualizarInformacionAsync();
        };

        Loaded += async (s, e) =>
        {
            await _mainViewModel.ActualizarInformacionAsync();
            MostrarVistaPos();
        };
    }

    private void RestablecerEstiloBotones()
    {
        var normalBg = Brushes.Transparent;
        var normalFg = new SolidColorBrush(Color.FromRgb(30, 41, 59));

        BtnNavPos.Background = normalBg;
        BtnNavPos.Foreground = normalFg;

        BtnNavInventario.Background = normalBg;
        BtnNavInventario.Foreground = normalFg;

        BtnNavCaja.Background = normalBg;
        BtnNavCaja.Foreground = normalFg;

        BtnNavClientes.Background = normalBg;
        BtnNavClientes.Foreground = normalFg;

        BtnNavConfiguracion.Background = normalBg;
        BtnNavConfiguracion.Foreground = normalFg;
    }

    private void ActivarBoton(System.Windows.Controls.Button btn)
    {
        RestablecerEstiloBotones();
        btn.Background = new SolidColorBrush(Color.FromRgb(37, 99, 235)); // Azul
        btn.Foreground = Brushes.White;
    }

    private void MostrarVistaPos()
    {
        ActivarBoton(BtnNavPos);
        MainContentControl.Content = _posView;
    }

    private void NavPos_Click(object sender, RoutedEventArgs e)
    {
        MostrarVistaPos();
    }

    private async void NavInventario_Click(object sender, RoutedEventArgs e)
    {
        ActivarBoton(BtnNavInventario);
        MainContentControl.Content = _inventarioView;
        await _inventarioView.ViewModel.CargarDatosAsync();
    }

    private async void NavCaja_Click(object sender, RoutedEventArgs e)
    {
        ActivarBoton(BtnNavCaja);
        MainContentControl.Content = _cajaView;
        await _cajaView.ViewModel.CargarDatosAsync();
    }

    private async void NavClientes_Click(object sender, RoutedEventArgs e)
    {
        ActivarBoton(BtnNavClientes);
        MainContentControl.Content = _clientesView;
        await _clientesView.ViewModel.CargarDatosAsync();
    }

    private async void NavConfiguracion_Click(object sender, RoutedEventArgs e)
    {
        ActivarBoton(BtnNavConfiguracion);
        MainContentControl.Content = _configuracionView;
        await _configuracionView.ViewModel.CargarDatosAsync();
    }
}