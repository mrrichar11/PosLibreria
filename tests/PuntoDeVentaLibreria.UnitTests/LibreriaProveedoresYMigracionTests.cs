using System.IO;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PuntoDeVentaLibreria.Application.Common;
using PuntoDeVentaLibreria.Application.DTOs.Inventario;
using PuntoDeVentaLibreria.Application.DTOs.Proveedores;
using PuntoDeVentaLibreria.Infrastructure.Data;
using PuntoDeVentaLibreria.Infrastructure.Services;
using Xunit;

namespace PuntoDeVentaLibreria.UnitTests;

public class LibreriaProveedoresYMigracionTests
{
    private AppDbContext CrearContextoEnMemoria()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task ProveedorService_GuardarYObtener_PersisteYActualizaCorrectamente()
    {
        using var context = CrearContextoEnMemoria();
        var service = new ProveedorService(context);

        var dto = new ProveedorDto
        {
            Nombre = "El Once Mayorista",
            RazonSocial = "Distribuidora El Once S.A.",
            Cuit = "30-55443322-9",
            Contacto = "Mariana Ventas",
            Telefono = "011-4455-6677",
            DiasVisitaOEntrega = "Martes y Viernes",
            Notas = "Descuento 5% por pago contado contra entrega."
        };

        var guardado = await service.GuardarAsync(dto);
        guardado.Id.Should().NotBeEmpty();

        var obtenido = await service.ObtenerPorIdAsync(guardado.Id);
        obtenido.Should().NotBeNull();
        obtenido!.Nombre.Should().Be("El Once Mayorista");
        obtenido.RazonSocial.Should().Be("Distribuidora El Once S.A.");
        obtenido.DiasVisitaOEntrega.Should().Be("Martes y Viernes");

        var todos = await service.ObtenerTodosAsync();
        todos.Should().HaveCount(1);
    }

    [Fact]
    public async Task InventarioService_BusquedaPorCodigosSecundariosYProveedor_EncuentraArticulo()
    {
        using var context = CrearContextoEnMemoria();
        var service = new InventarioService(context);

        var art = new ArticuloDto
        {
            Nombre = "Cuaderno Tapa Dura 48H Rivadavia",
            SKU = "CUAD-001",
            CodigoBarras = "7791234567890",
            CodigosBarrasSecundarios = "7791234567891, 7791234567892", // Variantes colores rojo y azul
            CodigoProveedor = "31030",
            PrecioCosto = 1200m,
            IvaPorcentaje = 21m,
            PorcentajeGanancia = 60m,
            PrecioVenta = 2323.20m,
            StockActual = 40,
            StockMinimo = 10
        };

        await service.GuardarArticuloAsync(art);

        // 1. Búsqueda por código de barras secundario (variante de color)
        var encontradoPorVariante = await service.BuscarPorCodigoBarrasAsync("7791234567892");
        encontradoPorVariante.Should().NotBeNull();
        encontradoPorVariante!.Nombre.Should().Be("Cuaderno Tapa Dura 48H Rivadavia");
        encontradoPorVariante.SKU.Should().Be("CUAD-001");

        // 2. Búsqueda por código de catálogo del proveedor
        var encontradoPorCodProv = await service.BuscarArticulosAsync("31030");
        encontradoPorCodProv.Should().HaveCount(1);
        encontradoPorCodProv[0].CodigoProveedor.Should().Be("31030");

        // 3. Búsqueda general
        var encontradoGeneral = await service.BuscarArticulosAsync("7791234567891");
        encontradoGeneral.Should().HaveCount(1);
    }

    [Fact]
    public void CalculoPreciosUtils_ConIvaLibrosYLibreria_CalculaPreciosExactos()
    {
        // 1. Artículo de librería general (21% IVA, 60% ganancia)
        var costoLibreria = 1000m;
        var ventaLibreria = CalculoPreciosUtils.CalcularPrecioVenta(costoLibreria, 60m, 21m);
        // Costo con IVA = 1210. Con 60% ganancia = 1210 * 1.6 = 1936
        ventaLibreria.Should().Be(1936m);

        // 2. Libro exento (0% IVA Ley del Libro, 50% ganancia)
        var costoLibro = 1000m;
        var ventaLibro = CalculoPreciosUtils.CalcularPrecioVenta(costoLibro, 50m, 0m);
        ventaLibro.Should().Be(1500m);
    }

