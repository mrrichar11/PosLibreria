namespace PuntoDeVentaLibreria.Application.DTOs.Proveedores;

public class ProveedorDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? RazonSocial { get; set; }
    public string? Cuit { get; set; }
    public string? Contacto { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public string? DiasVisitaOEntrega { get; set; }
    public string? Notas { get; set; }
    public int CantidadArticulos { get; set; }
}
