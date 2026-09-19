using PuntoDeVentaLibreria.Application.DTOs.Backup;

namespace PuntoDeVentaLibreria.Application.Services;

public interface IBackupService
{
    Task<BackupInfoDto> CrearBackupAsync(string? rutaDestino = null, string tipoBackup = "manual", CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BackupInfoDto>> ObtenerHistorialBackupsAsync(CancellationToken cancellationToken = default);
    Task<bool> RestaurarBackupAsync(string rutaArchivoBackup, CancellationToken cancellationToken = default);
    Task<int> PurgarBackupsAntiguosAsync(int diasRetencion = 30, CancellationToken cancellationToken = default);
    Task<bool> EliminarBackupAsync(string rutaCompleta, CancellationToken cancellationToken = default);
    string ObtenerCarpetaBackupsPredeterminada();
}

