namespace PuntoDeVentaLibreria.Application.DTOs.Ventas;

public class RegistrarVentaDto
{
    public Guid TurnoCajaId { get; set; }
    public Guid? ClienteId { get; set; }
    public string VendedoraNombre { get; set; } = "Cajero";
    public List<ItemCarritoDto> Items { get; set; } = new();
    public string MetodoPago { get; set; } = "Efectivo"; // Efectivo, Debito, Credito, Transferencia, CtaCte
    public int CantidadCuotas { get; set; } = 1;
    public decimal DescuentoEfectivoMonto { get; set; }
    public decimal RecargoCuotasMonto { get; set; }
}

public class VentaRealizadaDto
{
    public Guid VentaId { get; set; }
    public string NumeroComprobante { get; set; } = string.Empty;
    public decimal TotalCobrado { get; set; }
    public DateTime Fecha { get; set; }
}
