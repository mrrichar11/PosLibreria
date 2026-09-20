using PuntoDeVentaLibreria.Domain.Entities.Seguridad;

namespace PuntoDeVentaLibreria.Application.DTOs.Seguridad;

public class UsuarioDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public RolUsuario Rol { get; set; } = RolUsuario.Vendedor;
    public string RolDescripcion => Rol switch
    {
        RolUsuario.Administrador => "Administrador",
        RolUsuario.Encargado => "Encargado",
        _ => "Cajero / Vendedor"
    };
}

public class LoginRequestDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class SesionUsuarioDto
{
    public Guid UsuarioId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public RolUsuario Rol { get; set; } = RolUsuario.Vendedor;
    public DateTime FechaInicioSesion { get; set; } = DateTime.Now;

    public bool EsAdmin => Rol == RolUsuario.Administrador;
}

public class CrearUsuarioDto
{
    public string Username { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public RolUsuario Rol { get; set; } = RolUsuario.Vendedor;
}

public class ActualizarUsuarioDto
{
    public Guid Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public RolUsuario Rol { get; set; } = RolUsuario.Vendedor;
    public string? NuevaPassword { get; set; }
}

public class CambiarPasswordDto
{
    public Guid UsuarioId { get; set; }
    public string PasswordActual { get; set; } = string.Empty;
    public string NuevaPassword { get; set; } = string.Empty;
}
