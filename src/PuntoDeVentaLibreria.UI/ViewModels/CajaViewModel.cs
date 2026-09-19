using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PuntoDeVentaLibreria.Application.Services;
using PuntoDeVentaLibreria.Domain.Entities.Finanzas;

namespace PuntoDeVentaLibreria.UI.ViewModels;

public partial class CajaViewModel : ObservableObject
{
    private readonly ICajaService _cajaService;

    [ObservableProperty]
    private bool _esCajaAbierta;

    [ObservableProperty]
    private TurnoCaja? _turnoActivo;

    [ObservableProperty]
    private decimal _montoInicial;

    [ObservableProperty]
    private decimal _totalEfectivoEnCaja;

    [ObservableProperty]
    private decimal _totalVentasEfectivo;

    [ObservableProperty]
    private decimal _totalVentasDigitales;

    [ObservableProperty]
    private decimal _totalGastos;

    [ObservableProperty]
    private decimal _totalRetiros;

    [ObservableProperty]
    private string _nombreTerminal = "Caja Principal";

    [ObservableProperty]
    private string _modoTerminal = "Compartida";

    public ObservableCollection<MovimientoCaja> Movimientos { get; } = new();

    public event Action? OnCajaModificada;
    public Func<TurnoCaja, Task<bool>>? SolicitarCierreCajaDialogo { get; set; }
    public Func<string, Task<bool>>? SolicitarGastoRetiroDialogo { get; set; }

    private readonly IConfiguracionService _configuracionService;

    public CajaViewModel(ICajaService cajaService, IConfiguracionService configuracionService)
    {
        _cajaService = cajaService ?? throw new ArgumentNullException(nameof(cajaService));
        _configuracionService = configuracionService ?? throw new ArgumentNullException(nameof(configuracionService));
    }

    public async Task CargarDatosAsync()
    {
        try
        {
            var config = await _configuracionService.ObtenerConfiguracionAsync();
            NombreTerminal = config.NombreTerminal;
            ModoTerminal = config.ModoCajaMultiTerminal;
        }
        catch { }

        var turno = await _cajaService.ObtenerTurnoActivoAsync();
        TurnoActivo = turno;
        EsCajaAbierta = turno != null;


        Movimientos.Clear();

        if (turno != null)
        {
            MontoInicial = turno.MontoInicialEfectivo;

            var lista = turno.Movimientos.OrderByDescending(m => m.FechaCreacion).ToList();
            foreach (var m in lista)
            {
                Movimientos.Add(m);
            }

            TotalVentasEfectivo = lista.Where(m => m.Tipo == TipoMovimientoCaja.IngresoVenta && m.MetodoPago == "Efectivo").Sum(m => m.Monto);
            TotalVentasDigitales = lista.Where(m => m.Tipo == TipoMovimientoCaja.IngresoVenta && m.MetodoPago != "Efectivo").Sum(m => m.Monto);
            TotalGastos = lista.Where(m => m.Tipo == TipoMovimientoCaja.GastoOperativo).Sum(m => m.Monto);
            TotalRetiros = lista.Where(m => m.Tipo == TipoMovimientoCaja.RetiroDueño).Sum(m => m.Monto);

            TotalEfectivoEnCaja = MontoInicial + TotalVentasEfectivo - TotalGastos - TotalRetiros;
        }
        else
        {
            MontoInicial = 0;
            TotalEfectivoEnCaja = 0;
            TotalVentasEfectivo = 0;
            TotalVentasDigitales = 0;
            TotalGastos = 0;
            TotalRetiros = 0;
        }
    }

    [RelayCommand]
    private async Task AbrirCajaAsync()
    {
        if (EsCajaAbierta) return;
        await _cajaService.AbrirTurnoAsync(10000m, "Cajero");
        await CargarDatosAsync();
        OnCajaModificada?.Invoke();
    }

    [RelayCommand]
    private async Task CerrarCajaAsync()
    {
        if (!EsCajaAbierta || TurnoActivo == null) return;

        if (SolicitarCierreCajaDialogo != null)
        {
            var cerrado = await SolicitarCierreCajaDialogo(TurnoActivo);
            if (cerrado)
            {
                await CargarDatosAsync();
                OnCajaModificada?.Invoke();
            }
        }
    }

    [RelayCommand]
    private async Task RegistrarGastoAsync()
    {
        if (!EsCajaAbierta) return;

        if (SolicitarGastoRetiroDialogo != null)
        {
            var registrado = await SolicitarGastoRetiroDialogo("GastoOperativo");
            if (registrado)
            {
                await CargarDatosAsync();
                OnCajaModificada?.Invoke();
            }
        }
    }

    [RelayCommand]
    private async Task RegistrarRetiroAsync()
    {
        if (!EsCajaAbierta) return;

        if (SolicitarGastoRetiroDialogo != null)
        {
            var registrado = await SolicitarGastoRetiroDialogo("RetiroDueño");
            if (registrado)
            {
                await CargarDatosAsync();
                OnCajaModificada?.Invoke();
            }
        }
    }
}
