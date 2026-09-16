using PuntoDeVentaLibreria.Application.DTOs.Seguridad;

namespace PuntoDeVentaLibreria.Application.Services;

public interface IAuthService
{
    Task<SesionUsuarioDto?> IniciarSesionAsync(string username, string password, CancellationToken ct = default);
    Task<IReadOnlyList<UsuarioDto>> ObtenerUsuariosActivosAsync(CancellationToken ct = default);
}
