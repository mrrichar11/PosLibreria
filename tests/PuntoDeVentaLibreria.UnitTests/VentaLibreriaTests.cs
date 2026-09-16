using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PuntoDeVentaLibreria.Application.DTOs.Ventas;
using PuntoDeVentaLibreria.Domain.Entities.Catalogo;
using PuntoDeVentaLibreria.Domain.Entities.Combos;
using PuntoDeVentaLibreria.Domain.Entities.Finanzas;
using PuntoDeVentaLibreria.Infrastructure.Data;
using PuntoDeVentaLibreria.Infrastructure.Services;
using Xunit;

namespace PuntoDeVentaLibreria.UnitTests;

public class VentaLibreriaTests
{
    private AppDbContext CrearContextoEnMemoria()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task ProcesarVenta_ItemEstandar_DescuentaStockEImpactaCaja()
    {
        using var context = CrearContextoEnMemoria();
        var turno = new TurnoCaja { MontoInicialEfectivo = 10000m, UsuarioApertura = "admin" };
        context.TurnosCaja.Add(turno);

        var cuaderno = new Articulo
        {
            Nombre = "Cuaderno Rivadavia",
            SKU = "CUAD-01",
            PrecioCosto = 2000m,
            PrecioVenta = 3200m,
            StockActual = 50,
            Tipo = TipoArticulo.Estandar
        };
        context.Articulos.Add(cuaderno);
        await context.SaveChangesAsync();

        var service = new VentaService(context);
        var dto = new RegistrarVentaDto
        {
            TurnoCajaId = turno.Id,
            VendedoraNombre = "Cajero",
            MetodoPago = "Efectivo",
            Items = new List<ItemCarritoDto>
            {
                new()
                {
                    ArticuloId = cuaderno.Id,
                    Descripcion = cuaderno.Nombre,
                    SKU = cuaderno.SKU,
                    Cantidad = 2,
                    PrecioUnitario = cuaderno.PrecioVenta,
                    PrecioCosto = cuaderno.PrecioCosto
                }
            }
        };

        var resultado = await service.ProcesarVentaAsync(dto);

        resultado.Should().NotBeNull();
        resultado.TotalCobrado.Should().Be(6400m);

        var cuadernoDb = await context.Articulos.FindAsync(cuaderno.Id);
        cuadernoDb!.StockActual.Should().Be(48);

        var movimientoCaja = await context.MovimientosCaja.FirstOrDefaultAsync(m => m.TurnoCajaId == turno.Id);
        movimientoCaja.Should().NotBeNull();
        movimientoCaja!.Monto.Should().Be(6400m);
    }

    [Fact]
    public async Task ProcesarVenta_ComboEscolar_DescuentaComponentesIndividuales()
    {
        using var context = CrearContextoEnMemoria();
        var turno = new TurnoCaja { MontoInicialEfectivo = 5000m, UsuarioApertura = "admin" };
        context.TurnosCaja.Add(turno);

        var cuaderno = new Articulo { Nombre = "Cuaderno", SKU = "CUA", StockActual = 20, PrecioCosto = 1000m, PrecioVenta = 1500m };
        var lapiz = new Articulo { Nombre = "Lápiz", SKU = "LAP", StockActual = 30, PrecioCosto = 200m, PrecioVenta = 400m };
        context.Articulos.AddRange(cuaderno, lapiz);
        await context.SaveChangesAsync();

        var combo = new Articulo
        {
            Nombre = "Kit Escolar Básico (1 Cuaderno + 2 Lápices)",
            SKU = "KIT-01",
            Tipo = TipoArticulo.ComboKit,
            PrecioCosto = 1400m,
            PrecioVenta = 2100m,
            StockActual = 0
        };
        context.Articulos.Add(combo);
        await context.SaveChangesAsync();

        context.ComboItems.AddRange(
            new ComboItem { ComboArticuloId = combo.Id, ComponenteArticuloId = cuaderno.Id, Cantidad = 1 },
            new ComboItem { ComboArticuloId = combo.Id, ComponenteArticuloId = lapiz.Id, Cantidad = 2 }
        );
        await context.SaveChangesAsync();

        var service = new VentaService(context);
        var dto = new RegistrarVentaDto
        {
            TurnoCajaId = turno.Id,
            VendedoraNombre = "Cajero",
            MetodoPago = "Efectivo",
            Items = new List<ItemCarritoDto>
            {
                new()
                {
                    ArticuloId = combo.Id,
                    Descripcion = combo.Nombre,
                    SKU = combo.SKU,
                    Cantidad = 1,
                    PrecioUnitario = combo.PrecioVenta,
                    PrecioCosto = combo.PrecioCosto,
                    EsCombo = true
                }
            }
        };

        var resultado = await service.ProcesarVentaAsync(dto);

        resultado.TotalCobrado.Should().Be(2100m);

        var cuadernoDb = await context.Articulos.FindAsync(cuaderno.Id);
        cuadernoDb!.StockActual.Should().Be(19);

        var lapizDb = await context.Articulos.FindAsync(lapiz.Id);
        lapizDb!.StockActual.Should().Be(28);

        var movs = await context.MovimientosStock.Where(m => m.ArticuloId == cuaderno.Id || m.ArticuloId == lapiz.Id).ToListAsync();
        movs.Should().HaveCount(2);
    }

