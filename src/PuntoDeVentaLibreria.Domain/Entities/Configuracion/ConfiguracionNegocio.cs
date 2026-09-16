using PuntoDeVentaLibreria.Domain.Common;

namespace PuntoDeVentaLibreria.Domain.Entities.Configuracion;

public class ConfiguracionNegocio : BaseEntity
{
    public string NombreComercio { get; set; } = "Librería & Regalería MR";
    public string Direccion { get; set; } = "Av. San Martín 450";
    public string Telefono { get; set; } = "+54 9 11 0000-0000";
    public string Cuit { get; set; } = "20-00000000-0";
    public string VendedoraDefecto { get; set; } = "Cajero Principal";
    public string? LogoRuta { get; set; }

    // Políticas Financieras y Sugerencia de Precios
    public decimal PorcentajeDescuentoEfectivo { get; set; } = 10.0m;
    public decimal ComisionTarjetaDebito { get; set; } = 1.5m;
    public decimal ComisionTarjetaCredito { get; set; } = 4.5m;
    public decimal RecargoCuotasTarjetaCredito { get; set; } = 15.0m;
    public decimal MargenGananciaSugerido { get; set; } = 65.0m;

    // Cuotas Diferenciadas
    public bool Habilitar3Cuotas { get; set; } = true;
    public decimal Recargo3Cuotas { get; set; } = 15.0m;
    public bool Habilitar6Cuotas { get; set; } = true;
    public decimal Recargo6Cuotas { get; set; } = 25.0m;

    public decimal TopeFiadoDefecto { get; set; } = 60000m;
    public decimal TopeMensualRetiroDueño { get; set; } = 700000m;
    public string TemaInterfaz { get; set; } = "Light";

    // Región, Moneda y Denominaciones de Billetes
    public string Pais { get; set; } = "Argentina";
    public string SimboloMoneda { get; set; } = "$";
    public string BilletesHabilitados { get; set; } = "100,200,500,1000,2000,10000,20000";

    // Actualizaciones Oficiales Silenciosas
    public string GitHubRepoOwner { get; set; } = "mrrichar11";
    public string GitHubRepoName { get; set; } = "puntoVentaLibreriaMR";
}
