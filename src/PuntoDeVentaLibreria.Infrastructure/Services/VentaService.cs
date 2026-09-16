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

        var venta = new Venta
        {
            NumeroComprobante = numeroComprobante,
            TurnoCajaId = turno.Id,
            ClienteId = dto.ClienteId,
            VendedoraNombre = dto.VendedoraNombre,
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
                    else if (articulo.Tipo != TipoArticulo.Servicio)
                    {
                        // Artículo físico estándar o fraccionable: descontar directamente
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
                // Si la venta es con Cuenta Corriente / Fiado, sumarle el saldo deudor
                if (dto.MetodoPago == "CtaCte")
                {
                    cliente.SaldoDeudorActual += totalVenta;
                }
            }
        }

        // Registrar Pago
        venta.Pagos.Add(new PagoVenta
        {
            VentaId = venta.Id,
            MetodoPago = dto.MetodoPago,
            Monto = totalVenta,
            ReferenciaOperacion = dto.ReferenciaPago
        });

        // Generar concepto detallado para trazabilidad de caja y movimientos
        var clienteInfo = !string.IsNullOrWhiteSpace(dto.ClienteNombre) ? dto.ClienteNombre : (cliente?.NombreCompleto ?? "Consumidor Final");
        var detalleRef = !string.IsNullOrWhiteSpace(dto.ReferenciaPago) ? $" - Ref/Titular: {dto.ReferenciaPago}" : string.Empty;

        string concepto = dto.MetodoPago switch
        {
            "CtaCte" => $"Venta fiada Cta. Cte. {numeroComprobante} - Cliente: {clienteInfo}",
            "Transferencia" => $"Cobro venta {numeroComprobante} (Transferencia{detalleRef})",
            "Debito" => $"Cobro venta {numeroComprobante} (Débito{detalleRef})",
            "Credito" => $"Cobro venta {numeroComprobante} (Crédito {dto.CantidadCuotas}c{detalleRef})",
            _ => $"Cobro venta {numeroComprobante} (Efectivo - {clienteInfo})"
        };

        // Registrar en Movimientos de Caja
        _context.MovimientosCaja.Add(new MovimientoCaja
        {
            TurnoCajaId = turno.Id,
            Tipo = dto.MetodoPago == "CtaCte" ? TipoMovimientoCaja.CobroCuentaCorriente : TipoMovimientoCaja.IngresoVenta,
            Monto = totalVenta,
            MetodoPago = dto.MetodoPago,
            Concepto = concepto,
            UsuarioNombre = dto.VendedoraNombre,
            VentaId = venta.Id
        });

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
            MetodoPago = dto.MetodoPago,
            ClienteNombre = clienteInfo,
            Fecha = venta.FechaVenta,
            Lineas = dto.Items
        };
    }
}
