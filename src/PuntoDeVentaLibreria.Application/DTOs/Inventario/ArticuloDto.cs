using PuntoDeVentaLibreria.Domain.Entities.Catalogo;

namespace PuntoDeVentaLibreria.Application.DTOs.Inventario;

public class ArticuloDto
{
    public Guid Id { get; set; }
    public string? CodigoBarras { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public Guid? CategoriaId { get; set; }
    public string CategoriaNombre { get; set; } = string.Empty;
    public Guid? MarcaId { get; set; }
    public string MarcaNombre { get; set; } = string.Empty;
    public TipoArticulo Tipo { get; set; }
    public decimal PrecioCosto { get; set; }
    public decimal PorcentajeGanancia { get; set; }
    public decimal PrecioVenta { get; set; }
    public decimal StockActual { get; set; }
    public decimal StockMinimo { get; set; }
    public string? UnidadMedida { get; set; }
    public string? Ubicacion { get; set; }
    public bool EsBotonRapido { get; set; }
    public string? ColorBoton { get; set; }
    public List<ComboComponenteDto> ComponentesDelCombo { get; set; } = new();
}

public class ComboComponenteDto
{
    public Guid ComponenteArticuloId { get; set; }
    public string NombreArticulo { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public decimal StockActualDisponible { get; set; }
}
