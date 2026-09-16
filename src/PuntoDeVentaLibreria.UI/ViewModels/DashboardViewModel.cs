using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PuntoDeVentaLibreria.Application.DTOs.Dashboard;
using PuntoDeVentaLibreria.Application.Services;

namespace PuntoDeVentaLibreria.UI.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IReporteService _reporteService;

    [ObservableProperty]
    private DashboardReporteDto _datos = new();

    [ObservableProperty]
    private bool _estaCargando;

    [ObservableProperty]
    private string _filtroSeleccionado = "Hoy";

    [ObservableProperty]
    private DateTime _fechaDesde = DateTime.Today.AddDays(-7);

    [ObservableProperty]
    private DateTime _fechaHasta = DateTime.Today;

    public ObservableCollection<MetricaMedioPagoDto> MetodosPago { get; } = new();
    public ObservableCollection<ArticuloMasVendidoDto> TopArticulos { get; } = new();

    public DashboardViewModel(IReporteService reporteService)
    {
        _reporteService = reporteService ?? throw new ArgumentNullException(nameof(reporteService));
    }

    public async Task CargarDatosAsync()
    {
        switch (FiltroSeleccionado)
        {
            case "Semana":
                await FiltrarSemanaAsync();
                break;
            case "Mes":
                await FiltrarMesAsync();
                break;
            case "Historico":
                await FiltrarHistoricoAsync();
                break;
            default:
                await FiltrarHoyAsync();
                break;
        }
    }

    [RelayCommand]
    private async Task FiltrarHoyAsync()
    {
        FiltroSeleccionado = "Hoy";
        var ahora = DateTime.Now;
        var desde = new DateTime(ahora.Year, ahora.Month, ahora.Day, 0, 0, 0, DateTimeKind.Utc);
        var hasta = new DateTime(ahora.Year, ahora.Month, ahora.Day, 23, 59, 59, DateTimeKind.Utc);
        await ConsultarReporteAsync(desde, hasta, "Hoy (Jornada Actual)");
    }

    [RelayCommand]
    private async Task FiltrarSemanaAsync()
    {
        FiltroSeleccionado = "Semana";
        var ahora = DateTime.Now;
        var desde = ahora.Date.AddDays(-7);
        var hasta = new DateTime(ahora.Year, ahora.Month, ahora.Day, 23, 59, 59, DateTimeKind.Utc);
        await ConsultarReporteAsync(DateTime.SpecifyKind(desde, DateTimeKind.Utc), hasta, "Últimos 7 Días");
    }

    [RelayCommand]
    private async Task FiltrarMesAsync()
    {
        FiltroSeleccionado = "Mes";
        var ahora = DateTime.Now;
        var desde = new DateTime(ahora.Year, ahora.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var hasta = new DateTime(ahora.Year, ahora.Month, DateTime.DaysInMonth(ahora.Year, ahora.Month), 23, 59, 59, DateTimeKind.Utc);
        await ConsultarReporteAsync(desde, hasta, $"Mes Actual ({ahora:MMMM yyyy})");
    }

    [RelayCommand]
    private async Task FiltrarHistoricoAsync()
    {
        FiltroSeleccionado = "Historico";
        var desde = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var hasta = DateTime.UtcNow.AddDays(1);
        await ConsultarReporteAsync(desde, hasta, "Histórico Acumulado Completo");
    }

    [RelayCommand]
    private async Task FiltrarRangoPersonalizadoAsync()
    {
        FiltroSeleccionado = "Personalizado";
        var desde = DateTime.SpecifyKind(FechaDesde.Date, DateTimeKind.Utc);
        var hasta = DateTime.SpecifyKind(FechaHasta.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
        await ConsultarReporteAsync(desde, hasta, $"Rango: {FechaDesde:dd/MM/yyyy} a {FechaHasta:dd/MM/yyyy}");
    }

    [RelayCommand]
    private async Task ExportarReporteCsvAsync()
    {
        try
        {
            var sfd = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Exportar Reporte Financiero a CSV",
                Filter = "Archivo CSV (*.csv)|*.csv",
                FileName = $"Reporte_Ventas_{DateTime.Now:yyyyMMdd_HHmm}.csv"
            };

            if (sfd.ShowDialog() == true)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"REPORTE FINANCIERO MR SYS - {Datos.PeriodoTexto}");
                sb.AppendLine($"Generado:;{DateTime.Now:dd/MM/yyyy HH:mm}");
                sb.AppendLine($"Desde:;{Datos.FechaDesde:dd/MM/yyyy};Hasta:;{Datos.FechaHasta:dd/MM/yyyy}");
                sb.AppendLine();
                sb.AppendLine("RESUMEN GENERAL");
                sb.AppendLine($"Facturación Total Bruta:;${Datos.FacturacionTotal:F2}");
                sb.AppendLine($"Costo de Mercadería (CMV):;${Datos.CostoTotalEstimado:F2}");
                sb.AppendLine($"Utilidad Neta Estimada:;${Datos.GananciaNetaEstimada:F2}");
                sb.AppendLine($"Margen Neto Porcentual:;{Datos.MargenPorcentualPromedio}%");
                sb.AppendLine($"Cantidad de Transacciones / Ventas:;{Datos.CantidadVentas}");
                sb.AppendLine($"Unidades de Artículos Vendidos:;{Datos.CantidadArticulosVendidos}");
                sb.AppendLine($"Ticket Promedio por Venta:;${Datos.TicketPromedio:F2}");
                sb.AppendLine();
                sb.AppendLine("DESGLOSE POR MEDIO DE PAGO");
                sb.AppendLine("Medio de Pago;Total Facturado;Porcentaje %;Cantidad de Operaciones");
                foreach (var m in MetodosPago)
                {
                    sb.AppendLine($"\"{m.MedioPago}\";{m.Total:F2};{m.PorcentajeTotal}%;{m.CantidadOperaciones}");
                }
                sb.AppendLine();
                sb.AppendLine("TOP 10 ARTÍCULOS MÁS VENDIDOS");
                sb.AppendLine("Artículo / Servicio;Unidades Vendidas;Total Facturado");
                foreach (var a in TopArticulos)
                {
                    sb.AppendLine($"\"{a.Descripcion.Replace("\"", "\"\"")}\";{a.CantidadVendida};{a.TotalFacturado:F2}");
                }

                await System.IO.File.WriteAllTextAsync(sfd.FileName, sb.ToString(), System.Text.Encoding.UTF8);
                System.Windows.MessageBox.Show("Reporte exportado exitosamente a CSV para Excel.", "MR SYS Exportación", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error al exportar reporte: {ex.Message}", "MR SYS Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task ConsultarReporteAsync(DateTime desde, DateTime hasta, string periodoTexto)
    {
        if (EstaCargando) return;

        try
        {
            EstaCargando = true;
            Datos = await _reporteService.ObtenerMetricasDashboardAsync(desde, hasta, periodoTexto);

            MetodosPago.Clear();
            foreach (var m in Datos.VentasPorMedioPago)
            {
                MetodosPago.Add(m);
            }

            TopArticulos.Clear();
            foreach (var a in Datos.TopArticulos)
            {
                TopArticulos.Add(a);
            }
        }
        finally
        {
            EstaCargando = false;
        }
    }
}
