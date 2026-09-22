using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PuntoDeVentaLibreria.Application.DTOs.Inventario;
using PuntoDeVentaLibreria.Application.DTOs.Ventas;
using PuntoDeVentaLibreria.Domain.Entities.Catalogo;
using PuntoDeVentaLibreria.Domain.Entities.Finanzas;
using PuntoDeVentaLibreria.Infrastructure.Data;
using PuntoDeVentaLibreria.Infrastructure.Services;
using Xunit;

namespace PuntoDeVentaLibreria.UnitTests;

public class LibreriaOperativaTests
{
    private static AppDbContext CrearContextoEnMemoria()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"LibreriaOperativaDb_{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task VentaService_ArticuloPack_DescuentaStockDeArticuloBase()
    {
        using var context = CrearContextoEnMemoria();
        var ventaService = new VentaService(context);

        var turno = new TurnoCaja { MontoInicialEfectivo = 1000m, UsuarioApertura = "Cajera Turno" };
        context.TurnosCaja.Add(turno);
        await context.SaveChangesAsync();

        // 1. Crear artículo base suelto (ej. Bolígrafo Multicolor individual con 20 unidades)
        var articuloBase = new Articulo
        {
            Id = Guid.NewGuid(),
            Nombre = "Bolígrafo Multicolor Suelto",
            SKU = "BOL-MULTI-01",
            CodigoBarras = "779000100001",
            PrecioCosto = 1000m,
            PrecioVenta = 2000m,
            StockActual = 20m,
            Rubro = "Librería"
        };
        context.Articulos.Add(articuloBase);

        // 2. Crear artículo Pack (ej. Caja x4 lapiceras a $6.500) vinculado al artículo base
        var packCaja = new Articulo
        {
            Id = Guid.NewGuid(),
            Nombre = "Caja Bolígrafos Multicolor x4",
            SKU = "CAJA-MULTI-04",
            CodigoBarras = "779000100004",
            PrecioCosto = 3500m,
            PrecioVenta = 6500m,
            StockActual = 0m,
            EsPack = true,
            ArticuloBaseId = articuloBase.Id,
            CantidadPorPack = 4m,
            Rubro = "Librería"
        };
        context.Articulos.Add(packCaja);
        await context.SaveChangesAsync();

        // 3. Vender 2 Cajas en el punto de venta
        var dto = new RegistrarVentaDto
        {
            TurnoCajaId = turno.Id,
            VendedoraNombre = "Cajera Turno",
            MetodoPago = "Efectivo",
            MontoEntregado = 13000m,
            Items = new List<ItemCarritoDto>
            {
                new()
                {
                    ArticuloId = packCaja.Id,
                    Cantidad = 2, // 2 cajas de 4 = 8 unidades
                    PrecioUnitario = 6500m,
                    PrecioCosto = 3500m,
                    Descripcion = packCaja.Nombre,
                    SKU = packCaja.SKU
                }
            }
        };

        var resultado = await ventaService.ProcesarVentaAsync(dto);
        resultado.Should().NotBeNull();
        resultado.TotalCobrado.Should().Be(13000m);

        // 4. Verificar que se descontaron exactamente 8 unidades del artículo base (20 - 8 = 12)
        var baseActualizado = await context.Articulos.FindAsync(articuloBase.Id);
        baseActualizado.Should().NotBeNull();
        baseActualizado!.StockActual.Should().Be(12m);

        // 5. Verificar que se generó el movimiento de kardex sobre el artículo base
        var movimiento = await context.MovimientosStock.FirstOrDefaultAsync(m => m.ArticuloId == articuloBase.Id);
        movimiento.Should().NotBeNull();
        movimiento!.Cantidad.Should().Be(-8m);
        movimiento.StockPrevio.Should().Be(20m);
        movimiento.StockPosterior.Should().Be(12m);
    }

