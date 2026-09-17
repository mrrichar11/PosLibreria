namespace PuntoDeVentaLibreria.Application.DTOs.Inventario;

public class ArticuloAumentoPrecioItemDto
{
    public Guid ArticuloId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? CodigoProveedor { get; set; }
    public string? CodigoBarras { get; set; }
    public decimal CostoAnterior { get; set; }
    public decimal CostoNuevo { get; set; }
    public decimal VentaAnterior { get; set; }
    public decimal VentaNueva { get; set; }
    public decimal PorcentajeGanancia { get; set; }
    public decimal IvaPorcentaje { get; set; }
    public decimal VariacionPorcentaje => CostoAnterior > 0 
        ? Math.Round(((CostoNuevo - CostoAnterior) / CostoAnterior) * 100m, 2) 
        : 0m;
    public bool Aplicar { get; set; } = true;
}

public class ResumenPrevisualizacionAumentoDto
{
    public int CoincidenciasEncontradas { get; set; }
    public int CoincidenciasConCambioDePrecio { get; set; }
    public int NoEncontradosEnCatalogo { get; set; }
    public List<ArticuloAumentoPrecioItemDto> ItemsParaActualizar { get; set; } = new();
}

public class ActualizacionPreciosResultadoDto
{
    public int TotalActualizados { get; set; }
    public int Errores { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}
