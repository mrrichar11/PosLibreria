using Microsoft.EntityFrameworkCore;
using PuntoDeVentaLibreria.Application.DTOs.Seguridad;
using PuntoDeVentaLibreria.Application.Services;
using PuntoDeVentaLibreria.Infrastructure.Data;

namespace PuntoDeVentaLibreria.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;

    public AuthService(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<SesionUsuarioDto?> IniciarSesionAsync(string username, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return null;

        var limpio = username.Trim().ToLowerInvariant();

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Activo && u.Username.ToLower() == limpio, ct);

        if (usuario == null)
            return null;

        // Comprobación de credencial (demo y hash simple)
        if (usuario.PasswordHash != password.Trim())
            return null;

        return new SesionUsuarioDto
        {
            UsuarioId = usuario.Id,
            Username = usuario.Username,
            NombreCompleto = usuario.NombreCompleto,
            Rol = usuario.Rol,
            FechaInicioSesion = DateTime.Now
        };
    }

    public async Task<IReadOnlyList<UsuarioDto>> ObtenerUsuariosActivosAsync(CancellationToken ct = default)
    {
        return await _context.Usuarios
            .Where(u => u.Activo)
            .OrderBy(u => u.Rol)
            .ThenBy(u => u.NombreCompleto)
            .Select(u => new UsuarioDto
            {
                Id = u.Id,
                Username = u.Username,
                NombreCompleto = u.NombreCompleto,
                Rol = u.Rol
            })
            .ToListAsync(ct);
    }
}
