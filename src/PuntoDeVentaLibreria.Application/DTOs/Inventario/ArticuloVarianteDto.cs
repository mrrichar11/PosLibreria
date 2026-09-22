namespace PuntoDeVentaLibreria.Application.DTOs.Inventario;

public class ArticuloVarianteDto
{
    public Guid Id { get; set; }
    public Guid ArticuloId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? CodigoBarras { get; set; }
    public string? CodigoProveedor { get; set; }
    public decimal StockActual { get; set; }
    public decimal StockMinimo { get; set; } = 2;
    public bool Activo { get; set; } = true;
    public DateTime? UltimaAuditoriaStock { get; set; }

    public bool StockBajo => StockActual <= StockMinimo;
    public bool Agotado => StockActual <= 0;
}
