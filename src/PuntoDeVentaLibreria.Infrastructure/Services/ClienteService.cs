using Microsoft.EntityFrameworkCore;
using PuntoDeVentaLibreria.Application.DTOs.Clientes;
using PuntoDeVentaLibreria.Application.Services;
using PuntoDeVentaLibreria.Domain.Entities.Clientes;
using PuntoDeVentaLibreria.Domain.Entities.Finanzas;
using PuntoDeVentaLibreria.Infrastructure.Data;

namespace PuntoDeVentaLibreria.Infrastructure.Services;

public class ClienteService : IClienteService
{
    private readonly AppDbContext _context;

    public ClienteService(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<ClienteDto>> BuscarClientesAsync(string criterio, CancellationToken cancellationToken = default)
    {
        var query = _context.Clientes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(criterio))
        {
            var c = criterio.Trim().ToLower();
            query = query.Where(x =>
                x.NombreCompleto.ToLower().Contains(c) ||
                (x.DniOCuit != null && x.DniOCuit.Contains(c)) ||
                (x.ColegioOInstitucion != null && x.ColegioOInstitucion.ToLower().Contains(c)));
        }

        return await query
            .OrderBy(x => x.NombreCompleto)
            .Select(x => new ClienteDto
            {
                Id = x.Id,
                NombreCompleto = x.NombreCompleto,
                DniOCuit = x.DniOCuit,
                Telefono = x.Telefono,
                Email = x.Email,
                Direccion = x.Direccion,
                ColegioOInstitucion = x.ColegioOInstitucion,
                PermiteFiado = x.PermiteFiado,
                LimiteCredito = x.LimiteCredito,
                SaldoDeudorActual = x.SaldoDeudorActual
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ClienteDto?> ObtenerPorIdAsync(Guid clienteId, CancellationToken cancellationToken = default)
    {
        var x = await _context.Clientes.FindAsync(new object[] { clienteId }, cancellationToken);
        if (x == null) return null;

        return new ClienteDto
        {
            Id = x.Id,
            NombreCompleto = x.NombreCompleto,
            DniOCuit = x.DniOCuit,
            Telefono = x.Telefono,
            Email = x.Email,
            Direccion = x.Direccion,
            ColegioOInstitucion = x.ColegioOInstitucion,
            PermiteFiado = x.PermiteFiado,
            LimiteCredito = x.LimiteCredito,
            SaldoDeudorActual = x.SaldoDeudorActual
        };
    }

    public async Task<Cliente> GuardarClienteAsync(ClienteDto dto, CancellationToken cancellationToken = default)
    {
        Cliente cliente;
        if (dto.Id != Guid.Empty)
        {
            cliente = await _context.Clientes.FindAsync(new object[] { dto.Id }, cancellationToken)
                      ?? throw new InvalidOperationException($"No se encontró el cliente con ID {dto.Id}");
        }
        else
        {
            cliente = new Cliente();
            _context.Clientes.Add(cliente);
        }

        cliente.NombreCompleto = dto.NombreCompleto.Trim();
        cliente.DniOCuit = dto.DniOCuit?.Trim();
        cliente.Telefono = dto.Telefono?.Trim();
        cliente.Email = dto.Email?.Trim();
        cliente.Direccion = dto.Direccion?.Trim();
        cliente.ColegioOInstitucion = dto.ColegioOInstitucion?.Trim();
        cliente.PermiteFiado = dto.PermiteFiado;
        cliente.LimiteCredito = dto.LimiteCredito;

        await _context.SaveChangesAsync(cancellationToken);
        return cliente;
    }

    public async Task EliminarClienteAsync(Guid clienteId, CancellationToken cancellationToken = default)
    {
        var cliente = await _context.Clientes.FindAsync(new object[] { clienteId }, cancellationToken);
        if (cliente != null)
        {
            _context.Clientes.Remove(cliente);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<ReciboCobroCtaCteDto> CobrarSaldoCuentaCorrienteAsync(RegistrarEntregaCuentaCorrienteDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.MontoEntrega <= 0)
            throw new ArgumentException("El monto a entregar debe ser mayor a 0.");

        var cliente = await _context.Clientes.FindAsync(new object[] { dto.ClienteId }, cancellationToken)
                      ?? throw new InvalidOperationException("Cliente no encontrado.");

        var turno = await _context.TurnosCaja.FirstOrDefaultAsync(t => t.Id == dto.TurnoCajaId && t.FechaCierre == null, cancellationToken)
                    ?? throw new InvalidOperationException("No hay una caja abierta para asentar el cobro.");

        decimal saldoAnterior = cliente.SaldoDeudorActual;
        cliente.SaldoDeudorActual = Math.Max(0, cliente.SaldoDeudorActual - dto.MontoEntrega);
        decimal saldoRestante = cliente.SaldoDeudorActual;

        // Asentar ingreso en caja como cobro de cuenta corriente
        _context.MovimientosCaja.Add(new MovimientoCaja
        {
            TurnoCajaId = turno.Id,
            ClienteId = cliente.Id,
            Tipo = TipoMovimientoCaja.CobroCuentaCorriente,
            Monto = dto.MontoEntrega,
            MetodoPago = dto.MetodoPago,
            Concepto = $"Cobro Cta. Cte. Cliente: {cliente.NombreCompleto}. {dto.Observaciones}".Trim(),
            UsuarioNombre = dto.UsuarioNombre
        });

        await _context.SaveChangesAsync(cancellationToken);

        var random = new Random();
        var numRecibo = $"REC-{DateTime.Now:yyyyMMdd}-{random.Next(1000, 9999)}";

        return new ReciboCobroCtaCteDto
        {
            NumeroRecibo = numRecibo,
            Fecha = DateTime.Now,
            ClienteNombre = cliente.NombreCompleto,
            ClienteDni = cliente.DniOCuit,
            SaldoAnterior = saldoAnterior,
            MontoAbonado = dto.MontoEntrega,
            SaldoRestante = saldoRestante,
            MetodoPago = dto.MetodoPago,
            Cajero = dto.UsuarioNombre,
            Observaciones = dto.Observaciones
        };
    }

    public async Task<IReadOnlyList<MovimientoClienteDto>> ObtenerHistorialClienteAsync(Guid clienteId, CancellationToken cancellationToken = default)
    {
        var cliente = await _context.Clientes.FindAsync(new object[] { clienteId }, cancellationToken);
        if (cliente == null) return Array.Empty<MovimientoClienteDto>();

        var historial = new List<MovimientoClienteDto>();

        // 1. Ventas realizadas al cliente
        var ventas = await _context.Ventas
            .AsNoTracking()
            .Include(v => v.LineasVenta)
            .Where(v => v.ClienteId == clienteId)
            .ToListAsync(cancellationToken);

        foreach (var v in ventas)
        {
            bool esCtaCte = v.MetodoPagoPrincipal == "CtaCte";
            var detalleItems = string.Join(", ", v.LineasVenta.Take(3).Select(l => $"{l.Cantidad}x {l.Descripcion}"));
            if (v.LineasVenta.Count > 3)
            {
                detalleItems += $" y {v.LineasVenta.Count - 3} más...";
            }

            historial.Add(new MovimientoClienteDto
            {
                Fecha = v.FechaVenta,
                Tipo = esCtaCte ? "Compra Fiada (Cta. Cte.)" : $"Compra ({v.MetodoPagoPrincipal})",
                Comprobante = v.NumeroComprobante,
                Monto = v.TotalVenta,
                MetodoPago = v.MetodoPagoPrincipal,
                Detalle = detalleItems,
                EsAbono = false
            });
        }

        // 2. Cobros / Abonos a la cuenta corriente
        var movimientosCaja = await _context.MovimientosCaja
            .AsNoTracking()
            .Where(m => m.ClienteId == clienteId || 
                       (m.Tipo == TipoMovimientoCaja.CobroCuentaCorriente && m.Concepto.Contains(cliente.NombreCompleto)))
            .ToListAsync(cancellationToken);

        foreach (var m in movimientosCaja)
        {
            historial.Add(new MovimientoClienteDto
            {
                Fecha = m.FechaCreacion,
                Tipo = "Abono / Cobro Cta. Cte.",
                Comprobante = "Abono en Caja",
                Monto = m.Monto,
                MetodoPago = m.MetodoPago,
                Detalle = m.Concepto,
                EsAbono = true
            });
        }

        return historial.OrderByDescending(h => h.Fecha).ToList();
    }
}
