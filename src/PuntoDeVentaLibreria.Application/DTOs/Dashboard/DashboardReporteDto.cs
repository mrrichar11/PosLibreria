namespace PuntoDeVentaLibreria.Application.DTOs.Dashboard;

public class MetricaMedioPagoDto
{
    public string MedioPago { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public int CantidadOperaciones { get; set; }
    public double PorcentajeTotal { get; set; }
}

public class ArticuloMasVendidoDto
{
    public string Descripcion { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public decimal CantidadVendida { get; set; }
    public decimal TotalFacturado { get; set; }
}

public class DashboardReporteDto
{
    public DateTime FechaDesde { get; set; }
    public DateTime FechaHasta { get; set; }
    public string PeriodoTexto { get; set; } = "Hoy";

    // KPIs Generales
    public decimal FacturacionTotal { get; set; }
    public decimal CostoTotalEstimado { get; set; }
    public decimal GananciaNetaEstimada => FacturacionTotal - CostoTotalEstimado;
    public decimal MargenPorcentualPromedio => FacturacionTotal > 0 ? Math.Round((GananciaNetaEstimada / FacturacionTotal) * 100, 1) : 0;
    
    public int CantidadVentas { get; set; }
    public decimal TicketPromedio => CantidadVentas > 0 ? Math.Round(FacturacionTotal / CantidadVentas, 2) : 0;
    public decimal CantidadArticulosVendidos { get; set; }

    // Desglose por Rubro (Librería vs Regalería)
    public decimal FacturacionLibreria { get; set; }
    public decimal FacturacionRegaleria { get; set; }
    public double PorcentajeLibreria => FacturacionTotal > 0 ? Math.Round((double)(FacturacionLibreria / FacturacionTotal) * 100.0, 1) : 0;
    public double PorcentajeRegaleria => FacturacionTotal > 0 ? Math.Round((double)(FacturacionRegaleria / FacturacionTotal) * 100.0, 1) : 0;
    public decimal CantidadArticulosLibreria { get; set; }
    public decimal CantidadArticulosRegaleria { get; set; }

    // Desgloses
    public List<MetricaMedioPagoDto> VentasPorMedioPago { get; set; } = new();
    public List<ArticuloMasVendidoDto> TopArticulos { get; set; } = new();
}
