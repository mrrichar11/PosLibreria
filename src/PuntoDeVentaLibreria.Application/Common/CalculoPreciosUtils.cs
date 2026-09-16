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

    public static decimal CalcularMargenPorcentaje(decimal costo, decimal precioVenta)
    {
        if (costo <= 0) return 0;
        return Math.Round(((precioVenta - costo) / costo) * 100m, 1);
    }
}
