using System.Windows;
using System.Windows.Media;
using PuntoDeVentaLibreria.UI.ViewModels;
using PuntoDeVentaLibreria.UI.Views.Caja;
using PuntoDeVentaLibreria.UI.Views.Clientes;
using PuntoDeVentaLibreria.UI.Views.Configuracion;
using PuntoDeVentaLibreria.UI.Views.Dashboard;
using PuntoDeVentaLibreria.UI.Views.Inventario;
using PuntoDeVentaLibreria.UI.Views.Pos;

namespace PuntoDeVentaLibreria.UI;

public partial class MainWindow : Window
{
    private readonly MainViewModel _mainViewModel;
    private readonly PosView _posView;
    private readonly DashboardView _dashboardView;
    private readonly InventarioView _inventarioView;
    private readonly CajaView _cajaView;
    private readonly ClientesView _clientesView;
    private readonly ConfiguracionView _configuracionView;

    public event Action? OnCerrarSesionSolicitado;

    public MainWindow(
        MainViewModel mainViewModel,
        PosView posView,
        DashboardView dashboardView,
        InventarioView inventarioView,
        CajaView cajaView,
        ClientesView clientesView,
        ConfiguracionView configuracionView)
    {
        InitializeComponent();

        _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
        _posView = posView ?? throw new ArgumentNullException(nameof(posView));
        _dashboardView = dashboardView ?? throw new ArgumentNullException(nameof(dashboardView));
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

        BtnNavDashboard.Background = normalBg;
        BtnNavDashboard.Foreground = normalFg;

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
        try
        {
            MostrarVistaPos();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar Punto de Venta: {ex.Message}", "MR SYS Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void NavInventario_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ActivarBoton(BtnNavInventario);
            MainContentControl.Content = _inventarioView;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar Inventario: {ex.Message}", "MR SYS Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void NavCaja_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ActivarBoton(BtnNavCaja);
            MainContentControl.Content = _cajaView;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar Caja: {ex.Message}", "MR SYS Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void NavClientes_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ActivarBoton(BtnNavClientes);
            MainContentControl.Content = _clientesView;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar Clientes: {ex.Message}", "MR SYS Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void NavDashboard_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ActivarBoton(BtnNavDashboard);
            MainContentControl.Content = _dashboardView;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar Dashboard: {ex.Message}", "MR SYS Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void NavConfiguracion_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ActivarBoton(BtnNavConfiguracion);
            MainContentControl.Content = _configuracionView;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar Configuración: {ex.Message}", "MR SYS Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CerrarSesion_Click(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            "¿Desea cerrar la sesión actual y cambiar de usuario?",
            "Cerrar Sesión / Cambiar Cajero",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm == MessageBoxResult.Yes)
        {
            OnCerrarSesionSolicitado?.Invoke();
        }
    }
}