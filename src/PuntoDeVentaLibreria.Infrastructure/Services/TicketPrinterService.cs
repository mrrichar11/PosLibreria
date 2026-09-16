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
            var safeNumero = string.Join("_", venta.NumeroComprobante.Split(Path.GetInvalidFileNameChars()));
            var filePath = Path.Combine(dir, $"Ticket_{safeNumero}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            await File.WriteAllTextAsync(filePath, textoTicket, Encoding.UTF8, cancellationToken);
        }
        catch { }

        return true;
    }

    public Task<bool> AbrirCajonDineroAsync(string nombreImpresora, CancellationToken cancellationToken = default)
    {
        // Secuencia estándar ESC/POS para cajón de dinero: ESC p 0 25 250
        return Task.FromResult(true);
    }

    public Task<string> GenerarTicketTextoAsync(VentaRealizadaDto venta, ConfiguracionTicketDto config, CancellationToken cancellationToken = default)
    {
        int ancho = config.AnchoPapelMm == 58 ? 32 : 44;
        var sepDoble = new string('=', ancho);
        var sepSimple = new string('-', ancho);
        var sb = new StringBuilder();

        // 1. Encabezado del Comercio
        sb.AppendLine(Centrar(config.NombreComercio.ToUpperInvariant(), ancho));
        if (!string.IsNullOrWhiteSpace(config.Cuit))
            sb.AppendLine(Centrar($"CUIT: {config.Cuit}", ancho));
        if (!string.IsNullOrWhiteSpace(config.Direccion))
            sb.AppendLine(Centrar(config.Direccion, ancho));
        if (!string.IsNullOrWhiteSpace(config.Telefono))
            sb.AppendLine(Centrar($"TEL: {config.Telefono}", ancho));

        sb.AppendLine(sepDoble);

        // 2. Datos de la Operación
        sb.AppendLine($"TICKET N°: {venta.NumeroComprobante}");
        sb.AppendLine($"FECHA:     {venta.Fecha:dd/MM/yyyy  HH:mm:ss}");
        if (!string.IsNullOrWhiteSpace(venta.VendedoraNombre))
            sb.AppendLine($"CAJERO/A:  {venta.VendedoraNombre}");
        var cliente = !string.IsNullOrWhiteSpace(venta.ClienteNombre) ? venta.ClienteNombre : "Consumidor Final";
        sb.AppendLine($"CLIENTE:   {cliente}");

        sb.AppendLine(sepSimple);

        // 3. Detalle de Ítems
        if (ancho >= 44)
        {
            // Formato 80 mm (44 columnas)
            sb.AppendLine("CANT DESCRIPCIÓN             P.UNIT    TOTAL");
            sb.AppendLine(sepSimple);

            foreach (var item in venta.Lineas)
            {
                var cantStr = item.Cantidad.ToString("0.##").PadLeft(4);
                var unitStr = $"${item.PrecioUnitario:N2}";
                var subStr = $"${item.Subtotal:N2}";
                var maxDescLen = 22;

                var desc = item.Descripcion.Trim();
                if (desc.Length <= maxDescLen)
                {
                    var descPadded = desc.PadRight(maxDescLen);
                    sb.AppendLine($"{cantStr} {descPadded} {unitStr,9} {subStr,9}");
                }
                else
                {
                    // Dividir descripción en dos líneas para evitar truncar
                    var primeraLinea = desc.Substring(0, maxDescLen);
                    var restante = desc.Substring(maxDescLen).Trim();
                    if (restante.Length > maxDescLen)
                        restante = restante.Substring(0, maxDescLen);

                    sb.AppendLine($"{cantStr} {primeraLinea.PadRight(maxDescLen)} {unitStr,9} {subStr,9}");
                    sb.AppendLine($"     {restante.PadRight(maxDescLen)}");
                }
            }
        }
        else
        {
            // Formato 58 mm (32 columnas)
            sb.AppendLine("CANT ARTÍCULO              TOTAL");
            sb.AppendLine(sepSimple);

            foreach (var item in venta.Lineas)
            {
                var cantStr = item.Cantidad.ToString("0.##").PadLeft(3);
                var subStr = $"${item.Subtotal:N2}";
                var maxDescLen = 17;

                var desc = item.Descripcion.Trim();
                if (desc.Length > maxDescLen)
                    desc = desc.Substring(0, maxDescLen);

                sb.AppendLine($"{cantStr} {desc.PadRight(maxDescLen)} {subStr,10}");
            }
        }

        sb.AppendLine(sepSimple);

        // 4. Totales
        if (venta.DescuentoMonto > 0 || venta.RecargoMonto > 0)
        {
            sb.AppendLine(AlinearExtremos("Subtotal:", $"${venta.SubtotalBruto:N2}", ancho));

            if (venta.DescuentoMonto > 0)
                sb.AppendLine(AlinearExtremos("Descuento Efectivo:", $"-${venta.DescuentoMonto:N2}", ancho));

            if (venta.RecargoMonto > 0)
                sb.AppendLine(AlinearExtremos("Recargo Cuotas:", $"+${venta.RecargoMonto:N2}", ancho));

            sb.AppendLine(sepSimple);
        }

        sb.AppendLine(sepDoble);
        sb.AppendLine(AlinearExtremos("TOTAL COBRADO:", $"${venta.TotalCobrado:N2}", ancho));
        sb.AppendLine(sepDoble);

        // 5. Forma de Pago y Desglose
        var medioTexto = FormatearMedioPago(venta.MetodoPago);
        sb.AppendLine($"FORMA DE PAGO: {medioTexto}");

        if (venta.MetodoPago == "Efectivo" && venta.MontoEntregado > 0)
        {
            sb.AppendLine(AlinearExtremos("Dinero Recibido:", $"${venta.MontoEntregado:N2}", ancho));
            sb.AppendLine(AlinearExtremos("SU VUELTO:", $"${venta.Vuelto:N2}", ancho));
        }
        else if (venta.MetodoPago == "CtaCte")
        {
            sb.AppendLine(Centrar("*** CUENTA CORRIENTE (FIADO) ***", ancho));
        }
        else if (!string.IsNullOrWhiteSpace(venta.ReferenciaPago))
        {
            sb.AppendLine($"Ref/Comprobante: {venta.ReferenciaPago}");
        }

        sb.AppendLine(sepSimple);

        // 6. Mensaje de Pie Configurable
        var mensajePie = !string.IsNullOrWhiteSpace(config.MensajePie) 
            ? config.MensajePie 
            : "¡Muchas gracias por su compra!\nCambios con ticket dentro de los 15 días.";

        var lineasPie = mensajePie.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var linea in lineasPie)
        {
            sb.AppendLine(Centrar(linea.Trim(), ancho));
        }

        sb.AppendLine(sepSimple);
        sb.AppendLine(Centrar("*** MR SYS - SISTEMA DE VENTAS ***", ancho));

        return Task.FromResult(sb.ToString());
    }

    private static string FormatearMedioPago(string metodo)
    {
        return metodo switch
        {
            "Efectivo" => "EFECTIVO",
            "Debito" => "TARJETA DE DÉBITO",
            "Credito" => "TARJETA DE CRÉDITO",
            "Transferencia" => "TRANSFERENCIA BANCARIA / QR",
            "CtaCte" => "CTA. CTE. (FIADO)",
            _ => metodo.ToUpperInvariant()
        };
    }

    private static string Centrar(string texto, int ancho)
    {
        if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
        if (texto.Length >= ancho) return texto.Substring(0, ancho);
        int espacios = (ancho - texto.Length) / 2;
        return texto.PadLeft(espacios + texto.Length);
    }

    private static string AlinearExtremos(string izquierda, string derecha, int ancho)
    {
        int espacios = ancho - izquierda.Length - derecha.Length;
        if (espacios < 1) espacios = 1;
        return izquierda + new string(' ', espacios) + derecha;
    }
}
