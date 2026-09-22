using Microsoft.EntityFrameworkCore;
using PuntoDeVentaLibreria.Application.DTOs.Ventas;
using PuntoDeVentaLibreria.Application.Services;
using PuntoDeVentaLibreria.Domain.Entities.Catalogo;
using PuntoDeVentaLibreria.Domain.Entities.Clientes;
using PuntoDeVentaLibreria.Domain.Entities.Finanzas;
using PuntoDeVentaLibreria.Domain.Entities.Inventario;
using PuntoDeVentaLibreria.Domain.Entities.Ventas;
using PuntoDeVentaLibreria.Infrastructure.Data;

namespace PuntoDeVentaLibreria.Infrastructure.Services;

public class VentaService : IVentaService
{
    private readonly AppDbContext _context;

    public VentaService(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<VentaRealizadaDto> ProcesarVentaAsync(RegistrarVentaDto dto, CancellationToken ct = default)
    {
        if (dto.Items == null || dto.Items.Count == 0)
            throw new ArgumentException("El carrito de compras no contiene artículos.");

        var turno = await _context.TurnosCaja.FirstOrDefaultAsync(t => t.Id == dto.TurnoCajaId && t.FechaCierre == null, ct)
                    ?? throw new InvalidOperationException("No hay una caja abierta para procesar la venta.");

        decimal subtotalBruto = dto.Items.Sum(i => i.Subtotal);
        decimal totalVenta = subtotalBruto - dto.DescuentoEfectivoMonto + dto.RecargoCuotasMonto;
        decimal totalCostoHistorico = 0;

        var correlativo = await _context.Ventas.CountAsync(ct) + 1;
        var numeroComprobante = $"L-{correlativo:D6}";
        var config = await _context.Configuraciones.AsNoTracking().FirstOrDefaultAsync(ct);
        var nombreTerminal = config?.NombreTerminal ?? "Caja Principal";

        var venta = new Venta
        {
            NumeroComprobante = numeroComprobante,
            TurnoCajaId = turno.Id,
            ClienteId = dto.ClienteId,
            VendedoraNombre = dto.VendedoraNombre,
            NombreTerminal = nombreTerminal,
            SubtotalBruto = subtotalBruto,
            DescuentoEfectivoMonto = dto.DescuentoEfectivoMonto,
            RecargoCuotasMonto = dto.RecargoCuotasMonto,
            TotalVenta = totalVenta,
            MetodoPagoPrincipal = dto.MetodoPago,
            CantidadCuotas = dto.CantidadCuotas
        };


        foreach (var item in dto.Items)
        {
            var linea = new LineaVenta
            {
                VentaId = venta.Id,
                ArticuloId = item.ArticuloId,
                ArticuloVarianteId = item.ArticuloVarianteId,
                VarianteNombre = item.VarianteNombre,
                Descripcion = item.Descripcion,
                SKU = item.SKU,
                CodigoBarras = item.CodigoBarras,
                Cantidad = item.Cantidad,
                PrecioUnitarioVenta = item.PrecioUnitario,
                EsVentaManual = item.EsVentaManual,
                EsCombo = item.EsCombo
            };

            if (item.ArticuloId.HasValue && !item.EsVentaManual)
            {
                var articulo = await _context.Articulos
                    .Include(a => a.ItemsDelCombo)
                        .ThenInclude(c => c.ComponenteArticulo)
                    .FirstOrDefaultAsync(a => a.Id == item.ArticuloId.Value, ct);

                if (articulo != null)
                {
                    linea.PrecioCostoHistorico = articulo.PrecioCosto;
                    totalCostoHistorico += articulo.PrecioCosto * item.Cantidad;

                    // Descuento de stock según tipo de artículo
                    if (articulo.Tipo == TipoArticulo.ComboKit)
                    {
                        // Descontar cada componente individual del combo escolar
                        foreach (var comboItem in articulo.ItemsDelCombo)
                        {
                            var componente = comboItem.ComponenteArticulo;
                            if (componente != null && componente.Tipo != TipoArticulo.Servicio)
                            {
                                decimal unidadesADescontar = comboItem.Cantidad * item.Cantidad;
                                decimal prev = componente.StockActual;
                                componente.StockActual -= unidadesADescontar;

                                _context.MovimientosStock.Add(new MovimientoStock
                                {
                                    ArticuloId = componente.Id,
                                    Tipo = TipoMovimientoStock.Venta,
                                    Cantidad = -unidadesADescontar,
                                    StockPrevio = prev,
                                    StockPosterior = componente.StockActual,
                                    Motivo = $"Venta Combo {articulo.Nombre} ({numeroComprobante})",
                                    UsuarioNombre = dto.VendedoraNombre
                                });
                            }
                        }
                    }
                    else if (articulo.EsPack && articulo.ArticuloBaseId.HasValue)
                    {
                        // Artículo configurado como Pack o Caja fraccionable: descuenta las unidades del artículo base
                        var baseArt = await _context.Articulos.FirstOrDefaultAsync(a => a.Id == articulo.ArticuloBaseId.Value, ct);
                        if (baseArt != null)
                        {
                            decimal factor = articulo.CantidadPorPack > 0 ? articulo.CantidadPorPack : 1;
                            decimal unidadesADescontar = factor * item.Cantidad;
                            decimal prev = baseArt.StockActual;
                            baseArt.StockActual -= unidadesADescontar;

                            _context.MovimientosStock.Add(new MovimientoStock
                            {
                                ArticuloId = baseArt.Id,
                                Tipo = TipoMovimientoStock.Venta,
                                Cantidad = -unidadesADescontar,
                                StockPrevio = prev,
                                StockPosterior = baseArt.StockActual,
                                Motivo = $"Venta Pack {articulo.Nombre} (x{item.Cantidad}) - Descuento base ({numeroComprobante})",
                                UsuarioNombre = dto.VendedoraNombre
                            });
                        }
                    }
                    else if (articulo.Tipo != TipoArticulo.Servicio)
                    {
                        // Si se vendió una variante / color específico
                        if (item.ArticuloVarianteId.HasValue)
                        {
                            var variante = await _context.ArticuloVariantes.FirstOrDefaultAsync(v => v.Id == item.ArticuloVarianteId.Value, ct);
                            if (variante != null)
                            {
                                decimal prevVar = variante.StockActual;
                                variante.StockActual -= item.Cantidad;

                                decimal prevArt = articulo.StockActual;
                                articulo.StockActual -= item.Cantidad;

                                _context.MovimientosStock.Add(new MovimientoStock
                                {
                                    ArticuloId = articulo.Id,
                                    ArticuloVarianteId = variante.Id,
                                    Tipo = TipoMovimientoStock.Venta,
                                    Cantidad = -item.Cantidad,
                                    StockPrevio = prevVar,
                                    StockPosterior = variante.StockActual,
                                    Motivo = $"Venta mostrador {numeroComprobante} ({variante.Nombre})",
                                    UsuarioNombre = dto.VendedoraNombre
                                });
                            }
                            else
                            {
                                decimal prev = articulo.StockActual;
                                articulo.StockActual -= item.Cantidad;

                                _context.MovimientosStock.Add(new MovimientoStock
                                {
                                    ArticuloId = articulo.Id,
                                    Tipo = TipoMovimientoStock.Venta,
                                    Cantidad = -item.Cantidad,
                                    StockPrevio = prev,
                                    StockPosterior = articulo.StockActual,
                                    Motivo = $"Venta mostrador {numeroComprobante}",
                                    UsuarioNombre = dto.VendedoraNombre
                                });
                            }
                        }
                        else
                        {
                            // Artículo físico estándar sin variantes
                            decimal prev = articulo.StockActual;
                            articulo.StockActual -= item.Cantidad;

                            _context.MovimientosStock.Add(new MovimientoStock
                            {
                                ArticuloId = articulo.Id,
                                Tipo = TipoMovimientoStock.Venta,
                                Cantidad = -item.Cantidad,
                                StockPrevio = prev,
                                StockPosterior = articulo.StockActual,
                                Motivo = $"Venta mostrador {numeroComprobante}",
                                UsuarioNombre = dto.VendedoraNombre
                            });
                        }
                    }
                }
            }
            else
            {
                // Venta manual / rápida
                linea.PrecioCostoHistorico = item.PrecioCosto;
                totalCostoHistorico += item.PrecioCosto * item.Cantidad;
            }

            venta.LineasVenta.Add(linea);
        }

        venta.TotalCostoHistorico = totalCostoHistorico;

        // Si se especificó un cliente registrado, asociarlo a la venta
        Cliente? cliente = null;
        if (dto.ClienteId.HasValue)
        {
            cliente = await _context.Clientes.FindAsync(new object[] { dto.ClienteId.Value }, ct);
            if (cliente != null)
            {
                venta.ClienteId = cliente.Id;
            }
        }

        var clienteInfo = !string.IsNullOrWhiteSpace(dto.ClienteNombre) ? dto.ClienteNombre : (cliente?.NombreCompleto ?? "Consumidor Final");
        var detalleRef = !string.IsNullOrWhiteSpace(dto.ReferenciaPago) ? $" - Ref/Titular: {dto.ReferenciaPago}" : string.Empty;

        bool esFiadoConEntrega = dto.MetodoPago == "CtaCte" && dto.TieneEntregaInicial && dto.MontoEntregaInicial > 0;
        decimal montoEntrega = esFiadoConEntrega ? Math.Min(dto.MontoEntregaInicial, totalVenta) : 0m;
        decimal montoFiado = esFiadoConEntrega ? (totalVenta - montoEntrega) : (dto.MetodoPago == "CtaCte" ? totalVenta : 0m);
        string metodoEntrega = string.IsNullOrWhiteSpace(dto.MetodoPagoEntrega) ? "Efectivo" : dto.MetodoPagoEntrega;

        if (cliente != null && dto.MetodoPago == "CtaCte")
        {
            // Solo sumar al saldo deudor la porción que realmente queda fiada
            cliente.SaldoDeudorActual += montoFiado;
        }

        if (esFiadoConEntrega)
        {
            // 1. Asentar entrega inmediata
            if (montoEntrega > 0)
            {
                venta.Pagos.Add(new PagoVenta
                {
                    VentaId = venta.Id,
                    MetodoPago = metodoEntrega,
                    Monto = montoEntrega,
                    ReferenciaOperacion = dto.ReferenciaEntrega
                });

                _context.MovimientosCaja.Add(new MovimientoCaja
                {
                    TurnoCajaId = turno.Id,
                    Tipo = TipoMovimientoCaja.IngresoVenta,
                    Monto = montoEntrega,
                    MetodoPago = metodoEntrega,
                    Concepto = $"Entrega inicial venta fiada {numeroComprobante} ({metodoEntrega}) - Cliente: {clienteInfo}",
                    UsuarioNombre = dto.VendedoraNombre,
                    VentaId = venta.Id,
                    ClienteId = cliente?.Id
                });
            }

            // 2. Asentar saldo fiado en cuenta corriente
            if (montoFiado > 0)
            {
                venta.Pagos.Add(new PagoVenta
                {
                    VentaId = venta.Id,
                    MetodoPago = "CtaCte",
                    Monto = montoFiado
                });

                _context.MovimientosCaja.Add(new MovimientoCaja
                {
                    TurnoCajaId = turno.Id,
                    Tipo = TipoMovimientoCaja.CobroCuentaCorriente,
                    Monto = montoFiado,
                    MetodoPago = "CtaCte",
                    Concepto = $"Venta fiada Cta. Cte. {numeroComprobante} (Saldo fiado) - Cliente: {clienteInfo}",
                    UsuarioNombre = dto.VendedoraNombre,
                    VentaId = venta.Id,
                    ClienteId = cliente?.Id
                });
            }
        }
        else
        {
            // Flujo de pago único estándar
            venta.Pagos.Add(new PagoVenta
            {
                VentaId = venta.Id,
                MetodoPago = dto.MetodoPago,
                Monto = totalVenta,
                ReferenciaOperacion = dto.ReferenciaPago
            });

            string concepto = dto.MetodoPago switch
            {
                "CtaCte" => $"Venta fiada Cta. Cte. {numeroComprobante} - Cliente: {clienteInfo}",
                "Transferencia" => $"Cobro venta {numeroComprobante} (Transferencia{detalleRef})",
                "Debito" => $"Cobro venta {numeroComprobante} (Débito{detalleRef})",
                "Credito" => $"Cobro venta {numeroComprobante} (Crédito {dto.CantidadCuotas}c{detalleRef})",
                _ => $"Cobro venta {numeroComprobante} (Efectivo - {clienteInfo})"
            };

            _context.MovimientosCaja.Add(new MovimientoCaja
            {
                TurnoCajaId = turno.Id,
                Tipo = dto.MetodoPago == "CtaCte" ? TipoMovimientoCaja.CobroCuentaCorriente : TipoMovimientoCaja.IngresoVenta,
                Monto = totalVenta,
                MetodoPago = dto.MetodoPago,
                Concepto = concepto,
                UsuarioNombre = dto.VendedoraNombre,
                VentaId = venta.Id,
                ClienteId = cliente?.Id
            });
        }

        _context.Ventas.Add(venta);
        await _context.SaveChangesAsync(ct);

        return new VentaRealizadaDto
        {
            VentaId = venta.Id,
            NumeroComprobante = numeroComprobante,
            TotalCobrado = totalVenta,
            SubtotalBruto = subtotalBruto,
            DescuentoMonto = dto.DescuentoEfectivoMonto,
            RecargoMonto = dto.RecargoCuotasMonto,
            MontoEntregado = dto.MontoEntregado > 0 ? dto.MontoEntregado : totalVenta,
            Vuelto = dto.Vuelto,
            MetodoPago = dto.MetodoPago,
            ClienteNombre = clienteInfo,
            VendedoraNombre = dto.VendedoraNombre,
            ReferenciaPago = dto.ReferenciaPago,
            Fecha = venta.FechaVenta,
            Lineas = dto.Items,
            TieneEntregaInicial = esFiadoConEntrega,
            MontoEntregaInicial = montoEntrega,
            MetodoPagoEntrega = metodoEntrega,
            MontoFiado = montoFiado
        };
    }
}
