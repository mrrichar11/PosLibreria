using System.Globalization;
using System.Windows;
using System.Windows.Input;
using PuntoDeVentaLibreria.Application.DTOs.Inventario;
using PuntoDeVentaLibreria.Application.Services;
using PuntoDeVentaLibreria.Domain.Entities.Catalogo;

namespace PuntoDeVentaLibreria.UI.Views.Pos;

public partial class ArticuloRapidoModalWindow : Window
{
    private readonly IInventarioService _inventarioService;
    public ArticuloDto? ArticuloCreado { get; private set; }

    public ArticuloRapidoModalWindow(IInventarioService inventarioService, string codigoInicial = "")
    {
        _inventarioService = inventarioService ?? throw new ArgumentNullException(nameof(inventarioService));
        InitializeComponent();

        if (!string.IsNullOrWhiteSpace(codigoInicial))
        {
            TxtCodigoBarras.Text = codigoInicial.Trim();
        }

        Loaded += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(TxtCodigoBarras.Text))
            {
                TxtCodigoBarras.Focus();
            }
            else
            {
                TxtNombre.Focus();
            }
        };

        PreviewKeyDown += (s, e) =>
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        };
    }

    private async void BtnAutoCodigo_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            TxtCodigoBarras.Text = await _inventarioService.GenerarCodigoBarrasSugeridoAsync();
            TxtNombre.Focus();
        }
        catch { }
    }

    private void RbLibreria_Checked(object sender, RoutedEventArgs e)
    {
    }

    private void RbRegaleria_Checked(object sender, RoutedEventArgs e)
    {
    }

    private void TxtNombre_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            TxtPrecioVenta.Focus();
            TxtPrecioVenta.SelectAll();
        }
    }

    private void TxtPrecioVenta_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            GuardarYAgregar();
        }
    }

    private void BtnGuardar_Click(object sender, RoutedEventArgs e)
    {
        GuardarYAgregar();
    }

    private async void GuardarYAgregar()
    {
        var nombre = TxtNombre.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(nombre))
        {
            MessageBox.Show("Por favor, ingrese la descripción o nombre del artículo.", "Dato requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtNombre.Focus();
            return;
        }

        var ventaTxt = TxtPrecioVenta.Text?.Trim().Replace("$", "").Trim() ?? "0";
        if (!decimal.TryParse(ventaTxt, NumberStyles.Any, CultureInfo.CurrentCulture, out var precioVenta) &&
            !decimal.TryParse(ventaTxt, NumberStyles.Any, CultureInfo.InvariantCulture, out precioVenta))
        {
            MessageBox.Show("Por favor, ingrese un precio de venta válido.", "Precio incorrecto", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtPrecioVenta.Focus();
            return;
        }

        if (precioVenta <= 0)
        {
            MessageBox.Show("El precio de venta debe ser mayor a 0.", "Precio requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtPrecioVenta.Focus();
            return;
        }

        var costoTxt = TxtPrecioCosto.Text?.Trim().Replace("$", "").Trim() ?? "0";
        decimal.TryParse(costoTxt, NumberStyles.Any, CultureInfo.CurrentCulture, out var precioCosto);
        if (precioCosto <= 0)
        {
            decimal.TryParse(costoTxt, NumberStyles.Any, CultureInfo.InvariantCulture, out precioCosto);
        }

        var stockTxt = TxtStockInicial.Text?.Trim() ?? "1";
        decimal.TryParse(stockTxt, NumberStyles.Any, CultureInfo.InvariantCulture, out var stockInicial);
        if (stockInicial < 0) stockInicial = 0;

        string codigoBarras = TxtCodigoBarras.Text?.Trim() ?? string.Empty;
        string rubro = (RbRegaleria.IsChecked == true) ? "Regalería" : "Librería";

        try
        {
            var sku = await _inventarioService.GenerarSkuSugeridoAsync();

            var dto = new ArticuloDto
            {
                Id = Guid.Empty,
                Nombre = nombre,
                CodigoBarras = string.IsNullOrWhiteSpace(codigoBarras) ? null : codigoBarras,
                SKU = sku,
                Rubro = rubro,
                PrecioVenta = precioVenta,
                PrecioCosto = precioCosto,
                PorcentajeGanancia = (precioCosto > 0 && precioVenta > precioCosto) ? Math.Round(((precioVenta - precioCosto) / precioCosto) * 100, 1) : 60,
                StockActual = stockInicial,
                StockMinimo = 5,
                Tipo = TipoArticulo.Estandar,
                IvaPorcentaje = 21.0m,
                EsBotonRapido = false
            };

            var guardado = await _inventarioService.GuardarArticuloAsync(dto);
            ArticuloCreado = guardado;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al guardar el artículo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
