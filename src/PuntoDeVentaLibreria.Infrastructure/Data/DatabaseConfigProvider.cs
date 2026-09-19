using System.Text.Json;
using Npgsql;

namespace PuntoDeVentaLibreria.Infrastructure.Data;

public class DatabaseConfig
{
    public string Motor { get; set; } = "SQLite"; // "SQLite" o "PostgreSQL"
    public string ServidorPostgres { get; set; } = "localhost";
    public int PuertoPostgres { get; set; } = 5432;
    public string BaseDatosPostgres { get; set; } = "mr_sys_libreria";
    public string UsuarioPostgres { get; set; } = "postgres";
    public string PasswordPostgres { get; set; } = string.Empty;
    public bool SslRequerido { get; set; } = true;
    public string NombreTerminal { get; set; } = "Caja Principal";
    public string ModoCajaMultiTerminal { get; set; } = "Compartida";
}

public static class DatabaseConfigProvider
{
    private static readonly string ConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "database_config.json");

    public static DatabaseConfig Cargar()
    {
        try
        {
            if (File.Exists(ConfigFilePath))
            {
                var json = File.ReadAllText(ConfigFilePath);
                var config = JsonSerializer.Deserialize<DatabaseConfig>(json);
                if (config != null)
                {
                    return config;
                }
            }
        }
        catch { }

        return new DatabaseConfig();
    }

    public static void Guardar(DatabaseConfig config)
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(config, options);
            File.WriteAllText(ConfigFilePath, json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"No se pudo guardar la configuración de base de datos: {ex.Message}", ex);
        }
    }

    public static string ObtenerCadenaConexion(DatabaseConfig config)
    {
        if (config.Motor.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
        {
            var sslMode = config.SslRequerido ? "Prefer" : "Disable";
            return $"Host={config.ServidorPostgres};Port={config.PuertoPostgres};Database={config.BaseDatosPostgres};Username={config.UsuarioPostgres};Password={config.PasswordPostgres};SSL Mode={sslMode};Timeout=10;";
        }

        var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "punto_venta_libreria.db");
        return $"Data Source={dbPath}";
    }

    public static async Task<(bool Exito, string Mensaje)> ProbarConexionPostgresAsync(DatabaseConfig config, CancellationToken cancellationToken = default)
    {
        try
        {
            var connStr = ObtenerCadenaConexion(config);
            await using var conn = new NpgsqlConnection(connStr);
            await conn.OpenAsync(cancellationToken);
            await using var cmd = new NpgsqlCommand("SELECT version();", conn);
            var version = await cmd.ExecuteScalarAsync(cancellationToken);
            return (true, $"¡Conexión exitosa! Versión del servidor: {version}");
        }
        catch (Exception ex)
        {
            return (false, $"Error al conectar a PostgreSQL:\n{ex.Message}");
        }
    }
}
