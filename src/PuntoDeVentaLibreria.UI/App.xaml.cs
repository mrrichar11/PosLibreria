using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PuntoDeVentaLibreria.Application.Services;
using PuntoDeVentaLibreria.Infrastructure.Data;
using PuntoDeVentaLibreria.Infrastructure.Services;
using PuntoDeVentaLibreria.UI.ViewModels;
using PuntoDeVentaLibreria.UI.Views.Caja;
using PuntoDeVentaLibreria.UI.Views.Clientes;
using PuntoDeVentaLibreria.UI.Views.Configuracion;
using PuntoDeVentaLibreria.UI.Views.Dashboard;
using PuntoDeVentaLibreria.UI.Views.Inventario;
using PuntoDeVentaLibreria.UI.Views.Login;
using PuntoDeVentaLibreria.UI.Views.Pos;

namespace PuntoDeVentaLibreria.UI;

public partial class App : System.Windows.Application
{
    private IHost? _host;
    private static bool _isShowingUnhandledError = false;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        this.DispatcherUnhandledException += (s, ev) =>
        {
            if (_isShowingUnhandledError)
            {
                ev.Handled = true;
                return;
            }

            _isShowingUnhandledError = true;
            try
            {
                MessageBox.Show($"Ocurrió un error en la interfaz:\n\n{ev.Exception.Message}\n\n{ev.Exception.StackTrace}", "MR SYS Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isShowingUnhandledError = false;
            }
            ev.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (s, ev) =>
        {
            if (ev.ExceptionObject is Exception ex)
            {
                MessageBox.Show($"Error crítico:\n\n{ex.Message}\n\n{ex.StackTrace}", "MR SYS Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Carga dinámica de configuración de base de datos (SQLite Local o PostgreSQL Nube / Plan PRO)
                var dbConfig = DatabaseConfigProvider.Cargar();
                if (dbConfig.Motor.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
                {
                    var connStr = DatabaseConfigProvider.ObtenerCadenaConexion(dbConfig);
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseNpgsql(connStr));
                }
                else
                {
                    var dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "punto_venta_libreria.db");
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseSqlite($"Data Source={dbPath}"));
                }

                // Servicios de Dominio e Infraestructura
                services.AddScoped<IInventarioService, InventarioService>();
                services.AddScoped<IProveedorService, ProveedorService>();
                services.AddScoped<IVentaService, VentaService>();
                services.AddScoped<ICajaService, CajaService>();
                services.AddScoped<IConfiguracionService, ConfiguracionService>();
                services.AddScoped<IClienteService, ClienteService>();
                services.AddScoped<ILicenseService, LicenseService>();
                services.AddScoped<IUpdateService, GitHubUpdateService>();
                services.AddScoped<IBackupService, BackupService>();
                services.AddScoped<IDatabaseMigrationService, DatabaseMigrationService>();
                services.AddScoped<ITicketPrinterService, TicketPrinterService>();
                services.AddScoped<IReporteService, ReporteService>();
                services.AddScoped<IAuthService, AuthService>();


                // ViewModels
                services.AddSingleton<MainViewModel>();
                services.AddSingleton<PosViewModel>();
                services.AddSingleton<DashboardViewModel>();
                services.AddSingleton<InventarioViewModel>();
                services.AddSingleton<CajaViewModel>();
                services.AddSingleton<ClientesViewModel>();
                services.AddSingleton<ConfiguracionViewModel>();
                services.AddTransient<LoginViewModel>();

                // Vistas Principales (UserControls y Ventana)
                services.AddSingleton<PosView>();
                services.AddSingleton<DashboardView>();
                services.AddSingleton<InventarioView>();
                services.AddSingleton<CajaView>();
                services.AddSingleton<ClientesView>();
                services.AddSingleton<ConfiguracionView>();
                services.AddTransient<LoginWindow>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        await _host.StartAsync();

        // Inicialización y Seeding de la Base de Datos SQLite
        using (var scope = _host.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await DatabaseInitializer.InitializeAsync(dbContext);
            await DataSeeder.SeedAsync(dbContext);

            try
            {
                var configService = scope.ServiceProvider.GetRequiredService<IConfiguracionService>();
                var cfg = await configService.ObtenerConfiguracionAsync();
                PuntoDeVentaLibreria.UI.Helpers.ThemeHelper.AplicarTema(cfg.TemaInterfaz);
            }
            catch { }
        }

        // Iniciar flujo con ventana de Login obligatoria
        IniciarFlujoLogin();
    }

    private void IniciarFlujoLogin()
    {
        try
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var loginWindow = _host!.Services.GetRequiredService<LoginWindow>();
            var loginResult = loginWindow.ShowDialog();

            if (loginResult == true && loginWindow.ViewModel.SesionAutenticada != null)
            {
                var sesion = loginWindow.ViewModel.SesionAutenticada;

                var mainVm = _host.Services.GetRequiredService<MainViewModel>();
                mainVm.EstablecerSesion(sesion);

                var posVm = _host.Services.GetRequiredService<PosViewModel>();
                posVm.UsuarioActual = sesion.NombreCompleto;

                var configVm = _host.Services.GetRequiredService<ConfiguracionViewModel>();
                configVm.EstablecerSesion(sesion);

                var mainWindow = _host.Services.GetRequiredService<MainWindow>();
                mainWindow.OnCerrarSesionSolicitado -= HandleCerrarSesion;
                mainWindow.OnCerrarSesionSolicitado += HandleCerrarSesion;

                MainWindow = mainWindow;
                ShutdownMode = ShutdownMode.OnMainWindowClose;

                mainWindow.Show();
                mainWindow.Activate();
            }
            else
            {
                Shutdown();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al iniciar el sistema:\n\n{ex.Message}\n\n{ex.StackTrace}", "Error de Inicio - MR SYS", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    private void HandleCerrarSesion()
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        var mainWindow = _host!.Services.GetRequiredService<MainWindow>();
        mainWindow.Hide();
        IniciarFlujoLogin();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            try
            {
                using var scope = _host.Services.CreateScope();
                var configService = scope.ServiceProvider.GetRequiredService<IConfiguracionService>();
                var config = await configService.ObtenerConfiguracionAsync();
                if (config.BackupAutomaticoAlCerrarSistema && config.MotorBaseDatos.Equals("SQLite", StringComparison.OrdinalIgnoreCase))
                {
                    var backupService = scope.ServiceProvider.GetRequiredService<IBackupService>();
                    await backupService.CrearBackupAsync(null, "cierre_sistema");
                }
            }
            catch { }

            await _host.StopAsync();
            _host.Dispose();
        }
        base.OnExit(e);
    }
}

