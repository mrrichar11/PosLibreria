namespace PuntoDeVentaLibreria.Application.DTOs.Clientes;

public class ReciboCobroCtaCteDto
{
    public string NumeroRecibo { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.Now;
    public string ClienteNombre { get; set; } = string.Empty;
    public string? ClienteDni { get; set; }
    public decimal SaldoAnterior { get; set; }
    public decimal MontoAbonado { get; set; }
    public decimal SaldoRestante { get; set; }
    public string MetodoPago { get; set; } = "Efectivo";
    public string Cajero { get; set; } = "Cajero";
    public string? Observaciones { get; set; }
}
