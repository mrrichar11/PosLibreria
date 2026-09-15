using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PuntoDeVentaLibreria.Application.DTOs.Inventario;
using PuntoDeVentaLibreria.Application.DTOs.Ventas;
using PuntoDeVentaLibreria.Application.Services;
using PuntoDeVentaLibreria.Domain.Entities.Catalogo;

namespace PuntoDeVentaLibreria.UI.ViewModels;

public partial class PosViewModel : ObservableObject
{
    private readonly IInventarioService _inventarioService;
    private readonly IVentaService _ventaService;
    private readonly ICajaService _cajaService;

    [ObservableProperty]
    private string _codigoBarrasInput = string.Empty;

    [ObservableProperty]
    private string _mensajeEstado = "Listo para escanear artículos...";

    [ObservableProperty]
    private bool _esCajaAbierta;

    [ObservableProperty]
    private string _usuarioActual = "Cajero";

    [ObservableProperty]
    private string _numeroComprobanteUltimaVenta = "-";

    [ObservableProperty]
    private decimal _subtotalBruto;

    [ObservableProperty]
    private decimal _totalDescuentos;

    [ObservableProperty]
    private decimal _totalVenta;

    [ObservableProperty]
    private decimal _cantidadArticulos;

    public ObservableCollection<PosItemModel> Items { get; } = new();
    public ObservableCollection<ArticuloDto> BotonesRapidos { get; } = new();

    public Guid? TurnoActivoId { get; private set; }

    // Delegado para invocar la ventana modal de cobro desde la View
    public Func<CobroModalViewModel, Task<bool>>? SolicitarCobroDialogo { get; set; }

    public PosViewModel(IInventarioService inventarioService, IVentaService ventaService, ICajaService cajaService)
    {
        _inventarioService = inventarioService ?? throw new ArgumentNullException(nameof(inventarioService));
        _ventaService = ventaService ?? throw new ArgumentNullException(nameof(ventaService));
        _cajaService = cajaService ?? throw new ArgumentNullException(nameof(cajaService));

        Items.CollectionChanged += (s, e) => RecalcularTotales();
    }

    public async Task InicializarAsync()
    {
        await RecargarCajaAsync();
        await RecargarBotonesRapidosAsync();
    }

    public async Task RecargarCajaAsync()
    {
        var turno = await _cajaService.ObtenerTurnoActivoAsync();
        if (turno == null)
        {
            // Si no hay turno abierto, abrimos uno inicial automático para no frenar la venta en mostrador
            turno = await _cajaService.AbrirTurnoAsync(10000m, UsuarioActual);
        }

        TurnoActivoId = turno.Id;
        EsCajaAbierta = true;
        MensajeEstado = $"Caja abierta activa. Inicio: {turno.FechaApertura:HH:mm}";
    }

    public async Task RecargarBotonesRapidosAsync()
    {
        BotonesRapidos.Clear();
        var lista = await _inventarioService.ObtenerBotonesRapidosAsync();
        foreach (var btn in lista)
        {
            BotonesRapidos.Add(btn);
        }
    }

    [RelayCommand]
    private async Task EscanearCodigoAsync()
    {
        var query = CodigoBarrasInput?.Trim();
        CodigoBarrasInput = string.Empty;

        if (string.IsNullOrWhiteSpace(query))
            return;

        // 1. Buscar por código de barras exacto
        var art = await _inventarioService.BuscarPorCodigoBarrasAsync(query);

        // 2. Si no encuentra, buscar por SKU o coincidencia de texto
        if (art == null)
        {
            var coincidencias = await _inventarioService.BuscarArticulosAsync(query);
            art = coincidencias.FirstOrDefault();
        }

        if (art == null)
        {
            MensajeEstado = $"No se encontró ningún artículo para: '{query}'";
            return;
        }

        AgregarArticuloAlTicket(art);
    }

    [RelayCommand]
    private void AgregarBotonRapido(ArticuloDto? articulo)
    {
        if (articulo == null) return;
        AgregarArticuloAlTicket(articulo);
    }

    private void AgregarArticuloAlTicket(ArticuloDto art)
    {
        var itemExistente = Items.FirstOrDefault(i => i.ArticuloId == art.Id);
        if (itemExistente != null)
        {
            itemExistente.Cantidad += 1;
            MensajeEstado = $"Incrementado: {art.Nombre} (x{itemExistente.Cantidad})";
            RecalcularTotales();
            return;
        }

        var nuevo = new PosItemModel
        {
            ArticuloId = art.Id,
            SKU = art.SKU,
            CodigoBarras = art.CodigoBarras,
            Descripcion = art.Nombre,
            PrecioUnitario = art.PrecioVenta,
            PrecioCosto = art.PrecioCosto,
            Cantidad = 1,
            EsCombo = art.Tipo == TipoArticulo.ComboKit,
            EsServicio = art.Tipo == TipoArticulo.Servicio
        };

        nuevo.PropertyChanged += (s, e) => RecalcularTotales();
        Items.Add(nuevo);
        MensajeEstado = $"Agregado: {art.Nombre}";
    }

