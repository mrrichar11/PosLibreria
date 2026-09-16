namespace PuntoDeVentaLibreria.Application.DTOs.Clientes;

public class MovimientoClienteDto
{
    public DateTime Fecha { get; set; }
    public string Tipo { get; set; } = string.Empty; // "Compra Fiada", "Abono / Pago", "Compra Contado"
    public string Comprobante { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public string MetodoPago { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;
    public bool EsAbono { get; set; }
}
