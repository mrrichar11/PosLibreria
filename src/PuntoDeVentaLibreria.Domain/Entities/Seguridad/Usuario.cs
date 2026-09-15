using PuntoDeVentaLibreria.Domain.Common;

namespace PuntoDeVentaLibreria.Domain.Entities.Seguridad;

public enum RolUsuario
{
    Administrador = 1,
    Encargado = 2,
    Vendedor = 3
}

public class Usuario : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public RolUsuario Rol { get; set; } = RolUsuario.Vendedor;
}
