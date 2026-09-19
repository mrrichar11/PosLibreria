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
    public async Task InventarioService_MigracionAlmaLibre_ConStockFechasTarjetaYMayoristaOnce_ProcesaCorrectamente()
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

        // 3. Validar cruce con Mayorista El Once
        var itemsElOnce = preview.Where(i => i.EsDeMayoristaElOnce).ToList();
        itemsElOnce.Should().NotBeEmpty();

        // 4. Probar importación selectiva con asignación de stock inicial
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
}
