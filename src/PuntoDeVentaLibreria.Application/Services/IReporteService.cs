using PuntoDeVentaLibreria.Application.DTOs.Dashboard;

namespace PuntoDeVentaLibreria.Application.Services;

public interface IReporteService
{
    Task<DashboardReporteDto> ObtenerMetricasDashboardAsync(DateTime fechaDesde, DateTime fechaHasta, string periodoTexto = "Período", CancellationToken ct = default);
}
