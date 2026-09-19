using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using PuntoDeVentaLibreria.Application.Services;
using PuntoDeVentaLibreria.Infrastructure.Data;

namespace PuntoDeVentaLibreria.Infrastructure.Services;

public class DatabaseMigrationService : IDatabaseMigrationService
{
    private readonly AppDbContext _localContext;

    public DatabaseMigrationService(AppDbContext localContext)
    {
        _localContext = localContext ?? throw new ArgumentNullException(nameof(localContext));
    }

    public async Task<ResultadoMigracionDbDto> MigrarSqliteAPostgresAsync(string connectionStringPostgres, IProgress<string>? progreso = null, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var resultado = new ResultadoMigracionDbDto();

        try
        {
            progreso?.Report("Conectando con base de datos PostgreSQL de destino...");
            
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql(connectionStringPostgres);

            await using var postgresContext = new AppDbContext(optionsBuilder.Options);
            
            progreso?.Report("Creando esquema y tablas en PostgreSQL...");
            await postgresContext.Database.EnsureCreatedAsync(cancellationToken);

            // 1. Configuraciones
            progreso?.Report("Migrando configuraciones del comercio...");
            var configs = await _localContext.Configuraciones.AsNoTracking().ToListAsync(cancellationToken);
            foreach (var cfg in configs)
            {
                if (!await postgresContext.Configuraciones.AnyAsync(c => c.Id == cfg.Id, cancellationToken))
                {
                    postgresContext.Configuraciones.Add(cfg);
                }
            }
            await postgresContext.SaveChangesAsync(cancellationToken);

            // 2. Usuarios y Licencias
            progreso?.Report("Migrando usuarios y permisos...");
            var usuarios = await _localContext.Usuarios.AsNoTracking().ToListAsync(cancellationToken);
            foreach (var u in usuarios)
            {
                if (!await postgresContext.Usuarios.AnyAsync(x => x.Id == u.Id, cancellationToken))
                {
                    postgresContext.Usuarios.Add(u);
                }
            }
            resultado.UsuariosMigrados = usuarios.Count;

            var licencias = await _localContext.Licencias.AsNoTracking().ToListAsync(cancellationToken);
            foreach (var lic in licencias)
            {
                if (!await postgresContext.Licencias.AnyAsync(x => x.Id == lic.Id, cancellationToken))
                {
                    postgresContext.Licencias.Add(lic);
                }
            }
            await postgresContext.SaveChangesAsync(cancellationToken);

            // 3. Categorías y Marcas
            progreso?.Report("Migrando catálogo (Categorías y Marcas)...");
            var categorias = await _localContext.Categorias.AsNoTracking().ToListAsync(cancellationToken);
            foreach (var cat in categorias)
            {
                if (!await postgresContext.Categorias.AnyAsync(x => x.Id == cat.Id, cancellationToken))
                {
                    postgresContext.Categorias.Add(cat);
                }
            }

            var marcas = await _localContext.Marcas.AsNoTracking().ToListAsync(cancellationToken);
            foreach (var m in marcas)
            {
                if (!await postgresContext.Marcas.AnyAsync(x => x.Id == m.Id, cancellationToken))
                {
                    postgresContext.Marcas.Add(m);
                }
            }
            await postgresContext.SaveChangesAsync(cancellationToken);

            // 4. Proveedores
            progreso?.Report("Migrando proveedores...");
            var proveedores = await _localContext.Proveedores.AsNoTracking().ToListAsync(cancellationToken);
            foreach (var p in proveedores)
            {
                if (!await postgresContext.Proveedores.AnyAsync(x => x.Id == p.Id, cancellationToken))
                {
                    postgresContext.Proveedores.Add(p);
                }
            }
            resultado.ProveedoresMigrados = proveedores.Count;
            await postgresContext.SaveChangesAsync(cancellationToken);

            // 5. Artículos
            progreso?.Report("Migrando catálogo de artículos...");
            var articulos = await _localContext.Articulos.AsNoTracking().ToListAsync(cancellationToken);
            foreach (var a in articulos)
            {
                if (!await postgresContext.Articulos.AnyAsync(x => x.Id == a.Id, cancellationToken))
                {
                    postgresContext.Articulos.Add(a);
                }
            }
            resultado.ArticulosMigrados = articulos.Count;
            await postgresContext.SaveChangesAsync(cancellationToken);

            // 6. Combos Escolares
            progreso?.Report("Migrando combos escolares...");
            var combos = await _localContext.ComboItems.AsNoTracking().ToListAsync(cancellationToken);
            foreach (var ci in combos)
            {
                if (!await postgresContext.ComboItems.AnyAsync(x => x.Id == ci.Id, cancellationToken))
                {
                    postgresContext.ComboItems.Add(ci);
                }
            }
            await postgresContext.SaveChangesAsync(cancellationToken);

            // 7. Clientes
            progreso?.Report("Migrando cartera de clientes...");
            var clientes = await _localContext.Clientes.AsNoTracking().ToListAsync(cancellationToken);
            foreach (var c in clientes)
            {
                if (!await postgresContext.Clientes.AnyAsync(x => x.Id == c.Id, cancellationToken))
                {
                    postgresContext.Clientes.Add(c);
                }
            }
            resultado.ClientesMigrados = clientes.Count;
            await postgresContext.SaveChangesAsync(cancellationToken);

            // 8. Turnos de Caja y Movimientos
            progreso?.Report("Migrando historial de cajas...");
            var turnos = await _localContext.TurnosCaja.AsNoTracking().ToListAsync(cancellationToken);
            foreach (var t in turnos)
            {
                if (!await postgresContext.TurnosCaja.AnyAsync(x => x.Id == t.Id, cancellationToken))
                {
                    postgresContext.TurnosCaja.Add(t);
                }
            }
            resultado.TurnosMigrados = turnos.Count;
            await postgresContext.SaveChangesAsync(cancellationToken);

            var movimientos = await _localContext.MovimientosCaja.AsNoTracking().ToListAsync(cancellationToken);
            foreach (var mov in movimientos)
            {
                if (!await postgresContext.MovimientosCaja.AnyAsync(x => x.Id == mov.Id, cancellationToken))
                {
                    postgresContext.MovimientosCaja.Add(mov);
                }
            }
            await postgresContext.SaveChangesAsync(cancellationToken);

            // 9. Ventas y Líneas
            progreso?.Report("Migrando registro histórico de ventas...");
            var ventas = await _localContext.Ventas.AsNoTracking().ToListAsync(cancellationToken);
            foreach (var v in ventas)
            {
                if (!await postgresContext.Ventas.AnyAsync(x => x.Id == v.Id, cancellationToken))
                {
                    postgresContext.Ventas.Add(v);
                }
            }
            resultado.VentasMigradas = ventas.Count;
            await postgresContext.SaveChangesAsync(cancellationToken);

            var lineas = await _localContext.LineasVenta.AsNoTracking().ToListAsync(cancellationToken);
            foreach (var lv in lineas)
            {
                if (!await postgresContext.LineasVenta.AnyAsync(x => x.Id == lv.Id, cancellationToken))
                {
                    postgresContext.LineasVenta.Add(lv);
                }
            }

            var pagos = await _localContext.PagosVenta.AsNoTracking().ToListAsync(cancellationToken);
            foreach (var pv in pagos)
            {
                if (!await postgresContext.PagosVenta.AnyAsync(x => x.Id == pv.Id, cancellationToken))
                {
                    postgresContext.PagosVenta.Add(pv);
                }
            }
            await postgresContext.SaveChangesAsync(cancellationToken);

            sw.Stop();
            resultado.Exitoso = true;
            resultado.Duracion = sw.Elapsed;
            resultado.Mensaje = $"¡Migración completada con éxito en {resultado.Duracion.TotalSeconds:N1}s! {resultado.ArticulosMigrados} artículos, {resultado.ClientesMigrados} clientes, {resultado.VentasMigradas} ventas.";
            progreso?.Report("¡Migración finalizada con éxito!");
            return resultado;
        }
        catch (Exception ex)
        {
            sw.Stop();
            resultado.Exitoso = false;
            resultado.Duracion = sw.Elapsed;
            resultado.Mensaje = $"Error durante la migración:\n{ex.Message}";
            return resultado;
        }
    }
}
