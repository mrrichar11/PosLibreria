namespace PuntoDeVentaLibreria.Application.DTOs.Inventario;

public class MapeoColumnasExcelDto
{
    public string? ColumnaCodigoBarras { get; set; }
    public string? ColumnaSku { get; set; }
    public string? ColumnaDescripcion { get; set; }
    public string? ColumnaPrecioVenta { get; set; }
    public string? ColumnaPrecioCosto { get; set; }
    public string? ColumnaPrecioTarjeta { get; set; }
    public string? ColumnaStock { get; set; }
    public string? ColumnaCategoria { get; set; }
    public string? ColumnaMarca { get; set; }
    public string? ColumnaCodigoProveedor { get; set; }
    public string? ColumnaFechaAlta { get; set; }
    public string? ColumnaFechaUltimoPrecio { get; set; }

    public decimal PorcentajeGananciaDefecto { get; set; } = 60m;
    public decimal IvaDefecto { get; set; } = 21m;
    public decimal StockDefecto { get; set; } = 0m;
}

public class AnalisisExcelResultadoDto
{
    public List<string> ColumnasDetectadas { get; set; } = new();
    public MapeoColumnasExcelDto MapeoSugerido { get; set; } = new();
    public List<ItemPrevisualizacionAlmaLibreDto> Items { get; set; } = new();
    public int TotalFilas { get; set; }
}
