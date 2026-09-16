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
