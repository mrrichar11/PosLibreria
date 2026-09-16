using PuntoDeVentaLibreria.Application.DTOs.Backup;

namespace PuntoDeVentaLibreria.Application.Services;

public interface IBackupService
{
    Task<BackupInfoDto> CrearBackupAsync(string? rutaDestino = null, bool esAutomaticoCierre = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BackupInfoDto>> ObtenerHistorialBackupsAsync(CancellationToken cancellationToken = default);
    Task<bool> RestaurarBackupAsync(string rutaArchivoBackup, CancellationToken cancellationToken = default);
    string ObtenerCarpetaBackupsPredeterminada();
}
