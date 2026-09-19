namespace PuntoDeVentaLibreria.Application.Services;

public class ResultadoMigracionDbDto
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public int ArticulosMigrados { get; set; }
    public int ClientesMigrados { get; set; }
    public int ProveedoresMigrados { get; set; }
    public int VentasMigradas { get; set; }
    public int TurnosMigrados { get; set; }
    public int UsuariosMigrados { get; set; }
    public TimeSpan Duracion { get; set; }
}

public interface IDatabaseMigrationService
{
    Task<ResultadoMigracionDbDto> MigrarSqliteAPostgresAsync(string connectionStringPostgres, IProgress<string>? progreso = null, CancellationToken cancellationToken = default);
}
