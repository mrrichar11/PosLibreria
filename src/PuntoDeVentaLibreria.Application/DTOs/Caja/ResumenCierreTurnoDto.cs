namespace PuntoDeVentaLibreria.Application.DTOs.Caja;

public class ResumenCierreTurnoDto
{
    public Guid TurnoId { get; set; }
    public DateTime FechaApertura { get; set; }
    public DateTime FechaCierre { get; set; }
    public string UsuarioApertura { get; set; } = string.Empty;
    public string UsuarioCierre { get; set; } = string.Empty;

    // Fondos y Flujo de Efectivo
    public decimal FondoInicial { get; set; }
    public decimal VentasEfectivo { get; set; }
    public decimal CobrosCtaCteEfectivo { get; set; }
    public decimal TotalIngresosEfectivo => VentasEfectivo + CobrosCtaCteEfectivo;
    public decimal GastosOperativos { get; set; }
    public decimal RetirosDueño { get; set; }
    public decimal TotalEgresosEfectivo => GastosOperativos + RetirosDueño;

    public decimal EfectivoEsperadoEnCajon => FondoInicial + TotalIngresosEfectivo - TotalEgresosEfectivo;
    public decimal EfectivoRealContado { get; set; }
    public decimal Diferencia => EfectivoRealContado - EfectivoEsperadoEnCajon;

    // Otros Medios de Pago Electrónicos y Fiado
    public decimal VentasDebito { get; set; }
    public decimal VentasCredito { get; set; }
    public decimal VentasTransferencia { get; set; }
    public decimal VentasCtaCte { get; set; } // Fiado otorgado en el turno

    public decimal TotalFacturadoTurno => VentasEfectivo + VentasDebito + VentasCredito + VentasTransferencia + VentasCtaCte;
    public int CantidadOperaciones { get; set; }
    public string? Observaciones { get; set; }
}
