using PuntoDeVentaLibreria.Application.DTOs.Caja;
using PuntoDeVentaLibreria.Application.DTOs.Clientes;
using PuntoDeVentaLibreria.Application.DTOs.Peripherals;
using PuntoDeVentaLibreria.Application.DTOs.Ventas;

namespace PuntoDeVentaLibreria.Application.Services;

public interface ITicketPrinterService
{
    Task<bool> ImprimirTicketVentaAsync(VentaRealizadaDto venta, ConfiguracionTicketDto config, CancellationToken cancellationToken = default);
    Task<bool> AbrirCajonDineroAsync(string nombreImpresora, CancellationToken cancellationToken = default);
    Task<string> GenerarTicketTextoAsync(VentaRealizadaDto venta, ConfiguracionTicketDto config, CancellationToken cancellationToken = default);
    Task<string> GenerarTicketCierreCajaAsync(ResumenCierreTurnoDto resumen, ConfiguracionTicketDto config, CancellationToken cancellationToken = default);
    Task<string> GenerarTicketReciboCtaCteAsync(ReciboCobroCtaCteDto recibo, ConfiguracionTicketDto config, CancellationToken cancellationToken = default);
}
