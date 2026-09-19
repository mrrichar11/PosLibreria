using Microsoft.EntityFrameworkCore;
using PuntoDeVentaLibreria.Application.DTOs.Caja;
using PuntoDeVentaLibreria.Application.Services;
using PuntoDeVentaLibreria.Domain.Entities.Finanzas;
using PuntoDeVentaLibreria.Infrastructure.Data;

namespace PuntoDeVentaLibreria.Infrastructure.Services;

public class CajaService : ICajaService
{
    private readonly AppDbContext _context;
    private readonly IBackupService _backupService;

    public CajaService(AppDbContext context, IBackupService backupService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
    }

    public async Task<TurnoCaja?> ObtenerTurnoActivoAsync(CancellationToken ct = default)
    {
        var config = await _context.Configuraciones.AsNoTracking().FirstOrDefaultAsync(ct);
        var esIndependiente = config != null && config.ModoCajaMultiTerminal.Equals("Independiente", StringComparison.OrdinalIgnoreCase);
        var nombreTerminal = config?.NombreTerminal ?? "Caja Principal";

        if (esIndependiente)
        {
            return await _context.TurnosCaja
                .Include(t => t.Movimientos)
                .FirstOrDefaultAsync(t => t.FechaCierre == null && t.NombreTerminal == nombreTerminal, ct);
        }

        return await _context.TurnosCaja
            .Include(t => t.Movimientos)
            .FirstOrDefaultAsync(t => t.FechaCierre == null, ct);
    }

