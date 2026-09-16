using System.Collections.ObjectModel;
using System.Printing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PuntoDeVentaLibreria.Application.DTOs.Backup;
using PuntoDeVentaLibreria.Application.DTOs.Configuracion;
using PuntoDeVentaLibreria.Application.DTOs.Peripherals;
using PuntoDeVentaLibreria.Application.DTOs.Seguridad;
using PuntoDeVentaLibreria.Application.DTOs.Sistema;
using PuntoDeVentaLibreria.Application.DTOs.Ventas;
using PuntoDeVentaLibreria.Application.Services;
using Wpf.Ui.Appearance;

namespace PuntoDeVentaLibreria.UI.ViewModels;

public partial class ConfiguracionViewModel : ObservableObject
{
    private readonly IConfiguracionService _configuracionService;
    private readonly ILicenseService _licenseService;
    private readonly IUpdateService _updateService;
    private readonly IBackupService _backupService;
    private readonly ITicketPrinterService _ticketPrinterService;

    [ObservableProperty]
    private ConfiguracionNegocioDto _config = new();

    [ObservableProperty]
    private EstadoRetirosDueñoDto _estadoRetiros = new();

    [ObservableProperty]
    private EstadoLicenciaDto _estadoLicencia = new();

    [ObservableProperty]
    private string _claveActivacionInput = string.Empty;

    [ObservableProperty]
    private string _mensajeActivacion = string.Empty;

    [ObservableProperty]
    private ActualizacionDto _actualizacionInfo = new();

    [ObservableProperty]
    private string _mensajeActualizacion = string.Empty;

    [ObservableProperty]
    private bool _estaDescargandoActualizacion;

    [ObservableProperty]
    private double _progresoDescarga;

    [ObservableProperty]
    private string _mensajeBackup = string.Empty;

    [ObservableProperty]
    private string _impresoraSeleccionada = string.Empty;

    [ObservableProperty]
    private int _anchoPapel = 80;

    [ObservableProperty]
    private string _mensajeGuardado = string.Empty;

    public ObservableCollection<string> ImpresorasDisponibles { get; } = new();
    public ObservableCollection<BackupInfoDto> HistorialBackups { get; } = new();

    public event Action? OnConfiguracionGuardada;

