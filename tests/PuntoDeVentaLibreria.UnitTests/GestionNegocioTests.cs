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
            MargenGananciaSugerido = 70m
        };

        await service.GuardarConfiguracionAsync(dto);
        var guardado = await service.ObtenerConfiguracionAsync();

        guardado.NombreComercio.Should().Be("Librería Central San Martín");
        guardado.PorcentajeDescuentoEfectivo.Should().Be(15m);
        guardado.MargenGananciaSugerido.Should().Be(70m);
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
}
