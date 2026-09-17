using PuntoDeVentaLibreria.Domain.Common;
using PuntoDeVentaLibreria.Domain.Entities.Catalogo;

namespace PuntoDeVentaLibreria.Domain.Entities.Proveedores;

public class Proveedor : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string? RazonSocial { get; set; }
    public string? Cuit { get; set; }
    public string? Contacto { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public string? DiasVisitaOEntrega { get; set; }
    public string? Notas { get; set; }

    public ICollection<Articulo> Articulos { get; set; } = new List<Articulo>();
}
