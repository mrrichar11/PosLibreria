using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PuntoDeVentaLibreria.Application.DTOs.Caja;
using PuntoDeVentaLibreria.Application.DTOs.Clientes;
using PuntoDeVentaLibreria.Application.DTOs.Configuracion;
using PuntoDeVentaLibreria.Application.DTOs.Inventario;
using PuntoDeVentaLibreria.Application.DTOs.Ventas;
using PuntoDeVentaLibreria.Domain.Entities.Clientes;
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

    [Fact]
    public async Task TicketPrinterService_GenerarTicketCierreCaja_MuestraDesgloseYDiferencia()
    {
        var printer = new TicketPrinterService();
        var resumen = new ResumenCierreTurnoDto
        {
            TurnoId = Guid.NewGuid(),
            FechaApertura = new DateTime(2026, 9, 16, 8, 0, 0),
            FechaCierre = new DateTime(2026, 9, 16, 20, 0, 0),
            UsuarioApertura = "Laura",
            UsuarioCierre = "Laura",
            FondoInicial = 10000m,
            VentasEfectivo = 35000m,
            CobrosCtaCteEfectivo = 5000m,
            VentasDebito = 12000m,
            VentasCredito = 8000m,
            VentasTransferencia = 15000m,
            VentasCtaCte = 4000m,
            GastosOperativos = 2000m,
            RetirosDueño = 5000m,
            EfectivoRealContado = 43000m,
            CantidadOperaciones = 42,
            Observaciones = "Cierre sin novedades"
        };

        var config = new PuntoDeVentaLibreria.Application.DTOs.Peripherals.ConfiguracionTicketDto
        {
            NombreComercio = "MR SYS Librería",
            AnchoPapelMm = 80
        };

        var ticket = await printer.GenerarTicketCierreCajaAsync(resumen, config);

        ticket.Should().Contain("CIERRE Y ARQUEO DE CAJA");
        ticket.Should().Contain("Ventas en Efectivo:");
        ticket.Should().Contain("Cobros Fiado Efectivo:");
        ticket.Should().Contain("TOTAL FACTURADO:");
        ticket.Should().Contain("EFECTIVO ESPERADO:");
        ticket.Should().Contain("EFECTIVO REAL DECLARADO:");
        ticket.Should().Contain("EXACTO ($0.00)");
        ticket.Should().Contain("Cierre sin novedades");
    }

    [Fact]
    public async Task TicketPrinterService_GenerarTicketReciboCtaCte_MuestraAbonoYSaldoRestante()
    {
        var printer = new TicketPrinterService();
        var recibo = new ReciboCobroCtaCteDto
        {
            NumeroRecibo = "REC-20260916-4521",
            Fecha = new DateTime(2026, 9, 16, 16, 45, 0),
            ClienteNombre = "Escuela Normal N° 1",
            ClienteDni = "30-55443322-9",
            SaldoAnterior = 50000m,
            MontoAbonado = 20000m,
            SaldoRestante = 30000m,
            MetodoPago = "Efectivo",
            Cajero = "Laura",
            Observaciones = "Pago cuota mensual útiles"
        };

        var config = new PuntoDeVentaLibreria.Application.DTOs.Peripherals.ConfiguracionTicketDto
        {
            NombreComercio = "MR SYS Librería",
            Direccion = "Av. San Martín 450",
            Cuit = "20-12345678-9",
            Telefono = "11-4567-8900",
            AnchoPapelMm = 80
        };

        var ticket = await printer.GenerarTicketReciboCtaCteAsync(recibo, config);

        ticket.Should().Contain("RECIBO DE COBRO - CTA. CTE.");
        ticket.Should().Contain("RECIBO:  REC-20260916-4521");
        ticket.Should().Contain("CLIENTE: Escuela Normal N° 1");
        ticket.Should().Contain("Saldo Anterior:");
        ticket.Should().Contain("50");
        ticket.Should().Contain("(-) MONTO ABONADO:");
        ticket.Should().Contain("20");
        ticket.Should().Contain("SALDO PENDIENTE:");
        ticket.Should().Contain("30");
        ticket.Should().Contain("Pago cuota mensual útiles");
    }

    [Fact]
    public async Task InventarioService_ExportarEImportarCsv_ExportaArticulosCorrectamente()
    {
        using var context = CrearContextoEnMemoria();
        var inventarioService = new InventarioService(context);

        await inventarioService.GuardarArticuloAsync(new ArticuloDto
        {
            CodigoBarras = "7791234567890",
            Nombre = "Cuaderno Espiral 84 Hojas",
            CategoriaNombre = "Cuadernos",
            PrecioCosto = 1000m,
            PrecioVenta = 1800m,
            StockActual = 50,
            StockMinimo = 10
        });

        var csv = await inventarioService.ExportarCatalogoCsvAsync();

        csv.Should().Contain("CodigoBarras;SKU;Nombre;Categoria;Marca;Tipo;PrecioCosto;PorcentajeGanancia;PrecioVenta;StockActual;StockMinimo;Ubicacion");
        csv.Should().Contain("7791234567890;;Cuaderno Espiral 84 Hojas;;;0;1000;0;1800;50;10;");
    }

    [Fact]
    public async Task ClienteService_ObtenerHistorialCliente_DevuelveMovimientosCorrectos()
    {
        using var context = CrearContextoEnMemoria();
        var clienteService = new ClienteService(context);

        var cliente = await clienteService.GuardarClienteAsync(new ClienteDto
        {
            NombreCompleto = "Profesor Carlos Mendoza",
            PermiteFiado = true,
            LimiteCredito = 80000m
        });

        var turno = new TurnoCaja { MontoInicialEfectivo = 5000m, UsuarioApertura = "cajero" };
        context.TurnosCaja.Add(turno);
        await context.SaveChangesAsync();

        cliente.SaldoDeudorActual = 12000m;
        await context.SaveChangesAsync();

        // Realizar un abono
        var recibo = await clienteService.CobrarSaldoCuentaCorrienteAsync(new RegistrarEntregaCuentaCorrienteDto
        {
            ClienteId = cliente.Id,
            TurnoCajaId = turno.Id,
            MontoEntrega = 5000m,
            MetodoPago = "Efectivo",
            Observaciones = "Abono inicial",
            UsuarioNombre = "Laura"
        });

        recibo.SaldoAnterior.Should().Be(12000m);
        recibo.MontoAbonado.Should().Be(5000m);
        recibo.SaldoRestante.Should().Be(7000m);
        recibo.NumeroRecibo.Should().StartWith("REC-");

        var historial = await clienteService.ObtenerHistorialClienteAsync(cliente.Id);
        historial.Should().HaveCount(1);
        historial[0].Tipo.Should().Contain("Abono");
        historial[0].Monto.Should().Be(5000m);
        historial[0].EsAbono.Should().BeTrue();
    }

    [Fact]
    public async Task ReporteService_FiltroRangoPersonalizado_CalculaMetricasCorrectas()
    {
        using var context = CrearContextoEnMemoria();
        var reporteService = new ReporteService(context);

        var turno = new TurnoCaja { MontoInicialEfectivo = 1000m, UsuarioApertura = "cajero" };
        context.TurnosCaja.Add(turno);

        var fechaVenta = new DateTime(2026, 9, 10, 14, 0, 0, DateTimeKind.Utc);
        var venta = new PuntoDeVentaLibreria.Domain.Entities.Ventas.Venta
        {
            NumeroComprobante = "VTA-001",
            FechaVenta = fechaVenta,
            TurnoCajaId = turno.Id,
            VendedoraNombre = "Laura",
            TotalVenta = 5000m,
            TotalCostoHistorico = 2000m,
            MetodoPagoPrincipal = "Efectivo"
        };
        venta.Pagos.Add(new PuntoDeVentaLibreria.Domain.Entities.Ventas.PagoVenta
        {
            VentaId = venta.Id,
            MetodoPago = "Efectivo",
            Monto = 5000m
        });
        venta.LineasVenta.Add(new PuntoDeVentaLibreria.Domain.Entities.Ventas.LineaVenta
        {
            VentaId = venta.Id,
            Descripcion = "Kit Lapiceras Parker",
            Cantidad = 2,
            PrecioUnitarioVenta = 2500m
        });

        context.Ventas.Add(venta);
        await context.SaveChangesAsync();

        // Consultar con filtro personalizado que incluye la venta
        var desde = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var hasta = new DateTime(2026, 9, 15, 23, 59, 59, DateTimeKind.Utc);

        var reporte = await reporteService.ObtenerMetricasDashboardAsync(desde, hasta, "Rango Personalizado");

        reporte.FacturacionTotal.Should().Be(5000m);
        reporte.CostoTotalEstimado.Should().Be(2000m);
        reporte.GananciaNetaEstimada.Should().Be(3000m);
        reporte.CantidadVentas.Should().Be(1);
        reporte.CantidadArticulosVendidos.Should().Be(2);
        reporte.TopArticulos.Should().HaveCount(1);
        reporte.TopArticulos[0].Descripcion.Should().Be("Kit Lapiceras Parker");
    }

    [Fact]
    public async Task VentaService_VentaFiadaConEntregaInicial_RegistraIngresoYSaldoDeudorCorrectamente()
    {
        using var context = CrearContextoEnMemoria();
        var ventaService = new VentaService(context);
        var printer = new TicketPrinterService();

        var turno = new TurnoCaja { MontoInicialEfectivo = 10000m, UsuarioApertura = "cajero" };
        context.TurnosCaja.Add(turno);

        var cliente = new Cliente
        {
            NombreCompleto = "Colegio San Martín - Primaria",
            PermiteFiado = true,
            LimiteCredito = 100000m,
            SaldoDeudorActual = 0m
        };
        context.Clientes.Add(cliente);
        await context.SaveChangesAsync();

        var ventaDto = new RegistrarVentaDto
        {
            TurnoCajaId = turno.Id,
            ClienteId = cliente.Id,
            ClienteNombre = cliente.NombreCompleto,
            VendedoraNombre = "Laura",
            MetodoPago = "CtaCte",
            TieneEntregaInicial = true,
            MontoEntregaInicial = 20000m,
            MetodoPagoEntrega = "Transferencia",
            ReferenciaEntrega = "Alias colegio.pagos",
            Items = new List<ItemCarritoDto>
            {
                new() { Descripcion = "Resma Hojas A4 x500", Cantidad = 10, PrecioUnitario = 5000m, PrecioCosto = 3000m, EsVentaManual = true }
            }
        };

        var resultado = await ventaService.ProcesarVentaAsync(ventaDto);

        // 1. Verificación de montos de la venta
        resultado.TotalCobrado.Should().Be(50000m);
        resultado.TieneEntregaInicial.Should().BeTrue();
        resultado.MontoEntregaInicial.Should().Be(20000m);
        resultado.MetodoPagoEntrega.Should().Be("Transferencia");
        resultado.MontoFiado.Should().Be(30000m);

        // 2. Verificación de que al cliente solo se le sumó el saldo restante fiado ($30.000, NO $50.000)
        var clienteEnDb = await context.Clientes.FindAsync(cliente.Id);
        clienteEnDb!.SaldoDeudorActual.Should().Be(30000m);

        // 3. Verificación de movimientos de caja (el abono de $20.000 en transferencia ingresa como IngresoVenta)
        var movEntrega = await context.MovimientosCaja.FirstOrDefaultAsync(m => m.MetodoPago == "Transferencia");
        movEntrega.Should().NotBeNull();
        movEntrega!.Monto.Should().Be(20000m);
        movEntrega.Tipo.Should().Be(TipoMovimientoCaja.IngresoVenta);
        movEntrega.Concepto.Should().Contain("Entrega inicial");

        // 4. Verificación de que el ticket térmico desglosa el pago mixto
        var configTicket = new PuntoDeVentaLibreria.Application.DTOs.Peripherals.ConfiguracionTicketDto
        {
            NombreComercio = "MR SYS Librería",
            AnchoPapelMm = 80
        };

        var ticketTexto = await printer.GenerarTicketTextoAsync(resultado, configTicket);
        ticketTexto.Should().Contain("FORMA DE PAGO: MIXTO / FIADO CON ENTREGA");
        ticketTexto.Should().Contain("Entrega");
        ticketTexto.Should().Contain("20");
        ticketTexto.Should().Contain("Saldo a Cta. Cte. (Fiado):");
        ticketTexto.Should().Contain("30");
    }

    [Fact]
    public async Task InventarioService_GenerarSkuYCodigoBarrasSugerido_GeneraValoresUnicosYValidos()
    {
        using var context = CrearContextoEnMemoria();
        var service = new InventarioService(context);

        var sku = await service.GenerarSkuSugeridoAsync();
        sku.Should().NotBeNullOrWhiteSpace();
        sku.Should().StartWith("ART-");

        var codigoBarras = await service.GenerarCodigoBarrasSugeridoAsync();
        codigoBarras.Should().NotBeNullOrWhiteSpace();
        codigoBarras.Length.Should().Be(13); // EAN-13 standard
        codigoBarras.Should().StartWith("20");

        // Validar dígito verificador EAN-13
        int suma = 0;
        for (int i = 0; i < 12; i++)
        {
            int d = codigoBarras[i] - '0';
            suma += (i % 2 == 0) ? d : d * 3;
        }
        int checkDigitEsperado = (suma % 10 == 0) ? 0 : 10 - (suma % 10);
        (codigoBarras[12] - '0').Should().Be(checkDigitEsperado);
    }
}
