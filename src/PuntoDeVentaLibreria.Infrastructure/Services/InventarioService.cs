using Microsoft.EntityFrameworkCore;
using PuntoDeVentaLibreria.Application.DTOs.Inventario;
using PuntoDeVentaLibreria.Application.Services;
using PuntoDeVentaLibreria.Domain.Entities.Catalogo;
using PuntoDeVentaLibreria.Infrastructure.Data;

namespace PuntoDeVentaLibreria.Infrastructure.Services;

public class InventarioService : IInventarioService
{
    private readonly AppDbContext _context;

    public InventarioService(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<ArticuloDto>> BuscarArticulosAsync(string criterio, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(criterio))
        {
            var articulos = await _context.Articulos
                .AsNoTracking()
                .Include(a => a.Categoria)
                .Include(a => a.Marca)
                .Where(a => a.Activo)
                .OrderBy(a => a.Nombre)
                .Take(50)
                .ToListAsync(ct);

            return articulos.Select(MapToDto).ToList();
        }

        var limpio = criterio.Trim().ToUpper();

        // 1. Coincidencia exacta de Código de Barras o SKU (prioridad escáner)
        var exacto = await _context.Articulos
            .AsNoTracking()
            .Include(a => a.Categoria)
            .Include(a => a.Marca)
            .FirstOrDefaultAsync(a => a.Activo && (a.CodigoBarras == limpio || a.SKU == limpio), ct);

        if (exacto != null)
        {
            return new List<ArticuloDto> { MapToDto(exacto) };
        }

        // 2. Coincidencia por múltiples términos
        var tokens = limpio.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var query = _context.Articulos
            .AsNoTracking()
            .Include(a => a.Categoria)
            .Include(a => a.Marca)
            .Where(a => a.Activo);

        foreach (var token in tokens)
        {
            query = query.Where(a =>
                a.Nombre.ToUpper().Contains(token) ||
                a.SKU.ToUpper().Contains(token) ||
                (a.CodigoBarras != null && a.CodigoBarras.Contains(token)) ||
                (a.Marca != null && a.Marca.Nombre.ToUpper().Contains(token)) ||
                (a.Categoria != null && a.Categoria.Nombre.ToUpper().Contains(token)));
        }

        var lista = await query.Take(50).ToListAsync(ct);
        return lista.Select(MapToDto).ToList();
    }

    public async Task<ArticuloDto?> BuscarPorCodigoBarrasAsync(string codigoBarras, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(codigoBarras)) return null;
        var limpio = codigoBarras.Trim();

        var art = await _context.Articulos
            .AsNoTracking()
            .Include(a => a.Categoria)
            .Include(a => a.Marca)
            .Include(a => a.ItemsDelCombo)
                .ThenInclude(c => c.ComponenteArticulo)
            .FirstOrDefaultAsync(a => a.Activo && (a.CodigoBarras == limpio || a.SKU == limpio), ct);

        return art != null ? MapToDto(art) : null;
    }

    public async Task<IReadOnlyList<ArticuloDto>> ObtenerBotonesRapidosAsync(CancellationToken ct = default)
    {
        var articulos = await _context.Articulos
            .AsNoTracking()
            .Include(a => a.Categoria)
            .Include(a => a.Marca)
            .Where(a => a.Activo && a.EsBotonRapido)
            .OrderBy(a => a.Nombre)
            .ToListAsync(ct);

        return articulos.Select(MapToDto).ToList();
    }

    public async Task<ArticuloDto> GuardarArticuloAsync(ArticuloDto dto, CancellationToken ct = default)
    {
        Articulo? entidad;
        if (dto.Id == Guid.Empty)
        {
            entidad = new Articulo { Id = Guid.NewGuid() };
            _context.Articulos.Add(entidad);
        }
        else
        {
            entidad = await _context.Articulos.FindAsync(new object[] { dto.Id }, ct)
                      ?? throw new InvalidOperationException("Artículo no encontrado.");
        }

        entidad.Nombre = dto.Nombre.Trim();
        entidad.SKU = dto.SKU.Trim();
        entidad.CodigoBarras = string.IsNullOrWhiteSpace(dto.CodigoBarras) ? null : dto.CodigoBarras.Trim();
        entidad.CategoriaId = dto.CategoriaId;
        entidad.MarcaId = dto.MarcaId;
        entidad.Tipo = dto.Tipo;
        entidad.PrecioCosto = dto.PrecioCosto;
        entidad.PorcentajeGanancia = dto.PorcentajeGanancia;
        entidad.PrecioVenta = dto.PrecioVenta;
        entidad.StockActual = dto.StockActual;
        entidad.StockMinimo = dto.StockMinimo;
        entidad.UnidadMedida = dto.UnidadMedida;
        entidad.Ubicacion = dto.Ubicacion;
        entidad.EsBotonRapido = dto.EsBotonRapido;
        entidad.ColorBoton = dto.ColorBoton;

        await _context.SaveChangesAsync(ct);
        return MapToDto(entidad);
    }

    private static ArticuloDto MapToDto(Articulo a) => new()
    {
        Id = a.Id,
        CodigoBarras = a.CodigoBarras,
        SKU = a.SKU,
        Nombre = a.Nombre,
        Descripcion = a.Descripcion,
        CategoriaId = a.CategoriaId,
        CategoriaNombre = a.Categoria?.Nombre ?? "",
        MarcaId = a.MarcaId,
        MarcaNombre = a.Marca?.Nombre ?? "",
        Tipo = a.Tipo,
        PrecioCosto = a.PrecioCosto,
        PorcentajeGanancia = a.PorcentajeGanancia,
        PrecioVenta = a.PrecioVenta,
        StockActual = a.StockActual,
        StockMinimo = a.StockMinimo,
        UnidadMedida = a.UnidadMedida,
        Ubicacion = a.Ubicacion,
        EsBotonRapido = a.EsBotonRapido,
        ColorBoton = a.ColorBoton,
        ComponentesDelCombo = a.ItemsDelCombo.Select(c => new ComboComponenteDto
        {
            ComponenteArticuloId = c.ComponenteArticuloId,
            NombreArticulo = c.ComponenteArticulo?.Nombre ?? "",
            SKU = c.ComponenteArticulo?.SKU ?? "",
            Cantidad = c.Cantidad,
            StockActualDisponible = c.ComponenteArticulo?.StockActual ?? 0
        }).ToList()
    };
}
