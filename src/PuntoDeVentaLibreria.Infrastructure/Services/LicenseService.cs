using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PuntoDeVentaLibreria.Application.DTOs.Seguridad;
using PuntoDeVentaLibreria.Application.Services;
using PuntoDeVentaLibreria.Domain.Entities.Seguridad;
using PuntoDeVentaLibreria.Infrastructure.Data;

namespace PuntoDeVentaLibreria.Infrastructure.Services;

public class LicenseService : ILicenseService
{
    private static readonly byte[] MasterSecretKey = Encoding.UTF8.GetBytes("MR-SYS-Libreria-Master-Key-2026-Fliac#Secured!");
    private readonly AppDbContext _context;

    public LicenseService(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<EstadoLicenciaDto> ValidarLicenciaAsync(CancellationToken cancellationToken = default)
    {
        var codigoInstalacion = ObtenerCodigoInstalacion();
        var licencia = await _context.Licencias.FirstOrDefaultAsync(cancellationToken);

        if (licencia == null)
        {
            // Crear licencia de prueba inicial (30 días de cortesía en Plan Estándar)
            licencia = new LicenciaSistema
            {
                CodigoInstalacion = codigoInstalacion,
                ClaveActivacion = "TRIAL-ESTANDAR-30",
                FechaActivacion = DateTime.UtcNow,
                FechaExpiracion = DateTime.UtcNow.AddDays(30),
                TieneModuloIA = false,
                ComercioNombre = "Librería"
            };

            _context.Licencias.Add(licencia);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var ahora = DateTime.UtcNow;
        bool esValida = licencia.FechaExpiracion > ahora;
        var dias = (licencia.FechaExpiracion.Date - ahora.Date).Days;
        bool esPro = !string.IsNullOrWhiteSpace(licencia.ClaveActivacion) && 
                     (licencia.ClaveActivacion.Contains("PRO") || licencia.TieneModuloIA);

        string nombrePlan = esPro ? "Plan PRO Multi-Terminal (Nube)" : "Plan Estándar (1 PC Local)";

        string mensaje = esValida
            ? (dias <= 5 ? $"¡Atención! Su {nombrePlan} vence en {dias} días. Contacte soporte por WhatsApp al +54 9 3493 495801 para renovar." : $"{nombrePlan} activo y verificado.")
            : "Su período de suscripción ha expirado. Ingrese una clave de activación o solicítela por WhatsApp al +54 9 3493 495801.";

        return new EstadoLicenciaDto
        {
            EsValida = esValida,
            Mensaje = mensaje,
            FechaExpiracion = licencia.FechaExpiracion,
            CodigoInstalacion = codigoInstalacion,
            PlanNombre = nombrePlan,
            EsPlanPro = esPro
        };
    }

    public async Task<ResultadoActivacionDto> ActivarLicenciaAsync(string claveActivacion, CancellationToken cancellationToken = default)
    {
        var clave = claveActivacion?.Trim().ToUpperInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(clave))
            return new ResultadoActivacionDto { Exitoso = false, Mensaje = "La clave no puede estar vacía." };

        var codigoInstalacion = ObtenerCodigoInstalacion();
        var licencia = await _context.Licencias.FirstOrDefaultAsync(cancellationToken);
        if (licencia == null)
        {
            licencia = new LicenciaSistema
            {
                CodigoInstalacion = codigoInstalacion
            };
            _context.Licencias.Add(licencia);
        }

        // Validación criptográfica por ID de máquina
        var (esValida, plan, dias, mensaje) = LicenseCryptography.ValidarClave(clave, codigoInstalacion);
        if (!esValida && !string.IsNullOrWhiteSpace(licencia.CodigoInstalacion) && licencia.CodigoInstalacion != codigoInstalacion)
        {
            // Probar también contra el código de instalación registrado en la base compartida
            (esValida, plan, dias, mensaje) = LicenseCryptography.ValidarClave(clave, licencia.CodigoInstalacion);
        }

        if (!esValida)
        {
            return new ResultadoActivacionDto
            {
                Exitoso = false,
                Mensaje = mensaje
            };
        }

        bool esPro = plan == "PRO";
        licencia.TieneModuloIA = esPro;
        licencia.ClaveActivacion = clave;
        licencia.FechaActivacion = DateTime.UtcNow;
        licencia.FechaExpiracion = (licencia.FechaExpiracion > DateTime.UtcNow ? licencia.FechaExpiracion : DateTime.UtcNow).AddDays(dias);

        await _context.SaveChangesAsync(cancellationToken);

        string nombrePlan = esPro ? "Plan PRO Multi-Terminal" : "Plan Estándar";

        return new ResultadoActivacionDto
        {
            Exitoso = true,
            Mensaje = $"¡{nombrePlan} activado con éxito por {dias} días! Vigente hasta el {licencia.FechaExpiracion:dd/MM/yyyy}.",
            NuevaFechaExpiracion = licencia.FechaExpiracion,
            EsPlanPro = esPro
        };
    }

    public string ObtenerCodigoInstalacion()
    {
        try
        {
            var raw = $"{Environment.MachineName}-{Environment.UserName}-{Environment.ProcessorCount}";
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            var hex = Convert.ToHexString(hash).Substring(0, 16);
            return $"LIB-{hex.Substring(0, 4)}-{hex.Substring(4, 4)}-{hex.Substring(8, 4)}-{hex.Substring(12, 4)}";
        }
        catch
        {
            return "LIB-8842-9910-1420-5544";
        }
    }
}
