using System.IO.Compression;
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
        try
        {
            var config = _context.Configuraciones.AsNoTracking().FirstOrDefault();
            if (config != null && !string.IsNullOrWhiteSpace(config.CarpetaBackupsPersonalizada) && Directory.Exists(config.CarpetaBackupsPersonalizada))
            {
                return config.CarpetaBackupsPersonalizada;
            }
        }
        catch { }

        var carpetaBase = AppDomain.CurrentDomain.BaseDirectory;
        var carpetaBackups = Path.Combine(carpetaBase, "Backups");
        if (!Directory.Exists(carpetaBackups))
        {
            Directory.CreateDirectory(carpetaBackups);
        }
        return carpetaBackups;
    }

    public async Task<BackupInfoDto> CrearBackupAsync(string? rutaDestino = null, string tipoBackup = "manual", CancellationToken cancellationToken = default)
    {
        var carpetaBackups = ObtenerCarpetaBackupsPredeterminada();

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var prefijoLimpio = tipoBackup.ToLowerInvariant().Replace(" ", "_");
        
        string archivoZipFinal;
        if (string.IsNullOrWhiteSpace(rutaDestino))
        {
            archivoZipFinal = Path.Combine(carpetaBackups, $"backup_MR_SYS_{prefijoLimpio}_{timestamp}.zip");
        }
        else
        {
            archivoZipFinal = rutaDestino.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) 
                ? rutaDestino 
                : Path.Combine(rutaDestino, $"backup_MR_SYS_{prefijoLimpio}_{timestamp}.zip");
        }

        var dirDestino = Path.GetDirectoryName(archivoZipFinal);
        if (!string.IsNullOrEmpty(dirDestino) && !Directory.Exists(dirDestino))
        {
            Directory.CreateDirectory(dirDestino);
        }

        // Crear snapshot temporal de SQLite con VACUUM INTO
        var tempSnapshotDb = Path.Combine(Path.GetTempPath(), $"mrsys_temp_snap_{Guid.NewGuid():N}.db");
        if (File.Exists(tempSnapshotDb))
        {
            File.Delete(tempSnapshotDb);
        }

        var rutaSqlite = tempSnapshotDb.Replace('\\', '/').Replace("'", "''");

#pragma warning disable EF1002
        await _context.Database.ExecuteSqlRawAsync($"VACUUM INTO '{rutaSqlite}';", cancellationToken);
#pragma warning restore EF1002

        try
        {
            if (File.Exists(archivoZipFinal))
            {
                File.Delete(archivoZipFinal);
            }

            // Comprimir la base en un archivo ZIP con nombre legible
            using (var zip = ZipFile.Open(archivoZipFinal, ZipArchiveMode.Create))
            {
                zip.CreateEntryFromFile(tempSnapshotDb, "punto_venta_libreria.db", CompressionLevel.Optimal);
            }
        }
        finally
        {
            if (File.Exists(tempSnapshotDb))
            {
                try { File.Delete(tempSnapshotDb); } catch { }
            }
        }

        // Purgar backups antiguos según política de retención
        try
        {
            var config = await _context.Configuraciones.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
            var diasRetencion = config?.DiasRetencionBackups ?? 30;
            await PurgarBackupsAntiguosAsync(diasRetencion, cancellationToken);
        }
        catch { }

        var fileInfo = new FileInfo(archivoZipFinal);
        return new BackupInfoDto
        {
            NombreArchivo = fileInfo.Name,
            RutaCompleta = fileInfo.FullName,
            TamañoBytes = fileInfo.Length,
            FechaCreacion = fileInfo.CreationTime,
            EsAutomaticoCierre = tipoBackup.Contains("cierre", StringComparison.OrdinalIgnoreCase),
            TipoBackup = MapearTipoLegible(tipoBackup)
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

        var archivos = dirInfo.GetFiles("*.*")
            .Where(f => f.Extension.Equals(".zip", StringComparison.OrdinalIgnoreCase) || f.Extension.Equals(".db", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(f => f.CreationTime)
            .Select(f => new BackupInfoDto
            {
                NombreArchivo = f.Name,
                RutaCompleta = f.FullName,
                TamañoBytes = f.Length,
                FechaCreacion = f.CreationTime,
                EsAutomaticoCierre = f.Name.Contains("cierre_caja", StringComparison.OrdinalIgnoreCase),
                TipoBackup = InferirTipoDesdeNombre(f.Name)
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<BackupInfoDto>>(archivos);
    }

    public async Task<bool> RestaurarBackupAsync(string rutaArchivoBackup, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(rutaArchivoBackup))
            return false;

        // 1. Crear copia de seguridad preventiva antes de sobreescribir
        try
        {
            await CrearBackupAsync(null, "antes_de_restaurar", cancellationToken);
        }
        catch { }

        var dbActual = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "punto_venta_libreria.db");

        if (rutaArchivoBackup.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"mrsys_restore_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                ZipFile.ExtractToDirectory(rutaArchivoBackup, tempDir, true);
                var extractedDb = Directory.GetFiles(tempDir, "*.db").FirstOrDefault();
                if (extractedDb == null || !File.Exists(extractedDb))
                {
                    return false;
                }

                File.Copy(extractedDb, dbActual, true);
                return true;
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
        else if (rutaArchivoBackup.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
        {
            File.Copy(rutaArchivoBackup, dbActual, true);
            return true;
        }

        return false;
    }

    public Task<int> PurgarBackupsAntiguosAsync(int diasRetencion = 30, CancellationToken cancellationToken = default)
    {
        if (diasRetencion <= 0) return Task.FromResult(0);

        var carpeta = ObtenerCarpetaBackupsPredeterminada();
        var dirInfo = new DirectoryInfo(carpeta);
        if (!dirInfo.Exists) return Task.FromResult(0);

        var fechaLimite = DateTime.Now.AddDays(-diasRetencion);
        int borrados = 0;

        foreach (var archivo in dirInfo.GetFiles("*.*"))
        {
            if ((archivo.Extension.Equals(".zip", StringComparison.OrdinalIgnoreCase) || archivo.Extension.Equals(".db", StringComparison.OrdinalIgnoreCase))
                && archivo.CreationTime < fechaLimite)
            {
                try
                {
                    archivo.Delete();
                    borrados++;
                }
                catch { }
            }
        }

        return Task.FromResult(borrados);
    }

    public Task<bool> EliminarBackupAsync(string rutaCompleta, CancellationToken cancellationToken = default)
    {
        if (File.Exists(rutaCompleta))
        {
            try
            {
                File.Delete(rutaCompleta);
                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }
        return Task.FromResult(false);
    }

    private static string MapearTipoLegible(string tipo)
    {
        if (tipo.Contains("cierre_caja", StringComparison.OrdinalIgnoreCase)) return "Cierre de Caja";
        if (tipo.Contains("cierre_sistema", StringComparison.OrdinalIgnoreCase)) return "Cierre de Sistema";
        if (tipo.Contains("antes_de_restaurar", StringComparison.OrdinalIgnoreCase)) return "Preventivo Pre-Restauración";
        return "Manual";
    }

    private static string InferirTipoDesdeNombre(string nombre)
    {
        if (nombre.Contains("cierre_caja", StringComparison.OrdinalIgnoreCase)) return "Cierre de Caja";
        if (nombre.Contains("cierre_sistema", StringComparison.OrdinalIgnoreCase)) return "Cierre de Sistema";
        if (nombre.Contains("antes_de_restaurar", StringComparison.OrdinalIgnoreCase)) return "Preventivo Pre-Restauración";
        return "Manual";
    }
}

