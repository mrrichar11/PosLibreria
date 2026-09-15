using PuntoDeVentaLibreria.Domain.Common;

namespace PuntoDeVentaLibreria.Domain.Entities.Ventas;

public class PagoVenta : BaseEntity
{
    public Guid VentaId { get; set; }
    public Venta Venta { get; set; } = null!;

    public string MetodoPago { get; set; } = "Efectivo";
    public decimal Monto { get; set; }
    public string? ReferenciaOperacion { get; set; }
}
