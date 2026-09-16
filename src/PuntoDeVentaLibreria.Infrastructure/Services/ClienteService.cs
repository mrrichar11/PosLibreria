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

    public async Task CobrarSaldoCuentaCorrienteAsync(RegistrarEntregaCuentaCorrienteDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.MontoEntrega <= 0)
            throw new ArgumentException("El monto a entregar debe ser mayor a 0.");

        var cliente = await _context.Clientes.FindAsync(new object[] { dto.ClienteId }, cancellationToken)
                      ?? throw new InvalidOperationException("Cliente no encontrado.");

        var turno = await _context.TurnosCaja.FirstOrDefaultAsync(t => t.Id == dto.TurnoCajaId && t.FechaCierre == null, cancellationToken)
                    ?? throw new InvalidOperationException("No hay una caja abierta para asentar el cobro.");

        cliente.SaldoDeudorActual = Math.Max(0, cliente.SaldoDeudorActual - dto.MontoEntrega);

        // Asentar ingreso en caja
        _context.MovimientosCaja.Add(new MovimientoCaja
        {
            TurnoCajaId = turno.Id,
            Tipo = TipoMovimientoCaja.IngresoVenta,
            Monto = dto.MontoEntrega,
            MetodoPago = dto.MetodoPago,
            Concepto = $"Cobro Cta. Cte. Cliente: {cliente.NombreCompleto}. {dto.Observaciones}".Trim(),
            UsuarioNombre = dto.UsuarioNombre
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}
