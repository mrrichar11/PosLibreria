namespace PuntoDeVentaLibreria.Domain.Entities.Auditoria;

public class ArticuloHistoricoSistemaAnterior
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Codigo { get; set; } = string.Empty;
    public string? CodigoProveedor { get; set; }
    public string? CodigoBarras { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal PrecioVenta { get; set; }
    public decimal PrecioLista { get; set; }
    public decimal PrecioCosto { get; set; }
    public string? Rubro { get; set; }
    public string? SubRubro { get; set; }
    public string? Proveedor { get; set; }
    public DateTime FechaCarga { get; set; } = DateTime.Now;
    public string ArchivoOrigen { get; set; } = string.Empty;
}
