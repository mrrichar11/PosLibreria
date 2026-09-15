using Microsoft.EntityFrameworkCore;
using PuntoDeVentaLibreria.Application.Services;
using PuntoDeVentaLibreria.Domain.Entities.Finanzas;
using PuntoDeVentaLibreria.Infrastructure.Data;

namespace PuntoDeVentaLibreria.Infrastructure.Services;

public class CajaService : ICajaService
{
    private readonly AppDbContext _context;

    public CajaService(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<TurnoCaja?> ObtenerTurnoActivoAsync(CancellationToken ct = default)
    {
        return await _context.TurnosCaja
            .Include(t => t.Movimientos)
            .FirstOrDefaultAsync(t => t.FechaCierre == null, ct);
    }

    public async Task<TurnoCaja> AbrirTurnoAsync(decimal montoInicial, string usuario, CancellationToken ct = default)
    {
        var activo = await ObtenerTurnoActivoAsync(ct);
        if (activo != null)
            throw new InvalidOperationException("Ya existe una caja abierta actualmente.");

        var turno = new TurnoCaja
        {
            FechaApertura = DateTime.UtcNow,
            MontoInicialEfectivo = montoInicial,
            UsuarioApertura = usuario
        };

        _context.TurnosCaja.Add(turno);
        await _context.SaveChangesAsync(ct);
        return turno;
    }

    public async Task<TurnoCaja> CerrarTurnoAsync(decimal montoEfectivoReal, string usuario, string? observaciones, CancellationToken ct = default)
    {
        var turno = await ObtenerTurnoActivoAsync(ct)
                    ?? throw new InvalidOperationException("No hay ningún turno de caja abierto.");

        var ingresosEfectivo = turno.Movimientos
            .Where(m => m.MetodoPago == "Efectivo" && m.Tipo == TipoMovimientoCaja.IngresoVenta)
            .Sum(m => m.Monto);

        var egresosEfectivo = turno.Movimientos
            .Where(m => m.MetodoPago == "Efectivo" && (m.Tipo == TipoMovimientoCaja.GastoOperativo || m.Tipo == TipoMovimientoCaja.RetiroDueño))
            .Sum(m => m.Monto);

        decimal sistemaEfectivo = turno.MontoInicialEfectivo + ingresosEfectivo - egresosEfectivo;

        turno.FechaCierre = DateTime.UtcNow;
        turno.UsuarioCierre = usuario;
        turno.MontoCierreEfectivoSistema = sistemaEfectivo;
        turno.MontoCierreEfectivoReal = montoEfectivoReal;
        turno.DiferenciaEfectivo = montoEfectivoReal - sistemaEfectivo;
        turno.ObservacionesCierre = observaciones;

        await _context.SaveChangesAsync(ct);
        return turno;
    }

    public async Task<MovimientoCaja> RegistrarGastoOperativoAsync(decimal monto, string concepto, string usuario, CancellationToken ct = default)
    {
        var turno = await ObtenerTurnoActivoAsync(ct)
                    ?? throw new InvalidOperationException("La caja debe estar abierta para registrar gastos.");

        var mov = new MovimientoCaja
        {
            TurnoCajaId = turno.Id,
            Tipo = TipoMovimientoCaja.GastoOperativo,
            Monto = monto,
            MetodoPago = "Efectivo",
            Concepto = concepto.Trim(),
            UsuarioNombre = usuario
        };

        _context.MovimientosCaja.Add(mov);
        await _context.SaveChangesAsync(ct);
        return mov;
    }

    public async Task<MovimientoCaja> RegistrarRetiroDueñoAsync(decimal monto, string concepto, string usuario, CancellationToken ct = default)
    {
        var turno = await ObtenerTurnoActivoAsync(ct)
                    ?? throw new InvalidOperationException("La caja debe estar abierta para registrar retiros.");

        var mov = new MovimientoCaja
        {
            TurnoCajaId = turno.Id,
            Tipo = TipoMovimientoCaja.RetiroDueño,
            Monto = monto,
            MetodoPago = "Efectivo",
            Concepto = concepto.Trim(),
            UsuarioNombre = usuario
        };

        _context.MovimientosCaja.Add(mov);
        await _context.SaveChangesAsync(ct);
        return mov;
    }
}
