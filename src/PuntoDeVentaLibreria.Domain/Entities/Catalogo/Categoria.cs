using PuntoDeVentaLibreria.Domain.Common;

namespace PuntoDeVentaLibreria.Domain.Entities.Catalogo;

public class Categoria : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Icono { get; set; }
    public bool EsAccesoRapido { get; set; }
    public int Orden { get; set; }
    public ICollection<Articulo> Articulos { get; set; } = new List<Articulo>();
}