    [RelayCommand]
    private void IncrementarCantidad(PosItemModel? item)
    {
        if (item == null) return;
        item.Cantidad += 1;
        RecalcularTotales();
    }

    [RelayCommand]
    private void DecrementarCantidad(PosItemModel? item)
    {
        if (item == null) return;
        if (item.Cantidad > 1)
        {
            item.Cantidad -= 1;
        }
        else
        {
            Items.Remove(item);
        }
        RecalcularTotales();
    }

    [RelayCommand]
    private void EliminarItem(PosItemModel? item)
    {
        if (item != null && Items.Contains(item))
        {
            Items.Remove(item);
            RecalcularTotales();
        }
    }

    [RelayCommand]
    private void LimpiarTicket()
    {
        Items.Clear();
        RecalcularTotales();
        MensajeEstado = "Ticket cancelado / limpio.";
    }

    [RelayCommand]
    private void AgregarVentaManual(string? descripcion)
    {
        var desc = string.IsNullOrWhiteSpace(descripcion) ? "Venta Rápida / Mostrador" : descripcion;
        var nuevo = new PosItemModel
        {
            ArticuloId = null,
            SKU = "MANUAL",
            CodigoBarras = null,
            Descripcion = desc,
            PrecioUnitario = 100m,
            PrecioCosto = 0m,
            Cantidad = 1,
            EsVentaManual = true
        };

        nuevo.PropertyChanged += (s, e) => RecalcularTotales();
        Items.Add(nuevo);
        MensajeEstado = "Artículo manual agregado. Ajuste precio y cantidad.";
    }

    [RelayCommand]
    private async Task AbrirCobroAsync()
    {
        if (Items.Count == 0)
        {
            MensajeEstado = "El ticket está vacío. Agregue artículos antes de cobrar.";
            return;
        }

        if (TurnoActivoId == null)
        {
            await RecargarCajaAsync();
        }

        var cobroVm = new CobroModalViewModel(TotalVenta);

        if (SolicitarCobroDialogo != null)
        {
            var confirmado = await SolicitarCobroDialogo(cobroVm);
            if (confirmado && cobroVm.VentaConfirmada)
            {
                await ProcesarVentaFinalizadaAsync(cobroVm);
            }
        }
    }

    private async Task ProcesarVentaFinalizadaAsync(CobroModalViewModel cobroVm)
    {
        try
        {
            var ventaDto = new RegistrarVentaDto
            {
                TurnoCajaId = TurnoActivoId!.Value,
                VendedoraNombre = UsuarioActual,
                MetodoPago = cobroVm.MetodoPago,
                CantidadCuotas = cobroVm.CantidadCuotas,
                DescuentoEfectivoMonto = cobroVm.DescuentoEfectivoMonto,
                RecargoCuotasMonto = cobroVm.RecargoCuotasMonto,
                Items = Items.Select(i => new ItemCarritoDto
                {
                    ArticuloId = i.ArticuloId,
                    SKU = i.SKU,
                    CodigoBarras = i.CodigoBarras,
                    Descripcion = i.Descripcion,
                    Cantidad = i.Cantidad,
                    PrecioUnitario = i.PrecioUnitario,
                    PrecioCosto = i.PrecioCosto,
                    EsVentaManual = i.EsVentaManual,
                    EsCombo = i.EsCombo
                }).ToList()
            };

            var resultado = await _ventaService.ProcesarVentaAsync(ventaDto);

            NumeroComprobanteUltimaVenta = resultado.NumeroComprobante;
            MensajeEstado = $"¡Venta {resultado.NumeroComprobante} confirmada exitosamente! Total: ${resultado.TotalCobrado:N2}";

            Items.Clear();
            RecalcularTotales();
        }
        catch (Exception ex)
        {
            MensajeEstado = $"Error al procesar cobro: {ex.Message}";
        }
    }

    public void RecalcularTotales()
    {
        SubtotalBruto = Items.Sum(i => i.Subtotal);
        TotalDescuentos = 0; // Descuentos por cupón o globales
        TotalVenta = SubtotalBruto - TotalDescuentos;
        CantidadArticulos = Items.Sum(i => i.Cantidad);
    }
}
