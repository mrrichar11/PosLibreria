using CommunityToolkit.Mvvm.ComponentModel;

namespace PuntoDeVentaLibreria.UI.ViewModels;

public partial class PosItemModel : ObservableObject
{
    public Guid? ArticuloId { get; set; }
    public Guid? ArticuloVarianteId { get; set; }
    public string? VarianteNombre { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string? CodigoBarras { get; set; }
    public string Descripcion { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Subtotal))]
    private decimal _cantidad = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Subtotal))]
    private decimal _precioUnitario;

    public decimal PrecioCosto { get; set; }
    public bool EsCombo { get; set; }
    public bool EsServicio { get; set; }
    public bool EsVentaManual { get; set; }

    // Campos de Precio Dólar (USD - Joyería / Regalería importada)
    public bool EsPrecioDolar { get; set; }
    public decimal PrecioCostoDolar { get; set; }
    public string? DetalleDolar { get; set; }

    public decimal Subtotal => Cantidad * PrecioUnitario;
}
