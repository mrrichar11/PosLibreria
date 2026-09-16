using Microsoft.EntityFrameworkCore;
using PuntoDeVentaLibreria.Application.DTOs.Backup;
using PuntoDeVentaLibreria.Application.Services;
using PuntoDeVentaLibreria.Infrastructure.Data;

namespace PuntoDeVentaLibreria.Infrastructure.Services;

public class BackupService : IBackupService
{
    private readonly AppDbContext _context;

    public BackupService(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public string ObtenerCarpetaBackupsPredeterminada()
    {
        var carpetaBase = AppDomain.CurrentDomain.BaseDirectory;
        var carpetaBackups = Path.Combine(carpetaBase, "Backups");
        if (!Directory.Exists(carpetaBackups))
        {
            Directory.CreateDirectory(carpetaBackups);
        }
        return carpetaBackups;
    }

    public async Task<BackupInfoDto> CrearBackupAsync(string? rutaDestino = null, bool esAutomaticoCierre = false, CancellationToken cancellationToken = default)
    {
        var carpetaBackups = ObtenerCarpetaBackupsPredeterminada();

        if (string.IsNullOrWhiteSpace(rutaDestino))
        {
            var tipoPrefijo = esAutomaticoCierre ? "cierre_caja" : "manual";
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var nombreArchivo = $"backup_libreria_{tipoPrefijo}_{timestamp}.db";
            rutaDestino = Path.Combine(carpetaBackups, nombreArchivo);
        }

        var dirDestino = Path.GetDirectoryName(rutaDestino);
        if (!string.IsNullOrEmpty(dirDestino) && !Directory.Exists(dirDestino))
        {
            Directory.CreateDirectory(dirDestino);
        }

        if (File.Exists(rutaDestino))
        {
            File.Delete(rutaDestino);
        }

        var rutaSqlite = rutaDestino.Replace('\\', '/').Replace("'", "''");

        // Snapshot atómico nativo de SQLite con VACUUM INTO (no admite parámetros @p0)
#pragma warning disable EF1002
        await _context.Database.ExecuteSqlRawAsync($"VACUUM INTO '{rutaSqlite}';", cancellationToken);
#pragma warning restore EF1002

        var fileInfo = new FileInfo(rutaDestino);

        return new BackupInfoDto
        {
            NombreArchivo = fileInfo.Name,
            RutaCompleta = fileInfo.FullName,
            TamañoBytes = fileInfo.Length,
            FechaCreacion = fileInfo.CreationTime,
            EsAutomaticoCierre = esAutomaticoCierre
        };
    }

    public Task<IReadOnlyList<BackupInfoDto>> ObtenerHistorialBackupsAsync(CancellationToken cancellationToken = default)
    {
        var carpeta = ObtenerCarpetaBackupsPredeterminada();
        var dirInfo = new DirectoryInfo(carpeta);
        if (!dirInfo.Exists)
        {
            return Task.FromResult<IReadOnlyList<BackupInfoDto>>(new List<BackupInfoDto>());
        }

        var archivos = dirInfo.GetFiles("*.db")
            .OrderByDescending(f => f.CreationTime)
            .Select(f => new BackupInfoDto
            {
                NombreArchivo = f.Name,
                RutaCompleta = f.FullName,
                TamañoBytes = f.Length,
                FechaCreacion = f.CreationTime,
                EsAutomaticoCierre = f.Name.Contains("cierre_caja")
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<BackupInfoDto>>(archivos);
    }

    public async Task<bool> RestaurarBackupAsync(string rutaArchivoBackup, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(rutaArchivoBackup))
            return false;

        // Crear primero un backup de seguridad de la base actual
        await CrearBackupAsync(null, false, cancellationToken);

        var dbActual = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "punto_venta_libreria.db");
        File.Copy(rutaArchivoBackup, dbActual, true);
        return true;
    }
}
