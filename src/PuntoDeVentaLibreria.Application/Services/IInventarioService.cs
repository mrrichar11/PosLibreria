using PuntoDeVentaLibreria.Application.DTOs.Inventario;

namespace PuntoDeVentaLibreria.Application.Services;

public interface IInventarioService
{
    Task<IReadOnlyList<ArticuloDto>> BuscarArticulosAsync(string criterio, CancellationToken ct = default);
    Task<ArticuloDto?> BuscarPorCodigoBarrasAsync(string codigoBarras, CancellationToken ct = default);
    Task<IReadOnlyList<ArticuloDto>> ObtenerBotonesRapidosAsync(CancellationToken ct = default);
    Task<ArticuloDto> GuardarArticuloAsync(ArticuloDto dto, CancellationToken ct = default);
    Task<string> ExportarCatalogoCsvAsync(CancellationToken ct = default);
    Task<(int Creados, int Actualizados, int Errores)> ImportarCatalogoCsvAsync(string contenidoCsv, CancellationToken ct = default);
    Task<bool> EliminarArticuloAsync(Guid articuloId, CancellationToken ct = default);
    Task<string> GenerarSkuSugeridoAsync(CancellationToken ct = default);
    Task<string> GenerarCodigoBarrasSugeridoAsync(CancellationToken ct = default);

    // Migración e importación masiva de librerías anteriores (ej. Alma Libre / Sixtored)
    Task<IReadOnlyList<ItemPrevisualizacionAlmaLibreDto>> PrevisualizarCatalogoAlmaLibreAsync(Stream archivoExcelStream, CancellationToken ct = default);
    Task<MigracionResultadoDto> ImportarCatalogoAlmaLibreAsync(Stream archivoExcelStream, Guid? proveedorId = null, CancellationToken ct = default);

    // Actualizador masivo de precios desde listas de mayoristas (ej. El Once)
    Task<ResumenPrevisualizacionAumentoDto> PrevisualizarActualizacionPreciosProveedorAsync(Stream archivoExcelStream, Guid? proveedorId = null, CancellationToken ct = default);
    Task<ActualizacionPreciosResultadoDto> AplicarActualizacionPreciosAsync(IEnumerable<ArticuloAumentoPrecioItemDto> items, CancellationToken ct = default);
}
