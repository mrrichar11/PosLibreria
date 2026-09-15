using Microsoft.EntityFrameworkCore;

namespace PuntoDeVentaLibreria.Infrastructure.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        // 1. Asegura creación de tablas
        await context.Database.EnsureCreatedAsync(cancellationToken);

        // 2. Optimización WAL para SQLite
        try
        {
            await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode = WAL;", cancellationToken);
            await context.Database.ExecuteSqlRawAsync("PRAGMA synchronous = NORMAL;", cancellationToken);
        }
        catch { }
    }
}
