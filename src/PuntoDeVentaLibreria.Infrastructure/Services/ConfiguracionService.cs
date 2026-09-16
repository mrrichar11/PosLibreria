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
            GitHubRepoOwner = config.GitHubRepoOwner,
            GitHubRepoName = config.GitHubRepoName
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
        config.GitHubRepoOwner = dto.GitHubRepoOwner.Trim();
        config.GitHubRepoName = dto.GitHubRepoName.Trim();

        await _context.SaveChangesAsync(cancellationToken);
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
