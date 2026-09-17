using PuntoDeVentaLibreria.Application.DTOs.Proveedores;

namespace PuntoDeVentaLibreria.Application.Services;

public interface IProveedorService
{
    Task<IReadOnlyList<ProveedorDto>> ObtenerTodosAsync(CancellationToken ct = default);
    Task<ProveedorDto?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default);
    Task<ProveedorDto> GuardarAsync(ProveedorDto dto, CancellationToken ct = default);
    Task<bool> EliminarAsync(Guid id, CancellationToken ct = default);
}
