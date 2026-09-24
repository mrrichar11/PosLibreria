using System.Text.RegularExpressions;
using ExcelDataReader;
using Microsoft.EntityFrameworkCore;
using PuntoDeVentaLibreria.Application.Common;
using PuntoDeVentaLibreria.Application.DTOs.Inventario;
using PuntoDeVentaLibreria.Application.Services;
using PuntoDeVentaLibreria.Domain.Entities.Catalogo;
using PuntoDeVentaLibreria.Domain.Entities.Inventario;
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
                .Include(a => a.Proveedor)
                .Include(a => a.ArticuloBase)
                .Include(a => a.Variantes)
                .Where(a => a.Activo)
                .OrderBy(a => a.Nombre)
                .Take(50)
                .ToListAsync(ct);

            return articulos.Select(MapToDto).ToList();
        }

        var limpio = criterio.Trim().ToUpper();

        // 1. Coincidencia exacta de Código de Barras (artículo o variante), SKU, Código de Proveedor o Código Secundario
        var exacto = await _context.Articulos
            .AsNoTracking()
            .Include(a => a.Categoria)
            .Include(a => a.Marca)
            .Include(a => a.Proveedor)
            .Include(a => a.ArticuloBase)
            .Include(a => a.Variantes)
            .FirstOrDefaultAsync(a => a.Activo && (
                a.CodigoBarras == limpio ||
                a.SKU == limpio ||
                a.CodigoProveedor == limpio ||
                (a.CodigosBarrasSecundarios != null && a.CodigosBarrasSecundarios.Contains(limpio)) ||
                a.Variantes.Any(v => v.Activo && v.CodigoBarras == limpio)), ct);

        if (exacto != null)
        {
            var dto = MapToDto(exacto);
            var vMatch = dto.Variantes.FirstOrDefault(v => v.CodigoBarras != null && v.CodigoBarras.Equals(limpio, StringComparison.OrdinalIgnoreCase));
            if (vMatch != null) dto.VarianteEscaneada = vMatch;
            return new List<ArticuloDto> { dto };
        }

        // 2. Coincidencia por múltiples términos (incluyendo nombres y códigos de variantes)
        var tokens = limpio.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var query = _context.Articulos
            .AsNoTracking()
            .Include(a => a.Categoria)
            .Include(a => a.Marca)
            .Include(a => a.Proveedor)
            .Include(a => a.ArticuloBase)
            .Include(a => a.Variantes)
            .Where(a => a.Activo);

        foreach (var token in tokens)
        {
            query = query.Where(a =>
                a.Nombre.ToUpper().Contains(token) ||
                a.SKU.ToUpper().Contains(token) ||
                (a.CodigoBarras != null && a.CodigoBarras.Contains(token)) ||
                (a.CodigoProveedor != null && a.CodigoProveedor.Contains(token)) ||
                (a.CodigosBarrasSecundarios != null && a.CodigosBarrasSecundarios.Contains(token)) ||
                (a.Marca != null && a.Marca.Nombre.ToUpper().Contains(token)) ||
                (a.Categoria != null && a.Categoria.Nombre.ToUpper().Contains(token)) ||
                (a.Proveedor != null && a.Proveedor.Nombre.ToUpper().Contains(token)) ||
                (a.Rubro != null && a.Rubro.ToUpper().Contains(token)) ||
                a.Variantes.Any(v => v.Activo && (v.Nombre.ToUpper().Contains(token) || (v.CodigoBarras != null && v.CodigoBarras.Contains(token)))));
        }

        var lista = await query.Take(50).ToListAsync(ct);
        return lista.Select(MapToDto).ToList();
    }

    public async Task<ArticulosPaginadosResultadoDto> ObtenerArticulosPaginadosAsync(ConsultaInventarioPaginadaDto consulta, CancellationToken ct = default)
    {
        var queryBase = _context.Articulos.AsNoTracking().Where(a => a.Activo);

        // Métricas de stock globales
        var totalGlobal = await queryBase.CountAsync(ct);
        var articulosValores = await queryBase
            .Select(a => new { a.PrecioVenta, a.StockActual })
            .ToListAsync(ct);
        var valorGlobal = articulosValores.Sum(a => a.PrecioVenta * a.StockActual);

        var query = queryBase
            .Include(a => a.Categoria)
            .Include(a => a.Marca)
            .Include(a => a.Proveedor)
            .Include(a => a.ArticuloBase)
            .Include(a => a.Variantes)
            .Include(a => a.ItemsDelCombo)
                .ThenInclude(c => c.ComponenteArticulo)
            .AsQueryable();

        // Filtro por Rubro si aplica
        if (!string.IsNullOrWhiteSpace(consulta.Rubro) && !consulta.Rubro.Equals("Todos", StringComparison.OrdinalIgnoreCase))
        {
            var rubroNorm = consulta.Rubro.Trim().ToUpper();
            query = query.Where(a => a.Rubro != null && a.Rubro.ToUpper() == rubroNorm);
        }

        // Filtro por Criterio de búsqueda si aplica
        if (!string.IsNullOrWhiteSpace(consulta.CriterioBusqueda))
        {
            var limpio = consulta.CriterioBusqueda.Trim().ToUpper();
            var tokens = limpio.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                query = query.Where(a =>
                    a.Nombre.ToUpper().Contains(token) ||
                    a.SKU.ToUpper().Contains(token) ||
                    (a.CodigoBarras != null && a.CodigoBarras.Contains(token)) ||
                    (a.CodigoProveedor != null && a.CodigoProveedor.Contains(token)) ||
                    (a.CodigosBarrasSecundarios != null && a.CodigosBarrasSecundarios.Contains(token)) ||
                    (a.Marca != null && a.Marca.Nombre.ToUpper().Contains(token)) ||
                    (a.Categoria != null && a.Categoria.Nombre.ToUpper().Contains(token)) ||
                    (a.Proveedor != null && a.Proveedor.Nombre.ToUpper().Contains(token)) ||
                    (a.Rubro != null && a.Rubro.ToUpper().Contains(token)) ||
                    a.Variantes.Any(v => v.Activo && (v.Nombre.ToUpper().Contains(token) || (v.CodigoBarras != null && v.CodigoBarras.Contains(token)))));
            }
        }

        var totalFiltrados = await query.CountAsync(ct);
        var pagina = consulta.Pagina < 1 ? 1 : consulta.Pagina;
        var pageSize = consulta.CantidadPorPagina;

        List<Articulo> lista;
        int totalPaginas;

        if (pageSize <= 0) // "Ver Todos"
        {
            lista = await query.OrderBy(a => a.Nombre).ToListAsync(ct);
            totalPaginas = 1;
            pagina = 1;
        }
        else
        {
            totalPaginas = (int)Math.Ceiling((double)totalFiltrados / pageSize);
            if (totalPaginas == 0) totalPaginas = 1;
            if (pagina > totalPaginas) pagina = totalPaginas;

            lista = await query
                .OrderBy(a => a.Nombre)
                .Skip((pagina - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);
        }

        return new ArticulosPaginadosResultadoDto
        {
            Items = lista.Select(MapToDto).ToList(),
            TotalRegistros = totalFiltrados,
            PaginaActual = pagina,
            CantidadPorPagina = pageSize,
            TotalPaginas = totalPaginas,
            TotalArticulosGlobal = totalGlobal,
            ValorTotalStockGlobal = valorGlobal
        };
    }

    public async Task<ArticuloDto?> BuscarPorCodigoBarrasAsync(string codigoBarras, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(codigoBarras)) return null;
        var limpio = codigoBarras.Trim();

        var art = await _context.Articulos
            .AsNoTracking()
            .Include(a => a.Categoria)
            .Include(a => a.Marca)
            .Include(a => a.Proveedor)
            .Include(a => a.ArticuloBase)
            .Include(a => a.Variantes)
            .Include(a => a.ItemsDelCombo)
                .ThenInclude(c => c.ComponenteArticulo)
            .FirstOrDefaultAsync(a => a.Activo && (
                a.CodigoBarras == limpio ||
                a.SKU == limpio ||
                a.CodigoProveedor == limpio ||
                (a.CodigosBarrasSecundarios != null && a.CodigosBarrasSecundarios.Contains(limpio)) ||
                a.Variantes.Any(v => v.Activo && v.CodigoBarras == limpio)), ct);

        if (art == null) return null;

        var dto = MapToDto(art);
        var vMatch = dto.Variantes.FirstOrDefault(v => v.CodigoBarras != null && v.CodigoBarras.Equals(limpio, StringComparison.OrdinalIgnoreCase));
        if (vMatch != null)
        {
            dto.VarianteEscaneada = vMatch;
        }

        return dto;
    }

    public async Task<IReadOnlyList<ArticuloDto>> ObtenerBotonesRapidosAsync(CancellationToken ct = default)
    {
        var articulos = await _context.Articulos
            .AsNoTracking()
            .Include(a => a.Categoria)
            .Include(a => a.Marca)
            .Include(a => a.Proveedor)
            .Include(a => a.ArticuloBase)
            .Include(a => a.Variantes)
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
        entidad.CodigosBarrasSecundarios = string.IsNullOrWhiteSpace(dto.CodigosBarrasSecundarios) ? null : dto.CodigosBarrasSecundarios.Trim();
        entidad.CodigoProveedor = string.IsNullOrWhiteSpace(dto.CodigoProveedor) ? null : dto.CodigoProveedor.Trim();
        entidad.CategoriaId = dto.CategoriaId;
        entidad.MarcaId = dto.MarcaId;
        entidad.ProveedorId = dto.ProveedorId;
        entidad.Tipo = dto.Tipo;
        entidad.PrecioCosto = dto.PrecioCosto;
        entidad.IvaPorcentaje = dto.IvaPorcentaje;
        entidad.PorcentajeGanancia = dto.PorcentajeGanancia;
        entidad.PrecioVenta = dto.PrecioVenta;
        entidad.StockActual = dto.StockActual;
        entidad.StockMinimo = dto.StockMinimo;
        entidad.UnidadMedida = dto.UnidadMedida;
        entidad.Ubicacion = dto.Ubicacion;
        entidad.EsBotonRapido = dto.EsBotonRapido;
        entidad.ColorBoton = dto.ColorBoton;
        entidad.Rubro = string.IsNullOrWhiteSpace(dto.Rubro) ? "Librería" : dto.Rubro.Trim();
        entidad.EsPack = dto.EsPack;
        entidad.ArticuloBaseId = dto.ArticuloBaseId;
        entidad.CantidadPorPack = dto.CantidadPorPack > 0 ? dto.CantidadPorPack : 1;
        entidad.UltimaAuditoriaStock = dto.UltimaAuditoriaStock;

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

        // Gestión y persistencia de Variantes (Colores / Modelos con stock propio)
        if (dto.Id != Guid.Empty)
        {
            await _context.Entry(entidad).Collection(a => a.Variantes).LoadAsync(ct);
        }

        if (dto.Variantes != null && dto.Variantes.Count > 0)
        {
            var dtoVarIds = dto.Variantes.Where(v => v.Id != Guid.Empty).Select(v => v.Id).ToHashSet();

            foreach (var existingVar in entidad.Variantes.ToList())
            {
                if (!dtoVarIds.Contains(existingVar.Id))
                {
                    existingVar.Activo = false;
                }
            }

            foreach (var vDto in dto.Variantes)
            {
                var existing = entidad.Variantes.FirstOrDefault(v => v.Id == vDto.Id && vDto.Id != Guid.Empty);
                if (existing != null)
                {
                    existing.Nombre = vDto.Nombre.Trim();
                    existing.CodigoBarras = string.IsNullOrWhiteSpace(vDto.CodigoBarras) ? null : vDto.CodigoBarras.Trim();
                    existing.CodigoProveedor = string.IsNullOrWhiteSpace(vDto.CodigoProveedor) ? null : vDto.CodigoProveedor.Trim();
                    existing.StockActual = vDto.StockActual;
                    existing.StockMinimo = vDto.StockMinimo;
                    existing.Activo = true;
                }
                else
                {
                    entidad.Variantes.Add(new ArticuloVariante
                    {
                        Id = Guid.NewGuid(),
                        ArticuloId = entidad.Id,
                        Nombre = vDto.Nombre.Trim(),
                        CodigoBarras = string.IsNullOrWhiteSpace(vDto.CodigoBarras) ? null : vDto.CodigoBarras.Trim(),
                        CodigoProveedor = string.IsNullOrWhiteSpace(vDto.CodigoProveedor) ? null : vDto.CodigoProveedor.Trim(),
                        StockActual = vDto.StockActual,
                        StockMinimo = vDto.StockMinimo,
                        Activo = true
                    });
                }
            }

            // Sincronizar el stock acumulado del artículo principal
            entidad.StockActual = entidad.Variantes.Where(v => v.Activo).Sum(v => v.StockActual);

            // Mantener códigos secundarios en sync para interoperabilidad
            var codigosVariantes = entidad.Variantes
                .Where(v => v.Activo && !string.IsNullOrWhiteSpace(v.CodigoBarras))
                .Select(v => v.CodigoBarras!)
                .Distinct();
            entidad.CodigosBarrasSecundarios = string.Join(", ", codigosVariantes);
        }
        else
        {
            // Sin variantes: desactivar si tenía anteriores y respetar stock directo
            foreach (var existingVar in entidad.Variantes)
            {
                existingVar.Activo = false;
            }
            entidad.StockActual = dto.StockActual;
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
            .Include(a => a.Proveedor)
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

    public async Task<string> GenerarSkuSugeridoAsync(CancellationToken ct = default)
    {
        var total = await _context.Articulos.CountAsync(ct);
        int secuencia = total + 1;
        while (true)
        {
            var sku = $"ART-{secuencia:D5}";
            var existe = await _context.Articulos.AnyAsync(a => a.SKU == sku, ct);
            if (!existe)
            {
                return sku;
            }
            secuencia++;
        }
    }

    public async Task<string> GenerarCodigoBarrasSugeridoAsync(CancellationToken ct = default)
    {
        var total = await _context.Articulos.CountAsync(ct);
        long baseNum = 200000000000L + (total + 1);
        while (true)
        {
            var baseStr = baseNum.ToString("D12");
            var checkDigit = CalcularDigitoVerificadorEan13(baseStr);
            var codigoCompleto = $"{baseStr}{checkDigit}";

            var existe = await _context.Articulos.AnyAsync(a => a.CodigoBarras == codigoCompleto, ct);
            if (!existe)
            {
                return codigoCompleto;
            }
            baseNum++;
        }
    }

    // =========================================================================
    // IMPORTACIÓN Y MIGRACIÓN UNIVERSAL DE CATÁLOGO (EXCEL / CSV)
    // =========================================================================

    private static DateTime? ParseFechaExcel(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;

        var formatos = new[]
        {
            "dd-MMM-yy", "dd-MMM-yyyy", "d-MMM-yy", "d-MMM-yyyy",
            "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy",
            "yyyy-MM-dd", "dd/MM/yy", "dd-MM-yy", "yyyy/MM/dd",
            "yyyy-MM-dd HH:mm:ss", "dd/MM/yyyy HH:mm:ss"
        };

        var culturaEs = new System.Globalization.CultureInfo("es-AR");
        if (DateTime.TryParseExact(valor.Trim(), formatos, culturaEs, System.Globalization.DateTimeStyles.None, out var dtEs))
        {
            return dtEs;
        }

        if (DateTime.TryParseExact(valor.Trim(), formatos, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dtInv))
        {
            return dtInv;
        }

        if (DateTime.TryParse(valor.Trim(), culturaEs, System.Globalization.DateTimeStyles.None, out var dtGeneral))
        {
            return dtGeneral;
        }

        if (double.TryParse(valor.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var serial) && serial > 30000 && serial < 80000)
        {
            try { return DateTime.FromOADate(serial); } catch { }
        }

        return null;
    }

    public async Task<AnalisisExcelResultadoDto> AnalizarExcelGenericoAsync(Stream archivoExcelStream, MapeoColumnasExcelDto? mapeoPersonalizado = null, CancellationToken ct = default)
    {
        var (headers, rows) = LeerExcelSimple(archivoExcelStream);
        var resultado = new AnalisisExcelResultadoDto
        {
            ColumnasDetectadas = headers,
            TotalFilas = rows.Count
        };

        var mapeo = mapeoPersonalizado ?? AutoDetectarMapeo(headers);
        resultado.MapeoSugerido = mapeo;

        var skusExistentes = (await _context.Articulos.AsNoTracking().Select(a => a.SKU).ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var barrasExistentes = (await _context.Articulos.AsNoTracking().Where(a => a.CodigoBarras != null).Select(a => a.CodigoBarras!).ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var items = new List<ItemPrevisualizacionAlmaLibreDto>();

        foreach (var row in rows)
        {
            var sku = ObtenerValorPorColumnaOMapeo(row, mapeo.ColumnaSku, "codigo", "sku", "cod", "codart");
            var codProv = ObtenerValorPorColumnaOMapeo(row, mapeo.ColumnaCodigoProveedor, "codprov", "codigoproveedor", "cod_prov");
            var codBarra = ObtenerValorPorColumnaOMapeo(row, mapeo.ColumnaCodigoBarras, "codbarra", "codigodebarra", "codigobarra", "barra", "barcode", "ean");
            var descrip = ObtenerValorPorColumnaOMapeo(row, mapeo.ColumnaDescripcion, "descrip", "descripcion", "nombre", "articulo", "detalle", "producto");
            var rubro = ObtenerValorPorColumnaOMapeo(row, mapeo.ColumnaCategoria, "rubro", "categoria", "familia", "depto");
            var marca = ObtenerValorPorColumnaOMapeo(row, mapeo.ColumnaMarca, "marca", "fabricante");

            if (string.IsNullOrWhiteSpace(descrip) && string.IsNullOrWhiteSpace(sku) && string.IsNullOrWhiteSpace(codBarra)) 
                continue;

            CalculoPreciosUtils.TryParseMonto(ObtenerValorPorColumnaOMapeo(row, mapeo.ColumnaPrecioCosto, "costo", "preciocosto", "compra", "pcompra"), out var costo);
            CalculoPreciosUtils.TryParseMonto(ObtenerValorColumna(row, "pciva", "iva"), out var pciva);
            if (pciva <= 0) pciva = mapeo.IvaDefecto > 0 ? mapeo.IvaDefecto : 21.0m;

            CalculoPreciosUtils.TryParseMonto(ObtenerValorColumna(row, "pcgan", "ganancia%", "utilidad", "margen"), out var pcgan);
            if (pcgan <= 0) pcgan = mapeo.PorcentajeGananciaDefecto > 0 ? mapeo.PorcentajeGananciaDefecto : 60.0m;

            CalculoPreciosUtils.TryParseMonto(ObtenerValorPorColumnaOMapeo(row, mapeo.ColumnaPrecioVenta, "precio", "precioventa", "pvp", "publico", "pventa"), out var precio);
            if (precio <= 0 && costo > 0)
            {
                precio = CalculoPreciosUtils.CalcularPrecioVenta(costo, pcgan, pciva);
            }

            // Precio Tarjeta & Recargo Financiero
            CalculoPreciosUtils.TryParseMonto(ObtenerValorPorColumnaOMapeo(row, mapeo.ColumnaPrecioTarjeta, "tarjeta", "preciotarjeta", "tarj", "pcredito"), out var pTarjeta);
            decimal? recargoPct = null;
            if (pTarjeta > 0 && precio > 0 && pTarjeta > precio)
            {
                recargoPct = Math.Round(((pTarjeta - precio) / precio) * 100m, 1);
            }

            // Stock Inicial
            CalculoPreciosUtils.TryParseMonto(ObtenerValorPorColumnaOMapeo(row, mapeo.ColumnaStock, "stock", "cantidad", "cant", "existencia", "stockactual", "unidades"), out var stockExcel);
            if (stockExcel <= 0 && mapeo.StockDefecto > 0)
            {
                stockExcel = mapeo.StockDefecto;
            }

            // Fechas de Alta y Último Precio
            var fchAltaStr = ObtenerValorPorColumnaOMapeo(row, mapeo.ColumnaFechaAlta, "fchalta", "fechaalta", "alta", "creado");
            var fchUltPreStr = ObtenerValorPorColumnaOMapeo(row, mapeo.ColumnaFechaUltimoPrecio, "fchultpre", "fechaultprecio", "ultprecio", "fchprecio", "actualizado");

            var fechaAlta = ParseFechaExcel(fchAltaStr);
            var fechaUltPrecio = ParseFechaExcel(fchUltPreStr);

            var existe = (!string.IsNullOrWhiteSpace(sku) && skusExistentes.Contains(sku)) ||
                         (!string.IsNullOrWhiteSpace(codBarra) && barrasExistentes.Contains(codBarra));

            items.Add(new ItemPrevisualizacionAlmaLibreDto
            {
                Seleccionado = true,
                SKU = !string.IsNullOrWhiteSpace(sku) ? sku : (!string.IsNullOrWhiteSpace(codBarra) ? codBarra : "SIN-SKU"),
                CodigoProveedor = string.IsNullOrWhiteSpace(codProv) ? null : codProv,
                CodigoBarras = string.IsNullOrWhiteSpace(codBarra) ? null : codBarra,
                Nombre = !string.IsNullOrWhiteSpace(descrip) ? descrip : "Sin Descripción",
                CategoriaRubro = string.IsNullOrWhiteSpace(rubro) ? null : rubro,
                PrecioCosto = costo,
                IvaPorcentaje = pciva,
                PorcentajeGanancia = pcgan,
                PrecioVenta = precio,
                PrecioTarjeta = pTarjeta > 0 ? pTarjeta : null,
                RecargoTarjetaPorcentaje = recargoPct,
                FechaAlta = fechaAlta,
                FechaUltimaActualizacionPrecio = fechaUltPrecio,
                StockImportar = stockExcel,
                EsDeMayoristaElOnce = false,
                CodigoOnceDetectado = null,
                EsYaImportado = existe
            });
        }

        resultado.Items = items;
        return resultado;
    }

    private static MapeoColumnasExcelDto AutoDetectarMapeo(List<string> headers)
    {
        var mapeo = new MapeoColumnasExcelDto();

        foreach (var h in headers)
        {
            var norm = NormalizarTextoColumna(h);

            if (mapeo.ColumnaCodigoBarras == null && (norm.Contains("codbarra") || norm.Contains("codigobarra") || norm.Contains("barcode") || norm.Contains("ean") || norm == "barra" || norm == "barras"))
                mapeo.ColumnaCodigoBarras = h;
            else if (mapeo.ColumnaSku == null && (norm.Contains("sku") || norm == "codigo" || norm == "cod" || norm == "codart" || norm == "articulo" || norm == "id"))
                mapeo.ColumnaSku = h;
            else if (mapeo.ColumnaDescripcion == null && (norm.Contains("descrip") || norm.Contains("nombre") || norm.Contains("detalle") || norm.Contains("producto") || norm == "articulo"))
                mapeo.ColumnaDescripcion = h;
            else if (mapeo.ColumnaPrecioVenta == null && (norm.Contains("precioventa") || norm == "precio" || norm == "pvp" || norm.Contains("publico") || norm == "pventa" || norm == "precio1" || norm == "lista"))
                mapeo.ColumnaPrecioVenta = h;
            else if (mapeo.ColumnaPrecioCosto == null && (norm.Contains("preciocosto") || norm == "costo" || norm.Contains("compra") || norm == "pcompra" || norm == "neto"))
                mapeo.ColumnaPrecioCosto = h;
            else if (mapeo.ColumnaPrecioTarjeta == null && (norm.Contains("tarjeta") || norm.Contains("tarj") || norm.Contains("credito") || norm == "pcredito"))
                mapeo.ColumnaPrecioTarjeta = h;
            else if (mapeo.ColumnaStock == null && (norm.Contains("stock") || norm.Contains("cantidad") || norm == "cant" || norm.Contains("existencia") || norm.Contains("unidades")))
                mapeo.ColumnaStock = h;
            else if (mapeo.ColumnaCategoria == null && (norm.Contains("rubro") || norm.Contains("categoria") || norm.Contains("familia") || norm.Contains("depto") || norm == "seccion"))
                mapeo.ColumnaCategoria = h;
            else if (mapeo.ColumnaMarca == null && (norm.Contains("marca") || norm.Contains("fabricante")))
                mapeo.ColumnaMarca = h;
            else if (mapeo.ColumnaCodigoProveedor == null && (norm.Contains("codprov") || norm.Contains("codigoproveedor") || norm.Contains("prov_cod")))
                mapeo.ColumnaCodigoProveedor = h;
            else if (mapeo.ColumnaFechaAlta == null && (norm.Contains("fchalta") || norm.Contains("fechaalta") || norm == "alta"))
                mapeo.ColumnaFechaAlta = h;
            else if (mapeo.ColumnaFechaUltimoPrecio == null && (norm.Contains("fchultpre") || norm.Contains("fechaultprecio") || norm.Contains("ultprecio") || norm.Contains("actualizado")))
                mapeo.ColumnaFechaUltimoPrecio = h;
        }

        return mapeo;
    }

    private static string? ObtenerValorPorColumnaOMapeo(Dictionary<string, string> row, string? nombreColumnaMapeada, params string[] aliasFallback)
    {
        if (!string.IsNullOrWhiteSpace(nombreColumnaMapeada) && row.TryGetValue(nombreColumnaMapeada, out var val))
        {
            return val?.Trim();
        }

        return ObtenerValorColumna(row, aliasFallback);
    }

    public async Task<byte[]> GenerarPlantillaExcelModeloAsync(CancellationToken ct = default)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("CodigoBarras;SKU;Nombre;Categoria;Marca;PrecioCosto;PorcentajeGanancia;PrecioVenta;StockActual;StockMinimo;CodigoProveedor");
        sb.AppendLine("7791234567890;ART-00001;Cuaderno Rivadavia 48H Rayado;Cuadernos;Rivadavia;1500;60;2400;25;5;RIV-48R");
        sb.AppendLine("7799876543210;ART-00002;Bolígrafo BIC Cristal Azul 1.0;Escritura;BIC;350;70;595;100;20;BIC-AZ-01");
        sb.AppendLine("7795551122334;ART-00003;Resaltador Pelikan Fluo Amarillo;Escritura;Pelikan;900;65;1485;40;10;PEL-RES-AM");
        sb.AppendLine("7793332221110;ART-00004;Goma de Borrar Dos Banderas;Escritura;Dos Banderas;200;80;360;50;15;GOM-DB-01");
        sb.AppendLine("7798887776665;ART-00005;Cartuchera Canopla 2 Cierres;Marroquineria;Mooving;4500;60;7200;10;2;MOOV-CAN-02");

        // UTF-8 con BOM para que Excel en Windows lo abra perfecto con tildes y caracteres especiales
        var bytesTexto = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        var resultado = new byte[bom.Length + bytesTexto.Length];
        Buffer.BlockCopy(bom, 0, resultado, 0, bom.Length);
        Buffer.BlockCopy(bytesTexto, 0, resultado, bom.Length, bytesTexto.Length);

        return await Task.FromResult(resultado);
    }

    public async Task<IReadOnlyList<ItemPrevisualizacionAlmaLibreDto>> PrevisualizarCatalogoAlmaLibreAsync(Stream archivoExcelStream, CancellationToken ct = default)
    {
        var analisis = await AnalizarExcelGenericoAsync(archivoExcelStream, null, ct);
        return analisis.Items;
    }


    public async Task<MigracionResultadoDto> ImportarCatalogoAlmaLibreAsync(Stream archivoExcelStream, Guid? proveedorId = null, CancellationToken ct = default)
    {
        var items = await PrevisualizarCatalogoAlmaLibreAsync(archivoExcelStream, ct);
        return await ImportarCatalogoSeleccionadoAsync(items, proveedorId, ct);
    }

    public async Task<MigracionResultadoDto> ImportarCatalogoSeleccionadoAsync(IReadOnlyList<ItemPrevisualizacionAlmaLibreDto> items, Guid? proveedorIdPorDefecto = null, CancellationToken ct = default)
    {
        var itemsSeleccionados = items.Where(i => i.Seleccionado).ToList();
        var resultado = new MigracionResultadoDto { TotalFilasProcesadas = itemsSeleccionados.Count };

        // Precargar categorías para mapear rápidamente
        var categorias = await _context.Categorias.ToListAsync(ct);
        var dictCategorias = categorias.ToDictionary(c => c.Nombre.Trim().ToUpperInvariant(), c => c.Id);

        // Precargar artículos existentes por SKU y Barras
        var articulosExistentes = await _context.Articulos.Include(a => a.MovimientosStock).ToListAsync(ct);
        var dictPorSku = articulosExistentes.Where(a => !string.IsNullOrEmpty(a.SKU))
            .ToDictionary(a => a.SKU.Trim().ToUpperInvariant(), a => a);
        var dictPorBarra = articulosExistentes.Where(a => !string.IsNullOrEmpty(a.CodigoBarras))
            .ToDictionary(a => a.CodigoBarras!.Trim().ToUpperInvariant(), a => a);

        int secuenciaSku = articulosExistentes.Count + 1;

        foreach (var item in itemsSeleccionados)
        {
            try
            {
                var sku = item.SKU?.Trim();
                var codProv = item.CodigoProveedor?.Trim();
                var codBarra = item.CodigoBarras?.Trim();
                var descrip = item.Nombre?.Trim();
                var rubro = item.CategoriaRubro?.Trim();

                if (string.IsNullOrWhiteSpace(descrip)) continue;

                if (string.IsNullOrWhiteSpace(sku) || sku == "SIN-SKU")
                {
                    sku = !string.IsNullOrWhiteSpace(codBarra) ? codBarra : $"ART-{secuenciaSku++:D5}";
                }

                // Resolver Categoría
                Guid? catId = null;
                if (!string.IsNullOrWhiteSpace(rubro))
                {
                    var rubroUpper = rubro.ToUpperInvariant();
                    if (dictCategorias.TryGetValue(rubroUpper, out var existingCatId))
                    {
                        catId = existingCatId;
                    }
                    else
                    {
                        var nuevaCat = new Categoria { Id = Guid.NewGuid(), Nombre = rubro };
                        _context.Categorias.Add(nuevaCat);
                        dictCategorias[rubroUpper] = nuevaCat.Id;
                        catId = nuevaCat.Id;
                        resultado.CategoriasCreadas++;
                    }
                }

                // Asignar Proveedor por defecto si el usuario lo seleccionó
                Guid? provId = proveedorIdPorDefecto;

                // Buscar si ya existe por SKU o Código de Barra
                Articulo? articulo = null;
                if (dictPorSku.TryGetValue(sku.ToUpperInvariant(), out var porSku))
                {
                    articulo = porSku;
                }
                else if (!string.IsNullOrWhiteSpace(codBarra) && dictPorBarra.TryGetValue(codBarra.ToUpperInvariant(), out var porBarra))
                {
                    articulo = porBarra;
                }

                if (articulo != null)
                {
                    // Actualizar existente
                    articulo.Nombre = descrip;
                    if (!string.IsNullOrWhiteSpace(codBarra)) articulo.CodigoBarras = codBarra;
                    if (!string.IsNullOrWhiteSpace(codProv)) articulo.CodigoProveedor = codProv;
                    if (catId.HasValue) articulo.CategoriaId = catId;
                    if (provId.HasValue) articulo.ProveedorId = provId;

                    articulo.PrecioCosto = item.PrecioCosto;
                    articulo.IvaPorcentaje = item.IvaPorcentaje > 0 ? item.IvaPorcentaje : 21.0m;
                    articulo.PorcentajeGanancia = item.PorcentajeGanancia > 0 ? item.PorcentajeGanancia : 60.0m;
                    articulo.PrecioVenta = item.PrecioVenta;
                    articulo.Activo = true;

                    // Si el usuario especificó stock inicial a cargar
                    if (item.StockImportar > 0)
                    {
                        var stockPrevio = articulo.StockActual;
                        articulo.StockActual = item.StockImportar;
                        _context.Set<MovimientoStock>().Add(new MovimientoStock
                        {
                            Id = Guid.NewGuid(),
                            ArticuloId = articulo.Id,
                            Cantidad = item.StockImportar,
                            StockPrevio = stockPrevio,
                            StockPosterior = item.StockImportar,
                            Tipo = TipoMovimientoStock.AjusteManualPositivo,
                            Motivo = "Carga / ajuste de stock inicial desde catálogo migrado",
                            FechaCreacion = DateTime.UtcNow
                        });
                        resultado.TotalStockIngresado += item.StockImportar;
                    }

                    resultado.ArticulosActualizados++;
                }
                else
                {
                    // Crear nuevo
                    var nuevo = new Articulo
                    {
                        Id = Guid.NewGuid(),
                        SKU = sku,
                        CodigoBarras = string.IsNullOrWhiteSpace(codBarra) ? null : codBarra,
                        CodigoProveedor = string.IsNullOrWhiteSpace(codProv) ? null : codProv,
                        Nombre = descrip,
                        CategoriaId = catId,
                        ProveedorId = provId,
                        PrecioCosto = item.PrecioCosto,
                        IvaPorcentaje = item.IvaPorcentaje > 0 ? item.IvaPorcentaje : 21.0m,
                        PorcentajeGanancia = item.PorcentajeGanancia > 0 ? item.PorcentajeGanancia : 60.0m,
                        PrecioVenta = item.PrecioVenta,
                        StockActual = item.StockImportar,
                        StockMinimo = 5,
                        Activo = true
                    };

                    if (item.StockImportar > 0)
                    {
                        nuevo.MovimientosStock.Add(new MovimientoStock
                        {
                            Id = Guid.NewGuid(),
                            ArticuloId = nuevo.Id,
                            Cantidad = item.StockImportar,
                            StockPrevio = 0,
                            StockPosterior = item.StockImportar,
                            Tipo = TipoMovimientoStock.AjusteManualPositivo,
                            Motivo = "Carga de stock inicial desde catálogo migrado",
                            FechaCreacion = DateTime.UtcNow
                        });
                        resultado.TotalStockIngresado += item.StockImportar;
                    }

                    _context.Articulos.Add(nuevo);
                    dictPorSku[sku.ToUpperInvariant()] = nuevo;
                    if (!string.IsNullOrWhiteSpace(codBarra))
                    {
                        dictPorBarra[codBarra.ToUpperInvariant()] = nuevo;
                    }
                    resultado.ArticulosCreados++;
                }

                item.YaExisteEnSistema = true;
            }
            catch (Exception ex)
            {
                resultado.Errores++;
                if (resultado.MensajesErrores.Count < 20)
                {
                    resultado.MensajesErrores.Add($"Error en '{item.Nombre}': {ex.Message}");
                }
            }
        }

        await _context.SaveChangesAsync(ct);
        return resultado;
    }

    // =========================================================================
    // ACTUALIZACIÓN MASIVA DE PRECIOS POR LISTA DE MAYORISTA (ej. El Once)
    // =========================================================================

    public static int? DetectarFactorPack(string? textoProveedor, string? nombreArticulo, decimal costoAnterior, decimal costoProveedor)
    {
        if (costoProveedor <= 0) return null;

        var textoCompleto = $"{textoProveedor} {nombreArticulo}".ToLowerInvariant();
        var candidatos = new HashSet<int>();

        // 1. Patrones explícitos en texto: 'x15', 'x 50', 'caja x 12', 'pack x 10', 'potes x 60', 'display x 24', 'x15 potes'
        var regexes = new[]
        {
            @"\b(?:pack|caja|blister|bulto|potes?|display|paq(?:uete)?|bolsa|tubo)\s*(?:x\s*)?(\d{1,4})\b",
            @"\b[xX]\s*(\d{1,4})\b",
            @"\b(\d{1,4})\s*(?:unid(?:ades)?|u\b|potes?|sobres?|piezas?)\b"
        };

        foreach (var r in regexes)
        {
            var matches = Regex.Matches(textoCompleto, r);
            foreach (Match m in matches)
            {
                if (m.Groups.Count > 1 && int.TryParse(m.Groups[1].Value, out int factor) && factor > 1 && factor <= 1000)
                {
                    candidatos.Add(factor);
                }
            }
        }

        // Si el costo del proveedor es significativamente mayor que el costo anterior (> 150% del anterior)
        // y no encontramos candidatos en texto, evaluamos factores comunes de papelería / librería
        if (costoAnterior > 0 && costoProveedor > costoAnterior * 2.0m)
        {
            int[] factoresComunes = { 5, 6, 10, 12, 15, 20, 24, 25, 30, 36, 48, 50, 60, 72, 100, 120, 144, 200, 250, 500 };
            foreach (var f in factoresComunes)
            {
                candidatos.Add(f);
            }
        }

        if (candidatos.Count == 0) return null;

        // Si tenemos costo anterior > 0, evaluamos cuál candidato da el costo unitario más razonable
        if (costoAnterior > 0)
        {
            int? mejorCandidato = null;
            decimal menorDiferencia = decimal.MaxValue;

            foreach (var f in candidatos)
            {
                var unitario = costoProveedor / f;
                var ratio = unitario / costoAnterior;

                // Ratio esperado para un aumento/ajuste razonable de inflación: entre 0.45 y 1.95
                // (es decir, una variación entre -55% y +95% respecto al costo anterior)
                if (ratio >= 0.45m && ratio <= 1.95m)
                {
                    var dif = Math.Abs(unitario - costoAnterior);
                    if (dif < menorDiferencia)
                    {
                        menorDiferencia = dif;
                        mejorCandidato = f;
                    }
                }
            }

            if (mejorCandidato.HasValue) return mejorCandidato;
        }

        // Si no hay costo anterior (ej. costo 0), pero encontramos un candidato explícito en el texto del proveedor
        var matchPrimerFactor = Regex.Match(textoCompleto, @"\b(?:pack|caja|blister|bulto|potes?|display)?\s*[xX]\s*(\d{1,4})\b");
        if (matchPrimerFactor.Success && int.TryParse(matchPrimerFactor.Groups[1].Value, out int factorTexto) && factorTexto > 1)
        {
            return factorTexto;
        }

        return null;
    }

    private static ArticuloAumentoPrecioItemDto ConstruirItemDto(Articulo art, decimal costoNuevo, string? descProveedor)
    {
        var factorSugerido = DetectarFactorPack(descProveedor, art.Nombre, art.PrecioCosto, costoNuevo);

        var item = new ArticuloAumentoPrecioItemDto
        {
            ArticuloId = art.Id,
            SKU = art.SKU,
            Nombre = art.Nombre,
            CodigoProveedor = art.CodigoProveedor,
            CodigoBarras = art.CodigoBarras,
            DescripcionProveedor = descProveedor,
            CostoAnterior = art.PrecioCosto,
            CostoOriginalProveedor = costoNuevo,
            FactorConversion = 1m,
            FactorSugerido = factorSugerido,
            PorcentajeGanancia = art.PorcentajeGanancia,
            IvaPorcentaje = art.IvaPorcentaje,
            VentaAnterior = art.PrecioVenta
        };

        item.Recalcular();

        // Si la variación es extrema (> 80% o < -50%), no se tilda para aplicar por defecto
        // para proteger al comercio de aumentos exorbitantes accidentales
        if (item.EsAlertaVariacionExtrema)
        {
            item.Aplicar = false;
        }
        else
        {
            item.Aplicar = Math.Abs(item.CostoNuevo - item.CostoAnterior) > 0.01m;
        }

        return item;
    }

    public async Task<ResumenPrevisualizacionAumentoDto> PrevisualizarActualizacionPreciosProveedorAsync(Stream archivoExcelStream, Guid? proveedorId = null, CancellationToken ct = default)
    {
        var (_, rows) = LeerExcelSimple(archivoExcelStream);
        var resumen = new ResumenPrevisualizacionAumentoDto();

        var query = _context.Articulos
            .AsNoTracking()
            .Include(a => a.Proveedor)
            .Where(a => a.Activo);

        if (proveedorId.HasValue && proveedorId.Value != Guid.Empty)
        {
            query = query.Where(a => a.ProveedorId == proveedorId.Value);
        }

        var articulos = await query.ToListAsync(ct);
        resumen.TotalArticulosCatalogo = articulos.Count;

        // Indexar catálogo de forma segura contra duplicados usando GroupBy
        var dictPorCodProv = articulos
            .Where(a => !string.IsNullOrWhiteSpace(a.CodigoProveedor))
            .GroupBy(a => a.CodigoProveedor!.Trim().ToUpperInvariant())
            .ToDictionary(g => g.Key, g => g.First());

        var dictPorBarraPrincipal = articulos
            .Where(a => !string.IsNullOrWhiteSpace(a.CodigoBarras))
            .GroupBy(a => a.CodigoBarras!.Trim().ToUpperInvariant())
            .ToDictionary(g => g.Key, g => g.First());

        var articulosConSecundarios = articulos
            .Where(a => !string.IsNullOrWhiteSpace(a.CodigosBarrasSecundarios))
            .ToList();

        // Mapa para evitar duplicar artículos de la tienda en los resultados
        // Clave: ArticuloId -> ItemDto
        var itemsPorArticulo = new Dictionary<Guid, ArticuloAumentoPrecioItemDto>();
        int noEncontrados = 0;

        foreach (var row in rows)
        {
            var codProvExcel = ObtenerValorColumna(row, "codigo", "codprov", "codigoproducto", "codigoproveedor")?.Trim();
            var codBarraExcel = ObtenerValorColumna(row, "codigodebarra", "codigobarra", "codbarra", "barra", "barcode")?.Trim();
            var descExcel = ObtenerValorColumna(row, "descripcion", "detalle", "nombre", "articulo", "producto", "denominacion")?.Trim();

            // Costo S/IVA y C/IVA del proveedor
            CalculoPreciosUtils.TryParseMonto(ObtenerValorColumna(row, "s/iva", "siva", "costo", "siniva", "costosiva", "preciocosto"), out var costoSiva);
            CalculoPreciosUtils.TryParseMonto(ObtenerValorColumna(row, "c/iva", "civa", "coniva", "costociva"), out var costoCiva);

            decimal costoNuevo = costoSiva > 0 ? costoSiva : (costoCiva > 0 ? Math.Round(costoCiva / 1.21m, 2) : 0m);
            if (costoNuevo <= 0) continue;

            // Coincidencia jerárquica:
            // 1. Por Código Proveedor (el más exacto)
            // 2. Por Código de Barra principal
            // 3. Por Códigos Secundarios
            Articulo? art = null;
            bool matchPorCodigoProveedor = false;

            if (!string.IsNullOrWhiteSpace(codProvExcel) && dictPorCodProv.TryGetValue(codProvExcel.ToUpperInvariant(), out var encontradoProv))
            {
                art = encontradoProv;
                matchPorCodigoProveedor = true;
            }
            else if (!string.IsNullOrWhiteSpace(codBarraExcel) && dictPorBarraPrincipal.TryGetValue(codBarraExcel.ToUpperInvariant(), out var encontradoBarra))
            {
                art = encontradoBarra;
            }
            else if (!string.IsNullOrWhiteSpace(codBarraExcel))
            {
                art = articulosConSecundarios.FirstOrDefault(a => a.CodigosBarrasSecundarios!.Contains(codBarraExcel));
            }

            if (art == null)
            {
                noEncontrados++;
                continue;
            }

            // Si ya procesamos este artículo de la tienda:
            if (itemsPorArticulo.TryGetValue(art.Id, out _))
            {
                // Si el nuevo match es por código de proveedor y el anterior fue por código de barras, reemplazamos por el más específico
                if (matchPorCodigoProveedor)
                {
                    itemsPorArticulo[art.Id] = ConstruirItemDto(art, costoNuevo, descExcel);
                }
                // Si ya fue emparejado, no duplicamos la fila
                continue;
            }

            itemsPorArticulo[art.Id] = ConstruirItemDto(art, costoNuevo, descExcel);
        }

        resumen.CoincidenciasEncontradas = itemsPorArticulo.Count;
        resumen.CoincidenciasConCambioDePrecio = itemsPorArticulo.Values.Count(i => Math.Abs(i.CostoNuevo - i.CostoAnterior) > 0.01m);
        resumen.CoincidenciasConAlerta = itemsPorArticulo.Values.Count(i => i.EsAlertaVariacionExtrema);
        resumen.NoEncontradosEnCatalogo = noEncontrados;
        resumen.ItemsParaActualizar = itemsPorArticulo.Values
            .OrderByDescending(i => i.EsAlertaVariacionExtrema)
            .ThenByDescending(i => Math.Abs(i.VariacionPorcentaje))
            .ToList();

        return resumen;
    }

    public async Task<ActualizacionPreciosResultadoDto> AplicarActualizacionPreciosAsync(
        IEnumerable<ArticuloAumentoPrecioItemDto> items, 
        Guid? asignarProveedorId = null, 
        CancellationToken ct = default)
    {
        var itemsSeleccionados = items.Where(i => i.Aplicar).ToList();
        if (!itemsSeleccionados.Any())
        {
            if (items.Any())
            {
                // Si se pasaron items explícitamente pero ninguno tenía el flag Aplicar marcado (ej. llamadas de servicio o tests directos)
                itemsSeleccionados = items.ToList();
            }
            else
            {
                return new ActualizacionPreciosResultadoDto
                {
                    TotalActualizados = 0,
                    Mensaje = "No se seleccionó ningún artículo para actualizar."
                };
            }
        }

        var ids = itemsSeleccionados.Select(i => i.ArticuloId).Distinct().ToList();
        var articulos = await _context.Articulos.Where(a => ids.Contains(a.Id)).ToListAsync(ct);
        
        var dictItems = itemsSeleccionados
            .GroupBy(i => i.ArticuloId)
            .ToDictionary(g => g.Key, g => g.First());

        PuntoDeVentaLibreria.Domain.Entities.Proveedores.Proveedor? proveedorAsignar = null;
        if (asignarProveedorId.HasValue && asignarProveedorId.Value != Guid.Empty)
        {
            proveedorAsignar = await _context.Proveedores.FindAsync(new object[] { asignarProveedorId.Value }, ct);
        }

        int actualizados = 0;
        int proveedoresAsignados = 0;

        foreach (var a in articulos)
        {
            if (dictItems.TryGetValue(a.Id, out var item))
            {
                a.PrecioCosto = item.CostoNuevo;
                a.PrecioVenta = item.VentaNueva;

                if (proveedorAsignar != null && a.ProveedorId != proveedorAsignar.Id)
                {
                    a.ProveedorId = proveedorAsignar.Id;
                    proveedoresAsignados++;
                }

                actualizados++;
            }
        }

        await _context.SaveChangesAsync(ct);

        var mensaje = $"Se actualizaron los precios de {actualizados} artículos exitosamente.";
        if (proveedoresAsignados > 0 && proveedorAsignar != null)
        {
            mensaje += $" Se vinculó el proveedor '{proveedorAsignar.Nombre}' a {proveedoresAsignados} artículos.";
        }

        return new ActualizacionPreciosResultadoDto
        {
            TotalActualizados = actualizados,
            Mensaje = mensaje
        };
    }

    public async Task<ArticuloDto> AjustarStockRapidoAsync(Guid articuloId, decimal nuevoStock, string motivo = "Auditoría de Stock", string usuarioNombre = "Administrador", Guid? articuloVarianteId = null, CancellationToken ct = default)
    {
        var art = await _context.Articulos
            .Include(a => a.Categoria)
            .Include(a => a.Marca)
            .Include(a => a.Proveedor)
            .Include(a => a.ArticuloBase)
            .Include(a => a.Variantes)
            .FirstOrDefaultAsync(a => a.Id == articuloId, ct)
            ?? throw new InvalidOperationException("Artículo no encontrado.");

        if (articuloVarianteId.HasValue)
        {
            var variante = art.Variantes.FirstOrDefault(v => v.Id == articuloVarianteId.Value)
                ?? throw new InvalidOperationException("Variante de artículo no encontrada.");

            decimal stockPrevioVar = variante.StockActual;
            decimal deltaVar = nuevoStock - stockPrevioVar;

            variante.StockActual = nuevoStock;
            variante.UltimaAuditoriaStock = DateTime.UtcNow;

            art.UltimaAuditoriaStock = DateTime.UtcNow;
            art.StockActual = art.Variantes.Where(v => v.Activo).Sum(v => v.StockActual);

            _context.MovimientosStock.Add(new MovimientoStock
            {
                ArticuloId = art.Id,
                ArticuloVarianteId = variante.Id,
                Tipo = deltaVar >= 0 ? TipoMovimientoStock.AjusteManualPositivo : TipoMovimientoStock.AjusteManualNegativo,
                Cantidad = deltaVar,
                StockPrevio = stockPrevioVar,
                StockPosterior = nuevoStock,
                Motivo = string.IsNullOrWhiteSpace(motivo) ? $"Auditoría de Stock - Color: {variante.Nombre}" : $"{motivo} ({variante.Nombre})",
                UsuarioNombre = string.IsNullOrWhiteSpace(usuarioNombre) ? "Administrador" : usuarioNombre
            });
        }
        else
        {
            decimal stockPrevio = art.StockActual;
            decimal delta = nuevoStock - stockPrevio;

            art.StockActual = nuevoStock;
            art.UltimaAuditoriaStock = DateTime.UtcNow;

            _context.MovimientosStock.Add(new MovimientoStock
            {
                ArticuloId = art.Id,
                Tipo = delta >= 0 ? TipoMovimientoStock.AjusteManualPositivo : TipoMovimientoStock.AjusteManualNegativo,
                Cantidad = delta,
                StockPrevio = stockPrevio,
                StockPosterior = nuevoStock,
                Motivo = string.IsNullOrWhiteSpace(motivo) ? "Auditoría de Stock (Conteo Rápido)" : motivo,
                UsuarioNombre = string.IsNullOrWhiteSpace(usuarioNombre) ? "Administrador" : usuarioNombre
            });
        }

        await _context.SaveChangesAsync(ct);
        return MapToDto(art);
    }

    public async Task<AuditoriaStockProgresoDto> ObtenerProgresoAuditoriaAsync(CancellationToken ct = default)
    {
        var total = await _context.Articulos.CountAsync(a => a.Activo, ct);
        var auditados = await _context.Articulos.CountAsync(a => a.Activo && a.UltimaAuditoriaStock != null, ct);

        return new AuditoriaStockProgresoDto
        {
            TotalArticulos = total,
            ArticulosAuditados = auditados
        };
    }

    // =========================================================================
    // UTILIDADES PRIVADAS
    // =========================================================================

    private static ArticuloDto MapToDto(Articulo a) => new()
    {
        Id = a.Id,
        CodigoBarras = a.CodigoBarras,
        CodigosBarrasSecundarios = a.CodigosBarrasSecundarios,
        SKU = a.SKU,
        CodigoProveedor = a.CodigoProveedor,
        Nombre = a.Nombre,
        Descripcion = a.Descripcion,
        CategoriaId = a.CategoriaId,
        CategoriaNombre = a.Categoria?.Nombre ?? "",
        MarcaId = a.MarcaId,
        MarcaNombre = a.Marca?.Nombre ?? "",
        ProveedorId = a.ProveedorId,
        ProveedorNombre = a.Proveedor?.Nombre ?? "",
        Tipo = a.Tipo,
        PrecioCosto = a.PrecioCosto,
        IvaPorcentaje = a.IvaPorcentaje,
        PorcentajeGanancia = a.PorcentajeGanancia,
        PrecioVenta = a.PrecioVenta,
        StockActual = a.Variantes != null && a.Variantes.Any(v => v.Activo)
            ? a.Variantes.Where(v => v.Activo).Sum(v => v.StockActual)
            : a.StockActual,
        StockMinimo = a.StockMinimo,
        UnidadMedida = a.UnidadMedida,
        Ubicacion = a.Ubicacion,
        EsBotonRapido = a.EsBotonRapido,
        ColorBoton = a.ColorBoton,
        Rubro = a.Rubro ?? "Librería",
        EsPack = a.EsPack,
        ArticuloBaseId = a.ArticuloBaseId,
        ArticuloBaseNombre = a.ArticuloBase?.Nombre ?? "",
        CantidadPorPack = a.CantidadPorPack > 0 ? a.CantidadPorPack : 1,
        UltimaAuditoriaStock = a.UltimaAuditoriaStock,
        Variantes = a.Variantes != null
            ? a.Variantes.Where(v => v.Activo).Select(v => new ArticuloVarianteDto
            {
                Id = v.Id,
                ArticuloId = v.ArticuloId,
                Nombre = v.Nombre,
                CodigoBarras = v.CodigoBarras,
                CodigoProveedor = v.CodigoProveedor,
                StockActual = v.StockActual,
                StockMinimo = v.StockMinimo,
                Activo = v.Activo,
                UltimaAuditoriaStock = v.UltimaAuditoriaStock
            }).ToList()
            : new List<ArticuloVarianteDto>(),
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

    private static (List<string> Headers, List<Dictionary<string, string>> Rows) LeerExcelSimple(Stream stream)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        var headers = new List<string>();
        var rows = new List<Dictionary<string, string>>();
        bool headerFound = false;

        while (reader.Read())
        {
            if (!headerFound)
            {
                var candidateHeaders = new List<string>();
                bool esCabecera = false;
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var val = reader.GetValue(i)?.ToString()?.Trim() ?? "";
                    candidateHeaders.Add(val);
                    var norm = NormalizarTextoColumna(val);
                    if (norm.Contains("cod") || norm.Contains("descrip") || norm.Contains("precio") || norm.Contains("costo") || norm.Contains("siva"))
                    {
                        esCabecera = true;
                    }
                }

                if (esCabecera)
                {
                    headers = candidateHeaders;
                    headerFound = true;
                }
                continue;
            }

            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            bool rowHasData = false;
            for (int i = 0; i < reader.FieldCount && i < headers.Count; i++)
            {
                var h = headers[i];
                if (string.IsNullOrWhiteSpace(h)) continue;
                var val = reader.GetValue(i)?.ToString()?.Trim() ?? "";
                if (!string.IsNullOrWhiteSpace(val)) rowHasData = true;
                row[h] = val;
            }

            if (rowHasData)
            {
                rows.Add(row);
            }
        }

        return (headers, rows);
    }

    private static string NormalizarTextoColumna(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var sb = new System.Text.StringBuilder();
        foreach (var ch in input)
        {
            if (ch == 'ó' || ch == 'Ó' || ch == '\uFFFD' || (int)ch == 65533) sb.Append('o');
            else if (ch == 'á' || ch == 'Á') sb.Append('a');
            else if (ch == 'é' || ch == 'É') sb.Append('e');
            else if (ch == 'í' || ch == 'Í') sb.Append('i');
            else if (ch == 'ú' || ch == 'Ú') sb.Append('u');
            else if (char.IsLetterOrDigit(ch)) sb.Append(char.ToLowerInvariant(ch));
        }
        return sb.ToString();
    }

    private static string ObtenerValorColumna(Dictionary<string, string> row, params string[] nombresPosibles)
    {
        // 1. Coincidencia exacta normalizada
        foreach (var p in nombresPosibles)
        {
            var normSearch = NormalizarTextoColumna(p);
            foreach (var kvp in row)
            {
                var normKey = NormalizarTextoColumna(kvp.Key);
                if (normKey.Equals(normSearch, StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(kvp.Value))
                        return kvp.Value;
                }
            }
        }

        // 2. Coincidencia semántica flexible (evitando colisión entre código de producto y código de barra)
        foreach (var p in nombresPosibles)
        {
            var normSearch = NormalizarTextoColumna(p);
            foreach (var kvp in row)
            {
                var normKey = NormalizarTextoColumna(kvp.Key);

                if (normSearch == "codigo" || normSearch == "codprov" || normSearch == "codigoproducto")
                {
                    if (normKey.Contains("barra") || normKey.Contains("barcode")) continue;
                    if (normKey == "codigo" || normKey == "cod" || normKey == "codprov" || (normKey.StartsWith("c") && normKey.EndsWith("digo")))
                    {
                        if (!string.IsNullOrWhiteSpace(kvp.Value))
                            return kvp.Value;
                    }
                }
                else if (normSearch.Contains("barra"))
                {
                    if (normKey.Contains("barra") || normKey.Contains("barcode"))
                    {
                        if (!string.IsNullOrWhiteSpace(kvp.Value))
                            return kvp.Value;
                    }
                }
                else
                {
                    if (normKey.Contains(normSearch, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrWhiteSpace(kvp.Value))
                            return kvp.Value;
                    }
                }
            }
        }
        return string.Empty;
    }

    private static int CalcularDigitoVerificadorEan13(string primeros12Digitos)
    {
        int suma = 0;
        for (int i = 0; i < 12; i++)
        {
            int d = primeros12Digitos[i] - '0';
            suma += (i % 2 == 0) ? d : d * 3;
        }
        int resto = suma % 10;
        return (resto == 0) ? 0 : 10 - resto;
    }
}
