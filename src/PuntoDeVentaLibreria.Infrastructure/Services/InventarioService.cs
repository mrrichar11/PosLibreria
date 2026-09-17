using ExcelDataReader;
using Microsoft.EntityFrameworkCore;
using PuntoDeVentaLibreria.Application.Common;
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
                .Include(a => a.Proveedor)
                .Where(a => a.Activo)
                .OrderBy(a => a.Nombre)
                .Take(50)
                .ToListAsync(ct);

            return articulos.Select(MapToDto).ToList();
        }

        var limpio = criterio.Trim().ToUpper();

        // 1. Coincidencia exacta de Código de Barras, SKU, Código de Proveedor o Código Secundario (prioridad escáner)
        var exacto = await _context.Articulos
            .AsNoTracking()
            .Include(a => a.Categoria)
            .Include(a => a.Marca)
            .Include(a => a.Proveedor)
            .FirstOrDefaultAsync(a => a.Activo && (
                a.CodigoBarras == limpio ||
                a.SKU == limpio ||
                a.CodigoProveedor == limpio ||
                (a.CodigosBarrasSecundarios != null && a.CodigosBarrasSecundarios.Contains(limpio))), ct);

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
            .Include(a => a.Proveedor)
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
                (a.Proveedor != null && a.Proveedor.Nombre.ToUpper().Contains(token)));
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
            .Include(a => a.Proveedor)
            .Include(a => a.ItemsDelCombo)
                .ThenInclude(c => c.ComponenteArticulo)
            .FirstOrDefaultAsync(a => a.Activo && (
                a.CodigoBarras == limpio ||
                a.SKU == limpio ||
                a.CodigoProveedor == limpio ||
                (a.CodigosBarrasSecundarios != null && a.CodigosBarrasSecundarios.Contains(limpio))), ct);

        return art != null ? MapToDto(art) : null;
    }

    public async Task<IReadOnlyList<ArticuloDto>> ObtenerBotonesRapidosAsync(CancellationToken ct = default)
    {
        var articulos = await _context.Articulos
            .AsNoTracking()
            .Include(a => a.Categoria)
            .Include(a => a.Marca)
            .Include(a => a.Proveedor)
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
    // MIGRACIÓN E IMPORTACIÓN MASIVA (ej. Lista de Precios ALMA LIBRE)
    // =========================================================================

    public async Task<IReadOnlyList<ItemPrevisualizacionAlmaLibreDto>> PrevisualizarCatalogoAlmaLibreAsync(Stream archivoExcelStream, CancellationToken ct = default)
    {
        var (_, rows) = LeerExcelSimple(archivoExcelStream);
        var resultado = new List<ItemPrevisualizacionAlmaLibreDto>();

        var skusExistentes = (await _context.Articulos.AsNoTracking().Select(a => a.SKU).ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var barrasExistentes = (await _context.Articulos.AsNoTracking().Where(a => a.CodigoBarras != null).Select(a => a.CodigoBarras!).ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            var sku = ObtenerValorColumna(row, "codigo", "sku");
            var codProv = ObtenerValorColumna(row, "codprov", "codigoproveedor");
            var codBarra = ObtenerValorColumna(row, "codbarra", "codigodebarra", "codigobarra", "barra");
            var descrip = ObtenerValorColumna(row, "descrip", "descripcion", "nombre", "articulo");
            var rubro = ObtenerValorColumna(row, "rubro", "categoria");

            if (string.IsNullOrWhiteSpace(descrip) && string.IsNullOrWhiteSpace(sku)) continue;

            CalculoPreciosUtils.TryParseMonto(ObtenerValorColumna(row, "costo", "preciocosto"), out var costo);
            CalculoPreciosUtils.TryParseMonto(ObtenerValorColumna(row, "pciva", "iva"), out var pciva);
            if (pciva <= 0) pciva = 21.0m; // Default IVA Argentina librerías

            CalculoPreciosUtils.TryParseMonto(ObtenerValorColumna(row, "pcgan", "ganancia%", "utilidad", "margen"), out var pcgan);
            if (pcgan <= 0) pcgan = 60.0m;

            CalculoPreciosUtils.TryParseMonto(ObtenerValorColumna(row, "precio", "precioventa", "pvp"), out var precio);
            if (precio <= 0 && costo > 0)
            {
                precio = CalculoPreciosUtils.CalcularPrecioVenta(costo, pcgan, pciva);
            }

            var existe = (!string.IsNullOrWhiteSpace(sku) && skusExistentes.Contains(sku)) ||
                         (!string.IsNullOrWhiteSpace(codBarra) && barrasExistentes.Contains(codBarra));

            resultado.Add(new ItemPrevisualizacionAlmaLibreDto
            {
                SKU = !string.IsNullOrWhiteSpace(sku) ? sku : (!string.IsNullOrWhiteSpace(codBarra) ? codBarra : "SIN-SKU"),
                CodigoProveedor = string.IsNullOrWhiteSpace(codProv) ? null : codProv,
                CodigoBarras = string.IsNullOrWhiteSpace(codBarra) ? null : codBarra,
                Nombre = !string.IsNullOrWhiteSpace(descrip) ? descrip : "Sin Descripción",
                CategoriaRubro = string.IsNullOrWhiteSpace(rubro) ? null : rubro,
                PrecioCosto = costo,
                IvaPorcentaje = pciva,
                PorcentajeGanancia = pcgan,
                PrecioVenta = precio,
                YaExisteEnSistema = existe
            });
        }

        return resultado;
    }

    public async Task<MigracionResultadoDto> ImportarCatalogoAlmaLibreAsync(Stream archivoExcelStream, Guid? proveedorId = null, CancellationToken ct = default)
    {
        var (_, rows) = LeerExcelSimple(archivoExcelStream);
        var resultado = new MigracionResultadoDto { TotalFilasProcesadas = rows.Count };

        // Precargar categorías para mapear rápidamente
        var categorias = await _context.Categorias.ToListAsync(ct);
        var dictCategorias = categorias.ToDictionary(c => c.Nombre.Trim().ToUpperInvariant(), c => c.Id);

        // Precargar artículos existentes por SKU y Barras
        var articulosExistentes = await _context.Articulos.ToListAsync(ct);
        var dictPorSku = articulosExistentes.Where(a => !string.IsNullOrEmpty(a.SKU))
            .ToDictionary(a => a.SKU.Trim().ToUpperInvariant(), a => a);
        var dictPorBarra = articulosExistentes.Where(a => !string.IsNullOrEmpty(a.CodigoBarras))
            .ToDictionary(a => a.CodigoBarras!.Trim().ToUpperInvariant(), a => a);

        int secuenciaSku = articulosExistentes.Count + 1;

        foreach (var row in rows)
        {
            try
            {
                var sku = ObtenerValorColumna(row, "codigo", "sku")?.Trim();
                var codProv = ObtenerValorColumna(row, "codprov", "codigoproveedor")?.Trim();
                var codBarra = ObtenerValorColumna(row, "codbarra", "codigodebarra", "codigobarra", "barra")?.Trim();
                var descrip = ObtenerValorColumna(row, "descrip", "descripcion", "nombre", "articulo")?.Trim();
                var rubro = ObtenerValorColumna(row, "rubro", "categoria")?.Trim();

                if (string.IsNullOrWhiteSpace(descrip))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(sku))
                {
                    sku = !string.IsNullOrWhiteSpace(codBarra) ? codBarra : $"ART-{secuenciaSku++:D5}";
                }

                CalculoPreciosUtils.TryParseMonto(ObtenerValorColumna(row, "costo", "preciocosto"), out var costo);
                CalculoPreciosUtils.TryParseMonto(ObtenerValorColumna(row, "pciva", "iva"), out var pciva);
                if (pciva <= 0) pciva = 21.0m;

                CalculoPreciosUtils.TryParseMonto(ObtenerValorColumna(row, "pcgan", "ganancia%", "utilidad", "margen"), out var pcgan);
                if (pcgan <= 0) pcgan = 60.0m;

                CalculoPreciosUtils.TryParseMonto(ObtenerValorColumna(row, "precio", "precioventa", "pvp"), out var precio);
                if (precio <= 0 && costo > 0)
                {
                    precio = CalculoPreciosUtils.CalcularPrecioVenta(costo, pcgan, pciva);
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
                    }
                }

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
                    if (proveedorId.HasValue) articulo.ProveedorId = proveedorId;

                    articulo.PrecioCosto = costo;
                    articulo.IvaPorcentaje = pciva;
                    articulo.PorcentajeGanancia = pcgan;
                    articulo.PrecioVenta = precio;
                    articulo.Activo = true;

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
                        ProveedorId = proveedorId,
                        PrecioCosto = costo,
                        IvaPorcentaje = pciva,
                        PorcentajeGanancia = pcgan,
                        PrecioVenta = precio,
                        StockActual = 0,
                        StockMinimo = 5,
                        Activo = true
                    };

                    _context.Articulos.Add(nuevo);
                    dictPorSku[sku.ToUpperInvariant()] = nuevo;
                    if (!string.IsNullOrWhiteSpace(codBarra))
                    {
                        dictPorBarra[codBarra.ToUpperInvariant()] = nuevo;
                    }
                    resultado.ArticulosCreados++;
                }
            }
            catch (Exception ex)
            {
                resultado.Errores++;
                if (resultado.MensajesErrores.Count < 20)
                {
                    resultado.MensajesErrores.Add($"Error en fila: {ex.Message}");
                }
            }
        }

        await _context.SaveChangesAsync(ct);
        return resultado;
    }

    // =========================================================================
    // ACTUALIZACIÓN MASIVA DE PRECIOS POR LISTA DE MAYORISTA (ej. El Once)
    // =========================================================================

    public async Task<ResumenPrevisualizacionAumentoDto> PrevisualizarActualizacionPreciosProveedorAsync(Stream archivoExcelStream, Guid? proveedorId = null, CancellationToken ct = default)
    {
        var (_, rows) = LeerExcelSimple(archivoExcelStream);
        var resumen = new ResumenPrevisualizacionAumentoDto();

        var query = _context.Articulos
            .AsNoTracking()
            .Include(a => a.Proveedor)
            .Where(a => a.Activo);

        if (proveedorId.HasValue)
        {
            query = query.Where(a => a.ProveedorId == proveedorId);
        }

        var articulos = await query.ToListAsync(ct);

        // Indexar catálogo para coincidencia veloz O(1)
        var dictPorCodProv = articulos
            .Where(a => !string.IsNullOrWhiteSpace(a.CodigoProveedor))
            .ToDictionary(a => a.CodigoProveedor!.Trim().ToUpperInvariant(), a => a);

        var dictPorBarraPrincipal = articulos
            .Where(a => !string.IsNullOrWhiteSpace(a.CodigoBarras))
            .ToDictionary(a => a.CodigoBarras!.Trim().ToUpperInvariant(), a => a);

        int noEncontrados = 0;

        foreach (var row in rows)
        {
            var codProvExcel = ObtenerValorColumna(row, "codigo", "codprov", "codigoproducto", "codigoproveedor")?.Trim();
            var codBarraExcel = ObtenerValorColumna(row, "codigodebarra", "codigobarra", "codbarra", "barra", "barcode")?.Trim();

            // Costo S/IVA y C/IVA del proveedor
            CalculoPreciosUtils.TryParseMonto(ObtenerValorColumna(row, "s/iva", "siva", "costo", "siniva"), out var costoSiva);
            CalculoPreciosUtils.TryParseMonto(ObtenerValorColumna(row, "c/iva", "civa", "coniva"), out var costoCiva);

            decimal costoNuevo = costoSiva > 0 ? costoSiva : (costoCiva > 0 ? Math.Round(costoCiva / 1.21m, 2) : 0m);
            if (costoNuevo <= 0) continue;

            // Coincidencia:
            // 1. Por Código Proveedor (el más exacto para El Once)
            // 2. Por Código de Barra principal
            // 3. Por Códigos Secundarios
            Articulo? art = null;
            if (!string.IsNullOrWhiteSpace(codProvExcel) && dictPorCodProv.TryGetValue(codProvExcel.ToUpperInvariant(), out var encontradoProv))
            {
                art = encontradoProv;
            }
            else if (!string.IsNullOrWhiteSpace(codBarraExcel) && dictPorBarraPrincipal.TryGetValue(codBarraExcel.ToUpperInvariant(), out var encontradoBarra))
            {
                art = encontradoBarra;
            }
            else if (!string.IsNullOrWhiteSpace(codBarraExcel))
            {
                art = articulos.FirstOrDefault(a => a.CodigosBarrasSecundarios != null && a.CodigosBarrasSecundarios.Contains(codBarraExcel));
            }

            if (art == null)
            {
                noEncontrados++;
                continue;
            }

            resumen.CoincidenciasEncontradas++;

            // Calcular nuevo precio de venta manteniendo el margen del local y su IVA
            var ventaNueva = CalculoPreciosUtils.CalcularPrecioVenta(costoNuevo, art.PorcentajeGanancia, art.IvaPorcentaje);

            bool hayCambio = Math.Abs(art.PrecioCosto - costoNuevo) > 0.01m || Math.Abs(art.PrecioVenta - ventaNueva) > 0.01m;
            if (hayCambio)
            {
                resumen.CoincidenciasConCambioDePrecio++;
            }

            resumen.ItemsParaActualizar.Add(new ArticuloAumentoPrecioItemDto
            {
                ArticuloId = art.Id,
                SKU = art.SKU,
                Nombre = art.Nombre,
                CodigoProveedor = art.CodigoProveedor,
                CodigoBarras = art.CodigoBarras,
                CostoAnterior = art.PrecioCosto,
                CostoNuevo = costoNuevo,
                VentaAnterior = art.PrecioVenta,
                VentaNueva = ventaNueva,
                PorcentajeGanancia = art.PorcentajeGanancia,
                IvaPorcentaje = art.IvaPorcentaje,
                Aplicar = hayCambio
            });
        }

        resumen.NoEncontradosEnCatalogo = noEncontrados;
        return resumen;
    }

    public async Task<ActualizacionPreciosResultadoDto> AplicarActualizacionPreciosAsync(IEnumerable<ArticuloAumentoPrecioItemDto> items, CancellationToken ct = default)
    {
        var itemsSeleccionados = items.Where(i => i.Aplicar).ToList();
        if (!itemsSeleccionados.Any())
        {
            return new ActualizacionPreciosResultadoDto
            {
                TotalActualizados = 0,
                Mensaje = "No se seleccionó ningún artículo para actualizar."
            };
        }

        var ids = itemsSeleccionados.Select(i => i.ArticuloId).ToList();
        var articulos = await _context.Articulos.Where(a => ids.Contains(a.Id)).ToListAsync(ct);
        var dictItems = itemsSeleccionados.ToDictionary(i => i.ArticuloId);

        int actualizados = 0;
        foreach (var a in articulos)
        {
            if (dictItems.TryGetValue(a.Id, out var item))
            {
                a.PrecioCosto = item.CostoNuevo;
                a.PrecioVenta = item.VentaNueva;
                actualizados++;
            }
        }

        await _context.SaveChangesAsync(ct);
        return new ActualizacionPreciosResultadoDto
        {
            TotalActualizados = actualizados,
            Mensaje = $"Se actualizaron los precios de {actualizados} artículos exitosamente."
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
