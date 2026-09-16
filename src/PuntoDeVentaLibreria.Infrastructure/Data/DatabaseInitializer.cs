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

        // 3. Optimización WAL para SQLite
        try
        {
            await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode = WAL;", cancellationToken);
            await context.Database.ExecuteSqlRawAsync("PRAGMA synchronous = NORMAL;", cancellationToken);
        }
        catch { }
    }
}
