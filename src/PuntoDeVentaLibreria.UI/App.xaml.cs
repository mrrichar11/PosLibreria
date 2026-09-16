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
using PuntoDeVentaLibreria.UI.Views.Inventario;
using PuntoDeVentaLibreria.UI.Views.Pos;

namespace PuntoDeVentaLibreria.UI;

public partial class App : System.Windows.Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Base de datos SQLite local
                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlite("Data Source=punto_venta_libreria.db"));

                // Servicios de Dominio e Infraestructura
                services.AddScoped<IInventarioService, InventarioService>();
                services.AddScoped<IVentaService, VentaService>();
                services.AddScoped<ICajaService, CajaService>();
                services.AddScoped<IConfiguracionService, ConfiguracionService>();
                services.AddScoped<IClienteService, ClienteService>();
                services.AddScoped<ILicenseService, LicenseService>();
                services.AddScoped<IUpdateService, GitHubUpdateService>();
                services.AddScoped<IBackupService, BackupService>();
                services.AddScoped<ITicketPrinterService, TicketPrinterService>();

                // ViewModels
                services.AddSingleton<MainViewModel>();
                services.AddSingleton<PosViewModel>();
                services.AddSingleton<InventarioViewModel>();
                services.AddSingleton<CajaViewModel>();
                services.AddSingleton<ClientesViewModel>();
                services.AddSingleton<ConfiguracionViewModel>();

                // Vistas Principales (UserControls y Ventana)
                services.AddSingleton<PosView>();
                services.AddSingleton<InventarioView>();
                services.AddSingleton<CajaView>();
                services.AddSingleton<ClientesView>();
                services.AddSingleton<ConfiguracionView>();
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
        }

        // Mostrar ventana principal
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        base.OnExit(e);
    }
}
