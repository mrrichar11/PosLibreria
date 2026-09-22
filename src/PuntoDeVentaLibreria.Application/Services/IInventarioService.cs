using PuntoDeVentaLibreria.Application.DTOs.Inventario;

namespace PuntoDeVentaLibreria.Application.Services;

public interface IInventarioService
{
    Task<IReadOnlyList<ArticuloDto>> BuscarArticulosAsync(string criterio, CancellationToken ct = default);
    Task<ArticulosPaginadosResultadoDto> ObtenerArticulosPaginadosAsync(ConsultaInventarioPaginadaDto consulta, CancellationToken ct = default);
    Task<ArticuloDto?> BuscarPorCodigoBarrasAsync(string codigoBarras, CancellationToken ct = default);
    Task<IReadOnlyList<ArticuloDto>> ObtenerBotonesRapidosAsync(CancellationToken ct = default);
    Task<ArticuloDto> GuardarArticuloAsync(ArticuloDto dto, CancellationToken ct = default);
    Task<string> ExportarCatalogoCsvAsync(CancellationToken ct = default);
    Task<(int Creados, int Actualizados, int Errores)> ImportarCatalogoCsvAsync(string contenidoCsv, CancellationToken ct = default);
    Task<bool> EliminarArticuloAsync(Guid articuloId, CancellationToken ct = default);
    Task<string> GenerarSkuSugeridoAsync(CancellationToken ct = default);
    Task<string> GenerarCodigoBarrasSugeridoAsync(CancellationToken ct = default);

    // Migración e importación masiva universal de catálogos y sistemas anteriores
    Task<AnalisisExcelResultadoDto> AnalizarExcelGenericoAsync(Stream archivoExcelStream, MapeoColumnasExcelDto? mapeoPersonalizado = null, CancellationToken ct = default);
    Task<IReadOnlyList<ItemPrevisualizacionAlmaLibreDto>> PrevisualizarCatalogoAlmaLibreAsync(Stream archivoExcelStream, CancellationToken ct = default);
    Task<MigracionResultadoDto> ImportarCatalogoAlmaLibreAsync(Stream archivoExcelStream, Guid? proveedorId = null, CancellationToken ct = default);
    Task<MigracionResultadoDto> ImportarCatalogoSeleccionadoAsync(IReadOnlyList<ItemPrevisualizacionAlmaLibreDto> items, Guid? proveedorIdPorDefecto = null, CancellationToken ct = default);
    Task<byte[]> GenerarPlantillaExcelModeloAsync(CancellationToken ct = default);

    // Actualizador masivo de precios desde listas de mayoristas (ej. El Once)
    Task<ResumenPrevisualizacionAumentoDto> PrevisualizarActualizacionPreciosProveedorAsync(Stream archivoExcelStream, Guid? proveedorId = null, CancellationToken ct = default);
    Task<ActualizacionPreciosResultadoDto> AplicarActualizacionPreciosAsync(IEnumerable<ArticuloAumentoPrecioItemDto> items, CancellationToken ct = default);

    // Auditoría y Conteo rápido de Stock por Góndola (Sunday-Ready & Gradual)
    Task<ArticuloDto> AjustarStockRapidoAsync(Guid articuloId, decimal nuevoStock, string motivo = "Auditoría de Stock", string usuarioNombre = "Administrador", Guid? articuloVarianteId = null, CancellationToken ct = default);
    Task<AuditoriaStockProgresoDto> ObtenerProgresoAuditoriaAsync(CancellationToken ct = default);
}

