using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PuntoDeVentaLibreria.Application.Common;
using PuntoDeVentaLibreria.Application.DTOs.Configuracion;
using PuntoDeVentaLibreria.Domain.Entities.Catalogo;
using PuntoDeVentaLibreria.Domain.Entities.Configuracion;
using PuntoDeVentaLibreria.Infrastructure.Data;
using PuntoDeVentaLibreria.Infrastructure.Services;
using Xunit;

namespace PuntoDeVentaLibreria.UnitTests;

public class CotizadorDolarYRedondeoTests
{
    private AppDbContext CrearContextoEnMemoria()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public void RedondeoPrecio_MenorA100_RedondeaADecenaMasCercana_YNuncaQuedaEnCero()
    {
        // Artículos chicos como hojas de carpeta sueltas, clips, fotocopias
        decimal precio1 = 45.60m;
        decimal redondeado1 = CalculoPreciosUtils.RedondearPrecioVenta(precio1, ReglaRedondeoPrecio.CentenaCercana);
        redondeado1.Should().Be(50m);

        decimal precio2 = 23.10m;
        decimal redondeado2 = CalculoPreciosUtils.RedondearPrecioVenta(precio2, ReglaRedondeoPrecio.CentenaCercana);
        redondeado2.Should().Be(20m);

        decimal precio3 = 4.50m;
        decimal redondeado3 = CalculoPreciosUtils.RedondearPrecioVenta(precio3, ReglaRedondeoPrecio.CentenaCercana);
        redondeado3.Should().Be(10m, "porque un artículo nunca debe quedar en $0 y el mínimo es $10");

        decimal precio4 = 94.20m;
        decimal redondeado4 = CalculoPreciosUtils.RedondearPrecioVenta(precio4, ReglaRedondeoPrecio.CentenaCercana);
        redondeado4.Should().Be(90m);
    }

    [Fact]
    public void RedondeoPrecio_MayorA100_RedondeaACentenaMasCercana()
    {
        decimal precio1 = 1230m;
        decimal redondeado1 = CalculoPreciosUtils.RedondearPrecioVenta(precio1, ReglaRedondeoPrecio.CentenaCercana);
        redondeado1.Should().Be(1200m);

        decimal precio2 = 1260m;
        decimal redondeado2 = CalculoPreciosUtils.RedondearPrecioVenta(precio2, ReglaRedondeoPrecio.CentenaCercana);
        redondeado2.Should().Be(1300m);

        decimal precio3 = 980m;
        decimal redondeado3 = CalculoPreciosUtils.RedondearPrecioVenta(precio3, ReglaRedondeoPrecio.CentenaCercana);
        redondeado3.Should().Be(1000m);
    }

    [Fact]
    public void ArticuloJoyeria_CalculoDolarADinamicoPesos_CalculaCostoYPrecioVentaCorrecto()
    {
        // Joyería o importado con costo en dólares (ej. 2.30 USD)
        decimal costoDolar = 2.30m;
        decimal margen = 60.0m;
        decimal iva = 21.0m;

        // Cotización 1: $1.350
        decimal cotizacion1 = 1350m;
        decimal costoArs1 = Math.Round(costoDolar * cotizacion1, 2); // $3.105,00
        decimal ventaSinRedondeo1 = CalculoPreciosUtils.CalcularPrecioVenta(costoArs1, margen, iva); // 3105 * 1.60 * 1.21 = 6011.28
        decimal ventaFinal1 = CalculoPreciosUtils.RedondearPrecioVenta(ventaSinRedondeo1, ReglaRedondeoPrecio.CentenaCercana);

        costoArs1.Should().Be(3105.00m);
        ventaFinal1.Should().Be(6000.00m);

        // Cotización 2: El dólar sube a $1.400
        decimal cotizacion2 = 1400m;
        decimal costoArs2 = Math.Round(costoDolar * cotizacion2, 2); // $3.220,00
        decimal ventaSinRedondeo2 = CalculoPreciosUtils.CalcularPrecioVenta(costoArs2, margen, iva); // 3220 * 1.60 * 1.21 = 6233.92
        decimal ventaFinal2 = CalculoPreciosUtils.RedondearPrecioVenta(ventaSinRedondeo2, ReglaRedondeoPrecio.CentenaCercana);

        costoArs2.Should().Be(3220.00m);
        ventaFinal2.Should().Be(6200.00m);
    }

    [Fact]
    public async Task ConfiguracionService_ActualizarCotizacionDolar_PersisteYDisparaEvento()
    {
        using var context = CrearContextoEnMemoria();
        var service = new ConfiguracionService(context);

        decimal eventoValorRecibido = 0;
        ConfiguracionService.CotizacionDolarCambiada += val =>
        {
            eventoValorRecibido = val;
        };

        await service.ActualizarCotizacionDolarAsync(1420.50m);

        var config = await service.ObtenerConfiguracionAsync();
        config.CotizacionDolar.Should().Be(1420.50m);
        config.FechaCotizacionDolar.Should().NotBeNull();
        eventoValorRecibido.Should().Be(1420.50m);
    }
}
