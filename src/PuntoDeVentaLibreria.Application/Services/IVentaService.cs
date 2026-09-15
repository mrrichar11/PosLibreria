using PuntoDeVentaLibreria.Application.DTOs.Ventas;

namespace PuntoDeVentaLibreria.Application.Services;

public interface IVentaService
{
    Task<VentaRealizadaDto> ProcesarVentaAsync(RegistrarVentaDto dto, CancellationToken ct = default);
}
