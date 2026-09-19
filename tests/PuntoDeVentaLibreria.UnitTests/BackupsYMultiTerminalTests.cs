using PuntoDeVentaLibreria.Application.DTOs.Backup;
using PuntoDeVentaLibreria.Application.DTOs.Caja;
using PuntoDeVentaLibreria.Infrastructure.Data;
using Xunit;

namespace PuntoDeVentaLibreria.UnitTests;

public class BackupsYMultiTerminalTests
{
    [Fact]
    public void BackupInfoDto_FormateoBytes_CalculaCorrectamente()
    {
        var backupKb = new BackupInfoDto
        {
            NombreArchivo = "backup_test.zip",
            TamañoBytes = 512 * 1024, // 512 KB
            FechaCreacion = DateTime.Now
        };

        var backupMb = new BackupInfoDto
        {
            NombreArchivo = "backup_test2.zip",
            TamañoBytes = 5 * 1024 * 1024, // 5 MB
            FechaCreacion = DateTime.Now
        };

        Assert.Contains("KB", backupKb.TamañoFormateado);
        Assert.Contains("MB", backupMb.TamañoFormateado);
    }

    [Fact]
    public void DatabaseConfig_CadenaConexion_PostgresYSqlite()
    {
        var configSqlite = new DatabaseConfig
        {
            Motor = "SQLite"
        };

        var connStrSqlite = DatabaseConfigProvider.ObtenerCadenaConexion(configSqlite);
        Assert.Contains("Data Source=", connStrSqlite);
        Assert.Contains("punto_venta_libreria.db", connStrSqlite);

        var configPostgres = new DatabaseConfig
        {
            Motor = "PostgreSQL",
            ServidorPostgres = "db.servidor.com",
            PuertoPostgres = 5432,
            BaseDatosPostgres = "pos_db",
            UsuarioPostgres = "admin_user",
            PasswordPostgres = "supersecret",
            SslRequerido = true
        };

        var connStrPostgres = DatabaseConfigProvider.ObtenerCadenaConexion(configPostgres);
        Assert.Contains("Host=db.servidor.com", connStrPostgres);
        Assert.Contains("Port=5432", connStrPostgres);
        Assert.Contains("Database=pos_db", connStrPostgres);
        Assert.Contains("Username=admin_user", connStrPostgres);
        Assert.Contains("Password=supersecret", connStrPostgres);
        Assert.Contains("SSL Mode=Prefer", connStrPostgres);
    }

    [Fact]
    public void ResumenCierreTurnoDto_CalculosConsolidados()
    {
        var resumen = new ResumenCierreTurnoDto
        {
            FondoInicial = 25000m,
            VentasEfectivo = 60000m,
            CobrosCtaCteEfectivo = 5000m,
            GastosOperativos = 8000m,
            RetirosDueño = 10000m,
            EfectivoRealContado = 72000m
        };

        Assert.Equal(72000m, resumen.EfectivoEsperadoEnCajon);
        Assert.Equal(0m, resumen.Diferencia);
        Assert.Equal(65000m, resumen.TotalIngresosEfectivo);
        Assert.Equal(18000m, resumen.TotalEgresosEfectivo);
    }
}

