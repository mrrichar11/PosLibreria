using PuntoDeVentaLibreria.Domain.Common;

namespace PuntoDeVentaLibreria.Domain.Entities.Catalogo;

public class ArticuloVariante : BaseEntity
{
    public Guid ArticuloId { get; set; }
    public Articulo Articulo { get; set; } = null!;

    /// <summary>Nombre de la variante / color (ej. 'Azul', 'Rojo', 'Pastel Verde')</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Código de barras específico de esta variante / color</summary>
    public string? CodigoBarras { get; set; }

    /// <summary>Código de proveedor específico de esta variante (opcional)</summary>
    public string? CodigoProveedor { get; set; }

    /// <summary>Stock actual disponible exclusivamente de esta variante / color</summary>
    public decimal StockActual { get; set; }

    /// <summary>Stock mínimo para disparar alerta de reposición de este color</summary>
    public decimal StockMinimo { get; set; } = 2;

    /// <summary>Fecha y hora en que se realizó el último conteo físico de este color</summary>
    public DateTime? UltimaAuditoriaStock { get; set; }
}