    public ConfiguracionViewModel(
        IConfiguracionService configuracionService,
        ILicenseService licenseService,
        IUpdateService updateService,
        IBackupService backupService,
        ITicketPrinterService ticketPrinterService)
    {
        _configuracionService = configuracionService ?? throw new ArgumentNullException(nameof(configuracionService));
        _licenseService = licenseService ?? throw new ArgumentNullException(nameof(licenseService));
        _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));
        _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
        _ticketPrinterService = ticketPrinterService ?? throw new ArgumentNullException(nameof(ticketPrinterService));
    }

    public async Task CargarDatosAsync()
    {
        Config = await _configuracionService.ObtenerConfiguracionAsync();
        EstadoRetiros = await _configuracionService.ObtenerEstadoRetirosDueñoMesAsync();
        EstadoLicencia = await _licenseService.ValidarLicenciaAsync();

        // Cargar impresoras instaladas en Windows
        ImpresorasDisponibles.Clear();
        try
        {
            var server = new LocalPrintServer();
            foreach (var q in server.GetPrintQueues())
            {
                ImpresorasDisponibles.Add(q.Name);
            }
        }
        catch { }

        await CargarHistorialBackupsAsync();
    }

    private async Task CargarHistorialBackupsAsync()
    {
        HistorialBackups.Clear();
        var lista = await _backupService.ObtenerHistorialBackupsAsync();
        foreach (var b in lista)
        {
            HistorialBackups.Add(b);
        }
    }

    [RelayCommand]
    private async Task GuardarConfiguracionAsync()
    {
        await _configuracionService.GuardarConfiguracionAsync(Config);
        MensajeGuardado = "¡Configuración guardada exitosamente!";
        OnConfiguracionGuardada?.Invoke();
    }

    [RelayCommand]
    private void CambiarTema(string? tema)
    {
        if (string.Equals(tema, "Dark", StringComparison.OrdinalIgnoreCase))
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Dark);
            Config.TemaInterfaz = "Dark";
        }
        else
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Light);
            Config.TemaInterfaz = "Light";
        }
    }

    [RelayCommand]
    private async Task ActivarLicenciaAsync()
    {
        var res = await _licenseService.ActivarLicenciaAsync(ClaveActivacionInput);
        MensajeActivacion = res.Mensaje;
        EstadoLicencia = await _licenseService.ValidarLicenciaAsync();
        ClaveActivacionInput = string.Empty;
    }

    [RelayCommand]
    private async Task BuscarActualizacionesAsync()
    {
        MensajeActualizacion = "Consultando versiones disponibles en GitHub...";
        var res = await _updateService.VerificarActualizacionesAsync(Config.GitHubRepoOwner, Config.GitHubRepoName);
        ActualizacionInfo = res;

        if (res.HayActualizacion)
        {
            MensajeActualizacion = $"¡Nueva versión v{res.VersionDisponible} disponible!";
        }
        else
        {
            MensajeActualizacion = $"Tiene instalada la versión más reciente (v{res.VersionActual}).";
        }
    }

    [RelayCommand]
    private async Task DescargarEInstalarActualizacionAsync()
    {
        if (!ActualizacionInfo.HayActualizacion || string.IsNullOrEmpty(ActualizacionInfo.UrlDescargaZip))
            return;

        EstaDescargandoActualizacion = true;
        MensajeActualizacion = "Descargando actualización oficial...";

        try
        {
            var progreso = new Progress<ProgresoDescargaDto>(p =>
            {
                ProgresoDescarga = p.Porcentaje;
                MensajeActualizacion = p.Mensaje;
            });

            var rutaZip = await _updateService.DescargarActualizacionAsync(ActualizacionInfo, progreso);
            MensajeActualizacion = "Instalando y reiniciando sistema...";
            _updateService.IniciarInstalacion(rutaZip);
        }
        catch (Exception ex)
        {
            MensajeActualizacion = $"Error en la actualización: {ex.Message}";
        }
        finally
        {
            EstaDescargandoActualizacion = false;
        }
    }

    [RelayCommand]
    private async Task CrearBackupAhoraAsync()
    {
        try
        {
            var b = await _backupService.CrearBackupAsync(null, false);
            MensajeBackup = $"Copia creada exitosamente: {b.NombreArchivo} ({b.TamañoFormateado})";
            await CargarHistorialBackupsAsync();
        }
        catch (Exception ex)
        {
            MensajeBackup = $"Error al crear respaldo: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ProbarTicketAsync()
    {
        var ventaDemo = new VentaRealizadaDto
        {
            NumeroComprobante = "L-DEMO01",
            Fecha = DateTime.Now,
            SubtotalBruto = 4120m,
            TotalCobrado = 4120m,
            MetodoPago = "Efectivo",
            Lineas = new List<ItemCarritoDto>
            {
                new() { Descripcion = "Cuaderno Tapa Dura", Cantidad = 1, PrecioUnitario = 3520m },
                new() { Descripcion = "Bolígrafo BIC Azul", Cantidad = 1, PrecioUnitario = 600m }
            }
        };

        var configTicket = new ConfiguracionTicketDto
        {
            NombreComercio = Config.NombreComercio,
            Direccion = Config.Direccion,
            Telefono = Config.Telefono,
            Cuit = Config.Cuit,
            AnchoPapelMm = AnchoPapel,
            ImpresoraNombre = ImpresoraSeleccionada
        };

        await _ticketPrinterService.ImprimirTicketVentaAsync(ventaDemo, configTicket);
        MensajeGuardado = "Ticket de prueba generado en carpeta ./Tickets/";
    }
}
