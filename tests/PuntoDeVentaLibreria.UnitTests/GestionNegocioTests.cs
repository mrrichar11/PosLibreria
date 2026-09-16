using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PuntoDeVentaLibreria.Application.DTOs.Clientes;
using PuntoDeVentaLibreria.Application.DTOs.Configuracion;
using PuntoDeVentaLibreria.Domain.Entities.Finanzas;
using PuntoDeVentaLibreria.Infrastructure.Data;
using PuntoDeVentaLibreria.Infrastructure.Services;
using Xunit;

namespace PuntoDeVentaLibreria.UnitTests;

public class GestionNegocioTests
{
    private AppDbContext CrearContextoEnMemoria()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task ConfiguracionService_GuardarYObtener_MantieneDatosNegocio()
    {
        using var context = CrearContextoEnMemoria();
        var service = new ConfiguracionService(context);

        var dto = new ConfiguracionNegocioDto
        {
            NombreComercio = "Librería Central San Martín",
            Direccion = "Av. Libertador 1200",
            Telefono = "+54 11 9988-7766",
            Cuit = "20-11223344-5",
            PorcentajeDescuentoEfectivo = 15m,
            MargenGananciaSugerido = 70m,
            Pais = "Chile",
            SimboloMoneda = "$",
            BilletesHabilitados = "1000,2000,5000,10000,20000"
        };

        await service.GuardarConfiguracionAsync(dto);
        var guardado = await service.ObtenerConfiguracionAsync();

        guardado.NombreComercio.Should().Be("Librería Central San Martín");
        guardado.PorcentajeDescuentoEfectivo.Should().Be(15m);
        guardado.MargenGananciaSugerido.Should().Be(70m);
        guardado.Pais.Should().Be("Chile");
        guardado.BilletesHabilitados.Should().Be("1000,2000,5000,10000,20000");
    }

    [Fact]
    public async Task ClienteService_CobrarSaldoCuentaCorriente_DescuentaDeudaEImpactaCaja()
    {
        using var context = CrearContextoEnMemoria();
        var turno = new TurnoCaja { MontoInicialEfectivo = 10000m, UsuarioApertura = "cajero" };
        context.TurnosCaja.Add(turno);

        var service = new ClienteService(context);
        var nuevoCliente = await service.GuardarClienteAsync(new ClienteDto
        {
            NombreCompleto = "Colegio Belgrano - Primaria",
            ColegioOInstitucion = "Colegio Manuel Belgrano",
            PermiteFiado = true,
            LimiteCredito = 100000m
        });

        // Simular deuda de fiado por compra de útiles
        nuevoCliente.SaldoDeudorActual = 25000m;
        await context.SaveChangesAsync();

        // Registrar entrega de $15.000 en efectivo
        await service.CobrarSaldoCuentaCorrienteAsync(new RegistrarEntregaCuentaCorrienteDto
        {
            ClienteId = nuevoCliente.Id,
            TurnoCajaId = turno.Id,
            MontoEntrega = 15000m,
            MetodoPago = "Efectivo",
            Observaciones = "Pago a cuenta de cuadernos",
            UsuarioNombre = "cajero"
        });

        var clienteDb = await service.ObtenerPorIdAsync(nuevoCliente.Id);
        clienteDb!.SaldoDeudorActual.Should().Be(10000m);

        var movCaja = await context.MovimientosCaja.FirstOrDefaultAsync(m => m.TurnoCajaId == turno.Id);
        movCaja.Should().NotBeNull();
        movCaja!.Monto.Should().Be(15000m);
        movCaja.MetodoPago.Should().Be("Efectivo");
    }

