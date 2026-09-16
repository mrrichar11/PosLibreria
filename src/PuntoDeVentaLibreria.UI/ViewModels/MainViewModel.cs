using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PuntoDeVentaLibreria.Application.Services;

namespace PuntoDeVentaLibreria.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IConfiguracionService _configuracionService;
    private readonly ICajaService _cajaService;
    private readonly ILicenseService _licenseService;
    private readonly IUpdateService _updateService;

    [ObservableProperty]
    private string _titulo = "MR SYS Retail · Librería & Regalería";

    [ObservableProperty]
    private string _nombreComercio = "Librería & Regalería San Martín";

    [ObservableProperty]
    private string _estadoCajaTexto = "CAJA ABIERTA";

    [ObservableProperty]
    private string _colorFondoEstadoCaja = "#10B981"; // Verde por defecto

    [ObservableProperty]
    private string _licenciaBadgeTexto = "PLAN PRO ACTIVO";

    [ObservableProperty]
    private string _licenciaBadgeColor = "#2563EB"; // Azul

    [ObservableProperty]
    private string _usuarioActivoTexto = "Cajero: Mostrador Principal";

    [ObservableProperty]
    private PuntoDeVentaLibreria.Application.DTOs.Seguridad.SesionUsuarioDto? _sesionActual;

    [ObservableProperty]
    private bool _hayActualizacionDisponible;

    [ObservableProperty]
    private string _nuevaVersionTexto = "v1.0.1";

    [ObservableProperty]
    private string _statusBarIzquierdaTexto = "Librería & Regalería · Base SQLite Activa";

    [ObservableProperty]
    private string _statusBarDerechaTexto = "MR SYS v1.0.0 · ONLINE";

    public MainViewModel(
        IConfiguracionService configuracionService,
        ICajaService cajaService,
        ILicenseService licenseService,
        IUpdateService updateService)
    {
        _configuracionService = configuracionService ?? throw new ArgumentNullException(nameof(configuracionService));
        _cajaService = cajaService ?? throw new ArgumentNullException(nameof(cajaService));
        _licenseService = licenseService ?? throw new ArgumentNullException(nameof(licenseService));
        _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));
    }

    public void EstablecerSesion(PuntoDeVentaLibreria.Application.DTOs.Seguridad.SesionUsuarioDto sesion)
    {
        SesionActual = sesion;
        UsuarioActivoTexto = $"👤 {sesion.NombreCompleto} ({sesion.Rol})";
    }

    public async Task ActualizarInformacionAsync()
    {
        await ActualizarEstadoCajaAsync();
        await ActualizarConfiguracionAsync();
        await ActualizarLicenciaAsync();
        _ = VerificarActualizacionesEnSegundoPlanoAsync();
    }

    public async Task ActualizarConfiguracionAsync()
    {
        try
        {
            var config = await _configuracionService.ObtenerConfiguracionAsync();
            NombreComercio = config.NombreComercio;
            StatusBarIzquierdaTexto = $"{config.NombreComercio} · {config.Direccion}";
        }
        catch { }
    }

    public async Task ActualizarEstadoCajaAsync()
    {
        try
        {
            var turno = await _cajaService.ObtenerTurnoActivoAsync();
            if (turno != null)
            {
                EstadoCajaTexto = "CAJA ABIERTA";
                ColorFondoEstadoCaja = "#10B981"; // Verde
                UsuarioActivoTexto = $"Turno de {turno.UsuarioApertura} ({turno.FechaApertura:HH:mm})";
            }
            else
            {
                EstadoCajaTexto = "CAJA CERRADA";
                ColorFondoEstadoCaja = "#EF4444"; // Rojo
                UsuarioActivoTexto = "Sin turno abierto";
            }
        }
        catch { }
    }

    public async Task ActualizarLicenciaAsync()
    {
        try
        {
            var lic = await _licenseService.ValidarLicenciaAsync();
            if (lic.EsValida)
            {
                LicenciaBadgeTexto = $"PRO ({lic.DiasRestantes} días)";
                LicenciaBadgeColor = lic.DiasRestantes <= 5 ? "#F59E0B" : "#2563EB";
            }
            else
            {
                LicenciaBadgeTexto = "PLAN EXPIRADO";
                LicenciaBadgeColor = "#DC2626";
            }
        }
        catch { }
    }

    private async Task VerificarActualizacionesEnSegundoPlanoAsync()
    {
        try
        {
            var config = await _configuracionService.ObtenerConfiguracionAsync();
            var res = await _updateService.VerificarActualizacionesAsync(config.GitHubRepoOwner, config.GitHubRepoName);
            if (res.HayActualizacion)
            {
                HayActualizacionDisponible = true;
                NuevaVersionTexto = res.VersionDisponible;
            }
        }
        catch { }
    }
}
