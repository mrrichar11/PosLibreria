namespace PuntoDeVentaLibreria.Application.DTOs.Inventario;

public class AuditoriaStockProgresoDto
{
    public int TotalArticulos { get; set; }
    public int ArticulosAuditados { get; set; }
    public int ArticulosPendientes => Math.Max(0, TotalArticulos - ArticulosAuditados);
    public double PorcentajeCompletado => TotalArticulos > 0 ? Math.Round((double)ArticulosAuditados / TotalArticulos * 100.0, 1) : 0;
}