    [Fact]
    public async Task InventarioService_MigracionAlmaLibre_ProcesaCatalogoRealSiExiste()
    {
        var rutaArchivo = @"C:\Proyectos\PuntoDeVentaLibreria\Lista de precios ALMA LIBRE.xlsx";
        if (!File.Exists(rutaArchivo)) return; // Omitir si se corre en entorno sin archivo

        using var context = CrearContextoEnMemoria();
        var service = new InventarioService(context);

        using (var stream = File.OpenRead(rutaArchivo))
        {
            var preview = await service.PrevisualizarCatalogoAlmaLibreAsync(stream);
            preview.Should().NotBeEmpty();
            preview.Count.Should().BeGreaterThan(2500); // 2.863 artículos

            var primerItem = preview.First();
            primerItem.Nombre.Should().NotBeNullOrWhiteSpace();
            primerItem.PrecioVenta.Should().BeGreaterThan(0);
        }

        using (var stream = File.OpenRead(rutaArchivo))
        {
            var resultado = await service.ImportarCatalogoAlmaLibreAsync(stream);
            resultado.ArticulosCreados.Should().BeGreaterThan(2500);

            var totalEnDb = await context.Articulos.CountAsync();
            totalEnDb.Should().Be(resultado.ArticulosCreados);

            var artEjemplo = await context.Articulos.FirstOrDefaultAsync(a => a.SKU == "5");
            artEjemplo.Should().NotBeNull();
            artEjemplo!.PrecioVenta.Should().Be(5900m);
        }
    }

    [Fact]
    public async Task InventarioService_ActualizacionPreciosMayorista_ActualizaPreciosDesdeElOnce()
    {
        var rutaArchivo = @"C:\Proyectos\PuntoDeVentaLibreria\lista de precio proveedor El Once_2026-09-17 (Con Cod.Barra).xls";
        if (!File.Exists(rutaArchivo)) return;

        using var context = CrearContextoEnMemoria();
        var service = new InventarioService(context);

        // Crear un artículo que coincide con un código real de El Once (ej. código proveedor "31030" o barra de El Once)
        var art = new ArticuloDto
        {
            Nombre = "Artículo de Prueba El Once",
            SKU = "TEST-001",
            CodigoProveedor = "31030",
            PrecioCosto = 500m,
            IvaPorcentaje = 21m,
            PorcentajeGanancia = 60m,
            PrecioVenta = 968m,
            StockActual = 10,
            StockMinimo = 5
        };
        await service.GuardarArticuloAsync(art);

        using (var stream = File.OpenRead(rutaArchivo))
        {
            var resumen = await service.PrevisualizarActualizacionPreciosProveedorAsync(stream);
            resumen.CoincidenciasEncontradas.Should().BeGreaterThanOrEqualTo(1);

            var item = resumen.ItemsParaActualizar.FirstOrDefault(i => i.CodigoProveedor == "31030");
            item.Should().NotBeNull();
            item!.CostoAnterior.Should().Be(500m);
            item.CostoNuevo.Should().BeGreaterThan(0);
            item.VentaNueva.Should().BeGreaterThan(item.CostoNuevo);

            // Aplicar actualización
            var res = await service.AplicarActualizacionPreciosAsync(new[] { item });
            res.TotalActualizados.Should().Be(1);

            var artActualizado = await service.BuscarArticulosAsync("TEST-001");
            artActualizado[0].PrecioCosto.Should().Be(item.CostoNuevo);
            artActualizado[0].PrecioVenta.Should().Be(item.VentaNueva);
        }
    }

