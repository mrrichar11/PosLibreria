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
    private readonly IClienteService _clienteService;
    private readonly IConfiguracionService _configuracionService;
    private readonly ITicketPrinterService _ticketPrinterService;

    [ObservableProperty]
    private string _codigoBarrasInput = string.Empty;

    [ObservableProperty]
    private string _usuarioActual = "Cajero Principal";

    [ObservableProperty]
    private string _mensajeEstado = "Listo para escanear o buscar artículos.";

    [ObservableProperty]
    private decimal _subtotalBruto;

    [ObservableProperty]
    private decimal _totalDescuentos;

    [ObservableProperty]
    private decimal _totalVenta;

    [ObservableProperty]
    private bool _esCajaAbierta;

    [ObservableProperty]
    private string _numeroComprobanteUltimaVenta = "---";

    [ObservableProperty]
    private decimal _cantidadArticulos;

    private int _contadorEspera = 1;
    private VentaRealizadaDto? _ultimaVentaRealizada;

    public ObservableCollection<PosItemModel> Items { get; } = new();
    public ObservableCollection<ArticuloDto> BotonesRapidos { get; } = new();
    public ObservableCollection<VentaEnEsperaDto> VentasEnEspera { get; } = new();

    public bool HayVentasEnEspera => VentasEnEspera.Count > 0;
    public bool HayItemsEnCarrito => Items.Count > 0;
    public bool TieneUltimaVenta => _ultimaVentaRealizada != null;

    public Guid? TurnoActivoId { get; private set; }

    // Delegados para interacción modal con la View
    public Func<CobroModalViewModel, Task<bool>>? SolicitarCobroDialogo { get; set; }
    public Func<IReadOnlyList<ArticuloDto>, string, Task<ArticuloDto?>>? SolicitarSeleccionArticulo { get; set; }
    public Func<Task<(string descripcion, decimal precio, decimal cantidad)?>>? SolicitarVentaManualDialogo { get; set; }
    public Func<string, string, Task<bool>>? SolicitarConfirmacionDialogo { get; set; }
    public Func<string, string, int, string, Task>? SolicitarVistaPreviaTicket { get; set; }

    public PosViewModel(
        IInventarioService inventarioService,
        IVentaService ventaService,
        ICajaService cajaService,
        IClienteService clienteService,
        IConfiguracionService configuracionService,
        ITicketPrinterService ticketPrinterService)
    {
        _inventarioService = inventarioService ?? throw new ArgumentNullException(nameof(inventarioService));
        _ventaService = ventaService ?? throw new ArgumentNullException(nameof(ventaService));
        _cajaService = cajaService ?? throw new ArgumentNullException(nameof(cajaService));
        _clienteService = clienteService ?? throw new ArgumentNullException(nameof(clienteService));
        _configuracionService = configuracionService ?? throw new ArgumentNullException(nameof(configuracionService));
        _ticketPrinterService = ticketPrinterService ?? throw new ArgumentNullException(nameof(ticketPrinterService));

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
        if (art != null)
        {
            AgregarArticuloAlTicket(art);
            return;
        }

        // 2. Si no encuentra por código de barras exacto, buscar coincidencias por SKU, nombre o descripción
        var coincidencias = await _inventarioService.BuscarArticulosAsync(query);

        if (coincidencias == null || coincidencias.Count == 0)
        {
            MensajeEstado = $"No se encontró ningún artículo para: '{query}'";
            return;
        }

        // Si hay una sola coincidencia y su SKU coincide exactamente con el texto buscado
        if (coincidencias.Count == 1 && string.Equals(coincidencias[0].SKU, query, StringComparison.OrdinalIgnoreCase))
        {
            AgregarArticuloAlTicket(coincidencias[0]);
            return;
        }

        // Si hay múltiples resultados o la búsqueda fue por texto/descripción, abrir selector para el cajero
        if (SolicitarSeleccionArticulo != null)
        {
            var seleccionado = await SolicitarSeleccionArticulo(coincidencias, query);
            if (seleccionado != null)
            {
                AgregarArticuloAlTicket(seleccionado);
            }
            else
            {
                MensajeEstado = "Búsqueda cancelada por el usuario.";
            }
        }
        else
        {
            AgregarArticuloAlTicket(coincidencias[0]);
        }
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
        if (Items.Count == 0) return;
        Items.Clear();
        RecalcularTotales();
        MensajeEstado = "Ticket cancelado / limpio.";
    }

    [RelayCommand]
    private async Task LimpiarTicketConConfirmacionAsync()
    {
        if (Items.Count == 0)
        {
            MensajeEstado = "El carrito ya está vacío.";
            return;
        }

        if (SolicitarConfirmacionDialogo != null)
        {
            var confirmar = await SolicitarConfirmacionDialogo(
                "¿Desea cancelar y vaciar el ticket actual?",
                "Vaciar Carrito [F12]");
            if (!confirmar) return;
        }

        Items.Clear();
        RecalcularTotales();
        MensajeEstado = "Ticket cancelado y vaciado.";
    }

    [RelayCommand]
    private void PausarVentaActual()
    {
        if (Items.Count == 0)
        {
            if (VentasEnEspera.Count > 0)
            {
                ReanudarVentaEnEspera(VentasEnEspera[0]);
                return;
            }

            MensajeEstado = "El carrito está vacío. No hay artículos para poner en espera.";
            return;
        }

        var listaClonada = Items.Select(i => new PosItemModel
        {
            ArticuloId = i.ArticuloId,
            SKU = i.SKU,
            CodigoBarras = i.CodigoBarras,
            Descripcion = i.Descripcion,
            PrecioUnitario = i.PrecioUnitario,
            PrecioCosto = i.PrecioCosto,
            Cantidad = i.Cantidad,
            EsCombo = i.EsCombo,
            EsServicio = i.EsServicio,
            EsVentaManual = i.EsVentaManual
        }).ToList();

        var ticketNum = _contadorEspera++;
        var espera = new VentaEnEsperaDto
        {
            NumeroTicket = ticketNum,
            FechaHora = DateTime.Now,
            Items = listaClonada,
            TotalVenta = TotalVenta,
            CantidadArticulos = CantidadArticulos
        };

        VentasEnEspera.Add(espera);
        Items.Clear();
        RecalcularTotales();
        OnPropertyChanged(nameof(HayVentasEnEspera));
        MensajeEstado = $"🟡 Venta puesta en espera (Ticket #{ticketNum} por ${espera.TotalVenta:N2}). Pantalla libre para nueva venta.";
    }

    [RelayCommand]
    private void ReanudarVentaEnEspera(VentaEnEsperaDto? espera)
    {
        if (espera == null) return;

        // Si el carrito actual tiene artículos, auto-pausarlo en espera para no perderlo
        if (Items.Count > 0)
        {
            var actualPausado = new VentaEnEsperaDto
            {
                NumeroTicket = _contadorEspera++,
                FechaHora = DateTime.Now,
                Items = Items.Select(i => new PosItemModel
                {
                    ArticuloId = i.ArticuloId,
                    SKU = i.SKU,
                    CodigoBarras = i.CodigoBarras,
                    Descripcion = i.Descripcion,
                    PrecioUnitario = i.PrecioUnitario,
                    PrecioCosto = i.PrecioCosto,
                    Cantidad = i.Cantidad,
                    EsCombo = i.EsCombo,
                    EsServicio = i.EsServicio,
                    EsVentaManual = i.EsVentaManual
                }).ToList(),
                TotalVenta = TotalVenta,
                CantidadArticulos = CantidadArticulos
            };
            VentasEnEspera.Add(actualPausado);
        }

        Items.Clear();
        foreach (var item in espera.Items)
        {
            item.PropertyChanged += (s, e) => RecalcularTotales();
            Items.Add(item);
        }

        VentasEnEspera.Remove(espera);
        RecalcularTotales();
        OnPropertyChanged(nameof(HayVentasEnEspera));
        MensajeEstado = $"🟢 Reanudada venta en espera #{espera.NumeroTicket} (${espera.TotalVenta:N2}).";
    }

    [RelayCommand]
    private async Task DescartarVentaEnEsperaAsync(VentaEnEsperaDto? espera)
    {
        if (espera == null) return;

        if (SolicitarConfirmacionDialogo != null)
        {
            var confirmar = await SolicitarConfirmacionDialogo(
                $"¿Desea descartar y eliminar el ticket en espera #{espera.NumeroTicket} (${espera.TotalVenta:N2})?",
                "Descartar Venta en Espera");
            if (!confirmar) return;
        }

        VentasEnEspera.Remove(espera);
        OnPropertyChanged(nameof(HayVentasEnEspera));
        MensajeEstado = $"Ticket en espera #{espera.NumeroTicket} descartado.";
    }

    [RelayCommand]
    private async Task AgregarVentaManualAsync(string? descripcion)
    {
        if (SolicitarVentaManualDialogo != null)
        {
            var res = await SolicitarVentaManualDialogo();
            if (res.HasValue)
            {
                var (desc, precio, cant) = res.Value;
                var nuevoManual = new PosItemModel
                {
                    ArticuloId = null,
                    SKU = "MANUAL",
                    CodigoBarras = null,
                    Descripcion = desc,
                    PrecioUnitario = precio,
                    PrecioCosto = 0m,
                    Cantidad = cant,
                    EsVentaManual = true
                };

                nuevoManual.PropertyChanged += (s, e) => RecalcularTotales();
                Items.Add(nuevoManual);
                RecalcularTotales();
                MensajeEstado = $"Venta manual agregada: {desc} x{cant} (${precio:N2})";
            }
            return;
        }

        var descDef = string.IsNullOrWhiteSpace(descripcion) ? "Venta Rápida / Mostrador" : descripcion;
        var nuevo = new PosItemModel
        {
            ArticuloId = null,
            SKU = "MANUAL",
            CodigoBarras = null,
            Descripcion = descDef,
            PrecioUnitario = 100m,
            PrecioCosto = 0m,
            Cantidad = 1,
            EsVentaManual = true
        };

        nuevo.PropertyChanged += (s, e) => RecalcularTotales();
        Items.Add(nuevo);
        RecalcularTotales();
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

        var config = await _configuracionService.ObtenerConfiguracionAsync();
        var clientes = await _clienteService.BuscarClientesAsync(string.Empty);
        var cobroVm = new CobroModalViewModel(TotalVenta, clientes, config.BilletesHabilitados, config.SimboloMoneda);

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
                ClienteId = cobroVm.ClienteSeleccionado?.Id,
                ClienteNombre = cobroVm.NombreCliente,
                ReferenciaPago = cobroVm.ReferenciaPago,
                VendedoraNombre = UsuarioActual,
                MetodoPago = cobroVm.MetodoPago,
                CantidadCuotas = cobroVm.CantidadCuotas,
                DescuentoEfectivoMonto = cobroVm.DescuentoEfectivoMonto,
                RecargoCuotasMonto = cobroVm.RecargoCuotasMonto,
                MontoEntregado = cobroVm.MontoEntregado,
                Vuelto = cobroVm.Vuelto,
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

            _ultimaVentaRealizada = resultado;
            NumeroComprobanteUltimaVenta = resultado.NumeroComprobante;
            OnPropertyChanged(nameof(TieneUltimaVenta));

            MensajeEstado = $"¡Venta {resultado.NumeroComprobante} confirmada exitosamente ({cobroVm.NombreCliente})! Total: ${resultado.TotalCobrado:N2}";

            Items.Clear();
            RecalcularTotales();

            // Gestionar generación y vista previa / impresión del ticket térmico
            var config = await _configuracionService.ObtenerConfiguracionAsync();
            var configTicket = new PuntoDeVentaLibreria.Application.DTOs.Peripherals.ConfiguracionTicketDto
            {
                NombreComercio = config.NombreComercio,
                Direccion = config.Direccion,
                Telefono = config.Telefono,
                Cuit = config.Cuit,
                AnchoPapelMm = config.AnchoPapelMm,
                ImpresoraNombre = config.ImpresoraTickets,
                MensajePie = config.MensajePieTicket
            };

            var textoTicket = await _ticketPrinterService.GenerarTicketTextoAsync(resultado, configTicket);
            await _ticketPrinterService.ImprimirTicketVentaAsync(resultado, configTicket);

            if (config.MostrarVistaPreviaTicket && SolicitarVistaPreviaTicket != null)
            {
                await SolicitarVistaPreviaTicket(textoTicket, resultado.NumeroComprobante, config.AnchoPapelMm, config.ImpresoraTickets);
            }
        }
        catch (Exception ex)
        {
            MensajeEstado = $"Error al procesar cobro: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ReimprimirUltimoTicketAsync()
    {
        if (_ultimaVentaRealizada == null)
        {
            MensajeEstado = "No hay ventas registradas en esta sesión para reimprimir.";
            return;
        }

        var config = await _configuracionService.ObtenerConfiguracionAsync();
        var configTicket = new PuntoDeVentaLibreria.Application.DTOs.Peripherals.ConfiguracionTicketDto
        {
            NombreComercio = config.NombreComercio,
            Direccion = config.Direccion,
            Telefono = config.Telefono,
            Cuit = config.Cuit,
            AnchoPapelMm = config.AnchoPapelMm,
            ImpresoraNombre = config.ImpresoraTickets,
            MensajePie = config.MensajePieTicket
        };

        var textoTicket = await _ticketPrinterService.GenerarTicketTextoAsync(_ultimaVentaRealizada, configTicket);

        if (SolicitarVistaPreviaTicket != null)
        {
            await SolicitarVistaPreviaTicket(textoTicket, _ultimaVentaRealizada.NumeroComprobante, config.AnchoPapelMm, config.ImpresoraTickets);
        }
    }

    public void RecalcularTotales()
    {
        SubtotalBruto = Items.Sum(i => i.Subtotal);
        TotalDescuentos = 0; // Descuentos por cupón o globales
        TotalVenta = SubtotalBruto - TotalDescuentos;
        CantidadArticulos = Items.Sum(i => i.Cantidad);
        OnPropertyChanged(nameof(HayItemsEnCarrito));
        OnPropertyChanged(nameof(HayVentasEnEspera));
    }
}
