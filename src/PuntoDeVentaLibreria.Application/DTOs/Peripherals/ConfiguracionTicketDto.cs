namespace PuntoDeVentaLibreria.Application.DTOs.Peripherals;

public class ConfiguracionTicketDto
{
    public string NombreComercio { get; set; } = "Librería & Regalería MR";
    public string Direccion { get; set; } = "Av. San Martín 450";
    public string Telefono { get; set; } = "+54 9 11 0000-0000";
    public string Cuit { get; set; } = "20-00000000-0";
    public int AnchoPapelMm { get; set; } = 80; // 58 o 80
    public string ImpresoraNombre { get; set; } = string.Empty;
    public string MensajeEncabezado { get; set; } = "¡Bienvenidos a nuestra librería!";
    public string MensajePie { get; set; } = "Muchas gracias por su compra. Cambios con ticket dentro de los 15 días.";
    public bool CortarPapelAutomatico { get; set; } = true;
    public bool AbrirCajonDinero { get; set; } = true;
}
