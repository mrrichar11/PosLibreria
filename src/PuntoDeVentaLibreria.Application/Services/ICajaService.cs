using PuntoDeVentaLibreria.Domain.Entities.Finanzas;

namespace PuntoDeVentaLibreria.Application.Services;

public interface ICajaService
{
    Task<TurnoCaja?> ObtenerTurnoActivoAsync(CancellationToken ct = default);
    Task<TurnoCaja> AbrirTurnoAsync(decimal montoInicial, string usuario, CancellationToken ct = default);
    Task<TurnoCaja> CerrarTurnoAsync(decimal montoEfectivoReal, string usuario, string? observaciones, CancellationToken ct = default);
    Task<MovimientoCaja> RegistrarGastoOperativoAsync(decimal monto, string concepto, string usuario, CancellationToken ct = default);
    Task<MovimientoCaja> RegistrarRetiroDueñoAsync(decimal monto, string concepto, string usuario, CancellationToken ct = default);
}
