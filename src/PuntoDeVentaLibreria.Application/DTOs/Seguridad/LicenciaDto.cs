namespace PuntoDeVentaLibreria.Application.DTOs.Seguridad;

public class EstadoLicenciaDto
{
    public bool EsValida { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public DateTime? FechaExpiracion { get; set; }
    public int DiasRestantes => FechaExpiracion.HasValue ? Math.Max(0, (FechaExpiracion.Value.Date - DateTime.UtcNow.Date).Days) : 0;
    public string CodigoInstalacion { get; set; } = string.Empty;
    public string PlanNombre { get; set; } = "Plan Mensual Librería PRO";
}

public class ResultadoActivacionDto
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public DateTime? NuevaFechaExpiracion { get; set; }
}