    [Fact]
    public async Task InventarioService_MigracionGenerica_ConStockFechasYTarjetas_ProcesaCorrectamente()
    {
        var rutaArchivo = @"C:\Proyectos\PuntoDeVentaLibreria\Lista de precios ALMA LIBRE.xlsx";
        if (!File.Exists(rutaArchivo)) return;

        using var context = CrearContextoEnMemoria();
        var service = new InventarioService(context);

        using var stream = File.OpenRead(rutaArchivo);
        var preview = await service.PrevisualizarCatalogoAlmaLibreAsync(stream);
        preview.Should().NotBeEmpty();

        // 1. Validar fechas parseadas
        var itemsConFecha = preview.Where(i => i.FechaAlta.HasValue || i.FechaUltimaActualizacionPrecio.HasValue).ToList();
        itemsConFecha.Should().NotBeEmpty();

        // 2. Validar precios de tarjeta y recargos
        var itemsConTarjeta = preview.Where(i => i.PrecioTarjeta.HasValue && i.PrecioTarjeta > 0).ToList();
        itemsConTarjeta.Should().NotBeEmpty();
        var ejemploTarjeta = itemsConTarjeta.First();
        ejemploTarjeta.PrecioTarjeta.Should().BeGreaterThan(0);
        ejemploTarjeta.RecargoTarjetaPorcentaje.Should().BeGreaterThan(0);

        // 3. Probar importación selectiva con asignación de stock inicial
        var muestraImportar = preview.Take(5).ToList();
        foreach (var m in muestraImportar)
        {
            m.Seleccionado = true;
            m.StockImportar = 12m;
        }

        var res = await service.ImportarCatalogoSeleccionadoAsync(muestraImportar);
        res.ArticulosCreados.Should().Be(5);
        res.TotalStockIngresado.Should().Be(60m);

        var guardados = await context.Articulos.Include(a => a.MovimientosStock).ToListAsync();
        guardados.Should().HaveCount(5);
        foreach (var g in guardados)
        {
            g.StockActual.Should().Be(12m);
            g.MovimientosStock.Should().HaveCount(1);
            g.MovimientosStock.First().Cantidad.Should().Be(12m);
        }
    }

    [Fact]
    public void DetectarFactorPack_ConDiferentesCasos_IdentificaFactorExacto()
    {
        // Caso 1: Broches Clips Pastel N6 (15 potes de 60 unidades)
        // Costo anterior tienda: $1.750. Costo proveedor por bulto: $25.986
        var factorClips = InventarioService.DetectarFactorPack(
            "*BROCHES CLIPS PASTEL Nº6 x15 Potes x60u", 
            "CLIPS PASTEL N6 EN POTE - SIFAP", 
            1750m, 
            25986m);
        factorClips.Should().Be(15);

        // Caso 2: Bolígrafos BIC caja x 50
        // Costo anterior tienda: $350. Costo proveedor caja: $18.200
        var factorBic = InventarioService.DetectarFactorPack(
            "BOLIGRAFO BIC CRISTAL AZUL OP. X 50 UNID", 
            "BOLIGRAFO BIC CRISTAL AZUL", 
            350m, 
            18200m);
        factorBic.Should().Be(50);

        // Caso 3: Cuadernos pack x 10
        // Costo anterior tienda: $2.100. Costo proveedor: $21.500
        var factorCuadernos = InventarioService.DetectarFactorPack(
            "CUADERNO ABC 48 HJS RAY. (PACK X 10)", 
            "CUADERNO ABC 48HS RAYADO", 
            2100m, 
            21500m);
        factorCuadernos.Should().Be(10);

        // Caso 4: Artículo con aumento inflacionario normal (sin factor pack)
        // Costo anterior: $300. Costo proveedor: $360 (+20%)
        var factorGoma = InventarioService.DetectarFactorPack(
            "GOMA DE BORRAR FACTIS S20", 
            "GOMA FACTIS S20", 
            300m, 
            360m);
        factorGoma.Should().BeNull();
    }

    [Fact]
    public void ArticuloAumentoPrecioItemDto_AlModificarFactorConversion_RecalculaPreciosYAlerta()
    {
        var item = new ArticuloAumentoPrecioItemDto
        {
            SKU = "CLIPS-001",
            Nombre = "CLIPS PASTEL N6 EN POTE",
            CostoAnterior = 1750m,
            CostoOriginalProveedor = 25986m,
            PorcentajeGanancia = 60m,
            IvaPorcentaje = 21m,
            FactorConversion = 1m,
            FactorSugerido = 15
        };

        item.Recalcular();

        // Con factor 1: el costo nuevo es $25.986 (+1384.9%) -> alerta extrema activa
        item.CostoNuevo.Should().Be(25986m);
        item.VariacionPorcentaje.Should().BeGreaterThan(1000m);
        item.EsAlertaVariacionExtrema.Should().BeTrue();
        item.MostrarBotonSugerido.Should().BeTrue();
        item.SugerenciaBotonTexto.Should().Be("÷15");

        // Al aplicar factor 15 (como en un pack de 15 potes):
        item.FactorConversion = 15m;

        // Costo nuevo unitario: 25.986 / 15 = 1.732,40
        item.CostoNuevo.Should().Be(1732.40m);
        item.VariacionPorcentaje.Should().BeApproximately(-1.0m, 0.2m);
        item.EsAlertaVariacionExtrema.Should().BeFalse();
        item.MostrarBotonSugerido.Should().BeFalse();

        // Precio de venta recalculado manteniendo el 60% de ganancia y 21% IVA
        // Costo con IVA = 1732.40 * 1.21 = 2096.204. Con 60% = 3353.93
        item.VentaNueva.Should().Be(CalculoPreciosUtils.CalcularPrecioVenta(1732.40m, 60m, 21m));
    }

