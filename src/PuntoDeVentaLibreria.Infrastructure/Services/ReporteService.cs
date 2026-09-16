using Microsoft.EntityFrameworkCore;
using PuntoDeVentaLibreria.Application.DTOs.Dashboard;
using PuntoDeVentaLibreria.Application.Services;
using PuntoDeVentaLibreria.Infrastructure.Data;

namespace PuntoDeVentaLibreria.Infrastructure.Services;

public class ReporteService : IReporteService
{
    private readonly AppDbContext _context;

    public ReporteService(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<DashboardReporteDto> ObtenerMetricasDashboardAsync(
        DateTime fechaDesde,
        DateTime fechaHasta,
        string periodoTexto = "Período",
        CancellationToken ct = default)
    {
        // Traer ventas del rango temporal especificado
        var ventas = await _context.Ventas
            .AsNoTracking()
            .Include(v => v.LineasVenta)
            .Include(v => v.Pagos)
            .Where(v => v.FechaVenta >= fechaDesde && v.FechaVenta <= fechaHasta)
            .ToListAsync(ct);

        decimal facturacionTotal = ventas.Sum(v => v.TotalVenta);
        decimal costoTotal = ventas.Sum(v => v.TotalCostoHistorico);
        int cantidadVentas = ventas.Count;
        decimal cantidadArticulosVendidos = ventas.SelectMany(v => v.LineasVenta).Sum(l => l.Cantidad);

        // Desglose por Medio de Pago
        var pagos = ventas.SelectMany(v => v.Pagos).ToList();
        var mediosGroup = pagos
            .GroupBy(p => string.IsNullOrWhiteSpace(p.MetodoPago) ? "Efectivo" : p.MetodoPago)
            .Select(g =>
            {
                var total = g.Sum(x => x.Monto);
                var pct = facturacionTotal > 0 ? (double)Math.Round((total / facturacionTotal) * 100, 1) : 0;
                return new MetricaMedioPagoDto
                {
                    MedioPago = FormatearNombreMedioPago(g.Key),
                    Total = total,
                    CantidadOperaciones = g.Count(),
                    PorcentajeTotal = pct
                };
            })
            .OrderByDescending(m => m.Total)
            .ToList();

        // Top 10 Artículos Más Vendidos
        var todasLineas = ventas.SelectMany(v => v.LineasVenta).ToList();
        var topArticulos = todasLineas
            .GroupBy(l => l.Descripcion)
            .Select(g => new ArticuloMasVendidoDto
            {
                Descripcion = g.Key,
                Categoria = "Útiles / Servicios",
                CantidadVendida = g.Sum(x => x.Cantidad),
                TotalFacturado = g.Sum(x => x.Subtotal)
            })
            .OrderByDescending(a => a.CantidadVendida)
            .ThenByDescending(a => a.TotalFacturado)
            .Take(10)
            .ToList();

        return new DashboardReporteDto
        {
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta,
            PeriodoTexto = periodoTexto,
            FacturacionTotal = facturacionTotal,
            CostoTotalEstimado = costoTotal,
            CantidadVentas = cantidadVentas,
            CantidadArticulosVendidos = cantidadArticulosVendidos,
            VentasPorMedioPago = mediosGroup,
            TopArticulos = topArticulos
        };
    }

    private static string FormatearNombreMedioPago(string medio)
    {
        return medio switch
        {
            "Efectivo" => "💵 Efectivo",
            "Debito" => "💳 Tarjeta Débito",
            "Credito" => "💳 Tarjeta Crédito",
            "Transferencia" => "📱 Transferencia / QR",
            "CtaCte" => "📒 Cta. Cte. / Fiado",
            _ => medio
        };
    }
}
