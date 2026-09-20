using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PuntoDeVentaLibreria.Domain.Entities.Catalogo;
using PuntoDeVentaLibreria.Domain.Entities.Finanzas;
using PuntoDeVentaLibreria.Domain.Entities.Seguridad;
using PuntoDeVentaLibreria.Domain.Entities.Ventas;
using PuntoDeVentaLibreria.Infrastructure.Data;
using PuntoDeVentaLibreria.Infrastructure.Services;
using Xunit;

namespace PuntoDeVentaLibreria.UnitTests;

public class SeguridadYReportesTests
{
    private AppDbContext CrearContextoEnMemoria()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        return context;
    }

    [Fact]
    public async Task AuthService_Autenticar_ConUsuarioYPasswordValidos_RetornaSesion()
    {
        using var context = CrearContextoEnMemoria();
        await DataSeeder.SeedAsync(context);

        var authService = new AuthService(context);

        var usuarios = await authService.ObtenerUsuariosActivosAsync();
        usuarios.Should().NotBeEmpty();

        var sesionAdmin = await authService.IniciarSesionAsync("admin", "admin");
        sesionAdmin.Should().NotBeNull();
        sesionAdmin!.Username.Should().Be("admin");
        sesionAdmin.Rol.Should().Be(RolUsuario.Administrador);

        var sesionCajero = await authService.IniciarSesionAsync("cajero", "1234");
        sesionCajero.Should().NotBeNull();
        sesionCajero!.Username.Should().Be("cajero");
        sesionCajero.Rol.Should().Be(RolUsuario.Vendedor);
    }

    [Fact]
    public async Task AuthService_Autenticar_ConPasswordInvalida_RetornaNull()
    {
        using var context = CrearContextoEnMemoria();
        await DataSeeder.SeedAsync(context);

        var authService = new AuthService(context);

        var sesionInvalida = await authService.IniciarSesionAsync("admin", "clave_erronea_999");
        sesionInvalida.Should().BeNull();

        var sesionFallida = await authService.IniciarSesionAsync("usuario_fantasma", "1234");
        sesionFallida.Should().BeNull();
    }

    [Fact]
    public async Task ReporteService_ObtenerMetricasDashboardAsync_CalculaFacturacionYTopArticulos()
    {
        using var context = CrearContextoEnMemoria();
        var reporteService = new ReporteService(context);

        var turno = new TurnoCaja { MontoInicialEfectivo = 10000m, UsuarioApertura = "cajero" };
        context.TurnosCaja.Add(turno);

        var articulo1 = new Articulo
        {
            Nombre = "Cuaderno Rivadavia Tapa Dura",
            SKU = "CUAD-RIV",
            CodigoBarras = "7791234567890",
            PrecioVenta = 5000m,
            PrecioCosto = 3000m,
            StockActual = 50,
            Tipo = TipoArticulo.Estandar
        };
        var articulo2 = new Articulo
        {
            Nombre = "Fotocopia A4 Simple Faz",
            SKU = "FOTO-A4",
            CodigoBarras = "FOTO01",
            PrecioVenta = 100m,
            PrecioCosto = 20m,
            Tipo = TipoArticulo.Servicio,
            StockActual = 0
        };

        context.Articulos.AddRange(articulo1, articulo2);
        await context.SaveChangesAsync();

        var venta = new Venta
        {
            NumeroComprobante = "B-0001-00000001",
            TurnoCajaId = turno.Id,
            SubtotalBruto = 10200m,
            TotalVenta = 10200m,
            TotalCostoHistorico = 6040m,
            VendedoraNombre = "cajero",
            MetodoPagoPrincipal = "Transferencia",
            FechaVenta = DateTime.Today.AddHours(12)
        };

        venta.LineasVenta.Add(new LineaVenta
        {
            ArticuloId = articulo1.Id,
            Descripcion = articulo1.Nombre,
            SKU = articulo1.SKU,
            PrecioUnitarioVenta = 5000m,
            PrecioCostoHistorico = 3000m,
            Cantidad = 2
        });

        venta.LineasVenta.Add(new LineaVenta
        {
            ArticuloId = articulo2.Id,
            Descripcion = articulo2.Nombre,
            SKU = articulo2.SKU,
            PrecioUnitarioVenta = 100m,
            PrecioCostoHistorico = 20m,
            Cantidad = 2
        });

        venta.Pagos.Add(new PagoVenta
        {
            MetodoPago = "Transferencia",
            Monto = 10200m,
            ReferenciaOperacion = "Comprobante #98124"
        });

        context.Ventas.Add(venta);
        await context.SaveChangesAsync();

        var hoy = DateTime.Today;
        var dto = await reporteService.ObtenerMetricasDashboardAsync(hoy, hoy.AddDays(1).AddTicks(-1), "Hoy");

        dto.Should().NotBeNull();
        dto.FacturacionTotal.Should().Be(10200m);
        dto.CantidadVentas.Should().Be(1);
        dto.TicketPromedio.Should().Be(10200m);
        dto.CostoTotalEstimado.Should().Be(6040m);
        dto.GananciaNetaEstimada.Should().Be(4160m);

        dto.VentasPorMedioPago.Should().HaveCount(1);
        dto.VentasPorMedioPago.First().MedioPago.Should().Be("📱 Transferencia / QR");
        dto.VentasPorMedioPago.First().Total.Should().Be(10200m);

        dto.TopArticulos.Should().HaveCount(2);
        dto.TopArticulos.First().Descripcion.Should().Be("Cuaderno Rivadavia Tapa Dura");
        dto.TopArticulos.First().CantidadVendida.Should().Be(2);
    }
}
