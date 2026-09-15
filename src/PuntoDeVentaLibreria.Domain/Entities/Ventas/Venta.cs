using PuntoDeVentaLibreria.Domain.Common;
using PuntoDeVentaLibreria.Domain.Entities.Clientes;
using PuntoDeVentaLibreria.Domain.Entities.Finanzas;

namespace PuntoDeVentaLibreria.Domain.Entities.Ventas;

public class Venta : BaseEntity
{
    public string NumeroComprobante { get; set; } = string.Empty;
    public DateTime FechaVenta { get; set; } = DateTime.UtcNow;

    public Guid? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public Guid TurnoCajaId { get; set; }
    public TurnoCaja TurnoCaja { get; set; } = null!;

    public string VendedoraNombre { get; set; } = string.Empty;

    // Totales
    public decimal SubtotalBruto { get; set; }
    public decimal DescuentoEfectivoMonto { get; set; }
    public decimal RecargoCuotasMonto { get; set; }
    public decimal TotalVenta { get; set; }
    public decimal TotalCostoHistorico { get; set; }
    public decimal GananciaNetaEstimada => TotalVenta - TotalCostoHistorico;

    public string MetodoPagoPrincipal { get; set; } = "Efectivo";
    public int CantidadCuotas { get; set; } = 1;

    public ICollection<LineaVenta> LineasVenta { get; set; } = new List<LineaVenta>();
    public ICollection<PagoVenta> Pagos { get; set; } = new List<PagoVenta>();
}
