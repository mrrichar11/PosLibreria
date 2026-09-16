namespace PuntoDeVentaLibreria.Application.DTOs.Sistema;

public class ActualizacionDto
{
    public bool HayActualizacion { get; set; }
    public string VersionActual { get; set; } = "1.0.0";
    public string VersionDisponible { get; set; } = "1.0.0";
    public string UrlDescargaZip { get; set; } = string.Empty;
    public string NotasLanzamiento { get; set; } = string.Empty;
    public DateTime? FechaPublicacion { get; set; }
}

public class ProgresoDescargaDto
{
    public double Porcentaje { get; set; }
    public long BytesRecibidos { get; set; }
    public long? TotalBytes { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}
