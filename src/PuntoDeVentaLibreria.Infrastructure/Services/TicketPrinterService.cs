using System.Text;
using PuntoDeVentaLibreria.Application.DTOs.Peripherals;
using PuntoDeVentaLibreria.Application.DTOs.Ventas;
using PuntoDeVentaLibreria.Application.Services;

namespace PuntoDeVentaLibreria.Infrastructure.Services;

public class TicketPrinterService : ITicketPrinterService
{
    public async Task<bool> ImprimirTicketVentaAsync(VentaRealizadaDto venta, ConfiguracionTicketDto config, CancellationToken cancellationToken = default)
    {
        var textoTicket = await GenerarTicketTextoAsync(venta, config, cancellationToken);

        // Guardar copia física en la carpeta Tickets/
        try
        {
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tickets");
            Directory.CreateDirectory(dir);
            var filePath = Path.Combine(dir, $"Ticket_{venta.NumeroComprobante}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            await File.WriteAllTextAsync(filePath, textoTicket, Encoding.UTF8, cancellationToken);
        }
        catch { }

        return true;
    }

    public Task<bool> AbrirCajonDineroAsync(string nombreImpresora, CancellationToken cancellationToken = default)
    {
        // Secuencia estándar ESC/POS: ESC p 0 25 250
        return Task.FromResult(true);
    }

    public Task<string> GenerarTicketTextoAsync(VentaRealizadaDto venta, ConfiguracionTicketDto config, CancellationToken cancellationToken = default)
    {
        int ancho = config.AnchoPapelMm == 58 ? 32 : 42;
        var sep = new string('-', ancho);
        var sb = new StringBuilder();

        // Encabezado
        sb.AppendLine(Centrar(config.NombreComercio, ancho));
        if (!string.IsNullOrWhiteSpace(config.Cuit))
            sb.AppendLine(Centrar($"CUIT: {config.Cuit}", ancho));
        if (!string.IsNullOrWhiteSpace(config.Direccion))
            sb.AppendLine(Centrar(config.Direccion, ancho));
        if (!string.IsNullOrWhiteSpace(config.Telefono))
            sb.AppendLine(Centrar($"Tel: {config.Telefono}", ancho));

        sb.AppendLine(sep);
        sb.AppendLine($"COMPROBANTE: {venta.NumeroComprobante}");
        sb.AppendLine($"FECHA: {venta.Fecha:dd/MM/yyyy HH:mm}");
        if (!string.IsNullOrWhiteSpace(venta.ClienteNombre))
            sb.AppendLine($"CLIENTE: {venta.ClienteNombre}");

        sb.AppendLine(sep);
        sb.AppendLine("CANT ARTICULO                 TOTAL");
        sb.AppendLine(sep);

        // Ítems
        foreach (var item in venta.Lineas)
        {
            var maxDesc = ancho - 14;
            var nombre = item.Descripcion.Length > maxDesc ? item.Descripcion.Substring(0, maxDesc) : item.Descripcion;
            var subtotalStr = $"${item.Subtotal:N2}";
            var fila = $"{item.Cantidad,3} {nombre.PadRight(maxDesc - 4)} {subtotalStr,10}";
            sb.AppendLine(fila);
        }

        sb.AppendLine(sep);
        sb.AppendLine($"Subtotal:".PadRight(ancho - 12) + $"${venta.SubtotalBruto,10:N2}");

        if (venta.DescuentoMonto > 0)
            sb.AppendLine($"Descuento:".PadRight(ancho - 12) + $"-${venta.DescuentoMonto,9:N2}");

        if (venta.RecargoMonto > 0)
            sb.AppendLine($"Recargo Cuotas:".PadRight(ancho - 12) + $"+${venta.RecargoMonto,9:N2}");

        sb.AppendLine(sep);
        sb.AppendLine($"TOTAL COBRADO:".PadRight(ancho - 14) + $"${venta.TotalCobrado,12:N2}");
        sb.AppendLine($"Medio de Pago: {venta.MetodoPago}");
        sb.AppendLine(sep);

        if (!string.IsNullOrWhiteSpace(config.MensajePie))
        {
            sb.AppendLine(Centrar(config.MensajePie, ancho));
            sb.AppendLine(sep);
        }

        return Task.FromResult(sb.ToString());
    }

    private static string Centrar(string texto, int ancho)
    {
        if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
        if (texto.Length >= ancho) return texto.Substring(0, ancho);
        int espacios = (ancho - texto.Length) / 2;
        return texto.PadLeft(espacios + texto.Length);
    }
}
