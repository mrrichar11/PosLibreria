using System.Globalization;

namespace PuntoDeVentaLibreria.Application.Common;

public static class CalculoPreciosUtils
{
    public static bool TryParseMonto(string? input, out decimal result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(input)) return false;

        var limpio = input.Trim().Replace("$", "").Replace("%", "").Trim();
        if (string.IsNullOrWhiteSpace(limpio)) return false;

        // Si contiene tanto punto como coma (ej: 1.250,50 o 1,250.50)
        if (limpio.Contains(',') && limpio.Contains('.'))
        {
            if (limpio.IndexOf('.') < limpio.IndexOf(','))
            {
                limpio = limpio.Replace(".", "").Replace(',', '.');
            }
            else
            {
                limpio = limpio.Replace(",", "");
            }
        }
        else
        {
            // Solo tiene coma o solo tiene punto: SIEMPRE es separador decimal
            limpio = limpio.Replace(',', '.');
        }

        return decimal.TryParse(limpio, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
    }

    public static decimal CalcularPrecioVenta(decimal costo, decimal margenPorcentaje)
    {
        return Math.Round(costo * (1m + (margenPorcentaje / 100m)), 2);
    }

    public static decimal CalcularPrecioVenta(decimal costo, decimal margenPorcentaje, decimal ivaPorcentaje)
    {
        var costoConIva = costo * (1m + (ivaPorcentaje / 100m));
        return Math.Round(costoConIva * (1m + (margenPorcentaje / 100m)), 2);
    }

    public static decimal CalcularMargenPorcentaje(decimal costo, decimal precioVenta)
    {
        if (costo <= 0) return 0;
        return Math.Round(((precioVenta - costo) / costo) * 100m, 1);
    }

    public static decimal RedondearPrecioVenta(decimal precio, ReglaRedondeoPrecio regla)
    {
        if (precio <= 0) return 0;
        return regla switch
        {
            ReglaRedondeoPrecio.CentenaSuperior => Math.Ceiling(precio / 100m) * 100m,
            ReglaRedondeoPrecio.CentenaCercana => Math.Round(precio / 100m, MidpointRounding.AwayFromZero) * 100m,
            ReglaRedondeoPrecio.CincuentaCercano => Math.Round(precio / 50m, MidpointRounding.AwayFromZero) * 50m,
            _ => Math.Round(precio, 2)
        };
    }
}

public enum ReglaRedondeoPrecio
{
    CentenaCercana = 0,   // Al $100 más cercano (ej. 1240 -> 1200, 1260 -> 1300)
    CentenaSuperior = 1,  // Al $100 superior (ej. 1210 -> 1300)
    CincuentaCercano = 2, // Al $50 más cercano (ej. 1220 -> 1200, 1235 -> 1250)
    SinRedondeo = 3       // Precio exacto con decimales
}
