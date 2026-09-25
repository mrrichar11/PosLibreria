using Microsoft.EntityFrameworkCore;

namespace PuntoDeVentaLibreria.Infrastructure.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        // 1. Asegura creación de tablas
        await context.Database.EnsureCreatedAsync(cancellationToken);

        // 2. Migración segura de nuevas columnas si la base de datos ya existía
        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Configuraciones ADD COLUMN Pais TEXT NOT NULL DEFAULT 'Argentina';", cancellationToken);
        }
        catch { }

        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Configuraciones ADD COLUMN SimboloMoneda TEXT NOT NULL DEFAULT '$';", cancellationToken);
        }
        catch { }

        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Configuraciones ADD COLUMN BilletesHabilitados TEXT NOT NULL DEFAULT '100,200,500,1000,2000,10000,20000';", cancellationToken);
        }
        catch { }

        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Configuraciones ADD COLUMN ImpresoraTickets TEXT NOT NULL DEFAULT '';", cancellationToken);
        }
        catch { }

        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Configuraciones ADD COLUMN AnchoPapelMm INTEGER NOT NULL DEFAULT 80;", cancellationToken);
        }
        catch { }

        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Configuraciones ADD COLUMN MensajePieTicket TEXT NOT NULL DEFAULT '¡Muchas gracias por su compra!\nCambios con ticket dentro de los 15 días.\nNo se aceptan cambios de fotocopias.';", cancellationToken);
        }
        catch { }

        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Configuraciones ADD COLUMN ImprimirAutomaticoAlCobrar INTEGER NOT NULL DEFAULT 0;", cancellationToken);
        }
        catch { }

        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Configuraciones ADD COLUMN MostrarVistaPreviaTicket INTEGER NOT NULL DEFAULT 1;", cancellationToken);
        }
        catch { }

        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE MovimientosCaja ADD COLUMN ClienteId TEXT NULL;", cancellationToken);
        }
        catch { }

        // Nuevas columnas para Backups y Terminales Multi-PC
        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Configuraciones ADD COLUMN CarpetaBackupsPersonalizada TEXT NULL;", cancellationToken);
        }
        catch { }

        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Configuraciones ADD COLUMN BackupAutomaticoAlCerrarSistema INTEGER NOT NULL DEFAULT 1;", cancellationToken);
        }
        catch { }

        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Configuraciones ADD COLUMN BackupAutomaticoAlCierreCaja INTEGER NOT NULL DEFAULT 1;", cancellationToken);
        }
        catch { }

        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Configuraciones ADD COLUMN DiasRetencionBackups INTEGER NOT NULL DEFAULT 30;", cancellationToken);
        }
        catch { }

        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Configuraciones ADD COLUMN NombreTerminal TEXT NOT NULL DEFAULT 'Caja Principal';", cancellationToken);
        }
        catch { }

        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Configuraciones ADD COLUMN ModoCajaMultiTerminal TEXT NOT NULL DEFAULT 'Compartida';", cancellationToken);
        }
        catch { }

        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Configuraciones ADD COLUMN MotorBaseDatos TEXT NOT NULL DEFAULT 'SQLite';", cancellationToken);
        }
        catch { }

        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Configuraciones ADD COLUMN ServidorPostgres TEXT NOT NULL DEFAULT 'localhost';", cancellationToken);
        }
        catch { }

        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Configuraciones ADD COLUMN PuertoPostgres INTEGER NOT NULL DEFAULT 5432;", cancellationToken);
        }
        catch { }

        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Configuraciones ADD COLUMN BaseDatosPostgres TEXT NOT NULL DEFAULT 'mr_sys_libreria';", cancellationToken);
        }
        catch { }

        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Configuraciones ADD COLUMN UsuarioPostgres TEXT NOT NULL DEFAULT 'postgres';", cancellationToken);
        }
        catch { }

        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Configuraciones ADD COLUMN PasswordPostgres TEXT NOT NULL DEFAULT '';", cancellationToken);
        }
        catch { }

        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE TurnosCaja ADD COLUMN NombreTerminal TEXT NOT NULL DEFAULT 'Caja Principal';", cancellationToken);
        }
        catch { }

        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE TurnosCaja ADD COLUMN ModoTerminal TEXT NOT NULL DEFAULT 'Compartida';", cancellationToken);
        }
        catch { }

        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Ventas ADD COLUMN NombreTerminal TEXT NOT NULL DEFAULT 'Caja Principal';", cancellationToken);
        }
        catch { }

        // 2.1 Tablas y columnas añadidas para Proveedores y multi-código
        try
        {
            await context.Database.ExecuteSqlRawAsync(@"CREATE TABLE IF NOT EXISTS Proveedores (
                Id TEXT PRIMARY KEY,
                Nombre TEXT NOT NULL,
                RazonSocial TEXT NULL,
                Cuit TEXT NULL,
                Contacto TEXT NULL,
                Telefono TEXT NULL,
                Email TEXT NULL,
                Direccion TEXT NULL,
                DiasVisitaOEntrega TEXT NULL,
                Notas TEXT NULL,
                FechaCreacion TEXT NOT NULL,
                FechaModificacion TEXT NULL,
                Activo INTEGER NOT NULL DEFAULT 1
            );", cancellationToken);
        }
        catch { }

        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Proveedores ADD COLUMN Activo INTEGER NOT NULL DEFAULT 1;", cancellationToken);
        }
        catch { }

        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Articulos ADD COLUMN CodigosBarrasSecundarios TEXT NULL;", cancellationToken);
        }
        catch { }

        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Articulos ADD COLUMN CodigoProveedor TEXT NULL;", cancellationToken);
        }
        catch { }

        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Articulos ADD COLUMN ProveedorId TEXT NULL;", cancellationToken);
        }
        catch { }

        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Articulos ADD COLUMN IvaPorcentaje REAL NOT NULL DEFAULT 21.0;", cancellationToken);
        }
        catch { }

        // 2.2 Columnas para Rubro Librería/Regalería, Packs Fraccionables y Auditoría de Stock (v1.2.3)
        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Articulos ADD COLUMN Rubro TEXT NOT NULL DEFAULT 'Librería';", cancellationToken);
        }
        catch { }

        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Articulos ADD COLUMN EsPack INTEGER NOT NULL DEFAULT 0;", cancellationToken);
        }
        catch { }

        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Articulos ADD COLUMN ArticuloBaseId TEXT NULL;", cancellationToken);
        }
        catch { }

        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Articulos ADD COLUMN CantidadPorPack NUMERIC NOT NULL DEFAULT 1;", cancellationToken);
        }
        catch { }

        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Articulos ADD COLUMN UltimaAuditoriaStock TEXT NULL;", cancellationToken);
        }
        catch { }

        // 2.3 Soporte para Artículos con Variantes (Colores/Modelos) (v1.2.6)
        try
        {
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ArticuloVariantes (
                    Id TEXT PRIMARY KEY,
                    ArticuloId TEXT NOT NULL,
                    Nombre TEXT NOT NULL,
                    CodigoBarras TEXT NULL,
                    CodigoProveedor TEXT NULL,
                    StockActual NUMERIC NOT NULL DEFAULT 0,
                    StockMinimo NUMERIC NOT NULL DEFAULT 2,
                    Activo INTEGER NOT NULL DEFAULT 1,
                    UltimaAuditoriaStock TEXT NULL,
                    FechaCreacion TEXT NOT NULL DEFAULT (CURRENT_TIMESTAMP),
                    FechaModificacion TEXT NULL,
                    FOREIGN KEY(ArticuloId) REFERENCES Articulos(Id) ON DELETE CASCADE
                );", cancellationToken);
        }
        catch { }

        try
        {
            await context.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS IX_ArticuloVariantes_ArticuloId ON ArticuloVariantes(ArticuloId);", cancellationToken);
            await context.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS IX_ArticuloVariantes_CodigoBarras ON ArticuloVariantes(CodigoBarras);", cancellationToken);
        }
        catch { }

        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE LineasVenta ADD COLUMN ArticuloVarianteId TEXT NULL;", cancellationToken);
        }
        catch { }

        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE LineasVenta ADD COLUMN VarianteNombre TEXT NULL;", cancellationToken);
        }
        catch { }

        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE MovimientosStock ADD COLUMN ArticuloVarianteId TEXT NULL;", cancellationToken);
        }
        catch { }

        // Nuevas columnas para Joyería / Moneda Extranjera (USD)
        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Configuraciones ADD COLUMN CotizacionDolar NUMERIC NOT NULL DEFAULT 1350.0;", cancellationToken);
        }
        catch { }

        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Configuraciones ADD COLUMN FechaCotizacionDolar TEXT NULL;", cancellationToken);
        }
        catch { }

        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Articulos ADD COLUMN EsPrecioDolar INTEGER NOT NULL DEFAULT 0;", cancellationToken);
        }
        catch { }

        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Articulos ADD COLUMN PrecioCostoDolar NUMERIC NOT NULL DEFAULT 0.0;", cancellationToken);
        }
        catch { }

        // 3. Optimización WAL para SQLite
        try
        {
            await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode = WAL;", cancellationToken);
            await context.Database.ExecuteSqlRawAsync("PRAGMA synchronous = NORMAL;", cancellationToken);
        }
        catch { }
    }
}
