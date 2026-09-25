using Microsoft.EntityFrameworkCore;
using PuntoDeVentaLibreria.Application.DTOs.Configuracion;
using PuntoDeVentaLibreria.Application.Services;
using PuntoDeVentaLibreria.Domain.Entities.Configuracion;
using PuntoDeVentaLibreria.Domain.Entities.Finanzas;
using PuntoDeVentaLibreria.Infrastructure.Data;

namespace PuntoDeVentaLibreria.Infrastructure.Services;

public class ConfiguracionService : IConfiguracionService
{
    private readonly AppDbContext _context;

    public ConfiguracionService(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<ConfiguracionNegocioDto> ObtenerConfiguracionAsync(CancellationToken cancellationToken = default)
    {
        var config = await _context.Configuraciones.FirstOrDefaultAsync(cancellationToken);
        if (config == null)
        {
            config = new ConfiguracionNegocio();
            _context.Configuraciones.Add(config);
            await _context.SaveChangesAsync(cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(config.GitHubRepoName) || config.GitHubRepoName == "puntoVentaLibreriaMR")
        {
            config.GitHubRepoName = "PosLibreria";
            config.GitHubRepoOwner = string.IsNullOrWhiteSpace(config.GitHubRepoOwner) ? "mrrichar11" : config.GitHubRepoOwner;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new ConfiguracionNegocioDto
        {
            Id = config.Id,
            NombreComercio = config.NombreComercio,
            Direccion = config.Direccion,
            Telefono = config.Telefono,
            Cuit = config.Cuit,
            VendedoraDefecto = config.VendedoraDefecto,
            LogoRuta = config.LogoRuta,
            PorcentajeDescuentoEfectivo = config.PorcentajeDescuentoEfectivo,
            ComisionTarjetaDebito = config.ComisionTarjetaDebito,
            ComisionTarjetaCredito = config.ComisionTarjetaCredito,
            RecargoCuotasTarjetaCredito = config.RecargoCuotasTarjetaCredito,
            Habilitar3Cuotas = config.Habilitar3Cuotas,
            Recargo3Cuotas = config.Recargo3Cuotas,
            Habilitar6Cuotas = config.Habilitar6Cuotas,
            Recargo6Cuotas = config.Recargo6Cuotas,
            MargenGananciaSugerido = config.MargenGananciaSugerido,
            TopeFiadoDefecto = config.TopeFiadoDefecto,
            TopeMensualRetiroDueño = config.TopeMensualRetiroDueño,
            TemaInterfaz = config.TemaInterfaz,
            Pais = config.Pais,
            SimboloMoneda = config.SimboloMoneda,
            BilletesHabilitados = config.BilletesHabilitados,
            ImpresoraTickets = config.ImpresoraTickets,
            AnchoPapelMm = config.AnchoPapelMm,
            MensajePieTicket = config.MensajePieTicket,
            ImprimirAutomaticoAlCobrar = config.ImprimirAutomaticoAlCobrar,
            MostrarVistaPreviaTicket = config.MostrarVistaPreviaTicket,
            GitHubRepoOwner = config.GitHubRepoOwner,
            GitHubRepoName = config.GitHubRepoName,
            CarpetaBackupsPersonalizada = config.CarpetaBackupsPersonalizada,
            BackupAutomaticoAlCerrarSistema = config.BackupAutomaticoAlCerrarSistema,
            BackupAutomaticoAlCierreCaja = config.BackupAutomaticoAlCierreCaja,
            DiasRetencionBackups = config.DiasRetencionBackups,
            NombreTerminal = config.NombreTerminal,
            ModoCajaMultiTerminal = config.ModoCajaMultiTerminal,
            MotorBaseDatos = config.MotorBaseDatos,
            ServidorPostgres = config.ServidorPostgres,
            PuertoPostgres = config.PuertoPostgres,
            BaseDatosPostgres = config.BaseDatosPostgres,
            UsuarioPostgres = config.UsuarioPostgres,
            PasswordPostgres = config.PasswordPostgres,
            CotizacionDolar = config.CotizacionDolar > 0 ? config.CotizacionDolar : 1350m,
            FechaCotizacionDolar = config.FechaCotizacionDolar ?? DateTime.Now
        };
    }

    public async Task GuardarConfiguracionAsync(ConfiguracionNegocioDto dto, CancellationToken cancellationToken = default)
    {
        var config = await _context.Configuraciones.FirstOrDefaultAsync(cancellationToken);
        if (config == null)
        {
            config = new ConfiguracionNegocio();
            _context.Configuraciones.Add(config);
        }

        config.NombreComercio = dto.NombreComercio.Trim();
        config.Direccion = dto.Direccion.Trim();
        config.Telefono = dto.Telefono.Trim();
        config.Cuit = dto.Cuit.Trim();
        config.VendedoraDefecto = dto.VendedoraDefecto.Trim();
        config.LogoRuta = dto.LogoRuta;
        config.PorcentajeDescuentoEfectivo = dto.PorcentajeDescuentoEfectivo;
        config.ComisionTarjetaDebito = dto.ComisionTarjetaDebito;
        config.ComisionTarjetaCredito = dto.ComisionTarjetaCredito;
        config.RecargoCuotasTarjetaCredito = dto.RecargoCuotasTarjetaCredito;
        config.Habilitar3Cuotas = dto.Habilitar3Cuotas;
        config.Recargo3Cuotas = dto.Recargo3Cuotas;
        config.Habilitar6Cuotas = dto.Habilitar6Cuotas;
        config.Recargo6Cuotas = dto.Recargo6Cuotas;
        config.MargenGananciaSugerido = dto.MargenGananciaSugerido;
        config.TopeFiadoDefecto = dto.TopeFiadoDefecto;
        config.TopeMensualRetiroDueño = dto.TopeMensualRetiroDueño;
        config.TemaInterfaz = dto.TemaInterfaz;
        config.Pais = string.IsNullOrWhiteSpace(dto.Pais) ? "Argentina" : dto.Pais.Trim();
        config.SimboloMoneda = string.IsNullOrWhiteSpace(dto.SimboloMoneda) ? "$" : dto.SimboloMoneda.Trim();
        config.BilletesHabilitados = string.IsNullOrWhiteSpace(dto.BilletesHabilitados) ? "100,200,500,1000,2000,10000,20000" : dto.BilletesHabilitados.Trim();
        config.ImpresoraTickets = dto.ImpresoraTickets ?? string.Empty;
        config.AnchoPapelMm = dto.AnchoPapelMm == 58 ? 58 : 80;
        config.MensajePieTicket = string.IsNullOrWhiteSpace(dto.MensajePieTicket) ? "¡Muchas gracias por su compra!" : dto.MensajePieTicket;
        config.ImprimirAutomaticoAlCobrar = dto.ImprimirAutomaticoAlCobrar;
        config.MostrarVistaPreviaTicket = dto.MostrarVistaPreviaTicket;
        config.GitHubRepoOwner = dto.GitHubRepoOwner.Trim();
        config.GitHubRepoName = dto.GitHubRepoName.Trim();
        config.CarpetaBackupsPersonalizada = dto.CarpetaBackupsPersonalizada;
        config.BackupAutomaticoAlCerrarSistema = dto.BackupAutomaticoAlCerrarSistema;
        config.BackupAutomaticoAlCierreCaja = dto.BackupAutomaticoAlCierreCaja;
        config.DiasRetencionBackups = dto.DiasRetencionBackups;
        config.NombreTerminal = string.IsNullOrWhiteSpace(dto.NombreTerminal) ? "Caja Principal" : dto.NombreTerminal.Trim();
        config.ModoCajaMultiTerminal = string.IsNullOrWhiteSpace(dto.ModoCajaMultiTerminal) ? "Compartida" : dto.ModoCajaMultiTerminal.Trim();
        config.MotorBaseDatos = string.IsNullOrWhiteSpace(dto.MotorBaseDatos) ? "SQLite" : dto.MotorBaseDatos.Trim();
        config.ServidorPostgres = string.IsNullOrWhiteSpace(dto.ServidorPostgres) ? "localhost" : dto.ServidorPostgres.Trim();
        config.PuertoPostgres = dto.PuertoPostgres <= 0 ? 5432 : dto.PuertoPostgres;
        config.BaseDatosPostgres = string.IsNullOrWhiteSpace(dto.BaseDatosPostgres) ? "mr_sys_libreria" : dto.BaseDatosPostgres.Trim();
        config.UsuarioPostgres = string.IsNullOrWhiteSpace(dto.UsuarioPostgres) ? "postgres" : dto.UsuarioPostgres.Trim();
        config.PasswordPostgres = dto.PasswordPostgres ?? string.Empty;

        if (dto.CotizacionDolar > 0)
        {
            config.CotizacionDolar = dto.CotizacionDolar;
            config.FechaCotizacionDolar = DateTime.Now;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public static event Action<decimal>? CotizacionDolarCambiada;

    public async Task ActualizarCotizacionDolarAsync(decimal nuevaCotizacion, CancellationToken cancellationToken = default)
    {
        if (nuevaCotizacion <= 0) return;
        var config = await _context.Configuraciones.FirstOrDefaultAsync(cancellationToken);
        if (config == null)
        {
            config = new ConfiguracionNegocio();
            _context.Configuraciones.Add(config);
        }

        config.CotizacionDolar = nuevaCotizacion;
        config.FechaCotizacionDolar = DateTime.Now;
        await _context.SaveChangesAsync(cancellationToken);

        CotizacionDolarCambiada?.Invoke(nuevaCotizacion);
    }


    public async Task<EstadoRetirosDueñoDto> ObtenerEstadoRetirosDueñoMesAsync(CancellationToken cancellationToken = default)
    {
        var config = await ObtenerConfiguracionAsync(cancellationToken);
        var ahora = DateTime.UtcNow;
        var primerDiaMes = new DateTime(ahora.Year, ahora.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var montos = await _context.MovimientosCaja
            .Where(m => m.Tipo == TipoMovimientoCaja.RetiroDueño && m.FechaCreacion >= primerDiaMes)
            .Select(m => m.Monto)
            .ToListAsync(cancellationToken);

        var totalRetirado = montos.Sum();

        return new EstadoRetirosDueñoDto
        {
            TopeMensual = config.TopeMensualRetiroDueño,
            TotalRetiradoMes = totalRetirado
        };
    }
}