    public async Task<TurnoCaja> AbrirTurnoAsync(decimal montoInicial, string usuario, CancellationToken ct = default)
    {
        var activo = await ObtenerTurnoActivoAsync(ct);
        if (activo != null)
            throw new InvalidOperationException("Ya existe una caja abierta actualmente para esta estación.");

        var config = await _context.Configuraciones.AsNoTracking().FirstOrDefaultAsync(ct);
        var nombreTerminal = config?.NombreTerminal ?? "Caja Principal";
        var modoTerminal = config?.ModoCajaMultiTerminal ?? "Compartida";

        var turno = new TurnoCaja
        {
            FechaApertura = DateTime.UtcNow,
            MontoInicialEfectivo = montoInicial,
            UsuarioApertura = usuario,
            NombreTerminal = nombreTerminal,
            ModoTerminal = modoTerminal
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
            .Where(m => m.MetodoPago == "Efectivo" && (m.Tipo == TipoMovimientoCaja.IngresoVenta || m.Tipo == TipoMovimientoCaja.CobroCuentaCorriente))
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

        // Backup automático de cierre de caja si está habilitado
        try
        {
            var config = await _context.Configuraciones.AsNoTracking().FirstOrDefaultAsync(ct);
            if (config != null && config.BackupAutomaticoAlCierreCaja && config.MotorBaseDatos.Equals("SQLite", StringComparison.OrdinalIgnoreCase))
            {
                await _backupService.CrearBackupAsync(null, "cierre_caja", ct);
            }
        }
        catch { }

        return turno;
    }

    public async Task<ResumenCierreTurnoDto> ObtenerResumenTurnoAsync(Guid turnoId, CancellationToken ct = default)
    {
        var turno = await _context.TurnosCaja
            .Include(t => t.Movimientos)
            .FirstOrDefaultAsync(t => t.Id == turnoId, ct)
            ?? throw new InvalidOperationException($"No se encontró el turno con ID {turnoId}");

        var ventasEf = turno.Movimientos.Where(m => m.Tipo == TipoMovimientoCaja.IngresoVenta && m.MetodoPago == "Efectivo").Sum(m => m.Monto);
        var cobrosCta = turno.Movimientos.Where(m => m.Tipo == TipoMovimientoCaja.CobroCuentaCorriente && m.MetodoPago == "Efectivo").Sum(m => m.Monto);
        var gastos = turno.Movimientos.Where(m => m.Tipo == TipoMovimientoCaja.GastoOperativo).Sum(m => m.Monto);
        var retiros = turno.Movimientos.Where(m => m.Tipo == TipoMovimientoCaja.RetiroDueño).Sum(m => m.Monto);

        var ventasDeb = turno.Movimientos.Where(m => m.Tipo == TipoMovimientoCaja.IngresoVenta && m.MetodoPago == "Debito").Sum(m => m.Monto);
        var ventasCred = turno.Movimientos.Where(m => m.Tipo == TipoMovimientoCaja.IngresoVenta && m.MetodoPago == "Credito").Sum(m => m.Monto);
        var ventasTransf = turno.Movimientos.Where(m => m.Tipo == TipoMovimientoCaja.IngresoVenta && m.MetodoPago == "Transferencia").Sum(m => m.Monto);
        var ventasCta = turno.Movimientos.Where(m => m.Tipo == TipoMovimientoCaja.IngresoVenta && m.MetodoPago == "CtaCte").Sum(m => m.Monto);

        var fechaCierre = turno.FechaCierre ?? DateTime.UtcNow;
        var realContado = turno.MontoCierreEfectivoReal ?? (turno.MontoInicialEfectivo + ventasEf + cobrosCta - gastos - retiros);

        return new ResumenCierreTurnoDto
        {
            TurnoId = turno.Id,
            FechaApertura = turno.FechaApertura,
            FechaCierre = fechaCierre,
            UsuarioApertura = turno.UsuarioApertura,
            UsuarioCierre = turno.UsuarioCierre ?? "Cajero",
            FondoInicial = turno.MontoInicialEfectivo,
            VentasEfectivo = ventasEf,
            CobrosCtaCteEfectivo = cobrosCta,
            GastosOperativos = gastos,
            RetirosDueño = retiros,
            EfectivoRealContado = realContado,
            VentasDebito = ventasDeb,
            VentasCredito = ventasCred,
            VentasTransferencia = ventasTransf,
            VentasCtaCte = ventasCta,
            CantidadOperaciones = turno.Movimientos.Count,
            Observaciones = turno.ObservacionesCierre
        };
    }

    public async Task<ResumenCierreTurnoDto> ObtenerResumenConsolidadoDelDiaAsync(DateTime? fecha = null, CancellationToken ct = default)
    {
        var targetFecha = fecha?.Date ?? DateTime.UtcNow.Date;
        var turnosDelDia = await _context.TurnosCaja
            .Include(t => t.Movimientos)
            .Where(t => t.FechaApertura.Date == targetFecha)
            .ToListAsync(ct);

        var todosMovs = turnosDelDia.SelectMany(t => t.Movimientos).ToList();

        var fondoInicialTotal = turnosDelDia.Sum(t => t.MontoInicialEfectivo);
        var ventasEf = todosMovs.Where(m => m.Tipo == TipoMovimientoCaja.IngresoVenta && m.MetodoPago == "Efectivo").Sum(m => m.Monto);
        var cobrosCta = todosMovs.Where(m => m.Tipo == TipoMovimientoCaja.CobroCuentaCorriente && m.MetodoPago == "Efectivo").Sum(m => m.Monto);
        var gastos = todosMovs.Where(m => m.Tipo == TipoMovimientoCaja.GastoOperativo).Sum(m => m.Monto);
        var retiros = todosMovs.Where(m => m.Tipo == TipoMovimientoCaja.RetiroDueño).Sum(m => m.Monto);

        var ventasDeb = todosMovs.Where(m => m.Tipo == TipoMovimientoCaja.IngresoVenta && m.MetodoPago == "Debito").Sum(m => m.Monto);
        var ventasCred = todosMovs.Where(m => m.Tipo == TipoMovimientoCaja.IngresoVenta && m.MetodoPago == "Credito").Sum(m => m.Monto);
        var ventasTransf = todosMovs.Where(m => m.Tipo == TipoMovimientoCaja.IngresoVenta && m.MetodoPago == "Transferencia").Sum(m => m.Monto);
        var ventasCta = todosMovs.Where(m => m.Tipo == TipoMovimientoCaja.IngresoVenta && m.MetodoPago == "CtaCte").Sum(m => m.Monto);

        var realContado = turnosDelDia.Sum(t => t.MontoCierreEfectivoReal ?? (t.MontoInicialEfectivo + t.Movimientos.Where(m => m.Tipo == TipoMovimientoCaja.IngresoVenta && m.MetodoPago == "Efectivo").Sum(m => m.Monto) - t.Movimientos.Where(m => m.Tipo == TipoMovimientoCaja.GastoOperativo).Sum(m => m.Monto)));

        return new ResumenCierreTurnoDto
        {
            TurnoId = Guid.Empty,
            FechaApertura = targetFecha,
            FechaCierre = targetFecha.AddDays(1).AddSeconds(-1),
            UsuarioApertura = "Todas las Cajas (Consolidado)",
            UsuarioCierre = "Reporte General",
            FondoInicial = fondoInicialTotal,
            VentasEfectivo = ventasEf,
            CobrosCtaCteEfectivo = cobrosCta,
            GastosOperativos = gastos,
            RetirosDueño = retiros,
            EfectivoRealContado = realContado,
            VentasDebito = ventasDeb,
            VentasCredito = ventasCred,
            VentasTransferencia = ventasTransf,
            VentasCtaCte = ventasCta,
            CantidadOperaciones = todosMovs.Count,
            Observaciones = $"Total de turnos computados: {turnosDelDia.Count}"
        };
    }

    public async Task<IReadOnlyList<TurnoCaja>> ObtenerHistorialTurnosAsync(DateTime? desde = null, DateTime? hasta = null, CancellationToken ct = default)
    {
        var query = _context.TurnosCaja.Include(t => t.Movimientos).AsQueryable();

        if (desde.HasValue)
        {
            query = query.Where(t => t.FechaApertura >= desde.Value);
        }

        if (hasta.HasValue)
        {
            query = query.Where(t => t.FechaApertura <= hasta.Value);
        }

        return await query.OrderByDescending(t => t.FechaApertura).ToListAsync(ct);
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

