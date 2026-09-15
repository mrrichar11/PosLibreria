using PuntoDeVentaLibreria.Domain.Common;

namespace PuntoDeVentaLibreria.Domain.Entities.Finanzas;

public class TurnoCaja : BaseEntity
{
    public DateTime FechaApertura { get; set; } = DateTime.UtcNow;
    public DateTime? FechaCierre { get; set; }
    public decimal MontoInicialEfectivo { get; set; }
    public decimal? MontoCierreEfectivoReal { get; set; }
    public decimal? MontoCierreEfectivoSistema { get; set; }
    public decimal? DiferenciaEfectivo { get; set; }

    public string UsuarioApertura { get; set; } = string.Empty;
    public string? UsuarioCierre { get; set; }
    public string? ObservacionesCierre { get; set; }
    public bool EstaAbierto => FechaCierre == null;

    public ICollection<MovimientoCaja> Movimientos { get; set; } = new List<MovimientoCaja>();
}
