using System.Windows;
using System.Windows.Input;
using PuntoDeVentaLibreria.Application.Common;
using PuntoDeVentaLibreria.Application.DTOs.Inventario;
using PuntoDeVentaLibreria.Application.Services;
using PuntoDeVentaLibreria.Domain.Entities.Auditoria;
using PuntoDeVentaLibreria.Domain.Entities.Catalogo;
using PuntoDeVentaLibreria.UI.Views.Inventario;

namespace PuntoDeVentaLibreria.UI.Views.Pos;

public partial class ConsultarPrecioModalWindow : Window
{
    private readonly IInventarioService _inventarioService;
    private readonly decimal _porcentajeDescuentoEfectivo;
    private readonly decimal _cotizacionDolar;
    private string _ultimoQueryBuscado = string.Empty;

    public ArticuloDto? ArticuloSeleccionado { get; private set; }

    public ConsultarPrecioModalWindow(
        IInventarioService inventarioService, 
        decimal porcentajeDescuentoEfectivo = 10m,
        decimal cotizacionDolar = 1350m)
    {
        InitializeComponent();
        _inventarioService = inventarioService ?? throw new ArgumentNullException(nameof(inventarioService));
        _porcentajeDescuentoEfectivo = porcentajeDescuentoEfectivo;
        _cotizacionDolar = cotizacionDolar > 0 ? cotizacionDolar : 1350m;

        TxtPorcentajeDesc.Text = $" (-{_porcentajeDescuentoEfectivo:0.#}%)";

        Loaded += (s, e) =>
        {
            TxtBusqueda.Focus();
        };

        PreviewKeyDown += ConsultarPrecioModalWindow_PreviewKeyDown;
    }

