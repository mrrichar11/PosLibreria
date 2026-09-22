using PuntoDeVentaLibreria.Domain.Common;
using PuntoDeVentaLibreria.Domain.Entities.Catalogo;

namespace PuntoDeVentaLibreria.Domain.Entities.Inventario;

public enum TipoMovimientoStock
{
    IngresoCompra = 1,
    Venta = 2,
    AjusteManualPositivo = 3,
    AjusteManualNegativo = 4,
    DesarmeCombo = 5,
    MermaODaño = 6
}

public class MovimientoStock : BaseEntity
{
    public Guid ArticuloId { get; set; }
    public Articulo Articulo { get; set; } = null!;

    public Guid? ArticuloVarianteId { get; set; }
    public ArticuloVariante? ArticuloVariante { get; set; }

    public TipoMovimientoStock Tipo { get; set; }
    public decimal Cantidad { get; set; }
    public decimal StockPrevio { get; set; }
    public decimal StockPosterior { get; set; }
    public string? Motivo { get; set; }
    public string? UsuarioNombre { get; set; }
}
