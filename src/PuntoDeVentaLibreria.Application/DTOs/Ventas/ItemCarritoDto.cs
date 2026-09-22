namespace PuntoDeVentaLibreria.Application.DTOs.Ventas;

public class ItemCarritoDto
{
    public Guid? ArticuloId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string? CodigoBarras { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal Cantidad { get; set; } = 1;
    public decimal PrecioUnitario { get; set; }
    public decimal PrecioCosto { get; set; }
    public decimal Subtotal => Cantidad * PrecioUnitario;
    public bool EsVentaManual { get; set; }
    public bool EsCombo { get; set; }
    public Guid? ArticuloVarianteId { get; set; }
    public string? VarianteNombre { get; set; }
}
