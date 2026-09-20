namespace PuntoDeVentaLibreria.Application.DTOs.Seguridad;

public class EstadoLicenciaDto
{
    public bool EsValida { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public DateTime? FechaExpiracion { get; set; }
    public int DiasRestantes => FechaExpiracion.HasValue ? Math.Max(0, (FechaExpiracion.Value.Date - DateTime.UtcNow.Date).Days) : 0;
    public string CodigoInstalacion { get; set; } = string.Empty;
    public string PlanNombre { get; set; } = "Plan Estándar (Prueba de 30 Días)";
    public bool EsPlanPro { get; set; }
    public string TipoPlan => EsPlanPro ? "PRO Multi-Terminal (Nube)" : "Estándar (1 PC Local)";
    public string TelefonoSoporteWhatsApp { get; set; } = "+54 9 3493 495801";
    public string EnlaceWhatsApp => "https://wa.me/5493493495801?text=Hola,%20quisiera%20activar/renovar%20el%20Plan%20de%20MR%20SYS%20Librer%C3%ADa%20para%20mi%20negocio";
}

public class ResultadoActivacionDto
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public DateTime? NuevaFechaExpiracion { get; set; }
    public bool EsPlanPro { get; set; }
}