    [Fact]
    public async Task ConfiguracionService_ObtenerEstadoRetirosDueñoMes_CalculaTotalCorrectamente()
    {
        using var context = CrearContextoEnMemoria();
        var service = new ConfiguracionService(context);

        var turno = new TurnoCaja { MontoInicialEfectivo = 10000m, UsuarioApertura = "cajero" };
        context.TurnosCaja.Add(turno);

        // Agregar 2 retiros de dueño en el mes corriente
        context.MovimientosCaja.Add(new MovimientoCaja
        {
            TurnoCajaId = turno.Id,
            Tipo = TipoMovimientoCaja.RetiroDueño,
            Monto = 5000m,
            Concepto = "Retiro parcial de ganancias",
            UsuarioNombre = "admin",
            MetodoPago = "Efectivo"
        });

        context.MovimientosCaja.Add(new MovimientoCaja
        {
            TurnoCajaId = turno.Id,
            Tipo = TipoMovimientoCaja.RetiroDueño,
            Monto = 7500m,
            Concepto = "Retiro semanal",
            UsuarioNombre = "admin",
            MetodoPago = "Efectivo"
        });

        await context.SaveChangesAsync();

        var estado = await service.ObtenerEstadoRetirosDueñoMesAsync();
        estado.TotalRetiradoMes.Should().Be(12500m);
    }

    [Fact]
    public async Task TicketPrinterService_GenerarTicket80mm_ContieneDatosComercioYLineasVenta()
    {
        var printer = new TicketPrinterService();
        var venta = new PuntoDeVentaLibreria.Application.DTOs.Ventas.VentaRealizadaDto
        {
            NumeroComprobante = "L-0001-00000123",
            Fecha = new DateTime(2026, 9, 16, 11, 30, 0),
            SubtotalBruto = 5000m,
            TotalCobrado = 5000m,
            MontoEntregado = 10000m,
            Vuelto = 5000m,
            MetodoPago = "Efectivo",
            ClienteNombre = "Juan Pérez",
            VendedoraNombre = "Laura",
            Lineas = new List<PuntoDeVentaLibreria.Application.DTOs.Ventas.ItemCarritoDto>
            {
                new() { Descripcion = "Cuaderno Rivadavia Tapa Dura", Cantidad = 2, PrecioUnitario = 2500m }
            }
        };

        var config = new PuntoDeVentaLibreria.Application.DTOs.Peripherals.ConfiguracionTicketDto
        {
            NombreComercio = "MR SYS Librería",
            Cuit = "20-33445566-7",
            Direccion = "San Martín 123",
            Telefono = "11-4455-6677",
            AnchoPapelMm = 80,
            MensajePie = "Gracias por su compra."
        };

        var texto = await printer.GenerarTicketTextoAsync(venta, config);

        texto.Should().Contain("MR SYS LIBRERÍA");
        texto.Should().Contain("CUIT: 20-33445566-7");
        texto.Should().Contain("TICKET N°: L-0001-00000123");
        texto.Should().Contain("CAJERO/A:  Laura");
        texto.Should().Contain("CLIENTE:   Juan Pérez");
        texto.Should().Contain("Cuaderno Rivadavia Tap");
        texto.Should().Contain("TOTAL COBRADO:");
        texto.Should().Contain("Dinero Recibido:");
        texto.Should().Contain("SU VUELTO:");
        texto.Should().Contain("Gracias por su compra.");
    }

    [Fact]
    public async Task TicketPrinterService_GenerarTicket58mm_GeneraFormatoCompacto()
    {
        var printer = new TicketPrinterService();
        var venta = new PuntoDeVentaLibreria.Application.DTOs.Ventas.VentaRealizadaDto
        {
            NumeroComprobante = "L-0001-00000124",
            Fecha = new DateTime(2026, 9, 16, 11, 35, 0),
            SubtotalBruto = 1200m,
            TotalCobrado = 1200m,
            MetodoPago = "Debito",
            ReferenciaPago = "Operación #9876",
            ClienteNombre = "Consumidor Final",
            Lineas = new List<PuntoDeVentaLibreria.Application.DTOs.Ventas.ItemCarritoDto>
            {
                new() { Descripcion = "Regla 30cm Maped", Cantidad = 1, PrecioUnitario = 1200m }
            }
        };

        var config = new PuntoDeVentaLibreria.Application.DTOs.Peripherals.ConfiguracionTicketDto
        {
            NombreComercio = "MR SYS Librería",
            AnchoPapelMm = 58
        };

        var texto = await printer.GenerarTicketTextoAsync(venta, config);

        texto.Should().Contain("MR SYS LIBRERÍA");
        texto.Should().Contain("TARJETA DE DÉBITO");
        texto.Should().Contain("Ref/Comprobante: Operación #9876");
    }
}
