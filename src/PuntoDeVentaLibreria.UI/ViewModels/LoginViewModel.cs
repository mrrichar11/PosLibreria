using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PuntoDeVentaLibreria.Application.DTOs.Seguridad;
using PuntoDeVentaLibreria.Application.Services;

namespace PuntoDeVentaLibreria.UI.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly IConfiguracionService? _configuracionService;

    [ObservableProperty]
    private string _username = "admin";

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _mensajeError = string.Empty;

    [ObservableProperty]
    private bool _estaCargando;

    [ObservableProperty]
    private string? _logoRuta;

    [ObservableProperty]
    private string _nombreComercio = "Librería & Regalería";

    [ObservableProperty]
    private bool _tieneLogoCustom;

    public ObservableCollection<UsuarioDto> UsuariosDisponibles { get; } = new();

    public SesionUsuarioDto? SesionAutenticada { get; private set; }

    public event Action? OnLoginExitoso;

    public LoginViewModel(IAuthService authService, IConfiguracionService? configuracionService = null)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _configuracionService = configuracionService;
    }

    public async Task InicializarAsync()
    {
        if (_configuracionService != null)
        {
            try
            {
                var cfg = await _configuracionService.ObtenerConfiguracionAsync();
                if (!string.IsNullOrWhiteSpace(cfg.NombreComercio))
                {
                    NombreComercio = cfg.NombreComercio;
                }
                if (!string.IsNullOrWhiteSpace(cfg.LogoRuta) && System.IO.File.Exists(cfg.LogoRuta))
                {
                    LogoRuta = cfg.LogoRuta;
                    TieneLogoCustom = true;
                }
                else
                {
                    TieneLogoCustom = false;
                }
            }
            catch { }
        }

        UsuariosDisponibles.Clear();
        var lista = await _authService.ObtenerUsuariosActivosAsync();
        foreach (var u in lista)
        {
            UsuariosDisponibles.Add(u);
        }

        if (UsuariosDisponibles.Count > 0 && string.IsNullOrWhiteSpace(Username))
        {
            Username = UsuariosDisponibles[0].Username;
        }
    }

    [RelayCommand]
    private void SeleccionarUsuario(UsuarioDto? usuario)
    {
        if (usuario == null) return;
        Username = usuario.Username;
        MensajeError = string.Empty;
    }

    [RelayCommand]
    private async Task IniciarSesionAsync()
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            MensajeError = "Por favor ingrese o seleccione su nombre de usuario.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            MensajeError = "Por favor ingrese su contraseña o PIN de acceso.";
            return;
        }

        EstaCargando = true;
        MensajeError = string.Empty;

        try
        {
            var sesion = await _authService.IniciarSesionAsync(Username, Password);
            if (sesion == null)
            {
                MensajeError = "Usuario o contraseña incorrectos. Verifique sus datos.";
                return;
            }

            SesionAutenticada = sesion;
            OnLoginExitoso?.Invoke();
        }
        finally
        {
            EstaCargando = false;
        }
    }
}
