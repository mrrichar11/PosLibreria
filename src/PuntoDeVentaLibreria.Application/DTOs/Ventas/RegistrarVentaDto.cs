namespace PuntoDeVentaLibreria.Application.DTOs.Ventas;

public class RegistrarVentaDto
{
    public Guid TurnoCajaId { get; set; }
    public Guid? ClienteId { get; set; }
    public string? ClienteNombre { get; set; }
    public string? ReferenciaPago { get; set; }
    public string VendedoraNombre { get; set; } = "Cajero";
    public List<ItemCarritoDto> Items { get; set; } = new();
    public string MetodoPago { get; set; } = "Efectivo"; // Efectivo, Debito, Credito, Transferencia, CtaCte
    public int CantidadCuotas { get; set; } = 1;
    public decimal DescuentoEfectivoMonto { get; set; }
    public decimal RecargoCuotasMonto { get; set; }
    public decimal MontoEntregado { get; set; }
    public decimal Vuelto { get; set; }

    // Doble método de pago en fiados (Entrega inicial)
    public bool TieneEntregaInicial { get; set; }
    public decimal MontoEntregaInicial { get; set; }
    public string MetodoPagoEntrega { get; set; } = "Efectivo";
    public string? ReferenciaEntrega { get; set; }
}

public class VentaRealizadaDto
{
    public Guid VentaId { get; set; }
    public string NumeroComprobante { get; set; } = string.Empty;
    public decimal TotalCobrado { get; set; }
    public decimal SubtotalBruto { get; set; }
    public decimal DescuentoMonto { get; set; }
    public decimal RecargoMonto { get; set; }
    public decimal MontoEntregado { get; set; }
    public decimal Vuelto { get; set; }
    public string MetodoPago { get; set; } = "Efectivo";
    public string? ClienteNombre { get; set; }
    public string? VendedoraNombre { get; set; }
    public string? ReferenciaPago { get; set; }
    public DateTime Fecha { get; set; }
    public List<ItemCarritoDto> Lineas { get; set; } = new();

    // Detalle de doble medio de pago
    public bool TieneEntregaInicial { get; set; }
    public decimal MontoEntregaInicial { get; set; }
    public string MetodoPagoEntrega { get; set; } = string.Empty;
    public decimal MontoFiado { get; set; }
}
