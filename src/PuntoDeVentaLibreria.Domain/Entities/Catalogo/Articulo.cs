using PuntoDeVentaLibreria.Domain.Common;
using PuntoDeVentaLibreria.Domain.Entities.Combos;
using PuntoDeVentaLibreria.Domain.Entities.Inventario;

namespace PuntoDeVentaLibreria.Domain.Entities.Catalogo;

public class Articulo : BaseEntity
{
    /// <summary>Código de barras comercial (EAN-13, UPC o ISBN)</summary>
    public string? CodigoBarras { get; set; }

    /// <summary>Código interno / SKU para búsqueda rápida</summary>
    public string SKU { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    public Guid? CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }

    public Guid? MarcaId { get; set; }
    public Marca? Marca { get; set; }

    public TipoArticulo Tipo { get; set; } = TipoArticulo.Estandar;

    // Precios y Márgenes
    public decimal PrecioCosto { get; set; }
    public decimal PorcentajeGanancia { get; set; } = 60.0m;
    public decimal PrecioVenta { get; set; }

    // Control de Inventario
    public decimal StockActual { get; set; }
    public decimal StockMinimo { get; set; } = 5;
    public string? UnidadMedida { get; set; } = "UN"; // UN, MTS, PLIEGO, HOJA

    /// <summary>Ubicación física en el local (ej. Estantería 2, Góndola Escolar)</summary>
    public string? Ubicacion { get; set; }

    /// <summary>Si es true, aparece en la botonera táctil de acceso rápido del mostrador</summary>
    public bool EsBotonRapido { get; set; }
    public string? ColorBoton { get; set; }

    // Relaciones
    public ICollection<MovimientoStock> MovimientosStock { get; set; } = new List<MovimientoStock>();
    public ICollection<ComboItem> ComoComponenteEnCombos { get; set; } = new List<ComboItem>();
    public ICollection<ComboItem> ItemsDelCombo { get; set; } = new List<ComboItem>();
}
