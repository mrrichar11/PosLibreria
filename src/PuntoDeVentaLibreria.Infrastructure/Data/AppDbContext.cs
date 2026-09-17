using Microsoft.EntityFrameworkCore;
using PuntoDeVentaLibreria.Domain.Entities.Catalogo;
using PuntoDeVentaLibreria.Domain.Entities.Clientes;
using PuntoDeVentaLibreria.Domain.Entities.Combos;
using PuntoDeVentaLibreria.Domain.Entities.Configuracion;
using PuntoDeVentaLibreria.Domain.Entities.Finanzas;
using PuntoDeVentaLibreria.Domain.Entities.Inventario;
using PuntoDeVentaLibreria.Domain.Entities.Proveedores;
using PuntoDeVentaLibreria.Domain.Entities.Seguridad;
using PuntoDeVentaLibreria.Domain.Entities.Ventas;

namespace PuntoDeVentaLibreria.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Articulo> Articulos => Set<Articulo>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Marca> Marcas => Set<Marca>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<ComboItem> ComboItems => Set<ComboItem>();
    public DbSet<MovimientoStock> MovimientosStock => Set<MovimientoStock>();

    public DbSet<TurnoCaja> TurnosCaja => Set<TurnoCaja>();
    public DbSet<MovimientoCaja> MovimientosCaja => Set<MovimientoCaja>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<LineaVenta> LineasVenta => Set<LineaVenta>();
    public DbSet<PagoVenta> PagosVenta => Set<PagoVenta>();

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<ConfiguracionNegocio> Configuraciones => Set<ConfiguracionNegocio>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<LicenciaSistema> Licencias => Set<LicenciaSistema>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Relaciones de Combos Escolares
        modelBuilder.Entity<ComboItem>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.HasOne(c => c.ComboArticulo)
                  .WithMany(a => a.ItemsDelCombo)
                  .HasForeignKey(c => c.ComboArticuloId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.ComponenteArticulo)
                  .WithMany(a => a.ComoComponenteEnCombos)
                  .HasForeignKey(c => c.ComponenteArticuloId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Proveedor
        modelBuilder.Entity<Proveedor>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.Nombre);
        });

        // Articulo
        modelBuilder.Entity<Articulo>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => a.CodigoBarras);
            entity.HasIndex(a => a.SKU);
            entity.HasIndex(a => a.CodigoProveedor);

            entity.HasOne(a => a.Proveedor)
                  .WithMany(p => p.Articulos)
                  .HasForeignKey(a => a.ProveedorId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