    [Fact]
    public async Task InventarioService_AjustarStockRapido_ActualizaExistenciasYKardex()
    {
        using var context = CrearContextoEnMemoria();
        var inventarioService = new InventarioService(context);

        var art = new Articulo
        {
            Id = Guid.NewGuid(),
            Nombre = "Cuaderno Tapa Dura 48h",
            SKU = "CD-48H-AZUL",
            CodigoBarras = "779998877665",
            PrecioCosto = 1500m,
            PrecioVenta = 3200m,
            StockActual = 5m,
            Rubro = "Librería"
        };
        context.Articulos.Add(art);
        await context.SaveChangesAsync();

        // Auditoría física: en góndola hay 14 cuadernos
        var res = await inventarioService.AjustarStockRapidoAsync(art.Id, 14m, "Auditoría Domingo", "Vendedora 1");

        res.StockActual.Should().Be(14m);
        res.YaAuditado.Should().BeTrue();
        res.UltimaAuditoriaStock.Should().NotBeNull();

        var artDb = await context.Articulos.FindAsync(art.Id);
        artDb!.StockActual.Should().Be(14m);
        artDb.UltimaAuditoriaStock.Should().NotBeNull();

        var movimiento = await context.MovimientosStock.FirstOrDefaultAsync(m => m.ArticuloId == art.Id);
        movimiento.Should().NotBeNull();
        movimiento!.Cantidad.Should().Be(9m); // 14 - 5 = +9
        movimiento.StockPrevio.Should().Be(5m);
        movimiento.StockPosterior.Should().Be(14m);
        movimiento.Motivo.Should().Be("Auditoría Domingo");
    }

    [Fact]
    public async Task InventarioService_ObtenerProgresoAuditoria_CalculaMetricasCorrectamente()
    {
        using var context = CrearContextoEnMemoria();
        var inventarioService = new InventarioService(context);

        // Crear 4 artículos: 1 auditado y 3 pendientes
        context.Articulos.AddRange(
            new Articulo { Id = Guid.NewGuid(), Nombre = "Art 1", SKU = "A1", UltimaAuditoriaStock = DateTime.UtcNow },
            new Articulo { Id = Guid.NewGuid(), Nombre = "Art 2", SKU = "A2", UltimaAuditoriaStock = null },
            new Articulo { Id = Guid.NewGuid(), Nombre = "Art 3", SKU = "A3", UltimaAuditoriaStock = null },
            new Articulo { Id = Guid.NewGuid(), Nombre = "Art 4", SKU = "A4", UltimaAuditoriaStock = null }
        );
        await context.SaveChangesAsync();

        var progreso = await inventarioService.ObtenerProgresoAuditoriaAsync();
        progreso.TotalArticulos.Should().Be(4);
        progreso.ArticulosAuditados.Should().Be(1);
        progreso.ArticulosPendientes.Should().Be(3);
        progreso.PorcentajeCompletado.Should().Be(25.0);
    }

