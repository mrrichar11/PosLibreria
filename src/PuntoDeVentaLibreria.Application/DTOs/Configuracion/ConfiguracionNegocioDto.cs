namespace PuntoDeVentaLibreria.Application.DTOs.Configuracion;

public class ConfiguracionNegocioDto
{
    public Guid Id { get; set; }
    public string NombreComercio { get; set; } = "Librería & Regalería MR";
    public string Direccion { get; set; } = "Av. San Martín 450";
    public string Telefono { get; set; } = "+54 9 11 0000-0000";
    public string Cuit { get; set; } = "20-00000000-0";
    public string VendedoraDefecto { get; set; } = "Cajero Principal";
    public string? LogoRuta { get; set; }

    public decimal PorcentajeDescuentoEfectivo { get; set; } = 10.0m;
    public decimal ComisionTarjetaDebito { get; set; } = 1.5m;
    public decimal ComisionTarjetaCredito { get; set; } = 4.5m;
    public decimal RecargoCuotasTarjetaCredito { get; set; } = 15.0m;

    public bool Habilitar3Cuotas { get; set; } = true;
    public decimal Recargo3Cuotas { get; set; } = 15.0m;
    public bool Habilitar6Cuotas { get; set; } = true;
    public decimal Recargo6Cuotas { get; set; } = 25.0m;

    public decimal MargenGananciaSugerido { get; set; } = 65.0m;
    public decimal TopeFiadoDefecto { get; set; } = 60000m;
    public decimal TopeMensualRetiroDueño { get; set; } = 700000m;

    public string TemaInterfaz { get; set; } = "Light";
    public string Pais { get; set; } = "Argentina";
    public string SimboloMoneda { get; set; } = "$";
    public string BilletesHabilitados { get; set; } = "100,200,500,1000,2000,10000,20000";

    public string ImpresoraTickets { get; set; } = string.Empty;
    public int AnchoPapelMm { get; set; } = 80;
    public string MensajePieTicket { get; set; } = "¡Muchas gracias por su compra!\nCambios con ticket dentro de los 15 días.\nNo se aceptan cambios de fotocopias.";
    public bool ImprimirAutomaticoAlCobrar { get; set; } = false;
    public bool MostrarVistaPreviaTicket { get; set; } = true;

    public string GitHubRepoOwner { get; set; } = "mrrichar11";
    public string GitHubRepoName { get; set; } = "puntoVentaLibreriaMR";
}

public class EstadoRetirosDueñoDto
{
    public decimal TopeMensual { get; set; }
    public decimal TotalRetiradoMes { get; set; }
    public decimal SaldoDisponible => Math.Max(0m, TopeMensual - TotalRetiradoMes);
    public decimal PorcentajeConsumido => TopeMensual > 0 ? Math.Min(100m, Math.Round((TotalRetiradoMes / TopeMensual) * 100m, 1)) : 0m;
    public bool HaSuperadoTope => TotalRetiradoMes > TopeMensual;
}
