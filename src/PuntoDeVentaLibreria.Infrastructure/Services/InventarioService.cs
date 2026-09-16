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

        if (dto.Tipo == TipoArticulo.ComboKit)
        {
            if (dto.Id != Guid.Empty)
            {
                await _context.Entry(entidad).Collection(a => a.ItemsDelCombo).LoadAsync(ct);
                entidad.ItemsDelCombo.Clear();
            }

            foreach (var c in dto.ComponentesDelCombo)
            {
                entidad.ItemsDelCombo.Add(new PuntoDeVentaLibreria.Domain.Entities.Combos.ComboItem
                {
                    ComboArticuloId = entidad.Id,
                    ComponenteArticuloId = c.ComponenteArticuloId,
                    Cantidad = c.Cantidad
                });
            }
        }

        await _context.SaveChangesAsync(ct);
        return MapToDto(entidad);
    }

    public async Task<string> ExportarCatalogoCsvAsync(CancellationToken ct = default)
    {
        var articulos = await _context.Articulos
            .AsNoTracking()
            .Include(a => a.Categoria)
            .Include(a => a.Marca)
            .Where(a => a.Activo)
            .OrderBy(a => a.Nombre)
            .ToListAsync(ct);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("CodigoBarras;SKU;Nombre;Categoria;Marca;Tipo;PrecioCosto;PorcentajeGanancia;PrecioVenta;StockActual;StockMinimo;Ubicacion");

        foreach (var a in articulos)
        {
            var cod = a.CodigoBarras ?? "";
            var sku = a.SKU;
            var nom = a.Nombre.Replace(";", ",");
            var cat = a.Categoria?.Nombre.Replace(";", ",") ?? "";
            var mar = a.Marca?.Nombre.Replace(";", ",") ?? "";
            var tipo = a.Tipo.ToString();
            var costo = a.PrecioCosto.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var margen = a.PorcentajeGanancia.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var venta = a.PrecioVenta.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var stock = a.StockActual.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var min = a.StockMinimo.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var ubi = (a.Ubicacion ?? "").Replace(";", ",");

            sb.AppendLine($"{cod};{sku};{nom};{cat};{mar};{tipo};{costo};{margen};{venta};{stock};{min};{ubi}");
        }

        return sb.ToString();
    }

    public async Task<(int Creados, int Actualizados, int Errores)> ImportarCatalogoCsvAsync(string contenidoCsv, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(contenidoCsv)) return (0, 0, 0);

        int creados = 0, actualizados = 0, errores = 0;
        var lineas = contenidoCsv.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 1; i < lineas.Length; i++)
        {
            var linea = lineas[i].Trim();
            if (string.IsNullOrWhiteSpace(linea)) continue;

            try
            {
                var partes = linea.Split(';');
                if (partes.Length < 9)
                {
                    errores++;
                    continue;
                }

                var cod = partes[0].Trim();
                var sku = partes[1].Trim();
                var nom = partes[2].Trim();
                if (string.IsNullOrWhiteSpace(nom))
                {
                    errores++;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(sku))
                {
                    sku = !string.IsNullOrWhiteSpace(cod) ? cod : Guid.NewGuid().ToString().Substring(0, 8).ToUpperInvariant();
                }

                decimal.TryParse(partes[6].Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var costo);
                decimal.TryParse(partes[7].Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var margen);
                decimal.TryParse(partes[8].Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var venta);
                decimal stock = 0, min = 5;
                if (partes.Length > 9)
                    decimal.TryParse(partes[9].Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out stock);
                if (partes.Length > 10)
                    decimal.TryParse(partes[10].Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out min);
                var ubi = partes.Length > 11 ? partes[11].Trim() : null;

                var existente = await _context.Articulos.FirstOrDefaultAsync(a => a.SKU == sku || (!string.IsNullOrEmpty(cod) && a.CodigoBarras == cod), ct);
                if (existente != null)
                {
                    existente.Nombre = nom;
                    if (!string.IsNullOrEmpty(cod)) existente.CodigoBarras = cod;
                    existente.PrecioCosto = costo;
                    existente.PorcentajeGanancia = margen > 0 ? margen : 60m;
                    existente.PrecioVenta = venta > 0 ? venta : (costo * 1.6m);
                    existente.StockActual = stock;
                    existente.StockMinimo = min;
                    if (!string.IsNullOrEmpty(ubi)) existente.Ubicacion = ubi;
                    actualizados++;
                }
                else
                {
                    _context.Articulos.Add(new Articulo
                    {
                        Id = Guid.NewGuid(),
                        CodigoBarras = string.IsNullOrEmpty(cod) ? null : cod,
                        SKU = sku,
                        Nombre = nom,
                        PrecioCosto = costo,
                        PorcentajeGanancia = margen > 0 ? margen : 60m,
                        PrecioVenta = venta > 0 ? venta : (costo * 1.6m),
                        StockActual = stock,
                        StockMinimo = min,
                        Ubicacion = ubi
                    });
                    creados++;
                }
            }
            catch
            {
                errores++;
            }
        }

        await _context.SaveChangesAsync(ct);
        return (creados, actualizados, errores);
    }

    public async Task<bool> EliminarArticuloAsync(Guid articuloId, CancellationToken ct = default)
    {
        var art = await _context.Articulos.FindAsync(new object[] { articuloId }, ct);
        if (art == null) return false;

        art.Activo = false;
        await _context.SaveChangesAsync(ct);
        return true;
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
        ComponentesDelCombo = new System.Collections.ObjectModel.ObservableCollection<ComboComponenteDto>(
            a.ItemsDelCombo.Select(c => new ComboComponenteDto
            {
                ComponenteArticuloId = c.ComponenteArticuloId,
                NombreArticulo = c.ComponenteArticulo?.Nombre ?? "",
                SKU = c.ComponenteArticulo?.SKU ?? "",
                Cantidad = c.Cantidad,
                StockActualDisponible = c.ComponenteArticulo?.StockActual ?? 0
            }))
    };
}
