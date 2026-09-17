namespace PuntoDeVentaLibreria.Application.DTOs.Inventario;

public class MigracionResultadoDto
{
    public int TotalFilasProcesadas { get; set; }
    public int ArticulosCreados { get; set; }
    public int ArticulosActualizados { get; set; }
    public int Errores { get; set; }
    public List<string> MensajesErrores { get; set; } = new();
}

public class ItemPrevisualizacionAlmaLibreDto
{
    public string SKU { get; set; } = string.Empty;
    public string? CodigoProveedor { get; set; }
    public string? CodigoBarras { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? CategoriaRubro { get; set; }
    public decimal PrecioCosto { get; set; }
    public decimal IvaPorcentaje { get; set; }
    public decimal PorcentajeGanancia { get; set; }
    public decimal PrecioVenta { get; set; }
    public bool YaExisteEnSistema { get; set; }
}
