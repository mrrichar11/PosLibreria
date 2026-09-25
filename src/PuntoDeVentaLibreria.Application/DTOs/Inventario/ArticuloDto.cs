using PuntoDeVentaLibreria.Domain.Entities.Catalogo;

namespace PuntoDeVentaLibreria.Application.DTOs.Inventario;

public class ArticuloDto
{
    public Guid Id { get; set; }
    public string? CodigoBarras { get; set; }
    public string? CodigosBarrasSecundarios { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string? CodigoProveedor { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public Guid? CategoriaId { get; set; }
    public string CategoriaNombre { get; set; } = string.Empty;
    public Guid? MarcaId { get; set; }
    public string MarcaNombre { get; set; } = string.Empty;
    public Guid? ProveedorId { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;
    public TipoArticulo Tipo { get; set; }
    public decimal PrecioCosto { get; set; }
    public decimal IvaPorcentaje { get; set; } = 21.0m;
    public decimal PorcentajeGanancia { get; set; }
    public decimal PrecioVenta { get; set; }
    public bool EsPrecioDolar { get; set; } = false;
    public decimal PrecioCostoDolar { get; set; } = 0.0m;
    public decimal StockActual { get; set; }
    public decimal StockMinimo { get; set; }
    public string? UnidadMedida { get; set; }
    public string? Ubicacion { get; set; }
    public bool EsBotonRapido { get; set; }
    public string? ColorBoton { get; set; }

    // Rubro (Librería vs Regalería)
    public string Rubro { get; set; } = "Librería";

    // Pack / Caja Fraccionable
    public bool EsPack { get; set; } = false;
    public Guid? ArticuloBaseId { get; set; }
    public string ArticuloBaseNombre { get; set; } = string.Empty;
    public decimal CantidadPorPack { get; set; } = 1;

    // Auditoría / Conteo de Stock
    public DateTime? UltimaAuditoriaStock { get; set; }
    public bool YaAuditado => UltimaAuditoriaStock.HasValue;

    // Variantes / Colores con stock propio
    public List<ArticuloVarianteDto> Variantes { get; set; } = new();
    public bool TieneVariantes => Variantes != null && Variantes.Any(v => v.Activo);
    public ArticuloVarianteDto? VarianteEscaneada { get; set; }
    public string VariantesResumenTexto
    {
        get
        {
            if (!TieneVariantes) return string.Empty;
            var activos = Variantes.Where(v => v.Activo).ToList();
            return $"{activos.Count} colores: " + string.Join(", ", activos.Select(v => $"{v.StockActual:N0} {v.Nombre}"));
        }
    }

    public System.Collections.ObjectModel.ObservableCollection<ComboComponenteDto> ComponentesDelCombo { get; set; } = new();
}

public class ComboComponenteDto
{
    public Guid ComponenteArticuloId { get; set; }
    public string NombreArticulo { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public decimal StockActualDisponible { get; set; }
}
