using PuntoDeVentaLibreria.Domain.Common;

namespace PuntoDeVentaLibreria.Domain.Entities.Finanzas;

public enum TipoMovimientoCaja
{
    IngresoVenta = 1,
    GastoOperativo = 2,
    RetiroDueño = 3,
    CobroCuentaCorriente = 4,
    AjusteManualIngreso = 5,
    AjusteManualEgreso = 6
}

public class MovimientoCaja : BaseEntity
{
    public Guid TurnoCajaId { get; set; }
    public TurnoCaja TurnoCaja { get; set; } = null!;

    public TipoMovimientoCaja Tipo { get; set; }
    public decimal Monto { get; set; }
    public string MetodoPago { get; set; } = "Efectivo"; // Efectivo, Debito, Credito, Transferencia, CtaCte
    public string Concepto { get; set; } = string.Empty;
    public string? UsuarioNombre { get; set; }
    public Guid? VentaId { get; set; }
    public Guid? ClienteId { get; set; }
}
