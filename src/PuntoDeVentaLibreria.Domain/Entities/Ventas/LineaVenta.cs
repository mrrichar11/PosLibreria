using PuntoDeVentaLibreria.Domain.Common;
using PuntoDeVentaLibreria.Domain.Entities.Catalogo;

namespace PuntoDeVentaLibreria.Domain.Entities.Ventas;

public class LineaVenta : BaseEntity
{
    public Guid VentaId { get; set; }
    public Venta Venta { get; set; } = null!;

    public Guid? ArticuloId { get; set; }
    public Articulo? Articulo { get; set; }

    public string Descripcion { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string? CodigoBarras { get; set; }

    public decimal Cantidad { get; set; }
    public decimal PrecioUnitarioVenta { get; set; }
    public decimal PrecioCostoHistorico { get; set; }
    public decimal Subtotal => Cantidad * PrecioUnitarioVenta;

    /// <summary>Si es true, no exige ni descuenta existencias de inventario</summary>
    public bool EsVentaManual { get; set; }

    /// <summary>Si es true, fue vendida como kit combo escolar</summary>
    public bool EsCombo { get; set; }
}
