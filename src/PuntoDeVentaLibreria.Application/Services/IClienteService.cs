using PuntoDeVentaLibreria.Application.DTOs.Clientes;
using PuntoDeVentaLibreria.Domain.Entities.Clientes;

namespace PuntoDeVentaLibreria.Application.Services;

public interface IClienteService
{
    Task<IReadOnlyList<ClienteDto>> BuscarClientesAsync(string criterio, CancellationToken cancellationToken = default);
    Task<ClienteDto?> ObtenerPorIdAsync(Guid clienteId, CancellationToken cancellationToken = default);
    Task<Cliente> GuardarClienteAsync(ClienteDto dto, CancellationToken cancellationToken = default);
    Task EliminarClienteAsync(Guid clienteId, CancellationToken cancellationToken = default);
    Task<ReciboCobroCtaCteDto> CobrarSaldoCuentaCorrienteAsync(RegistrarEntregaCuentaCorrienteDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MovimientoClienteDto>> ObtenerHistorialClienteAsync(Guid clienteId, CancellationToken cancellationToken = default);
}