    private async void ConsultarPrecioModalWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.F8)
        {
            AbrirListaHistorica();
            e.Handled = true;
            return;
        }

        // Atajos para alternar entre Pack y Unidad vinculada
        if ((e.Key == Key.U || e.Key == Key.P || e.Key == Key.F2) && 
            !TxtBusqueda.IsKeyboardFocused && 
            ArticuloSeleccionado != null && 
            ArticuloSeleccionado.TieneVinculoPackOUnidad)
        {
            await AgregarArticuloVinculadoAsync();
            e.Handled = true;
            return;
        }

        // Si se presiona Enter fuera de la caja de texto y hay un artículo en pantalla, agregarlo al ticket
        if (e.Key == Key.Enter && !TxtBusqueda.IsKeyboardFocused && ArticuloSeleccionado != null)
        {
            BtnAgregarAlTicket_Click(sender, e);
            e.Handled = true;
        }
    }

    private async void BtnBuscar_Click(object sender, RoutedEventArgs e)
    {
        await RealizarBusquedaAsync();
    }

    private async void TxtBusqueda_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var query = TxtBusqueda.Text?.Trim() ?? string.Empty;

            // Si ya hay un artículo cargado en pantalla:
            // Si el usuario presiona Enter y el texto en el buscador no cambió (o es el código/sku del artículo cargado o está vacío),
            // significa que confirma que desea agregar este artículo al carrito!
            if (ArticuloSeleccionado != null &&
                (string.IsNullOrWhiteSpace(query) ||
                 string.Equals(query, _ultimoQueryBuscado, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(query, ArticuloSeleccionado.CodigoBarras, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(query, ArticuloSeleccionado.SKU, StringComparison.OrdinalIgnoreCase)))
            {
                BtnAgregarAlTicket_Click(sender, e);
                e.Handled = true;
                return;
            }

            // De lo contrario, se trata de una nueva búsqueda de artículo
            await RealizarBusquedaAsync();
            e.Handled = true;
        }
    }

    private async Task RealizarBusquedaAsync()
    {
        var query = TxtBusqueda.Text?.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            TxtBusqueda.Focus();
            return;
        }

        try
        {
            Mouse.OverrideCursor = Cursors.Wait;

            // 1. Buscar coincidencia exacta por código de barras o SKU
            var art = await _inventarioService.BuscarPorCodigoBarrasAsync(query);

            // 2. Si no encuentra exacto, buscar por coincidencia en texto (SKU o Nombre)
            if (art == null)
            {
                var lista = await _inventarioService.BuscarArticulosAsync(query);
                if (lista != null && lista.Count > 0)
                {
                    art = lista[0];
                }
            }

            if (art != null)
            {
                await _inventarioService.EnriquecerVinculoPackOUnidadAsync(art);
                _ultimoQueryBuscado = query;
                MostrarArticulo(art);
            }
            else
            {
                // Buscar si existe en la lista histórica del sistema anterior para dar respuesta al cajero
                var historico = await _inventarioService.BuscarArticuloHistoricoPorCodigoOBarrasAsync(query);
                MostrarNoEncontrado(query, historico);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al consultar el artículo: {ex.Message}", "Error de Consulta", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            Mouse.OverrideCursor = null;
            TxtBusqueda.SelectAll();
            TxtBusqueda.Focus();
        }
    }

    private void MostrarArticulo(ArticuloDto art)
    {
        ArticuloSeleccionado = art;

        decimal precioTarjeta = (art.PrecioTarjeta > 0 && art.PrecioTarjeta >= art.PrecioVenta) ? art.PrecioTarjeta : Math.Round(art.PrecioVenta * 1.25m, 2);
        decimal precioEfectivo = art.PrecioVenta;
        string detalleExtra = string.Empty;

        if (art.EsPrecioDolar && art.PrecioCostoDolar > 0)
        {
            decimal costoPesos = Math.Round(art.PrecioCostoDolar * _cotizacionDolar, 2);
            decimal pCalculado = CalculoPreciosUtils.CalcularPrecioVenta(costoPesos, art.PorcentajeGanancia, art.IvaPorcentaje);
            precioEfectivo = CalculoPreciosUtils.RedondearPrecioVenta(pCalculado, ReglaRedondeoPrecio.CentenaCercana);
            precioTarjeta = Math.Round(precioEfectivo * 1.25m, 2);
            detalleExtra = $"💵 Cotización Dólar: USD ${art.PrecioCostoDolar:N2} x ${_cotizacionDolar:N0}";
        }

        decimal ahorro = Math.Max(0m, precioTarjeta - precioEfectivo);
        decimal porcAhorro = precioTarjeta > 0 && ahorro > 0 ? Math.Round(ahorro / precioTarjeta * 100m, 1) : _porcentajeDescuentoEfectivo;

        TxtNombreArticulo.Text = art.Nombre;
        TxtSkuArticulo.Text = $"SKU: {art.SKU}";
        TxtCodigoArticulo.Text = string.IsNullOrWhiteSpace(art.CodigoBarras) ? "Sin Código EAN" : $"Cód: {art.CodigoBarras}";
        TxtRubroArticulo.Text = string.IsNullOrWhiteSpace(art.CategoriaNombre) ? "General" : art.CategoriaNombre;

        TxtPrecioTarjeta.Text = $"${precioTarjeta:N2}";
        TxtPrecioEfectivo.Text = $"${precioEfectivo:N2}";
        TxtPorcentajeDesc.Text = $" (-{porcAhorro:0.#}%)";
        TxtAhorroEfectivo.Text = $"Ahorro: ${ahorro:N2} (-{porcAhorro:0.#}%)";

        TxtStock.Text = $"{art.StockActual:0.##} unidades en existencia";
        TxtDetalleExtra.Text = detalleExtra;

        // Visualización de Vínculo Pack / Suelto (-1)
        if (art.TieneVinculoPackOUnidad)
        {
            PanelVinculoPackUnidad.Visibility = Visibility.Visible;
            BtnLlevarVinculadoInferior.Visibility = Visibility.Visible;

            decimal pTarjetaVinc = (art.ArticuloVinculadoPrecioTarjeta > 0 && art.ArticuloVinculadoPrecioTarjeta >= art.ArticuloVinculadoPrecioVenta) 
                ? art.ArticuloVinculadoPrecioTarjeta 
                : Math.Round(art.ArticuloVinculadoPrecioVenta * 1.25m, 2);
            decimal pEfectivoVinc = art.ArticuloVinculadoPrecioVenta;

            TxtNombreVinculado.Text = art.ArticuloVinculadoNombre;
            TxtSkuVinculado.Text = $"SKU: {art.ArticuloVinculadoSKU}";
            TxtPrecioTarjetaVinculado.Text = $"${pTarjetaVinc:N2}";
            TxtPrecioEfectivoVinculado.Text = $"${pEfectivoVinc:N2}";

            if (art.EsPack)
            {
                TxtBadgeTipo.Text = $"📦 PACK (x{art.CantidadPorPack:0.#} u.)";
                TxtBadgeTipo.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(238, 242, 255));
                TxtBadgeTipo.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(79, 70, 229));

                TxtTituloVinculado.Text = "🔗 VERSIÓN INDIVIDUAL SUELTA DISPONIBLE (-1):";
                BtnLlevarVinculado.Content = "➕ Llevar Unidad Suelta [U]";
                BtnLlevarVinculadoInferior.Content = "➕ Llevar Unidad Suelta [U]";
                BtnAgregarAlTicket.Content = $"➕ Llevar Pack Completo [ENTER]";
            }
            else
            {
                TxtBadgeTipo.Text = "🏷️ UNIDAD INDIVIDUAL";
                TxtBadgeTipo.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 253, 244));
                TxtBadgeTipo.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(22, 163, 74));

                TxtTituloVinculado.Text = $"🔗 PACK COMPLETO VINCULADO (x{art.ArticuloVinculadoCantidadPorPack:0.#} u.):";
                BtnLlevarVinculado.Content = "➕ Llevar Pack Completo [P]";
                BtnLlevarVinculadoInferior.Content = "➕ Llevar Pack Completo [P]";
                BtnAgregarAlTicket.Content = $"➕ Llevar Unidad Suelta [ENTER]";
            }
        }
        else
        {
            PanelVinculoPackUnidad.Visibility = Visibility.Collapsed;
            BtnLlevarVinculadoInferior.Visibility = Visibility.Collapsed;
            TxtBadgeTipo.Text = art.EsPack ? $"📦 PACK (x{art.CantidadPorPack:0.#} u.)" : "🏷️ ARTÍCULO";
            BtnAgregarAlTicket.Content = "➕ Agregar al Carrito [ENTER]";
        }

        PanelInicial.Visibility = Visibility.Collapsed;
        PanelNoEncontrado.Visibility = Visibility.Collapsed;
        PanelResultado.Visibility = Visibility.Visible;

        BtnAgregarAlTicket.IsEnabled = true;
    }

    private void MostrarNoEncontrado(string query, ArticuloHistoricoSistemaAnterior? historico)
    {
        ArticuloSeleccionado = null;
        _ultimoQueryBuscado = string.Empty;
        TxtNoEncontradoDetalle.Text = $"No se encontró ningún artículo activo con '{query}'.";

        if (historico != null)
        {
            PanelEncontradoHistorico.Visibility = Visibility.Visible;
            TxtHistoricoDetalle.Text = $"Código: {historico.Codigo} | {historico.Descripcion}\n" +
                                       $"Precio Original: ${historico.PrecioVenta:N2} | Rubro: {historico.Rubro} | Proveedor: {historico.Proveedor}";
        }
        else
        {
            PanelEncontradoHistorico.Visibility = Visibility.Collapsed;
        }

        PanelInicial.Visibility = Visibility.Collapsed;
        PanelResultado.Visibility = Visibility.Collapsed;
        PanelNoEncontrado.Visibility = Visibility.Visible;

        BtnAgregarAlTicket.IsEnabled = false;
        BtnLlevarVinculadoInferior.Visibility = Visibility.Collapsed;
    }

    private void BtnAgregarAlTicket_Click(object sender, RoutedEventArgs e)
    {
        if (ArticuloSeleccionado != null)
        {
            DialogResult = true;
            Close();
        }
    }

    private async void BtnLlevarVinculado_Click(object sender, RoutedEventArgs e)
    {
        await AgregarArticuloVinculadoAsync();
    }

    private async Task AgregarArticuloVinculadoAsync()
    {
        if (ArticuloSeleccionado?.ArticuloVinculadoId != null)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                var vinculado = await _inventarioService.ObtenerPorIdAsync(ArticuloSeleccionado.ArticuloVinculadoId.Value);
                if (vinculado != null)
                {
                    ArticuloSeleccionado = vinculado;
                    DialogResult = true;
                    Close();
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el artículo vinculado: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }
    }

    private void BtnAbrirListaAnterior_Click(object sender, RoutedEventArgs e)
    {
        AbrirListaHistorica();
    }

    private void BtnConsultarArticuloEnListaAnterior_Click(object sender, RoutedEventArgs e)
    {
        AbrirListaHistorica(ArticuloSeleccionado?.CodigoBarras ?? ArticuloSeleccionado?.SKU ?? ArticuloSeleccionado?.Nombre);
    }

    private void AbrirListaHistorica(string? queryInicial = null)
    {
        var modal = new ConsultaListaAnteriorModalWindow(_inventarioService)
        {
            Owner = this
        };

        var query = queryInicial ?? TxtBusqueda.Text?.Trim();
        if (!string.IsNullOrWhiteSpace(query))
        {
            modal.PreestablecerBusqueda(query);
        }

        modal.ShowDialog();
    }

    private void BtnCerrar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
