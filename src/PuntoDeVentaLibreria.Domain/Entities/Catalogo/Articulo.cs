using PuntoDeVentaLibreria.Domain.Common;
using PuntoDeVentaLibreria.Domain.Entities.Combos;
using PuntoDeVentaLibreria.Domain.Entities.Inventario;

namespace PuntoDeVentaLibreria.Domain.Entities.Catalogo;

public class Articulo : BaseEntity
{
    /// <summary>Código de barras comercial principal (EAN-13, UPC o ISBN)</summary>
    public string? CodigoBarras { get; set; }

    /// <summary>Códigos de barra adicionales para variantes del mismo producto (ej. colores azul, rojo, amarillo de un cuaderno)</summary>
    public string? CodigosBarrasSecundarios { get; set; }

    /// <summary>Código interno / SKU para búsqueda rápida</summary>
    public string SKU { get; set; } = string.Empty;

    /// <summary>Código del producto según el catálogo o factura del proveedor (ej. 31030 de El Once)</summary>
    public string? CodigoProveedor { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    public Guid? CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }

    public Guid? MarcaId { get; set; }
    public Marca? Marca { get; set; }

    public Guid? ProveedorId { get; set; }
    public PuntoDeVentaLibreria.Domain.Entities.Proveedores.Proveedor? Proveedor { get; set; }

    public TipoArticulo Tipo { get; set; } = TipoArticulo.Estandar;

    // Precios y Márgenes
    public decimal PrecioCosto { get; set; }
    public decimal IvaPorcentaje { get; set; } = 21.0m;
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

    /// <summary>Rubro comercial principal: 'Librería' o 'Regalería'</summary>
    public string Rubro { get; set; } = "Librería";

    /// <summary>Indica si este artículo es un pack o caja cerrada que fracciona existencias de un artículo base</summary>
    public bool EsPack { get; set; } = false;
    public Guid? ArticuloBaseId { get; set; }
    public Articulo? ArticuloBase { get; set; }
    public decimal CantidadPorPack { get; set; } = 1;

    /// <summary>Fecha y hora en que se realizó la última auditoría o conteo físico de stock</summary>
    public DateTime? UltimaAuditoriaStock { get; set; }

    // Relaciones
    public ICollection<MovimientoStock> MovimientosStock { get; set; } = new List<MovimientoStock>();
    public ICollection<ComboItem> ComoComponenteEnCombos { get; set; } = new List<ComboItem>();
    public ICollection<ComboItem> ItemsDelCombo { get; set; } = new List<ComboItem>();
    public ICollection<ArticuloVariante> Variantes { get; set; } = new List<ArticuloVariante>();

    /// <summary>Indica si el artículo posee variantes activas con stock propio (colores, modelos)</summary>
    public bool TieneVariantes => Variantes != null && Variantes.Any(v => v.Activo);
}