    [Fact]
    public async Task ReporteService_DesgloseLibreriaYRegaleria_CalculaFacturacionSeparada()
    {
        using var context = CrearContextoEnMemoria();
        var reporteService = new ReporteService(context);

        var artLibreria = new Articulo { Id = Guid.NewGuid(), Nombre = "Cartuchera Escolar", SKU = "LIB-01", Rubro = "Librería" };
        var artRegaleria = new Articulo { Id = Guid.NewGuid(), Nombre = "Taza Cerámica Flores", SKU = "REG-01", Rubro = "Regalería" };
        context.Articulos.AddRange(artLibreria, artRegaleria);

        var venta = new PuntoDeVentaLibreria.Domain.Entities.Ventas.Venta
        {
            Id = Guid.NewGuid(),
            NumeroComprobante = "T0001-00000001",
            FechaVenta = DateTime.UtcNow,
            TotalVenta = 10000m,
            LineasVenta = new List<PuntoDeVentaLibreria.Domain.Entities.Ventas.LineaVenta>
            {
                new() { Id = Guid.NewGuid(), ArticuloId = artLibreria.Id, Cantidad = 2, PrecioUnitarioVenta = 3000m }, // 6000 Librería
                new() { Id = Guid.NewGuid(), ArticuloId = artRegaleria.Id, Cantidad = 1, PrecioUnitarioVenta = 4000m }  // 4000 Regalería
            },
            Pagos = new List<PuntoDeVentaLibreria.Domain.Entities.Ventas.PagoVenta>
            {
                new() { Id = Guid.NewGuid(), MetodoPago = "Efectivo", Monto = 10000m }
            }
        };
        context.Ventas.Add(venta);
        await context.SaveChangesAsync();

        var reporte = await reporteService.ObtenerMetricasDashboardAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));

        reporte.FacturacionTotal.Should().Be(10000m);
        reporte.FacturacionLibreria.Should().Be(6000m);
        reporte.FacturacionRegaleria.Should().Be(4000m);
        reporte.PorcentajeLibreria.Should().Be(60.0);
        reporte.PorcentajeRegaleria.Should().Be(40.0);
        reporte.CantidadArticulosLibreria.Should().Be(2m);
        reporte.CantidadArticulosRegaleria.Should().Be(1m);
    }

    [Fact]
    public async Task InventarioService_AnalizarExcelAlmaLibre_ProcesaArchivoCorrectamente()
    {
        var rutaArchivo = @"c:\Proyectos\PuntoDeVentaLibreria\Lista de precios ALMA LIBRE.xlsx";
        if (!System.IO.File.Exists(rutaArchivo)) return;

        using var context = CrearContextoEnMemoria();
        var inventarioService = new InventarioService(context);

        using var stream = System.IO.File.OpenRead(rutaArchivo);
        var resultado = await inventarioService.AnalizarExcelGenericoAsync(stream);

        resultado.Should().NotBeNull();
        resultado.Items.Should().NotBeEmpty();
        resultado.ColumnasDetectadas.Should().NotBeEmpty();
    }

    [Fact]
    public async Task InventarioService_ObtenerArticulosPaginados_NavegaYFiltraCorrectamente()
    {
        using var context = CrearContextoEnMemoria();
        var inventarioService = new InventarioService(context);

        // Crear 120 artículos (70 Librería, 50 Regalería)
        for (int i = 1; i <= 120; i++)
        {
            context.Articulos.Add(new Articulo
            {
                Id = Guid.NewGuid(),
                Nombre = $"Artículo Prueba {i:D3}",
                SKU = $"SKU-{i:D3}",
                CodigoBarras = $"779000{i:D6}",
                PrecioCosto = 100m,
                PrecioVenta = 200m,
                StockActual = 5m,
                Rubro = i <= 70 ? "Librería" : "Regalería",
                Activo = true
            });
        }
        await context.SaveChangesAsync();

        // 1. Página 1 con tamaño 50
        var pag1 = await inventarioService.ObtenerArticulosPaginadosAsync(new ConsultaInventarioPaginadaDto
        {
            Pagina = 1,
            CantidadPorPagina = 50,
            Rubro = "Todos"
        });

        pag1.TotalArticulosGlobal.Should().Be(120);
        pag1.TotalRegistros.Should().Be(120);
        pag1.TotalPaginas.Should().Be(3);
        pag1.PaginaActual.Should().Be(1);
        pag1.Items.Should().HaveCount(50);
        pag1.RegistroDesde.Should().Be(1);
        pag1.RegistroHasta.Should().Be(50);
        pag1.ValorTotalStockGlobal.Should().Be(120 * 200m * 5m);

        // 2. Página 2
        var pag2 = await inventarioService.ObtenerArticulosPaginadosAsync(new ConsultaInventarioPaginadaDto
        {
            Pagina = 2,
            CantidadPorPagina = 50,
            Rubro = "Todos"
        });

        pag2.PaginaActual.Should().Be(2);
        pag2.Items.Should().HaveCount(50);
        pag2.RegistroDesde.Should().Be(51);
        pag2.RegistroHasta.Should().Be(100);

        // 3. Página 3 (restante de 20)
        var pag3 = await inventarioService.ObtenerArticulosPaginadosAsync(new ConsultaInventarioPaginadaDto
        {
            Pagina = 3,
            CantidadPorPagina = 50,
            Rubro = "Todos"
        });

        pag3.PaginaActual.Should().Be(3);
        pag3.Items.Should().HaveCount(20);
        pag3.RegistroDesde.Should().Be(101);
        pag3.RegistroHasta.Should().Be(120);

        // 4. Filtrado por Rubro "Regalería"
        var regaleria = await inventarioService.ObtenerArticulosPaginadosAsync(new ConsultaInventarioPaginadaDto
        {
            Pagina = 1,
            CantidadPorPagina = 50,
            Rubro = "Regalería"
        });

        regaleria.TotalArticulosGlobal.Should().Be(120);
        regaleria.TotalRegistros.Should().Be(50);
        regaleria.TotalPaginas.Should().Be(1);
        regaleria.Items.Should().HaveCount(50);
        regaleria.Items.All(a => a.Rubro == "Regalería").Should().BeTrue();
    }

    [Fact]
    public async Task InventarioService_ArticuloConVariantes_GuardaVariantesYSincronizaStock()
    {
        using var context = CrearContextoEnMemoria();
        var inventarioService = new InventarioService(context);

        var articuloDto = new ArticuloDto
        {
            Nombre = "Cuaderno Éxito E4 48h Rayado",
            SKU = "CUAD-EXITO-E4",
            CodigoBarras = "779123450000",
            PrecioCosto = 2000m,
            PrecioVenta = 3500m,
            Rubro = "Librería",
            Variantes = new List<ArticuloVarianteDto>
            {
                new() { Nombre = "Azul", CodigoBarras = "779123450001", StockActual = 10, StockMinimo = 2 },
                new() { Nombre = "Rojo", CodigoBarras = "779123450002", StockActual = 5, StockMinimo = 2 },
                new() { Nombre = "Verde", CodigoBarras = "779123450003", StockActual = 0, StockMinimo = 2 }
            }
        };

        var guardado = await inventarioService.GuardarArticuloAsync(articuloDto);

        guardado.Should().NotBeNull();
        guardado.TieneVariantes.Should().BeTrue();
        guardado.StockActual.Should().Be(15m); // 10 + 5 + 0
        guardado.Variantes.Should().HaveCount(3);

        // Búsqueda por código de barras de variante específica
        var encontradoAzul = await inventarioService.BuscarPorCodigoBarrasAsync("779123450001");
        encontradoAzul.Should().NotBeNull();
        encontradoAzul!.VarianteEscaneada.Should().NotBeNull();
        encontradoAzul.VarianteEscaneada!.Nombre.Should().Be("Azul");
        encontradoAzul.VarianteEscaneada.StockActual.Should().Be(10m);

        // Búsqueda por código de barras del producto principal
        var encontradoPrincipal = await inventarioService.BuscarPorCodigoBarrasAsync("779123450000");
        encontradoPrincipal.Should().NotBeNull();
        encontradoPrincipal!.VarianteEscaneada.Should().BeNull();
        encontradoPrincipal.TieneVariantes.Should().BeTrue();
    }

    [Fact]
    public async Task VentaService_VentaDeVariante_DescuentaStockDeVarianteYDelArticulo()
    {
        using var context = CrearContextoEnMemoria();
        var inventarioService = new InventarioService(context);
        var ventaService = new VentaService(context);

        var turno = new TurnoCaja { MontoInicialEfectivo = 1000m, UsuarioApertura = "Cajera Turno" };
        context.TurnosCaja.Add(turno);
        await context.SaveChangesAsync();

        var articuloDto = new ArticuloDto
        {
            Nombre = "Cartulina Escolar",
            SKU = "CART-ESC",
            CodigoBarras = "779999900000",
            PrecioCosto = 300m,
            PrecioVenta = 600m,
            Rubro = "Librería",
            Variantes = new List<ArticuloVarianteDto>
            {
                new() { Nombre = "Amarillo", CodigoBarras = "779999900001", StockActual = 20, StockMinimo = 5 },
                new() { Nombre = "Celeste", CodigoBarras = "779999900002", StockActual = 15, StockMinimo = 5 }
            }
        };

        var guardado = await inventarioService.GuardarArticuloAsync(articuloDto);
        var varianteAmarillo = guardado.Variantes.First(v => v.Nombre == "Amarillo");

        // Vender 3 unidades de Amarillo
        var ventaDto = new RegistrarVentaDto
        {
            TurnoCajaId = turno.Id,
            VendedoraNombre = "Cajera Turno",
            MetodoPago = "Efectivo",
            MontoEntregado = 1800m,
            Items = new List<ItemCarritoDto>
            {
                new()
                {
                    ArticuloId = guardado.Id,
                    ArticuloVarianteId = varianteAmarillo.Id,
                    VarianteNombre = varianteAmarillo.Nombre,
                    Cantidad = 3,
                    PrecioUnitario = 600m,
                    PrecioCosto = 300m,
                    Descripcion = $"{guardado.Nombre} (Amarillo)",
                    SKU = guardado.SKU
                }
            }
        };

        var ventaResultado = await ventaService.ProcesarVentaAsync(ventaDto);
        ventaResultado.Should().NotBeNull();
        ventaResultado.TotalCobrado.Should().Be(1800m);

        // Verificar stock de variante en BD: 20 - 3 = 17
        var varianteDb = await context.ArticuloVariantes.FindAsync(varianteAmarillo.Id);
        varianteDb.Should().NotBeNull();
        varianteDb!.StockActual.Should().Be(17m);

        // Verificar stock del artículo general en BD: 35 - 3 = 32
        var articuloDb = await context.Articulos.FindAsync(guardado.Id);
        articuloDb.Should().NotBeNull();
        articuloDb!.StockActual.Should().Be(32m);

        // Verificar kardex
        var kardex = await context.MovimientosStock.FirstOrDefaultAsync(m => m.ArticuloVarianteId == varianteAmarillo.Id);
        kardex.Should().NotBeNull();
        kardex!.Cantidad.Should().Be(-3m);
        kardex.StockPrevio.Should().Be(20m);
    }

    [Fact]
    public async Task InventarioService_AjustarStockRapido_AjustaVarianteYArticuloTotal()
    {
        using var context = CrearContextoEnMemoria();
        var inventarioService = new InventarioService(context);

        var articuloDto = new ArticuloDto
        {
            Nombre = "Témpera Alba 250ml",
            SKU = "TEMP-ALBA-250",
            CodigoBarras = "779888800000",
            PrecioCosto = 1500m,
            PrecioVenta = 2800m,
            Rubro = "Librería",
            Variantes = new List<ArticuloVarianteDto>
            {
                new() { Nombre = "Negro", CodigoBarras = "779888800001", StockActual = 4, StockMinimo = 2 },
                new() { Nombre = "Blanco", CodigoBarras = "779888800002", StockActual = 6, StockMinimo = 2 }
            }
        };

        var guardado = await inventarioService.GuardarArticuloAsync(articuloDto);
        var varianteBlanco = guardado.Variantes.First(v => v.Nombre == "Blanco");

        // Ajustar stock rápido en auditoría física de góndola: se cuentan 10 unidades de Blanco (tenía 6)
        var actualizado = await inventarioService.AjustarStockRapidoAsync(
            guardado.Id,
            10m,
            "Auditoría Góndola: Blanco",
            "Auditor",
            articuloVarianteId: varianteBlanco.Id);

        actualizado.Should().NotBeNull();
        actualizado.StockActual.Should().Be(14m); // 4 Negro + 10 Blanco

        var varianteBlancoActualizada = actualizado.Variantes.First(v => v.Id == varianteBlanco.Id);
        varianteBlancoActualizada.StockActual.Should().Be(10m);
    }
}
