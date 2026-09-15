namespace PuntoDeVentaLibreria.Domain.Entities.Catalogo;

public enum TipoArticulo
{
    /// <summary>Artículo físico estándar con código de barras o SKU (cuaderno, lapicera, mochila, adorno)</summary>
    Estandar = 1,

    /// <summary>Servicio sin stock (fotocopia, impresión, anillado, plastificado)</summary>
    Servicio = 2,

    /// <summary>Venta fraccionada / a granel (papel por pliego, cinta por metro, etc.)</summary>
    Fraccionable = 3,

    /// <summary>Kit o Combo escolar que agrupa múltiples artículos individuales</summary>
    ComboKit = 4
}
