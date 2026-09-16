using PuntoDeVentaLibreria.Application.DTOs.Seguridad;

namespace PuntoDeVentaLibreria.Application.Services;

public interface ILicenseService
{
    Task<EstadoLicenciaDto> ValidarLicenciaAsync(CancellationToken cancellationToken = default);
    Task<ResultadoActivacionDto> ActivarLicenciaAsync(string claveActivacion, CancellationToken cancellationToken = default);
    string ObtenerCodigoInstalacion();
}