    [Fact]
    public async Task InventarioService_ActualizacionPrecios_DeduplicaCoincidenciasYAsignaProveedor()
    {
        using var context = CrearContextoEnMemoria();
        var inventarioService = new InventarioService(context);

        // 1. Dar de alta un proveedor "Distribuidora El Once"
        var proveedor = new PuntoDeVentaLibreria.Domain.Entities.Proveedores.Proveedor
        {
            Id = Guid.NewGuid(),
            Nombre = "Distribuidora El Once",
            RazonSocial = "El Once S.A."
        };
        context.Proveedores.Add(proveedor);

        // 2. Dar de alta artículos en catálogo que NO tienen proveedor asignado (migrados de AlmaLibre)
        var art1 = new PuntoDeVentaLibreria.Domain.Entities.Catalogo.Articulo
        {
            Id = Guid.NewGuid(),
            SKU = "ART-001",
            Nombre = "Cuaderno Rivadavia Tapa Dura",
            CodigoProveedor = "31030",
            PrecioCosto = 1000m,
            PrecioVenta = 1936m,
            PorcentajeGanancia = 60m,
            IvaPorcentaje = 21m,
            Activo = true,
            ProveedorId = null
        };
        var art2 = new PuntoDeVentaLibreria.Domain.Entities.Catalogo.Articulo
        {
            Id = Guid.NewGuid(),
            SKU = "ART-002",
            Nombre = "Témpera Alba 250ml",
            CodigoProveedor = "42010",
            PrecioCosto = 800m,
            PrecioVenta = 1548.80m,
            PorcentajeGanancia = 60m,
            IvaPorcentaje = 21m,
            Activo = true,
            ProveedorId = null
        };
        context.Articulos.AddRange(art1, art2);
        await context.SaveChangesAsync();

        // 3. Preparar items de actualización, incluyendo un duplicado accidental en la lista
        var item1 = new ArticuloAumentoPrecioItemDto
        {
            ArticuloId = art1.Id,
            SKU = art1.SKU,
            Nombre = art1.Nombre,
            CostoAnterior = 1000m,
            CostoNuevo = 1200m,
            VentaNueva = 2323.20m,
            Aplicar = true
        };
        // Duplicado accidental del mismo artículo:
        var item1Duplicado = new ArticuloAumentoPrecioItemDto
        {
            ArticuloId = art1.Id,
            SKU = art1.SKU,
            Nombre = art1.Nombre,
            CostoAnterior = 1000m,
            CostoNuevo = 1200m,
            VentaNueva = 2323.20m,
            Aplicar = true
        };
        var item2 = new ArticuloAumentoPrecioItemDto
        {
            ArticuloId = art2.Id,
            SKU = art2.SKU,
            Nombre = art2.Nombre,
            CostoAnterior = 800m,
            CostoNuevo = 950m,
            VentaNueva = 1839.20m,
            Aplicar = true
        };

        // 4. Aplicar actualización con asignación automática de proveedor
        var items = new[] { item1, item1Duplicado, item2 };
        var resultado = await inventarioService.AplicarActualizacionPreciosAsync(items, asignarProveedorId: proveedor.Id);

        resultado.TotalActualizados.Should().Be(2); // Deduplicado a 2 artículos distintos
        resultado.Mensaje.Should().Contain("Distribuidora El Once");

        // 5. Verificar que en la base de datos se actualizaron los precios y se asignó el proveedor
        var art1Db = await context.Articulos.FindAsync(art1.Id);
        art1Db!.PrecioCosto.Should().Be(1200m);
        art1Db.PrecioVenta.Should().Be(2323.20m);
        art1Db.ProveedorId.Should().Be(proveedor.Id);

        var art2Db = await context.Articulos.FindAsync(art2.Id);
        art2Db!.PrecioCosto.Should().Be(950m);
        art2Db.PrecioVenta.Should().Be(1839.20m);
        art2Db.ProveedorId.Should().Be(proveedor.Id);
    }
}
