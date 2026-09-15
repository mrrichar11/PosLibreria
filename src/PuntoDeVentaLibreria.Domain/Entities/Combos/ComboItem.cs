using PuntoDeVentaLibreria.Domain.Common;
using PuntoDeVentaLibreria.Domain.Entities.Catalogo;

namespace PuntoDeVentaLibreria.Domain.Entities.Combos;

/// <summary>
/// Relación entre un artículo tipo ComboKit y los artículos individuales que lo integran.
/// </summary>
public class ComboItem : BaseEntity
{
    public Guid ComboArticuloId { get; set; }
    public Articulo ComboArticulo { get; set; } = null!;

    public Guid ComponenteArticuloId { get; set; }
    public Articulo ComponenteArticulo { get; set; } = null!;

    /// <summary>Cantidad de este componente incluida en el combo</summary>
    public decimal Cantidad { get; set; } = 1;
}
