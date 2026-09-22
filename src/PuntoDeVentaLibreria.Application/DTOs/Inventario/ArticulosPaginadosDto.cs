namespace PuntoDeVentaLibreria.Application.DTOs.Inventario;

public class ConsultaInventarioPaginadaDto
{
    public string? CriterioBusqueda { get; set; }
    public string? Rubro { get; set; } = "Todos";
    public int Pagina { get; set; } = 1;
    public int CantidadPorPagina { get; set; } = 50;
}

public class ArticulosPaginadosResultadoDto
{
    public IReadOnlyList<ArticuloDto> Items { get; set; } = new List<ArticuloDto>();
    public int TotalRegistros { get; set; }
    public int PaginaActual { get; set; } = 1;
    public int CantidadPorPagina { get; set; } = 50;
    public int TotalPaginas { get; set; } = 1;
    public int TotalArticulosGlobal { get; set; }
    public decimal ValorTotalStockGlobal { get; set; }

    public int RegistroDesde => TotalRegistros == 0 ? 0 : (CantidadPorPagina <= 0 ? 1 : ((PaginaActual - 1) * CantidadPorPagina) + 1);
    public int RegistroHasta => TotalRegistros == 0 ? 0 : (CantidadPorPagina <= 0 ? TotalRegistros : Math.Min(PaginaActual * CantidadPorPagina, TotalRegistros));
}
