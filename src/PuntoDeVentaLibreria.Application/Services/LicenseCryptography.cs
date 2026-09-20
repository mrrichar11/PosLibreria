using System.Security.Cryptography;
using System.Text;

namespace PuntoDeVentaLibreria.Application.Services;

public static class LicenseCryptography
{
    private static readonly byte[] MasterSecretKey = Encoding.UTF8.GetBytes("MR-SYS-Libreria-Master-Key-2026-Fliac#Secured!");

    public static string GenerarClave(string codigoInstalacion, string tipoPlan, int dias)
    {
        if (string.IsNullOrWhiteSpace(codigoInstalacion))
            throw new ArgumentException("El código de instalación no puede estar vacío.", nameof(codigoInstalacion));

        var cleanId = codigoInstalacion.Trim().ToUpperInvariant();
        var plan = (tipoPlan ?? "PRO").Trim().ToUpperInvariant();
        if (plan != "PRO" && plan != "ESTANDAR" && plan != "STD")
            plan = "PRO";

        if (dias <= 0) dias = 30;

        // Extraer segmento identificatorio de la máquina (ej: ADE3 de LIB-ADE3-...)
        var partes = cleanId.Split('-');
        var subId = partes.Length >= 2 ? partes[1] : (cleanId.Length >= 4 ? cleanId[..4] : "KEY");

        var payload = $"{cleanId}|{plan}|{dias}";
        using var hmac = new HMACSHA256(MasterSecretKey);
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var hashHex = Convert.ToHexString(hashBytes)[..8];

        return $"MRSYS-{plan}-{dias}-{subId}-{hashHex}";
    }

    public static (bool EsValida, string Plan, int Dias, string Mensaje) ValidarClave(string clave, string codigoInstalacion)
    {
        if (string.IsNullOrWhiteSpace(clave))
            return (false, string.Empty, 0, "La clave no puede estar vacía.");

        var limpia = clave.Trim().ToUpperInvariant();

        // Clave de desarrollo / override maestro si se requiere en emergencias
        if (limpia == "MRSYS-DEV-MASTER-PRO-OVERRIDE")
            return (true, "PRO", 365, "Clave maestra de desarrollador.");

        // Formato esperado: MRSYS-{PLAN}-{DIAS}-{SUBID}-{HASH}
        if (limpia.StartsWith("MRSYS-"))
            limpia = limpia["MRSYS-".Length..];

        var partes = limpia.Split('-');
        if (partes.Length < 4)
            return (false, string.Empty, 0, "El formato de la clave es inválido.");

        var plan = partes[0];
        if (!int.TryParse(partes[1], out int dias) || dias <= 0)
            return (false, string.Empty, 0, "La duración especificada en la clave no es válida.");

        var subId = partes[2];
        var hashHex = partes[3];

        var cleanId = (codigoInstalacion ?? string.Empty).Trim().ToUpperInvariant();
        var payload = $"{cleanId}|{plan}|{dias}";

        using var hmac = new HMACSHA256(MasterSecretKey);
        var expectedHashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var expectedHashHex = Convert.ToHexString(expectedHashBytes)[..8];

        if (!CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(hashHex),
            Encoding.UTF8.GetBytes(expectedHashHex)))
        {
            return (false, string.Empty, 0, "La clave no corresponde al ID de este equipo o la firma es inválida.");
        }

        return (true, plan, dias, "Clave criptográfica verificada correctamente.");
    }
}
