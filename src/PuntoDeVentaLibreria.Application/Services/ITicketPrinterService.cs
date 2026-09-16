using PuntoDeVentaLibreria.Application.DTOs.Peripherals;
using PuntoDeVentaLibreria.Application.DTOs.Ventas;

namespace PuntoDeVentaLibreria.Application.Services;

public interface ITicketPrinterService
{
    Task<bool> ImprimirTicketVentaAsync(VentaRealizadaDto venta, ConfiguracionTicketDto config, CancellationToken cancellationToken = default);
    Task<bool> AbrirCajonDineroAsync(string nombreImpresora, CancellationToken cancellationToken = default);
    Task<string> GenerarTicketTextoAsync(VentaRealizadaDto venta, ConfiguracionTicketDto config, CancellationToken cancellationToken = default);
}
