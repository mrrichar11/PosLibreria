using PuntoDeVentaLibreria.Domain.Common;

namespace PuntoDeVentaLibreria.Domain.Entities.Seguridad;

public class LicenciaSistema : BaseEntity
{
    public string CodigoInstalacion { get; set; } = string.Empty;
    public string ClaveActivacion { get; set; } = string.Empty;
    public DateTime FechaActivacion { get; set; } = DateTime.UtcNow;
    public DateTime FechaExpiracion { get; set; }
    public bool TieneModuloIA { get; set; } = true;
    public string? ComercioNombre { get; set; }
}
