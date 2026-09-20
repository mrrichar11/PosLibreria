using Microsoft.EntityFrameworkCore;
using PuntoDeVentaLibreria.Application.DTOs.Seguridad;
using PuntoDeVentaLibreria.Application.Services;
using PuntoDeVentaLibreria.Domain.Entities.Seguridad;
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

    public async Task<(bool Exitoso, string Mensaje)> CambiarPasswordAsync(Guid usuarioId, string passwordActual, string nuevaPassword, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(nuevaPassword))
            return (false, "La nueva contraseña no puede estar vacía.");

        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId && u.Activo, ct);
        if (usuario == null)
            return (false, "Usuario no encontrado.");

        if (usuario.PasswordHash != passwordActual.Trim())
            return (false, "La contraseña actual ingresada es incorrecta.");

        usuario.PasswordHash = nuevaPassword.Trim();
        await _context.SaveChangesAsync(ct);

        return (true, "¡Contraseña actualizada con éxito!");
    }

    public async Task<(bool Exitoso, string Mensaje, UsuarioDto? Usuario)> CrearUsuarioAsync(CrearUsuarioDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password) || string.IsNullOrWhiteSpace(dto.NombreCompleto))
            return (false, "Debe completar nombre de usuario, nombre completo y contraseña.", null);

        var limpioUsername = dto.Username.Trim().ToLowerInvariant();
        bool yaExiste = await _context.Usuarios.AnyAsync(u => u.Username.ToLower() == limpioUsername, ct);
        if (yaExiste)
            return (false, $"El nombre de usuario '{dto.Username}' ya está en uso.", null);

        var nuevo = new Usuario
        {
            Id = Guid.NewGuid(),
            Username = dto.Username.Trim(),
            NombreCompleto = dto.NombreCompleto.Trim(),
            PasswordHash = dto.Password.Trim(),
            Rol = dto.Rol,
            Activo = true
        };

        _context.Usuarios.Add(nuevo);
        await _context.SaveChangesAsync(ct);

        var usuarioDto = new UsuarioDto
        {
            Id = nuevo.Id,
            Username = nuevo.Username,
            NombreCompleto = nuevo.NombreCompleto,
            Rol = nuevo.Rol
        };

        return (true, "Usuario creado exitosamente.", usuarioDto);
    }

    public async Task<(bool Exitoso, string Mensaje)> ActualizarUsuarioAsync(ActualizarUsuarioDto dto, CancellationToken ct = default)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == dto.Id, ct);
        if (usuario == null)
            return (false, "Usuario no encontrado.");

        if (string.IsNullOrWhiteSpace(dto.NombreCompleto))
            return (false, "El nombre completo no puede estar vacío.");

        usuario.NombreCompleto = dto.NombreCompleto.Trim();
        usuario.Rol = dto.Rol;

        if (!string.IsNullOrWhiteSpace(dto.NuevaPassword))
        {
            usuario.PasswordHash = dto.NuevaPassword.Trim();
        }

        await _context.SaveChangesAsync(ct);
        return (true, "Usuario actualizado correctamente.");
    }

    public async Task<(bool Exitoso, string Mensaje)> EliminarODesactivarUsuarioAsync(Guid usuarioId, Guid usuarioOperadorId, CancellationToken ct = default)
    {
        if (usuarioId == usuarioOperadorId)
            return (false, "No puede eliminar su propio usuario en sesión.");

        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId, ct);
        if (usuario == null)
            return (false, "Usuario no encontrado.");

        // Verificar que no sea el último administrador activo
        if (usuario.Rol == RolUsuario.Administrador)
        {
            int adminsActivos = await _context.Usuarios.CountAsync(u => u.Activo && u.Rol == RolUsuario.Administrador, ct);
            if (adminsActivos <= 1)
                return (false, "No se puede eliminar al único administrador del sistema.");
        }

        usuario.Activo = false;
        await _context.SaveChangesAsync(ct);
        return (true, "Usuario desactivado correctamente.");
    }
}