    [Fact]
    public async Task ProcesarVenta_ServicioFotocopia_NoDescuentaStock()
    {
        using var context = CrearContextoEnMemoria();
        var turno = new TurnoCaja { MontoInicialEfectivo = 5000m, UsuarioApertura = "admin" };
        context.TurnosCaja.Add(turno);

        var fotocopia = new Articulo
        {
            Nombre = "Fotocopia B/N",
            SKU = "FOTO",
            Tipo = TipoArticulo.Servicio,
            StockActual = 9999,
            PrecioVenta = 80m
        };
        context.Articulos.Add(fotocopia);
        await context.SaveChangesAsync();

        var service = new VentaService(context);
        var dto = new RegistrarVentaDto
        {
            TurnoCajaId = turno.Id,
            Items = new List<ItemCarritoDto>
            {
                new()
                {
                    ArticuloId = fotocopia.Id,
                    Descripcion = fotocopia.Nombre,
                    SKU = fotocopia.SKU,
                    Cantidad = 15,
                    PrecioUnitario = fotocopia.PrecioVenta
                }
            }
        };

        var resultado = await service.ProcesarVentaAsync(dto);

        resultado.TotalCobrado.Should().Be(1200m);
        var fotocopiaDb = await context.Articulos.FindAsync(fotocopia.Id);
        fotocopiaDb!.StockActual.Should().Be(9999);
    }

    [Fact]
    public async Task ProcesarVenta_CuentaCorrienteFiado_IncrementaSaldoDeudorCliente()
    {
        using var context = CrearContextoEnMemoria();
        var turno = new TurnoCaja { MontoInicialEfectivo = 5000m, UsuarioApertura = "admin" };
        context.TurnosCaja.Add(turno);

        var cliente = new PuntoDeVentaLibreria.Domain.Entities.Clientes.Cliente
        {
            NombreCompleto = "Colegio Sarmiento",
            PermiteFiado = true,
            LimiteCredito = 50000m,
            SaldoDeudorActual = 1000m
        };
        context.Clientes.Add(cliente);
        await context.SaveChangesAsync();

        var service = new VentaService(context);
        var dto = new RegistrarVentaDto
        {
            TurnoCajaId = turno.Id,
            ClienteId = cliente.Id,
            ClienteNombre = cliente.NombreCompleto,
            MetodoPago = "CtaCte",
            Items = new List<ItemCarritoDto>
            {
                new()
                {
                    ArticuloId = null,
                    Descripcion = "Resma A4 Autor",
                    Cantidad = 2,
                    PrecioUnitario = 4500m,
                    EsVentaManual = true
                }
            }
        };

        var resultado = await service.ProcesarVentaAsync(dto);

        resultado.TotalCobrado.Should().Be(9000m);
        resultado.ClienteNombre.Should().Be("Colegio Sarmiento");

        var clienteDb = await context.Clientes.FindAsync(cliente.Id);
        clienteDb!.SaldoDeudorActual.Should().Be(10000m); // 1000 anterior + 9000 de la compra fiada
    }
}
