using PuntoDeVentaLibreria.Application.DTOs.Seguridad;

namespace PuntoDeVentaLibreria.Application.Services;

public interface IAuthService
{
    Task<SesionUsuarioDto?> IniciarSesionAsync(string username, string password, CancellationToken ct = default);
    Task<IReadOnlyList<UsuarioDto>> ObtenerUsuariosActivosAsync(CancellationToken ct = default);
    Task<(bool Exitoso, string Mensaje)> CambiarPasswordAsync(Guid usuarioId, string passwordActual, string nuevaPassword, CancellationToken ct = default);
    Task<(bool Exitoso, string Mensaje, UsuarioDto? Usuario)> CrearUsuarioAsync(CrearUsuarioDto dto, CancellationToken ct = default);
    Task<(bool Exitoso, string Mensaje)> ActualizarUsuarioAsync(ActualizarUsuarioDto dto, CancellationToken ct = default);
    Task<(bool Exitoso, string Mensaje)> EliminarODesactivarUsuarioAsync(Guid usuarioId, Guid usuarioOperadorId, CancellationToken ct = default);
}
